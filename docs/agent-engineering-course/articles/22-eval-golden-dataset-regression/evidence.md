# Article 22 Evidence｜Evidence Merge

> Gate：`EVIDENCE_MERGE + EVIDENCE_GATE`
> Retrieved / Verified：`2026-08-28 (Asia/Shanghai)`
> Decision：`PASS / LAB 06 OBSERVATION MERGED / OUTLINE ELIGIBLE`
> Counts：`12 Claims / 12 Cards / 3 CONFIRMED / 6 PARTIAL / 3 PROPOSAL / 0 BLOCKED`

行为性 Claim `22-C07`、`22-C10` 已由真实 Lab06 observation 支撑，但 `CONFIRMED` 只覆盖 `lab06-fixture-v1 / corpus r1 / scorer v1 / Windows + .NET 10.0.301`。`22-C09` 只对已执行的 `REGRESSION / UNCHANGED / UNKNOWN / INCOMPARABLE` 路径有 runtime evidence，`IMPROVEMENT` 未执行，因此保持 `PARTIAL`。

## Source Manifest

| Source ID | Source | Fixed identity | Retrieved | Drift boundary |
|---|---|---|---|---|
| `S-NIST-RMF` | NIST AI RMF Core / AI RMF 1.0 | `NIST AI 100-1`，2023-01 | 2026-08-28 | NIST 页面注明 1.0 正在修订；本文只采用当前 1.0 的 MEASURE 文本 |
| `S-OAI-EVAL` | OpenAI Evaluation Best Practices | official hosted page | 2026-08-28 | 动态页面；legacy Evals platform 已公告 2026-10/11 退役计划，概念指导不等于 API 长期合同 |
| `S-OAI-GRADER` | OpenAI Graders API / eval guidance | official hosted docs | 2026-08-28 | grader/model/options 可变化；只使用已披露 grader 形态与 judge limitations |
| `S-GOOGLE-SPLIT` | Google MLCC, Dividing datasets | last updated `2025-12-03 UTC` | 2026-08-28 | ML-specific educational guidance；Agent app 映射保持 `PARTIAL` |
| `S-DATASHEETS` | Gebru et al., Datasheets for Datasets | arXiv `1803.09010v8`，2021-12-01；CACM 2021-12 | 2026-08-28 | 原始论文 Proposal；非 Golden Dataset 标准 |
| `S-REPO` | canonical、Article 22 Card、Published Article 21 | kickoff ref `470c362567d71aa4b7e5d951406b9af92b5b1adf` | 2026-08-28 | repository-local course scope |
| `S-LAB06` | Lab 06 frozen design、source、raw observations、process records、normalized results 与 hashes | `lab06-fixture-v1 / corpus r1 / scorer v1`；Windows 10.0.19045；.NET SDK 10.0.301 | 2026-08-28 | 8 个 synthetic cases、deterministic exact/rule scorer；无 Agent/model/Provider/production/statistical generalization |

## Evidence 22-E01｜Activity ownership

- Article: `22｜Eval、Golden Dataset 与 Regression`
- Claim ID: `22-C01`
- Claim: `Demo、Test、Benchmark、Eval 与 Regression 应按目的分账，不能互相替代。`
- Evidence Status: `PROPOSAL`
- Evidence Class: `DESIGN_PROPOSAL`
- Source Type: `official framework + official product guidance + course design`
- Source: `S-NIST-RMF`；`S-OAI-EVAL`
- Repository: `N/A`
- Commit: `N/A`
- File: `N/A`
- Symbol: `N/A`
- Call Path: `N/A`
- Experiment: `N/A`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-28 Asia/Shanghai`
- Version Scope: `NIST AI RMF 1.0；OpenAI hosted page as retrieved`
- Reproduction: `阅读 NIST MEASURE 5.3 与 OpenAI “Types of evals / anti-patterns”段落。`
- Observation: `NIST 并列 software testing、performance assessment、benchmark、uncertainty 与持续测量；OpenAI 区分 industry benchmark、numerical score、application-specific eval，并拒绝 vibe-based eval。`
- Counter-evidence Searched: `检查来源是否给出与本文相同的五类互斥 taxonomy；未发现。`
- Interpretation: `来源支持这些活动不应压成一个分数或一次演示；本文五分法是课程 ownership model。`
- Proves: `需要先说明评估活动的目的和输出，Demo/score 单独不足以承担完整 Eval/Regression 结论。`
- Does Not Prove: `五分法是行业标准、穷尽或严格互斥。`
- Limitations: `术语在不同组织和框架中可重叠。`
- Course Usage: `问题空间、五种活动分账。`
- BuildPilot Implication: `ADOPT — 设计治理语言；不表示 Runtime 已实现。`
- Owner: `Article 22 Researcher`
- Verified At: `2026-08-28`

## Evidence 22-E02｜Minimum eval contract

- Article: `22｜Eval、Golden Dataset 与 Regression`
- Claim ID: `22-C02`
- Claim: `可审计 Eval 需要显式 objective、dataset、scorer、metric、threshold、baseline 与 version manifest。`
- Evidence Status: `PARTIAL`
- Evidence Class: `OFFICIAL_DOC`
- Source Type: `official framework + official product documentation`
- Source: `S-NIST-RMF`；`S-OAI-EVAL`
- Repository: `N/A`
- Commit: `N/A`
- File: `N/A`
- Symbol: `N/A`
- Call Path: `N/A`
- Experiment: `N/A`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-28 Asia/Shanghai`
- Version Scope: `NIST AI RMF 1.0；OpenAI hosted page as retrieved`
- Reproduction: `核对 NIST MEASURE 2.1 与 OpenAI eval workflow 1—5。`
- Observation: `NIST 要求记录 test sets、metrics、TEVV tools；OpenAI 要求 objective、dataset、metrics、run/compare、continuous evaluation。`
- Counter-evidence Searched: `检查两来源是否要求本文全部 threshold/baseline/version-manifest 字段；没有。`
- Interpretation: `核心 concern 有直接支持；完整最小合同是课程扩展。`
- Proves: `Eval 不应只有输出分数；objective、data、measurement method/tool 需要显式化。`
- Does Not Prove: `本文字段是唯一或充分 schema，或字段存在就保证 validity。`
- Limitations: `baseline、threshold、manifest 的具体组合主要来自课程设计。`
- Course Usage: `Eval Contract 抽象模型。`
- BuildPilot Implication: `ADOPT — 后续设计需携带版本化合同；当前 NOT IMPLEMENTED。`
- Owner: `Article 22 Researcher`
- Verified At: `2026-08-28`

## Evidence 22-E03｜Candidate is not Golden

- Article: `22｜Eval、Golden Dataset 与 Regression`
- Claim ID: `22-C03`
- Claim: `Trace candidate 必须经 lineage、review、acceptance 与 split assignment 才能成为 fixture-scoped Golden sample。`
- Evidence Status: `PROPOSAL`
- Evidence Class: `DESIGN_PROPOSAL`
- Source Type: `primary paper + official guidance + repository handoff`
- Source: `S-DATASHEETS`；OpenAI 2025 eval guidance `https://openai.com/index/evals-drive-next-chapter-of-ai/`；`S-REPO`
- Repository: `TwoEggDu/twoegg-tech-stack`
- Commit: `470c362567d71aa4b7e5d951406b9af92b5b1adf`
- File: `content/ai-empowerment/agent-engineering-21-trace-replay-failure-taxonomy.md`
- Symbol: `Trace 只把候选样本交给 Eval`
- Call Path: `Trace candidate -> curation -> acceptance -> dataset revision`
- Experiment: `N/A`
- Fixture: `lab06-fixture-v1 design`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-28 Asia/Shanghai`
- Version Scope: `Datasheets v8；OpenAI post 2025-11-19；repository kickoff ref`
- Reproduction: `读取论文 abstract/questions、OpenAI Specify/Measure guidance 与 Article21 handoff table。`
- Observation: `论文要求记录 dataset motivation/composition/collection/use/maintenance；OpenAI 建议由领域专家形成并维护 golden reference；Article21 明确只交 candidate+lineage。`
- Counter-evidence Searched: `搜索是否存在“真实 Trace 自动成为 Golden”的 primary rule；未发现。`
- Interpretation: `来源支持 documentation/expert curation concern；本文 acceptance state machine 是课程 Proposal。`
- Proves: `来源和 lineage 本身不足以证明 label/oracle 已被接受。`
- Does Not Prove: `所有团队必须采用本文 acceptance 字段，或 accepted sample 具备代表性。`
- Limitations: `“Golden”是实践术语，不是本文来源共同定义的标准状态。`
- Course Usage: `Trace-to-Eval seam、sample lineage。`
- BuildPilot Implication: `ADOPT — 只接收 candidate，需独立 acceptance；DESIGN ONLY。`
- Owner: `Article 22 Researcher`
- Verified At: `2026-08-28`

## Evidence 22-E04｜Case identity and lineage

- Article: `22｜Eval、Golden Dataset 与 Regression`
- Claim ID: `22-C04`
- Claim: `Eval Case 应具有稳定 identity、input/lineage、oracle/scorer、criticality、revision 与 applicability。`
- Evidence Status: `PROPOSAL`
- Evidence Class: `DESIGN_PROPOSAL`
- Source Type: `primary paper + course contract`
- Source: `S-DATASHEETS`；`S-NIST-RMF`；`S-REPO`；`S-LAB06`
- Repository: `TwoEggDu/twoegg-tech-stack`
- Commit: `470c362567d71aa4b7e5d951406b9af92b5b1adf`
- File: `docs/agent-engineering-course/labs/lab-06-trace-eval/README.md`
- Symbol: `Frozen data contracts`
- Call Path: `dataset revision -> case -> oracle/scorer -> run observation`
- Experiment: `Lab 06 / COMPLETE / auxiliary evidence`
- Fixture: `lab06-golden-corpus-r1`
- Trace: `observations/run-a/；observations/fault-injection/`
- Retrieved / Run At: `2026-08-28 Asia/Shanghai`
- Version Scope: `Lab06 design v1`
- Reproduction: `核对 frozen fixtures、formal result schema、manifest-mismatch output 与 hashes.sha256。`
- Observation: `8 个 case 以稳定 case_id、criticality、source_trace_ref、input 与 oracle 进入固定 corpus revision；run results 保存 dataset/scorer/candidate revisions。该合同支持本 fixture 的可比运行和 fault injection。`
- Counter-evidence Searched: `检查是否可仅凭自然语言 case 名稳定比较；无法处理 revision、oracle 或 applicability 漂移。`
- Interpretation: `Lab 表明这套字段在本 fixture 中足以驱动可审计比较；“所有 Eval 都应采用这套字段”仍是课程 Proposal，不能因一个实现升级为通用事实。`
- Proves: `课程已有一套经实际 build/run 使用的固定 case contract，且 revision/manifest 参与了可比性判定。`
- Does Not Prove: `该 schema 适用于所有 Eval 或生产规模。`
- Limitations: `criticality 与 applicability 仍是 policy-bound classification。`
- Course Usage: `最小 Eval Case 与 Lab fixture。`
- BuildPilot Implication: `DEFER — 后续 Article 43 才回收治理设计。`
- Owner: `Article 22 Researcher`
- Verified At: `2026-08-28`

## Evidence 22-E05｜Scorer families and errors

- Article: `22｜Eval、Golden Dataset 与 Regression`
- Claim ID: `22-C05`
- Claim: `Exact、rule、structured、semantic/model 与 human scorer 的证明范围和错误边界不同。`
- Evidence Status: `PARTIAL`
- Evidence Class: `OFFICIAL_DOC`
- Source Type: `official product documentation`
- Source: `S-OAI-EVAL`；`S-OAI-GRADER`
- Repository: `N/A`
- Commit: `N/A`
- File: `N/A`
- Symbol: `StringCheckGrader / TextSimilarityGrader / ScoreModelGrader`
- Call Path: `sample output -> grader -> score`
- Experiment: `N/A`
- Fixture: `Lab06 uses deterministic rule scorer only`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-28 Asia/Shanghai`
- Version Scope: `OpenAI hosted docs as retrieved`
- Reproduction: `核对 grader types、LLM-as-judge challenges 与 human calibration guidance。`
- Observation: `官方文档列出 string check、text similarity、model grader，并披露 position/verbosity bias；建议保持 human feedback/calibration。`
- Counter-evidence Searched: `检查是否有一种 grader 被声明为无偏真值；没有，文档明确说 no strategy is perfect。`
- Interpretation: `scorer 选择改变可测对象与误差；课程五类总结比来源更宽。`
- Proves: `自动 scorer 的类型和适用边界应被记录，model judge 不能默认当 oracle。`
- Does Not Prove: `本文 taxonomy 穷尽，human judgment 无误，或某 grader 在本任务准确。`
- Limitations: `产品行为和推荐会漂移；本阶段未校准任何 model grader。`
- Course Usage: `Oracle/scorer 分账与反模式。`
- BuildPilot Implication: `SIMPLIFY — M0 优先可重复 deterministic scorer；语义 judge 延后。`
- Owner: `Article 22 Researcher`
- Verified At: `2026-08-28`

## Evidence 22-E06｜Metric, benchmark and uncertainty

- Article: `22｜Eval、Golden Dataset 与 Regression`
- Claim ID: `22-C06`
- Claim: `Metric、aggregation、threshold、baseline、uncertainty 与 release gate 是不同责任面。`
- Evidence Status: `PARTIAL`
- Evidence Class: `OFFICIAL_DOC`
- Source Type: `official framework`
- Source: `S-NIST-RMF`；`S-LAB06`
- Repository: `N/A`
- Commit: `N/A`
- File: `N/A`
- Symbol: `AI RMF 1.0 §5.3 MEASURE`
- Call Path: `risk/objective -> measure -> benchmark comparison -> documented result`
- Experiment: `Lab 06 / COMPLETE / auxiliary evidence`
- Fixture: `lab06-scorer-v1`
- Trace: `observations/run-a/known-regression/result.json；observations/fault-injection/scorer-v2/result.json`
- Retrieved / Run At: `2026-08-28 Asia/Shanghai`
- Version Scope: `NIST AI RMF 1.0`
- Reproduction: `核对 MEASURE 1.1—2.5、4.3；读取 frozen policy 与 Lab06 formal/fault-injection results。`
- Observation: `NIST 要求 metrics/methods、uncertainty、performance benchmarks、test/tool documentation、improvements/declines 与 generalizability limitations。Lab06 又实际分开输出 aggregate、critical、threshold、baseline delta、comparability 与 overall gate。`
- Counter-evidence Searched: `检查 NIST 是否规定本文 aggregate/critical gate 公式；没有。`
- Interpretation: `这些 concern 应分开记录；Lab06 证明分账可在一个窄 deterministic fixture 中运行，但具体 release gate 仍是课程 policy。`
- Proves: `单个 aggregate score 不是完整评估报告；在 Lab06 中，aggregate PASS 与 critical/overall FAIL 确实同时存在。`
- Does Not Prove: `任何固定阈值正确，或本文 gate 能代表生产风险容忍度。`
- Limitations: `NIST 框架是 voluntary、use-case agnostic。`
- Course Usage: `Metric/threshold/baseline/uncertainty 模型。`
- BuildPilot Implication: `ADOPT — 将测量与决策 policy 分账；NOT IMPLEMENTED。`
- Owner: `Article 22 Researcher`
- Verified At: `2026-08-28`

## Evidence 22-E07｜Critical gate resists averaging

- Article: `22｜Eval、Golden Dataset 与 Regression`
- Claim ID: `22-C07`
- Claim: `在 Lab06 冻结的 8-case exact/rule fixture 中，hard critical-case gate 阻止 aggregate threshold PASS 掩盖已知关键退化。`
- Evidence Status: `CONFIRMED`
- Evidence Class: `RUNTIME_OBSERVATION`
- Lab Dependency: `SATISFIED / LAB06 FIXTURE`
- Source Type: `frozen experiment design + raw runtime observation`
- Source: `S-LAB06`
- Repository: `TwoEggDu/twoegg-tech-stack`
- Commit: `N/A — uncommitted Article22 transaction`
- File: `docs/agent-engineering-course/labs/lab-06-trace-eval/fixtures/scorer-policy.json；observations/run-a/known-regression/result.json`
- Symbol: `overall_gate`
- Call Path: `8 cases -> aggregate accuracy + critical accuracy -> overall gate`
- Experiment: `Lab 06 / COMPLETE`
- Fixture: `lab06-fixture-v1`
- Trace: `observations/tdd-red/；observations/tdd-green/；observations/run-a/；observations/run-b/；observations/verification/`
- Retrieved / Run At: `2026-08-28 Asia/Shanghai`
- Version Scope: `lab06-fixture-v1 / corpus r1 / scorer v1 / Windows + .NET 10.0.301`
- Reproduction: `按 process-record.md 的 locked restore/build、RED/GREEN、Run A/B 与 formal verifier；读取 normalized result.json 和 hashes.sha256。`
- Observation: `baseline=8/8、aggregate=1、critical=1、overall PASS；known regression=7/8、aggregate=0.875 且 threshold PASS、critical=0.5、overall FAIL；C01=REGRESSION。Run A/B bytes 与 SHA-256 一致。`
- Counter-evidence Searched: `aggregate threshold 单独确实通过；只有额外 hard critical gate 才阻止 overall PASS。若任务没有 critical requirement，aggregate-only policy 仍可能是另一种合法设计。`
- Interpretation: `在本 fixture 的冻结 policy 下，critical gate 实际阻止 aggregate threshold 掩盖已知 critical regression。`
- Proves: `该 hard-gate 机制在本地 deterministic 8-case fixture 中按冻结判据生效且可重复。`
- Does Not Prove: `所有 Eval 都必须使用 hard critical gate、0.80/1.00 阈值适合生产，或任何真实 Agent 风险被控制。`
- Limitations: `8 个 synthetic cases；fixed candidate；exact/rule scorer；单一 Windows/.NET 环境；无统计外推。`
- Course Usage: `Lab 06 核心行为 Claim。`
- BuildPilot Implication: `DEFER — Lab fixture 不等于 BuildPilot gate implementation。`
- Owner: `Article 22 Researcher`
- Verified At: `2026-08-28`

## Evidence 22-E08｜Leakage and test-set wear-out

- Article: `22｜Eval、Golden Dataset 与 Regression`
- Claim ID: `22-C08`
- Claim: `split 污染、重复样本与反复暴露会降低对未见数据表现的信心。`
- Evidence Status: `PARTIAL`
- Evidence Class: `OFFICIAL_DOC`
- Source Type: `official educational documentation`
- Source: `S-GOOGLE-SPLIT`
- Repository: `N/A`
- Commit: `N/A`
- File: `N/A`
- Symbol: `Training, validation, and test sets`
- Call Path: `split -> tune on validation -> final test -> refresh`
- Experiment: `N/A`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-28 Asia/Shanghai`
- Version Scope: `page last updated 2025-12-03 UTC`
- Reproduction: `核对重复使用 test set、duplicate examples、representativeness 与 refresh 段落。`
- Observation: `Google 明确说反复用 test set 指导修改会拟合其 peculiarities；validation/test 会 wear out；训练集重复样本污染测试。`
- Counter-evidence Searched: `文档也说明 split 比例没有固定要求，统计意义需按任务判断。`
- Interpretation: `Agent eval 可借用暴露/去重原则，但不能把监督学习 split 机械等同所有 Agent workflow。`
- Proves: `复用和 duplication 会削弱未见数据解释，split lineage 值得记录。`
- Does Not Prove: `本文 split 名称/比例适合所有 Agent，或 leakage 已被完全消除。`
- Limitations: `官方页面是 ML 教学文档，不是 Agent Eval 标准。`
- Course Usage: `data leakage、overfitting、corpus maintenance。`
- BuildPilot Implication: `ADOPT — 设计时保留 exposure/split metadata；NOT IMPLEMENTED。`
- Owner: `Article 22 Researcher`
- Verified At: `2026-08-28`

## Evidence 22-E09｜Regression verdict state space

- Article: `22｜Eval、Golden Dataset 与 Regression`
- Claim ID: `22-C09`
- Claim: `Regression 应保留 IMPROVEMENT、REGRESSION、UNCHANGED、UNKNOWN、INCOMPARABLE，而非只留总分 delta；Lab06 已执行其中四条路径。`
- Evidence Status: `PARTIAL`
- Evidence Class: `RUNTIME_OBSERVATION + DESIGN_PROPOSAL`
- Source Type: `course design informed by official framework + raw runtime observation`
- Source: `S-NIST-RMF`；`S-REPO`；`S-LAB06`
- Repository: `TwoEggDu/twoegg-tech-stack`
- Commit: `470c362567d71aa4b7e5d951406b9af92b5b1adf`
- File: `docs/agent-engineering-course/labs/lab-06-trace-eval/README.md`
- Symbol: `Verdict contract`
- Call Path: `comparability -> per-case comparison -> run verdict`
- Experiment: `Lab 06 / COMPLETE`
- Fixture: `lab06-fixture-v1`
- Trace: `observations/run-a/known-regression/result.json；observations/fault-injection/`
- Retrieved / Run At: `2026-08-28 Asia/Shanghai`
- Version Scope: `course proposal v1`
- Reproduction: `读取 FI-01/FI-02/FI-03 process records 与 normalized results；复核 per-case verdict counts 和 manifest_comparable。`
- Observation: `FI-01 产生 C01=REGRESSION、其余 7 个 UNCHANGED；FI-02 产生 UNKNOWN/fail-closed；FI-03 产生 INCOMPARABLE/fail-closed，且不输出 ordinary aggregate/delta。IMPROVEMENT 路径未执行。`
- Counter-evidence Searched: `简单 deterministic suite 可只用 PASS/FAIL；但它无法区分 missing、manifest drift 与真实 regression。`
- Interpretation: `本 fixture 证明四类已执行状态可被分账，避免把 missing 或 manifest drift 强折成普通 regression delta；完整五状态仍只有部分 runtime 覆盖。`
- Proves: `本实现实际保存 REGRESSION、UNCHANGED、UNKNOWN、INCOMPARABLE，并对 unknown/incomparable fail closed。`
- Does Not Prove: `IMPROVEMENT 路径已执行、五状态是行业标准、或这组状态足以覆盖随机/统计比较。`
- Limitations: `IMPROVEMENT 未运行；8-case deterministic fixture；实际系统可能需要置信区间、显著性和更多状态。`
- Course Usage: `Regression record、unknown/incomparable boundary。`
- BuildPilot Implication: `ADOPT — 后续治理设计候选；当前 NOT IMPLEMENTED。`
- Owner: `Article 22 Researcher`
- Verified At: `2026-08-28`

## Evidence 22-E10｜Lab06 known regression capture

- Article: `22｜Eval、Golden Dataset 与 Regression`
- Claim ID: `22-C10`
- Claim: `Lab 06 的冻结 corpus/scorer 能让 baseline PASS，并捕获一个已知 critical regression。`
- Evidence Status: `CONFIRMED`
- Evidence Class: `RUNTIME_OBSERVATION`
- Lab Dependency: `SATISFIED / LAB06 FIXTURE`
- Source Type: `frozen experiment design + fixed inputs + raw runtime observation`
- Source: `S-LAB06`
- Repository: `TwoEggDu/twoegg-tech-stack`
- Commit: `N/A — uncommitted Article22 transaction`
- File: `docs/agent-engineering-course/labs/lab-06-trace-eval/fixtures/；observations/run-a/；observations/run-b/；observations/verification/`
- Symbol: `baseline / known-regression candidates`
- Call Path: `fixed candidate -> deterministic scorer -> per-case delta -> gate`
- Experiment: `Lab 06 / COMPLETE`
- Fixture: `lab06-fixture-v1 / lab06-golden-corpus-r1 / lab06-scorer-v1`
- Trace: `observations/execution-log.md；observations/tdd-red/；observations/tdd-green/；observations/run-a/；observations/run-b/；observations/fault-injection/；observations/verification/`
- Retrieved / Run At: `2026-08-28 Asia/Shanghai`
- Version Scope: `Windows 10.0.19045 / .NET SDK 10.0.301 / net10.0 / BCL-only / offline`
- Reproduction: `使用各 process-record.md 中 exact commands；locked restore/build、GREEN Specs、formal verifier、A/B repeatability 和 fault injections 均有 exit/raw artifact。`
- Observation: `locked restore/build exit 0；valid RED 0/5 后 unchanged GREEN 5/5；baseline 8/8 overall PASS；known regression 7/8、C01 REGRESSION、overall FAIL；formal verifier 2/2；A/B normalized bytes/hash equal。`
- Counter-evidence Searched: `实现可能忽略 group gate、读取 expected shortcut、篡改 threshold 或用不同 corpus；这些均被 acceptance/falsifier 禁止。`
- Interpretation: `冻结 corpus/scorer/manifest 下，该 evaluator 可重复让 baseline 通过并捕获预置 C01 critical regression；结论只覆盖本 fixture。`
- Proves: `Lab06 的冻结 deterministic mechanism 已真实 build/run，并按设计捕获已知 critical regression 与 repeatability。`
- Does Not Prove: `真实 Trace 标签正确、Agent/model 退化已被发现、生产 gate 有效、或跨环境/Provider 泛化。`
- Limitations: `完全 synthetic、fixed candidates、BCL-only、单一 Windows/.NET 环境、无 Provider/model/network/statistics。`
- Course Usage: `Lab 06 execution handoff。`
- BuildPilot Implication: `REJECT — 不把 Lab fixture 冒充 BuildPilot Runtime。`
- Owner: `Article 22 Researcher`
- Verified At: `2026-08-28`

## Evidence 22-E11｜Evaluation ceiling

- Article: `22｜Eval、Golden Dataset 与 Regression`
- Claim ID: `22-C11`
- Claim: `Eval/Regression PASS 只覆盖固定任务、数据、系统版本与判据，不等于生产质量、泛化或统计显著。`
- Evidence Status: `PARTIAL`
- Evidence Class: `OFFICIAL_DOC`
- Source Type: `official framework + official product guidance`
- Source: `S-NIST-RMF`；`S-OAI-EVAL`；`S-GOOGLE-SPLIT`；`S-LAB06`
- Repository: `N/A`
- Commit: `N/A`
- File: `N/A`
- Symbol: `MEASURE 2.3/2.5；task-specific evals；test representativeness`
- Call Path: `fixed eval result -> bounded interpretation -> continued monitoring`
- Experiment: `Lab 06 / COMPLETE / boundary evidence`
- Fixture: `lab06-fixture-v1`
- Trace: `observations/environment/；observations/run-a/；observations/run-b/；observations/verification/`
- Retrieved / Run At: `2026-08-28 Asia/Shanghai`
- Version Scope: `sources as listed in manifest`
- Reproduction: `核对 deployment-like conditions、generalizability limitations、real-world distribution 与 statistically significant test guidance；读取 Lab06 runtime manifest、frozen inputs 与 repeatability evidence。`
- Observation: `NIST 要求说明超出开发条件的泛化限制；OpenAI 要求 task-specific/continuous evaluation；Google 要求 representativeness 和足够样本。Lab06 只观测了 8 个 synthetic fixed candidates、deterministic scorer 与单一 Windows/.NET 环境。`
- Counter-evidence Searched: `某些 deterministic contract tests 可对自身边界给出完整判定，但仍不覆盖未测试的生产风险。`
- Interpretation: `Lab06 的 repeatable PASS/FAIL 是 scope-bound decision；它为边界披露提供具体证据，但不能单独证明所有 Eval 的通用 ceiling。`
- Proves: `文章必须披露固定 scope 与未覆盖条件；Lab06 成功不能被写成生产质量、泛化或统计显著。`
- Does Not Prove: `Lab06 的 8 cases 有统计意义、生产代表性或跨 Provider 泛化。`
- Limitations: `不同来源覆盖 risk framework、product eval 与 ML split，组合推理保持 PARTIAL。`
- Course Usage: `验证边界、发布门禁上限。`
- BuildPilot Implication: `ADOPT — 任何 future gate 必须带 scope/limitations；DESIGN ONLY。`
- Owner: `Article 22 Researcher`
- Verified At: `2026-08-28`

## Evidence 22-E12｜Course and future-asset boundary

- Article: `22｜Eval、Golden Dataset 与 Regression`
- Claim ID: `22-C12`
- Claim: `Article 22 是 Part IV 必修收束且 Required Lab 06；BuildPilot design-only；Article23 SKIP/PLANNED/zero assets；Article24 forbidden/zero assets。`
- Evidence Status: `CONFIRMED`
- Evidence Class: `OFFICIAL_DOC`
- Source Type: `repository canonical and transaction artifacts`
- Source: `S-REPO`
- Repository: `TwoEggDu/twoegg-tech-stack`
- Commit: `470c362567d71aa4b7e5d951406b9af92b5b1adf`
- File: `docs/agent-engineering-series-plan.md；docs/agent-engineering-course/articles/22-eval-golden-dataset-regression/article-card.md；README.md`
- Symbol: `Part IV / Engineering Labs route / Frozen Boundaries`
- Call Path: `canonical -> Article Card -> current transaction`
- Experiment: `N/A`
- Fixture: `Lab06 planned`
- Trace: `docs/agent-engineering-course/articles/22-eval-golden-dataset-regression/subagent-trace.md`
- Retrieved / Run At: `2026-08-28 Asia/Shanghai`
- Version Scope: `current repository transaction`
- Reproduction: `读取 canonical rows 22—24、Lab06 route 与 Article22 kickoff artifacts；检查 future directories/assets absent。`
- Observation: `22 为 non-optional L，Lab06 已在当前 transaction 中完成 Observation 与 Evidence Merge；23 Advanced/Optional；24 下一 Part；当前文件扫描确认 Article23/24 asset count 仍为 0。`
- Counter-evidence Searched: `检查当前 task 是否授权 Article23/24 或 BuildPilot Runtime；没有。`
- Interpretation: `这是 repository-local current scope fact。`
- Proves: `本 worker 只能交付 Article22 final Evidence Merge；不得启动未来文章。`
- Does Not Prove: `Article22 已发布、Part IV Audit 已通过，或 Article23/24 已获启动权。`
- Limitations: `未来 canonical 变更需独立流程；本 worker 无权修改。`
- Course Usage: `scope guard、收束和 handoff。`
- BuildPilot Implication: `DEFER — 保持 DESIGN / NOT IMPLEMENTED / NOT RUN。`
- Owner: `Article 22 Researcher`
- Verified At: `2026-08-28`

## Evidence Merge Trace

| Claim | Experiment | Observation | Evidence Interpretation | Claim Status |
|---|---|---|---|---|
| `22-C04` | frozen corpus/candidate manifests 被 formal runs 与 mismatch injection 使用 | results 保存 case/dataset/scorer identity；scorer v2 被拒绝比较 | schema 在本 fixture 可审计地工作，但通用字段要求仍是课程设计 | `PROPOSAL` |
| `22-C06` | aggregate、critical、threshold、baseline 与 comparability 分栏计算 | candidate aggregate threshold PASS，同时 critical/overall FAIL | 分账机制在 fixture 中可行；release policy 的通用正确性未证明 | `PARTIAL` |
| `22-C07` | baseline 与 known-regression 共享冻结合同，C01 是唯一 injected critical fault | `0.875 aggregate PASS / 0.5 critical FAIL / overall FAIL` | hard critical gate 在本 fixture 确实阻止平均值掩盖退化 | `CONFIRMED / LAB06 FIXTURE-SCOPED` |
| `22-C09` | FI-01 regression、FI-02 missing、FI-03 manifest mismatch | `REGRESSION + UNCHANGED`、`UNKNOWN`、`INCOMPARABLE` 被分别保存并 fail closed | 四条执行路径有 runtime evidence；`IMPROVEMENT` 未执行，五状态不是标准 | `PARTIAL` |
| `22-C10` | locked restore/build、RED/GREEN、formal A/B、independent verifier | baseline `8/8 PASS`；known regression `7/8 FAIL`；verifier `2/2`；A/B byte/hash equal | frozen evaluator 可重复捕获预置 C01 regression | `CONFIRMED / LAB06 FIXTURE-SCOPED` |
| `22-C11` | fixed 8-case synthetic inputs 与单一 Windows/.NET environment | deterministic output 可重复；没有 Agent/model/production traffic/statistical sampling | 成功只能解释为 fixture-scoped mechanism evidence | `PARTIAL` |

Raw chain anchor：`docs/agent-engineering-course/labs/lab-06-trace-eval/observations/`。10/10 recorded hashes match current bytes；baseline 与 regression 的 Run A/B bytes 分别相等。FI-02 Runtime native exit=`2`，FI-03 native exit=`3`；outer-shell generic non-zero 显示和首次 ad-hoc `SequenceEqual` 调用错误均保留为 tooling limitation，不改变 normalized observations。

## Evidence Gate Decision

- `EVIDENCE_MERGE`: `PASS` — `Experiment -> Observation -> Evidence Interpretation -> Claim Status` 已落盘，raw observations、Design/Hypothesis/Acceptance 均未被改写。
- `EVIDENCE_GATE`: `PASS` — 12/12 Claims 与 12/12 Cards 完整；final counts=`3 CONFIRMED / 6 PARTIAL / 3 PROPOSAL / 0 BLOCKED`；每张 Card 均保留 Proves、Does Not Prove、Limitations 与 Counter-evidence。
- Core behavioral Claims：`22-C07 / 22-C10 = CONFIRMED` only inside Lab06；`22-C09 = PARTIAL` because `IMPROVEMENT` was not run；不存在行为性 `BLOCKED` Claim。
- Lab scope：8 个 synthetic cases、fixed candidates、deterministic exact/rule scorer、Windows/.NET 10.0.301；不覆盖 semantic/human judge、真实 Trace curation、Agent/model variability、production traffic、security/compliance 或统计显著性。
- BuildPilot：`DESIGN / NOT IMPLEMENTED / NOT RUN` outside the fixture；Article23/24 asset count=`0`。
- Next allowed worker gate: `OUTLINE`。
