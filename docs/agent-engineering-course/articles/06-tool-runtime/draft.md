# Tool Runtime：Validate、Policy、Execute、Result 与 Trace

> 本文资料与 API contract 核对时间：2026-08-20。官方合同来自 OpenAI Function Calling 与 .NET 10 文档；运行结果只来自固定的本地 Lab 02。Provider、network、credential 调用均为 `0`。本文采用的 Pipeline、Policy v1、Result Contract v1、Idempotency v1 与 JSONL Trace Schema v1 都是 **COURSE PROPOSAL / NOT INDUSTRY STANDARD**。

上一篇已经建立一个边界：对 client-executed tools，模型返回 Tool Call，只是在表达结构化行动意图；是否 route、reject 或 execute，仍由 Host 决定。

但如果 Host 的实现只是下面这样，这个“决定权”其实没有真正落地：

```text
收到 name + JSON arguments
  -> 按 name 找同名函数
  -> Deserialize
  -> Invoke
  -> 把 ToString() 交给模型
```

它看起来像普通 RPC 或反射调用，却省掉了几个关键问题：参数可能结构合法，但路径已经越界；同一个 invocation 可能因 retry 再执行一次副作用；caller 取消与 timeout 被压成同一个异常；handler 返回了错误 shape，系统仍把它当成功结果；大结果同时进入模型、UI 与日志；最后只剩一条“调用失败”，却不知道失败前哪些步骤已经发生。

这也是 Tool 不能只当普通函数包装的原因。模型是概率性调用者，Host 面对的不是一组可信的函数实参，而是一份需要逐层接受或拒绝的行动候选。Tool Runtime 的价值，不是让调用语法更漂亮，而是把“为什么能继续、为什么必须停止、是否执行过、结果能给谁看”变成可判定的工程合同。

普通函数调用通常共享一组隐含前提：调用者来自同一程序，参数已经由业务流程准备，权限与生命周期在更外层解决，调用失败时开发者还能沿调用栈定位。Tool Call 并不天然拥有这些前提。它可能来自不稳定的模型输出，也可能在上层没有保存完整状态时被重新送达。于是，过去藏在调用者约定里的条件必须被搬到显式 Runtime 中，否则“调用成功”只能说明某个函数返回了，不能说明输入安全、执行获准、结果有效或审计闭合。

这里也不需要把模型描绘成恶意参与者。即使模型完全按照 schema 生成参数，schema 本身也不知道当前文件拓扑、资源策略、调用预算和历史 invocation。风险来自责任信息不在同一层，而不是来自某一次输出“看起来不正常”。因此，Runtime 的设计目标不是猜测模型意图，而是让任何候选都经过相同、可复核的边界。

全文先保留四条边界：

```text
ToolDefinition != function
Schema Valid != Policy Allowed
Result != Evidence
Sandbox != Permission
```

## ToolDefinition 不是函数：模型视图与 Host Registry 是两个合同面

模型需要看见能力，才能产生调用候选。对一个只读文件 Tool，它可能看见 name、description，以及只含 `relative_path` 的 input schema。模型不需要、也不应该看见本地 handler 实例、allow-root 绝对路径、默认 timeout、最大读取字节数、spill 目录或测试 fault seam。

Host 则恰好相反。它不能只知道“模型选了 `read_text`”，还要知道该名字是否注册、真实实现是谁、资源约束是什么、结果应满足哪种 contract，以及这次调用能否进入 execution。因此，更稳的责任模型是：

```text
Model-visible ToolDefinition
  name + description + input schema
          ↓ call candidate
Host Registry
  registered name -> executable handler
  + timeout + result limit + resource policy metadata
          ↓ only after local gates
Host implementation
```

[OpenAI 当前 Function Calling 文档](https://developers.openai.com/api/docs/guides/function-calling)把“向模型提供 definitions”“收到 call”“由 application 执行代码”“回注 output”列为不同步骤。这个合同支持 definition / call 与 application-side implementation 分离，却没有规定本课程 Registry 必须使用什么类、依赖注入容器或 metadata 字段。Registry 是 Host 承载这条 seam 的一种课程设计，不是 Provider wire schema。

这个结论还必须限定在 **client-executed tools**。OpenAI built-in tools，以及其他生态的 server-executed tools，都说明 execution owner 可以位于 Provider infrastructure，而不在本地 application process。准确说法不是“所有 Tool 都由 Host 执行”，而是：只要本地 Host 拥有 client-tool 的执行责任，就必须为 model-visible definition 与 executable implementation 指定清楚的边界和 owner。

`ToolDefinition != function` 也不是说二者永远没有关联。Registry 最终当然要把 registered name 关联到可执行实现；它强调的是：给模型看的能力合同，不应直接等同于拥有本地副作用的函数对象。这样，未知 name 才能在 execute 前 fail closed，Host-only 风险元数据也不会被模型合同吞掉。

这个分层还解决了变更管理问题。模型合同关心“能力怎样被描述、参数怎样被表达”；Host Registry关心“当前部署实际注册了什么、由谁执行、受什么限制”。两者可以分别演进，但必须在调用时重新关联。只修改模型可见description，不应悄悄改变本地资源边界；替换handler实现，也不应自动扩大模型可见能力。评审时若找不到这两个变更面的明确owner，所谓“Tool封装”往往已经把产品描述、运行配置与副作用代码揉成一个难以验证的对象。

Registry lookup本身也应是一个可失败的步骤。模型曾经见过某个definition，不代表当前Host仍注册该能力；名字存在，也不代表本次请求使用的是预期资源配置。本文不提出版本协商方案，只保留最小判断：在进入参数处理和副作用之前，Host必须先确认自己能为这个名字承担执行责任。

## 抽象模型：从 Call 到 Trace，每个 Stage 都要能停止

如果所有失败都落入 `try / catch`，调用方只能知道“某处抛错”，不知道第一次失败在哪一层，也不知道后续动作是否误跑。课程为此采用下面的 Tool Runtime v1：

> **证据状态：COURSE PROPOSAL / NOT INDUSTRY STANDARD。** Stage 的命名、拆分、顺序、Policy merge、spill 与 Trace 字段都是课程规范性设计。Lab 02 只能验证固定 fixture 是否符合这份设计，不能把它升级成行业统一架构。

```text
Call
  -> Registry Lookup
  -> Canonicalize Arguments
  -> Schema / DTO / Domain Validate
  -> Merge Tool / Resource Policy
  -> Check Invocation Idempotency
  -> Execute with caller token + timeout budget
  -> Validate Result
  -> Render Inline / Spill
  -> Append Trace
```

这条链的核心不是箭头数量，而是 first-failure contract。每个 stage 都必须是 `PASS / FAIL / NOT_RUN` 之一。Registry lookup 失败，后面的 canonicalize、validate、policy 与 execute 都不能假装运行过；Policy 返回 `DENY` 或 `ASK`，handler 必须保持未进入；handler 返回的 result 不满足 contract，render 与 cache 都不能继续。无论成功或失败，每个 invocation 最后都追加一条 terminal Trace，保留它走到了哪里、为何停止、哪些动作没有发生。

`NOT_RUN`不是为了把日志填满，而是为了阻止事后推理越界。缺少字段时，读者无法区分“这一层没有记录”“这一层默认通过”和“前面已经失败所以根本没有执行”。显式写出`NOT_RUN`，才能回答副作用是否有机会发生，也才能让测试检查early terminal之后没有偷偷继续。这一点对成功路径同样重要：只有result validation与render都明确通过，才能说Runtime产生了可消费视图；handler执行一次并不是整条调用成功。

可以把每次invocation看成一张单向推进的状态卡。每一层只能读取前一层已接受的输出，并追加自己的decision；失败时立即形成terminal，不回头改写早先事实。课程Trace采用每次invocation一条terminal记录，正是为了让这张状态卡能够被复核。现实系统也可以记录更细的event，但无论采用何种载体，都不应让后一个“成功”标签覆盖前一个已经发生的失败。

这条Pipeline还刻意把输入验证与结果验证分开。输入通过，只表示handler获得了一份可执行候选；handler返回，只表示产生了结果候选。二者分别站在副作用之前和之后，承担不同风险。如果用同一个“validation”模糊带过，团队很容易在监控中看到一次execute成功，就误判整个Tool成功。

这里的 Trace 仍不是 Evidence。它只是结构化运行记录。只有把冻结实验、原始观测、环境范围和反例边界连起来，完成 `Experiment -> Observation -> Evidence Interpretation -> Claim Status`，Trace 才能支持某个可审计主张。把日志文件命名为 trace，不会自动完成这条解释链。

现实系统可以把几个 stage 合并在一个组件中，也可以把 Registry、Policy 或 execution 放到不同进程。责任模型不要求一层一个服务；它要求的是：每个判断有 owner，失败有 terminal，副作用发生与否可判定。只要这三点丢失，代码即使分成十个 class，也仍然只是普通函数包装。

## Execute 前：路径 Canonicalize 不等于字符串前缀检查

`relative_path` 通过 Schema，只说明它是一个符合结构要求的字符串。它没有说明目标资源位于允许目录，也没有说明路径中途经过的 link / junction 最终指向哪里。

.NET 10 提供了构建这道边界所需的公开 surface：[Path.GetFullPath](https://learn.microsoft.com/en-us/dotnet/api/system.io.path.getfullpath?view=net-10.0)可以基于固定 base path 得到 fully-qualified lexical candidate；[Path.GetRelativePath](https://learn.microsoft.com/en-us/dotnet/api/system.io.path.getrelativepath?view=net-10.0)会先处理 full path，并使用当前平台的 path comparison 计算相对关系；[Directory.ResolveLinkTarget](https://learn.microsoft.com/en-us/dotnet/api/system.io.directory.resolvelinktarget?view=net-10.0)提供解析 symbolic link / junction final target 的 surface。

但这些 API 只是构件，不是“调用一次就安全”的授权算法。`GetFullPath` 不会替 Host 决定 allow-root；lexical containment 也看不见 allow-root 内某个 junction 最终指向 root 外。课程中的 Path Decision v1 因此分两次检查：

```text
fix fully-qualified allowRoot
candidate = GetFullPath(relativePath, allowRoot)
reject if GetRelativePath(allowRoot, candidate) escapes

for each existing component:
  resolve link / junction final target
  reject if resolved target escapes

open read-only only after both checks pass
```

第一道检查处理不同 root、rooted relative result、`..` 及其后续路径；第二道检查逐个查看现有 component，对 link / junction 解析 final target，再重新判断 containment。两道都通过后，才允许只读 open。它比 `candidate.StartsWith(allowRoot)` 更明确，也仍然不是 production Sandbox。

这两道检查解决的是不同问题。lexical candidate回答“按照字符串和平台路径规则归一化后，目标写在哪里”；resolved target回答“文件系统实际跟随重解析点后，会走到哪里”。如果只做后者，输入中的显式越界没有清晰的first-failure分类；如果只做前者，合法外观下的link topology又会被忽略。把它们拆开，Trace才能分别记录`PATH_OUTSIDE_ROOT`与`PATH_LINK_OUTSIDE_ROOT`，调用方也能知道拒绝发生在open之前。

固定allow-root同样重要。若canonicalization依赖可变current directory，同一份arguments可能因Host启动位置不同而得到不同结果，审计时也无法重建当时的资源边界。课程设计要求先取得fully-qualified existing root，再以它作为所有相对路径的base。这是设计判断，不是说“绝对路径天然安全”；真正的许可范围仍由Host policy定义。

下面三条 observed behavior 的完整证据范围相同：**fixed Calculator + ReadOnlyFileTool / ASCII fixture、Windows 10.0.19045、.NET SDK 10.0.301、single process、no concurrent link mutation**。

| Case | Observed terminal | Handler count | 后续状态 |
|---|---|---:|---|
| `small.txt` | `SUCCEEDED / OK` | 1 | result validation与inline render继续 |
| `../outside/secret.txt` | `CANONICALIZE / PATH_OUTSIDE_ROOT` | 0 | validate、policy、execute、result、render均`NOT_RUN` |
| `link-out/secret.txt` | `CANONICALIZE / PATH_LINK_OUTSIDE_ROOT` | 0 | validate、policy、execute、result、render均`NOT_RUN` |

在上述完整 fixed scope 内，两个 fresh setup 都真实创建了 `JUNCTION`；`.NET Directory.ResolveLinkTarget(path, true)` 确认 final target 位于 allow-root 外、但仍在 Lab-owned run-root 内。valid read 没有被边界检查误拒绝，lexical traversal 与真实 junction escape 都在 execute 前停止。

这些观察仍没有关闭 TOCTOU。检查完成之后、open 发生之前，另一个并发 actor 可能替换 link target；本 Lab 明确不做 concurrent link mutation。它也没有验证 symlink fallback、其他 OS / filesystem、handle-based confinement 或对抗性 production load。因此准确结论是：固定 topology 下的两个拒绝边界成立；不能写成“这套算法消除了所有路径逃逸”，更不能写成“path canonicalization 已经实现 Sandbox”。

## Execute 前：Schema Valid 之后，Policy 仍可以拒绝

路径留在 allow-root，只能回答“目标在哪里”；它没有回答“这次调用能不能读”。同样，arguments 通过 Schema、DTO 与 Domain，也只能说明候选满足当前数据合同，不会授予资源使用许可。这就是 `Schema Valid != Policy Allowed`。

课程 Policy v1 接收 global、tool、resource 三层输入，每层只取 `ALLOW / DENY / ASK / MISSING`：任何 `DENY` 都返回 `DENY`；没有 deny 但存在 `MISSING` 时也 fail closed；没有前两者而存在 `ASK` 时返回 `ASK`；只有全部 `ALLOW` 才进入 idempotency 与 execute。

> **证据状态：COURSE PROPOSAL / NOT INDUSTRY STANDARD。** `Deny > Ask > Allow` 是课程为 Lab 冻结的 merge rule。其他系统完全可能使用 specificity、priority、first-match 或显式人工 override。

下面两条 observed behavior 只覆盖：**fixed Calculator + ReadOnlyFileTool / ASCII fixture、Windows 10.0.19045、.NET SDK 10.0.301、single process、no concurrent link mutation**。

| Policy inputs | Observed decision / terminal | Handler | Result / render |
|---|---|---:|---|
| `ALLOW / ASK / DENY` | `DENY / POLICY_DENIED` | 0 | `NOT_RUN / NOT_RUN` |
| `ALLOW / ASK / ALLOW` | `ASK / APPROVAL_REQUIRED` | 0 | `NOT_RUN / NOT_RUN` |

在上述完整 fixed scope 内，`DENY` 没有被 `ALLOW` 或 `ASK` 覆盖；无 deny 但存在 ask 时，也没有进入 handler。它证明的是实现符合 Course Policy v1 的两个冻结冲突 case，不证明这条 merge rule 最优或通用。

`APPROVAL_REQUIRED` 还不是完整 Permission / Approval 系统。Lab 在这里直接形成 terminal，不等待真人输入，也没有 identity、credential scope、approval UX、resume state 或 enforcement model。Policy 可以表达“当前不能继续”，却不会自动生成完整权限体系；正如 `Sandbox != Permission`，一个隔离环境也不会自动回答谁批准了什么能力。

Policy gate的工程价值还在于让冲突显式化。若global层允许只读Tool，tool层要求询问，resource层明确拒绝某个目录，那么最终decision不应由配置加载顺序偶然决定。课程proposal选择保守合并，并把三个input与最终decision一起写入Trace。这样Reviewer看到`POLICY_DENIED`时，可以追溯是哪一层给出了deny，而不是只看到一个没有上下文的布尔值。

这也解释了为什么Policy不能藏在handler内部。handler内部当然可以再次防御，但如果它是唯一拒绝位置，Runtime在进入副作用代码之前无法统一判断，也难以保证deny后的idempotency、execute与result stages保持`NOT_RUN`。把Policy作为独立gate，不是为了增加抽象名词，而是为了让“未执行”成为可验证事实。

## Execute 中：timeout 与 caller cancellation 不能压成一个 CANCELLED

很多包装层会统一 catch `OperationCanceledException`，然后只返回 `CANCELLED`。这个结果丢失了至少三个事实：是谁发出的停止请求、handler 是否已经进入、Runtime 停止等待时 underlying work 是否真的停止。

[.NET managed cancellation 指南](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads)把 cancellation 描述为 requester 与 listener 配合的 cooperative model；[CancellationTokenSource.CancelAfter](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtokensource.cancelafter?view=net-10.0)只是调度 cancellation request；[Task<TResult>.WaitAsync](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1.waitasync?view=net-10.0)则提供 task completion、timeout 与 caller token 等 completion surface。这些合同支持保留 source identity，却都不会承诺任意第三方 handler 会及时停止。

课程 Runtime 为每个 invocation 接收 caller token，另建 timeout source，再把 linked cooperative token 交给 handler。terminal 必须保留 `cancellation_origin=CALLER | TIMEOUT`，不能只留下一个无法解释的异常类型。

下面两条 observed behavior 只覆盖：**fixed Calculator + ReadOnlyFileTool / ASCII fixture、Windows 10.0.19045、.NET SDK 10.0.301、single process、no concurrent link mutation**，并且 timeout 使用 test-only cooperative never-release gate，而不是真实慢 I/O。

| Fault | Observed terminal / origin | Handler count | Result / render |
|---|---|---:|---|
| caller active，50ms timeout | `TIMED_OUT / TIMEOUT` | 1 | `NOT_RUN / NOT_RUN` |
| caller预先取消，5000ms timeout | `CALLER_CANCELLED / CALLER` | 0 | `NOT_RUN / NOT_RUN` |

在上述完整 fixed scope 内，timeout case 进入了 handler test gate，再由 timeout source结束等待；caller预取消则在 handler之前停止。两者都没有成功 result，Trace 保留了不同 origin。

这个观察不能推出“timeout 强制杀死 Tool”。Runtime 结束等待，只证明当前 invocation 已按合同形成 terminal；如果 handler忽略 token、执行不可取消 I/O，或者在另一个进程中继续工作，underlying work 可能仍未停止。本 Lab 没有覆盖两个 source 同时触发的 race、真实慢 I/O、process isolation或精确 deadline。工程上必须分开“停止等待”“发出取消请求”和“工作已确认停止”三种断言。

这三个状态会直接影响后续动作。只知道“调用方不再等待”，不能安全地把同一invocation当作从未发生；只知道token已requested，也不能立即释放所有与handler共享的资源；只有实现提供额外确认，系统才有资格声称工作已经停止。本文不设计恢复和补偿，但Runtime至少要保存origin与handler是否进入，为后续判断留下事实，而不是用一个`CANCELLED`抹掉不确定性。

timeout budget也不等同于性能指标。50ms只是冻结fault case用来稳定触发分支，不代表任何production Tool的推荐deadline。真实budget需要结合操作语义、上层任务时限和资源策略另行决定；本文只验证分类边界，不从一次本地elapsed time推导性能结论。

## Execute 后：先验证 Result，再决定给谁看

handler 正常 return，只说明执行入口返回了一个 candidate result。它可能 kind 错误、缺字段、数值非法、byte count 与 digest 不一致。若 Runtime 在验证前就把它写进 cache、UI 或模型上下文，失败结果会被后续系统当作成功事实。

课程 Result Contract v1 先产生 Canonical Result candidate，再检查 kind、required fields、type、finite value、byte count 与 digest。只有 validation 通过，才决定 inline 或 spill，并派生不同 consumer view：

| View | 主要职责 | 内容边界 |
|---|---|---|
| Canonical Result | result validation输入 | 只在execution期内存中存在 |
| Model View | 有界回注候选 | preview + byte count + digest + relative spill ref |
| UI View | Host显示metadata | inline或relative spill ref，不把absolute temp path当稳定合同 |
| Trace View | stage / decision / digest审计 | 不保存preview或full content |

> **证据状态：COURSE PROPOSAL / NOT INDUSTRY STANDARD。** 四种 view、64-byte inline threshold、4096-byte read cap、spill 目录和字段 shape 都是课程设计。它们不是生产 redaction 标准，也不是唯一的大结果处理方式。

下面的 observed behavior 只覆盖：**fixed Calculator + ReadOnlyFileTool / ASCII fixture、Windows 10.0.19045、.NET SDK 10.0.301、single process、no concurrent link mutation**。

在上述完整 fixed scope 内，valid Calculator 返回数值 `5`，valid `small.txt` read 返回 11 bytes，二者通过 result validation并 inline render。test seam 让 Calculator handler 返回错误的 `file_text` kind时，execute仍为`PASS`、handler count为`1`，但 terminal 是`RESULT_VALIDATION / RESULT_SCHEMA_INVALID`，render与cache均未发生。这个 case把“handler返回”与“结果可消费”明确分开。

同一完整 fixed scope 内，读取1024-byte `large.txt`时，result validation通过，full bytes写入Lab-owned temp spill；Model View只有64-byte preview、byte count、SHA-256与relative spill ref，UI View记录`SPILLED` metadata，Trace View不含preview、full content或absolute temp path。两次保留spill都为1024 bytes，SHA-256均是：

```text
26AD8132E3B544CAEFD85B30BF36DF8D012DC7245C9D2224E0F9F50A2AC55A61
```

spill 是 Host internal Lab artifact，不是 ReadOnlyFileTool 获得了业务写能力。这个实验也没有覆盖 binary、encoding attack、secret redaction、真实模型消费或production内容安全。因此不能把“Trace没有全文”升级成“敏感数据绝不会泄露”。

更重要的是 `Result != Evidence`。Validated Result只表示当前内部contract接受它。它没有自动携带provenance、retrieval time、claim-to-source mapping与独立verification。特定Tool可以输出带来源的rich result，但是否足以支持某个Claim，仍要由后续Evidence contract判断；把结果放进专用result object，不会让内容天然变真。

四种view还把“内容存在”与“内容可暴露”分开。Canonical Result可以在execution期间持有完整数据，用于验证byte count与digest；Model View只交付完成下一步推理所需的有界信息；UI View服务于人类检查与按需打开；Trace View则优先保留decision与关联信息。它们可以来自同一个validated result，却不必共享相同序列化对象。若为了省事复用一个字符串，任何消费者扩大的内容需求都会同时扩大其他三个面，最终很难说明敏感内容究竟流向了哪里。

先validation再render还有一个直接收益：cache中不会混入handler已经返回、但contract尚未接受的candidate。否则duplicate replay可能稳定复用一个无效结果，把偶发错误变成可重复错误。课程fixture只验证wrong kind不会进入cache；它没有覆盖所有结果错误，但足以说明result gate必须站在cache与consumer view之前。

## Idempotency 与 Trace：可判定重复，不等于 exactly-once

duplicate invocation 不是边缘问题。模型可能重复请求，上层可能因timeout retry，workflow也可能replay先前step。Runtime若只看tool name与arguments，就无法知道“这是新调用，还是同一调用的再次送达”；若只看invocation ID，又无法处理同ID却换了参数的冲突。

课程 Idempotency v1 用 `invocation_id` 作为key，缓存 canonical arguments digest、validated result与render metadata：

```text
new invocation_id
  -> execute once -> cache args digest + validated result

same id + same digest
  -> REPLAYED -> no second handler execution

same id + different digest
  -> IDEMPOTENCY_CONFLICT -> no second handler execution
```

> **证据状态：COURSE PROPOSAL / NOT INDUSTRY STANDARD。** single-process cache、digest字段、replay语义与每invocation一行JSONL都是课程设计，不是distributed idempotency或exactly-once协议。

下面的 observed behavior 只覆盖：**fixed Calculator + ReadOnlyFileTool / ASCII fixture、Windows 10.0.19045、.NET SDK 10.0.301、single process、no concurrent link mutation**。

| Case | Args relation | Observed terminal | Execute | Handler count | Result |
|---|---|---|---|---:|---|
| `TR-11.1` | first | `SUCCEEDED / OK` | `PASS` | 1 | digest A |
| `TR-11.2` | same ID / same digest | `IDEMPOTENCY / REPLAYED` | `NOT_RUN` | 1 | digest A |
| `TR-12.1` | first | `SUCCEEDED / OK` | `PASS` | 1 | digest A |
| `TR-12.2` | same ID / different digest | `IDEMPOTENCY / IDEMPOTENCY_CONFLICT` | `NOT_RUN` | 1 | none |

在上述完整 fixed scope 内，同ID同参数复用了validated result，handler count没有增加；同ID异参数形成conflict，没有第二次执行，也没有result。每个attempt仍追加独立Trace row，因此replay与conflict不是“没有记录”。

同一完整 fixed scope 内，每个fresh run先用`FileMode.CreateNew`创建空artifact，再逐invocation以append写入；spec还验证second CreateNew被拒绝，并在每次append后确认旧bytes仍为新文件prefix。两个run各有12个case groups、14条invocation rows，sequence均为`1..14`。两份trace各10607 bytes，SHA-256完全相同，并经独立byte-array comparison确认byte-identical：

```text
50CEA4EC1B0D2F15E9789603032AB0BF4DD7F4B0ADDFFC16BBF9733A0321BD67
```

这只证明固定两次本机run生成了相同artifact，以及single-process de-dup seam按frozen contract工作。Calculator与ReadOnlyFileTool没有业务写副作用；cache不跨process、restart或crash；实验也没有concurrent call、cache eviction、distributed lock、transactional side effect或global ordering。因此不能使用“exactly-once”“durable idempotency”或“分布式重放安全”的语态。

幂等键本身也不是魔法。Runtime必须先对arguments形成稳定canonical representation与digest，才能判断“same args”；如果不同表示被错误地视为不同请求，replay会退化成第二次execute；如果不同语义被错误地压成同一digest，conflict又会被误当作replay。Lab冻结property order与InvariantCulture来获得可比较artifact，这仍只是fixture约束，不是跨语言canonicalization标准。

对真实副作用Tool，de-dup cache还要面对更难的时间窗口：副作用已经发生但result尚未成功写入cache，或者process在两者之间crash。本文没有证据关闭这个窗口，所以只把invocation identity、digest comparison与terminal记录建立为seam。seam让问题可见，却没有替系统完成事务协调。

Trace同样不等于完整Failure Taxonomy。Lab的JSONL只覆盖课程Trace Schema v1与固定case；它不含wall-clock、absolute temp path、environment variable value、file content、credential或stack trace。这个取舍有利于deterministic artifact，却不证明它足以承担production observability、跨step replay或事故调查。

## Lab 02：怎样把 Expected、Observed 与 Interpretation 分开

只有sample code、README和expected output，不算Lab完成。Lab 02在执行前冻结12个case groups、14个invocation rows、terminal stage/code、handler count、timeout budget、64/4096-byte threshold、Policy v1与Trace fields；任何invalid case被接受、early terminal后仍execute、link无法真实创建，或两次artifact不同，都会让对应Claim保持BLOCKED。

冻结Expected的意义，是让实现不能在看到结果后再改“什么算通过”。Lab Engineer只负责实现、执行与保存raw observation；Researcher随后重新读取Design、failure output、trace、result views、run-state与spill，再决定Claim能否升级。若真实junction无法创建，正确结果应是实验失败并保留BLOCKED，而不是用字符串模拟一个“像link的路径”。这种角色与阶段分离，让文章结论受实验约束，而不是让实验为文章结论服务。

同理，两次run不是为了用重复次数制造统计显著性。它们只检查在相同冻结输入、不同fresh process与不同unique temp root下，课程要求的deterministic artifact是否仍一致。两次一致可以支持这个窄问题；它不能回答case集合之外的概率、负载或跨平台差异。把重复运行的研究问题写清，才不会把“跑了两次”包装成比实际更强的可靠性证据。

下面整张汇总表中的observed behavior都只覆盖：**fixed Calculator + ReadOnlyFileTool / ASCII fixture、Windows 10.0.19045、.NET SDK 10.0.301、single process、no concurrent link mutation**。

| Boundary | Frozen cases | Observed | Interpretation |
|---|---|---|---|
| Path | valid、traversal、real junction escape | valid成功；两种escape在execute前拒绝 | fixed topology成立；TOCTOU未覆盖 |
| Policy | Deny conflict、Ask conflict | `POLICY_DENIED`与`APPROVAL_REQUIRED`，handler=0 | 实现符合Course Policy v1，不外推行业规则 |
| Cancellation | timeout gate、caller pre-cancel | `TIMED_OUT/TIMEOUT`与`CALLER_CANCELLED/CALLER` | source identity保留；不证明强制停止 |
| Result | valid、wrong kind、1024-byte result | valid成功；wrong kind不render/cache；large result spill | Result Contract v1在ASCII fixture成立 |
| Duplicate / Trace | replay、conflict、two fresh runs | handler不二次执行；14-row traces byte-identical | single-process de-dup与fixed artifact成立 |

在上述完整 fixed scope 内，两次accepted setup都创建真实junction，并确认final target在allow-root外；两个run-root不同。cleanup前，full spill被复制到Lab artifact目录供复核；cleanup则依次核对absolute temp parent、name prefix、sentinel、non-parent、owned final link target、root非reparse point与移除link后无remaining reparse point，最后才recursive delete。两次temp root最终都确认不存在。

实验没有调用Provider、network或credential，没有开放shell Tool，没有business write。唯一可执行Tool是Calculator与ReadOnlyFileTool；Host internal write只限unique Lab temp spill和Lab artifacts。完整Design、Observed与Interpretation保存在[Lab 02](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-02-tool-runtime/README.md)，accepted与failed command历史保存在[Execution Summary](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-02-tool-runtime/artifacts/logs/execution.md)，两份raw trace分别是[first JSONL](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-02-tool-runtime/artifacts/observation-first.jsonl)与[second JSONL](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-02-tool-runtime/artifacts/observation.jsonl)。

因此，Lab的结论不是“Tool Runtime安全可靠”，而是：在完整fixed scope内，C05—C09的Expected、Observed与Interpretation逐项对齐；C04仍然只是课程设计proposal。环境、case、failure history与limitations不能从结论中删除。

## 三次 first failure：修复不能把失败历史抹平

一条全绿流水线很容易制造错觉：好像设计第一次就正确落地，环境也没有差异。Lab 02保留了三次first failure，因为它们能回答另一个重要问题：后来的patch究竟只修了执行入口，还是为了PASS偷偷改变了Expected？

| First failure | 原始观察 | 最小patch | 没有改变什么 |
|---|---|---|---|
| ProjectReference | `RESTORE-01` exit 0，但stdout两次报告spec ProjectReference多退一层并被跳过 | 只修relative ProjectReference，再执行accepted restore | Design、cases、Expected、dependency边界 |
| PowerShell 5 `GetRelativePath` | `SETUP-FIRST-01` exit 1；temp root创建前失败 | 改为fully-qualified parent + separator的`OrdinalIgnoreCase` containment guard | 四类安全classification、真实link要求 |
| junction `Remove-Item` | `CLEANUP-FIRST-01` exit 1并抛`NullReferenceException`；root与evidence保持原样 | 只用同进程`[IO.Directory]::Delete(path, false)`删除已验证junction，再重跑guarded cleanup | parent/prefix/sentinel/link-target/remaining-reparse guards |

第一次restore虽然exit code是0，却不能只看进程成功码；stdout已经说明ProjectReference被跳过，因此被标为rejected attempt。第一次setup在temp root创建前失败，没有留下需要猜测归属的目录。第一次cleanup失败时，script也没有继续recursive delete，而是保留root与所有evidence，等待修补后重跑。失败时的安全状态和accepted rerun同样重要。

三个patch都没有修改hypothesis、12-case matrix、50/5000ms timeout、64/4096-byte threshold、terminal code、Trace fields或acceptance。它们也不能被外推成所有PowerShell或filesystem的通用做法。保存first failure的目的不是美化“排障能力”，而是让Reviewer能核对：最终PASS有没有通过降低判据换来。

## 工程边界：fixed-scope evidence 不能替代 production assurance

到这里，最危险的误读不是漏记某个stage，而是把局部证据升级成完整安全保证。下面这些等号都不成立：

| 当前已经有的东西 | 不会自动变成什么 | 仍缺什么证据 |
|---|---|---|
| lexical + resolved-target check | production Sandbox | TOCTOU、handle confinement、其他OS / filesystem、对抗性负载 |
| Course Policy v1 | Permission / Approval系统 | identity、credential scope、human resume、enforcement |
| timeout / caller terminal | underlying work已停止 | handler cooperation、真实I/O、process isolation、race |
| validated bounded Result | Evidence | provenance、claim mapping、independent verification |
| same-ID de-dup | exactly-once side effect | durable store、crash recovery、transaction、distributed coordination |
| append-only JSONL | complete Trace / Replay / Failure Taxonomy | 跨step state、production events、replay semantics、retention |

`Sandbox != Permission`尤其容易被说粗。Sandbox约束“代码在什么环境与能力集合中运行”；Permission回答“哪个主体在什么条件下被允许做什么”；Approval又涉及谁作出临时决定、决定怎样记录与恢复。三者可以协作，但不能互相替代。本篇只说明Path与Policy gate怎样在execute前形成terminal，不进入完整Permission设计。

同样，课程Pipeline不是行业标准。其他Runtime可以把Canonicalize与Validate合并，可以先做cost/resource precheck，也可以把Policy与execution委托给远程服务。只要它能清楚回答责任、terminal与evidence问题，就不必复制本课程的class或stage names。课程proposal的价值在于提供一套可运行、可注入失败、可保存`NOT_RUN`的最小样本，而不是规定所有系统必须长成同一形状。

生产方案至少还要继续问：path是否需要handle-based confinement；高风险Tool是否需要process isolation；idempotency是否需要durable store与业务事务；result是否需要secret classification与redaction；Policy是否绑定identity和credential；crash后怎样恢复unknown side effect；Trace怎样关联step、budget与operator decision。这些是后续验证问题，不是本文已经给出的设计答案。

这些问题也说明“安全”不是一个可以一次盖章的属性。输入安全、资源授权、执行隔离、取消完成、结果暴露、重复副作用与审计可重建分别需要不同证据。一个方案可以在其中两项做得很好，仍然不能替另外几项宣布成功。工程评审更有价值的输出，不是“这个Tool安全/不安全”的总评，而是一张责任与证据矩阵：哪些边界已由合同建立，哪些在fixed fixture中观测过，哪些仍需要production验证。

反过来，证据边界也不意味着Lab没有价值。固定fixture把本来容易停留在口号里的fail closed、`NOT_RUN`、cancellation origin、result validation与duplicate classification变成了可执行判据。它提供的是一个可以继续扩展的验证起点：未来条件变化时，应增加新的Expected与observation，而不是删掉当前限制后直接外推。

## 怎样审查一条 Tool Runtime？

拿到一个项目里的Tool实现，可以用下面十个问题做一次可判定审查：

1. 当前能力是client-executed，还是Provider-managed built-in / server tool？execution owner在哪里？
2. model-visible ToolDefinition与Host Registry、handler、risk metadata是否分开？unknown name会不会直接落入反射调用？
3. arguments在execute前经过哪些canonicalization、Schema / DTO / Domain与resource checks？第一次失败在哪一层？
4. `Schema Valid`之后是否仍有独立Policy decision？Deny、Ask与Missing怎样fail closed？
5. invocation identity与canonical arguments digest是否可审查？same-ID same-args与different-args分别怎样处理？
6. caller cancellation与timeout是否保留source？系统是否把“停止等待”误写成“工作已停止”？
7. handler result是否在render与cache前验证？Model、UI、Trace是否各有bounded view？
8. early terminal之后，哪些stage必须是`NOT_RUN`？每条terminal path是否都有记录？
9. Trace能否关联到冻结的Expected、环境、原始失败与limitations，还是只有一条无法解释的success log？
10. 当前结论来自official API contract、course proposal、fixed Lab observation，还是production evidence？这四类有没有被混写？

这组问题也对应几项可检验的工程能力：能否切出contract seam，能否为失败建立状态模型，能否识别filesystem与cancellation的证明边界，能否设计bounded result，能否拒绝把single-process de-dup叫成exactly-once，以及能否用失败历史解释最终结论。评审不要求所有项目采用同一个类名；它要求每个判断有owner，每次副作用可追踪，每个保证有相称的证据。

## Learning Check

1. client-executed ToolDefinition已有name、description与schema，为什么仍需要Host Registry？built-in / server tool对结论有什么限制？
2. `relative_path`通过Schema，且`GetFullPath`结果位于allow-root下，能否直接读取？还缺哪一道检查，哪类race仍未关闭？
3. global=`ALLOW`、tool=`ASK`、resource=`DENY`时，Course Policy v1怎样终止？为什么不能把答案写成行业规则？
4. timeout case返回`TIMED_OUT`，能否断言handler已经停止？它与caller预取消有哪些可观察差别？
5. handler执行成功，但result kind错误，execute、result validation、render与cache分别应是什么状态？
6. 为什么Canonical Result、Model View、UI View与Trace View不能共用一个无界string？为什么validated Result仍不等于Evidence？
7. same invocation ID + same args与same ID + different args应怎样处理？为什么都不能证明exactly-once？
8. 两次14-row JSONL byte-identical、spill hash一致且junction cleanup成功，能证明什么，不能证明什么？
9. ProjectReference、PowerShell 5 `GetRelativePath`与junction `Remove-Item`三次first failure为什么必须保留？
10. 怎样分别验证`Schema Valid != Policy Allowed`、`Result != Evidence`与`Sandbox != Permission`？

### 参考思路

下列参考思路凡复述C05—C09的Lab observed behavior，均只指：**fixed Calculator + ReadOnlyFileTool / ASCII fixture、Windows 10.0.19045、.NET SDK 10.0.301、single process、no concurrent link mutation**。

1. Definition负责模型可见合同，Registry负责executable owner与Host-only metadata；Provider-managed tools反驳“全部本地执行”。
2. 还要检查link / junction resolved target；fixed Lab只验证无并发link mutation的topology，未关闭check/open TOCTOU。
3. 返回`DENY / POLICY_DENIED`，execute=`NOT_RUN`；merge顺序是课程proposal。
4. 不能；cancellation cooperative。fixed test gate中timeout进入handler且origin=`TIMEOUT`，预取消在handler前停止且origin=`CALLER`。
5. execute=`PASS`，result validation=`FAIL`，render与cache=`NOT_RUN`。
6. 消费者需要不同内容边界；internal contract有效不等于具备provenance与独立verification。
7. 前者replay且不二次执行，后者conflict且无result；single-process cache与无业务写fixture不支持exactly-once结论。
8. 只证明完整fixed scope内的case行为与artifact determinism；不证明跨环境、TOCTOU、production safety、Permission或distributed idempotency。
9. 它们让Reviewer确认原始环境差异、失败时安全状态、最小patch与frozen Expected没有被改写。
10. 分别检查数据合同后的Policy gate、Result之外的Evidence contract，以及隔离机制之外的主体/能力授权与approval记录。

## 最短结论

`Tool Runtime 的价值，不是替模型多包一层函数，而是让每次执行都知道为何能继续、为何必须停止、结果能给谁看，以及证据到底证明到哪里。`

## 参考资料

- [OpenAI：Function calling](https://developers.openai.com/api/docs/guides/function-calling)
- [Microsoft：Path.GetFullPath](https://learn.microsoft.com/en-us/dotnet/api/system.io.path.getfullpath?view=net-10.0)
- [Microsoft：Path.GetRelativePath](https://learn.microsoft.com/en-us/dotnet/api/system.io.path.getrelativepath?view=net-10.0)
- [Microsoft：Directory.ResolveLinkTarget](https://learn.microsoft.com/en-us/dotnet/api/system.io.directory.resolvelinktarget?view=net-10.0)
- [Microsoft：Cancellation in managed threads](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads)
- [Microsoft：CancellationTokenSource.CancelAfter](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtokensource.cancelafter?view=net-10.0)
- [Microsoft：Task<TResult>.WaitAsync](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1.waitasync?view=net-10.0)

### 本地证据资产

- [Article 06 Evidence Register](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/articles/06-tool-runtime/evidence.md)
- [Lab 02 Design / Observation / Interpretation](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-02-tool-runtime/README.md)
- [Lab 02 Execution Summary](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-02-tool-runtime/artifacts/logs/execution.md)
- [First JSONL Observation](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-02-tool-runtime/artifacts/observation-first.jsonl)
- [Second JSONL Observation](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-02-tool-runtime/artifacts/observation.jsonl)
- [First Result Views](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-02-tool-runtime/artifacts/result-views-first.json)
- [Second Result Views](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-02-tool-runtime/artifacts/result-views-second.json)
