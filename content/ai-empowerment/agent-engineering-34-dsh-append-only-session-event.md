---
title: "Append-only Session Event：Replay、Resume、Fork 与 Projection"
slug: "agent-engineering-34-dsh-append-only-session-event"
date: "2026-08-30T00:00:00+08:00"
description: "从 append-only SessionEvent 的写读链与五组可复现实验出发，区分 Durable/Live Event、四类 Projection，以及 Replay、Resume、Fork 与 Compaction 的工程边界。"
draft: false
tags:
  - "Agent Engineering"
  - "DeepSeek Harness"
  - "Session Event"
  - "Replay"
series: "Agent Engineering"
primary_series: "agent-engineering"
series_role: "article"
series_order: 350
weight: 3350
---

# Append-only Session Event：Replay、Resume、Fork 与 Projection

> **上一篇**：[Inbox、Turn、Step 与 Agent Loop]({{< relref "ai-empowerment/agent-engineering-33-dsh-inbox-turn-step-agent-loop.md" >}})

> **课程索引**：[Agent Engineering 系统课程]({{< relref "ai-empowerment/agent-engineering-series-index.md" >}})

“恢复一段 Agent 会话”到底意味着什么？

它可能是读取已有记录，重新生成 Model History；也可能是在原 Session 上继续追加；还可能是在某个已完成边界切出新 Session，让 parent 与 child 走向不同后缀。

这三个动作看起来都像“把聊天打开”，工程语义却完全不同：

```text
Replay  = 从已接受的 event prefix 重建 projection
Resume  = 在同一 Session identity 上追加 suffix
Fork    = 从选定 prefix 创建新的 child identity 与 lineage
```

如果只保存聊天文本，这些差异无从表达。我们不知道 Tool 是否已经执行，不知道 policy decision 属于哪一轮，不知道 compaction 隐藏了哪些旧消息，也不知道当前页面展示的是完整过程还是模型下一次请求真正会看到的 History。

所以，本篇的核心不是“怎样恢复聊天”，而是：

> 先把 Session 写成可排序的事实流，再把 Model History、UI Transcript、Domain State 与 raw Trace 视为四种不同 Projection；Replay、Resume 与 Fork 才能拥有可验证的边界。

本文所有 DSH 源码事实都绑定官方仓库的 [`dsh-v0.1.2-alpha.1`](https://github.com/deepseek-ai/deepseek-harness/tree/dsh-v0.1.2-alpha.1)，完整 commit 为 [`cd5ef8148158c3a752a658978873241fdf8e2bbc`](https://github.com/deepseek-ai/deepseek-harness/tree/cd5ef8148158c3a752a658978873241fdf8e2bbc)。实验使用 clean external fixture，结束后 `HEAD` 仍是该 SHA，working tree 与 diff 均为空。

本轮证据账为 `15 / 15 Claims`、`15 / 15 Evidence Cards`：`9 CONFIRMED / 5 PARTIAL / 1 PROPOSAL / 0 BLOCKED`。五组 required experiment 全部通过，selected owner tests 为 `12 passed / 122 skipped / 0 failed`。

但这里的运行证据来自 repo-owned deterministic fixture。UI Transcript 与 SessionQuery Trace 没有独立 runtime snapshot；真实 Provider、network、permission service、billing 与外部副作用没有执行。后文会把这些限制保留为 `PARTIAL`，不会用源码存在或测试通过替代运行事实。

## 1. 一份聊天记录为什么不够

最直觉的会话存储，是按时间保存 user 与 assistant 的文本：

```text
user: 查一下构建状态
assistant: 正在查询
assistant: 构建成功
```

它适合展示，却不足以恢复一次 Agent 执行。

“正在查询”和“构建成功”之间，可能发生过 model request、Tool call、Tool result、retry、approval decision 与 context compaction。只存最终文本，就会丢失这些能够解释“为何走到现在”的事实。

更棘手的是，同一份记录会被不同消费者读取：

- 模型只需要当前有效的 History；
- UI 希望保留用户看得懂的过程；
- Domain projector 要折叠 goal、todo 或 schedule 状态；
- Trace 要保留原始事件、correlation 与 cursor。

它们都读 Session，却不应该得到同一种结果。

如果强行把四者压成一份 messages，常见后果是：

1. Compaction 后 UI 的历史被误删；
2. Replay 为了补齐状态再次调用 Tool；
3. Fork 被误解成复制外部系统；
4. Live notification 被误当成已经持久化；
5. permission 或 budget 因为“在旧记录里出现过”而被静默继承。

这些不是序列化格式问题，而是事实与视图没有分层。

## 2. 先建立 Event Stream 与 Projection 模型

一个更稳的最小模型只有两部分：append-only event stream 与 pure projection。

```text
Producer
  -> append EventEnvelope
  -> Durable Event Stream
       -> History Projection
       -> Transcript Projection
       -> Domain Projection
       -> Trace Projection
```

事件回答“发生了什么”。

Projection 回答“某个消费者现在应该怎样理解这些事实”。

可以把 Projection 写成一个函数：

```text
View = Project(StreamPrefix, TransformVersion)
```

这里有三个关键约束。

第一，输入必须是明确的 prefix。没有 cursor 或 boundary，就无法判断两次重建是否基于同一批事实。

第二，Projection 不能悄悄制造新事实。它可以过滤、折叠、替换表面节点，但不应该为了“补齐结果”重新调用模型或 Tool。

第三，不同 Projection 不需要相等。它们只需要各自对同一 prefix 可解释、可追溯。

这也是本篇最重要的反等同关系：

```text
Durable Event != Live Event
Transcript    != Model History
Replay        != Resume
Resume        != Fork
Event Prefix  != External World Snapshot
```

## 3. DSH 的 SessionEvent 到底记录什么

在 pinned DSH 中，核心 envelope 是：

```ts
type SessionEvent<T> = {
  type: T
  seq: number
  time: number
  data: EventData<T>
}
```

对应源码可见 [`packages/core/session/src/types.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/core/session/src/types.ts)。

`seq` 是 Session 内的单调序号，不是全局序号。Turn、Step、tool call 等 correlation 位于各自 payload；core envelope 没有 universal `runId`。

这点很重要。某些 plugin 确实存在 `run-start` / `run-end` 事件，但不能把局部 vocabulary 推广成整个 SessionEvent 的统一 Run contract。

Pinned build 的 known-event catalog 会 fail closed。它大致分为这些 family：

| Family | Representative types | 主要用途 |
|---|---|---|
| Lifecycle | `turn/start`、`turn/end`、`step/start`、`step/end` | 标记执行区间与 payload correlation |
| Model surface | `user/message`、`assistant/message`、`tool/result` | 形成当前模型消息表面 |
| Request / raw trace | `request/header`、`request/context`、`assistant/chunk`、`tool/call` | 保留请求、流与调用事实 |
| Compaction | `compaction/start`、`summary`、`prune`、`end` | 记录压缩事务与 provenance |
| Policy / control | `approval/*`、`permission/preset`、`sandbox/mode` | 记录当时的配置或决定 |
| Domain / workflow | `goal/change`、`todo/write`、`team/*`、`tool-workflow/*` | 供插件投影领域状态 |

完整 catalog 在 [`known-event-types.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/core/session/src/known-event-types.ts)。

这里的“完整”只对 pinned build 成立。下游可以延迟注册新类型，因此它不是未来版本永久不变的枚举。

## 4. Durable Event 与 Live Event 是两条证据通道

`Session.append` 的核心顺序不是“先写数据库，再通知观察者”。

Pinned call path 是：

```text
producer
  -> Session.append(type, data, surfaceIntent)
  -> seq = log.length
  -> validate + freeze
  -> in-memory log.push(event)                 [live acceptance]
  -> publish session/event
  -> PersistenceCoordinator / write-behind
  -> backend.append(contiguous batch)          [durable completion]
  -> session/flush                             [durability barrier]
```

实现入口见 [`packages/core/session/src/index.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/core/session/src/index.ts)，持久化 owner 见 [`packages/session/session-persistence/src/coordinator.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/session/session-persistence/src/coordinator.ts)。

因此，收到 `session/event` 只证明事件已经被 live Session 接受，不证明 backend 已完成持久化。

Observer 失败也不会把已经接受的事件从 log 回滚。Durability 的可靠证据应是成功的 flush barrier，或之后从 durable backend 完整读回相同 contiguous prefix。

这给工程实现一个直接约束：

```text
LiveReceipt(event seq)        = 可用于刷新 UI
DurabilityReceipt(cursor)     = 才可用于恢复承诺
```

把二者合并成一个 `saved=true`，会让崩溃窗口变得不可见。

## 5. Read path 先选 source，再生成 view

读 Session 也不是一个 `getMessages()` 就结束。

DSH 的路径至少包含两层：

1. 选择 exact live source 或 prepared durable source；
2. 从 contiguous events 生成目标 Projection。

`SessionQuery` 可以返回 header、raw events、cursor 与可选 projections；`SessionHistoryController` 负责 page/follow，并把 durable page 与后续 live frames 接成无 gap 的序列。

相关实现见 [`packages/session/session-query/src/index.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/session/session-query/src/index.ts) 与 [`packages/api/session-controller/src/history.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/api/session-controller/src/history.ts)。

“读到了事件”与“获得了某种视图”必须分账。前者验证 stream identity/order，后者验证 transform semantics。

## 6. 同一条 stream，四种不相等的 Projection

### 6.1 Model History：下一次模型真正看到什么

Model History 读取 current surface nodes。

`user/message`、非空 `assistant/message` 与 `tool/result` 可以变成模型消息；boundary、chunk 和 log-only events 不进入 History。发生 replacement 时，旧 surface node 会被 shadow。

核心实现见 [`surface.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/core/session/src/surface.ts)。

### 6.2 UI Transcript：人要看到怎样的过程

UI/history transport 面向可分页、可 follow 的事件历史。Append-origin message 仍可以作为 transcript material，即使它已经不在当前 Model History 中。

这意味着 Transcript 适合解释“发生过什么”，History 适合回答“下一次 request 看见什么”。

### 6.3 Domain State：把事件折叠成领域状态

Goal、Todo、Schedule 等状态不必反复扫描为文本。Projection registry 可以把每个 committed event 送入 registered pure fold，并保留 state、version 与 observed watermark。

实现见 [`packages/session/session-projection/src/index.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/session/session-projection/src/index.ts)。Projection cache 是加速层，不是事实 authority。

### 6.4 raw Trace：保留诊断所需的原始账本

Trace 关心 exact header、contiguous raw events、cursor 与 correlation。它不应该因为 Model History 做过 compaction 就丢掉旧事件。

四种 Projection 可以整理成下面这张表：

| Projection | 输入 | 主要 transform | 输出关注点 |
|---|---|---|---|
| Model History | current surface prefix | filter + shadow/replace | 下一次模型请求 |
| UI Transcript | durable/live history frames | page + pack + follow | 人类可读过程 |
| Domain State | every committed event | registered fold | goal/todo 等状态 |
| raw Trace | exact contiguous prefix | validate + cursor | 审计与诊断 |

本轮 X01 对 History、Domain 与 representative raw/live observations 有运行覆盖，但 UI Transcript 与 SessionQuery Trace 没有独立 runtime snapshot。因此“四投影源码路径已闭合”是 `CONFIRMED` 的 source fact，而“四投影都经过独立 runtime 对照”并未成立，整体保持 `PARTIAL`。

## 7. Transcript 不等于 Model History

这个边界在 Compaction 后最清楚。

假设原始 stream 中有十轮对话与 Tool result。Compaction 可以追加一条 summary，并用 replacement message 改变 current surface。下一次模型只看到摘要加近期消息，但 raw stream 中的旧事件仍在。

于是：

```text
Transcript: 原始消息 + compaction 过程 + 后续消息
History:    replacement summary + 后续 current surface
Trace:      所有 raw events 与 correlation
```

如果要求 Transcript 必须等于 History，UI 要么丢掉历史，要么把本应被压缩的上下文重新塞回模型。

正确的不变量不是“两个数组相等”，而是：它们都能说明输入 prefix、transform 与输出用途。

## 8. Replay：重建记录的意义，不重新演算世界

在本篇语境中，Replay 指 reconstruction replay：

```text
accepted event prefix
  -> Session.create(id, seed)
  -> validate known types + contiguous seq
  -> fold current surface
  -> deriveMessages()
```

X02 从保存的 prefix 创建 fresh Session，重建出与原先相等的 Model History。这个过程没有调用 Provider，也没有重新执行 Tool。

Owner test anchor 位于 [`packages/core/agent-loop/tests/loop.spec.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/core/agent-loop/tests/loop.spec.ts)。

Replay 成功只证明：对这个 accepted prefix，确定性的 Projection 得到了预期结果。

它不证明：

- 再次请求同一个模型会采样出相同 token；
- 网络响应仍然相同；
- 历史 Tool 再执行一次仍得到相同结果；
- 外部系统仍处于事件记录时的状态。

因此，Replay 的验收项应该是 History equality、correlation completeness 与 zero re-execution receipt，而不是“模型又说了一遍同样的话”。

## 9. Resume：同一 Session 上继续追加

Resume 与 Replay 都要读取 durable prefix，但它们的目标不同。

Pinned path 是：

```text
AgentLoop.resume(sessionId)
  -> SessionPersistence.prepare(id)
  -> balanced durable load
  -> SessionStore.prepare(id, full seed/header)
  -> setupAndPublish(source="resume")
  -> append future events to same Session identity
```

实现见 [`packages/core/agent-loop/src/index.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/core/agent-loop/src/index.ts) 与 [`packages/session/session-persistence/src/index.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/session/session-persistence/src/index.ts)。

X03 的 before/after 结果是：

```text
Session id: unchanged
Old prefix: unchanged
New events: contiguous suffix
Turns: [1] -> [1, 2]
History messages: 3 -> 5
```

Resume 不依赖旧进程里的 live subscriber。Fresh runtime 会建立自己的 live channel；恢复依据仍是 durable prefix。

一句话概括：Replay 只读并重建，Resume 重建以后在同一 identity 上继续写。

## 10. Fork：从完成边界建立新的 lineage

Fork 的核心不是复制一个 messages 数组，而是选择合法 boundary，冻结 prefix，再创建新 identity。

```text
source Session + completed boundary
  -> validate prefix
  -> detach/freeze seed
  -> child header { new id, parentSession, seedLength }
  -> independent child suffix
```

Core fork 见 [`SessionStore.fork`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/core/session/src/index.ts)，Host fork 见 [`SessionCommands.fork`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/api/session-controller/src/commands.ts)。

X04 验证了三个重要事实：

1. child 可以从 earlier completed boundary 重建 History；
2. child append 不修改 parent，parent append 也不进入 child；
3. cold Host fork 可以读取 persisted source 并创建 child，而不先 resume source Agent。

历史 Tool record 在重建与 Fork 中不会自动重跑。这个行为是安全边界的一部分，而不是缺少功能。

## 11. Replay、Resume、Fork 的差异矩阵

| 维度 | Replay | Resume | Fork |
|---|---|---|---|
| Session identity | 读取/重建指定记录 | 保留原 id | 创建 child id |
| Event prefix | 输入 | 保留完整 stored prefix | 复制选定 completed prefix |
| Future suffix | 通常无 | 追加到原 Session | 追加到 child |
| seq | 验证既有序列 | 从原末尾连续 | child 内从 seed 末尾继续 |
| Model History | 从 prefix 重建 | 重建后参与后续 request | 从 cut 重建，随后分化 |
| Provider/Tool | 不应重跑 | 新 work 可调用 | 新 child work 可调用 |
| Live subscriber | 不需要旧 subscriber | fresh runtime channel | child channel |
| External world | 不重放 | 不从记录恢复 | 不复制 |
| Permission/budget | 不由 Replay 推断 | 仅按明确 owner contract | 仅按明确 owner contract |

这张表比一个笼统的 `restoreSession()` 更有价值，因为每一格都能形成独立验收条件。

## 12. Fork 不复制 external world

Event stream 能复制事实描述，不能复制事实发生时的整个世界。

一次 Tool call 可能已经：

- 写入文件；
- 提交远端事务；
- 发送消息；
- 消耗配额；
- 获得一次性 authorization。

Fork 复制对应 event prefix，不会自动克隆进程、文件系统、credentials、remote transaction 或当前权限。

X04 使用 in-memory sentinel 验证“复制 event 不会重新执行历史 Tool，也不会把 sentinel 当成 child state 克隆”。这只是 event-copy boundary 的证据，不是生产副作用幂等、事务回滚或远端快照的证明。

因此，child 若要再次操作 external world，必须重新读取现实状态，并经过当前权限与 policy gate。

历史里的 `approval/decided` 或 `permission/preset` 是“当时发生过什么”的事实，不是今天仍有效的 authorization token。

## 13. Permission 与 Budget 哪些真的会继承

Pinned source 中，`delegationDepth` 有明确 durable contract，因此 resume 后 recursion budget-like state 可继续。

但本轮没有找到统一的 credential、generic cost budget 或 turn budget fork-transfer contract，也没有运行 permission service 或 billing system。

所以正确表述是：

```text
delegationDepth: explicit persisted contract
generic permission inheritance: absent or unproved
credential inheritance: absent or unproved
cost / turn budget inheritance: absent or unproved
```

“未证明”不能被默认成继承，也不能被默认成重置。真正的安全实现应让每项 authority state 拥有显式 owner 和 receipt。

## 14. Compaction 是 append facts，再 replace surface

Compaction 最容易被写成“删除旧消息”。Pinned DSH 的实际边界更精确。

它先向 raw log 追加：

```text
compaction/start
compaction/summary
compaction/prune
compaction/end
```

这些是 log-only transaction facts。与此同时，一条新的 replacement `user/message` 使用 `surfaceOp: replace` 与 `sourceEventSeqs`，改变 current Model History surface。

实现可见 [`packages/compaction/src/types.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/compaction/src/types.ts) 与前述 `surface.ts`。

因此它同时具有两种观察结果：

```text
Raw stream: append-only，旧事件仍在
Model History: current surface 被 replacement 改写
```

X05 保存了 before/after raw stream，验证旧事实仍在 storage，compaction transaction 与 replacement checkpoint 作为新事件追加。

只观察 token 数下降，无法判断系统究竟删除了旧事实、增加了摘要，还是仅改变了 History projection。必须同时看 raw stream 与 current surface。

## 15. Provenance 不等于 verified / unverified

`sourceEventSeqs` 能回答 replacement 来自哪些 event，它是一条 provenance link。

但 pinned `SessionEvent` contract 没有 generic `verified` 字段。它无法统一表达：

- 某条日志是否经过设备复现；
- 某个远端结果是否由独立渠道确认；
- 某个推断是否仍未验证；
- 某份摘要是否完整保留原证据强度。

所以，Compaction 保留 provenance link，不等于保留了 Evidence verification semantics。

更稳的设计，是让领域 Receipt 显式带上：

```text
source identities
transform version
verification state
limitations
output digest
```

摘要只能说“由这些记录转换而来”，不能自动说“与原证据等价”。

## 16. 怎样从 stream 重建 History，再安全 Fork

下面是一条不依赖具体 API 名的最小流程。

### 第一步：冻结 durable prefix

记录 Session id、end cursor、event count 与 digest。不要从正在变化的 live list 随手复制。

### 第二步：验证 event contract

检查：

- `seq` 是否从零连续；
- type 是否在当前 build 的 known catalog；
- payload correlation 是否完整；
- cut 是否落在 completed boundary；
- 是否存在 duplicate identity 或 payload conflict。

### 第三步：用 fresh projector 重建 History

```text
historyA = ProjectHistory(prefix)
providerCalls = 0
toolCalls = 0
```

如果重建需要重新请求 Provider 或 Tool，它就不再是 projection replay。

### 第四步：创建 child lineage

```text
child = CreateSession(
  id = NewId(),
  parentSession = source.id,
  seedLength = prefix.length,
  seed = DeepDetached(prefix)
)
```

### 第五步：分别追加 suffix

给 parent 与 child 投递不同 followup，然后断言：

```text
parent.prefix == frozenPrefix
child.prefix  == frozenPrefix
parent.suffix != child.suffix
parent append does not mutate child
child append does not mutate parent
```

### 第六步：单独处理 external authority

不要从 prefix 推断当前 credential、approval、budget 或 remote state。为它们分别生成 observed、reset、revalidated、absent 或 unknown receipt。

这六步把“Fork 成功”拆成了 History reconstruction、lineage、isolation 与 authority boundary 四类证据。

## 17. 五组实验实际证明了什么

本轮 selected tests 共执行 6 个 file executions，结果为 `12 passed / 122 skipped / 0 failed`。

| Trace | 通过项 | 仍未证明 |
|---|---|---|
| X01 | durable/live representative order、History、Domain、tool correlation | UI/Trace 独立 runtime snapshot |
| X02 | accepted prefix -> equal History，zero historical re-execution | 相同模型采样输出 |
| X03 | same id、prefix unchanged、seq/Turn/History continuation | generic permission/cost inheritance |
| X04 | detached Fork、parent unchanged、cold Host fork | 真实 external world clone/rollback |
| X05 | raw events retained、compaction facts append、surface replace | generic verified semantics |

这张表同时记录 positive result 与 evidence ceiling。`12 passed` 不是对真实 Provider、网络、远端副作用和所有插件的整体认证。

## 18. 一个更稳的工程骨架

落到自己的 Runtime 时，应先定义 typed receipt，而不是先做一个含糊的“恢复会话”按钮：

```text
DurabilityReceipt = sessionId + durableCursor + prefixDigest
ProjectionSnapshot = inputCursor + transformVersion + outputDigest
ForkReceipt = parentId + childId + seedLength + seedDigest + authorityState

Replay(prefix) -> ProjectionSnapshot
Resume(sessionId, durableCursor) -> AppendCapability
Fork(prefix, boundary) -> ForkReceipt + ChildSession
```

Live notification 只服务低延迟体验；DurabilityReceipt 才负责恢复承诺。ProjectionSnapshot 保存 transform version，避免把不同算法生成的 view 当成同一结果。

## 19. BuildPilot 候选接口：只到 Proposal

对 BuildPilot，可以考虑由 `IContextContributor` 产出带 provenance 的 `Receipt`，再由 projection/assembly 层选择哪些 Receipt 进入 History、UI 或 Trace。

例如：

```ts
interface IContextContributor {
  collect(input: ContextInput): Promise<Receipt>
}
```

这个方向有两个潜在价值：一是来源、转换与验证状态能同行；二是 Compaction 不必把 summary 伪装成原始证据。

但它目前只是 `PROPOSAL`。本篇没有 BuildPilot ADR、schema、migration、implementation 或 runtime 证据，也不声称 DSH 已经提供这个接口。是否采用、谁拥有 Receipt、怎样与 Event Stream 对接，必须留到 Part VII 的设计评审。

Article 35 与 Part VII 都尚未启动。

## 20. Evidence Boundary

最后把证据层级重新钉死：

- `CONFIRMED`：pinned event envelope/catalog、append/persistence/query paths、Replay/Resume/Fork/Compaction owner path，以及 selected fixture 已观察行为；
- `PARTIAL`：UI Transcript 与 Trace 的独立 runtime snapshot、真实 external-world behavior、generic permission/cost inheritance、verified/unverified preservation；
- `PROPOSAL`：BuildPilot `IContextContributor + Receipt`；
- `ABSENT_IN_PINNED_SOURCE`：只代表对这个 frozen build 的 bounded search，没有发现 generic contract，不代表所有未来版本永远不存在。

源码能证明 owner 与 call path，deterministic test 能证明选定 fixture 行为；二者都不能替真实 Provider、权限系统、billing 或远端副作用的运行证据。

## 21. 最短结论

Article 35 会继续进入完整 Tool Pipeline；本篇不提前展开它，也不启动 Article 38 或 Part VII。

如果只保留一句话：

> 先保存可排序的事实，再生成带 provenance 的视图；Replay、Resume 与 Fork 只有在 identity、prefix、suffix 和 external-world boundary 都明确时，才是可验证的工程能力。
