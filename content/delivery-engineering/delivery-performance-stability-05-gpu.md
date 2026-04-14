---
title: "性能与稳定性工程 05｜GPU 性能工程——Draw Call、带宽、Shader 与移动端"
slug: "delivery-performance-stability-05-gpu"
date: "2026-04-14"
description: "GPU 性能工程的交付视角：Draw Call 怎么控、带宽怎么省、Shader 变体怎么管、移动端 TBDR 架构要注意什么。"
tags:
  - "Delivery Engineering"
  - "Performance"
  - "GPU"
  - "Shader"
series: "性能与稳定性工程"
primary_series: "delivery-performance-stability"
series_role: "article"
series_order: 50
weight: 1350
delivery_layer: "practice"
delivery_volume: "V14"
delivery_reading_lines:
  - "L1"
  - "L2"
---

## 这篇解决什么问题

V14-04 讲了 CPU 维度的性能工程。GPU 是帧时间的另一半——渲染管线的瓶颈通常在 GPU 端。这一篇从交付工程视角讲 GPU 性能：**哪些指标需要预算、哪些退化需要在 CI 中拦截、哪些问题是移动端独有的。**

## GPU 帧时间的构成

GPU 一帧的工作大致分为：

```
顶点处理（Vertex Shader）
    ↓
光栅化
    ↓
片元处理（Fragment Shader）
    ↓
混合与后处理
    ↓
帧缓冲输出
```

瓶颈通常集中在三个地方：**Draw Call 数量（CPU 提交侧）、显存带宽（数据搬运）、片元着色器复杂度。**

## Draw Call 与合批策略

### Draw Call 预算

| 设备档位 | Draw Call 预算 | 说明 |
|---------|---------------|------|
| 高档 | ≤500 | Vulkan/Metal 开销低 |
| 中档 | ≤300 | OpenGL ES 开销较高 |
| 低档 | ≤150 | CPU 提交能力有限 |

### 合批策略对比

| 策略 | 原理 | 适用场景 | 限制 |
|------|------|---------|------|
| SRP Batcher | 按 Shader 变体缓存材质属性 | URP/HDRP 项目 | 要求 Shader 使用 CBUFFER |
| GPU Instancing | 同 Mesh + 同 Material 一次提交 | 植被、道具、小物件 | 动态合批的上限 |
| Static Batching | 构建时合并静态物体 Mesh | 不动的场景物件 | 增加内存（合并后的 Mesh） |
| Dynamic Batching | 运行时合并小 Mesh | 小物体（顶点 < 300） | 有 CPU 开销，收益递减 |

**交付建议**：

- URP 项目优先依赖 SRP Batcher，它的合批效率最高且不需要额外 CPU 开销
- GPU Instancing 用于大量重复物体（树、草、石头）
- Static Batching 看情况——它换内存换 Draw Call，包体敏感的项目慎用
- Dynamic Batching 在 URP 下通常不需要开启（SRP Batcher 已覆盖）

## 显存带宽优化

移动端 GPU 的最大瓶颈通常不是算力，而是**带宽**——GPU 和显存之间的数据搬运速度。

### 带宽消耗来源

| 来源 | 说明 | 优化方向 |
|------|------|---------|
| 纹理采样 | 每个像素采多张纹理 | 压缩格式、Mipmap、分辨率控制 |
| Render Target 读写 | 后处理每个 Pass 都读写一次 | 减少 Pass 数、降低 RT 分辨率 |
| 帧缓冲读取 | 透明物体需要读取已有帧缓冲 | 减少透明物体、避免 GrabPass |
| Overdraw | 同一像素被多次着色 | 前后排序、遮挡剔除 |

### 纹理压缩格式选择

纹理压缩格式直接影响显存带宽和包体大小（与 V02-06 多平台资源管线呼应）：

| 平台 | 推荐格式 | 压缩率 | 说明 |
|------|---------|--------|------|
| iOS | ASTC 6x6 / 4x4 | 3.56 / 8 bpp | A8+ 全支持 |
| Android | ASTC 6x6（优先）/ ETC2 | 3.56 / 4 bpp | ASTC 覆盖率已 > 90% |
| WebGL | ETC2 / DXT | 4 bpp | 看目标浏览器支持 |

## Shader 优化

### 精度

| 精度 | 类型 | 适用 |
|------|------|------|
| float (32-bit) | 高精度 | 世界空间坐标、深度计算 |
| half (16-bit) | 中精度 | 颜色、UV、光照计算 |
| fixed (11-bit) | 低精度 | 简单颜色混合（部分平台已等同 half） |

**原则**：默认用 half，只在需要高精度的地方用 float。移动端 half 的性能优势显著。

### 分支

```hlsl
// 差——动态分支导致线程分歧
if (useEffect) { ... } else { ... }

// 好——用 multi_compile 在编译期分离
#pragma multi_compile _ _USE_EFFECT
```

但 multi_compile 会增加 Shader 变体数量——这是一个包体大小 vs GPU 性能的权衡。

### Shader 变体管理

Shader 变体数量是一个交付指标：

| 指标 | 建议阈值 | 说明 |
|------|---------|------|
| 单个 Shader 变体数 | ≤128 | 超过说明 keyword 组合爆炸 |
| 项目总变体数 | ≤10,000 | 影响构建时间和包体 |
| 实际使用变体数 | 通过 ShaderVariantCollection 收集 | 与总数差距大说明有浪费 |

**CI 门禁**：每次构建后解析 Shader 编译日志，变体总数超阈值 → 标记告警。V16 CI/CD 将覆盖具体集成方法。

## 移动端 GPU 特殊考量

### TBDR 架构

移动端 GPU（Adreno、Mali、Apple GPU）使用 Tile-Based Deferred Rendering 架构：

```
屏幕被划分为 16x16 或 32x32 的 Tile
    ↓
每个 Tile 在 Tile Memory（片上缓存）中完成渲染
    ↓
最终结果写回主显存
```

**TBDR 的优势**：Tile Memory 读写极快（不走显存带宽）。

**TBDR 的禁忌**：

| 操作 | 问题 | 说明 |
|------|------|------|
| 全屏 Blit | 强制把 Tile Memory 写回显存再读回来 | 后处理每个 Pass 都触发 |
| MSAA Resolve | 额外的 Resolve Pass | URP 中用 Native Render Pass 避免 |
| GrabPass | 读取当前帧缓冲 | 极其昂贵，避免使用 |
| 过多 Render Target | 每个 RT 都占 Tile Memory | 减少 MRT 数量 |

### 热降频

移动设备长时间高 GPU 负载会触发热降频——芯片温度过高时系统降低 GPU 频率。

- 表现：前 5 分钟帧率稳定，之后逐渐下降
- 度量：长时间跑测（15-30 分钟）采集帧率曲线
- 治理：GPU 负载不追求跑满，留 20-30% 余量给散热

## GPU 性能作为 CI 门禁

| 门禁项 | 数据来源 | 拦截条件 |
|--------|---------|---------|
| Draw Call 回归 | Frame Debugger / 自动化抓帧 | 主场景 Draw Call 增长 > 15% |
| Shader 变体增长 | 构建日志解析 | 变体总数增长 > 阈值 |
| 纹理格式合规 | 资源扫描 | 非目标压缩格式 → 标记告警 |
| Overdraw 回归 | 自动化 Overdraw 抓帧 | Overdraw 率退化 > 20% |

## 小结与检查清单

- [ ] 是否有 Draw Call 预算并按设备档位区分
- [ ] 是否明确了合批策略（SRP Batcher / GPU Instancing / Static Batching）
- [ ] 纹理压缩格式是否按平台正确设置（ASTC 优先）
- [ ] Shader 是否优先使用 half 精度
- [ ] Shader 变体数量是否有阈值管控
- [ ] 是否了解 TBDR 架构的禁忌操作（全屏 Blit、GrabPass）
- [ ] 性能跑测是否包含长时间测试（检测热降频）
- [ ] CI 是否有 Draw Call 和 Shader 变体的回归检测

---

**下一步应读**：[内存与包体治理]({{< relref "delivery-engineering/delivery-performance-stability-06-memory.md" >}}) — GPU 之后看内存：预算分配、OOM 防护和资源瘦身

**扩展阅读**：GPU 优化系列（7 篇）— Draw Call 分析、Shader 优化、TBDR 架构的完整技术深挖
