# Article Card｜11 Long-running Agent

> 权威基线是`docs/agent-engineering-series-plan.md`。`docs/agent-engineering-course-plan-v3.1-review.md`的Article 11 frozen section只作结构输入，不承担生产期状态或事实权威。本文件只机械实例化既有课程职责，不预设Research / Evidence / Lab结论。

## Canonical identity

- Title：`Long-running Agent：Checkpoint、Retry、Cancellation 与 Recovery`
- Part：`Part II｜从模型到 Agent`
- Weight：`M（Standard Core Lesson）`
- Optional：`No`
- Mode：`LAB_ARTICLE`
- Required Lab：`Lab 04 State Machine + Checkpoint`

## Positioning

长任务工程篇。把Article 10的确定性State / Workflow骨架扩展到可中断、可恢复且不会盲目重试的运行；Recovery必须是显式合同，而不是“再跑一次”。

## Reader questions

1. Timeout、Cancellation、Retry、Resume与Recovery怎样区分？
2. 哪些State、Evidence、已完成Action与Budget应进入Checkpoint？
3. 幂等性为什么决定某个失败能否安全Retry？
4. 外部副作用已经发生但响应丢失时，还能恢复什么、必须诚实报告什么？
5. Long-running Task怎样输出known / unknown / unverified / next safe action的partial result？

## Dependencies

- Article 06：Tool Runtime、Result、Trace与副作用执行边界。
- Article 08：Loop、committed Step、Observation、State与Stop / Success分离。
- Article 09：Plan / execution / verified state / authorization分离。
- Article 10：State Machine / Workflow、legal transition与checkpoint bridge。

## Candidate mental model

```text
Failure / Cancel
  -> Classify
  -> Retry / Checkpoint / Compensate / Ask / Stop
  -> Resume within explicit boundary
```

这只是canonical研究对象，不是已验证的跨产品universal recovery algorithm。

## Canonical content spine

1. 长任务新增的失败面：模型、Tool、网络、异步系统与用户取消。
2. Checkpoint保存什么、不保存什么，以及Checkpoint为什么不等于Memory。
3. Retry的transient / permanent分类、idempotency key与Retry Budget。
4. Cancellation如何传播到模型、Tool与Workflow，并落最后安全状态。
5. Recovery / Resume怎样从安全点继续、补偿或人工接棒；无法回滚的外部系统必须诚实报告。
6. Partial Result表达known、unknown、unverified与next safe action，并用Lab 04收束。

## Lab 04 responsibility

- Runtime：`C# / .NET`。
- Candidate input：`Fake Long-running Investigation`。
- Required observations：State、Checkpoint、Retry Count、Cancellation Trace、Resume Trace。
- Required behaviors：取消后从显式安全边界恢复；幂等Retry不重复外部副作用；checkpoint必须记录未完成动作或等价恢复信息。
- Required failure themes：重复副作用、checkpoint缺失未完成动作、无法安全恢复时必须拒绝伪成功。
- Evidence requirement：真实source / tests / build / run / fault injection / raw trace；精确case matrix、deterministic substitute、schema与acceptance criteria由Researcher在frozen Lab Design中确定。

## Evidence requirements

- current official / primary sources对checkpoint、retry、idempotency、cancellation、resume / recovery的产品或规范范围；
- Preliminary Evidence必须标明product / version / retrieved scope，依赖Lab的Claim不得提前`CONFIRMED`；
- Lab 04 frozen Design、真实运行证据、取消Trace与幂等性测试；
- `Experiment -> Observation -> Evidence Interpretation -> Claim Status`完整映射；
- Retry != Recovery、Timeout != Cancellation、Resume != Replay、Checkpoint != Memory的反证搜索。

## Explicit non-goals

- 不设计分布式事务，不承诺回滚所有外部副作用。
- 不把某个产品的checkpoint schema、retry policy或cancellation API写成行业统一接口。
- 不把serialization、restart或re-execution直接等同Resume / Recovery。
- 不把Expected Observable、test assertion或frozen Design写成Observed Result。
- 不展开Article 12 Context Engineering，也不进入Memory、RAG、Multi-Agent或Harness实现。
- 不读取DeepSeek Harness源码，不实现或预演BuildPilot Runtime。

## Learning check candidates

1. Tool已创建远端资源后超时，能否直接Retry？
2. 用户取消时为什么仍需落最后安全状态？
3. 哪些短Compile Case状态无需Checkpoint？
4. Checkpoint有current state但没有in-flight action / side-effect identity，能否安全Resume？

## Job competency target

- 能把长任务failure surface转成显式分类、预算与恢复边界。
- 能依据副作用与幂等性决定Retry、Resume、Compensate、Ask或Stop。
- 能用raw trace与checkpoint证明恢复行为，同时诚实限定fixture与产品scope。
