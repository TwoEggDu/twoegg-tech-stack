# Agent Engineering 课程术语表

本表只维护课程内的工作定义、首次引入和正式展开位置，不试图替代百科或产品文档。不同生态若使用同名异义，文章必须说明采用哪一种定义。

| Term | 课程工作定义 | 首次引入 | 正式展开 | 边界说明 |
|---|---|---:|---:|---|
| LLM / Model | 依据输入上下文生成输出的模型能力；本身不是完整应用或 Agent | 00 | 01 | 稳定抽象；具体 API、部署和版本留给 01 |
| AI Application | 把模型调用与软件逻辑、数据、工具或界面组合成可用能力的应用 | 00 | 01—04 | 可以是单次调用、确定性流程，也可以包含 Agent |
| Product | 面向用户交付的软件 / 产品边界，可以提供一个或多个 Surface / Entry Point | 00 | 00 | 产品术语；与 AI Application 可以重叠，但不主张在所有语境下严格同义 |
| Surface / Entry Point | 产品对外可观察的使用入口，例如 CLI、IDE、Web、Desktop、CI Integration、Unity Editor Integration | 00 | 00 | 产品 / 可观察术语；Surface 不等于 Host，二者映射需要实现证据 |
| Copilot | 厂商定义的产品 / 产品族名称，可包含同步辅助和 Agentic 能力 | 00 | 00 | 产品术语；不对应固定架构层，也不是 Agent 的前置阶段 |
| Agent | 围绕用户目标，由模型参与决定推进方式，并通过行动 / 工具与反馈处理多步任务的软件系统 | 00 | 08 | 稳定工程核心；Loop、State、Stop 的正式定义留给 08 |
| Agentic | 不同生态用来描述自主行为、Agent 模式或更广义 LLM 系统的词 | 00 | 00 | 生态依赖；课程只用作程度 / 特征描述，不作为严格类型 |
| Agent Runtime | 本课程对 Agent 执行职责的称呼，包括模型调用、工具分派、循环、状态 / continuation 与停止 | 00 | 25 | 课程工作定义；命名、边界和承载者随框架变化 |
| Harness | 本课程对 Runtime 周围可复用工程控制与约束层的称呼 | 00 | 24—27 | 课程工作定义；现有证据不足以支持统一行业定义，也不等同 DSH 产品全貌 |
| Host | 本课程对承载或集成 Agent 执行 / Agent Runtime 的具体宿主程序、进程或运行环境的称呼 | 00 | 25、29 | 课程工作定义；多个 Surface 可共享 Host，一个 Surface 也可能涉及多个运行组件，映射需独立证据 |
| Capability | Agent 可被授予和治理的一类能力契约 | 00 | 26 | 不等同某一个具体 Tool |
| Provider | 提供模型、工具或基础设施服务、账号、认证、配额与 API contract 的主体或平台；在 Adapter / Gateway 语境也可指向一项可替换实现 | 01 | 01、04、31 | 主体与 implementation target 是两个语境；具体差异需要 Adapter / seam 吸收 |
| Model Adapter | 把某个 Provider 的请求、stream、terminal、usage 与 error contract 保真映射到应用侧接口的边界 | 04 | 04、31 | 课程责任切分，不是行业统一定义；不拥有领域真相、Agent Loop 或 Recovery |
| LLM Gateway | 位于一个或多个模型调用路径上的流量与治理边界，可承载路由、认证、限流或观测等产品能力 | 04 | 04 | 产品能力集合随实现与 service tier 变化；Gateway 不自动等于 Agent Runtime |
| Prompt | 提供给模型的任务、指令、示例与输出要求 | 00 | 02 | 只是 Context 的一部分 |
| Context | 某一步中模型用于生成的 effective token / 信息集合；应用侧只能装配、记录和比较自己可见的 Context Snapshot | 00 | 12—13 | 12 建立 Select / Order / Scope / Fit Budget、Snapshot 与 Receipt；Packing、Compression、Pollution、重建边界留给 13 |
| Tool | 暴露给模型选择或请求的外部数据 / 动作能力 | 00 | 05—07 | 模型发出调用请求，实际执行与安全控制属于应用 / Runtime |
| Function Calling | 模型用结构化 tool / function call 表达行动意图的机制 | 05 | 05 | call request 不等于授权、执行、Observation 或事实成立 |
| Tool Runtime | Host 对工具请求执行 Validate、Policy、Execute、Result 与 Trace 的受控管线 | 05 | 06 | 课程责任边界；不等于 Function Calling，也不证明 production safety 或 exactly-once |
| MCP | Host / Client 与外部 Server 能力之间的协议与传输边界 | 07 | 07 | 协议可达或调用成功不自动授予权限，也不证明完整 Tool Runtime 或 Agent Loop |
| Skill | 可按需加载的领域说明、方法和配套资源 | 00 | 17 | 具体封装和激活方式随生态变化，不只是更长的 Prompt |
| Workflow | 通过较预定义的步骤、分支与决策点推进任务的骨架 | 00 | 10 | 生态边界不完全一致；Agent 可以嵌入 Workflow |
| Plan | Goal 与 Current Evidence 条件下对剩余候选行动的表示 | 09 | 09 | 课程工作定义；不是 Execution、Observation、State、Authorization 或 Workflow |
| Observation | Tool Outcome 经关联与正规化后提供给 Agent Loop / reducer 的可解释观察 | 08 | 08 | 课程 Host 边界；不等于 raw Result，也不自动成为 accepted Evidence 或 State |
| State | 当前已提交的控制位置与相关权威数据 | 08 | 10 | 不是 Plan、History、Trace 或 Checkpoint |
| State Machine | 用 State、合法 Transition、Guard 与 Terminal 约束推进的确定性骨架 | 10 | 10 | 不自动等于 Workflow、Agent Loop 或 Recovery implementation |
| Guard | 某条 Transition 本次是否 enabled 的布尔前置条件 | 10 | 10 | 不生成开放式候选，不等于 Invariant，也不证明副作用完成 |
| Invariant | 所有 reachable State 都必须保持成立的 predicate | 10 | 10 | 课程 / source-informed 工作定义；具体 commit hook 不是行业统一合同 |
| Agent Decision Point | 在确定性过滤后的合法候选之间，让 Agent 基于上下文与 Evidence 提交 suggestion 的窄接口 | 10 | 10 | `COURSE PROPOSAL`；suggestion 不具有 State commit authority |
| Checkpoint | 把已提交位置、稳定身份、已知 / 未知、in-flight action、预算与 continuation boundary 绑定成可恢复 artifact | 10 | 11 | 课程 candidate；不是 State 的磁盘截图、Memory、Session 或 Context Snapshot |
| Retry | 在资格、同一 action intent、稳定身份、副作用状态与预算允许时再次尝试同一 intent | 04 | 04、11 | 不等于 semantic repair 或 Recovery；不承诺幂等、exactly-once 或最终成功 |
| Cancellation | requester 发出停止请求、listener 观察并协作停止的控制事实 | 06 | 11 | 不等于强制终止、工作已停、rollback 或 checkpoint 已保存 |
| Recovery | 中断后先还原已知 / 未知，再选择 Resume、Retry、Reconcile、Compensate、Ask 或 Stop 的控制过程 | 04 | 11 | 课程工作定义；不是“再跑一次”，也不自动证明副作用安全 |
| Memory | 系统在步骤或会话之间保留、恢复或检索信息 / 状态的机制统称 | 00 | 14—15 | 作用域、持久性和生命周期依实现而异 |
| Working Memory | 当前任务中可更新的短期工作状态 | 00 | 14 | 不等同完整会话日志 |
| Session | 一次可追踪、恢复或回放的交互与执行边界；可拥有、引用或治理 history | 00 | 12、15、34 | 课程工作定义；不等于 OpenAI Sessions 产品对象，也不是单次请求的 Context Snapshot / Receipt |
| Long-term Memory | 跨 Session 保留、检索和更新的信息 | 00 | 15 | 需要作用域、可信度和遗忘策略 |
| Project Memory | 绑定项目范围的事实、决策和经验记忆 | 00 | 15 | 不能自动当作当前事实 |
| Knowledge Base | 经过组织、可检索并带来源边界的知识集合 | 00 | 16 | 与未经治理的聊天历史不同 |
| RAG | 检索外部知识、把结果加入模型输入，再生成回答的技术模式 | 00 | 16 | Filter、Rerank、Cite 是后续工程环节，不是最低定义；检索到不代表正确 |
| Evidence | 支撑某个可审计主张的来源、观测、实验或明确设计提案 | 00 | 18 | 必须声明证明范围与不证明范围 |
| Trace | 跨 step、tool、provider 和状态变化的结构化执行记录 | 00 | 21 | 日志是载体之一，不自动等于完整 Trace |
| Replay | 基于已记录事件重建状态或重演执行的能力 | 00 | 21、34 | 重建与重新调用外部副作用需区分 |
| Eval | 用固定任务、判据和数据度量系统行为的评估过程 | 00 | 22 | 单次 Demo 不是回归评估 |
