# Article 28 Card｜怎样把 DeepSeek Harness 当作 Evidence-first 源码教材

## Identity

- Canonical ID: `28`
- Part: `VI｜DeepSeek Harness`
- Weight: `S`
- Optional: `NO`
- Required Lab: `NONE`
- Required Evidence Work: `BASELINE PROBES`
- Article Type: `STAGE_NAVIGATION / SOURCE_METHOD`
- Mode: `DSH_SOURCE_MODE`

## Problem Space

直接从目录、README 或同名类型开始，会把当前 alpha 实现误写成通用 Harness 定义。本篇先冻结版本、来源层级、运行边界和验证路径，再允许 Article 29—37 进入源码结论。

## Core Questions

1. 怎样证明后续十篇研究的是同一个 DSH 快照？
2. Official Doc、Pinned Source、Runtime Observation、Experiment、Inference 与 Design Proposal 怎样分层？
3. symbol、call path、test、minimal run 与 trace 各能证明到哪一层？
4. build/test/run 失败怎样限制 Claim，而不是被删除或包装成成功？
5. Article 29—37 分别要沿哪些源码与运行入口取证？

## Teaching Spine

```text
先冻结研究对象
-> 再冻结证据分类与安全边界
-> 从 symbol 逐级走到 runtime trace
-> 把失败也纳入证据
-> 最后路由 Article 29—37
```

## Frozen Boundaries

- 不逐模块讲解 DSH，不抢跑 Host/Runtime/Harness/Product 分层结论。
- 不把目录存在、依赖声明或 README 描述升级为 runtime fact。
- 不使用真实生产输入、生产凭证或公网绑定。
- Article 38—44 保持零资产。
