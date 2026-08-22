# Article 01｜模型调用到底发生了什么：LLM、Model API、Messages 与 Token

- Canonical ID：`01`
- Canonical Title：`模型调用到底发生了什么：LLM、Model API、Messages 与 Token`
- Workspace：`01-model-api-messages-token`
- Part：`Part I｜从 LLM 到可编程模型`
- Course Weight：`M（Standard Core Lesson）`
- Optional：`No`
- Lifecycle Status：`PUBLISHED`
- Evidence Status：`CONFIRMED`（`11 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 1 PROPOSAL`）
- Required Lab：`NONE`
- Lab Status：`N/A`
- Current Gate：`END_ARTICLE / CHECKPOINT_VERIFIED`
- Post-publication Review：`01-IR-F01 CLOSED`（历史记录）；`01-IR-F02 CLOSED / PASS`（Anthropic generic / top-level baseline 与 model-specific mid-conversation `role: system` 例外）
- Next Allowed Action：`NONE / GLOBAL_POINTER_OWNS_CURRENT_COURSE_TRANSACTION`
- Blocker：`NONE`
- Published Content：`content/ai-empowerment/agent-engineering-01-model-api-messages-token.md`
- Published Route：`/ai-empowerment/agent-engineering-01-model-api-messages-token/`
- Build Verification：`hugo --gc --minify`，Hugo `0.157.0`，`1230 Pages / 0 ERROR / 0 WARNING`
- Post-hotfix Build Verification：`hugo --gc --minify`，Hugo `0.157.0`，`1230 Pages / 0 ERROR / 0 WARNING`
- `01-IR-F02` Build Verification：`hugo --gc --minify`，Hugo `0.157.0`，`1230 Pages / 0 ERROR / 0 WARNING`，exit code `0`

## 本篇职责

建立一次 Single Model Call 的最小工程心智模型，使读者能把 Application、SDK / HTTP Client、Provider API Contract、Structured Input、Model、Response 和 Application Handling 分层理解。

本篇不把课程候选模型写成某一家 Provider 的唯一内部实现，也不研究 Provider 未公开的服务端执行架构。

## 生产资产

- [Article Card](article-card.md)：课程位置、Reader Promise、概念边界、示例职责和职业能力目标。
- [Research Record](research.md)：11 个 RQ 均已由一手官方资料回答。
- [Evidence Register](evidence.md)：12 个 Claim 与 12 张 Evidence Cards；无 `BLOCKED`。
- [Detailed Outline](outline.md)：Teaching Spine、Claim Coverage、图、示例、Learning Check 与课程边界。
- [Final Draft](draft.md)：A5 修订后的冻结正文；与 Published Body 语义一致。
- [Review Record](review.md)：A1—A5 Gate、首轮 Findings、修订处置与最终 `92 / 100` 评分。
- [Review Record](review.md) 还保留 `01-IR-F01` post-publication Independent Review Hotfix；原 Formal Review 历史未删除或重写。
- [Review Record](review.md) 新增并闭合 `01-IR-F02` post-publication hotfix；fresh Reviewer 完成内容复核后，依据 Master 的 Hugo Build PASS 将 Finding 标记为 `CLOSED / PASS`。

本篇未创建 `assets/` 或 Lab，也未执行 API 实验；官方 contract 已足以支撑核心 Claim。

## A1 Gate

- [x] Article 00 已为 `PUBLISHED`
- [x] Article 01 Workspace 已实例化
- [x] Article Card 完整
- [x] Research Questions 完整
- [x] Claim Skeleton 与 Evidence Plan 已建立
- [x] Article 02 / 03 / 04 / 08 边界明确
- [x] Job Competency Mapping 已建立
- [x] 未创建 Draft、Lab 或 Research Conclusion

## A2 Evidence Gate

- [x] 11 个 RQ 已获得一手官方资料回答
- [x] 12 个 Claim 均有 Evidence Card
- [x] 核心 Claim 无 `BLOCKED`
- [x] OpenAI 主示例已由 Anthropic / Google 定向反查
- [x] `Messages != Memory`、`Token != Character`、Role 差异与 Failure Layer 已明确证明边界
- [x] Lab 继续为 `N/A`

## A3 Outline Gate

- [x] 12 个 Claim 全部进入 Outline，0 个 `BLOCKED`
- [x] 主线遵循问题空间 → 抽象模型 → C# / HTTP 具体落地 → 工程边界
- [x] 两张图、三个代码职责与 Learning Check 已冻结
- [x] Provider-specific 字段与可迁移职责明确分开
- [x] Article 02 / 03 / 04 / 08 / 14—15 / 20—21 边界未越界
- [x] Lab 继续为 `N/A`

## A4 Draft Gate

- [x] `draft.md` 仅在 DRAFTING 阶段创建
- [x] 正文围绕 Single Model Call，没有写成 Provider SDK 教程
- [x] 两个长期心智模型与 `Messages != Memory` 预埋均已兑现
- [x] C# / raw HTTP / response envelope 示例均服务于分层理解
- [x] 六项 Job Competency 均有正文解释与 Learning Check
- [x] 版本敏感事实标明核对日期，核心结论未超出 Evidence
- [x] Lab 继续为 `N/A`

## A5 Formal Review Gate

- [x] 第一轮 Findings 在修订前完成记录
- [x] `BLOCKER = 0`，`MAJOR = 0`
- [x] 3 个 `MINOR` 与 1 个 `EDITORIAL` Finding 全部关闭
- [x] Technical / Evidence / Course Review 均为 `PASS`
- [x] Final Score：`92 / 100`，所有分项满足阈值
- [x] 正文知识内容进入 `FINAL` 并冻结

## A6 Publish Gate

- [x] Front matter、slug、series、primary_series、series_order 与 weight 符合仓库规范
- [x] FINAL Draft 与 Published Body 机械归一后 `Semantic Change = 0`
- [x] 12 个一手官方外链均可访问，仍支持正文 Claim
- [x] 系列索引自动收录 Article 00 / 01，顺序正确；Article 00 页面本身没有自动 Next，因此只把原有“下一篇”文字改为 Article 01 `relref`
- [x] 生成 HTML：H1 = 1、H2 = 9、table = 3、pre = 9、C# highlighting = true、TOC = true
- [x] 静态响应式检查：站点存在 `720px / 980px` 窄屏规则；代码块 CSS 使用 `overflow-x: auto`，宽内容可横向滚动
- [x] 可视验证边界已记录：本机 browser trusted-RPC 依赖未能建立连接，因此没有完成窄屏截图，也未把静态 CSS 检查记成真机 / 截图验收
- [x] `hugo --gc --minify`：`1230 Pages / 0 ERROR / 0 WARNING`
- [x] canonical、status 与课程入口已回写

## Stop Line

Article 01 保持 `PUBLISHED`；Foundation publication 与两次 post-publication hotfix 均已由 Git history 保存，最新 hotfix checkpoint=`798443c1d41f03960253b1190fcbc91425d4f285`。本 workspace 不再路由后续课程 transaction；当前课程对象与下一动作只由 global run state 决定。
