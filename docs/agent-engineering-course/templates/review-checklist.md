# Agent Engineering 课程审查清单

每次审查记录 Reviewer、日期、结论和处置项。结论只能是 `PASS`、`PASS_WITH_NOTES` 或 `BLOCKED`；任一类为 `BLOCKED` 时文章不能进入 `FINAL`。

## Technical Review

- [ ] 核心概念定义准确，且与术语表一致。
- [ ] 机制描述没有把模型能力、Runtime、Harness 和 Host 混为一层。
- [ ] 代码、接口、状态机和流程图内部一致。
- [ ] 版本敏感事实标明适用版本与时间。
- [ ] 示例的正常路径、失败路径和停止条件都可解释。
- [ ] 设计选择与已实现行为使用不同语态。
- [ ] 没有把单一产品实现描述成行业唯一标准。
- [ ] Tool / Skill / Workflow / Agent / Harness 没有混用。
- [ ] Context / Working Memory / Session / Long-term Memory / RAG 没有混用。

记录：

- Reviewer：
- Date：
- Outcome：`NOT_STARTED`
- Findings / Disposition：

## Evidence Review

- [ ] 每个核心主张都有 Claim ID 与 Evidence Card。
- [ ] Evidence Status 与正文措辞强度匹配。
- [ ] `Proves` 与 `Does Not Prove` 均已填写。
- [ ] `PARTIAL` 证据对应的主张已经收窄。
- [ ] 正文不存在依赖 `BLOCKED` 证据的行为性结论。
- [ ] `PROPOSAL` 明确标成设计，而不是运行事实。
- [ ] Lab 有 fixture、复现步骤、原始输出和限制。
- [ ] DSH 篇分别检查源码确认与运行确认。
- [ ] 推断链没有把相关性写成因果性。

记录：

- Reviewer：
- Date：
- Outcome：`NOT_STARTED`
- Findings / Disposition：

## Course Review

- [ ] 文章只承担 canonical 指定的课程职责。
- [ ] 前置文章与术语依赖满足，或给出最小补桥。
- [ ] 概念深度符合 Progressive Definition 所在阶段。
- [ ] 内容投入与本篇 `S / M / L` 权重匹配。
- [ ] 没有提前吞掉后续文章，也没有重复从零定义。
- [ ] 开头说明这篇解决什么问题，结尾完成向下一篇的桥接。
- [ ] 读者变化与 Article Card 一致。
- [ ] 案例和实验服务于工程判断，而不是堆工具操作。
- [ ] 与 AI 赋能、Harness Engineering 等相邻系列边界清楚。

记录：

- Reviewer：
- Date：
- Outcome：`NOT_STARTED`
- Findings / Disposition：

## Final Gate

- [ ] 三类审查均无 `BLOCKED`。
- [ ] 所有阻断项已关闭或从文章范围中明确移除。
- [ ] 最终标题、描述、术语、链接和图注已核对。
- [ ] 发布后需要回写的 status、canonical 和系列入口已列出。
