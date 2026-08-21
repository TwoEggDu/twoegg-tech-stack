# Article 10 Evidence

## Status

- Gate：`RESEARCH / EVIDENCE_GATE_CANDIDATE`
- Owner：`RESEARCHER`
- Article：`10`
- Retrieved Date：`2026-08-21`
- Evidence Status：`PASS_RECOMMENDED`
- Required Lab：`NONE`
- Claim Summary：`6 CONFIRMED / 1 PARTIAL / 0 BLOCKED / 3 PROPOSAL`

## Evidence policy

- `CONFIRMED`只表示证据在卡片注明的规范、产品版本页或 frozen fixture scope 内直接支持该窄化 Claim。
- `PARTIAL`表示来源支持比较轴，但产品词义重叠，正文必须把结论写成课程 taxonomy。
- `PROPOSAL`表示工程设计或课程接口，不伪装成来源已经观察到的行为。
- current hosted product docs均按`retrieved 2026-08-21`使用；没有执行 package compatibility run，不补写未核验版本保证。
- Article 10没有Required Lab。本文复用Lab 03 `AL-04` raw artifacts；任何State Machine映射都标为`PROPOSAL / NOT EXECUTED`。

## Claim Register

| Claim ID | Exact wording | Status | Scope | Evidence Cards | Counter-evidence / limitation | Course usage |
|---|---|---|---|---|---|---|
| `10-C01` | Plan、Workflow Definition、Runtime State与Trace拥有不同producer、consumer与证明力，不能互相替代 | `CONFIRMED` | `PRODUCT + REPOSITORY-SCOPED` | `10-E01` | 产品可把多个对象暴露在同一容器；存储共置不等于语义相同 | 第一层对象边界 |
| `10-C02` | Agent Loop、State Machine与Workflow共享“当前信息到下一推进”的最小骨架，但decision owner与scope不同；此分类是课程taxonomy | `PARTIAL` | `COURSE-TAXONOMY + PRODUCT-SCOPED` | `10-E02` | AWS直接把state machine称为workflow；不能写成行业统一分类 | 核心对照 |
| `10-C03` | State configuration、Transition、Guard与Terminal可由SCXML / AWS窄化定义并映射到课程runtime | `CONFIRMED` | `SPEC + PRODUCT-SCOPED` | `10-E03` | SCXML的层级、并行语义不代表所有业务workflow | 状态机基础 |
| `10-C04` | Stage、Step与Invariant采用本文工作定义：Stage是粗粒度治理分组，Step沿用Article 08，Invariant是所有reachable State保持的predicate | `PROPOSAL` | `SOURCE-INFORMED COURSE DEFINITION` | `10-E04` | AWS的step可指state，LangGraph还有super-step；不得跨产品等同 | 术语去重 |
| `10-C05` | 改变authoritative State的legal transition由程序验证；Agent只提交candidate suggestion | `PROPOSAL` | `SOURCE-INFORMED CONTROL DESIGN` | `10-E05` | 来源支持transition condition和code / LLM分工，不直接规定本课程commit protocol | 中心工程判断 |
| `10-C06` | Workflow调Agent、Agent调受控Workflow Tool与code orchestration在引用的current official products中均可构造且可组合 | `CONFIRMED` | `CITED-PRODUCTS-SCOPED` | `10-E06` | 不证明三种形态互斥、完备或可靠性排序 | 三种控制形态 |
| `10-C07` | Agent Decision Point只在多个合法候选仍需语境判断时使用，输入/输出受schema与guard约束 | `PROPOSAL` | `COURSE INTERFACE DESIGN` | `10-E07` | official docs支持structured tool与混合编排，但没有定义本文Agent Decision Point | decision point contract |
| `10-C08` | AL-04直接证明fixed fixture中的repeat、no-progress、missing requirements与fake-success rejection；illegal-transition只属分析overlay | `CONFIRMED` | `FIXTURE-SCOPED + PROPOSAL OVERLAY` | `10-E08` | fixture没有Workflow runtime，也不代表真实模型或production reliability | bounded bad trace |
| `10-C09` | Current State不自动具备Checkpoint的持久化identity、continuation与resume boundary | `CONFIRMED` | `LANGGRAPH-CURRENT-DOCS-SCOPED` | `10-E09` | LangGraph字段不是行业统一checkpoint schema | Article 11 bridge |
| `10-C10` | 引用产品以不同方式组合Workflow、Agent与Runtime职责，足以反驳“唯一正确架构”的写法 | `CONFIRMED` | `COUNTER-EVIDENCE PRODUCT-SCOPED` | `10-E10` | 产品可构造性不等于架构质量或适合所有场景 | engineering boundary |

## Evidence Cards

### 10-E01｜Plan、Workflow Definition、Runtime State与Trace是不同对象

- **Supports Claim**：`10-C01`
- **Status**：`CONFIRMED / PRODUCT + REPOSITORY-SCOPED`
- **Sources**：
  - [AWS Step Functions: state machine concepts](https://docs.aws.amazon.com/step-functions/latest/dg/concepts-statemachines.html)，current service docs，retrieved `2026-08-21`。
  - [AWS ASL state machine structure](https://docs.aws.amazon.com/step-functions/latest/dg/statemachine-structure.html)，ASL default version `1.0` / current docs，retrieved `2026-08-21`。
  - [AWS GetExecutionHistory](https://docs.aws.amazon.com/step-functions/latest/apireference/API_GetExecutionHistory.html)，current API docs，retrieved `2026-08-21`。
  - [Article 08 Published Content](../../../../content/ai-empowerment/agent-engineering-08-agent-loop.md)与[Article 09 Published Content](../../../../content/ai-empowerment/agent-engineering-09-planning.md)，repository dependencies，retrieved `2026-08-21`。
- **Observation**：AWS把ASL definition与每次execution instance分开，Standard Workflow还能返回按时间排列的execution events；Article 08把authoritative State与append-only Trace分开；Article 09把Plan限定为Goal和Current Evidence下的剩余行动候选。
- **Counter-evidence**：产品UI、SDK object或session container可以同时携带definition、state与history；这会让存储位置重叠，但不消除producer、consumer与证明力差异。
- **Interpretation**：正文可以按对象合同区分：Plan描述“考虑什么”，Definition描述“允许怎样走”，Runtime State描述“当前已接受什么”，Trace描述“记录中发生过什么”。
- **Proves**：在引用产品与课程fixture中，definition、execution、state、history / trace与plan可分别识别，不能因名称相近就互相充当完成证据。
- **Does Not Prove**：不证明所有引擎必须拆成四个文件、Trace绝对完整、State一定持久化，或有Definition就已执行成功。
- **Limitations**：`GetExecutionHistory`不支持Express Workflows；课程Trace合同也不等同AWS event schema。
- **Course Usage**：文章第一张对象边界表；禁止把Plan当执行、把Trace当authoritative current State、把State当Definition。

### 10-E02｜Agent Loop、State Machine与Workflow的课程比较轴

- **Supports Claim**：`10-C02`
- **Status**：`PARTIAL / COURSE-TAXONOMY + PRODUCT-SCOPED`
- **Sources**：
  - [W3C SCXML 1.0 Recommendation](https://www.w3.org/TR/scxml/)，W3C Recommendation `2015-09-01`，retrieved `2026-08-21`。
  - [AWS Step Functions: state machine concepts](https://docs.aws.amazon.com/step-functions/latest/dg/concepts-statemachines.html)，current service docs，retrieved `2026-08-21`。
  - [LangGraph: Workflows and agents](https://docs.langchain.com/oss/python/langgraph/workflows-agents)，current hosted docs / package version未绑定，retrieved `2026-08-21`。
  - [Article 08 Published Content](../../../../content/ai-empowerment/agent-engineering-08-agent-loop.md)，repository dependency，retrieved `2026-08-21`。
- **Observation**：SCXML定义state configuration与enabled transitions；LangGraph把workflow描述为predetermined code path，把agent描述为动态决定process / tool use；Article 08的Agent Loop则由decision candidate、Host gate、Tool Outcome、Observation和reducer组成。
- **Counter-evidence**：AWS将Step Functions state machine直接称为workflow，并称每个state为step；LangGraph也能在同一graph runtime里承载workflow与agent行为。
- **Interpretation**：共同点只收窄为“根据当前信息反复选择下一推进并走向terminal”；差异按decision owner、合法候选集合的位置和运行对象scope讨论。
- **Proves**：来源足以支持比较这些控制责任，也直接证明产品术语并不统一。
- **Does Not Prove**：不证明Agent Loop、State Machine、Workflow是三个行业公认互斥类型，或其中一种天然更可靠。
- **Limitations**：LangGraph页面未绑定本地package build；课程Agent Loop是Article 08的工作定义。
- **Course Usage**：正文必须显式写“本课程比较轴”，`PARTIAL` Claim不得升级为行业taxonomy。

### 10-E03｜State、Transition、Guard与Terminal的规范锚点

- **Supports Claim**：`10-C03`
- **Status**：`CONFIRMED / SPEC + PRODUCT-SCOPED`
- **Sources**：
  - [W3C SCXML 1.0 Recommendation](https://www.w3.org/TR/scxml/)，W3C Recommendation `2015-09-01`，retrieved `2026-08-21`。
  - [AWS ASL state machine structure](https://docs.aws.amazon.com/step-functions/latest/dg/statemachine-structure.html)，ASL default version `1.0` / current docs，retrieved `2026-08-21`。
- **Observation**：SCXML把当前configuration定义为active states集合；transition在source relation、event匹配且`cond`不存在或为true时enabled；进入顶层final后解释器终止。AWS ASL用`StartAt`、`States`、`Next`、`End`以及Succeed / Fail组织执行。
- **Counter-evidence**：并非所有workflow都用SCXML，且AWS既有`End: true`也有Succeed / Fail terminal forms；terminal形式与成功含义并不唯一。
- **Interpretation**：课程把State映射为当前已提交控制位置与相关权威数据；Transition是合法状态变化；Guard是edge的布尔前置条件；Terminal只表示该execution不再推进，outcome另算。
- **Proves**：这些窄化语义有规范 / 产品锚点，guard失败时对应transition本次不可enabled，terminal不必等同success。
- **Does Not Prove**：不证明所有业务workflow都需实现SCXML层级 / 并行语义，或tool call本身就是transition。
- **Limitations**：从规范映射到课程runtime属于概念映射，不是对某个Article 10 runtime的执行观察。
- **Course Usage**：为状态图、合法edge与terminal outcome分离提供术语基础。

### 10-E04｜Stage、Step与Invariant的工作定义

- **Supports Claim**：`10-C04`
- **Status**：`PROPOSAL / SOURCE-INFORMED COURSE DEFINITION`
- **Sources**：
  - [Using TLC to Check Inductive Invariance](https://lamport.azurewebsites.net/tla/inductive-invariant.pdf)，Leslie Lamport，`2018-08-23`，retrieved `2026-08-21`。
  - [AWS Step Functions: state machine concepts](https://docs.aws.amazon.com/step-functions/latest/dg/concepts-statemachines.html)，current service docs，retrieved `2026-08-21`。
  - [LangGraph: Workflows and agents](https://docs.langchain.com/oss/python/langgraph/workflows-agents)，current hosted docs，retrieved `2026-08-21`。
  - [Article 08 Published Content](../../../../content/ai-empowerment/agent-engineering-08-agent-loop.md)，repository dependency，retrieved `2026-08-21`。
- **Observation**：Lamport把invariant建立在所有reachable states上；Article 08按一次committed loop iteration计Step；AWS却把state称为step，其他graph runtime还可能以super-step计执行tick。
- **Counter-evidence**：没有跨生态统一的Stage或Step粒度；Invariant也不等于某个产品的validation hook名称。
- **Interpretation**：本文Proposal：Stage是治理 / 可视化 / 责任分组，可包含多个State或Step；Step沿用Article 08本地可审计单位；Invariant是在所有reachable State都必须成立的predicate，并在transition commit边界检查适用项。
- **Proves**：source只直接支持reachable-state invariant及产品术语漂移。
- **Does Not Prove**：不证明Stage是标准状态机对象、Stage与State一一对应，或所有runtime已自动执行pre/post invariant check。
- **Limitations**：commit前后检查是课程设计，不是Lamport材料规定的runtime API。
- **Course Usage**：所有三词首次出现须附工作定义；图中要标明Stage为分组而非authoritative State。

### 10-E05｜Legal transition由程序提交，Agent只给候选

- **Supports Claim**：`10-C05`
- **Status**：`PROPOSAL / SOURCE-INFORMED CONTROL DESIGN`
- **Sources**：
  - [W3C SCXML 1.0 Recommendation](https://www.w3.org/TR/scxml/)，W3C Recommendation `2015-09-01`，retrieved `2026-08-21`。
  - [AWS ASL state machine structure](https://docs.aws.amazon.com/step-functions/latest/dg/statemachine-structure.html)，current docs，retrieved `2026-08-21`。
  - [OpenAI Agents SDK: Agent orchestration](https://openai.github.io/openai-agents-python/multi_agent/)，current hosted docs / package version未绑定，retrieved `2026-08-21`。
  - [Article 09 Published Content](../../../../content/ai-empowerment/agent-engineering-09-planning.md)，repository dependency，retrieved `2026-08-21`。
- **Observation**：SCXML把enabled transition交给可计算的source / event / cond条件；AWS definition列出allowed states与`Next`；OpenAI official docs区分LLM-driven与code-driven orchestration并允许混合；Article 09把Plan明确限制为候选而非执行。
- **Counter-evidence**：某些runtime可能让model直接选择tool或route，且来源没有规定本文的revision、evidence和invariant字段。
- **Interpretation**：课程提出commit protocol：程序校验current source / revision、definition edge、guard / authorization / Evidence、post-state invariant和terminal completion contract；Agent输出只能是schema-bounded suggestion，runtime在提交前重验。
- **Proves**：来源支持“transition有可验证条件”以及“LLM与code可以分担控制”。
- **Does Not Prove**：不证明该五项protocol已在Article 10运行、不证明它是唯一安全设计，也不证明模型不能执行任何routing。
- **Limitations**：这是核心工程Proposal；正文必须用“应 / 本文采用”，不能写成observed product guarantee。
- **Course Usage**：中心判断；图中必须分成`Agent suggestion -> deterministic validation -> State commit`三段。

### 10-E06｜三种control-owner形态都可构造

- **Supports Claim**：`10-C06`
- **Status**：`CONFIRMED / CITED-PRODUCTS-SCOPED`
- **Sources**：
  - [Microsoft Agent Framework: Functional Workflow API](https://learn.microsoft.com/en-us/agent-framework/concepts/workflows/functional)，current page / Python Functional API标记`experimental`，retrieved `2026-08-21`。
  - [Microsoft Agent Framework: Using Workflows as Agents](https://learn.microsoft.com/en-us/agent-framework/workflows/as-agents)，page updated `2026-07-29`，retrieved `2026-08-21`。
  - [OpenAI Agents SDK: Agent orchestration](https://openai.github.io/openai-agents-python/multi_agent/)，current hosted docs / package version未绑定，retrieved `2026-08-21`。
  - [OpenAI Agents SDK: Tools](https://openai.github.io/openai-agents-python/tools/)，current hosted docs / package version未绑定，retrieved `2026-08-21`。
- **Observation**：Microsoft Functional Workflow示例在workflow中调用agent并可用`@step`持久化 / 缓存边界；Microsoft另一个current页面允许workflow包装成Agent-compatible object并作为另一Agent的tool。OpenAI允许Python function包装为FunctionTool，并明确code orchestration可sequence、branch、loop且能与LLM orchestration混用。
- **Counter-evidence**：OpenAI Tools文档也说明直接调用`__wrapped__`会绕过schema、guardrails、timeouts、failure handling与tracing等runtime pipeline；“函数可调用”不等于“受控Tool调用”。
- **Interpretation**：Workflow -> Agent、Agent -> controlled Workflow Tool、Code orchestration均有current official product可构造性证据；比较应按control owner和validation boundary，而非按营销类名。
- **Proves**：三种形态至少在所引产品能力中存在并可组合。
- **Does Not Prove**：不证明任意workflow自动获得tool guard、三者覆盖所有架构、某一种总是更可靠，或需要展开Multi-Agent。
- **Limitations**：Microsoft Functional API为experimental；各hosted docs未绑定本文未运行的package build。
- **Course Usage**：正文三种control shape对照表，并明确Agent 10只讨论控制责任、不进入Multi-Agent topology。

### 10-E07｜Agent Decision Point的窄接口

- **Supports Claim**：`10-C07`
- **Status**：`PROPOSAL / COURSE INTERFACE DESIGN`
- **Sources**：
  - [LangGraph: Workflows and agents](https://docs.langchain.com/oss/python/langgraph/workflows-agents)，current hosted docs，retrieved `2026-08-21`。
  - [OpenAI Agents SDK: Agent orchestration](https://openai.github.io/openai-agents-python/multi_agent/)，current hosted docs，retrieved `2026-08-21`。
  - [OpenAI Agents SDK: Tools](https://openai.github.io/openai-agents-python/tools/)，current hosted docs，retrieved `2026-08-21`。
- **Observation**：LangGraph对照predetermined workflow path与动态Agent decision；OpenAI同时支持LLM和code orchestration，并用FunctionTool schema / runtime pipeline约束函数调用。
- **Counter-evidence**：产品可以让Agent选择工具、handoff或route，并没有使用“Agent Decision Point”作为统一标准；不是所有选择都需要先列出完整枚举候选。
- **Interpretation**：本文Proposal只在确定性过滤后仍有多个legal candidate且选择依赖非结构化 / 多源 / 语境Evidence时调用Agent；输入为allowed State view、Evidence refs与可选Plan，输出为schema-bounded transition suggestion，最终仍由runtime guard验证。
- **Proves**：来源支持动态decision与code / schema边界可以组合。
- **Does Not Prove**：不证明此接口是官方标准、模型输出天然合法，或guard可由prompt替代。
- **Limitations**：这是课程设计；正文需要提供输入 / 输出 / rejection示例，不能声称执行过。
- **Course Usage**：用于解释“确定性骨架 + 局部Agent判断”，并排除纯布尔、枚举、权限和完整性判断交给模型重想。

### 10-E08｜AL-04 bounded bad trace与未执行overlay

- **Supports Claim**：`10-C08`
- **Status**：`CONFIRMED / FIXTURE-SCOPED + PROPOSAL OVERLAY`
- **Sources**：
  - [AL-04 trace.jsonl](../../labs/lab-03-minimal-agent-loop/observations/run-a/trace.jsonl)、[tool-outcomes.jsonl](../../labs/lab-03-minimal-agent-loop/observations/run-a/tool-outcomes.jsonl)、[observations.jsonl](../../labs/lab-03-minimal-agent-loop/observations/run-a/observations.jsonl)、[states.jsonl](../../labs/lab-03-minimal-agent-loop/observations/run-a/states.jsonl)、[case-results.jsonl](../../labs/lab-03-minimal-agent-loop/observations/run-a/case-results.jsonl)，Windows / .NET deterministic fixture `run-a`，retrieved `2026-08-21`。
  - [Article 08 Published Content](../../../../content/ai-empowerment/agent-engineering-08-agent-loop.md)，published `2026-08-20`，retrieved `2026-08-21`。
- **Observation**：AL-04初始`REQ_LOG / REQ_SOURCE` unresolved且accepted Goal Evidence为空；Step 1读取`Unrelated.cs`，Tool成功但`goal_relevant=false / NO_PROGRESS`；Step 2使用同一action fingerprint `C25D...`再次读取同一文件，`repeat_detected=true`且goal-state digest保持`BF511...`；随后scripted decision请求`SUCCEEDED`并引用`EV-FAKE`，最终结果为`STOP_CONTRACT_FAILED / FAILED`，两项requirement仍unresolved。
- **Counter-evidence**：fixture使用scripted decisions，不是对真实模型的统计；raw artifacts没有Workflow Definition、State Machine Runtime或transition event。
- **Interpretation**：raw facts支持“重复、无Goal progress、required Evidence缺失、fake success被拒绝”。为Article 10建立的状态图只能是分析overlay。
- **Proves**：在这个固定case中，同一动作确实重复且未改变Goal State；completion contract确实拒绝了伪成功。
- **Does Not Prove**：不证明发生过或提交过illegal workflow transition、不证明固定Stage被runtime漏过、不证明State Machine能自动修复planning quality，也不证明production behavior。
- **Limitations**：action fingerprint与digest在正文只需使用“相同 / 未变”，避免把截断显示当完整hash。
- **Course Usage**：唯一bounded bad trace；正文必须把下面overlay标成`PROPOSAL / NOT EXECUTED`。

#### Bounded Bad Trace Contract

**Observed trace（只来自raw artifacts）：**

| Order | Observed action / state | Evidence classification |
|---|---|---|
| 0 | `REQ_LOG / REQ_SOURCE` unresolved；accepted Goal Evidence为空 | `OBSERVED` |
| 1 | 读取`Unrelated.cs`，Tool success但`goal_relevant=false / NO_PROGRESS` | `OBSERVED` |
| 2 | 同一action fingerprint再次读取同一文件；`repeat_detected=true`；goal-state digest不变 | `OBSERVED` |
| 3 | 请求`SUCCEEDED`并引用`EV-FAKE` | `OBSERVED` |
| 4 | completion validation返回`STOP_CONTRACT_FAILED / FAILED`；requirements仍unresolved | `OBSERVED` |

**Analytical overlay（没有运行）：**

| Proposed edge | Proposed deterministic guard | AL-04 mapping |
|---|---|---|
| `INTAKE -> LOG_READY` | 已接受与Goal相关的log Evidence及locator | 两次unrelated read均不能越过 |
| `LOG_READY -> SOURCE_READY` | 已接受与log关联的source Evidence | 未到达 |
| `SOURCE_READY -> VERIFIED` | 两项required Evidence已接受且无unresolved failure | 未到达 |
| `VERIFIED -> SUCCEEDED` | output / Evidence / success completion contract全部满足 | `EV-FAKE`请求应被拒绝 |
| `any -> FAILED` | deterministic terminal rule触发并保存failure reason | raw fixture实际以`STOP_CONTRACT_FAILED / FAILED`停止，但未执行此overlay edge |

以上整张transition table均为`PROPOSAL / NOT EXECUTED`；最后一行只做结果对齐，不能改写为“overlay runtime提交了FAILED transition”。

### 10-E09｜Current State与Checkpoint的Article 11 stop line

- **Supports Claim**：`10-C09`
- **Status**：`CONFIRMED / LANGGRAPH-CURRENT-DOCS-SCOPED`
- **Sources**：
  - [LangGraph: Checkpointers](https://docs.langchain.com/oss/python/langgraph/checkpointers)，current hosted docs，retrieved `2026-08-21`。
- **Observation**：LangGraph的`StateSnapshot`除values外还列出`next`、thread / checkpoint identity、metadata、parent与tasks；文档把恢复绑定到checkpoint boundary，并说明replay时checkpoint之后的LLM / API等节点会重新执行。
- **Counter-evidence**：不同runtime可以使用event sourcing、数据库事务、workflow history或其他恢复模型，不必复制LangGraph字段。
- **Interpretation**：Current State只回答当前接受了什么；Checkpoint还要绑定durable identity、continuation与resume / replay boundary。Article 10只建立边界，不设计恢复协议。
- **Proves**：至少在current LangGraph product中，checkpoint不是单独一份values；当前State对象本身不能推出跨中断恢复已解决。
- **Does Not Prove**：不定义行业统一checkpoint schema，也不证明任何Article 10对象已持久化、可恢复或side-effect safe。
- **Limitations**：产品特定且未运行Lab 04；retry、cancellation、resume、replay、去重 / compensation均无Article 10行为证据。
- **Course Usage**：正文只保留桥句：`State描述当前位置；Checkpoint把可恢复位置、持久化边界与continuation metadata绑定起来。` 后续全部交Article 11 / Lab 04。

### 10-E10｜现实产品组合反驳唯一架构

- **Supports Claim**：`10-C10`
- **Status**：`CONFIRMED / COUNTER-EVIDENCE PRODUCT-SCOPED`
- **Sources**：
  - [AWS Step Functions: state machine concepts](https://docs.aws.amazon.com/step-functions/latest/dg/concepts-statemachines.html)，current docs，retrieved `2026-08-21`。
  - [LangGraph: Workflows and agents](https://docs.langchain.com/oss/python/langgraph/workflows-agents)，current hosted docs，retrieved `2026-08-21`。
  - [Microsoft Agent Framework: Functional Workflow API](https://learn.microsoft.com/en-us/agent-framework/concepts/workflows/functional)与[Using Workflows as Agents](https://learn.microsoft.com/en-us/agent-framework/workflows/as-agents)，current docs；后一页updated `2026-07-29`，retrieved `2026-08-21`。
  - [OpenAI Agents SDK: Agent orchestration](https://openai.github.io/openai-agents-python/multi_agent/)，current hosted docs，retrieved `2026-08-21`。
- **Observation**：AWS将state machine / workflow命名合并；LangGraph在同一graph模型中描述workflow与agent；Microsoft既支持workflow调用Agent，也支持workflow-as-agent / agent tool；OpenAI允许LLM-driven与code-driven orchestration混用。
- **Counter-evidence**：这些来源彼此的abstraction、maturity和execution semantics不同，不能拼成一套统一API。
- **Interpretation**：可守住的是职责问题：谁拥有legal edge、谁提交State、谁验证guard / invariant、谁产生候选；类名、嵌套方向与部署拓扑没有唯一答案。
- **Proves**：至少四组official product facts反驳“State Machine、Workflow与Agent只能按一种方向组合”。
- **Does Not Prove**：不证明任意组合都安全、等价、可恢复或生产就绪，也不提供架构优先级排行。
- **Limitations**：只用于counter-evidence与边界，不进入产品选型或Multi-Agent教程。
- **Course Usage**：作为正文“不要背产品名，检查控制责任”的反例组。

## Cross-claim counter-evidence matrix

| Overstrong statement to reject | Counter-evidence | Allowed narrowed wording |
|---|---|---|
| Workflow与State Machine永远是两层不同对象 | AWS直接称state machine为workflow | 本课程按definition / transition semantics / application scope比较职责 |
| 一个State就等于一个Step | AWS如此命名，但Article 08按committed loop iteration计Step，LangGraph还有super-step | 每次明确计数单位，不跨产品强行等同 |
| Workflow只能调用Agent，Agent不能反向使用Workflow | Microsoft同时展示workflow内Agent与workflow-as-agent / agent tool | 两种方向都可构造，关键是control owner与guard boundary |
| LLM orchestration与code orchestration只能二选一 | OpenAI official docs明确可mix and match | 可以混合，但authoritative commit责任必须清楚 |
| Tool成功就等于Workflow推进 | AL-04两次Tool success均`NO_PROGRESS` | Tool Outcome先转为Observation，再由guard决定是否允许State transition |
| 有当前State就等于有Checkpoint / Recovery | LangGraph checkpoint还携带identity、next、metadata、tasks与resume boundary | State只描述当前位置；Recovery contract留Article 11 |

## Evidence Gate Checklist

- [x] 10个核心Claim均有Evidence Card，Source、Retrieved Date、Version Scope、Observation、Counter-evidence、Interpretation、Proves、Does Not Prove、Limitations与Course Usage齐全。
- [x] `10-C02`保持`PARTIAL`，正文只允许写课程taxonomy与product-scoped comparison。
- [x] 核心行为性Claim无`BLOCKED`；三个设计判断显式保持`PROPOSAL`。
- [x] Plan、Workflow Definition、Runtime State、Trace已分开。
- [x] Agent Loop、State Machine、Workflow、State、Stage、Step、Transition、Guard、Invariant、Terminal、Checkpoint已分开。
- [x] Product reality的counter-evidence覆盖术语重叠、双向composition及code / LLM混合控制。
- [x] AL-04 raw facts与`PROPOSAL / NOT EXECUTED` overlay已分层；没有把expected写成observed。
- [x] Article 10 Required Lab为`NONE`；没有启动或暗示新Lab。
- [x] Article 11 stop line保持：checkpoint / retry / cancellation / resume / replay / recovery / side-effect语义均未展开。

## Evidence Gate Recommendation

`PASS_RECOMMENDED -> OUTLINE`。

核心行为性Claim在规范、current official product docs和frozen repository artifacts的窄scope内均无`BLOCKED`。Author必须保留`10-C02 PARTIAL`以及`10-C04 / C05 / C07 PROPOSAL`标签；必须将AL-04 State Machine分析标为`PROPOSAL / NOT EXECUTED`；不得跨过Article 11 Checkpoint / Recovery stop line。
