---
date: "2026-04-12"
title: "变体排障速查：从现象到修复的最短路径"
description: "按现象分支的排障速查卡：粉材质、效果不对、首帧卡顿、AB 才出问题、特定平台才出问题——每个分支给出最短的定位步骤和对应修复方案，不讲原理只给动作。"
slug: "unity-shader-variant-troubleshooting-quick-reference"
weight: 170
featured: false
tags:
  - "Unity"
  - "Shader"
  - "Variant"
  - "Troubleshooting"
  - "Debug"
series: "Unity Shader Variant 治理"
  - "Unity 资产系统与序列化"
  - "Unity Shader Variant 治理"
---
这篇是排障速查卡，不讲原理。如果你需要理解为什么会出现这些问题，请回到[变体全流程总览]({{< relref "rendering/unity-shader-variant-full-lifecycle-overview.md" >}})。

使用方式：找到和你的现象最匹配的分支，按步骤走。

---

## 现象 A：粉材质 / Error Shader / 关键 Pass 完全失效

`这是最严重的情况：运行时找不到任何可接受的 variant，直接回退到 Error Shader。`

### 定位步骤

1. **确认是构建后才出现还是编辑器里就有**
   - 编辑器里就粉 → 大概率是 Shader 编译错误，看 Console 的 Shader Error
   - 只在构建后出现 → 继续下一步

2. **开启 Strict Shader Variant Matching**
   - `Player Settings → Other Settings → Strict Shader Variant Matching` 打勾
   - 重新打包，运行后看日志（Console / logcat）
   - 日志会报告具体是哪个 Shader、哪个 Pass、哪组 keyword 找不到匹配

3. **根据日志定位缺失原因**
   - 日志里提到的 keyword 组合，检查是否有材质在使用 → 如果没有，需要通过 SVC 显式登记
   - 检查该 Shader 是否使用了 `shader_feature` → 如果是，该 keyword 必须有材质引用或 SVC 登记才会被编译
   - 检查该 Shader 是否在 `IPreprocessShaders` / `OnProcessShader` 回调里被自定义规则删掉了

4. **检查 URP Prefiltering**
   - 如果 keyword 和 URP 功能相关（Decal Layers、SSAO、Additional Lights 等），检查构建时生效的 URP Asset 是否开启了该功能
   - 检查所有 Quality Level 关联的 URP Asset，确认功能开关覆盖完整

### 修复方案

| 原因 | 修复 |
|------|------|
| `shader_feature` keyword 无材质引用 | 把对应变体加入 SVC，或改用 `multi_compile` |
| URP 功能未开启导致 Prefiltering 删除 | 在对应 URP Asset 里开启该功能 |
| 自定义 stripping 误删 | 修改 IPreprocessShaders 规则，排除该 Shader |
| 全局 Shader 不在 Always Included 里 | 加入 `Graphics Settings → Always Included Shaders` |

---

## 现象 B：画面不对，但不粉——光照/阴影/特效/贴花效果异常

`这是最隐蔽的情况：运行时找不到精确 variant，退化到了一条"能跑但不正确"的近似路径。`

### 定位步骤

1. **开启 Strict Shader Variant Matching**
   - 同现象 A 的步骤 2。Strict Matching 会把模糊退化变成明确的错误日志
   - 如果开启后出现了新的粉材质或错误日志，说明确实存在精确变体缺失

2. **用 Frame Debugger 确认实际走了哪条路径**
   - `Window → Analysis → Frame Debugger`
   - 找到出问题的 Draw Call，查看它实际使用的 Shader、Pass 和 keyword 状态
   - 对比预期的 keyword 组合和实际命中的 keyword 组合

3. **检查是不是运行时 keyword 设置问题**
   - 有些 keyword 是全局设置的（`Shader.EnableKeyword`），有些是逐材质的（`Material.EnableKeyword`）
   - 如果全局 keyword 没有正确设置，所有材质的命中都会偏
   - 如果是动态创建的材质，检查创建后是否正确设置了 keyword

4. **检查 Quality Level 切换**
   - 如果不同质量档使用不同的 URP Asset，切换质量档后某些功能开关可能变化
   - 开发时在最高画质档正常，切到低画质档后功能消失 → 低画质档的 URP Asset 没开对应功能

### 修复方案

| 原因 | 修复 |
|------|------|
| 精确 variant 缺失，退化到 fallback | 同现象 A 的修复方案 |
| 全局 keyword 未正确设置 | 检查 `Shader.EnableKeyword` / `DisableKeyword` 调用时机 |
| 动态材质 keyword 未设置 | 创建材质后显式设置需要的 keyword |
| 质量档 URP Asset 功能不一致 | 统一各质量档 URP Asset 的功能开关 |

---

## 现象 C：首帧卡顿，后续正常

`变体在构建产物里，运行时也能正确命中，但第一次使用时 GPU 需要编译该变体的平台程序，造成一次性卡顿。`

### 定位步骤

1. **开启 Log Shader Compilation**
   - `Player Settings → Other Settings → Log Shader Compilation` 打勾
   - 重新打包运行
   - 日志中会显示运行时编译 Shader 的条目，确认是否有大量首次编译发生在关键时刻

2. **检查 WarmUp 是否覆盖**
   - 确认 SVC 包含了首帧可见的所有 Shader 变体
   - 确认 `WarmUp()` 或 `WarmUpProgressively()` 在 Loading Screen 期间被调用
   - 确认 WarmUp 在 Loading Screen 关闭之前完成

3. **检查 Profiler**
   - CPU 模块里看 `Shader.CreateGPUProgram` 标记
   - 如果该标记出现在首帧而不是 Loading 期间，说明 WarmUp 没覆盖到

### 修复方案

| 原因 | 修复 |
|------|------|
| SVC 未包含首帧变体 | 补充 SVC 内容，确保首帧可见的变体都被登记 |
| WarmUp 未调用 | 在 Loading Screen 期间调用 `svc.WarmUp()` |
| WarmUp 调用太晚 | 把 WarmUp 提前到 Loading Screen 开始阶段 |
| 变体太多 WarmUp 太慢 | 拆分 SVC，只预热当前场景需要的；或用 `WarmUpProgressively(N)` 分帧 |

---

## 现象 D：编辑器正常，AB 加载后才出问题

`这是 AssetBundle 场景下最常见的变体问题，根源通常是交付责任归属不对。`

### 定位步骤

1. **确认 Shader 的交付责任**
   - 这个 Shader 是跟 Player 走（Always Included / 场景直接引用）还是跟 bundle 走？
   - 如果 bundle 里的材质引用了一个不在 Always Included 里的 Shader，且该 Shader 没有被任何 bundle 显式包含 → 运行时找不到

2. **检查 AB 构建和 Player 构建的一致性**
   - AB 是什么时候打的？Player 是什么时候打的？
   - 如果 AB 构建时的 Graphics Settings / URP Asset / Quality Settings 和 Player 构建时不一致，变体保留面可能不同
   - 特别注意：AB 构建不会自动使用 Player 构建时的 `Always Included Shaders` 列表作为裁剪豁免

3. **检查 Shader 是否在 bundle 的依赖链中**
   - 如果 Shader 被打进了 bundle A，但引用它的材质在 bundle B，而 B 加载时 A 还没加载 → 材质找不到 Shader
   - 用 `AssetDatabase.GetAssetBundleDependencies` 或框架的依赖分析工具检查

4. **快速验证：把问题 Shader 加入 Always Included**
   - 如果加入 Always Included 后问题消失，说明原因确实是交付责任归属问题
   - 注意：这只是验证手段，不一定是最终方案——Always Included 会增大包体

### 修复方案

| 原因 | 修复 |
|------|------|
| Shader 不在 Always Included，bundle 也没带 | 加入 Always Included（全局 Shader）或确保 bundle 包含 Shader |
| AB 构建和 Player 构建配置不一致 | 统一构建配置，确保 AB 和 Player 用同一套 Graphics Settings |
| bundle 依赖关系断裂 | 修正 bundle 打包策略，确保 Shader 所在 bundle 先于使用者加载 |
| bundle 里的 Shader 变体被过度裁剪 | 检查 AB 构建时的 IPreprocessShaders 规则，确认关键变体未被删除 |

---

## 现象 E：特定平台才出问题

`在 PC / 编辑器上正常，在 Android / iOS / 特定 GPU 上出问题。`

### 定位步骤

1. **检查图形 API 差异**
   - PC 默认用 D3D11/D3D12，移动端用 Vulkan / GLES / Metal
   - 不同图形 API 的 Shader 是独立编译的——D3D11 的变体保留了不等于 Vulkan 的也保留了
   - `Player Settings → Other Settings → Graphics APIs` 检查目标平台启用了哪些 API

2. **检查平台特有的 keyword**
   - 某些 keyword 是平台相关的（比如 `_SURFACE_TYPE_TRANSPARENT` 在某些平台上行为不同）
   - URP Prefiltering 会根据平台能力过滤变体（比如 GL 设备上 Decal Layers 变体会被强制裁掉）

3. **在目标设备上开启日志**
   - Android：用 `adb logcat | grep -i shader` 捕获 Shader 相关日志
   - iOS：用 Xcode Console 查看
   - 同样开启 Strict Shader Variant Matching 和 Log Shader Compilation

4. **检查 GPU 兼容性**
   - 某些老旧 GPU 不支持特定 Shader 特性
   - Mali / Adreno / PowerVR 对某些指令的支持不同
   - 如果只在特定 GPU 上出问题，检查 Shader 是否有平台兼容分支

### 修复方案

| 原因 | 修复 |
|------|------|
| 目标图形 API 的变体未编译 | 确认 `Graphics APIs` 列表正确，重新构建 |
| 平台 Prefiltering 删除了变体 | 检查 URP 的平台特定过滤逻辑 |
| GPU 不支持特定特性 | 添加 fallback Shader 或平台兼容分支 |

---

## 通用工具速查

| 工具 | 用途 | 位置 |
|------|------|------|
| Strict Shader Variant Matching | 把退化命中变成明确错误日志 | Player Settings → Other Settings |
| Log Shader Compilation | 记录运行时首次编译的变体 | Player Settings → Other Settings |
| Frame Debugger | 查看 Draw Call 实际使用的 Shader/Pass/keyword | Window → Analysis → Frame Debugger |
| IPreprocessShaders 日志 | 观测构建期保留的变体统计 | 自定义 Editor 脚本 |
| Editor.log | 查看构建期 Shader 编译统计 | Console → Open Editor Log |
| Memory Profiler | 查看运行时 Shader 对象的内存占用 | Package Manager 安装 |
| ShaderVariantCollection | 登记关键变体 + WarmUp 预热 | 通过编辑器录制或脚本生成 |

---

## 官方文档参考

- [Graphics Settings](https://docs.unity3d.com/Manual/class-GraphicsSettings.html)
- [ShaderVariantCollection](https://docs.unity3d.com/ScriptReference/ShaderVariantCollection.html)
- [Shader loading](https://docs.unity3d.com/Manual/shader-loading.html)

## 排障决策树（最短路径）

```
你的问题是什么？
│
├─ 粉材质 / Error Shader
│   ├─ 编辑器里就粉 → Shader 编译错误，看 Console
│   └─ 只在构建后 → 开启 Strict Matching，看缺什么 → 补 SVC 或 Always Included 或修 URP Asset
│
├─ 效果不对但不粉
│   ├─ 开启 Strict Matching 出现新错误 → 精确 variant 缺失，同上处理
│   └─ Strict Matching 无新错误 → Frame Debugger 对比 keyword，检查全局/材质 keyword 设置
│
├─ 首帧卡顿后续正常
│   ├─ Log Shader Compilation 有大量首次编译 → WarmUp 未覆盖，补 SVC + 提前 WarmUp
│   └─ 无大量编译 → 不是变体问题，查 Instantiate / 贴图上传 / 其他加载开销
│
├─ AB 加载后才出问题
│   ├─ 加 Always Included 能修好 → 交付责任问题，决定是留 Always Included 还是修 bundle 策略
│   └─ 加了也不好 → 检查 AB 构建配置一致性 / bundle 依赖链
│
└─ 特定平台才出问题
    ├─ 检查 Graphics APIs 列表
    ├─ 目标设备开日志 + Strict Matching
    └─ 检查平台 Prefiltering 和 GPU 兼容性
```
