# Article Card｜06 Tool Runtime

## Canonical identity

- Title：`Tool Runtime：Validate、Policy、Execute、Result 与 Trace`
- Part：`Part II｜从模型到 Agent`
- Weight：`L（Major Core Lesson）`
- Optional：`No`
- Mode：`LAB_ARTICLE`
- Required Lab：`Lab 02 Tool Runtime`

## Responsibility

建立模型 Tool Call intent 到真实 Host execution 之间的工程管线，解释 ToolDefinition / Registry、canonical arguments、validation、policy、timeout / cancellation、idempotency、result validation、render / spill 与 trace 的责任边界。

## Reader questions

1. Tool 为什么不是普通函数包装？
2. Model-visible Schema 与 Host-only metadata 为什么分开？
3. Tool Runtime 怎样处理副作用、幂等、超时与取消？
4. Result 为什么需要分别面向 Model、UI 与 Trace？
5. Policy 冲突为什么必须 fail closed？

## Dependencies

- Article 03：Parse / Schema / DTO / Domain validation。
- Article 05：Function Calling、Tool Call intent、correlation 与 Host decision。

## Canonical model to investigate

```text
Call -> Canonicalize -> Validate -> Policy
     -> Execute -> Validate Result -> Render / Spill -> Trace
```

这只是 canonical 研究问题，不是已验证实现结论。

## Required evidence

- current official / standard / primary-source contracts relevant to Tool calling, cancellation, path handling, result limits and trace boundaries；
- Lab 02 frozen Design、source、tests、raw observations 与 trace；
- Expected -> Observed -> Evidence mapping；
- environment、dependency pin、failure injection 与 limitations。

## Lab 02 responsibility

- Inputs：Calculator + ReadOnly File Tool。
- Observe：arguments、Policy Decision、Timeout / Cancellation、Result、Trace。
- Required failures：越界路径、大结果、取消、重复调用。
- Behavior chapters remain `BLOCKED` until Lab execution and Evidence Merge。

## Explicit non-goals

- 不开放 Shell、写文件或生产凭证。
- 不把 Sandbox 写成 Permission，也不展开 Article 19 的 approval / permission system。
- 不讲 Article 08 的 Agent Loop。
- 不进入 Article 35 的 DeepSeek Harness source verification。
- Lab 02 不进入 BuildPilot，也不证明跨 runtime / OS / filesystem 的通用行为。
