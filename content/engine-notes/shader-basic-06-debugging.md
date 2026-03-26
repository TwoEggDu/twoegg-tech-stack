+++
title = "Shader 语法基础 06｜调试技巧：颜色可视化、Frame Debugger、RenderDoc"
slug = "shader-basic-06-debugging"
date = 2026-03-26
description = "Shader 出错时没有断点、没有 Debug.Log，只能靠颜色输出来观察中间值。掌握颜色可视化法线/UV/光照数据，配合 Frame Debugger 和 RenderDoc 定位问题。"
[taxonomies]
tags = ["Shader", "HLSL", "URP", "语法基础", "调试", "Frame Debugger", "RenderDoc"]
series = ["Shader 手写技法"]
[extra]
weight = 4100
+++

Shader 调试和 C# 完全不同——没有断点，没有 `print`，出了问题只能观察屏幕上的颜色。这篇整理实际开发中最常用的调试手段。

---

## 核心方法：把数据输出为颜色

Fragment Shader 的输出是颜色，所以"看数据"的方式是**把中间值映射成颜色输出**。

```hlsl
// 临时改 return，观察某个值
return half4(normalWS * 0.5 + 0.5, 1);   // 法线可视化
return half4(input.uv, 0, 1);            // UV 可视化
return half4(NdotL, NdotL, NdotL, 1);   // 光照强度灰度图
return half4(abs(normalWS), 1);          // 法线绝对值（彩色）
```

**关键技巧：值域映射**

大多数数据的范围不是 [0, 1]，直接输出颜色会截断（小于 0 变黑，大于 1 变白）。需要映射到 [0, 1]：

| 原始范围 | 映射公式 | 示例 |
|---------|---------|------|
| [-1, 1]（法线） | `v * 0.5 + 0.5` | 法线颜色化（蓝紫色是正常的） |
| [0, N]（任意正数） | `v / N` | 深度、距离 |
| 任意范围 | `saturate((v - min) / (max - min))` | 通用归一化 |

---

## 常用可视化模式

### 法线可视化

```hlsl
return half4(normalWS * 0.5 + 0.5, 1.0);
```

正常的世界空间法线：
- 朝上（0,1,0）→ 绿色
- 朝右（1,0,0）→ 红色
- 朝前（0,0,1）→ 蓝色
- 背面（负值）→ 变暗（小于 0.5 的颜色）

如果法线变换有问题，这里会看到奇怪的颜色或全黑/全白。

### UV 可视化

```hlsl
return half4(input.uv.x, input.uv.y, 0, 1.0);
```

正常 UV：左下角黑色(0,0,0)，右下角红色(1,0,0)，左上角绿色(0,1,0)，右上角黄色(1,1,0)。

UV 有问题（超出 0-1、镜像、旋转）在这里立刻可见。

### 光照强度可视化

```hlsl
float NdotL = saturate(dot(normalWS, mainLight.direction));
return half4(NdotL, NdotL, NdotL, 1.0);   // 灰度图
```

正面受光应该是白色，背面应该是黑色，侧面渐变。如果全黑，法线方向或光线方向有问题。

### 顶点颜色可视化

```hlsl
// 在 Attributes 里加 float4 color : COLOR;
return half4(input.color.rgb, 1.0);
```

检查顶点颜色是否正确导入，常用于检查草地弯曲遮罩、顶点绘制的 AO 等。

### 深度可视化

```hlsl
// positionHCS.z / positionHCS.w 是线性深度（0~1，近~远）
float depth = input.positionHCS.z / input.positionHCS.w;
return half4(depth, depth, depth, 1.0);
```

---

## 调试时的常见排查顺序

遇到 Shader 显示异常，按这个顺序排查：

1. **全白/全黑**：颜色输出超出 0-1 范围（缺少 `saturate`），或者贴图采样返回 0
2. **返回固定颜色（如 `return half4(1,0,0,1)`）正常，加光照后异常**：光照计算问题，可视化 NdotL
3. **形状正确但颜色错**：贴图问题，可视化 UV
4. **光照方向反了**：可视化法线，确认变换是否正确
5. **只有一部分像素正常**：检查透明度、AlphaTest 的 clip 条件

---

## Frame Debugger

**在哪里**：Unity 菜单 `Window → Analysis → Frame Debugger`

**能做什么**：

- 看当前帧所有 Draw Call 和 Pass 的执行顺序
- 每个 Pass 的渲染目标（RT）前后对比
- 验证某个 Pass 是否在执行
- 确认 SRP Batcher 合批是否生效（合批的 Draw Call 显示为 SRP Batch）

**常见用途**：

```
✅ 确认 ShadowCaster Pass 有没有执行
✅ 确认 ForwardLit Pass 的 RT 颜色对不对
✅ 检查后处理 Pass 的执行顺序
✅ 查看 Draw Call 数量，确认合批效果
```

**使用方式**：

1. 打开 Frame Debugger，点击 Enable
2. 游戏视图里每帧会暂停在第一个 Draw Call
3. 用左右箭头逐步前进，观察每一步的渲染结果
4. 点击某个 Pass，右侧显示该 Pass 的 Shader、Properties、RT

---

## RenderDoc

**适合场景**：Frame Debugger 看不到的细节——像素级调试、Shader 输入输出、顶点数据。

**安装**：从 renderDoc 官网下载，Unity 内置了集成接口。

**在 Unity 里使用**：

1. 游戏视图标题栏右键 → Load RenderDoc（或直接在 RenderDoc 里启动 Unity）
2. 捕获一帧（F12 或 RenderDoc 里的 Capture）
3. 在 RenderDoc 里打开捕获文件

**常用功能**：

| 功能 | 路径 | 用途 |
|------|------|------|
| Texture Viewer | 点击任意 Pass → Outputs | 看每个 RT 的内容 |
| Mesh Viewer | 点击 Draw Call → Mesh | 看顶点数据（位置、法线、UV）是否正确 |
| Pixel History | 右键某个像素 → Pixel History | 看这个像素被哪些 Pass 写过 |
| Shader Debugger | 点击 Draw Call → Shader | 逐步调试 Vertex/Fragment Shader |

**Pixel History** 特别有用：选中一个看起来不对的像素，RenderDoc 会列出所有影响它的 Draw Call，以及每次写入的颜色值——深度测试通过还是失败、Blend 前后的值都能看到。

---

## 快速验证 SRP Batcher 兼容性

SRP Batcher 要求 CBUFFER 结构满足条件。如果材质 Inspector 里显示 "SRP Batcher：Not compatible"，原因通常是：

1. CBUFFER 里的变量不完整（缺少某个 Property）
2. CBUFFER 外面有变量（贴图 `_MainTex_ST` 放错了位置）
3. Shader 有 `UNITY_INSTANCING_BUFFER` 且配置错误

Frame Debugger 里，SRP Batcher 合批成功的 Draw Call 显示为一条 `SRP Batch`，未合批的显示为单独条目。用这个快速验证优化效果。

---

## 调试 Shader 变体问题

如果 Shader 在某个平台表现不对，可能是变体问题：

```csharp
// 用代码检查当前材质启用了哪些关键字
foreach (var kw in material.shaderKeywords)
    Debug.Log(kw);
```

或者在 Shader Inspector 里点击 "Compile and show code"，选择目标平台，看编译后的 HLSL 是否包含预期代码。

---

## 移动端远程调试

| 工具 | 平台 | 功能 |
|------|------|------|
| Xcode GPU Frame Capture | iOS | 完整 GPU 帧分析，Metal 级别 |
| Snapdragon Profiler | Android (Adreno) | 捕获帧、Shader 分析、带宽数据 |
| Mali Graphics Debugger | Android (Mali) | 帧捕获、纹理、着色器 |
| Android GPU Inspector | Android (通用) | Google 出品，支持 Vulkan |

这些工具在真机上捕获帧，能看到 Unity 里 Frame Debugger 看不到的硬件级信息（tile memory 使用量、带宽、GPU cycle 分布）。

---

## 常见问题速查

| 现象 | 可能原因 | 验证方法 |
|------|---------|---------|
| 全黑 | 法线方向反了；贴图返回 0；光照系数 < 0 未 saturate | 可视化 NdotL、法线 |
| 全白 | 颜色值超出 1.0 | 把 return 改成 `return half4(1,0,0,1)` 确认 |
| 花纹 / 噪点 | half 精度不足；UV 超范围 | 用 float 替换 half 对比 |
| 错误的光方向 | 法线没有归一化；切线空间计算错误 | 可视化法线方向 |
| ShadowCaster 无效 | Pass 缺失；关键字未添加 | Frame Debugger 确认 Pass 执行 |
| 合批失效 | CBUFFER 不合规 | Frame Debugger 看 SRP Batch 数量 |

---

## 小结

| 方法 | 适合场景 |
|------|---------|
| 颜色输出中间值 | 快速验证法线、UV、光照数据是否正确 |
| Frame Debugger | 确认 Pass 执行顺序、合批效果、RT 内容 |
| RenderDoc | 像素级调试、顶点数据、Shader 逐步执行 |
| 移动端工具 | 硬件级性能数据（带宽、tile memory） |

语法基础层到这里结束。下一层进入**核心光照**——Blinn-Phong、PBR（金属度/粗糙度）、法线贴图、阴影接收，把光照模型真正写完整。
