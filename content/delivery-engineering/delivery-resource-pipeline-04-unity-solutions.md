---
title: "资源管线 04｜Unity 实践：AssetBundle vs Addressables vs YooAsset"
slug: "delivery-resource-pipeline-04-unity-solutions"
date: "2026-04-14"
description: "三套 Unity 资源管理方案各自的定位、架构差异和选型依据。不是'哪个最好'，而是'你的项目约束适合哪个'。"
tags:
  - "Delivery Engineering"
  - "Resource Pipeline"
  - "Unity"
  - "AssetBundle"
  - "Addressables"
  - "YooAsset"
series: "资源管线"
primary_series: "delivery-resource-pipeline"
series_role: "article"
series_order: 40
weight: 440
delivery_layer: "practice"
delivery_volume: "V05"
delivery_reading_lines:
  - "L2"
---

## 这篇解决什么问题

前三篇从原理层讲了资源管线的四个核心问题和打包策略的决策框架。这一篇落地到 Unity：三套主流方案各自怎么回答这四个问题，选型时该看什么。

这是一篇 Unity 实践层文章。如果你使用其他引擎，可以跳过本篇。

## 三套方案的定位

| 方案 | 定位 | 维护者 |
|------|------|--------|
| **AssetBundle（原生）** | Unity 底层资源打包 API | Unity 官方 |
| **Addressables** | 在 AB 之上的高层框架，提供地址化加载和自动依赖管理 | Unity 官方 |
| **YooAsset** | 社区开源的资源管理框架，定位类似 Addressables 但 API 设计不同 | 社区 |

关键认知：**Addressables 和 YooAsset 的底层都是 AssetBundle**。它们不是替代方案，而是在 AB 之上提供不同的管理抽象。

## 对比：四个核心问题怎么回答

### 打包

| 维度 | AssetBundle | Addressables | YooAsset |
|------|------------|-------------|---------|
| 分组方式 | 手动指定 AB Name | Group 可视化配置 | Package 配置 |
| 依赖管理 | 手动管理 Manifest | 自动依赖分析和打包 | 自动依赖分析 |
| 冗余控制 | 手动检查 | 自动检测 + 可视化 | 自动检测 |
| 构建管线 | BuildPipeline API | SBP (Scriptable Build Pipeline) | 自定义构建管线 |
| 增量构建 | 有限支持 | SBP 支持 | 支持 |

**判断**：原生 AB 的打包需要大量手动工作（指定 AB Name、管理依赖、检查冗余）。Addressables 和 YooAsset 都在这一层做了自动化。如果你从零开始，不建议直接用原生 AB API。

### 加载

| 维度 | AssetBundle | Addressables | YooAsset |
|------|------------|-------------|---------|
| 加载接口 | AB.LoadAsset / AB.LoadAssetAsync | Addressables.LoadAssetAsync<T> | YooAsset.LoadAssetAsync<T> |
| 地址化 | 无（用 AB 名 + 资源路径） | 有（用地址字符串或 AssetReference） | 有（用 Location 或路径） |
| 场景加载 | 手动管理 | 集成支持 | 集成支持 |
| 同步加载 | 支持 | 不原生支持（需扩展） | 支持 |

**判断**：原生 AB 的加载接口需要先加载 Bundle 再加载 Asset，两步操作。Addressables 和 YooAsset 封装为一步调用，并自动处理依赖 Bundle 的加载。

### 版本管理

| 维度 | AssetBundle | Addressables | YooAsset |
|------|------------|-------------|---------|
| 版本清单 | .manifest 文件（哈希比对） | Catalog（JSON，含哈希和依赖） | Manifest（二进制，含版本和依赖） |
| 增量更新 | 手动实现 | 内置 Content Update workflow | 内置补丁包机制 |
| 远程加载 | 手动实现 URL 拼接 | 内置 Remote Catalog + CDN | 内置远程资源模式 |
| 回退机制 | 手动实现 | 需扩展 | 内置缓存回退 |

**判断**：版本管理是三套方案差异最大的地方。原生 AB 几乎没有内置的版本管理，需要完全自建。Addressables 有 Content Update 但流程复杂。YooAsset 的补丁包机制相对直观。

### 生命周期

| 维度 | AssetBundle | Addressables | YooAsset |
|------|------------|-------------|---------|
| 引用计数 | 手动管理 | 自动引用计数 | 自动引用计数 |
| 卸载 | AB.Unload(true/false) | Addressables.Release | YooAsset.UnloadAsset |
| 泄漏检测 | 无（需自建） | 事件回调可监控 | 内置泄漏检测 |
| 内存报告 | 无 | Profiler 集成 | 内置统计 |

**判断**：原生 AB 的 Unload(true) vs Unload(false) 是臭名昭著的陷阱——选错会导致资源丢失或内存泄漏。Addressables 和 YooAsset 用自动引用计数封装了这个问题。

## 选型决策框架

不是"哪个最好"，而是"你的项目约束适合哪个"。

### 选原生 AssetBundle 的场景

- 已有成熟的自建资源管理框架，只需要底层打包能力
- 对资源管线有完全控制的需求（自研引擎移植、特殊的打包策略）
- 团队有丰富的 AB 经验和维护能力

### 选 Addressables 的场景

- Unity 官方方案，长期支持有保障
- 需要和 Unity 其他系统（Scenes、Prefabs、ScriptableObjects）深度集成
- 团队能接受 Addressables 的学习曲线和调试复杂度

### 选 YooAsset 的场景

- 需要比 Addressables 更简洁的 API
- 需要更好的同步加载支持
- 对中文文档和社区支持有需求
- 需要更灵活的补丁包机制

### 不推荐的做法

**同一个项目混用两套框架**。例如 Addressables 管理场景资源、自建系统管理 UI 资源。两套引用计数系统无法互通，生命周期管理会出问题。

**选好了又换**。资源管线的框架选型应该在项目早期决定，中期更换的迁移成本极高（所有加载代码、所有打包配置、所有热更新逻辑都要改）。

## 与交付链路的关系

无论选哪套方案，它们在交付链路中的位置是相同的：

```
V02 资源生产 → V05 资源管线（打包/加载/版本/生命周期）→ V06 包体管理 → V07 多端构建
                    ↑ 这一层是 AB / Addressables / YooAsset 的覆盖范围
```

选型决策影响 V06（包体管理）和 V08（热更新）的工程方案，因为热更新的资源包格式和版本管理逻辑与资源管线框架强绑定。

## 小结与检查清单

- [ ] 是否明确了资源管线框架的选型及理由
- [ ] 是否在项目早期完成了选型（而非中期更换）
- [ ] 是否只使用一套框架管理所有资源（不混用）
- [ ] 引用计数和生命周期管理是否有泄漏检测机制
- [ ] 版本管理和热更新流程是否经过端到端验证
- [ ] 框架的构建管线是否集成到 CI

---

**下一步应读**：[资源加载与生命周期]({{< relref "delivery-engineering/delivery-resource-pipeline-05-loading-lifecycle.md" >}}) — 加载时机、引用计数和内存管理的工程设计

**扩展阅读**：
- [Addressables 系列]({{< relref "engine-toolchain/" >}}) — Addressables 的格式、构建时、Catalog、加载、生命周期完整深挖
- [YooAsset 系列]({{< relref "engine-toolchain/" >}}) — YooAsset 的架构、打包和加载流程深挖
