# Article 06｜Tool Runtime：Validate、Policy、Execute、Result 与 Trace

- Canonical ID：`06`
- Workspace：`06-tool-runtime`
- Part：`Part II｜从模型到 Agent`
- Weight：`L（Major Core Lesson）`
- Optional：`No`
- Mode：`LAB_ARTICLE`
- Lifecycle Status：`PUBLISHED`
- Evidence Status：`PASS / 8 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 1 PROPOSAL`
- Required Lab：`Lab 02 Tool Runtime`
- Lab Status：`CONFIRMED / EVIDENCE_MERGED`
- Current Gate：`GIT_DIFF_VERIFY`
- Next Allowed Action：`MASTER_GIT_DIFF_VERIFY`
- Blocker：`NONE`
- Published Content：`content/ai-empowerment/agent-engineering-06-tool-runtime.md`
- Publisher Result：`PASS`
- Build Result：`PASS`；Hugo `0.157.0`；`1235 Pages / 0 ERROR / 0 WARNING`；process `exit 0`
- Master Reconciliation：`PASS`

## Canonical responsibility

本篇是 Tool Runtime 核心工程篇，只负责建立从模型行动意图到 Host 实际执行之间不可省略的 Canonicalize、Validate、Policy、Execute、Result Validation、Render / Spill 与 Trace 管线。

## Preconditions

- Article 03 `PUBLISHED`：Parse / Schema / DTO / Domain validation boundary 已建立。
- Article 05 `PUBLISHED`：Tool Call intent、correlation 与 Host decision seam 已建立；checkpoint `c0cf180c281ea5dbb70c891176735f4ed9e34d3f` verified。
- Article 06 PRECHECK / ARTICLE_KICKOFF / WORKSPACE_INIT：`PASS`。

## Production assets

- [Article Card](article-card.md)：canonical 问题、依赖、边界与 Lab requirement 的机械实例化。
- [Research](research.md)：Evidence Merge complete；`8 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 1 PROPOSAL`，Provider Calls=`0`，两次local Lab run共28 invocation rows。
- [Evidence](evidence.md)：`9 Claims / 9 Cards`；C05—C09已在fixed Windows/.NET/single-process/no-concurrent-link-mutation scope内升级为`CONFIRMED`；C04继续为课程`PROPOSAL`。
- [Lab 02](../../labs/lab-02-tool-runtime/README.md)：`CONFIRMED / EVIDENCE_MERGED`；built-in-only `net10.0`，每run 12 case groups / 14 rows exact；两份JSONL SHA-256=`50CEA4EC...21BD67`且byte-identical；三次失败历史保留。
- [Outline](outline.md)：Master Outline Gate=`PASS`；`9 / 9 Claims`语义映射，`8 CONFIRMED / 1 PROPOSAL`语态保持，`new core facts=0`，`RETURN_TO_RESEARCH=NONE`。
- [Draft](draft.md)：Master Draft Gate=`PASS`；正文CJK字符`6,692`，`9 / 9 Claims`覆盖，7/7 Evidence官方URL，三次first failure与fixed-scope边界保留。
- [Review](review.md)：Cycle 2 fresh score=`93 / 100`；`06-F01 / 06-F02 CLOSED`，0 unclosed Findings，Review / Final Gate=`PASS`，Lifecycle=`FINAL / NOT PUBLISHED`。
- Published Content：`content/ai-empowerment/agent-engineering-06-tool-runtime.md`；Publisher / Build=`PASS`。

## Publication Evidence

- Published Path：`content/ai-empowerment/agent-engineering-06-tool-runtime.md`
- Published Route：`/ai-empowerment/agent-engineering-06-tool-runtime/`
- Canonical URL Candidate：`https://twoeggdu.github.io/twoegg-tech-stack/ai-empowerment/agent-engineering-06-tool-runtime/`
- Front Matter：`PASS`；Hugo production build 已实际解析 YAML；title 与 canonical exact match；slug=`agent-engineering-06-tool-runtime`；日期=`2026-08-20T00:00:00+08:00`，早于 Publisher 核对时的本地时间 `2026-08-20 09:22 +08:00`，不是 future content；`draft: false`；四个 tags、`series: Agent Engineering`、`primary_series: agent-engineering`、`series_role: article`、`series_order: 70` 与 `weight: 3070` 符合 canonical / series convention。
- Series / Internal Links：`PASS`；Article 06 顶部“上一篇”只指向 Article 05，Article 05 只把既有可见文字 `Article 06` 包成 Article 06 `relref`，没有其他可见 prose 变化。source 与 rendered HTML 中 06 → 05、05 → 06 各一次，06 → 07 为 `0`；shortcode 参数均使用 ASCII 双引号。自动 series directory 按 `series_order` 渲染 00、01、02、03、04、05、06，位置严格递增。
- Semantic Diff：`PASS`；移除 FINAL Draft 唯一 H1，并排除 Published front matter 与顶部 previous navigation 后，Published knowledge body 与 FINAL Draft 逐行、逐字符 exact match：`380` 行、`28,460` characters、UTF-8 `44,000` bytes；双方 SHA-256 均为 `C26E8FC3B0F89682728A23DA9993EE734EDFBEDEAD8B7438955A1D3352B50267`；知识内容变化=`0`。
- Markdown / Official Links：`PASS`；code fence markers=`16` 且成对；trailing whitespace=`0`；FINAL Draft 与 Published Content 的 7 个 unique official URLs（14 occurrences）数量、顺序和字符串 exact match。
- Lab Evidence Links：`PASS`；Published Content 保留 11 个 GitHub evidence blob occurrences / 7 unique targets；7 个 blob suffix 均精确映射到当前 transaction 内存在的本地 Evidence / Lab 文件；legacy `](evidence.md)` / `](../../labs/` occurrences=`0`；rendered HTML 保留 11 个 evidence href。由于本 transaction 按边界不 push，远端 blob 当前可访问性只标 `PRE-PUSH UNVERIFIED`，不冒充已上线证据。
- Build Command：`hugo --gc --minify`
- Build Evidence：首次 sandboxed 启动因现有 `hugo.exe` `拒绝访问`，没有产生有效 build result；按权限流程以同一命令重跑后，Hugo `v0.157.0-7747abbb316b03c8f353fd3be62d5011fa883ee6+extended windows/amd64`；`1235 Pages`；`0 WARNING`；`0 ERROR`；process `exit 0`；Hugo total `5862 ms`，command wrapper `6247 ms`。
- Final Reverify：Publication Evidence 落盘后再次运行同一 `hugo --gc --minify`；Hugo `0.157.0`；`1235 Pages / 0 WARNING / 0 ERROR`；process `exit 0`；Hugo total `5865 ms`，command wrapper `6254 ms`。
- Route / Series Render：`PASS`；Article 06 route、canonical title、Article 06 → 05 previous href、Article 05 → 06 next href、06 → 07 absence、series directory 00 → 06 顺序、14 个 official href 与 11 个 evidence blob href 均已在 `public/` 产物中定位。
- Files Written：`content/ai-empowerment/agent-engineering-06-tool-runtime.md`；`content/ai-empowerment/agent-engineering-05-function-calling-tool-use.md`（仅既有 `Article 06` 桥接的 `relref`）；`docs/agent-engineering-course/articles/06-tool-runtime/README.md`（仅 Publisher / Build evidence）。
- Recommended Transition：`PUBLISHED candidate only`；只允许 Master 进入 state reconciliation / Git Diff Verify，Publisher 不直接写 `PUBLISHED`。
- Recommended Status Changes：由 Master 核对 Reviewer Final PASS、Publisher PASS、Build PASS 与 repository consistency 后，统一更新 Article 06 Lifecycle、global status、run state 与 checkpoint metadata；Publisher 未写 global durable state。
- Canonical Update Candidate：canonical Article 06 当前 plain-title entry 可由 Master 在 repository reconciliation 后链接到 Published Path；Publisher 未应用 canonical 修改。
- Checkpoint Readiness：`READY_FOR_MASTER_STATE_UPDATE_AND_GIT_DIFF_VERIFY`；尚未执行 Master reconciliation、独立 Article 06 checkpoint commit 或 commit verification。
- Publisher Boundary：Publisher交付时Lifecycle保持 `FINAL`；Publisher未修改 canonical、glossary、`status.md`、`course-run-state.md`、course README、Research、Evidence、Outline、Draft、Review、Lab、Article 07、theme 或 CI，未 stage、commit、push或创建 PR。

## Master Reconciliation Evidence

- Reviewer Final Gate：`PASS / 93`；`06-F01 / 06-F02 CLOSED`；0 unclosed Findings。
- Publisher / Build：`PASS`；Master独立复算semantic body=`380 lines / 28,460 chars / 44,000 UTF-8 bytes / SHA-256 C26E8FC3...B50267`，Draft / Published exact=`true`。
- Master Final Build：`hugo --gc --minify`；Hugo `0.157.0`；`1235 Pages / 0 WARNING / 0 ERROR`；process `exit 0`；Hugo total `5881 ms`。
- Route / Navigation / Evidence：Article 06 route存在；06 → 05=`1`、05 → 06=`1`、06 → 07=`0`；rendered evidence blob href=`11`；series index 00 → 06存在且顺序递增。
- Canonical / Global State：Article 06 canonical title已链接Published Path；course README、status、run-state与Lab current routing同步为`PUBLISHED / GIT_DIFF_VERIFY`。
- Checkpoint Boundary：Lifecycle=`PUBLISHED`仍不等于transaction complete；只有独立`Publish Agent Engineering Article 06`commit及commit verification通过后才允许Article 07 PRECHECK。

## Stop Line

Cycle 2 Final Gate、Publisher、Hugo Build与Master Reconciliation均为`PASS`，Lifecycle=`PUBLISHED`。当前只允许Master执行完整Git Diff Verify、显式stage、独立Article 06 checkpoint commit与commit verification；在checkpoint验证前不得启动Article 07。
