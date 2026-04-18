---
title: "ECMA-335 基础｜CLI Security：Strong Name、Code Access Security 与现代演进"
slug: "ecma335-cli-security-strong-name-cas"
date: "2026-04-15"
description: "从 ECMA-335 规范出发，拆解 CLI security 模型的三类机制——evidence-based security、permission-based security、strong name；说明 CAS 在现代 .NET 中被废弃的原因，以及 strong name 在 CoreCLR/Mono/IL2CPP/HybridCLR/LeanCLR 中的实际角色。"
weight: 19
featured: false
tags:
  - "ECMA-335"
  - "CLR"
  - "Security"
  - "StrongName"
  - "CAS"
series: "dotnet-runtime-ecosystem"
series_id: "ecma335"
---

> CAS（Code Access Security）已经被现代 .NET 抛弃，但 strong name 和 publisher policy 在 Unity 项目里仍然影响 assembly 加载行为。理解规范层的 security 模型，才能知道哪些机制是历史包袱，哪些仍然在工作。

这是 .NET Runtime 生态全景系列的 ECMA-335 基础层第 12 篇。

前 11 篇覆盖了 metadata、CIL 指令集、类型系统、执行模型、程序集模型、内存模型、泛型共享、verification、custom attribute、P/Invoke。这些层次描述了 CLI 的代码组织、执行语义和与原生世界的接口。这一篇补一块容易被简单跳过但仍然在工程上发挥作用的拼图：security——CLI 规范层定义的安全机制是什么，哪些已经废弃，哪些仍然在 runtime 中工作。

> **本文明确不展开的内容：**
> - .NET Framework 时代 CAS 的完整 permission 体系（已废弃，工程上不再有意义）
> - Authenticode 代码签名（OS 层机制，不在 CLI 规范层）
> - .NET Core/5+ 的现代安全模型（如 single file deployment、tamper detection，属于宿主层而非 CLI 规范）

## CLI Security 的规范定义

ECMA-335 Partition I §13 把 CLI security 划分成三类机制。这个划分是理解整个 security 模型的起点——不同机制的工作层级和现代命运完全不同。

**Evidence-based security** — assembly 的"来源"作为权限决策的输入。来源由若干 evidence 组成：assembly 所在的 zone（本机 / Intranet / Internet）、加载 URL、publisher（代码签名证书）、strong name（密码学标识）。runtime 把这些 evidence 喂给 security policy，得到"这个 assembly 能拿到哪些权限"的答案。

**Permission-based security** — 在确定权限之后，代码可以通过声明式（Attribute）或命令式（API）的方式要求、断言、拒绝某些权限。runtime 在敏感 API（文件 IO、反射、非托管代码调用、网络访问）的入口检查权限是否满足，不满足则抛 `SecurityException`。

**Strong name** — assembly 的密码学标识，由 RSA 密钥对生成。它同时承担三个角色：assembly identity 的组成部分（在 A5 已展开）、防篡改保证、绑定到具体 publisher 的依据。

三者的关系是：strong name 是 evidence 的一种来源；evidence 决定 permission set；permission set 决定哪些受保护操作能执行。整套机制构成了 CAS（Code Access Security）的骨架。

CAS 的设计意图非常具体：让一台 .NET 机器能够安全地运行来自不可信源的代码。比如浏览器里跑的 Silverlight 控件、IIS 上跑的 medium trust web 应用、企业内网的 ClickOnce 部署应用——这些场景中 runtime 必须基于代码来源做出权限决策，而不是把所有 assembly 都视为完全可信。

到了现代 .NET 生态，partial trust 场景已经全面退场，CAS 已被 .NET Core 完全移除，但 strong name 在多个 runtime 上仍然存在——这是本文要厘清的核心边界。

## Strong Name 的工作原理

ECMA-335 Partition II §6.2.1.3 定义了 strong name 的密码学结构。strong name 不是简单的"签名"——它是 assembly identity 与签名机制的结合。

### 四元组身份

A5 已经展开过 assembly identity 的四元组：

```
Name + Version + Culture + PublicKeyToken
```

PublicKeyToken 就是 strong name 的紧凑表达。完整的 strong name 包含完整的 RSA public key（最长 1024 字节），但在 AssemblyRef、类型签名等需要频繁出现的地方，规范用 PublicKeyToken（public key 的 SHA-1 哈希后 8 字节）作为紧凑标识符。

### 生成流程

工程上的 strong name 生成步骤：

1. 用 `sn.exe -k MyKey.snk` 生成一对 RSA 密钥（默认 1024 位，可指定到 2048 / 4096）
2. 编译时通过 `[assembly: AssemblyKeyFile("MyKey.snk")]` 或项目文件中的 `<SignAssembly>true</SignAssembly>` 引用密钥
3. 编译器把 public key 写入 metadata 的 Assembly 表（PublicKey 列，blob heap 中的二进制数据）
4. 编译器对整个 assembly 内容（除签名区域本身）算 SHA-1 哈希，再用 private key 对哈希签名
5. 签名结果写入 PE 文件的 strong name signature 区（PE 头部 CLI Header 中由 `StrongNameSignature` RVA 指向的位置）

整个过程的核心是把一个"内容指纹 + 签名"嵌入到 assembly 本身的二进制中。任何对 assembly 内容的修改（修改 IL、添加资源、改 metadata）都会让原始签名失效。

### 验证流程

runtime 加载 strong-named assembly 时的验证步骤：

1. 从 PE 头部找到 strong name signature 的 RVA 和长度
2. 从 metadata 的 Assembly 表读出 public key blob
3. 对 assembly 内容（同样跳过签名区域本身）算 SHA-1 哈希
4. 用 public key 验证签名是否对应这个哈希
5. 验证失败 → 抛 `FileLoadException` 或 `BadImageFormatException`，加载终止

这个验证是 runtime 强制的——不是由开发者代码主动调用。在启用 strong name 验证的 runtime 上，未通过验证的 assembly 根本进不到 JIT/解释器阶段。

### PublicKeyToken 的作用

为什么需要 PublicKeyToken 这个紧凑形式？因为 AssemblyRef 表（每个跨 assembly 引用一条记录）会大量重复出现 public key 信息。1024 字节 × 几十条 AssemblyRef 会让 metadata 显著膨胀。SHA-1 哈希的后 8 字节足以做实际的身份匹配（理论碰撞概率极低，在 strong name 体系内可接受）。

举一个 Unit / Player 参考类的例子：如果 `Player` 类在一个 strong-named assembly `MyGame.Logic` 里，并且引用了 `MyGame.Core.Unit`，那么 `Player` 所在 assembly 的 AssemblyRef 表会有这样一条记录：

```
AssemblyRef[0]:
  Name        = "MyGame.Core"
  Version     = 1.0.0.0
  Culture     = neutral
  PublicKeyToken = abc123def4567890   ← 8 字节，紧凑标识
```

runtime 解析 `Player` 中对 `Unit` 的引用时，要找到一个名字 + 版本 + culture + PublicKeyToken 都匹配的 assembly。如果加载到的 `MyGame.Core.dll` PublicKeyToken 不一致，加载失败——即使 IL 完全相同。这是 strong name 提供的"identity 隔离"语义。

## Code Access Security（CAS）的规范基础

ECMA-335 Partition I §13.5 定义了 CAS 的核心概念。CAS 的目标是在同一进程内为不同 assembly 划出不同的权限边界——一个不可信的 assembly 即使加载到与可信代码同一个进程，也无法越权访问受保护资源。

### SecurityAction 枚举

CAS 通过 `SecurityAction` 枚举表达不同的权限操作意图：

| SecurityAction | 含义 |
|----------------|------|
| `Demand` | 要求调用栈上所有 caller 都拥有指定权限 |
| `Assert` | 断言当前帧拥有权限，阻止权限检查向上传播 |
| `Deny` | 拒绝当前帧及其调用的代码使用指定权限 |
| `PermitOnly` | 只允许指定权限，其他权限视为 Deny |
| `LinkDemand` | 在 JIT 编译期检查直接 caller 的权限 |
| `InheritanceDemand` | 要求继承类拥有指定权限 |

`Demand` 是 CAS 最常用的形式：runtime 沿着调用栈逐帧检查每个 assembly 是否拥有要求的权限。只要有一帧的 assembly 缺少权限，就抛 `SecurityException`。这就是经典的"stack walking permission check"。

### PermissionSet

权限本身是 `IPermission` 接口的实现。BCL 内置了一组 permission：

| Permission | 控制什么 |
|------------|---------|
| `FileIOPermission` | 文件读写、目录访问 |
| `ReflectionPermission` | 反射访问私有成员、Reflection.Emit |
| `UnmanagedCodePermission` | P/Invoke 调用原生代码 |
| `SecurityPermission` | 修改 security policy 自身 |
| `EnvironmentPermission` | 读写环境变量 |
| `RegistryPermission` | Windows 注册表访问 |

一个 assembly 实际拥有的权限是一个 `PermissionSet`——多个 permission 的集合。runtime 根据 evidence（来源）通过 security policy 计算出这个 set。

### DeclSecurity 表

ECMA-335 Partition II §22.11 定义了 DeclSecurity 表（表编号 0x0E），用于存储声明式 security 信息。每条记录包含：

- Action（SecurityAction 值）
- Parent（被附加 security 的 metadata 元素：assembly、type、method）
- PermissionSet（权限集，存储为 blob）

C# 编译器把 `[SecurityPermission(SecurityAction.Demand, ...)]` 这样的 attribute 翻译成 DeclSecurity 表中的一条记录。runtime 在加载或调用对应元素时读取 DeclSecurity 表执行权限检查。

### 典型用法

CAS 的典型用法（已废弃但要理解其语义）：

```csharp
[SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
public class Player : Unit {
    [DllImport("game_native.dll")]
    public static extern int CalculateDamage(int playerId, int targetId);

    public int Hit(Unit target) {
        // 调用前 runtime 沿调用栈检查每一帧的 UnmanagedCode 权限
        return CalculateDamage(this.id, target.id);
    }
}
```

`Player` 类被声明为"调用我必须有 UnmanagedCode 权限"。任何调用 `Player.Hit` 的代码栈上每一层 assembly 都必须拥有这个权限——只要有一个不可信 assembly 在中间，调用失败。

这套机制在 .NET Framework 时代是 plugin 沙箱、ClickOnce 部署、ASP.NET medium trust 的核心防线。

## CAS 在现代 .NET 中的废弃

CAS 从 .NET Framework 4.0 开始就被官方逐步降低优先级。现代 .NET 对它的处理是：完全移除。

**.NET Framework 4.0**：引入"Security Transparent Code"模型，把代码分成 Transparent / SafeCritical / Critical 三类，简化 CAS 的复杂度。但社区对 CAS 整体的批评仍然集中在两点——心智负担过重、攻击面过大。

**.NET Core 1.0**：完全移除 CAS。所有 `SecurityAction` 在 CoreCLR 中是 no-op——`[SecurityPermission]` attribute 仍然能被 reflection 读到（DeclSecurity 表数据保留），但 runtime 不会基于它做任何权限检查。`PermissionSet`、`CodeAccessPermission` 等核心类型在 .NET Core 中要么被标记为 `[Obsolete]`，要么直接抛 `PlatformNotSupportedException`。

废弃的根本原因是工程实践的反思。进程内沙箱模型在过去十多年间被多次证明不可靠：

- 多次 sandbox escape CVE 显示 CAS 自己的复杂度成了攻击面（reflection、custom attribute、generic 等机制都被用作绕过 CAS 的载体）
- JIT 编译器和 GC 的 bug 经常意外提供绕过 CAS 的路径（比如 type confusion、stack 不一致）
- 维护"一个进程内多种信任级别"的 BCL 需要在每个敏感 API 入口加 `Demand`，工程成本极高且容易遗漏

现代实践用进程隔离取代进程内沙箱：每个不可信代码跑在独立进程或容器中，OS 层面的隔离机制（用户态权限、namespace、cgroup、Windows AppContainer）来强制权限边界。这种方式的安全性依赖于 OS 内核而不是 CLI 自身，攻击面小得多。

对工程的影响：

- 旧代码中的 `[SecurityPermission]`、`[FileIOPermission]` 在 .NET Core 中不会报错也不会生效
- 任何依赖"调用方必须有特定权限才能调用我"假设的旧 BCL 代码，在迁移到 .NET Core 时需要重新设计
- 上述 attribute 在 metadata 中仍然占空间，但运行时完全无视

## Strong Name 在现代 .NET 的角色

CAS 整体退场了，但 strong name 没有跟着退场——它的核心价值（assembly identity 与防篡改）独立于 CAS 机制本身。各 runtime 对 strong name 的处理差异很大。

### CoreCLR

Strong name 验证默认开启，但被显著简化：

- 不再用于 GAC（Global Assembly Cache）的资产隔离——.NET Core 整体没有 GAC 概念
- 不再参与 evidence-based security 决策（那套机制已经被移除）
- 仍然用于 assembly identity 的匹配——AssemblyRef 中的 PublicKeyToken 必须与实际加载的 assembly 一致
- 仍然用于防篡改——签名失败的 assembly 加载被拒绝

实际工程中，BCL（`System.*`、`Microsoft.*` 等）全部使用 strong name；NuGet 上的第三方包则参差不齐，越来越多的包不再使用 strong name（因为现代 .NET 不依赖它做权限决策，签名只增加发布流程的复杂度）。

### Mono

Mono 实现了 strong name 验证，但提供了配置开关：

- 默认行为：验证 strong name 签名，验证失败则拒绝加载
- 环境变量 `MONO_DISABLE_STRONG_NAMES=1` 关闭验证
- Unity 编辑器使用的 Mono runtime（Mono Scripting Backend）默认开启验证

历史上 Mono 还实现过部分 CAS 机制（用于 Moonlight，Mono 对 Silverlight 的实现），但随着 Moonlight 项目终止和 Mono 主线向 .NET Core 兼容靠拢，CAS 部分基本进入维护模式。

### IL2CPP

构建时 il2cpp.exe 已经把所有 assembly 静态绑定成 C++ 代码，运行时不再有"加载 assembly"的步骤——因此 strong name 的运行时验证不存在。

但 strong name 不是完全无用：il2cpp.exe 在转换前从 metadata 中读取 AssemblyRef 的 PublicKeyToken 来匹配输入 assembly。如果一个 assembly 引用了 strong-named BCL 但实际被链接的 BCL 是不同 PublicKeyToken 的版本，转换阶段会报错。换句话说，strong name 验证从运行时前移到了构建时，由 Unity 编辑器层（而非 IL2CPP runtime）负责。

### HybridCLR

热更 DLL 通常不签 strong name。原因和 A5 中讲的相同：热更 DLL 频繁重建、签名增加构建复杂度、HybridCLR 不依赖 strong name 做加载决策。

HybridCLR 的 `Assembly::LoadFromBytes` 路径在加载热更 DLL 时不验证 strong name 签名。这是工程上的取舍——热更 DLL 的可信度由开发者通过控制 DLL 来源（CDN 签名、HTTPS 传输、应用层校验和）保证，而不是由 strong name 机制保证。

### LeanCLR

LeanCLR 当前不验证 strong name。嵌入式场景的设计假设是"宿主信任所有加载的代码"——任何 DLL 能进入 LeanCLR 的加载路径，本身就意味着已经通过了宿主层的信任决策。strong name 在这种场景下不增加额外的安全性，反而拖慢加载速度。

## 现代 .NET 的安全替代

CAS 退场后，那些"想在进程内为不可信代码划权限边界"的需求并没有消失，而是被分散到几套不同的机制中。这里只做最简短的桥接说明，不展开实现细节。

**进程隔离** — Docker 容器、Windows Sandbox、AppContainer、macOS App Sandbox。每个不可信代码跑在独立进程内，OS 内核强制资源访问边界。这是现代云原生服务的主流安全模型——服务之间的隔离由容器边界保证，应用代码本身可以全权信任。

**AssemblyLoadContext** — CoreCLR 提供的可卸载隔离单元（继承自 `AssemblyLoadContext` 基类）。它能做到 assembly 加载、类型隔离、整体卸载，但**不强制权限**——加载到 ALC 中的 assembly 与主域 assembly 拥有相同的权限。ALC 解决的是"卸载与重新加载"问题（热更新、插件系统），不是 sandbox 问题。

**Roslyn analyzer** — 编译期静态检查危险 API 调用。比如禁止热更代码调用 `System.IO.File`、禁止反射访问敏感类型。Analyzer 在 IDE 和 CI 中跑，把权限检查左移到代码审查阶段。这个机制比运行时 CAS 弱（开发者可以禁用 analyzer），但工程上更可控。

**Authenticode + SignTool** — OS 层的代码签名（Windows 通过 Authenticode 验证 PE 文件的发布者签名）。这与 strong name 是两个完全不同的机制：strong name 是 CLI 规范定义的 assembly identity；Authenticode 是 OS 定义的 PE 文件签名，验证的是"这个文件来自某个证书持有者"。两者可以同时存在，互不替代。

## 工程影响

CLI security 模型在工程上还有几个具体影响。

### Unity 项目

Unity 编辑器使用 Mono Scripting Backend，strong name 验证默认开启。如果你引入的第三方 DLL 使用了 strong name 但与 Unity 链接的 BCL PublicKeyToken 不一致，编辑器会在加载该 DLL 时报错。常见情形：从 NuGet 拿到的包是为 .NET Framework 编译的，其 AssemblyRef 中记录的 mscorlib PublicKeyToken 可能与 Unity Mono 的 mscorlib PublicKeyToken 不匹配。

Player 构建用 IL2CPP 时，这个问题在构建时就被解决——il2cpp.exe 完成静态链接后，运行时不再涉及 strong name 验证。所以同一个 DLL 在编辑器报错但 Player 构建成功的情况确实存在。

HybridCLR 热更 DLL 通常不签名。开发者控制热更 DLL 的来源——通过私有 CDN 分发、应用层 SHA-256 校验、HTTPS 传输等机制保证 DLL 没有被篡改。这套机制不依赖 ECMA-335 规范，是应用层自己设计的。

### HybridCLR 安全策略

HybridCLR 商业版提供 access control policy（白名单机制）——可以配置热更 DLL 只能调用特定 BCL API、只能访问特定命名空间下的类型、不能 P/Invoke 等等。这是 CAS 的现代替代方案，但它的实现完全在 HybridCLR 自身，不依赖 ECMA-335 规范的 SecurityAction / PermissionSet 机制。

设计上的差异：

- 检查时机：HybridCLR 的 access control 在 metadata 加载阶段就拒绝违规调用（解析 MemberRef 时检查目标是否在白名单），CAS 的 Demand 是运行时栈遍历
- 颗粒度：HybridCLR 以 method / type 为最小单位，CAS 以 permission 为单位
- 心智模型：HybridCLR 的白名单语义直接、可枚举；CAS 的栈遍历 + 信任传播在多层调用下难以预测

这套机制在工程上比 CAS 实用得多——因为它把安全决策放在 metadata 解析这个明确的入口，而不是分散到每个敏感 API 的实现里。

### 遗留代码迁移

从 .NET Framework 迁移到 .NET Core / .NET 5+ 的项目，所有依赖 CAS 的代码段都会静默失效：

- `[SecurityPermission(SecurityAction.Demand, ...)]` 不再触发权限检查
- `CodeAccessPermission.Assert()` 调用不再有效（在 .NET Core 抛 `PlatformNotSupportedException`）
- 基于 evidence 的权限决策代码全部需要重写或删除

迁移的正确做法是：把所有 CAS 相关代码标记为废弃，用进程隔离 + Roslyn analyzer 重新设计权限模型。强制保留 CAS 风格的代码不会带来安全性，只会带来不必要的复杂度。

## 收束

CAS 是 ECMA-335 设计中最被遗忘的部分。当年它承载了 .NET 在 partial trust 时代的全部安全期望——浏览器控件、企业内网部署、ASP.NET medium trust——但工程实践证明进程内沙箱模型不可靠，最终在 .NET Core 中被整体推倒。

但 strong name 仍然是 assembly identity 的核心组成（A5 已展开），P/Invoke 的 native code 权限边界（A10）仍然由 OS 而非 CLI 控制。这两块是 CLI security 模型中真正活下来的部分。

理解规范层定义的 security 模型，能解释几个具体问题：

- 为什么 .NET Core 把 CAS 整体推倒重来——因为进程内沙箱模型在工程上被反复证明不可靠
- 为什么 strong name 验证在不同 runtime 上有不同行为——因为各 runtime 对 assembly identity 与防篡改的权衡不同
- 为什么 HybridCLR 商业版要自己重新设计 access control——因为 ECMA-335 的 CAS 机制在热更场景下无法直接复用，需要重新设计 metadata 阶段的检查机制

至此 ECMA-335 基础层的 12 篇文章覆盖了 metadata、CIL 指令集、类型系统、执行模型、程序集模型、内存模型、泛型共享、verification、custom attribute、P/Invoke、security 这一整套规范层契约。下一篇展开 CLI threading 模型——volatile、memory barrier、interlocked 操作的规范定义，以及它们如何与现代 CPU 的内存模型对接。

## 系列位置

- 上一篇：<a href="{{< relref "engine-toolchain/ecma335-pinvoke-native-interop-marshaling-spec.md" >}}">A10 P/Invoke 与 Native Interop</a>
- 下一篇：<a href="{{< relref "engine-toolchain/ecma335-threading-memory-model-volatile-barriers.md" >}}">A12 CLI Threading 内存模型</a>
