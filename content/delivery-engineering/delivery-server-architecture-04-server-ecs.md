---
title: "服务端架构与构建 04｜Server ECS 选型——服务端为什么需要 ECS"
slug: "delivery-server-architecture-04-server-ecs"
date: "2026-04-14"
description: "Server ECS 不是客户端 ECS 的简单复用。服务端对 ECS 的需求来自高性能模拟和数据驱动的状态管理——但引入 ECS 也带来了 Schema 迁移和序列化的交付挑战。"
tags:
  - "Delivery Engineering"
  - "Server"
  - "ECS"
  - "Architecture"
series: "服务端架构与构建"
primary_series: "delivery-server-architecture"
series_role: "article"
series_order: 40
weight: 940
delivery_layer: "principle"
delivery_volume: "V10"
delivery_reading_lines:
  - "L1"
  - "L3"
---

## 这篇解决什么问题

ECS（Entity Component System）在客户端用于高性能渲染和物理。服务端引入 ECS 的动机不同——主要是大规模状态管理和高性能模拟（RTS 单位、弹幕系统、大世界 AI）。

从交付视角，Server ECS 的引入带来了特有的工程挑战。

## 服务端引入 ECS 的交付影响

### Component 即 Schema

ECS 中，数据存储在 Component 中。服务端的 Component 定义就是状态的 Schema：

```csharp
struct HealthComponent { public int CurrentHP; public int MaxHP; }
struct PositionComponent { public float X, Y, Z; }
```

**交付影响**：Component 结构变更 = Schema 迁移。新增、删除或修改 Component 的字段会影响：
- 内存中已有实体的数据布局
- 持久化到数据库的数据格式
- 客户端和服务端之间的状态同步格式

### System 可替换

ECS 中，逻辑在 System 中。System 是无状态的——它只读取和写入 Component 数据。

**交付影响**：System 的替换相对安全——替换一个 System 不影响数据布局。这意味着 System 级别的 Bug 修复可以通过热重载完成（类似 ET 的 Hotfix），而 Component 变更需要完整部署 + 数据迁移。

### 选型对比

| 方案 | 语言 | 特点 | 交付考量 |
|------|------|------|---------|
| **FLECS** | C | 高性能，功能丰富 | 原生代码，需要交叉编译 |
| **EnTT** | C++ | 轻量，头文件库 | C++ 编译时间长 |
| **Bevy ECS** | Rust | 内存安全，并行调度 | Rust 生态较新 |
| **C# ECS（自研）** | C# | 和业务代码同语言 | .NET 构建简单 |
| **Unity DOTS（服务端）** | C# | 利用 Unity Burst/Jobs | 需要 Unity 服务端 Runtime |

选型时从交付视角需要关注：
- 构建工具链是否成熟（编译、打包、CI 集成）
- 是否和业务代码同一语言（减少桥接层）
- Schema 变更时的数据迁移策略是否有框架支持

## Schema 迁移策略

Server ECS 最大的交付挑战是 Component Schema 变更：

**方案一：版本号 + 迁移函数**
```
ComponentV1: { HP: int }
ComponentV2: { CurrentHP: int, MaxHP: int }
迁移函数: V1→V2: { CurrentHP = HP, MaxHP = HP }
```
每次 Schema 变更写一个迁移函数，部署时按版本号依次执行。

**方案二：宽松序列化**
使用支持字段增减的序列化格式（Protobuf / MessagePack）。新增字段用默认值，删除字段忽略。

**方案三：双版本并行**
新旧两个版本的 Component 同时存在，逐步迁移实体。适合不能停机的大规模服务。

## 小结

- [ ] Server ECS 的 Component 变更是否有 Schema 迁移策略
- [ ] System 级别变更是否可以热重载
- [ ] ECS 框架选型是否考虑了构建工具链成熟度
- [ ] 状态序列化格式是否支持字段增减

---

**下一步应读**：[服务端编译与容器化]({{< relref "delivery-engineering/delivery-server-architecture-05-build-containerize.md" >}})

**扩展阅读**：[Server ECS 系列]({{< relref "system-design/" >}}) — FLECS、EnTT、Bevy、C# ECS 的完整对比和实现深挖
