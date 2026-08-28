# Article 22 Outline｜Eval、Golden Dataset 与 Regression：修复以后还会不会再坏

## Outline contract

- Article Type: `PRINCIPLE / LAB_ARTICLE`
- Course Weight: `L / Major Core Lesson`
- Teaching Spine: Article 21 candidate Trace handoff -> problem space（一次修复通过不等于系统改善）-> abstract model（Eval Contract、Golden lifecycle、Case/Scorer/Metric/Verdict）-> concrete implementation and experiment（Lab 06）-> engineering decisions（release gate、leakage、proof ceiling）-> Part IV boundary
- Core Claim Scope: `22-C01`—`22-C12` only；不新增 core Claim / Evidence Card
- Evidence Posture: `3 CONFIRMED / 6 PARTIAL / 3 PROPOSAL / 0 BLOCKED`
- Fixture-confirmed Claims: `22-C07`、`22-C10` only within `lab06-fixture-v1 / corpus r1 / scorer v1 / Windows + .NET 10.0.301`
- Partially observed verdict Claim: `22-C09 PARTIAL`；只执行了 `REGRESSION / UNCHANGED / UNKNOWN / INCOMPARABLE`，`IMPROVEMENT` 未执行
- Required Lab: `Lab 06｜Trace + Eval / VERIFIED / EVIDENCE_MERGED / FIXTURE-SCOPED`
- BuildPilot: `DESIGN / NOT IMPLEMENTED / NOT RUN` outside the Lab-owned fixture
- Future boundary: Article 23=`Advanced / Optional / SKIP / PLANNED / ZERO ASSETS`；Article 24=`FORBIDDEN / ZERO ASSETS`；正文只声明边界，不预告或展开其内容
- Draft fact boundary: Draft 只能重组 `research.md`、`evidence.md`、Published Article 21 handoff 与 Lab06 retained observations 中已落盘的事实；若需要新的核心事实、数值、产品行为或行业标准结论，必须 `RETURN_TO_RESEARCH`

> 如果这篇只记一句话：`修复是否可靠，不取决于这次 Demo 看起来是否成功，而取决于一份版本化、可比较的评估合同，能否保留关键退化、未知与不可比。`

## Reader transformation

读者开始时可能只有“修完后再跑几个例子”的直觉。文章结束时，读者应能：

1. 区分 Demo、Test、Benchmark、Eval 与 Regression 的目标和输出，不让任一活动替另一活动作结论。
2. 把 Trace candidate 经 lineage、review、acceptance 与 split assignment 转成一个有边界的 Golden sample candidate，而不是自动晋升。
3. 写出包含 objective、dataset revision、case、oracle/scorer、metric、threshold、baseline、manifest 与 verdict policy 的 Eval Contract。
4. 先判断 comparability，再解释 improvement、regression、unchanged、unknown 与 incomparable；不会把缺观测或版本漂移折成普通分数。
5. 读懂 Lab06 为什么在 aggregate `0.875` 仍过线时判 overall `FAIL`，同时准确说明这只证明一个 8-case synthetic fixture 的机制。
6. 把 Eval 结果接入 release gate，同时拒绝把固定集合 PASS 写成生产质量、跨模型泛化或统计显著性。

## Teaching Spine

```text
Article 21 provides candidate trace slices + lineage, not Golden truth
  -> one repaired example passes, but "better" and "will not regress" remain unanswered
  -> separate Demo / Test / Benchmark / Eval / Regression by decision ownership
  -> freeze an Eval Contract before interpreting a score
  -> curate candidate traces into accepted, versioned dataset samples
  -> bind each Eval Case to identity, lineage, oracle/scorer and applicability
  -> choose scorer families with explicit error boundaries
  -> keep metric / aggregation / threshold / baseline / uncertainty separate
  -> check manifest comparability before classifying change
  -> preserve REGRESSION / UNCHANGED / UNKNOWN / INCOMPARABLE;
     keep IMPROVEMENT explicitly unexecuted in Lab06
  -> protect splits from duplication, leakage and repeated exposure
  -> use Lab06 to observe one deterministic mechanism, not to narrate raw commands
  -> turn the result into a bounded release gate and an explicit proof ceiling
  -> close Part IV without starting future Article assets or claiming BuildPilot runtime
```

### Spine checkpoints

| Stage | Reader transformation | Required article artifact | Failure if omitted |
|---|---|---|---|
| Problem space | 从“再跑一次”转向“先说清这次活动拥有哪种决定权” | 五类活动 ownership 表 + 一个 aggregate 过线但关键 case 退化的开场矛盾 | 文章退化成测试术语清单或产品 Evals 教程 |
| Abstract model | 能把 dataset、case、scorer、metric、baseline、manifest、verdict 组织成一份合同 | Eval Contract 链路 + Golden lifecycle + Case schema + scorer/metric/verdict 分账 | 只剩一个总分，无法审计为什么 PASS/FAIL |
| Concrete mechanism | 能从固定输入追到 raw observation 与 Claim ceiling | Lab06 baseline/regression/fault-injection 对照 | 只讲抽象，不证明窄机制能运行 |
| Engineering judgment | 能处理 leakage、critical gate、unknown/incomparable 与 proof ceiling | release gate checklist + split/exposure ledger + limitations | 把回归门禁写成生产质量保证 |
| Course boundary | 能说清本篇完成什么、未来内容未启动什么 | Part IV 收束 + future-asset guard + BuildPilot label | 越界预写未来文章或虚构 Runtime |

## Opening bridge｜“这次修好了”为什么回答不了“以后还会不会再坏”

- Reader Question: 一个失败案例修复后重新运行成功，为什么仍不能宣布系统改善或回归风险关闭？
- Core Questions: `Q1`、`Q8`、`Q9`。
- Claims / Evidence: `22-C01 PROPOSAL / 22-E01`，`22-C07 CONFIRMED FIXTURE-SCOPED / 22-E07`，`22-C10 CONFIRMED FIXTURE-SCOPED / 22-E10`，`22-C11 PARTIAL / 22-E11`。
- Planned teaching move:
  - 接住 Article 21 的冻结 handoff：Trace 只交 candidate slice、lineage、版本/脱敏/effect/unknown refs；不交 Golden acceptance、oracle、metric、threshold、baseline 或 verdict。
  - 用 Lab06 的一个真实矛盾做开场，不先展示命令：known-regression candidate `7/8 = 0.875`，aggregate threshold 单独 `PASS`，但 critical accuracy=`0.5`、overall=`FAIL`。
  - 立即声明证据上限：这不是 Agent/model run，不是生产流量，不是统计实验；只是 fixed synthetic exact/rule fixture 中的 runtime observation。
- Boundary / Non-goal:
  - 不把开场写成“总分无用”；无 critical requirement 的窄任务可以采用不同 policy。
  - 不把一次 Lab fail 写成真实系统事故，也不声称 `0.80 / 1.00` 是通用阈值。
- Transition purpose: 从“修复后的单次成功”过渡到“不同验证活动究竟拥有哪一种结论”。
- Learning check: 如果修复样例已经通过，仍缺哪三类信息才能回答 regression？期望答案至少包含固定数据/判据、可比 baseline/manifest、逐 case/gate verdict。
- Practical action: 让读者先写下当前团队所谓“回归测试”实际保存了什么，再用下一节的 ownership 表检查它是否只是 Demo 或 Test。

## Part A｜问题空间：先把五种活动的决定权分开

### 1. Demo、Test、Benchmark、Eval、Regression 分别回答什么

- Reader Question: 五个词经常被混用时，怎样按目的而不是按工具名分账？
- Core Question: `Q1`。
- Claims / Evidence: `22-C01 PROPOSAL / 22-E01`。
- Required table `T22-01`:

  | Activity | Primary question | Minimum output | Must not impersonate |
  |---|---|---|---|
  | Demo | 这一次能否展示目标路径？ | scenario + one observed run | repeatability、coverage、Regression |
  | Test | 一个局部确定性合同是否满足？ | assertion + pass/fail + raw failure | system-level quality、representativeness |
  | Benchmark | 固定协议下怎样测量/比较？ | protocol + dataset + metric + score | task-specific release decision |
  | Eval | 对声明目标，行为是否满足已声明判据？ | objective + dataset + scorer + metrics + limitations | 永久真值、未覆盖风险 |
  | Regression | 相对可比 baseline，哪些行为改变？ | manifests + per-case delta + gates + verdict | 只凭最新总分宣布修复有效 |

- Evidence wording:
  - NIST/OpenAI 来源只支持“testing、benchmark、application-specific eval、continuous measurement 等关注面需要分账”。
  - 五分法是课程 ownership model，不是行业标准、穷尽或严格互斥 taxonomy。
- Figure responsibility: `T22-01` 是本节主视觉；最后一列专门阻止结论越权。
- Transition purpose: 分清活动后，下一步不问“跑哪个工具”，而问“做决定前必须冻结哪些合同”。
- Learning check: 一个漂亮的 Demo 是否能兼任 Regression？期望答案：不能；缺固定 baseline、可比 manifest、数据/判据与变化分类。
- Practical action: 为团队每个验证入口标一个 primary owner；若同一入口被要求同时证明五件事，列出缺失 artifact。
- Section takeaway: **活动可以共用基础设施，但不能互相借用决定权。**

## Part B｜抽象模型：先冻结 Eval Contract，再解释分数

### 2. 最小 Eval Contract：objective 到 verdict 的完整链路

- Reader Question: 一次 Eval 至少要冻结哪些对象，结果才可审计、可比较、可复现解释？
- Core Questions: `Q2`、`Q5`、`Q7`。
- Claims / Evidence: `22-C02 PARTIAL / 22-E02`，`22-C06 PARTIAL / 22-E06`，`22-C09 PARTIAL / 22-E09`。
- Proposed model `F22-01`:

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

- Minimum contract fields:
  - objective / risk and intended decision；
  - dataset identity/revision, split and lineage；
  - case identity/input, oracle/scorer refs, criticality and applicability；
  - metric, aggregation and uncertainty disclosure；
  - threshold and multi-gate policy；
  - baseline identity；
  - system/provider/model/runtime/tool/policy/prompt/harness version manifest where applicable；
  - comparability policy, change verdict and limitations。
- Evidence wording: official sources directly support objective、dataset、metrics/methods、benchmark、uncertainty 与持续评估 concerns；完整字段组合保持 `COURSE PROPOSAL / PARTIAL`。
- Boundary / Non-goal: 字段齐全不自动保证数据有效、oracle 正确或生产代表性。
- Transition purpose: 合同给出了“要保存什么”；下一节处理最容易被偷换的第一步——candidate Trace 到 accepted Golden sample。
- Learning check: 为什么有 dataset + score 仍不是完整 Eval Contract？期望答案：还缺 objective、scorer/oracle、threshold/baseline、manifest/comparability、verdict policy 与 limitations。
- Practical action: 用上述字段审计最近一次评估报告；把缺失项标为 `UNKNOWN`，不要补默认值。
- Section takeaway: **分数是合同运行后的结果，不是合同本身。**

### 3. Trace candidate 怎样成为 accepted Golden sample

- Reader Question: 真实 Trace 来源是否足以让一个样本自动成为 Golden？
- Core Question: `Q3`。
- Claims / Evidence: `22-C03 PROPOSAL / 22-E03`；Article 21 candidate-to-Eval handoff。
- Proposed lifecycle `F22-02`:

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

- Required distinction:
  - provenance/lineage 说明“从哪里来”；
  - label/oracle review 说明“怎样判断”；
  - acceptance 说明“谁以什么 policy 接纳”；
  - split/exposure 说明“在哪里使用过”；
  - dataset revision 说明“属于哪个固定集合”。
- Example responsibility: 使用 Lab06 `ACCEPTED_FOR_FIXTURE` sample-lineage shape，明确它只代表 synthetic course corpus，不代表生产授权、统计代表性或跨系统 truth。
- Boundary / Non-goal: Datasheets 与 OpenAI guidance 只提供 documentation/expert-curation precedent；本文 state machine 仍是 Proposal。
- Transition purpose: 样本被接受后，还要让每个 Case 的 identity、oracle 与适用版本可单独审计。
- Learning check: `source_trace_ref` 和 `acceptance_decision` 为什么必须分开？期望答案：来源真实性不等于标签正确或获准进入该 split。
- Practical action: 为现有回归样本补一条 lineage/acceptance ledger；无法回答 reviewer、revision 或 split 的样本保持 candidate。
- Section takeaway: **Trace 能提供候选与 provenance，不能自封 Golden truth。**

### 4. Eval Case 与版本清单：一个样本怎样获得稳定身份

- Reader Question: 一个可审计 Case 最小要保存什么，才能在 scorer/corpus/system 变化时知道仍能不能比较？
- Core Question: `Q2`。
- Claims / Evidence: `22-C04 PROPOSAL / 22-E04`，辅助 `22-C02 PARTIAL / 22-E02`。
- Proposed case contract `EX22-01`:

  ```yaml
  case_id: LAB06-C01
  input_ref: synthetic://lab06/C01
  lineage_ref: lab06-fixture-v1
  split: REGRESSION
  criticality: CRITICAL
  oracle_ref: lab06-oracle-v1
  scorer_ref: lab06-scorer-v1
  acceptance_revision: lab06-acceptance-v1
  dataset_revision: lab06-golden-corpus-r1
  applicable_system_manifest: lab06-candidate-v1
  ```

- Teaching responsibility:
  - Case identity 与 input 内容分开；lineage 与 oracle 分开；criticality 是 policy，不是观测事实。
  - corpus/scorer/candidate schema revision 参与 comparability，不应只藏在文件名里。
- Boundary / Non-goal: Lab06 实际使用这些关注面，只能说明该 schema 在本 fixture 可运行；不能升级为所有 Eval 的唯一/充分 schema。
- Transition purpose: Case 合同固定“比较对象”，下一节固定“怎样判”的 scorer/oracle 与误差边界。
- Learning check: 如果 `case_id` 相同但 scorer revision 变了，能否直接算 regression delta？期望答案：先按 comparability policy 判断，不能默认可比。
- Practical action: 给每个 Case 添加 `oracle_ref / scorer_ref / acceptance_revision / applicability`；缺项形成审查任务，而不是被静默填充。
- Section takeaway: **稳定 ID 让样本可追踪，版本与 applicability 才让比较有资格。**

### 5. Oracle / Scorer taxonomy：可重复不等于无误差

- Reader Question: Exact、rule、structured execution、semantic/model judge 与 human judgment 各能证明什么，又会怎样错？
- Core Question: `Q4`。
- Claims / Evidence: `22-C05 PARTIAL / 22-E05`。
- Required table `T22-02`:

  | Type | Suitable for | Main error boundary | Minimum disclosure |
  |---|---|---|---|
  | Exact | canonical token、enum、digest | 合法变体 false negative | canonicalization/version |
  | Rule-based | schema、field relation、invariant | 未覆盖质量不可见 | rule IDs/version/coverage |
  | Structured execution | compile/test/query/result | environment/hidden dependency | command/env/exit/raw output |
  | Semantic/model judge | acceptable variants、complex rubric | position/verbosity/rubric/model drift | judge/rubric/order/calibration |
  | Human-judged | domain value、ambiguity、high risk | disagreement/fatigue/policy drift | rubric/reviewer/agreement/history |

- Teaching responsibility:
  - 区分 oracle（什么算对）与 scorer（怎样判定 observation 是否符合 oracle）。
  - model judge 与 human judge 都需要 rubric/calibration；不能被写成无偏真值。
- Evidence wording: OpenAI hosted docs 当前提供 grader 形态、position/verbosity bias 与 human calibration guidance；不证明 taxonomy 穷尽或某 scorer 适合当前任务。
- Boundary / Non-goal: Lab06 只执行 deterministic exact/rule scorer；semantic/human 路径未运行。
- Transition purpose: scorer 确定“怎样测”；下一节把 measurement 与 decision policy 分开，避免一个总分吞掉关键失败。
- Learning check: Exact scorer 为什么既可重复又可能不正确？期望答案：canonicalization/等价类不完整会错杀合法变体。
- Practical action: 给每个 scorer 写一条 `Proves / Does Not Prove / known error modes`，并记录版本与校准证据。
- Section takeaway: **Scorer 的稳定性只说明判法可重复，不说明 oracle 完整或结论无偏。**

### 6. Metric、aggregation、threshold、baseline、uncertainty 与 release gate 怎样分账

- Reader Question: 为什么 `accuracy=0.875` 不能独自决定发布？
- Core Question: `Q5`。
- Claims / Evidence: `22-C06 PARTIAL / 22-E06`，`22-C07 CONFIRMED FIXTURE-SCOPED / 22-E07`。
- Required responsibility ledger `T22-03`:
  - `metric`: observation 怎样被测量；
  - `aggregation`: case/group 指标怎样汇总；
  - `threshold`: 单项测量的决策边界；
  - `baseline`: 本次变化与谁比较；
  - `uncertainty`: 小样本、随机性、测量误差与未覆盖范围；
  - `release gate`: 多项必要条件怎样布尔组合；
  - `regression verdict`: 怎样解释变化与可比性。
- Concrete gate example from Lab06:

  ```text
  aggregate_accuracy >= 0.80
  AND critical_accuracy == 1.00
  AND missing_or_unknown == 0
  AND manifest_comparable == true
  ```

- Boundary / Non-goal:
  - 公式必须标 `LAB06 FROZEN POLICY / NOT A UNIVERSAL RELEASE STANDARD`。
  - NIST 不规定本文阈值或 hard gate；Lab 只确认该机制在冻结 fixture 中生效。
- Transition purpose: 当 gate 包含 baseline 与 manifest 时，必须先判断能否比较，再谈 improvement/regression。
- Learning check: aggregate threshold 已 PASS，为什么 overall 仍可 FAIL？期望答案：release gate 可以包含独立 critical、missing/unknown、comparability 必要条件。
- Practical action: 把当前总分报告拆成 measurement ledger 与 decision policy；让每个 hard gate 能单独解释。
- Section takeaway: **Metric 负责测量，gate 负责决策；把两者混成一个分数，就失去了失败原因。**

### 7. Comparability-first Regression：变化、未知与不可比不能互相冒充

- Reader Question: 什么时候能说 improvement/regression，什么时候只能说 unknown 或 incomparable？
- Core Question: `Q7`。
- Claims / Evidence: `22-C09 PARTIAL / 22-E09`，辅助 `22-C02 / E02`、`22-C06 / E06`。
- Proposed flow `F22-03`:

  ```text
  compare dataset/scorer/schema/system manifests
    ├─ mismatch --------------------> INCOMPARABLE / fail closed
    └─ comparable
         ├─ observation missing ----> UNKNOWN / fail closed
         └─ compare baseline/candidate pass state
              ├─ fail -> pass ------> IMPROVEMENT
              ├─ pass -> fail ------> REGRESSION
              └─ same state --------> UNCHANGED
  ```

- Evidence wording:
  - 状态集合是课程 Proposal，不声称行业统一。
  - Lab06 实际观察 `REGRESSION`、`UNCHANGED`、`UNKNOWN`、`INCOMPARABLE`；`IMPROVEMENT` 未执行，必须在图和正文中标为 `DEFINED / NOT OBSERVED IN LAB06`。
  - `UNKNOWN` 表示 observation/qualification 缺口；`INCOMPARABLE` 表示合同不允许普通 delta；二者不是另一种 0 分。
- Transition purpose: 即使可比性正确，反复暴露同一集合仍会让 Regression corpus 失去对未见风险的解释力。
- Learning check: scorer v1 -> v2 时 candidate 分数更高，能否直接叫 improvement？期望答案：不能；先按 manifest 规则判 `INCOMPARABLE` 或建立新的桥接证据。
- Practical action: 在 CI/release 输出里保留 manifest diff、per-case verdict counts、unknown/incomparable reason，不只留 pass/fail。
- Section takeaway: **先证明“可以比较”，再讨论“变好还是变坏”。**

### 8. Split、leakage 与 test-set wear-out：测到会背答案以后怎么办

- Reader Question: 为什么固定 Golden corpus 既是回归资产，也会因反复暴露、重复样本和调参而失效？
- Core Question: `Q6`。
- Claims / Evidence: `22-C08 PARTIAL / 22-E08`，辅助 `22-C03 / E03`。
- Planned split model:
  - development set：频繁用于修正；
  - regression corpus：每次变更守护已知合同；
  - holdout/canary：较少暴露的独立检查；
  - 每个样本保留 lineage、dedup、split 与 exposure history；
  - 看见答案后修系统，不静默改 oracle/threshold 换 PASS。
- Evidence wording: Google MLCC 对 train/validation/test、重复样本与 test wear-out 有直接支持；映射到 Agent application eval 保持 `PARTIAL`，不机械复制固定比例。
- Boundary / Non-goal: 这些控制不能保证无 leakage，也不让 8-case Lab06 获得统计代表性。
- Figure responsibility: `F22-04` 用 exposure ledger 画“candidate -> development/regression/holdout”分流，明确 split 不是文件夹命名。
- Transition purpose: 抽象模型已经完整；下一部分用 Lab06 把 Case、Scorer、Gate、Verdict 与证据链落成一个窄机制。
- Learning check: 为什么反复用最终 test set 指导修复会降低其解释力？期望答案：系统/团队逐渐拟合该集合的 peculiarities，未见数据信心下降。
- Practical action: 记录每个样本何时进入哪个 split、被看过多少次、因何 revision；无法追踪 exposure 的样本不承担 holdout 结论。
- Section takeaway: **Golden 不是永不磨损的真值库；它需要版本、暴露与维护记录。**

## Part C｜具体实现与实验：Lab06 怎样把已知退化变成可审计观察

### 9. Lab06 的最小机制：固定合同，不把文章写成命令流水账

- Reader Question: 如何用最小 deterministic fixture 验证“aggregate 过线仍不能掩盖 critical regression”？
- Core Question: `Q8`。
- Claims / Evidence: `22-C04 PROPOSAL / 22-E04`，`22-C06 PARTIAL / 22-E06`，`22-C07 CONFIRMED FIXTURE-SCOPED / 22-E07`，`22-C09 PARTIAL / 22-E09`，`22-C10 CONFIRMED FIXTURE-SCOPED / 22-E10`，`22-C11 PARTIAL / 22-E11`。
- Teaching integration:
  1. **Design panel**：8 个 synthetic cases（2 critical + 6 normal）、固定 corpus r1、scorer v1、baseline/known-regression candidates；known-regression 只破坏 C01。
  2. **Mechanism panel**：三字段 exact/rule case score；aggregate + critical + missing/unknown + comparability gate；baseline/candidate per-case change verdict。
  3. **Observation panel**：只展示能回答本篇问题的 normalized results；命令、TDD stdout 与完整 raw ledger 通过路径引用，不逐条复述。
- Required observation table `T22-04`:

  | Run | Comparable | Aggregate | Critical | Verdict / Overall | Exact teaching point |
  |---|---:|---:|---:|---|---|
  | baseline | true | `8/8 = 1.0` | `2/2 = 1.0` | `PASS / PASS` | frozen baseline qualifies |
  | known regression | true | `7/8 = 0.875`, threshold PASS | `1/2 = 0.5`, gate FAIL | `REGRESSION / FAIL` | C01 被捕获，其他 7 为 UNCHANGED |
  | missing N06 | false | `0.75`（仅作 retained output） | `0.5` | `UNKNOWN / FAIL` | 缺观测 fail closed，不冒充 ordinary delta |
  | scorer v2 mismatch | false | ordinary aggregate absent | absent | `INCOMPARABLE / FAIL` | scorer drift 阻止普通比较 |

- Runtime integrity callout:
  - locked restore/build exit `0`；valid RED=`0/5`，unchanged GREEN=`5/5`；formal verifier=`2/2`。
  - Run A/B baseline 与 regression normalized artifacts 各自 byte-identical，hashes retained。
  - native failure exits `2 / 3` 与外层 shell generic status 分开记录；首次 ad-hoc `SequenceEqual` tooling error 原样保留。
- Raw trace anchors:
  - `docs/agent-engineering-course/labs/lab-06-trace-eval/observations/run-a/baseline/result.json`
  - `docs/agent-engineering-course/labs/lab-06-trace-eval/observations/run-a/known-regression/result.json`
  - `docs/agent-engineering-course/labs/lab-06-trace-eval/observations/fault-injection/missing-n06/result.json`
  - `docs/agent-engineering-course/labs/lab-06-trace-eval/observations/fault-injection/scorer-v2/result.json`
  - `docs/agent-engineering-course/labs/lab-06-trace-eval/observations/verification/`
- Boundary / Non-goal:
  - 不复制 execution-log 全文，不把 TDD 本身写成 Eval correctness 证明。
  - `22-C07 / C10` 只写 fixture-scoped CONFIRMED；`22-C09` 仍 PARTIAL；`IMPROVEMENT` 不得被图示为 observed。
  - candidate outputs 不是 Agent/model 生成；Lab 不是 BuildPilot Runtime。
- Transition purpose: Lab 证明窄机制可以工作；下一节把机制翻译成 release gate 决策，同时明确其不能证明什么。
- Learning check: 哪条 raw result 证明 aggregate PASS 与 overall FAIL 同时发生？期望答案：known-regression `result.json` 中 `aggregate_threshold_pass=true`、`critical_gate_pass=false`、`overall_gate=FAIL`。
- Practical action: 用“Design / Observation / Interpretation”三栏审查自己的 Lab；若 expected 与 observed 混写，先拆开再引用结论。
- Section takeaway: **Lab 的价值不是多一组绿色命令，而是把固定合同、失败路径与 Claim 上限连成可追溯证据链。**

## Part D｜工程判断：把 Eval 接入发布，但不给它超额决定权

### 10. Release gate 应怎样消费结果，又为什么不能冒充生产质量

- Reader Question: Eval/Regression 结果怎样进入发布决策，同时保留未覆盖风险和人工判断？
- Core Question: `Q9`。
- Claims / Evidence: `22-C06 PARTIAL / 22-E06`，`22-C09 PARTIAL / 22-E09`，`22-C11 PARTIAL / 22-E11`。
- Proposed release record `EX22-02`:

  ```text
  contract_ref + dataset/scorer/system manifests
  + comparability status
  + per-case/group metrics
  + hard-gate results
  + regression verdict counts
  + unknown/incomparable reasons
  + uncertainty/limitations
  + decision owner and override/review record
  ```

- Engineering rules:
  1. manifest mismatch 先 fail closed，不计算普通 improvement/regression；
  2. critical、security、policy 或其他声明 hard gates 与 aggregate 分账；
  3. unknown/incomparable 保持一等状态，不强折 0/1；
  4. threshold/release policy 版本化，变更需独立 review，不为当前 candidate 临时降线；
  5. Eval PASS 只对 contract scope 有效；生产监控、canary、human review 与持续 eval 仍是不同责任面。
- Proof ceiling callout:
  - 能证明：Lab06 frozen mechanism repeatably detected its pre-injected critical regression。
  - 不能证明：真实 Trace 标签正确、真实 Agent/model 改善、生产风险受控、跨 Provider/model/环境泛化、统计显著性或 BuildPilot behavior。
- Figure responsibility: `F22-05` 将 `measurement -> gate -> human/release decision -> monitoring feedback` 画成链路，明确 Eval output 不是自动 production truth。
- Transition purpose: gate 规则给出可执行判断；结尾把这些判断压缩成读者可带走的工程动作与 Part IV 边界。
- Learning check: 一个固定 corpus 全 PASS，发布记录至少还要披露哪些上限？期望答案：任务/数据/版本/判据 scope、uncertainty、未覆盖风险、生产/泛化不成立。
- Practical action: 为发布门禁增加 `contract_ref / manifest_comparable / limitations / decision_owner`，并让 threshold 变更留下独立审查记录。
- Section takeaway: **Eval 可以给发布一个可审计输入，不能替发布、生产监控或风险 owner 作全部决定。**

### 11. 一套 Eval / Regression 设计通常怎样写坏

- Reader Question: 哪些捷径会让一套看似有数据、有分数、有 CI 的评估系统失真？
- Core Questions: `Q1`—`Q9` 的反面检查；不新增 Claim。
- Claims / Evidence: `22-C01`—`22-C11 / 22-E01`—`22-E11`。
- Required anti-pattern table `T22-05`:

  | Shortcut | Responsibility swallowed | Minimum correction |
  |---|---|---|
  | `demo passed = eval passed` | fixed task/data/criteria | declare activity owner and missing contract |
  | `trace exists = golden sample` | curation/oracle/acceptance | keep candidate + lineage until review |
  | `one score = release verdict` | groups/critical gates/uncertainty | separate metric, aggregation and gate |
  | `exact scorer = objective truth` | canonicalization/coverage | disclose equivalence and false-negative risk |
  | `LLM judge = semantic oracle` | bias/calibration/rubric drift | version rubric/judge and calibrate with humans |
  | `higher score = improvement` | baseline/manifest comparability | compare manifests first |
  | `missing = failed case` | observation qualification | preserve UNKNOWN / fail closed |
  | `scorer changed, still compare` | measurement contract | preserve INCOMPARABLE or bridge explicitly |
  | `golden corpus is permanent` | exposure/leakage/drift | version, dedup, split and refresh |
  | `threshold moved, candidate passed` | decision-policy integrity | version/review threshold separately |
  | `fixture pass = production quality` | representativeness/generalization | publish proof ceiling and monitoring gap |
  | `Lab06 = BuildPilot runtime` | implementation/observation ownership | retain DESIGN / NOT IMPLEMENTED / NOT RUN |

- Transition purpose: 反模式收敛到“每种 artifact 只能拥有自己的决定权”，为结尾行动清单做准备。
- Learning check: 让读者任选最近一份 eval report，指出至少一个被吞掉的责任面，并写出最小修正。
- Practical action: 把表作为 code-review / release-review checklist，不把它当行业标准 taxonomy。

### 12. 工程行动清单与 Part IV 收束

- Reader Question: 读完后，团队下一次修复应该留下哪组最小可审计产物？
- Core Question: `Q10`。
- Claims / Evidence: `22-C12 CONFIRMED / 22-E12`，并收束 `22-C01`—`22-C11`。
- Minimum reader action bundle:
  1. 写清当前活动是 Demo/Test/Benchmark/Eval/Regression 中的哪种 ownership；
  2. 冻结 objective、dataset/case revisions、oracle/scorer、metrics/gates、baseline 与 system manifest；
  3. 把 Trace sample 保持 candidate，直到 lineage/review/acceptance/split 完整；
  4. 保存 per-case observation、group metrics、unknown/incomparable 与 raw failure；
  5. 先判 comparability，再给 change verdict；
  6. 记录 split exposure、dedup 与 refresh；
  7. 将 Eval 作为 release input，同时披露 proof ceiling；
  8. 对任何 policy/threshold 变更做独立 version/review。
- Course boundary:
  - Article 22 是当前 Part IV 必修收束，Required Lab06 已在本 transaction 中真实执行并完成 Evidence Merge。
  - Article 23 只记录 `Advanced / Optional / SKIP / PLANNED / ZERO ASSETS`；不预览内容、不创建资产。
  - Article 24 `FORBIDDEN / ZERO ASSETS`；不启动、不创建、不预写。
  - BuildPilot 在 Lab fixture 外保持 `DESIGN / NOT IMPLEMENTED / NOT RUN`。
- Transition purpose: 从本篇工程动作回到最短判断，不建立 future-Article bridge 或暗示下一 transaction 已获授权。
- Final learning check: “Lab06 PASS”最精确的完整句子是什么？期望答案必须包含 frozen 8-case synthetic fixture、corpus/scorer/version/environment、known critical regression、repeatability 与不外推 production/generalization/statistics。
- Closing sentence: `修复不会因为这次看起来成功就变得可靠；可靠来自一份能冻结比较条件、暴露关键退化，并诚实保留 unknown 与 incomparable 的评估合同。`

## Core Question coverage（10 / 10）

| Core Question | Primary sections | Claims / Evidence | Lab role | Required boundary |
|---|---|---|---|---|
| Q1 五种活动怎样分账 | Opening, 1, 11 | `C01 / E01` | aggregate-vs-gate 只作反例 | 五分法=COURSE PROPOSAL |
| Q2 最小 Eval Case/Contract | 2, 4 | `C02/C04 / E02/E04` | actual fixture uses versioned identities | schema 不称唯一/充分 |
| Q3 Trace candidate 到 Golden | 3 | `C03 / E03` | synthetic `ACCEPTED_FOR_FIXTURE` example | provenance != acceptance/truth |
| Q4 scorer/oracle errors | 5 | `C05 / E05` | exact/rule only | semantic/human 未执行 |
| Q5 metric/threshold/baseline/aggregation/uncertainty | 2, 6, 10 | `C06/C07 / E06/E07` | aggregate PASS + critical FAIL | C07 fixture-scoped only |
| Q6 split/leakage/overfit | 8 | `C08 / E08` | 8-case corpus 无统计代表性 | ML guidance mapping remains PARTIAL |
| Q7 verdict state | 7, 9, 10 | `C09 / E09` | four states observed | IMPROVEMENT unexecuted; C09 PARTIAL |
| Q8 Lab06 known regression | Opening, 9 | `C07/C10 / E07/E10` | core concrete mechanism | fixed synthetic fixture only |
| Q9 release gate/proof ceiling | 10 | `C06/C09/C11 / E06/E09/E11` | bounded gate input | not production/generalization/significance |
| Q10 Part IV/future boundary | 12 | `C12 / E12` | Lab06 completed in Article22 | no Article23/24 assets; BuildPilot design-only |

## Claim-to-section and evidence coverage（12 / 12）

| Claim | Status ceiling | Primary sections | Evidence Card | Lab anchor | Mandatory wording / boundary |
|---|---|---|---|---|---|
| `22-C01` | `PROPOSAL` | Opening, 1, 11 | `22-E01` | Lab contradiction only | five-way ownership is course model, not standard |
| `22-C02` | `PARTIAL` | 2, 4 | `22-E02` | manifest fields as auxiliary | official sources support concerns, not full schema |
| `22-C03` | `PROPOSAL` | 3 | `22-E03` | `ACCEPTED_FOR_FIXTURE` example | candidate/provenance != Golden acceptance |
| `22-C04` | `PROPOSAL` | 4, 9 | `22-E04` | corpus/candidate/result identities | working fixture does not universalize schema |
| `22-C05` | `PARTIAL` | 5 | `22-E05` | exact/rule only | taxonomy wider than source; no unbiased judge claim |
| `22-C06` | `PARTIAL` | 2, 6, 9, 10 | `22-E06` | aggregate/critical/comparability outputs | NIST does not prescribe gate formula |
| `22-C07` | `CONFIRMED / FIXTURE-SCOPED` | Opening, 6, 9 | `22-E07` | known-regression result | only frozen 8-case hard-gate mechanism |
| `22-C08` | `PARTIAL` | 8 | `22-E08` | limitations only | Agent mapping bounded; no fixed split ratio |
| `22-C09` | `PARTIAL` | 7, 9, 10 | `22-E09` | FI-01/FI-02/FI-03 | four paths observed; IMPROVEMENT not run |
| `22-C10` | `CONFIRMED / FIXTURE-SCOPED` | Opening, 9 | `22-E10` | baseline/regression A/B + verifier | no Agent/model/production claim |
| `22-C11` | `PARTIAL` | Opening, 9, 10 | `22-E11` | environment/limitations/repeatability | PASS not quality/generalization/statistics |
| `22-C12` | `CONFIRMED` | 12 | `22-E12` | Lab06 transaction boundary | future assets remain zero; no BuildPilot runtime |

Coverage=`12 / 12`；Evidence Cards=`12 / 12`；Status mix=`3 CONFIRMED / 6 PARTIAL / 3 PROPOSAL / 0 BLOCKED`；new core Claim/Card=`NONE`。

## Figures, tables and examples plan

| ID | Form | Teaching responsibility | Evidence source | Mandatory label / restraint |
|---|---|---|---|---|
| `T22-01` | five-activity ownership table | 防止 Demo/Test/Benchmark/Eval/Regression 相互冒充 | `E01` | COURSE PROPOSAL / not exhaustive |
| `F22-01` | Eval Contract flow | 从 objective 到 verdict 建立抽象主链 | `E02/E06/E09` | concern-supported, full schema is course design |
| `F22-02` | candidate-to-Golden lifecycle | 分离 provenance、curation、acceptance、split | `E03` | no automatic Golden promotion |
| `EX22-01` | Eval Case YAML sketch | 展示 identity/lineage/oracle/scorer/revision/applicability | `E04` + Lab contract | synthetic course shape |
| `T22-02` | scorer family/error table | 让每类判据带错误边界 | `E05` | semantic/human not executed in Lab |
| `T22-03` | measurement/decision ledger | 拆 metric、aggregation、threshold、baseline、uncertainty、gate | `E06/E07` | Lab policy not universal threshold |
| `F22-03` | comparability-first verdict tree | 把 unknown/incomparable 与普通 delta 分开 | `E09` | IMPROVEMENT defined, not observed |
| `F22-04` | split/exposure ledger | 展示 dedup、leakage、wear-out 与 refresh | `E08` | PARTIAL Agent mapping |
| `T22-04` | Lab06 four-run contrast | 用少量真实结果落地抽象模型 | `E07/E09/E10/E11` + raw JSON | fixture-scoped; no raw-log narrative |
| `F22-05` | Eval-to-release decision chain | 说明 Eval 是 release input，不是 production truth | `E06/E11` | no production/generalization claim |
| `T22-05` | anti-pattern table | 汇总被吞掉的责任与最小修正 | `E01-E11` | review heuristic, not industry taxonomy |

Asset policy: Outline/Draft 优先使用 Markdown 表和 ASCII 图；本 Gate 不创建 `assets/`。若后续需要发布图片，图中所有 runtime 数值必须可追到 Lab06 raw result，所有 Proposal/Partial/fixture labels 必须可见。

## Learning Check（题目 + answer expectations）

1. Demo、Test、Benchmark、Eval、Regression 为什么不能互相替代？
   - Expected: primary question、artifact 与 decision ownership 不同；共用基础设施不等于共用结论。
2. 为什么 dataset + score 仍不是完整 Eval Contract？
   - Expected: 还缺 objective、case/oracle/scorer、threshold/baseline、manifest/comparability、verdict policy、uncertainty/limitations。
3. 为什么真实 Trace 不能自动变成 Golden sample？
   - Expected: 仍需 scope/permission/redaction、dedup、label/oracle review、acceptance、revision 与 split lineage。
4. Exact/rule scorer 的 repeatability 为什么不等于 truth？
   - Expected: canonicalization/coverage/oracle 可能错，合法变体或未覆盖质量不可见。
5. known-regression aggregate=`0.875` 为什么 overall=`FAIL`？
   - Expected: aggregate threshold虽过线，critical accuracy=`0.5` 未满足 hard gate；overall是多条件组合。
6. scorer version mismatch 为什么不能直接写 regression？
   - Expected: measurement manifest不一致，普通 delta无资格；Lab06保存 `INCOMPARABLE / fail closed`。
7. missing N06 为什么是 UNKNOWN，不只是一个失败 case？
   - Expected: 缺 observation 是 qualification gap；把它折成普通0分会伪造可比的测量。
8. Golden corpus 为什么会 wear out？
   - Expected: repeated exposure、dedup failure、用最终集合指导修改会降低对未见数据的信心；需split/exposure/version/refresh。
9. Lab06 实际覆盖了哪四种 change state，哪一种未执行？
   - Expected: `REGRESSION / UNCHANGED / UNKNOWN / INCOMPARABLE` observed；`IMPROVEMENT` not run，因此C09保持PARTIAL。
10. Lab06 PASS 最多证明什么？
    - Expected: 在 frozen 8-case synthetic exact/rule fixture 和单一 Windows/.NET环境中，evaluator可重复让baseline PASS并捕获预置critical regression；不证明生产质量/泛化/统计显著。
11. release gate 为什么要把 threshold policy 单独版本化？
    - Expected: 防止为当前candidate临时移动决策边界；measurement与policy应可独立审查。
12. Article 22 完成时，Article 23/24 与 BuildPilot 的合法状态是什么？
    - Expected: Article23 Optional SKIP/PLANNED/zero assets，Article24 forbidden/zero assets，BuildPilot fixture外 design/not implemented/not run。

## Practical reader actions

| Action | Minimum artifact | Review question | Evidence ceiling |
|---|---|---|---|
| classify activity ownership | one-page Demo/Test/Benchmark/Eval/Regression ledger | 这次输出究竟允许做什么决定？ | course model |
| curate samples | lineage + review + acceptance + split record | 来源、权限、标签与暴露是否可追？ | proposal-informed |
| freeze contract | objective/corpus/scorer/metric/gate/baseline/manifest refs | 同一份结果以后能否解释？ | partial/course schema |
| preserve per-case result | normalized cases + group metrics + raw refs | 关键失败是否被平均值隐藏？ | Lab mechanism confirmed in fixture |
| check comparability | manifest diff + qualification result | 是否有资格计算 ordinary delta？ | Lab four paths observed |
| manage leakage | dedup/split/exposure/revision ledger | 是否用最终集合反复指导修改？ | partial mapping |
| gate release | versioned policy + limitations + owner decision | PASS覆盖什么、不覆盖什么？ | scope-bound only |
| maintain evaluation | refresh/review/change history | corpus/oracle/scorer是否漂移或磨损？ | engineering proposal |

## Job Competency mapping

| Competency | Reader-visible output | Observable standard | Explicit ceiling |
|---|---|---|---|
| Evaluation architecture | five-activity ledger + Eval Contract flow | 能把objective、data、measurement、decision与limitations分账 | taxonomy/schema不称行业标准 |
| Dataset governance | candidate-to-Golden lifecycle + lineage/exposure ledger | 能保留来源、permission/redaction、review、acceptance、split/revision | 不证明代表性或label truth |
| Contract/schema design | Case/manifest/version model | 能识别同ID不同revision时的comparability风险 | course proposal，非生产充分schema |
| Measurement judgment | scorer family/error table + metric/gate ledger | 能说明scorer错误边界，不让总分吞critical failure | semantic/human未在Lab执行 |
| Regression engineering | comparability-first verdict tree | 能保存regression/unchanged/unknown/incomparable并拒绝无资格delta | improvement未执行；state set非标准 |
| Experiment discipline | Lab Design/Observation/Interpretation chain | 能从raw result追到fixture-scoped Claim，不改判据迎合结论 | 8-case deterministic fixture only |
| Release governance | versioned gate + proof ceiling | 能把Eval作为release input并保留override/review/monitoring责任 | no production/generalization guarantee |
| Reliability reasoning | critical gate + failure-path retention | 能解释aggregate pass/overall fail和fail-closed语义 | threshold/policy不外推 |
| Technical communication | figures/tables with status labels | 能让CONFIRMED/PARTIAL/PROPOSAL与observed/not-run一眼可见 | no hidden future preview |
| Course boundary discipline | Part IV close + future-asset guard | 能结束本篇而不启动可选/禁止future transaction | repository-local fact only |

## Frontmatter and publication plan

```yaml
---
title: "Eval、Golden Dataset 与 Regression：修复以后还会不会再坏"
slug: "agent-engineering-22-eval-golden-dataset-regression"
date: "2026-08-28T00:00:00+08:00"
description: "把 Demo、Test、Benchmark、Eval 与 Regression 分账，用版本化 Golden Dataset、可比基线和回归门禁判断修复是否再次退化。"
draft: false
tags:
  - "Agent Engineering"
  - "AI Engineering"
  - "Evaluation Engineering"
  - "Reliability Engineering"
series: "Agent Engineering"
primary_series: "agent-engineering"
series_role: "article"
series_order: 230
weight: 3230
---
```

- Published Path: `content/ai-empowerment/agent-engineering-22-eval-golden-dataset-regression.md`
- Previous link: Published Article 21 exact path。
- Course index link: existing Agent Engineering series index。
- Next link: `NONE in this Article22 Draft/Publish plan`；Article23 is skipped/zero-asset and Article24 is forbidden, so Author must not fabricate a `relref` or preview route。
- Metadata rationale: follow published Article20=`series_order 210 / weight 3210` and Article21=`220 / 3220` sequence；Publisher must validate repository consistency without changing frozen knowledge content。
- YAML quote rule: current strings contain no ASCII quote characters；double-quoted scalars are valid。Any later title/description edit adding quotation marks must switch outer YAML quoting safely。

## Exact no-new-fact boundary for Draft

Draft may:

- paraphrase and reorganize only `22-C01`—`22-C12` and `22-E01`—`22-E12`；
- quote Lab06 normalized values already present in the four retained `result.json` files and README Observation/Evidence Merge；
- use Published Article21 only for the candidate-slice/lineage handoff and its declared non-ownership；
- present course models as `COURSE PROPOSAL` and source-supported concerns as `PARTIAL`；
- mark `22-C07 / C10` as `CONFIRMED` only with full fixture/version/environment qualification；
- define the five-state verdict model while stating `IMPROVEMENT = NOT OBSERVED IN LAB06` and retaining `22-C09 PARTIAL`；
- compress Lab execution into design/mechanism/observation/limitation, with raw paths for auditability。

Draft must not:

- introduce a new core Claim, Evidence Card, source behavior, statistic, threshold, schema field or Lab observation；
- call the five-activity taxonomy、Golden lifecycle、Case schema、verdict states or release record an industry standard；
- upgrade any `PARTIAL / PROPOSAL` wording, or remove the fixture qualifier from `C07 / C10`；
- write `IMPROVEMENT` as executed, infer it from baseline PASS, or imply all five verdict states have runtime coverage；
- describe Lab06 candidates as Agent/model outputs, real Trace samples, production traffic or statistically representative data；
- infer production quality、cross-model/provider/environment generalization、security/compliance、business benefit or statistical significance；
- turn TDD/build/hash success into proof that the oracle、threshold or production release policy is correct；
- add new OpenAI/NIST/Google/Datasheets product/version facts beyond Evidence Cards without returning to Research；
- claim BuildPilot Runtime exists or ran；fixture-local code is not BuildPilot implementation；
- create, read into narrative, preview or link future Article23/24 content/assets；
- create `draft.md` until Master validates this OUTLINE Gate and dispatches AUTHOR_DRAFT。

Trigger: if Draft requires any fact outside this boundary, return `RETURN_TO_RESEARCH` with the exact missing Claim/Evidence need; do not fill it with memory, inference or “common practice”。

## Explicit non-scope

- 不写成任何 Provider 的 Evals API、grader API、CI 产品或 dashboard 教程。
- 不实现生产 dataset registry、annotation platform、judge calibration service、statistical test、release orchestrator 或 BuildPilot Runtime。
- 不宣称 Golden Dataset 无泄漏、永久有效、完整代表生产分布或标签绝对正确。
- 不把 exact/rule/semantic/human scorer 任一种称为无偏真值或通用最佳方案。
- 不规定所有团队都必须采用 `0.80 / 1.00`、critical hard gate、五种 verdict 或本文字段集。
- 不执行新的 Lab、Provider/model call、生产流量、外部网络、真实 Trace curation 或 fault injection。
- 不修改 frozen Lab Design/Hypothesis/Acceptance、fixtures、raw observations 或 Evidence interpretation。
- 不创建 Draft、Review、Published Content、assets、global/canonical/Git/future-Article artifact。
- 不启动 Article23 或 Article24；不提前展开其教学内容。
- BuildPilot remains `DESIGN / NOT IMPLEMENTED / NOT RUN` outside Lab06。

## OUTLINE Gate checklist

- [x] Article Type fixed as `PRINCIPLE / LAB_ARTICLE`；结构从 Problem Space -> Abstract Model -> Concrete Implementation/Experiment -> Engineering Decisions/Boundary，不以 API 开篇。
- [x] Teaching Spine 从 Article21 candidate handoff 和“单次修复不等于改善”开始，最后回到 release gate 与 proof ceiling。
- [x] Core Questions coverage=`10 / 10`；Claims/Evidence Cards coverage=`12 / 12`；new core Claim/Card=`NONE`。
- [x] Evidence posture preserved exactly: `3 CONFIRMED / 6 PARTIAL / 3 PROPOSAL / 0 BLOCKED`。
- [x] `22-C07 / C10` 仅 fixture-scoped CONFIRMED；`22-C09` 保持 PARTIAL，`IMPROVEMENT` 明确未执行。
- [x] Demo/Test/Benchmark/Eval/Regression、candidate-to-Golden、Eval Case/Contract、scorer taxonomy/errors、metric/threshold/baseline/aggregation/uncertainty、split/leakage、verdict states、release gate/proof ceiling 均有独立教学职责。
- [x] Lab06 集成实际 observed values 与 raw paths，但正文规划不退化成命令/日志流水账。
- [x] 每个主要 section 均有 Reader Question、Claim/Evidence、Boundary、Transition purpose、Learning Check 与 Practical Action。
- [x] Figures/Tables、Learning Checks、Practical Actions、Job Competency、Frontmatter plan、Claim/Evidence/Lab mapping 完整。
- [x] Article23=`SKIP / PLANNED / ZERO ASSETS`、Article24=`FORBIDDEN / ZERO ASSETS`；无 future content preview/link。
- [x] BuildPilot=`DESIGN / NOT IMPLEMENTED / NOT RUN`；Lab fixture 不被写成 BuildPilot evidence。
- [x] Draft no-new-fact boundary 明确；任何新增核心事实触发 `RETURN_TO_RESEARCH`。
- [x] 本 Gate 不创建 Draft/Review/content/assets/Lab/global/canonical/Git/future-Article artifact。
- [x] OUTLINE Gate recommendation: `PASS`；next allowed gate candidate: `AUTHOR_DRAFT`；Master validation remains outside this artifact。
