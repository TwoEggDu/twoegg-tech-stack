# Unity 6 渲染管线升级实战系列规划

## 专栏定位

这组文章不是 Unity 6 渲染新功能的文档翻译，也不是 URP 配置手册的增量更新。

它真正要解决的问题是：

`从 Unity 2022 到 Unity 6，渲染管线发生了哪些结构性变化——GPU Resident Drawer 改变了什么调度模型，BIRP 废弃意味着什么，HDRP 停更后怎么选型——让读者在升级前知道代价，升级后知道怎么用好新能力。`

一句话说，这个专栏的重点不是"Unity 6 有哪些新参数"，而是：

`渲染管线从 2022 到 6.0 的结构性变化，以及这些变化对现有项目的实际影响。`

---

## 目标读者

- 正在用 Unity 2022 LTS 做项目，考虑是否升级到 Unity 6 的客户端主程
- 已经升级到 Unity 6，但还没理解 GPU Resident Drawer 调度模型的图形程序
- 负责渲染管线选型（URP / HDRP / 自定义 SRP）的技术决策者
- 配合阅读"URP 深度"系列、"Unity 渲染系统"系列、"Shader Variant 治理"系列的读者

---

## 专栏在整体内容地图里的位置

```
[Unity 渲染系统]（系列二）
  讲 Unity 里各类渲染资产和管线架构逻辑（基于 2022）
        ↓
[URP 深度]
  讲 URP 从前置概念到扩展开发到平台配置（基于 2022 URP 14.x）
        ↓
[Unity 6 渲染管线升级实战]          ← 本专栏
  讲 Unity 6 渲染管线的结构性变化，以及怎样从 2022 升级过来
        ↓
[Shader Variant 治理]
  讲 Shader Variant 在 Unity 6 下的变化（本专栏会指出 diff，不吞掉该系列）
```

本专栏是"URP 深度"的 Unity 6 增量层，上游是已有的 URP 架构理解，下游是 Shader Variant 治理的版本适配。

---

## 系列边界

### 属于这个系列的内容

- GPU Resident Drawer 的调度模型、与 SRP Batcher 的关系和差异
- BatchRendererGroup API 的自动化路径 vs 手动路径
- BIRP deprecated 后的迁移策略和工程成本
- HDRP 停更后的选型决策框架
- URP 14.x → 17.x 的 API 破坏性变更清单
- Deferred+ 渲染路径在 URP 中的实现和移动端适用性
- Unity 6 对 Shader Variant 治理流程的影响（diff 层面）
- 渲染管线统一路线图的技术可行性分析

### 不属于这个系列的内容

- URP 基础概念（CommandBuffer、RTHandle、渲染路径对比）→ 已在"URP 深度"系列
- Shader 手写技法 → 已有独立系列
- 完整的 Shader Variant 治理 → 已有独立系列
- 非渲染相关的 Unity 6 变化（Awaitable、Content Pipeline 等）→ 放在"运行时与工具链"系列

---

## 文章规划

### 第一组：核心架构变化（3 篇）

| 编号 | slug 方向 | 标题方向 | 核心问题 |
|------|-----------|----------|----------|
| U6R-01 | `unity6-rendering-01-gpu-resident-drawer` | GPU Resident Drawer 原理：从 SRP Batcher 到自动 Instancing | GPU Resident Drawer 改变了什么调度模型？BatchRendererGroup 的自动化路径是什么？与手动 GPU Instancing / SRP Batcher 的关系和替代关系；开启条件和硬件要求 |
| U6R-02 | `unity6-rendering-02-gpu-resident-drawer-vs-srp-batcher` | GPU Resident Drawer vs SRP Batcher：性能模型对比与切换时机 | 什么场景 GPU Resident Drawer 比 SRP Batcher 快，什么场景反而慢？LOD cross-fade 不支持的影响；实测数据和 Profiler 对比方法；项目切换的判断框架 |
| U6R-03 | `unity6-rendering-03-deferred-plus` | Deferred+ 在 URP 中的落地：移动端是否可用 | Forward / Deferred / Forward+ / Deferred+ 四条路径在 Unity 6 中的定位；Deferred+ 与 GPU Resident Drawer 的配合；移动端 TBDR 架构下的性能特征 |

### 第二组：管线选型与迁移（3 篇）

| 编号 | slug 方向 | 标题方向 | 核心问题 |
|------|-----------|----------|----------|
| U6R-04 | `unity6-rendering-04-birp-deprecated` | BIRP 正式 Deprecated：现有项目的迁移成本和策略 | 6.5 标记 deprecated 意味着什么（还能用多久）；BIRP → URP 的 Shader 改写清单；Material 转换工具的能力边界；迁移优先级排序框架 |
| U6R-05 | `unity6-rendering-05-hdrp-maintenance` | HDRP 停更后的选型决策：哪些项目还值得用 HDRP | HDRP 进入维护模式的含义（不加新功能 ≠ 立刻不能用）；HDRP 独有能力清单（体积云、SSR、area light 等）在 URP 中的替代进度；Switch 2 的 HDRP 适配；新项目是否还应该选 HDRP |
| U6R-06 | `unity6-rendering-06-urp-14-to-17-breaking` | 从 URP 14.x 到 17.x：API 破坏性变更与适配清单 | 升级 URP 包版本后哪些代码会编译失败；RenderGraph API 的强制迁移；RendererFeature 和自定义 Pass 的改写点；已有 URP 深度系列文章中哪些内容需要版本标注 |

### 第三组：前瞻与工程影响（2~3 篇）

| 编号 | slug 方向 | 标题方向 | 核心问题 |
|------|-----------|----------|----------|
| U6R-07 | `unity6-rendering-07-shader-variant-diff` | Unity 6 Shader Variant 治理变化：与 2022 的 Diff | shader_feature / multi_compile 行为是否有变；GPU Resident Drawer 要求 DOTS instancing keyword 的影响；Shader Variant Collection / warmup 流程的变化；与已有 Shader Variant 治理系列的接口 |
| U6R-08 | `unity6-rendering-08-pipeline-unification` | 渲染管线统一路线图解读：URP 吞并 HDRP 的技术可行性 | Unity 官方 2026 路线图解读；URP 要具备 HDRP 哪些能力才能"统一"；feature parity 的技术难度评估；对项目选型的短期和中期建议 |

---

## 与已有系列的关系

| 已有系列 | 本系列的关系 |
|----------|-------------|
| URP 深度（22 篇） | 本系列是 URP 深度的 Unity 6 增量层；URP 深度的 `urp-ext-06-migration` 是最直接的衔接点 |
| Unity 渲染系统（36 篇） | 本系列不重复渲染资产和管线架构的基础讲解，默认读者已有该系列的知识 |
| Shader Variant 治理（25+ 篇） | U6R-07 只讲 diff，不吞掉 Shader Variant 系列的完整体系 |
| Shader 手写技法（75 篇） | 无直接依赖，但 BIRP → URP 迁移涉及 Shader 改写时会交叉引用 |

---

## 推荐写作顺序

1. U6R-01（GPU Resident Drawer 原理）→ 技术基础，后续文章依赖
2. U6R-02（vs SRP Batcher）→ 紧接 01，完成性能判断框架
3. U6R-06（URP 14→17 breaking）→ 最实用，升级项目最先查
4. U6R-04（BIRP deprecated）→ 影响面最大的决策
5. U6R-07（Shader Variant diff）→ 与已有系列衔接
6. U6R-05（HDRP 停更）→ 选型决策
7. U6R-03（Deferred+）→ 前沿特性
8. U6R-08（管线统一路线图）→ 前瞻分析

---

## 当前状态

- 系列规划：✅ 完成
- 已完成文章：0/8
- 下一步：从 U6R-01 开始编辑定位

---

*创建日期：2026-04-13*
