---
title: "Session、Long-term Memory 与 Project Memory：事实、经验和作用域"
slug: "agent-engineering-15-session-long-term-project-memory"
date: "2026-08-23T00:00:00+08:00"
description: "把 Session、Long-term Memory 与 Project Memory 按作用域、生命周期和权威边界分层，并用写入晋升、召回资格与冲突处置约束跨 Session 复用。"
draft: false
tags:
  - "Agent Engineering"
  - "AI Engineering"
  - "Memory Engineering"
  - "Session Management"
series: "Agent Engineering"
primary_series: "agent-engineering"
series_role: "article"
series_order: 160
weight: 3160
---

> **上一篇**：[Working Memory 与 Investigation State：当前任务正在想什么]({{< relref "ai-empowerment/agent-engineering-14-working-memory-investigation-state.md" >}})

上一轮 Unity 构建调查结束时，Working Memory 留下一条谨慎记录：`X 可能与启动变慢有关`。它仍是 `HYPOTHESIS / ACTIVE`，还缺当前构建产物和测量结果。

系统随后先保存会话摘要，再在下一轮调查里召回旧记录。问题是，摘要把“可能”改成“已确认”，召回路径又没有核对 project、build、profile、观察时间和当前源码。新一轮 Agent 于是把旧假设当成当前事实，跳过测量，直接沿错误方向行动。

这不是某个框架的已知事故，而是本文使用的 **COURSE PROPOSAL / FAILURE PATTERN**。它包含两个不同故障：

- 写入侧：hypothesis 因“已摘要、已提取、已保存”被错误晋升为 durable fact；
- 召回侧：historical record 因“已检索到”被直接当作 Current Reality 注入 Context。

忘记信息会损失效率；错误地记住并复用信息，却会给错误结论增加历史权威。Memory 真正要解决的，不是怎样存得更多、更久，而是什么有资格留下，谁能批准晋升，未来在哪个作用域下可以召回，冲突和过期时又怎样处置。

如果这篇只记一句话，我建议记住：

> **COURSE PROPOSAL**：Memory 是历史贡献者，不是 Current Reality 的镜像；写入要审晋升，召回要审作用域、新鲜度与冲突。

> 证据范围：窄产品事实来自 2026-08-23 检索的 OpenAI Agents SDK / Responses / Conversations、Google ADK、LangGraph、Microsoft Semantic Kernel 与 W3C PROV 官方资料。OpenAI Sandbox Agent memory 是 Beta，Semantic Kernel Agent Memory 是 experimental，未锁 package 的 hosted docs 只代表检索日可见合同。本文的八对象分层、`MemoryWriteCandidate`、promotion policy、`Eligible`、conflict verdict 与 BuildPilot 场景都是课程提案；Required Lab = NONE，没有 Runtime、设备或生产结论。

## 为什么“被记住”仍然可能是错的

```text
persisted        != semantically accepted
stored for long  != cross-session authority
retrieved        != eligible current input
```

| 粗糙等式 | 实际最多证明 | 仍缺什么 |
|---|---|---|
| 保存成功 = 事实成立 | 某种 representation 已写入资源 | claim kind、Evidence、semantic acceptance、promotion authority |
| 保存很久 = Long-term Memory | 数据具有 persistence / retention | thread / session / project scope 与跨任务复用 authority |
| 搜索返回 = 当前可用 | query / namespace 返回 candidate | scope、freshness、provenance、trust、conflict 与用途检查 |

[LangGraph Persistence](https://docs.langchain.com/oss/python/langgraph/persistence)把 thread-scoped checkpointer 与 cross-thread Store 分开。因此，一份 thread checkpoint 即使在数据库中保留 90 天，仍可只是该 thread 的恢复状态；介质和时长没有改变逻辑作用域。

同样，ADK、LangGraph、OpenAI Sandbox memory 与 Semantic Kernel 展示了显式加入、提取、整合或查询 memory 的不同实现面。这些动作能生成并保存内容，却没有承诺内容因此成为语义真相。

所以，第一条设计原则不是选数据库，而是把 scope、authority 与 lifecycle 从 storage 中拆出来。

## 抽象模型：七个对象，加一条 Knowledge Base 边界

下面是全文中心模型。它是 **COURSE PROPOSAL / NOT INDUSTRY STANDARD**；逻辑责任可以共享文件、数据库或服务，不要求八套物理存储。

| Object | 它回答的问题 | Scope / authority | 它不负责什么 |
|---|---|---|---|
| Context Snapshot | 本 Step 被应用实际选入什么？ | step-scoped；application assembly | 完整 History、durable truth、恢复位置 |
| History | 按时间发生过什么？ | message / event / transition sequence | 自动裁决哪条 statement 当前有效 |
| Working Memory | 当前未完成任务按什么继续？ | task-scoped、versioned projection；Host acceptance | 完整 transcript、跨任务 truth、Workflow gate |
| Session | 哪些交互与执行属于同一连续边界？ | session boundary；映射依实现 | 单次 request、model call、固定 provider resource |
| Long-term Memory | 哪些信息跨 Session / thread 保留、检索和更新？ | cross-session / cross-thread policy | 因保存得久就自动成立 |
| Project Memory | 哪些 facts、decisions、experiences 绑定项目？ | project-scoped policy | 当前 repo、build、config 或 Runtime 的权威镜像 |
| Checkpoint | 中断后从哪一 committed boundary 恢复？ | Runtime / Workflow recovery authority | Memory 分类、完整 History 或当前 Context |
| Knowledge Base | 哪些带来源边界的组织化知识可被检索？ | source / collection lifecycle | 未治理聊天历史、自动 Current Reality |

同一句“build failed”在各对象中职责不同：History 记录它何时发生；Working Memory 保存当前任务是否接受该 observation 及其 ref；Project Memory 可保留旧 build 记录；Context Snapshot 只表示它是否被选入本 Step；Checkpoint 关心从哪个控制边界恢复。

```text
History ----------------------┐
Working Memory ---------------+
Long-term / Project Memory ---+--> Context Assembly --> Context Snapshot
Knowledge Base ---------------┘

Session     -> 治理一次交互与执行的连续边界
Checkpoint -> 恢复 committed control boundary
```

这不是数据库拓扑。Project Memory 与 Knowledge Base 可以共用 searchable store，也可以引用同一来源；共用设施不等于同义。本文只治理 project-scoped facts / decisions / experiences 的写入与召回边界。

前文边界保持不变：[Article 11]({{< relref "ai-empowerment/agent-engineering-11-long-running-agent.md" >}})定义 Checkpoint 与 Recovery；[Article 12]({{< relref "ai-empowerment/agent-engineering-12-context-engineering.md" >}})定义 Context Snapshot 与 Receipt；[Article 13]({{< relref "ai-empowerment/agent-engineering-13-context-debugging.md" >}})定位 application-visible Context 失真；[Article 14]({{< relref "ai-empowerment/agent-engineering-14-working-memory-investigation-state.md" >}})定义 task-scoped、versioned Working Memory。本文不重写这些 schema、taxonomy、mutation pipeline 或 Recovery 机制。

## 先固定课程 Session，再做产品映射

课程先固定责任，再比较 provider 名词：

> **Session 是一次可追踪、恢复或回放的交互与执行边界；可拥有、引用或治理 History。**

它不是单次 request、单个 model call，也不预设固定 provider resource。下面只映射 2026-08-23 检查到的产品责任，不建立行业统一 taxonomy。

| Product object | Checked responsibility | 不等同 |
|---|---|---|
| OpenAI Agents SDK `Session` | client-managed conversation history | Conversations resource、Project Memory |
| OpenAI `conversation_id` | server-managed named conversation | application-visible 全部 Context、跨项目 truth |
| OpenAI `previous_response_id` | lightweight server-managed continuation | durable Session taxonomy、跨任务 memory |
| OpenAI `Runner.run(...)` | logical turn，可含多个 Agent / LLM call | 单个固定 model request、整个 Session |
| Google ADK `Session` | user-agent conversation thread，含 Events、State 与 identity | cross-session MemoryService |
| LangGraph `thread_id` + checkpointer | thread-scoped state / checkpoints | cross-thread Store、统一 Session 标准 |

[OpenAI Agents SDK Running agents](https://openai.github.io/openai-agents-python/running_agents/)把 client-managed Session、server-managed conversation / previous response 和 logical run 分成不同策略；[Google ADK Session](https://adk.dev/sessions/session/)明确使用 conversation thread 的表述。结论不是“Session 不能叫 thread”，而是 checked products 没有稳定的一一映射。

设计评审应先固定连续性责任：追踪哪些交互，History 由谁治理，怎样恢复或回放；然后再映射 provider object。

## 写路径：MemoryWriteCandidate 与 Promotion Authority

Article 14 已让模型只能提出 typed hypothesis / mutation candidate，Host 才拥有 semantic acceptance。本篇只接住跨 Session 留存前的缝：当前任务中的记录，何时有资格成为未来任务可复用的信息？

下面整条写路径都是 **COURSE PROPOSAL / NOT FRAMEWORK CONTRACT**：

```text
Observation / user statement / Working Memory entry / Session event
  -> MemoryWriteCandidate
  -> validate scope + provenance + claim kind + freshness + policy
  -> PROMOTE | KEEP_CANDIDATE | REJECT | CONFLICT | INVALIDATE
  -> durable memory revision
```

`MemoryWriteCandidate` 只表达“可能值得未来复用”，不表达“已经为真”。Model、extractor、summarizer 或 reducer都可以产生 candidate / representation；写入成功不能替代 Host 的语义接受决定。

字段集合仍是课程候选。[W3C PROV-O](https://www.w3.org/TR/prov-o/)只为 provenance、derivation、revision、generation 与 invalidation等部分词汇提供支点，不定义完整 Memory schema 或 truth policy。

| 治理问题 | Candidate fields | 缺失规则 |
|---|---|---|
| 属于谁、哪里有效？ | tenant / user / organization / project / environment / version range | `UNKNOWN`，不得扩大 scope |
| 从哪里、何时观察？ | source_ref / provenance / observed_at / source_version | `UNKNOWN`，不得伪造时间或版本 |
| 是什么 claim？ | FACT / DECISION / EXPERIENCE / PREFERENCE / HYPOTHESIS / UNKNOWN | 保留原 kind，不靠措辞升级 |
| 证据边界？ | evidence_refs / confidence / trust | confidence 非校准概率；trust 不等于 truth |
| 怎样演化？ | expires_at / invalidated_at / update_of / conflict_set / deletion_state / forgetting_policy | lifecycle event分账 |
| 谁批准？ | promotion status / authority / rule | 无 authority 就保持 candidate |

最危险的 promotion bug candidate 是：

```text
HYPOTHESIS / ACTIVE
  -> summarizer writes “confirmed”
  -> extractor stores FACT
  -> later Session retrieves it
  -> no Host semantic acceptance occurred
```

故障不在 summarization 或 extraction 必然错误，而在 representation change 越过 claim-kind 与 authority seam。durable revision应保留原 kind、Evidence refs、scope 与 promotion decision；没有通过规则时，只能 `KEEP_CANDIDATE`、`REJECT`、`CONFLICT` 或 `INVALIDATE`，不能用“写进去了”补做批准。

## 召回路径：Stored、Retrieved、Eligible、Injected 分账

多个官方实现足以确认：存储、查询和向模型提供 Context 可以是分离动作。例如 LangGraph 分别执行 put、search 与 message assembly；ADK 分开 add 与 search；Semantic Kernel experimental provider分开 extraction、query 与 context provision；OpenAI Sandbox memory组合 summary injection 与按需 search。它们没有定义统一四阶段协议。

下面四段命名，尤其 `Eligible`，是 **COURSE PROPOSAL**：

```text
Stored Memory
  -> Retrieved Candidates
  -> Eligible Candidates
       scope / provenance / freshness / trust / conflict / purpose
  -> Injected Context Contributors
  -> Context Snapshot + Receipt
```

| Stage | 最小可观察事实 | 不自动证明 |
|---|---|---|
| Stored | object 存在于 store / file / provider resource | 本次会找到、内容正确 |
| Retrieved | query / namespace 返回 candidate | scope 正确、新鲜、可信 |
| Eligible（COURSE PROPOSAL） | Host policy 允许成为本 Step contributor | 必须注入、模型必使用 |
| Injected | application 放入 Context Snapshot | Provider 完整使用、输出正确 |

read trace至少应把 candidate ref / revision、retrieval scope、eligibility verdict / reason 与 Context Receipt ref分开。scope、freshness 或 conflict不明时，课程建议保留 `UNKNOWN`、`REJECT` 或 `HISTORICAL_ONLY` 及原因，而不是默认注入。

本文只回答召回后谁有资格进入当前 Step；检索、排序、引用质量与评估不在本文范围内。

## Scope、Freshness、Conflict：Project Memory 不是 Current Reality

Project Memory 更适合提供 locator、历史事实、决策理由和经验候选，让系统知道从哪里开始核验，而不是替当前环境下结论。

[OpenAI Sandbox Agent memory](https://openai.github.io/openai-agents-python/sandbox/memory/) 的当前 Beta 文档提醒 memory 可能 stale，应把它当 guidance并信任 current environment。映射到课程 Project Memory 后，窄结论是：旧记录不能覆盖当前 authoritative artifact；不能外推成“当前环境永远正确”或“自动更新已经解决冲突”。

下面的判断顺序和 verdict vocabulary 都是 **COURSE PROPOSAL**：

```text
same tenant / user / organization / project / environment?
  -> applicable version / profile / time horizon?
  -> provenance + current authoritative source available?
  -> conflict present?
  -> CURRENTLY_ELIGIBLE | HISTORICAL_ONLY | CONFLICT | INVALIDATE | UNKNOWN
```

当前源码、配置、build artifact、measurement 或 service response都可能成为复核材料，但也要保留自身来源与边界。Project Memory不能凭“更像经验”压过当前 artifact；当前 artifact也不能凭“更新”抹掉旧记录在旧 scope 下的历史价值。

### Scope mismatch 的证据上限

Google ADK 的 session / user / app / temp scope、OpenAI memory layout，以及 Semantic Kernel 的 Application / Agent / Thread / User scope都说明：scope key 或 layout会改变哪些内容可共享。

本研究没有执行越权或安全实验，因此 `15-C12` 保持 **PARTIAL**。最多只能说：若 Host 把错误 tenant、user、project 或 environment映射到可共享 namespace，out-of-scope内容可能成为 retrieval candidate，带来 privacy / contamination risk。它不证明已发生泄漏、ACL失败、攻击路径或合规影响；完整授权与安全执行不在本文展开。

### Conflict 不默认 newest-wins

下面是 **COURSE PROPOSAL**：冲突记录至少保留 competing values、scope、source、version、observed_at、`conflict_set` 与 resolution state，例如 `UNRESOLVED / SUPERSEDED / INVALIDATED / SCOPED_BOTH_VALID`。

新记录不自动胜出。不同 build的两条记录可能互相矛盾，也可能只是在各自 scope内有效。先判断 scope，再判断 revision和证据；无法裁决就保持 conflict，不用 last-write-wins制造一份平滑但错误的“当前记忆”。

## 生命周期：Update、Conflict、Delete、Forget 不是同一动作

| Event | 改变什么 | 不自动证明 |
|---|---|---|
| Update / revision | 新版本或替代关系；保留 update_of、source、version | 旧记录物理消失 |
| Conflict | competing records并存；保留 conflict_set / resolution | 新记录已经胜出 |
| Invalidate / expire | 某 scope / time下不再有效 | backend已删除 |
| Delete request / result | 特定 resource / item按产品合同执行删除 | backup、log、derived copy全删或合规满足 |
| Forgetting / retention / consolidation | policy不再保留或主动使用某内容 | 等同 resource deletion |

当前 OpenAI Conversations API 给出直接反例：删除 conversation不会自动删除其中items，item有独立 delete surface。SDK Session clear、ADK delete_session、OpenAI Sandbox memory consolidation与LangGraph checkpoint pruning / retention也只说明操作不同，不能合并成“删除 memory”。

课程 lifecycle candidate可以表达：

```text
CANDIDATE -> PROMOTED -> ACTIVE
    |            +-> SUPERSEDED / CONFLICT / INVALIDATED / EXPIRED
    |            +-> DELETE_REQUESTED -> DELETE_RESULT_RECORDED
    |            +-> FORGOTTEN_BY_POLICY
    +-> REJECTED
```

这不是产品状态机。它只阻止错误等号：expired不等于deleted，delete result不等于所有副本消失，forgetting不等于合规删除，冲突也不能靠静默删除其中一侧解决。

## 具体落地：BuildPilot / Unity build 4310 到 4472

SYNTHETIC ILLUSTRATIVE / NOT EXECUTED / NO RUNTIME CLAIM

所有 build ID、`X`、时间、瓶颈和记录都是教学构造；不存在真实 BuildPilot Runtime、build artifact、设备观测或生产结果。

```text
Session A / build 4310
  MemoryWriteCandidate:
  “WeChat Mini Game startup bottleneck may be associated with X”
  scope = project P / WX / build 4310 / profile WX / observed_at T1
  kind  = HYPOTHESIS or EXPERIENCE_CANDIDATE

Session B / build 4472
  Retrieved Candidate
  -> reject direct Current Reality use
  -> verify project + platform + build + profile + source revision
  -> acquire current artifact / measurement
  -> corroborate | update | conflict | invalidate | historical-only
```

1. **Write**：4310 hypothesis不能因 summary或write success变成 FACT；它先是 `MemoryWriteCandidate`。
2. **Recall**：build scope不同，旧记录先进入 `HISTORICAL_ONLY / NEEDS_CURRENT_VERIFICATION`；这两个 verdict是课程提案。
3. **Verify**：只核对当前 build identity、platform / profile、source revision，以及匹配问题的 artifact / measurement类型；不编造路径、bytes或指标。
4. **Conflict**：当前测量与旧记录不一致时保留两份 scoped record；若只是适用范围不同，可标 `SCOPED_BOTH_VALID`。
5. **Lifecycle**：旧记录可以 update、invalidate或保持 historical-only，不为收口直接 delete。

```text
4310 write path                           4472 recall path
HYPOTHESIS                                Retrieved
  -> Candidate                              -> scope/build mismatch
  -> promotion decision                     -> HISTORICAL_ONLY
  -> scoped memory revision --------------> -> current verification gate
                                             -> disposition -> action
```

**最多证明：**课程 policy能表达跨 build freshness、historical-only与conflict disposition。

**不证明：**build 4310 / 4472真实存在，`X`是瓶颈，性能改善或退化，修复或回归成立，BuildPilot Runtime / memory pipeline已经实现，或任何设备与生产结果存在。

## 坏实现、工程边界与最小治理

| Bad implementation | Failure | Minimum guard |
|---|---|---|
| transcript / summary直接变 durable fact | promotion bug | Candidate + Host decision |
| 用 duration分类 | task state获得错误跨任务authority | scope-first classification |
| Retrieved直接等于Injected | stale / wrong-scope / conflict污染 | explicit Eligible + Receipt |
| Project Memory覆盖当前repo / build | 历史冒充现实 | current authoritative verification |
| conflict默认newest-wins | scoped facts被抹掉 | conflict_set + resolution |
| 一个deleted布尔值 | resource / retention / derived-copy语义丢失 | lifecycle分账 |
| Memory吞掉Knowledge Base | 退化成检索系统教程 | 只保留interface boundary |
| scope risk扩成权限系统 | 证据与责任越界 | 只保留risk与policy seam |

最小可审查治理仍全部属于 **COURSE PROPOSAL**：

1. durable record至少表达 scope、source / provenance、observed_at / version、claim kind与lifecycle；
2. 写入先candidate，再由明确authority决定promotion；
3. 召回把Stored、Retrieved、Eligible、Injected分账；
4. Current Reality重新取证，conflict不静默覆盖；
5. deletion、invalidation与forgetting按具体资源合同分别记录。

字段可按风险裁剪，但未知显式写 `UNKNOWN`。logical roles可共用store，但不能取消scope / authority guard。高复用价值不等于高真实性；本文没有Runtime Lab，只能评价设计可审查性。

边界必须停住：

- Article 14：只承接typed hypothesis、Evidence refs与Host acceptance，不重写Investigation State schema、认知两轴或mutation pipeline；
- Knowledge Base：只说明可与Project Memory交叉或共用设施，不展开embedding、chunking、vector database、retriever、filter、rerank、cite或retrieval eval；
- 权限与安全：只保留scope mismatch的privacy / contamination risk，不设计permission、approval、sandbox、ACL、identity proof或enforcement；
- BuildPilot：只保留synthetic illustration，不宣称Runtime存在。

## Claim-to-section Traceability

| Claim | Ceiling | 正文落点 | Wording boundary |
|---|---|---|---|
| `15-C01` | CONFIRMED | Session产品映射 | checked products无一一映射，不穷尽行业 |
| `15-C02` | PROPOSAL | 八对象模型 | COURSE PROPOSAL；logical role可共享store |
| `15-C03` | CONFIRMED | OpenAI Session / continuation / run | current hosted docs；不用旧Assistants contract |
| `15-C04` | CONFIRMED | 三个危险等式、八对象模型 | durability不改变scope |
| `15-C05` | CONFIRMED | 四段召回路径 | official facts只证明动作可分；Eligible标proposal |
| `15-C06` | PROPOSAL | 写路径 | Host-owned promotion是课程提案 |
| `15-C07` | PROPOSAL | candidate envelope、lifecycle | W3C只支持部分vocabulary |
| `15-C08` | PROPOSAL | 开场、promotion bug | bad pattern，不指控产品漏洞 |
| `15-C09` | CONFIRMED | Project Memory不是Current Reality | OpenAI事实限Beta；Project Memory为课程映射 |
| `15-C10` | CONFIRMED | lifecycle分账 | deletion不等于all copies / compliance |
| `15-C11` | PROPOSAL | 4310 -> 4472 | synthetic / not executed / no runtime claim |
| `15-C12` | PARTIAL | scope mismatch | 只写risk，不写incident / permission结论 |
| `15-C13` | PROPOSAL | conflict policy、lifecycle | 不默认last-write-wins |
| `15-C14` | PROPOSAL | 八对象模型、工程边界 | 不展开完整Knowledge Base检索链 |

Coverage：**14 / 14**；`CONFIRMED=7`，`PARTIAL=1`，`PROPOSAL=6`，`BLOCKED=0`。没有新增核心Claim，也没有把PARTIAL或PROPOSAL升格。

## Learning Check

1. `HYPOTHESIS / ACTIVE` 被summarizer写成confirmed，store成功。现在是durable fact吗？下一步记录什么？
2. thread checkpoint在数据库保留90天，为什么不自动成为Long-term Memory？
3. Project Memory被search返回，project相同，但build不同、`observed_at`较旧。它处于哪一步，还缺什么？
4. `4310`与`4472`记录不一致，为什么不能newest-wins？
5. delete conversation成功后，能否写“所有Memory已删除且满足合规”？
6. SDK Session、`conversation_id`、`previous_response_id`与`Runner.run`能否互换？
7. 错误project namespace让另一项目记录成为candidate，本文最多能下什么结论？
8. Project Memory与Knowledge Base共用searchable store，是否证明同义？

### 参考思路

1. 不能，仍是candidate；Host按scope、refs、kind与rule决定 `PROMOTE / KEEP_CANDIDATE / REJECT / CONFLICT / INVALIDATE`（COURSE PROPOSAL）。
2. 分类看thread / cross-thread scope与reuse authority，不看duration或存储介质。
3. 只证明已Retrieved，尚未自动Eligible；还需核对build / profile / source revision，并取得current artifact或measurement。
4. 不同scope的记录可以各自有效；应保留scope、source、version、observed_at、conflict set与resolution。`SYNTHETIC ILLUSTRATIVE / NOT EXECUTED / NO RUNTIME CLAIM`。
5. 不能。conversation与item的删除语义可分，backup、log、derived memory与合规结果都未证明。
6. 不能。先固定课程Session的连续性责任，再映射history、named resource、lightweight continuation与logical turn。
7. 只能确认sharing受scope / layout影响并保留privacy / contamination risk；不能声称发生incident、ACL failure、attack或compliance breach。
8. 不能。Project Memory按project-scoped facts / decisions / experiences治理，Knowledge Base按organized knowledge与source boundary治理；物理设施可以重叠。

## 最短结论

> **COURSE PROPOSAL**：记忆不是当前真相的副本；写入先审晋升，召回再审作用域、新鲜度与冲突。

## 参考资料

- [OpenAI Agents SDK：Running agents](https://openai.github.io/openai-agents-python/running_agents/)
- [OpenAI Agents SDK：Session protocol](https://openai.github.io/openai-agents-python/ref/memory/session/)
- [OpenAI Responses API：Create response](https://developers.openai.com/api/reference/cli/resources/responses/methods/create)
- [OpenAI Conversations API：Create conversation](https://developers.openai.com/api/reference/python/resources/conversations/methods/create)
- [OpenAI Conversations API：Delete conversation](https://developers.openai.com/api/reference/typescript/resources/conversations/methods/delete)
- [OpenAI Conversations API：Delete item](https://developers.openai.com/api/reference/ruby/resources/conversations/subresources/items/methods/delete)
- [OpenAI Agents SDK：Sandbox Agent memory](https://openai.github.io/openai-agents-python/sandbox/memory/)（Beta；2026-08-23检索）
- [Google ADK：Session](https://adk.dev/sessions/session/)
- [Google ADK：State](https://adk.dev/sessions/state/)
- [Google ADK：Memory](https://adk.dev/sessions/memory/)
- [LangGraph：Memory overview](https://docs.langchain.com/oss/python/concepts/memory)
- [LangGraph：Persistence](https://docs.langchain.com/oss/python/langgraph/persistence)
- [LangGraph：Add memory](https://docs.langchain.com/oss/python/langgraph/add-memory)
- [Microsoft Semantic Kernel：Using memory with Agents](https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/agent-memory)（experimental；last updated 2025-06-09）
- [W3C Recommendation：PROV-O](https://www.w3.org/TR/prov-o/)
