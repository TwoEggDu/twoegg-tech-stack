# Article 08｜Agent Loop：Turn、Step、Decide、Act、Observe 与 Stop

- Canonical ID: `08`
- Workspace: `08-agent-loop`
- Lifecycle Status: `EVIDENCE_READY`
- Evidence Status: `PASS / 6 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 2 PROPOSAL`
- Required Lab: `Lab 03 Minimal Agent Loop`
- Lab Status: `VERIFIED / EVIDENCE_MERGED`
- Mode: `LAB_ARTICLE`
- Current Gate: `OUTLINE / PAUSED_WORKER_EXECUTION`
- Next Allowed Action: `RETRY_REAL_AUTHOR_OUTLINE_WHEN_SUBAGENT_RUNTIME_AVAILABLE`
- Blocker: `SUBAGENT_RUNTIME_UNAVAILABLE / NO_DURABLE_OUTLINE`

## Dependencies

- Article 03：Structured Output 的 Parse / Schema / DTO / Domain validation 边界；checkpoint `857fe9f` verified。
- Article 05：Function Calling 只表达结构化行动候选，Host 决定是否执行。
- Article 06：Tool Runtime 的 validate / policy / execute / result / trace 管线；checkpoint `199d4e1` verified。
- Article 07：MCP 协议成功不能单独证明 Agent Loop、Permission、Tool Runtime gates 或完整 Evidence；checkpoint `f3de0f2` pushed / live-remote verified。

## Current Scope

本篇是 required Lab Article，只负责正式建立 Agent Loop 的 Turn / Step / Decide / Act / Observe / State / Stop 边界。Planning、Workflow / State Machine、Long-running recovery、Context / Memory、多 Agent、DeepSeek Harness source verification 与 BuildPilot Runtime 均留给后续课程对象。

## Transaction Record

- PRECHECK：`PASS`；canonical 顺序为 `07 -> 08 -> 09`，Article 08 为 `L / non-optional / LAB_ARTICLE`。
- Repository boundary：Article 07 local / live `origin/main` 均为 `f3de0f2a7b1e06c530900627183bd364ca0b4314`，left/right=`0/0`；PRECHECK 时 worktree clean。
- ARTICLE_KICKOFF：`PASS`；Master 已取得唯一 Article 08 transaction ownership。
- WORKSPACE_INIT：`PASS`；只创建 `README.md / article-card.md / research.md / evidence.md / review.md` 的 `PLANNED` skeleton。
- Worker Start：真实 Researcher `/root/article_08_researcher` 已启动；Allowed Writes 仅为 `research.md / evidence.md / labs/lab-03-minimal-agent-loop/README.md`。
- Research / Preliminary Evidence：`COMPLETE`；`8 Claims / 8 Evidence Cards`；`4 CONFIRMED / 2 PARTIAL / 0 BLOCKED / 2 PROPOSAL`；Evidence Gate=`NOT_READY`。
- Lab 03 Design：`FROZEN / COMPLETE / NOT EXECUTED`；Design SHA-256=`242F28DB7151E4AA3359B4C22F526A98D2C476A48D27C85DB7752BBE0DDCDD86`；555 lines。
- Lab Worker Start：真实 Lab Engineer `/root/article_08_lab_engineer` 已启动；Allowed Writes 仅为 `labs/lab-03-minimal-agent-loop/` 内 implementation / fixtures / tests / observations。
- Lab Execute / Observation：`COMPLETE`；formal restore / build / test / run-a / run-b / independent verifier均 exit `0`；4 cases、10 STEP、4 TERMINAL、10 states、7 Tool Outcomes、7 Observations、10 decisions、1 SUCCEEDED；run-a/run-b六文件pairwise byte-identical。
- Frozen Design Integrity：first `30,312` bytes SHA-256仍为`242F28DB7151E4AA3359B4C22F526A98D2C476A48D27C85DB7752BBE0DDCDD86`。
- Evidence Merge Start：真实 Researcher `/root/article_08_researcher` 已启动；Lab status与Claim升级尚未由Master预判。
- Evidence Merge / Gate：Researcher=`PASS recommendation`，Master artifact check=`PASS`；final Claim=`6 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 2 PROPOSAL`；C03/C05保持课程`PROPOSAL`，C07/C08仅在fixed Host / deterministic fixture scope内`CONFIRMED`。
- Outline Worker Start：真实 Author `/root/article_08_outliner` 已启动；Allowed Writes仅为`outline.md`。
- Outline Worker Failure：`/root/article_08_outliner`、`/root/article_08_outliner_resume`、`/root/article_08_outliner_minimal` 三个真实 Author task 均在只读阶段长时间无响应；Master在确认`outline.md`始终不存在后依次中断。没有越界写入、Draft、Review或Article 09资产。
- Fresh Resume Reconciliation：2026-08-20 重新核验 local `HEAD`、fresh-fetched `origin/main` 与 live `ls-remote refs/heads/main` 均为 `1045264057f1eced21f8e7438b43bb7448a67091`；worktree clean；Published Content、`outline.md`、`draft.md` 均不存在。Factory 已恢复为 `RUNNING / OUTLINE`，等待 fresh real Author durable output。
- Fresh Author Retry Failure：`/root/article_08_author_outline` 与最小上下文 `/root/article_08_author_outline_minimal` 均在重复等待和明确收敛消息后无 durable output，已安全中断；结合三次历史失败，当前判定为`SUBAGENT_RUNTIME_UNAVAILABLE`。没有越界写入、Draft、Review、Published Content 或Article 09资产。
- Article 08 Published Content：`NONE`
- Lab 03 directory：`docs/agent-engineering-course/labs/lab-03-minimal-agent-loop/`；Design、implementation、tests、raw observations 与 Evidence Merge 已由 recovery checkpoint `1045264` 保存。
- Article 09 workspace：`NONE`

## Stop Line

Evidence Gate已关闭，Factory因`SUBAGENT_RUNTIME_UNAVAILABLE`安全暂停在`OUTLINE`。恢复时只允许 fresh real Author 创建Detailed Outline；Master不得代写。不得创建Draft、修改Evidence/Lab或启动Article 09；若Outline需要新核心事实，必须`RETURN_TO_RESEARCH`。
