---
title: "04｜绑定与蒙皮：怎样让模型能够正确变形（提纲）"
slug: "game-art-production-04-rigging-skinning"
date: "2026-10-25"
description: "章节提纲，正文待写：分清骨架、控制器和蒙皮权重，认识动作制作之前需要建立的形变基础。"
draft: false
featured: false
content_stage: "outline"
weight: 84
tags: ["美术生产", "客户端", "协作"]
series: "游戏美术生产与客户端协作"
primary_series: "game-art-production-collaboration"
series_order: 4
type: "reading-series"
reading_title: "绑定与蒙皮：怎样让模型能够正确变形"
reading_summary: "分清骨架、控制器和蒙皮权重，认识动作制作之前需要建立的形变基础。"
---

> **章节提纲 · 正文待写。** 本页列出后续正文的内容安排；拟制作的素材、示例与验证尚未完成。

## 本篇要回答的问题

静态模型怎样成为动画师可操控、引擎可使用的角色？

## 内容提纲

### 1. 分清骨架、控制器与权重

用抬手动作分清网格、骨架、关节变换、动画控制器与蒙皮权重。

### 2. 按动作需求建立骨架

根据动作需求设计骨架层级、关节位置、初始姿势，以及根节点和武器挂点约定。

### 3. 用控制系统辅助制作

控制器、约束、FK/IK 如何帮助制作动作；区分制作端控制系统和实际导出内容。

### 4. 检查权重与关节形变

解释权重分配与关节变形：肩、肘、膝等位置怎样检查塌陷、拉伸和穿插。

### 5. 处理修正形变与装备

介绍修正形变、Blend Shape、装备与换装的关联，说明不同方案的适用范围。

### 6. 验证、导出与通知变更

用姿势组验证后交给动画；导出骨架、烘焙结果和骨架版本变化需要怎样通知下游。

## 拟制作的图与示例

骨架/控制器/网格关系图、权重可视化与错误/修正姿势对照；骨架和挂点约定表。

贯穿教学示例是一名持杖角色进入遗迹庭院并施法；本篇先用相关的局部产物解释问题。示例制作后再补过程图与实际观察记录。

## 本篇边界

重点是制作与交接；GPU 蒙皮实现作为选读。绑定可以由独立绑定师、角色人员或动画 TA 等承担，按示例说明。

## 前置与延伸阅读

先按需阅读：[03｜角色：从设计稿到模型与材质]({{< relref "delivery-engineering/game-art-production-03-character-assets.md" >}})。

- [骨骼蒙皮与 Blend Shape]({{< relref "rendering/unity-rendering-03-skeletal-animation.md" >}})：按需补充相关机制与协作背景。

[回到系列首页与完整目录]({{< relref "delivery-engineering/game-art-production-collaboration-series-index.md" >}})。
