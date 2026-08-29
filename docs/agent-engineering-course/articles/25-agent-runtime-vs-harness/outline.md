# Article 25 Outline｜Agent Runtime vs Harness：执行内核与工程控制面

## Outline contract

- Article Type: `PRINCIPLE`
- Course Weight: `L / Major Core Lesson`
- Required Lab: `NONE`
- Experiment Count: `0`
- Runtime Observation: `ABSENT`
- BuildPilot: `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`
- Teaching Spine: Article 24 established why a shared Harness boundary appears -> problem space（产品边界、厂商术语和模块名字不能直接当责任边界）-> responsibility-based abstract model（owner / state / invariant / failure / replacement 五问）-> layer allocation（Host、Business Agent / Workflow、Runtime、Harness、Agent Framework / Workflow Engine）-> concrete BuildPilot allocation case（需求变更链按层分账）-> engineering judgment（可替换性、失败定位、状态所有权和 same-product multi-layer packaging）-> Article 26 / 27 bridge
- Core Claim Scope: `25-C01`—`25-C12` only；不新增 core Claim / Evidence Card
- Evidence Posture: `4 CONFIRMED / 6 PARTIAL / 2 PROPOSAL / 0 BLOCKED`
- Claim Coverage: `12 / 12`
- Evidence Cards: `12 / 12`
- Proposal Discipline: Runtime / Harness / Host / Business split 是本课程的教学 taxonomy；不能写成行业标准、厂商标准或唯一架构。五问测试 `25-C11` 是 course proposal，不是外部来源确认的方法论。
- BuildPilot Discipline: BuildPilot 只作为 bounded allocation design case；必须持续保留 `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`。不能说它已经实现、运行、扫描 Unity、查询 Jenkins、创建 PR、修改项目、生成真实 BuildReport 或完成生产验证。
- Future Boundary: Article 26 才展开 Harness 的最小能力模型；Article 27 才讨论复杂度、Bloat、可替换性、演化和采纳时机。Article 25 只能点到这些主题作为边界，不提前完成后两篇。
- Draft fact boundary: Draft 只能重组 `research.md`、`evidence.md`、Article 25 card/README、series plan、glossary、Published Article 24 以及必要前文边界。若需要新的外部事实、实现事实、运行观测、BuildPilot 证据或完整 capability schema，必须 `RETURN_TO_RESEARCH`。

> 如果这篇只记一句话：`Runtime 负责把一次 Agent Run 推进下去；Harness 负责让跨 Run、跨 Tool、跨 Workflow 的身份、权限、证据、预算、Trace、审批、恢复和能力暴露保持同一种可审计语义。`

## Reader transformation

读者开始时可能会把 Runtime、Harness、Host、Agent Framework、Workflow Engine 和业务 Agent 当成几个可互换的产品名。文章结束时，读者应能：

1. 解释为什么产品模块名、SDK 类名或厂商术语不能直接决定责任边界。
2. 用 owner、state、invariant、failure、replacement 五个问题判断一项责任应该放在哪一层。
3. 区分 Host、Business Agent / Workflow、Agent Runtime、Harness、Agent Framework / Workflow Engine 各自自然拥有的责任。
4. 说明 Runtime 为什么更接近执行推进者：模型调用、工具分派、等待、续跑、状态推进和停止边界。
5. 说明 Harness 为什么更接近共享工程控制面：identity、permission、approval、sandbox、budget、trace、evidence、checkpoint/replay policy、registry/discovery 和 recovery convention。
6. 区分 Context assembly 与 Context policy，不把“装配给模型看的材料”和“什么可以暴露、保留、脱敏、压缩、重建”写成同一件事。
7. 区分 business state、execution state、governance state、host/UI state 四类状态 owner 和生命周期。
8. 解释 failure / retry / recovery / human takeover 为什么要拆成执行机制和治理决策。
9. 用 BuildPilot 需求变更链把 Host、业务逻辑、Runtime、Harness 和 Owner 的责任分账，同时不把设计案例伪装成实现事实。
10. 在看到同一产品包下多个模块时，保留概念边界，不把“同进程实现”误读成“同一责任”。

## Teaching Spine

```text
Article 24 says a shared carrying boundary is needed
  -> Article 25 asks where that boundary sits relative to execution
  -> product names and vendor terms overlap, so names cannot decide architecture
  -> use five questions: owner, state, invariant, failure, replacement
  -> split Host / Business Agent or Workflow / Runtime / Harness by responsibility
  -> keep Agent Framework and Workflow Engine as packaging/programming-model examples,
     not as universal layer names
  -> Runtime owns execution progression and local run mechanics
  -> Harness owns shared governance semantics and audit/recovery conventions
  -> Context assembly differs from context policy
  -> business, execution, governance and host/UI state have different owners
  -> registry/discovery, budget, trace, evidence, checkpoint and replay cross layers
  -> failure handling splits retry mechanics from safe-retry / takeover decisions
  -> BuildPilot requirement-change chain shows the allocation as design-only
  -> same product can implement many layers, but replacement pressure still exposes boundaries
  -> close by handing Article 26 the minimum Harness model and Article 27 the trade-off question
```

### Spine checkpoints

| Stage | Reader transformation | Required article artifact | Failure if omitted |
|---|---|---|---|
| Problem pressure | 从“哪个产品模块叫 Harness”转向“哪项责任由谁拥有” | vendor terminology counter-evidence + product-boundary warning | 文章变成 SDK / 产品名词解释 |
| Abstract model | 能用五问测试切责任边界 | owner/state/invariant/failure/replacement table | Runtime/Harness split 变成口号 |
| Layer allocation | 能比较 Host、业务 Agent/Workflow、Runtime、Harness、Framework/Workflow Engine | responsibility ledger + four-state-owner model | Harness 被写成 God Object 或 Runtime 被写成 policy brain |
| Concrete mechanism | 能判断执行、context、policy、registry、trace、budget、recovery 在哪一层发生 | execution/governance/cross-layer tables | 只停留在抽象概念，不可用于设计审查 |
| Design case | 能把同一需求变更链按层分账 | BuildPilot allocation sequence + failure/takeover examples | BuildPilot 被误写成已实现、已运行或自动改项目 |
| Engineering boundary | 能处理 same-product multi-layer packaging 与替换压力 | anti-pattern table + replacement matrix + Article 26/27 bridge | 提前写完后续 Capability model 或 trade-off framework |

## Opening bridge｜Article 24 回答了“为什么”，Article 25 回答“谁负责”

- Reader Question: 既然 Article 24 已经说明为什么需要 Harness，为什么还要单独写 Runtime vs Harness？
- Core Questions: `25-C01`、`25-C07`、`25-C10`、`25-C11`。
- Claims / Evidence: `25-C01 CONFIRMED / 25-E01`，`25-C07 PARTIAL / 25-E07`，`25-C10 CONFIRMED / 25-E10`，`25-C11 PROPOSAL / 25-E11`，辅助 Published Article 24。
- Planned teaching move:
  - 接住 Article 24 的终点：共享控制边界已经出现，但它还没有定义“执行内核”和“工程控制面”的相对位置。
  - 直接指出新的混淆：团队常把“运行模型调用的 loop”“承载审批与证据的控制面”“宿主应用”“业务任务编排”全部叫 Agent 系统，然后按产品模块名分工。
  - 立刻建立证据上限：本篇不是行业 taxonomy 文章，也不是任何厂商 SDK 的类图教程；证据显示厂商术语存在重叠，课程只能按责任和不变量教学。
- Boundary / Non-goal:
  - 不重讲 Article 24 的横切能力为什么出现。
  - 不把 Microsoft / LangChain / OpenAI / MCP 任一术语当成课程统一标准。
  - 不提前给 Article 26 的 Capability / Policy / Session / Trace / Recovery 完整模型。
- Transition purpose: 从“为什么需要共享承载边界”推进到“责任边界怎样切”。
- Learning check: 如果一个产品文档把 model call、tool call、approval、state 和 UX 都放在同一个 harness 概念下，课程还能不能说 Runtime 与 Harness 要区分？期望答案：可以，但必须说这是课程 responsibility split，不是对该产品命名的纠错。
- Section takeaway: **Article 25 不按产品名分类，而按责任、状态、不变量、失败和替换压力分类。**

## Part A｜问题空间：产品边界不等于责任边界

### 1. 同一个产品可以同时实现 Host、Runtime、Harness 和业务 Agent

- Reader Question: 为什么不能看到某个产品有 `AgentHarness`、`Runner`、`Workflow` 或 `App Host` 就直接把它归为某一层？
- Core Questions: `25-C10`、`25-C11`。
- Claims / Evidence: `25-C10 CONFIRMED / 25-E10`，`25-C11 PROPOSAL / 25-E11`。
- Planned teaching move:
  - 用一个产品内多模块打包的现实开场：CLI / IDE 插件 / Web 应用可能是 Surface；进程或服务可能是 Host；内部 runner 推进 Agent loop；approval / sandbox / trace / budget 规则又可能在同一个代码库里。
  - 说明“同产品实现多层”不稀奇，真正危险的是把实现打包方式误当概念边界。
  - 引入 vendor terminology counter-evidence：不同公开来源对 runtime、framework、harness、host、workflow 的词义并不一致。
- Required table `T25-01`:

  | What you see in a product | Why it is not enough | What Article 25 asks instead |
  |---|---|---|
  | 一个叫 Harness 的模块 | 可能同时包含 execution、state、approval、UX | 它保护什么不变量，保存什么治理状态？ |
  | 一个 Runner / Runtime API | 可能内置 tool policy、session 或 tracing hook | 它是在推进执行，还是在定义共享治理语义？ |
  | 一个 Workflow Engine | 可能只表达确定性步骤，也可能包装 Agent call | 它拥有 business sequence，还是拥有跨 workflow policy？ |
  | 一个 Host / App | 可能承载 UI、workspace、clients、permission prompt | 它只是环境边界，还是也承担控制面？ |
  | 一个 Agent Framework | 可能提供开发抽象，也可能附带 runtime/harness 功能 | 哪些能力替换 framework 后应该保留？ |

- Evidence wording:
  - `25-E10` 支持“厂商术语方差存在，因此不能宣称课程 split 是行业标准”。
  - 本节只使用术语差异作为反证，不评价任一厂商命名对错。
- Boundary / Non-goal:
  - 不用 vendor class names 建立 taxonomy。
  - 不写成“某某产品错把 Harness 包含 Runtime”；只能说“产品打包方式与课程责任边界不同”。
- Transition purpose: 名字不可靠，所以需要一套不依赖名字的判断方法。
- Practical action: 让读者审查一个 Agent 产品时先标出 capabilities 和 state owners，再标产品模块名，顺序不要反过来。
- Section takeaway: **产品可以把多层装在一起，但责任边界不能由包名决定。**

### 2. 混淆的代价：失败定位、审计和替换都会失去锚点

- Reader Question: 如果同一系统最后都能跑，Runtime 和 Harness 分不清到底会坏在哪里？
- Core Questions: `25-C07`、`25-C08`、`25-C09`、`25-C10`。
- Claims / Evidence: `25-C07 PARTIAL / 25-E07`，`25-C08 PARTIAL / 25-E08`，`25-C09 PARTIAL / 25-E09`，`25-C10 CONFIRMED / 25-E10`。
- Required failure examples:
  - Execution success 被误当 evidence acceptance：Tool 返回成功后，Draft 直接写成 confirmed fact。
  - Runtime retry 被误当 safe recovery：同一个写副作用 intent 在 effect unknown 时被重新执行，没有 policy 决策。
  - Host permission prompt 被误当 business approval：用户允许读目录，不等于 owner 批准变更方案。
  - Workflow state 被误当 business state：流程跑到 `APPROVED`，但业务需求、owner、证据或 diff 已经 stale。
  - Trace existence 被误当 replayability：有日志，但缺 checkpoint、manifest、redaction 和 effect boundary。
- Required table `T25-02`:

  | Boundary collapsed | Immediate illusion | Later failure |
  |---|---|---|
  | Runtime = Harness | 执行 loop 只要能跑就像已治理 | 权限、证据、预算和审批语义随每个 loop 漂移 |
  | Host = Harness | App 弹窗像完整授权 | workspace / client 权限不等于业务风险决策 |
  | Workflow = Harness | 步骤固定像共享控制已解决 | 跨 workflow 的 identity、evidence、approval、recovery 仍不一致 |
  | Framework = architecture | API 名字像责任已经明确 | 替换 framework 后不知道哪些状态和不变量必须迁移 |
  | Business Agent = everything | 领域判断像能顺手管治理 | 业务逻辑吞掉权限、预算、Trace 和恢复，难审计也难替换 |

- Boundary / Non-goal:
  - 不声称所有项目都必须物理拆成五个服务。
  - 不讨论 Article 27 的成本阈值，只说边界混淆会造成定位和替换困难。
- Transition purpose: 混淆的坏处不是概念洁癖，而是需要一个可操作的边界测试。
- Learning check: 为什么“Host 已经弹过权限确认”不能证明“业务变更获批”？期望答案：Host 权限只描述环境/capability access，业务 approval 要绑定需求、证据、owner、scope 和 stale condition。
- Section takeaway: **责任混成一团时，最先消失的是失败发生在哪一层、由谁批准、替换时保留什么。**

## Part B｜抽象模型：用五问切责任，而不是用名词切责任

### 3. 五个问题：owner、state、invariant、failure、replacement

- Reader Question: 不靠厂商名字时，怎样判断一项能力应该放到哪一层？
- Core Questions: `25-C08`、`25-C09`、`25-C11`。
- Claims / Evidence: `25-C08 PARTIAL / 25-E08`，`25-C09 PARTIAL / 25-E09`，`25-C11 PROPOSAL / 25-E11`。
- Proposed boundary test `F25-01`:

  ```text
  Concern / responsibility
    -> Owner: who is accountable for the decision?
    -> State: what state does it create, read or mutate?
    -> Invariant: what must remain true across runs, tools or workflows?
    -> Failure: how does it fail, and who can classify/recover it?
    -> Replacement: if model/runtime/framework/host changes, should it survive?
       -> place responsibility by answers, not by class name
  ```

- Required table `T25-03`:

  | Question | It reveals | Typical Runtime answer | Typical Harness answer |
  |---|---|---|---|
  | Owner | 谁对决定负责 | runner / executor 对 step 推进负责 | governance owner 对 policy / approval / evidence semantics 负责 |
  | State | 产生或修改哪类状态 | run state、step state、tool result、continuation | permission state、approval record、budget ledger、trace/evidence policy |
  | Invariant | 哪条规则必须持续成立 | stop condition、tool-call lifecycle、local consistency | least privilege、auditability、evidence boundary、budget ceiling、replay policy |
  | Failure | 失败怎样分类与恢复 | timeout、tool error、model error、wait/resume mechanics | safe retry、human takeover、policy violation、evidence rejection |
  | Replacement | 替换后什么必须保留 | execution implementation 可替换 | governance records、business authority 和 accepted evidence 必须可迁移或可审计 |

- Boundary / Non-goal:
  - 五问不是 formal method，不保证自动给出唯一答案。
  - 这是 Article 25 的教学工具；Article 26 才会进一步形成最小 Harness 能力模型。
- Transition purpose: 有了五问，下一节把常见层次放在同一张责任表里。
- Practical action: 评审一个 Agent 设计时，为每个 concern 填五问；如果一个字段同时改变 business、execution 和 governance state，优先拆开。
- Section takeaway: **边界不是看名字，而是看谁负责、改什么状态、守什么不变量、怎样失败、替换后该留下什么。**

### 4. Layer responsibility ledger：Host、Business、Runtime、Harness、Framework / Workflow Engine

- Reader Question: 这些层各自自然拥有的职责是什么？它们为什么可以同进程实现但概念上仍要分开？
- Core Questions: `25-C01`、`25-C02`、`25-C04`、`25-C08`、`25-C10`、`25-C11`。
- Claims / Evidence: `25-C01 CONFIRMED / 25-E01`，`25-C02 CONFIRMED / 25-E02`，`25-C04 PARTIAL / 25-E04`，`25-C08 PARTIAL / 25-E08`，`25-C10 CONFIRMED / 25-E10`，`25-C11 PROPOSAL / 25-E11`。
- Required table `T25-04`:

  | Layer / lens | Owns | State it should own | Invariant it protects | Does not own alone |
  |---|---|---|---|---|
  | Host | application / environment / workspace / user interaction boundary | UI state、workspace roots、client lifecycle、environment config、host-visible context sources | user-visible boundary and environment coordination remain explicit | domain judgment、all governance semantics、model loop correctness |
  | Business Agent / Workflow | domain goal interpretation, domain order, suggested business action | requirement candidate、task-specific plan、domain finding、owner conversation state | business meaning and owner accountability stay outside the execution engine | shared permission、evidence acceptance、budget policy、global trace semantics |
  | Agent Runtime | execution progression | run state、turn/step state、tool invocation result、handoff/wait/final/error boundary | execution advances through legal mechanics and stops at declared boundaries | business authority、evidence acceptance、shared approval lifecycle |
  | Harness | shared engineering control plane around execution | identity ledger、permission/approval state、budget ledger、trace/evidence policy refs、checkpoint/replay policy、registry/discovery governance | cross-run and cross-surface semantics remain consistent, auditable and recoverable | domain intent、all UI state、every tool implementation、full product strategy |
  | Agent Framework | reusable programming model and developer surface | framework config、agent definitions、middleware hooks、adapter wiring | implementations can be composed consistently inside that framework | universal architecture boundary or proof of governance correctness |
  | Workflow Engine | structured transition / durable process mechanism | workflow instance state、step/superstep/checkpoint records | deterministic structure, state isolation and resumability inside the workflow model | all Agent reasoning, all Harness policy, business truth |

- Evidence wording:
  - `25-C04` remains `PARTIAL` because workflow/state mechanisms differ across products.
  - Framework / Workflow Engine rows are comparison lenses, not new course layers.
- Boundary / Non-goal:
  - 不要求这些层物理分仓、分进程、分团队。
  - 不把 Business Workflow 写成 Harness；Workflow 可承载业务顺序，Harness 承载共享控制语义。
- Transition purpose: 责任表建立后，再深入四类 state owner，否则状态会被全部塞进 runtime loop。
- Learning check: 替换 Runtime 后，哪些东西不应该消失？期望答案：业务事实、owner 决策、approval scope、evidence acceptance、budget/trace/replay policy 等 governance/business records 不应只活在 runtime-local state 里。
- Section takeaway: **同一个产品可以实现多层，设计审查仍要按责任 ledger 分账。**

### 5. 四类 State owner：business、execution、governance、host/UI

- Reader Question: 为什么“Agent state”这个词太粗？一份 state 为什么不该吞掉所有事实？
- Core Questions: `25-C04`、`25-C08`、`25-C09`。
- Claims / Evidence: `25-C04 PARTIAL / 25-E04`，`25-C08 PARTIAL / 25-E08`，`25-C09 PARTIAL / 25-E09`。
- Required state split `T25-05`:

  | State type | Owner | Lifetime | Example | If stored in wrong place |
  |---|---|---|---|---|
  | Business state | Business Agent / Workflow + Owner | survives runtime replacement and must stay meaningful to the domain | requirement contract candidate、finding disposition、owner decision | Runtime retry may overwrite domain truth or hide owner accountability |
  | Execution state | Runtime / Workflow Engine | bound to run, step, graph or durable execution instance | current step、pending tool call、wait boundary、local continuation | Governance records vanish when execution is compacted or rerun |
  | Governance state | Harness | survives across runs/tools/workflows as audit and control fact | approval scope、permission grant、budget reservation、evidence acceptance、trace policy | Each workflow invents its own `APPROVED` / `PASS` / `RETRY` meaning |
  | Host/UI state | Host | bound to application surface, workspace and interaction | selected project、visible files、workspace roots、active user prompt | UI convenience accidentally becomes business authority |

- Boundary / Non-goal:
  - 不声明这四类是外部标准；这是 source-supported course allocation，保持 `PARTIAL`。
  - 不设计 Article 26 的 Session/Event schema。
- Transition purpose: state owner 清楚后，再处理最容易混的 context assembly 与 context policy。
- Practical action: 对一个 pending approval 或 checkpoint 记录，标注它属于哪类 state；如果同时承载四类含义，拆成 refs。
- Section takeaway: **State 的核心问题不是放在哪里，而是谁拥有、活多久、替换哪一层时还能不能解释。**

### 6. Context assembly vs context policy：装配动作和治理规则分开

- Reader Question: Runtime 给模型拼 context，是否意味着 Runtime 也拥有 context 的暴露、保留、脱敏和预算规则？
- Core Questions: `25-C05`、`25-C08`。
- Claims / Evidence: `25-C05 PARTIAL / 25-E05`，`25-C08 PARTIAL / 25-E08`，辅助 Published Article 12。
- Required distinction:
  - Context assembly: select、order、compress、inject、rehydrate、pass local context；这是 execution-time operation，通常由 Runtime 或 Workflow step 执行。
  - Context policy: allowed source、sensitivity、retention、redaction、budget priority、cross-run reuse、staleness、receipt requirement；这是 governance decision，属于 Harness 的共享语义。
  - Host supplies or limits environment-visible context sources；Business Agent decides which domain facts are relevant to the current task.
- Required table `T25-06`:

  | Context concern | Natural owner | Why |
  |---|---|---|
  | 当前 Step 需要把哪些片段放进 model-visible context | Runtime / Business Agent together | execution needs concrete assembly; domain relevance comes from task logic |
  | 哪些项目文件、资源、会话或 roots 对系统可见 | Host | environment and workspace boundary |
  | 哪些数据允许跨 run 保留、脱敏、压缩或重建 | Harness | shared policy must be consistent and auditable |
  | 哪些 context receipt 可以被后续 evidence / trace / eval 引用 | Harness with Runtime-produced refs | Runtime records what was assembled; Harness defines acceptance and retention semantics |

- Boundary / Non-goal:
  - 不重讲 Article 12 的 Context Snapshot / Receipt 细节。
  - 不创建完整 context store、session store 或 retrieval policy；Article 26/34/37 后续处理。
- Transition purpose: context split gives a template，下一组 concerns 也要拆 execution fact 与 governance semantics。
- Learning check: 为什么“Runtime 负责拼 prompt”不能推出“Runtime 负责所有 context policy”？期望答案：拼接是执行动作，暴露/保留/脱敏/预算/跨 run 复用是共享治理规则。
- Section takeaway: **Runtime 可以组装 context，但 Harness 要定义哪些 context 允许被看见、保留、引用和复用。**

## Part C｜具体机制：执行内核与工程控制面怎样分账

### 7. Runtime：执行推进者，而不是事实接受者

- Reader Question: Agent Runtime 的最小责任是什么？
- Core Questions: `25-C01`、`25-C03`、`25-C04`。
- Claims / Evidence: `25-C01 CONFIRMED / 25-E01`，`25-C03 CONFIRMED / 25-E03`，`25-C04 PARTIAL / 25-E04`。
- Required flow `F25-02`:

  ```text
  receive run input
    -> load run/execution state
    -> assemble current context under policy constraints
    -> call model
    -> interpret final/tool/handoff/wait/error output
    -> dispatch tool or workflow step when allowed
    -> normalize observation / result refs
    -> update execution state
    -> stop, wait, recover, or continue
  ```

- Runtime owns:
  - model call progression；
  - turn / step loop；
  - tool dispatch mechanics；
  - handoff / continuation / wait mechanics；
  - local run state and stop/error/max-turn boundaries；
  - workflow or graph execution mechanics when that is the selected execution model。
- Runtime does not own alone:
  - whether a tool request is authorized for this user / task / resource；
  - whether a result is accepted as evidence；
  - whether retry is safe after unknown side effect；
  - whether budget should admit the next action；
  - whether an owner approval is stale。
- Evidence wording:
  - `25-C01` confirms execution progression is a documentable responsibility.
  - 不说每个 SDK 都有单独 `AgentRuntime` class。
- Transition purpose: 明确执行内核后，下一节把 identity、permission、approval、sandbox 放到治理控制面。
- Practical action: 画一次 Agent Run 时，用不同颜色标出“mechanic happened”和“decision accepted”；不要让 Runtime 箭头直接等于 Evidence / Approval。
- Section takeaway: **Runtime 的核心是推进执行和保存执行位置，不是替系统接受事实或授予业务权限。**

### 8. Harness：Identity、Permission、Approval、Sandbox 的共享语义

- Reader Question: 如果 Runtime 已经能执行，Harness 为什么还要介入身份、权限、审批和沙箱？
- Core Questions: `25-C02`、`25-C03`、`25-C06`、`25-C09`。
- Claims / Evidence: `25-C02 CONFIRMED / 25-E02`，`25-C03 CONFIRMED / 25-E03`，`25-C06 PARTIAL / 25-E06`，`25-C09 PARTIAL / 25-E09`，辅助 Published Article 19。
- Required chain `F25-03`:

  ```text
  action intent
    -> stable actor / run / step / resource identity
    -> permission check
    -> risk route: deny / auto-allow / approval-required
    -> approval request bound to frozen scope
    -> sandbox / capability scope enforcement
    -> execution by Runtime / Tool Runtime
    -> decision record + trace/evidence refs
    -> resume / stop / human takeover route
  ```

- Required distinctions:
  - Identity is not just user name; it must join run, step, action, resource, evidence, budget and approval records.
  - Permission is not approval; permission describes allowed capability/resource boundary, approval binds a concrete risky action to a decision scope.
  - Sandbox is not a safety promise; it is a declared limitation surface with known gaps.
  - Host may enforce environment boundary, but Harness owns shared governance semantics if multiple runs/tools/workflows depend on the same meaning.
- Boundary / Non-goal:
  - 不设计完整 permission matrix、approval schema 或 sandbox implementation。
  - 不说 Human Review 能保证安全；它只是 gate，仍需 state / scope / stale / resume semantics。
- Transition purpose: 权限与审批只是治理的一组；下一节处理 budget、trace、evidence、checkpoint、replay 这些容易互相冒充的控制事实。
- Learning check: Tool call 被允许执行后，为什么还不能说 finding 已被接受？期望答案：授权只允许动作进入执行；evidence acceptance 还要看来源、观测、证明范围、反证和验收规则。
- Section takeaway: **Harness 不是替 Runtime 执行动作，而是让动作获权、审批、限制和恢复拥有同一套可审计语义。**

### 9. Budget、Trace、Evidence、Checkpoint、Replay：相关但不等价

- Reader Question: 为什么这些可靠性机制不能被塞成一个 `run metadata`？
- Core Questions: `25-C07`、`25-C08`、`25-C09`。
- Claims / Evidence: `25-C07 PARTIAL / 25-E07`，`25-C08 PARTIAL / 25-E08`，`25-C09 PARTIAL / 25-E09`，辅助 Published Articles 11、18、20、21、22。
- Required ledger `T25-07`:

  | Concern | Runtime can produce / execute | Harness must define / preserve | Must not impersonate |
  |---|---|---|---|
  | Budget | token/step/cost/latency observation, stop on local boundary | admission, reservation, exhaustion route, reconciliation and owner-visible ledger | usage report after the fact |
  | Trace | span/event/step/tool records | trace identity, required fields, redaction, joinability and audit semantics | evidence acceptance or root cause |
  | Evidence | observation refs and raw result links | claim acceptance policy, source class, limitations and wording ceiling | HTTP 200, tool success or log existence |
  | Checkpoint | saved execution position and continuation data | what must be stable, what unknown remains, when resume is valid | full memory, proof of side-effect safety |
  | Replay | rehydrate or reconstruct execution under available data | replayability manifest, effect boundary, redaction and comparison rules | rerun, repair or authorization |

- Required boundary sentences:
  - “Trace shows what happened; Evidence decides what a claim may rely on.”
  - “Checkpoint helps resume; Recovery decides whether resume/retry/reconcile/ask/stop is allowed.”
  - “Budget must constrain future action, not only summarize past usage.”
- Boundary / Non-goal:
  - 不重讲 Article 18—22 的完整模型。
  - 不写 Article 26 的 data model，也不引入新的 Lab output。
- Transition purpose: 有了可靠性 concern ledger，下一节再讲 registry/discovery，因为 capability exposure 同时影响 execution 与 governance。
- Practical action: 对一次失败 run 做五列归档：budget、trace、evidence、checkpoint、replay；任一列缺失都不要用另一列代替。
- Section takeaway: **这些机制彼此关联，但每一个都只能证明自己的证据层，不能替别的层作决定。**

### 10. Registry / Discovery：看见能力、选择能力和治理能力是三件事

- Reader Question: Tool / Skill Registry 与 Capability Discovery 到底归 Runtime、Harness 还是 Host？
- Core Questions: `25-C03`、`25-C06`、`25-C10`、`25-C11`。
- Claims / Evidence: `25-C03 CONFIRMED / 25-E03`，`25-C06 PARTIAL / 25-E06`，`25-C10 CONFIRMED / 25-E10`，`25-C11 PROPOSAL / 25-E11`，辅助 Published Articles 06、07、17。
- Required split:
  - Host exposes environment and external capability surfaces: filesystem roots, connected clients, installed tools, user-visible app surfaces.
  - Registry stores tool/skill/workflow definitions, schemas, annotations, version and provider/source metadata.
  - Runtime reads an allowed view and dispatches selected calls.
  - Harness governs visibility, authorization, approval, budget, evidence labels, tool-gap records and capability evolution.
  - Business Agent / Workflow decides which capability is relevant for the domain task.
- Required table `T25-08`:

  | Question | Belongs primarily to | Example |
  |---|---|---|
  | What capabilities exist in this environment? | Host / registry surface | installed MCP servers, local tools, project roots |
  | What capabilities may the model see for this run? | Harness policy + Runtime assembly | per-user/per-project/per-risk allowed view |
  | Which capability helps this domain goal? | Business Agent / Workflow | Unity config scan before packaging advice |
  | How is a selected call executed? | Runtime / Tool Runtime | validate args, invoke, normalize result |
  | How are new capability gaps handled? | Harness + Owner | governed capability evolution, not silent install |

- Boundary / Non-goal:
  - 不展开 Article 26 的 `Capability` contract；只说明 discovery/visibility/execution/governance 的边界。
  - 不说 Tool Registry 本身就是 Harness；registry 是 surface，治理语义才是 Harness concern。
- Transition purpose: Registry split 可以自然落进 BuildPilot：需求变更链会同时碰到 Host、业务判断、Runtime 执行和 Harness 治理。
- Learning check: 为什么“某个工具在 registry 里”不等于“模型本轮应该看见它”？期望答案：存在、可见、相关、获权、预算允许、审批通过和证据可接受是不同问题。
- Section takeaway: **能力发现不是一张工具清单，而是存在、可见、相关、获权、执行和治理的分层链路。**

## Part D｜BuildPilot allocation case：同一条需求变更链怎样按层分账

### 11. BuildPilot read-only requirement-change chain

- Reader Question: 在一个 Unity 需求变更助手里，Host、业务逻辑、Runtime、Harness 和 Owner 分别做什么？
- Core Questions: `25-C11`、`25-C12`。
- Claims / Evidence: `25-C11 PROPOSAL / 25-E11`，`25-C12 PROPOSAL / 25-E12`，辅助 Published Article 24。
- Mandatory label:
  - BuildPilot remains `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`.
  - Owner implements real changes outside BuildPilot.
- Required sequence `F25-04`:

  ```text
  Host receives owner request and workspace/project context
    -> Business Agent / Workflow interprets requirement and chooses domain check order
    -> Harness produces allowed capability view and governance constraints
    -> Runtime executes task graph, model calls and read-only tool checks
    -> Tool Runtime returns normalized observations and raw refs
    -> Harness labels evidence, budget, trace, approval and unknown state
    -> Business Agent / Workflow prepares evidence-backed Change Request
    -> Owner reviews and implements outside BuildPilot
    -> Runtime re-verifies selected read-only checks
    -> Harness stores auditable governance result and capability-gap candidates
  ```

- Required allocation table `T25-09`:

  | Step | Primary owner | State produced | Harness involvement | Proof ceiling |
  |---|---|---|---|---|
  | 接收需求与项目上下文 | Host | host/UI context, workspace refs | identity / root visibility policy | no business approval yet |
  | 解释需求与检查顺序 | Business Agent / Workflow | requirement candidate, domain plan | evidence and approval requirements constrain wording | suggestion, not implementation |
  | 执行只读检查 | Runtime / Tool Runtime | step results, observations, errors | permission, sandbox, budget and trace gates | tool success only |
  | 形成 Change Request | Business Agent / Workflow | finding + proposed change intent + unknowns | evidence labels and owner routing | not a patch, not a fix |
  | 人审与实施 | Owner outside BuildPilot | decision and real project change | approval scope / stale / trace refs | only owner can authorize/implement |
  | 复验 | Runtime / Tool Runtime | fresh read-only observations | evidence acceptance / budget / trace / recovery | no device/runtime production proof |
  | 记录治理结果 | Harness | audit record, capability gap, knowledge candidate | stable semantics across future runs | design-only in Article 25 |

- Boundary / Non-goal:
  - 不设计真实 Unity adapter、schema、UI、PR flow、Jenkins integration 或 asset scan。
  - 不写 “BuildPilot 会自动修复”；只写 “提出建议、保留 evidence、复验 owner 实施后的结果”。
- Transition purpose: 有了正常路径，下一节展示失败和 human takeover 怎样分层。
- Learning check: BuildPilot 输出了 evidence-backed Change Request，能不能说问题已经解决？期望答案：不能。它只是建议与证据包；owner 尚未实施，runtime re-verification 和生产证据也不存在。
- Section takeaway: **BuildPilot 案例的价值是展示责任分账，不是证明一个产品已经运行。**

### 12. Failure / retry / recovery / human takeover：执行机制与治理决策分开

- Reader Question: 一条 BuildPilot 需求变更链失败时，谁决定 retry、resume、reconcile、ask 或 stop？
- Core Questions: `25-C07`、`25-C09`、`25-C12`。
- Claims / Evidence: `25-C07 PARTIAL / 25-E07`，`25-C09 PARTIAL / 25-E09`，`25-C12 PROPOSAL / 25-E12`，辅助 Published Articles 11、19、21。
- Required examples:
  - Read-only tool timeout: Runtime records timeout and can retry only if Harness budget/policy allows.
  - Permission denied: Runtime stops or waits; Harness records deny reason and required owner action.
  - Evidence insufficient: Business Agent cannot upgrade claim; Harness labels `INSUFFICIENT_EVIDENCE`; owner may ask for more evidence.
  - Approval stale: Harness invalidates approval when requirement/diff/evidence/owner scope changes; Runtime resumes only after new decision.
  - Tool gap: Business Agent may identify missing check; Harness routes governed capability evolution candidate; no silent tool install.
  - Owner takeover: Owner implements outside BuildPilot; Runtime may later re-verify declared checks; Harness stores decision and trace linkage.
- Required table `T25-10`:

  | Failure / interruption | Runtime responsibility | Harness responsibility | Business / Owner responsibility |
  |---|---|---|---|
  | Tool timeout | stop, retry mechanically, preserve partial result | decide retry eligibility, budget impact and evidence status | decide whether the missing signal blocks the recommendation |
  | Permission blocked | pause or return failure boundary | preserve deny reason, scope and escalation route | choose whether to grant, narrow or reject |
  | Evidence conflict | surface observations and refs | keep claim status `PARTIAL / BLOCKED / UNKNOWN` as appropriate | revise recommendation or request more evidence |
  | Approval stale | wait for decision before continuing | compare stale conditions and invalidate old approval | re-review changed scope |
  | Unknown side effect | refuse blind duplicate execution | require reconcile / human takeover before retry | confirm external reality or repair manually |

- Boundary / Non-goal:
  - 不写完整 recovery policy 或 incident-response playbook；Article 26 handles minimum model, Article 27 handles cost/complexity.
  - 不把 retry mechanics 写成 recovery correctness。
- Transition purpose: 从 BuildPilot 失败分层回到通用工程判断：同产品多层实现时，替换压力能暴露边界。
- Practical action: 为每个失败写三列：mechanical next step、policy decision、business owner decision；如果三列由同一段 prompt 决定，标记为 boundary risk。
- Section takeaway: **Runtime 可以执行 retry/resume，Harness 决定何时允许，Owner 决定业务风险是否接受。**

## Part E｜工程判断：边界清楚才谈可替换性和演化

### 13. Replacement pressure：换模型、换 Runtime、换 Host 或换 Framework 时，什么应该留下

- Reader Question: 为什么 replacement 是五问里最能暴露边界的一问？
- Core Questions: `25-C08`、`25-C10`、`25-C11`。
- Claims / Evidence: `25-C08 PARTIAL / 25-E08`，`25-C10 CONFIRMED / 25-E10`，`25-C11 PROPOSAL / 25-E11`。
- Required replacement matrix `T25-11`:

  | Replacement scenario | Should change | Should survive / remain auditable |
  |---|---|---|
  | Replace model provider | request/response adapter, usage details, model behavior envelope | evidence records, approval scope, business findings, trace identity, budget policy |
  | Replace Runtime | loop implementation, scheduling, tool-dispatch internals | business state, governance state, accepted evidence, owner decisions, replay/checkpoint policy where applicable |
  | Replace Host surface | UI interaction, workspace integration, client lifecycle | core governance records and business decisions, subject to migrated environment refs |
  | Replace Framework | developer APIs, middleware hooks, registry wiring | course-level responsibility split and durable records |
  | Replace Workflow design | step ordering and deterministic gates | evidence boundaries, owner approvals, audit lineage and capability governance |

- Teaching move:
  - 如果替换 Runtime 会丢掉 approval/evidence/budget/trace 语义，说明治理事实被错误塞进执行局部。
  - 如果替换 Host 会改变业务结论，说明 UI state 和 business state 被混用。
  - 如果替换 Framework 后不知道哪些记录必须迁移，说明产品 API 被误当架构模型。
- Boundary / Non-goal:
  - 不说替换一定容易或总值得做；成本与采纳时机属于 Article 27。
  - 不设计 migration plan。
- Transition purpose: replacement pressure 能帮助总结常见坏设计。
- Learning check: 为什么“换 Runtime 后 trace ID 丢了”不仅是实现问题，也是架构边界问题？期望答案：trace identity 被多个治理面引用，不能只存在 runtime-local transient state。
- Section takeaway: **替换压力会逼出真正的 owner：执行细节可以换，共享治理语义不能无声丢失。**

### 14. 一套 Runtime / Harness 边界通常怎样写坏

- Reader Question: 最常见的错误分账有哪些，怎样最小修正？
- Core Questions: `25-C01`—`25-C12`。
- Claims / Evidence: `25-E01`—`25-E12`。
- Required anti-pattern table `T25-12`:

  | Shortcut | What gets swallowed | Minimum correction |
  |---|---|---|
  | `Runtime can call tools, so Runtime owns authorization` | permission, approval, sandbox scope | Runtime executes only an allowed view; Harness defines and records authority |
  | `Tool success means evidence accepted` | evidence contract and wording ceiling | Tool result becomes observation; Evidence acceptance remains separate |
  | `Host permission prompt is business approval` | owner accountability and stale scope | Host collects interaction; Harness stores approval semantics; Owner accepts business risk |
  | `Workflow has steps, so governance is solved` | cross-workflow identity, budget, trace and recovery | Workflow owns sequence; Harness owns shared semantics |
  | `Harness owns everything` | business intent, domain plan, owner implementation | Harness controls shared invariants; business layer keeps domain judgment |
  | `Framework naming decides architecture` | responsibility analysis | Use owner/state/invariant/failure/replacement test |
  | `Trace + checkpoint means replay is safe` | effect reconciliation and authorization | Replayability manifest and recovery decision stay explicit |
  | `BuildPilot suggestion fixed the project` | implementation and runtime verification | Keep design-only label; owner implements; runtime re-verification is future design |
  | `Article 25 can define the whole Harness model` | Article 26 / 27 boundaries | Only define responsibility split and hand off minimum model/trade-off questions |

- Boundary / Non-goal:
  - 不把所有 bad designs 写成一刀切禁止；它们是 review heuristic。
  - 不扩展成 Article 27 的 adoption / bloat framework。
- Transition purpose: 用坏法压实本篇可证明与不可证明的边界。
- Practical action: 在设计评审中逐条问：这项职责是否被错误地归给能执行它的组件，而不是能负责它的 owner？
- Section takeaway: **最危险的边界错误，是让“能执行”冒充“有权决定”。**

### 15. 本篇能建立什么，不能证明什么

- Reader Question: Article 25 的证明上限是什么？哪些内容必须留给后续文章？
- Core Questions: `25-C01`—`25-C12`。
- Claims / Evidence: `25-E01`—`25-E12`。
- Can establish:
  - Execution progression is a real responsibility cluster：model calls、tool dispatch、handoff/continuation、wait、stop/error/max-turn boundaries。
  - Host can be treated as surrounding application / environment / workspace / user-interaction boundary。
  - Tool discovery/schema/call/result and permission/evidence acceptance are separable。
  - Workflow/state-machine/durable execution mechanisms provide structured transitions and state boundaries, but product boundaries vary。
  - Context assembly and context policy are meaningfully distinct concerns。
  - Identity、permission、approval、sandbox、budget、trace、evidence、checkpoint、replay、registry/discovery and recovery need shared semantics when reused across runs/tools/workflows。
  - Business state、execution state、governance state and host/UI state should be separated by owner and lifetime in the course model。
  - Vendor terminology variance requires responsibility-based comparison, not product-label comparison。
  - The five-question boundary test and BuildPilot allocation case are useful course proposals.
- Cannot prove:
  - Runtime / Harness / Host split is an industry standard, vendor standard or canonical taxonomy.
  - Every framework physically separates these layers.
  - Article 25 contains the complete minimum Harness capability model.
  - Article 25 settles adoption thresholds, Bloat risk, complexity cost or evolution path.
  - BuildPilot is implemented, run, integrated, deployed, benchmarked, production-ready or safe by construction.
  - Any Unity project was scanned, modified, built, profiled, packaged, device-tested or runtime-observed.
  - Suggestion-first, Human Review, sandbox or Harness guarantees safety, compliance, correctness or no regression.
- Mandatory evidence boundary:
  - Required Lab=`NONE`
  - Experiment Count=`0`
  - Runtime Observation=`ABSENT`
  - Evidence mix=`4 CONFIRMED / 6 PARTIAL / 2 PROPOSAL / 0 BLOCKED`
- Closing transition:
  - Article 25 answers where execution and governance split.
  - Article 26 asks what minimum Harness capabilities must exist.
  - Article 27 asks when that design becomes too costly, bloated or unnecessary.
- Final boundary sentence:
  - `本篇只建立责任边界，不交付 Harness API；只有先知道谁负责什么，下一篇的最小能力模型才不会变成一张无限膨胀的功能清单。`
- Section takeaway: **Article 25 的终点是责任分账，不是完整平台设计。**

## Claim-to-section coverage（12 / 12）

| Claim | Status ceiling | Primary sections | Evidence Card | Mandatory wording / boundary |
|---|---|---|---|---|
| `25-C01` | `CONFIRMED` | Opening, 4, 7, 15 | `25-E01` | Runtime 是课程对 execution progression owner 的称呼；不要求 SDK 暴露同名 class |
| `25-C02` | `CONFIRMED` | 4, 8, 15 | `25-E02` | Host 是 application/environment boundary；不自动拥有完整 Harness |
| `25-C03` | `CONFIRMED` | 7, 8, 10, 15 | `25-E03` | tool discovery/call != permission/evidence acceptance |
| `25-C04` | `PARTIAL` | 4, 5, 7, 15 | `25-E04` | Workflow/state mechanisms differ from free-form loop, but product boundaries vary |
| `25-C05` | `PARTIAL` | 6, 15 | `25-E05` | Context assembly vs policy 是课程综合边界，不是 universal product API |
| `25-C06` | `PARTIAL` | 8, 10, 15 | `25-E06` | identity/permission/approval/sandbox separable; complete model deferred |
| `25-C07` | `PARTIAL` | Opening, 2, 9, 12, 15 | `25-E07` | budget/trace/evidence/checkpoint/replay related but not equivalent; no runtime observation |
| `25-C08` | `PARTIAL` | 2, 4, 5, 6, 13, 15 | `25-E08` | four-state-owner model is source-supported course allocation, not standard |
| `25-C09` | `PARTIAL` | 2, 5, 8, 9, 12, 15 | `25-E09` | retry/recovery/takeover split mechanics from policy decision |
| `25-C10` | `CONFIRMED` | Opening, 1, 2, 4, 10, 13, 15 | `25-E10` | vendor terminology variance is counter-evidence against industry-standard wording |
| `25-C11` | `PROPOSAL` | Opening, 1, 3, 4, 10, 11, 13, 15 | `25-E11` | five-question boundary test is course teaching model |
| `25-C12` | `PROPOSAL` | 11, 12, 14, 15 | `25-E12` | BuildPilot allocation case remains design-only, not implemented/run |

Coverage=`12 / 12`；Evidence Cards=`25-E01`—`25-E12`；Status mix=`4 CONFIRMED / 6 PARTIAL / 2 PROPOSAL / 0 BLOCKED`；new core Claim/Card=`NONE`。

## Core Question coverage（8 / 8）

| Core Question | Primary sections | Claims / Evidence | Required boundary |
|---|---|---|---|
| Q1 谁执行模型调用、Tool 调用、调度、等待、状态推进与恢复动作 | 4, 7, 9, 12 | `25-C01/C04/C09` | Runtime executes mechanics; Harness governs shared semantics |
| Q2 Context 选择、组装、裁剪、隔离、持久化分别由谁负责 | 5, 6, 9 | `25-C05/C08` | assembly vs policy split; no full context store design |
| Q3 Identity、Permission、Approval、Sandbox Policy 由谁定义、执行和保存 | 4, 8, 12 | `25-C02/C03/C06/C09` | Host may enforce environment; Harness owns shared governance semantics |
| Q4 Budget、Trace、Evidence、Checkpoint、Replay 属于执行事实还是治理语义 | 5, 9, 12, 15 | `25-C07/C08/C09` | each proves only its layer; no collapse into run metadata |
| Q5 Tool/Skill Registry 与 Capability Discovery 怎样分工 | 10, 11, 15 | `25-C03/C06/C10/C11` | existence, visibility, relevance, authorization, execution and evidence are separate |
| Q6 业务状态与治理/执行状态为什么不能混成一份 State | 5, 11, 13 | `25-C08/C11/C12` | four state owners; course allocation only |
| Q7 Failure classification、retry boundary 和 human takeover 由谁决定 | 8, 9, 12, 14 | `25-C07/C09/C12` | mechanics vs policy vs business owner decision |
| Q8 同一产品实现多层责任时，为什么概念边界仍要保留 | Opening, 1, 2, 4, 13, 15 | `25-C10/C11` | same-product packaging does not erase responsibility boundaries |

## Figures, tables and examples plan

| ID | Form | Teaching responsibility | Evidence source | Mandatory label / restraint |
|---|---|---|---|---|
| `T25-01` | product-name warning table | 解释产品模块名、SDK 名词和责任边界不能直接等同 | `25-E10/E11` | terminology variance / course taxonomy |
| `T25-02` | collapsed-boundary failure table | 展示 Runtime=Harness、Host=Harness 等混淆怎样造成后续失败 | `25-E07/E08/E09/E10` | no universal service split requirement |
| `F25-01` | five-question boundary test | 用 owner/state/invariant/failure/replacement 切责任 | `25-E11` | COURSE PROPOSAL |
| `T25-03` | five-question detail table | 把 Runtime/Harness 的典型回答并排 | `25-E08/E09/E11` | heuristic, not formal method |
| `T25-04` | layer responsibility ledger | 比较 Host、Business、Runtime、Harness、Framework、Workflow Engine | `25-E01/E02/E04/E08/E10/E11` | product packaging may combine layers |
| `T25-05` | four-state-owner table | 分离 business/execution/governance/host-UI state | `25-E08` | PARTIAL / course allocation |
| `T25-06` | context responsibility table | 拆 Context assembly 与 Context policy | `25-E05` + Article 12 | no full context model |
| `F25-02` | Runtime execution flow | 展示 run input 到 stop/wait/recover/continue 的执行闭环 | `25-E01/E03/E04` | execution responsibility, not evidence acceptance |
| `F25-03` | authority chain | 展示 identity/permission/approval/sandbox 到 execution 的链路 | `25-E06/E09` | no complete permission schema |
| `T25-07` | reliability concern ledger | 拆 budget/trace/evidence/checkpoint/replay | `25-E07` + Articles 18—22 | related but not equivalent |
| `T25-08` | registry/discovery split | 说明存在、可见、相关、获权、执行、治理分层 | `25-E03/E06/E10/E11` | no Article 26 Capability model |
| `F25-04` | BuildPilot allocation sequence | 把设计案例按 Host/Business/Runtime/Harness/Owner 分账 | `25-E12` | DESIGN CASE / NOT IMPLEMENTED / NOT RUN |
| `T25-09` | BuildPilot allocation table | 每步 owner/state/Harness/proof ceiling | `25-E12` | read-only / suggestion-first |
| `T25-10` | failure/takeover table | 区分 retry mechanics、policy decision、owner decision | `25-E09/E12` | no recovery engine |
| `T25-11` | replacement matrix | 用替换压力暴露 owner | `25-E08/E10/E11` | no migration design |
| `T25-12` | anti-pattern table | 汇总常见误读与最小修正 | `25-E01-E12` | review heuristic |

Asset policy: Outline/Draft 优先使用 Markdown 表和 ASCII 图；本 Gate 不创建 `assets/`，不生成截图，不伪造 UI、Trace、BuildPilot、Unity、Jenkins 或 runtime artifact。若后续 Publisher 需要发布图，所有图必须标明 `COURSE TAXONOMY / PARTIAL / PROPOSAL / NOT RUN` 的适用位置。

## Learning Check（题目 + answer expectations）

1. 为什么 Article 25 不能按厂商类名解释 Runtime 和 Harness？
   - Expected: 官方/产品术语存在重叠；课程只能按责任、状态、不变量、失败和替换压力建立教学边界。
2. Runtime 的核心责任是什么？
   - Expected: 推进一次 Agent Run：模型调用、step/turn loop、tool dispatch、wait/handoff、state update、stop/error/max-turn boundary。
3. Harness 的核心责任是什么？
   - Expected: 承载跨 run/tool/workflow 的共享治理语义：identity、permission、approval、sandbox、budget、trace、evidence、checkpoint/replay policy、registry/discovery、recovery conventions。
4. Host 和 Harness 为什么不能直接等同？
   - Expected: Host 是应用/环境/workspace/user-interaction boundary；可以执行或暴露部分控制，但不自动拥有全部治理语义。
5. Business Agent / Workflow 为什么不该吞掉 Harness？
   - Expected: 它拥有 domain goal、业务顺序、需求解释和建议；共享权限、证据、预算、Trace、审批和恢复语义应可跨 workflow 保持一致。
6. 五问测试分别问什么？
   - Expected: owner、state、invariant、failure、replacement；答案决定责任层，而不是模块名。
7. Context assembly 和 Context policy 怎样区分？
   - Expected: assembly 是选择/排序/压缩/注入的执行动作；policy 是暴露、保留、脱敏、预算、staleness、receipt 和跨 run 复用规则。
8. 四类 state owner 是什么？
   - Expected: business state、execution state、governance state、host/UI state；它们 owner、lifetime 和替换边界不同。
9. Tool registry 中存在一个工具，为什么还不能让模型默认看见并调用？
   - Expected: existence、visibility、relevance、authorization、approval、budget、execution 和 evidence acceptance 是不同环节。
10. Trace、Evidence、Checkpoint、Replay 为什么不能互相冒充？
    - Expected: Trace 记录发生过什么，Evidence 接受主张可依赖什么，Checkpoint 保存恢复边界，Replay 重建或重演需 manifest/effect policy。
11. Runtime retry 为什么不是 recovery correctness？
    - Expected: retry 是机械再试；是否安全要看 same action intent、effect state、budget、permission、approval 和 human takeover policy。
12. BuildPilot 在本文里到底是什么？
    - Expected: `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`；只用于说明责任分账。
13. BuildPilot 输出 Change Request 后，真实修改由谁完成？
    - Expected: Owner 在 BuildPilot 外实施；BuildPilot 设计中只能建议和后续只读复验。
14. 同一产品实现多层时，为什么仍要保留概念边界？
    - Expected: 因为失败定位、审计、审批、证据、预算、trace join 和替换迁移仍依赖不同 owner 与 state。
15. Article 25 结束后，Article 26 和 Article 27 分别接什么？
    - Expected: 26 接 Harness 最小能力模型；27 接成本、Bloat、可替换性、演化和采纳/不采纳取舍。

## Practical reader actions

| Action | Minimum artifact | Review question | Evidence ceiling |
|---|---|---|---|
| inventory product packaging | module/surface list + responsibility tags | 我看到的是产品名，还是责任 owner？ | terminology-aware review |
| apply five-question test | owner/state/invariant/failure/replacement table | 这项 concern 为什么属于这一层？ | COURSE PROPOSAL |
| split state owners | business/execution/governance/host-state ledger | 哪些 state 需要跨 runtime 或 host 替换仍可审计？ | source-supported PARTIAL |
| audit context handling | assembly vs policy table | 谁在拼 context，谁在决定可见/保留/脱敏/复用？ | Article 12 continuity |
| separate execution and authority | action-authority chain | Tool 能执行和当前请求获权是否分开记录？ | Article 19 continuity |
| separate trace and evidence | trace/evidence/checkpoint/replay ledger | 日志是否被误当 claim acceptance？ | Article 18/21 continuity |
| evaluate registry/discovery | existence/visibility/relevance/authorization/execution chain | registry 中有工具是否被误当可见可用？ | no Article 26 full model |
| classify failures | mechanics/policy/business-owner table | retry/resume/ask/stop 由谁决定？ | no recovery engine |
| map BuildPilot flow | layer allocation sequence | 每一步 owner、state、proof ceiling 是否明确？ | DESIGN CASE / NOT RUN |
| test replacement pressure | replacement matrix | 换 runtime/framework/host 后哪些事实不能丢？ | architecture review |

## Job Competency mapping

| Competency | Reader-visible output | Observable standard | Explicit ceiling |
|---|---|---|---|
| Agent architecture judgment | five-question boundary test + layer ledger | 能用 responsibility 而不是 product label 分层 | course taxonomy, not industry standard |
| Runtime design literacy | Runtime execution flow | 能说清 loop、tool dispatch、wait/resume、state progression 与 stop boundary | no SDK tutorial |
| Harness governance literacy | authority chain + reliability ledger | 能拆 identity、permission、approval、sandbox、budget、trace、evidence、replay | no full Harness API |
| State ownership discipline | four-state-owner model | 能分离 business/execution/governance/host state | source-supported PARTIAL |
| Context engineering continuity | assembly/policy split | 能把 Article 12 的 context receipt 思想接到 Harness 边界 | no context store design |
| Capability governance | registry/discovery split | 能区分工具存在、可见、相关、获权、执行和 evidence acceptance | Article 26 deferred |
| Failure/recovery reasoning | failure/takeover table | 能把 retry mechanics、policy decision 和 owner takeover 分账 | no recovery implementation |
| BuildPilot design-case thinking | read-only requirement-change allocation | 能在 suggestion-first 设计中保留 owner implementation 与 proof ceiling | BuildPilot not implemented/run |
| Replaceability reasoning | replacement matrix | 能判断替换模型/runtime/host/framework 时哪些 records 必须保留 | no adoption-cost framework |
| Evidence communication | claim-to-section matrix + status labels | 能保持 `CONFIRMED/PARTIAL/PROPOSAL` 和 `NOT RUN` 标签可见 | no status upgrade |

## Frontmatter and publication plan

```yaml
---
title: "Agent Runtime vs Harness：执行内核与工程控制面"
slug: "agent-engineering-25-agent-runtime-vs-harness"
date: "2026-08-29T00:00:00+08:00"
description: "用 owner、state、invariant、failure 与 replacement 五问，把 Agent Runtime 的执行推进职责和 Harness 的共享治理控制面切清楚。"
draft: false
tags:
  - "Agent Engineering"
  - "AI Engineering"
  - "Harness Engineering"
  - "Agent Runtime"
series: "Agent Engineering"
primary_series: "agent-engineering"
series_role: "article"
series_order: 260
weight: 3260
---
```

- Published Path Candidate: `content/ai-empowerment/agent-engineering-25-agent-runtime-vs-harness.md`
- Previous link: Published Article 24 exact path `content/ai-empowerment/agent-engineering-24-why-harness-cross-cutting-capabilities.md`
- Course index link: existing Agent Engineering series index
- Next link: Article 26 planned title only if Publisher later confirms path exists；Draft 阶段可用 prose bridge，不创建 broken `relref`。
- Metadata rationale: `series_order=(25+1)*10=260`，`weight=3000+260=3260`，follows Article 24 `series_order=250 / weight=3250`。
- YAML quote rule: current title/description contain no ASCII quote characters；double-quoted scalars are valid。Any later edit adding quotation marks must switch outer YAML quoting safely per repository rule。

## Exact no-new-fact boundary for Draft

Draft may:

- paraphrase and reorganize only `25-C01`—`25-C12` and `25-E01`—`25-E12`;
- state Runtime / Harness / Host split as course-defined responsibility taxonomy, not as external standard;
- use Published Article 24 only as bridge: Article 24 explains why shared boundary appears; Article 25 splits execution and governance responsibility;
- use Published Articles 06、07、10、11、12、18、19、20、21、22 only for continuity boundaries already preserved in Evidence Cards;
- compare Host、Business Agent / Workflow、Runtime、Harness、Agent Framework and Workflow Engine without relying on vendor class names;
- include owner/state/invariant/failure/replacement five-question test as `COURSE PROPOSAL`;
- preserve `4 CONFIRMED / 6 PARTIAL / 2 PROPOSAL / 0 BLOCKED`, Claim Coverage `12 / 12`, Evidence Cards `12 / 12`;
- describe Context assembly vs Context policy as `PARTIAL` course synthesis;
- describe four state owners as source-supported course allocation, not industry taxonomy;
- use BuildPilot requirement-change flow only as `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`;
- state Owner implements real changes outside BuildPilot; Runtime may re-verify only as design case; Harness records governance results only as design case;
- mention Article 26/27 only as forward boundaries, not as complete models.

Draft must not:

- introduce a core Claim/Evidence Card beyond `25-C01`—`25-C12`;
- claim Runtime / Harness / Host / Framework / Workflow Engine taxonomy is industry standard, vendor standard or universal architecture;
- upgrade any `PARTIAL` or `PROPOSAL` claim to `CONFIRMED`;
- quote or depend on vendor class names as the organizing structure;
- define Article 26 minimum Capability / Policy / Session / Trace / Recovery model beyond short bridge language;
- define Article 27 adoption threshold, bloat framework, migration strategy or cost model;
- describe BuildPilot as implemented, run, integrated, deployed, production-ready, benchmarked or capable of direct automated fixes;
- claim any Unity Editor, Jenkins, CI, Addressables Analyze, package build, runtime profile, device test or project scan was executed;
- fabricate metrics, screenshots, logs, traces, approval records, BuildReports, registry output, PRs, owner reviews, runtime observations or production results;
- turn suggestion-first, Human Review, sandbox, approval, eval or Harness into a guarantee of safety, correctness, compliance, no regression or future prevention;
- silently install tools, expand permissions, mutate capability registry or propose production writes as completed behavior;
- create Draft/Review/content/assets/Lab/runtime/global/canonical/Git/future-Article artifacts during OUTLINE gate.

Trigger: if Draft requires any fact outside this boundary, return `RETURN_TO_RESEARCH` with the exact missing Claim/Evidence need; do not fill it with memory, inference or “common practice.”

## Explicit non-scope

- 不写成 OpenAI Agents SDK、Microsoft Agent Framework、LangChain / LangGraph、MCP、Temporal、AWS、OpenTelemetry、NIST、Unity 或任何厂商产品教程。
- 不实现或设计完整 Harness API、Capability Registry、Policy Engine、Session Store、Trace Store、Evidence Store、Replay Engine、Eval Runner、Knowledge Store、Tool Registry、Workflow Engine 或 BuildPilot Runtime。
- 不运行 Lab、外部网络、Provider/model call、Unity Editor、Jenkins、CI、package build、device test、Addressables Analyze、真实项目扫描或任何 runtime experiment。
- 不创建或修改 Article 26—28 workspace/content/assets，不启动 Article 26，不触碰 Article 28。
- 不修改 `research.md`、`evidence.md`、README、article-card、review、subagent-trace、series plan、status tracker、course-run-state、published content 或任何全局/canonical 文件。
- 不创建 branch/worktree/commit/push。
- Frozen reality: Required Lab=`NONE`；Experiment Count=`0`；Runtime Observation=`ABSENT`；BuildPilot=`COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`。

## Closing bridge

- Closing sentence: `Runtime 的成熟在于把一次执行推进到可解释的边界；Harness 的成熟在于让多次执行共享同一套身份、权限、证据、预算、Trace、审批、恢复和能力治理语义。`
- Bridge to Article 26:
  - Article 25 只建立责任分账。
  - Article 26 才回答一个最小 Harness 至少需要哪些 Capability、Policy、Session、Trace 与 Recovery 能力。
  - Article 27 再回答什么时候这种抽象会过重、过早、过度平台化，什么时候应该保留在局部链路里。
- Mandatory final boundary sentence: **只有先把执行内核和工程控制面切清楚，后续的 Harness 最小模型才不会滑向“什么都管”的功能清单。**

## OUTLINE Gate checklist

- [x] Article Type fixed as `PRINCIPLE`；L-weight structure follows problem space -> responsibility-based abstract model -> concrete mechanisms -> BuildPilot allocation case -> engineering judgment，not API-first。
- [x] Teaching Spine begins from Article 24 shared boundary and answers Runtime / Harness / Host / Business responsibility split.
- [x] Five questions are explicit: owner、state、invariant、failure、replacement.
- [x] Host、Business Agent / Workflow、Runtime、Harness、Agent Framework / Workflow Engine are compared without relying on vendor class names.
- [x] Runtime coverage includes model call、tool dispatch、scheduling/waiting、state progression、stop/error/max-turn、handoff/continuation and execution-side recovery mechanics.
- [x] Harness coverage includes identity、permission、approval、sandbox、budget、trace、evidence、checkpoint/replay policy、registry/discovery governance and recovery conventions.
- [x] Context assembly vs Context policy is explicitly split.
- [x] Business state、execution state、governance state、host/UI state are split by owner and lifetime.
- [x] Failure/retry/recovery/human takeover separates mechanics, policy decision and business owner decision.
- [x] Same-product multi-layer packaging and replacement pressure are explicit, while taxonomy remains course-defined and non-standard.
- [x] BuildPilot requirement-change scenario is bounded as `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`; Owner implements real changes outside BuildPilot.
- [x] Required Lab=`NONE`；Experiment Count=`0`；Runtime Observation=`ABSENT`；no experiment output, metric, screenshot, runtime observation or project experience invented.
- [x] Article 26 minimum model and Article 27 trade-off/adoption/bloat framework are future boundaries, not defined here.
- [x] Claim coverage=`12 / 12`；Evidence Cards=`25-E01`—`25-E12` only；new core Claim/Card=`NONE`.
- [x] Evidence posture preserved exactly: `4 CONFIRMED / 6 PARTIAL / 2 PROPOSAL / 0 BLOCKED`.
- [x] Figures/Tables、Learning Checks、Practical Actions、Job Competency、Frontmatter plan and Draft no-new-fact boundary are complete.
- [x] No Draft/Review/content/assets/Lab/runtime/global/canonical/Git/future-Article artifact belongs to this Author result.
- [x] OUTLINE Gate recommendation: `PASS`；next allowed gate candidate: `AUTHOR_DRAFT`；Master validation remains outside this artifact。
