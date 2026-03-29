---
date: "2026-03-21"
title: "Engine Notes"
description: "把 Unity / Unreal、资源、构建、脚本、运行时和渲染放回同一条系统链路里理解。"
hero_title: "先看清系统链路，再判断问题到底站在哪一层。"
---

这一栏不是 API 摘录，而是把引擎、工具链、运行时和渲染放回同一条系统链路里理解。

我更关注这些问题：

- 资源、构建、脚本和运行时是怎样互相影响的
- 渲染、性能和工程边界为什么总会在同一个问题里相遇
- 哪些问题该按系统分层去拆，哪些问题该回到工具链和交付链路判断

如果你更想按角色或目标进入，可以先看：

- [客户端程序阅读入口｜先按问题类型，再按系统层级进入]({{< relref "engine-notes/client-programmer-reading-entry.md" >}})
- [引擎开发阅读入口｜先立分层地图，再按图形、运行时和平台抽象进入]({{< relref "engine-notes/engine-programmer-reading-entry.md" >}})
- [美术资源提交前自检表｜贴图、材质、LOD、UI 和特效该先看什么]({{< relref "engine-notes/artist-resource-self-check-before-submit.md" >}})
- [TA 不是“调效果的人”｜职责边界、协作接口和典型产出物]({{< relref "engine-notes/ta-role-boundaries-and-deliverables.md" >}})
