---
title: "服务端架构与构建 03｜C# 服务端实践：ET Framework 的交付视角"
slug: "delivery-server-architecture-03-et-framework"
date: "2026-04-14"
description: "ET Framework 是 C# 游戏服务端的代表方案。从交付视角看它的包化架构、Actor 模型和热重载能力——不讲 API，讲工程选型和交付影响。"
tags:
  - "Delivery Engineering"
  - "Server"
  - "ET Framework"
  - "C#"
  - "Actor"
series: "服务端架构与构建"
primary_series: "delivery-server-architecture"
series_role: "article"
series_order: 30
weight: 930
delivery_layer: "practice"
delivery_volume: "V10"
delivery_reading_lines:
  - "L3"
---

## 这篇解决什么问题

ET Framework 是国内 Unity + C# 全栈开发中使用最广泛的服务端框架之一。这一篇从交付工程视角看 ET 的架构设计——不重复 API 用法，聚焦它对构建、部署和更新的影响。

## ET 的交付相关特征

### 包化架构

ET9 采用了包化（Package-based）架构——核心框架通过 NuGet 或 Git 子模块分发，项目只包含业务代码：

```
ET 项目结构（简化）：
├── Unity/          (客户端项目，引用 ET 客户端包)
├── Server/         (服务端项目)
│   ├── Hotfix/     (可热重载的业务代码)
│   ├── Model/      (数据模型，不可热重载)
│   └── Entry/      (启动入口)
├── Share/          (客户端和服务端共享的代码)
└── Proto/          (协议定义，客户端和服务端共享)
```

**交付影响**：
- 客户端和服务端共享协议定义（`Proto/`）和部分模型代码（`Share/`）——协议变更时两端必须同步更新
- 框架版本和业务代码分开管理——升级 ET 版本不需要修改业务代码（理想情况下）
- 服务端的 `Hotfix/` 和 `Model/` 分离对应了热重载的边界

### Actor 模型的交付含义

ET 基于 Actor 模型——每个游戏实体（玩家、场景、房间）是一个 Actor，通过消息异步通信。

从交付视角，Actor 模型带来了：

**可分布部署**：Actor 可以运行在不同的进程/服务器上。部署时可以按 Actor 类型分配到不同节点：
- 登录 Actor → 登录服务器
- 场景 Actor → 场景服务器
- 玩家 Actor → 玩家所在的场景服务器

**状态迁移难题**：更新时正在运行的 Actor 有内存中的状态。滚动更新时，旧节点上的 Actor 需要迁移到新节点——状态序列化和反序列化的兼容性是关键。

### 热重载能力

ET 的 `Hotfix/` Assembly 支持运行时热重载——不重启服务器就替换业务逻辑代码。

**热重载的边界**：
- `Hotfix/` 中的代码可以热重载（System、Handler 等业务逻辑）
- `Model/` 中的数据结构不能热重载（因为内存中的实例已经创建）
- 新增字段到 Model 需要重启服务器

**交付意义**：热重载让服务端的 Bug 修复可以做到秒级生效，不需要滚动更新。但新增数据结构仍然需要重启——这决定了哪些变更可以走热重载通道、哪些必须走完整部署。

## ET 的构建管线

```
Proto 编译（协议 → C# 代码）
    ↓
Share 编译（共享代码）
    ↓
Server 编译
├── Model.dll（数据模型）
├── Hotfix.dll（可热重载的业务逻辑）
└── Entry（启动入口）
    ↓
Docker 镜像打包（.NET Runtime + 产物）
    ↓
归档
```

**CI 集成要点**：
- Proto 编译必须在服务端和客户端构建之前（两端都依赖生成的协议代码）
- Share 代码变更需要同时触发客户端和服务端的 CI
- Hotfix.dll 可以独立构建和部署（热重载场景）

## 常见事故

**协议不同步**。客户端和服务端使用了不同版本的 Proto 文件。新客户端发送的消息格式服务端不认识，反序列化失败。

**预防**：Proto 文件在共享仓库中管理，CI 构建时先编译 Proto 再编译两端。Proto 变更必须同时触发两端构建。

## 小结

- [ ] Proto 编译是否在两端构建之前
- [ ] Share 代码变更是否同时触发客户端和服务端 CI
- [ ] Hotfix 热重载的边界是否团队清楚（哪些改动需要重启）
- [ ] Actor 状态迁移策略是否有设计

---

**下一步应读**：[Server ECS 选型]({{< relref "delivery-engineering/delivery-server-architecture-04-server-ecs.md" >}})

**扩展阅读**：[ET 框架源码解析系列]({{< relref "et-framework/" >}}) — ET9 的完整架构和源码分析
