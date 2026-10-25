---
slug: "hybridclr-dhe-内部机制dhao-文件格式差分算法与函数级分流实现"
title: "HybridCLR DHE 工作流｜DHAO、MetaVersion 与函数级分流的公开契约"
date: "2026-04-13"
description: "从主包快照、变化判定和运行时加载三层解释 DHE 的公开工作流，区分 DHAO 与 MetaVersion，并说明注入策略、平台和主包版本为什么必须配对。"
weight: 58
featured: false
tags:
  - "HybridCLR"
  - "IL2CPP"
  - "DHE"
  - "Architecture"
  - "Runtime"
series: "HybridCLR"
hybridclr_version: "商业公开文档 9.x，查阅于 2026-10-25"
lastmod: "2026-10-25"
---

> DHE 能否正确分流，首先取决于“这份热更究竟对应哪个主包”是否可追溯。

这是 HybridCLR 系列第 25 篇。[DHE 定位篇]({{< relref "engine-toolchain/hybridclr-dhe-why-not-just-faster-interpreter.md" >}})解释了为什么要让未变逻辑留在 AOT。这篇继续回答：当线上存在多个主包，构建和加载系统怎样把变化判定交给正确的运行时。

> 资料范围：商业行为依据 2026-10-25 查阅的 HybridCLR 官方 9.x 文档。本文没有商业源码或商业版 Player 实测，不描述 DHAO 的私有二进制布局，也不把社区版结构体字段当作 DHE 内部实现的证据。

## 先把工作流拆成三层

一个差分执行系统至少需要三组信息：

| 层次 | 输入与输出 | 需要保持的关系 |
|---|---|---|
| 主包基线 | 实际打包的程序集、注入规则与构建目标 | 快照描述的是这个主包中的 AOT 实现 |
| 变化判定 | 基线、最新程序集和配套工具产出的变化／版本数据 | 数据与最新 DLL 对应，能识别类型和方法 |
| 运行时装载 | 当前主包身份、最新 DLL 与对应变化／版本数据 | 在执行业务前建立正确的执行路由 |

“文件都存在”不等于三层一致。即使两个 DLL 的程序集名相同，也不能用另一次构建的快照代替线上主包基线。

## DHAO 工作流：把差异与主包绑定

[官方 DHAO 工作流](https://www.hybridclr.cn/docs/business/ultimate/dhaoworkflow)要求在导出工程或实际构建主包后创建 AOT Snapshot。不能把 `Generate/All` 的中间 DLL 当作最终快照。快照保存 DLL、注入规则和 manifest，随后以它和最新 DLL 计算对应的 DHAO 数据。

可以把这条链理解为：

```text
主包 A 的实际构建 → AOT Snapshot A
Snapshot A + 最新 DLL → 主包 A 对应的 DHAO
最新 DLL + DHAO → 客户端专用加载入口
```

如果主包 B 也在线，需要考虑 B 的独立基线。官方对 DHAO 的管理单位是“主包与平台”的组合；跨平台不能因为 C# 输入类似就复用产物。可阅读的 spec 适合进入版本管理和审阅，不是运行时必需资源。

工程上建议给每组发布材料保留主包标识、BuildTarget、工具包版本、DLL 和注入规则校验值。这样出现问题时，才能区分“变化判定本身有问题”与“客户端拿到了另一主包的判定结果”。

## 差分判定不是一般的程序等价证明

[官方 DHAO 文件说明](https://www.hybridclr.cn/docs/business/ultimate/dhao)说明工具通过比较 IL 判定变化。这不能推出“IL 字节完全相同就是全部判据”，更不能推出“语义相同就一定保持 AOT”。

例如，两个独立语句交换顺序后，行为可能相同，生成的 IL 却不同。反过来，方法的可执行性还可能受所依赖类型、方法和注入规则影响。仅检查一个方法体的字节，无法覆盖这些依赖。

metadata token 是当前模块的行标识，重新编译后可能变化。跨版本匹配需要识别对应的类型与成员，但公开文档没有公布 DHAO 完整匹配算法和序列化字段；本文不把一种可行设计写成产品格式。

### 手工标注的风险不对称

`UnchangedAttribute` 可以覆盖自动判定。把实际上没变的方法判为变化，通常增加解释执行成本；把确实改变的方法强行声明为未变化，则可能继续执行不适用的旧实现，影响正确性。

因此，这个标注应服务于可审查的例外，不能用来压低“变化方法数”指标。应保留为什么认为它可保持 AOT、依赖类型是否变化、覆盖了哪些用例的记录。

## MetaVersion：减少多主包差分文件的管理量

[官方 MetaVersion 工作流](https://www.hybridclr.cn/docs/business/ultimate/metaversionworkflow)从 v7.7.0 开始提供，并被当前文档推荐。它为类型和函数计算版本信息，运行时比较主包与最新程序集的版本数据。

变化的是发布管理方式：同一热更新 DLL 的版本文件可以用于对应的多个主包，避免每次为各主包单独生成 DHAO。没有消失的是主包基线。客户端仍需要当前主包对应的原始 meta version，构建侧仍需维护快照、注入规则、签名映射和版本计算关系。

尤其不要把“一个更新文件服务多个主包”理解成“任意旧主包都兼容”。平台、DHE 程序集集合及快照生成关系仍受工作流约束。

## 加载接口为什么必须跟工作流一致

[官方加载说明](https://www.hybridclr.cn/docs/business/ultimate/loadassembly)区分三个入口：

| 场景 | 入口 | 输入要点 |
|---|---|---|
| 明确使用原始实现 | `LoadOriginalDifferentialHybridAssembly` | 程序集名；还要满足原始加载的依赖要求 |
| DHAO 工作流 | `LoadDifferentialHybridAssemblyWithDHAO` | 最新 DLL、可选符号以及与其对应的 DHAO |
| MetaVersion 工作流 | `LoadDifferentialHybridAssemblyWithMetaVersion` | 最新 DLL、可选符号、当前主包原始版本数据与最新版本数据 |

表格描述输入关系，完整签名以所安装包为准。尤其 MetaVersion 不是“DLL + 任意一份版本文件”。

DHE 程序集需要额外的分流信息，不能直接按普通热更程序集使用 `Assembly.Load`。依赖也必须先装载；由于 DHE 会进入主包 AOT 构建，不能反向依赖普通纯热更程序集。

## 函数分流能确认到哪一层

公开资料能够确认：DHE 以变化信息选择原始 AOT 或最新解释执行，并通过函数入口注入减少脏函数向调用方传播。

不能据此确认它“只替换已有 MethodInfo 的 methodPointer”。新增方法没有对应的旧方法入口；直调、虚调、委托和反射也需要各自满足运行时契约。具体 metadata 对象是否复用、指针如何写入、采用什么表布局，都需要对应商业实现证据。

对于未注入的函数，后续修改**不会因此自动执行旧逻辑或失去热更能力**。工具会处理脏函数传播，代价可能是更多调用者进入解释执行。完整模型和配置取舍留在 [函数注入与脏函数传染]({{< relref "engine-toolchain/hybridclr-dhe-function-injection-dirty-contagion.md" >}})，本篇不再重复展开。

## 发布时应该验证什么

这篇没有给出商业版实验成功记录。项目接入时至少应覆盖以下路径：

1. 原始主包首次启动，以及显式装载后再进入 DHE 业务。
2. 方法体变化、新增方法、依赖类型变化与未注入方法变化。
3. 多个有效主包分别加载同一轮更新，并检查它们各自的执行结果。
4. 故意错配 DLL 与变化数据，确认发布系统会在运行前拒绝错误组合。
5. 对目标平台验证加载、资源挂载及 native 边界限制。

DHE 的可维护性建立在可追溯的主包基线与变化数据上，具体闭源路由实现不应靠字段名猜出来。

## 系列位置

- 选型前置：[DHE 为什么不是普通解释执行更快一点]({{< relref "engine-toolchain/hybridclr-dhe-why-not-just-faster-interpreter.md" >}})
- 下一篇：[函数注入与脏函数传染]({{< relref "engine-toolchain/hybridclr-dhe-function-injection-dirty-contagion.md" >}})
- 回到入口：[HybridCLR 系列索引]({{< relref "engine-toolchain/hybridclr-series-index.md" >}})
