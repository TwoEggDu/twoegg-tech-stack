---
title: "泛型调用综合实验｜从 ECMA-335 签名到 IL2CPP 实现与 HybridCLR 执行"
slug: "runtime-generic-call-experiment"
date: "2026-10-25"
description: "用同一个 Echo<T>，对照显式 AOT 实例和补充元数据，串起 MethodSpec、原生实现、解释器内联与 Player 返回值。"
series: "HybridCLR"
weight: 64
hybridclr_version: "runtime v9.0.1 / package v9.0.3；本机自定义 Unity 2022.3.62f3"
tags: [Unity, ECMA-335, IL2CPP, HybridCLR, Generics, Experiment]
---

同一个泛型方法，传 `string` 能执行，改成 `int` 却报缺少 AOT 泛型实例。补充元数据后两者都成功了，但调试器没有命中 `Echo` 的独立解释帧。这些现象并不矛盾：**调用描述、可用实现和最终执行路径，是三份需要分别核对的证据。**

这次用一个返回输入值的 `Echo<T>`，把 ECMA-335、IL2CPP 和 HybridCLR 三个专栏接到同一次 Player 实验上。我们只改变两个条件：主包是否显式引用三种泛型实例、运行前是否补充目标 AOT 程序集的元数据。正文保留失败组，也解释成功组实际走过的路径。

> **读这篇之前**：先读 [签名与引用解析]({{< relref "engine-toolchain/ecma335-signatures-and-reference-resolution.md" >}})、[IL2CPP 泛型代码生成]({{< relref "engine-toolchain/il2cpp-generic-code-generation-sharing-instantiation.md" >}}) 和 [AOT 泛型与补充元数据]({{< relref "engine-toolchain/hybridclr-aot-generics-and-supplementary-metadata.md" >}})。调试环境准备见 [HybridCLR 源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}})。

> **实测范围**：2026-10-25，Windows x64 IL2CPP Development Player，本机自定义 Unity Editor。下文版本、配置、哈希和运行结果构成一组记录，不推广为标准发行版、所有平台或其他 HybridCLR 版本的结论；原有 v6.1.0 源码阅读说明继续保留。

## 先把一次调用拆成三层

| 层次 | 本次要回答的问题 | 证据 |
|---|---|---|
| CLI 描述 | 调用哪个定义，给哪个泛型参数代入什么类型？ | 实际 `call` 操作数、MemberRef、MethodSpec、MethodDef 签名 |
| AOT 产物 | 主包有没有这次调用能使用的原生实现？ | 生成符号、注册产物、匹配的 GameAssembly 与 PDB |
| 运行时执行 | 最终进入原生函数，还是把补充的方法体转换后解释执行？ | 目标断点、调用栈、方法身份、返回值与异常 |

ECMA-335 描述程序如何表达泛型调用；它不规定 Unity 必须生成几份原生函数，也不规定 HybridCLR 怎样缓存或内联方法。MethodSpec 存在，只证明调用有描述。`Assembly.Load` 成功，只证明装载这一步完成。两者都不能替代目标方法的执行证据。

## 样本与实验条件

### 三个程序集各做一件事

`Generic.AOT` 在主包中定义泛型方法和结构体，`HotUpdate` 调用它们，`Probe.AOT` 负责装载、反射调用包装入口并检查结果。热更程序集不进入 AOT 转换。

完整实验维护在公开仓库 [hybridclr-runtime-lab](https://github.com/TwoEggDu/hybridclr-runtime-lab)。本文固定到 [提交 `e0220099`](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/e0220099d293f7a1a4601ee9931b49a10dd519f9)（完整 SHA：`e0220099d293f7a1a4601ee9931b49a10dd519f9`），P0、P1 分别位于 [projects/generic-p0](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/e0220099d293f7a1a4601ee9931b49a10dd519f9/projects/generic-p0) 和 [projects/generic-p1](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/e0220099d293f7a1a4601ee9931b49a10dd519f9/projects/generic-p1)。下面保留核心自有代码，完整 asmdef、构建器、启动器、元数据读取器与 CDB 指令以该提交为准。

```csharp
// Generic.AOT
[Preserve]
public struct Payload
{
    public int X;
    public long Y;
}

[Preserve]
public static class GenericTarget
{
    [Preserve, MethodImpl(MethodImplOptions.NoInlining)]
    public static T Echo<T>(T value) => value;
}

// HotUpdate.GenericCalls，三个包装方法都没有参数
public static string RunString() => GenericTarget.Echo<string>("echo-string");
public static int RunInt() => GenericTarget.Echo<int>(42);
public static long RunPayload()
{
    var result = GenericTarget.Echo<Payload>(new Payload { X = 7, Y = 11 });
    return result.X * 1000000L + result.Y;
}
```

`Preserve` 表达保留需求，不能把它当作三个闭合实例都已生成的证明。`NoInlining` 用于保留可观察的原生调用；本次还会看到，不能据此假定另一套解释器的转换过程一定不内联。

启动器先决定是否调用 `LoadMetadataForAOTAssembly`，再 `Assembly.Load` 热更 DLL。每个包装方法调用两次，分别断言 `echo-string`、`42`、`7000011`。发生异常时继续检查其他输入，最终用退出码和失败计数收束。

### 两份构建，四个独立进程

| 构建 | 显式 AOT 实例引用 | 启动方式 |
|---|---|---|
| P0 | 没有 seeds；泛型定义仍保留 | M0 不补元数据；M1 补元数据 |
| P1 | AOT 启动代码实际调用 `Echo<string>`、`Echo<int>`、`Echo<Payload>` | M0 不补元数据；M1 补元数据 |

每种启动方式使用全新进程，避免先补入元数据再把同一进程当成“不补”组。P1 的 seeds 在热更装载前执行，因此它同时改变构建引用和运行时预先调用状态；本实验研究实现可用性与分派，不用 P0/P1 比较冷启动性能。

两个独立工程分别编译出的热更 DLL 哈希最初不同。核对调用 IL 和签名一致后，最终运行统一投放 **P0 的同一份 `HotUpdate.dll`**，并重新验证 P1 两组。补充元数据则各用各自主包同次裁剪的 `Generic.AOT.dll`，采用 `HomologousImageMode.Consistent`；其哈希与最终 Player 备份目录中的裁剪 DLL 一致，没有跨主包混用。

`GenerateAll` 正常执行，桥接代码正常生成。生成的 `AOTGenericReferences.cs` 里三个 Echo 引用只是注释清单，不是已执行的泛型实例调用；P1 的实际 seeds 另外提供。

### 固定环境

| 项目 | 本次记录 |
|---|---|
| Editor | 本机自定义 `2022.3.62f3_96770f904ca7`，手动登记 |
| package | v9.0.3，`6cadd24d101f4cbd00cd80190106f6621132d24d` |
| HybridCLR runtime | v9.0.1，`83929787581bce6c7219418f62348a10c33582ee` |
| il2cpp_plus | v2022.3.x-9.0.1，`ba3a76c9979d14cee404fb5d44301773e0768866` |
| Player | StandaloneWindows64、IL2CPP、Development、C++ Debug |
| 实际构建输入 | 两组 Bee 记录均为 `Ruleset=Minimal`、`EnableFullGenericSharing=false` |
| 调试与读取 | 匹配 GameAssembly/PDB 的 CDB；.NET SDK 10.0.402 的只读 metadata 工具 |

源码组合沿用前次已经验证的环境，没有修改 runtime 或 Editor 安装目录。构建选项和生成符号同时保留：后文出现 `FullySharedGenericAny` 符号，并不等于上表开关为 true，更不能据此宣称验证了 HybridCLR 商业完全泛型共享能力。

## 第一层：从实际 call 找回泛型定义

读取器用 `System.Reflection.Metadata` 分析最终投放的 DLL，按 IL 操作数宽度推进，定位真正执行的 `call`；没有只枚举 MethodSpec 表就把所有记录当成调用。

| HotUpdate 包装方法 | 调用位置 | 操作数 | MethodSpec blob | 泛型实参 |
|---|---|---|---|---|
| RunString | `IL_0005: call` | `0x2B000001` | `0A 01 0E` | string |
| RunInt | `IL_0002: call` | `0x2B000002` | `0A 01 08` | int32 |
| RunPayload | `IL_001C: call` | `0x2B000003` | `0A 01 11 2D` | valuetype GenericProbe.Payload |

三个 MethodSpec 都指向 HotUpdate 内的 `MemberRef 0x0A000009`，声明类型是 `Generic.AOT:GenericProbe.GenericTarget`。这是一个普通静态类，泛型参数属于方法，所以此处 MemberRef 的父记录是 **TypeRef**；不要照搬泛型类 `Box<T>` 示例中的 TypeSpec 路径。

MemberRef 与目标 AOT `MethodDef 0x06000003` 的方法签名均为：

```text
10 01 01 1E 00 1E 00
│  │  │  └────┬────┘
│  │  │       返回类型与唯一参数都是 MVAR 0（!!0）
│  │  一个普通参数
│  一个方法泛型参数
带 GENERIC 标志，无 HASTHIS 的默认调用约定
```

目标 GenericParam 的序号为 0、名称 T、约束标志 None、约束记录数为 0。MethodSpec blob 的 `0A` 表示泛型方法实例签名，后面的 `01` 是实参数量；结构体签名里的 `2D` 是 TypeDefOrRef 编码索引，不能直接当作普通 metadata token。

因此，这一层可以确定：**三个调用使用同一方法定义，分别代入三种类型。** 还不能确定哪个闭合调用在主包里有可用的原生入口。规范依据是 [ECMA-335 第六版](https://ecma-international.org/publications-and-standards/standards/ecma-335/) Partition II 的 MemberRef、MethodSpec 与签名定义，以及 Partition III 的 `call`。

## 第二层：显式实例确实改变了原生产物

生成物检查与 PDB 符号给出了以下差异。表中省略编译器哈希后缀，只保留辨识实现需要的类型部分；不转载生成的 C++ 函数体。

| 构建 | 观察到的 Echo 共享实现 |
|---|---|
| P0 | `Echo_TisIl2CppFullySharedGenericAny…_gshared` |
| P1 | 上述实现，以及 `Echo_TisRuntimeObject…_gshared`、`Echo_TisInt32…_gshared`、`Echo_TisPayload…_gshared` |

没有显式 seeds，不等于产物中完全没有 Echo 原生实现。P0 留下了共享实现，`string` 调用确实进入它。但“文件中存在一个共享函数”也不能证明任意闭合实例都能调用它：本次 P0 的 `int`、`Payload` 在不补元数据时仍然失败。

P1 热更调用命中的分别是 RuntimeObject、Int32、Payload 对应的原生实现。`string` 在这里使用引用类型共享路径，没有要求每个引用类型各生成一份函数。这与 [HybridCLR 9.x AOT 泛型文档](https://www.hybridclr.cn/docs/basic/aotgeneric) 的共享规则相衔接；当前自定义工具链的具体符号与分派仍以本次产物和断点为准。

还有一项容易误查：`ScriptingAssemblies.json` 含有 HotUpdate 名称，因为 package 会补写热更程序集列表。判断它有没有被 AOT 转换，本次检查的是最终裁剪备份、生成源文件和原生 code registration：均没有 HotUpdate 的 AOT 实现，并由后续解释器断点确认包装方法的执行路径。

## 第三层：四组实测结果

下表中的每个输入都调用两次，结果一致。四组无调试器进程与四组原生调试记录相互核对；P0/M0 的非零退出码是保留的对照结果。

| 构建／启动 | string | int | Payload | 失败数／退出码 |
|---|---|---|---|---|
| P0/M0，无 seeds、不补 | `echo-string` | MissingMethodException | MissingMethodException | 4／2 |
| P0/M1，无 seeds、补充 | `echo-string` | `42` | `7000011` | 0／0 |
| P1/M0，有 seeds、不补 | `echo-string` | `42` | `7000011` | 0／0 |
| P1/M1，有 seeds、补充 | `echo-string` | `42` | `7000011` | 0／0 |

两个 M1 进程均记录 `GENERIC_METADATA code=OK bytes=4608`。P0/M0 的异常明确指出 `Generic.AOT.dll` 中对应的 Echo 闭合调用缺少 AOT 泛型实例；它不是 HotUpdate 文件未找到，也不是返回值校验失败。

### P0/M0：包装方法进了解释器，目标调用仍可失败

三个 Run 包装方法均完成首次 Transform，并各进入两次 `Interpreter::Execute`。其中只有 RunString 内部的 Echo 命中原生函数，调用栈经过 `Managed2NativeCallByReflectionInvoke`、fully shared invoker，再进入 Echo 实现。

RunInt 和 RunPayload 两次都抛出相同类别的异常。这说明 **Execute 入口命中不是整个方法执行成功的证据**；包装方法已可解释执行，也不等于它引用的 AOT 泛型目标已可调用。

### P0/M1：补入方法体后，Echo 被内联到解释器指令中

string 仍命中原来的原生共享实现。int 和 Payload 则出现另一条链：

```text
RunInt / RunPayload 首次 Transform
  → 判断 Echo 方法体是否可内联
  → TransformContext::TransformSubMethodBody（目标名称 Echo）
  → ConsistentAOTHomologousImage 的 MethodDef 0x06000003 方法体
  → 转换结果并入调用者
  → 调用者 Interpreter::Execute
  → 返回 42 / 7000011
```

两个闭合目标的 MethodInfo 地址不同，名称和定义 token 相同；断点发生在各自包装方法转换期间。方法体来自补充的 AOT image，不能只看 `0x06000003` 就认定它属于 HotUpdate——token 的含义必须带上 image。

这组没有命中 Echo 的独立 `EnterFrameFromInterpreter`，也没有命中 Echo<int>/Echo<Payload> 原生实现。结合 `TransformSubMethodBody`、方法体来源、调用者 Execute 和正确返回值，才能确认这个短方法被内联到解释执行路径。**补元数据后的解释执行，不必表现为每个被调方法都有独立解释帧。**

本次 Echo 带有 `NoInlining`，仍观察到上述行为。固定 runtime 的内联判定会检查 IL 大小、异常区和控制流等条件；源码阅读应跟着这版转换器走，不能用原生编译器的预期替代。相关入口见固定提交中的 [MethodBodyCache.cpp](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/metadata/MethodBodyCache.cpp) 与 [TransformContext.cpp](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/transform/TransformContext.cpp)。

### P1：补元数据没有强制替换已经可用的原生路径

P1 两组都命中 RuntimeObject、Int32、Payload 原生共享实现，补充元数据之后也没有将这些调用强制改成解释执行。

统计断点时要剔除 seeds：每个原生共享实现命中 3 次，其中 1 次来自 AOT 启动调用，另外 2 次来自热更包装方法。原生 wrapper 与共享函数也可能各触发一次断点，不能把所有名称包含 Echo 的命中都当作独立业务调用。

四组中，每个包装方法均只发生一次 Transform。两次 Execute 记录的 MethodInfo 和非空 interpData 相同；这项缓存观察包括失败组的包装方法，不能由此推断失败的 Echo 目标已经有了有效实现。

## 复现时怎样保留完整证据

本节对应实验仓库首个提交的重新构建与取证，构建标识为 `136f3c65-0c8b-4944-a838-39a5f4024e27`，结果与前次隔离实验一致。使用 [固定版本 README](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/e0220099d293f7a1a4601ee9931b49a10dd519f9/README.md) 和 [lab-lock.json](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/e0220099d293f7a1a4601ee9931b49a10dd519f9/lab-lock.json)，不要把后续 main 上的代码与本次摘要混用。

```powershell
git clone https://github.com/TwoEggDu/hybridclr-runtime-lab.git
cd hybridclr-runtime-lab
git checkout --detach e0220099d293f7a1a4601ee9931b49a10dd519f9
python tools/prepare.py
# $editor 为锁定基线的 Unity.exe；$cdb 为 Windows SDK x64 cdb.exe
python tools/lab.py build --editor $editor
python tools/lab.py run
python tools/lab.py trace --cdb $cdb
```

1. `prepare` 按锁定提交准备依赖；`build` 核对实际 Editor 身份，构建 Add、P0、P1 三个隔离工程，保留同次 GameAssembly 与 PDB。
2. 构建器统一投放 P0 的热更 DLL，并检查各自 supplementary DLL 与最终裁剪产物一致；只有 P1 包含 seeds。
3. `run` 在独立进程执行 Add 和四组泛型对照；P0/M0 的退出码 2 是预期负对照，其他组必须为 0。
4. `trace` 使用匹配符号运行 CDB，再调用 `verify_generics.py` 和 `verify_call_chain.py` 检查结果、方法身份、缓存和路径计数。构建、运行与日志通过 build ID 和哈希绑定，旧记录不能代替本次验证。
5. 按 README 的 MetadataReader 命令读取实际 DLL。公开的 [签名输出](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/e0220099d293f7a1a4601ee9931b49a10dd519f9/evidence/2026-10-25/metadata.txt) 与 [泛型结果摘要](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/e0220099d293f7a1a4601ee9931b49a10dd519f9/evidence/2026-10-25/generics.json) 属于上述同一构建。

本次公共热更 DLL 的 SHA-256 为 `6AB0CC588F8AA86BFE66697E0FC0EBF6F6266F53AC127726004D18A6F1E53864`。两份 GameAssembly、PDB 和 supplementary DLL 的完整哈希在摘要中；它们用于辨认这次产物，不要求另一台机器重建出相同二进制。

公开材料仅包含自有 C#、操作脚本、签名结果和脱敏观察。Editor 安装、完整构建日志、Unity C++ 实现、原生二进制与 PDB 留在本地。本次没有测性能，没有验证 ARM64、WebGL、Release 裁剪、热更自定义值类型或商业 FGS；这些都不能由四组功能结果推出。

## 回到三个专栏，各自带走什么

| 专栏 | 这次实验补上的联系 | 回看入口 |
|---|---|---|
| ECMA-335 | 从实际 call 到 MemberRef、MethodSpec 和方法定义；签名身份不等于执行能力 | [ECMA-335 索引]({{< relref "engine-toolchain/ecma335-series-index.md" >}}) |
| IL2CPP | 从显式实例引用到不同原生产物，再用目标断点确认共享实现 | [IL2CPP 索引]({{< relref "engine-toolchain/il2cpp-series-index.md" >}}) |
| HybridCLR | 从补充方法体到转换、内联、缓存与执行；区分包装方法和目标方法 | [HybridCLR 索引]({{< relref "engine-toolchain/hybridclr-series-index.md" >}}) |

**排查泛型调用时，先确认它描述的是谁，再确认可用实现，最后用实际执行路径解释结果。**
