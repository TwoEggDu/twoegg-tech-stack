---
title: "03｜接上现有工具：Jenkins、SVN 与 Unity 要交出什么（提纲）"
slug: "game-delivery-agent-03-existing-tool-contracts"
date: "2026-10-26"
description: "章节提纲，正文待写：梳理检查任务、版本查询、对象定位与依赖提交的适配契约；实际接口尚未核对，适配器未实现。"
draft: false
featured: false
content_stage: "outline"
research_stage: "design-outline"
validation_status: "not-run"
weight: 3930
tags: ["交付工程", "Agent", "Jenkins", "SVN", "研究提纲"]
series: "游戏交付 Agent 研究"
primary_series: "game-delivery-agent"
series_role: "article"
series_order: 30
type: "reading-series"
reading_title: "接上现有工具：Jenkins、SVN 与 Unity 要交出什么"
reading_summary: "怎样让现有工具提供可调用、可定位、可追踪的能力？"
---

> **文稿状态：章节提纲，正文待写。实验状态：未执行。** 以现有工具可改造为设计前提；本系列尚未核对具体源码、版本、接口与权限。下列为拟研究的适配需求，新增适配器未实现，接入实验未执行。

## 本篇要回答的问题

问题处理服务要调用什么能力，工具又必须返回什么事实？本篇准备从现有操作入口推导适配契约，区分检查机能力与作者本地能力。

## 1. 先盘点能力，再选择调用形式

- 列出 Jenkins 检查任务、SVN 查询、Unity 扫描、MissingFind、依赖提交和现有通知入口。
- 分别核对命令行、编辑器操作、报告文件及可调用接口，不默认所有工具都能无人值守运行。
- 依据部署环境比较函数、进程或服务调用；MCP 作为后续接入选择，协议选型不替代业务契约。

## 2. 检查机怎样交出完整结果

- 研究任务请求、排队、执行、报告与完成回执怎样关联同一次检查。
- 对照第 02 篇的输入快照，确认真正检查的 revision、规则与范围。
- 设计成功时的明确报告，以及工具失败、扫描未完成和报告缺失的不同状态。

## 3. 版本查询怎样支持定位

- 明确查询指定版本的文件、差异、历史和资源存在情况所需的参数。
- 保留证据对应的 revision 与路径，避免把当前文件解释成过去版本的事实。
- 将目录负责人或提交者作为协作线索，研究怎样避免直接把最后修改者认作根因责任人。

## 4. 作者本地工具怎样接上共享问题

- 为 MissingFind 设计定位输入：资源、对象、组件、属性及可取得的稳定标识。
- 研究本地版本不匹配、对象已移动、缺少引用标识时怎样提示与回退。
- 为依赖提交设计独立的候选预览：资源与 meta 关系、SVN 状态、纳入原因和采集时的副本信息。
- 将预览、确认与实际提交拆开，讨论确认前文件变化如何重新核对。

## 5. 一次工具调用怎样形成可靠回执

- 列出请求身份、参数校验、允许范围、执行状态、结果完整性及原始证据入口。
- 区分模型提出动作、服务允许执行、工具实际完成和领域目标达成。
- 研究超时、重复请求、部分结果及本地工具不可用时的返回语义；状态恢复转交第 07 篇。

## 计划图表与待补证据

- 计划图表：检查机与作者本地能力图；现有入口到拟议接口的映射表。
- 待补材料：各工具实际输入输出、成功与失败报告、依赖提交是否可独立导出候选的源码或操作证据。
- 待设计验证：请求与真实任务关联、版本不匹配、空结果与执行失败区分、预览后文件变化。
- 验证状态：适配方案未实现；尚无当前项目接口可用性结论。

## 本篇边界与后续阅读

- [第 04 篇：普通 Missing 的固定流程]({{< relref "delivery-engineering/game-delivery-agent-04-missing-workflow-baseline.md" >}})使用这些契约组织正常路径。
- [Tool Runtime]({{< relref "ai-empowerment/agent-engineering-06-tool-runtime.md" >}})说明工具执行的通用分层。
- [Harness 在交付工程里的位置]({{< relref "harness-engineering/harness-engineering-06-in-delivery-engineering.md" >}})提供异步交付接口的相关视角。

<!-- DATA-TODO: 固定实际 Jenkins、SVN、Unity 及自研工具版本，核对报告与入口；不公开内部地址、凭据、人员或受限源码。 -->
