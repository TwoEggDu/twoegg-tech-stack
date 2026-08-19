# Article 05 Review｜Function Calling 与 Tool Use

> Reviewer：Fresh Reviewer / Codex subagent
>
> Review Date：`2026-08-20（Asia/Shanghai）`
>
> Review Type：`FIRST FORMAL REVIEW / FRESH CONTEXT`
>
> Status：`COMPLETE`
>
> Gate Decision：`PASS`
>
> Lifecycle Transition Candidate：`REVIEW -> FINAL`

## Review Inputs

- Canonical：`docs/agent-engineering-series-plan.md` 与 `docs/agent-engineering-course-plan-v3.1-review.md` Article 05 frozen section。
- Article workspace：`README.md`、`article-card.md`、`research.md`、`evidence.md`、`outline.md`、`draft.md`、本 Review。
- Course contracts：`course-factory.md`、`production-workflow.md`、`subagent-contracts.md`、`templates/review-checklist.md`、current glossary。
- Writing method：`twoegg-article-method` 及其要求的 Article Method、Outline Template、Series Planning Method、Article Production Workflow。
- Dependencies：已发布 Article 03、Article 04 及各自 workspace README。
- Runtime boundary：Provider Calls / Tool Execution=`NONE / NONE`；Provider runtime=`UNVERIFIED`；Calculator 与 `deleteFile` 均为 synthetic / not executed；两条 Provider trace 均为 official document contract / not locally executed。

## Current Official-source Recheck

本轮只重新打开 Claim-relevant current official primary sources，没有调用 Provider、SDK Tool Runner 或任何 Tool execution：

- [OpenAI Function calling](https://developers.openai.com/api/docs/guides/function-calling)：重新确认 client function flow 把 model call、application execution 与 correlated `function_call_output` 分开；Responses 使用 `call_id`，arguments streaming 保留 delta / done 边界；一轮可有多个 calls，`parallel_tool_calls=false` 只约束 call 数量，不替 Host 决定副作用执行策略；built-in tools 保留为 execution-owner counterexample。
- [Anthropic Define tools](https://platform.claude.com/docs/en/agents-and-tools/tool-use/define-tools)、[Handle tool calls](https://platform.claude.com/docs/en/agents-and-tools/tool-use/handle-tool-calls)、[Streaming Messages](https://platform.claude.com/docs/en/build-with-claude/streaming)、[Fine-grained tool streaming](https://platform.claude.com/docs/en/agents-and-tools/tool-use/fine-grained-tool-streaming)、[Parallel tool use](https://platform.claude.com/docs/en/agents-and-tools/tool-use/parallel-tool-use)、[How tool use works](https://platform.claude.com/docs/en/agents-and-tools/tool-use/how-tool-use-works)：重新确认 `input_schema` / `tool_choice`、`tool_use.id -> tool_result.tool_use_id`、partial JSON 与 content-block completion、fine-grained invalid / incomplete JSON guard、多个 calls 的 Host-selected concurrent / sequential semantics，以及 client-executed / server-executed tool 的 owner 差异。

Recheck Result：`NO CURRENT CONTRADICTION / NO RETURN_TO_RESEARCH`。Hosted docs 仍未 pinned，Publisher 在发布前仍应按 Evidence Register 做 current recheck。

## Technical Review

- [x] `Function Calling != Tool Runtime`，model intent 与 client-side route / reject / execute responsibility 明确分离。
- [x] client-executed scope 与 OpenAI built-in / Anthropic server-executed counterexample 同时存在，没有把 Host execution 外推到所有 Tool。
- [x] OpenAI Responses item 与 Anthropic Messages content-block wire shape 分开描述，没有伪造统一 payload。
- [x] `call_id` 与 `tool_use.id -> tool_use_id` 的 correlation 跨入下一次 model request，且 Result 没有被写成自动生成或自动可信。
- [x] argument fragment、completed candidate、validated arguments、authorized action、executed result 五态分离；helper / strict 没有提前升级 fragment。
- [x] multiple calls 与 Host execution concurrency 分离，没有写成“Host 必须并行”。
- [x] unknown / invalid / unauthorized 在副作用前 fail closed；`Schema Valid != Authorized`。
- [x] Calculator 与 `deleteFile` 均保留 synthetic / not executed 标签，没有 Provider behavior 或文件副作用暗示。
- [x] 正常路径、拒绝路径、error-result seam 与 stop condition 均可解释；未把未运行方案写成 runtime fact。

Outcome：`PASS`

## Evidence Review

- [x] `05-C01`—`05-C08` 均有 Claim / Evidence Card，Draft semantic coverage=`8 / 8`。
- [x] `05-C01`—`05-C06` 的 `CONFIRMED` 措辞没有超过 current official document contract。
- [x] `05-C07` 保持 `PARTIAL`：只说 generic Tool Result envelope 本身不自动等于 Evidence，并保留 rich result / citation / provenance counterexample。
- [x] `05-C08` 保持 `PARTIAL / COURSE WORKING BOUNDARY`：只说一次 Tool Use 不足以证明课程定义下的 Agent Loop，不主张行业唯一分类。
- [x] Provider Calls / Tool Execution=`NONE / NONE`，runtime=`UNVERIFIED`；正文没有把 official examples 写成本地 observation。
- [x] Calculator fixture 无 prompt / model / sample / Expected / Observed；正文只使用静态 contract-surface 结论。
- [x] OpenAI / Anthropic traces 明示 `OFFICIAL EXAMPLE / DOCUMENT CONTRACT / NOT LOCALLY EXECUTED`，没有声称 credentials、network、side effect、result truth 或 final-answer quality。
- [x] Draft 没有新增 selection quality、schema adherence、parallel frequency / order、error recovery、SDK Runner behavior 或 side-effect 核心事实。

Outcome：`PASS`

## Course Review

- [x] Problem Space -> Abstract Model -> Concrete Mechanism -> Engineering Judgment -> Verification Boundary 教学主线完整。
- [x] 开头先立“结构化 call 不等于行动发生”的工程误判，没有退化为 API 字段教程。
- [x] Article 03 的 Parse / Schema / DTO / Domain 与 Article 04 的 streaming completion / Provider scope 被正确继承，没有从零重讲。
- [x] Article 05 只承担 Function Calling / Tool Use 的 mechanism responsibility；Article 06 / 07 / 08 / 18 / 19 stop lines 均明确。
- [x] 没有提前展开 Tool Runtime implementation、MCP、Agent Loop state / stop、Evidence schema 或 Permission / Sandbox implementation。
- [x] Learning Check 覆盖 intent / execution、schema / authorization、Provider correlation、stream completion、multiple-call concurrency、Evidence 与 Agent Loop 边界。
- [x] Job competency 覆盖 Provider contract reading、state modeling、fail-closed seam、correlation 与 evidence discipline，符合 M 级核心课职责。
- [x] 结尾以最短责任判断收束，并自然桥接 Article 06；Article 06 workspace 仍不存在。

Outcome：`PASS`

## Publication / Scoped Static Checks

| Check | Result | Evidence |
|---|---|---|
| Unique H1 | `PASS` | `1` |
| Workspace Draft frontmatter | `PASS` | `NONE`，符合 Draft artifact；由后续 Publisher 创建 Hugo frontmatter |
| Code fences | `PASS` | `20` markers，偶数且配对 |
| Trailing whitespace / tab lines | `PASS` | `0 / 0` |
| External links | `PASS` | `7 / 7`，exact match Evidence source whitelist；本轮均可打开为 official hosted docs |
| Hugo shortcode / local relref risk | `PASS` | Draft 中无 shortcode；未来 06 / 07 / 08 / 18 / 19 只用 prose，不创建未发布 target |
| Runtime / fixture labels | `PASS` | 顶部集中声明，Calculator、`deleteFile` 与两条 traces 在正文再次就近标注 |
| Future Article leakage | `PASS` | Article 06 workspace absent；没有创建 content / assets / Lab |

本 Review 按任务边界没有运行 Hugo；Hugo、frontmatter、route、render 与 series navigation 属于后续 Publisher / Build Gate，不由本次 Review PASS 替代。

## Finding Register

Finding schema：`Finding ID / Status / Severity / Category / Claim / Source or Location / Acceptance Criteria`。

`NONE`。本轮未创建 Finding，因此没有同轮关闭、降级或隐藏处置项。

| Severity | OPEN Count |
|---|---:|
| `BLOCKER` | 0 |
| `MAJOR` | 0 |
| `MINOR` | 0 |
| `EDITORIAL` | 0 |

## Review Score

| Dimension | Score | Basis |
|---|---:|---|
| Technical Accuracy | `19 / 20` | owner、wire、correlation、streaming 与 multi-call 边界准确且有 counterexample |
| Evidence Discipline | `20 / 20` | 两项 PARTIAL、NONE / UNVERIFIED、synthetic / official-example 标签全部保真 |
| Teaching Quality | `19 / 20` | 从真实误判推进到责任链、状态模型、负例与 Learning Check |
| Engineering Transfer | `19 / 20` | 可直接用于审查 client-tool registry、buffer、authorization 与 correlation seam |
| Readability & Compression | `18 / 20` | M 级范围内信息密度高但主线稳定，未用重复篇幅掩盖 Evidence 边界 |
| **Total** | **`95 / 100`** | 超过课程 Review baseline；无 actionable Finding |

## Final Gate

- Technical Review：`PASS`
- Evidence Review：`PASS`
- Course Review：`PASS`
- Publication Risk Review：`PASS_FOR_REVIEW_STAGE`
- Unclosed Findings：`NONE`
- Finding Counts：`0 BLOCKER / 0 MAJOR / 0 MINOR / 0 EDITORIAL`
- Blocker：`NONE`
- Final Gate Decision：`PASS`
- Lifecycle Transition Candidate：`REVIEW -> FINAL`
- Next Action：由 Master 验证本 Review artifact 与 Gate decision 后应用 Lifecycle transition，再路由 Publisher；Reviewer 不写 global durable state。

## Stop Line

本次首轮 Review 只写 `review.md`，没有修改 Draft、README、Article Card、Research、Evidence、Outline、canonical、glossary、global state、Published Content 或 assets；没有运行 Hugo、调用 Provider、执行 Tool、stage、commit 或 push。Article 05 完成 Publisher、Build、Master reconciliation、独立 checkpoint commit 与 commit verification 前，不得启动 Article 06。
