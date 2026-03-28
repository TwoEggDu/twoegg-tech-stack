---
date: "2026-03-28"
title: "Unity 脚本编译管线 03｜Domain Reload：为什么改一行代码要等那么久"
description: "解释 Unity 编辑器里的 Domain Reload 是什么：脚本编译完之后为什么还要重新加载所有程序集，这个过程做了哪些事，为什么代价高，以及怎么通过 EnterPlayMode 设置来减少等待。"
slug: "unity-script-compilation-pipeline-03-domain-reload"
weight: 64
featured: false
tags:
  - "Unity"
  - "Domain Reload"
  - "Editor"
  - "Compilation"
  - "Performance"
series: "Unity 脚本编译管线"
series_order: 3
---

> 编译只是第一段等待，`Domain Reload` 才是第二段——而且往往更长。

---

## 这篇要回答什么

1. 编译完了，新代码为什么还不能用？
2. `Domain Reload` 具体做了哪些事？
3. 为什么改一行代码也要触发全量重载？
4. 怎么通过 `EnterPlayMode` 设置把等待缩到最短？

---

## 场景还原

你在 Unity 编辑器里修了一个拼写错误，保存，右下角出现编译进度条，转完，然后你点播放——又转。或者你根本没点播放，编辑器就在那里转了五秒、十秒，窗口标题变成"Importing…"。

这个等待其实分两段：

| 阶段 | 做什么 | 典型耗时 |
|------|--------|----------|
| 编译 | Roslyn 把 .cs 转成 .dll | 1–10 秒 |
| **Domain Reload** | 把新 .dll 加载进编辑器进程 | 2–30 秒 |

第一段的主角是 Roslyn 和 ILPP，前两篇已经讲过。这篇只讲第二段。

---

## 编译完了不等于可以用

Roslyn 编译结束后，新的 `.dll` 文件已经写到磁盘的 `Library/ScriptAssemblies/` 目录里了。但这不代表编辑器进程就能立刻使用新代码。

原因很简单：Unity 编辑器是一个常驻的 .NET 进程。进程里已经加载了一套旧的程序集——那套旧代码还活着，占着内存。.NET 的程序集一旦加载就无法从 `AppDomain` 里卸载（单个程序集不支持卸载），所以唯一的办法是把整个 `AppDomain` 销毁，重新创建一个，再把所有程序集重新加载进来。

这就是 `Domain Reload`。

---

## Domain Reload 做了什么

整个过程按顺序分五步：

### 1. 序列化编辑器状态

卸载旧程序集之前，Unity 必须先把当前编辑器的状态保存下来，否则重载完之后一切都丢了——你打开的 Inspector 绑的是哪个对象、ScriptableObject 的值是什么、自定义 EditorWindow 里的字段……全部需要序列化成字节流暂存。

项目越大、编辑器扩展越多，这一步越慢。

### 2. 卸载旧 AppDomain

Unity 销毁当前的 `AppDomain`。这会把所有已加载的托管程序集一并卸载，包括：

- `Assembly-CSharp.dll`（你的游戏代码）
- `Assembly-CSharp-Editor.dll`（你的编辑器代码）
- 所有 Package 的托管程序集
- Unity 引擎自身的托管层

注意：是**全部**，不是只卸载你改动的那一个。

### 3. 创建新 AppDomain 并加载所有程序集

Unity 新建一个干净的 `AppDomain`，然后把 `Library/ScriptAssemblies/` 里的所有 `.dll` 依次加载进来。

同样是**全部重新加载**。不管你只改了一个文件，还是改了一百个文件，加载的程序集数量是一样的。

### 4. 重跑静态初始化

程序集加载完成后，所有标记了以下属性的代码会被重新执行：

- `[InitializeOnLoad]`（类的静态构造函数）
- `[InitializeOnLoadMethod]`（静态方法）

这些回调的存在是为了让编辑器扩展在加载完成后能初始化自己的状态。但如果项目里有大量编辑器扩展、或者某个 `[InitializeOnLoad]` 里做了重度初始化（比如扫描整个 AssetDatabase），这里就会变成瓶颈。

### 5. 反序列化编辑器状态

最后，第一步保存的字节流被还原回来：Inspector 重新绑定到对应的对象，EditorWindow 的字段恢复，编辑器回到"可用"状态。

---

## 完整时序图

```
脚本保存
  → 检测到变化（AssetDatabase 文件监听）
  → Roslyn 编译（bee_backend 调度）
  → ILPP 字节码处理
  → 写入 Library/ScriptAssemblies/
  → Domain Reload 开始
      → 序列化编辑器状态
      → 卸载旧 AppDomain（全部程序集）
      → 创建新 AppDomain
      → 加载所有程序集（全部，不只是改动的）
      → 重跑 [InitializeOnLoad] / 静态构造函数
      → 反序列化编辑器状态
  → 编辑器可用
```

---

## 为什么代价高

理解了上面的步骤，代价高的原因就很直白：

**全量加载。** 改了一个 .dll，但加载的是全部程序集。项目依赖 100 个程序集，就要加载 100 个。没有增量机制。

**静态初始化不可跳过。** 每次 Domain Reload 都必须重跑所有 `[InitializeOnLoad]`。项目大了之后这些回调的累计耗时相当可观，而且不容易排查是哪个在拖慢。

**状态序列化开销随项目增长。** 编辑器窗口越多、ScriptableObject 越多、Inspector 绑定越复杂，序列化和反序列化的开销就越大。

**进入 Play Mode 默认也触发一次。** 默认配置下，每次点播放都会触发一次完整的 Domain Reload（即使代码没有改动），这是很多人觉得"进 Play Mode 太慢"的根本原因。

---

## 什么时候触发 Domain Reload

| 触发条件 | 说明 |
|----------|------|
| 脚本编译完成 | 每次编译结束必定触发，无法关闭 |
| 进入 Play Mode | 默认配置下触发，可以关闭 |
| 手动调用 | `EditorUtility.RequestScriptReload()` |

---

## 如何减少等待：EnterPlayMode 设置

Unity 2019.3 提供了 `Enter Play Mode Settings`，可以分别控制进入 Play Mode 时的两个重载行为。

**路径：Edit → Project Settings → Editor → Enter Play Mode Settings**

勾选 `Enter Play Mode Options` 后，可以独立控制：

| 选项 | 含义 |
|------|------|
| `Reload Domain` | 进 Play Mode 时是否重载程序集 |
| `Reload Scene` | 进 Play Mode 时是否重新加载场景 |

### 关闭 Reload Domain

这是最大的提速手段。关闭后，点播放时跳过整个 Domain Reload 流程，进入 Play Mode 的速度可以从数秒缩短到不足一秒。

**代价：静态变量不会自动重置。**

正常情况下，Domain Reload 会销毁整个 AppDomain，静态变量自然清零。关闭 Reload Domain 后，上一次 Play Mode 留下的静态状态会残留到下一次。如果你的代码依赖静态变量在每次进入 Play Mode 时是初始值，就必须手动处理。

官方推荐的做法是使用：

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
static void ResetStaticState()
{
    // 在这里手动重置静态变量
}
```

这个回调在每次进入 Play Mode 时都会执行，即使没有 Domain Reload。

### 关闭 Reload Scene

关闭后，进 Play Mode 时不重新加载场景，直接在当前场景状态上运行。适合只想快速验证某段逻辑、不关心场景初始状态的情况。

### 推荐配置

| 场景 | 建议 |
|------|------|
| 快速迭代逻辑、频繁点播放 | 两个都关闭，手动管理静态状态 |
| 需要验证场景初始化流程 | 只关闭 Reload Domain，保留 Reload Scene |
| 最终验收、提交前测试 | 两个都开启，确保行为与发布一致 |

---

## 排查慢在哪里

如果 Domain Reload 明显偏慢，可以用以下方法定位：

**开启 Editor 日志的时间戳。** Console 窗口右上角菜单 → `Log Entry` → `Timestamp`。Domain Reload 开始和结束都有对应的日志条目，可以直接看耗时。

**搜索 `[InitializeOnLoad]` 调用耗时。** 在日志里找 `Refreshing native plugins`、`ReloadAssembly` 等关键字，前后时间差就是 Domain Reload 的总耗时。

**审查 `[InitializeOnLoad]` 代码。** 全局搜索项目里所有 `[InitializeOnLoad]` 标记，逐一检查是否有重度操作（遍历 AssetDatabase、网络请求、大量反射等）。

---

## 小结

1. `Domain Reload` 是编译之后的第二段等待：销毁旧 `AppDomain`、重建、重新加载全部程序集、重跑静态初始化、恢复编辑器状态。
2. 代价高的根本原因是**全量**：不管改了多少，加载的程序集数量不变，静态初始化全部重跑。
3. 脚本编译完后必然触发一次；进入 Play Mode 默认也触发一次。
4. 通过 `Enter Play Mode Settings` 关闭 `Reload Domain` 可以把进 Play Mode 的等待缩到极短，代价是需要用 `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` 手动重置静态状态。
5. 如果 Domain Reload 异常慢，重点排查 `[InitializeOnLoad]` 里有没有重度初始化逻辑。

---

- 上一篇：[Unity 脚本编译管线 02｜ILPP：Unity 为什么要偷偷改你的字节码]({{< relref "engine-notes/unity-script-compilation-pipeline-02-ilpp.md" >}})
- 下一篇：[Unity 脚本编译管线 04｜编译卡死怎么看：从日志定位哪一环出了问题]({{< relref "engine-notes/unity-script-compilation-pipeline-04-debug-hang.md" >}})
