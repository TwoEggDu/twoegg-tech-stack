---
title: "HybridCLR 源码阅读起步｜版本、调试工程与第一条调用链"
slug: "hybridclr-source-reading-start"
date: "2026-10-25"
description: "先对齐源码、构建产物与运行进程，再用一个热更方法的两次调用，跟清装载、转换、缓存和解释执行。"
series: "HybridCLR"
weight: 29
hybridclr_version: "v6.1.0 / 5567f367502d916c6742873d8ed2ab13d0f6900a"
tags: [Unity, HybridCLR, IL2CPP, SourceCode, Debug]
---

打开源码后，搜索到了 `Interpreter::Execute`，也下了断点，但运行项目时一次都没有停下来。此时最容易做的事是继续往调用者里找；更应该先确认的是：**当前进程是否真的使用了你正在看的那份 runtime。**

源码、构建产物和运行进程没有对齐，再详细的调用链也很难跟下来。这篇负责把它们接起来：先固定阅读基线，再准备调试工程，最后用同一个热更方法的两次调用，观察它如何从 DLL 变成可执行的解释器方法。

> **读这篇之前**：先理解 [IL2CPP 运行时地图]({{< relref "engine-toolchain/il2cpp-runtime-map-global-metadata-gameassembly-libil2cpp.md" >}})。不熟悉 token 和方法体时，补读 [DLL 与 CLI Metadata]({{< relref "engine-toolchain/hybridclr-pre-cli-metadata-typedef-methoddef-token-stream.md" >}})；完整前置见 [ECMA-335 索引]({{< relref "engine-toolchain/ecma335-series-index.md" >}})。

> **证据范围**：本文已核对下列固定 commit 的公开源码。调试步骤和实验表是操作方案与预期观察，尚未完成 Unity Player 断点验证；不把源码推导当作设备实测。

## 先建立三份地图

| 地图 | 要回答的问题 | 第一轮需要留下的证据 |
|---|---|---|
| 源码 | 哪套 package、HybridCLR、配套 IL2CPP 实现？ | 版本映射、commit、本地改动 |
| 构建产物 | 这次 Player 实际编译了哪份 runtime？ | 构建日志中的源路径、生成物、原生二进制与匹配符号 |
| 运行进程 | 调试器附加的是哪个程序？ | 进程名、加载模块路径、符号加载状态、目标方法调用栈 |

`GenerateAll` 成功属于构建准备；Player 能启动属于运行入口；`Add` 的目标断点命中并返回正确结果，才是这次跟读的验证终点。三者需要分别确认。

## 固定源码版本，而不是跟着 main 漂移

为衔接本系列已有的 v6.x 源码文章，本篇选择 **v6.1.0 作为历史阅读样本**。它不是升级建议，也不代表你当前项目必须切换到这个版本。

| 仓库 | 固定基线 | 用途 |
|---|---|---|
| `hybridclr_unity` | v6.1.0，`88e20505f87d57cbff5695be5f7baa35d626f619` | C# API、安装与构建工具 |
| `hybridclr` | v6.1.0，`5567f367502d916c6742873d8ed2ab13d0f6900a` | metadata、transform、interpreter |
| `il2cpp_plus` | v2022-6.1.0，`ce2e34097e099e458e14192bed96c28bc761392b` | 该 package 对 Unity 2022 配置的配套 runtime 分支 |

上述配套关系来自 package 的 [hybridclr_version.json](https://github.com/focus-creative-games/hybridclr_unity/blob/88e20505f87d57cbff5695be5f7baa35d626f619/Data%7E/hybridclr_version.json)。**同一个 package 对不同 Unity 大版本可能配置不同 runtime tag**，不要只看 package 版本便假定三者一致。

下面命令用于建立独立的源码阅读副本，不会把代码安装进 Unity 工程。已有项目按自己的锁定版本执行对应操作：

```powershell
git clone --branch v6.1.0 --depth 1 https://github.com/focus-creative-games/hybridclr.git hybridclr-reading
git -C hybridclr-reading rev-parse HEAD
git -C hybridclr-reading status --short

git clone --branch v6.1.0 --depth 1 https://github.com/focus-creative-games/hybridclr_unity.git hybridclr-package-reading
git -C hybridclr-package-reading rev-parse HEAD
Get-Content hybridclr-package-reading/Data~/hybridclr_version.json
```

如果要实际运行，再填一份实验记录。**本文没有验证某个 Unity 2022.3 patch 与以上组合的运行结果**；应先按所选 package 的兼容说明确认，再记录完整 patch 号：

```text
Unity 完整版本：待填写（不能只写 2022.3）
工程 commit / 本地修改：待填写
package 版本、来源与 commit：待填写
实际安装的 hybridclr / il2cpp_plus 版本与本地修改：待填写
目标：Windows Standalone x86_64，IL2CPP
Development / C++ Compiler Configuration / stripping / defines：待填写
热更 DLL 与补充 AOT DLL 的构建来源、SHA256：待填写
Player 构建编号、原生二进制和 PDB 路径：待填写
```

在 [SettingsUtil.cs](https://github.com/focus-creative-games/hybridclr_unity/blob/88e20505f87d57cbff5695be5f7baa35d626f619/Editor/SettingsUtil.cs) 中可以找到该版本的本地 runtime 路径规则：`HybridCLRData/LocalIl2CppData-{Application.platform}/il2cpp`。安装结果可能是复制出来的目录，没有 `.git`；此时保留安装记录、版本文件和源码差异，不要把阅读副本的 SHA 当成实际构建证明。还要在构建日志中确认编译输入确实来自这份目录。

## 第一轮只读这几个入口

先读谁，取决于当前要解释哪个状态变化。下面的链接全部固定到本文基线。

| 次序 | 入口 | 带着什么问题读 |
|---|---|---|
| 1 | package 的 [PrebuildCommand.GenerateAll](https://github.com/focus-creative-games/hybridclr_unity/blob/88e20505f87d57cbff5695be5f7baa35d626f619/Editor/Commands/PrebuildCommand.cs) | 热更 DLL、裁剪后 AOT DLL、bridge 的生成为什么有依赖顺序？ |
| 2 | [metadata/Assembly.cpp](https://github.com/focus-creative-games/hybridclr/blob/5567f367502d916c6742873d8ed2ab13d0f6900a/hybridclr/metadata/Assembly.cpp) | 字节如何进入 `InterpreterImage` 并关联程序集？ |
| 3 | [interpreter/InterpreterModule.cpp](https://github.com/focus-creative-games/hybridclr/blob/5567f367502d916c6742873d8ed2ab13d0f6900a/hybridclr/interpreter/InterpreterModule.cpp) | 反射调用怎样找到解释器 invoker？方法体从哪里取？ |
| 4 | [transform/Transform.cpp](https://github.com/focus-creative-games/hybridclr/blob/5567f367502d916c6742873d8ed2ab13d0f6900a/hybridclr/transform/Transform.cpp) | 输入 CIL 怎样变成内部寄存器指令？ |
| 5 | [interpreter/Interpreter_Execute.cpp](https://github.com/focus-creative-games/hybridclr/blob/5567f367502d916c6742873d8ed2ab13d0f6900a/hybridclr/interpreter/Interpreter_Execute.cpp) | 指令指针、参数槽和返回值怎样推动执行？ |

第一次不必读完整个 switch。先让一个加法方法走通，再选与它相关的指令分支。metadata 表细节、泛型 inflation 和异常展开都可以在第二轮读。

`RuntimeApi.LoadMetadataForAOTAssembly` 属于另一条入口。它处理补充 AOT 元数据，不是加载热更 DLL 的替代调用。需要研究它时再读 [RuntimeApi.cs](https://github.com/focus-creative-games/hybridclr_unity/blob/88e20505f87d57cbff5695be5f7baa35d626f619/Runtime/RuntimeApi.cs) 和 [AOT 泛型与补充元数据]({{< relref "engine-toolchain/hybridclr-aot-generics-and-supplementary-metadata.md" >}})。

## 准备能停在 C++ 源码里的 Player

第一轮选 Windows Standalone，可以先避开设备连接和移动端符号归档的问题。操作前准备 Unity 对应的 Windows IL2CPP 支持模块，以及可构建该版本项目的 Visual Studio C++ 工具链。

1. 在已按所选版本接入 HybridCLR 的实验工程中，切到 Windows x86_64，脚本后端选择 IL2CPP。
2. 将 `C++ Compiler Configuration` 设为 `Debug`，构建设置启用 `Create Visual Studio Solution`。Development 标志在 Generate 与最终构建间保持一致，并记录下来。
3. 按目标平台运行生成流程，再构建导出工程。检查日志中的 runtime 路径和生成物来源，不复用另一平台或另一构建配置的产物。
4. 打开导出的解决方案，以对应架构和 Debug 配置构建。通过原生调试器启动 Player，或在触发实验前附加到它。
5. 在调试器的模块窗口确认实际加载的 `GameAssembly.dll` 路径和匹配的 PDB，再检查源码断点是否已经绑定。

这些设置依据 [HybridCLR 官方源码调试说明](https://www.hybridclr.cn/docs/basic/sourceinspect) 和 [Unity 2022.3 Windows 构建设置](https://docs.unity3d.com/cn/2022.3/Manual/WindowsStandaloneBinaries.html)。前者查阅时为 9.x 文档，界面与平台步骤应回到自己的 Unity 版本核对；本文函数位置以固定 commit 为准。

**Editor 的 Play 按钮不能代替这个实验。** 在 Editor 中执行 C# 并不证明进入了 HybridCLR 的 native 解释器。`Script Debugging` 也不能代替匹配的原生调试符号；这里跟的是 runtime C++ 的控制流。

Android 和 iOS 可在主链走通后再做。仍按相同原则确认实际模块、架构、符号与源码映射，不能用 Windows 的断点结果替代移动设备验证。

## 用一个方法做两次调用

准备两个程序集：AOT 入口负责读 DLL 和反射调用；`HotUpdate` 负责下面的方法。`HotUpdate` 要在 HybridCLR 配置中被识别为热更程序集，不能又作为 AOT 代码进入主包。AOT 入口不直接引用它的类型。

热更程序集中的 `ReadingProbe.cs`：

```csharp
namespace HotUpdate
{
    public static class ReadingProbe
    {
        public static int Add(int a, int b) => a + b;
    }
}
```

AOT 程序集中的调用入口：

```csharp
using System;
using System.Reflection;
using UnityEngine;

public static class SourceReadingProbe
{
    // dllBytes 必须是本次按目标平台编译的 HotUpdate.dll。
    public static void Run(byte[] dllBytes)
    {
        var assembly = Assembly.Load(dllBytes);
        var type = assembly.GetType("HotUpdate.ReadingProbe", true);
        var method = type.GetMethod("Add", BindingFlags.Public | BindingFlags.Static);
        if (method == null)
            throw new MissingMethodException(type.FullName, "Add");

        var first = (int)method.Invoke(null, new object[] { 2, 3 });
        var second = (int)method.Invoke(null, new object[] { 7, 8 });
        Debug.Log($"ReadingProbe: {first}, {second}");
    }
}
```

这是实验入口片段，不是完整的资源加载器。可以接在现有 Launcher 的字节加载完成处，或者在独立 Windows 实验场景中用 `File.ReadAllBytes` 读取明确路径后调用它。**一个 Player 进程只触发一次 Run**；比较的是同一方法对象的两次调用，不是重复装载同名程序集。

前置条件：完成所选版本要求的安装、热更程序集配置与生成；若工程需要补充 AOT 元数据，按既有加载流程处理并检查返回码。本例没有业务泛型，不需要为了制造现象而人为遗漏这些准备步骤。

开始前确认当前进程尚未调用或预热 `ReadingProbe.Add`。本例没有显式静态构造和模块初始化逻辑，目的是减少初次观察中的干扰。预期日志为 `ReadingProbe: 5, 15`，但日志正确仍不能单独证明解释器路径成立。

## 断点要观察什么

这些是本文 v6.1.0 源码中核对过的入口；行号仅帮助第一次定位，换版本后应重新搜索符号。

| 阶段 | 断点 | 观察重点 | 能说明什么 |
|---|---|---|---|
| 装载 | `Assembly::LoadFromBytes` / `Assembly::Create`，Assembly.cpp:97 / 107 | 输入长度、`image`、`ass->aname.name`；在相应对象创建之后检查 | 本次字节进入解释程序集的装载路径 |
| 反射调用 | `InterpreterInvoke`，InterpreterModule.cpp:373（Unity 2021+ 分支） | `method->name`、`method->klass`、`method->interpData` | 当前方法被接到解释器 invoker |
| 方法准备 | `InterpreterModule::GetInterpMethodInfo`，同文件:588 | `methodInfo`、image 来源、token、`methodBody->ilcodes` | 在正确的 image 中取得目标方法体 |
| 转换 | `HiTransform::Transform`，Transform.cpp:150 | 输入 body；返回后结果的 `codes`、`codeLength`、参数槽信息 | CIL 已转换成内部表示 |
| 执行 | `Interpreter::Execute`，Interpreter_Execute.cpp:1645 | `methodInfo`、参数槽；完成建帧后再观察 `ip` 和 `imi` | 目标方法进入内部指令执行 |

`Execute` 会被许多方法命中。先核对命名空间、类名、方法名；找到目标 `MethodInfo*` 后，可用调试器条件限制为该地址。不要把启动阶段其他方法的命中当作本例证据，也不要在循环里长期单步所有指令。

### 首次调用与再次调用的差别

在本基线的 [GetInterpMethodInfo 实现](https://github.com/focus-creative-games/hybridclr/blob/5567f367502d916c6742873d8ed2ab13d0f6900a/hybridclr/interpreter/InterpreterModule.cpp#L588) 中，先检查 `interpData`；未准备时从 image 获取方法体，调用 Transform，再把结果关联到该方法。反射 invoker 也会直接检查这个缓存。

| 观察点 | 第一次调用的预期 | 同进程第二次调用的预期 |
|---|---|---|
| invoker 入口的 `interpData` | 未预热条件下为空 | 已存在 |
| 目标方法的 Transform | 需要执行 | 复用结果，不再为同一方法转换 |
| 目标方法的 Execute | 执行，参数为 2、3 | 仍需执行，参数为 7、8 |
| 返回值 | 5 | 15 |

这说明的是**转换结果复用**，不是第二次变成 native JIT。如果首调已有缓存，先找预热、先前调用或其他触发路径。如果第二次再次转换，先核对是否同一进程、同一方法对象和同一泛型实例，不要直接判定缓存失效。

系列旧文有些地方使用 `MethodBodyCache` 概括方法体缓存路径。本文固定的 v6.1.0 入口实际调用 `image->GetMethodBody`，转换结果缓存于 `interpData`；跟读时以当前源码为准。方法体读取缓存与转换结果缓存也不是同一个概念。

## 断点不命中，按证据顺序排

| 现象 | 先检查 | 再做什么 |
|---|---|---|
| 所有 native 断点都不命中 | 是否只运行了 Editor；是否附加到正确 Player | 确认进程与脚本后端 |
| 断点显示未绑定 | 模块是否加载、PDB 是否匹配、源路径是否对应 | 先修正符号和源码映射 |
| 装载断点命中，目标 Execute 不命中 | 是否实际调用 Add、是否更早抛异常、程序集是否误进 AOT | 看返回异常与调用栈，确认方法身份 |
| Execute 命中，Transform 不命中 | `interpData` 是否已有值、是否提前预热 | 新进程中重做首次调用对照 |
| 源码能打开但行号与行为不符 | 实际编译的 LocalIl2CppData 与阅读副本是否一致 | 核对构建输入、本地改动和符号 |
| 参数看不到或显示已优化 | native 配置和变量作用域 | 在 Debug 构建及赋值之后观察 |

加载异常先留完整异常类型与内部异常，再进入 [按层排障手册]({{< relref "engine-toolchain/hybridclr-troubleshooting-diagnose-by-layer.md" >}})。不要同时修改版本、加载顺序和补 metadata 配置，否则很难解释是哪一步改变了结果。

## 读到哪里算完成第一轮

留下一份小记录即可：版本与构建身份、DLL 哈希、目标方法的调用栈、首次／再次的缓存状态，以及两个返回值。断点截图要带上方法身份，单张 `Execute` 截图不够。

<!-- DATA-TODO: 在固定 Unity patch 的 Windows IL2CPP Player 中执行本例，补版本记录、两次目标调用的断点截图和日志；当前仅核对公开源码。 -->

完成后按问题继续读：

- 想解释每个模块为何这样分工：[原理拆解]({{< relref "engine-toolchain/hybridclr-principle-from-runtimeapi-to-interpreter-execute.md" >}})。
- 想追 Unity 侧的反射与方法分派入口：[调用链实战]({{< relref "engine-toolchain/hybridclr-call-chain-follow-a-hotfix-method.md" >}})，注意其版本说明与本文固定基线的区别。
- 想解释生成物、bridge 或 AOT 泛型失败：[工具链]({{< relref "engine-toolchain/hybridclr-toolchain-what-generate-buttons-do.md" >}}) → [ABI 桥接]({{< relref "engine-toolchain/hybridclr-bridge-abi-cross-boundary-calling-convention.md" >}}) → [AOT 泛型与补充元数据]({{< relref "engine-toolchain/hybridclr-aot-generics-and-supplementary-metadata.md" >}})。
- 想找完整阅读路径：[HybridCLR 索引]({{< relref "engine-toolchain/hybridclr-series-index.md" >}})。

**源码阅读的第一步，是证明当前 Player 正在执行你手里的那份代码，再解释它为什么走到这里。**
