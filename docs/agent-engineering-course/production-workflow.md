# Agent Engineering 课程生产工作流

本工作流是 [通用文章生产工作流](../article-production-workflow.md)在 Agent Engineering 课程中的扩展，不替代通用方法。它增加了课程依赖、Evidence Card、Lab、DSH 源码证据和 BuildPilot 设计案例的 Gate。

多篇文章的顺序、恢复、worker 路由、Review cycle、Part Audit 与 Course Final Audit 由 [Course Factory contract](course-factory.md) 编排；角色写入边界见 [Subagent contracts](subagent-contracts.md)，执行 pointer 见 [course-run-state.md](course-run-state.md)。Course Factory 是 multi-article orchestration layer，本文件仍是 Article-level production protocol，二者不互相替代。

## Course Factory pipeline alignment

Normal Article：

```text
PRECHECK -> ARTICLE_KICKOFF -> WORKSPACE_INIT -> RESEARCH -> EVIDENCE_GATE
         -> OUTLINE -> AUTHOR_DRAFT -> REVIEW / REVISION / RECHECK
         -> FINAL_GATE -> PUBLISH -> BUILD_VERIFY
         -> MASTER_STATE_UPDATE -> PRE_COMMIT_RECONCILIATION -> GIT_DIFF_VERIFY
         -> ARTICLE_CHECKPOINT_COMMIT -> ARTICLE_COMMIT_VERIFY
         -> PUSH_MAIN -> REMOTE_VERIFY
         -> POST_COMMIT_RECONCILIATION_READ_ONLY -> END_ARTICLE
```

Lab Article：

```text
PRECHECK -> ARTICLE_KICKOFF -> WORKSPACE_INIT -> RESEARCH -> PRELIMINARY_EVIDENCE
         -> LAB_DESIGN -> LAB_EXECUTE -> LAB_OBSERVATION
         -> EVIDENCE_MERGE -> EVIDENCE_GATE
         -> OUTLINE -> AUTHOR_DRAFT -> REVIEW / REVISION / RECHECK
         -> FINAL_GATE -> PUBLISH -> BUILD_VERIFY
         -> MASTER_STATE_UPDATE -> PRE_COMMIT_RECONCILIATION -> GIT_DIFF_VERIFY
         -> ARTICLE_CHECKPOINT_COMMIT -> ARTICLE_COMMIT_VERIFY
         -> PUSH_MAIN -> REMOTE_VERIFY
         -> POST_COMMIT_RECONCILIATION_READ_ONLY -> END_ARTICLE
```

`ARTICLE_KICKOFF` 是 PRECHECK `PASS` 后由 Master 执行的显式 transaction ownership step；`WORKSPACE_INIT` 只能在 Kickoff 后执行。`LAB_DESIGN` 与 `EVIDENCE_MERGE` 属于 Researcher；`LAB_EXECUTE / LAB_OBSERVATION` 属于 Lab Engineer。Article Lifecycle 不因这些 operational Gate 改名。整个 production transaction 必须直接运行在 `main`，任何 role 都不得创建 branch。Lifecycle 写为 `PUBLISHED` 后仍必须完成 Pre-Commit Reconciliation、唯一 Article completion commit、一次 main push、remote equality 与只读 post-commit reconciliation，才能结束 transaction 或开始下一篇。

Persisted Checkpoint Gate = `PRE_COMMIT_RECONCILIATION`。Current Runtime Completion = `ResolveArticleCompletion(N)`。同一 checkpoint 在 commit 前解析为 `INCOMPLETE`，在有效 commit / push / remote reconciliation 后解析为 `END_ARTICLE`；两者之间不写入任何 bridge。resolver 只决定 completion；下一篇启动权威 = candidate pointer + resolver `END_ARTICLE` + policy，不消费 Lifecycle 或 README prose。

Git completion boundary：

- `PRE_COMMIT_RECONCILIATION` 是最后一个可写 Gate，必须把 Article lifecycle、下一 Article PRECHECK pointer candidate、status、run state、course / Article README、final trace 与必要 canonical / navigation 全部纳入同一 checkpoint diff；
- `ARTICLE_CHECKPOINT_COMMIT` 使用唯一 message `Publish Agent Engineering Article NN`，commit 内容不自引用自身 SHA；
- commit 后只允许 read-only Commit Verify、`git push origin main`、Remote Verify 与 Post-Commit Reconciliation；repository writes=`ZERO`；
- completion SHA 以 Git history 为权威。不得为了回写 SHA、`END_ARTICLE` 或 reconciliation result 创建第二个 commit。
- `ResolveArticleCompletion(N)` 在运行时只读检查 completion commit、其在 local / `origin/main` / live `main` current refs 中的 ancestor containment，以及 `HEAD == origin/main == live main`；输出 `END_ARTICLE` 或 `INCOMPLETE / exact reason`。

## Article Transaction Authorization

明确的人类“启动 Article N”“继续 Article N”、`START_ARTICLE_N` 或 `CONTINUE_ARTICLE_N`，默认授权 Master 执行当前 Article 的全部剩余 Gate，直到 `END_ARTICLE_N` 或真实 blocker；默认单位是完整 Article transaction，不是单个 Gate。只有原始指令明确写出“仅执行某 Gate”“停在 Review 前”“不要 Publish”等边界时，才记录 `explicit_stop_line` 并在该边界停止。

初次START由PRECHECK后的`ARTICLE_KICKOFF`激活；mid-Article CONTINUE在fresh Resume Reconciliation后，以幂等`ARTICLE_AUTHORIZATION_RESUME`从durable current Gate激活，不回放PRECHECK、Kickoff、已完成worker或已通过Gate。命中explicit stop line时唯一投影为`PAUSED / active_blocker=NONE / stop_reason=EXPLICIT_HUMAN_STOP_LINE / human_decision_required=false`，next action指向当前Article的resume Gate。

授权有效时，每个 Gate `PASS` 后必须完成 result validation、state transition并自动派发下一 required worker。Review 中可修复 Finding在最大轮次内自动走 `REVIEW -> REVISION -> REVIEW_RECHECK -> FINAL_GATE`；普通 worker结束、Research完成或 Evidence Gate通过都不要求再次取得人类确认。

Article transaction authorization只覆盖当前 Article N，不能泄漏到Article N+1。它与multi-Article `continuous_run`分账：`continuous_run.enabled: false`或`auto_continue_after_end_article: false`只阻止`END_ARTICLE N -> Article N+1 PRECHECK`，不能截断已获授权的Article N内部流程。合同回归场景见[Article transaction authorization regression](audits/article-transaction-authorization-regression.md)。

## 生命周期

```text
Article Card
  -> Research Questions
  -> Evidence
  -> Experiment / Lab（需要时）
  -> Detailed Outline
  -> Draft
  -> Technical Review + Evidence Review + Course Review
  -> Final
  -> Published
```

文章不能仅凭“内容已经写完”越过 Gate。状态表示已经满足的生产条件，而不是文件是否存在。

## 状态机

| 状态 | 含义 | 进入条件 | 允许的下一状态 |
|---|---|---|---|
| `PLANNED` | 已有课程定位和 Article Card，尚未开始研究 | canonical 中存在且范围明确 | `RESEARCHING` |
| `RESEARCHING` | 正在拆研究问题、搜集来源和设计验证 | 前置文章满足或已标注依赖风险 | `BLOCKED`、`EVIDENCE_READY` |
| `BLOCKED` | 关键证据、环境、版本或前置依赖缺失 | 已写清阻塞项和解除条件 | `RESEARCHING` |
| `EVIDENCE_READY` | 核心主张均有足够证据，局部不确定性已降级表述 | Evidence Gate 通过，所需 Lab 已完成 | `OUTLINE_READY` |
| `OUTLINE_READY` | 详细提纲已把问题、主张、证据、实验和边界逐段绑定 | Outline Gate 通过 | `DRAFTING` |
| `DRAFTING` | 正在依据已批准提纲写正文 | 创建 `draft.md`，不得引入无证据新主张 | `REVIEW`、`RESEARCHING` |
| `REVIEW` | 技术、证据、课程一致性审查中 | 草稿完整且自检通过 | `DRAFTING`、`FINAL` |
| `FINAL` | 三类审查通过，待发布 | 无未处理的阻断问题 | `PUBLISHED` |
| `PUBLISHED` | 正文已进入 Hugo 内容树并完成构建验证 | 发布 Gate 通过 | 重新进入 `RESEARCHING` 进行修订 |

`BLOCKED` 是研究阶段的中断态，不是失败终态。解除阻塞后必须回到 `RESEARCHING` 复核证据，不能直接跳到 `EVIDENCE_READY`。

### Post-publication factual hotfix

已 `PUBLISHED` 的文章若被独立审稿发现范围明确、证据可直接复核的事实遗漏，可保持 `PUBLISHED` 并追加 `POST_PUBLICATION_HOTFIX` 记录，不伪造完整生命周期重跑。必须保留原 Review 历史，定向复核 current primary evidence，同步修订 Research、Evidence、Draft 与 Published Content 的相关措辞，并重新通过 Hugo Build。若修复需要新核心主张、重构教学主线或推翻原 Final Gate，则按状态机重新进入 `RESEARCHING`。

## Gate 定义

### Gate 0：Article Card

- 文章在 canonical 中的位置、依赖、读者变化和非目标明确。
- 文章回答的问题与相邻文章边界明确。
- 需要的 Evidence 与 Lab 类型已列出，但不预设结论。

### Gate 1：Research Questions

- 每个核心问题都能转化为可核验主张或明确的课程抽象。
- 版本敏感、实现敏感和行业术语差异已标记。
- 公开产品事实与内部实现推测严格分离。

### Gate 2：Evidence

- 每个核心主张都有 Evidence Card。
- 状态只能是 `CONFIRMED`、`PARTIAL`、`BLOCKED` 或 `PROPOSAL`。
- `PARTIAL` 必须收窄措辞；`BLOCKED` 行为性主张不得进入后续正文。
- 来源证明了什么、没有证明什么均已写清。
- Normal Article 可在研究证据完成后进入本 Gate。
- Lab Article 必须先完成 Preliminary Evidence、Researcher-owned Lab Design、Lab Execute / Observation 与 Researcher-owned Evidence Merge。
- 依赖 Lab 的 Claim 在 Preliminary Evidence 阶段保留正式 Evidence Status，并加 `Lab Dependency: REQUIRED`；不得因为计划做实验提前标 `CONFIRMED`。
- 需要实验的主张已完成真实 Lab，或明确降级为不作结论；expected 与 observed 必须分开保存。

### Gate 3：Outline

- 每一节都有读者问题、核心主张、证据引用和边界。
- 概念按课程的 Progressive Definition 深度引入。
- 没有为了完整而提前讲完后续文章。
- 案例、图和 Lab 的教学职责明确。

### Gate 4：Draft

- 只有到此 Gate 才创建 `draft.md`。
- 正文遵循“问题空间 -> 抽象模型 -> 具体机制 -> 工程判断 -> 验证边界”。
- 新出现的关键主张必须退回 Evidence Gate，而不是直接补进正文。

### Gate 5：Review

- Technical Review：概念、机制、代码和版本表述正确。
- Evidence Review：每个重要结论与证据强度匹配。
- Course Review：依赖、术语深度、重复和前后桥接正确。
- 三类审查均无阻断问题后才能进入 `FINAL`。

### Gate 6：Publish

- 正文写入 `content/ai-empowerment/agent-engineering-<id>-<slug>.md`。
- 发布图片迁入 `static/images/agent-engineering/<id>-<slug>/`。
- Hugo 构建 `ERROR` 为零。
- Publisher 返回 Publication Result 与 state / canonical update candidate；Master 验证 Reviewer Final PASS、Publisher PASS、Build PASS 与 repository consistency 后，在 `PRE_COMMIT_RECONCILIATION` 统一回写 Article README lifecycle、`status.md`、run state、下一篇 PRECHECK pointer candidate 与必要 canonical publication metadata。根 `doc-plan.md` 只按系列级路由规则更新；checkpoint commit 后禁止继续写 repository。完成由运行时 `ResolveArticleCompletion(N)` 决定；下一篇启动必须同时满足 candidate pointer、resolver `END_ARTICLE` 与 policy，不由 Lifecycle 或 README prose 决定。

## 特殊证据路径

### DSH 源码篇（28—37）

除通用 Evidence Gate 外，还必须记录：

1. 固定仓库与 commit/tag；
2. 源码符号、文件和调用路径；
3. 可复现的运行入口与观测；
4. `SOURCE_CONFIRMED` 与 `RUNTIME_CONFIRMED` 是否分别成立；
5. 对课程架构的 `ADOPT / SIMPLIFY / REJECT / DEFER` 决策。

只读到源码不能自动推出运行时行为；只看到运行结果也不能自动推出内部实现。

### BuildPilot 设计篇（38—44）

- 默认 Evidence Status 为 `PROPOSAL`，用于表达设计与权衡。
- 所有“当前系统已经……”的表述都需要独立实现证据，否则禁止使用。
- 课程可以产出接口、状态机、Schema、失败语义和验证计划，但 M0 不实现 Runtime。
## 工作区文件生成时机

| 文件 | 最早生成 Gate |
|---|---|
| `README.md`、`article-card.md`、`research.md`、`evidence.md`、`review.md` | PRECHECK `PASS` 后的 `WORKSPACE_INIT`，Lifecycle 仍为 `PLANNED` |
| `outline.md` | `RESEARCHING`，只能先放空骨架 |
| `draft.md` | `DRAFTING` |
| `assets/` | 确有图、日志或实验产物时 |

WORKSPACE_INIT 只允许 canonical metadata、template skeleton、initial status、dependency reference 与空 / `NOT_STARTED` section；不得写 Research Answer、Evidence Conclusion、Claim Confirmation、Teaching Thesis、Outline、Draft 或 Review Finding。
