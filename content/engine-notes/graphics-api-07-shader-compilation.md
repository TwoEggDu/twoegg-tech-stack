---
title: "图形 API 07｜Shader 编译管线：HLSL → SPIR-V / MSL / DXBC，Variant 爆炸与缓存"
slug: "graphics-api-07-shader-compilation"
date: "2026-03-26"
description: "Unity 的 HLSL Shader 在构建时要编译成多个目标平台的 bytecode，同时每组 keyword 组合产生一个变体。这篇讲清楚 Shader 编译的完整管线：源码→中间表示→目标 bytecode，变体爆炸的成因与控制，以及 Shader 缓存的工作原理。"
weight: 760
tags:
  - "图形API"
  - "Shader编译"
  - "HLSL"
  - "SPIR-V"
  - "Shader变体"
  - "DXC"
  - "HLSLcc"
  - "Unity"
series: "图形 API 基础"
---
## 从源码到可执行 bytecode 的完整路径

一个 Unity `.shader` 文件从源码到最终运行在 GPU 上，要经历三个阶段：预处理展开、编译为中间表示、转译/编译为目标平台 bytecode。

```
Unity HLSL (.shader / .hlsl)
    ↓ Unity Shader Compiler（C++，随 Editor 分发）
[语法分析 + #pragma keyword 展开 → 每个变体单独处理]
    ↓
HLSL AST（Abstract Syntax Tree）

    ┌─────────────────────┬────────────────────────┐
    ↓                     ↓                        ↓
FXC (fxc.exe)         DXC (dxc.exe)            DXC → spirv-cross / HLSLcc
    ↓                 ↙        ↘                   ↓              ↓
DXBC (DX11)     DXIL (DX12)  SPIR-V (Vulkan)    MSL (Metal)  GLSL ES (OpenGL ES)
```

每条路径都是独立编译，互不共享中间产物。一个 Shader 在 Windows PC 上可能同时生成 DXBC（给 DX11）和 SPIR-V（给 Vulkan），在 Android 上生成 SPIR-V，在 iOS 上生成 MSL。

---

## 三个关键编译器

### FXC（fxc.exe）

微软提供的老版 Shader 编译器，生成 DXBC（DirectX Bytecode），上限是 Shader Model 5.0（SM 5.0）。DX11 时代的标准工具链。

特点：编译稳定，但不支持 SM 6.x 的任何新特性。Unity 2022 的 DX11 后端仍然用 FXC 生成 DXBC。

### DXC（DirectXShaderCompiler）

微软开源的新一代编译器（基于 LLVM/Clang），支持 SM 6.0~6.8，包含：

- **Wave Intrinsics**（SM 6.0+）：`WaveActiveSum`、`WaveGetLaneIndex`、`WaveBallot` 等 SIMD 组内操作，可以替代复杂的 Compute Shader reduce 算法
- **Mesh Shader / Amplification Shader**（SM 6.5+）
- **Ray Tracing**（DXIL Ray Tracing，SM 6.3+）

DXC 可以输出两种格式：
- **DXIL**：给 DX12 用，需要 GPU 驱动的 DXIL 签名验证
- **SPIR-V**：直接输出给 Vulkan，绕过 glslang，质量比 HLSLcc 路径更好

### HLSLcc 与 spirv-cross

这两个是**转译器**，不是编译器。它们把已经编译好的中间字节码（DXBC 或 SPIR-V）反编译再输出为文本格式的 MSL 或 GLSL ES。

转译的局限性：
- 某些 HLSL 结构（如 `precise` 修饰符、某些 intrinsic 的语义）在转译后精度或行为略有不同
- 转译出的 MSL 有时比手写 MSL 多出冗余的中间变量，Metal Shader 编译器无法完全消除，影响 iOS 上的 Shader 性能
- `WaveIntrinsics`（SM 6.x）经由 HLSLcc 转译后不可用；要在 Vulkan 上使用，必须走 DXC → SPIR-V 路径

---

## Shader 变体（Shader Variant）原理

每条 `#pragma multi_compile` 声明一组互斥的 keyword 选项，编译器对每个合法的 keyword 组合都单独编译一个变体。

```hlsl
// 以下 3 条 pragma 产生 4 × 2 × 2 = 16 个变体
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile _ _ADDITIONAL_LIGHTS
#pragma multi_compile _ _SHADOWS_SOFT
```

一个典型 URP Lit Shader 在考虑所有 keyword 后有 200~500+ 个变体，复杂项目（多个 Render Feature、自定义光照模型）可能轻松超过 5000 个。每个变体是一个独立编译单元，对应一份独立的 bytecode。

---

## multi_compile vs shader_feature vs shader_feature_local

这三种 pragma 是控制变体数量的核心工具，行为差异很关键：

**`multi_compile`**

```hlsl
#pragma multi_compile _ MY_FEATURE
```

无论项目里有没有材质用到 `MY_FEATURE`，这个 keyword 的两个变体（关闭/开启）都会被编译进包体。运行时可以随时调用 `Shader.EnableKeyword("MY_FEATURE")` 切换，不会丢失变体。

适合：全局光照、阴影等引擎级特性，运行时需要动态开关。

**`shader_feature`**

```hlsl
#pragma shader_feature MY_FEATURE
```

Build 阶段分析场景里所有材质，只编译实际用到的组合。如果没有任何材质开启 `MY_FEATURE`，对应的变体就不会进包。

代价：运行时无法动态开关未编译的 keyword。如果用 `AssetBundle` 或运行时创建材质，需要额外配置 Shader Variant Collection 保证变体存在。

**`shader_feature_local`**

```hlsl
#pragma shader_feature_local MY_FEATURE
```

与 `shader_feature` 相同的编译行为，但 keyword 是 per-material 的，不占用全局 keyword 空间（Unity 全局 keyword 上限是 256 个）。自定义 Shader 应该优先用这个。

---

## 变体爆炸的控制手段

### IPreprocessShaders 接口

在 Build 前通过代码 strip 掉不需要的变体：

```csharp
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine.Rendering;

public class CustomShaderStripper : IPreprocessShaders
{
    public int callbackOrder => 0;

    public void OnProcessShader(
        Shader shader,
        ShaderSnippetData snippet,
        IList<ShaderCompilerData> data)
    {
        // 移除所有包含 _SHADOWS_SOFT 且平台是 OpenGL ES 的变体
        var keyword = new ShaderKeyword(shader, "_SHADOWS_SOFT");
        for (int i = data.Count - 1; i >= 0; i--)
        {
            if (data[i].shaderCompilerPlatform == ShaderCompilerPlatform.GLES3x
                && data[i].shaderKeywordSet.IsEnabled(keyword))
            {
                data.RemoveAt(i);
            }
        }
    }
}
```

这个接口在每次 Build 时自动触发，可以精确控制哪些变体进入包体。

### Graphics Settings Shader Stripping

**Edit → Project Settings → Graphics → Shader Stripping**

Unity 内置的 strip 选项：

- **Instancing Variants**：不用 GPU Instancing 时关掉
- **Lightmap Modes**：根据项目实际用到的 Lightmap 模式只保留需要的
- **Fog Modes**：只保留项目用到的 Fog 类型（Linear / Exponential / Exp2）

### 查看变体数量

在 Inspector 里点击 Shader，下方会显示每个 Pass 的 variant count。也可以用命令行：

```
# Build 日志里查找 Shader 变体数
grep -i "shader variants" Editor.log
```

---

## Shader 缓存机制

### Editor Cache（Library/ShaderCache）

Unity Editor 把编译结果按 Shader 源码的 hash 缓存在 `Library/ShaderCache` 目录。增量编译时只重新编译源码发生变化的 Shader，没变的直接读缓存。

这个目录可以安全删除（下次打开 Editor 会重建），但如果团队共享 `Library` 目录（不推荐），要注意不同平台的 Editor 生成的缓存不兼容。

### PSO Cache（运行时缓存）

DX12 和 Vulkan 支持 Pipeline State Object 的序列化：把编译好的 PSO 写入磁盘，下次应用启动直接读取，跳过驱动端的 Shader 编译阶段。

这是消除"首次运行卡顿"的关键手段。没有 PSO 缓存时，每个材质/Shader 组合在首次渲染时触发驱动编译，出现帧率突降（compile stutter）。

Unity 在不同平台的 PSO 缓存支持程度：

- **DX12**：通过 `ID3D12PipelineLibrary` 支持，Unity 2021+ 开始逐步启用
- **Vulkan**：通过 `VkPipelineCache` 支持，存储在应用的持久化目录
- **Metal**：Metal 驱动有内置的 Shader 缓存机制，不需要额外处理

### ShaderVariantCollection 预热

```csharp
// 在 Loading 场景结束前触发，把常用变体提前编译
[SerializeField] ShaderVariantCollection variantCollection;

IEnumerator WarmUpShaders()
{
    variantCollection.WarmUp();
    yield return null; // 等一帧让编译完成
    // 进入游戏主场景
    SceneManager.LoadScene("Main");
}
```

`ShaderVariantCollection` 可以在 Editor 里通过 **Edit → Project Settings → Graphics → Shader Preloading** 记录运行时实际用到的变体，保存成 Asset，然后在加载屏幕调用 `WarmUp()`。

---

## 编译时间优化

构建时间随变体数量线性增长，几个减少编译时间的手段：

- 用 `shader_feature_local` 替代 `multi_compile`，让 Build 只编译项目实际用到的组合
- 用 `#pragma exclude_renderers gles gles3 glcore` 跳过不需要支持的平台 Shader 编译
- 开启 **Cache Shader Preprocessor**（Unity 2021.2+）：**Project Settings → Editor → Cache Preprocessor**，缓存预处理阶段的结果，增量构建时只处理变化的文件
- Shader Graph 的 Master Stack 会自动继承管线的所有 keyword，有时候变体数比手写 Shader 更多，注意在 Graph Inspector 里关掉项目不用的特性选项（如 Receive Fog、Write Rendering Layers）
- 拆分大型 Shader：把不同渲染需求写成独立的 Shader，而不是一个 Uber Shader 里塞所有分支

---

## 小结

- Shader 编译路径：DX11 用 FXC → DXBC；DX12 用 DXC → DXIL；Vulkan 用 DXC → SPIR-V；Metal/GLES 用 HLSLcc 转译
- `multi_compile` 始终编译全部变体，`shader_feature` 只编译用到的，`shader_feature_local` 不占全局 keyword 配额
- 变体数 = 所有 `multi_compile` 维度的笛卡尔积，控制变体爆炸需要 `IPreprocessShaders` + Graphics Settings Stripping 双管齐下
- PSO Cache（DX12/Vulkan）和 `ShaderVariantCollection.WarmUp()` 是消除运行时首次渲染卡顿的两个核心手段
- 减少编译时间：`shader_feature_local`、`exclude_renderers`、Cache Preprocessor、避免 Uber Shader
