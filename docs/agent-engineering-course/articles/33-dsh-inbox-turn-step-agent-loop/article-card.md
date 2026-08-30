# Article 33 Card｜Inbox、Turn、Step 与 Agent Loop

## Problem Space

一次 Agent Run 不是“模型回复一次”这么简单。Host 输入怎样进入 Inbox/event，Runtime 怎样划分 Turn 与 Step，Tool Batch 怎样执行并回到模型，以及 Policy、Budget、Error、Cancellation 最终由谁决定继续或停止，必须由源码路径和运行 Trace 共同解释。

## Required Questions

1. Host 写入 Inbox / event 的入口在哪里？
2. Runtime 如何形成 Turn，Turn 与 Step 的边界是什么？
3. Step 的 assembly、model call、parse 与 event 生命周期怎样闭合？
4. Tool batch 的并发/顺序与结果汇总语义是什么？
5. Continue / Stop 的决策权在哪里，Stop 是否等于 Success？
6. Policy、Budget、Error 与 Cancellation 怎样影响停止？
7. Cancellation signal 怎样穿过 Loop？
8. 四条 required Trace 分别证明什么、不能证明什么？

## Boundaries

- Inbox 不等于 Chat UI。
- Turn 不等于 Step。
- Tool Batch 不等于 Multi-Agent。
- Stop 不等于 Success。
- mock/test trace 不升级为真实 provider runtime。
- BuildPilot implication 仅为 Part VII 输入，不启动 Part VII。
