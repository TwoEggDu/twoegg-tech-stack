---
date: "2026-04-12"
title: "Unreal World Partition：新一代大世界管理，HLOD 自动生成"
description: "把 Unreal 的 World Partition 从'大世界方案'还原成自动分块、Actor 级 Streaming 和 HLOD 生成的工程结构，讲清它和传统 Level Streaming 的本质区别，以及它解决了哪些手动工作、引入了哪些新约束。"
slug: "unreal-world-partition-hlod-streaming"
weight: 74
featured: false
tags:
  - "Unreal"
  - "World Partition"
  - "HLOD"
  - "Streaming"
  - "Open World"
series: "Unity 资产系统与序列化"
---
上一篇讲了 Level Streaming 的基本思路：把大世界拆成场景块，按距离触发加载和卸载。这套方案能工作，但有大量手动工作——场景怎么拆、触发器怎么放、跨场景引用怎么处理，全都需要人来维护。

Unreal Engine 5 引入的 World Partition 用一套更自动化的方式解决了这些问题。这篇不是 Unreal 教程，而是从架构层面讲清楚它的设计思路，以及它和传统 Level Streaming 的本质区别。

## 先给一句总判断

`World Partition 的核心设计思路是：把 Level Streaming 从"手动拆场景、手动设触发器"变成"编辑器自动按网格分块、运行时按 Actor 级别决定加载和卸载"。配合 HLOD 自动生成，远处的内容用低精度替代物表示，近处的内容按需加载完整版本。设计者只管在一个连续的大世界里摆放内容，引擎负责拆分和调度。`

## 一、传统 Level Streaming 的痛点

在讲 World Partition 之前，先明确它要解决什么问题。

### 1. 手动拆分场景的维护成本

传统 Level Streaming 需要开发者手动决定世界怎么拆成多个 Sub-Level。这在早期还行，但随着世界规模增大、团队人数增多，Sub-Level 的拆分方案本身就变成了一个需要持续维护的工程问题。

### 2. 跨 Level 的协作冲突

多人同时编辑同一个 Sub-Level 会产生合并冲突。如果为了避免冲突把 Level 拆得很碎，管理成本又上去了。

### 3. Streaming 粒度受限于 Level

传统方案的 Streaming 粒度是 Sub-Level 级别的——要么整个 Level 加载，要么整个 Level 卸载。如果一个 Level 里有些 Actor 应该早加载、有些应该晚加载，传统方案很难表达。

### 4. LOD 和 Streaming 是两套系统

传统的 LOD 系统和 Level Streaming 各管各的。远处的物体用低 LOD 渲染，但它们的完整数据可能还在内存里。要想远处只加载低精度版本、近处才加载完整版本，需要手动搭建 HLOD。

## 二、World Partition 的核心设计

### 1. 一个 World，不再拆 Level

在 World Partition 模式下，整个大世界是一个 Level。设计者在一个连续的编辑器环境里工作，不需要手动拆分 Sub-Level。

但这不意味着运行时会一次性加载整个世界。World Partition 在后台自动按空间网格把世界划分成 Cell，每个 Cell 覆盖一块区域。这个划分对设计者是透明的——你只看到一个连续的世界，引擎在底层按 Cell 管理加载和卸载。

### 2. Actor 级别的 Streaming

传统 Level Streaming 的粒度是 Level，World Partition 的粒度下沉到了 Actor。

每个 Actor 都有 Streaming 相关的属性：

- 所属的 Streaming Cell（由空间位置自动决定）
- 是否参与 Streaming（有些 Actor 可以标记为 Always Loaded）
- Streaming 优先级

运行时，引擎根据玩家位置，按 Cell 为单位决定哪些 Actor 需要加载、哪些需要卸载。同一个 Cell 内的 Actor 一起加载，不同 Cell 独立调度。

### 3. Data Layers

World Partition 引入了 Data Layer 概念，允许在同一个世界空间内叠加不同的内容层。比如：

- 基础地形层：永远加载
- 白天 NPC 层：白天加载
- 夜晚 NPC 层：夜晚加载
- 任务触发层：特定任务进行时加载

Data Layer 给 Streaming 增加了逻辑维度，不仅仅是空间距离决定加载，还可以由游戏逻辑决定。

### 4. One File Per Actor (OFPA)

为了解决多人协作冲突，World Partition 默认使用 One File Per Actor 的存储方式：每个 Actor 存储在独立的文件中，而不是所有 Actor 混在一个 Level 文件里。

这意味着：

- 两个人同时编辑不同的 Actor 不会产生文件冲突
- 版本控制系统（Perforce / Git）可以按 Actor 粒度锁定
- 只修改一个 Actor 时，只需要提交一个小文件，不需要提交整个 Level

## 三、HLOD：Hierarchical Level of Detail

HLOD 是 World Partition 的配套系统，解决的问题是：远处的内容不需要加载完整的 Actor，只需要一个低精度的替代表示。

### 1. HLOD 的基本思路

把一个 Cell 内的所有静态 Mesh 合并成一个（或少量几个）简化的 Mesh，作为该 Cell 的远景替代物。当玩家距离远时，只渲染 HLOD Mesh；当玩家接近时，卸载 HLOD Mesh，加载完整的 Actor。

### 2. 自动生成

Unreal 的 HLOD 系统可以自动生成这些替代 Mesh：

- 把 Cell 内的静态 Actor 收集起来
- 合并和简化 Mesh（减面、合并材质、降低贴图分辨率）
- 生成 HLOD Actor，与原始 Actor 的 Cell 关联

这个过程在编辑器中执行（Build HLOD），不需要美术手动制作低模。

### 3. 多级 HLOD

HLOD 可以是多级的：

- Level 0：完整 Actor（近处）
- Level 1：Cell 级简化 Mesh（中距离）
- Level 2：多个 Cell 合并的更粗糙 Mesh（远距离）

每一级覆盖更大的区域，使用更粗糙的表示。这样即使世界非常大，远处也有内容可以渲染，只是精度更低。

### 4. HLOD 和 Streaming 的配合

HLOD 的关键价值不仅是视觉 LOD，更是 Streaming 减负：

- 远处的 Cell 不需要加载完整的 Actor 数据，只加载 HLOD Mesh
- HLOD Mesh 通常比完整 Actor 小一到两个数量级
- 这大幅降低了同时加载的数据量，让更大的世界成为可能

## 四、和 Unity 方案的对比

| 维度 | Unity Level Streaming | Unreal World Partition |
|------|----------------------|----------------------|
| 编辑方式 | 手动拆分 Sub-Level | 一个连续世界，自动分 Cell |
| Streaming 粒度 | Level 级别 | Actor 级别（按 Cell 组织） |
| 多人协作 | 同一 Level 内冲突 | One File Per Actor，粒度细 |
| LOD 集成 | LOD 和 Streaming 分离 | HLOD 和 Streaming 一体 |
| 触发方式 | 手动设距离/触发器 | 引擎自动按距离调度 |
| 逻辑层控制 | 需要自己实现 | Data Layer 内置支持 |
| 适用场景 | 中小规模开放世界 | 大规模开放世界 |
| 入门复杂度 | 概念简单但手动工作多 | 概念多但自动化程度高 |

## 五、World Partition 引入的新约束

World Partition 解决了很多问题，但也引入了一些约束：

### 1. 只适用于静态场景结构

World Partition 的自动分 Cell 和 HLOD 主要针对静态放置的 Actor。运行时动态生成的对象不受 World Partition 管理，需要自己处理生命周期。

### 2. 构建时间

HLOD 的生成需要构建时间。世界越大，HLOD Build 越慢。每次修改场景后可能需要重新构建 HLOD。

### 3. 动态光照和 Streaming 的矛盾

如果使用烘焙光照，每个 Cell 的 Lightmap 需要独立烘焙并在 Streaming 时加载。跨 Cell 的光照一致性是一个挑战。Lumen 等实时 GI 方案可以缓解这个问题，但性能开销更高。

### 4. 对项目结构的要求

启用 World Partition 后，整个关卡的组织方式都要围绕它来设计。从传统 Level Streaming 迁移到 World Partition 不是改个设置就行，而是需要重构关卡的组织方式。

## 六、什么时候该用 World Partition

不是所有项目都需要 World Partition。判断标准：

- 如果你的世界足够小，一次性加载不成问题，不需要 Level Streaming，更不需要 World Partition
- 如果你的世界需要 Level Streaming，但规模可控（十几个 Sub-Level），传统方案的手动成本还可以接受
- 如果你的世界非常大（几十 km² 级别）、团队人数多、需要自动化的 Streaming 和 LOD，World Partition 的自动化能力才真正值得引入的复杂度

## 结语

World Partition 不是一个"更好的 Level Streaming"，而是一套重新设计的大世界管理架构。它把手动拆分场景的工作自动化了，把 Streaming 粒度从 Level 下沉到 Actor，把 LOD 和 Streaming 集成在一起。代价是更高的概念复杂度和构建成本。对于真正需要承载大规模开放世界的项目，这些代价是值得的。

到这里，系列八的打包、加载与流式系统全部写完了。从切包粒度到首包策略，从压缩权衡到异步加载管线，从内存控制到场景优化，再到 Level Streaming 和 World Partition——这条资源交付链的工程地图，现在完整了。
