# Agent Runtime vs Harness：执行内核与工程控制面

如果这篇只记一句话，可以先记这一句：

> Runtime 负责把一次 Agent Run 推进下去；Harness 负责让跨 Run、跨 Tool、跨 Workflow 的身份、权限、证据、预算、Trace、审批、恢复和能力暴露保持同一种可审计语义。

上一篇我们解决的是“为什么最终需要 Harness”：当 Evidence、Permission、Budget、Trace、Replay、Eval、Regression、Human Review 这些横切能力必须跨多条执行链保持一致时，它们不能只散落在某个 Prompt、某个 Tool、某个业务 Workflow，或者某个 Host UI 里。

但这只回答了“为什么要有一个共享承载边界”。真正开始设计系统时，工程师还会遇到更具体的问题：

- 模型调用、工具调用、等待、暂停、恢复，到底是谁推进？
- 上下文由谁组装，又由谁决定哪些上下文可以暴露、保留、复用？
- Tool 被发现、被选择、被授权、被执行，是同一件事吗？
- Trace、Evidence、Budget、Checkpoint、Replay，是不是都可以塞进一个 `RunState`？
- 如果明天换模型、换 Host、换 Workflow Engine、换 Agent Framework，哪些记录应该继续有效？

这些问题听起来像架构命名问题，其实不是。它们是责任归属问题。

这篇把上一篇的 Harness 概念继续拆开，重点区分四类东西：

- Host：承载 Agent 的应用、环境、UI、工作区和外部系统入口。
- Business Agent / Workflow：理解业务目标，做领域判断，组织业务步骤。
- Agent Runtime：推进一次执行过程，把模型、工具、状态、等待和停止串起来。
- Harness：承载跨执行链共享的治理语义，例如身份、权限、审批、沙箱、预算、证据、Trace、Checkpoint、Replay 和能力注册规则。

注意：这里的 Runtime / Harness / Host 划分，是本课程为了讲清 Agent Engineering 责任边界而使用的教学 taxonomy，不是行业统一标准，也不是某个厂商产品名称的同义词。公开资料里的术语本来就会重叠：Microsoft Agent Framework 会用 Agent Harness 描述一组运行支架能力；LangChain 也会按自己的产品模型区分 runtime、framework、harness；OpenAI 相关产品语境中也会出现更接近“model-native harness”的表达。正因为名字会重叠，我们不能按模块名判断边界，而要按 owner、state、invariant、failure 和 replacement 判断边界。

本文证据姿态保持不变：`12 / 12` Claims 已有对应 Evidence Cards，状态为 `4 CONFIRMED / 6 PARTIAL / 2 PROPOSAL / 0 BLOCKED`。本篇没有 Required Lab，Required Lab=`NONE`；Experiment Count=`0`；Runtime Observation=`ABSENT`。BuildPilot 只作为 `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST` 使用；它不是已经实现、已经运行、已经接入 Unity、已经调用 Jenkins、已经创建 PR，或已经产生生产证据的系统。

## 1. 不要先问“产品里哪个模块叫 Harness”

很多 Agent 架构讨论会被一个看似自然的问题带偏：

> 我们用的框架里，哪个模块叫 Runtime？哪个模块叫 Harness？

这个问题太早了。

因为真实产品不会按课程概念乖乖切成四个盒子。一个 Agent Framework 可能同时提供模型调用、工具注册、状态保存、审批 Hook、Tracing、UI 组件和部署入口。一个 Host 应用可能既负责工作区、文件系统、用户交互，也承载权限弹窗。一个 Workflow Engine 可能既保存流程状态，也触发模型和工具调用。

所以，如果你只按产品名或类名切边界，很容易得到一种虚假的清晰：

| 看到的产品信号 | 为什么不能直接等同 | 更应该追问的问题 |
| --- | --- | --- |
| 某个模块叫 `Harness` | 它可能同时包含执行、状态、审批和 UX | 哪些 invariant 由它维护？哪些治理状态跨 Run 存活？ |
| 某个 API 叫 `Runner` 或 `Runtime` | 它可能包含 session、policy 或 tracing 能力 | 它是在推进执行，还是在定义共享治理语义？ |
| 某个 Workflow Engine 可以调用 Agent | 它能组织业务步骤，不代表它拥有权限、证据和预算规则 | 业务序列和跨 Workflow policy 是不是分开？ |
| Host 里有审批弹窗 | UI 承载审批动作，不代表 UI 拥有审批语义 | 批准的 scope、身份、过期、撤销和记录归谁？ |
| Agent Framework 提供全套能力 | 框架是产品包装，不等于你的系统边界 | 如果替换框架，哪些记录必须迁移并继续成立？ |

一个更可靠的起点是：不要问“它叫什么”，先问“它负责什么”。

这也是本篇和前几篇的连续关系。

在 Tool Runtime 那篇里，我们区分过“模型看见了工具定义”和“Host Registry 真的允许调用工具”。在 MCP 那篇里，我们说过协议能描述发现、Schema 和调用，但协议成功不等于业务授权成功。在 Workflow 那篇里，我们区分过 Plan、Definition、Runtime State 和 Trace。到 Evidence、Permission、Budget、Trace、Replay、Eval 这些篇章时，我们又反复把“发生了什么”和“可以据此作出什么结论”拆开。

Article 25 要做的事情，就是把这些拆分收束成一个工程责任表。

## 2. 分不清责任时，失败会失去位置

为什么要这么较真？

因为 Agent 系统一旦变复杂，最昂贵的问题往往不是“某次调用失败了”，而是“失败以后不知道该找谁”。

比如：

- 工具调用返回了 200，但执行结果不满足证据标准。是 Tool 的问题，还是 Evidence Gate 的问题？
- 用户在 UI 上点了同意，但执行动作超出了原始请求范围。是 Host UI 的问题，还是审批语义没有冻结 scope？
- Workflow 重试了一步，结果预算被超额消耗。是 Runtime 没停住，还是 Harness 没有统一预算 ledger？
- Trace 里有完整日志，但审稿人仍然不能接受结论。是 Trace 不够，还是 Evidence acceptance 从未定义？
- Agent 记住了某段上下文，但这个上下文不应该跨项目复用。是 Context assembly 的问题，还是 Context policy 的问题？

这些问题都不是靠“再加一个 if”解决的。它们需要边界。

边界不是为了画组织结构图，而是为了让失败能被定位，让责任能被替换，让记录能被审计。

| 被合并的边界 | 表面上省掉了什么 | 代价是什么 |
| --- | --- | --- |
| Runtime = Harness | 少一层概念，执行 loop 可以直接调工具 | 每条执行链都可能复制一套权限、预算、证据和恢复语义 |
| Host = Harness | UI 弹窗和环境入口看起来就是治理层 | 用户交互状态容易被误认为授权事实 |
| Workflow = Harness | 业务流程里顺手写审批、重试、预算逻辑 | 换一个流程后，共享规则失去一致性 |
| Agent Framework = 系统架构 | 直接接受框架默认边界 | 框架升级或替换时，不知道哪些业务记录和治理记录必须保留 |
| Business Agent = 全部 | Prompt 里解释所有规则 | 业务判断、执行状态、权限证据和恢复策略混在一起 |

好架构的味道，往往不是“每层都很强”，而是“每层失败时，其他层知道它失败在哪里”。

## 3. 五问：判断责任边界的最小工具

本课程用一个五问模型来判断 Runtime、Harness、Host 和 Business Agent 的边界。

这不是外部标准，而是一个教学工具。它的价值在于，当你面对一个真实框架或产品时，不必争论名字，而是可以快速判断责任归属。

| 问题 | 要找的不是 | 要找的是 |
| --- | --- | --- |
| Owner：谁拥有这个责任？ | 谁刚好调用了这个函数 | 谁必须保证这条规则在不同 Run、Tool、Workflow、Host 中仍成立 |
| State：状态归谁保存？ | 有没有一个 JSON 字段 | 状态的生命周期、恢复方式、审计语义和删除边界 |
| Invariant：不变量是什么？ | 当前 demo 能不能跑通 | 换模型、换工具、换流程后仍然不能破坏的规则 |
| Failure：失败时谁接手？ | 谁抛出了异常 | 谁决定 retry、resume、escalate、ask human、stop 或 mark unknown |
| Replacement：替换一层时什么不能丢？ | 哪个库最方便 | 哪些业务记录、治理记录、证据链和用户承诺必须继续有效 |

你可以把这五问用在任何一个“模糊模块”上。

例如，一个 SDK 提供了 `runAgent()`，里面包括模型调用、工具调用、memory、tracing 和 human approval。按产品名看，它可能叫 Agent Runtime。按五问看，就要拆成几层：

- 推进 step、等待 tool result、继续下一轮模型调用：偏 Runtime。
- 定义哪些工具在什么身份下可见：偏 Harness。
- 弹出用户确认界面：偏 Host。
- 判断这个任务应该修改需求、生成报告还是放弃：偏 Business Agent / Workflow。
- 保存“某次审批只针对某个冻结请求有效”：偏 Harness 的 governance state。

这就是本篇最重要的技巧：不要用一个名词吞掉多个 owner。

## 4. 先把几层放在一张责任表里

下面这张表，是 Article 25 的核心工作表。

| 层 | 主要负责 | 不应该独自负责 |
| --- | --- | --- |
| Host | 应用生命周期、用户交互、工作区、文件系统或外部系统入口、环境可见范围 | 跨 Run 的权限语义、证据接受标准、预算治理、业务判断 |
| Business Agent / Workflow | 业务目标解释、领域判断、任务拆解、业务步骤排序、产出组织 | 通用身份授权模型、通用预算 ledger、通用证据接受规则、底层执行推进 |
| Agent Runtime | 模型调用、工具调用推进、step loop、等待、暂停、恢复、停止、局部执行状态 | 跨业务的权限政策、证据政策、长期审计语义、业务最终决策 |
| Harness | 身份、权限、审批、沙箱、预算、Trace、Evidence、Checkpoint、Replay、能力注册与发现的共享规则 | 具体业务目标判断、Host UI 呈现细节、每一步模型输出内容 |
| Agent Framework | 提供开发抽象、SDK、默认 runner、memory、tools、workflow 或 tracing 能力 | 自动等同于你的系统边界 |
| Workflow Engine | 保证流程定义、合法迁移、流程状态和执行顺序 | 自动等同于 Harness；也不必吞掉所有 Runtime 责任 |

这张表不是说真实系统里一定要有六个独立进程。它说的是：即使实现上合并，责任也要能被分开讲清楚。

一个小团队完全可以把 Runtime 和 Harness 写在同一个代码仓库里，甚至同一个服务里。问题不在于物理部署是否合并，而在于：

- 审批记录是不是有自己的 scope、actor、resource、expiry 和 decision？
- Budget 是不是能跨工具、跨步骤、跨 workflow 被统一计算？
- Evidence acceptance 是不是独立于“工具返回了什么”？
- Runtime 重试时，是不是重新检查权限、预算和上下文可见性？
- 换一个业务 Agent 后，治理语义是不是仍然一致？

如果这些问题没有答案，那么你只是把层画在图上，并没有把责任拆出来。

## 5. 四类 State：不要把所有东西都叫 Agent State

“Agent State” 是一个很容易变大的词。

一开始它可能只表示一次运行里的消息历史。很快，它会开始装：

- 当前执行到哪一步；
- 用户批准了什么；
- 哪些上下文已经被引用；
- 预算还剩多少；
- 哪些证据已经被接受；
- Tool schema 版本是什么；
- 业务任务处在什么阶段；
- Host 当前打开的是哪个项目。

这时如果还把它们都叫一个 state，系统会变得很难维护。更好的方式是按 owner 和生命周期拆。

| State 类型 | 典型 owner | 生命周期 | 例子 | 错放后的问题 |
| --- | --- | --- | --- | --- |
| Business State | Business Agent / Workflow，加上 Owner 决策 | 业务任务生命周期 | 需求候选、问题分类、审稿处置、是否采纳建议 | Runtime 换掉后业务事实丢失，或执行日志被误当业务决策 |
| Execution State | Runtime / Workflow Engine | 单次 Run、step 或 workflow instance | 当前 step、pending tool call、wait boundary、resume cursor | 失败恢复只能靠猜，或无法判断该从哪里继续 |
| Governance State | Harness | 跨 Run、跨 Tool、跨 Workflow 的治理生命周期 | approval scope、permission grant、budget reservation、evidence acceptance、trace policy | 授权、预算、证据和审计语义被复制到每条执行链里 |
| Host / UI State | Host | 用户会话、应用窗口、工作区或环境生命周期 | 当前项目、可见文件、workspace root、当前用户输入、UI 选择 | 把“用户看见/点了什么”误当成“系统获权/证明了什么” |

这个拆分能解释很多 Agent 系统里的奇怪 bug。

比如，一个用户在 Host 里选中了某个项目目录，这只是 Host / UI state。Runtime 可以用它来组装当前执行上下文，但它本身不是长期权限授权。Harness 还要判断这个目录是否在允许的工作区内、要执行的动作是否超出当前批准范围、结果能否进入证据链。

再比如，Workflow 里某个节点叫“等待人工批准”，这说明业务流程需要 human review，但审批记录本身不应该只藏在这个节点的局部变量里。批准了什么、谁批准、什么时候批准、针对哪个冻结请求、是否过期、是否可撤销，这些是 governance state。

一句话：Execution State 让执行能继续；Governance State 让继续这件事仍然合规、可审计、可复盘。

## 6. Context assembly 不是 Context policy

Context 是另一块最容易混在一起的边界。

一个 Agent 在运行时当然要组装上下文：用户目标、系统说明、历史消息、文件片段、工具结果、检索结果、前一轮决策、预算状态、错误信息……这些东西最终会进入模型可见的上下文窗口。

但“谁把上下文拼起来”和“谁决定上下文能不能被拼进去”不是同一个问题。

本课程把它拆成四个角色：

| 角色 | 在 Context 中的责任 |
| --- | --- |
| Host | 提供环境入口，例如当前项目、文件系统、浏览器状态、用户会话、外部系统连接；也限制哪些环境对象可见 |
| Business Agent / Workflow | 判断当前业务目标需要哪些事实、材料和前置决策，决定业务上的上下文优先级 |
| Runtime | 在一次执行中按当前 step 需要装配 prompt、messages、tool observations 和局部执行状态 |
| Harness | 定义上下文暴露、保留、裁剪、隔离、复用、预算、过期、引用回执、敏感信息处理等共享 policy |

举一个具体例子。

假设 BuildPilot 需要根据一个需求变更请求，读取项目里的配置文件、测试报告和历史构建记录，生成一份变更建议。Runtime 可以负责把当前 step 需要的片段装进模型上下文。Business Agent 判断“这次重点是兼容性风险，而不是 UI 文案”。Host 提供当前 workspace root 和用户选择的文件。Harness 则要决定：

- 哪些路径属于本次允许读取的范围；
- 哪些日志可以被引用，哪些需要脱敏；
- 上下文片段是否需要记录来源；
- 某个旧观察能否跨 Run 复用；
- 超出 token 或成本预算时如何裁剪；
- 模型输出中哪些结论必须绑定证据；
- 当上下文不完整时是否必须保留 unknown。

如果把这些全塞给 Runtime，Runtime 会变成“又要跑、又要判、又要管、又要背锅”的巨型黑盒。短期看写起来快，长期看每一次新增工具、每一个新业务 Workflow、每一种新审批场景都会复制一遍规则。

所以更清晰的表述是：

> Runtime 组装当前执行所需的上下文；Harness 约束上下文的可见性、可复用性和证据语义。

## 7. Runtime：负责把执行推进到边界

Agent Runtime 的核心不是“聪明”，而是“推进”。

它负责把一次 Agent Run 从输入推进到可解释的边界：完成、等待、调用工具、转交、暂停、失败、恢复，或者停止。

在本课程里，一个最小 Runtime 通常会做这些事：

```text
receive run input
  -> load execution state
  -> assemble current context under policy constraints
  -> call model
  -> interpret output as final / tool call / handoff / wait / error
  -> dispatch allowed tool call or workflow step
  -> normalize observation references
  -> update execution state
  -> continue, wait, recover, or stop
```

这条链条里有两个容易误解的点。

第一，Runtime 可以执行检查，但不等于 Runtime 拥有所有检查规则。

例如 Runtime 在 tool dispatch 之前可以调用 permission check，在调用模型之前可以询问 budget admission，在生成答案之后可以触发 evidence review。但这些检查规则如果要跨 Run、跨 Tool、跨 Workflow 保持一致，就不应该只散落在 Runtime 的私有逻辑里。Runtime 是执行者和推进者，Harness 才是共享治理语义的承载者。

第二，Runtime 可以记录 observation，但不等于 observation 已经变成 evidence。

工具返回、日志片段、HTTP 状态码、文件读取结果、测试输出，都只是 observation。它们能不能支持某个 claim，还要经过证据接受标准。这个标准不属于单次执行 loop 的自然副产品，而属于 Harness 或与 Harness 协作的 Evidence Gate。

这就是为什么 Tool Runtime 那篇强调过：Tool call 的成功不自动等于业务成功。Article 25 继续把这件事推广到整个 Agent Runtime。

Runtime 的好设计，应该让你能回答：

- 现在执行到哪一步？
- 为什么进入下一步？
- 是否正在等待工具、用户、子任务或外部系统？
- 如果失败，从哪个边界恢复？
- 恢复时是否需要重新检查权限、预算、上下文和证据？
- 这次执行产生了哪些 observation，但哪些还不是 accepted evidence？

如果 Runtime 能回答这些问题，它就是一个可调试的执行内核。

如果 Runtime 同时私吞了所有审批、预算、证据、注册和业务决策，它就不再是执行内核，而是一个会随着系统规模增长而越来越难替换的总线怪兽。

## 8. Harness：负责共享治理语义

Harness 的核心也不是“聪明”，而是“让共享控制保持同一种语义”。

在本课程里，Harness 负责承载这些横切能力：

- identity：谁在发起、代表谁执行、以什么 actor 身份访问资源；
- permission：某个 actor 对某个 resource、action、scope 是否有权限；
- approval：某次人工批准绑定到哪个冻结请求、动作范围和有效期；
- sandbox：执行能力被限制在哪些文件、网络、命令、账户或外部系统范围内；
- budget：token、成本、时间、步骤、工具调用次数、风险额度等预算如何入账和停止；
- trace：发生了什么、谁触发、在哪个 step、影响了哪些资源；
- evidence：哪些 observation 能支持哪些 claim，置信度和接受状态是什么；
- checkpoint：哪些边界可以保存，用于恢复、审计或人工接管；
- replay：哪些部分可以重放，哪些只能解释而不能重放；
- registry / discovery：能力如何注册、被看见、被选择、被授权和演化。

它不是每一步业务逻辑的主人，也不一定是单独部署的服务。它更像一组跨运行链条共享的规则和账本。

可以把 Harness 的权限链路想成这样：

```text
action intent
  -> stable actor / run / step / resource identity
  -> permission check
  -> risk route
  -> approval bound to frozen scope
  -> sandbox / capability scope
  -> execution by Runtime / Tool Runtime
  -> decision record and evidence references
  -> resume, stop, or human takeover
```

这里最重要的是顺序和绑定关系。

“用户点了同意”不是一个孤立事实。它必须绑定到：

- 谁批准；
- 批准了哪个动作；
- 针对哪个冻结后的请求；
- 作用于哪个资源范围；
- 在什么时间和预算内有效；
- 是否允许重试；
- 是否允许在 resume 后继续复用；
- 执行后如何留下 trace 和 evidence reference。

如果这些绑定没有被记录，那么 approval 只是一个 UI 事件，不是治理事实。

同样，“工具可见”也不是授权。“工具调用成功”也不是证据。“Trace 里有日志”也不是结论。“Checkpoint 存在”也不代表可以安全 replay。

Harness 的职责，是把这些区别写进系统行为，而不是写在团队脑子里。

## 9. Budget、Trace、Evidence、Checkpoint、Replay：一组相关但不同的账

很多系统会把 Budget、Trace、Evidence、Checkpoint、Replay 全部塞进 observability 或 state management。这样做短期很诱人，因为它们都“和记录有关”。

但它们回答的问题不同。

| 名称 | 主要回答 | 典型 owner | 不能替代什么 |
| --- | --- | --- | --- |
| Budget | 还允许花多少资源？什么时候必须降级、询问或停止？ | Harness，Runtime 负责执行停止边界 | 不能证明输出正确，也不能解释失败根因 |
| Trace | 发生了什么？因果链和资源影响是什么？ | Harness / Observability，Runtime 提供事件 | 不能自动变成证据接受，也不能自动允许 replay |
| Evidence | 哪些 observation 支持哪些 claim？接受状态是什么？ | Evidence Gate / Harness | 不能替代完整日志，也不能替代审批 |
| Checkpoint | 哪个边界可保存并恢复？恢复需要哪些前置条件？ | Runtime / Workflow Engine 与 Harness 协作 | 不能保证重放会得到相同结果 |
| Replay | 哪些输入、环境、工具版本和状态可重放？重放目的是什么？ | Harness 定义语义，Runtime 执行可重放片段 | 不能替代生产验证，也不能证明原执行无误 |

这张表背后有几条硬边界：

- Trace 记录发生过什么，Evidence 判断能支持什么。
- Checkpoint 让系统从边界恢复，Replay 让某些路径可复现或可解释。
- Budget 控制资源消耗，不负责证明结果正确。
- Runtime 负责在边界处停下、恢复和推进；Harness 定义这些边界的共享语义。
- 一个 HTTP 200、一次工具成功、一个生成文件、一次模型回答，都只证明自己的那一层。

这也是为什么本课程一直强调 layered evidence。Agent Engineering 的可靠性，不是“多存日志”就能得到的，而是要让每类记录知道自己能证明什么，不能证明什么。

## 10. Registry / Discovery：存在、可见、相关、获权、执行是五个问题

工具能力也经常被一个词吞掉：discovery。

“Agent 发现了工具”到底是什么意思？

至少有五种可能：

| 问题 | 含义 | 主要 owner |
| --- | --- | --- |
| 存在 | 系统里注册了某个工具、技能、MCP server、API 或能力入口 | Registry / Host / Framework |
| 可见 | 当前 actor、workspace、risk level、budget 下是否能看见这个能力 | Harness |
| 相关 | 当前业务目标是否应该使用这个能力 | Business Agent / Workflow |
| 获权 | 当前请求是否允许调用这个能力，是否需要审批或沙箱限制 | Harness |
| 执行 | 如何把调用发出去、等待结果、处理错误、记录 observation | Runtime / Tool Runtime |

如果把这五件事都叫 discovery，系统会很快变危险。

例如，MCP server 报告了某个 tool schema，只说明这个 server 暴露了一个能力描述。它不自动说明当前用户有权调用它，不说明这次业务任务应该调用它，也不说明调用结果能成为 evidence。

一个更稳的链条是：

```text
registry knows capability exists
  -> harness filters allowed view
  -> business agent selects relevant capability
  -> harness checks permission / approval / budget / sandbox
  -> runtime dispatches call
  -> tool runtime returns observation
  -> evidence gate decides whether claim can rely on it
```

这个链条听起来长，但它可以防止一个常见错误：把“能力已经被集成”误认为“能力已经被安全使用”。

能力注册是工程事实。能力可见是治理事实。能力相关是业务判断。能力获权是权限事实。能力执行是运行事实。能力结果能不能成为证据，是 Evidence Gate 的判断。

它们最好不要互相冒充。

## 11. BuildPilot：把一条需求变更链按层分账

现在用 BuildPilot 做一个设计案例。

先再次标明边界：BuildPilot 在本文中只是 `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`。它没有被实现，没有运行，没有扫描 Unity 工程，没有接入 Jenkins，没有创建 PR，也没有修改任何项目文件。它只是一个用来训练边界感的课程案例。

假设一个团队想让 BuildPilot 处理这样的 owner 请求：

> 看一下这个需求变更会影响哪些客户端模块，给我一份可审稿的变更建议，不要自动改代码。

如果按“一个 Agent 全都做”的方式写，Prompt 可能会说：

> 读取项目，分析影响，生成建议，必要时调用工具。

这在 demo 里可能能工作，但工程边界很糊。我们按本文责任表重新拆：

| 步骤 | 责任 owner | 应该产生什么 | 不应该越界做什么 |
| --- | --- | --- | --- |
| Owner 提交需求变更 | Host | 用户输入、当前 workspace、可见项目范围 | 不把“用户说了”直接当成修改授权 |
| 理解需求和风险方向 | Business Agent / Workflow | 业务任务结构、检查方向、输出格式 | 不拥有通用权限、预算和证据规则 |
| 过滤可见能力和上下文 | Harness | allowed tool view、read-only scope、预算和证据要求 | 不做具体业务结论 |
| 推进读取和分析步骤 | Runtime | model calls、tool calls、等待、失败边界、execution state | 不把 observation 直接当 accepted evidence |
| 执行只读工具 | Tool Runtime | 文件片段、配置读取、测试报告、日志观察 | 不自动扩大权限或修改文件 |
| 整理证据状态 | Harness / Evidence Gate | observation、claim、confidence、acceptance、unknown | 不把缺失证据补成结论 |
| 生成变更建议 | Business Agent / Workflow | evidence-backed change request | 不替 owner 采纳或实施 |
| Owner 审阅和实施 | Owner / 外部流程 | 是否采纳、如何修改、谁负责上线 | BuildPilot 不自动提交 PR 或改生产 |
| 后续只读复核 | Runtime + Harness | 在允许范围内重新检查 evidence 和 trace | 不把复核当完整生产验证 |

这个案例里，Runtime 的价值是把执行链跑下去：读哪些材料、何时等待、何时停止、何时恢复、何时报告失败。Harness 的价值是让 BuildPilot 的行为始终保持同一种治理语义：只读、建议优先、证据绑定、预算可控、审批可审计、能力可解释。

如果工具超时，Runtime 负责处理 pending call、timeout 和恢复边界；Harness 决定是否允许 retry，以及 retry 是否消耗新的预算。

如果某个文件不在允许读取范围内，Host 可以展示用户当前选择的 workspace，但 Harness 决定这个读取是否被允许；Runtime 只负责按结果继续、等待或停止。

如果模型推断“这个需求可能影响登录链路”，Business Agent 可以把它组织成候选影响；Evidence Gate 必须标明它是推断、观察还是已接受证据。

如果 owner 决定采纳建议并让工程师修改，那是 owner 和外部实现流程的事情。本文中的 BuildPilot 不越过这个边界。

这就是 suggestion-first 的含义：Agent 可以准备更好的判断材料，但不偷走 owner 的决策和实施责任。

## 12. 失败时谁来 retry、resume、ask 或 stop

边界真正有用的时刻，是失败发生之后。

一个可维护的 Agent 系统，不应该只记录“失败了”。它应该能回答：失败属于哪一层？谁有权恢复？恢复前要重新检查什么？

| 失败场景 | Runtime 负责 | Harness 负责 | Business / Host / Owner 负责 |
| --- | --- | --- | --- |
| 模型调用失败 | 标记当前 step、重试可执行片段、保留错误 observation | 根据预算、风险和策略决定是否允许 retry | 通常不需要业务介入，除非持续失败 |
| 工具超时 | 记录 pending call、timeout、resume boundary | 判断 retry 次数、预算消耗、是否降级 | 业务层决定是否接受不完整材料 |
| 权限不足 | 停止或进入等待，不伪造结果 | 给出 permission denial、approval path、scope 要求 | Host 展示请求；Owner 决定是否批准 |
| Evidence 不足 | 输出 unknown 或 evidence gap，不补结论 | 标记 claim 不可接受或需要补证 | Business Agent 重组报告；Owner 判断是否继续 |
| Approval 过期或 scope 不匹配 | 暂停执行，不复用旧批准 | 判定 stale approval，要求重新冻结请求 | Host 重新询问；Owner 重新决策 |
| Budget 用尽 | 停止、降级或等待 | 执行 budget stop rule，记录原因 | Business Agent 给出残缺范围；Owner 决定是否追加预算 |
| 工具能力缺失 | 报告 tool gap，不假装完成 | 记录 capability gap 和安全集成候选 | Owner / 工程团队决定是否开发或接入新工具 |
| 需要人工接管 | 暂停到明确 checkpoint | 记录 takeover reason、state、scope 和证据状态 | Human owner 接管判断和后续动作 |

这里最容易犯的错误，是把 retry 当成纯 Runtime 问题。

Runtime 当然要会 retry。但能不能 retry、retry 几次、是否需要重新审批、是否重新计预算、是否会改变证据语义，这些不是单纯的执行技巧。它们属于 Harness 的共享 policy。

另一个容易犯的错误，是把 ask human 当成一句自然语言。

“询问用户”如果只是发一句话，就很容易丢失 scope。更稳的做法是让 Human Review 变成状态机：问题是什么、当前证据是什么、可选决策是什么、每个决策授权哪些后续动作、过期条件是什么、回到 Runtime 后从哪里恢复。

人不是系统的异常处理器。人是某些决策的 owner。

## 13. 用替换压力检查边界

一个边界是不是真的清楚，可以用替换压力测试。

问自己：如果换掉某一层，哪些东西应该继续成立？

| 替换对象 | 可以改变 | 不应该丢失 |
| --- | --- | --- |
| 换模型 provider | prompt 格式、模型行为、token 价格、响应风格 | 权限、审批、预算 ledger、证据接受标准、业务记录 |
| 换 Runtime SDK | step loop 实现、tool dispatch API、resume 机制 | governance state、evidence records、business state、owner decisions |
| 换 Host 应用 | UI、文件选择方式、环境入口、用户交互模式 | approval semantics、permission model、trace/evidence 可审计性 |
| 换 Workflow Engine | 流程定义语法、状态机实现、编排方式 | 业务目标、证据约束、权限规则、预算规则 |
| 换 Agent Framework | SDK、默认 memory、内置 tool abstraction、observability 插件 | 你的系统责任边界和已经承诺给用户的治理语义 |
| 换业务 Agent | Prompt、业务检查策略、报告结构 | 共享 Harness 规则、Host 环境边界、Runtime 执行协议 |

这张表的目标不是鼓励你频繁替换框架。相反，它是在设计早期暴露耦合。

如果换一个 Runtime SDK 就导致所有审批记录失效，说明 approval 被藏进了 Runtime 私有状态。

如果换一个 Host 就导致证据链不可审计，说明 Evidence 和 Trace 太依赖 UI 状态。

如果换一个业务 Agent 就要重写权限、预算和沙箱，说明 Harness 没有真正出现。

替换压力测试有点像架构里的“轻轻拽一下线头”。如果一拽整件毛衣都散了，就说明这些线其实没有分开。

## 14. 一套 Runtime / Harness 边界通常怎样写坏

下面这些坏味道，在早期 Agent 项目里非常常见。

| 坏味道 | 表面症状 | 更深的问题 |
| --- | --- | --- |
| 所有东西都叫 `agent_state` | 一个状态对象越来越大，字段没人敢删 | Business、Execution、Governance、Host/UI state 混在一起 |
| Tool schema 可见就允许调用 | 模型能看见工具，所以直接调用 | Discovery、permission、approval、sandbox 没拆开 |
| Trace 被当 Evidence | 日志很全，于是结论默认可信 | Observation、claim、acceptance 没拆开 |
| Approval 是一句“用户同意了” | 没有冻结 scope、actor、resource、expiry | UI 事件冒充治理事实 |
| Retry 写在每个 tool wrapper 里 | 各工具重试次数、预算和错误语义不一致 | Retry policy 没有提升到 Harness |
| Context 由 Prompt 随便拼 | 上下文越来越长，来源和复用边界不清 | Context assembly 和 Context policy 混在一起 |
| Business Agent 直接改生产 | 建议、采纳、实施混成一个动作 | Owner 决策和执行授权被跳过 |
| 换框架等于重写治理 | 所有边界跟某 SDK 绑定 | 产品抽象被误认为系统架构 |

这些问题的共同点是：系统能跑，但不可解释；demo 很顺，但上线之后每个异常都像雾。

真正的工程边界不是为了让图好看，而是为了让你能在异常里保持判断力。

## 15. 本篇能建立什么，不能证明什么

到这里，Article 25 建立的是一个责任边界模型：

- Runtime 是执行推进者。
- Harness 是共享治理语义的承载者。
- Host 是环境和用户交互的承载者。
- Business Agent / Workflow 是业务目标和领域判断的组织者。
- Tool/Skill Registry、Discovery、Permission、Execution 和 Evidence Acceptance 必须分开看。
- Context assembly 和 Context policy 必须分开看。
- Business State、Execution State、Governance State、Host/UI State 必须分开看。
- Failure、Retry、Recovery、Human Takeover 要按 owner 拆开。
- 替换压力可以帮助发现边界是否真的成立。

同时，本篇不能证明这些事：

- 它没有证明某个具体产品的模块划分就是本文划分。
- 它没有证明 BuildPilot 已实现、已运行或已接入任何真实工程。
- 它没有提供 Article 26 的最小 Harness 模型。
- 它没有展开 Article 27 的 adoption、bloat、framework tie-in 和 governance cost 权衡框架。
- 它没有提供运行时实验、性能数据、生产事故复盘或线上验证。

这些限制不是缺陷，而是边界的一部分。Article 25 的任务是先把责任分清楚。下一篇可以在这个基础上继续问：如果真的要做一个最小可用 Harness，哪些能力必须先进来，哪些能力可以等，哪些东西绝不能一开始就膨胀。

## Claim Traceability（12 / 12）

为了保持本篇的证据边界，下面把 12 个 Claim 与正文位置对应起来。

| Claim | 本文落点 | 状态 |
| --- | --- | --- |
| C01 Runtime owns execution progression | 第 7 节 Runtime 执行推进链 | CONFIRMED |
| C02 Host 是独立应用 / 环境承载边界 | 第 4、6、11 节 Host 责任 | CONFIRMED |
| C03 Tool discovery / call / result 与 permission / evidence acceptance 分离 | 第 10 节能力链条，第 7、9 节证据边界 | CONFIRMED |
| C04 Workflow / state mechanisms 与自由 Agent loop 不同 | 第 4、5、12 节 Workflow 与 Execution State | PARTIAL |
| C05 Context assembly 与 Context policy 分离 | 第 6 节 | PARTIAL |
| C06 Identity / Permission / Approval / Sandbox 可作为可分离控制面 | 第 8 节 | PARTIAL |
| C07 Budget / Trace / Evidence / Checkpoint / Replay 相关但不等价 | 第 9 节 | PARTIAL |
| C08 Business / Execution / Governance / Host UI state 按 owner 和生命周期区分 | 第 5 节 | PARTIAL |
| C09 Failure / Retry / Recovery / Human Takeover 分执行机制和 policy 决策 | 第 12 节 | PARTIAL |
| C10 Vendor terminology varies; compare responsibility, not product labels | 第 1 节 | CONFIRMED |
| C11 Five-question boundary test is course model | 第 3 节 | PROPOSAL |
| C12 BuildPilot allocation case is design-only | 第 11 节 | PROPOSAL |

这里的状态保持 `4 CONFIRMED / 6 PARTIAL / 2 PROPOSAL / 0 BLOCKED`。状态不是文章修辞，而是证据约束：CONFIRMED 可以作为较稳的事实背景；PARTIAL 只能支撑课程内的工程判断；PROPOSAL 只能作为课程方法或设计建议。

## Learning Check

1. 如果一个框架同时提供 model call、tool call、state、approval 和 tracing，它是否自动等于本文的 Harness？
2. Tool schema 出现在模型上下文里，是否说明当前 Agent 有权调用它？
3. Runtime 保存了 checkpoint，是否说明这次执行可以完整 replay？
4. 用户在 Host UI 点了同意，为什么还不等于长期授权？
5. BuildPilot 在本文中为什么只能生成 suggestion，而不能自动改代码？

### 参考思路

1. 不自动等于。产品能力可以合并，但本文按 owner、state、invariant、failure、replacement 判断责任。
2. 不说明。schema 可见只是能力描述或 allowed view 的一部分，还要经过 permission、approval、sandbox、budget 等检查。
3. 不说明。Checkpoint 只是恢复边界；Replay 还需要输入、环境、工具版本、状态和目的语义。
4. 因为 approval 必须绑定 actor、resource、action、scope、expiry、frozen request 和记录语义。UI 事件本身不是完整治理事实。
5. 因为本文中的 BuildPilot 明确是 `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`。它可以准备证据化建议，owner 和外部流程才负责采纳、实施和上线。

## 参考资料

- [Microsoft Agent Framework documentation](https://learn.microsoft.com/en-us/agent-framework/)
- [LangChain documentation](https://docs.langchain.com/)
- [LangGraph documentation](https://langchain-ai.github.io/langgraph/)
- [OpenAI Agents SDK documentation](https://openai.github.io/openai-agents-python/)
- [Model Context Protocol documentation](https://modelcontextprotocol.io/)

这些资料用于支撑“公开产品术语存在重叠”和“运行、工具、Host、Workflow、治理能力可以被不同产品以不同方式包装”的事实背景。本文不把任何一个产品的命名当成行业标准，而只抽取课程内的责任边界模型。

## 最短结论

Agent Runtime 和 Harness 的区别，不是“哪个模块更底层”，而是“谁负责把执行推进下去，谁负责让共享治理语义长期成立”。

Runtime 要会跑：调用模型、派发工具、等待、恢复、停止。

Harness 要会管：身份、权限、审批、沙箱、预算、Trace、Evidence、Checkpoint、Replay 和能力暴露规则。

Host 要会承载：用户、工作区、环境入口和交互。

Business Agent / Workflow 要会判断：业务目标、领域步骤、建议结构和 owner 决策材料。

这四者可以在实现上合并，但不能在责任上糊成一团。否则系统越能跑，越难知道它到底凭什么跑、为谁跑、还能不能继续跑。

下一篇，我们就可以在这个边界上继续收缩问题：一个最小 Harness，到底应该先做哪些能力，才能在不膨胀成平台怪兽的前提下，真正撑住 Agent Engineering 的第一条生产线。
