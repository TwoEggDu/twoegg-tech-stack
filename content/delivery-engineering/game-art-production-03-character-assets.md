---
title: "03｜角色：从设计稿到模型与材质（提纲）"
slug: "game-art-production-03-character-assets"
date: "2026-10-25"
description: "章节提纲，正文待写：连接建模、拓扑、UV、烘焙与贴图，理解角色制作的主要判断及其对后续工作的影响。"
draft: false
featured: false
content_stage: "outline"
weight: 83
tags: ["美术生产", "客户端", "协作"]
series: "游戏美术生产与客户端协作"
primary_series: "game-art-production-collaboration"
series_order: 3
type: "reading-series"
reading_title: "角色：从设计稿到模型与材质"
reading_summary: "连接建模、拓扑、UV、烘焙与贴图，理解角色制作的主要判断及其对后续工作的影响。"
---

> **章节提纲 · 正文待写。** 本页列出后续正文的内容安排；拟制作的素材、示例与验证尚未完成。

## 本篇要回答的问题

一个角色怎样获得合适的外形、表面和后续可用性？

## 内容提纲

### 1. 先确认体块与剪影

从主视角确认比例、体块、剪影和重点；用初步模型检查设计是否在游戏镜头下成立。

### 2. 选择造型与拓扑流程

解释造型、高模、低模与重拓扑各自解决的问题；区分可采用高低模流程与直接制作低模的情形。

### 3. 让拓扑服务后续使用

拓扑怎样服务轮廓、关节形变和拆件；角色、服装、装备如何安排连接关系。

### 4. 组织 UV 与烘焙细节

UV、纹素密度与接缝；法线等烘焙结果怎样把表面细节转给运行时模型。

### 5. 制作贴图并预览材质

贴图、材质与 Shader 的分工；颜色、粗糙度等表面信息如何在约定光照和色彩设置下预览。

### 6. 整理交付并检查可用性

导出、材质槽、LOD、部件命名和版本交付；初步姿势检查后交给绑定继续验证。

## 拟制作的图与示例

同一模型的造型、线框、UV 和表面效果对照；角色交付物目录样例。读者能解释“面数更少”为什么不能独立代表角色可用性。

贯穿教学示例是一名持杖角色进入遗迹庭院并施法；本篇先用相关的局部产物解释问题。示例制作后再补过程图与实际观察记录。

## 本篇边界

蒙皮权重细节归 04；动作归 05；渲染方程和 Shader 实现通过现有渲染专题延伸。

## 前置与延伸阅读

先按需阅读：[02｜原画：怎样把视觉想法变成可制作的设计]({{< relref "delivery-engineering/game-art-production-02-concept-art.md" >}})。

- [Unity Importer 的职责]({{< relref "engine-toolchain/unity-importer-what-does-it-do-and-why-source-file-is-not-just-a-file.md" >}})：按需补充相关机制与协作背景。

[回到系列首页与完整目录]({{< relref "delivery-engineering/game-art-production-collaboration-series-index.md" >}})。
