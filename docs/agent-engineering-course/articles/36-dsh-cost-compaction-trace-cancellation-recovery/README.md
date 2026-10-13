# Article 36｜Cost、Compaction、Trace、Cancellation 与 Recovery

- Canonical ID: `36`
- Workspace: `36-dsh-cost-compaction-trace-cancellation-recovery`
- Part: `VI｜DeepSeek Harness`
- Course Weight: `L`
- Optional: `NO`
- Article Type: `SOURCE_TRACE / REQUIRED_SOURCE_EXPERIMENT`
- Lifecycle Status: `PAUSED / RECOVERY_CYCLE_10 / NOT_PUBLISHED`
- Evidence Status: `SOURCE_MAP PASS / REQUIRED RUNTIME EVIDENCE BLOCKED`
- Required Evidence Work: `HOOK MAP / USAGE / LONG SESSION / COMPACTION / CANCEL-RESUME TRACE`
- Mode: `DSH_SOURCE_MODE`
- Current Gate: `LAB_EXECUTE / RECOVERY CYCLE 10`
- Active Worker: `NONE`
- Review Cycle: `1`
- Findings: `A36-C2-RR-F01 / A36-C2-RR-F02 CLOSED`; `A36-C2-FR-F01—F05 TRUTHFULLY_DISPOSED`; `A36-C6-RR-F03 CLOSED BY FACTUAL STATE TRANSITION`; `A36-C7-DR-F01—F09 + A36-C7-RR-F10 + A36-C7-RR2-F11/F12 CLOSED`; `A36-C8-DR-F01—F06 / RR1-F07 / RR2-F08 CLOSED`; `A36-C9-DR-F01—F03 + RR-F04/F05 + FR-F01 CLOSED / 0 OPEN`
- Blocker: `BLOCKED_EVIDENCE / HUMAN_DECISION_REQUIRED`; Cycle10 outcome and invocation budget are unknown. Required article runtime evidence remains BLOCKED. Cycle9 command/exit/error remains `NOT_DURABLY_PROVABLE`; historical failures immutable.
- Expected Completion Message: `Publish Agent Engineering Article 36`
- Next Allowed Action: `RECONCILE_CYCLE10_OUTCOME_BEFORE_ANY_EXECUTION`; no current experiment authorization. The historical preflight PASS does not establish execution or authorize a retry.

## Recovery checkpoint — 2026-10-13

2026-10-13 Recovery checkpoint：用户授权保存并推送现有课程资料，不包含继续实验或发布 Article36。仓库中的最后一条 Cycle10 Probe 记录停在 `Dispatch RUNNING`，没有完成结果或 observation；只读检查确认固定 TEMP root 存在但为空。无法据此证明 caller 是否曾启动、预算是否消耗或 Probe 成败，均保持 `UNKNOWN / NOT_DURABLY_PROVABLE`。Factory 当前为 `PAUSED / BLOCKED_EVIDENCE / HUMAN_DECISION_REQUIRED / active NONE`，Article36 保持 `NOT_PUBLISHED`；`current_gate=LAB_EXECUTE` 表示未闭合的历史执行阶段，恢复前必须先核对既有结果与预算，不允许据此重跑。历史 preflight PASS 仅作为最后已保存的 worker result 保留，不代表当前执行授权。旧失败现场、方案、脚本和原始记录不修改、不清理；Article37—44 不启动。

## Frozen baseline

- Repository: `https://github.com/deepseek-ai/deepseek-harness`
- Tag: `dsh-v0.1.2-alpha.1`
- Commit: `cd5ef8148158c3a752a658978873241fdf8e2bbc`
- Fixture: `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`
- Current recovery source copy (2026-09-07 authorized): `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814-cycle10-restored-20260907`; same pinned commit, pristine source, dependencies NOT_INSTALLED. Original fixture retained with 2936 missing tracked files; cause UNKNOWN.

## Evidence boundary

- Source existence is not runtime execution; tests and mocks do not establish production guarantees or provider billing truth.
- Compaction is not Memory, Trace is not Session, Cancel is not Rollback, and Resume does not restore external side effects.
- Article 37 remains not started; Article 38—44 remain forbidden and zero-assets.

## Workspace artifacts

- [Article Card](article-card.md)
- [Research](research.md)
- [Evidence](evidence.md)
- [Repository Map](repository-map.md)
- [Call Path](call-path.md)
- [Outline](outline.md)
- [Review](review.md)
- [Subagent Trace](subagent-trace.md)

Research=`PASS / 16 RQ / 17 preliminary Claims+Cards`；Source Map/Call Path=`PASS`。36-X01 Attempt 1=`exit 1 / Vitest NOT STARTED / selection 0 / P0-P5 NOT OBSERVED`，15项失败raw与14/14 manifest继续保留，fixture clean。2026-09-01 Human Resume只授权一次启动器恢复设计：使用fixture-local `.bin`、Corepack解析仓库声明的pnpm `11.7.0`，并复用exact archived harness；不得覆盖旧失败、安装或更新依赖、改变实验语义或自行增加第二次恢复。

Recovery Cycle 1已耗尽唯一授权attempt：Corepack pnpm=`11.7.0`、Vitest selection=`1`，但harness在P0 correlation读取undefined `.data`并exit=`1`；P1—P5未观察。test-owned runtime raw转存失败且清理后源root已删除，Recovery manifest另有1项bytes/SHA不一致，correlation failure receipt也不满足closed-value合同。Fresh independent review=`Spec FAIL / Evidence integrity FAIL / State parity FAIL`；Master已把current boundary闭合为`PAUSED / FAILED_LAB / HUMAN_DECISION_REQUIRED`。没有新的Human Decision不得修metadata、harness、collector或重试。

2026-09-01 Human Resume已授权Recovery Cycle 2，但只恢复到`EXPERIMENT_DESIGN`：允许为Cycle1追加truthful disposition、最小修复harness的空初始event处理、修复并预验证collector，然后由fresh Reviewer放行独立`recovery-cycle-2`中的唯一实验。Attempt1/Cycle1原文件、实验假设、验收标准、安全边界与DSH baseline都不得改变。

Cycle2 Revision prevalidation Attempt 1=`FAIL / exit9 / UNHANDLED_COLLECTOR_ERROR`：当前Windows PowerShell缺少`System.IO.Path.GetRelativePath`。失败closed receipt与cleanup已保留，DSH/Vitest未执行、fixture未触碰。Fix Round 1只允许把collector relative-path实现改为已审计的root-prefix substring，并在独立Attempt 2 dummy roots重新预验证；候选harness不得再改。

Fix Round 1 / prevalidation Attempt 2=`FAIL / exit9 / UNHANDLED_COLLECTOR_ERROR`：root-prefix修复已生效，但当前Windows PowerShell不接受`Split-Path -LiteralPath ... -Parent`参数组合。Attempt2 closed receipt和cleanup保持不变。Fix Round 2只允许将destination parent计算替换为`[System.IO.Path]::GetDirectoryName()`并在独立Attempt3中预验证；不得修改harness或扩大collector语义。

Fix Round 2 / prevalidation Attempt 3=`PASS / exit0 / 3 of 3 exact path+bytes+SHA+count / cleanup PASS`。Fresh Reviewer已派发，只读复核Cycle1 disposition、单一harness diff、collector及三次预验证、fixture与state parity；Reviewer PASS前不得执行DSH/Vitest实验。

Fresh Reviewer结果=`FAIL / STATE_PARITY_FAILURE`：技术候选与证据完整性均通过，但`A36-C2-RR-F01`发现course README current pointer残留，`A36-C2-RR-F02`发现ACTIVE authorization scope仍为旧schema值。当前只允许修这两项并fresh recheck；唯一实验仍未执行。

Recovery Cycle 2=`FAIL / FAILED_LAB`：preflight在fixture-local `.bin`优先PATH下执行唯一允许的`corepack pnpm --version`探针，exit=`1 / MODULE_NOT_FOUND`，因此focused command保持`0 / NOT_EXECUTED`，P0—P5未观察、acceptance=`0/10`。collector一次调用返回`SOURCE_ROOT_MISSING`且未伪造runtime raw；17/17 self-excluding manifest、cleanup和fixture pinned/clean已由Master复算。STOP POLICY WINS，当前hard lock等待新的Human Resume。

2026-09-01 Human Resume授权Recovery Cycle 3：允许truthful修复`A36-C2-FR-F01—F05`、修复并预验证Corepack/PATH启动方式，并仅在fresh recheck通过后执行一次实验；hypothesis、acceptance与DSH baseline保持冻结。Cycle3 Attempt1证明absolute Corepack的pnpm/Vitest版本探针可运行，但fresh Reviewer发现`A36-C3-RR-F01`：旧launcher要求fixture clean与已复制的untracked target spec同时成立，focused路径结构上不可达。Master plan-defect ruling仅追加一次launcher-only Attempt2预验证，不增加真实实验attempt。

Recovery Cycle 3 Attempt2=`FAIL / HUMAN_DECISION_REQUIRED`：唯一orchestration在copy前的clean receipt阶段把五条Git命令错误地记录为空argument vector并全部exit1，launcher及其Corepack/pnpm/Vitest探针均未调用；初始闭合又在空nested-file list上触发`String.Join(null)`。失败树保留6个文件，top self-excluding manifest=`5/5 exact / E9DE132A...B284`，但required launcher subtree缺失。fresh read-only audit确认target spec/runtime source/Cycle3 raw均不存在、fixture仍为`cd5ef814... / dsh-v0.1.2-alpha.1 / clean`，且launcher/probe/focused/target-test/collector计数均为0。Addendum明确禁止Attempt3、重修或重试；`LAB_EXECUTE`未获授权，当前hard lock等待新的Human Resume。

2026-09-01 Human Resume授权bounded Recovery Cycle 4。Cycle3失败记录与launcher v2保持不可变；由于旧orchestration源码未持久化，Cycle4只创建新的durable orchestrator，并以isolated RED/GREEN验证argument-vector与empty-list两个failure class。RED=`EXPECTED_FAIL / exit41 / 7 of 7 exact`，GREEN=`PASS / exit0 / 7 of 7 exact`，但两阶段receipt未保存test SHA，因此阶段间test immutability=`NOT_DURABLY_PROVABLE`。

Recovery Cycle 4 Attempt1=`FAIL / HUMAN_DECISION_REQUIRED`：exact harness spec copy PASS后，orchestrator调用unchanged launcher v2一次；launcher完成自身、Corepack与package-manager identity receipts后exit90，未完成focused-prestate、PATH mutation、Vitest resolution或两个version probes，nested launcher manifest缺失。内部首因没有被receipts保留，保持`NOT_PROVABLE`。Attempt top manifest=`28 of 28 exact / CD7495F2...34E7`，exact spec removal与fixture cleanup PASS；launcher child/probe/focused/target-test/collector/bare-Corepack全部为0。Cycle4禁止Attempt2或重试；`LAB_EXECUTE`未获授权，当前hard lock等待新的Human Resume。

2026-09-01 Human Resume授权bounded Recovery Cycle 5，仅建立focused-state边界的isolated diagnostic surface，不调用原launcher、Corepack、pnpm、Vitest、DSH或正式实验。RED=`EXPECTED_FAIL / exit41`，tests SHA已持久化；GREEN进程虽exit0，但receipt把`expected_failed_assertions`序列化为`null`而非required `[]`，且RED/GREEN均缺frozen plan要求的exact-command receipt。Fresh review=`SPEC FAIL / QUALITY FAIL / TERMINAL`；diagnostic attempt未启动，Cycle5 GREEN预算已消耗，Factory回到`PAUSED / HUMAN_DECISION_REQUIRED`。

2026-09-03 Human Resume授权bounded Recovery Cycle 6：Cycle5失败树、旧GREEN及其`null`/provenance缺陷全部冻结，不允许修改、回填或重跑。Cycle6采用独立schema-first closure和不可变测试，严格按一次RED、一次GREEN、一次isolated diagnostic执行；生产serializer只能在预期RED之后创建。原launcher、Corepack、pnpm、Vitest、DSH、正式实验、hypothesis、acceptance与baseline均为零调用/零修改；fresh recheck后无条件停止。

Cycle6唯一RED预算已消耗并异常失败：tests-local closure在创建首个receipt前，于PowerShell binder调用`JsonObject.Add(key,value)`的failure surface终止，未得到冻结的`EXPECTED_FAIL / exit41 / four findings`。RED目录存在但文件数为0；production serializer、candidate、GREEN和isolated diagnostic均未创建/未运行。按no-retry合同直接进入fresh read-only recheck，不做现场修复或重跑。

Fresh Reviewer=`FAIL / CYCLE6_RECEIPT_CLOSURE_NOT_CLOSED / TERMINAL`：静态源码只足以把failure surface定位到PowerShell binder对`JsonObject.Add(key,value)`的调用；由于RED root为空，exact argv、实际host、exit与stdout/stderr均保持`NOT_DURABLY_PROVABLE`，不能把环境重载结论升级成独立runtime fact。Cycle5 explicit-empty-array与exact-command provenance缺陷仍OPEN；Factory hard lock已恢复，等待新的Human Resume。

2026-09-03 Human Resume接受Cycle7最小化恢复：不再建设或修复Cycle6通用schema-first serializer，只保留必要的exact command/host/exit/stdout/stderr、explicit empty array、manifest与crash-safe failure evidence。当前fresh Researcher只冻结plan；兼容性预验证、fresh recheck和正式实验必须按序放行，Cycle5/6历史不得修改或重跑。
