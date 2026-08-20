# Article 07｜MCP 与外部能力边界：协议解决什么，宿主仍需解决什么

- Canonical ID: `07`
- Workspace: `07-mcp-external-capability-boundary`
- Lifecycle Status: `PUBLISHED`
- Evidence Status: `PASS`（`7 CONFIRMED / 1 PARTIAL / 0 BLOCKED / 1 PROPOSAL`）
- Required Lab: `NONE`
- Mode: `NORMAL_ARTICLE`
- Current Gate: `GIT_DIFF_VERIFY`
- Next Allowed Action: `MASTER_VERIFY_DIFF_CREATE_CHECKPOINT_PUSH_AND_VERIFY_REMOTE`
- Blocker: `NONE`

## Dependencies

- Article 05：Function Calling 与 Tool Use。
- Article 06：Tool Runtime；checkpoint `199d4e19ba6150c8c598788a2daa8488e6e855f3` verified。

## Current Scope

本篇只进入 MCP 的协议映射、外部能力边界与 Host / Server 双层责任研究。不设独立 Lab，不搭建 production MCP Server，不提前进入 Article 08 Agent Loop。

## Research Summary

- Protocol baseline：`MCP 2026-07-28`；official sources rechecked `2026-08-20`。
- Claims：`9`；`7 CONFIRMED / 1 PARTIAL / 0 BLOCKED / 1 PROPOSAL`。
- Evidence Cards：`9`；Provider calls=`0`；local MCP runtime=`0`；Required Lab=`NONE`。
- Gate Recommendation：Researcher `PASS_CANDIDATE`；Master current-spec cross-check accepted=`PASS / NEXT_OUTLINE`。

## Evidence Gate Stop Line (historical)

Evidence Gate 接受时只允许 Author依据`07-C01`—`07-C09`创建`outline.md`；该 Gate 已由后续 Outline PASS 关闭。`C08 PROPOSAL`与`C09 PARTIAL`边界继续有效。

## Outline Gate

- Author delivery：`PASS_RECOMMENDED`；671 lines / SHA-256 `08EE7DFE...4DB4`；9/9 Claims；new core facts=`0`。
- Fresh Research Integrator：`PASS`；actionable Findings=`NONE`；Evidence leakage=`NONE`。
- Master decision：`PASS / NEXT AUTHOR_DRAFT`。

## Current Stop Line

Article 07 Review / Publisher / Semantic Diff / Hugo / Master Reconciliation均`PASS`，Lifecycle=`PUBLISHED`，Hugo=`1236 Pages / 0 WARNING / 0 ERROR / exit 0`。当前只允许Master执行完整Git Diff Verify、显式stage、独立`Publish Agent Engineering Article 07`commit、commit verification、push与remote verification；在远程checkpoint验证前不得启动Article 08。

## Publication Result

- Publisher Result：`PASS`；完成时间=`2026-08-20T12:21:33.011+08:00`。
- Published Path：`content/ai-empowerment/agent-engineering-07-mcp-external-capability-boundary.md`
- Published Route：`/ai-empowerment/agent-engineering-07-mcp-external-capability-boundary/`
- Canonical URL Candidate：`https://twoeggdu.github.io/twoegg-tech-stack/ai-empowerment/agent-engineering-07-mcp-external-capability-boundary/`
- Front Matter Result：`PASS`；title与canonical exact match；slug=`agent-engineering-07-mcp-external-capability-boundary`且content tree唯一；date=`2026-08-20`；`draft: false`；tags=`Agent Engineering / AI Engineering / Model Context Protocol / MCP`；`series: Agent Engineering`、`primary_series: agent-engineering`、`series_role: article`、`series_order: 80`、`weight: 3080`均符合发布合同。YAML与shortcode均使用ASCII双引号。
- Series Result：`PASS`；rendered series directory前8篇严格按`00 -> 01 -> 02 -> 03 -> 04 -> 05 -> 06 -> 07`递增，Article 07位于`series_order=80 / weight=3080`，没有Article 08发布项。
- Internal Link Result：`PASS`；Article 07 source含3个合法`relref`：顶部07 -> 06一次，正文07 -> 05一次、07 -> 06一次；Article 06仅新增一条机械“下一篇”07 `relref`。rendered HTML中07 -> 05=`1`、07 -> 06=`2`、06 -> 07=`1`、07 -> 08=`0`；课程Glossary workspace link机械映射为1个GitHub evidence link。
- Semantic Diff Result：`PASS`；移除FINAL Draft唯一H1，排除Published front matter与顶部上一篇导航，并把3处发布载体链接机械归一回workspace目标后，Published knowledge body与FINAL Draft逐行、逐字符exact match：`319`行、`18,279` characters、UTF-8 `29,767` bytes；双方SHA-256均为`6D46F60947C3885D012B19AEBFBC3C1BA46D398CC2FC5B91D14D142D5202FE5C`；knowledge semantics change=`0`。Article 06移除新增下一篇导航后与HEAD逐字符一致。
- Source URL Set：`PASS`；FINAL Draft与Published Content的12个MCP官方protocol / architecture URLs数量、顺序与字符串exact match；新增的Glossary GitHub link仅是workspace link的发布载体转换，不进入protocol source set。
- Markdown Result：`PASS`；Published knowledge body H1=`0`；code fence markers=`16`且成对；trailing whitespace=`0`；Article 08 publication link=`0`。
- Build Command：`hugo --gc --minify`
- Build Startup Note：首次sandboxed启动现有WinGet `hugo.exe`时PowerShell返回`ResourceUnavailable / 拒绝访问`，未产生有效Hugo build result；按权限流程以同一命令重跑。
- Hugo Version：`v0.157.0-7747abbb316b03c8f353fd3be62d5011fa883ee6+extended windows/amd64`；BuildDate=`2026-02-25T16:38:33Z`；VendorInfo=`gohugoio`。
- Build Result：`PASS`；`1236 Pages / 0 Paginator / 0 Non-page / 44 Static / 0 Processed images / 0 Aliases / 0 Cleaned`；Hugo total=`6794 ms`；process exit code=`0`。
- Warnings：`NONE / 0`
- Errors：`NONE / 0`
- Route Render Result：`PASS`；目标`public/.../index.html`存在，rendered canonical URL、title、07 -> 05、07 -> 06、06 -> 07、Glossary link与series order均已定位；07 -> 08=`0`。
- Files Written：`content/ai-empowerment/agent-engineering-07-mcp-external-capability-boundary.md`；`content/ai-empowerment/agent-engineering-06-tool-runtime.md`（仅机械下一篇导航）；`docs/agent-engineering-course/articles/07-mcp-external-capability-boundary/README.md`（仅本Publication Result）。
- Recommended Article Transition：`FINAL -> PUBLISHED candidate only`；Publisher未直接声明或写入`PUBLISHED`。
- Recommended Status Changes：由Master核对Reviewer Final Gate、Publisher PASS、Build PASS、published content、global state与repository consistency后，统一更新Article 07 README lifecycle、`status.md`、`course-run-state.md`与checkpoint metadata；建议下一Gate=`MASTER_STATE_UPDATE`，不是Article 08。
- Canonical Update Candidate：canonical Article 07当前plain-title entry可由Master在repository reconciliation后链接到Published Path；Publisher未应用canonical修改。
- Checkpoint Readiness：`READY_FOR_MASTER_STATE_UPDATE_AND_GIT_DIFF_VERIFY`；尚未执行Master reconciliation、Article 07 checkpoint commit或commit verification。
- Publisher Boundary：Publisher未修改Research、Evidence、Outline、Draft、Review、Trace、canonical、Glossary、`status.md`、`course-run-state.md`、theme、CI或Article 08，未stage、commit、push或创建PR；global Lifecycle继续以Master durable state为准。
