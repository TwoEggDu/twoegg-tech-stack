---
title: "Harness 的设计取舍：可替换性、复杂度、Bloat 与演化"
slug: "agent-engineering-27-harness-design-tradeoffs"
date: "2026-08-30T00:00:00+08:00"
description: "从收益、成本、风险和退出条件判断什么时候值得建设 Harness，什么时候应该停在局部工作流，并用 BuildPilot V1 说明克制采用路径。"
draft: false
tags:
  - "Agent Engineering"
  - "AI Engineering"
  - "Harness Engineering"
  - "Reliability Engineering"
series: "Agent Engineering"
primary_series: "agent-engineering"
series_role: "article"
series_order: 280
weight: 3280
---

> **上一篇**：[Harness 的最小能力模型：Capability、Policy、Session、Trace 与 Recovery]({{< relref "ai-empowerment/agent-engineering-26-harness-minimum-capability-model.md" >}})

> **课程索引**：[Agent Engineering 系统课程]({{< relref "ai-empowerment/agent-engineering-series-index.md" >}})

# Harness 的设计取舍：可替换性、复杂度、Bloat 与演化

> **上一篇**：[Harness 的最小能力模型：Capability、Policy、Session、Trace 与 Recovery]({{< relref "ai-empowerment/agent-engineering-26-harness-minimum-capability-model.md" >}})

> **课程索引**：[Agent Engineering 系统课程]({{< relref "ai-empowerment/agent-engineering-series-index.md" >}})

如果这篇只记一句话，可以先记这一句：

> Harness 的问题从来不是能不能设计出来，而是它什么时候比局部 Prompt、Tool wrapper、Workflow、CI 和 Review 更值得拥有。

前面三篇把 Harness 这件事一步步推到了桌面上。Article 24 说清了为什么横切能力会散落：身份、权限、证据、预算、Trace、审批、恢复、知识和能力发现，一旦跨多个 Agent、Tool、Workflow 重复出现，就不再只是某个局部实现细节。Article 25 把 Runtime、Harness、Host 和业务 Agent 的责任切开，避免把执行推进和共享治理混成一个盒子。Article 26 又收缩到最小能力模型：Capability、Policy、Session、Trace、Recovery 这些能力为什么能形成一条责任闭环。

到这里，一个很自然的误解会冒出来：

> 既然这些能力都有道理，那是不是就应该开始做一个 Harness？

不一定。

这篇要回答的正是这个反问题：`可以设计` 不等于 `值得建设`。一套 Harness 可以在概念上很清楚，在工程上仍然不划算；可以在一个团队里解决漂移，在另一个团队里制造瓶颈；可以让权限、证据和恢复更可审计，也可能让审批疲劳、隐私风险、配置漂移和平台耦合变得更难收拾。

所以本文不把 Harness 写成“成熟团队必备平台”。更准确地说，Harness 是一笔治理投资，也是一笔治理债务。只有当重复的权限、证据、Trace、恢复和审查漂移，已经比新建共享控制面更昂贵时，它才值得从局部工作流里长出来。

先把证据上限说清楚。Article 27 是原则与取舍文章，Required Lab=`NONE`，Experiment Count=`0`，Runtime Observation=`ABSENT`。本文沿用 `11 / 11` Claims 与 `11 / 11` Evidence Cards，状态为 `1 CONFIRMED / 7 PARTIAL / 3 PROPOSAL / 0 BLOCKED`。其中 Stage 0-4、no-build 判断和 BuildPilot V1 建议都是课程 proposal，不是外部标准、运行结果、ROI 测量、延迟基准或缺陷下降证据。BuildPilot 在本文中仍然只是 `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`。

换句话说：本文讨论的是采用判断，不是发布平台架构；讨论的是权衡模型，不是证明某个系统已经运行；讨论的是怎样克制地建设，不是启动下一篇或实现 BuildPilot。

## 1. 先问值不值得，而不是先问能不能做

工程师很容易从“能不能做”进入系统设计。

能不能做一个能力注册表？能。能不能做统一权限策略？能。能不能把每次模型调用、工具调用、审批、Trace、Evidence 都记录下来？能。能不能再加一个插件层，让模型、工具、Host、Workflow 将来都能换？也能。

但这些问题都太早。

真正决定 Harness 是否应该出现的，不是“这些能力能不能被设计出来”，而是“哪些共享治理漂移已经贵到必须被集中承载”。如果没有这个压力，Harness 很容易从控制面变成成本面：多一组 schema，多一层配置，多一套审批，多一个排队 owner，多一片日志和隐私风险，最后还没有解决任何真实重复问题。

所以 Article 27 的起点不是模块清单，而是采用判断：

```text
coherent model
   |
   v
adoption judgment
   |
   +--> repeated governance drift? yes/no
   +--> risk and scale high enough? yes/no
   +--> owner capacity exists? yes/no
   +--> rollback path exists? yes/no
```

这里的每一问都很朴素。

第一，是否已经出现重复治理漂移？如果只有一个人、一个低风险任务、一个稳定脚本，把规则写在本地 checklist 里可能就够了。没有重复，就没有共享的必要。

第二，风险和规模是否足够高？如果只是生成一份临时文档摘要，错误成本有限，保留人工阅读就好。相反，如果同一套权限、证据、审批和恢复语义会影响多个仓库、多个工具、多个团队，漂移成本才开始接近 Harness 的建设成本。

第三，是否有人能长期拥有这层治理？共享层不是写完就结束。它要维护策略、版本、日志、隐私、迁移、回滚和异常处理。没有 owner 的 Harness，往往只是把风险从局部脚本搬到一个更难审计的中心位置。

第四，能不能回退？一个好的 Harness 不应该让团队一旦接入就不能下车。如果某个能力只有一个消费者、某个 adapter 长期没人用、某套审批规则造成队列堆积，就应该能降级、删除、回到本地流程，而不是用“平台已经建了”绑架后续决策。

这就是本文和 Article 26 的分工。Article 26 回答“如果真的需要一个最小 Harness，它至少要保护哪些不变量”。本文回答“这些不变量什么时候值得进入真实工程表面”。前者是模型，后者是投资判断。模型成立，只是建设的前置条件，不是建设的充分理由。

## 2. 收益、成本、风险要放在同一张账上

讨论 Harness 最常见的写偏方式，是只写收益。

比如：统一权限、统一证据、统一 Trace、统一 Review、统一 Capability Registry。每个词都对，但如果只把它们写成收益，文章就会变成平台广告。真实工程里，“统一”从来不是免费的。它同时带来更集中的 owner、更长的路径、更复杂的配置、更大的数据责任，以及一个更显眼的失败点。

更稳的方式，是把收益、成本和风险放在同一张账上：

| 维度 | 先问什么 | 典型例子 |
|---|---|---|
| 收益 | 它减少了什么漂移或不一致？ | permission language、evidence acceptance、trace identity、review state、recovery semantics |
| 成本 | 每次运行以后多背了什么账？ | token/context cost、storage/retention、latency、operator attention、migration work |
| 风险 | 中心层会制造什么新失败模式？ | bottleneck、single point of failure、policy drift、false safety、privacy exposure、approval fatigue |

统一治理的收益主要来自“同一个词终于有同一个合同”。`APPROVED` 不再在一个 workflow 里表示“用户点过一次同意”，在另一个 workflow 里表示“owner 对冻结范围承担责任”；`PASS` 不再一会儿代表 HTTP 200，一会儿代表证据被接受，一会儿代表 fixture regression 通过；`RETRY` 不再只是“再跑一次”，而是要检查 same intent、预算、权限、side-effect uncertainty 和恢复边界。

这些收益是真实的。架构资料也支持把公共关注点抽出来：安全、日志、路由、限流、审计、审批、状态和追踪都可能成为共享控制问题。

但同一类资料也提醒我们，共享层会引入自己的问题。聚合层可能成为单点故障或瓶颈；网关可能制造耦合、级联失败、额外延迟和配置风险；Telemetry 会带来敏感信息、保留周期、访问控制和脱敏责任；人工审批会引入等待状态和注意力成本；持久化恢复会要求清楚地区分已提交状态、悬挂动作、副作用不确定性和过期策略。

所以这篇所有相关结论都只能用对称措辞：

- 共享治理 `can reduce` duplicated drift；
- 中心层 `can introduce` bottleneck、coupling、latency、privacy 和 approval cost；
- 采用决策 `must be balanced` by scale、risk、ownership and rollback。

不能写成“用了 Harness 就更安全、更省钱、更快、更少 bug”。本文没有运行实验，没有价格测量，没有 reviewer throughput 数据，没有生产事故前后对比，也没有 BuildPilot runtime。成本和收益在这里是工程分类与采用判断，不是量化结果。

在开工前，团队至少应该先写下五件事：

```text
Before building a Harness, list:
1. repeated governance words that already disagree across workflows;
2. decisions that currently cannot be audited after the run;
3. failures that cannot be safely retried or resumed;
4. reviews that go stale but are still being reused;
5. costs the team can actually own after centralization.
```

如果这五项写不出来，先不要急着抽象平台。也许问题不是缺 Harness，而是缺一份清楚的本地 checklist、一条固定 CI gate、一个明确 owner，或者一份更诚实的 Evidence Contract。

## 3. 什么时候统一治理真的值得

统一治理值得出现的信号，通常不是“我们想让架构更漂亮”，而是局部实现已经开始互相打架。

例如，一个团队有三个 Agent workflow：一个看 Unity 编译错误，一个看 Jenkins 发布失败，一个看客户端启动性能。它们都需要读文件、读日志、引用历史结论、生成变更建议、等待 owner review。最初每条 workflow 都自己写一点规则，这很正常。但跑久以后，问题会变成这样：

- 编译诊断里的 `READ_ONLY` 只限制文件写入，发布诊断里的 `READ_ONLY` 还限制 Jenkins action，性能诊断里的 `READ_ONLY` 却可以读取更敏感的设备日志；
- 一个 workflow 把工具输出当 evidence，另一个 workflow 要求 evidence card，第三个 workflow 只要 reviewer 觉得合理；
- 一个系统里 `APPROVED` 会随着 diff 变化而 stale，另一个系统里旧批准可以被复用到新 scope；
- 一个 workflow 可以从 checkpoint 继续，另一个 workflow 只能重跑，第三个 workflow 没有记录上次是否已经触发外部副作用；
- 工具 schema 出现在模型上下文里以后，有的链路认为可以调用，有的链路还会做 permission gate。

这时统一治理开始有价值。不是因为“统一”本身高级，而是因为不统一已经让失败失去位置。团队不再知道该相信哪条规则，也不知道后续 review、recovery、eval 和 knowledge intake 应该继承哪个语义。

可以用下面这张表判断：

| Pressure | Local symptom | Harness helps only if | New cost |
|---|---|---|---|
| Permission drift | 同一动作在一个 workflow 被允许，在另一个 workflow 被拒绝 | shared authority gate 能被拥有和审计 | policy 维护、拒绝提示和例外处理 |
| Evidence drift | `PASS` 在不同系统里代表不同证据层 | claim / evidence / status 语言能共享 | artifact 存储和 reviewer 纪律 |
| Trace / recovery drift | 日志存在，但不知道从哪里安全恢复 | trace 与 recovery state 能关联 | retention、privacy 和 replay 限制 |
| Review drift | scope 变化后仍复用旧 approval | approval scope 与 stale rules 能执行 | approval fatigue 和排队成本 |
| Capability drift | tool visible 被当成 tool authorized | visibility / authority / execution / evidence 分开 | registry、version、trust 维护 |

注意第三列的 `only if`。Harness 不是把这些词搬进中心配置就完成了。它必须真的能让这些控制事实被拥有、审计、回滚和复验。如果只是多了一份 YAML，但没有 owner、没有 stale rule、没有 evidence acceptance、没有恢复语义，那只是把漂移换了个地方。

反过来，很多场景并不需要 Harness：

- 单个低风险助手，只回答一个人的本地问题；
- 一次性脚本，跑完就丢；
- 固定确定性 workflow，已有 CI / Review gate 可以更清楚地承担证据和授权；
- 工具和 Host 很稳定，没有第二消费者；
- 团队没有能力维护策略、Trace、隐私和迁移；
- 中心层会让所有小改动都排队等一个平台 owner。

这就是 Harness 采用判断里最重要的一点：重复不一定要抽象，漂移才值得抽象；共享不一定要平台化，能被拥有和回退的共享才值得平台化。

## 4. 可替换性不是提前做插件平台

可替换性是 Harness 讨论里另一个很容易过度设计的词。

一听到 Harness，很多人会自然想到插件系统、Provider adapter、Tool marketplace、Policy extension、Trace exporter。仿佛只要所有东西都能插拔，系统就未来可控。

但可替换性不是提前把所有东西做成插件。真正的可替换性来自清楚的合同和真实的变化压力。

```text
imagined future replacement
  -> not enough

real variation pressure
  -> possible reason to introduce a contract
```

什么叫真实变化压力？

- 已经有第二个模型 Provider，而不是“以后也许会换”；
- 已经有第二个 Host，例如 CLI 和 CI 都要消费同一套治理记录；
- 已经有第二条 workflow，需要同一套 evidence / permission / review 语义；
- 已经有第二个 evidence sink，例如本地报告和审计系统都要读同一 claim status；
- 已经有第二个 policy consumer，例如 Runtime dispatch 和 Review gate 都要使用同一 authority decision；
- 已经有第二个 capability implementation；
- 已经有明确迁移需求、owner 和时间窗口；
- 已经有独立版本生命周期，不能再靠同一个本地函数顺手处理。

如果这些压力都不存在，就不要急着搭插件平台。先定义窄合同，把实现留在本地。一个固定 read-only capability list，加上 source、version、trust 和 scope，往往比一个早熟的 capability marketplace 更可靠。一个单仓库模块化单体，也常常比多 adapter、多配置、多生命周期的“平台雏形”更适合早期团队。

可以用四个问题检查：

| 问题 | 如果答不上来 |
|---|---|
| 哪些记录在替换 Runtime 或框架后必须继续有效？ | 先保持实现本地，不要抽平台接口 |
| 今天是哪两个消费者需要同一份合同？ | 不要为想象中的第二消费者抽象 |
| 哪个迁移有明确 owner 和期限？ | 把替换性写成 proposal，而不是 requirement |
| 哪些字段稳定到足以让旧 Trace、Review 和 Evidence 继续可读？ | 不要扩大版本化表面 |

真正需要避免的 lock-in，也不只在模型 Provider。它可能藏在很多地方：

- Trace 格式只服务某个 SDK，换 Runtime 后旧审计记录读不懂；
- approval record 藏在 Host UI 事件里，换 Host 后审批语义丢失；
- knowledge schema 绑定某个检索实现，换 KB 后来源和 freshness 无法保留；
- capability descriptor 接受外部 annotation 当 authority，换 server 后信任边界混乱；
- evidence acceptance 写在 workflow 私有逻辑里，换 workflow 就重写审计。

所以可替换性的第一步不是“到处加插件 seam”，而是问：什么承诺不能丢？什么记录必须迁移？什么语义必须保持？只有这些答案出现以后，插件、adapter 和扩展点才有工程价值。

## 5. Bloat 的来源：功能越多不等于治理越强

Harness 的 bloat 很少是一夜之间出现的。它通常是很多“看起来合理”的小增强慢慢叠出来的。

第一次，团队说：既然要管 capability，就做一个开放注册表吧。第二次，团队说：既然要审计，就把所有 trace 都留着吧。第三次，团队说：既然要安全，就每一步都加审批吧。第四次，团队说：既然要可靠，就把 eval、regression、knowledge graph、replay、cost dashboard、admin console 都先占个位置吧。

每一步都能解释。合起来就开始变重。

| Bloat pattern | 为什么吸引人 | 危险在哪里 | 更稳的做法 |
|---|---|---|---|
| pluginize everything | 看起来可替换、可扩展 | 增加 lifecycle、version、config 和 owner 负担 | 等真实第二实现或迁移压力出现 |
| trace everything | 看起来可审计 | 产生 privacy、retention、redaction 和 replay 误解 | 最小化、脱敏，并绑定 claim / purpose |
| approve everything | 看起来更安全 | 形成 click-through fatigue，旧批准还可能 stale | 只在风险边界路由 owner 决策 |
| centralize every rule | 看起来一致 | 平台 owner 变成所有小变更的瓶颈 | 只集中共享不变量，本地规则留本地 |
| keep every memory | 看起来越用越聪明 | stale knowledge 冒充当前事实 | 要求 provenance、freshness 和 retirement |
| eval everything | 看起来质量可控 | 没有 dataset / oracle / manifest 时只是另一种仪式 | 先定义 verdict boundary，再接 runner |
| recover everything | 看起来长任务可靠 | 副作用不明时 replay/retry 会重复风险 | 先区分 known / unknown / in-flight / committed |

这里最微妙的是 approval fatigue。很多团队会把“多问一次人”当成安全增强。可真实系统里，人不是无限注意力资源。如果每个低风险 tool call 都弹窗，每个小文件读取都让 owner 确认，每个已知规则都需要人工点一次，那么 reviewer 很快会学会点通过。审批数量上去了，判断质量反而下降。

恢复复杂度也类似。Checkpoint 看起来是可靠性功能，但 checkpoint 只说明系统知道某个状态。它不自动说明外部副作用是否已经发生，也不说明旧权限是否仍有效，不说明上下文是否过期，不说明预算是否允许继续，不说明再次执行会不会重复动作。恢复系统如果没有这些边界，就会给人一种“可以继续”的错觉。

还有 observability。Trace 对诊断很有价值，但 Trace 也可能包含 prompt、工具输入输出、用户请求、日志片段、token、路径、账号信息或业务上下文。更多可观测性不等于更安全。没有 minimization、retention、access control 和 redaction policy，Trace 会从可靠性资产变成新的风险面。

因此，判断 Harness 是否 bloat，不看它功能多不多，而看每个功能是否回答了真实压力，是否有 owner，是否有退出条件，是否能在缺证据时说 `UNKNOWN`，是否能在过期时说 `STALE`，是否能在不该继续时停下来。

## 6. 虚假安全感比没有 Harness 更危险

没有 Harness 的风险通常很直观：规则散落、证据分散、审批靠人记、恢复靠猜。麻烦归麻烦，至少团队知道自己很多事还靠人工判断。

更危险的情况，是有了一个看起来很完整的 Harness，但它制造了虚假安全感。

| False safety | 看起来像什么 | 实际还需要什么 |
|---|---|---|
| `tool visible = authorized` | tool schema 已经出现在模型上下文里 | use-time authority、scope、sandbox 和 audit |
| `trace exists = replayable` | 有完整日志或 trace ref | input、environment、version、state、side-effect boundary |
| `redacted = safe` | 某个视图里敏感值被隐藏 | minimization、retention、access、review 和导出策略 |
| `approved once = approved forever` | 用户点过同意或 reviewer 留过评论 | actor、action、resource、scope、expiry、stale rules |
| `memory says so = true now` | 旧 run 或 KB 里有一条经验 | provenance、freshness、applicability 和 contradiction check |
| `eval passed = production safe` | fixture 或 regression 绿了 | release gates、monitoring、owner acceptance 和外部证据 |
| `requirement guessed = owner intent` | Agent 推断出一个目标 | intent confirmation、unknown label 和 owner decision |

这些错误有一个共同点：把某种治理信号当成更强的治理事实。

工具可见，只能说明当前上下文里有能力描述。它不说明当前用户、当前请求、当前资源、当前风险级别都获权。尤其是外部 server 给出的 annotation、title、hint、read-only 标记，如果没有信任边界，就只能当提示，不能当 authority。

Trace 存在，只能说明发生过一些事件。它不自动成为 evidence，也不自动支持 replay。要 replay，至少要知道输入、环境、工具版本、状态、随机性、副作用和目的。一个 redacted trace 也不能自动证明“可安全共享”，因为敏感性不只来自单个 token，还可能来自路径、上下文组合、业务行为和用户身份。

Approval 更不能永久化。一次批准必须绑定 actor、action、resource、scope、request digest、expiry 和 stale condition。需求变了、diff 变了、owner 变了、证据过期了、工具版本变了，都可能让旧 approval 失效。把“用户说过可以”当成长期授权，是很多 Agent 系统最容易滑过去的地方。

Knowledge 也是一样。历史记忆、事故卡、RAG 命中、前一次诊断结论，都不是当前事实。它们需要来源、时间、适用范围、冲突处理和退场条件。否则 Harness 只是把旧错误包装成新上下文。

因此，Article 27 要求 Stage 1-4 和 BuildPilot V1 都保留这些状态：

```text
UNKNOWN / STALE / NOT_PROVEN / NEEDS_REVIEW
```

这些词不是保守修辞，而是工程出口。`UNKNOWN` 让系统承认不知道；`STALE` 让旧证据和旧审批不能悄悄复用；`NOT_PROVEN` 防止 observation 冒充 conclusion；`NEEDS_REVIEW` 把决策交还给正确 owner。没有这些出口的 Harness，看起来更自动，实际上更危险。

## 7. No-build 不是失败，是设计判断

一套健康的采用模型，必须允许团队明确不建 Harness。

这句话很重要。否则所有讨论都会被一种隐含叙事牵着走：Stage 越高越成熟，平台越大越专业，停在本地流程只是暂时落后。这个叙事在工程上很坏。很多系统不是因为不够平台化失败，而是因为太早平台化，把局部问题变成了长期平台债。

下面这些 no-build 或 remain-low-stage 场景，都应该被认真记录：

- 单人、单入口、低风险 assistant；
- 一次性文档助手或 throwaway prototype；
- 固定工具链脚本，输入输出稳定，失败成本有限；
- 现有 CI / Review / 发布流程已经更清楚地承担 evidence 和 authority；
- 团队没有能力维护 policy、privacy、trace、migration 和 recovery；
- 领域判断本来就应该由人和既有流程承担，而不是交给 agent governance；
- 证据和权限可以被现有工具以更低耦合解决；
- 引入共享层会让普通本地修改排队等待平台 owner。

可以把选择写成这样：

| Situation | Better choice | Why |
|---|---|---|
| one-off low-risk task | Stage 0 | 平台开销超过复用收益 |
| repeated local task with weak evidence notes | Stage 1 | 先用纪律和结构化输出改善，不急着平台化 |
| two workflows share evidence and permission language | Stage 2 candidate | 局部漂移开始变成 review 成本 |
| multiple hosts/providers/evidence sinks | Stage 3 candidate | 出现真实 variation pressure |
| multi-team shared infrastructure | Stage 4 candidate | 共享治理 owner 可能值得存在 |
| no owner for privacy/policy/recovery | no-build or defer | 无人拥有的 Harness 只是治理表演 |

`Defer` 是设计决策，不是失败。停在 Stage 0、1 或 2，也可以是正确架构。

这也意味着，Harness 的采用记录里不应该只有“为什么做”。它还应该写“为什么现在不做”“哪些能力暂时不做”“什么条件出现才上移”“什么条件出现就回退”。一份没有 no-build 选项的架构决策，通常只是愿景，不是工程判断。

## 8. Stage 0-4：阶段顺序不是成熟命运

下面这套 Stage 0-4 是本文的课程采用模型，不是外部标准，也不是团队成熟度排名。阶段可以停留、回退、拆分或拒绝；向上移动必须来自观察到的压力，而不是架构野心。

同一个组织也可能同时处在多个阶段。文档摘要助手停在 Stage 0，内部诊断流程停在 Stage 1，共享 evidence contract 进入 Stage 2，高风险多团队平台才需要 Stage 4。高阶段增加的是证明和运维责任，不是地位。

| Stage | Entry signals | Build | Benefits | Costs and risks | Exit / rollback | Explicit ability not to build |
|---|---|---|---|---|---|---|
| Stage 0｜No Harness | 单用户、单低风险 workflow、没有外部副作用、没有共享审批、没有跨 run 证据承诺 | plain prompt、script、checklist 或既有流程 | 最快；没有新平台 owner；开销最低 | 靠人工纪律；复用弱；审计弱 | 只有反复出现 evidence / permission / trace 漂移时才进入 Stage 1 | one-off document helper、throwaway prototype 不需要 Harness |
| Stage 1｜Local disciplined workflow | 一个团队重复同类任务，需要只读证据或有限审批，但工具和 Host 稳定 | local conventions、structured output、read-only checks、evidence notes、simple approval checklist | 不引入平台成本也能减少歧义 | 仍依赖本地纪律；跨 workflow 容易漂移 | workflow 不再重复，或 review 成本超过收益时回到 Stage 0 | 不创建 registry、plugin system、session store |
| Stage 2｜Modular monolith Harness slice | 两个以上 workflow 共享 permission、evidence、trace、budget 或 review 语义 | 单仓库内共享 policy / evidence / session / trace contracts，固定核心和窄 extension points | 同一治理词开始有同一含义；review 和 recovery 更容易 | 中心 owner 可能成瓶颈；配置优先级 bug；迁移负担；本地耦合 | 如果一个团队成为所有变化的队列，就拆分、简化或删除未用 extension | 不把所有能力做成插件；不默认开放写自动化 |
| Stage 3｜Governed extension architecture | 多 Host、多 Provider、多 Capability 或多证据出口需要独立生命周期，且已有第二实现或迁移压力 | versioned capability registry、effective config dump、provider adapters、owner-routed review、bounded recovery、retention/redaction policy | 真实可替换性；更安全的迁移；更好的审计 | 状态更多；兼容性成本；policy drift；approval fatigue；延迟和存储成本 | 冻结新 adapter；退役无人使用版本；单消费者 extension 回收到本地 | 没有第二消费者或明确迁移时不扩展 |
| Stage 4｜Platform / ecosystem Harness | 多团队把 Harness 当共享基础设施，高风险 Agent 工作依赖它演化 | change-controlled policy/versioning、rollout/sunset process、observability/privacy governance、regression/eval hooks、audit/reporting、operational ownership | 支撑高风险、多团队 agent work 的共享基础设施 | 平台团队瓶颈；false safety；governance theater；高迁移与隐私负担 | sunset capability；限制 owner budget；把 domain logic 移出去；降回 Stage 2/3 | 不用 Stage 4 证明成熟；没有运维和治理人力就不要上 |

这张表里最重要的列，其实不是 `Build`，而是 `Exit / rollback` 和 `Explicit ability not to build`。因为每个阶段都会吸引团队继续加东西：Stage 1 想要一个小 registry，Stage 2 想要插件化，Stage 3 想要平台化，Stage 4 想要治理所有事情。没有退出条件，阶段模型就会变成单向膨胀路线图。

Stage 0 的价值，是承认很多事情就该简单。Stage 1 的价值，是让本地纪律先变稳。Stage 2 的价值，是在一个代码边界里统一共享治理语义。Stage 3 的价值，是只有在真实变化压力出现时才引入 extension architecture。Stage 4 的价值，是服务多团队基础设施，而不是给架构图贴金。

所以这套模型不是成熟命运，而是一组可逆选择。

## 9. BuildPilot V1：从 Stage 1/2 开始，而不是假装平台

现在把这个采用模型落到 BuildPilot。

先再次冻结边界：

```text
BuildPilot in Article 27:
  COURSE PROPOSAL
  DESIGN CASE
  NOT IMPLEMENTED
  NOT RUN
  READ-ONLY
  SUGGESTION-FIRST
```

这意味着本文不能写 BuildPilot 已经运行，不能写它已经扫描 Unity 或 Jenkins，不能写它创建 PR、修改代码、部署、降低成本、缩短延迟、减少缺陷或证明生产安全。它只是课程里的设计案例，用来说明一个专用工程 Agent 的 Harness 采用应该怎样克制。

如果 BuildPilot V1 要成立，它不应该从 Stage 3/4 起步。更合理的起点是 Stage 1/2：

- Stage 1：先把本地只读诊断流程做得有纪律，包括 scope、read-only、evidence notes、unknown label、owner review 和 re-verification plan。
- Stage 2：当编译诊断、发布诊断、启动性能调查等多个 workflow 都开始复用同一套 evidence / permission / trace / review 语义时，再把这些合同放进一个模块化单体 Harness slice。

V1 的取舍可以写成这样：

| Decision | V1 treatment | Why |
|---|---|---|
| `ADOPT` | restricted read checks、Evidence package、Trace reference、Change Request、Human Review、unknown/stale labels、re-verification plan | 这些能力让 suggestion-first 工作可审计，同时不偷走 owner 的决策和实施责任 |
| `SIMPLIFY` | budget 先做 step/time/tool-call caps 和 stop reasons；capability registry 先做固定 read-only list，带 source/version/trust notes | 避免过早做成本平台或开放能力市场 |
| `DEFER` | multi-project knowledge graph、semantic/multi-trial eval、governed capability evolution、full durable replay、autonomous code modification、PR creation、production deployment | 这些能力需要真实压力、运行证据和更强 authority |
| `REJECT` | BuildPilot 已经运行、扫描 Unity/Jenkins、修改代码、创建 PR、改善成本/延迟、降低缺陷或证明生产安全 | Article 27 范围内没有这些证据 |

BuildPilot V1 的最小建议链可以很朴素：

```text
Owner request
   |
   v
Read-only intake + scope
   |
   v
Restricted checks
   |
   v
Evidence-backed finding
   |
   v
Change Request proposal
   |
   v
Human Review
   |
   v
Owner implements outside BuildPilot
   |
   v
Read-only re-verification plan
```

这条链里，BuildPilot 不替 owner 改代码，不替 owner 发布，不把 suggestion 写成 fix completed。它做的是把需求、只读证据、风险、未知、建议和复验计划整理成可审稿材料。Owner 是否采纳、由谁实施、如何上线，仍然在 BuildPilot 外部。

这不是保守到不能前进，而是把第一版放在能证明的地方。先让 `READ-ONLY / SUGGESTION-FIRST / EVIDENCE-BACKED / HUMAN-REVIEWED` 这些词真的有一致语义，再讨论是否引入更复杂的 provider adapter、知识图谱、eval 平台、能力演化和写权限。否则 BuildPilot 会在还没有跑起来之前，就背上一整个平台的债。

## 10. 采用前可以做的实际检查

这篇不提供 ROI 表，也不提供成本数字，但它应该让读者马上能做三件事。

第一，做一次 governance-word audit。

找出团队里几个已经存在的 Agent、脚本、CI、Review 或诊断 workflow，把这些词逐个写下来：

```text
For each repeated workflow, write down:
- What does PASS mean?
- What does APPROVED mean?
- What does RETRY mean?
- What does EVIDENCE mean?
- What does TRACE prove and not prove?
- What makes old approval stale?
- Who owns privacy, retention and redaction?
- Who can reject a capability expansion?
```

如果这些词在不同 workflow 里含义不一样，先不要急着抽象系统。先把差异列出来：哪些差异是合理的 domain difference，哪些是已经影响 review、recovery、knowledge intake 的治理漂移。只有后者才是 Harness pressure。

第二，写一页 adoption decision record。

| Field | Answer format |
|---|---|
| Problem pressure | 具体重复漂移或风险，不写抽象愿景 |
| Current local owner | prompt / wrapper / workflow / CI / review |
| Proposed shared owner | 谁长期维护这层语义 |
| Expected benefit | 没有测量就只写定性收益 |
| New cost | token / storage / latency / reviewer / privacy / migration / operations |
| Failure mode | bottleneck、false safety、stale policy、lock-in、recovery confusion |
| No-build option | 明确 Stage 0/1/local alternative |
| Exit / rollback | 什么条件下删掉、降级或回到本地 |

这张表的价值不在格式，而在它逼团队同时写“为什么做”和“为什么不做”。如果一项能力找不到当前 pressure，也找不到 owner，也找不到 rollback，那它就不应该靠“以后会需要”进入 V1。

第三，先做最小有用切片。

最小切片通常不是插件系统，也不是完整平台。它可能只是：

- 一套共享 evidence status 语言；
- 一个只读 capability list，记录 source、version、trust、scope；
- 一个 approval record，绑定 actor、action、resource、request digest、expiry；
- 一个 trace reference，能把 finding、tool observation、review 和 re-verification 关联起来；
- 一条 stop rule，在 unknown、stale、not proven 或 needs review 时停止。

如果这些薄合同都还不能稳定执行，先不要加更大的能力。反过来，如果这些薄合同已经被多个 workflow 重复复制、互相漂移，再进入 Stage 2 才有理由。

## 11. 本篇能建立什么，不能证明什么

到这里，Article 27 可以建立的结论是：

- 共享治理可以在重复压力下减少权限、证据、Trace、Review、Recovery 和 Capability 语义漂移。
- 共享治理也会引入集中瓶颈、单点故障、耦合、延迟、隐私、审批疲劳、迁移和恢复复杂度。
- Context、Trace、Evidence、Policy 和 Approval 会带来 token/context、storage/retention、direct usage/cost reconciliation、user-visible latency 和 reviewer attention 等成本类别。
- 可替换性应该由真实 variation pressure 驱动，而不是由想象中的未来 provider 或插件热情驱动。
- Policy drift、misconfiguration、stale knowledge、wrong intent 和 trusted-looking hints 都可能制造 false safety。
- No-build、defer 和 remain-low-stage 是有效设计结果。
- Stage 0-4 是课程采用 proposal，不是外部成熟度标准。
- BuildPilot V1 应保持 read-only、suggestion-first，并从 Stage 1/2 的克制切片开始。

本文不能证明的是：

- 每个团队都需要 Harness。
- Stage 0-4 是行业标准或成熟度排名。
- 中心化一定提升安全、质量、速度、成本或缺陷率。
- BuildPilot 已经存在、运行、扫描 Unity/Jenkins、创建 PR、修改代码、部署或验证生产行为。
- Article 27 有任何 runtime observation、lab result、ROI metric、latency metric、cost metric 或 defect-reduction evidence。
- Article 28、Part VI、DeepSeek Harness source reading 或 BuildPilot implementation architecture 已经可以启动。

为了让证据边界继续可审查，本文的 Claim Traceability 如下：

| Claim | Status | 正文落点 | 边界说明 |
|---|---|---|---|
| `27-C01` shared governance benefit and central bottleneck risk | `PARTIAL` | 1、2、3、11 | 只能说 can reduce / can introduce，不说必然更安全或更省 |
| `27-C02` context / trace / evidence / policy / approval cost surfaces | `PARTIAL` | 2、3、5、11 | 只列成本类别，不给测量数字 |
| `27-C03` replaceability requires real variation pressure | `PARTIAL` | 4 | 不把可替换性写成默认插件平台 |
| `27-C04` drift / stale / confused signals create false safety | `PARTIAL` | 5、6、11 | 保留 `UNKNOWN / STALE / NOT_PROVEN / NEEDS_REVIEW` |
| `27-C05` HITL and recovery are stateful and costly | `PARTIAL` | 3、5、6 | 不声称测得 approval fatigue 或 exactly-once recovery |
| `27-C06` Stage 0-4 adoption model | `PROPOSAL` | 1、7、8、10、11 | 课程 proposal，不是外部标准 |
| `27-C07` no-build / remain-low-stage cases | `PROPOSAL` | 1、7、8、10、11 | defer / no-build 是设计判断，不是落后 |
| `27-C08` restrained BuildPilot V1 | `PROPOSAL` | 8、9 | design recommendation only，read-only / suggestion-first |
| `27-C09` Article reality and forbidden claims | `CONFIRMED` | 开场、8、9、11 | Required Lab NONE、experiment 0、runtime absent、BuildPilot not run |
| `27-C10` observability vs privacy / secrets | `PARTIAL` | 2、5、6、9、10、11 | Trace 是审计资产，也是风险面 |
| `27-C11` eval / regression / capability evolution need scoped pressure | `PARTIAL` | 2、5、6、9、10、11 | 单次 trace、eval pass 或 proposal 不证明 production quality |

Coverage=`11 / 11`。Evidence 状态保持 `1 CONFIRMED / 7 PARTIAL / 3 PROPOSAL / 0 BLOCKED`。Required Lab=`NONE`，Experiment Count=`0`，Runtime Observation=`ABSENT`。BuildPilot 保持 `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`。

## Learning Check

1. 为什么 Article 26 的最小能力模型成立以后，Article 27 仍然要问是否值得建？
2. 统一治理的收益和集中式瓶颈为什么必须一起讨论？
3. 哪些信号说明团队应该停在 Stage 0 或 Stage 1？
4. 为什么 Stage 0-4 不是成熟度排名？
5. 可替换性为什么需要真实 variation pressure，而不是提前做插件平台？
6. `trace exists` 为什么不能直接推出 `replayable` 或 `evidence accepted`？
7. Approval fatigue 怎样让更多人工确认反而降低安全性？
8. No-build 决策应该记录哪些理由？
9. BuildPilot V1 为什么应该保持 read-only / suggestion-first？
10. 哪些 BuildPilot 能力必须 `DEFER` 或 `REJECT`？
11. Article 27 为什么不能给出 ROI、延迟、成本或缺陷下降数字？

### 参考思路

1. 因为模型成立只说明责任边界可设计，不说明建设成本低于局部漂移成本。采用还要看重复压力、风险规模、owner capacity 和 rollback path。
2. 共享治理能减少 permission、evidence、trace、review、recovery 等语义漂移，但也会引入 bottleneck、coupling、privacy、approval fatigue、migration 和 recovery complexity。
3. 单用户、低风险、一次性、固定工具链、现有 CI/Review 已足够、没有共享 owner 或没有第二消费者时，应优先停在 Stage 0/1。
4. Stage 是可逆 operating choice，不是 prestige ladder。同一组织可在不同任务上使用不同 stage，高阶段意味着更多证明和运维责任。
5. 没有第二 provider、第二 host、第二 workflow、第二 evidence sink、第二 policy consumer 或明确迁移需求时，插件平台只会增加版本和配置负担。
6. Trace 记录发生历史；Replay 需要输入、环境、工具版本、状态和副作用边界；Evidence acceptance 还要看 claim、source、scope 和证明上限。
7. 审批太多会训练 reviewer 点击通过；真正有价值的是在风险边界把正确问题交给正确 owner，并保存 scope、expiry 和 stale condition。
8. 至少记录当前 pressure、local alternative、shared owner、cost/risk、no-build option、exit/rollback condition。
9. 因为 Article 27 范围内 BuildPilot 仍是 design case，没有 runtime、Unity/Jenkins scan、PR、修改、部署或生产验证；V1 应先让只读建议链可审计。
10. Multi-project knowledge graph、semantic/multi-trial eval、governed capability evolution、full durable replay、autonomous modification、PR creation 和 production deployment 都应先 defer；所有已运行或已产生收益的说法都应 reject。
11. 因为本文没有实验、计费读取、延迟测量、缺陷统计、reviewer throughput 数据或 BuildPilot 运行证据；只能保留定性工程判断和 evidence ceiling。

## 参考资料 / 证据边界

本文依据 Article 27 Research / Evidence source manifest：Microsoft Azure Architecture Center 的 gateway aggregation、microservices 与 AI gateway guidance；MCP `2025-06-18` Tools 与 Authorization；OpenAI Agents SDK running agents、HITL 与 tracing/config；OpenTelemetry sensitive-data 与 semantic convention 文档；GitHub rulesets 与 CODEOWNERS；LangChain / LangGraph HITL、persistence 与 product terminology；Temporal workflow execution；NIST AI RMF Core；以及本课程已发布的 Articles 18-22、24-26。

这些资料支持本文讨论 shared control、authorization、approval、tracing、sensitive telemetry、workflow recovery、review gates、terminology variance 和 measurement uncertainty 等机制；它们不证明本文 Stage 0-4 是外部标准，不证明所有团队都需要 Harness，也不证明 BuildPilot 已实现或产生任何运行收益。凡是采用阶段、no-build 判断和 BuildPilot V1 取舍，均保持课程 proposal 语态。

## 最短结论

Harness 的取舍，不是把 Agent 系统一步步升级成更大的平台，而是持续判断哪一部分治理漂移已经值得被共享承载，哪一部分仍应该留在本地流程、现有工具或人工判断里。

真正稳的 Harness，不是阶段最高，而是能在收益、成本、风险、退出条件和不建设选项之间保持诚实。它知道什么时候建，建到哪里，什么时候停，以及什么时候不建。

> **上一篇**：[Harness 的最小能力模型：Capability、Policy、Session、Trace 与 Recovery]({{< relref "ai-empowerment/agent-engineering-26-harness-minimum-capability-model.md" >}})

> **课程索引**：[Agent Engineering 系统课程]({{< relref "ai-empowerment/agent-engineering-series-index.md" >}})
