---
title: "HybridCLR AOTGenericReferences、DisStripCode、补 metadata 到底怎么配合"
date: "2026-03-31"
description: "把泛型扫描清单、显式 AOT 引用和主包绑定的补充元数据接入发布流程，区分功能支持、native 优化与每阶段交付证据。"
weight: 51
tags:
  - "HybridCLR"
  - "IL2CPP"
  - "AOT"
  - "AOTGenericReferences"
  - "DisStripCode"
  - "LoadMetadataForAOTAssembly"
series: "HybridCLR"
hybridclr_version: "源码阅读 runtime v9.0.1 / package v9.0.3；实测范围逐节注明"
lastmod: "2026-10-25"
---

扫描清单、显式 AOT 引用和补充元数据会出现在同一条发布流水线里，但它们不是必须依次执行的三个修复步骤。项目可以按正常方案补充元数据，再只为有性能证据的热点维护 native 实例。

[修法决策]({{< relref "engine-toolchain/hybridclr-fix-decision-disstrip-linkxml-metadata.md" >}}) 已解释各自作用；这里把它们落实为主包与热更发布之间的交接。

> **阅读基线**：本篇按 runtime v9.0.1（`83929787581bce6c7219418f62348a10c33582ee`）与 package v9.0.3 核对公开源码。实际 Player 使用本机自定义 Unity；环境与固定提交见 [源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}})。源码判断与实测范围分别标明。

## 为每个产物明确生产者与消费者

| 产物 | 生产者 | 消费者与用途 |
|---|---|---|
| `AOTGenericReferences.cs` | 固定版本扫描器，读取对应构建输入 | 人工评审或版本绑定的分析工具，识别 AOT 泛型需求及补充程序集候选 |
| 显式 AOT 调用文件 | 工程维护者 | 主包 C# 编译、裁剪及 IL2CPP，影响 native 实例生成 |
| 主包裁剪后的 AOT DLL | 该主包的构建流水线 | runtime 补充元数据加载，以及发布身份校验 |
| 热更 DLL | 对应平台、模式和符号的热更构建 | runtime 程序集装载与业务执行 |
| Player 验收记录 | 自动化与调试取证 | 发布评审，确认实际功能和执行路径 |

生成清单中的注释不是可执行保留代码。人工文件不是生成器的固定输出，也不应让下一次 Generate 直接覆盖。补充 DLL 则是与主包绑定的发布输入，不能每轮热更时随便换成新构建出来的 framework DLL。

## 主包阶段：固定可复用的证据

主包构建统一目标平台、Development、编译符号、Unity 与 HybridCLR 配套版本，按 [Generate 工作流]({{< relref "engine-toolchain/hybridclr-toolchain-what-generate-buttons-do.md" >}}) 生成相关产物。

随后评审泛型清单：哪些调用可以通过已有共享实例覆盖，哪些需要 metadata，哪些因真实热点值得显式实例化。每个新增保留调用都应附目标成员和测试入口，不必把扫描结果全部手写一遍。

最终保存本次主包的裁剪 AOT DLL、哈希、生成清单、热更程序集身份及 Player 记录。元数据文件的“来源主包”是发布契约的一部分。

## 热更阶段：围绕已发布主包判断增量

热更增加容器、状态机或反射路径后，重新扫描得到的是**新需求**。先将它与已发布主包能力对照，再决定发布方案：

- 主包已有适用的 native 实例：执行新增业务路径验证。
- 缺少 native 实例，但已满足或可提供正确的补充元数据：验证解释路径与性能预算。
- 依赖被裁剪的 AOT 定义，或确实要求新的 native 实现：评估更新主包，不能只改热更 DLL 假装补齐。

这不是“发现新增签名就必须发主包”，也不是“metadata 加载返回成功就可以跳过所有运行验证”。身份、功能与性能是不同的验收项。

## 运行阶段：先检查输入，再交给业务入口

工程上可按“读取已发布主包身份 → 选择匹配补充数据 → 检查加载返回值 → 装载依赖与热更 DLL → 启动入口”组织日志。这个顺序方便避免首次调用抢跑；API 的硬约束是相关方法首次使用前准备就绪，补充程序集之间没有固定加载排序要求。

日志至少应关联主包 build ID、目标平台、热更 DLL 哈希、补充 DLL 哈希及加载结果。失败时应阻止依赖该输入的业务入口，并保留原始错误，不能只吞掉异常后继续进入场景。

## 把交接责任收成三份记录

构建方交付“主包与哪些补充 DLL 对应”；代码维护者交付“新签名通过哪条执行路径支持”；验证方交付“哪些 Player 用例实际执行过，哪些性能结论还没有数据”。

[泛型综合实验]({{< relref "engine-toolchain/runtime-generic-call-experiment.md" >}}) 提供固定提交的可复现样本。真实项目扩展范围时，用 [高频场景检查]({{< relref "engine-toolchain/hybridclr-aot-generic-pitfall-patterns-unitask-linq-dictionary-delegate-custom-container.md" >}}) 挑选用例，再把证据接到 [回归检查]({{< relref "engine-toolchain/hybridclr-aot-generic-guardrails-generate-ci-build-checks.md" >}})，无需在每篇重新解释同一套修法。
