# 从 C# 到 CLR：入口主线系列规划

## 一、这条计划要做什么

这条计划不是要再写一套 `C# 语法大全`，也不是把现有 `ECMA-335 / CoreCLR / Mono / IL2CPP / HybridCLR / LeanCLR / runtime-cross` 深水文重写一遍。

它真正要做的事情是：

`补出一条从 C# 语言表层出发，顺着类型系统、对象模型、调用分派、对象布局、执行模型一路走到 ECMA-335、CoreCLR、Mono、IL2CPP、HybridCLR、LeanCLR 的连续阅读线。`

也就是说，这条系列的角色是：

- 对上承接 `设计模式前置知识`
- 对下桥接 `ECMA-335 / CoreCLR`
- 对右分叉到 `Mono / IL2CPP / HybridCLR / LeanCLR / runtime-cross`

一句话定性：

`前置知识解决“别卡在词上”，这条系列解决“这些词在语言、规范和运行时里到底怎么落地”。`

---

## 二、仓库里已经有什么

这一步不是从零开始。仓库里已经有一整套很深的 runtime 生态内容。

### 1. 已有总索引与系列规划

已经存在的核心索引与规划包括：

- `content/engine-toolchain/dotnet-runtime-ecosystem-series-index.md`
- `content/engine-toolchain/ecma335-series-index.md`
- `content/engine-toolchain/coreclr-series-index.md`
- `content/engine-toolchain/mono-series-index.md`
- `content/engine-toolchain/il2cpp-series-index.md`
- `content/engine-toolchain/hybridclr-series-index.md`
- `content/engine-toolchain/leanclr-series-index.md`
- `content/engine-toolchain/runtime-cross-series-index.md`
- `docs/dotnet-runtime-ecosystem-master-plan.md`

这说明仓库并不缺 runtime 深度，缺的是“入口主线”。

### 2. 已有可直接复用的深水文章

#### A. CLI / ECMA-335 标准层

- `content/engine-toolchain/ecma335-type-system-value-ref-generic-interface.md`
- `content/engine-toolchain/ecma335-memory-model-object-layout-gc-contract-finalization.md`
- `content/engine-toolchain/ecma335-custom-attributes-reflection-encoding.md`

#### B. CoreCLR 参考实现层

- `content/engine-toolchain/coreclr-type-system-methodtable-eeclass-typehandle.md`
- `content/engine-toolchain/coreclr-generics-sharing-specialization-canon.md`
- `content/engine-toolchain/coreclr-reflection-emit-dynamic-code-generation.md`
- `content/engine-toolchain/coreclr-ryujit-il-to-ir-to-native-code.md`
- `content/engine-toolchain/coreclr-gc-generational-precise-workstation-server.md`

#### C. Mono / IL2CPP / HybridCLR / LeanCLR / cross-runtime 层

- `content/engine-toolchain/mono-architecture-overview-embedded-runtime-unity.md`
- `content/engine-toolchain/mono-mini-jit-il-to-ssa-to-native.md`
- `content/engine-toolchain/mono-sgen-gc-precise-generational-nursery.md`
- `content/engine-toolchain/il2cpp-architecture-csharp-to-cpp-to-native-pipeline.md`
- `content/engine-toolchain/runtime-cross-type-system-methodtable-il2cppclass-rtclass.md`
- `content/engine-toolchain/runtime-cross-generic-implementation-sharing-specialization-fgs.md`
- `content/engine-toolchain/runtime-cross-method-execution-jit-aot-interpreter-hybrid.md`
- `content/engine-toolchain/hybridclr-bridge-il2cpp-generic-sharing-rules.md`
- `content/engine-toolchain/hybridclr-aot-generics-and-supplementary-metadata.md`
- `content/engine-toolchain/hybridclr-principle-from-runtimeapi-to-interpreter-execute.md`
- `content/engine-toolchain/leanclr-object-model-rtobject-rtclass-vtable.md`
- `content/engine-toolchain/leanclr-type-system-generic-inflation-interface-dispatch.md`
- `content/engine-toolchain/leanclr-vs-hybridclr-two-routes-same-team.md`

### 3. 已有桥接层内容

这部分尤其重要，因为它们决定我们是不是在“重复补桥”。

#### A. 设计层桥接入口

- `content/system-design/pattern-prerequisites-series-index.md`
- `content/system-design/pattern-prerequisites-01-type-instance-static.md`
- `content/system-design/pattern-prerequisites-02-interface-abstract-virtual.md`
- `content/system-design/pattern-prerequisites-03-inheritance-composition-dependency.md`
- `content/system-design/pattern-prerequisites-04-delegate-callback-event.md`
- `content/system-design/pattern-prerequisites-05-const-readonly-immutability.md`

#### B. HybridCLR 已有桥接文

- `content/engine-toolchain/hybridclr-pre-cli-metadata-typedef-methoddef-token-stream.md`
- `content/engine-toolchain/hybridclr-pre-cil-instruction-set-stack-machine-model.md`
- `content/engine-toolchain/hybridclr-bridge-abi-cross-boundary-calling-convention.md`
- `content/engine-toolchain/hybridclr-bridge-il2cpp-generic-sharing-rules.md`
- `content/engine-toolchain/hybridclr-bridge-interpreter-basics-dispatch-stack-register-ir.md`
- `content/engine-toolchain/hybridclr-bridge-il2cpp-gc-model-boehm-root-write-barrier.md`

这组桥接文说明：

`仓库里并不是完全没有“入口桥”，而是这些桥还没有被收成一条从 C# 表层概念持续走向 runtime 的主线。`

---

## 三、已有文章应该怎么处理

这一节必须写清楚，因为这决定了我们接下来是“补主线”还是“重写生态”。

### 1. 直接复用，不重写主体

下面这些内容已经足够深，应该作为下游阅读或旁路对照，不应重写成新系列正文：

- `ecma335-*` 深水文
- `coreclr-*` 深水文
- `mono-*` 深水文
- `il2cpp-*` 深水文
- `runtime-cross-*` 横向对照文
- `hybridclr-*` 原理、工具链、AOT 泛型、补 metadata、桥接机制文
- `leanclr-*` 对象模型、类型系统、路线对比文

策略：

`保留原文，作为新系列的“继续追深”链接，不重复展开主体。`

特别说明：

`本系列不重写 runtime-cross G1~G9。涉及“同一组 C# 语义在多 runtime 下的差异”时，原则上只做入口导流，不再重写横向总比较。`

### 2. 需要补桥，不需要重写主体

下面这些内容已经存在，但需要增加“从 C# 到 CLR”这条入口桥：

- `content/system-design/pattern-prerequisites-series-index.md`
- `content/system-design/pattern-prerequisites-01-type-instance-static.md`
- `content/system-design/pattern-prerequisites-02-interface-abstract-virtual.md`
- `content/system-design/pattern-prerequisites-05-const-readonly-immutability.md`
- `content/engine-toolchain/dotnet-runtime-ecosystem-series-index.md`

策略：

- 在前置知识文末尾增加“往下再走一步：它在 .NET / CLR 里怎么实现”小节
- 在 runtime 总索引里增加“从 C# 概念进入”的入口分流
- 在关键模式文章开头补“读这篇之前”卡片，链回前置文和新系列入口

### 3. 需要修订后再当正式入口使用

这一步不是“之后再看”，而是正式开写前的阻塞项。

当前最需要单独核查并修订的是：

- `pattern-prerequisites-01-type-instance-static.md`
- `pattern-prerequisites-02-interface-abstract-virtual.md`
- `pattern-prerequisites-05-const-readonly-immutability.md`

原因不是它们方向错，而是：

- 这几篇现在还没有和 runtime 文章形成稳定桥接
- 当前文件内容存在明显 encoding / 内容状态问题，不能直接作为系列入口挂给读者
- 作为上游入口，它们必须先稳定，再去挂更多下游链接

建议一并核查：

- `pattern-prerequisites-03-inheritance-composition-dependency.md`
- `pattern-prerequisites-04-delegate-callback-event.md`
- `content/engine-toolchain/dotnet-runtime-ecosystem-series-index.md`

这里的结论要写重一点：

`这不是“有空再润一下”，而是正式开写前的 P0 清理任务。`

### 4. 需要新增的内容

真正缺的不是更多深水文，而是这些“入口层但不浅”的主题：

- 值类型、引用类型、对象
- 内建类型的运行时语义
- `string` 和 `object`
- `class / struct / record`
- 装箱与拆箱
- `interface / virtual / override` 的运行时分派
- 委托、事件、async 与 continuation 的运行时入口
- 同一组语义在 `Mono / CoreCLR / IL2CPP / HybridCLR / LeanCLR` 里的不同工程答案

这部分就是新系列真正要补出来的主干。

---

## 四、系列定位与边界

### 1. 这条系列是什么

它是：

- `设计模式前置知识` 的下游机制层
- `ECMA-335 / CoreCLR` 的上游入口层
- `Mono / IL2CPP / HybridCLR / LeanCLR / runtime-cross` 的分叉桥梁

### 2. 这条系列不是什么

它不是：

- 完整的 C# 语法教程
- 另一套 CLR 源码逐文件索引
- 替代现有 `ECMA-335 / CoreCLR / Mono / IL2CPP / HybridCLR / LeanCLR` 系列的重写版
- Unity API 用法专题
- 另一套 `runtime-cross` 总比较系列

### 3. 这条系列要覆盖哪些内容

属于这条系列的内容：

- C# 内建类型在运行时里的真实语义
- `class / struct / record / object / string` 的边界
- 值类型、引用类型、对象、装箱、拆箱
- 数组、字符串、连续内存的基础模型，以及为后续理解 `Span / ref struct` 留出的入口坐标
- `interface / virtual / override` 的调用分派
- `delegate / event / async` 的运行时入口
- 从 C# 表层概念映射到 ECMA-335 / CoreCLR / Mono / IL2CPP / HybridCLR / LeanCLR 的路径

不属于这条系列的内容：

- 完整 C# 入门
- 完整 CLR 源码逐文件索引
- 完整 HybridCLR 深度系列替代版
- `runtime-cross G1~G9` 的横向重写版
- 数组 / `Span` / `只读引用` / `ref struct` 的深入语义总论（下沉到 `ECMA-335 / CoreCLR / runtime-cross` 深水文）
- Unity 具体 API 实战

---

## 五、系列总结构（18 篇权威清单）

下面这 18 篇是本计划的**权威清单**。后续批次和排期必须与这里保持一致。

### A 层：语言表层入口（6 篇）

职责：

`先把读者在 C# 表层看到的类型和语义分清楚。`

1. CCLR-00｜从 C# 到 CLR：这条线到底在讲什么
2. CCLR-01｜值类型、引用类型、对象：先把 3 个最容易混的词讲清楚
3. CCLR-02｜int、bool、enum、char、decimal：内建类型不是“特殊语法”，而是运行时约定
4. CCLR-03｜string 和 object：一个最特殊，一个最基础
5. CCLR-04｜class、struct、record：三种边界，不是三种写法
6. CCLR-05｜装箱与拆箱：什么时候只是转换，什么时候真的产生对象

### B 层：CLI / ECMA-335 规范桥（4 篇）

职责：

`把 C# 表层概念映射到 CLI 标准。`

7. CCLR-06｜从 C# 到 CLI：语言前端、CTS、CLS 到底怎么对应
8. CCLR-07｜ECMA-335 里的值类型和引用类型：先把类型分类对上号
9. CCLR-08｜成员的元数据长什么样：方法、字段、属性和事件怎么被描述
10. CCLR-09｜泛型约束和签名：不是更难写，而是更早把边界说清楚

### C 层：CoreCLR 参考实现桥（4 篇）

职责：

`把前两层的概念落到最适合做参考的 runtime 实现上。`

11. CCLR-10｜对象在 CoreCLR 里怎么存在：对象头、MethodTable、字段布局
12. CCLR-11｜值类型到底在哪里：栈、堆、寄存器和“值类型都在栈上”的误解
13. CCLR-12｜virtual、interface、override：多态分派到底怎么跑
14. CCLR-13｜delegate、event、async：把行为交给运行时和框架去安排

说明：

- `CCLR-10 ~ CCLR-12` 都属于**入口导引版**，不是重写 `coreclr-type-system-methodtable-eeclass-typehandle.md`
- 这几篇要明确承担“先立坐标，再把读者导到 B3 / G2 / G3”而不是“把深水细节吞掉”

### D 层：跨运行时分叉（4 篇）

职责：

`告诉读者：同一组语言语义，在不同 runtime 约束下会长成不同工程形态。`

15. CCLR-14｜Mono、CoreCLR 与 IL2CPP：同样的 C#，为什么会走向三种执行模型
16. CCLR-15｜从 AOT 到热更新：为什么 HybridCLR 要补 metadata、解释器和 bridge
17. CCLR-16｜从零到 CLR：LeanCLR 为什么选择另一条路
18. CCLR-17｜同一组 C# 语义，在不同 runtime 里分别牺牲了什么

说明：

- `CCLR-14` 明确把 Mono 纳入 D 层，不再让 Unity runtime 演进线断掉
- `CCLR-17` 只做入口总收束，不重写 `runtime-cross G1~G9`
- `CCLR-17` 的正文颗粒控制在 4~5 段 trade-off 总结，职责是按问题类型把读者送进 `runtime-cross-series-index`

---

## 六、最值得先写的第一批（Batch C-A）

虽然总规划是 18 篇，但不建议一口气全开。

更稳的做法是先写 6 篇入口主线，把最缺的阅读坡道立起来。

### Batch C-A（6 篇）

1. CCLR-00｜从 C# 到 CLR：这条线到底在讲什么
2. CCLR-01｜值类型、引用类型、对象：先把 3 个最容易混的词讲清楚
3. CCLR-02｜int、bool、enum、char、decimal：内建类型不是“特殊语法”，而是运行时约定
4. CCLR-03｜string 和 object：一个最特殊，一个最基础
5. CCLR-04｜class、struct、record：三种边界，不是三种写法
6. CCLR-05｜装箱与拆箱：什么时候只是转换，什么时候真的产生对象

这 6 篇的目标不是进入深水，而是：

`把“C# 表层概念 -> runtime 心智模型”的坡道先铺出来。`

---

## 七、后续批次怎么走（与 18 篇权威清单一致）

### Batch C-B：标准语义层（4 篇）

1. CCLR-06｜从 C# 到 CLI：语言前端、CTS、CLS 到底怎么对应
2. CCLR-07｜ECMA-335 里的值类型和引用类型：先把类型分类对上号
3. CCLR-08｜成员的元数据长什么样：方法、字段、属性和事件怎么被描述
4. CCLR-09｜泛型约束和签名：不是更难写，而是更早把边界说清楚

### Batch C-C：参考实现层（4 篇）

1. CCLR-10｜对象在 CoreCLR 里怎么存在：对象头、MethodTable、字段布局
2. CCLR-11｜值类型到底在哪里：栈、堆、寄存器和“值类型都在栈上”的误解
3. CCLR-12｜virtual、interface、override：多态分派到底怎么跑
4. CCLR-13｜delegate、event、async：把行为交给运行时和框架去安排

### Batch C-D：跨运行时分叉层（4 篇）

1. CCLR-14｜Mono、CoreCLR 与 IL2CPP：同样的 C#，为什么会走向三种执行模型
2. CCLR-15｜从 AOT 到热更新：为什么 HybridCLR 要补 metadata、解释器和 bridge
3. CCLR-16｜从零到 CLR：LeanCLR 为什么选择另一条路
4. CCLR-17｜同一组 C# 语义，在不同 runtime 里分别牺牲了什么

这四个批次加总，和第五节的 18 篇权威清单一一对应，不再允许出现“总表一套、批次一套”的情况。

---

## 八、与现有文章怎么互链

### 1. 每篇新文都要至少做三种链接

- **向上链**：链到 `设计模式前置知识` 或总入口页
- **向下链**：链到一个 `ECMA-335 / CoreCLR` 深水文
- **向旁链**：链到一个 `Mono / IL2CPP / HybridCLR / LeanCLR` 对照文

### 2. 最关键的桥接组合

#### `CCLR-01 值类型 / 引用类型 / 对象`

- 上游：`pattern-prerequisites-01-type-instance-static`
- 下游：`ecma335-type-system-value-ref-generic-interface`
- 对照：`runtime-cross-type-system-methodtable-il2cppclass-rtclass`

#### `CCLR-03 string 和 object`

- 上游：`pattern-prerequisites-01-type-instance-static`
- 下游：`ecma335-memory-model-object-layout-gc-contract-finalization`
- 对照：`il2cpp-architecture-csharp-to-cpp-to-native-pipeline`

#### `CCLR-05 装箱与拆箱`

- 上游：`pattern-prerequisites-05-const-readonly-immutability`
- 下游：`coreclr-generics-sharing-specialization-canon`
- 对照：`hybridclr-bridge-il2cpp-generic-sharing-rules`

#### `CCLR-12 多态分派`

- 上游：`pattern-prerequisites-02-interface-abstract-virtual`
- 下游：`coreclr-type-system-methodtable-eeclass-typehandle`
- 对照：`runtime-cross-type-system-methodtable-il2cppclass-rtclass`

#### `CCLR-13 delegate / event / async`

- 上游：`pattern-prerequisites-04-delegate-callback-event`
- 下游：`coreclr-type-system-methodtable-eeclass-typehandle`
- 对照：`et-pre-02-async-await-state-machine-and-continuation`

#### `CCLR-15 HybridCLR 为什么要补这些东西`

- 上游：`il2cpp-architecture-csharp-to-cpp-to-native-pipeline`
- 下游：`hybridclr-principle-from-runtimeapi-to-interpreter-execute`
- 对照：`leanclr-vs-hybridclr-two-routes-same-team`

#### `CCLR-17 多 runtime 牺牲了什么`

- 上游：`dotnet-runtime-ecosystem-series-index`
- 下游：不重写主体，统一导流 `runtime-cross-series-index`
- 对照：`runtime-cross-method-execution-jit-aot-interpreter-hybrid`

---

## 九、CoreCLR、Mono、IL2CPP、HybridCLR、LeanCLR 在这条系列里的角色

### 1. CoreCLR 的角色

CoreCLR 是这条系列默认选择的**参考实现**。

原因很简单：

- 它是最完整、最适合承接 CLI 语义落地问题的主参考实现
- 仓库里相关文章密度最高、链路最完整
- 它最适合承担“语言语义落地后，在真实 runtime 里怎么跑”这类解释任务

### 2. Mono 的角色

Mono 在这条系列里不能消失。

它承担的是：

`Unity 在 IL2CPP 之前长期使用的运行时现实，以及“为什么 Unity 的运行时路线不是直接从 CoreCLR 跳到 IL2CPP”的历史桥。`

它至少要在 D 层承担一个角色：

- 把 Unity 的 runtime 演进线补全
- 解释为什么 Editor / 老项目 / 某些工具链讨论里，Mono 仍然是绕不过去的一层

### 3. IL2CPP 的角色

IL2CPP 是：

`同样的 C# 语义在 AOT 约束下的主工程答案。`

它是理解 HybridCLR、AOT 泛型、补 metadata、热更新边界的直接前提。

### 4. HybridCLR 的角色

HybridCLR 不能被写成“一个热更新插件”。

它更准确的角色是：

`在 IL2CPP 已经决定了 AOT 和运行时边界之后，再把解释器、补充 metadata、MethodBridge、AOT 泛型补救和热更链路补进去的一套工程答案。`

### 5. LeanCLR 的角色

LeanCLR 也不该只是“另一个产品”。

它最有价值的地方在于：

`它提供了一个极其清晰的对照实验：如果完全不复用 IL2CPP / CoreCLR / Mono，而是从零重新实现 CLR，哪些部分是最小必需，哪些部分是工程增强项。`

---

## 十、正式开写前的阻塞项

这部分不是建议，而是开写前必须先处理的检查表。

### P0：必须先处理

1. **修复前置入口文章的 encoding / 内容状态**
- `pattern-prerequisites-01-type-instance-static.md`
- `pattern-prerequisites-02-interface-abstract-virtual.md`
- `pattern-prerequisites-05-const-readonly-immutability.md`

2. **抽查其余入口文与 runtime 总索引的编码状态**
- `pattern-prerequisites-03-inheritance-composition-dependency.md`
- `pattern-prerequisites-04-delegate-callback-event.md`
- `dotnet-runtime-ecosystem-series-index.md`

3. **确认本计划中的 18 篇权威清单与批次清单始终一致**
- 后续任何删改题目，都必须同时改第五节和第七节

### P1：建议先处理

4. **补齐前置文到 runtime 的尾部桥接小节**
5. **给 runtime 总索引新增“从 C# 概念进入”的入口分流**
6. **给关键模式文章补“读这篇之前”卡片**

---

## 十一、开写前希望 Claude 审核什么

为了避免这条系列写着写着又变成“新的 runtime 大百科”，建议在正式开写前让 Claude 重点审核下面四件事：

1. **定位是否准确**
- 这条系列是不是“入口主线”，而不是与 `ECMA-335 / CoreCLR / Mono / IL2CPP / HybridCLR / LeanCLR` 重叠的新系列

2. **已有内容处理是否合理**
- 哪些现有文章应该直接复用
- 哪些旧文应该补桥而不是重写
- 哪些前置文必须先修稳再挂链接

3. **新增文章清单是否必要**
- 18 篇是不是过多或过少
- 首批 6 篇是不是最值当的入口批次

4. **与 Mono / HybridCLR / LeanCLR 的关系是否讲清楚**
- 它们是不是被当成了“分叉答案”，而不是零散案例

---

## 十二、最终判断

这条新系列值得做，而且应该做。

但正确姿势不是：

`再开一套新的 runtime 大百科。`

正确姿势是：

`用一条“从 C# 到 CLR”的入口主线，把已经存在的 ECMA-335、CoreCLR、Mono、IL2CPP、HybridCLR、LeanCLR 这些深度内容接成一条可走的连续坡道。`

如果只做一句最终判断，我会这样写：

`你仓库里已经有一整片 runtime 山脉，现在最该补的不是更高的山，而是一条能让读者顺着 C# 概念走上去的山路。`
---

## 十三、配套执行文档

这份 `series-plan` 只回答“这条系列写什么、分哪几层、挂到哪些下游”。

真正开写时，还要配合下面三份文档一起使用：

- `docs/csharp-to-clr-codex-prompt.md`
  - 回答：写作时必须遵守哪些协议、哪些边界不能越、怎样交付
- `docs/csharp-to-clr-series-workflow.md`
  - 回答：一篇文章怎样从选题走到交付、多 agent 怎样并行协作
- `docs/csharp-to-clr-claude-audit-prompt.md`
  - 回答：把这套计划发给 Claude 做结构化审核时，应该按什么维度去审

一句话分工：

- `series-plan` 管选题地图
- `codex-prompt` 管写作协议
- `workflow` 管执行路线
- `claude-audit-prompt` 管外部审核入口

