# Article 00 Definition Matrix

- Reviewed At：`2026-08-19（Asia/Shanghai）`
- Scope：Article 00 地图级定义；正式机制以对应后续文章为准

| Term | Article 00 Working Definition | Definition Type | Confidence | Formal Article |
|---|---|---|---|---:|
| LLM | 以 token 化输入为条件生成输出的语言模型能力。 | `STABLE_ABSTRACTION` | High | 01 |
| Model | 可通过 API 或本地接口调用、对输入产生输出的模型能力；不是完整应用。 | `STABLE_ABSTRACTION` | High | 01 |
| AI Application | 把模型调用与软件逻辑、数据、工具或界面组合成可用能力的应用；可以是单次调用，也可以包含 Agent。 | `STABLE_ABSTRACTION` | High | 01—04 |
| Product | 面向用户交付的软件 / 产品边界；可以提供一个或多个 Surface / Entry Point，不等同某个具体运行入口。 | `PRODUCT_TERM` | Medium-High | 00 |
| Surface / Entry Point | 产品对外可观察的使用入口，例如 CLI、IDE、Web、Desktop、CI Integration、Unity Editor Integration；不能据此确定内部 Host 或 Runtime。 | `PRODUCT_TERM` | High | 00 |
| Copilot | 由厂商定义的产品 / 产品族名称，可包含同步辅助和 Agentic 能力；不对应固定架构层。 | `PRODUCT_TERM` | High | 00 |
| Agent | 围绕用户目标，由模型参与决定推进方式，并通过行动 / 工具与反馈处理多步任务的软件系统。 | `STABLE_ABSTRACTION` | Medium-High | 08 |
| Agentic | 不同生态用于描述自主行为、Agent 模式或更广义 LLM 系统的词；本课程只把它当程度 / 特征描述。 | `ECOSYSTEM_DEPENDENT` | High | 00 |
| Agent Runtime | 本课程对 Agent 执行职责的称呼：模型调用、工具分派、循环推进、状态 / continuation 与停止。 | `COURSE_WORKING_DEFINITION` | Medium | 25 |
| Harness | 本课程对 Runtime 周围可复用工程控制与约束层的称呼；现有证据不足以支持统一行业定义。 | `COURSE_WORKING_DEFINITION` | Medium | 24—27 |
| Host | 本课程对承载或集成 Agent 执行 / Agent Runtime 的具体宿主程序、进程或运行环境的称呼；Surface 不等于 Host，二者映射需要独立实现证据。 | `COURSE_WORKING_DEFINITION` | Medium | 25、29 |
| Prompt | 给模型的任务、指令、示例与输出要求；只是 Context 的一部分。 | `STABLE_ABSTRACTION` | High | 02 |
| Context | 某一步推理时模型实际可见的 token / 信息集合。 | `STABLE_ABSTRACTION` | High | 12—13 |
| Tool | 暴露给模型选择或请求的外部数据 / 动作能力；实际执行由应用或 Runtime 负责。 | `STABLE_ABSTRACTION` | High | 05—06 |
| Skill | 可按需加载的领域说明、方法和配套资源；具体封装和激活方式随生态变化。 | `ECOSYSTEM_DEPENDENT` | Medium-High | 17 |
| Workflow | 通过较预定义的步骤、分支与决策点推进任务的骨架；Agent 可以嵌入其中。 | `ECOSYSTEM_DEPENDENT` | Medium | 10 |
| Memory | 系统在步骤或会话之间保留、恢复或检索信息 / 状态的机制统称；作用域和生命周期依实现而异。 | `ECOSYSTEM_DEPENDENT` | Medium | 14—15 |
| RAG | 检索外部知识、把结果加入模型输入，再生成回答的技术模式。 | `STABLE_ABSTRACTION` | High | 16 |

## Navigation Proposal

```text
User Goal
↓
Product / Application
↓ provides one or more
Surface / Entry Point：CLI / IDE / Web / Desktop / CI / Unity Editor
⋮ public-to-internal mapping requires implementation evidence
Host：承载或集成 Agent 执行的宿主程序、进程或运行环境
↓ course responsibility view
Harness
↓
Agent Runtime
↓
Model + Tool + State
↓
External World
```

这是一张课程导航图，用于区分可观察产品事实与课程工程职责。`Product / Application` 表示面向用户的软件边界，并不主张 Product 与 Application 在所有语境下严格同义；`Surface / Entry Point` 是外部可见入口；`Host` 是承载或集成 Agent 执行的课程工作定义。Surface 与 Host 没有预设的一一映射。整张图不是所有 Agent 产品的统一部署架构，也不能用于反推 Claude Code、Codex 或 DeepSeek Harness 的内部实现。
