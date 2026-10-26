---
title: "包体管理与分发 02｜包体大小控制——压缩、裁剪与贡献度追踪"
slug: "delivery-package-distribution-02-size-control"
date: "2026-04-14"
lastmod: "2026-10-26"
description: "包体超预算时不是'随便砍点资源'。系统性的包体控制需要知道每个模块贡献了多少、哪些可以压缩、哪些可以裁剪、哪些必须延迟加载。"
tags:
  - "Delivery Engineering"
  - "Package Management"
  - "Size Optimization"
series: "包体管理与分发"
primary_series: "delivery-package-distribution"
series_role: "article"
series_order: 20
weight: 520
delivery_layer: "principle"
delivery_volume: "V06"
delivery_reading_lines:
  - "L1"
  - "L2"
---

## 这篇解决什么问题

上一篇定义了首包 / 追加 / 热更的三层架构和包体预算。这一篇回答：预算定了之后，具体怎么控制包体大小——从贡献度分析到压缩策略到代码裁剪。

> 证据范围：下文提供分析方向和待执行检查，没有本项目的构建占比、压缩收益或设备结果。涉及 Unity 的边界按 6000.0 文档核对，具体产物与运行表现需另行验证。

## 本质是什么

包体控制的核心流程：

```
度量（知道哪里大） → 分析（知道为什么大） → 优化（有针对性地减小） → 监控（防止反弹）
```

不度量就优化 = 盲目砍资源。不监控就发版 = 下次还会超。

### 度量：包体贡献度分析

第一步是知道包体里每个部分占了多少：

| 类别 | 本次构建占比 | 优先检查方向 |
|------|---------|-----------|
| 纹理 | 待统计 | 实际格式、分辨率、Mip 与重复依赖 |
| 模型与动画 | 待统计 | 导入数据、动画压缩与必要表现 |
| 音频 | 待统计 | 编码、质量、采样率与语言交付 |
| 代码（IL2CPP 产出） | 待统计 | Managed Stripping 与实际保留需求 |
| Shader 变体 | 待统计 | 候选、裁剪与实际交付 |
| 配置数据 | 待统计 | 重复、表示与交付范围 |
| 引擎运行时 | 待统计 | 实际模块与 Engine Stripping 条件 |

**贡献度报告**应该在每次构建后自动生成，按模块和资源类型分列。CI 中设置包体预算告警——超过阈值时构建标红。

### 分析：为什么大

贡献度告诉你"哪里大"，还需要进一步分析"为什么大"：

**纹理大**的常见原因：
- 目标格式与预期不符，例如原计划使用支持设备上的 GPU 块压缩，实际产物却未采用；格式字节不能直接代表下载包压缩比
- 分辨率高于目标镜头和显示条件需要的范围
- 保留了当前使用不需要的 Mip；是否需要应结合实际缩放、投影与过滤判断，不能只凭“UI”分类决定
- `Read/Write Enabled` 增加了运行时 CPU 可寻址副本；它是内存检查项，不能据此断言包体也变成双份。见 [TextureImporter.isReadable](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/TextureImporter-isReadable.html)

**代码大**的常见原因：
- Managed Stripping 级别太低（保留了大量未使用的 BCL 类型）
- 泛型实例化爆炸（IL2CPP 为每种泛型参数组合生成独立代码）
- 第三方 SDK 引入了大量不需要的代码

**Shader 变体大**的常见原因：
- multi_compile 关键字组合爆炸
- 未使用的变体没有被裁剪
- 把关键字作用范围与候选组合数混在一起，未按实际构建报告定位增长

### 优化：按类别处理

**纹理优化**：
- 按目标能力和纹理用途选择格式，检查实际产物与回退；具体见[多端资源差异]({{< relref "delivery-engineering/delivery-content-pipeline-06-multiplatform-assets.md" >}})
- 根据目标镜头与画面接受条件约束最大分辨率
- 按 UI 的实际显示与缩放条件比较是否需要 Mip，再检查画质和占用
- 关闭不必要的 Read/Write Enabled
- 评估 Texture Atlas 的合批条件、边距与共用范围，不把图片数量减少直接当成包体收益；机制见[存储基础]({{< relref "rendering/unity-rendering-supp-a-mesh-texture-storage.md" >}})

**代码裁剪**：
- 提高 Managed Stripping 级别（配合 link.xml 保护反射和热更新需要的类型）
- Engine Code Stripping 裁剪未使用的引擎模块
- 审查第三方 SDK 的代码体积，移除不需要的功能模块

**Shader 变体裁剪**：
- 用构建配置及 `IPreprocessShaders` 等入口处理不需要的候选；`ShaderVariantCollection` 用于记录组合并参与保留/预热流程，不是等效的通用裁剪器
- 减少不必要的候选组合，按 Shader、pass、平台和构建阶段检查数量
- 仅在使用需求适合时选择 `shader_feature`，动态切换与热更所需组合仍需保留规则和目标 Player 验证，不能统一替换 `multi_compile`

集合、构建处理与预热的区别见 [ShaderVariantCollection](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ShaderVariantCollection.html)及[裁剪层次]({{< relref "rendering/unity-shader-variant-stripping-stages-prefiltering-builtin-custom.md" >}})。本篇没有实际裁剪回执或运行命中结果。

**音频优化**：
- 降低非关键音效的采样率和质量
- 比较长音频是否适合流式加载；这是运行驻留与播放策略，不会单独让同一编码文件的包体变小
- 移除未使用的音频文件

**资源外移**：
- 将非首次体验必需的资源移到追加下载层
- 将多语言语音包按语言拆分，按需下载
- 将高清过场动画移到 CDN 按需流式播放

### 监控：防止反弹

包体优化不是一次性的——如果不持续监控，每次版本迭代都会让包体重新增长。

**CI 监控**：
- 每次构建后输出包体报告（总大小 + 按模块分列）
- 设置包体预算阈值（警告线和阻止线）
- 包体增长超过阈值时自动通知责任人

**版本趋势**：
- 记录每个版本的包体大小，绘制趋势曲线
- 每个版本的包体变化应该能追溯到具体的变更（"这个版本比上个版本大了 15MB，原因是新增了 3 个角色"）

**贡献度追踪**：
- 每个模块 / 每个团队的包体贡献度单独统计
- 新增资源的包体影响在 Code Review 时展示

## 常见错误做法

**等到提审被拒才开始优化**。平台审核的包体限制是硬性约束。等到提审被拒再紧急优化，通常只能粗暴地降低所有贴图质量——牺牲了不该牺牲的视觉质量。应该从项目开始就有预算和监控。

**只优化最大的资源**。"最大的贴图"可能是合理的高清角色贴图，不该砍。真正该优化的是那些"不大但数量多"的资源——比如 500 个各 200KB 的未压缩小图标，总共 100MB。

**优化后不验证视觉质量**。压缩和裁剪可能影响视觉效果。每次包体优化后必须做视觉回归——至少在目标设备上截图对比优化前后的画面。

## 小结与检查清单

- [ ] 是否有每次构建后的包体贡献度报告
- [ ] CI 是否有包体预算告警（警告线 + 阻止线）
- [ ] 纹理格式是否被目标设备支持，并经过对应用途的质量与产物检查
- [ ] Managed Stripping 级别是否已优化（配合 link.xml）
- [ ] Shader 变体是否有裁剪与动态需求保留机制，并保留构建和运行验证记录
- [ ] 非首次体验必需的资源是否已移到追加下载层
- [ ] 包体大小是否有版本趋势追踪
- [ ] 包体优化后是否做了视觉回归验证

---

**下一步应读**：[Android 分发：AAB / PAD / 多渠道]({{< relref "delivery-engineering/delivery-package-distribution-03-android.md" >}}) — Android 平台的包体分发机制

**扩展阅读**：
- [包体大小监控]({{< relref "code-quality/package-size-monitoring-bundle-alerts-and-contribution-tracking.md" >}}) — CI 中的包体监控和贡献度追踪实践
- [包体大小优化]({{< relref "code-quality/package-size-optimization-stripping-split-packages-and-asset-trimming.md" >}}) — IL2CPP Stripping、Split APK/AAB、资产精简策略
