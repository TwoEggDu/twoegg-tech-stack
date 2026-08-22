# Article 02｜Prompt Engineering：任务合同、角色、示例与边界

- Canonical ID：`02`
- Canonical Title：`Prompt Engineering：任务合同、角色、示例与边界`
- Workspace：`02-prompt-engineering-contract-boundaries`
- Part：`Part I｜从 LLM 到可编程模型`
- Course Weight：`M（Standard Core Lesson）`
- Optional：`No`
- Lifecycle Status：`PUBLISHED`
- Evidence Status：`PARTIAL`（`7 CONFIRMED / 1 PARTIAL / 0 BLOCKED / 1 PROPOSAL`）
- Required Lab：`NONE`
- Lab Status：`N/A`
- Current Gate：`END_ARTICLE / CHECKPOINT_VERIFIED`
- Next Allowed Action：`NONE / GLOBAL_POINTER_OWNS_CURRENT_COURSE_TRANSACTION`
- Review Findings：`02-F01 CLOSED`、`02-F02 CLOSED`；Unclosed Findings = `NONE`
- Evidence Constraints：`02-C04` 只能使用收窄措辞，`02-C09` 必须保持 `NOT_EXECUTED / PROPOSAL`
- Published Content：`content/ai-empowerment/agent-engineering-02-prompt-engineering-contract-boundaries.md`
- Publisher Result：`PASS`
- Build Result：`PASS`；Hugo `0.157.0`；`1231 Pages / 0 ERROR / 0 WARNING`；process `exit 0`

## 本篇职责

按照 canonical 与 v3.1 结构基线，本篇负责研究 Prompt 怎样表达任务合同，以及任务、稳定指令、动态输入、示例、输出要求与失败语义的边界。

本篇不预设最终研究结论，不把 Prompt 写成权限、状态、事实校验、Context Engineering、RAG、Memory、Structured Output 或 Eval 的替代品。

## 生产资产

- [Article Card](article-card.md)：课程位置、研究职责、边界与预期学习结果。
- [Research](research.md)：Research Questions、当前一手来源与 Research Answers；当前为 `COMPLETE`。
- [Evidence](evidence.md)：9 个 Claim 的证据卡与 Gate 结论；当前为 `7 CONFIRMED / 1 PARTIAL / 0 BLOCKED / 1 PROPOSAL`。
- [Outline](outline.md)：已完成的 M 级原理篇结构、Claim coverage 与课程 Stop Lines。
- [Draft](draft.md)：已完成的 Article 02 FINAL Draft；已机械映射为 Hugo publication candidate。
- [Review](review.md)：首轮独立 Review 为 `FAIL`；完成一次 Revision / Recheck 后，`02-F01`、`02-F02` 均已关闭，Final Gate 为 `PASS`（`92 / 100`）。

本篇没有 required Lab，因此未创建 `assets/`。Reviewer 已关闭全部 Finding 并给出 Final Gate `PASS`；Publisher 已完成机械映射、internal link、front matter、series metadata、semantic diff 与 Hugo build 验证；Master reconciliation 已核对 Reviewer PASS、Publisher PASS、Build PASS、published path 与 canonical candidate，Lifecycle 进入 `PUBLISHED`。

## Publication Evidence

- Published Path：`content/ai-empowerment/agent-engineering-02-prompt-engineering-contract-boundaries.md`
- Published Route：`/ai-empowerment/agent-engineering-02-prompt-engineering-contract-boundaries/`
- Canonical URL Candidate：`https://twoeggdu.github.io/twoegg-tech-stack/ai-empowerment/agent-engineering-02-prompt-engineering-contract-boundaries/`
- Front Matter：`PASS`；canonical title、slug、date、`draft: false`、tags、series、`primary_series`、`series_order: 30`、`weight: 3030` 均符合冻结 metadata。
- Series / Internal Links：`PASS`；Article 02 顶部“上一篇”指向 Article 01，Article 01 的既有“下一篇”桥接指向 Article 02；两个 `relref` 均使用 ASCII 双引号。
- Semantic Diff：`PASS`；去除 Draft H1、Published front matter 与课程 internal links 后，Published body 与 FINAL Draft 逐行一致，没有知识内容变化。
- Build Command：`hugo --gc --minify`
- Build Evidence：Hugo `v0.157.0-7747abbb316b03c8f353fd3be62d5011fa883ee6+extended windows/amd64`；`1231 Pages`；`0 WARNING`；`0 ERROR`；process `exit 0`；总耗时 `5875 ms`。
- Master Pre-commit Reverification：Master reconciliation 后再次执行 `hugo --gc --minify`；Hugo `0.157.0`；`1231 Pages / 0 ERROR / 0 WARNING`；process `exit 0`；总耗时 `5932 ms`。
- Route / Series Render：`PASS`；生成页面、Article 01 → 02 导航、Article 02 → 01 导航与自动 series directory 均可在 `public/` 产物中定位。
- Publisher Boundary：Publisher 交付时 Lifecycle 保持 `FINAL`，且没有修改 canonical、`status.md`、`course-run-state.md`、course README、Research、Evidence、Outline、Draft 或 Review；后续 `PUBLISHED` 与 durable state 由 Master reconciliation 写入。

## WORKSPACE_INIT Result

- [x] Article 01 已为 `PUBLISHED`
- [x] Article 02 canonical entry 已冻结
- [x] Mode = `NORMAL_ARTICLE`
- [x] Required Lab = `NONE`
- [x] Workspace 与 Published Content 在 PRECHECK 前均不存在
- [x] 只创建 `PLANNED` 阶段允许的 skeleton
- [x] 未写 Research Answer、Evidence Conclusion、Outline、Draft 或 Review Finding

## Stop Line

Article 02 已完成全部 production Gate、独立 completion commit 与 commit verification；checkpoint=`b359a329df02ce7487b0cb1a9feaad66c886d4dc`，Lifecycle=`PUBLISHED`。本 workspace 不再路由 Article 03 或任何后续 transaction；当前课程对象与下一动作只由 global run state 决定。
