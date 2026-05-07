# Agent Compatibility for TechStackShow Harness

> 这份文件回答的是：同一套内容生产 Harness 如何在 Codex、Claude、DeepSeek 等不同 Agent 之间复用。

## 1. 核心判断

可以跨 Agent 通用，但必须分清两层：

- Harness Core：任务边界、上下文入口、阶段流程、产物格式、验证口径
- Agent Adapter：某个 Agent 具体怎样读取文件、调用工具、运行命令、应用阶段提示词

Core 写在 Markdown 里，保持可读、可复制、可审查。

Adapter 只处理不同 Agent 的能力差异，不能反过来改变 Core。

## 2. 可移植内容

以下内容应保持跨 Agent 通用：

- `AGENTS.md` 中的项目规则
- `.agents/harness/content-production.md` 中的流程
- `docs/article-production-workflow.md`
- `docs/article-writing-method.md`
- `docs/*-series-plan.md`
- `.agents/skills/*/SKILL.md` 中的阶段提示词

这些文件都应被视为普通 Markdown，而不是某个工具专属配置。

## 3. 能力矩阵

| 能力 | Codex | Claude | DeepSeek | 降级策略 |
|------|-------|--------|----------|----------|
| 读取仓库文件 | 通常可用 | 取决于 Claude Code / IDE | 取决于 IDE / Web UI | 让作者粘贴必要上下文 |
| 搜索仓库 | 通常可用 | 取决于工具环境 | 取决于工具环境 | 使用作者提供的文件列表 |
| 修改文件 | 通常可用 | 取决于 Claude Code / IDE | 取决于 IDE | 输出 patch 或明确改写片段 |
| 运行 `hugo` | 通常可用 | 取决于本地 shell | 通常不稳定 | 输出待运行命令，由作者执行 |
| 使用 `col-*` skills | 原生可用 | 不原生 | 不原生 | 直接读取对应 `SKILL.md` 当 prompt |
| 长上下文 | 较强 | 较强 | 取决于模型版本 | 使用阶段产物和 `Harness State` 压缩交接 |

结论：

跨 Agent 的关键不是让每个 Agent 有同样工具，而是让它们共享同一套输入、输出和停止点。

## 4. Stage Prompt 映射

不同 Agent 不需要理解 Codex skill 机制，只要按阶段读取对应文件即可。

| Harness 阶段 | Codex 用法 | Claude / DeepSeek 用法 |
|-------------|------------|------------------------|
| 编辑定位 | 使用 `col-editor` | 读取 `.agents/skills/col-editor/SKILL.md`，按其格式输出 |
| 技术深挖 | 使用 `col-drilldown` | 读取 `.agents/skills/col-drilldown/SKILL.md`，按七层结构输出 |
| 验证设计 | 使用 `col-verify` | 读取 `.agents/skills/col-verify/SKILL.md`，只输出实验设计 |
| 初稿骨架 | 使用 `col-draft` | 读取 `.agents/skills/col-draft/SKILL.md`，不写最终正文 |
| 风险审查 | 使用 `col-risk` | 读取 `.agents/skills/col-risk/SKILL.md`，只列高风险句 |
| 解释桥接 | 使用 `col-bridge` | 读取 `.agents/skills/col-bridge/SKILL.md`，只列卡点和补桥方案 |
| 系列一致性 | 使用 `col-consistency` | 读取 `.agents/skills/col-consistency/SKILL.md`，只做一致性审查 |
| 最小改写 | 使用 `col-rewrite` | 读取 `.agents/skills/col-rewrite/SKILL.md`，只改确认的问题 |

## 5. 通用启动 Prompt

当使用不理解本仓库 skill 机制的 Agent 时，先给它这段启动语：

```text
你正在参与 TechStackShow 内容生产 Harness。

请先读取或使用我提供的以下上下文：
1. 项目规则：AGENTS.md
2. Harness 主流程：.agents/harness/content-production.md
3. 当前阶段对应的 SKILL.md
4. 相关 series plan
5. 已有文章或执行层材料

你必须遵守：
- 只执行当前阶段，不越级写最终正文
- 输出必须符合当前阶段的格式
- 不确定事实标注 [待核查]
- 需要真实数据使用 DATA-TODO
- 需要项目经验使用 EXPERIENCE-TODO
- 每次输出末尾附 Harness State
```

如果 Agent 不能直接读文件，把上述文件中相关片段粘贴给它。

## 6. 交接协议

跨 Agent 切换时，不要只粘贴上一轮聊天记录。优先粘贴最后一个 `Harness State`，再补必要阶段产物。

标准格式：

```text
Harness State
- Task: [当前文章或任务]
- Stage: [当前阶段]
- Context read: [已读取的关键文档]
- Decisions: [已经确认的边界和判断]
- Open questions: [需要作者或下一个 agent 确认的问题]
- Next stage: [建议下一步]
- Changed files: [已修改文件，没有则写 none]
- Verification: [已运行检查，没有则写 not run + 原因]
```

最小交接包：

- `Harness State`
- 编辑工作单
- 深挖分析稿
- 验证设计稿
- 当前文章草稿或骨架
- 已确认的问题清单

## 7. 不同 Agent 的使用建议

### 7.1 Codex

适合：

- 直接改仓库文件
- 跑本地 `hugo`
- 串联多个阶段
- 把反馈沉淀回 Markdown 规则

执行要求：

- 优先使用本仓库 `.agents/skills/col-*`
- 文件修改后检查 `git diff`
- 涉及 `content/` 时运行 `hugo`

### 7.2 Claude

适合：

- 长文结构判断
- 大段初稿审查
- 复杂边界讨论
- 多轮写作推敲

执行要求：

- 如果使用 Claude Code，让它读取 Harness Core 和对应 `SKILL.md`
- 如果只在 Web UI 使用，粘贴阶段输入和对应 `SKILL.md` 的输出格式
- 要求它输出 `Harness State`，方便回到 Codex 落文件和验证

### 7.3 DeepSeek

适合：

- 中文表达对照
- 快速生成备选结构
- 局部段落改写
- 低成本多方案发散

执行要求：

- 不让它直接决定系列结构
- 不让它绕过风险审查生成最终稿
- 让它输出可审查的候选，而不是直接替换正文
- 事实、版本、平台相关结论默认标注 `[待核查]`

## 8. 反模式

以下做法会破坏跨 Agent 通用性：

- 把核心流程写成某个产品专属命令
- 只在聊天记录里保存决策，不回写 Markdown
- 让不同 Agent 各自发明文章结构
- 没有 `Harness State` 就中途切换 Agent
- 用 DeepSeek / Claude 的流畅改写覆盖已经确认的工程边界
- 用 Codex 的文件操作便利性跳过人工确认点

## 9. 最短结论

`Harness 要跨 Agent 通用，核心就必须是 Markdown 规程和阶段产物；工具、skill、命令只是适配层。`
