# Article Card｜07 MCP 与外部能力边界

> 来源基线：`docs/agent-engineering-series-plan.md` 与 `docs/agent-engineering-course-plan-v3.1-review.md` 的 Article 07 frozen section。本文件只机械实例化既有课程职责，不预设 Research / Evidence 结论。

## Canonical identity

- Title：`MCP 与外部能力边界：协议解决什么，宿主仍需解决什么`
- Part：`Part II｜从模型到 Agent`
- Weight：`M（Standard Core Lesson）`
- Optional：`No`
- Mode：`NORMAL_ARTICLE`
- Required Lab：`NONE`

## Positioning

协议映射篇。把 MCP 放在 Tool 发现与远程调用层，不把它写成 Agent 或安全系统。

## Reader questions

1. MCP Tool 与本地 ToolDefinition 有何关系？
2. MCP Server 是否决定 Agent 权限？
3. Transport、Capability Discovery 和 Tool Runtime 怎样分层？
4. 远程错误和取消怎样映射？
5. 为什么连接 MCP 不等于完成 Agent？

## Dependencies

- Article 05：Function Calling 与 Tool Use。
- Article 06：Tool Runtime。

## Candidate mental model

```text
Agent Host Tool Runtime
→ MCP Client / Transport
→ MCP Server Tool
→ External System
```

这是 canonical 研究对象，不是已验证的统一实现或安全保证。

## Canonical content spine

1. 远程能力为什么需要标准发现和调用。
2. MCP 对 Capability、Schema、Call、Result 与协议错误负责到哪里。
3. 业务授权、Agent Policy、领域 Validation 与最终审计为何仍属于宿主边界。
4. 本地 Host Policy 与远端 Server Policy 的双层责任。
5. 网络、取消、超时、身份、版本等工程边界，并在停止线处引向 Article 08。

## Evidence requirements

- current MCP official Specification；
- 明确版本日期；
- 最小消息 Trace；
- 对协议责任与 Host / Server 责任的逐项边界和反证搜索。

## Explicit non-goals

- 不搭建 production MCP Server。
- 不穷举所有 Transport、Resources 或其他协议类型。
- 不把 MCP 写成 Agent、权限系统、Sandbox、Trace 或 Eval 的替代品。
- 不进入 Article 08 的 Agent Loop。
- 不读取 DeepSeek Harness 源码，不实现 BuildPilot Runtime。
