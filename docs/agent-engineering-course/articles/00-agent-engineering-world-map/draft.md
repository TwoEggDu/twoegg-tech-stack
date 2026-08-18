# Agent Engineering 世界地图：从 Model、Agent 到 Harness / Host

Claude Code 的官方文档把它称为 Agentic Coding Tool，Codex CLI 可以检查和编辑仓库、运行命令，GitHub 有 Copilot，有些资料又会讨论 Agentic Workflow。DeepSeek 的系统，名字直接就是 DeepSeek Harness。

如果把这些词放在同一张白板上，很容易产生一种错觉：它们似乎都在描述同一种东西，只是能力强弱不同。于是我们开始追问：Copilot 是不是 Agent 的初级形态？Agentic 是不是比 Agent 更高级？装在终端里的工具是不是 Runtime？一个产品名字里有 Harness，是否就代表它采用了某种标准 Harness 架构？

这些问题之所以难答，不一定是因为术语本身有多深，而是因为我们把不同层次的词混在了一起：有的描述模型能力，有的是产品名称，有的描述系统行为，有的是软件执行职责，还有一些只是特定生态或这门课程采用的工程抽象。

这篇不会一次讲完所有概念。它只做一件事：先建立一张地图。以后再遇到一个新的 AI 产品或架构名词，我们至少知道应该先问三件事：

1. 这个词描述的是哪一层？
2. 它是可以从外部资料确认的事实，还是某个框架或课程采用的工作定义？
3. 当前证据允许我们判断到哪里，哪里开始只是猜测？

## 一、调用了 LLM 的软件，不一定是 Agent

假设你在 Unity 工具里加了一个按钮：`总结这份 Build Log`。

用户点击以后，程序读取日志，拼出一段 Prompt，调用模型，把模型返回的摘要显示在窗口里：

```text
User clicks "Summarize Build Log"
        ↓
Application reads log
        ↓
Application builds prompt
        ↓
Model generates response
        ↓
Application displays summary
```

这已经是一个 AI Application。它有界面，有读取日志的逻辑，有模型调用，也有结果处理。但它完全可以不是 Agent。

这里最先要拆开的，是 Model 和 Application。

Model 提供的是一种可调用能力：应用给它输入，它根据当前输入生成输出。[OpenAI 的文本生成文档](https://developers.openai.com/api/docs/guides/text)展示的直接模型请求，就是这个最小边界。

Application 则是承载这次调用的软件。读取哪份日志、怎样构造输入、结果显示在哪里、失败以后怎么处理，这些都不是“模型自己”天然拥有的产品职责。它们属于应用。

对传统工程师来说，可以先把它类比成：数据库引擎不是电商系统，Unity Runtime 也不是一款完整游戏。底层能力很关键，但“拥有某种能力”和“已经构成某类应用”不是同一个判断。

所以，看到一个程序使用了 LLM，我们只能先确认它是一个使用模型能力的应用。至于它是否值得被称为 Agent，还要继续看：模型有没有参与决定任务怎样向前推进，系统有没有围绕目标执行行动、接收反馈并处理多步工作。

这里先停在这个位置。行动如何变成循环，状态怎样保存，系统何时停止，会在后面的 Agent Loop 文章中正式展开。

## 二、Copilot、Agent、Agentic 不是三级能力阶梯

Model 和 Application 分开以后，另一个常见误区是把 Copilot、Agent、Agentic 排成一条能力升级路线：

```text
Copilot
   ↓
Agent
   ↓
Agentic
```

这张图看起来很顺，却把产品命名、工程系统和行为描述画成了同一种分类。

### Copilot 更接近产品语境

Copilot 往往是厂商定义的产品或产品族名称，而不是一个可以跨产品直接套用的固定架构层。

[Microsoft Copilot](https://www.microsoft.com/en-us/microsoft-copilot/for-individuals/)使用这个名字描述面向个人用户的数字伴侣；[GitHub Copilot 的功能文档](https://docs.github.com/en/copilot/get-started/features)则在同一产品族中同时列出同步辅助能力和能够自主推进工作的 Agentic 能力。

这至少说明：只看到“Copilot”这个名字，我们不能判断它内部是否存在 Agent，也不能把它自动放到 Agent 之前的某个成熟度等级。本课程因此只在产品语境中使用 Copilot，不让它承担架构分层职责。

### Agent 有一个可迁移的工程核心

不同生态对 Agent 的表述并不完全相同，但从 [OpenAI Agents 文档](https://developers.openai.com/api/docs/guides/agents)、[Anthropic 的工程文章](https://www.anthropic.com/engineering/building-effective-agents)以及 [Google Cloud 的说明](https://cloud.google.com/discover/what-are-ai-agents)中，可以保留一个适合本篇导航的共同核心：

> Agent 是围绕用户目标，由模型参与决定推进方式，并通过行动或工具与反馈处理多步任务的软件系统。

这比“一次模型调用”多出来的，不是一个更响亮的产品名字，而是任务推进方式发生了变化。

```text
一次调用

Input → Model → Output


Agent 的导航级特征

Goal
  ↓
Model participates in deciding what to do next
  ↓
Action / Tool
  ↓
Result / Feedback
  ↓
Continue toward the goal
```

这仍然不是 Agent Loop 的正式定义。它只帮助我们在地图入口处做判断：一个按钮调用一次模型，与一个围绕目标持续采取行动的系统，不应该因为都使用 LLM 就被画成同一层。

### Agentic 描述特征，不代表更高等级

Agentic 的边界更依赖使用它的生态。Anthropic 会用 agentic systems 同时讨论较预定义的 workflow 和由模型动态决定过程的 agent；GitHub 会用 Agentic 描述产品中的自主工作能力；Claude Code 的官方页面则把它称为 agentic coding tool。

因此，这门课程把 Agentic 当作描述自主行为或自主程度的词，不把它设成一种严格架构类型。它不是 `Agentic > Agent`，也不是从 Copilot 通往 Agent 的必经阶段。

到这里，我们已经得到第一组判断：产品叫什么、厂商使用什么形容词，和系统实际承担什么工程职责，是两件事。

## 三、从 Product 到 Runtime：先学职责，不猜文件夹

现在假设我们面对的是一款 Coding Agent 产品。它可能同时提供终端、IDE、Web，甚至 CI 集成。用户看到的是不同入口，但这不代表产品内部有三个彼此独立的 Agent Runtime。

为了让后续课程能稳定讨论这些问题，我们采用下面这张课程导航图：

```text
                         User Goal
                             ↓
                   Product / Application
                             ↓
                  uses or exposes one or more
                             ↓
        ┌──────────┬──────────┬──────────┬──────────┬──────────────┐
        │ CLI Host │ IDE Host │ Web Host │ CI Host  │ Unity Editor │
        └──────────┴──────────┴──────────┴──────────┴──────────────┘
                             ↓
                         Harness
                             ↓
                      Agent Runtime
                             ↓
                    Model + Tool + State
                             ↓
                       External World
```

> **Figure 1｜Agent Engineering 课程导航模型**
>
> **Course Navigation Model：它用于帮助区分职责与学习位置，不代表所有 Agent 产品都采用这一部署结构。**现实系统可能合并这些职责、拆成更多模块，或者使用完全不同的名称。

这张图从上往下分别回答不同问题。

**Product / Application** 是面向用户的软件边界。Product 更强调最终交付给用户的产品，Application 更强调承载模型与软件逻辑的应用；两者在很多场景会重叠，但这里不要求它们在所有语境中严格同义。

**Host** 是这门课程对具体运行或集成入口的称呼，例如 CLI、IDE、Web、Desktop、CI 或 Unity Editor。同一个 Product 可以暴露多个 Host。Host 负责把用户或集成环境带进系统，但一个公开入口只能证明“用户可以从这里使用它”，不能证明内部 Runtime 怎样分层。

**Agent Runtime** 是这门课程对 Agent 执行职责的称呼。我们会用它组织模型调用、工具分派、任务推进、状态延续与停止等问题。具体系统可能让应用、SDK、托管平台或其他模块承载这些职责，“Runtime”在不同框架里也可能指向不同边界。因此这里讨论的是职责，不是要求项目中一定存在一个叫 `AgentRuntime` 的程序集。

**Harness** 是这门课程为了后续工程学习采用的另一层抽象：它表示 Runtime 周围那些可复用的工程控制与约束。

这里必须克制一点。目前可以观察到多个官方生态在使用 harness 或 agent harness 这样的说法，但含义并不完全一致。[Anthropic 关于可信 Agent 的文章](https://www.anthropic.com/research/trustworthy-agents)用 harness 指模型运行时所处的 instructions 和 guardrails；[DeepSeek Harness 官方仓库](https://github.com/deepseek-ai/deepseek-harness)则把一套完整开源系统称为 agent harness。这样的有限样本不足以证明行业已经形成统一定义，也不足以证明所有 Harness 都彼此不同。

所以，本课程不会把 Harness 写成既成的行业标准，也不会断言每个 Agent 产品内部都有一个独立 Harness 模块。我们只明确自己的学习约定：后续用 Harness 讨论 Runtime 周围可复用的工程控制与约束；更完整的能力边界和设计取舍，留到 Harness Engineering 部分。

这张地图真正要留下的不是模块命名，而是职责判断：Product 是交付边界，Host 是具体入口，Runtime 关注执行，Harness 帮助我们组织执行周围的工程约束。看见一个目录名、产品名或 UI 入口，都不能代替对职责的验证。

## 四、七个横向术语，先知道它们在解决什么

有了纵向地图，Prompt、Context、Tool、Skill、Workflow、Memory、RAG 又该放在哪里？

它们并不是从 Model 到 Product 依次向上的七个固定层级，而是在系统不同位置反复出现的关注问题。Article 00 只要求先知道每个词大致在解决什么，以及后面去哪里正式学习。

| Term | 现在只需要知道 | 正式展开 |
|---|---|---:|
| Prompt | 给模型表达任务、指令、示例和输出要求，是 Context 的一部分 | 02 |
| Tool | 模型可以选择或请求的外部数据或动作能力；实际执行仍由应用或 Runtime 控制 | 05—07 |
| Workflow | 通过较预定义的步骤、分支和决策点推进任务的骨架，Agent 可以嵌入其中 | 10 |
| Context | 当前一步推理时模型实际能够看到的信息或 token 集合 | 12—13 |
| Memory | 系统在步骤或会话之间保留、恢复或检索信息与状态的机制统称 | 14—15 |
| RAG | 检索外部知识，把结果加入当前模型输入，再生成回答的技术模式 | 16 |
| Skill | 可按需加载的领域说明、方法和配套资源；具体封装方式随生态变化 | 17 |

这张表故意没有继续解释 Context Packing、Tool Policy、Memory Store、Vector Database、Skill Activation 或 Workflow State Machine。它们并不是不重要，而是各自需要单独的机制、证据和工程边界。导论如果把这些内容全部讲完，读者反而只会得到一组密集但无法使用的定义。

还需要注意：这些术语的确定性来源并不一样。

```text
相对稳定的最低工程抽象
────────────────────
Model / Application / Agent
Prompt / Context / Tool / RAG

产品或生态相关用法
────────────────────
Product / Copilot
Agentic / Skill / Workflow / Memory（具体边界）

本课程采用的工作定义
────────────────────
Host / Agent Runtime / Harness
```

> **Figure 2｜术语确定性与定义来源**
>
> 这不是成熟度排行，也不是正确与错误的分类。它表达的是：在 Article 00 阶段，我们对这些词能使用多强的确定性语言。稳定抽象仍有待后文展开；生态相关不等于错误；课程工作定义也不等于行业已经采纳。

面对新术语时，先判断它来自哪一种语境，比急着把它塞进一张“万能 Agent 架构图”更有用。

## 五、拿三个产品练习证据边界

现在用三张很短的产品卡练习这套方法。目的不是比较产品，而是区分“官方资料可以确认什么”和“我们不能因此推出什么”。以下事实均以 2026 年 8 月 18 日检索到的公开资料为边界。

### Claude Code

**公开可以确认：**[官方文档](https://code.claude.com/docs/en/overview)把 Claude Code 描述为 agentic coding tool，公开了读取代码库、编辑文件、运行命令以及 terminal、IDE、desktop、web 等使用入口。

**不能因此推出：**这些入口不能证明 Claude Code 内部怎样划分 Harness、Runtime、Memory 或 Workflow。

### Codex CLI

**公开可以确认：**[Codex CLI 官方文档](https://learn.chatgpt.com/docs/codex/cli)说明它可以在本地仓库中检查和编辑文件、运行命令，并支持交互以及脚本 / CI 场景。

**不能因此推出：**CLI 入口不等于 Agent Runtime，也不能证明不同 Codex 使用入口内部采用完全相同的结构。

### DeepSeek Harness

**公开可以确认：**[DeepSeek Harness 官方仓库](https://github.com/deepseek-ai/deepseek-harness)把它定位为开源 agent harness，当前处于 developer preview，并公开了 Web UI 运行入口。

**不能因此推出：**产品名称不能证明它内部等同于本课程的 Harness 定义；本篇也没有固定源码 commit、研究内部结构或验证实际运行稳定性。

这三张卡都指向同一个工程习惯：公开入口、可见能力和产品自我定位是事实；内部模块边界需要另一组证据。我们可以用产品帮助理解地图，但不能倒过来用地图填补产品没有公开的部分。

## 六、看完 Harness，为什么下一篇反而回到 Model API

到这里，我们已经看到了 Agent、Runtime、Harness、Context、Memory、Tool、RAG。继续向前最诱人的路线，可能是直接打开一套复杂 Agent 框架，沿着目录开始读源码。

但这张世界地图只是学习路线索引，不是学习终点。它告诉我们有哪些层次以及它们为什么不能混在一起，却还没有建立任何一层的可验证机制。

Runtime 怎样调用模型，Tool 请求怎样表达，Context 为什么会超限，Memory 保存的究竟是事实还是状态，Harness 怎样控制成本和失败——这些问题最终都依赖同一个最小可观察单元：应用向 Model 发起一次调用，并拿到一个结果。

因此，本课程选择从底向上生长：

```text
Model
  ↓
Output Contract / Adapter
  ↓
Tool / Agent Loop / Workflow
  ↓
Context / Memory / RAG / Skill
  ↓
Reliable Agent Engineering
  ↓
Harness Engineering
  ↓
Evidence-first Source Reading and Design
```

这不是所有团队构建 Agent 的唯一顺序，而是这门课程为了让每一层都可解释、可验证而采用的依赖顺序。

以后遇到一个陌生 AI 产品，可以把本文的三问重新拿出来：它在描述哪一层？这是外部事实还是工作定义？现有证据允许推断到哪里？只要这三个问题还能回答，产品名字和生态术语再多，也不至于重新混成一团。

下一篇将回到整条链路最小、也最容易被忽略的起点：**模型调用到底发生了什么——LLM、Model API、Messages 与 Token。**

## Learning Check

请先尝试用自己的语言回答，再看后面的参考思路。

1. 一个只调用一次 LLM 的摘要应用，为什么不自动是 Agent？
2. 产品名字叫 Copilot，为什么不能判断它比 Agent“低一级”？
3. 一个产品同时有 CLI、IDE 和 Web，为什么不能把这些入口当成 Runtime？
4. 为什么本课程可以使用 Harness 这个词，却不能把它写成行业统一标准？
5. 为什么 Article 00 讲完 Harness 后，Article 01 反而回到 Model API？

### 参考思路

1. 使用 Model 只能证明它是 AI Application；Agent 还涉及围绕目标、由模型参与决定推进方式，并通过行动与反馈处理多步任务。
2. Copilot 是产品语境中的名称，已经观察到的官方用法同时覆盖辅助和 Agentic 能力，名称本身不是架构等级。
3. CLI、IDE、Web 是具体 Host；它们证明使用入口，不证明内部执行职责怎样划分。
4. 已观察到的官方 harness 用法含义不同，有限样本不足以支持统一行业定义；课程只能明确声明自己的工作定义。
5. 地图提供位置，Model 调用才是后续 Tool、Runtime、Context、Memory 与 Harness 能力共同依赖的最小可观察基础。

如果这篇只留下一句话：**面对 Agent 世界的新名词，先拆概念层，再辨定义来源，最后把推断停在证据边界上。**
