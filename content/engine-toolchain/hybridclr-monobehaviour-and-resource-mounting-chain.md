---
date: "2026-03-20"
title: "HybridCLR MonoBehaviour 与资源挂载链路｜为什么资源上挂着热更脚本也能正确实例化"
description: "从 ScriptingAssemblies.json、placeholder assembly 到真实热更 DLL 覆盖 placeholder，拆解 HybridCLR 为什么能让资源上挂着的热更 MonoBehaviour 正确实例化。"
weight: 33
featured: false
tags:
  - "Unity"
  - "IL2CPP"
  - "HybridCLR"
  - "MonoBehaviour"
  - "Asset"
series: "HybridCLR"
hybridclr_version: "源码阅读 runtime v9.0.1 / package v9.0.3；实测范围逐节注明"
lastmod: "2026-10-25"
---

代码里能成功 AddComponent，并不证明 Prefab 上挂着的热更组件也能恢复。资源反序列化发生时，Unity 必须把保存的脚本身份解析到实际类型；这条链还包含构建时保留的程序集信息和 runtime 装载状态。

> **阅读基线**：本篇按 runtime v9.0.1（`83929787581bce6c7219418f62348a10c33582ee`）与 package v9.0.3 核对公开源码。实际 Player 使用本机自定义 Unity；环境与固定提交见 [源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}})。源码判断与实测范围分别标明。

这是系列第 4 篇，解释资源身份链。本文是源码分析，**尚未用公开实验仓库完成 Prefab/AssetBundle 的独立 Player 对照**；现有普通方法调用实验不能替代资源链验收。

## 代码路径与资源路径各需要什么

代码路径先拿到 `Type`，再调用 `AddComponent(type)`。资源路径从序列化脚本引用开始，要求程序集、类型与资源构建时记录的身份匹配。

Editor 的资源引用会涉及 GUID 与 local fileID。它们用于定位资产或资产内对象，**fileID 不是 CLI 程序集内的类型 token**；Player 构建还会转换资源引用。不能把 Editor YAML 中的 GUID/fileID 直接当作 runtime metadata 查找键。

阅读时把三件事分开：

| 身份 | 需要核对的内容 |
|---|---|
| 资源中的脚本引用 | 资源构建版本、脚本资产、程序集及类型名称 |
| 构建产物中的程序集登记 | 热更程序集名是否进入目标平台需要的清单 |
| runtime 中的程序集和类型 | 真实 DLL 是否已加载、类型是否可解析 |

## 构建期先区分 AOT 与热更

package 的 [FilterHotFixAssemblies.cs](https://github.com/focus-creative-games/hybridclr_unity/blob/6cadd24d101f4cbd00cd80190106f6621132d24d/Editor/BuildProcessors/FilterHotFixAssemblies.cs) 按热更配置过滤相关程序集，避免它们作为普通 AOT 代码进入主包编译链。热更 DLL 仍可作为数据文件随包分发；“不进 AOT 编译”不等于“主包里不能带 DLL 字节”。

过滤之后，资源系统仍需要这些程序集的身份。可沿 [PatchScriptingAssemblyList.cs](https://github.com/focus-creative-games/hybridclr_unity/blob/6cadd24d101f4cbd00cd80190106f6621132d24d/Editor/BuildProcessors/PatchScriptingAssemblyList.cs) 及相应 patcher 核对目标平台产物。具体载体与 Unity 分支有关，不能只在某个目录找到 `ScriptingAssemblies.json` 就认定所有平台都适用。

另一条构建输入是 generated `AssemblyManifest.cpp`。它记录预登记的热更程序集名称，供 runtime 建立 placeholder；名单应与 package 配置和资源使用的程序集保持一致。

## runtime 为什么需要 placeholder

固定版本 [Assembly.cpp](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/metadata/Assembly.cpp) 的 `InitializePlaceHolderAssemblies` 为配置中的名称创建并登记 placeholder。它先提供程序集身份，真实 metadata 内容要等 DLL 装载后补全。

实际装载时，`Assembly::Create` 按名称查找 placeholder。找到时复用已有程序集/image 外壳，未找到时走其他创建分支。这个对象复用关系，解释了构建期登记和运行时真实 DLL 如何接到同一身份上。

placeholder 并不意味着脚本已经可执行，也不允许把同一程序集的任意新版本反复覆盖。社区版的普通装载路径与商业热重载能力应分开判断。

## 怎样跟读这条链

按下列顺序观察，不必首先单步类型初始化的全部细节：

1. 记录资源构建身份、脚本程序集名称与热更 DLL hash。
2. 核对 AOT 编译过滤结果、generated manifest 与脚本程序集登记产物。
3. 观察 placeholder 创建时的程序集名及对象身份。
4. 加载真实 DLL，核对实际命中的 placeholder 或新建分支。
5. 确认目标组件类型已经可解析，再触发资源反序列化及实例化。

这样才能区分“资源指向错误身份”“DLL 没加载”“类型无法解析”和“组件生命周期执行失败”。单看 Inspector 的 Missing Script 或单次 AddComponent 成功，都不能唯一定位。

## 最小资源验证应怎么做

建议在同一固定 Player 中准备一个热更 MonoBehaviour，分别通过代码 AddComponent、Prefab 实例化、AssetBundle 加载触发。正向组先加载 DLL，再使用资源；负向组有意推迟装载并记录实际失败位置。每组都要新进程，防止前一组已登记的程序集遮住问题。

观测组件类型、字段数据、Awake/OnEnable 等实际调用，而不只看对象是否创建。若资源热更改变字段或程序集身份，还需独立验证序列化兼容性。

<!-- DATA-TODO: 在 hybridclr-runtime-lab 增加固定 DLL/Prefab/AssetBundle 的资源身份实验，保留构建 manifest、装载顺序与生命周期日志。本文当前仅完成源码核对。 -->

## 系列位置

构建输入见 [工具链篇]({{< relref "engine-toolchain/hybridclr-toolchain-what-generate-buttons-do.md" >}})，方法执行见 [调用链实战]({{< relref "engine-toolchain/hybridclr-call-chain-follow-a-hotfix-method.md" >}})，整体取舍见 [边界篇]({{< relref "engine-toolchain/hybridclr-boundaries-and-tradeoffs.md" >}})。

**资源上挂的热更组件能恢复，需要资源身份、程序集登记与真实类型装载在同一时刻对得上。**
