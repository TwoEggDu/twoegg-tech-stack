# Evidence Card 模板

## 通用字段

```markdown
### Evidence <article-id>-E<nn>｜<short name>

- Article: `<ID and title>`
- Claim ID: `<article-id>-C<nn>`
- Claim: `<可以被证实、证伪或明确标为设计选择的主张>`
- Evidence Status: `CONFIRMED | PARTIAL | BLOCKED | PROPOSAL`
- Evidence Class: `OFFICIAL_DOC | PINNED_SOURCE | RUNTIME_OBSERVATION | EXPERIMENT | INFERENCE | DESIGN_PROPOSAL`
- Source Type: `<specification / paper / official SDK / source / experiment / ADR>`
- Source: `<URL、文档名称或设计文档>`
- Repository: `<repo or N/A>`
- Commit: `<commit/tag or N/A>`
- File: `<path or N/A>`
- Symbol: `<symbol or N/A>`
- Call Path: `<entry -> ... -> target or N/A>`
- Experiment: `<experiment ID or N/A>`
- Fixture: `<fixture and version or N/A>`
- Trace: `<raw trace/log path or N/A>`
- Retrieved / Run At: `<date and timezone>`
- Version Scope: `<适用版本>`
- Reproduction: `<最小复现步骤或 N/A>`
- Observation: `<原始观测，不掺入推断>`
- Counter-evidence Searched: `<搜索过哪些反例、替代解释或冲突来源>`
- Interpretation: `<从观测到主张的推理>`
- Proves: `<该证据足以支持什么>`
- Does Not Prove: `<该证据不能支持什么>`
- Limitations: `<环境、样本、版本和替代解释>`
- Course Usage: `<在哪一节、以何种强度使用>`
- BuildPilot Implication: `ADOPT | SIMPLIFY | REJECT | DEFER | N/A` + `<reason>`
- Owner: `<name>`
- Verified At: `<date>`
```

## Evidence Status

| 状态 | 含义 | 正文规则 |
|---|---|---|
| `CONFIRMED` | 证据对当前版本和措辞提供直接支持 | 可按证明范围陈述 |
| `PARTIAL` | 仅支持主张的一部分，或版本/运行证据不完整 | 必须收窄和标注边界 |
| `BLOCKED` | 缺少关键来源、环境、版本或复现路径 | 行为性主张不得进入正文 |
| `PROPOSAL` | 这是课程设计选择或待实现方案 | 必须使用设计语态 |

## 证据类别说明

- `OFFICIAL_DOC`：官方文档或公开规范。
- `PINNED_SOURCE`：固定到 commit/tag 的源码事实。
- `RUNTIME_OBSERVATION`：可复现的运行时观测。
- `EXPERIMENT`：有 fixture、步骤、输入输出和限制的最小实验。
- `INFERENCE`：由多项证据推导的解释，必须显式标注推断链。
- `DESIGN_PROPOSAL`：接口、状态机或架构选择，不代表已经实现。

## DSH 源码证据扩展

```markdown
- DSH Repository: `<repo>`
- Pinned Revision: `<commit or tag>`
- Source Location: `<file:symbol>`
- Call Path: `<entry -> ... -> target>`
- Run Entry: `<command or scenario>`
- Runtime Trace: `<log/trace path>`
- DSH Verification: `SOURCE_CONFIRMED | RUNTIME_CONFIRMED | PARTIAL | BLOCKED`
- Course Decision: `ADOPT | SIMPLIFY | REJECT | DEFER`
- Decision Rationale: `<为什么这样吸收或不吸收>`
```

`SOURCE_CONFIRMED` 与 `RUNTIME_CONFIRMED` 是两个独立结论。两者只满足其一时，不得用一个替代另一个。
