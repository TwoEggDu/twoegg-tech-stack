# Article 08 Review｜Agent Loop

- Lifecycle：`REVIEW`
- Review Status：`FINAL_GATE_PASS`
- Review Cycle：`1 / 3`
- Final Gate：`PASS`
- Unclosed Findings：`0`
- Current Reviewer：`/root/article_08_final_gate`
- Execution：`REAL_SUBAGENT`
- Fresh Context：`YES / REPOSITORY ARTIFACTS ONLY`
- Date：`2026-08-20`（Asia/Shanghai）
- Allowed Writes：仅本文件 `review.md`
- Actual Writes：仅本文件 `review.md`
- Runtime / Provider Calls：`NONE / NONE`

## Review Scope

首轮为 initial Review / cycle 0。Reviewer 独立读取 repository instructions、TwoEgg 写作方法、Course Factory / worker / production contracts、review checklist、canonical Article 08—09 边界、Glossary、Article Card、Research、Evidence、Outline、完整 Draft、Claim 直接相关的 Published Article 03 / 05 / 06 / 07 边界，以及 Lab 03 frozen Design、run-a raw Result / Observation / State / Trace、execution log、失败记录与 limitations。

本次为 cycle 1 fresh `REVIEW_RECHECK`。Reviewer 只依据原 `08-F01`、Revision Disposition、变更后的 affected Draft 伪代码与相邻边界、`08-C06` / AL-03 Evidence，以及 run-a `trace.jsonl` / `case-results.jsonl` 的最小充分 raw evidence 独立复核。

两轮均未读取 Author / Revision Worker hidden reasoning、confidence 或 self-score；Reviewer 未修改 Draft、Outline、Research、Evidence、Lab、Published Content、canonical 或 global durable state；未重跑 Lab、Provider、Hugo、stage、commit 或 push。

本次为独立 `FINAL_GATE`。Reviewer 重新读取 final current Draft、完整 Finding / recheck 记录、五维评分、C01—C08 Evidence 状态与 Claim-direct Lab raw artifacts 的最小充分集合；只核验 Final Gate，不重开已关闭 Finding、不推进 Lifecycle durable state、不派发 Publisher。

## Finding Register

### 08-F01｜pre-decision guard 伪代码没有落下 terminal record

- Status：`CLOSED`
- Severity：`MINOR`
- Category：`TECHNICAL`
- Location：`draft.md`，`## 最小 Host Loop：每个 Step 只提交一次` 的伪代码
- Problem：伪代码在 `pre_decision_guard(...)` 返回 terminal 后直接执行 `break`，却没有展示 terminal record 的提交或 Trace 写入；相反，`REQUEST_STOP` 路径明确调用 `commit_terminal_once` 与 `append_step_and_terminal_trace`。这让文章中最关键的 `MAX_STEPS_EXHAUSTED / INCOMPLETE` 路径在可迁移骨架里表现为“退出循环但没有落盘终止事实”。
- Supporting Evidence：Lab 03 frozen algorithm 要求 guard 在下一次 Decide 前终止，并为 terminal 写独立 `TERMINAL` trace；run-a `trace.jsonl` 的 sequence 10 实际保存 `AL-03 / MAX_STEPS_EXHAUSTED / INCOMPLETE`，`case-results.jsonl` 同时保留未消费的 `al03-decision-03`。Draft 后文也要求 Trace 串起 control decision 与 terminal fields。
- Why It Matters：这是读者最可能直接迁移到工程里的最小骨架。若照伪代码实现，bounded stop 虽发生，却缺少可审计 termination reason / outcome，和全文“停止必须与成功分字段、可追溯”的中心判断不一致。
- Required Disposition：只修改该伪代码，使 guard terminal 在退出前显式写入 terminal record / trace（或调用一个语义明确、内部完成该写入的 terminal commit 函数）；同时保留“guard 在下一次 Decide 前生效”和“guard stop 不是一个已消费 Decision 的 Step”两条边界。不得借此展开 checkpoint、recovery 或 Article 11 内容。

## 08-F01 Revision Disposition

- Finding ID：`08-F01`
- Files Changed：`docs/agent-engineering-course/articles/08-agent-loop/draft.md`、`docs/agent-engineering-course/articles/08-agent-loop/review.md`
- What Changed：在最小 Host Loop 的 `pre_decision_guard(...)` terminal 分支中，先调用 `host_reducer.commit_terminal_once(...)` 提交 terminal record，再调用 `append_terminal_trace(...)` 写入独立 terminal trace，最后退出循环；并明确该 guard 在下一次 Decide 前生效，且不消费新的 Decision、不新增一个已消费 Decision 的 Step。
- Evidence Impact：未新增或升级 Claim，也未修改 Evidence；修订仅让伪代码与 AL-03 raw trace 的 `MAX_STEPS_EXHAUSTED / INCOMPLETE` terminal record、未消费 `al03-decision-03` 以及 08-C06 的既有 Evidence 解释一致。未展开 checkpoint、recovery、cancellation trajectory 或 Article 11 内容。
- Proposed Status：`READY_FOR_RECHECK`

## Cycle 1 Recheck Evidence

- Finding ID：`08-F01`
- Recheck Status：`CLOSED`
- Required Disposition Match：`PASS`。affected guard 分支依次执行 `host_reducer.commit_terminal_once(state, terminal)`、`append_terminal_trace(state)` 与 `break`，因此退出前同时留下 terminal record 与 terminal-only trace。
- Pre-Decide Boundary：`PASS`。`pre_decision_guard(...)` 仍位于 `decision_source.decide(...)` 之前；相邻说明明确该分支“在下一次 Decide 前生效”。
- No New Decision / Step：`PASS`。相邻说明明确 guard 没有消费新的 Decision，只提交 terminal record / trace，不新增一个已消费 Decision 的 Step；这与 run-a AL-03 terminal trace 的 `decision_id / decision_kind / decision_source = NOT_RUN`、`decision_calls_used=2`、`tool_calls_used=2` 一致。
- Raw Terminal Correlation：`PASS`。run-a `trace.jsonl` sequence 10 保存独立 `TERMINAL` record：`MAX_STEPS_EXHAUSTED / INCOMPLETE`、`steps_used=2`、`state_revision_before=2 / after=2`；`case-results.jsonl` 同时保存 `remaining_decision_ids=[al03-decision-03]`，证明第三个 Decision 未消费。
- Scope Boundary：`PASS`。该修订只补齐当前 Run 的 guard terminal 持久化语义；没有引入 checkpoint、resume、retry、recovery、cancellation trajectory 或 Article 11 展开。
- Evidence / Claim Impact：`NONE`。修订没有新增或升级 Claim，正文仍在 `08-C06 / E-08-10 / E-08-11` 的现有产品范围与 fixed-Lab conformance 内。
- Closure Basis：Required Disposition 全部满足，未发现由本次修订引入的新 `BLOCKER / MAJOR / MINOR / EDITORIAL` Finding；只有 Reviewer 在本次 recheck 将 `08-F01` 关闭。

## Finding Summary

| Severity | OPEN | CLOSED | ESCALATED |
|---|---:|---:|---:|
| BLOCKER | 0 | 0 | 0 |
| MAJOR | 0 | 0 | 0 |
| MINOR | 0 | 1 | 0 |
| EDITORIAL | 0 | 0 | 0 |
| Total | 0 | 1 | 0 |

## Claim And Boundary Review

- Claim traceability：`8 / 8 PASS`。C01—C08 均能从 Evidence Register 追到正文主落点，Draft 未新增核心 Claim。
- Proposal discipline：`08-C03 / 08-C05 PASS`。Run / Turn / Step、Host-owned reducer、terminal contract 与最小接口骨架持续标为课程工作定义或 Proposal，没有升级成行业标准或框架强制要求。
- Product scope：`08-C01 / C02 / C04 / C06 PASS`。OpenAI Python current docs、logical chat turn、LangGraph super-step 与 LangChain ToolMessage / Command 均保留产品 / counter scope，没有跨产品单位换算。
- Fixed-fixture scope：`08-C07 / C08 PASS`。正文就近保留 frozen Windows/.NET、固定 fixture、`ScriptedDecisionSource v1`、fixed Host 与 two-fresh-process 限定，没有外推 Model / Provider determinism、planning quality 或 production reliability。
- Raw Lab traceability：`PASS`。AL-02 failed Tool Outcome 与 `TOOL_FAILURE` Observation 使用同一 record digest；AL-03 第三个 Decision 未消费；AL-04 action fingerprint 相同、record digest 不同、goal-state digest 不变、`EV-FAKE` 被拒绝；四个 terminal 与 Draft 一致。
- Failure / limitations：`PASS`。CIM denied、compile collision、fixture EOF、unavailable testhost、live-reference snapshot digest mismatch 和两次 delivery interruption 均保留；正文没有把这些记录包装成 recovery 或 cancellation Evidence。
- Article boundary：`PASS`。Planning 留给 09，Workflow / State Machine 留给 10，checkpoint / retry / cancellation trajectory / recovery 留给 11；Context / Memory、Multi-Agent、budget、DSH、BuildPilot、Provider / MCP runtime 与 production reliability 均明确 non-scope。

## Checklist Outcomes

### Technical Review

- Outcome：`PASS`
- Disposition：概念、状态语义、四条轨迹与产品术语均准确；cycle 1 recheck 已确认 guard terminal 在 break 前持久化且不消费新 Decision / Step，`08-F01 CLOSED`。

### Evidence Review

- Outcome：`PASS`
- Disposition：8 / 8 Claim、Expected / Observed 分离、raw artifact correlation、failure ledger、Proves / Does Not Prove 与 scope 标签均成立；没有依赖 BLOCKED Claim。

### Course Review

- Outcome：`PASS`
- Disposition：L 级问题空间、抽象模型、C# 责任面、Lab 反例、Learning Check 与 Article 09—11 stop line完整。

### Final Gate

`PASS`。cycle 1 recheck 的 `08-F01` 仍为 `CLOSED`；当前没有未关闭 `BLOCKER / MAJOR / MINOR / EDITORIAL`，五维分数继续满足基线。Final Gate 只推荐 Lifecycle `FINAL`，不替代 Publisher、Build Verify 或 Master durable state update。

## Five-Dimension Score

| Dimension | Score | Threshold | Result |
|---|---:|---:|---|
| Technical Accuracy | 19 / 20 | 18 | PASS |
| Evidence Discipline | 20 / 20 | 18 | PASS |
| Teaching Quality | 18 / 20 | 17 | PASS |
| Engineering Transfer | 18 / 20 | 17 | PASS |
| Readability & Compression | 17 / 20 | — | PASS |
| Total | 92 / 100 | 88 | PASS_BY_SCORE |

cycle 1 recheck 未发现会降低原评分的新问题；总分与四项冻结最低线继续满足课程基线。

## Final Gate Execution

- Reviewer Recheck：`PASS / CONFIRMED`。`08-F01` 的 Required Disposition 仍全部满足；guard terminal 在下一次 Decide 前提交 terminal record 与独立 terminal trace，不消费第三个 Decision，也不新增一个已消费 Decision 的 Step。
- Finding Closure：`08-F01 CLOSED`；unclosed Finding 为 `0`，其中 `BLOCKER=0 / MAJOR=0 / MINOR=0 / EDITORIAL=0`；没有新 Finding。
- Score Baseline：`92 / 100 PASS`；Technical `19 >= 18`、Evidence `20 >= 18`、Teaching `18 >= 17`、Engineering Transfer `18 >= 17`、Total `92 >= 88`。
- Claim Traceability：`8 / 8 PASS`。Draft 的 Claim-to-section table 与 Evidence Register C01—C08 一一对应；没有依赖 `PARTIAL / BLOCKED` Claim，也没有 Revision 新增的核心事实。
- Proposal Discipline：`PASS`。C03 的 Run / Turn / Step 与 C05 的 Host-owned reducer、terminal contract、接口骨架持续标为课程工作定义或 Proposal；Lab conformance 没有被升级成行业标准。
- Product Scope：`PASS`。OpenAI Python current Runner / `max_turns`、logical chat turn、LangGraph `super-step` 与 LangChain ToolMessage / Command 均保留 cited-product / counter scope。
- Fixed-Fixture Scope：`PASS`。C07 / C08 仍限定到 frozen Windows/.NET、固定 read-only fixture、`ScriptedDecisionSource v1`、fixed Host 与 two-fresh-process artifacts；不外推真实 Model / Provider determinism、planning quality 或 production reliability。
- Raw Evidence Recheck：`PASS`。AL-02 的 failed Outcome 与 failure Observation 共享 record digest；AL-03 terminal 为 `MAX_STEPS_EXHAUSTED / INCOMPLETE` 且 `al03-decision-03` 未消费；AL-04 两次 action fingerprint 相同、record digest 不同、goal-state digest 不变、两步均 `NO_PROGRESS`，`EV-FAKE` 被拒绝；run-a / run-b 六个 artifact 对应文件仍逐 byte 相等。
- Failure / Limitations：`PASS`。正文保留 pre-green implementation / verification failures与两次 delivery interruption，并明确它们不是 recovery 或 cancellation Evidence；Lab limitations 仍覆盖 no Provider/network/MCP、no external side effect、no cancellation trajectory、sequential single-action fixture。
- Non-Scope：`PASS`。Planning、Workflow / State Machine、checkpoint / retry / cancellation trajectory / resume / recovery、Context / Memory、Multi-Agent、budget engineering、DSH、BuildPilot 与 production reliability 均未被本次 Revision 吞入。
- Revision Core-Fact Audit：`PASS / NONE ADDED`。Revision 只补齐 pre-decision guard terminal persistence 与相邻解释；该变化直接由现有 `08-C06 / E-08-10 / E-08-11` 和 AL-03 raw terminal 支撑，未创建、升级或重解释 Claim。
- Publication-risk check：标题、术语、repository-relative source / Lab links 与图表说明已复核；Draft 当前无发布 front matter，metadata、Hugo `relref`、series navigation 与 build 仍由 Publisher / Build Verify 负责。
- Post-publication update candidates：Article README Lifecycle、`status.md`、`course-run-state.md` 与必要 canonical publication metadata；只由 Master 在 Publisher / Build PASS 后统一回写。

Final Gate Decision：`PASS`。

## Gate Decision And Next Action

- Review Recheck Decision：`PASS / CONFIRMED`
- Final Gate Decision：`PASS`
- Lifecycle Recommendation：`FINAL`（recommendation only；由 Master 决定 durable transition）
- Final Candidate：`APPROVED`
- Unclosed Findings：`0 BLOCKER / 0 MAJOR / 0 MINOR / 0 EDITORIAL`
- Blocker：`NONE`。
- Next Action：推荐 `PUBLISH`；由 Master 验证本次 envelope、artifact 与实际 diff 后决定是否派发 Publisher。
- Prohibited Transition：Reviewer 不直接发布、不执行 Build Verify、不修改 global durable state，也不得启动 Article 09。

## Stop Line

本次 Final Gate 唯一写入为 `docs/agent-engineering-course/articles/08-agent-loop/review.md`。Reviewer 未直接修 Draft，未推进 durable state，未提交、推送或派发下一 worker。
