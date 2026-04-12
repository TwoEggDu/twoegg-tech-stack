---
title: "CachedShadows 阴影缓存 04｜为什么编辑器有阴影、打包后可能没了：SVC、Hidden Shader、StaticShaders 各管什么"
slug: "cachedshadows-04-shader-delivery"
date: "2026-04-01"
description: "结合 TopHeroUnity 的 CachedShadows 实现，拆开 Shader 本体、Variant、Hidden Shader 和运行时预加载这四层边界，讲清为什么编辑器有阴影、打包后却可能失效。"
tags:
  - "Unity"
  - "URP"
  - "Shader"
  - "CachedShadows"
  - "Shader Variant"
  - "AssetBundle"
series: "CachedShadows 阴影缓存"
primary_series: "cachedshadows"
series_role: "article"
series_order: 40
weight: 1740
---
写到这里，CachedShadows 的主链已经能先压成一句话：

`它自己生成主光阴影图，再把结果重新接回 URP receiver 侧。`

但项目里真正最容易把人卡住的，往往不是“原理没懂”，而是另一类问题：

`编辑器里明明有阴影，为什么包一打，真机上就没了？`

这个问题如果只用一句“shader 被裁了”去回答，通常是不够的。因为在 CachedShadows 这条链上，至少有四件事可能分别出错：

- shader 本体是否存在
- 对应 variant 是否还在
- `Hidden/CopyShadowMap` 这种隐藏 shader 能不能在运行时被找到
- 运行时是否真的走过了预加载和 loader 安装链路

这篇就只做一件事：

`把 SVC、Hidden Shader、StaticShaders.prefab、运行时 Loader 这几层边界拆开。`

---

## 先把问题拆成三层，不然永远会混

很多现场会把这类问题统称为“shader 没带上”。  
这个说法不完全错，但太粗。

对 CachedShadows 来说，更稳的拆法至少有三层：

### 第一层：Shader 本体在不在

最典型的问题是：

- `Shader.Find(...)` 返回 `null`
- `ArtShaderUtil.Find(...)` 返回 `null`
- `new Material(shader)` 直接在运行时炸掉

这类问题回答的是：

`这个 shader 文件本身，到底有没有进入最终可访问的运行时资源集合。`

### 第二层：Shader 在，但关键路径的 variant 在不在

最典型的问题是：

- 材质不是粉的
- shader 也找得到
- 但阴影相关分支不生效
- 某些 keyword 组合在 Player / AssetBundle 构建里被裁掉了

这类问题回答的是：

`shader 存在，但它需要走的那条编译路径还在不在。`

### 第三层：运行时有没有按项目约定拿到它

最典型的问题是：

- 编辑器里能找到
- 运行时本应从静态 loader 里取
- 但 preload 没执行，或者 loader 没装上

这类问题回答的是：

`项目的运行时资源获取协议，到底有没有被真的执行到。`

如果不先把这三层拆开，后面就会出现很典型的误判：

- 用 `SVC` 去解释隐藏 shader 丢失
- 用 `Always Included` 去兜运行时 loader 没装上
- 用“代码逻辑没问题”去掩盖构建物里压根没有 shader 本体

---

## 放回 CachedShadows：这几层分别对应谁

放回 TopHeroUnity 当前的 CachedShadows 实现，这几层大致对应下面这张表：

| 层 | 典型对象 | 主要回答什么 |
|---|---|---|
| Shader 本体 | `Hidden/CopyShadowMap`、`Hidden/CorgiClear`、各类 receiver shader | 运行时能不能找到这个 shader |
| Variant 保留 | `BundleSVC.shadervariants`、URP 资产配置、stripping 规则 | `_MAIN_LIGHT_SHADOWS` 这批路径还在不在 |
| 运行时获取协议 | `StaticShaders.prefab`、`StaticShaders.cs`、`DPShaderLoader`、`ProcedurePreload` | 项目实际从哪里拿 shader |

只要这张表先立住，后面很多判断就不会乱。

例如：

- `_MAIN_LIGHT_SHADOWS` 失效，优先怀疑 variant 侧
- `Hidden/CopyShadowMap` 为空，优先怀疑 shader 本体和 loader 侧
- 编辑器有、真机没，优先怀疑构建产物和 preload 侧

---

## `_MAIN_LIGHT_SHADOWS` 这批变体由谁保

先说 receiver 侧。

CachedShadows 最终之所以还能让 Lit / Toon / 自定义 receiver shader 采到主光阴影，一个前提是：

`receiver shader 里跟主光阴影相关的变体路径还得在。`

你在项目的 `BundleSVC.shadervariants` 里，能明确看到 `_MAIN_LIGHT_SHADOWS` 相关 keyword 组合被显式收录。这说明项目在治理上已经意识到：

`主光阴影采样路径是高价值关键路径，不能随便交给“构建自己猜”。`

所以这里的 SVC 角色很清楚：

`它在保 receiver 侧关键 variant。`

也就是说，它关心的是：

- 材质有没有用到
- `_MAIN_LIGHT_SHADOWS` / `_MAIN_LIGHT_SHADOWS_CASCADE` 这批 keyword 组合是不是显式保留
- Player / Bundle 构建时，这些路径会不会被裁掉

它并不直接回答：

`Hidden/CopyShadowMap 这个隐藏 shader 本体到底在不在。`

这两件事很容易混，但不是一回事。

---

## 为什么 `Hidden/CopyShadowMap` 不是靠 SVC 兜底

这就是 CachedShadows 这条链最容易被误判的地方。

在 `CachedShadowsRenderPass` 里，动态阴影叠加这条路径会走到这行逻辑：

```csharp
if (_copyDepthMat == null)
    _copyDepthMat = new Material(ArtBase.Resource.ArtShaderUtil.Find("Hidden/CopyShadowMap"));
```

这说明 `Hidden/CopyShadowMap` 在这里承担的职责不是“receiver shader 阴影采样分支”，而是：

`动态阴影叠加时，用来把已有主光 shadow map 拷到动态阴影目标上的一个隐藏拷贝 shader。`

这类 shader 的问题重点往往不在 variant，而在：

- 本体是否真的进入构建产物
- 运行时 `ArtShaderUtil.Find("Hidden/CopyShadowMap")` 能不能拿到它

更关键的是，这个 shader 本身不依赖你此刻最关心的那批 `_MAIN_LIGHT_SHADOWS` receiver keyword。  
也就是说，即便你把 `_MAIN_LIGHT_SHADOWS` 的 SVC 做得很好，也不能自动推出：

`Hidden/CopyShadowMap` 一定没问题。`

更准确的说法是：

`SVC 更像在保 receiver 侧的编译路径；Hidden/CopyShadowMap 更像在保 CachedShadows 动态叠加那一步运行时真的能找到对应 shader 本体。`

这就是为什么不能用一句“我们已经做了 SVC”去替代对 `Hidden/CopyShadowMap` 的单独确认。

---

## TopHeroUnity 在怎么保 `Hidden/CopyShadowMap`

TopHeroUnity 当前选的不是“把它塞进 Always Included 再说”，而是另一条更项目化的方案：

`把关键隐藏 shader 收进 StaticShaders.prefab，然后在启动预加载阶段安装静态 loader。`

先看构建侧。

在 `AssetBundleCollectorSetting.asset` 里，项目明确把下面两样东西作为资源收集入口：

- `Assets/ArtWork/Generate/Shared/BundleSVC.shadervariants`
- `Assets/ArtWork/Generate/Shared/StaticShaders.prefab`

这说明项目的思路不是“只在构建时保 variant”，而是同时做了两件事：

- 一边显式保留关键 variant
- 一边把需要运行时同步查找的关键 shader 本体集中进一个静态 prefab

再看 `StaticShaders.prefab` 本身。  
这里面能直接看到：

- `Hidden/CopyShadowMap`
- `Hidden/CorgiClear`

这一步的意义非常直接：

`让这批关键隐藏 shader 以明确的资源引用关系进入构建产物。`

这和“完全依赖运行时 `Shader.Find` 自己撞上去”是两种不同的治理思路。

---

## `StaticShaders.prefab` 到底在运行时做了什么

如果 `StaticShaders.prefab` 只是进了包，但运行时没人去加载、没人去注册，它仍然可能帮不上忙。

TopHeroUnity 的关键在于它后面还有一条完整的运行时接入链：

```text
ProcedurePreload
  -> 加载 StaticShaders.prefab
  -> ArtShaderUtil.SetLoader(new DPShaderLoader(go))
      -> DPShaderLoader 持有 StaticShaders 组件
          -> StaticShaders 建立 shaderName -> Shader 的字典
```

这里每一层都站在不同位置：

### `ProcedurePreload`

它负责在项目启动预加载阶段，把 `StaticShaders.prefab` 真的加载出来。  
如果这一步没走，后面整条静态 shader loader 链就没有输入。

### `ArtShaderUtil.SetLoader(...)`

它负责把项目的 shader 查找协议切换到自定义 loader 上。  
这意味着后续代码里大量的 `ArtShaderUtil.Find("...")`，不再只是盲目依赖 Unity 默认的 `Shader.Find(...)`。

### `DPShaderLoader`

它负责把预加载出来的 prefab 实例化并常驻，然后拿到 `StaticShaders` 组件。

### `StaticShaders`

它本身像一份静态 shader 名录：

- 序列化保存 shader 引用
- 在 `Awake()` 时建一张 `shader name -> shader ref` 的字典
- 运行时通过名字查 shader

如果把这一步压成一句话：

`StaticShaders.prefab 负责“把 shader 带进来”，StaticShaders + DPShaderLoader 负责“把 shader 交给运行时查找链”。`

---

## 为什么编辑器里正常，不代表包里一定正常

到这里就能解释一个非常常见的误区：

`编辑器里 ArtShaderUtil.Find 能拿到，为什么真机上还是会空？`

因为编辑器和包体不是一个环境。

在编辑器里，很多资源查找都带着更宽松的上下文：

- 资源本体就在项目里
- `Shader.Find(...)` 的成功率通常更高
- 某些资产引用问题会被 AssetDatabase 环境掩盖

但在 Player 或 AssetBundle 运行时，问题会立刻变得更严格：

- shader 本体必须真的被打进构建物
- variant 必须真的没被裁掉
- preload 必须真的执行过
- loader 必须真的被安装成功

所以“编辑器里能跑”在这条链上最多只能证明一件事：

`代码路径大概没写错。`

它不能证明：

- 构建物里一定有 `Hidden/CopyShadowMap`
- 运行时一定走了 `StaticShaders.prefab` 预加载
- 真机上一定能拿到 `_MAIN_LIGHT_SHADOWS` 相关 receiver variant

这也是为什么 CachedShadows 这种系统，必须把“编辑器验证”和“构建物验证”拆开看。

---

## 把 SVC、Hidden Shader、StaticShaders 收成一张职责表

如果只给一张最小职责表，我建议记这个：

| 部件 | 在 CachedShadows 里主要负责什么 | 不负责什么 |
|---|---|---|
| `BundleSVC.shadervariants` | 保 receiver 侧高价值主光阴影 variant | 不负责把 `Hidden/CopyShadowMap` 本体塞进运行时 |
| `StaticShaders.prefab` | 让关键隐藏 shader 形成显式资源引用并进入构建物 | 不负责 receiver variant 的精细枚举 |
| `ProcedurePreload` | 启动时把静态 shader 集真的加载出来 | 不负责 shader 编译路径是否被裁 |
| `DPShaderLoader + StaticShaders` | 让 `ArtShaderUtil.Find` 能按项目约定拿到静态 shader | 不负责生成缺失 variant |
| `URP / stripping / build 配置` | 决定哪些 variant 最终被保留或删除 | 不负责项目自定义 loader 是否已安装 |

只要这张表先立住，后面排查就会快很多。

你看到的是哪类现象，就先去查对应那层，而不是把所有问题都丢给一句模糊的“shader 丢了”。

---

## 这篇真正想留下来的结论

如果最后只允许留一句话，我会把这篇收成这样：

`在 TopHeroUnity 的 CachedShadows 里，SVC 主要在保 receiver 侧关键 variant，StaticShaders.prefab + preload + loader 主要在保运行时能找到 Hidden/CopyShadowMap 这类关键隐藏 shader，本体存在和变体存在不是一回事。`

下一篇会回到更直接的现场问题：

`当你看到“没影子、不刷新、只有 Editor 有、Android 没有”这些症状时，第一步到底该先查哪一层。`
