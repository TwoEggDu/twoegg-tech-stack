# Article Card｜13 Context Debugging

> 权威基线是 `docs/agent-engineering-series-plan.md`；`docs/agent-engineering-course-plan-v3.1-review.md` 的 Article 13 小节只作历史结构输入。本文件机械实例化 canonical 与已批准任务边界，不预设 Research、Lab 或 Evidence 结论。

## Canonical identity

- Title：`Context Debugging：Packing、Compression、Pollution 与可重建性`
- Part：`Part III｜Agent 的信息、状态与知识`
- Weight：`L（Major Core Lesson）`
- Optional：`No`
- Mode：`LAB_ARTICLE`
- Required Lab：`Lab 05 Context Debugging`

## Positioning

信息调试核心篇。Article 12 回答“这个 Step 应该看到什么”；Article 13 回答“当这个 Step 看到了错误、过期、冲突、污染、压缩或截断后的视图时，怎样定位 failure layer，并判断当前证据只支持 audit、某级 reconstruction，还是必须保持 UNKNOWN”。

## Reader questions

1. Context Packing 在哪些变换点会失真？
2. Missing、Stale、Wrong Scope、Conflict、Pollution、Compression Loss 与 Truncation 怎样区分？
3. 为什么增加更多 Context 不自动提高可靠性？
4. Compression 怎样丢失 provenance、scope、uncertainty、negative evidence 或 conflict？
5. 怎样从 Snapshot / Receipt 定位 Context regression？
6. 什么条件下只能 audit，什么条件下可以 semantic / decision reconstruction，什么条件下必须返回 UNKNOWN？

## Dependencies

- Article 02：Prompt bug 与任务合同。
- Article 08：Step、Observation、State 与 terminal。
- Article 10：State revision、stale suggestion 与 deterministic validation。
- Article 11：Checkpoint、Recovery、Replay / reconstruction boundary。
- Article 12：effective Context、application-visible Context Snapshot、Receipt 与 Context Assembly。

## Candidate investigation surface

```text
candidate sources
  -> selection
  -> scope
  -> ordering
  -> representation
  -> compression / summarization
  -> budget fit
  -> request materialization
```

这是待 Research 与 Lab 05 验证的诊断表面，不是 Provider 内部统一 pipeline。

## Required examples and failure cases

- 同一 Prompt、不同 Context Snapshot 的具体故障开场。
- stale State / stale capability / stale Evidence。
- obsolete Plan、无关 history、重复规则与不可信材料污染。
- conflict 被 summary 折叠、UNKNOWN 被压成确定结论、locator / provenance 丢失。
- required Evidence 被静默裁剪与 output reserve 被吞掉。
- Receipt 有 digest 但原 contributor 缺失，导致 `AUDITABLE != RECONSTRUCTABLE`。

## Evidence requirements

- 2026-08-22 当前官方 Provider / SDK 文档；Provider / API / model / feature / version scope 必须明确。
- Claim Inventory 使用 `CONFIRMED / PARTIAL / BLOCKED / PROPOSAL`，核心 `BLOCKED` Claim 不进入正文。
- 污染、诊断与重建 taxonomy 若由课程建立，必须标 `COURSE PROPOSAL`。
- Required Lab 05 必须保存 frozen Design、真实 Observations、raw artifacts、commands / exit codes、hashes、limitations 与 Evidence Merge。
- Receipt 不得被升级为完整 effective Context 或 Provider-internal token reconstruction guarantee。

## Explicit non-goals

- 不把 Prompt Workshop、Prompt 改写或“请更认真”当作 Context repair。
- 不证明真实模型准确率、hallucination 或 Provider 内部截断算法。
- 不展开 Article 14 Working Memory lifecycle / mutation / persistence。
- 不展开 Article 15—16 Long-term Memory、Project Memory、Vector DB、Embedding、Retriever、Reranker 或 RAG architecture。
- 不把 Lab fixed fixture 外推为 production、cross-provider 或 universal best practice。
- 不创建 Article 14 workspace、Research、Evidence、Outline 或 Draft。

## Learning check candidates

1. Prompt 没变但结果漂移，怎样先证明 Context 是否变化？
2. summary 删除 `UNKNOWN`、source revision 与 conflict 后，哪些 claim strength 被非法升级？
3. Receipt 只有 ref / digest / order，原 contributor 已不存在，为什么仍不能声称 byte-level reconstruction？

## Job competency target

- 能区分 Prompt bug 与 Context bug，并冻结失败 Step identity。
- 能检查 contributor 的 source、version、scope、authority、disposition 与 omitted set。
- 能用 deterministic fixture 检测 stale、conflict、pollution、compression loss、budget / truncation failure 与 reconstruction boundary。
- 能在 application-visible evidence 不足或 Provider-managed behavior 未知时返回 `UNKNOWN`。
