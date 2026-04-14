---
title: "包体管理与分发 02｜包体大小控制——压缩、裁剪与贡献度追踪"
slug: "delivery-package-distribution-02-size-control"
date: "2026-04-14"
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

## 本质是什么

包体控制的核心流程：

```
度量（知道哪里大） → 分析（知道为什么大） → 优化（有针对性地减小） → 监控（防止反弹）
```

不度量就优化 = 盲目砍资源。不监控就发版 = 下次还会超。

### 度量：包体贡献度分析

第一步是知道包体里每个部分占了多少：

| 类别 | 典型占比 | 可优化空间 |
|------|---------|-----------|
| 纹理 | 40-60% | 大（压缩格式、分辨率降级） |
| 模型与动画 | 10-20% | 中（面数限制、动画压缩） |
| 音频 | 5-15% | 中（压缩质量、采样率） |
| 代码（IL2CPP 产出） | 5-15% | 中（Managed Stripping） |
| Shader 变体 | 5-10% | 大（变体裁剪） |
| 配置数据 | 1-5% | 小 |
| 引擎运行时 | 3-8% | 小（Engine Stripping） |

**贡献度报告**应该在每次构建后自动生成，按模块和资源类型分列。CI 中设置包体预算告警——超过阈值时构建标红。

### 分析：为什么大

贡献度告诉你"哪里大"，还需要进一步分析"为什么大"：

**纹理大**的常见原因：
- 压缩格式不对（RGBA32 未压缩 vs ASTC 6x6 压缩比约 10:1）
- 分辨率超规范（该用 512 的用了 2048）
- Mipmap 不必要地开启（UI 贴图不需要 Mipmap）
- Read/Write Enabled 导致内存和包体双份

**代码大**的常见原因：
- Managed Stripping 级别太低（保留了大量未使用的 BCL 类型）
- 泛型实例化爆炸（IL2CPP 为每种泛型参数组合生成独立代码）
- 第三方 SDK 引入了大量不需要的代码

**Shader 变体大**的常见原因：
- multi_compile 关键字组合爆炸
- 未使用的变体没有被裁剪
- 全局关键字和局部关键字没有区分

### 优化：按类别处理

**纹理优化**：
- 统一压缩格式（V02-06 已覆盖多端纹理格式选择）
- 按规范限制最大分辨率
- UI 贴图关闭 Mipmap
- 关闭不必要的 Read/Write Enabled
- 使用 Texture Atlas 合并小贴图

**代码裁剪**：
- 提高 Managed Stripping 级别（配合 link.xml 保护反射和热更新需要的类型）
- Engine Code Stripping 裁剪未使用的引擎模块
- 审查第三方 SDK 的代码体积，移除不需要的功能模块

**Shader 变体裁剪**：
- 使用 IPreprocessShaders 或 ShaderVariantCollection 裁剪未使用的变体
- 减少 multi_compile 关键字数量
- 用 shader_feature 替代 multi_compile（shader_feature 只编译使用到的变体）

**音频优化**：
- 降低非关键音效的采样率和质量
- 长音频使用流式加载而非全部加载到内存
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
- [ ] 纹理是否统一使用了平台最优的压缩格式
- [ ] Managed Stripping 级别是否已优化（配合 link.xml）
- [ ] Shader 变体是否有裁剪机制
- [ ] 非首次体验必需的资源是否已移到追加下载层
- [ ] 包体大小是否有版本趋势追踪
- [ ] 包体优化后是否做了视觉回归验证

---

**下一步应读**：[Android 分发：AAB / PAD / 多渠道]({{< relref "delivery-engineering/delivery-package-distribution-03-android.md" >}}) — Android 平台的包体分发机制

**扩展阅读**：
- [包体大小监控]({{< relref "code-quality/package-size-monitoring-bundle-alerts-and-contribution-tracking.md" >}}) — CI 中的包体监控和贡献度追踪实践
- [包体大小优化]({{< relref "code-quality/package-size-optimization-stripping-split-packages-and-asset-trimming.md" >}}) — IL2CPP Stripping、Split APK/AAB、资产精简策略
