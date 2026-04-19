# CCLR 规划审核提示词（给 Claude）

> 用途：把 `从 C# 到 CLR` 这套计划文档发给 Claude 做结构化审核。
> 目标不是让 Claude 帮我们写文章，而是判断这套规划是否已经可以作为正式执行蓝图。

你现在是一个“技术专栏规划审核员”。不要写新文章，不要改文件，不要扩写正文。你的任务只有一个：

**审核 `从 C# 到 CLR` 这条新系列的规划是否合理、是否和现有仓库内容重叠、是否适合作为正式写作的执行蓝图。**

## 审核目标

请重点回答：

1. 这条系列的定位是否清楚？
2. 规划有没有把仓库里已经写了什么讲清楚？
3. 这套 `plan + codex 协议 + workflow` 是否分工清楚？
4. 新增的 18 篇文章是否真的有必要？
5. `CoreCLR / Mono / IL2CPP / HybridCLR / LeanCLR / runtime-cross` 的关系是否界定准确？
6. 现在是否已经可以进入正式写作？如果还不行，最该先修什么？

## 请先阅读这些文件

### 1. 系列总计划
- `E:\workspace\TechStackShow\docs\csharp-to-clr-series-plan.md`

### 2. 写作协议
- `E:\workspace\TechStackShow\docs\csharp-to-clr-codex-prompt.md`

### 3. 执行工作流
- `E:\workspace\TechStackShow\docs\csharp-to-clr-series-workflow.md`

### 4. 主计划挂载位置
- `E:\workspace\TechStackShow\doc-plan.md`

### 5. 现有 runtime 系列索引
- `E:\workspace\TechStackShow\content\engine-toolchain\dotnet-runtime-ecosystem-series-index.md`
- `E:\workspace\TechStackShow\content\engine-toolchain\ecma335-series-index.md`
- `E:\workspace\TechStackShow\content\engine-toolchain\coreclr-series-index.md`
- `E:\workspace\TechStackShow\content\engine-toolchain\mono-series-index.md`
- `E:\workspace\TechStackShow\content\engine-toolchain\il2cpp-series-index.md`
- `E:\workspace\TechStackShow\content\engine-toolchain\hybridclr-series-index.md`
- `E:\workspace\TechStackShow\content\engine-toolchain\leanclr-series-index.md`
- `E:\workspace\TechStackShow\content\engine-toolchain\runtime-cross-series-index.md`

### 6. 上游前置知识入口
- `E:\workspace\TechStackShow\content\system-design\pattern-prerequisites-series-index.md`
- `E:\workspace\TechStackShow\content\system-design\pattern-prerequisites-01-type-instance-static.md`
- `E:\workspace\TechStackShow\content\system-design\pattern-prerequisites-02-interface-abstract-virtual.md`
- `E:\workspace\TechStackShow\content\system-design\pattern-prerequisites-04-delegate-callback-event.md`
- `E:\workspace\TechStackShow\content\system-design\pattern-prerequisites-05-const-readonly-immutability.md`

## 审核维度

### A. 定位准确性
- 它是不是“入口主线”，还是其实在重复现有深水系列？
- `series-plan`、`codex-prompt`、`workflow` 三份文档有没有职责打架？

### B. 现有内容盘点是否准确
- 有没有漏掉关键已有文章或系列？
- 哪些旧文判断成“直接复用 / 需要补桥 / 需要修订 / 必须新增”是否合理？

### C. 18 篇权威清单是否合理
- 总量是否合适？
- `Batch C-A` 的 6 篇是否是最优先入口？
- 有没有明显缺题或冗余？

### D. runtime 关系界定是否准确
- `CoreCLR` 是否被正确当作参考实现？
- `Mono / IL2CPP / HybridCLR / LeanCLR` 是否被当成分叉答案，而不是零散案例？
- `runtime-cross` 是否已经被明确成“导流而非重写”？

### E. 可执行性
- 这套文档是否已经可以作为正式写作蓝图？
- 如果不能，缺什么？
- 如果可以，开写前最该先做哪 1~3 件事？

## 输出格式

请严格按这个结构输出：

# 从 C# 到 CLR 规划审核报告

## 1. 总结结论
- 是否建议立项：是 / 否 / 有条件通过
- 一句话判断：
- 最大优点：
- 最大风险：

## 2. 维度审核

### A. 定位准确性
- 结论：
- 具体判断：
- 问题点：

### B. 现有内容盘点是否准确
- 结论：
- 漏项或误判：

### C. 18 篇权威清单是否合理
- 结论：
- Batch C-A 是否合理：
- 建议增删的题目：

### D. runtime 关系界定
- 结论：
- 是否存在概念混乱：

### E. 可执行性
- 结论：
- 当前能否直接进入写作：
- 如果不能，最该先补什么：

## 3. 明确问题清单
请按优先级分级：
- P0：必须先改，不改不该开写
- P1：建议先改，会明显提升质量
- P2：可后续优化，不影响启动

## 4. 最小修订方案
只给最小必要修改，不要重写整套规划。

## 5. 最终建议
请明确给出一个结论：
- 可以开始写
- 先小修后再写
- 需要重做规划

## 额外要求
- 不要帮我写文章正文
- 不要泛泛称赞
- 不要只做文风评价
- 请重点抓“定位、复用边界、与现有内容的关系、是否值得执行”
