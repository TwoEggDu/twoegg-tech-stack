# Article 29 Card｜DeepSeek Harness 总图：从 Host 启动到一次 Agent Run

## Identity

- Canonical ID: `29`
- Part: `VI｜DeepSeek Harness`
- Weight: `M`
- Optional: `NO`
- Required Evidence Work: `SOURCE MAP + HOST_TO_AGENT_RUN PATH + BOUNDED TRACE`
- Article Type: `ARCHITECTURE_MAP / SOURCE_TRACE`
- Mode: `DSH_SOURCE_MODE`

## Problem Space

一张目录图无法说明 DSH 怎样从支持的 Host/profile 入口进入 Agent、Session、Turn 与终态。本篇以固定 commit 的 owner、symbol、call path 和 bounded Trace 校正初始假设，并把未观测段保留为缺口。

## Core Questions

1. Repository Map 与 core package relationships 怎样由实际依赖和注册关系建立？
2. 支持的 CLI/profile/boot 入口怎样进入 headless runner 与 Agent？
3. Agent、Session、Inbox、Turn、Step、Model/Tool/Session Event 与终态各由谁拥有？
4. 无真实 credential 时，哪些路径仍可通过 source/tests/fixture 验证，哪些必须保持 runtime gap？
5. Article 30—37 的专题边界怎样从总图路由而不提前得出结论？

## Frozen Boundaries

- 不用文件夹名、README 图或 package dependency 补齐 lifecycle。
- official test Trace、mock/fake Provider 与 supported Host runtime 分层记录。
- 不使用真实生产凭证、生产输入或公网绑定。
- 不启动 Article 30 或 Article 38—44。
