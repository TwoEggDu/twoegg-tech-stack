# CCLR-00 大纲草案

## 基本信息

- 文章编号：`CCLR-00`
- 暂定标题：`CCLR-00｜从 C# 到 CLR：这条线到底在讲什么`
- 暂定定位：`从 C# 到 CLR` 系列总入口 / 导读页
- 建议正文位置：`content/engine-toolchain/`（待作者最终确认）
- 预估字数：`1800~2600`

## 本篇要立住的 1 个核心洞察

`C# 表层概念不是一组语法词，而是一组 runtime 入口标签。你写下 class、interface、virtual、async 的那一刻，就已经把问题分别送进了对象模型、调用分派、状态机和执行模型。`

## 本文开头 3 段内必须说清的边界

- 本文不展开 `MethodTable / object header / GC / JIT / AOT` 细节，那是后续 C 层和下游深水文的职责。
- 本文不重写 `设计模式前置知识` 里的概念定义，只负责把“概念层”接到“规范层 / 实现层”。
- 本文不做多 runtime 横向总比较；涉及 `Mono / CoreCLR / IL2CPP / HybridCLR / LeanCLR` 时只负责立坐标并导流。

## 主小节结构（控制在 5 节内）

1. **为什么这篇必须单独存在**
   - 解释“会写 C#”不等于“知道这些词在 runtime 里怎么落地”
   - 点出本系列和 `设计模式前置知识`、`ECMA-335 / CoreCLR`、`runtime-cross` 的关系

2. **先看 3 个极小样例：对象、分派、闭包**
   - 样例 A：`new + instance method`
   - 样例 B：`interface` 调用
   - 样例 C：`delegate / lambda` 捕获

3. **把这条阅读线分成三层**
   - C# 表层概念层
   - CLI / ECMA-335 规范层
   - CoreCLR / Mono / IL2CPP / HybridCLR / LeanCLR 实现层

4. **直觉 vs 真相**
   - 你以为语法只是写法差异
   - 实际上不同写法会触发不同的对象分配、调用分派、代码生成和运行时约束

5. **从这里往下怎么走**
   - 去 `CCLR-01`
   - 去 `CCLR-06`
   - 去 `CCLR-10`
   - 旁路到 `runtime-cross / HybridCLR / LeanCLR`

## 最小代码示例主题

### 样例 1：对象 + 实例方法
- 用一个 `Counter` 或 `ShippingOrder` 级别的小例子
- 作用：建立“类型定义 != 运行时对象”这层直觉

### 样例 2：接口调用 + 多态分派
- 用 `IDiscountRule -> VipDiscountRule`
- 作用：引出“同一行调用，背后可能是接口分派、虚调用、去虚拟化”

### 样例 3：委托 + 闭包
- 用 `MakeAdder` 或等价样例
- 作用：引出“lambda 不只是短写法，它可能生成闭包对象和委托对象”

## 准备给出的 1 组“直觉 vs 真相”

- 你以为：`new` 只是“创建对象”，`interface` 只是“更抽象”，lambda 只是“更短的函数写法”。
- 实际上：`new` 牵出对象分配和生命周期，`interface` 牵出调用分派和优化边界，lambda 常常牵出委托对象、闭包对象和额外生命周期。
- 原因是：C# 语法不是终点，每个表层概念都会落到 metadata、对象布局、调用规则和执行模型上。

## 准备导流的下游链接

### 向上链
- `content/system-design/pattern-prerequisites-series-index.md`

### 向下链
- `CCLR-01｜值类型、引用类型、对象：先把 3 个最容易混的词讲清楚`
- `CCLR-06｜从 C# 到 CLI：语言前端、CTS、CLS 到底怎么对应`
- `CCLR-10｜对象在 CoreCLR 里怎么存在：对象头、MethodTable、字段布局`

### 向旁链
- `content/engine-toolchain/ecma335-series-index.md`
- `content/engine-toolchain/coreclr-series-index.md`
- `content/engine-toolchain/runtime-cross-series-index.md`

## 本轮调研结论（供写作时钉边界）

- 这篇最容易和 `pattern-prerequisites-series-index.md` 重叠，所以必须强调“这里只立 runtime 主线，不重讲概念定义”。
- 这篇最容易越界侵入 `CCLR-01 / 06 / 10 / 14`，所以每个点都只立坐标，不提前吃掉后续篇次。
- `delegate / event / async` 最适合作为“语言看起来很轻，但 runtime 会长出更多结构”的例子。
- `CCLR-00` 最终应是一篇“总入口”，不是“概念总论”，更不是“runtime 总论”。
