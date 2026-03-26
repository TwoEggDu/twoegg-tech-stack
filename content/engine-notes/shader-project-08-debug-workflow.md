+++
title = "项目实战 08｜Shader 调试与性能分析工作流"
slug = "shader-project-08-debug-workflow"
date = 2026-03-26
description = "Shader 出现问题时如何快速定位？性能超出预算时如何找到瓶颈？这篇整理一套完整的 Shader 调试方法论：从颜色可视化到 Frame Debugger，从 GPU 性能分析到变体追踪。"
[taxonomies]
tags = ["Shader", "HLSL", "URP", "项目实战", "调试", "性能分析", "工作流"]
series = ["Shader 手写技法"]
[extra]
weight = 4470
+++

Shader 开发中遇到问题是常态——渲染结果不对、性能超出预算、特定机型崩溃。有一套系统的调试方法，能把"玄学调参"变成有据可查的排查流程。

---

## 一、颜色可视化调试

最快的 Shader 调试手段：把中间值输出为颜色，直接观察。

```hlsl
// 法线可视化（-1~1 → 0~1）
return half4(normalWS * 0.5 + 0.5, 1.0);

// UV 可视化
return half4(input.uv.x, input.uv.y, 0, 1);

// 深度可视化（线性）
float depth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
return half4(depth / 50.0, 0, 0, 1);   // 除以最大深度做归一化

// 特定通道提取
return half4(mask.r, mask.r, mask.r, 1);  // 只看 R 通道

// 0/1 二值显示（检查 step/clip 边界）
return half4(step(0.5, albedo.a), 0, 0, 1);

// 向量长度（检查法线是否归一化）
float len = length(normalWS);
return half4(abs(len - 1.0) * 10.0, 0, 0, 1);  // 接近 0 = 已归一化
```

**常见问题对应的可视化：**

| 问题 | 可视化内容 |
|------|-----------|
| 法线贴图不生效 | 输出 `normalWS * 0.5 + 0.5`，对比开关前后 |
| 阴影错误 | 输出 `mainLight.shadowAttenuation`，值应为 0 或 1 |
| UV 坐标错误 | 输出 UV，红=U，绿=V |
| 深度差计算错误 | 输出 `depthDiff / 10.0`，检查渐变方向 |
| Fresnel 异常 | 输出 `fresnel`，掠射角时应接近 1 |

---

## 二、Frame Debugger

Unity 内置的 Frame Debugger 是分析渲染问题的主要工具。

**打开方式：** Window → Analysis → Frame Debugger

**核心用法：**

1. **单步查看 DrawCall**：左侧列表展示每一帧的渲染步骤，点击可暂停到任意一步，右侧显示该 DrawCall 使用的 Shader、材质属性、RT
2. **检查 Pass 顺序**：确认 ShadowCaster、DepthOnly、ForwardLit 等 Pass 按正确顺序执行
3. **查看 RT 内容**：点击某个 Blit/DrawCall 后，可在 Game View 里看到该步骤完成后的 RT 状态
4. **定位消失的物体**：如果物体没有渲染，检查它是否出现在 DrawCall 列表里——没有通常是被剔除、Layer Mask 问题或 Queue 错误

**URP 里的重要 Pass 名称：**

```
MainLightShadow          → 主灯阴影 Pass
DepthPrepass             → 深度预通道
DrawOpaqueObjects        → 不透明物体（ForwardLit）
DrawSkybox               → 天空盒
CopyColorPass            → Opaque Texture 拷贝
DrawTransparentObjects   → 透明物体
PostProcessPass          → URP 内置后处理
```

---

## 三、RenderDoc 深度调试

Frame Debugger 看不到的内容（Shader 汇编、寄存器使用量、逐像素断点）需要 RenderDoc。

**基本流程：**
1. 在 Unity Editor 里打开 RenderDoc（Window → Analysis → RenderDoc）
2. 点击 RenderDoc 的 Capture 按钮，在游戏运行时截帧
3. 在 RenderDoc 里找到目标 DrawCall
4. 右击 DrawCall → Go to Pixel，在 Texture Viewer 里点击像素，查看该像素的完整着色管线

**Pixel History（像素历史）：**
选中 Texture Viewer 右键 → Pixel History，可以看到该像素被哪些 DrawCall 写过，每次写入前后的值是什么——可以精确找到值被错误覆盖的位置。

**Shader Debug（着色器调试）：**
在 Pipeline State 里双击 Shader，可以反编译到接近 HLSL 的层面查看，或者（在支持的驱动上）逐步调试着色器执行。

---

## 四、GPU 性能分析

### Unity Profiler（基础）

Window → Analysis → Profiler → GPU Usage：
- 查看每帧 GPU 时间
- 找出时间最长的 Pass

局限：只显示总时间，不能精确到 Shader 指令层面。

### 平台专用工具

| 平台 | 工具 | 主要功能 |
|------|------|---------|
| Android Mali | Mali Graphics Debugger | 指令级别分析，ALU/纹理采样占比 |
| Android Adreno | Snapdragon Profiler | GPU 占用率，Warp 利用率 |
| iOS | Xcode GPU Frame Capture | Metal 指令调试，性能计数器 |
| PC NVIDIA | NSight Graphics | CUDA 核心利用率，Shader 热点 |
| PC AMD | Radeon GPU Profiler | 类似 NSight |

**关键指标：**
- **ALU Bound**：Shader 指令太多，减少数学计算（用 half，合并运算）
- **Texture Bound**：纹理采样太多，合并贴图通道，减少采样次数
- **Bandwidth Bound**：数据传输量太大，压缩贴图格式，降低 RT 精度

---

## 五、Shader 变体追踪

变体爆炸导致构建慢、内存高、加载时卡顿。

**查看变体数量：**
```
Project → Shader 文件 → Inspector
可以看到该 Shader 的变体数量
```

**启用 Shader 变体收集：**
```csharp
// 在编辑器脚本里打印当前场景使用的所有变体关键字
foreach (var r in FindObjectsOfType<Renderer>())
    foreach (var m in r.sharedMaterials)
        if (m && m.shader)
            Debug.Log($"{m.shader.name}: {string.Join(",", m.shaderKeywords)}");
```

**Shader Stripping 设置：**
Edit → Project Settings → Graphics → Shader Stripping
- 关闭 `Instancing Variants` 如果不用 GPU Instancing
- 关闭不需要的 Fog Modes、Lightmap Modes

**自定义 Stripping（`IPreprocessShaders`）：**
```csharp
class MyShaderStripper : IPreprocessShaders
{
    public int callbackOrder => 0;
    public void OnProcessShader(Shader shader, ShaderSnippetData snippet,
                                 IList<ShaderCompilerData> data)
    {
        // 移除特定关键字的变体
        for (int i = data.Count - 1; i >= 0; i--)
        {
            if (data[i].shaderKeywordSet.IsEnabled(new ShaderKeyword("_UNUSED_FEATURE")))
                data.RemoveAt(i);
        }
    }
}
```

---

## 六、常见问题快速排查表

| 现象 | 可能原因 | 排查方法 |
|------|---------|---------|
| 物体完全黑色 | NdotL 计算错误，法线朝内 | 输出 normalWS 可视化 |
| 阴影消失 | 缺少 ShadowCaster Pass，ZWrite Off | Frame Debugger 查看 ShadowCaster |
| 透明物体遮挡错误 | ZWrite On + Transparent Queue | 检查 ZWrite Off 和 Queue=Transparent |
| 法线贴图不起作用 | 未传入 TBN，UVW 不对 | 输出切线空间法线，检查 Tangent 通道 |
| 移动端显示粉色/错误 | Shader 用了不支持的特性 | 检查 Console 的 Shader 编译错误 |
| 闪烁（Flickering） | Z-Fighting：两面在同一深度 | 添加 `Offset -1, -1` 或增加偏移 |
| 接缝/缝隙 | 法线插值边缘 | 确保 `normalize()` 在 Fragment 里 |
| 透明体内有黑色区域 | 深度写入干扰 | 确认 ZWrite Off |
| Bloom 不触发 | 自发光颜色 Intensity ≤ 1 | 使用 [HDR] 标签，颜色 Intensity > 1 |

---

## 七、Shader 开发流程建议

```
1. 建立最小可复现场景
   - 新场景 + 简单球体/平面 + 固定光源
   - 排除其他 Shader / 后处理的干扰

2. 分模块开发和验证
   - 先让漫反射正确，再加高光，再加法线贴图
   - 每增加一个模块就验证一次，不要一次写完再调试

3. 颜色可视化驱动调试
   - 不确定某个值的时候，先输出颜色看
   - 确认每个中间值在预期范围内

4. Frame Debugger 确认管线
   - 确认 Pass 执行顺序正确
   - 确认 RT 在每个 Pass 后符合预期

5. 真机测试早介入
   - 移动端 Shader 不能只在编辑器测试
   - 在目标最低配置机型上测试，而不是高端机

6. 性能预算先确认
   - 知道这个 Shader 的性能预算（Fragment 采样数 / 指令数上限）
   - 开发前确认，不要做完再砍
```

---

## 小结

Shader 调试的核心工具链：
- **颜色可视化**：最快的中间值检查手段
- **Frame Debugger**：渲染管线结构和 Pass 顺序
- **RenderDoc**：逐像素精确调试，Pixel History
- **平台 GPU 工具**：ALU/带宽瓶颈定位
- **变体追踪**：构建和加载性能

掌握这套工作流，Shader 开发的调试时间可以减少到原来的 1/3——剩下的时间用在创造更好的视觉效果上。

---

至此，**Shader 手写技法**系列全部完结：

| 层次 | 篇数 | 内容 |
|------|------|------|
| 入门 | 4 篇 | URP 结构、Unlit、Lambert、顶点动画 |
| 语法基础 | 6 篇 | 数据类型、数学函数、矩阵、控制流、变体、调试 |
| 核心光照 | 6 篇 | Blinn-Phong、法线贴图、阴影、附加光、PBR、IBL |
| 核心技法 | 12 篇 | UV 动画、溶解、卡通、描边、透明、折射、雾、Decal、视差、屏幕 UV、自发光、顶点色 |
| 进阶技法 | 10 篇 | 软粒子、Stencil、后处理、SSR、SSS、布料、水体、地形、GPU 粒子、移动端优化 |
| 项目实战 | 8 篇 | 卡通角色、写实武器、水面、草地、皮肤、UI 特效、后处理特效、调试工作流 |
| **合计** | **46 篇** | — |
