+++
title = "Unity Shader Variant 缺失事故排查流程：从现象到根因的三层定位法"
description = "把 shader variant 缺失问题的排查分成现象分流、构建期定位（从未枚举 vs 被 strip）、运行时定位三层，给出可复用的决策树和每层具体的排查手段。"
slug = "unity-shader-variant-missing-diagnosis-flow"
weight = 59
featured = false
tags = ["Unity", "Shader", "Build", "AssetBundle", "Debugging"]
series = "Unity 资产系统与序列化"
+++

包里缺了 shader variant，是一个很常见但很难快速定位的问题。

原因是它的表面现象都长得很像：

- 粉材质
- 光照不对
- 特效效果缺失
- 某个平台才出问题
- 某个 AssetBundle 加载后才出问题

但背后的根因可以落在三层完全不同的位置。如果不先把层级分清，查到最后往往只是在猜，或者治标不治本。

这篇的目标是给出一套**可以直接执行的定位流程**，不是概念解释，而是决策树。

## 先给一套最小决策树

```
现象发生
  └→ 粉材质 / pass 丢失 / 光照完全不对
        └→ [构建期层] 这条 variant 是否存在于包里？
              ├→ 否 → 进一步区分：
              │         └→ IPreprocessShaders 有没有收到过这条 variant？
              │               ├→ 没收到 → [枚举阶段] 材质 keyword 组合没进入 usedKeywords
              │               │               → 根因：材质不在构建 allObjects / 需要 SVC 或 Always Included 补
              │               └→ 收到了但被删除 → [Stripping 阶段] 被 strip 规则删掉
              │                                   → 根因：检查 IPreprocessShaders 规则 / 全局 Graphics 配置
              └→ 是 → 进入运行时层
  └→ 首次出现卡一下，之后正常
        └→ [运行时层] Variant 存在但未预热
              └→ 检查 SVC 是否覆盖这条路径 / WarmUp 是否在首次渲染前执行
  └→ 效果略不对，但不粉
        └→ [运行时层] 退化到了相近的 fallback variant
              └→ Frame Debugger 查实际 shader pass → 确认精确 variant 是否存在
```

下面把每一层展开讲。

## 一、现象分流：先把问题归类，再选工具

### 1. 粉材质 / pass 完全丢失

这是最明确的缺 variant 信号。

Unity 找不到任何可接受的 variant 时（评分算法没有找到任何 pass 可用的匹配），才会退化到粉材质（error shader）。

这类问题几乎必然是构建期问题——某条关键 variant 根本没有进入包里。

**优先查构建期层。**

### 2. 效果略有异常，但不粉

这种情况可能是运行时退化到了相近但不精确的 fallback variant。

例如：本应走 `_EMISSION` keyword 的路径，但包里只有不带 `_EMISSION` 的 variant，运行时退化到没有发光效果的那条。

**先用 Frame Debugger 确认实际走了哪条 variant，再对照检查精确 variant 是否在包里。**

### 3. 首次出现卡顿，之后正常

这是 variant 存在、但未预热的典型症状。

延迟加载发生在第一次 draw call 时，后续走缓存就流畅了。

**优先查运行时层（SVC + WarmUp）。**

### 4. 编辑器正常，Player 或 AssetBundle 异常

这是 shader 构建边界问题的典型信号。编辑器的资源世界比线上完整，很多问题在编辑器里被遮住了。

**直接进构建期层排查。**

### 5. 特定平台才出问题

通常是 build target 或 Graphics API 导致的 variant 集合差异，或者 stripping 规则有平台差异。

**在目标平台构建产物上排查，不要用 Editor 或其他平台的构建结论。**

## 二、构建期层：variant 是否存在于包里

### 第一步：确认 variant 是否在包里

最直接的方式是用 `IPreprocessShaders` 记录构建日志，把每条 variant 的 shader name / pass / keyword 组合写成表。

查找目标 keyword 组合是否出现在日志里。

如果没有接 `IPreprocessShaders`，退而求其次可以看 Unity 构建日志里的 shader stripping 摘要（需要在 Project Settings 开启 Shader Variant Log Level）：

```
Compiling shader 'URP/Lit' pass 'ForwardLit' (vs_target 4.5)
  Full variant space: 1024
  After filtering: 256
  After builtin stripping: 48
  After IPreprocessShaders: 48
  Compiled variants: 48
```

这四个数字能告诉你 variant 在哪个阶段被减少了。

### 第二步：区分"从未枚举"和"枚举后被 strip"

这是构建期最关键的分叉：

**情况 A：IPreprocessShaders 从未收到这条 variant**

`IPreprocessShaders.OnProcessShader` 收到的，是所有通过了 `PrepareEnumeration` 枚举的 variant。如果某条 variant 连回调都没被调用过，说明它在枚举阶段就没有生成。

根因是这条 keyword 组合没有进入 `usedKeywords`。

`usedKeywords` 来源是 `ComputeBuildUsageTagOnObjects` 遍历 `allObjects`——也就是当前构建收集到的所有对象里的材质。如果某个材质不在这个集合里（在另一个没参与此次构建的包里，或是热更新包），那它的 keyword 组合就不会进入 `usedKeywords`，对应的 variant 也就永远不会被枚举生成。

**修法：**

| 修法 | 适用场景 |
|------|----------|
| 让材质参与本次构建（放入场景/Resources 或一起打包） | 依赖关系可以重组时 |
| 用 SVC 显式登记这条 keyword 组合（SVC 作为构建资产） | 热更内容材质无法参与 Player 构建时 |
| 把 shader 加入 Always Included | 需要全局兜底，数量少时 |

注意：SVC 作为构建资产（不是挂在 Preloaded Shaders）时，会把登记的 keyword 组合并入 `usedKeywords`，与材质并列参与枚举，而不是替代材质的贡献。

**情况 B：IPreprocessShaders 收到了，但 variant 被删除**

检查 `OnProcessShader` 里是否有 `RemoveAt` 操作对这条 variant 生效。

这是"枚举进来了、再被 strip 出去"。根因在 stripping 规则。

**修法：**

- 检查自定义 `IPreprocessShaders` 的规则逻辑，确认这条 keyword 组合是否被误判
- 检查 Unity 内置 stripping（`ShouldShaderKeywordVariantBeStripped`）：这层基于 `BuildUsageTagGlobal`，处理的是雾效、光照贴图、GPU Instancing 等全局开关。如果 Graphics Settings 里关闭了某个 feature，对应的 variant 会被这层剔除

构建日志里四行数字的变化能帮你定位是哪层剔除的：

- `Full variant space → After filtering` 之间减少：枚举阶段（usedKeywords 不足）
- `After filtering → After builtin stripping` 之间减少：Unity 内置 stripping
- `After builtin stripping → After IPreprocessShaders` 之间减少：自定义 stripping

## 三、运行时层：variant 在包里，但命中有问题

### 第一步：确认是"未预热"还是"退化 fallback"

用 Frame Debugger：
- 看出问题的 draw call 实际使用的 shader 和 pass
- 对比当前材质 keyword 状态（可以在代码里打出来）

如果 Frame Debugger 里看到的 pass 和预期不符，说明运行时退化到了其他 variant。再去构建日志里确认精确 keyword 组合的 variant 是否存在。

如果 pass 符合预期，只是第一次出现卡，那就是 WarmUp 问题。

### 第二步：查 SVC 和 WarmUp

确认：

1. 包里有没有覆盖这条路径的 SVC
2. SVC 是否进入了目标构建包
3. SVC 是否在首次渲染前被加载
4. 加载后是否调用了 `WarmUp()`

```csharp
// 最简单的验证
IEnumerator WarmupOnLoad(ShaderVariantCollection svc)
{
    yield return svc.WarmupProgressively(0.05f); // 或直接调用 WarmUp()
    Debug.Log($"Warmup done: {svc.name}");
}
```

常见的漏洞：

- SVC 资产存在，但没有进入目标 bundle
- SVC 在代码里被加载，但 `WarmUp()` 没有被调用
- `WarmUp()` 被调用，但在首场景已经开始渲染之后才执行

### 第三步：DX12 / Vulkan / Metal 上的特殊情况

这三个 API 上，`WarmUp()` 后仍可能出现首帧轻微抖动，这是驱动 PSO 编译触发，不是 SVC 问题。

Unity 2023.3+ 可以使用 `ShaderWarmup.WarmupShader` / `WarmupShaderFromCollection` 配合 `RequestSetPassShaders` 更精确地预热 PSO 状态。

## 四、快速决策表

| 现象 | 最可能的原因 | 第一步 |
|------|------------|--------|
| 粉材质 | 构建期缺 variant | 查 IPreprocessShaders 日志，确认 variant 是否生成 |
| 效果略不对 | 运行时退化 fallback | Frame Debugger 看实际 pass |
| 首次卡顿 | 未预热 | 检查 SVC 内容 + WarmUp 时机 |
| 编辑器好，Player 坏 | 构建边界 | 查构建 allObjects 是否包含材质 |
| 热更新包里材质粉 | 枚举阶段缺失 | 用 SVC 显式登记 keyword 组合 |
| 特定平台才粉 | stripping 规则差异 | 在目标平台构建里查日志 |
| 加到 Always Included 就好了 | 全局兜底绕过枚举 | 这是止血，根因可能是枚举问题 |
| 加到 SVC 就好了 | SVC 补充了 usedKeywords | 这是修法，确认 SVC 长期维护 |

## 五、几个容易搞混的点

### "加到 Always Included 就好了"不等于"Always Included 是正确答案"

Always Included 通过 `kShaderStripGlobalOnly` 策略绕过了 `usedKeywords` 枚举，所以能修复情况 A（枚举阶段缺失）。但它的代价是变体按全局状态兜底生成，Player 体积和构建时间都会上升。

它是强力止血，不是长期设计。找到根因后，更稳的做法通常是让材质参与构建，或者用 SVC 精细补充。

### "加到 SVC 就好了"分两种情况

- SVC 作为构建资产：补充了 `usedKeywords`，让枚举阶段生成了之前缺失的 variant。这是修复情况 A。
- SVC 作为 Preloaded Shaders（挂在 Graphics Settings）：替代了整个 `usedKeywords`，只生成 SVC 里登记的 variant。这是非常激进的覆盖，用错了会导致其他 variant 全部丢失。

两种用法效果完全不同，排查时要先确认 SVC 是怎么被使用的。

### 构建日志四个数字的含义

| 数字 | 含义 |
|------|------|
| Full variant space | 这个 shader 理论上所有 keyword 组合的总数 |
| After filtering | 经过 PrepareEnumeration 过滤后剩余（只保留 usedKeywords 里有的组合） |
| After builtin stripping | Unity 内置 stripping 后剩余（全局 feature 不用的路径被剔） |
| After IPreprocessShaders | 自定义 stripping 后剩余 |

如果 `Full variant space` 和 `After filtering` 之间差异很大，说明 usedKeywords 收集的组合远少于理论空间——这是正常的精细收集，但也意味着热更包里的材质组合如果不在 usedKeywords 里就会被漏。

## 最后收成一句话

`Shader variant 缺失的根因只有三层：枚举阶段没生成（material 不在 allObjects）、stripping 删掉了、运行时命中了错误路径；排查时先用构建日志和 IPreprocessShaders 回调确认变体是否存在，再区分"从未枚举"和"枚举后被删"，最后再去查预热时机。`
