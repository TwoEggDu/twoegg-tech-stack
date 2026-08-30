# Agent Engineering Course Engineering Compliance Audit

## 1. Audit identity and boundary

```yaml
audit_rubric_version: COURSE_ENGINEERING_COMPLIANCE_V1
audit_prompt_sha256: 2816029954B56390960B20822306ADF7FBDCBEE62C02D5327C9F5D40FE232ACD
audit_target_sha: 3908174accd733c6bf9ee0e9141b58b168b3f93c
audit_report_commit: SELF
audit_report_commit_resolution: the unique commit that first adds this path; resolve with git log --diff-filter=A --format=%H -- docs/agent-engineering-course/audits/course-engineering-compliance-audit-3908174.md
audit_started_at: 2026-08-30T09:35:05.2436471+08:00
evidence_adjudication_ended_at: 2026-08-30T10:12:01.9313435+08:00
timezone: Asia/Shanghai / China Standard Time
os: Windows NT 10.0.19045.0
powershell: 7.6.4
git: 2.53.0.windows.2
hugo: 0.157.0 extended
audit_completeness: PARTIAL
compliance_verdict: NON_COMPLIANT
severity_counts:
  BLOCKER: 0
  MAJOR: 4
  MINOR: 6
  NOTE: 2
```

`AUDIT_TARGET_SHA` 是被审核的课程版本；`AUDIT_REPORT_COMMIT` 是首次引入本报告的唯一报告提交，两者不得混同。提交 SHA 不能自洽地硬编码在其自身内容中，因此这里使用可由 Git 唯一解析的 `SELF` 标识；最终 push/remote receipt 由提交后的交接记录给出，不在提交前伪造。

冻结前已完成 `git fetch --prune origin`，并确认 `main`、干净 working tree/index、local `HEAD`、`origin/main` 和 live `refs/heads/main` 均为审计目标。报告写入前再次执行 live `ls-remote`；首次沙箱内 SSH 访问被拒绝（exit 1，未作为证据），授权的只读重试 exit 0 且仍返回审计目标。审计使用 `git archive` 导出的隔离快照；submodule=0、LFS=0、symlink=0、untracked=0，ignored=21，ignored 内容不进入审计对象。

Non-goals：不评价教学审美，不重写课程，不修复 Finding，不生产 Article，不更新课程状态，不重跑 Lab，不实现 BuildPilot Runtime，不迁移 DSH baseline。

```text
FROZEN_EVIDENCE_AUDITED
REPRODUCTION_NOT_RUN_BY_THIS_AUDIT
```

## 2. Verdict

本次审核为 `PARTIAL / NON_COMPLIANT`。`PARTIAL` 来自外链、历史/二进制 secret、全资产许可、完整 trust-boundary 语义覆盖和仓库自带 validator 等不可完全验证项；`NON_COMPLIANT` 来自已有 E3 直接证据支持的 mandatory `NOT_MET`：Article 35—44 缺失、Part VI/VII Audit 缺失、Article 30—34 deterministic Gate envelope 缺失或无效、全局 current state 互相冲突。

积极结果不抵消 mandatory failure：现有 34 篇 Required Article 均有发布稿和 Git completion；Article 23 一致地 Optional/Skipped；6 个 Lab 的 frozen evidence 成立；DSH tag 仍指向批准 commit；Hugo production build 和现有页面站内链接通过；未发现 BuildPilot Runtime 越界资产或正向生产能力 overclaim。

## 3. G0—G12 summary

| Gate | Status | Mandatory state | Adjudicated basis |
|---|---|---|---|
| G0 Repository Source of Truth | FAIL | NOT_MET | target 唯一，但 continuation authority 和全局 ledger 冲突（003/004） |
| G1 Inventory and Numbering | FAIL | NOT_MET | 00—44 连续；Required 35—44 缺失（001） |
| G2 Article Transaction / Git | FAIL | NOT_MET | completion 可达，但 Article 30—34 必需 deterministic envelopes 不成立（003） |
| G3 Factory State / Part Audit / Parity | FAIL | NOT_MET | Part VI/VII Audit 缺失且 current state 漂移（002/004） |
| G4 Per-Article Artifact | FAIL | NOT_MET | 现有 34 篇 core artifacts 存在；35—44 全链缺失（001） |
| G5 Evidence Contract | FAIL | NOT_MET | 现有 Evidence 结构成立；35—44 必需 DSH/Design evidence 缺失（001） |
| G6 Frozen Labs | PASS_WITH_FINDINGS | MET | 6/6 frozen evidence 成立；索引有 MINOR（006/007） |
| G7 DSH pinned source | FAIL | NOT_MET | tag、28—34、safety/license subset 通过；35—37 缺失，Article 34 有两条坏链（001/008） |
| G8 BuildPilot boundary | FAIL | NOT_MET | 38—44 / Design v1 缺失；现有表述保持 proposal，forbidden assets=0（001） |
| G9 Publication / Hugo / Links | FAIL | NOT_MET | build 与现有站内链接通过；10 个 required published pages 缺失（001） |
| G10 Security / Privacy / License | PASS_WITH_FINDINGS | mixed | current-tree 高置信 secret/private IP 未命中；本机路径、CI 固定性有 MINOR；部分 NOT_VERIFIABLE（009/010/012） |
| G11 Validator / Reproducibility | PASS_WITH_FINDINGS | mixed | canonical Hugo 可复现；无 repository-owned course validator，CI provenance 非 immutable（010/011） |
| G12 Cross-gate reconciliation | FAIL | NOT_MET | Git completion、Gate authority 与 current state 不可互相闭合（001—004） |

## 4. Compliance counts

| Scope | Result |
|---|---|
| Planned Articles | 45/45 canonical rows（00—44） |
| Required Article artifact/completion | 34/44；35—44 missing；不得解读为 34 篇全部 finding-free |
| Optional Article | 1/1 compliant；Article 23 Optional/Skipped/ZERO_ASSETS |
| Frozen Labs | 6/6 evidence compliant；G6 仍有 index MINOR |
| Part Audits | 4/7 MET + 1/7 PASS_WITH_FINDINGS + 2/7 missing |
| DSH Articles | 28—33 MET；34 MET_WITH_MINOR；35—37 NOT_MET |
| BuildPilot | 38—44 NOT_MET；Runtime/Article45/PartVIII/Unity plugin assets=0 |

## 5. Article Inventory / Completion Matrix — 45 rows

`Remote=yes` 表示 completion commit 是冻结目标的 ancestor。S/M/L 为课程规划权重，不是 Hugo weight。

| ID | Canonical title | Part | Req/Opt | Wt | Workspace | Published | Slug | Completion commit | Remote | Part audit | Inventory status |
|---:|---|---|---|:---:|---|---|---|---|:---:|---|---|
| 00 | Agent Engineering 世界地图：从 Model、Agent 到 Harness / Host | Intro | Required | S | `00-agent-engineering-world-map/` | `agent-engineering-00-agent-engineering-world-map.md` | `agent-engineering-00-agent-engineering-world-map` | `273c16e352c547421dd5afe49b5fece07af56abc` | yes | N/A | COMPLETED/PUBLISHED |
| 01 | 模型调用到底发生了什么：LLM、Model API、Messages 与 Token | I | Required | M | `01-model-api-messages-token/` | `agent-engineering-01-model-api-messages-token.md` | `agent-engineering-01-model-api-messages-token` | `b038c68fe4aefa3265b7e25761b569a0bdf852dc` | yes | Part I | COMPLETED/PUBLISHED |
| 02 | Prompt Engineering：任务合同、角色、示例与边界 | I | Required | M | `02-prompt-engineering-contract-boundaries/` | `agent-engineering-02-prompt-engineering-contract-boundaries.md` | `agent-engineering-02-prompt-engineering-contract-boundaries` | `b359a329df02ce7487b0cb1a9feaad66c886d4dc` | yes | Part I | COMPLETED/PUBLISHED |
| 03 | Structured Output：让模型输出成为机器可消费的合同 | I | Required | L | `03-structured-output-machine-contract/` | `agent-engineering-03-structured-output-machine-contract.md` | `agent-engineering-03-structured-output-machine-contract` | `857fe9fdc6baa541ced28d428d0c7fbe07d45ed9` | yes | Part I | COMPLETED/PUBLISHED |
| 04 | Model Adapter 与 LLM Gateway：Streaming、Error、Retry 和 Provider 差异 | I | Required | M | `04-model-adapter-llm-gateway/` | `agent-engineering-04-model-adapter-llm-gateway.md` | `agent-engineering-04-model-adapter-llm-gateway` | `ac10060b82d21534a014d7a4bef3b3e03f7bd475` | yes | Part I | COMPLETED/PUBLISHED |
| 05 | Function Calling 与 Tool Use：模型如何表达行动意图 | II | Required | M | `05-function-calling-tool-use/` | `agent-engineering-05-function-calling-tool-use.md` | `agent-engineering-05-function-calling-tool-use` | `c0cf180c281ea5dbb70c891176735f4ed9e34d3f` | yes | Part II | COMPLETED/PUBLISHED |
| 06 | Tool Runtime：Validate、Policy、Execute、Result 与 Trace | II | Required | L | `06-tool-runtime/` | `agent-engineering-06-tool-runtime.md` | `agent-engineering-06-tool-runtime` | `199d4e19ba6150c8c598788a2daa8488e6e855f3` | yes | Part II | COMPLETED/PUBLISHED |
| 07 | MCP 与外部能力边界：协议解决什么，宿主仍需解决什么 | II | Required | M | `07-mcp-external-capability-boundary/` | `agent-engineering-07-mcp-external-capability-boundary.md` | `agent-engineering-07-mcp-external-capability-boundary` | `f3de0f2a7b1e06c530900627183bd364ca0b4314` | yes | Part II | COMPLETED/PUBLISHED |
| 08 | Agent Loop：Turn、Step、Decide、Act、Observe 与 Stop | II | Required | L | `08-agent-loop/` | `agent-engineering-08-agent-loop.md` | `agent-engineering-08-agent-loop` | `d4693bd6d78ed63a669e181516e28247460fee11` | yes | Part II | COMPLETED/PUBLISHED |
| 09 | Planning：Agent 为什么需要计划，又为什么不能迷信计划 | II | Required | M | `09-planning/` | `agent-engineering-09-planning.md` | `agent-engineering-09-planning` | `7b9d733f33667fc8efab1708c682e67c13669846` | yes | Part II | COMPLETED/PUBLISHED |
| 10 | State Machine 与 Workflow：确定性骨架和 Agent Decision Point | II | Required | L | `10-state-machine-workflow/` | `agent-engineering-10-state-machine-workflow.md` | `agent-engineering-10-state-machine-workflow` | `b35b1f3225f9715f123496d39457f529362b997d` | yes | Part II | COMPLETED/PUBLISHED |
| 11 | Long-running Agent：Checkpoint、Retry、Cancellation 与 Recovery | II | Required | M | `11-long-running-agent/` | `agent-engineering-11-long-running-agent.md` | `agent-engineering-11-long-running-agent` | `31aef0aad617466f075725551a20bfa20715733f` | yes | Part II | COMPLETED/PUBLISHED |
| 12 | Context Engineering：每一个 Step 到底应该看到什么 | III | Required | L | `12-context-engineering/` | `agent-engineering-12-context-engineering.md` | `agent-engineering-12-context-engineering` | `a87f058ae2642870ade75fa7f23ac4396f17b94c` | yes | Part III | COMPLETED; Card MINOR |
| 13 | Context Debugging：Packing、Compression、Pollution 与可重建性 | III | Required | L | `13-context-debugging/` | `agent-engineering-13-context-debugging.md` | `agent-engineering-13-context-debugging` | `8b18b85b5a0f6a95f042832e36a8f7cb09f8609a` | yes | Part III | COMPLETED; summary MINOR |
| 14 | Working Memory 与 Investigation State：当前任务正在想什么 | III | Required | L | `14-working-memory-investigation-state/` | `agent-engineering-14-working-memory-investigation-state.md` | `agent-engineering-14-working-memory-investigation-state` | `a53d151ba051403ff5ef369e5c3860a9fbded03d` | yes | Part III | COMPLETED; Card MINOR |
| 15 | Session、Long-term Memory 与 Project Memory：事实、经验和作用域 | III | Required | M | `15-session-long-term-project-memory/` | `agent-engineering-15-session-long-term-project-memory.md` | `agent-engineering-15-session-long-term-project-memory` | `0c9465ca55e095bb1d78e71016b9c6ba357c7ac6` | yes | Part III | COMPLETED; Card MINOR |
| 16 | Knowledge Base 与 RAG：Retrieve、Filter、Rerank、Inject、Cite | III | Required | M | `16-knowledge-base-rag/` | `agent-engineering-16-knowledge-base-rag.md` | `agent-engineering-16-knowledge-base-rag` | `bf00d4e63f2f634d4b62afb5fe2ee44ae2051571` | yes | Part III | COMPLETED/PUBLISHED |
| 17 | Skill Engineering：按需加载领域方法，而不是再堆一层 Prompt | III | Required | M | `17-skill-engineering/` | `agent-engineering-17-skill-engineering.md` | `agent-engineering-17-skill-engineering` | `a59245507f83a8bc567f943fd2912271cc2efb82` | yes | Part III | COMPLETED/PUBLISHED |
| 18 | Evidence Contract：把自然语言推断变成可审计工程数据 | IV | Required | L | `18-evidence-contract/` | `agent-engineering-18-evidence-contract.md` | `agent-engineering-18-evidence-contract` | `a0d8d1b2fa5380f9a4150f72b962ac15fe11a96b` | yes | Part IV | COMPLETED/PUBLISHED |
| 19 | Permission、Approval、Human-in-the-loop 与 Sandbox | IV | Required | L | `19-permission-approval-hitl-sandbox/` | `agent-engineering-19-permission-approval-hitl-sandbox.md` | `agent-engineering-19-permission-approval-hitl-sandbox` | `73a0f628e5580226f4c65890f81372d7ededd43d` | yes | Part IV | COMPLETED/PUBLISHED |
| 20 | Budget Engineering：Token、Step、Cost 与 Latency | IV | Required | M | `20-budget-engineering-token-step-cost-latency/` | `agent-engineering-20-budget-engineering-token-step-cost-latency.md` | `agent-engineering-20-budget-engineering-token-step-cost-latency` | `59f8c44df5d10894335bf5cd97d5b27552a830fe` | yes | Part IV | COMPLETED/PUBLISHED |
| 21 | Trace、Replay 与 Failure Taxonomy：错误究竟发生在哪一层 | IV | Required | L | `21-trace-replay-failure-taxonomy/` | `agent-engineering-21-trace-replay-failure-taxonomy.md` | `agent-engineering-21-trace-replay-failure-taxonomy` | `470c362567d71aa4b7e5d951406b9af92b5b1adf` | yes | Part IV | COMPLETED/PUBLISHED |
| 22 | Eval、Golden Dataset 与 Regression：修复以后还会不会再坏 | IV | Required | L | `22-eval-golden-dataset-regression/` | `agent-engineering-22-eval-golden-dataset-regression.md` | `agent-engineering-22-eval-golden-dataset-regression` | `99bff931b02356358edd1357c2abd1c44621e720` | yes | Part IV + recheck | COMPLETED/FIXED |
| 23 | Single Agent、Subagent、Agent as Tool、Handoff 与 Multi-Agent | IV | Optional | M | absent by policy | absent by policy | — | N/A | N/A | Part IV | OPTIONAL/SKIPPED/ZERO_ASSETS |
| 24 | 为什么最终需要 Harness：横切能力由谁承载 | V | Required | L | `24-why-harness-cross-cutting-capabilities/` | `agent-engineering-24-why-harness-cross-cutting-capabilities.md` | `agent-engineering-24-why-harness-cross-cutting-capabilities` | `752a87de878830da1a7724d87d5f648d45ff3abb` | yes | Part V | COMPLETED/PUBLISHED |
| 25 | Agent Runtime vs Harness：执行内核与工程控制面 | V | Required | L | `25-agent-runtime-vs-harness/` | `agent-engineering-25-agent-runtime-vs-harness.md` | `agent-engineering-25-agent-runtime-vs-harness` | `07000ceb94dd244e5f312d7787a6c83795c47f58` | yes | Part V | COMPLETED/REPAIRED |
| 26 | Harness 的最小能力模型：Capability、Policy、Session、Trace 与 Recovery | V | Required | L | `26-harness-minimum-capability-model/` | `agent-engineering-26-harness-minimum-capability-model.md` | `agent-engineering-26-harness-minimum-capability-model` | `1ed76a3075c912e33553b4508757dd1066e7a201` | yes | Part V | COMPLETED/REPAIRED |
| 27 | Harness 的设计取舍：可替换性、复杂度、Bloat 与演化 | V | Required | M | `27-harness-design-tradeoffs/` | `agent-engineering-27-harness-design-tradeoffs.md` | `agent-engineering-27-harness-design-tradeoffs` | `6f7946b65ec4e45c687f939cce364a1bacbe69ac` | yes | Part V | COMPLETED/REPAIRED |
| 28 | 怎样把 DeepSeek Harness 当作 Evidence-first 源码教材 | VI | Required | S | `28-dsh-evidence-first-source-method/` | `agent-engineering-28-dsh-evidence-first-source-method.md` | `agent-engineering-28-dsh-evidence-first-source-method` | `c428273501482288fcd986ca0ad1818863d4675a` | yes | MISSING | COMPLETED/PUBLISHED |
| 29 | DeepSeek Harness 总图：从 Host 启动到一次 Agent Run | VI | Required | M | `29-dsh-host-to-agent-run/` | `agent-engineering-29-dsh-host-to-agent-run.md` | `agent-engineering-29-dsh-host-to-agent-run` | `817fd4dde802c6afffa2011d965382267b423aa6` | yes | MISSING | COMPLETED/PUBLISHED |
| 30 | Everything is a Plugin：插件内核如何承载 Capability 与生命周期 | VI | Required | M | `30-dsh-plugin-core/` | `agent-engineering-30-dsh-plugin-core.md` | `agent-engineering-30-dsh-plugin-core` | `edaafb279cc0c730a5be00cda3a3203d49044cbf` | yes | MISSING | COMPLETED; Gate MAJOR |
| 31 | Profile、Bundle、Provider 与 Capability Seam | VI | Required | M | `31-dsh-profile-bundle-capability-seam/` | `agent-engineering-31-dsh-profile-bundle-capability-seam.md` | `agent-engineering-31-dsh-profile-bundle-capability-seam` | `9a060c1ce91a620163a64cddd2aec446c4900fd0` | yes | MISSING | COMPLETED; Gate MAJOR |
| 32 | System Prompt Assembly 与 PromptContext：多来源 Context 怎样组成 | VI | Required | L | `32-dsh-system-prompt-assembly-prompt-context/` | `agent-engineering-32-dsh-system-prompt-assembly-prompt-context.md` | `agent-engineering-32-dsh-system-prompt-assembly-prompt-context` | `e6b6ca6dcab484e700e7608fcec51d22dc81993c` | yes | MISSING | COMPLETED; Gate/state MAJOR |
| 33 | Inbox、Turn、Step 与 Agent Loop | VI | Required | L | `33-dsh-inbox-turn-step-agent-loop/` | `agent-engineering-33-dsh-inbox-turn-step-agent-loop.md` | `agent-engineering-33-dsh-inbox-turn-step-agent-loop` | `be5d36a94db54823d64160d4bcebf01e1f7da080` | yes | MISSING | COMPLETED; Gate MAJOR |
| 34 | Append-only Session Event：Replay、Resume、Fork 与 Projection | VI | Required | L | `34-dsh-append-only-session-event/` | `agent-engineering-34-dsh-append-only-session-event.md` | `agent-engineering-34-dsh-append-only-session-event` | `3908174accd733c6bf9ee0e9141b58b168b3f93c` | yes | MISSING | COMPLETED; Gate/state MAJOR; link MINOR |
| 35 | Tool Registry 与 Tool Execution Pipeline | VI | Required | L | MISSING | MISSING | — | MISSING | no | MISSING | REQUIRED/MISSING |
| 36 | Cost、Compaction、Trace、Cancellation 与 Recovery | VI | Required | L | MISSING | MISSING | — | MISSING | no | MISSING | REQUIRED/MISSING |
| 37 | RAG、Skill、Workflow、Subagent 与 Web / Headless：核心事实和扩展映射 | VI | Required | M | MISSING | MISSING | — | MISSING | no | MISSING | REQUIRED/MISSING |
| 38 | 游戏生产问题空间：什么时候该写 Script、Rule、Workflow，什么时候才需要 Agent | VII | Required | M | MISSING | MISSING | — | MISSING | no | MISSING | REQUIRED/MISSING |
| 39 | 案例 A：Unity Compile Golden Fixture——设计一个可判定的诊断 Agent | VII | Required | L | MISSING | MISSING | — | MISSING | no | MISSING | REQUIRED/MISSING |
| 40 | 案例 B：启动性能调查——设计一个长链路、多假设 Agent | VII | Required | L | MISSING | MISSING | — | MISSING | no | MISSING | REQUIRED/MISSING |
| 41 | 从两个案例反推 BuildPilot Architecture：先找变化轴，再定模块 | VII | Required | L | MISSING | MISSING | — | MISSING | no | MISSING | REQUIRED/MISSING |
| 42 | BuildPilot 的 Context 与 Capability 设计：让知识、技能和工具各就各位 | VII | Required | L | MISSING | MISSING | — | MISSING | no | MISSING | REQUIRED/MISSING |
| 43 | BuildPilot 的治理闭环：Evidence、Policy、Session、Trace、Budget、Recovery 与 Eval | VII | Required | L | MISSING | MISSING | — | MISSING | no | MISSING | REQUIRED/MISSING |
| 44 | BuildPilot Design v1：设计评审、里程碑与退出条件 | VII | Required | S | MISSING | MISSING | — | MISSING | no | MISSING | REQUIRED/MISSING |

Totals: `45 planned / 44 required / 1 optional / 34 required complete+published / 10 required missing / 0 Article45 / 0 duplicate ID, slug, Hugo weight or series_order`。

## 6. Per-Article Artifact Contract Matrix — 45 rows

Legend：`Y`=存在且非空；`SAT`=review/revision 条件满足；`DSH`=固定源码/调用链/trace；`LabNN`=frozen observed evidence；`—`=required but absent；`OPT`=canonical optional skip。`Final/Pub/Build` 与 `Git/Remote` 分别核对发布边界和 completion/remote receipt。

| ID | Research | Claims/Evidence | Source/Call | Experiment/Raw | Merge/Gate | Outline/Draft | Review/Recheck | Final/Pub/Build | Git/Remote |
|---:|---|---|---|---|---|---|---|---|---|
| 00 | Y | Y | N/A | N/A | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y |
| 01 | Y | Y | N/A | N/A | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y |
| 02 | Y | Y | N/A | N/A | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y |
| 03 | Y | Y | N/A | Lab01 observed | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y |
| 04 | Y | Y | N/A | N/A | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y |
| 05 | Y | Y | N/A | N/A | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y |
| 06 | Y | Y | N/A | Lab02 observed | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y |
| 07 | Y | Y | N/A | N/A | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y |
| 08 | Y | Y | N/A | Lab03 observed | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y |
| 09 | Y | Y | N/A | N/A | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y |
| 10 | Y | Y | N/A | N/A | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y |
| 11 | Y | Y | N/A | Lab04 observed | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y |
| 12 | Y | Y; Card stale | N/A | N/A | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y |
| 13 | Y | Y | N/A | Lab05 observed | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y |
| 14 | Y | Y; Card stale | N/A | N/A | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y |
| 15 | Y | Y; Card stale | N/A | N/A | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y |
| 16 | Y | Y | N/A | N/A | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y |
| 17 | Y | Y | N/A | N/A | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y |
| 18 | Y | Y | N/A | N/A | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y |
| 19 | Y | Y | N/A | N/A | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y |
| 20 | Y | Y | N/A | N/A | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y |
| 21 | Y | Y | N/A | N/A | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y |
| 22 | Y | Y | N/A | Lab06 observed | Y | Y/Y | Y/hotfix recheck | Y/Y/Y | Y/Y |
| 23 | OPT | OPT | N/A | N/A | N/A | N/A | N/A | N/A | N/A |
| 24 | Y | Y | N/A | N/A | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y |
| 25 | Y | Y | N/A | N/A | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y |
| 26 | Y | Y | N/A | N/A | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y |
| 27 | Y | Y | N/A | N/A | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y |
| 28 | Y | Y | DSH baseline/source map | probes/raw Y | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y |
| 29 | Y | Y | DSH map/call path | host-run trace Y | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y |
| 30 | Y | Y | DSH map/call path | lifecycle trace Y | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y; Gate envelope invalid |
| 31 | Y | Y | DSH map/call path | config diff/raw Y | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y; Gate envelope invalid |
| 32 | Y | Y | DSH map/call path | assembly trace Y | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y; Gate envelope invalid |
| 33 | Y | Y | DSH map/call path | 4/4 required traces | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y; Gate envelope invalid |
| 34 | Y | Y | DSH map/call path; 2 bad links | replay/resume/fork Y | Y | Y/Y | Y/SAT | Y/Y/Y | Y/Y; Gate envelope invalid |
| 35 | — | — | required DSH tool path — | 5 traces — | — | — | — | — | — |
| 36 | — | — | required DSH recovery path — | long-session trace — | — | — | — | — | — |
| 37 | — | — | required extension map — | decision matrix — | — | — | — | — | — |
| 38 | — | DESIGN_PROPOSAL — | N/A production source | N/A production result | — | — | — | — | — |
| 39 | — | DESIGN_PROPOSAL — | N/A production source | N/A production result | — | — | — | — | — |
| 40 | — | DESIGN_PROPOSAL — | N/A production source | N/A production result | — | — | — | — | — |
| 41 | — | DESIGN_PROPOSAL — | N/A production source | N/A production result | — | — | — | — | — |
| 42 | — | DESIGN_PROPOSAL — | N/A production source | N/A production result | — | — | — | — | — |
| 43 | — | DESIGN_PROPOSAL — | N/A production source | N/A production result | — | — | — | — | — |
| 44 | — | DESIGN_PROPOSAL — | N/A production source | N/A production result | — | — | — | — | — |

Artifact coverage：`34 complete artifact chains / 1 optional skip / 10 required missing`。存在不等于全部技术事实逐句重验；现有 34 篇只证明结构与记录可追溯，Finding 003/005/006/008 仍适用。

## 7. Part Audit Matrix — 7 rows

| Part | Range | Required | Optional | Required complete | Missing | Durable audit | State | Compliance |
|---|---|---:|---:|---:|---|---|---|---|
| I | 01—04 | 4 | 0 | 4/4 | none | `part-i-audit.md` | PASS; historical minors closed | MET |
| II | 05—11 | 7 | 0 | 7/7 | none | `part-ii-audit.md` | PASS; four minors closed | MET |
| III | 12—17 | 6 | 0 | 6/6 | none | `part-iii-audit.md` | PASS; PIII-F03/F05 retained | PASS_WITH_FINDINGS |
| IV | 18—23 | 5 | 1 | 5/5 | none; 23 skipped | `part-iv-audit.md` + Article22 recheck | PASS | MET |
| V | 24—27 | 4 | 0 | 4/4 | none | `part-v-audit.md` | Cycle1 PASS; findings closed | MET |
| VI | 28—37 | 10 | 0 | 7/10 | 35—37 | MISSING | not eligible/not run | NOT_MET |
| VII | 38—44 | 7 | 0 | 0/7 | 38—44 | MISSING | not eligible/not run | NOT_MET |

## 8. Lab Evidence Matrix — 6 rows

| Lab | After | Fixture | Source commit | Environment | Frozen command ledger | Expected / Observed | Exit evidence | Raw/negative evidence | Hash | Status | Consumers |
|---|---:|---|---|---|---|---|---|---|---|---|---|
| Lab01 Structured Output | 03 | `lab-01-structured-output-validation/` | `857fe9fd...` | Win10; .NET SDK 10.0.301; NJsonSchema 11.6.1 | restore/build/test/runner | 8 cases; observed 8/8（1 accepted, 3 parse, 3 schema, 1 domain） | initial failures retained; final 0 | malformed/missing/type/extra/evidence/truncation/refusal | `C484C122...69CC8` matched | PASS / byte-equal | Article03 |
| Lab02 Tool Runtime | 06 | `lab-02-tool-runtime/` | `199d4e19...` | Win10; .NET SDK 10.0.301 | restore/build/spec/runA/runB/verifier | 12 groups/14 invocations; all matched | final required commands 0; earlier failures retained | invalid/deny/traversal/timeout/cancel/large/replay | `50CEA4EC...1BD67` matched | PASS | Article06 |
| Lab03 Minimal Agent Loop | 08 | `lab-03-minimal-agent-loop/` | raw `10452640...`; completion `d4693bd6...` | .NET SDK 10.0.301; BCL-only | restore/build/spec/runA/runB/verifier | success/tool failure/max-step/repeat stop; 4 cases/10 steps | all required 0 | missing/fake evidence/budget/pseudo-completion | runA/B 6/6 byte-equal | VERIFIED | Article08 |
| Lab04 State Machine + Checkpoint | 11 | `lab-04-state-machine-checkpoint/` | `31aef0aa...` | Win10; .NET SDK 10.0.301 | 12 fresh processes/suite | LR01—LR08; observed 8/8 | RED/static failures retained; accepted commands 0 | cancel/retry/lost response/unsafe replay/missing inflight/exhausted | normalized `27890bd8...9b9a`; 105 equal + run-root sentinel | CONFIRMED | Article11 |
| Lab05 Context Debugging | 13 | `lab-05-context-debugging/` | `8b18b85b...` | Win10; .NET SDK 10.0.301; offline BCL | RED/GREEN/runA/runB/verifier | A—G; GREEN 15/15; closure 8/8 | RED failure retained; closure 0 | stale/pollution/conflict/lossy/overflow/rebuild | 59/59; `621cde0e...3f50` | PASS | Article13 |
| Lab06 Trace + Eval | 22 | `lab-06-trace-eval/` | raw `99bff931...`; addendum `481ebd52...` | Win10; PS7.6.4; .NET SDK 10.0.301 | restore/build/red/green/evaluate/verify | RED 0/5; GREEN 5/5; baseline 8/8; regression 7/8 FAIL; missing UNKNOWN; mismatch INCOMPARABLE | RED 1; closure 0; negative native 2/3 | regression/missing/mismatch + hashes | `hashes.sha256` 10/10 | VERIFIED | Article22 |

全部 Lab 为独立 C#/.NET fixture，不连接真实 Unity/Jenkins/生产系统，不写 BuildPilot Runtime；Expected 与 Observed 分离，失败与 negative case 保留。Lab04 仅排除文档声明的 run-specific `.lab04-run-root` sentinel。G6 的 MINOR 只影响聚合索引，不改变 frozen evidence。

## 9. DSH Article 28—37 Matrix

Baseline：official `https://github.com/deepseek-ai/deepseek-harness`；tag `dsh-v0.1.2-alpha.1`；commit `cd5ef8148158c3a752a658978873241fdf8e2bbc`。审计时 remote tag 仍精确解析到该 commit；fixture origin/HEAD/tag clean，一致性成立；课程仓库未 vendor DSH tree。

| Article | Focus | Workspace/Published | Pinned | Source/call path | Required trace | Boundary/safety | State |
|---:|---|---|---|---|---|---|---|
| 28 | evidence-first baseline | present/present | exact | source map + 12 cards | structured probes；full suite failure/caveat retained | Developer Preview；not audited/production ready；sandbox risks explicit | MET |
| 29 | Host -> Agent Run | present/present | exact | repo map + bounded path | deterministic fixture；negative/credential limits retained | mock vs real Provider separated | MET |
| 30 | plugin core | present/present | exact | map + lifecycle call path | representative lifecycle/owner tests | no real Provider/network/token/cost claim | MET |
| 31 | Profile/Bundle/Provider seam | present/present | exact | map + config/capability path | effective-config diff + negatives | config != activation；OS confinement untested | MET |
| 32 | prompt assembly/context | present/present | exact | map + assembly path | two-Step mock diff + negatives | provenance/redaction；mock != provider | MET |
| 33 | Inbox/Turn/Step/Loop | present/present | exact | loop/cancel path | no-tool/single/multi/cancel = 4/4；owner tests 10/10 | stop != success；cancel != rollback | MET |
| 34 | Session Event/Replay/Resume/Fork | present/present | exact | map/write/read/projection；2 broken links | 12 selected tests PASS；PARTIAL ceilings retained | external world/permission/budget not overclaimed | MET_WITH_MINOR |
| 35 | Tool execution pipeline | missing/missing | no article recheck | missing | bad args/deny/timeout/cancel/large result = 0/5 | no learner surface | NOT_MET |
| 36 | Cost/Compaction/Cancel/Recovery | missing/missing | no article recheck | missing | long session -> compact -> cancel -> resume absent | no learner surface | NOT_MET |
| 37 | RAG/Skill/Workflow/Subagent/Web | missing/missing | no article recheck | missing | ADOPT/SIMPLIFY/REJECT/DEFER matrix absent | no Part VII consumption | NOT_MET |

安全/许可 subset：pinned release 的 pre-release/developer-preview、安全警告、sandbox/approval 非绝对保证、credential/network/process/file 风险、MIT/THIRD_PARTY/lockfile 归属均在现有 28—34 中保留；不等于完成全依赖法律意见或真实 Provider runtime 验证。

## 10. BuildPilot Article 38—44 Matrix

| Article | Required role/delivery | Workspace/Published/Completion | Existing learner state | Boundary result | State |
|---:|---|---|---|---|---|
| 38 | Script/Rule/Workflow/Agent decision matrix；C#/.NET CLI modular monolith；single supervised Agent | 0/0/0 | 设计规划；PLANNED/PROPOSAL | 无 runtime overclaim；必需 lesson absent | NOT_MET |
| 39 | Case A Unity Compile Golden Fixture；2022.3.62f3；public-safe；hidden answer split；real Unity compile | 0/0/0 | 设计规划；fixture 未实现 | 诚实未完成；package absent | NOT_MET |
| 40 | Case B Startup Investigation；synthetic/anonymized；multi-hypothesis；no ROI claim | 0/0/0 | 设计规划；fixture 未实现 | 诚实未完成；package absent | NOT_MET |
| 41 | two-Golden-case architecture；Scenario C coverage；module/trust/sequence responsibility | 0/0/0 | 设计规划 | design matrix/diagram absent | NOT_MET |
| 42 | Context/Capability；Tool/Skill/Workflow/Context Source/Memory/Contract；governed evolution | 0/0/0 | Design v1 only | delivery absent | NOT_MET |
| 43 | Evidence/Policy/Session/Trace/Budget/Recovery/Eval/Change Request/Human Review；六层模型 | 0/0/0 | Design v1 only | synthesis absent | NOT_MET |
| 44 | 13-item Design v1 review；final DSH matrix；ADR/risk/open；traceability；M0—M3/exit | 0/0/0 | 等待前置 | final package/review absent | NOT_MET |

现有 learner-facing surfaces 未发现正向 BuildPilot Runtime/生产能力/自动修改/部署/审批 overclaim；Article45、PartVIII、BuildPilot Runtime/source、Unity plugin assets 均为 0。Meta Auditor 驳回了把 plan 未逐字复制全部 G8 rubric 另计为第五条 MAJOR 的建议；G8 rubric 仍是未来 Article 38—44 的强制验收条件。

## 11. Build, links, navigation, security and license

| Area | Result | Evidence boundary |
|---|---|---|
| Hugo production build | MET | Hugo 0.157.0 extended；exit 0；1262 Pages / 44 Static / 1306 output files；aggregate manifest `4d7754ec...a6312b` |
| Current internal targets/anchors | MET | homepage + course index/alias + 34 Articles，共 37 learner pages / 1248 rendered refs / 0 missing targets or fragments |
| Course reachability | MET for present pages | index 34 unique Article links；homepage 1 course entry；34/34 Article previous/next markers；Article23 output absent |
| Required publication completeness | NOT_MET | Article35—44 source/output page absent |
| External URLs | NOT_VERIFIABLE globally | 295 unique off-site URLs未完成可靠全量状态复核；Article34 两条 pinned links 单独证实 404 |
| Frontmatter/route | MET for present pages | 34 rows；0 duplicate course slug/weight；0 course draft=true |
| Current-tree secret subset | MET within scan | 2357 text files；高置信 private-key/AWS/OpenAI/GitHub/Slack/Google=0；generic candidates为叙述/代码假阳性 |
| Private IP | MET within scan | focused course text private IPv4=0 |
| Local absolute paths | NOT_MET / MINOR | tracked evidence/workspaces 有 53 user-profile + 44 local-workspace hits；Published Content=0；值未写入报告 |
| CI provenance | NOT_MET / MINOR | Hugo version/command fixed；`ubuntu-latest` 与 5 个 Actions 使用 mutable tags |
| DSH license/no vendoring | MET subset | pinned MIT/license facts traceable；DSH tree 未 vendor |
| Repository-wide license | NOT_VERIFIABLE / NOTE | 无 root/nested LICENSE/NOTICE；未建立全资产/传递依赖 rights clearance；未发明 SBOM mandatory control |
| Agentic trust boundary | PARTIAL / NOT_VERIFIABLE globally | prompt injection、provenance、trace sensitivity、replay side effects有覆盖；完整恶意 repo/path/archive/parser 等语义覆盖未证实，且 35—44 缺失 |

## 12. Consolidated Findings Register

以下 12 条已由 fresh Meta Auditor 去重。`COURSE-AUDIT-001` 合并六路对“35—44 缺失”的重复；BuildPilot `BP-002` 因证据不足以构成独立违规而被驳回。

```yaml
- id: COURSE-AUDIT-001
  audit_target_sha: 3908174accd733c6bf9ee0e9141b58b168b3f93c
  criterion_id: G1.REQUIRED_ARTICLES_G4_G5_G7_G8_G9
  criterion_state: NOT_MET
  mandatory: true
  severity: MAJOR
  gate: G1/G4/G5/G7/G8/G9
  title: Ten required Articles 35-44 and their type-specific deliverables are absent
  expected: 44 required Articles have canonical workspace, type-specific evidence, publication, completion and DSH/BuildPilot deliverables; only 23 may be skipped.
  actual: 35-44 have zero workspace/publication/completion, including Article35/36 traces, Article37 matrix, 38-44 design packages and Article44 Design v1.
  evidence: [series-plan.md:213-238,255-283, status.md:120-129, target inventory commands exit0, hugo list all exit0]
  impact: Whole-course production, evidence, publication, Part closure and final compliance cannot pass.
  affected_articles_or_labs: [35,36,37,38,39,40,41,42,43,44]
  owner_role: COURSE_FACTORY_MASTER plus per-Article roles
  repair_scope: Sequentially complete only 35-44; preserve Article23 skip, pinned DSH, frozen Labs and no-Runtime/no-45/no-PartVIII boundary.
  verification_command_or_criteria: 45-row matrices, type-special evidence, completion/remote, Hugo/links, Article37 matrix and Article44 Design v1 all pass.
  dependencies: COURSE-AUDIT-003, COURSE-AUDIT-004, then 35->36->37->PartVI Audit->38..44->PartVII Audit
  confidence: HIGH
  evidence_strength: E3_DIRECT

- id: COURSE-AUDIT-002
  audit_target_sha: 3908174accd733c6bf9ee0e9141b58b168b3f93c
  criterion_id: G3.PART_AUDIT_COMPLETENESS
  criterion_state: NOT_MET
  mandatory: true
  severity: MAJOR
  gate: G3
  title: Mandatory Part VI and Part VII audit checkpoints are absent
  expected: Part I-VII each has a durable independent audit after eligibility.
  actual: Part I-V reports exist; Part VI/VII report and audit commits do not.
  evidence: [course-factory.md:459-495, audit-path and commit-subject enumeration exit0]
  impact: DSH cross-article and BuildPilot design closure are not independently established.
  affected_articles_or_labs: [PartVI 28-37, PartVII 38-44]
  owner_role: fresh independent PART_AUDITOR
  repair_scope: Part VI audit after 37; Part VII audit after 44; no historical audit changes.
  verification_command_or_criteria: audit-only remote-reachable reports reconcile all criteria with no open BLOCKER/MAJOR before PASS.
  dependencies: COURSE-AUDIT-001
  confidence: HIGH
  evidence_strength: E3_DIRECT

- id: COURSE-AUDIT-003
  audit_target_sha: 3908174accd733c6bf9ee0e9141b58b168b3f93c
  criterion_id: G2.TRANSACTION_DURABLE_GATE_ENVELOPES
  criterion_state: NOT_MET
  mandatory: true
  severity: MAJOR
  gate: G0/G2/G3/G11/G12
  title: Articles 30-34 advanced past invalid or missing deterministic Gate envelopes
  expected: MASTER_STATE_UPDATE and PRE_COMMIT_RECONCILIATION retain exact closed 11-field result; invalid/missing cannot become PASS/authority.
  actual: 30/31 omit notes; 32 uses prose; 33 lacks valid Master record; 34 lacks Master record and pre-commit artifact lists; run-state still records PASS/Article35 continuation.
  evidence: [subagent-contracts.md:14-54,87-115, Article30 trace:334-353, Article31 trace:389-408, Article32 trace:337-351, Article34 trace:408-424, run-state.md:11-46, closed-schema scan exit0]
  impact: Gate authorization, recoverability and Article35 continuation authority are not trustworthy although eventual Git outcomes exist.
  affected_articles_or_labs: [30,31,32,33,34]
  owner_role: MASTER_ORCHESTRATOR
  repair_scope: Append truthful bounded dispositions preserving MISSING/INVALID; never fabricate history or rerun frozen evidence.
  verification_command_or_criteria: Closed-schema parser, valid result_ref, historical omissions explicit, fresh Part VI eligibility audit.
  dependencies: separate human-authorized targeted repair
  confidence: HIGH
  evidence_strength: E3_DIRECT

- id: COURSE-AUDIT-004
  audit_target_sha: 3908174accd733c6bf9ee0e9141b58b168b3f93c
  criterion_id: G3.GLOBAL_CURRENT_STATE_PARITY
  criterion_state: NOT_MET
  mandatory: true
  severity: MAJOR
  gate: G0/G3/G12
  title: Current run-state, status, README and Git completion facts contradict one another
  expected: Current surfaces expose one Git-reconciled pointer or fail closed.
  actual: run-state says Article35/last34; status routes Article32 and says Article34 pending; README lists 28-34 progress but also calls Article28 forbidden/not-started/zero-assets.
  evidence: [course-run-state.md:11-46, status.md:63,72,113-129, README.md:85-100,152-157, git show target]
  impact: Resume may route to the wrong transaction or accept invalid READY state.
  affected_articles_or_labs: [28,29,30,31,32,33,34,FactoryState]
  owner_role: MASTER_ORCHESTRATOR
  repair_scope: After 003, reconcile current-only statements in status/run-state/README; preserve history.
  verification_command_or_criteria: One target-bound resolver/current pointer agrees with Git and all three files.
  dependencies: COURSE-AUDIT-003
  confidence: HIGH
  evidence_strength: E3_DIRECT

- id: COURSE-AUDIT-005
  audit_target_sha: 3908174accd733c6bf9ee0e9141b58b168b3f93c
  criterion_id: G4.ARTICLE_CARD_CURRENT_METADATA
  criterion_state: NOT_MET
  mandatory: true
  severity: MINOR
  gate: G4
  title: Article 12/14/15 Cards retain stale canonical/lifecycle metadata
  expected: Cards match canonical Part and current Research/Evidence state.
  actual: Article12 Part label non-canonical; Article14/15 still NOT_STARTED/BLOCKED despite completed assets/publication.
  evidence: [Article12 card:8, Article14 card:38-39, Article15 card:40-41, part-iii-audit.md:283-293]
  impact: Metadata misstates completed Articles; substantive evidence remains.
  affected_articles_or_labs: [12,14,15]
  owner_role: Course metadata owner
  repair_scope: Exact Card metadata fields only.
  verification_command_or_criteria: Cards match canonical/current artifacts; PIII-F03 closes.
  dependencies: []
  confidence: HIGH
  evidence_strength: E3_DIRECT

- id: COURSE-AUDIT-006
  audit_target_sha: 3908174accd733c6bf9ee0e9141b58b168b3f93c
  criterion_id: G9.COURSE_NAVIGATION_SUMMARY_PARITY
  criterion_state: NOT_MET
  mandatory: false
  severity: MINOR
  gate: G6/G9
  title: Course Index and Lab05 summary retain temporal/evidence-ceiling drift
  expected: Summaries use current wording without overstating fixture evidence.
  actual: index says Article13 will explain later after publication and frames Lab05 as reconstruction; Lab index overstates result relation.
  evidence: [series-index.md:48,196, labs/README.md:13, part-iii-audit.md:283-293]
  impact: Minor learner ambiguity; frozen Lab05 unchanged.
  affected_articles_or_labs: [13,Lab05]
  owner_role: Course navigation editor
  repair_scope: Exact summary phrases only.
  verification_command_or_criteria: PIII-F05 closes; Hugo/link pass.
  dependencies: []
  confidence: HIGH
  evidence_strength: E3_DIRECT

- id: COURSE-AUDIT-007
  audit_target_sha: 3908174accd733c6bf9ee0e9141b58b168b3f93c
  criterion_id: G6.LAB_INDEX_TRACEABILITY
  criterion_state: NOT_MET
  mandatory: false
  severity: MINOR
  gate: G6
  title: Aggregate Lab index labels Lab01-04 PLANNED/BLOCKED and omits links
  expected: Index links instantiated Labs and matches frozen receipts.
  actual: Lab01-04 remain unlinked/PLANNED/BLOCKED while canonical and receipts say executed/verified/merged.
  evidence: [labs/README.md:7-14, series-plan.md:242-253, six-row Lab matrix]
  impact: Readers/auditors can be misdirected; frozen evidence remains valid.
  affected_articles_or_labs: [Lab01,Lab02,Lab03,Lab04,Article03,Article06,Article08,Article11]
  owner_role: Course Lab index publisher
  repair_scope: Index links/status only; no raw/generated/hash changes.
  verification_command_or_criteria: Six rows agree; no raw diff; Hugo passes.
  dependencies: []
  confidence: HIGH
  evidence_strength: E3_DIRECT

- id: COURSE-AUDIT-008
  audit_target_sha: 3908174accd733c6bf9ee0e9141b58b168b3f93c
  criterion_id: G7.PINNED_SOURCE_DEEP_LINK_TRACEABILITY
  criterion_state: NOT_MET
  mandatory: false
  severity: MINOR
  gate: G7/G9
  title: Article34 publishes two nonexistent pinned-commit source paths
  expected: Fixed-commit DSH links resolve at mandated commit.
  actual: Two paths absent/HTTP404; exact candidate paths exist/HTTP200.
  evidence: [Article34 published:202,409, 44-path scan exit0=2 absent, HTTP check 404/404 vs 200/200]
  impact: Two source anchors不可复现；adjacent evidence仍支持 bounded claims。
  affected_articles_or_labs: [34]
  owner_role: Article34 author/editor
  repair_scope: Only two URLs and parity draft if required.
  verification_command_or_criteria: URLs resolve; pinned-tree missing=0; Hugo/parity pass.
  dependencies: []
  confidence: HIGH
  evidence_strength: E3_DIRECT

- id: COURSE-AUDIT-009
  audit_target_sha: 3908174accd733c6bf9ee0e9141b58b168b3f93c
  criterion_id: G10.LOCAL_PATH_PRIVACY
  criterion_state: NOT_MET
  mandatory: true
  severity: MINOR
  gate: G10
  title: Tracked internal evidence retains user-profile and machine-local absolute paths
  expected: Public repository normalizes machine/user topology unless reviewed exception is necessary.
  actual: Redacted scan found 53 user-profile hits/34 files and 44 workspace hits/20 files; learner-facing=0; no secret/private IP established.
  evidence: [redacted aggregate scan exit0; values intentionally omitted]
  impact: Public repository exposes local topology outside rendered lessons.
  affected_articles_or_labs: [PartVI workspaces,multiple frozen Labs]
  owner_role: Evidence privacy owner
  repair_scope: Human policy decision; documented exception or authorized derived/redacted evidence; no silent raw rewrite.
  verification_command_or_criteria: Repeat redacted scan; every retained hit has approved disposition.
  dependencies: human privacy-vs-frozen-evidence decision
  confidence: HIGH
  evidence_strength: E3_DIRECT

- id: COURSE-AUDIT-010
  audit_target_sha: 3908174accd733c6bf9ee0e9141b58b168b3f93c
  criterion_id: G10.CI_PROVENANCE_PINNING
  criterion_state: NOT_MET
  mandatory: true
  severity: MINOR
  gate: G10/G11
  title: CI deployment dependencies use mutable runner/action labels
  expected: Executable CI dependencies are immutably identifiable.
  actual: Hugo 0.157.0/command fixed, but ubuntu-latest and five Actions use mutable tags.
  evidence: [.github/workflows/hugo.yaml:24-52]
  impact: Same source may later execute different action code; no compromise established.
  affected_articles_or_labs: [Site publication pipeline]
  owner_role: CI maintainer
  repair_scope: Workflow only; reviewed full SHAs; no content/Hugo change.
  verification_command_or_criteria: Immutable actions; same Hugo production build passes.
  dependencies: maintainer-approved revisions
  confidence: HIGH
  evidence_strength: E3_DIRECT

- id: COURSE-AUDIT-011
  audit_target_sha: 3908174accd733c6bf9ee0e9141b58b168b3f93c
  criterion_id: G11.EXECUTABLE_FACTORY_VALIDATION
  criterion_state: NOT_VERIFIABLE
  mandatory: false
  severity: NOTE
  gate: G11
  title: Course Factory resolver/envelope validation has no repository-owned executable
  expected: If validators exist they are target-bound and their blind spots inspectable.
  actual: No course validator/resolver/link/audit/Hugo wrapper; manual checks and canonical Hugo used.
  evidence: [validator-like script enumeration exit0, course-factory.md:196-219, subagent-contracts.md:87-115]
  impact: Regression detection relies on ad hoc parsing; optional tooling is not made mandatory.
  affected_articles_or_labs: [Course Factory tooling]
  owner_role: Course Factory maintainer
  repair_scope: Optional read-only target-bound validator.
  verification_command_or_criteria: Explicit SHA input; fail-closed schema/state; machine output; no mutation.
  dependencies: []
  confidence: HIGH
  evidence_strength: E3_DIRECT

- id: COURSE-AUDIT-012
  audit_target_sha: 3908174accd733c6bf9ee0e9141b58b168b3f93c
  criterion_id: G10.REPOSITORY_LICENSE_COVERAGE
  criterion_state: NOT_VERIFIABLE
  mandatory: false
  severity: NOTE
  gate: G10
  title: Repository-wide license/attribution coverage is not centrally established
  expected: Reuse/attribution boundaries are discoverable when policy requires.
  actual: No LICENSE/NOTICE; selected DSH/NJsonSchema facts recorded and DSH not vendored; exhaustive rights clearance absent; no canonical SBOM/root-license mandate.
  evidence: [license inventory exit0, Article28 baseline-manifest:23, Article03 research:118-124/evidence:47]
  impact: Downstream reuse policy unclear; infringement not asserted.
  affected_articles_or_labs: [Whole repository]
  owner_role: Repository/legal-policy owner
  repair_scope: Human policy decision and bounded attribution inventory if adopted.
  verification_command_or_criteria: Approved policy and traceable redistributed third-party attributions.
  dependencies: human license decision
  confidence: HIGH
  evidence_strength: E2_CORROBORATED
```

## 13. NOT_PROVABLE_FROM_AVAILABLE_EVIDENCE

Count: `9`.

1. 每篇历史上只 push 一次。
2. 未持久化、未提供外部日志的 post-commit runtime envelopes 是否存在且正确。
3. 295 个 external URL 的课程级当前可达性/最终域名（Article34 两条直接检查除外）。
4. reachable Git history、binary、obfuscated、ignored、unknown format 中全面无 secret。
5. 每张图、引用、diagram、Lab/transitive dependency 的完整许可/归属法律清查。
6. 缺失与现有课程对全部 G10.3 trust-boundary 主题的完整语义覆盖。
7. 当前线上 GitHub Pages 与本地冻结渲染结果完全一致。
8. Lab01—06 的本轮 fresh reproduction；本轮只审 frozen evidence/hash/receipt。
9. mutable runner/action 条件下 Linux/future CI 的 byte-identical reproduction。

## 14. Audit limitations

Count: `11`.

1. 外链全量审核未完成，不能从 Article34 两条检查泛化。
2. 未安装 trusted full secret scanner；当前树保守规则有盲点。
3. history/binary/obfuscated secret assurance 未完成。
4. 无 canonical license checker、NOTICE inventory、SBOM 或法律政策。
5. trust-boundary 全语义覆盖未建立，且 35—44 缺失。
6. 无 repository-owned Factory resolver/envelope/link/security/license/final-audit validator。
7. Lab 未重跑；Windows archive CRLF 差异通过 target-identical clean worktree + `git diff --quiet` 后按原 blob/bytes 复核规避。
8. DSH build/test/article experiment 未重跑；remote tag equality 只是审计时刻事实。
9. DSH selected-output 记录不证明真实 Provider/model/network/token/cost 行为。
10. 未检查 live deployed site。
11. 与课程无关的旧 `.claude` worktrees/non-main refs 在确认无 course-path changes 后排除，其更广内容未裁决。

## 15. Bounded remediation batches — plan only

### Batch 0 — Transaction truth and current-state reconciliation（priority 0）

- Findings：003、004。
- Allowed：Article30—34 `subagent-trace.md` 仅追加 truthful disposition；`status.md`、`course-run-state.md`、course `README.md`；若授权合同要求，可包含受影响 Article README。
- Forbidden：Published lessons、DSH raw/experiment、Labs、completion commit rewrite、baseline migration。
- Dependency：新的 human targeted-repair 授权与 exact allowlist。
- Verify：11-field parser、MISSING/INVALID 保留、target-bound resolver、唯一 current pointer、Part VI eligibility。
- Done：continuation 不再依赖 invalid record，current surfaces 一致。

### Batch 1 — Required course production（priority 1）

- Finding：001。
- Allowed：一次只做一篇，35 -> 36 -> 37；Part VI closure 后 38 -> ... -> 44。
- Forbidden：23、45、Part VIII、BuildPilot Runtime/plugin、DSH migration、frozen Labs、bulk completion。
- Dependency：Batch0；固定 DSH；Part VI Audit before 38；本报告 G8 验收矩阵。
- Verify：per-Article completion、type-specific evidence/trace、review、Hugo、links、remote。
- Done：44/44 required；Article37 final matrix；Article44 Design v1；无 Runtime overclaim。

### Batch 2 — Part VI / VII independent audits（priority 2）

- Finding：002。
- Allowed：`part-vi-audit.md`、`part-vii-audit.md` 及 Factory 明确允许的 audit checkpoint projection。
- Forbidden：audit turn 内修改 Article/Lab/evidence；覆盖 Part I—V history。
- Dependency：Article37 / Article44 分别完成。
- Verify：audit-only commit、no open BLOCKER/MAJOR、Hugo/link/remote、DSH matrix consumption、G8 boundary。

### Batch 3 — Traceability/metadata cleanup（priority 3）

- Findings：005—008。
- Allowed：3 个 Article Card、series index、Lab README、Article34 published + parity final draft（如合同要求）。
- Forbidden：Lab raw/generated/hash、Evidence Cards、DSH trace、技术主张重写。
- Verify：PIII-F03/F05 close；Lab01—04 rows align；2 URLs resolve；Hugo/link；raw diff=0。

### Batch 4 — CI provenance pinning（priority 4）

- Finding：010。
- Allowed：`.github/workflows/hugo.yaml` only。
- Forbidden：content/Hugo version/course state。
- Verify：reviewed immutable action SHAs；同一 Hugo production build 与部署路径通过。

### Batch 5 — Privacy/license/tooling decision（priority 5, decision-first）

- Findings：009、011、012。
- Allowed before decision：NONE；后续必须另冻 exact allowlist。
- Forbidden：静默改 frozen raw evidence、发明法律清查、弱化 Evidence contract、下载不受信 scanner。
- Verify：approved privacy/license policy；redacted path scan；若采用 attribution inventory/validator，则 target-bound/fail-closed/non-mutating。

## 16. Command and tool receipt summary

| Command/check | Working scope | Exit/result |
|---|---|---|
| `git fetch --prune origin` | course repository, before freeze | 0 |
| branch/status/index/untracked checks | course repository | 0；main；clean |
| local HEAD / origin/main / live `ls-remote` | course repository + remote | 0；all `3908174...` at freeze |
| `git archive --format=tar ... 3908174...` | target Git object -> temp | 0 |
| submodule/LFS/symlink/ignored inventory | target/repository | 0；0/0/0/21 |
| completion subject + commit containment/scope scans | target Git objects | 0；00—22,24—34 completion；35—44 absent |
| closed 11-field Gate-envelope scan | snapshot | 0 command; criterion NOT_MET for 30—34 |
| `hugo version` | audit environment | 0；0.157.0 extended |
| production `hugo --gc --minify` with temp destination/cache | isolated snapshot | 0；1262 Pages / 44 Static / 0 ERROR |
| report-validation Hugo sandbox attempt | working tree + temp output | 1；process launch denied；not used as build evidence |
| report-validation Hugo approved host retry | working tree + temp output | 0；1262 Pages / 44 Static / 1 Alias / 0 ERROR |
| `hugo list all` | isolated snapshot | 0；34 course Articles + 1 course index；course drafts 0 |
| rendered internal target/fragment checker | temp public output | 0；37 pages / 1248 refs / 0 broken |
| production output SHA-256 manifest | temp public output | 0；1306 files；`4d7754ec...a6312b` |
| DSH local fixture identity/cleanliness | existing temp fixture | 0；official/clean/`cd5ef814...` |
| DSH remote tag `ls-remote` | official GitHub remote | 0；tag -> `cd5ef814...` |
| Article34 pinned path + HTTP checks | pinned tree/GitHub | 0 command；2 absent/404；candidate 200 |
| redacted current-tree credential scan | snapshot 2357 text files | 0；high-confidence hits 0 |
| private IP/local path scan | 943 course text files | 0；private IP 0；local-path Finding 009 |
| license/scanner/lockfile inventory | snapshot/toolchain | 0；scanner unavailable/not installed；LICENSE/NOTICE 0；course lockfiles 10 |
| report-write preflight `git ls-remote` sandbox attempt | repository sandbox | 1；SSH/network denied；not evidence |
| report-write preflight `git ls-remote` approved read-only retry | live remote | 0；still `3908174...` |

工具不可用、网络未全查、失败命令均未被改写为 PASS。构建/临时输出全部位于隔离临时目录，不进入课程仓库。

## 17. Auditor envelopes and Meta reconciliation

六个 fresh 专项角色分别覆盖 Repository/Factory、Inventory/Publication、Evidence/Lab、DSH、BuildPilot、Build/Link/Security/License；随后 fresh Meta Auditor 只读六份 envelope 与必要 target evidence。Meta 合并了六路同根的 required-article 缺失，保留 Part Audit 缺失为独立控制项，确认 003/004 为独立 MAJOR，接受 Article34 links / local paths / CI tags 为 MINOR，驳回 BuildPilot `BP-002` 的独立 MAJOR 计数。

Evidence envelopes 是本轮临时审计工作数据，不作为课程事实源，也不提交；本报告中的所有结论均回指冻结目标文件、Git object、remote ref 或带 exit code 的命令结果。

## 18. Final reconciliation

- Audit Completeness：`PARTIAL`。
- Compliance Verdict：`NON_COMPLIANT`，因为 `PARTIAL + mandatory NOT_MET`。
- Findings：`0 BLOCKER / 4 MAJOR / 6 MINOR / 2 NOTE`，同根问题只计一次。
- 没有无证据断言被提升为 BLOCKER/MAJOR；missing evidence 从未解释为 PASS。
- 审计目标在报告写入前仍为 local/origin/live `3908174accd733c6bf9ee0e9141b58b168b3f93c`，仓库无其他 tracked/index writes。
- 本报告是唯一允许写入；没有 Article、Lab、Evidence、state、navigation、CI 或 Runtime 修复。
- 下一允许动作：完成本报告的 exact-file commit、single push、remote verify 后停止；任何 remediation 都需要新的明确授权。
