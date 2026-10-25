---
title: "HybridCLR AOT 泛型回归防线｜怎么把这些坑前移到 Generate、CI 和构建检查里"
date: "2026-03-31"
description: "定义构建输入、泛型清单、保留代码与 Player 检查的证据和失败条件，避免将文件存在或日志关键词误作验收。"
weight: 53
tags:
  - "HybridCLR"
  - "IL2CPP"
  - "AOT"
  - "CI"
  - "GenerateAll"
  - "DisStripCode"
series: "HybridCLR"
hybridclr_version: "源码阅读 runtime v9.0.1 / package v9.0.3；实测范围逐节注明"
lastmod: "2026-10-25"
---

回归检查的目标，是在发布前发现输入错配与未覆盖的新调用，而不是证明一个生成文件“存在”。扫描成功、构建成功和目标路径已在 Player 运行，是三个不同的结果。

本文承接 [AOT 泛型工作流]({{< relref "engine-toolchain/hybridclr-aotgenericreferences-disstripcode-metadata-how-to-work-together.md" >}})。Generate 与 Development 的统一入口见 [CI 集成篇]({{< relref "engine-toolchain/hybridclr-ci-pipeline-generate-all-and-development-flag.md" >}})，这里集中定义应检查的事实和失败条件。

> **阅读基线**：本篇按 runtime v9.0.1（`83929787581bce6c7219418f62348a10c33582ee`）与 package v9.0.3 核对公开源码。实际 Player 使用本机自定义 Unity；环境与固定提交见 [源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}})。源码判断与实测范围分别标明。

## 输入一致性：比较值，不能只搜索单词

一次构建请求应记录 Unity 身份、package/runtime 提交、目标平台、架构、Development、编译符号、裁剪配置及热更程序集列表。生成阶段与最终 Player 阶段必须使用同一份请求，或明确记录差异及理由。

在日志里搜索到 `DEVELOPMENT_BUILD`，不能证明两个阶段取值相同；文件时间较新，也不能证明它来自本次输入。检查应比较实际值、生成回执和内容哈希。

| 检查 | 可接受证据 | 应阻断的情况 |
|---|---|---|
| 版本配套 | Editor 身份、package/runtime/il2cpp_plus 固定版本 | 版本未知或不兼容 |
| 构建模式 | 生成与 Player 两阶段的显式配置回执 | 目标、Development 或符号意外不同 |
| 生成产物来源 | 输入指纹、输出哈希与本次构建 ID | 复用了无法确认来源的旧产物 |
| 热更排除 | 最终主包裁剪输入／产物的程序集清单 | 热更程序集意外成为主包 AOT 内容 |
| metadata 绑定 | 对应已发布主包保存的 DLL 及哈希 | 以新热更构建的 DLL 替换旧主包补充数据 |

主包更新时重新形成产物集；只发布热更时，沿用的是对应已发布主包的补充数据身份。两者不能混为“每次 CI 都取目录里最新文件”。

## 泛型清单比较：保留注释里的有效信息

`AOTGenericReferences.cs` 的泛型引用主要以注释呈现。先把所有 `//` 行删除再做差异比较，会恰好丢掉需要审查的信息。

若自动分析，解析器必须与生成器版本绑定，提取程序集列表及规范化方法签名，保留解析失败状态。生成格式变化时应报错或降级人工审查，不能把“没有解析到条目”当作“没有新增需求”。

清单差异的用途是要求解释：新增签名由现有 native 共享、补充 metadata，还是新主包显式实例支持。差异本身不等于发布失败，也不能自动要求给所有签名生成保留代码。

## 保留代码检查：进入产物之后再判断

源目录中有 `DisStripCode.cs`，或者该文件编译通过，都不能证明目标 native 方法存在。验证顺序是：

1. 检查 helper 位于本次主包的 AOT 程序集，并保留了实际方法调用。
2. 检查最终裁剪后的 DLL 中有对应定义与调用。
3. 如验收目标是 native 执行，再检查生成实现／注册信息以及 Player 实际路径。
4. 如果接受补充元数据解释执行，则记录功能和性能预算，不将“没有 native”自动判为缺陷。

解析生成 C++ 时只把它作为本地验证输入；本公开仓库不复制受限 Unity C++ 函数体。公开记录可保留符号名、文件位置、哈希与观察结论。

## Player 检查：测试必须到达目标调用

最小用例应包含输入、目标成员、期望结果及失败条件。对 async 要真正挂起并恢复，对 LINQ 要枚举，对 Dictionary 要执行指定成员。只加载 DLL、反射找到类型或看到一行启动日志均不算目标调用通过。

现有 [泛型综合实验]({{< relref "engine-toolchain/runtime-generic-call-experiment.md" >}}) 已用独立 Player 对照 AOT 实例缺失、补充 metadata 与显式实例。它可以作为组织证据的样板，但不能自动覆盖另一个泛型签名、另一个编译模式或移动设备。

结果记录至少包含：

- 构建 ID、源码提交、主包／热更／补充 DLL 哈希。
- 用例名称、实际触发的完整方法身份与输入。
- 返回值、异常或退出码；原始日志位置。
- 若判断执行路径，附匹配符号和目标断点证据。
- 未覆盖项，以及超时、未触发断点、缺少日志等异常状态。

没有执行的用例应写“未运行”，断点没命中且目标未执行应写“未触达”，不能统一记为通过。

## 自动化先证明自己会失败

上线检查工具前，为它准备受控的反例：错一个输入哈希、移走一份生成回执、改变 Development、提供解析器不认识的格式，确认相应检查确实拒绝结果。再用已知可用产物验证正向路径。

这些是检查器自身的验证，不等于 Unity Player 验收。本篇不提供一份靠搜索文件名就输出 PASS 的 CI 脚本；实际实现应与项目的构建请求、产物目录和测试执行器绑定。

回到 [系列索引]({{< relref "engine-toolchain/hybridclr-series-index.md" >}}) 可按问题选择已有正文；遇到功能边界与商业能力选型，再进入对应选读文章。
