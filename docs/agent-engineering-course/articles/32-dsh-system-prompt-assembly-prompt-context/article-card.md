# Article 32 Card｜System Prompt Assembly 与 PromptContext：多来源 Context 怎样组成

## Problem Space

系统提示词不是一段静态字符串。身份、宿主、任务、变量、Tool guidance、动态状态与历史可能由多个 contributor 在不同 scope 和 Step 注入；若不保留顺序、来源、变换与冲突收据，就无法解释模型最终看到了什么。

## Required Questions

1. pinned Section、PromptContext、Provider 的真实字段、符号和调用路径是什么？
2. contributor 如何按顺序、scope 与终结语义参与 assembly？
3. Stable 与 Dynamic Context 如何分离，两个 Step 的 request 为什么不同？
4. duplicate section、覆盖与 bad variable 的真实行为是什么？
5. Effective Assembly / Context Snapshot 如何保留 provenance 与 transformation？
6. 当前版本是否会在 compaction 后重注入不变量？
7. BuildPilot 为什么只能把 `IContextContributor + Receipt` 作为候选设计？

## Boundaries

- 不把 source path 写成 runtime request receipt。
- 不把 mock/provider fixture 写成 real model/provider call。
- 不发明当前版本不存在的 compaction re-injection。
- 不启动 Article 33—44。
