---
title: "URP 深度光照 05｜Reflection Probes 与 APV：从烘焙探针到自适应探针体积"
slug: "urp-lighting-05-reflection-probes-and-apv"
date: "2026-04-14"
description: "URP 中 Reflection Probes 的三种模式、Box Projection 配置、多 Probe 混合策略、移动端取舍，以及 Unity 6 引入的 Adaptive Probe Volumes（APV）新方案。"
tags:
  - "Unity"
  - "URP"
  - "Reflection Probe"
  - "APV"
  - "光照"
  - "渲染管线"
series: "URP 深度"
weight: 1584
---
> **读这篇之前**：本篇涉及 URP 光照系统基础和反射算法原理。如果不熟悉，建议先看：
> - [URP 深度光照 01｜URP 光照系统]({{< relref "rendering/urp-lighting-01-lighting-system.md" >}})
> - [Unity 渲染系统补H｜反射方案对比]({{< relref "rendering/unity-rendering-supp-h-reflection.md" >}})

Reflection Probe 是 URP 里最基础的环境反射方案——在场景中放置探针，捕获周围环境到 Cubemap，附近物体采样这个 Cubemap 作为反射来源。原理不复杂，但模式选择、参数配置、多探针混合策略直接决定了反射效果和性能代价。Unity 6 还引入了 APV（Adaptive Probe Volumes），试图替代手动摆放 Light Probe Group 的传统流程。这篇把两者一起讲清楚。

---

## URP 中 Reflection Probes 的三种模式

URP 的 Reflection Probe 组件提供三种 Type 选项，决定 Cubemap 何时渲染、能反映什么内容：

**Baked（烘焙）**

编辑器中点击 Bake 后渲染一次 Cubemap，结果序列化为资产。运行时只有一次贴图采样的代价——几乎为零。但 Cubemap 是静态快照，不反映任何动态物体。适合室内墙壁、走廊、静态建筑外立面等不需要动态反射的场景。

**Realtime（实时）**

运行时按设定频率重新渲染 Cubemap。每次更新等于从探针位置渲染完整场景的 6 个面（或按 Time Slicing 设置分帧更新），代价极高。好处是能捕获动态物体——角色走过地面时，光滑地板上能看到角色的倒影。

Time Slicing 选项决定更新策略：
- `All Faces At Once`：单帧渲染 6 个面，延迟最低但单帧代价最高
- `Individual Faces`：每帧渲染 1 个面，6 帧完成一次完整更新
- `No Time Slicing`：与 All Faces At Once 相同，但不在帧间平滑过渡

**Custom（自定义）**

Cubemap 不自动刷新，完全由脚本控制刷新时机。通过 `ReflectionProbe.RenderProbe()` 手动触发一次渲染。适合可控场景：水面只在相机靠近时刷新反射，或者过场动画在特定时机更新一次环境反射。这是在画质和性能之间做精确取舍的最佳选择。

---

## 关键配置参数

Reflection Probe 的效果好不好，大部分取决于以下参数的配置：

**Box Projection**

默认的 Cubemap 采样假设反射来自无限远处——室内场景下，这会导致所有位置看到的反射完全一样，视差完全错误。开启 Box Projection 后，探针被限定在一个包围盒内，采样时根据视线与包围盒的交点修正 UV。室内走廊、房间等封闭空间必须开启，否则反射看起来是"贴了一张平面图"。

**Blend Distance**

控制多个 Probe 影响范围重叠时的混合过渡宽度。值为 0 时，两个 Probe 边界处反射会硬切；增大 Blend Distance 可以在边界处平滑过渡。通常设为 1~3 米即可。如果场景里 Probe 分布密集，适当增大 Blend Distance 避免走动时反射跳变。

**Importance**

当同一位置被多个 Probe 覆盖时，Importance 值高的优先。典型用法：场景里有一个大范围的低质量 Probe 作为兜底，局部区域放置高质量小范围 Probe 并设更高 Importance，确保局部反射细节不被全局 Probe 覆盖。

**Resolution**

每个 Cubemap 面的分辨率：128、256、512、1024 可选。分辨率直接影响反射清晰度和显存占用。一个 Cubemap 有 6 个面，RGBA32 格式下：256 分辨率 = 6 x 256 x 256 x 4 = 1.5 MB；1024 分辨率 = 24 MB。场景里放 10 个 1024 的 Probe，仅反射贴图就占 240 MB。

**Pipeline Asset 设置**

URP Pipeline Asset 的 Lighting 区域需要确保 Reflection Probes 已启用。另外 Probe Blending 选项控制是否允许多个 Probe 之间混合——如果关闭，每个像素只取最近的一个 Probe，边界处会硬切。

---

## 移动端策略

移动端对反射的预算极其有限，需要精打细算：

**Baked 为主，Realtime 例外使用。** 整个场景用 Baked Probe 覆盖，只对水面或镜面等视觉核心位置放一个 Realtime Probe，且用 Custom 模式手动控制刷新频率——不要每帧刷新，用 `OnDemand` 刷新模式配合脚本，在相机靠近时才触发 `RenderProbe()`。

**分辨率压到最低。** 填充用途的 Probe（走廊、大厅）用 128 分辨率足够；只有视觉核心表面（主角盔甲、汽车车漆）用 256，绝不超过 256。

**关闭 Probe Blending。** 低端机上 Probe Blending 意味着每个像素要采样两个 Cubemap 再混合——额外一次贴图采样和插值运算。对于 GPU 带宽紧张的低端设备，这个代价不值得。直接用最近的单个 Probe，接受边界处的跳变。

**考虑单个全局 Probe。** 如果场景主要是室外或对反射精度要求不高，一个放在场景中心的 Baked Probe 就能提供基础环境反射。省去多 Probe 的 Blend 计算和显存占用，视觉上通过粗糙度遮掩精度不足。

---

## APV（Adaptive Probe Volumes）——Unity 6+

### 传统 Light Probe Group 的痛点

Light Probe Group 是 Unity 传统的间接光方案：手动在场景中摆放一组探针点，烘焙时记录每个点的球谐（SH）数据，运行时动态物体通过四面体插值获取间接光。

问题是"手动摆放"。大场景里需要成百上千个探针，位置不合适会导致漏光或暗斑，调整一次需要重新理解场景结构。这个工作量在大型开放世界项目里不可接受。

### APV 的工作方式

Adaptive Probe Volumes 用体积自动分布探针。在场景中添加一个 Probe Volume 组件，定义覆盖范围，烘焙时系统自动根据几何复杂度调整探针密度——几何变化剧烈的区域（墙角、门框）密度高，开阔平坦区域密度低。

与传统 Light Probe Group 的关键区别：

- 探针位置由算法决定，不需要手动摆放
- SH 数据存储在 3D 纹理中（不是逐探针的数组），GPU 采样效率更高
- 支持 Dilation（膨胀）算法，自动处理探针落在几何体内部的情况
- 支持逐场景和跨场景的流式加载

### URP 中启用 APV

在 `Project Settings → Graphics → Lighting` 中找到 **Light Probe System** 选项，切换为 **Probe Volumes**（Unity 6 的命名）。切换后，传统 Light Probe Group 会被禁用——两者不能共存。

### 配置与烘焙

启用后：
1. 在场景中添加 **Probe Volume** 组件（`Add Component → Rendering → Probe Volume`），调整包围盒覆盖需要间接光的区域
2. 配置探针密度：`Min/Max Subdivision Level` 控制最小和最大细分级别，数值越大探针越密
3. 配置 Dilation：Dilation Iterations 和 Dilation Distance 控制对无效探针的修复力度
4. 打开 Lighting 窗口点击 Generate Lighting，APV 和 Lightmap 一起烘焙

烘焙结果以 3D 纹理形式存储 SH 数据。运行时动态物体的间接光通过对这个 3D 纹理的三线性插值获取——比传统 Light Probe Group 的四面体查找更高效。

---

## 从 Light Probe Group 迁移到 APV

如果项目从旧版本升级到 Unity 6 并准备使用 APV，迁移步骤：

1. **启用 APV**：在 Project Settings → Graphics → Lighting 中将 Light Probe System 切换为 Probe Volumes
2. **添加 Probe Volume**：在场景中需要间接光的区域添加 Probe Volume 组件，调整包围盒大小
3. **配置密度参数**：根据场景复杂度设置 Subdivision Level；室内场景需要更高密度，室外可以低一些
4. **配置 Dilation**：保持默认值即可满足多数场景，遇到漏光再增加 Dilation Iterations
5. **烘焙**：Generate Lighting
6. **移除旧 Light Probe Group**：确认烘焙结果正确后，删除场景中所有 Light Probe Group 组件

注意：**APV 和 Light Probe Group 不能在同一个场景中共存。** 启用 APV 后，旧的 Light Probe Group 数据会被忽略。不存在"渐进式迁移"的可能——必须一次性切换。

### 移动端 APV 内存考量

APV 的 SH 数据以 3D 纹理存储，纹理大小取决于探针密度和覆盖范围。移动端需要注意：

- 降低 Subdivision Level，不要追求极高密度
- 大场景使用多个小 Probe Volume 配合流式加载，避免一次性加载整个场景的 3D 纹理
- 监控 Profiler 中 Texture Memory 的变化，APV 的纹理可能占用 10~50 MB

---

## 常见问题

**Q：Reflection Probe 效果不对，反射看起来错位？**

首先检查 Box Projection 是否开启——室内场景不开 Box Projection，反射必然错位。其次检查分辨率是否过低导致反射模糊到看不出内容。最后确认 Probe 的包围盒是否正确包围了反射区域。

**Q：APV 烘焙后场景变暗？**

检查 Indirect Intensity 是否被调低（Lighting 窗口 → Environment → Indirect Intensity，默认 1）。再检查 Probe Volume 的覆盖范围是否遗漏了变暗区域——未被 Probe Volume 覆盖的区域不会接收到 APV 的间接光。如果探针密度太低，光照在稀疏探针间插值可能偏暗，适当提高 Subdivision Level。

**Q：移动端 APV 内存超预算？**

三个方向压缩：降低 Subdivision Level 减少探针总数；缩小 Probe Volume 覆盖范围只包围玩家可达区域；使用 Streaming 模式分块加载。如果仍然超标，考虑暂时回退到传统 Light Probe Group——手动摆放的探针数量完全可控。

---

## 导读

- [URP 深度光照 01｜URP 光照系统]({{< relref "rendering/urp-lighting-01-lighting-system.md" >}})
- [Unity 渲染系统补H｜反射方案对比]({{< relref "rendering/unity-rendering-supp-h-reflection.md" >}})
- [URP 深度配置 01｜Pipeline Asset 全字段解析]({{< relref "rendering/urp-config-01-pipeline-asset.md" >}})
