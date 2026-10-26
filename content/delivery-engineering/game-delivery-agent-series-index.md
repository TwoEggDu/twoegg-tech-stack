---
title: "游戏交付 Agent 研究：从提交检查到修正复检"
slug: "game-delivery-agent-series-index"
date: "2026-10-26"
description: "以 Missing 为贯穿案例，研究定位、补交与复检怎样形成闭环，以及何时值得引入模型。第 01～03 篇正文已写，其余 5 篇为提纲；新增方案未实现，实验未执行。"
draft: false
featured: false
weight: 3900
tags: ["交付工程", "Agent", "Unity", "研究规划", "Index"]
series: "游戏交付 Agent 研究"
series_id: "game-delivery-agent"
series_role: "index"
series_order: 0
series_nav_order: 22
series_title: "游戏交付 Agent 研究"
series_entry: true
series_audience: ["Unity 与工具链工程师", "构建检查维护者", "QA 与技术负责人"]
series_level: "工程研究"
series_best_for: "已有提交检查和交付工具，希望组织定位、修正与复检，并判断模型能在哪些例外中提供帮助。"
series_summary: "从 Missing 的固定处理流程出发，研究工具接口、版本与状态、逐轮模型判断及对照验证。第 01～03 篇正文已写，其余 5 篇为提纲；实验未执行。"
series_intro: "提交检查已经能发现问题，修正怎样进入可追踪的复检？本系列沿 Missing 研究现有工具、固定流程、人和模型的分工。"
series_reading_hint: "先读 01～03 正文了解分工、记录关联与工具契约，再沿 04 的提纲看固定流程、05～06 看模型调查、07～08 看可靠性与验证；后续 5 篇正文待写。"
research_stage: "design-analysis"
validation_status: "not-run"
type: "reading-series"
topic_key: "delivery"
reading_title: "游戏交付 Agent 研究"
reading_lead: "从一次 Missing，研究问题怎样真正处理完。"
reading_intro: "把定位、修正与复检接起来，再判断哪里值得引入模型。以可改造的 Jenkins、SVN 与 Unity 工具为设计前提，用固定流程和例外调查两条路径展开。"
reading_route: ["固定流程基线", "例外调查与模型判断", "修正复检与对照验证"]
reading_status: "第 01～03 篇正文已写，其余 5 篇为提纲；新增服务与适配器未实现，本系列实验未执行。"
reading_number_by_position: true
reading_groups:
  - title: "先让固定流程成立"
    intro: "从交接问题进入版本记录、工具适配与普通 Missing 的处理基线。"
    first: 10
    last: 40
  - title: "再研究模型在哪一轮参与"
    intro: "识别调查任务，逐轮拆开输入、工具选择、回执与状态推进。"
    first: 50
    last: 60
  - title: "用复检与对照回答是否有效"
    intro: "明确完成条件与失败恢复，比较固定流程和按需模型判断的结果。"
    first: 70
    last: 80
---

## 当前研究与验证状态 {#research-status}

| 维度 | 当前状态 |
| --- | --- |
| 内容进度 | 第 01～03 篇正文已写；第 04～08 篇为章节提纲，正文待写 |
| 历史材料 | 已有资源检查、MissingFind 与依赖提交的实践文章可供阅读；现有工具源码、接口和当前运行状态尚未在本系列核对 |
| 设计研究 | 第 01～03 篇已展开职责分工、记录关联与拟议工具契约；当前项目接口待核对，记录及适配方案未实现，后续模型选择仍待研究 |
| 新增工程实现 | 问题处理服务、适配器与自动状态管理尚未实现 |
| 工具与模型实验 | 本系列均未执行；没有真实模型调用轨迹、对照成绩或生产收益结论 |

阅读时请把“准备研究什么”和“已经验证什么”分开。后续文章会分别标记历史实践、设计推演、合成回放、真实工具实验与真实模型实验。

## 这组研究从哪里开始

已有的[资源完整性实践]({{< relref "engineering-journey/production-resource-completeness.md" >}})记录了 Missing 检查、编辑器定位和依赖提交怎样帮助团队处理资源问题。本系列从这些能力继续提问：一次报错怎样关联修正提交，怎样确认共享版本确实复检通过，证据不一致时又该怎样调查？

研究保留三种可能结论：现有工具已经足够、补齐固定流程即可、部分例外值得引入模型。后续比较需要把工具改进、流程改进和模型贡献分别记录。

## 系列目录 {#chapters}

第 01～03 篇为正文，依次解释流程与模型分工、记录关联、工具输入输出及回执。第 04～08 篇仍为提纲，列出核心问题、计划结构与待补证据；正文完成度与实验完成度分别标记。

{{< reading-series-chapters >}}

## 两条贯穿研究的路径

| 路径 | 准备研究的问题 | 当前证据身份 |
| --- | --- | --- |
| 常规路径 | 共享版本缺少依赖，本地存在匹配资源；定位、生成候选、确认补交并复检 | 设计推演，未执行；用于建立不调用模型的基线 |
| 例外路径 | 本地正常、检查机 Missing、依赖预览未给出可用补交方案；先核对预览含义与范围，再调查引用差异和内容意图 | 构造的示意场景，未执行；用于研究下一步查什么及何时转人工 |

示例中的版本号、引用标识、路径、工具请求和返回均需标明来源。缺少本地文件或引用证据时，应保留未知状态；接口能否提供这些信息，需要后续核对。

## 按问题选择阅读路径

- **想先把处理流程组织起来**：先读 [01：分工]({{< relref "delivery-engineering/game-delivery-agent-01-why-checks-need-follow-through.md" >}})、[02：记录关联]({{< relref "delivery-engineering/game-delivery-agent-02-check-finding-repair-records.md" >}})与 [03：工具契约]({{< relref "delivery-engineering/game-delivery-agent-03-existing-tool-contracts.md" >}})正文，再读第 04 篇提纲。
- **想知道模型究竟判断什么**：先看 [04：固定流程基线]({{< relref "delivery-engineering/game-delivery-agent-04-missing-workflow-baseline.md" >}})，再进入 05 → 06。
- **负责工具接入与可靠性**：重点看 02 → 03 → 07。
- **想判断是否值得投入**：沿 01 → 04 → 06 → 08 比较研究方案；当前尚无效果结论。

## 与已有专题怎样衔接 {#related}

- [生产管线中的资源完整性实践]({{< relref "engineering-journey/production-resource-completeness.md" >}})：提供已确认的历史问题与工具使用背景；本系列新增方案不倒写成历史成果。
- [Agent Engineering 系统课程]({{< relref "ai-empowerment/agent-engineering-series-index.md" >}})：按需补 Tool Runtime、Agent Loop、Context、Evidence 与 Eval；无需先读完整课程。BuildPilot 设计仍由该课程维护。
- [Harness 在交付工程里的位置]({{< relref "harness-engineering/harness-engineering-06-in-delivery-engineering.md" >}})：了解异步验证与交付工具的接口视角；各自证据边界以原文说明为准。
- [CI/CD 流水线]({{< relref "delivery-engineering/delivery-cicd-pipeline-series-index.md" >}})：按需回看检查、构建与结果聚合的交付机制。

首期聚焦提交检查问题的处理。编译、配置及构建失败调查是后续候选，是否扩展由材料和验证结果决定。
