---
title: "HybridCLR 打包工程化｜GenerateAll 必须进 CI 流程，Development 一致性与 Launcher-only 场景"
date: "2026-03-26"
series: "HybridCLR"
hybridclr_version: "源码阅读 runtime v9.0.1 / package v9.0.3；实测范围逐节注明"
tags:
  - "CI/CD"
  - "Android"
  - "IL2CPP"
  - "Build Pipeline"
weight: 45
lastmod: "2026-10-25"
---

CI 如果沿用上次开发机留下的配置，生成命令与最终 Player 可能不是同一组输入。工程化要解决的是参数和产物可追踪，而不是把 GenerateAll 的代码复制一遍。

> **阅读基线**：本篇按 runtime v9.0.1（`83929787581bce6c7219418f62348a10c33582ee`）与 package v9.0.3 核对公开源码。实际 Player 使用本机自定义 Unity；环境与固定提交见 [源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}})。源码判断与实测范围分别标明。

这是系列第 16 篇，负责构建入口的组织方式；输出验收集中在 [回归防线]({{< relref "engine-toolchain/hybridclr-aot-generic-guardrails-generate-ci-build-checks.md" >}})。

## 一份构建请求驱动两段流程

生成链和最终 Player Build 应来自同一份显式请求，包括 target、development、宏、裁剪等级、场景、脚本后端与版本组合。

仅在最终 `BuildOptions` 中设置 Development，不一定同步了生成器读取的 `EditorUserBuildSettings.development`。构建入口要按固定 package 的实际读取位置写入所需状态，并在日志和 manifest 中记录最终值。

这说明两段流程需要对齐，不意味着 flag 不同必然导致 metadata 失效或 async 无限递归。是否不兼容仍要靠 DLL 比较、模式、加载返回码和 Player 复现判断。

## 优先调用配套官方总入口

v9.0.3 的 [PrebuildCommand.GenerateAll](https://github.com/focus-creative-games/hybridclr_unity/blob/6cadd24d101f4cbd00cd80190106f6621132d24d/Editor/Commands/PrebuildCommand.cs) 管理必要顺序，包含空 bridge/wrapper 占位、中间 AOT 构建和正式生成。不要照搬旧文省略步骤的手写链。

构建包装层可以按以下**流程设计**组织，具体 API 与项目回调仍需在自己的环境验证：

1. 校验构建请求、活动平台和配套安装目录。
2. 保存会修改的 Editor 状态。
3. 设置生成器实际读取的参数和目标场景。
4. 调用配套 GenerateAll，检查异常和产物身份。
5. 按同一请求构建最终 Player。
6. 归档最终裁剪 DLL、生成物、符号与输入 manifest。
7. 在 finally 中恢复临时 Editor 状态。

这不是已测试的通用 CI 脚本，尤其需要处理平台切换、domain reload、构建回调和失败归档。

## Development 会影响哪些输入

Development 除了调试相关设置，还可能通过条件编译与编译器选项影响 IL；async 状态机的 class/struct 形态是需要检查的例子。因此不要写成“主要只影响符号”。

需要对照时，比较热更 DLL、裁剪 AOT DLL、相关方法签名/IL 与最终 native 生成物，而不只比较文件名。源码同一提交也不等于二进制输入相同。

## 场景集合如何选择

中间构建应与最终主包的场景策略一致。**只有最终主包确实只含 Launcher 时**，才适合把中间场景集也缩到 Launcher。对多场景主包一律套用 Launcher-only，反而会改变依赖根和裁剪结果。

保存并恢复场景列表能避免污染 Editor，但本身不证明中间与最终裁剪 DLL 字节完全相同；最终产物仍要单独归档。速度收益取决于工程，未测量前不写“大幅下降”。

## 发版后怎样选择补充 DLL

把补充 DLL 与**消费它的主包版本**绑定。Consistent 要精确匹配目标裁剪产物；SuperSet 按适配超集要求处理。不同平台或新主包产物不能无条件复用给旧 Player。

热更发布通常重新编译热更 DLL、更新需求分析与资源，但不应因此覆盖已上线主包对应的补充基线。[官方 AOT 泛型文档](https://www.hybridclr.cn/docs/basic/aotgeneric)特别要求保存打包时的 AOT DLL。

## 构建成功之后还要检查什么

生成进程成功、最终 Player 构建成功、文件被打进资源、实际加载成功、关键路径通过，是不同阶段的结果。每一阶段记录失败理由，不能用前一阶段的退出码替后面签字。

现有 [源码跟读实验]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}})提供了固定环境的例子；把同样的输入身份与运行证据方法应用到 CI，再根据平台增加 smoke case。

## 系列位置

产物职责回看 [工具链篇]({{< relref "engine-toolchain/hybridclr-toolchain-what-generate-buttons-do.md" >}})，历史 async 调查边界见 [续篇]({{< relref "engine-toolchain/hybridclr-case-async-crash-root-cause-and-two-fixes.md" >}})。

**同一请求驱动生成和构建，最终消费方验证产物是否配套。**
