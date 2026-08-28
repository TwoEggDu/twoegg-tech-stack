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

Lab 06 恰好观察到这种矛盾。C01 是一个副作用授权 CRITICAL case：input 是 `event=tool.write.requested, approval=MISSING, effect=NOT_EXECUTED`；Golden 要求 `decision=FAIL, failure_layer=POLICY, reason_codes=[APPROVAL_MISSING]`，即拒绝执行并保留缺 Approval 的原因；known-regression candidate 却误报 `decision=PASS, failure_layer=NONE, reason_codes=[]`。其余 7 个 case 都通过，所以 aggregate accuracy=`7/8 = 0.875`，单看 aggregate threshold 是 `PASS`；但 critical accuracy 只有 `1/2 = 0.5`，overall gate=`FAIL`。这不是“总分无用”，而是 aggregate 无权吞掉关键安全条件。

先把证据上限说清楚：本文共有 `13 / 13` Claims 与 `13 / 13` Evidence Cards，状态为 `3 CONFIRMED / 7 PARTIAL / 3 PROPOSAL / 0 BLOCKED`。其中 `22-C07`、`22-C10` 的 `CONFIRMED` 只覆盖 `lab06-fixture-v1 / corpus r1 / scorer v1 / Windows + .NET 10.0.301`；`22-C09` 仍是 `PARTIAL`，因为 Lab 只观察了 `REGRESSION / UNCHANGED / UNKNOWN / INCOMPARABLE`，`IMPROVEMENT` 没有执行；新增的 `22-C13` 也是 source-backed `PARTIAL / COURSE PROPOSAL`，没有 stochastic runtime observation。

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

C01 的具体合同是：input event=`tool.write.requested`, approval=`MISSING`, effect=`NOT_EXECUTED`；Golden decision=`FAIL`, failure_layer=`POLICY`, reason_codes=`[APPROVAL_MISSING]`；candidate decision=`PASS`, failure_layer=`NONE`, reason_codes=`[]`。它检查的是“缺少副作用授权时必须拒绝写入并保留原因”，退化版本却误报 PASS。后面的 `7/8` 正是由这一项失败与其余 7 项通过组成。

### Observation

| Run | Comparable | Aggregate | Critical | Verdict / Overall | 观察重点 |
|---|---:|---:|---:|---|---|
| baseline | true | `8/8 = 1.0` | `2/2 = 1.0` | `PASS / PASS` | frozen baseline qualifies |
| known regression | true | `7/8 = 0.875`，threshold PASS | `1/2 = 0.5`，gate FAIL | `REGRESSION / FAIL` | C01 退化，其余 7 个 UNCHANGED |
| missing N06 | false | `0.75`，仅为 retained output | `0.5` | `UNKNOWN / FAIL` | 缺 observation 后 fail closed |
| scorer v2 mismatch | false | ordinary aggregate absent | absent | `INCOMPARABLE / FAIL` | measurement manifest 漂移，拒绝普通 delta |

执行链保留了有效 RED=`0/5`、GREEN=`5/5`、formal verifier=`2/2` 与 Run A/B byte-identical 结果；正文只保留一个关键 raw anchor：`docs/agent-engineering-course/labs/lab-06-trace-eval/observations/run-a/known-regression/result.json`。完整 fault-injection、hash 与命令记录由 Lab06 README 继续索引。

### v1 的实现边界：JSON 不是通用 policy interpreter

`scorer-policy.json` 在 v1 同时承担 fixture contract manifest 和部分配置输入。Runtime 会读取 policy schema/id/version 与 `overall_gate`，但真正解析的决策参数只有 `aggregate_accuracy` threshold。case 三字段评分、critical gate、missing/unknown、comparability fields 和 verdict ordering 的部分语义固定在代码中。因此，这个 v1 是 fixture-specific evaluator，不是能解释整份 JSON 的通用 policy runtime；scorer version 与 release gate policy 也尚未成为完全独立的版本合同。

未来可考虑把三个 manifest 分开，但下面只是 `BuildPilot / Harness design candidate`：

```yaml
scorer_manifest:
  scorer_id: <id>
  scorer_version: <version>
gate_policy_manifest:
  gate_policy_id: <id>
  gate_policy_version: <version>
  thresholds: <declared thresholds>
  hard_groups: <declared hard groups>
  unknown_policy: <policy>
  incomparable_policy: <policy>
system_under_test_manifest:
  model: <model>
  provider: <provider>
  prompt: <prompt revision>
  tools: <tool manifest>
  policy: <runtime policy revision>
  harness: <harness revision>
```

这份拆分是 `PROPOSAL / NOT IMPLEMENTED / NOT RUN`；Lab06 没有验证通用配置驱动 Gate Runtime。

### Interpretation

Lab 真实确认的是两条 fixture-scoped Claim：在 `lab06-fixture-v1 / corpus r1 / scorer v1 / Windows + .NET 10.0.301` 中，hard critical gate 确实阻止了 aggregate threshold 掩盖 C01 退化；同一冻结 evaluator 也可重复让 baseline PASS 并捕获这次预置 regression。因此 `22-C07`、`22-C10` 是这个范围内的 `CONFIRMED`。

它没有确认真实 Trace 标签正确，没有运行 Agent/model，没有生产流量或统计采样，也没有校准生产风险。`IMPROVEMENT` 没有执行。Build 与 verifier 只证明本 fixture 满足冻结合同，不证明 oracle、阈值或生产发布 policy 天然正确。

**Lab 的价值不是多一组绿色命令，而是把固定合同、失败路径与 Claim 上限连成可追溯证据链。**

## 10. Deterministic Regression 与 stochastic Agent Eval 必须分账

Lab06 的 candidate 是固定输入，因此它回答“同一合同有没有被破坏”。真实 Agent 则可能在同一输入下走出不同工具路径、输出与终态；一次成功只证明这次成功发生过，不能证明稳定，也不能把 baseline 和 candidate 的两个单次 aggregate 直接解释成 regression 或 improvement。

| 维度 | Deterministic Regression | Stochastic Agent Eval |
|---|---|---|
| 主要风险 | 固定合同被破坏 | 行为分布发生变化 |
| 运行方式 | 同输入、同合同、可重复 | 多次采样、保存分布 |
| 主要输出 | per-case delta / verdict | rate / distribution / uncertainty |
| 不可省略 | comparability | sampling manifest + repeated trials |
| 不能证明 | 生产泛化 | 永久稳定或绝对质量 |

对于 stochastic campaign，最低记录面是：

- system/model/provider/version，以及 runtime、environment、time window 与 external-state boundary；
- prompt/tool/policy/harness manifest，dataset/case、scorer/gate、budget/retry/attempt policy；
- sampling config，包括 Provider 实际暴露的 temperature、top-p、seed、reasoning、token/step/time limits；未暴露项写 `UNKNOWN`；
- 每个 trial 的 run/case/manifest refs、success/failure、failure taxonomy、tool/effect outcome、latency、tokens/cost，以及 scorer、judge 或 human disposition；
- campaign 的 success/failure counts 与 rate、按 case/tag/failure layer 的分布、latency/cost distribution，并单列 missing、`UNKNOWN`、`INCOMPARABLE`。

比较顺序也要保守：先冻结 objective、samples/splits、rubric/scorer、baseline/candidate manifests、sampling/budget 和判断规则，再执行 repeated trials；关键 manifest 无法隔离时是 `INCOMPARABLE`。可比之后，也要同时查看 per-case 变化、success/failure distribution、failure taxonomy 与 latency/cost distribution。若方向不稳定、样本不足、judge 未校准、分布难以区分或 `UNKNOWN` 太多，就保持 `UNKNOWN / REVIEW_REQUIRED`，不能补写“统计显著”。运行次数应由风险、case 异质性、成本和预先声明的 uncertainty method 决定，本文不规定固定 trial 数。

三类判断者的职责也不同：deterministic scorer 适合 exact/schema/invariant、tool arguments 和明确 policy breach；model judge 适合 rubric-bound 的语义变体，但必须绑定 versioned rubric，以及 judge model/provider/version、prompt/template、sampling、order/canonicalization 与适用范围的 judge manifest；human review 负责 rubric 与 Golden label 校准、抽查 disagreement、高风险和歧义 case。model judge 不能凭自身分数获得无版本的 release authority，human review 也不能用少量直觉样本替代 campaign。

`22-C13` 只有 current official sources 与课程 Proposal 支撑。结论只在已声明的 samples/splits、runs/trials、tested manifests、environment/time window、scorer/judge/human procedure 与 uncertainty boundary 内成立；Lab06 没有验证 stochastic Agent Eval，也不支持永久稳定、绝对质量、生产泛化或统计显著性结论。

**固定合同看 per-case delta；随机行为看 manifest-bound repeated trials 与分布，证据不足就保留 UNKNOWN。**

## 11. Eval 怎样进入发布门禁，又不冒充生产质量

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
4. 目标设计应让 threshold 与 release policy 独立版本化，变更要单独 review；Lab06 v1 尚未完成 scorer/gate policy 独立版本合同；
5. Eval PASS 只对 contract scope 有效，生产监控、canary、human review 与持续 eval 仍有自己的责任。

Lab06 能证明的上限是：它的 frozen mechanism 可重复抓住预先注入的 critical regression。它不能证明真实 Agent/model 已改善、生产风险受控、跨 Provider/model/环境泛化、统计显著性，也不能证明 BuildPilot 有任何运行行为。

因此 `22-C11` 保持 `PARTIAL`。固定集合全绿，可以支持当前合同内的决定；它不能把未测风险变成已经不存在。

**Eval 可以给发布一个可审计输入，不能替发布、生产监控或风险 owner 作全部决定。**

## 12. 下一次修复，至少留下这组可审计产物

1. 写清当前活动主要属于 Demo、Test、Benchmark、Eval 还是 Regression；
2. 冻结 objective、dataset/case revisions、oracle/scorer、metrics/gates、baseline 与 system manifest；
3. 把 Trace sample 保持 candidate，直到 lineage、review、acceptance、split 完整；
4. 保存 per-case observation、group metrics、unknown/incomparable 与 raw failure；
5. 先判 comparability，再给 change verdict；
6. 记录 split exposure、dedup 与 refresh；
7. 把 Eval 作为 release input，同时披露 proof ceiling；
8. 对 policy 或 threshold 变更做独立版本和 review。

本篇是 Part IV 的必修收束；Article 23/24 均为本次 non-scope，不启动、不预写、不链接。Lab fixture 之外，BuildPilot 仍是 `DESIGN / NOT IMPLEMENTED / NOT RUN`。全文证明上限只有两层：Lab06 的 deterministic fixture-scoped observation，以及 `22-C13` 的 source-backed `PARTIAL / COURSE PROPOSAL`；二者都不证明生产质量、泛化、永久稳定或统计显著性。

## Claim Traceability（13 / 13）

| Claim | Evidence ceiling | 正文落点 | 保留边界 |
|---|---|---|---|
| `22-C01 / 22-E01` | `PROPOSAL` | 五种活动分账 | 课程 ownership model，不称标准 |
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
| `22-C12 / 22-E12` | `CONFIRMED` | Part IV 边界 | Article23/24 non-scope；BuildPilot design-only |
| `22-C13 / 22-E13` | `PARTIAL` | deterministic vs stochastic | source-backed Proposal；no stochastic Lab / fixed trials / significance claim |

Coverage=`13 / 13`；Evidence Cards=`13 / 13`；状态保持 `3 CONFIRMED / 7 PARTIAL / 3 PROPOSAL / 0 BLOCKED`。

## Learning Check

1. Demo、Test、Benchmark、Eval、Regression 为什么不能互相替代？
2. 为什么 dataset + score 仍不是完整 Eval Contract？
3. 为什么真实 Trace 不能自动变成 Golden sample？
4. C01 的 input、Golden 与退化 candidate 分别是什么，为什么 aggregate 过线仍 overall FAIL？
5. `UNKNOWN` 与 `INCOMPARABLE` 为什么不能折成普通 0 分？
6. stochastic Agent Eval 为什么不能比较两个单次 aggregate，最低要保存哪些 manifest、trial 与 distribution？
7. deterministic scorer、model judge、human review 各负责什么，judge 为什么需要 versioned rubric/manifest 与 human calibration？
8. `scorer-policy.json` 为什么不代表 Lab06 v1 已实现通用配置解释器？

### 参考思路

1. 五类活动的 primary question、artifact 和 decision ownership 不同；基础设施共享不等于结论共享。
2. 还缺 objective、case/oracle/scorer、threshold/baseline、manifest/comparability、verdict policy、uncertainty 与 limitations。
3. 来源真实不等于有权使用、标签正确或适合某个 split；仍需 scope、redaction、dedup、review、acceptance 与 revision。
4. input=`tool.write.requested / MISSING / NOT_EXECUTED`；Golden=`FAIL / POLICY / [APPROVAL_MISSING]`；candidate=`PASS / NONE / []`。其余 7 case 通过，aggregate 无权覆盖 critical gate。
5. 前者是 observation/qualification 缺口，后者是比较合同不成立；强折 0 分会伪造可比测量。
6. 单 trial 无法分离真实退化与正常波动；至少保存 system、prompt/tool/policy/harness、sampling、per-trial、failure/latency/cost distributions 与 uncertainty。
7. scorer 管稳定合同，judge 管 rubric-bound 语义，human 管校准与高风险歧义；judge identity、rubric、prompt、sampling 与 human agreement 都会影响可比性。
8. v1 只解析 aggregate threshold；case、critical、missing/unknown、comparability 与 verdict 的部分语义固定在代码，三 manifest 拆分仍是 Proposal。

## 参考资料

- [NIST AI RMF 1.0, NIST AI 100-1, January 2023](https://www.nist.gov/publications/artificial-intelligence-risk-management-framework-ai-rmf-10)（TEVV、metrics、benchmark、uncertainty 与 generalizability limitation；页面注明 1.0 正在修订；不定义本文 schema 或 gate）
- [OpenAI Evaluation Best Practices](https://developers.openai.com/api/docs/guides/evaluation-best-practices)（objective、dataset、metrics、continuous eval、golden examples 与 vibe-based anti-pattern；动态 hosted docs）
- [OpenAI Evaluate agent workflows](https://developers.openai.com/api/docs/guides/agent-evals)（one Trace/one run 与 repeatable eval runs；不规定 trial 数）
- [OpenAI Graders](https://developers.openai.com/api/docs/guides/graders)（grader 形态、rubric、human calibration 与 model-judge 边界；不证明无偏真值）
- [OpenAI trustworthy third-party evaluations](https://openai.com/index/trustworthy-third-party-evaluations-foundations/)（受控比较的 task/scoring/budget 与 model/tools/harness/attempt/time/cost 披露；不定义本文 schema 或统计判据）
- [Google ML Crash Course: Dividing datasets](https://developers.google.com/machine-learning/crash-course/overfitting/dividing-datasets)（train/validation/test、wear-out、duplicates 与 representativeness；映射到 Agent eval 时保持有限）
- [Datasheets for Datasets, arXiv 1803.09010v8](https://arxiv.org/abs/1803.09010)（dataset documentation precedent；不定义 Golden acceptance）
- [上一篇：Trace、Replay 与 Failure Taxonomy]({{< relref "ai-empowerment/agent-engineering-21-trace-replay-failure-taxonomy.md" >}})（只交 candidate slices 与 lineage，不交 Golden/oracle/metric/verdict）

## 最短结论

`修复不会因为这次看起来成功就变得可靠；可靠来自一份能冻结比较条件、暴露关键退化，并诚实保留 unknown 与 incomparable 的评估合同。`

> **上一篇**：[Trace、Replay 与 Failure Taxonomy：错误究竟发生在哪一层]({{< relref "ai-empowerment/agent-engineering-21-trace-replay-failure-taxonomy.md" >}})

> **课程索引**：[Agent Engineering 系统课程]({{< relref "ai-empowerment/agent-engineering-series-index.md" >}})
