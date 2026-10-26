---
title: "HybridCLR 源码阅读起步｜版本、调试工程与第一条调用链"
slug: "hybridclr-source-reading-start"
date: "2026-10-25"
description: "先对齐源码、构建产物与运行进程，再用一个热更方法的两次调用，跟清装载、转换、缓存和解释执行。"
series: "HybridCLR"
weight: 29
hybridclr_version: "源码与实验 runtime v9.0.1 / package v9.0.3；v6.1.0 历史对照"
tags: [Unity, HybridCLR, IL2CPP, SourceCode, Debug]
lastmod: "2026-10-26"
---

打开源码后，搜索到了 `Interpreter::Execute`，也下了断点，但运行项目时一次都没有停下来。此时最容易做的事是继续往调用者里找；更应该先确认的是：**当前进程是否真的使用了你正在看的那份 runtime。**

源码、构建产物和运行进程没有对齐，再详细的调用链也很难跟下来。这篇负责把它们接起来：先固定阅读基线，再准备调试工程，最后用同一个热更方法的两次调用，观察它如何从 DLL 变成可执行的解释器方法。

> **读这篇之前**：先理解 [IL2CPP 运行时地图]({{< relref "engine-toolchain/il2cpp-runtime-map-global-metadata-gameassembly-libil2cpp.md" >}})。不熟悉 token 和方法体时，补读 [DLL 与 CLI Metadata]({{< relref "engine-toolchain/hybridclr-pre-cli-metadata-typedef-methoddef-token-stream.md" >}})；完整前置见 [ECMA-335 索引]({{< relref "engine-toolchain/ecma335-series-index.md" >}})。

> **证据范围**：第一轮阅读与 Player 实验统一使用下文锁定的 v9 配套组合；v6.1.0 仅保留为历史对照。该样本已完成原生断点验证。Editor 是本机自定义版本，结果不代表其他 Unity 发行版、平台或 HybridCLR 版本的兼容性。

## 先建立三份地图

| 地图 | 要回答的问题 | 第一轮需要留下的证据 |
|---|---|---|
| 源码 | 哪套 package、HybridCLR、配套 IL2CPP 实现？ | 版本映射、commit、本地改动 |
| 构建产物 | 这次 Player 实际编译了哪份 runtime？ | 构建日志中的源路径、生成物、原生二进制与匹配符号 |
| 运行进程 | 调试器附加的是哪个程序？ | 进程名、加载模块路径、符号加载状态、目标方法调用栈 |

`GenerateAll` 成功属于构建准备；Player 能启动属于运行入口；`Add` 的目标断点命中并返回正确结果，才是这次跟读的验证终点。三者需要分别确认。

## 固定当前阅读与实验基线

第一轮按已验证的 v9 配套组合阅读，避免下载 v6 却在新版文章中寻找不存在的文件。版本锁用于重现本文样本，不是要求其他项目立即升级。

| 仓库 | 固定版本 | commit |
|---|---|---|
| hybridclr_unity | v9.0.3 | `6cadd24d101f4cbd00cd80190106f6621132d24d` |
| hybridclr | v9.0.1 | `83929787581bce6c7219418f62348a10c33582ee` |
| il2cpp_plus | v2022.3.x-9.0.1 | `ba3a76c9979d14cee404fb5d44301773e0768866` |

映射来自固定 package 的 [hybridclr_version.json](https://github.com/focus-creative-games/hybridclr_unity/blob/6cadd24d101f4cbd00cd80190106f6621132d24d/Data%7E/hybridclr_version.json)。package 和 runtime 版本不同是实际配套关系，不能把三者版本号强行设成相同。

下面只建立阅读副本。实际构建继续使用公开实验仓库的环境锁和准备脚本：

```powershell
git clone --branch v9.0.1 --depth 1 https://github.com/focus-creative-games/hybridclr.git hybridclr-reading
git -C hybridclr-reading rev-parse HEAD
git -C hybridclr-reading status --short

git clone --branch v9.0.3 --depth 1 https://github.com/focus-creative-games/hybridclr_unity.git hybridclr-package-reading
git -C hybridclr-package-reading rev-parse HEAD
Get-Content hybridclr-package-reading/Data~/hybridclr_version.json
```

将输出与上表核对。实际安装目录可能是复制结果，没有 `.git`；阅读副本的 SHA 不能代替编译输入证明。v9 的项目本地路径为 `Library/HybridCLR/LocalIl2CppData-WindowsEditor/il2cpp`，还要从构建日志核对实际使用位置。准备自己的实验时记录：

```text
Unity 完整版本与安装来源：待填写
工程 commit / 本地修改：待填写
package 与实际 runtime 的版本、commit、文件差异：待填写
目标平台 / 架构 / 脚本后端：待填写
Development / C++配置 / stripping / defines：待填写
热更 DLL 与补充 AOT DLL 的构建来源、SHA256：待填写
Player 构建编号、原生模块与匹配符号：待填写
```

### 本机隔离实验：明确自定义 Editor 的范围

2026-10-25 的实验使用 Unity Hub 手动登记的**本机自定义 Editor**，产品版本为 `2022.3.62f3_96770f904ca7`。目标为 Windows x86_64、IL2CPP、Development、C++ Debug，Script Debugging 关闭。工具环境为本机 Visual Studio Community 18、MSVC 14.51.36231 与 Windows SDK CDB；未覆盖 Editor 安装目录。

最初用 v6.1.0 尝试时，生成器与配套 runtime 的异常辅助接口不匹配；局部适配后虽然构建成功，Player 仍因缺少两个内存池导出停在脚本前。因此仅保留它作为历史阅读与失败记录的对照，另开短路径工程使用上表 v9 原始配套源码，不沿用兼容补丁。

本次使用上表的原始配套源码。840 个配套 libil2cpp 文件、79 个 HybridCLR 非生成文件与锁定源码逐文件哈希一致；桥接文件等按 GenerateAll 正常生成。

自有 C#、程序集配置和 CDB 命令现维护在公开仓库 [hybridclr-runtime-lab](https://github.com/TwoEggDu/hybridclr-runtime-lab)，本文使用 [固定提交 `e0220099`](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/e0220099d293f7a1a4601ee9931b49a10dd519f9)（完整 SHA：`e0220099d293f7a1a4601ee9931b49a10dd519f9`）。最小 Add 工程位于 [projects/call-chain](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/e0220099d293f7a1a4601ee9931b49a10dd519f9/projects/call-chain)，准备、构建和调试步骤见该提交的 [README](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/e0220099d293f7a1a4601ee9931b49a10dd519f9/README.md)。克隆后先 checkout 到这个提交，再执行准备与构建命令。

`tools/prepare.py` 将锁定依赖准备到实验仓库的 `.local/combined-libil2cpp`；`tools/lab.py build` 核对实际 Editor 身份，并构建三个隔离样本。构建器只写各自工程的场景列表、构建设置与本地 runtime。v9 的实际 runtime 路径是 `Library/HybridCLR/LocalIl2CppData-WindowsEditor/il2cpp`，不要套用历史 v6 的 `HybridCLRData` 路径。

从新仓库重新构建后，Add 的返回值、首次转换和再次复用均已复核；公开记录见 [call-chain.json](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/e0220099d293f7a1a4601ee9931b49a10dd519f9/evidence/2026-10-25/call-chain.json)，对应构建 `136f3c65-0c8b-4944-a838-39a5f4024e27`。该记录与历史 v6 记录分开保存。

这轮实际走到的边界如下：

| 验证层 | 实际结果 |
|---|---|
| Editor、热更 DLL、GenerateAll | 已完成 |
| 最终 IL2CPP 构建 | 新版原始配套源码成功构建，Editor 退出码 0 |
| 模块与符号 | CDB 加载本次 GameAssembly，显示完整 PDB 符号 |
| Player 脚本入口 | 装载 4608 字节 HotUpdate.dll，输出 PROBE_PASS |
| Add 两次执行与缓存 | 同一 MethodInfo，Transform 一次、Execute 两次，返回 5、15 |
| 脱离调试器运行 | 同一构建正常完成，进程退出码 0 |

这次证据支持该固定样本在本机自定义环境中的执行链，不扩展为 GC、线程、性能或移动端结论。详细的首次／再次观测值见 [调用链实战]({{< relref "engine-toolchain/hybridclr-call-chain-follow-a-hotfix-method.md" >}})。

## 第一轮只读这几个入口

下表统一使用 v9 配套提交。先问清每个入口负责哪个状态变化，再打开对应文件：

| 次序 | 入口 | 带着什么问题读 |
|---|---|---|
| 1 | package 的 [PrebuildCommand.GenerateAll](https://github.com/focus-creative-games/hybridclr_unity/blob/6cadd24d101f4cbd00cd80190106f6621132d24d/Editor/Commands/PrebuildCommand.cs) | 热更 DLL、裁剪后 AOT DLL 与 bridge 生成有什么依赖？ |
| 2 | [metadata/Assembly.cpp](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/metadata/Assembly.cpp) | 输入字节怎样建立解释程序集？ |
| 3 | [interpreter/InterpreterModule.cpp](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/interpreter/InterpreterModule.cpp) | 反射 invoker 怎样关联目标方法并取得解释信息？ |
| 4 | [metadata/MethodBodyCache.cpp](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/metadata/MethodBodyCache.cpp) 与 [transform/Transform.cpp](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/transform/Transform.cpp) | 从哪个 image 取得 IL 方法体，怎样转换成内部表示？ |
| 5 | [interpreter/Interpreter_Execute.cpp](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/interpreter/Interpreter_Execute.cpp) | 参数槽、指令和返回值怎样推动这次执行？ |

第一次只追加法方法相关分支。看不懂槽索引时先读[解释器基础]({{< relref "engine-toolchain/hybridclr-bridge-interpreter-basics-dispatch-stack-register-ir.md" >}})，不必通读全部 switch。

`RuntimeApi.LoadMetadataForAOTAssembly` 是补充 AOT 方法体等信息的另一条入口，不是加载热更 DLL 的替代调用。其用法继续看[AOT 泛型与补充元数据]({{< relref "engine-toolchain/hybridclr-aot-generics-and-supplementary-metadata.md" >}})。

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

下表是 **v9.0.1 的实际入口**。行号只用于定位固定提交，换版本后应重新按符号核对。

| 阶段 | 入口 | 观察什么 |
|---|---|---|
| 装载 | `Assembly::LoadFromBytes` / `Create`，Assembly.cpp:124 / 131 | 输入字节、image 与程序集身份 |
| 反射调用 | `InterpreterInvoke`，InterpreterModule.cpp:409 | 方法名、声明类型、`interpData` |
| 方法准备 | `GetInterpMethodInfo`，同文件:521 | 当前方法是否已有解释信息 |
| 转换 | `HiTransform::Transform`，Transform.cpp:34 | 目标 MethodInfo、内部读取的方法体与转换结果 |
| 执行 | `Interpreter::Execute`，Interpreter_Execute.cpp:1812 | 目标方法、参数槽、建帧后的指令位置与返回值 |

v9 的 Transform 接收 MethodInfo，并在内部经 MethodBodyCache 取得方法体。历史 v6 路径与此不同，见后面的版本对照以及[调用链篇]({{< relref "engine-toolchain/hybridclr-call-chain-follow-a-hotfix-method.md" >}})。

本轮采用 CDB 在创建进程时设置延迟符号断点，使用构建输出的 PDB 目录，再放行自动执行的样本。样本中的 `trace-v9.cdb` 记录模块、方法地址、缓存值、参数和栈帧。命令里的寄存器取参只适用于 Windows x64 和本例签名；断点命令用法见 [Microsoft 的 bp/bu/bm 文档](https://learn.microsoft.com/en-us/windows-hardware/drivers/debuggercmds/bp--bu--bm--set-breakpoint-)。

`Execute` 会被许多方法命中。先核对命名空间、类名、方法名；找到目标 `MethodInfo*` 后，可用调试器条件限制为该地址。不要把启动阶段其他方法的命中当作本例证据，也不要在循环里长期单步所有指令。

### 首次调用与再次调用的差别

先对照[调用链篇的首次／再次调用时序图]({{< relref "engine-toolchain/hybridclr-call-chain-follow-a-hotfix-method.md#first-repeat-call" >}})，再用下面的断点观察表逐项验证。图限定本文同一进程、同一静态 Add 方法的反射调用路径。

在本基线的 [GetInterpMethodInfo 实现](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/interpreter/InterpreterModule.cpp#L521) 中，先检查 `interpData`，未准备时调用 Transform 并关联结果。方法体读取在该转换链内部完成；反射 invoker 也会检查解释信息缓存。

| 观察点 | 第一次调用的预期 | 同进程第二次调用的预期 |
|---|---|---|
| invoker 入口的 `interpData` | 未预热条件下为空 | 已存在 |
| 目标方法的 Transform | 需要执行 | 复用结果，不再为同一方法转换 |
| 目标方法的 Execute | 执行，参数为 2、3 | 仍需执行，参数为 7、8 |
| 返回值 | 5 | 15 |

这说明的是**转换结果复用**，不是第二次变成 native JIT。如果首调已有缓存，先找预热、先前调用或其他触发路径。如果第二次再次转换，先核对是否同一进程、同一方法对象和同一泛型实例，不要直接判定缓存失效。

方法体读取缓存与已转换的解释信息是两层数据，不能把 `MaxMethodBodyCacheSize` 当成 `interpData` 的容量上限。

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

## 首次调用补证：初始化、转换和方法体分别观察 {#type-initialization-first-call}

早先 Add 首调实验只证明自身的转换与缓存行为，其入口没有显式 .cctor。2026-10-26 已增加独立 `projects/type-initialization`，沿用当前固定环境；同一份自有场景分别编成 AOT／热更代码，按显式初始化、beforefieldinit、泛型静态字段与失败初始化分组。详细结果集中在[类型初始化篇的 Player 实测]({{< relref "engine-toolchain/ecma335-type-loading-and-initialization.md#type-initialization-player" >}})，避免在每篇复述完整矩阵。

本轮 build_id `5b177115-e8ae-4e58-8808-2a0bc54b092a`，30 个普通进程与 2 个独立 native 取证进程。27 个普通进程完成；3 个热更内部 catch 对照出现初始化异常逃逸，未到第二次访问，单独记录为差异。共用 AOT 外层捕获的补充路径则在两侧都记录到两次异常、一次构造体执行。

| 证据 | 能确认什么 |
|---|---|
| 最终 AOT／hot DLL 的标志、.cctor CIL 和异常区间 | 输入确有目标语义；不是拿 Editor DLL 或源码直觉代替随包程序集 |
| 加载、解析、首次调用、初始化体、方法体与返回事件 | 分开记录类型可解析、静态状态建立和方法体实际执行 |
| .cctor 的匹配 PDB 调用栈 | 本轮实际经 Runtime::ClassInit 进入初始化器 |
| Ping 的 Transform 与结束时同一 MethodInfo 的 interpData | 确认目标已转换并缓存；仍须独立检查方法体是否执行 |

两个 native 对照都观察到 **Ping 先转换，随后执行 .cctor**。strict 的 Ping 方法体出现两次；failed 没有 Ping 方法体事件，退出前缓存却已非空。因此不能从“Transform 过一次／interpData 非空”反推“静态构造成功”或“目标方法执行过”。解释器内部调用也不一定再次进入原生 Execute 函数，函数入口计数与托管调用次数须分开。

复现入口是 `tools/init_lab.py`，本轮固定提交为 [46ac71c](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/46ac71c74cd068bc177d653dd143b7bb9dc482bd)，见[实验工程](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/46ac71c74cd068bc177d653dd143b7bb9dc482bd/projects/type-initialization)、[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/46ac71c74cd068bc177d653dd143b7bb9dc482bd/evidence/2026-10-26-type-initialization/README.md)与[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/46ac71c74cd068bc177d653dd143b7bb9dc482bd/evidence/2026-10-26-type-initialization/results.json)。`74da063` 仍为上一轮资源实验。异常捕获差异只在本机自定义 Editor／固定 v9／Windows x64 上复现，精确根因和其他平台仍待补。

其余训练按实际问题推进：constrained.／装箱需要对应 CIL 与对象身份，跨 AOT／解释器异常传播需要普通抛出与 finally 对照，并发／长期压力需要单独的调度、时长和停止协议。这些不会成为第一轮阅读的统一门槛。

## 历史 v6.1.0 对照

旧文章和失败的环境适配记录仍保留。需要复核它们时使用下表，不把旧路径拼进 v9 调用链：

| 仓库 | 历史版本与 commit |
|---|---|
| hybridclr_unity | v6.1.0，`88e20505f87d57cbff5695be5f7baa35d626f619` |
| hybridclr | v6.1.0，`5567f367502d916c6742873d8ed2ab13d0f6900a` |
| il2cpp_plus | v2022-6.1.0，`ce2e34097e099e458e14192bed96c28bc761392b` |

配套关系见旧 package 的[版本映射](https://github.com/focus-creative-games/hybridclr_unity/blob/88e20505f87d57cbff5695be5f7baa35d626f619/Data%7E/hybridclr_version.json)。旧本地目录使用 `HybridCLRData/LocalIl2CppData-{Application.platform}/il2cpp`；本轮 v9 目录在 Library 下。

v6 的 `GetInterpMethodInfo` 在 `InterpreterModule.cpp:588`，先经 `image->GetMethodBody` 取得 body，再调用接收 image、method、body 与 result 的 Transform。它没有 v9 的 MethodBodyCache 模块。历史源码核对不代表该组合通过了本机自定义 Editor 的 Player 验证。

## 读到哪里算完成第一轮

留下一份小记录即可：版本与构建身份、DLL 哈希、目标方法的调用栈、首次／再次的缓存状态，以及两个返回值。断点截图要带上方法身份，单张 `Execute` 截图不够。

本轮已经留下构建日志、DLL / PDB 哈希和 CDB 文本断点记录；没有使用受限源码截图。第一轮源码与 Player 均对应上面锁定的 v9 组合。

完成后按问题继续读：

- 想解释每个模块为何这样分工：[原理拆解]({{< relref "engine-toolchain/hybridclr-principle-from-runtimeapi-to-interpreter-execute.md" >}})。
- 想追 Unity 侧的反射与方法分派入口：[调用链实战]({{< relref "engine-toolchain/hybridclr-call-chain-follow-a-hotfix-method.md" >}})，与本文使用同一固定源码基线。
- 想解释生成物、bridge 或 AOT 泛型失败：[工具链]({{< relref "engine-toolchain/hybridclr-toolchain-what-generate-buttons-do.md" >}}) → [ABI 桥接]({{< relref "engine-toolchain/hybridclr-bridge-abi-cross-boundary-calling-convention.md" >}}) → [AOT 泛型与补充元数据]({{< relref "engine-toolchain/hybridclr-aot-generics-and-supplementary-metadata.md" >}})。
- 想找完整阅读路径：[HybridCLR 索引]({{< relref "engine-toolchain/hybridclr-series-index.md" >}})。

**源码阅读的第一步，是证明当前 Player 正在执行你手里的那份代码，再解释它为什么走到这里。**
