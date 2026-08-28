# Article 22 Research｜Eval、Golden Dataset 与 Regression

> Gate：`EVIDENCE_MERGE + EVIDENCE_GATE`
> Article Type：`PRINCIPLE / LAB_ARTICLE`
> Research Date：`2026-08-28 (Asia/Shanghai)`
> Evidence Gate：`PASS`；Required Lab 06 已真实执行并完成 Researcher Evidence Merge。

## 研究边界

- 本文解决“修复以后还会不会再坏”的评估合同，不写成某个 Provider 的 Evals API 教程。
- Article 21 只交付 candidate trace slice、lineage、版本/脱敏/effect/unknown refs；它不授予 Golden acceptance，也不决定 oracle、metric、threshold、baseline 或 Regression verdict。
- BuildPilot 仅为 `DESIGN / NOT IMPLEMENTED / NOT RUN`。Lab 06 是独立、完全本地的课程 fixture，不是 BuildPilot Runtime 或生产证据。
- Article 23 保持 `Advanced / Optional / SKIP / PLANNED / ZERO ASSETS`；Article 24 不启动且零资产。
- 只使用官方规范/官方产品文档、原始论文和仓库 canonical 作为技术来源。产品文档按访问日冻结语义，不把 hosted page 当永久 API 合同。

## Research Questions 与当前回答

| RQ | 问题 | Preliminary Answer | 状态 |
|---|---|---|---|
| `22-RQ01` | Demo、Test、Benchmark、Eval、Regression 怎样分账？ | 课程采用目的分账：Demo 展示一次能力；Test 判定局部合同；Benchmark 在固定协议下提供可比较测量；Eval 把目标、数据、判据与结果组合成评估；Regression 比较受控基线与候选并保存变化。该五分法是课程模型，不宣称行业统一。 | `PROPOSAL` |
| `22-RQ02` | 可审计 Eval 的最小合同是什么？ | 至少要固定 objective、case identity/input、dataset lineage/revision、oracle/scorer、metric/aggregation、threshold、baseline、system/version manifest 与 verdict policy。官方来源直接支持其中大部分关注面；字段组合属于课程 Proposal。 | `PARTIAL` |
| `22-RQ03` | Trace candidate 怎样成为 Golden sample？ | 必须经过 scope/consent/redaction、去重、label/oracle review、acceptance policy、reviewer、revision 与 split assignment；Trace presence/provenance 不等于 acceptance。 | `PROPOSAL` |
| `22-RQ04` | Eval Case 应保存什么？ | 课程 Proposal：稳定 case ID、task/input ref、lineage、split、criticality、oracle ref、scorer ref、tags、acceptance revision 与适用版本。 | `PROPOSAL` |
| `22-RQ05` | Oracle / scorer 有哪些边界？ | Exact/rule/structured scorer 可重复但覆盖面窄；semantic/model judge 能处理变体但存在 rubric、position、verbosity 与 calibration 风险；human judgment 也需 rubric、agreement 与 review history。 | `PARTIAL` |
| `22-RQ06` | Metric、threshold、baseline、aggregation、uncertainty 为什么不能合并？ | Metric 定义测什么，aggregation 决定怎样汇总，threshold 是决策边界，baseline 是比较对象，uncertainty 限制置信范围。NIST 要求 metrics、benchmarks、uncertainty 与文档化；本文具体字段是课程设计。 | `PARTIAL` |
| `22-RQ07` | 总分通过能否覆盖关键失败？ | 不能自动覆盖。Lab 06 在冻结的 8-case fixture 中观察到 candidate `7/8 = 0.875`、aggregate threshold PASS，但 critical `1/2 = 0.5`、overall FAIL；这只确认该 hard-gate 机制，不规定所有 Eval 都必须采用同一 gate。 | `CONFIRMED / LAB06 FIXTURE-SCOPED` |
| `22-RQ08` | 怎样避免 leakage 与 eval overfitting？ | 固定 train/dev/test 或 development/regression/canary 作用域，保存 split lineage、去重并避免反复用最终集合指导修改；重复使用会让集合“wear out”。该结论对 ML 有直接官方支持，映射到 Agent app eval 时保持边界。 | `PARTIAL` |
| `22-RQ09` | Regression verdict 应保存什么？ | 课程 Proposal 先判 comparability，再分别记录 `IMPROVEMENT / REGRESSION / UNCHANGED / UNKNOWN / INCOMPARABLE`。Lab 06 已实际观察 `REGRESSION`、`UNCHANGED`、`UNKNOWN`、`INCOMPARABLE`，但未触发 `IMPROVEMENT`，因此只确认该 fixture 中的四条已执行路径。 | `PARTIAL / LAB06 FIXTURE-SCOPED` |
| `22-RQ10` | Lab 06 能否捕获已知退化？ | 能，在冻结范围内：同一 corpus/scorer/manifest 下 baseline `8/8 PASS`；候选仅破坏 C01，得到 `7/8`、aggregate threshold PASS、critical/overall FAIL，并记录 `C01=REGRESSION`、其余 7 个 `UNCHANGED`。 | `CONFIRMED / LAB06 FIXTURE-SCOPED` |
| `22-RQ11` | Eval PASS 能否证明生产质量或跨模型泛化？ | 不能。来源要求 task/deployment-context relevance、generalizability limitation、持续评估与 human calibration；单一 fixture 不提供统计显著性或生产验收。 | `PARTIAL` |
| `22-RQ12` | 本篇怎样收束 Part IV？ | 仓库事实确认 Article 22 是 Part IV 必修收束，Required Lab 06；23 Optional 且本次 SKIP；24 禁止启动；BuildPilot 仍 design-only。 | `CONFIRMED` |

## 五种活动的课程分账

| Activity | 主要问题 | 最小输出 | 不能冒充 |
|---|---|---|---|
| Demo | 这一次能否展示目标路径？ | scenario + one observed run | repeatability、coverage、Regression |
| Test | 一个局部确定性合同是否满足？ | assertion + pass/fail + raw failure | system-level quality 或 real-world representativeness |
| Benchmark | 固定协议下系统如何被测量/比较？ | protocol + dataset + metric + score | task-specific release decision 或生产收益 |
| Eval | 对目标场景，系统行为是否满足已声明判据？ | objective + dataset + scorer + metrics + limitations | 永久真值或未覆盖风险 |
| Regression | 相对可比 baseline，哪些行为改善、退化或无法判断？ | comparable manifests + per-case delta + gates + verdict | 只凭最新总分得出“修复有效” |

这张表是 `22-C01 = PROPOSAL`。OpenAI 官方文档明确区分行业 benchmark、数值 score 与应用自定义 eval，并把“vibe-based eval”列为反模式；NIST AI RMF 又把 software testing、performance assessment、benchmark、uncertainty 与持续测量并列。它们支持需要分账，但不规定本文五种名称的完整互斥分类。

## 抽象模型：Eval Contract 与可比性先行

```text
Objective / Risk
  -> Accepted Dataset Revision
  -> Case + Oracle / Scorer Revision
  -> System-under-test Manifest
  -> Per-case Observation
  -> Metric + Aggregation + Thresholds
  -> Baseline Comparability
  -> Per-case Change Classification
  -> Release / Review Verdict + Limitations
```

课程建议先检查可比性，再计算变化。至少比较：

- corpus ID/revision 与 case set；
- scorer ID/version、rubric/oracle revision；
- output schema 与 normalization；
- model/provider/runtime/tool/policy/prompt/harness versions（适用时）；
- budget、attempt/retry、seed/time/external-data boundary（适用时）。

必要 manifest 不一致时，结果应为 `INCOMPARABLE`；缺 observation 或 scorer 无资格判断时应为 `UNKNOWN`。它们不是失败率的另一种写法，也不得被强制折成 0 分。

## Trace candidate 到 accepted Golden sample

```text
candidate trace slice
  -> scope / permission / sensitivity check
  -> payload availability + redaction record
  -> de-duplication / leakage check
  -> task + oracle / label review
  -> acceptance policy + reviewer decision
  -> dataset revision + split assignment
  -> immutable sample identity for one eval run
```

`Datasheets for Datasets` v8 为 motivation、composition、collection、use、distribution、maintenance 等文档面提供原始论文先例；OpenAI 2025 eval guidance 把 golden examples 描述为领域专家判断形成、持续维护的 reference。它们都不证明任一 Trace 自动成为 Golden，也不规定本文 acceptance state machine。

建议的 Sample Lineage（`COURSE PROPOSAL`）：

```yaml
sample_id: LAB06-C01
source_kind: SYNTHETIC_COURSE_FIXTURE
source_trace_ref: synthetic://lab06/C01
source_revision: lab06-fixture-v1
redaction: NOT_APPLICABLE_SYNTHETIC
label_oracle_revision: lab06-oracle-v1
acceptance_policy: lab06-acceptance-v1
acceptance_decision: ACCEPTED_FOR_FIXTURE
split: REGRESSION
dataset_revision: lab06-golden-corpus-r1
```

这里的 `ACCEPTED_FOR_FIXTURE` 只表示进入本地课程 corpus，不表示生产数据授权、统计代表性或跨系统 truth。

## Oracle / Scorer 分账

| 类型 | 适合 | 主要失真风险 | 最低披露 |
|---|---|---|---|
| Exact | canonical token、枚举、digest | 合法变体被错杀 | canonicalization/version |
| Rule-based | schema、字段关系、invariant | 规则未覆盖的质量不可见 | rule IDs/version/coverage |
| Structured execution | compile/test/query/result | environment 与 hidden dependency | command/env/exit/raw output |
| Semantic / model judge | 多个可接受表达、复杂 rubric | position/verbosity/rubric/model drift | judge model/rubric/order/calibration |
| Human-judged | 领域价值、歧义或高风险判断 | disagreement、fatigue、policy drift | rubric/reviewer/agreement/history |

OpenAI hosted docs 当前列出 string check、text similarity 与 model grader，并明确 LLM-as-judge 可能有 position/verbosity bias，建议以人类标注校准。该产品文档只证明这些 grader 形态和已披露挑战，不证明其适合所有任务或能提供无偏真值。

## Metric、Gate 与 Verdict

```text
metric             = 对 observation 的测量规则
aggregation        = case/group 指标如何汇总
threshold          = 某项测量的决策边界
baseline           = 比较对象与其固定 manifest
uncertainty        = 样本/随机性/测量误差带来的限制
release gate       = 多项必要条件的布尔组合
regression verdict = 对变化与可比性的结构化解释
```

Lab 06 冻结两个同时成立的门：

```text
aggregate_accuracy >= 0.80
AND critical_accuracy == 1.00
AND missing_or_unknown == 0
```

已知退化候选实际得到 `7/8 = 0.875`，故 aggregate threshold 单独通过；但它破坏一个 critical case，critical accuracy 为 `0.5`，overall gate 因而失败。该观测只把 `22-C07 / 22-C10` 升级到 Lab06 fixture-scoped `CONFIRMED`，不外推生产风险校准。

## Data Split、Leakage 与“测到会背答案”

Google ML Crash Course（last updated `2025-12-03 UTC`）直接说明：training、validation、test 要分开；反复用同一个 test set 指导修改会让系统拟合该集合；重复样本会污染测试；集合还要考虑真实分布与统计意义。本文只借用这一窄原理：

- development set 可以频繁用于修正；
- regression corpus 用于每次变更的已知合同守护；
- holdout/canary 用于较少暴露的独立检查；
- 样本从 Trace 进入任一 split 时必须记录 lineage 和 exposure；
- 看到 regression answer 后修复系统，不得悄悄改 oracle 或 threshold 来换 PASS。

这些做法不能保证无 leakage，也不自动让小样本具备代表性。Lab 06 只是 8 个 synthetic case 的机制验证，不作统计外推。

## Claim Register

| Claim ID | 主张摘要 | Preliminary Status | Lab Dependency |
|---|---|---|---|
| `22-C01` | Demo/Test/Benchmark/Eval/Regression 应按目的分账，不能互相替代。 | `PROPOSAL` | `NONE` |
| `22-C02` | 可审计 Eval 需要显式 objective、dataset、scorer、metric、threshold、baseline 与 version manifest。 | `PARTIAL` | `NONE` |
| `22-C03` | Trace candidate 必须经 lineage/review/acceptance/split 才能成为 fixture-scoped Golden sample。 | `PROPOSAL` | `NONE` |
| `22-C04` | Eval Case 应具有稳定 identity、input/lineage、oracle/scorer、criticality、revision 与 applicability。 | `PROPOSAL` | `NONE` |
| `22-C05` | Exact/rule/structured/semantic/human scorer 的证明范围与错误边界不同。 | `PARTIAL` | `NONE` |
| `22-C06` | Metric、aggregation、threshold、baseline、uncertainty 与 release gate 是不同责任面。 | `PARTIAL` | `NONE` |
| `22-C07` | 在 Lab06 冻结的 8-case exact/rule fixture 中，hard critical-case gate 阻止 aggregate threshold PASS 掩盖已知关键退化。 | `CONFIRMED` | `SATISFIED / LAB06 FIXTURE` |
| `22-C08` | split 污染、重复样本与反复暴露会降低对未见数据表现的信心。 | `PARTIAL` | `NONE` |
| `22-C09` | Regression 应保留 improvement/regression/unchanged/unknown/incomparable，而非只留总分 delta；Lab06 已执行其中 regression/unchanged/unknown/incomparable 路径。 | `PARTIAL` | `SATISFIED FOR OBSERVED PATHS / IMPROVEMENT NOT RUN` |
| `22-C10` | Lab 06 的冻结 corpus/scorer 能让 baseline PASS，并捕获一个已知 critical regression。 | `CONFIRMED` | `SATISFIED / LAB06 FIXTURE` |
| `22-C11` | Eval/Regression PASS 只覆盖固定任务、数据、系统版本与判据，不等于生产质量、泛化或统计显著。 | `PARTIAL` | `NONE` |
| `22-C12` | Article 22/Lab06/BuildPilot/Article23/24 的课程边界如 canonical 所述。 | `CONFIRMED` | `NONE` |

Coverage=`12 / 12`；Final posture=`3 CONFIRMED / 6 PARTIAL / 3 PROPOSAL / 0 BLOCKED`。`22-C07` 与 `22-C10` 的升级严格限于 `lab06-fixture-v1 / corpus r1 / scorer v1 / Windows + .NET 10.0.301`；`22-C09` 因 `IMPROVEMENT` 未执行且五状态仍属课程模型而保持 `PARTIAL`。

## 最小 Primary Evidence Set 与漂移边界

| Source | Identity / Date | 使用范围 | 漂移与限制 |
|---|---|---|---|
| NIST AI RMF 1.0 | `NIST AI 100-1`，2023-01；访问 `2026-08-28` | TEVV、metrics、benchmarks、uncertainty、test/tool documentation、regular evaluation、generalizability limitation | NIST 页面注明 1.0 正在修订；不规定本文 schema 或 release gate |
| OpenAI Evaluation Best Practices | hosted official docs；访问 `2026-08-28` | objective/dataset/metrics/continuous eval、logs-to-cases、human calibration、vibe anti-pattern、grader limitations | 动态产品文档；页面公告 legacy Evals platform 计划于 2026-10/11 退役；只采用概念指导，不依赖 API longevity |
| Google MLCC Dataset Split | page last updated `2025-12-03 UTC`；访问 `2026-08-28` | train/validation/test、wear-out、duplicates、representativeness | ML 教学文档；不自动覆盖所有 Agent application eval |
| Datasheets for Datasets | arXiv `1803.09010v8`，2021-12-01；CACM 2021-12；访问 `2026-08-28` | dataset motivation/composition/collection/use/distribution/maintenance documentation precedent | 原始论文 Proposal；不定义 Golden acceptance 或 Agent Trace schema |
| Repo canonical + Published Article 21 | repository state at Article22 kickoff | ownership、Lab06、future-Article 与 Trace-to-Eval handoff | repository-local course fact，不是行业标准 |

## Counter-evidence 与替代解释

- “一个总分就足够”可能在所有 case 等价、无 critical requirement 的窄 fixture 中成立；本文仍把 gate 设计为显式 policy，不宣称所有 eval 都必须分组。
- “exact scorer 最客观”只在答案可 canonicalize 且等价类完整时成立；合法变体会产生 false negative。
- “LLM judge 能覆盖语义”不等于无偏；官方文档自己列出 position/verbosity bias，human annotation 也有成本和 disagreement。
- “固定 Golden corpus 保证稳定”忽略 test-set wear-out、现实分布漂移、oracle 错误与 benchmark contamination。
- “Trace 来自真实事故所以更有价值”可能成立，但仍需权限、脱敏、去重、label review 和 acceptance；真实来源不等于正确 oracle。
- “Regression PASS 表示系统变好”只在 manifest 可比且覆盖面足够时有意义；新增能力、scorer/corpus 变化或 missing result 可让比较变成 `UNKNOWN / INCOMPARABLE`。

## Lab 06 Evidence Merge

解释顺序固定为 `Experiment -> Observation -> Evidence Interpretation -> Claim Status`：

| Claim | Experiment | Observation | Evidence Interpretation | Final Status |
|---|---|---|---|---|
| `22-C07` | frozen baseline 与 known-regression 使用同一 corpus/scorer/manifest；aggregate 与 critical hard gate 并存 | baseline `8/8 PASS`；candidate `7/8`、aggregate `0.875 PASS`、critical `0.5 FAIL`、overall `FAIL` | 在本 fixture 内，独立 critical gate 的确阻止 aggregate threshold 掩盖 C01 退化；不推出该阈值或 hard gate 适合所有任务 | `CONFIRMED / LAB06 FIXTURE-SCOPED` |
| `22-C09` | FI-01 known regression、FI-02 missing N06、FI-03 scorer v2 mismatch | `C01=REGRESSION`、其余 7 个 `UNCHANGED`；missing=`UNKNOWN`；mismatch=`INCOMPARABLE`；均 fail closed | 证明本实现能保留四类已执行状态；`IMPROVEMENT` 未运行，五状态也不是行业标准 | `PARTIAL` |
| `22-C10` | locked restore/build、RED `0/5`、GREEN `5/5`、formal A/B 与独立 verifier | baseline `8/8 PASS`；known regression `7/8 FAIL`；formal verifier `2/2`；A/B bytes 与 SHA-256 相同 | 在冻结本地 deterministic fixture 中，evaluator 可重复捕获已知 critical regression | `CONFIRMED / LAB06 FIXTURE-SCOPED` |
| `22-C11` | 固定 synthetic inputs、exact/rule scorer 与单一 Windows/.NET 环境 | 结果可重复，但只有 8 cases，candidate 不是 Agent/model 生成 | 该成功只支持 scope-bound 解释，不能升级成生产质量、跨模型泛化或统计显著性 | `PARTIAL` |

Raw Evidence：`docs/agent-engineering-course/labs/lab-06-trace-eval/observations/`。10 条冻结输入/结果 SHA-256 全部复核一致；Run A/B baseline 与 regression bytes 均相等。TDD RED/GREEN、formal verifier 与 FI-02/FI-03 的原始失败路径均保留；外层 shell generic exit 与 Runtime native `2/3` 的差异、首次 ad-hoc `SequenceEqual` 调用错误也没有被隐藏。

Evidence Gate：`PASS`。没有核心行为性 Claim 为 `BLOCKED`；所有 `PARTIAL / PROPOSAL` 均已收窄，Author 不得越过上述 fixture、来源与未执行路径。

## References

- https://airc.nist.gov/airmf-resources/airmf/5-sec-core/
- https://www.nist.gov/publications/artificial-intelligence-risk-management-framework-ai-rmf-10
- https://developers.openai.com/api/docs/guides/evaluation-best-practices
- https://platform.openai.com/docs/api-reference/graders
- https://openai.com/index/evals-drive-next-chapter-of-ai/
- https://developers.google.com/machine-learning/crash-course/overfitting/dividing-datasets
- https://arxiv.org/abs/1803.09010
