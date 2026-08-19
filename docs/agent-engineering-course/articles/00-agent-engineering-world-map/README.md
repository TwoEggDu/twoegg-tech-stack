# Article 00｜Agent Engineering 世界地图

- Canonical ID：`00`
- Canonical Title：`Agent Engineering 世界地图：从 Model、Agent 到 Harness / Host`
- Workspace：`00-agent-engineering-world-map`
- Current Lifecycle：`PUBLISHED`
- Evidence Status：`PARTIAL`（`7 CONFIRMED / 1 PARTIAL / 0 BLOCKED / 6 PROPOSAL`）
- Required Lab：`NONE`
- Current Gate：`M5 Publish PASSED`
- Published Content：`content/ai-empowerment/agent-engineering-00-agent-engineering-world-map.md`
- Published Route：`/ai-empowerment/agent-engineering-00-agent-engineering-world-map/`
- Series Entry：`content/ai-empowerment/agent-engineering-series-index.md`
- Completed Research：`RQ-01` 至 `RQ-09` 已完成 Evidence-first Research 与 M1 自检
- Completed Outline：6 个主体 Section、2 张计划图、5 道 Learning Check、14 项 Claim Coverage 已通过 M2 自检
- Completed Draft：6 个主体 Section、2 张首版 ASCII 图、5 道 Learning Check 已完成 M3 Draft Readiness 自检
- Completed Formal Review：Technical `PASS`、Evidence `PASS_WITH_NOTES`、Course `PASS`、Reader Value `PASS`；总分 `92 / 100`
- Completed Human Review Fix：`HR-F01 / HR-F02 RESOLVED`；`New Core Claims = 0`；Lifecycle 已按 `FINAL -> REVIEW -> FINAL` 完成定向校正
- Completed Publish：Front Matter、Series Navigation、外链格式、正文语义差异与本地渲染结构已验证；`hugo --gc --minify` 为 `1229 Pages / 0 ERROR`
- Remaining Blockers：`NONE`；`00-C06a` 继续保持 `PARTIAL`，是发布时必须保留的证据边界而非阻塞项
- Next Allowed Action：`Article 01｜Production Kickoff`（不自动执行，不创建 workspace）

## 工作区资产

- [Article Card](article-card.md)：来自 v3.1 的课程职责基线。
- [Research Conclusion Index](research.md)：9 个 RQ 的状态、主发现、Claim 映射与剩余不确定性。
- [Evidence Register](evidence.md)：14 个 Claim 与 15 张 Evidence Cards。
- [Definition Matrix](definition-matrix.md)：区分稳定抽象、课程工作定义、产品术语与生态依赖术语。
- [Detailed Outline](outline.md)：教学主线、Section 设计、图表、Learning Check、Claim Coverage 与停止线。
- [Draft](draft.md)：M4.1 通过后的 FINAL 内容来源，发布后继续保留用于 Research / Evidence / Review 追溯。
- [Review Record](review.md)：M1—M3 自检历史、M4 Formal Review，以及 M4.1 Human Independent Review Fix 与 Final Gate 记录。

当前没有 `assets/`；两张首版图以 ASCII 形式直接进入 `draft.md`，本轮没有生成最终图片、实验或运行产物。

## M1 结论边界

- Agent 有可迁移的稳定工程核心，但 Article 00 的表述仍是导航级 working definition。
- Copilot 是产品术语；Agentic 是生态依赖的描述词；二者都不是固定架构层。
- Product / Application 表示面向用户的软件边界；一个 Product 可提供多个 Surface / Entry Point。Host 是本课程对承载或集成 Agent 执行的宿主程序、进程或运行环境的工作定义；Surface 不等于 Host，映射需要独立实现证据。Agent Runtime 与 Harness 同样是课程工作定义，不宣称所有 Agent 产品都按此部署。
- Claude Code、Codex、DeepSeek Harness 只作为公开产品事实例子，不用于反推内部实现。

## Stop Line

M5 已停止。不得在本次任务中开始 Article 01、创建 Article 01 workspace、启动 Lab、DSH 源码专题或 BuildPilot 设计；下一步只能在单独任务中进入 Article 01 Production Kickoff。
