---
title: "Unity 6 运行时变化 01｜Awaitable vs UniTask vs Coroutine：三代异步方案的架构对比"
slug: "unity6-runtime-01-awaitable-vs-unitask-vs-coroutine"
date: "2026-04-13"
description: "Unity 6 新增的 Awaitable 在 Coroutine → UniTask → Awaitable 的演化链中处于什么位置：pooled class 而非 struct，能力是 UniTask 的子集，定位是零依赖的官方异步原语。库用 Awaitable，项目用 UniTask——但必须先看清两者的能力边界才能做这个判断。"
tags:
  - "Unity"
  - "Unity 6"
  - "Awaitable"
  - "UniTask"
  - "Coroutine"
  - "Async"
  - "PlayerLoop"
series: "Unity 6 运行时与工具链变化"
series_id: "unity6-runtime-toolchain"
weight: 2301
unity_version: "6000.0+"
---

Unity 6 内置了一个新的异步返回类型：`Awaitable`。

如果你在用 UniTask，第一反应大概是：官方终于做了，那 UniTask 还要不要留？如果你还在用 Coroutine，问题更直接：该换成 Awaitable 还是直接上 UniTask？

这篇文章不列 API 用法。它要回答的是：**Awaitable 在 Coroutine → UniTask → Awaitable 这条演化链中处于什么位置，它的能力边界在哪，和 UniTask 到底该选谁。**

---

## 三代异步方案的演化脉络

Unity 里的异步方案不是一次设计出来的，而是一代解决上一代遗留的问题，逐步长出来的。

```
~2005  Coroutine（Unity 早期版本）
      yield return 驱动，挂在 MonoBehaviour 生命周期上
      问题：不是真正的 async/await，没有返回值，异常会被吞，
            取消只能手动 flag，无法组合（没有 WhenAll）
            ↓
2018  UniTask（社区，Cysharp）
      struct 壳 + source 协议 + PlayerLoop 注入
      问题：第三方依赖，API surface 大，版本升级需跟进
            ↓
2023  Awaitable（Unity 2023.1 / Unity 6）
      Unity 官方的 async 返回类型，pooled class
      定位：零依赖的官方异步原语，能力是 UniTask 的子集
```

每一代的核心动机：

**Coroutine** 解决的是"怎么在帧驱动引擎里做跨帧等待"——在 async/await 还不存在的年代，yield return 是唯一方案。

**UniTask** 解决的是"async/await 到了 Unity 以后的错位"—— [00]({{< relref "engine-toolchain/unity-async-runtime-00-task-unity-mismatch.md" >}}) 已经拆过这个错位：Task 默认站在一般托管运行时的世界里，而 Unity 是主线程真相中心 + 强帧时序 + 强生命周期边界。UniTask 补上了 PlayerLoop 调度、帧时序原语、零 GC、CancellationToken 等 Unity 特有的语义。

**Awaitable** 解决的是"UniTask 这些能力里，哪些应该由引擎官方提供"——不是替代 UniTask 的全部能力，而是把最基础的部分做成引擎内置，让库开发者不需要引入外部依赖就能写 async 代码。

---

## Awaitable 的数据模型：pooled class 而非 struct

UniTask 的数据模型在 [01]({{< relref "engine-toolchain/unity-async-runtime-01-unitask-minimal-kernel-struct-source-token.md" >}}) 里已经拆过：一个很薄的 **struct 壳**，真正的状态下沉到 source 协议里，token 负责把壳和 source 安全绑定。struct 意味着 UniTask 本身不产生堆分配——这是 UniTask 相对于 Task 的主要性能优势之一（async 状态机和内部 runner 的分配仍然存在）。

Awaitable 选了不同的路：它是一个 **class 实例**，但通过**对象池回收**来减少分配。

```csharp
// UniTask：struct，零堆分配
UniTask task = UniTask.Yield();
// task 是一个值类型，不产生 GC

// Awaitable：class，但 pooled
Awaitable task = Awaitable.NextFrameAsync();
// task 是一个引用类型实例，完成后自动归还对象池
// 下次调用 NextFrameAsync() 可能拿到同一个实例
```

这个设计选择带来一个硬性约束：**Awaitable 实例不能 await 两次。**

```csharp
// ❌ 危险：await 完成后实例已归还池，第二次 await 可能拿到已被复用的实例
var awaitable = Awaitable.NextFrameAsync();
await awaitable;
await awaitable; // 未定义行为：可能异常、可能死锁

// ✅ 正确：每次 await 用新实例
await Awaitable.NextFrameAsync();
await Awaitable.NextFrameAsync();
```

UniTask 的 struct + source 模型也有"不能 await 两次"的约束（token 机制保证），但原因不同：UniTask 是因为 source 的 version 校验会拒绝重复消费；Awaitable 是因为对象已物理归还池。

**为什么 Unity 没选 struct？**

UniTask 能用 struct 是因为它有完整的 source 协议和 builder 体系来管理状态生命周期。Awaitable 作为引擎内置类型，需要能从不同线程（主线程和后台线程）安全引用同一个等待状态——class 的引用语义天然支持跨线程共享，而 struct 做跨线程共享需要额外的间接层。选 pooled class 是在"跨线程安全 + 实现简洁"和"零 GC"之间的取舍。

---

## 能力边界：Awaitable 能做什么、不能做什么

这张表是本文的核心。它决定了你在什么场景下可以只用 Awaitable，什么场景必须引入 UniTask。

| 能力维度 | Coroutine | Awaitable（Unity 6） | UniTask |
|---------|-----------|---------------------|---------|
| **语法** | yield return | async/await | async/await |
| **返回值** | 无 | `Awaitable<T>` | `UniTask<T>` |
| **数据模型** | Iterator 状态机 | pooled class | struct 壳 + source |
| **GC 压力** | 每次启动分配 Iterator | 池化复用，近似零分配 | struct，零分配 |
| **PlayerLoop 挂入点** | 固定（Update / LateUpdate / EndOfFrame / FixedUpdate） | 有限（NextFrame / EndOfFrame / FixedUpdate） | 完整枚举（PlayerLoopTiming，16 个时序点，覆盖各阶段 before/after） |
| **帧等待** | `yield return null` | `NextFrameAsync()` | `UniTask.Yield()` / `UniTask.NextFrame()` / `UniTask.DelayFrame()` |
| **秒数等待** | `WaitForSeconds` | `WaitForSecondsAsync()` | `UniTask.Delay()` |
| **EndOfFrame** | `WaitForEndOfFrame` | `EndOfFrameAsync()` | `UniTask.WaitForEndOfFrame()` |
| **FixedUpdate** | `WaitForFixedUpdate` | `FixedUpdateAsync()` | `UniTask.WaitForFixedUpdate()` |
| **DelayFrame** | 手动计数 | ❌ 不支持 | `UniTask.DelayFrame()` |
| **WhenAll / WhenAny** | ❌ 不支持 | ❌ 不支持 | `UniTask.WhenAll()` / `UniTask.WhenAny()` / `UniTask.WhenEach()` |
| **CancellationToken** | 手动 flag | ✅ 支持 + `Cancel()` 方法 | ✅ 完整支持 |
| **destroyCancellationToken** | ❌ | ✅（MonoBehaviour 的属性，Unity 2022.2+ 引入，非 Awaitable 独有） | ✅（`GetCancellationTokenOnDestroy()` 扩展） |
| **线程切换** | ❌ 仅主线程 | `BackgroundThreadAsync()` / `MainThreadAsync()` | `UniTask.SwitchToThreadPool()` / `UniTask.SwitchToMainThread()` |
| **AsyncOperation 适配** | yield return 直接等 | `FromAsyncOperation()` | `ToUniTask()` 扩展 |
| **异常传播** | 被 Unity 吞掉，只打 log | 正常 try/catch | 正常 try/catch |
| **Tracker / 调试窗口** | ❌ | ❌ | ✅ UniTask Tracker Window |
| **外部依赖** | 无 | 无（引擎内置） | 需要引入 UniTask 包 |
| **IL2CPP 兼容** | ✅ | ✅ | ✅（需注意 AOT 泛型限制） |

几个关键差异点：

### WhenAll / WhenAny 缺失

这是 Awaitable 和 UniTask 之间最大的功能差距。在实际项目中，同时等待多个异步操作完成是高频需求：

```csharp
// UniTask：原生支持
var (userData, configData, assetData) = await UniTask.WhenAll(
    LoadUserData(),
    LoadConfig(),
    LoadAsset()
);

// Awaitable：没有 WhenAll，只能手动拼
var userTask = LoadUserDataAsync();
var configTask = LoadConfigAsync();
var assetTask = LoadAssetAsync();
var userData = await userTask;
var configData = await configTask;
var assetData = await assetTask;
// 问题：这不是并发等待，而是顺序等待——三个任务变成串行
```

没有 WhenAll 意味着 Awaitable 无法优雅地表达"并发等待多个异步操作"。对于加载流程、网络请求批处理等场景，这是硬伤。

### PlayerLoop 时序粒度

UniTask 的 `PlayerLoopTiming` 枚举覆盖了 Unity PlayerLoop 各阶段的 before / after 时序点（UniTask 2.x 中共 16 个枚举值，如 `Update` / `LastUpdate`、`FixedUpdate` / `LastFixedUpdate` 等成对出现）。[02]({{< relref "engine-toolchain/unity-async-runtime-02-playerloop-injection-and-runner.md" >}}) 已经拆过这套 runner/queue 体系。

Awaitable 只提供 `NextFrameAsync()`、`EndOfFrameAsync()`、`FixedUpdateAsync()` 三个帧级原语，没有暴露底层 PlayerLoop 阶段的选择能力。对于大多数业务逻辑足够了，但如果你需要精确控制 continuation 在 Update 之前还是之后恢复，Awaitable 做不到。

### 线程切换

两者都支持主线程 ↔ 后台线程切换，API 风格不同但能力等价：

```csharp
// Awaitable
await Awaitable.BackgroundThreadAsync();
// 现在在后台线程
var result = HeavyComputation();
await Awaitable.MainThreadAsync();
// 回到主线程

// UniTask
await UniTask.SwitchToThreadPool();
var result = HeavyComputation();
await UniTask.SwitchToMainThread();
```

---

## 为什么 Unity 要做 Awaitable 而不是直接内置 UniTask

这不是技术问题，是工程约束问题。

**零外部依赖。** Unity 引擎的 API 不能依赖第三方包。UniTask 是 Cysharp 维护的社区项目，Unity 不会把第三方社区库直接变成引擎内置 API——这涉及维护责任、API 稳定性承诺和授权等多重约束。Awaitable 是引擎团队在"不引入任何外部依赖"这个约束下能做到的最大公约数。

**API 稳定性承诺。** 一旦成为引擎公共 API，就要遵守 Unity 的 deprecation policy——不能随便改签名、改行为。UniTask 作为社区库可以快速迭代，但引擎 API 做不到。所以 Awaitable 选择了一个很小的 API surface：只暴露最基础的帧等待、线程切换和 AsyncOperation 适配，不做 WhenAll 这种上层组合能力。

**最小 surface area。** Awaitable 的设计哲学是"做 Coroutine 的 async/await 等价物"——Coroutine 能做的（等一帧、等几秒、等 EndOfFrame、等 FixedUpdate），Awaitable 都能做，加上 Coroutine 做不到的返回值、CancellationToken 和正常异常传播。但 Coroutine 不能做的事（WhenAll、DelayFrame、精细 PlayerLoop 控制），Awaitable 也不做。

这个定位可以用一句话概括：**Awaitable 是 Coroutine 的现代替代品，不是 UniTask 的竞品。**

---

## 决策框架：库用 Awaitable，项目用 UniTask

根据上面的能力边界，选择逻辑可以归结为：

```
你在写什么？
  ├─ 可复用库 / 插件 / Asset Store 包（不想引入外部依赖）
  │   → 用 Awaitable
  │     公共 API 返回 Awaitable / Awaitable<T>
  │     内部如果需要 WhenAll，用 Task.WhenAll 桥接
  │
  ├─ 项目代码（已经或愿意引入 UniTask）
  │   → 用 UniTask
  │     WhenAll / WhenAny / DelayFrame / PlayerLoopTiming 全都需要
  │     UniTask Tracker Window 对调试有直接价值
  │
  └─ 项目代码（不想引入任何第三方依赖）
      → 用 Awaitable
        接受 WhenAll 缺失和 PlayerLoop 粒度限制
        适合异步需求简单的小型项目
```

**两者共存的场景：**

UniTask 提供了 `AsUniTask()` 扩展方法，可以把 Awaitable 转为 UniTask。这意味着：

```csharp
// 库暴露 Awaitable 接口
public async Awaitable<Texture2D> LoadTextureAsync(string path, CancellationToken ct)
{
    // ...
}

// 项目侧用 UniTask 消费
var (tex1, tex2) = await UniTask.WhenAll(
    LoadTextureAsync("a.png", ct).AsUniTask(),
    LoadTextureAsync("b.png", ct).AsUniTask()
);
```

库用 Awaitable 保持零依赖，项目用 UniTask 获得组合能力——两者通过 `AsUniTask()` 桥接，可以无摩擦共存。

**从 Coroutine 迁移的路径：**

| 现状 | 建议路径 |
|------|---------|
| 项目全是 Coroutine，想现代化 | 如果已有 UniTask → 继续用 UniTask；如果是新项目且异步需求简单 → 可以先用 Awaitable |
| 项目用 UniTask，升级到 Unity 6 | 继续用 UniTask，不需要迁移到 Awaitable |
| 正在写库 / 插件 | 公共 API 用 Awaitable，内部实现按需选择 |

---

## 小结

| | Coroutine | Awaitable | UniTask |
|---|---|---|---|
| 定位 | 帧驱动引擎的原始跨帧方案 | Coroutine 的 async/await 现代替代 | Unity 异步的完整运行时系统 |
| 设计约束 | 历史遗留 | 零依赖、最小 surface、API 稳定 | 社区驱动、快速迭代、功能完整 |
| 适用范围 | 简单跨帧等待 | 零依赖库、简单异步项目 | 项目级异步框架、复杂异步组合 |
| 核心短板 | 无返回值、吞异常、无取消 | 无 WhenAll、PlayerLoop 粒度粗 | 外部依赖、AOT 需注意 |

Awaitable 不是 UniTask 的替代品，而是 Unity 官方对 Coroutine 的替代品。它改变的设计假设是：**异步代码的基础原语应该由引擎内置提供，而不是必须依赖第三方库。**

但"基础原语"和"完整异步框架"之间有明确的能力差距——WhenAll、DelayFrame、精细 PlayerLoop 控制、Tracker Window——这些差距决定了 UniTask 在项目级开发中仍然不可替代。

本篇建立了 Awaitable 的定位和能力边界。但 feature matrix 里的"PlayerLoop 挂入点"那一行只给了数量对比，没有拆开每个 API 到底挂在 PlayerLoop 的哪个阶段——这些细节留给下一篇。

---

**下一步应读：** Unity 6 Awaitable 的 PlayerLoop 集成：帧时序与调度语义（待发布）— 逐 API 拆解 Awaitable 在 PlayerLoop 中的具体挂入阶段和时序行为

**扩展阅读：** [Task 在 Unity 里到底错位在哪，为什么会长出 UniTask]({{< relref "engine-toolchain/unity-async-runtime-00-task-unity-mismatch.md" >}}) — 如果对"为什么 Unity 需要 UniTask / Awaitable 而不能直接用 Task"的问题还不够清楚，回去补这篇
