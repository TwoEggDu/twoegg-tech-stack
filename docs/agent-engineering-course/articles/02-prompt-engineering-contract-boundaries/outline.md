# Article 02 Detailed Outline｜Prompt Engineering：任务合同、角色、示例与边界

- Lifecycle Input：`EVIDENCE_READY`
- Evidence Gate：`PASS_RECOMMENDED`（`7 CONFIRMED / 1 PARTIAL / 0 BLOCKED / 1 PROPOSAL`）
- Outline Gate：`PASS_RECOMMENDED`（由 Master 核对后推进）
- Article Type：`原理篇`
- Course Weight：`M（Standard Core Lesson）`
- Target Length：`约 4,500—6,000 中文字`
- Target Reading Time：`12—16 分钟`
- Provider Counter-check：`OpenAI + Anthropic（以 2026-08-19 当前公开 contract 为限）`
- Required Lab：`NONE`
- Teaching Fixture：`02-FX01 / SYNTHETIC_COURSE_PROPOSAL / NOT_EXECUTED`

## 1. Reader Transformation

读者从“Prompt Engineering 就是寻找更聪明的措辞”，转变为能够把一次模型请求审查为 Goal、Constraints、Inputs、Examples、Output Requirements 与 Failure Semantics 六项任务职责；能够把稳定应用指令、当次目标与动态输入分开，再按当前 Provider contract 映射；也能够指出 Prompt、真实权限、当前事实、Structured Output 与 Eval 之间的 stop line。

完成本篇后，读者不需要记住一套跨 Provider 的 role hierarchy，而应知道：**可迁移的是职责分层，字段、role、位置、优先级与生命周期必须回到 Provider、API、模型和版本的当前 contract 核对。**

## 2. Teaching Spine

> 如果这篇只记一句话：`Prompt 是可审查的任务表达合同，不是权限、状态、事实校验或结果正确性的替代品。`

| Teaching Phase | Reader Movement | Main Sections | Claim / Evidence |
|---|---|---|---|
| Problem Space | 从“改几个词”转向“哪些任务条件没有被显式表达、变更后怎样判断” | Opening | `02-C01`、`02-C08` / `02-E01`、`02-E08` |
| Abstract Model | 建立六项任务合同 review checklist；明确它不是行业 schema | Section 1 | `02-C01` / `02-E01` |
| Concrete Engineering Mechanism | 分离 Stable Instruction、Per-request Goal、Dynamic Input，再映射到当前 Provider contract | Section 2 | `02-C02`、`02-C03` / `02-E02`、`02-E03` |
| Engineering Judgment / Boundary | 判断 examples、delimiters 的有限作用，并切开 permission、current facts 与 Structured Output | Section 3—4 | `02-C04`—`02-C07` / `02-E04`—`02-E07` |
| Verification Boundary | 把 Prompt 变更纳入版本、固定 fixture、标准与判定记录；用未执行 Unity synthetic fixture 演示验证设计而非验证结果 | Section 5—6 | `02-C08`、`02-C09` / `02-E08`、`02-E09` |

### M 级篇幅职责

- 主体只保留六节：一个任务合同模型、一个 Provider 映射机制、两组边界判断、一个最低变更合同和一个未执行教学夹具。
- 不扩写成 Prompt 技巧大全，不列“魔法词”或模板库。
- 不实现 Lab，不展示 schema / parser / repair loop，不设计 Context、Memory、RAG、Skill、Permission Runtime 或完整 Eval system。
- OpenAI 与 Anthropic 只用于证明 Provider-specific mapping；不增加第三家 Provider，也不设计统一 Adapter。

## 3. Opening｜同一段 Unity 日志，为什么“把 Prompt 写长一点”仍然不可审查？

- Reader Question：当团队不断给 Prompt 增加“请认真”“不要猜”“按格式返回”时，究竟在改哪一项任务条件，又凭什么判断修改有效？
- Core Thesis：Prompt Engineering 的可迁移问题不是寻找万能措辞，而是把目标、约束、输入、可选示例、输出要求、失败语义与成功标准变成可检查、可修改、可复测的任务表达。
- Claim IDs：`02-C01`、`02-C08`
- Evidence IDs：`02-E01`、`02-E08`
- Opening Scene：使用 `02-FX01` 的合成 Unity 编译日志作为纯教学场景；先提出“请总结这段 Unity 构建日志”这一模糊请求会留下哪些审查问题，不展示任何模型输出，不声称 Prompt B 更好。
- Problem Questions：
  1. 目标是摘要、定位首个失败点，还是提出修复？
  2. 可以使用哪些输入，哪些信息不足时必须写 `UNKNOWN`？
  3. 输出只是给人阅读，还是要交给程序消费？
  4. 改完以后，是“看起来更顺眼”，还是满足了预先定义的标准？
- Wording Strength：六项是课程 review checklist；“可检查、可复测”表示工程责任，不保证得到正确结果。
- Boundary / Stop Line：不把开场写成已执行 A/B，也不从合成日志推导 Unity 项目真实根因或诊断准确率。
- Bridge：先不用 Provider 字段回答问题，建立一个最小任务合同模型。

## 4. Section 1｜抽象模型：把 Prompt 审查成六项任务职责

- Reader Question：一份任务 Prompt 最少应该从哪些角度接受审查？
- Core Thesis：本课程使用 Goal、Constraints、Inputs、Examples、Output Requirements、Failure Semantics 六项职责审查任务表达；它们不是行业统一 schema，不要求每次请求机械填满或使用固定顺序。
- Claim IDs：`02-C01`
- Evidence IDs：`02-E01`
- Main Presentation：`Task Contract Review Canvas`

| Element | Review Question | Boundary |
|---|---|---|
| Goal | 这次调用到底要完成什么？ | 不包含权限授予 |
| Constraints | 范围、禁止项、证据与质量约束是什么？ | 不等于外部 policy enforcement |
| Inputs | 本次可用数据、来源与可信边界是什么？ | 不吞掉 Context 生命周期 |
| Examples | 是否需要输入 / 输出示范来表达模式？ | 可选；不等于 Knowledge Base |
| Output Requirements | 调用方需要什么呈现形状？ | 不等于 schema / domain validation |
| Failure Semantics | 输入或证据不足时怎样显式返回？ | 不替代异常处理、审批或审计 |

- Explanation Order：
  1. 先用 Goal 与 Constraints 定义“做什么 / 不做什么”。
  2. 再说明 Inputs 是调用方已经提供的材料，不是模型自动获得的 current facts。
  3. Examples 只在需要展示任务模式时加入，不是强制字段。
  4. Output Requirements 先解决自然语言呈现期待；机器合同留给下一篇。
  5. Failure Semantics 给“不足以完成”一个明确出口，避免用猜测填空。
- Figure Responsibility：Figure 1 只画六项职责共同进入 `Prompt / Request Contract`；图题必须注明 `course review abstraction, not provider wire schema`。
- Wording Strength：使用“本课程采用”“用于审查”“按任务选用”，不写“所有好 Prompt 必须具备六段”。
- Boundary / Stop Line：不把这六项命名成 Provider request fields，也不宣称填写完整即可保证正确、安全或稳定。
- Bridge：职责可以抽象，但真正发送请求时仍要落到 Provider-specific contract。

## 5. Section 2｜具体机制：稳定指令、当次目标与动态输入怎样分层？

- Reader Question：哪些内容应该稳定维护，哪些内容应该按请求注入；它们在不同 Provider 中是否有统一 role？
- Core Thesis：稳定应用指令、当次用户目标与动态输入应显式分层；职责可迁移，但字段、承载位置、优先级与生命周期必须按 Provider、API、模型和版本映射。
- Claim IDs：`02-C02`、`02-C03`
- Evidence IDs：`02-E02`、`02-E03`
- Abstract Responsibility Layers：

```text
Stable Application Instruction
  + Per-request User Goal
  + Dynamic Input / Application-observed Facts
  -> map through current Provider contract
  -> one concrete request
```

- Provider Mapping Table Responsibility：只并排呈现当前公开 contract 的差异，不把行位置画成跨 Provider 统一优先级阶梯。

| Responsibility | OpenAI current contract | Anthropic current contract | Portable Judgment |
|---|---|---|---|
| Stable application instruction | `instructions` / developer 等当前机制 | 顶层 `system`；部分当前模型另有受位置约束的 mid-conversation system 机制 | 职责可抽象，字段与生命周期不可直接等同 |
| Per-request goal / input | user message / `input` 等当前表达 | user message 与当前 Messages contract | 当次目标必须映射到当前 API |
| Dynamic values / facts | 应用显式管理 typed / reviewed inputs | 由 Application 观察后按当前 contract 提供 state update；raw untrusted content 不自动提升权重 | 动态事实来自 Application，不来自 Prompt 措辞 |

- Provider Boundary Callout：
  - OpenAI 当前 developer 与 user 的优先级事实只属于其当前 contract。
  - Anthropic 顶层 system 与部分模型的 mid-conversation system 机制必须同时保留 model / placement / version 限定。
  - **不得**汇总为 `system > developer > user > tool` 一类跨 Provider 固定 hierarchy，也不得发布统一 role enum。
- Concrete Engineering Payoff：模板 / builder 应让动态变量可见、可 review；“稳定 / 动态”是内容职责，不等于永久绑定某个 role。
- Wording Strength：使用“当前公开 contract”“部分当前模型”“需重新核对”，并保留核对日期。
- Boundary / Stop Line：不设计 Model Adapter / Gateway，不推断 Provider 内部如何解释 priority，不把任一 Provider 的字段写成课程抽象本体。
- Bridge：分层解决“内容放在哪里”，但 example 与 delimiter 仍只能辅助表达，不能提供保证。

## 6. Section 3｜Examples 与 Delimiters：帮助表达模式，不提供普遍效果或安全保证

- Reader Question：Few-shot 和 XML / Markdown 分隔究竟能帮助什么，又不能证明什么？
- Core Thesis：few-shot 可示范格式、语气、结构与任务模式，示例应 relevant、diverse、structured；delimiter / data marking 能改善 instructions、context、examples 与 variable input 的可辨识性。两者都需要按具体 workload 验证，delimiter 也不提供 Prompt Injection immunity。
- Claim IDs：`02-C04`、`02-C05`
- Evidence IDs：`02-E04`、`02-E05`
- Teaching Sequence：
  1. 用一个简短 input / output pair 说明 example 的职责是展示模式，不把示例写成 current fact source。
  2. 用 `<LOG>...</LOG>` 说明 delimiter 的职责是标记数据边界，不宣称某种 XML / Markdown 形式普遍最佳。
  3. 用反例说明：示例分布单一或携带旧假设是工程风险；不在本篇量化影响。
  4. 用安全 callout 说明：clear separation 只是 defense-in-depth 的一层，后续仍需要 validation、least privilege、approval 与 isolation。
- `02-C04 PARTIAL` Wording Contract：
  - Allowed：`few-shot 可示范格式、语气、结构与任务模式`；`示例应相关、多样、结构清楚`；`效果需在具体 workload 上验证`。
  - Forbidden：通用准确率提升、通用 token 成本结论、staleness / bias 的量化结论、`越多越好`、`所有任务都应使用 few-shot`。
- Figure / Example Responsibility：Example 1 只比较“没有显式模式示例”与“给出一个结构化示例”在任务表达上的差异；不伪造模型响应，不标注 accuracy 箭头。
- Wording Strength：`可示范 / 能改善可辨识性 / 可能携带旧假设风险`，不写成跨模型因果定律。
- Boundary / Stop Line：Few-shot 不等于 Knowledge Base、RAG、Memory 或 Skill；delimiter 不等于 authorization、sanitization 或 injection immunity。
- Bridge：表达更清楚之后，仍必须把 Prompt 外部的工程责任切出来。

## 7. Section 4｜工程判断：Prompt 能表达期望，但不能吞掉外部系统责任

- Reader Question：把“不要删除”“只说真话”“按 JSON 返回”写入 Prompt 后，哪些合同仍然没有成立？
- Core Thesis：Prompt 可以表达期望行为，但不授予真实权限，不替代 authorization、least privilege、approval、状态观测或 current-fact verification；自然语言 Output Requirement 也不等于 schema adherence、类型校验或领域正确性。
- Claim IDs：`02-C05`、`02-C06`、`02-C07`
- Evidence IDs：`02-E05`、`02-E06`、`02-E07`
- Boundary Matrix：

| Prompt Can Express | External Engineering Responsibility | Stop Line |
|---|---|---|
| “不要删除文件” | 工具权限、credential / filesystem scope、approval、least privilege | Article 19 |
| “只使用当前分支和构建号” | Application 观察、检索、更新与校验 current facts | Article 12—17 |
| “用这些标签区分日志与指令” | 输入 / 输出验证、隔离与 injection defense-in-depth | Article 19 |
| “按指定字段呈现” | schema adherence、parse、type / domain validation、repair | Article 03 |

- Anti-pattern A：把 `System Prompt says do not delete` 写成“删除已被禁止”。纠正为：这是行为指令；真实安全边界由 Prompt 外的 permission / approval / sandbox 建立。
- Anti-pattern B：把长期模板里的分支名、构建号当 current fact。纠正为：动态事实必须由 Application 观察、检索或校验后提供。
- Anti-pattern C：看到整齐 JSON 就写“机器合同已成立”。纠正为：自然语言格式要求不等于 schema 或领域值正确。
- Wording Strength：使用“不替代”“不能由此证明”，同时保留 Prompt 作为 defense-in-depth 一层的有限价值。
- Boundary / Stop Line：本节只建立责任边界；不展开 Permission Runtime、Context assembly、RAG pipeline、Memory lifecycle 或 Structured Output implementation。
- Bridge：既然措辞本身不提供保证，Prompt 变更就必须进入可复测的工程流程。

## 8. Section 5｜具体机制：Prompt 变更的最低版本与测试合同

- Reader Question：怎样让一次 Prompt 修改可以 code review、复测和追责，而不是只凭“输出看起来更好”批准？
- Core Thesis：最低 Prompt change contract 应保存 code / config version、显式动态变量、变更原因、代表性 fixtures、成功标准与判定记录；Provider Prompt ID 或某个托管 Eval 平台都不是可迁移必需项。
- Claim IDs：`02-C08`
- Evidence IDs：`02-E08`
- Change Contract Checklist：
  1. 保存 prompt builder / template 的代码或配置版本。
  2. 把动态变量列成显式、可 review 的输入。
  3. 记录变更原因与预期影响的检查项。
  4. 固定代表性输入、边界输入、成功标准与判定方式。
  5. 记录 Provider、模型快照 / 版本与关键生成参数。
  6. 保存原始输出与判定；不把手写理想结果冒充 observation。
- Table Responsibility：`Prompt Change Record` 只列版本、变量、fixture、criteria、raw output pointer、judgment；不引入某个 Provider 专属 Prompt ID 作为必填主键。
- Engineering Judgment：同一 fixture 与预先定义标准提供最低可比接口；它仍不是完整 Eval / Golden Dataset / Regression system。
- Wording Strength：使用“最低可迁移记录建议”“代表性 fixtures”，不写“固定参数可消除非确定性”或“一个 fixture 足以证明质量”。
- Boundary / Stop Line：不展开 dataset construction、grader calibration、metrics、continuous evaluation 或 regression governance；留给 Article 22。
- Bridge：最后用一个未执行的 Unity 合成夹具说明怎样冻结问题，而不是假装已经得到模型结论。

## 9. Section 6｜验证边界：`02-FX01` 只是一份未执行的 Unity synthetic fixture

- Reader Question：没有执行模型调用时，怎样用 A/B 设计教学，又不把 proposal 写成 runtime evidence？
- Core Thesis：`02-FX01` 只能展示如何冻结同一输入、Prompt 版本与有限检查项；它是 `SYNTHETIC_COURSE_PROPOSAL / NOT_EXECUTED`，没有 runtime observation，也不能证明 accuracy、鲁棒性、因果优越性或生产收益。
- Claim IDs：`02-C09`
- Evidence IDs：`02-E09`
- Mandatory Status Box：

```text
Fixture ID: 02-FX01
Classification: SYNTHETIC_COURSE_PROPOSAL
Execution Status: NOT_EXECUTED
Provider / Model / Parameters: NOT_SELECTED
Runtime Observation: NONE
Claim Status: PROPOSAL
```

- Fixed Synthetic Input：只引用 Evidence 中已冻结的 Unity 2022.3.62f3 / Android / `CompileScripts` 合成日志；明确它不是项目真实日志。
- Prompt A Responsibility：保留“请总结这段 Unity 构建日志”作为模糊基线文本，不展示输出。
- Prompt B Responsibility：按 Goal、Constraints、Input、Output Requirements、Failure Semantics 展示任务合同表达；不把它标为更准确或更安全。
- If-executed Observation Checklist：
  1. 规定字段是否出现；
  2. Primary Failure 是否直接来自输入日志；
  3. Evidence 是否引用输入；
  4. 未知信息是否显式标记。
- Verification Boundary：即使未来锁定同一 Provider、模型快照、参数和输入并执行一次，也只能得到上述有限 observation；完整 accuracy / regression 判断需要更广的 dataset、labels、criteria 与重复验证。
- Figure / Table Restriction：不放“Prompt A vs B 结果优劣表”，不写 illustrative output 为 observed output，不使用 accuracy、success rate 或 production impact 图表。
- Wording Strength：所有未来行为使用条件式“若未来执行，可检查……”，不使用“实验显示”“结果证明”“准确率提高”。
- Boundary / Stop Line：不实际运行 fixture，不新增 Lab，不选择 Provider / model，不进入 Article 22 的完整 Eval 设计。
- Bridge：回到最短结论，并用 Learning Check 验证读者能否做边界判断。

## 10. Closing｜更长的 Prompt 不是终点，可审查的任务合同才是

- Reader Question：读者离开本篇后，审查 Prompt 时应先问什么？
- Core Thesis：先问任务职责是否显式、稳定与动态内容是否分层、Provider contract 是否当前有效、外部责任是否被误吞、变更是否有固定 fixture 和判据；不要先问哪句“神奇措辞”最好。
- Claim IDs：`02-C01`、`02-C02`、`02-C03`、`02-C06`、`02-C08`、`02-C09`
- Evidence IDs：`02-E01`、`02-E02`、`02-E03`、`02-E06`、`02-E08`、`02-E09`
- Recap：
  - `Task contract checklist != industry schema`
  - `Stable responsibility != fixed cross-provider role`
  - `Few-shot != guaranteed accuracy or Knowledge Base`
  - `Delimiter != injection immunity`
  - `Prompt instruction != permission / current fact`
  - `Output requirement != Structured Output validation`
  - `Fixed fixture != complete Eval`
- Forward Link：Article 03 将接住 Output Requirement，正式建立机器可消费输出的 schema、parse、validate 与 repair 边界；本篇到自然语言任务合同为止。
- Final Sentence：`Prompt 的工程价值，不在于让一句话显得更聪明，而在于让任务、边界和变更变得可审查。`

## 11. Figure / Table / Fixture Responsibilities

| ID | Artifact | Teaching Responsibility | Must Not Imply |
|---|---|---|---|
| Figure 1 | `Task Contract Review Canvas` | 把六项职责放进同一审查模型 | 行业统一 schema、必填模板、正确性保证 |
| Figure 2 | `Responsibility-to-Provider Mapping` | 区分稳定职责与 Provider-specific fields / roles / lifecycle | 统一 role enum、跨 Provider hierarchy、Provider 内部实现 |
| Table 1 | `Examples / Delimiters: Help vs Boundary` | 对照模式表达、可辨识性与未证明项 | 通用 accuracy 提升、injection immunity |
| Table 2 | `Prompt vs External Responsibility` | 把 permission、current facts、Structured Output 从 Prompt 中切出 | Prompt 单独构成 security / validation boundary |
| Table 3 | `Prompt Change Record` | 展示 version、variables、reason、fixtures、criteria、output / judgment record | Prompt ID 必填、一个 fixture 等于完整 Eval |
| `02-FX01` | Unity synthetic A/B teaching fixture | 教怎样冻结输入与有限检查项 | runtime observation、accuracy improvement、鲁棒性或生产收益 |

Asset Policy：本轮不创建 `assets/`。Outline 只定义图表职责；未来 Draft 如使用文本图或表格，应保持可由 Markdown 表达，避免为未执行 fixture 生成“结果图”。

## 12. Learning Check Plan

1. 把“请帮我分析 Unity 日志”拆成六项任务合同职责；哪些项可以省略，理由是什么？
   - Reference Judgment：六项是 review checklist，不是强制表单；应能说明 Goal、可用 Inputs、关键 Constraints、Output / Failure Semantics 是否足以支撑当前任务。
2. “稳定指令、当次目标、动态事实”是否分别固定对应 system、developer、user 三个 role？
   - Reference Judgment：否。职责可迁移，字段、位置、优先级与生命周期必须按当前 Provider contract 映射。
3. 在 system / developer instruction 中写“不要删除文件”，是否已经撤销删除权限？
   - Reference Judgment：没有。真实权限、least privilege、approval 与 sandbox 属于 Prompt 外部工程控制。
4. Few-shot 示例能安全宣称哪些作用，哪些结论必须通过具体 workload 的 Eval 才能成立？
   - Reference Judgment：可说它示范格式、语气、结构与任务模式；不能据此宣称通用 accuracy 提升或量化 staleness / bias / token 影响。
5. Prompt B 输出字段更整齐，是否已经证明诊断更正确？
   - Reference Judgment：没有。自然语言格式遵循、schema adherence、领域正确性与 accuracy 是不同判断。
6. 你要修改一个生产 Prompt，最低应保留哪些记录？
   - Reference Judgment：code / config version、显式变量、变更原因、代表性 fixtures、成功标准、Provider / model / parameters、原始输出与判定记录。
7. `02-FX01` 目前能证明什么？
   - Reference Judgment：它没有 runtime evidence；只能作为未执行课程提案展示怎样冻结输入与检查项。

## 13. Claim-to-Section Coverage Matrix

| Claim ID | Status | Main Placement | Evidence | Wording / Coverage Guard |
|---|---|---|---|---|
| `02-C01` | `CONFIRMED` | Opening、Section 1、Closing | `02-E01` | 六项是课程 review checklist，不是行业 schema 或保证 |
| `02-C02` | `CONFIRMED` | Section 2、Closing | `02-E02` | 分层职责可迁移，role / field mapping 不可直接迁移 |
| `02-C03` | `CONFIRMED` | Section 2、Closing | `02-E03` | 只写 current contract；禁止统一 role enum / hierarchy |
| `02-C04` | `PARTIAL` | Section 3 | `02-E04` | 只写模式示范与 relevant / diverse / structured；不写通用提升或量化影响 |
| `02-C05` | `CONFIRMED` | Section 3—4 | `02-E05` | delimiter 改善可辨识性，但不是 injection immunity |
| `02-C06` | `CONFIRMED` | Section 4、Closing | `02-E06` | Prompt 不授予权限，不替代 state observation / fact verification |
| `02-C07` | `CONFIRMED` | Section 4 | `02-E07` | 自然语言 Output Requirement 不等于 schema / type / domain validation |
| `02-C08` | `CONFIRMED` | Opening、Section 5、Closing | `02-E08` | 最低 change contract；不绑定 Prompt ID 或托管 Eval 平台 |
| `02-C09` | `PROPOSAL` | Section 6、Closing | `02-E09` | 必须标 `NOT_EXECUTED`；无 runtime observation、accuracy 或 production claim |

Coverage Result：`9 / 9 Claims mapped`；`0 BLOCKED` 进入 Outline；`02-C04` 与 `02-C09` 均有显式 wording guard。

## 14. Job Competency Mapping

| Competency | Article Evidence of Learning | Assessment Surface |
|---|---|---|
| Prompt contract review | 能用六项 checklist 指出任务表达缺口，同时说明哪些项按任务可选 | Learning Check 1 |
| Instruction / input boundary | 能把 stable instruction、per-request goal、dynamic input 分层 | Learning Check 2 |
| Cross-provider contract reading | 能拒绝统一 role hierarchy，并列出需要核对的 Provider / API / model / version facts | Section 2 + Learning Check 2 |
| Few-shot / delimiter judgment | 能说明模式表达与可辨识性的有限作用，不外推 accuracy 或 security guarantee | Learning Check 4 |
| Injection / permission boundary | 能解释 Prompt instruction 与真实 authorization / approval / sandbox 的差异 | Learning Check 3 |
| Output contract boundary | 能区分自然语言 Output Requirement、schema adherence 与领域正确性 | Learning Check 5 |
| Prompt versioning | 能写出最低 Prompt change record，而不是只比较主观观感 | Learning Check 6 |
| Fixed-fixture evaluation awareness | 能正确标注 proposal、expected 与 observed 的差异 | Learning Check 7 |

## 15. Adjacent Article Stop Lines

| Future Article | Article 02 May Introduce | Article 02 Must Stop Before |
|---|---|---|
| Article 03｜Structured Output | 自然语言 Output Requirement 与“格式整齐不等于机器合同”的问题 | JSON Schema、schema adherence 机制、parse、type / domain validation、repair / refusal handling |
| Article 12｜Context Engineering | Prompt 是模型当前可见 Context 的一部分；Input 需要由调用方提供 | Context assembly、selection、ordering、packing 与 step-level visibility design |
| Article 13｜Context Debugging | delimiter 可帮助区分输入类型 | compression、pollution、truncation、snapshot 与可重建性诊断 |
| Article 14｜Working Memory | 动态任务状态不应硬编码进长期指令 | Investigation State、Working Memory 数据模型与更新生命周期 |
| Article 15｜Session / Long-term / Project Memory | Prompt 不会自行建立跨 step / session 的持久状态 | Session、Long-term Memory、Project Memory 的作用域、可信度、恢复与遗忘策略 |
| Article 16｜Knowledge Base / RAG | Few-shot 不是知识库；Prompt 只能使用已提供信息 | retrieve、filter、rerank、inject、cite 与知识来源治理 |
| Article 17｜Skill Engineering | 长期领域方法不应默认无限堆进单个 Prompt | Skill package、activation、按需加载、资源与领域方法治理 |
| Article 19｜Permission / Approval / Sandbox | Prompt instruction 与真实权限不同；delimiter 只是 defense-in-depth | authorization、least privilege、tool approval、HITL、sandbox、fail-closed policy implementation |
| Article 22｜Eval / Golden Dataset / Regression | Prompt 变更需要 fixed fixtures、success criteria 与记录 | dataset design、grader calibration、metrics、continuous eval、golden dataset 与 regression system |

Cross-boundary Rule：如果 Draft 需要上述 stop line 之后的机制才能支撑核心结论，返回 `RETURN_TO_RESEARCH` 或缩小叙事，不以“顺便解释”扩写后续课程。

## 16. Evidence Omission List

- 不新增第三家 Provider：OpenAI + Anthropic 已足以证明 Provider-specific mapping；增加来源不改变本篇可安全结论。
- 不列通用“最佳 Prompt 模板”：Evidence 只支持任务职责与 review abstraction。
- 不量化 few-shot 的 token、staleness、bias 或 accuracy 影响：`02-C04 = PARTIAL`。
- 不把某种 XML / Markdown delimiter 写成最佳安全格式：Evidence 只支持可辨识性与 defense-in-depth。
- 不复制 Provider 的 role / priority 为跨 Provider hierarchy：`02-C03` 明确禁止。
- 不展开 Prompt ID 与即将退出的托管对象：只保留 code / config + fixture + criteria 的可迁移责任。
- 不执行 `02-FX01`，不手写模型输出，不写 accuracy、鲁棒性、因果优越性或生产收益：`02-C09 = PROPOSAL / NOT_EXECUTED`。

## 17. Outline Gate Checklist

- [x] Teaching Spine 遵循 Problem Space -> Abstract Model -> Concrete Engineering Mechanism -> Engineering Judgment / Boundary -> Verification Boundary
- [x] 六个主体 Section 均包含 Reader Question、Claim IDs、Evidence IDs、Wording Strength 与 Boundary / Stop Line
- [x] `9 / 9` Claim 已映射，核心行为性 Claim 为 `0 BLOCKED`
- [x] `02-C04 PARTIAL` 只使用收窄措辞，并列出禁止外推项
- [x] `02-C09` 明确为 `SYNTHETIC_COURSE_PROPOSAL / NOT_EXECUTED`，没有 runtime observation 或 accuracy improvement
- [x] Stable responsibility 与 Provider-specific role / instruction contract 分开，没有统一 hierarchy
- [x] 图、表与 Unity synthetic fixture 均有明确教学职责和不得暗示项
- [x] Learning Check 可验证 Reader Promise，Job Competency mapping 有 assessment surface
- [x] Article 03、12—17、19、22 stop lines 已逐项冻结
- [x] 保持 M 级职责，不扩成 Prompt 技巧、Context / Memory / Security / Eval 大全

Recommendation：`PASS`。建议 Master 将 Article 02 推进为 `OUTLINE_READY`；下一允许动作仅为依据本 Outline 启动 Draft Gate。Author 不更新 README、`status.md`、`course-run-state.md` 或其他 global durable state。
