# Article 31 Review｜Cycle 0

> Role：`REVIEWER / FRESH CONTEXT`
> Gate：`REVIEW`
> Review Date：`2026-08-30 (Asia/Shanghai)`
> Decision：`PASS_WITH_NOTES / REVISION REQUIRED`

## Review scope and independence

- Reviewer：`/root/part_vi_a31_reviewer`。
- Execution Type：`REAL_SUBAGENT / FRESH CONTEXT`。
- 独立读取 Article Card、Research、Evidence、Repository Map、Call Path、Effective Config Diff、Outline 与 Draft；并读取 canonical Article 31、Glossary、Course Factory / production workflow、Reviewer contract、review checklist 与 TwoEgg article method。
- 直接复核固定 DSH fixture 的 official origin、tag/full commit、clean state 与正文所列 pinned source paths；未读取或接收 Author hidden reasoning、confidence 或 self-score。
- 未修改 Draft、Outline、Research、Evidence、Repository Map、Call Path、Lab Trace、README、global state、Published Content、future Article assets、Git history或remote；本轮唯一写入为本 `review.md`。

## Required Draft identity recompute

- Draft path：`docs/agent-engineering-course/articles/31-dsh-profile-bundle-capability-seam/draft.md`。
- Recomputed identity：`39017 bytes / 674 physical lines / SHA-256 110AF0464D4F0CC04524E8BBE9015194FA2E2EF4A0D3983CB5CDE3241DF548EB`。
- Author trace identity：`39017 / 674 / 110AF046...F548EB`。
- Result：`PASS / IDENTITY_MATCH`。

## Claim, Evidence Card and baseline recompute

- Claim register：`15` unique IDs，exactly `31-C01` through `31-C15`。
- Evidence Cards：`15` unique IDs，exactly `31-E01` through `31-E15`；每张均含 `Claim ID / Evidence Status / Proves / Does Not Prove`。
- Outline 与 Draft 均覆盖 `15 / 15` Claims 和 `15 / 15` Cards；Draft traceability table 一一对应。
- Status mix：`12 CONFIRMED / 0 PARTIAL / 3 PROPOSAL / 0 BLOCKED`，在 merged Research、Evidence、Outline 与 Draft 中一致。
- DSH baseline：origin=`https://github.com/deepseek-ai/deepseek-harness.git`；`HEAD == dsh-v0.1.2-alpha.1 target == cd5ef8148158c3a752a658978873241fdf8e2bbc`；fixture status、diff、cached diff 均为空。
- Draft 中 `14` 个 unique pinned blob URL 对应的 source path 全部存在于固定 fixture。
- Result：`PASS`；两项 artifact / publication Finding 见下。

## Findings

### A31-R0-F01｜Source-stage artifacts 把 pre-Lab 状态写成当前状态

- Finding ID：`A31-R0-F01`
- Severity：`MINOR`
- Status：`OPEN`
- Category：`EVIDENCE / COURSE`
- Location：`repository-map.md:3,116-118`；`call-path.md:3,233-240`。
- Problem：两个 source-stage artifact 仍以当前语气写 `LAB NOT RUN`、`Handoff to Lab`、`hypotheses, not results` 和 `Until Lab records...`。当前 Article 已进入 Review，merged Research/Evidence 与 Lab Trace 已记录 Effective Config dumps、overlay activation negative、FS operation 与 permission fixture results；未标明 historical snapshot 会让同一 workspace 同时呈现“Lab 未运行”和“Lab 已完成”。
- Supporting Evidence：`research.md` / `evidence.md` 当前状态为 `EVIDENCE MERGED`，`experiments/effective-config-diff.md` 为 `RAW OBSERVATION COMPLETE`，README 当前 Gate 为 `REVIEW`；Draft 已使用这些 merged observations。
- Why It Matters：Repository Map 与 Call Path 是公开审计入口。时间语义冲突不推翻 Draft 结论，但会削弱 Evidence lineage，并让读者误判当前 Gate 和 runtime evidence 是否存在。
- Required Disposition：只做最小时间语义修订：把两个文件明确标为 `HISTORICAL SOURCE INVESTIGATION SNAPSHOT`，说明该 Gate 当时尚未运行 Lab；将当前 lifecycle 路由到 README、当前 Claim status 路由到 merged Research/Evidence、当前 runtime results 路由到 Effective Config Diff。不得回填或重写静态 source chain、raw observation、Draft 或 Claim conclusions。
- Gate Effect：`REVISION REQUIRED / REVIEW_RECHECK REQUIRED / NO RETURN_TO_RESEARCH / NO NEW LAB`。

### A31-R0-F02｜Schema 表格中的未转义 pipe 会破坏列结构

- Finding ID：`A31-R0-F02`
- Severity：`MINOR`
- Status：`OPEN`
- Category：`PUBLICATION / READER_VALUE`
- Location：`draft.md:105`。
- Problem：三列表格的第三列写成 `` `patchReload: live | startup` ``。Markdown table delimiter 会消费未转义的 `|`，使该行产生额外列，发布后 schema gate 表可能错位。
- Supporting Evidence：该表 header 固定为三列；同一行存在一个额外的内部 pipe，而 `live | startup` 本意是一个 enum 表达。
- Why It Matters：这是正文第一张具体 schema 表。错列会直接损害读者对 distributed contract 的理解，且属于 Publisher 机械映射前应关闭的 publication defect。
- Required Disposition：在不改变语义的前提下，将内部 pipe 转义为 `\|`，或改写为不含 pipe delimiter 的等价文字（例如 `` `live` 或 `startup` ``）；随后重算 Draft identity，并做 Reviewer recheck。
- Gate Effect：`REVISION REQUIRED / REVIEW_RECHECK REQUIRED / NO RETURN_TO_RESEARCH / NO NEW LAB`。

## Technical review

- Outcome：`PASS`。
- 正文从“Profile 能做什么”这个坏问题切入，先建立 config source、Capability path 与 runtime evidence 三轴模型，再落 DSH 源码和实验。
- `dump != activation` 被 repo-owned overlay 的 `exit 0 / 146 rows / 145 ids` 与 activation `exit 1 / duplicate cordis-host-runner` 直接闭合；missing named overlay `ENOENT / exit 1` 也与 optional user patch absent 分开。
- shipped source/config topology 保持 `FileSystem -> SandboxedFileSystem -> ToolFs`；targeted runtime operation 明确写成 `LocalFileSystem -> ToolFs / 1 passed`，没有借 shipped Provider 名称升级运行结论。
- configured、Provider active、Consumer active、operation observed 四层证据没有混写；literal `!!js` source 也没有被写成 resolved Session permission。
- permission cases 明确限定为 `SandboxingFakeFs` 周围的 protocol fixture；正文没有声称 OS enforcement、真实 approval UI 或 shipped SandboxedFileSystem enforcement。
- Headless/Web 只在 composition 层写共享 `87` ids 与 surface fork；`--help` 被限定为 mode-owned help path，不冒充 task、model call、listener 或 long-running Host。

## Evidence and Lab review

- Outcome：`PASS_WITH_ARTIFACT_FINDING`。
- Expected Observable、Observed Result、Interpretation 与 Does Not Prove 分离；Corepack wrong-cwd `EACCES`、PowerShell `$home` collision 与 `Start-Process` argument flattening 均保留为 orchestration mistakes。
- Exact receipts一致：Headless `11227 bytes / 89 / 89 / 7B00D284...5298`；Web effective/default `16558 bytes / 144 / 144 / 0958D6C3...689A`；overlay `16827 bytes / 146 / 145 / 679CC5ED...D76E9`。
- owner config tests `6/6`、Local FS targeted integration `1 passed / 32 skipped`、permission cases `1 + 3` 均按 fixture 上限表述；real model/provider/network/token/cost明确为 `NOT TESTED`。
- `A31-R0-F01` 只修正 source-stage artifact 的时间语义，不要求新 Research、重跑 Lab 或改写当前 Claim结论。

## Teaching quality, transfer and course scope

- Outcome：`PASS`。
- 三轴模型、四级 availability ladder、两条 FS seam 分账与“可以说 / 不能说”表具有直接迁移价值；实验服务于 provenance、activation 与 authority boundary。
- BuildPilot 只 `ADOPT` explicit Capability Set、read-only Profile 与 receipt contract；arbitrary layering、live reload/runtime replacement、multi-Host 保持 `DEFER`，并明确为 Part VI proposal。
- Article 32 只接 System Prompt Assembly / PromptContext owner；Article 33—37、Article 38—44、BuildPilot implementation 与 Part VII 均保持未开始。
- M 级正文信息密度较高，但通过层级表、receipts 和 Evidence Boundary 有效压缩；`A31-R0-F02` 关闭后 publication readability 才完整达标。

## Publication and Hugo preflight

- Draft 无 front matter，符合 Publisher 机械映射前边界；planned target/front matter 已由 Outline 固定。
- `relref=4`，均使用 ASCII 双引号并指向已存在的 Article 30 与课程索引；future Article `relref=0`。
- Fence markers=`44 / EVEN`；中文引号 shortcode=`0`；placeholder=`0`；trailing whitespace=`0`；`git diff --check -- draft.md` 通过。
- Fresh `hugo --renderToMemory`：`1258 pages / 44 static files / 1 alias`，exit `0`，无 `ERROR`。Draft 位于 `docs/`，所以该结果只证明当前站点 baseline；发布后 Build Gate 仍需真实 Hugo 验证。

## Five-dimensional score

| Dimension | Score | Review basis |
|---|---:|---|
| Technical Accuracy | `20 / 20` | composition、activation、FS provider identity、permission fixture 与 Host surface 边界均准确。 |
| Evidence Discipline | `18 / 20` | 15/15 traceability 与反证完整；source-stage artifact 的 historical/current 时间语义需修复。 |
| Teaching Quality | `19 / 20` | 问题 -> 三轴模型 -> 具体链路 -> 反例 -> transfer 主线完整。 |
| Engineering Transfer | `19 / 20` | capability receipt、negative activation 与 read-only contract 可迁移，且未冒充实现。 |
| Readability & Compression | `17 / 20` | M 级结构可读，但一处 schema 表格 pipe 会破坏发布列结构。 |
| **Total** | **`93 / 100`** | **数值阈值满足；两项 MINOR 必须经 Revision / Recheck 关闭。** |

Threshold check：Total `93 >= 88`；Technical `20 >= 18`；Evidence `18 >= 18`；Teaching `19 >= 17`；Engineering Transfer `19 >= 17`。Result=`ALL NUMERIC THRESHOLDS MET`。

## Open Finding Summary

| Severity | Open count | Finding IDs |
|---|---:|---|
| BLOCKER | `0` | `NONE` |
| MAJOR | `0` | `NONE` |
| MINOR | `2` | `A31-R0-F01`, `A31-R0-F02` |
| EDITORIAL | `0` | `NONE` |
| **Total actionable** | **`2`** | **Revision required** |

## Gate decision

- Review Decision：`PASS_WITH_NOTES`。
- Review execution：`COMPLETE`。
- Findings requiring repair：`A31-R0-F01`、`A31-R0-F02`。
- Next Allowed Gate：`REVISION`。
- Exact route：`REVIEW -> REVISION -> REVIEW_RECHECK`。
- Blocker：`NONE`。
- Return To Research Required：`NO`。
- New Lab Required：`NO`。
- Publication / Final Gate allowed now：`NO — both Findings must be closed by Reviewer recheck`。
- Non-claim boundary：本 Review 不是 Final Gate、Published Content、post-publication Hugo Build、commit、push、remote verify、Article 32 kickoff、Part VI Audit 或 `END_ARTICLE`。

Final Review decision：`PASS_WITH_NOTES / 2 OPEN MINOR FINDINGS / REVISION REQUIRED`。

## Cycle 1 Revision disposition

- Revision Worker：`/root/part_vi_a31_revision_cycle1`；Execution Type：`REAL_SUBAGENT / FRESH CONTEXT`。
- `A31-R0-F01`：已在 `repository-map.md` 与 `call-path.md` 做最小时间语义修订，将二者明确标为 Lab 执行前的 historical source investigation snapshot；当前 lifecycle、Claim status、runtime results 分别路由到 `README.md`、merged `research.md` / `evidence.md`、`experiments/effective-config-diff.md`。静态 source chain、raw observations 与 Claim conclusions 均未回填或改写。
- `A31-R0-F02`：已将 Draft schema 表格中的内部 pipe 改为不破坏列结构的 `` `patchReload: live` 或 `startup` ``，语义不变。
- Finding 状态：仍由 Reviewer 在 recheck 中裁定；Revision Worker 不自行标记 `CLOSED`。
- Revision status：`READY_FOR_RECHECK`。
- Next Allowed Gate：`REVIEW_RECHECK`。

## Cycle 1 Reviewer Recheck

> Role：`REVIEWER / FRESH CONTEXT`
> Gate：`REVIEW_RECHECK`
> Review Cycle：`1`
> Recheck Date：`2026-08-30 (Asia/Shanghai)`
> Decision：`PASS / ZERO OPEN FINDINGS`

### Recheck scope and identity

- Reviewer：`/root/part_vi_a31_reviewer_recheck_cycle1`；Execution Type：`REAL_SUBAGENT / FRESH CONTEXT`。
- 本轮只复核 `A31-R0-F01`、`A31-R0-F02`、修订后的 Draft identity、15/15 traceability、既有 Evidence Boundary 与 future-zero；未修改 Draft、source artifacts、Evidence、global state、Published Content、Git history或remote。
- Revised Draft identity：`39021 bytes / 674 physical lines / SHA-256 C70510DFB0B8DE33D0AD58518E2E29ED7CACA2F08B842EEA8695A27AF547BA8D`，与 Revision Worker receipt 精确一致。
- 将 Draft 中唯一修订短语 `` `patchReload: live` 或 `startup` `` 反向还原为 Cycle 0 的 `` `patchReload: live | startup` `` 后，得到 `39017 bytes / 674 physical lines / SHA-256 110AF0464D4F0CC04524E8BBE9015194FA2E2EF4A0D3983CB5CDE3241DF548EB`，精确匹配 Author / Cycle 0 Draft identity；因此正文其余内容未改变。

### A31-R0-F01 recheck

- Finding ID：`A31-R0-F01`
- Previous Status：`OPEN / MINOR`
- Current Status：`CLOSED`
- Verification：`repository-map.md` 与 `call-path.md` 现在都以 `HISTORICAL SOURCE INVESTIGATION SNAPSHOT / LAB NOT YET RUN AT THIS GATE` 标记 Source-stage 时间边界，并把当前 lifecycle、Claim status、runtime results 分别路由到 `README.md`、merged `research.md` / `evidence.md`、`experiments/effective-config-diff.md`。
- Non-regression：从当前两个文件中只撤销新增 routing paragraph、恢复原 `SOURCE_MAP COMPLETE / LAB NOT RUN` status，并恢复原 `Handoff to Lab` / `Lab handoff: hypotheses, not results` 标题后，SHA-256 分别精确回到 Source Investigator receipt：`EBB4F5562FDEBABEF177881AA15D36C76AE04AC9C7104EFA57D4E3BFD71AB463` 与 `94BA59CC40004D009730CB1C4BB91821492C5E65E8E00579D254D29E2B09F0D2`。静态 source chain、raw observation、实验假设和 Claim conclusion 均未被回填或改写。
- Disposition：`CLOSED / HISTORICAL-CURRENT ROUTING FIXED / SOURCE CHAIN UNCHANGED`。

### A31-R0-F02 recheck

- Finding ID：`A31-R0-F02`
- Previous Status：`OPEN / MINOR`
- Current Status：`CLOSED`
- Verification：Draft schema row 现在写为 `` ordered `bundles`、`patchReload: live` 或 `startup` ``；整行只有三列表格所需的 `4` 个 pipe delimiters，不再产生额外列。
- Semantic check：该文字仍完整表达 `patchReload` 的两个允许值 `live` 与 `startup`，与 `profile.ts` source mapping、Research、Repository Map 与后文 Profile 对照表一致，没有改变 schema 结论。
- Disposition：`CLOSED / TABLE STRUCTURE VALID / SEMANTICS PRESERVED`。

### Traceability, Evidence Boundary and future-zero recheck

- Evidence Claim register：`15` unique IDs，连续覆盖 `31-C01`—`31-C15`；Evidence Cards：`15` unique IDs，连续覆盖 `31-E01`—`31-E15`；Draft traceability table 为 `15` rows、`15` unique Claims、`15` unique Cards。
- Status mix：`12 CONFIRMED / 0 PARTIAL / 3 PROPOSAL / 0 BLOCKED`；Proposal 仍只落在 BuildPilot Capability Set、read-only Profile 与 deferred composition machinery。
- Evidence Boundary 保持不变：`dump != activation`；shipped `SandboxedFileSystem` source/config seam 与 targeted `LocalFileSystem -> ToolFs` operation 分账；permission protocol 限定于 fake FS Provider；real LLM Provider / model / token / cost 与 OS sandbox enforcement 都明确 `NOT TESTED`；Part VII 明确 `NOT STARTED`。
- Frozen fixture fresh check：`HEAD == dsh-v0.1.2-alpha.1 target == cd5ef8148158c3a752a658978873241fdf8e2bbc`；working diff 与 cached diff 均为空。
- Future-zero：`docs/.../articles` 中 Article 32—44 directories=`0`；`content/ai-empowerment` 中 Article 32—44 published files=`0`；Draft future Article 32—44 `relref=0`。

### Updated score and gate decision

| Dimension | Cycle 1 Score | Recheck basis |
|---|---:|---|
| Technical Accuracy | `20 / 20` | Schema 枚举语义未变，既有技术结论未被修订。 |
| Evidence Discipline | `20 / 20` | Historical/current routing 已闭合，旧 source 指纹可逆复现。 |
| Teaching Quality | `19 / 20` | 主线与既有证据边界保持完整。 |
| Engineering Transfer | `19 / 20` | ADOPT/DEFER 边界未变。 |
| Readability & Compression | `19 / 20` | schema table 已恢复稳定三列结构。 |
| **Total** | **`97 / 100`** | **全部数值阈值满足，且无未关闭 Finding。** |

- Review Cycle：`1`。
- Review Recheck Decision：`PASS`。
- Open Findings：`0`（BLOCKER=`0`、MAJOR=`0`、MINOR=`0`、EDITORIAL=`0`）。
- Gate Completed：`true`。
- Next Allowed Gate：`FINAL_GATE`。
- Blocker：`NONE`。
- Return To Research Required：`NO`。
- New Lab Required：`NO`。
- Non-claim boundary：本 Recheck 不是 Final Gate、Published Content、post-publication Hugo Build、commit、push、remote verify、Article 32 kickoff、Part VI Audit 或 `END_ARTICLE`。

Final Recheck decision：`PASS / 97 OF 100 / 0 OPEN FINDINGS / NEXT FINAL_GATE`。

## Independent Final Gate

> Role：`REVIEWER / FRESH CONTEXT`
> Gate：`FINAL_GATE`
> Final Gate Date：`2026-08-30 (Asia/Shanghai)`
> Decision：`PASS / ELIGIBLE_FOR_PUBLISH`

### Frozen revised Draft identity

- Reviewer：`/root/part_vi_a31_final_gate`；Execution Type：`REAL_SUBAGENT / FRESH CONTEXT`。
- Frozen Draft path：`docs/agent-engineering-course/articles/31-dsh-profile-bundle-capability-seam/draft.md`。
- Fresh direct recompute：`39021 bytes / 674 physical lines / SHA-256 C70510DFB0B8DE33D0AD58518E2E29ED7CACA2F08B842EEA8695A27AF547BA8D`。
- 该 identity 与 Cycle 1 Revision disposition、Reviewer Recheck receipt 完全一致；Final Gate 未修改 Draft、Outline、Research、Evidence、source artifacts、Lab artifact、global state 或 Published Content。

### Review closure and evidence ledger

- Review Cycle 1 durable result：`97 / 100 / PASS / 0 OPEN FINDINGS`。
- `A31-R0-F01`：`CLOSED`；Repository Map 与 Call Path 明确保留为 pre-Lab historical source snapshot，并把 current lifecycle、Claim status 与 runtime results 路由到各自 current owner。
- `A31-R0-F02`：`CLOSED`；schema row 当前为 `` `patchReload: live` 或 `startup` ``，该行保持三列表格所需的 `4` 个 delimiter，枚举语义不变。
- Evidence register fresh count：`15` unique Claim IDs（`31-C01`—`31-C15`）与 `15` unique Evidence Cards（`31-E01`—`31-E15`），一一对应。
- Status mix fresh count：`12 CONFIRMED / 0 PARTIAL / 3 PROPOSAL / 0 BLOCKED`；Draft traceability table 覆盖 `15 / 15`。
- Open Finding summary：BLOCKER=`0`、MAJOR=`0`、MINOR=`0`、EDITORIAL=`0`。

### Source, runtime and proposal boundary check

- Profile / Bundle / Patch contract、bundle -> profile -> home -> argv overlay -> boot-only telemetry hard patch precedence、whole-config replacement、warning/skip 与 duplicate semantics 均落到 pinned source；built dump、activation negative 与 owner tests没有被互相替代。
- Exact configuration receipts保持：Headless=`89 rows / 89 ids`，Web=`144 / 144`，shared ids=`87`；repo-owned overlay dump=`exit 0 / 146 rows / 145 ids`，同一 overlay activation=`exit 1 / duplicate cordis-host-runner`。正文始终把 dump 写成 composition receipt，不写成 activation certificate。
- Shipped source/config seam 保持 `FileSystem -> SandboxedFileSystem -> ToolFs`；targeted runtime seam 保持 `LocalFileSystem -> ToolFs / 1 passed / exact write-readback`。正文没有把 targeted Local provider test 改名为 shipped SandboxedFileSystem runtime。
- Permission evidence仍明确限定于真实 policy / ToolFs protocol 周围的 `SandboxingFakeFs` fixture；literal `!!js` source 不等于 resolved Session policy，也不证明 Windows ACL、Landlock、Seatbelt 或 OS enforcement。
- real LLM Provider、model request、credential value、network、token、cost、listener 与 production security 都保持 `NOT TESTED`。
- BuildPilot 的 Explicit Capability Set、read-only Profile 与 effective/default diff + activation receipt 仍为 `PROPOSAL / ADOPT`；arbitrary layering、live reload/runtime replacement 与 multi-Host 仍为 `PROPOSAL / DEFER`。Article 38 / Part VII 没有被写成已启动、ADR、code 或 runtime。

### Publication plan and future-zero check

- Planned frontmatter 已固定 canonical title、slug=`agent-engineering-31-dsh-profile-bundle-capability-seam`、series metadata、order=`320` 与 weight=`3320`；Draft 自身仍无 frontmatter，符合 Publisher mechanical mapping boundary。
- Draft `relref=4`，只指向已存在的 Article 30 与课程索引，且全部使用 ASCII double quotes；Article 32—44 future `relref=0`。
- 顶部和底部均保留 Previous + Course navigation；下一篇只作无链接 owner 提示，未提前创建 next link。
- `14` 个 unique pinned blob path 全部存在于固定 fixture；code fence markers=`44 / EVEN`，placeholder=`0`，`git diff --check -- Article31 workspace`=`PASS`。
- Article 31 Published target 在本 Gate 前仍不存在；Article 32—44 workspace directories=`0`、Published Content files=`0`。
- Frozen fixture fresh check：origin=`https://github.com/deepseek-ai/deepseek-harness.git`；`HEAD == tag target == cd5ef8148158c3a752a658978873241fdf8e2bbc`；working status、diff 与 cached diff 均为空。

### Final Gate decision

- Technical Review：`PASS`。
- Evidence Review：`PASS`。
- Course / Reader Value / Job Competency Review：`PASS`。
- Publication Preflight：`PASS`。
- Final Gate：`PASS / ELIGIBLE_FOR_PUBLISH`。
- Gate Completed：`true`。
- Next Allowed Gate：`PUBLISH`。
- Blocker：`NONE`。
- Non-claim boundary：本 Final Gate 不等于 Published Content 已创建、Hugo Build 已通过、global state 已回写、commit/push/remote verify 已完成、Article 31 已 `END_ARTICLE`、Article 32 已启动或 Part VI Audit 已执行。

Final Gate decision：`PASS / 97 OF 100 / 0 OPEN FINDINGS / ELIGIBLE_FOR_PUBLISH / NEXT PUBLISH`。
