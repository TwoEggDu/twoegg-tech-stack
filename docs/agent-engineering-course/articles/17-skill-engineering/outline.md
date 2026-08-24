# Skill Engineering：按需加载领域方法，而不是再堆一层 Prompt

> 最短判断：Skill 不是脱离 Prompt 的新原语，而是可发现、按任务选择、携带领域方法与资源、可独立治理生命周期的方法包；执行、授权、状态推进与结果成立仍属于其他工程层。

## Gate Metadata

- PRINCIPLE / NORMAL_ARTICLE / Required Lab NONE。
- Evidence Gate PASS：15 Claims / 12 Evidence Cards / 0 core BLOCKED。
- `EXPERIMENT COUNT = 0`；Observed Result / raw artifact / BuildPilot Runtime = ABSENT。
- 产品事实限 2026-08-24 核对的 OpenAI / Anthropic / GitHub surface；PARTIAL / PROPOSAL / UNKNOWN 不升级。
- 前置边界：Articles 02 / 06 / 08 / 10 / 12 / 13 / 15 / 16；后续 Article 18 / 19 / 22、Harness、DSH、BuildPilot capability design 不提前展开。

## Teaching Spine

1. 问题空间：常驻 Prompt 难以单独限定范围、加载、版本与退役；换文件名也不等于 Skill Engineering。
2. 抽象模型：稳定课程定义、八对象边界、package anatomy、progressive disclosure。
3. 具体机制：`discover -> select -> load -> execute -> verify -> context disposition`。
4. 工程判断：trigger、contract、安全、ownership、lifecycle 与“何时不建”。
5. 验证边界：连续 BuildPilot 案例只作 DESIGN，实验数保持 0。

## 1. 问题空间：长 Prompt 为什么没有自然长成 Skill

- **Reader Question**：把领域说明移入 `SKILL.md`，是否已经工程化？
- **Purpose / Teaching Move**：反驳“Prompt 越堆越全”和“换名即 Skill”，立住维护问题。
- **Claims / Evidence**：17-C03、C05、C15；17-EC01、EC02、EC03、EC04、EC08、EC12。
- **Main Points / Transition**：Prompt 表达当次任务；Skill 额外需要 discovery、scope、resources、version、test、rollback、retirement。许多实现仍把 instructions 注入 Context；progressive disclosure 只减少默认完整加载，不消除 catalog、全文和冲突成本。转入定义与边界。
- **Wording Boundary**：C03 / C05 = INFERENCE；C15 = UNKNOWN；不能写 measured effect。
- **BuildPilot Responsibility**：引入唯一案例“Jenkins 上 Unity Android 构建在 YooAsset 更新后失败”，固定 `DESIGN / NOT IMPLEMENTED / NOT RUN`；不诊断。
- **Must Not Claim**：Skill 与 Prompt 完全无关；必然省 token / 提质；已读日志或定位故障。
- **Visual**：Prompt pile vs discoverable package；无收益数字。

## 2. 抽象模型（一）：课程定义与八对象分账

- **Reader Question**：Skill 与 Prompt、Tool、Workflow、Agent、Memory、KB / RAG、Harness / Policy 各负责什么？
- **Purpose / Teaching Move**：冻结全篇边界，后文不重写前置文章。
- **Claims / Evidence**：17-C03、C07、C10、C11、C14；17-EC01、EC03、EC04、EC06、EC07、EC09、EC10、EC11。
- **Main Points / Transition**：课程定义：`可发现、在相关任务出现时加载、可携带说明 / 脚本 / 参考 / 模板的领域方法包`。责任表：

  | Object | Owns | Skill does not own |
  |---|---|---|
  | Prompt | 当次 Goal / Constraints / I-O / Failure | discovery、版本、权限 |
  | Skill | 按需领域方法与资源 | 能力、权威事实、完成证明 |
  | Tool Runtime | Validate / Policy / Execute / Result / Trace | Skill 只指导 |
  | Workflow | state / guard / legal edge / terminal | Skill 不提交状态 |
  | Agent / Subagent | identity / tools / context / lifecycle | Skill 不是主体 |
  | Memory | 跨 Step / Session 保存与召回 | 不保存当前调查状态 |
  | KB / RAG | 动态候选检索、注入、引用 | reference 不替 Retrieve / Verify |
  | Harness / Policy | packing / budget / permission / recovery | Skill 不 enforcement |

  物理实现可融合，审查责任不可消失。转入 package anatomy。
- **Wording Boundary**：课程责任模型 = DESIGN，不是行业 taxonomy；产品事实保持 surface-scoped。
- **BuildPilot Responsibility**：方法归 Skill；读取归 Tool Runtime；推进归 Workflow；授权归 Policy；历史候选归 KB / Memory；审计归 Host / Harness。
- **Must Not Claim**：Skill 是其他对象升级版；`allowed-tools` 等于授权；矩阵要求八个服务。
- **Visual**：八对象 ownership matrix。

## 3. 抽象模型（二）：Package Anatomy 与 Progressive Disclosure

- **Reader Question**：最小包是什么？metadata、instructions、resources 为什么分层？
- **Purpose / Teaching Move**：先写开放格式事实，再叠课程治理与产品差异。
- **Claims / Evidence**：17-C01、C02、C03、C05、C13；17-EC01、EC02、EC03、EC05、EC06、EC07。
- **Main Points / Transition**：FACT：开放格式最小目录含 `SKILL.md`，必填 `name / description`，可带 `scripts/ references/ assets/`。披露链：`metadata -> full instructions -> resources`。课程模型再加 Host-owned governance。Codex、ChatGPT、Anthropic、GitHub 在 roots、activation、version、context timing、collision、multi-agent 上不同；GitHub SDK eager preload 是反例。转入运行生命周期。
- **Wording Boundary**：C01 / C02 / C13 = scoped FACT；C03 / C05 = INFERENCE；governance = DESIGN。
- **BuildPilot Responsibility**：只画四个未来候选的 anatomy，不创建包或资源。
- **Must Not Claim**：所有产品同构 / 都 lazy load；开放格式统一 registry、permission、rollback、precedence、unload。
- **Visual**：三层 disclosure + Format Fact / Course Design / Product Fact 三栏。

## 4. 具体机制（一）：Discover -> Select -> Load -> Execute -> Verify -> Context Disposition

- **Reader Question**：从发现到一次使用结束，各阶段谁负责？
- **Purpose / Teaching Move**：建立 owner-aware 生命周期主图。
- **Claims / Evidence**：17-C02、C04、C06、C12；17-EC01—EC08、EC11。
- **Main Points / Transition**：Discover 只产生 candidate；Select 可 explicit / implicit，算法由 Host 决定；Load 形成 Context contributor；Execute 经过 Tool Runtime / Workflow；Verify 只建立 validator seam；Host 决定 retain / compact / release。课程 receipt 记录 candidate、selection reason、version、resources、tool/workflow、permission、artifact、terminal、context disposition。转入 package contract。
- **Wording Boundary**：产品 activation = scoped FACT；六段链 / receipt = DESIGN；precedence、collision、unload = UNKNOWN / product-specific。
- **BuildPilot Responsibility**：先选 `jenkins-build-triage`；Tool Runtime 读 exact build 并确认 first failing stage；再按证据选择 compile 或 resource path。DESIGN / NOT RUN。
- **Must Not Claim**：catalog 即 selected；selected 即执行；Tool success 即验证；Host 一定卸载。
- **Visual**：Host / Skill / Tool Runtime / Workflow swimlane，含 failure terminal。

## 5. 具体机制（二）：Production Skill Contract 不是开放规范字段

- **Reader Question**：scope、I/O、依赖、权限、失败与验证怎样表达？
- **Purpose / Teaching Move**：把 format minimum 与 production review contract 分开。
- **Claims / Evidence**：17-C01、C07、C10、C12；17-EC01、EC02、EC08、EC09、EC10、EC11。
- **Main Points / Transition**：开放格式没有统一 typed I/O / failure schema。COURSE DESIGN contract：scope + near-miss；input provenance / freshness；output artifact / locator；tool/package/network dependency；permission / approval need；procedure；validator；failure / stop；owner / source / version / review。`description = what + when + near-miss` 只作匹配；permission 声明不授予权限。转入 trigger。
- **Wording Boundary**：anatomy = format-scoped FACT；合同字段 = DESIGN / NOT OPEN-SPEC FIELDS。
- **BuildPilot Responsibility**：用 Jenkins 候选设计 scope、read-only dependency、output、credential missing / log truncated / identity drift / screenshot-only failure；无真实 Job / observation。
- **Must Not Claim**：课程字段是规范必填；validator PASS 等于诊断正确；permission 字段等于 enforcement。
- **Visual**：Format Facts vs Course Contract 表。

## 6. 具体机制（三）：False Trigger、Missed Trigger 与 Context 成本

- **Reader Question**：怎样降低 pollution，又为何仍会误触发、漏触发和冲突？
- **Purpose / Teaching Move**：同时保留 disclosure 机制价值与剩余风险。
- **Claims / Evidence**：17-C04、C05、C06、C08、C15；17-EC01、EC02、EC03、EC07、EC08、EC12。
- **Main Points / Transition**：用 should-trigger positives 与 near-miss negatives 分测 trigger；false trigger 加载不相关方法并增加冲突，missed trigger 缺失领域方法。每次必需内容在 `SKILL.md`，variant reference 延迟读；catalog、全文、overlap、eager preload 仍有成本。collision / precedence 只按产品描述。转入 trust。
- **Wording Boundary**：trigger guidance = scoped FACT；pollution = INFERENCE；precision / recall、token、quality、winner = UNKNOWN。
- **BuildPilot Responsibility**：四候选各有正例与 near-miss；compile 首错不触发 YooAsset audit，纯 CDN 问题不触发 compile diagnosis。只设计。
- **Must Not Claim**：description 保证触发；消除 Context cost；本文测出 precision / recall 或 token savings。
- **Visual**：trigger 2x2 + catalog/full/resource/overlap ledger，数值 UNKNOWN。

## 7. 工程边界（一）：Trust、Provenance、Least Privilege 与 Sandbox

- **Reader Question**：含脚本和外部资源时，谁决定可信、可执行与可访问？
- **Purpose / Teaching Move**：分开 provenance review、permission need 与 runtime enforcement。
- **Claims / Evidence**：17-C07、C10、C12；17-EC01、EC04、EC06、EC07、EC10、EC11。
- **Main Points / Transition**：Skill / scripts 是 trust boundary。审 source / owner / digest / resources / dependency / network / credential / side effect / validator / rollback。Tool Runtime / Policy 执行 approval、credential scope、sandbox 与 least privilege；scan、pre-approval、sandbox、`allowed-tools` 都不能单独证明安全。转入 Agent ownership。
- **Wording Boundary**：trust warning = product-scoped FACT；清单 / reject-disable-ask terminal = DESIGN；不做安全评级。
- **BuildPilot Responsibility**：四候选默认 read-only；访问 Jenkins / Unity / artifact / remote 由 runtime 授权；`release-evidence-pack` 不发布、不重跑、不改 Job。
- **Must Not Claim**：scan 后安全；sandbox = permission；pin = 正确；文本 read-only 可替 enforcement。
- **Visual**：Source -> Review -> Pin -> Host eligibility -> Runtime/Policy -> Result/Trace。

## 8. 工程边界（二）：通用 Agent + Skill、专用 Agent与 Multi-Agent Ownership

- **Reader Question**：何时用通用 Agent + Skill，何时专用 Agent？Skill 自动继承吗？
- **Purpose / Teaching Move**：按变化轴判断，ownership / inheritance 保持产品范围。
- **Claims / Evidence**：17-C11、C13；17-EC06、EC07。
- **Main Points / Transition**：偶发复用方法且不需独立 model/system/tool/credential/permission/context/lifecycle 时，用通用 Agent + Skill；需要独立身份、隔离或 delegated owner 时用专用 Agent。Anthropic Managed Agents per-agent 配置；GitHub SDK subagent 不继承 parent skills。课程要求 dispatch 前解析 effective Skill set。转入 lifecycle。
- **Wording Boundary**：两产品行为 = scoped FACT；跨产品 inheritance = UNKNOWN；effective-set receipt = DESIGN。
- **BuildPilot Responsibility**：保持单一通用诊断 Agent + 四候选；不设计 subagent topology。
- **Must Not Claim**：自动共享 / 继承 / 隔离；某形态普遍更优；BuildPilot 已有 multi-agent runtime。
- **Visual**：model/system/tool/credential/permission/context/lifecycle/owner 决策表。

## 9. 工程化生命周期：Test、Version、Review、Release、Observe、Rollback、Retire

- **Reader Question**：Skill 怎样测试、版本、发布、观测、回滚 / 禁用和退役？
- **Purpose / Teaching Move**：把一次编写扩成生命周期，并说明每个 Gate 的证据上限。
- **Claims / Evidence**：17-C08、C09、C10、C12、C15；17-EC03—EC08、EC10、EC11、EC12。
- **Main Points / Transition**：`Source -> Review -> Static Validate -> Trigger Eval -> With/Without or Old/New Eval -> Pin/Deploy -> Observe -> Roll Back/Disable -> Deprecate/Retire`。Static 只证机械合同；trigger 用正负例；behavior 保存 baseline、fixtures、assertions、raw output、trace、timing/token receipt、human review。产品 version/disable 各自分账；pin 只固定身份。转入 BuildPilot 收束。
- **Wording Boundary**：产品机制 / official guidance = scoped FACT；统一 lifecycle / receipt = DESIGN；效果 = UNKNOWN。
- **BuildPilot Responsibility**：四候选只设计 owner review、static check、正负 trigger set、bounded fixture、permission review、version/digest、disable、last-known-good、deprecation owner；全部 NOT RUN。
- **Must Not Claim**：validator PASS = 有效；pin = 可靠；本文完成 eval / rollback；存在统一 registry。
- **Visual**：lifecycle gates + “PASS 最多证明什么”表。

## 10. 连续案例收束：BuildPilot 恰好四个窄 Skill

- **Reader Question**：Unity / YooAsset / Jenkins 调查中，哪些属于 Skill，哪些留在其他层？
- **Purpose / Teaching Move**：用同一案例落地全部边界，拒绝万能构建 Skill。
- **Claims / Evidence**：17-C07、C10、C12、C14、C15；17-EC08、EC09、EC10、EC11、EC12。
- **Main Points / Transition**：固定 `DESIGN / NOT IMPLEMENTED / NOT RUN / EXPERIMENT COUNT 0 / OBSERVED RESULT ABSENT`。候选恰好四个：

  | Candidate | Trigger / exclusion | Designed artifact | Failure |
  |---|---|---|---|
  | `jenkins-build-triage` | job/build/stage/log；不改 Job、不重跑 | first stage、log locator、artifact inventory | credential、truncation、identity drift、screenshot-only |
  | `unity-compile-diagnosis` | compiler/player error；非一般 CDN | version/target/first error/source packet | stale log、generated pollution、missing revision |
  | `yooasset-artifact-chain-audit` | manifest/package/cache/download/remote；非纯 compile | source/config/artifact/runtime/request/cache/remote matrix | request 冒充 bytes、build success 冒充 usable、无 remote readback |
  | `release-evidence-pack` | investigation handoff；不发布 | claim map、gaps、decision log | locator 冒充 verification、proposal 冒充 observed、越权发布 |

  时序：Jenkins Skill -> Runtime exact read -> first-stage verification -> compile OR resource Skill -> artifact + UNKNOWN -> Workflow terminal -> Harness receipt。Ownership：方法=Skill；读/执行=Tool Runtime；guard/terminal=Workflow；permission/sandbox=Policy；catalog/context/trace=Host/Harness；动态知识=KB/Memory；claim acceptance=下游 Evidence。Decision log 记录 candidate/version/reason/resources/permission/artifacts/terminal/context disposition。转入“是否该建”。
- **Wording Boundary**：本节全部对象与时序 = DESIGN；effectiveness、根因、production status = UNKNOWN。
- **BuildPilot Responsibility**：唯一完整案例；不得增加第五候选或平行案例。
- **Must Not Claim**：四 Skill 已存在、触发、有效；已读真实系统；已发布；artifact 已成为 Evidence；Runtime 已完成。
- **Visual**：四 Skill swimlane + ownership matrix，DESIGN / NOT RUN 水印。

## 11. 工程判断：什么时候不要创建 Skill

- **Reader Question**：何时建 Skill，何时留在 Prompt、Tool、Workflow、Agent、KB 或 Policy？
- **Purpose / Teaching Move**：用 fail-closed checklist 阻止 Skill 泛化。
- **Claims / Evidence**：17-C03、C07、C08、C15；17-EC01、EC08、EC09、EC12。
- **Main Points / Transition**：适合重复、领域特定、可行动、仅部分任务需要、可写 near-miss / observable acceptance、值得独立 review/version。以下不建：一次性请求；全局规范；新能力；确定性编排/approval/publish authority；独立身份/隔离；动态事实；无法写 failure semantics 的建议。Checklist：重复价值？正负 trigger？I/O/provenance/freshness/failure？确为方法？可 progressive disclosure？scripts 有 dependency/permission/sandbox/validator？owner/version/rollback/retirement？有 baseline/fixture/assertions/trace/human review？多项否则分流。
- **Wording Boundary**：SUPPORTED PRACTICE + COURSE DESIGN；不是准入标准，不保证收益。
- **BuildPilot Responsibility**：拒绝万能 Skill；四候选也需逐个过清单，当前无实测通过。
- **Must Not Claim**：重复即应建；清单通过即有效；Skill 数量代表成熟度。
- **Visual**：yes/no 分流到 Prompt / Skill / Tool / Workflow / Agent / KB / Policy。
- **Transition**：最后回到零实验 ceiling。

## 12. 验证边界与最短结论

- **Reader Question**：哪些已有证据，哪些必须 UNKNOWN？下游从哪里接手？
- **Purpose / Teaching Move**：并列 FACT、DESIGN、UNKNOWN，锁住 Draft 语态。
- **Claims / Evidence**：17-C01—C15；17-EC01—EC12。
- **Main Points / Transition**：已有开放 anatomy/disclosure 与 scoped 产品差异；课程设计包括 production contract、lifecycle、receipt、BuildPilot 四候选；UNKNOWN 包括统一 precedence/collision/unload/inheritance 及 accuracy、quality、token、latency、cost、trigger precision/recall、success、production benefit。Article 18 接 Evidence，19 接 Permission，22 接 Eval；Harness / DSH / BuildPilot capability 留后文。最短结论：`Skill Engineering 不是给 Prompt 换目录，而是让领域方法可发现、按需装载、独立验证、受控执行，并在不适用时退出。`
- **Wording Boundary**：结论表达设计能力，不表达已测效果。
- **BuildPilot Responsibility**：只重申 DESIGN / experiment 0，不增模块或结果。
- **Must Not Claim**：本文证明有效 / 无效；任何效果数；后续完整合同或 Runtime 已实现。
- **Visual**：Confirmed Facts / Course Designs / Unknown 三栏；无新 transition。

## Human Question Coverage（10 / 10）

| Question | Sections |
|---|---|
| 1 定义与问题 | 1、2 |
| 2 与 Prompt/Tool/Workflow/Agent/KB 边界 | 2、8、10 |
| 3 按需方法而非 Prompt 堆叠 | 1、3、6 |
| 4 discover/select/load/execute/verify | 4、10 |
| 5 I/O/依赖/权限/范围/失败 | 5、7 |
| 6 context pollution | 3、6 |
| 7 通用 Agent + Skill vs 专用 Agent | 8 |
| 8 何时建 / 不建 | 11 |
| 9 测试/版本/审查/回归 | 6、9、12 |
| 10 BuildPilot ownership | 2、4、5、7、10、11 |

## Claim-to-section Traceability（15 / 15）

| Claim | Ceiling | Sections | Evidence |
|---|---|---|---|
| C01 | FACT | 3、5、12 | EC01 |
| C02 | FACT | 3、4、12 | EC01、EC02 |
| C03 | INFERENCE | 1、2、3、11、12 | EC01—EC04、EC09 |
| C04 | FACT | 4、6、12 | EC02—EC04、EC07 |
| C05 | INFERENCE | 1、3、6、12 | EC01—EC03、EC08 |
| C06 | UNKNOWN | 4、6、12 | EC02—EC07 |
| C07 | DESIGN | 2、5、7、10、11、12 | EC01、EC08—EC10 |
| C08 | FACT | 6、9、11、12 | EC08 |
| C09 | FACT | 9、12 | EC03—EC05 |
| C10 | FACT | 2、5、7、9、10、12 | EC04、EC06、EC07、EC10 |
| C11 | FACT | 2、8、12 | EC06、EC07 |
| C12 | DESIGN | 4、5、7、9、10、12 | EC02、EC05、EC08、EC11 |
| C13 | FACT | 3、8、12 | EC03—EC07 |
| C14 | DESIGN | 2、10、12 | EC09—EC11 |
| C15 | UNKNOWN | 1、6、9、10、11、12 | EC08、EC12 |

Coverage 15 / 15；新核心事实、效果数字、统一 runtime 语义或 BuildPilot observation => RETURN_TO_RESEARCH。

## Figures / Tables / Checklists

- S1—S4：Prompt pile、八对象矩阵、disclosure 三层、lifecycle swimlane。
- S5—S8：format/contract、trigger 2x2、trust chain、Agent 变化轴。
- S9—S12：lifecycle、BuildPilot ownership、create-or-not、evidence ceiling。
- BuildPilot 图固定 DESIGN / NOT RUN；效果数固定 UNKNOWN / NOT MEASURED。

## Learning Check

1. 移入 Skill 文件后还缺哪些 engineering 条件？为什么仍与 Prompt 有交集？
2. disclosure 减少什么、保留哪些成本？false / missed trigger 如何分测？
3. production contract、permission enforcement、Tool / Workflow ownership 如何分账？
4. 何时应升级为专用 Agent，能否假定 subagent 继承 Skill？
5. validator、eval、pin、rollback 各证明什么？实验数 0 能否声称效果？
6. 为何 BuildPilot 恰好四候选，且 evidence pack 不能发布？

## Job Competency Mapping

| Competency | Outcome | Boundary |
|---|---|---|
| Architecture / Context | 分开八对象，审 disclosure / trigger | course model；no measured effect |
| Contract / Security | 审 scope、I-O、dependency、permission、failure、provenance | not spec / certification |
| Lifecycle / Multi-agent | test/version/rollback/retire；按 isolation 选 Agent | NOT RUN；product-specific |
| Incident / Evidence | 拆四个 BuildPilot Skill，保持证据等级 | DESIGN；Article 18 downstream |

## Explicit Non-scope

- 不定义统一格式、trigger、precedence、collision、merge、unload、context-end、inheritance。
- 不重写前文，不提前写 Article 18 / 19 / 22、Harness、DSH、BuildPilot capability design 或 Multi-Agent topology。
- 不实现 BuildPilot，不创建 Skill / script / fixture / Lab / provider run，不触碰 Jenkins / Unity / YooAsset / release。
- 不增加第五个 BuildPilot Skill；四候选均 DESIGN / NOT IMPLEMENTED / NOT RUN。
- 不声明任何 accuracy、quality、token、latency、cost、precision/recall、success、adoption、production benefit 或事故根因。
- locator、request、build success、validator、pin、scan、sandbox、citation 都不自动等于 Verification / Permission / production assurance。
