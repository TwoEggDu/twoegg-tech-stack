# Article 03｜Structured Output：让模型输出成为机器可消费的合同

- Canonical ID：`03`
- Canonical Title：`Structured Output：让模型输出成为机器可消费的合同`
- Workspace：`03-structured-output-machine-contract`
- Part：`Part I｜从 LLM 到可编程模型`
- Course Weight：`L（Major Core Lesson）`
- Optional：`No`
- Mode：`LAB_ARTICLE`
- Lifecycle Status：`PUBLISHED`
- Evidence Status：`CONFIRMED`（`7 CONFIRMED / 0 PARTIAL / 0 BLOCKED`）
- Required Lab：`Lab 01｜Structured Output`
- Lab Status：`CONFIRMED / EVIDENCE_MERGED`
- Current Gate：`GIT_DIFF_VERIFY`
- Next Allowed Action：`ARTICLE_CHECKPOINT_COMMIT_AFTER_DIFF_VERIFY`
- Blocker：`NONE`
- Published Content：`content/ai-empowerment/agent-engineering-03-structured-output-machine-contract.md`
- Publisher Result：`PASS`
- Build Result：`PASS`；Hugo `0.157.0`；`1232 Pages / 0 ERROR / 0 WARNING`；process `exit 0`
- Future Series Metadata：`series_order: 40`；`weight: 3040`

## 本篇职责

把 Article 02 的自然语言 Output Requirement 推进为机器可消费的 Structured Output contract，研究 JSON Schema、Typed DTO、Parse、Schema Validation、Domain Validation、Refusal、Truncation 与 Repair 的边界。

本篇不预设 Provider 行为结论，不把合法 JSON、Schema Valid 或 Structured Result 写成事实正确、Evidence 已验证或 Tool 已执行。

## 生产资产

- [Article Card](article-card.md)：canonical 定位、依赖、边界、Lab 路由与候选学习问题。
- [Research](research.md)：Research Questions、Lab Design、Evidence Merge 与收窄后的本地结论已完成；`7 / 7` Claim 已进入 Review 输入。
- [Evidence](evidence.md)：`7 CONFIRMED / 0 PARTIAL / 0 BLOCKED`；Evidence Gate `PASS`。
- [Lab 01 Design / Observation](../../labs/lab-01-structured-output-validation/README.md)：`NJsonSchema 11.6.1`、Draft 4 最小关键词子集、八类 deterministic candidate inputs、冻结 Expected、实际 build/tests、双运行 JSONL、hash 与 Evidence interpretation 均已保存。
- [Outline](outline.md)：Outline Gate `PASS`；`7 / 7` Claim 已映射，未引入新核心事实。
- [Draft](draft.md)：Draft Gate `PASS`；约 `6,681` 字，`7 / 7` Claim coverage，new core facts=`0`。
- [Review](review.md)：首审 `90 / 100 / REVISION_REQUIRED`；Cycle 1 已关闭 `03-F01`—`03-F03`，Final Gate `PASS / 93`。

当前 Findings（仅 Reviewer 可关闭）：

- `03-F01`：`CLOSED`
- `03-F02`：`CLOSED`
- `03-F03`：`CLOSED`

`outline.md` 与 `draft.md` 只在 Lab 执行、Observation、Evidence Merge 和 Evidence Gate 全部通过后创建。Lab 01 也不在 WORKSPACE_INIT 阶段提前创建；Researcher 必须先完成 Preliminary Evidence 并冻结 Lab Design。

## Publication Evidence

- Published Path：`content/ai-empowerment/agent-engineering-03-structured-output-machine-contract.md`
- Published Route：`/ai-empowerment/agent-engineering-03-structured-output-machine-contract/`
- Canonical URL Candidate：`https://twoeggdu.github.io/twoegg-tech-stack/ai-empowerment/agent-engineering-03-structured-output-machine-contract/`
- Front Matter：`PASS`；canonical title、slug、calendar date `2026-08-20`、`draft: false`、四个冻结 tags、series、`primary_series`、`series_order: 40` 与 `weight: 3040` 均符合 publication metadata。日期写为 `2026-08-20T00:00:00+08:00`，以保持同一上海日历日期并避免 Hugo 在本地 `02:08 +08` 把无时区的午夜解释为尚未到达的 `00:00Z` future content。
- Series / Internal Links：`PASS`；Article 03 顶部“上一篇”指向 Article 02，Article 02 既有“机器可消费输出的完整合同属于下一篇”桥接已机械加入 Article 03 `relref`；两个 shortcode 均使用 ASCII 双引号。自动 series directory 按 `series_order` 渲染 00、01、02、03。
- Semantic Diff：`PASS`；移除 Draft H1，并忽略 Published front matter 与顶部 previous navigation 后，Published knowledge body 与 FINAL Draft 为逐行 exact match（`310` 行、`14,857` characters），知识内容变化=`0`。
- Lab Evidence Links：`4 / 4` absolute GitHub blob URLs 原样保留；URL owner / repository / `main` identity 与四个 suffix 均已核对，suffix 分别对应当前 transaction 内真实存在的 Lab `README.md`、execution log 与两份 JSONL。Rendered HTML 保留四个目标，legacy `../../labs/` link=`0`。
- Remote Accessibility：`UNVERIFIED_PRE_PUSH / repository targets verified`。受限网络首次请求返回 connection failure；按权限流程重试后四个 GitHub URL 当前均为 HTTP `404`，与同一 Article transaction 尚未 commit / push 到 `main` 的边界一致。Publisher 没有 push，也不把本地 target 存在写成远端已可访问；Master checkpoint 后仍需由部署 / 远端流程确认实际可达性。
- Build Command：`hugo --gc --minify`
- Build Evidence：首次真实 build 发现 plain `date: "2026-08-20"` 在当前时钟被 Hugo 解析为 future content，导致 Article 02 → 03 `REF_NOT_FOUND`，process `exit 1`；只把 publication timestamp 明确为同一上海日历日后重跑，Hugo `v0.157.0-7747abbb316b03c8f353fd3be62d5011fa883ee6+extended windows/amd64`，`1232 Pages`，`0 WARNING`，`0 ERROR`，process `exit 0`，总耗时 `5907 ms`；Publication Evidence 落盘后的 final reverify 同为 `1232 / 0 / 0 / exit 0`，总耗时 `5910 ms`。
- Master Pre-commit Reverification：Master 完成 canonical / global / Lifecycle reconciliation 后再次执行 `hugo --gc --minify`；Hugo `0.157.0`，`1232 Pages / 0 ERROR / 0 WARNING`，process `exit 0`，总耗时 `5878 ms`。
- Route / Series Render：`PASS`；Article 03 route、Article 03 → 02 previous href、Article 02 → 03 next href、Article 03 标题 / 最短结论 / 四个 Lab href 与自动 series directory 均已在 `public/` 产物中定位。
- Publisher Boundary：Lifecycle 保持 `FINAL`；Publisher 未修改 canonical、`status.md`、`course-run-state.md`、course README、Research、Evidence、Outline、Draft、Review 或 Lab，未 commit / push。`PUBLISHED`、global state 与 canonical publication link 只能由 Master reconciliation 写入。

## PRECHECK Result

- [x] Article 01、Article 02 均为 `PUBLISHED`
- [x] Article 02 checkpoint commit `b359a329df02ce7487b0cb1a9feaad66c886d4dc` 已完成 message、scope 与 clean-tree verification
- [x] Article 03 canonical entry、L 级权重、非 Optional 与 Lab 01 路由已确认
- [x] Mode = `LAB_ARTICLE`
- [x] Article workspace、Published Content 与 Lab 01 workspace 在 PRECHECK 前均不存在
- [x] 本机 `.NET SDK 10.0.301` 可用；这只证明本地 Lab runtime candidate 可用，不代表 Lab Design 或运行已通过

## WORKSPACE_INIT Result

- [x] 已执行显式 `ARTICLE_KICKOFF`
- [x] 只创建 Article Card、Research、Evidence、Review skeleton
- [x] 未写 Research Answer、Evidence Conclusion、Lab Hypothesis / Acceptance Criteria、Outline、Draft 或 Published Content
- [x] 未创建 `labs/lab-01-structured-output-validation/`

## Stop Line

`PRECHECK / ARTICLE_KICKOFF / WORKSPACE_INIT / RESEARCH / PRELIMINARY_EVIDENCE / LAB_DESIGN / LAB_EXECUTE / LAB_OBSERVATION / EVIDENCE_MERGE / EVIDENCE_GATE / OUTLINE / AUTHOR_DRAFT / REVIEW / REVISION / REVIEW_RECHECK / FINAL_GATE / PUBLISHER / BUILD_VERIFY / MASTER_STATE_RECONCILIATION` 已完成，Lifecycle 为 `PUBLISHED`。下一动作只能由 Master 执行 Article 03 `GIT_DIFF_VERIFY -> ARTICLE_CHECKPOINT_COMMIT -> ARTICLE_COMMIT_VERIFY`；独立 checkpoint 未验证前不得启动 Article 04。
