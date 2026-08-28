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

> **上一篇**：[Trace、Replay 与 Failure Taxonomy：错误究竟发生在哪一层]({{< relref "ai-empowerment/agent-engineering-21-trace-replay-failure-taxonomy.md" >}})

> **课程索引**：[Agent Engineering 系统课程]({{< relref "ai-empowerment/agent-engineering-series-index.md" >}})

# Eval、Golden Dataset 与 Regression：修复以后还会不会再坏

> 如果这篇只记一句话：`修复是否可靠，不取决于这次 Demo 看起来是否成功，而取决于一份版本化、可比较的评估合同，能否保留关键退化、未知与不可比。`

Article 21 把 Trace 的交付边界停在了 candidate：它可以交出 normalized slice、lineage、版本、脱敏、effect 与 unknown refs，却不能替 Eval 决定样本是否进入 Golden Dataset，也不能决定 oracle、metric、threshold、baseline 或 Regression verdict。

这条边界很重要。修复一个失败样例，再把它重新跑绿，只回答了“这一次，这条路径看起来通过”。它没有回答旧能力是否退化、这次和上次是否使用同一判据、缺失结果是否被算成零分，也没有回答总分背后是否藏着一个关键失败。

Lab 06 恰好观察到这种矛盾：known-regression candidate 通过 `7/8`，aggregate accuracy=`0.875`，单看 aggregate threshold 是 `PASS`；但 critical accuracy 只有 `1/2 = 0.5`，所以 overall gate=`FAIL`。这不是“总分无用”，而是说明 measurement 和 release decision 不能被压成同一个数字。

先把证据上限说清楚：本文共有 `12 / 12` Claims 与 `12 / 12` Evidence Cards，状态保持为 `3 CONFIRMED / 6 PARTIAL / 3 PROPOSAL / 0 BLOCKED`。其中 `22-C07`、`22-C10` 的 `CONFIRMED` 只覆盖 `lab06-fixture-v1 / corpus r1 / scorer v1 / Windows + .NET 10.0.301`；`22-C09` 仍是 `PARTIAL`，因为 Lab 只观察了 `REGRESSION / UNCHANGED / UNKNOWN / INCOMPARABLE`，`IMPROVEMENT` 被定义但没有执行。

## 1. 先把五种活动的决定权分开

Demo、Test、Benchmark、Eval、Regression 可以共用数据、运行器甚至 CI，但它们首先回答的是不同问题。

| Activity | 首要问题 | 最小输出 | 不能冒充 |
|---|---|---|---|
| Demo | 这一次能否展示目标路径？ | scenario + one observed run | repeatability、coverage、Regression |
| Test | 一个局部确定性合同是否满足？ | assertion + pass/fail + raw failure | system-level quality、representativeness |
| Benchmark | 固定协议下怎样测量或比较？ | protocol + dataset + metric + score | task-specific release decision |
| Eval | 对声明目标，行为是否满足已声明判据？ | objective + dataset + scorer + metrics + limitations | 永久真值、未覆盖风险 |
| Regression | 相对可比 baseline，哪些行为改变？ | manifests + per-case delta + gates + verdict | 只凭最新总分宣布修复有效 |

这张表是课程的 ownership model，不是行业统一、穷尽或严格互斥的 taxonomy。NIST AI RMF 把 software testing、performance assessment、benchmark、uncertainty 与持续测量放在不同关注面；OpenAI 的评估指导也区分行业 benchmark、数值 score 与应用自定义 eval，并把只凭感觉判断列为反模式。它们支持“需要分账”这个方向，不规定本文这五个名字必须怎样互斥。

真正需要避免的是结论借权：Demo 跑通不能借 Regression 的决定权；单元测试通过不能借生产质量的决定权；一个 benchmark score 也不能自动变成发布许可。

**活动可以共用基础设施，但不能互相借用决定权。**

## 2. 抽象模型：先冻结 Eval Contract，再解释分数

一次可审计 Eval，不应该从“跑哪个工具”开始，而应该从“这次决定依赖哪些固定对象”开始。

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

这条链至少要让 Reviewer 找到：

- 目标风险与预期决定；
- dataset identity、revision、split 与 lineage；
- case identity、input、oracle/scorer、criticality 与 applicability；
- metric、aggregation 与 uncertainty disclosure；
- threshold 和多条件 gate policy；
- baseline identity；
- 适用时的 model、provider、runtime、tool、policy、prompt、harness 等 system manifest；
- comparability policy、change verdict 与 limitations。

官方来源直接支持 objective、dataset、metrics/methods、benchmark、uncertainty 与持续评估等关注面；上面这套完整字段组合是课程设计，因此 `22-C02` 保持 `PARTIAL`。字段齐全也不会自动让数据有代表性、让 oracle 正确，或让评估结果获得生产资格。

**分数是合同运行后的结果，不是合同本身。**

## 3. Trace candidate 不能自动晋升为 Golden sample

真实 Trace 往往比凭空编造的 case 更贴近项目问题，但“来自真实运行”只回答来源，不回答标签是否正确、是否有权使用、是否重复，也不回答应该进入哪个 split。

本文采用下面这条课程 Proposal：

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

这里有五个容易混掉的责任：

- provenance / lineage 说明样本从哪里来；
- label / oracle review 说明什么算对；
- acceptance 说明谁按哪版 policy 接纳；
- split / exposure 说明它在哪里被使用过；
- dataset revision 说明它属于哪个固定集合。

Lab06 的 synthetic sample 可以记录为 `ACCEPTED_FOR_FIXTURE`，但这个状态只表示它被接纳进本地课程 corpus，不表示生产数据授权、统计代表性或跨系统 truth。Datasheets for Datasets 与 OpenAI 的 golden example 指导提供了 documentation 和 expert curation 的先例，却没有把本文 lifecycle 变成行业标准。

**Trace 能提供候选与 provenance，不能自封 Golden truth。**

## 4. Eval Case：稳定 ID 之外，还要保存比较资格

一个最小的课程级 Case 可以写成：

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

这份 shape 的重点不是字段越多越好，而是不要把不同责任藏进一个名字里：Case ID 与 input 内容分开，lineage 与 oracle 分开，criticality 是 policy classification，不是运行时观测；corpus、scorer 与 candidate schema revision 则参与 comparability 判断。

Lab06 实际使用了这些关注面，说明它们足以驱动这个窄 fixture 的固定比较与 fault injection；这仍不能证明它们是所有 Eval 的唯一或充分 schema。因此 `22-C04` 保持 `PROPOSAL`。

**稳定 ID 让样本可追踪，版本与 applicability 才让比较有资格。**

## 5. Oracle 与 Scorer：可重复不等于无误差

Oracle 回答“什么算对”，Scorer 回答“怎样判断 observation 是否符合 oracle”。把二者混成一个 grader 名字，会隐藏判据和实现各自可能怎样错。

| 类型 | 适合 | 主要错误边界 | 最低披露 |
|---|---|---|---|
| Exact | canonical token、enum、digest | 合法变体被错杀 | canonicalization / version |
| Rule-based | schema、字段关系、invariant | 规则未覆盖的质量不可见 | rule IDs / version / coverage |
| Structured execution | compile、test、query、result | environment 与 hidden dependency | command / env / exit / raw output |
| Semantic / model judge | 多个可接受表达、复杂 rubric | position、verbosity、rubric、model drift | judge / rubric / order / calibration |
| Human-judged | 领域价值、歧义、高风险判断 | disagreement、fatigue、policy drift | rubric / reviewer / agreement / history |

OpenAI 的 hosted docs 当前列出 string check、text similarity 与 model grader，并披露 model judge 的 position、verbosity 等风险，建议用 human feedback 做校准。这些材料支持“scorer 类型和误差边界需要显式记录”，但不证明本文 taxonomy 穷尽，也不证明任何 judge 是无偏真值。

Lab06 只执行 deterministic exact/rule scorer。semantic 与 human 路径没有在这个 Lab 中运行，所以 `22-C05` 保持 `PARTIAL`。

**Scorer 的稳定性只说明判法可重复，不说明 oracle 完整或结论无偏。**

## 6. Metric 负责测量，Gate 负责决定

下面这些词经常一起出现在一份报告里，却不应该共享一个责任：

| 对象 | 责任 |
|---|---|
| metric | observation 怎样被测量 |
| aggregation | case 或 group 指标怎样汇总 |
| threshold | 单项测量的决策边界 |
| baseline | 本次变化与谁比较 |
| uncertainty | 小样本、随机性、测量误差与未覆盖范围 |
| release gate | 多项必要条件怎样布尔组合 |
| regression verdict | 怎样解释变化与可比性 |

Lab06 冻结的 policy 是：

```text
aggregate_accuracy >= 0.80
AND critical_accuracy == 1.00
AND missing_or_unknown == 0
AND manifest_comparable == true
```

这是 `LAB06 FROZEN POLICY`，不是通用发布标准。NIST 没有规定这组阈值；Lab 也只确认这组 hard gate 在冻结 fixture 中按照设计生效。

这正是 `0.875` 仍然 overall `FAIL` 的原因：aggregate threshold 已通过，但 critical gate 没通过。总分没有被否定，它只是没有获得吞掉关键条件的权限。

**Metric 负责测量，gate 负责决策；把两者混成一个分数，就失去了失败原因。**

## 7. Comparability-first：先判断能不能比，再判断变好还是变坏

Regression 不是“新分数减旧分数”。在计算 ordinary delta 之前，必须先确认 dataset、scorer、schema 和 system manifest 仍满足比较合同。

```text
compare dataset / scorer / schema / system manifests
  ├─ mismatch --------------------> INCOMPARABLE / fail closed
  └─ comparable
       ├─ observation missing ----> UNKNOWN / fail closed
       └─ compare baseline/candidate pass state
            ├─ fail -> pass ------> IMPROVEMENT
            ├─ pass -> fail ------> REGRESSION
            └─ same state --------> UNCHANGED
```

这五种状态是课程 Proposal，不是行业统一 taxonomy。Lab06 真实观察到：

- C01=`REGRESSION`；
- 其余 7 个 case=`UNCHANGED`；
- missing N06=`UNKNOWN`；
- scorer v2 mismatch=`INCOMPARABLE`。

`IMPROVEMENT` 只在合同中定义，Lab06 没有执行这条路径。因此 `22-C09` 必须保持 `PARTIAL`，不能因为状态机写得完整，就假装五条分支都有 runtime coverage。

`UNKNOWN` 代表 observation 或 qualification 缺口；`INCOMPARABLE` 代表合同不允许普通 delta。把它们强折成 0 分，会制造一份看似完整、实际失去解释资格的测量。

**不能比较时，最诚实的结果不是一个更保守的分数，而是明确保存 `INCOMPARABLE`。**

## 8. Golden corpus 也会泄漏、过拟合和磨损

固定集合能让比较稳定，却不会天然让集合永远有效。反复用最终集合指导修改，会让系统越来越熟悉这组题；duplicate example 会污染 split；现实分布、oracle 与 policy 也可能变化。

Google ML Crash Course 对 training、validation、test 分离、test-set wear-out、重复样本和代表性给出了直接教学说明。本文只把这条窄原则映射到 Agent application eval，因此 `22-C08` 保持 `PARTIAL`，不规定固定 split 比例。

工程上至少应分开记录：

| Scope | 主要用途 | 最低治理记录 |
|---|---|---|
| development | 高频修正与调试 | exposure、revision、dedup |
| regression | 每次变更守护已知合同 | acceptance、baseline、per-case history |
| holdout / canary | 较少暴露的独立检查 | access、exposure、refresh decision |

样本从 Trace 进入任一集合时，都应留下 lineage 和 exposure。看到 regression answer 后修系统是正常闭环；为了让当前 candidate 过线而悄悄改 oracle、threshold 或 split，则是在改考试，不是在证明系统改善。

**Golden 的价值来自受控版本和维护记录，不来自“Golden”这个名字。**

## 9. Lab06：用固定合同捕获一次已知关键退化

Lab06 不把文章变成命令流水账。它只回答一个窄问题：在同一 accepted fixture corpus、冻结 scorer 与可比 manifest 下，一个 deterministic evaluator 能否让 baseline 通过，同时抓住一个已知 critical regression，并拒绝让 aggregate score 掩盖它？

### Design

- `8` 个 synthetic cases：`2` 个 CRITICAL，`6` 个 NORMAL；
- corpus=`lab06-golden-corpus / r1`；
- scorer=`lab06-deterministic-exact-scorer / v1`；
- baseline 与 known-regression 都是固定 candidate input，不是 Agent/model output；
- known-regression 只破坏 C01；
- exact/rule scorer 对 decision、failure layer、reason-code set 逐 case 判定；
- aggregate、critical、missing/unknown 与 comparability 各自保留。

### Observation

| Run | Comparable | Aggregate | Critical | Verdict / Overall | 观察重点 |
|---|---:|---:|---:|---|---|
| baseline | true | `8/8 = 1.0` | `2/2 = 1.0` | `PASS / PASS` | frozen baseline qualifies |
| known regression | true | `7/8 = 0.875`，threshold PASS | `1/2 = 0.5`，gate FAIL | `REGRESSION / FAIL` | C01 退化，其余 7 个 UNCHANGED |
| missing N06 | false | `0.75`，仅为 retained output | `0.5` | `UNKNOWN / FAIL` | 缺 observation 后 fail closed |
| scorer v2 mismatch | false | ordinary aggregate absent | absent | `INCOMPARABLE / FAIL` | measurement manifest 漂移，拒绝普通 delta |

执行证据还保留了 locked restore/build exit `0`、有效 RED=`0/5`、不改断言后的 GREEN=`5/5`、独立 formal verifier=`2/2`。Run A/B 的 baseline 与 known-regression normalized artifacts 分别 byte-identical，SHA-256 也一致。

四份正文所需的 raw anchor 是：

- `docs/agent-engineering-course/labs/lab-06-trace-eval/observations/run-a/baseline/result.json`
- `docs/agent-engineering-course/labs/lab-06-trace-eval/observations/run-a/known-regression/result.json`
- `docs/agent-engineering-course/labs/lab-06-trace-eval/observations/fault-injection/missing-n06/result.json`
- `docs/agent-engineering-course/labs/lab-06-trace-eval/observations/fault-injection/scorer-v2/result.json`

原始记录没有隐藏工具层噪声：外层 shell 对非零进程曾显示 generic exit `1`，显式 `$LASTEXITCODE` 才确认 Runtime native exits 为 `2 / 3`；第一次 ad-hoc PowerShell byte helper 还错误地把 `SequenceEqual` 当实例方法调用。两项都没有改变 result artifact，也没有被用来重写判据，而是作为 tooling limitation 保留。

### Interpretation

Lab 真实确认的是两条 fixture-scoped Claim：在 `lab06-fixture-v1 / corpus r1 / scorer v1 / Windows + .NET 10.0.301` 中，hard critical gate 确实阻止了 aggregate threshold 掩盖 C01 退化；同一冻结 evaluator 也可重复让 baseline PASS 并捕获这次预置 regression。因此 `22-C07`、`22-C10` 是这个范围内的 `CONFIRMED`。

它没有确认真实 Trace 标签正确，没有运行 Agent/model，没有生产流量或统计采样，也没有校准生产风险。`IMPROVEMENT` 没有执行。Build、TDD、hash 与 verifier 成功，只证明本 fixture 的实现和 retained artifact 满足冻结合同，不证明 oracle、阈值或生产发布 policy 天然正确。

**Lab 的价值不是多一组绿色命令，而是把固定合同、失败路径与 Claim 上限连成可追溯证据链。**

## 10. Eval 怎样进入发布门禁，又不冒充生产质量

Eval 可以成为 release input，但不能独自拥有全部发布决定。一个有边界的 release record 至少应能关联：

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

对应的工程规则是：

1. manifest mismatch 先 fail closed，不计算普通 improvement/regression；
2. critical、security、policy 或其他声明 hard gate 与 aggregate 分账；
3. unknown / incomparable 保持一等状态，不强折成 0/1；
4. threshold 与 release policy 独立版本化，变更要单独 review，不为当前 candidate 临时降线；
5. Eval PASS 只对 contract scope 有效，生产监控、canary、human review 与持续 eval 仍有自己的责任。

Lab06 能证明的上限是：它的 frozen mechanism 可重复抓住预先注入的 critical regression。它不能证明真实 Agent/model 已改善、生产风险受控、跨 Provider/model/环境泛化、统计显著性，也不能证明 BuildPilot 有任何运行行为。

因此 `22-C11` 保持 `PARTIAL`。固定集合全绿，可以支持当前合同内的决定；它不能把未测风险变成已经不存在。

**Eval 可以给发布一个可审计输入，不能替发布、生产监控或风险 owner 作全部决定。**

## 11. 一套 Eval / Regression 设计通常怎样写坏

| 捷径 | 被吞掉的责任 | 最小修正 |
|---|---|---|
| `demo passed = eval passed` | fixed task/data/criteria | 声明活动 owner 和缺失合同 |
| `trace exists = golden sample` | curation/oracle/acceptance | 保持 candidate + lineage，直到 review |
| `one score = release verdict` | groups/critical gates/uncertainty | 拆开 metric、aggregation 与 gate |
| `exact scorer = objective truth` | canonicalization/coverage | 披露等价类和 false-negative risk |
| `LLM judge = semantic oracle` | bias/calibration/rubric drift | 版本化 rubric/judge，并用 human calibration |
| `higher score = improvement` | baseline/manifest comparability | 先比较 manifests |
| `missing = failed case` | observation qualification | 保存 UNKNOWN 并 fail closed |
| `scorer changed, still compare` | measurement contract | 保存 INCOMPARABLE，或另建 bridge evidence |
| `golden corpus is permanent` | exposure/leakage/drift | version、dedup、split、refresh |
| `threshold moved, candidate passed` | decision-policy integrity | 独立版本化并审查 threshold |
| `fixture pass = production quality` | representativeness/generalization | 发布 proof ceiling 与 monitoring gap |
| `Lab06 = BuildPilot runtime` | implementation/observation ownership | 保持 DESIGN / NOT IMPLEMENTED / NOT RUN |

这张表是工程 review heuristic，不是行业标准 taxonomy。它的共同原则只有一个：每种 artifact 只拥有自己的决定权。

## 12. 下一次修复，至少留下这组可审计产物

1. 写清当前活动主要属于 Demo、Test、Benchmark、Eval 还是 Regression；
2. 冻结 objective、dataset/case revisions、oracle/scorer、metrics/gates、baseline 与 system manifest；
3. 把 Trace sample 保持 candidate，直到 lineage、review、acceptance、split 完整；
4. 保存 per-case observation、group metrics、unknown/incomparable 与 raw failure；
5. 先判 comparability，再给 change verdict；
6. 记录 split exposure、dedup 与 refresh；
7. 把 Eval 作为 release input，同时披露 proof ceiling；
8. 对 policy 或 threshold 变更做独立版本和 review。

本篇也是 Part IV 的必修收束：Required Lab06 已在当前 transaction 中真实执行并完成 Evidence Merge。课程边界保持不变——Article 23 是 `Advanced / Optional / SKIP / PLANNED / ZERO ASSETS`，Article 24 是 `FORBIDDEN / ZERO ASSETS`；本文不启动、不预写也不链接它们的内容。Lab fixture 之外，BuildPilot 继续是 `DESIGN / NOT IMPLEMENTED / NOT RUN`。

## 本篇能建立什么，不能证明什么

本篇可以安全建立：

- Demo、Test、Benchmark、Eval、Regression 应按决定权分账；完整五分法是课程 Proposal；
- 可审计 Eval 需要 objective、dataset、scorer、metric 等显式关注面；完整合同字段保持 `PARTIAL / COURSE DESIGN`；
- Trace candidate 到 Golden sample 之间需要 lineage、review、acceptance 与 split；具体 lifecycle 是 Proposal；
- Case、scorer、metric、gate、baseline、manifest、verdict 应分开保存各自责任；
- split 污染、重复样本与反复暴露会限制对未见数据的解释，映射到 Agent eval 时保持 `PARTIAL`；
- 在 `lab06-fixture-v1 / corpus r1 / scorer v1 / Windows + .NET 10.0.301` 内，critical gate 可重复抓住预置 C01 regression；
- `REGRESSION / UNCHANGED / UNKNOWN / INCOMPARABLE` 已在 Lab06 观察，`IMPROVEMENT` 未执行；
- Eval 是发布输入，不是生产质量、泛化或统计显著性的替代证明。

本篇不能证明：

- 本文 taxonomy、Golden lifecycle、Case schema、verdict states 或 release record 是行业标准；
- Golden corpus 无泄漏、永久有效、完整代表生产分布或标签绝对正确；
- semantic/model judge 或 human judge 已被校准，或任何 scorer 是无偏真值；
- `0.80 / 1.00`、critical hard gate 或五种 verdict 适合所有团队；
- 真实 Agent/model 已改善，或结果能跨 Provider、model、环境泛化；
- 生产质量、security/compliance、业务收益或统计显著性；
- BuildPilot Runtime 已实现、运行或被 Lab06 验证。

## Claim Traceability（12 / 12）

| Claim | Evidence ceiling | 正文落点 | 保留边界 |
|---|---|---|---|
| `22-C01 / 22-E01` | `PROPOSAL` | 五种活动分账、反模式 | 课程 ownership model，不称标准 |
| `22-C02 / 22-E02` | `PARTIAL` | Eval Contract | 来源支持 concern，不证明完整 schema |
| `22-C03 / 22-E03` | `PROPOSAL` | candidate-to-Golden lifecycle | provenance 不等于 acceptance/truth |
| `22-C04 / 22-E04` | `PROPOSAL` | Case identity、Lab mechanism | fixture 可运行不等于通用 schema |
| `22-C05 / 22-E05` | `PARTIAL` | scorer family/error table | taxonomy 更宽；semantic/human 未执行 |
| `22-C06 / 22-E06` | `PARTIAL` | measurement/gate/release | NIST 不规定本文 gate 公式 |
| `22-C07 / 22-E07` | `CONFIRMED / FIXTURE-SCOPED` | aggregate PASS、critical FAIL | 仅冻结 8-case hard-gate mechanism |
| `22-C08 / 22-E08` | `PARTIAL` | split/leakage/wear-out | Agent 映射有限，不规定 split 比例 |
| `22-C09 / 22-E09` | `PARTIAL` | comparability-first verdict、Lab faults | 四条路径 observed；IMPROVEMENT not run |
| `22-C10 / 22-E10` | `CONFIRMED / FIXTURE-SCOPED` | Lab baseline/regression/repeatability | 无 Agent/model/production Claim |
| `22-C11 / 22-E11` | `PARTIAL` | release proof ceiling | PASS 不等于生产质量/泛化/统计显著 |
| `22-C12 / 22-E12` | `CONFIRMED` | Part IV 与未来资产边界 | Article23/24 零资产；BuildPilot design-only |

Coverage=`12 / 12`；Evidence Cards=`12 / 12`；状态保持 `3 CONFIRMED / 6 PARTIAL / 3 PROPOSAL / 0 BLOCKED`，没有新增 core Claim。

## Learning Check

1. Demo、Test、Benchmark、Eval、Regression 为什么不能互相替代？
2. 为什么 dataset + score 仍不是完整 Eval Contract？
3. 为什么真实 Trace 不能自动变成 Golden sample？
4. Exact/rule scorer 的 repeatability 为什么不等于 truth？
5. known-regression aggregate=`0.875` 为什么 overall=`FAIL`？
6. scorer version mismatch 为什么不能直接写 regression？
7. missing N06 为什么是 `UNKNOWN`，不只是一个失败 case？
8. Golden corpus 为什么会 wear out？
9. Lab06 实际覆盖了哪四种 change state，哪一种未执行？
10. Lab06 PASS 最多证明什么？
11. release gate 为什么要把 threshold policy 单独版本化？
12. Article 22 收束时，Article 23/24 与 BuildPilot 的合法状态是什么？

### 参考思路

1. 五类活动的 primary question、artifact 和 decision ownership 不同；基础设施共享不等于结论共享。
2. 还缺 objective、case/oracle/scorer、threshold/baseline、manifest/comparability、verdict policy、uncertainty 与 limitations。
3. 来源真实不等于有权使用、标签正确或适合某个 split；仍需 scope、redaction、dedup、review、acceptance 与 revision。
4. canonicalization、equivalence class 或 coverage 可能不完整；可重复的判法仍可能稳定地错。
5. aggregate threshold 已过线，但 critical accuracy=`0.5` 没满足 hard gate，所以 overall=`FAIL`。
6. measurement manifest 已改变，ordinary delta 没有资格；应保存 `INCOMPARABLE` 或建立独立 bridge evidence。
7. 缺 observation 是 qualification gap；强折成普通 0 分会伪造完整、可比的测量。
8. repeated exposure、duplicate contamination 与反复用最终集合指导修改，会降低对未见数据的信心。
9. observed=`REGRESSION / UNCHANGED / UNKNOWN / INCOMPARABLE`；`IMPROVEMENT` 未执行，所以 `22-C09` 仍为 `PARTIAL`。
10. 只证明冻结 8-case synthetic exact/rule fixture 在单一 Windows/.NET 10.0.301 环境中，可重复让 baseline PASS 并捕获预置 critical regression。
11. 为了避免针对当前 candidate 临时移动决策边界；measurement 与 decision policy 应能独立审查。
12. Article 23=`Advanced / Optional / SKIP / PLANNED / ZERO ASSETS`；Article 24=`FORBIDDEN / ZERO ASSETS`；BuildPilot 在 fixture 外=`DESIGN / NOT IMPLEMENTED / NOT RUN`。

## Job Competency Mapping

| 能力 | 可观察产物 | 达标表现 | 明确上限 |
|---|---|---|---|
| Evaluation architecture | activity ledger + Eval Contract | 能把 objective、data、measurement、decision 与 limitations 分账 | taxonomy/schema 不称行业标准 |
| Dataset governance | candidate-to-Golden + exposure ledger | 能保留来源、review、acceptance、split 与 revision | 不证明代表性或 label truth |
| Contract design | Case/manifest/version model | 能识别同 ID 不同 revision 的 comparability 风险 | course proposal，非生产充分 schema |
| Measurement judgment | scorer errors + metric/gate ledger | 能说明 scorer 错误边界，不让总分吞 critical failure | semantic/human 未在 Lab 执行 |
| Regression engineering | comparability-first verdict | 能保留 regression/unchanged/unknown/incomparable | improvement 未执行；状态集非标准 |
| Experiment discipline | Design/Observation/Interpretation chain | 能从 raw result 追到 fixture-scoped Claim | 8-case deterministic fixture only |
| Release governance | versioned gate + proof ceiling | 能把 Eval 当 release input 并保存 limitation | 不保证 production/generalization |
| Reliability reasoning | critical gate + failure-path retention | 能解释 aggregate PASS / overall FAIL 与 fail closed | threshold/policy 不外推 |
| Course boundary discipline | Part IV close + future guard | 能结束本篇而不启动未来 transaction | repository-local fact only |

## 参考资料

- [NIST AI RMF 1.0, NIST AI 100-1, January 2023](https://www.nist.gov/publications/artificial-intelligence-risk-management-framework-ai-rmf-10)（TEVV、metrics、benchmark、uncertainty 与 generalizability limitation；页面注明 1.0 正在修订；不定义本文 schema 或 gate）
- [OpenAI Evaluation Best Practices](https://developers.openai.com/api/docs/guides/evaluation-best-practices)（objective、dataset、metrics、continuous eval、golden examples 与 vibe-based anti-pattern；动态 hosted docs）
- [OpenAI Graders](https://platform.openai.com/docs/api-reference/graders)（grader 形态与 model-judge 边界；不证明无偏真值）
- [Google ML Crash Course: Dividing datasets](https://developers.google.com/machine-learning/crash-course/overfitting/dividing-datasets)（train/validation/test、wear-out、duplicates 与 representativeness；映射到 Agent eval 时保持有限）
- [Datasheets for Datasets, arXiv 1803.09010v8](https://arxiv.org/abs/1803.09010)（dataset documentation precedent；不定义 Golden acceptance）
- [上一篇：Trace、Replay 与 Failure Taxonomy]({{< relref "ai-empowerment/agent-engineering-21-trace-replay-failure-taxonomy.md" >}})（只交 candidate slices 与 lineage，不交 Golden/oracle/metric/verdict）

## 最短结论

`修复不会因为这次看起来成功就变得可靠；可靠来自一份能冻结比较条件、暴露关键退化，并诚实保留 unknown 与 incomparable 的评估合同。`

> **上一篇**：[Trace、Replay 与 Failure Taxonomy：错误究竟发生在哪一层]({{< relref "ai-empowerment/agent-engineering-21-trace-replay-failure-taxonomy.md" >}})

> **课程索引**：[Agent Engineering 系统课程]({{< relref "ai-empowerment/agent-engineering-series-index.md" >}})
