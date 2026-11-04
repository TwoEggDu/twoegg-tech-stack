---
title: "游戏客户端性能工程｜从基础、预算与机型分档，到测试和持续监测"
slug: "client-performance-engineering-series-index"
date: "2026-11-04"
description: "先理解成本和观测指标，再学会制定预算、划分设备与画质档位，分析 UI、场景、角色、动画和特效，逐步建立可信的测试与监测体系。"
draft: false
content_stage: "article"
type: "reading-series"
topic_key: "performance"
series: "游戏客户端性能工程"
series_id: "client-performance-engineering"
series_role: "index"
series_order: 0
series_title: "游戏客户端性能工程"
series_entry: true
series_nav_order: 135
series_audience:
  - "客户端程序与主程"
  - "TA 与工具开发者"
  - "参与性能预算和验收的内容负责人"
series_level: "基础到工程实践"
series_best_for: "希望从理解性能指标，走到预算、设备分档、准确采集和持续回归"
series_summary: "围绕共同基础、预算、机型分档、五类内容、准确测量和持续监测，建立客户端性能工程的完整学习路线。"
series_intro: "先解释内容怎样产生成本、数字代表什么，再学习预算和机型分档，最终把判断依据落实为可复现的测试与持续监测。"
series_reading_hint: "第一次从共同基础开始；已有具体问题时按阶段跳转。硬件、OS 和引擎机制按需补齐，Unity 作为首个实现映射。"
reading_title: "游戏客户端性能工程"
reading_lead: "从基础、预算与机型分档，到测试和持续监测"
reading_intro: "面对一个界面、一组角色或一场战斗，怎样知道应该观察什么、允许花多少成本，以及测到的结果是否可信？沿着这些问题，逐步建立可以解释、验证和持续维护的性能工程能力。"
reading_route:
  - "理解成本与指标"
  - "制定预算与分档"
  - "分析内容与准确测量"
  - "建设测试与监测体系"
reading_status: "首篇通用观测方法已写；已有基础与专题可读，数据可比性、预算校准和工具建设继续推进。"
weight: 165
featured: false
tags: ["客户端", "性能工程", "预算", "机型分档", "测试"]
---

## 这条路线要解决什么

一个界面首次打开时卡顿，可能涉及资源准备、对象创建和布局重建；同一个角色增加同屏数量后，动画、蒙皮、绘制和内存的约束又会发生变化。换到另一档设备，原来的画质配置也需要重新检验。

要把这些问题处理好，需要连起几层判断：**内容做了什么工作，应该观察哪些指标，预算从哪里来，数据怎样取得，修改后如何复验。** 本专栏先建立引擎无关的原理与方法，再解释 UI、场景、角色、动画与特效的成本模型，最后进入具体采集与验证。Unity 是首个实现路径，通用结论仍保留硬件、平台和配置条件。

你可以从基本客户端开发经验出发。遇到执行、内存、渲染或设备机制时，再沿现有文章补齐必要知识；无需先学完全部硬件、操作系统和引擎课程。

## 从这里开始 {#start}

先用三篇文章建立共同语言：

1. [先认识硬件：CPU、GPU、内存、存储和显示怎样协作]({{< relref "performance/mobile-hardware-00-getting-started.md" >}})——知道工作交给谁完成，以及容量、带宽、延迟和吞吐分别约束什么。
2. [一帧到底是怎么完成的]({{< relref "performance/game-performance-frame-breakdown.md" >}})——区分执行、等待、渲染与呈现，理解不同时间指标的边界。
3. [内存到底占在哪里]({{< relref "performance/game-performance-memory-accounting.md" >}})——建立资源、分配、驻留与释放的账本。

随后进入本专栏首篇：[从一次操作到性能指标：对象、事件与观测边界]({{< relref "performance/client-performance-task-observation-model.md" >}})。它用“首次打开界面并出现特效”连接请求、资源、实例与帧，提供事件图、最小观测表和竞争解释。已有相关基础的读者可以直接从这里开始。

## 六个阶段怎样展开 {#chapters}

下面的链接通向已有文章；每个阶段注明后续要补齐的部分。六阶段是学习路线，已有文章仍保留原来的系列身份和阅读导航。

### 01｜共同基础：先知道数字代表什么 {#foundations}

从工作量与耗时、执行与等待、帧间隔与反馈延迟，讲到内存口径、加载就绪和统计窗口。每个指标都需要说明观测对象、单位、起止点、聚合方式，以及能够支持的判断。

已有入口：[资源读完为什么还不可用]({{< relref "performance/game-performance-read-does-not-mean-ready.md" >}})、[性能诊断中的现象、假设与证据]({{< relref "performance/game-performance-diagnosis-method.md" >}})。

**已经接上的方法**：[为一次操作设计观测]({{< relref "performance/client-performance-task-observation-model.md" >}})，区分就绪、代理事件与可见反馈，保留共享成本、缺失和失败状态。正文是教学方案，没有设备实测。

**接下来补齐**：按首批问题补必要成本机制，并用两组教学记录解释样本、窗口和采集条件怎样影响比较。

### 02｜性能预算：从体验目标推导约束 {#budgets}

先明确目标帧率、响应、加载、内存和持续运行要求，再用代表场景、镜头、并发负载和设备条件校准预算。进一步区分资源预警、正确性失败与性能验收失败，说明余量和超线后的处理方式。

已有入口：[帧预算怎样拆账]({{< relref "performance/game-budget-06-frame-budget-breakdown.md" >}})、[预算总表：记录边界、负责人和超线动作]({{< relref "performance/game-budget-16-budget-master-sheet-template.md" >}})。

**接下来补齐**：从目标、场景基线到预算和制作规则的完整推导案例。具体数值需要匹配项目条件，不能直接把粒子数或 DrawCall 当成通用合格线。

### 03｜机型分档：把能力、画质和动态调节分清 {#device-tiers}

设备能力档描述本项目负载下的 CPU、GPU、内存及持续能力；画质档是一组可执行配置；运行时动态调节则应对当前负载和系统状态。这三者需要分别定义，再建立映射与验证关系。

已有入口：[峰值与持续性能的区别]({{< relref "performance/mobile-hardware-02b-sustained-performance.md" >}})、[不同档位怎样保住必要体验]({{< relref "performance/device-tier-experience-consistency.md" >}})。

**接下来补齐**：代表与边界设备选择、高中低档标定、画质矩阵、未知设备回退，以及动态调节的稳定与恢复验证。预算和分档会随着测试结果往返修订。

### 04｜内容专项：把指标放回具体工作 {#content-domains}

五类内容共享度量方法，各有不同的成本来源和关键时点：

- **特效**：发射、模拟、存活量、屏占比、重叠，以及首放、爆发和结束。先读[特效预算的观察维度]({{< relref "performance/game-budget-09-vfx-budget.md" >}})。
- **UI**：数据变化、布局与图形重建、图集、叠层，以及首开、滚动和关闭。先读[UI 首开与叠层]({{< relref "performance/game-performance-ui-first-open-layering.md" >}})。
- **角色**：几何、材质、骨骼、LOD、换装和同屏规模。先读[角色预算]({{< relref "performance/game-budget-08-character-budget.md" >}})。
- **动画**：状态推进、采样、混合、IK、蒙皮与压缩误差。先从[动画制作与接入]({{< relref "delivery-engineering/game-art-production-05-animation.md" >}})理解过渡、打断、事件和正确性要求。
- **场景**：可见集合、灯光与阴影、流式加载和组合负载。先读[场景预算]({{< relref "performance/game-budget-07-scene-budget.md" >}})，再把其他对象放回固定路线和玩法高峰检验。

**接下来补齐**：各领域的指标定义、生命周期观测表和阈值校准。角色包含的动画工作、共享资源和场景组合成本要明确归属，避免重复记账。

### 05｜准确测量：确认采到的是需要的数据 {#measurement}

从指标定义选择采集手段，固定或记录场景、版本、设备、画质、时间窗口与缓存状态。再用已知答案的样例校准，通过对照和重复测试识别背景干扰、漏计、时间错位及工具开销。

已有入口：[怎样用证据与同条件对照诊断性能]({{< relref "performance/game-performance-diagnosis-method.md" >}})。

**接下来补齐**：采集参数与来源的对应表、测试协议和校准实验。ParticleEffectProfiler 将作为特效专项的公开实现案例，用来解释数据怎样生成，以及结果的适用边界。

### 06｜监测体系：让问题能够发现、处理和复验 {#monitoring}

把资源检查、单体测试、场景与设备回归连接起来，保存配置和版本身份、原始数据、预算规则与报告。异常需要能够复现、交接和复验，规则也需要有人维护。

已有入口：[把性能判断连成工作流]({{< relref "performance/game-performance-methodology-summary.md" >}})、[设备矩阵、基线与视觉回归]({{< relref "code-quality/device-tier-validation-matrix-baseline-and-visual-regression.md" >}})。

**接下来建设**：先跑通一个特效测试任务，再以 UI 验证哪些能力可以共用，随后扩展其他内容、批量测试与持续回归。线上遥测作为后续扩展。

## 带着问题选择入口 {#questions}

- **不确定应该测什么**：从[共同基础](#foundations)开始，先定义对象、阶段和指标。
- **不知道阈值怎样定**：进入[性能预算](#budgets)，把体验要求、代表场景和校准过程连起来。
- **准备做高中低档**：进入[机型分档](#device-tiers)，先区分设备能力与画质配置。
- **正在处理某类内容问题**：进入[内容专项](#content-domains)，再回查其所依赖的共同指标。
- **准备写采集工具**：先读[准确测量](#measurement)，明确口径和验证方法。
- **希望每天或每个版本都能检查**：进入[监测体系](#monitoring)，把有效测量接到基线、异常处理和复验。

## 当前进度与后续更新 {#progress}

目前已建立学习路线，首篇通用方法已写，并选出可以先读的基础与专题文章。一次任务的观测方案与最小字典已经可以阅读；预算和分档的完整校准案例、五类内容的观测补强，以及采集工具和持续监测仍在后续计划中。

**下一步讨论两次性能测试的数据能否比较。** 首篇的事件图与观测表是方法设计，尚未完成引擎采集或设备校准。随着内容推进，本页会更新相应阅读入口；新增正文、完成设备实验和工具实际可用分别说明。已有文章中的教学参数和历史设备信息，应结合其版本与证据范围阅读，不能直接作为项目验收标准。
