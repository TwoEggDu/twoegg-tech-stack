---
date: "2026-03-28"
title: "Unity 脚本编译管线 02｜ILPP：Unity 为什么要偷偷改你的字节码"
description: "解释什么是 ILPP（IL Post Processing），为什么 Unity 需要在编译后对 .dll 进行字节码注入，Burst 和 Job System 分别用 ILPP 做了什么，以及为什么它运行在独立进程里通过 gRPC 通信。"
slug: "unity-script-compilation-pipeline-02-ilpp"
weight: 63
featured: false
tags:
  - "Unity"
  - "ILPP"
  - "Burst"
  - "Jobs"
  - "IL"
  - "Compilation"
series: "Unity 脚本编译管线"
series_order: 2
---

> 你写的 C# 接口很干净，但运行时跑的字节码早已被 Unity 悄悄改过了。

---

## 这篇要回答什么

1. `ILPP` 是什么，它在编译管线的哪个环节介入？
2. Unity 为什么不用 Source Generator 或运行时反射，而是选择字节码后处理？
3. `Burst` 和 `Job System` 分别用 `ILPP` 做了什么具体的事？
4. `ILPP` 为什么运行在独立进程里，通过 gRPC 和 `bee_backend` 通信？

---

## 从一个问题开始

你写了这段代码：

```csharp
[BurstCompile]
public struct MyJob : IJob
{
    public NativeArray<float> data;

    public void Execute()
    {
        for (int i = 0; i < data.Length; i++)
            data[i] *= 2f;
    }
}
```

然后它就"魔法般地"工作了：`NativeArray` 有越界检查，Job 能被 Burst 编译成原生 SIMD 指令。

但你没有写任何安全检查代码，也没有告诉编译器"请用 LLVM 编译这个方法"。这些东西是谁插进去的？

答案是 `ILPP`。

---

## ILPP 是什么

**IL Post Processing**，IL 字节码后处理。

编译管线的顺序是这样的：

```
.cs 源文件
    ↓  Roslyn（csc.exe）
.dll（标准 .NET 程序集，含 IL 字节码）
    ↓  ILPP（Unity 的字节码后处理）
修改后的 .dll（IL 被注入/替换）
    ↓  运行时加载 / Burst 编译
最终执行
```

`ILPP` 介于 Roslyn 编译和运行时加载之间。它拿到标准的 .NET 程序集，用 [Mono.Cecil](https://github.com/jbevain/cecil) 读取和修改 IL 字节码，再写回一个新的 .dll。

从用户视角看，你写的是干净的接口。从运行时视角看，字节码已经不一样了。

---

## 为什么不用其他方案

Unity 也可以用别的方式实现同样的效果。为什么选 `ILPP`？

| 方案 | 原理 | 问题 |
|---|---|---|
| **Source Generator** | 在 Roslyn 编译阶段生成额外 C# 代码 | Unity 历史版本的 Roslyn 不完整；生成代码对用户可见，侵入感强；无法做 IL 级别的精细控制 |
| **运行时反射代理** | 运行时动态生成委托/代理 | 性能差，有 GC 压力；`Burst` 根本不支持反射，完全行不通 |
| **自定义 C# 编译器** | 替换或 fork Roslyn | 维护成本极高；IDE 支持（Rider、VS）立刻断掉 |
| **ILPP（字节码后处理）** | 编译完成后修改 .dll | 对用户完全透明；编译器无关；可做任意 IL 变换；不影响 IDE 的代码分析 |

`ILPP` 的核心优势是**对用户透明**：你写普通 C#，Unity 在你看不见的地方做完所有变换。你的源码、IDE 体验、类型检查全部不受影响。

---

## Burst 和 Jobs 各用 ILPP 做了什么

Unity 的 `ILPP` 架构允许任何包实现 `ILPostProcessor` 接口，注册自己的处理逻辑。Burst 和 Jobs 都有各自的处理器。

### Job System：`Unity.Jobs.CodeGen.JobsILPostProcessor`

Job System 的 `ILPP` 处理器做的事情比较直白：**在 `Execute()` 的前后插入安全检查代码**。

具体来说，对于每一个实现了 `IJob`、`IJobParallelFor` 等接口的结构体，处理器会：

1. 在 `Execute()` 入口处，对所有 `NativeContainer` 字段调用 `AtomicSafetyHandle.CheckWriteAndThrow` 或对应的读取检查
2. 在 `Execute()` 出口处（包括异常路径），释放对应的安全句柄持有

这就是为什么你用 `NativeArray` 越界访问会得到一个带有明确调用栈的异常，而不是一个无声的内存越界崩溃。这些检查代码**不是你写的，是 `ILPP` 注入的**。

在 Release 模式或 Burst 编译路径下，这些安全检查会被裁掉，不影响性能。

### Burst：`Unity.Burst.CodeGen.BurstILPostProcessor`

Burst 的 `ILPP` 处理器做的事情更底层：**标记和准备 Burst 编译入口**。

当它扫描到带有 `[BurstCompile]` 特性的类型或方法时，它会：

1. 在 IL 层面为这些方法生成或修改元数据，让 Burst 编译器能精确定位哪些方法需要 LLVM 编译
2. 处理 `[BurstCompile]` 方法的 IL，使其符合 Burst 的限制（比如不能有托管对象引用）
3. 生成必要的函数指针包装代码，让托管侧可以安全地调用 Burst 编译后的原生函数

简单说：`ILPP` 是 Burst 流程的**前置门卫**，它把需要 Burst 编译的东西整理清楚，后续才交给 LLVM 处理。没有这一步，Burst 根本不知道该编译什么。

---

## 为什么运行在独立进程里，通过 gRPC 通信

这是一个架构问题，理解它能解释你在编译日志里看到的很多奇怪现象。

### 两种语言，两个世界

Unity 的构建系统后端叫 `bee_backend`，它是用 C++ 写的高性能构建执行器，负责调度编译任务、管理依赖关系。

但 `ILPP` 处理器（比如 `Burst.CodeGen`、`Jobs.CodeGen`）是 .NET 程序集，必须运行在 .NET 运行时里。

C++ 进程无法直接调用 .NET 程序集。这就产生了跨进程通信的需求。

### Unity 的解法：ASP.NET Core + gRPC

Unity 的解法是：**启动一个 ASP.NET Core 进程，专门托管所有 `ILPostProcessor`，通过 gRPC（HTTP/2）对外暴露接口**。

流程如下：

```
bee_backend（C++）
    │
    │  gRPC 请求：发送原始 .dll 字节流
    ▼
ILPostProcessing 服务进程（.NET / ASP.NET Core）
    │
    ├── JobsILPostProcessor.Process(assembly)
    ├── BurstILPostProcessor.Process(assembly)
    └── （其他已注册的处理器...）
    │
    │  gRPC 响应：返回修改后的 .dll 字节流
    ▼
bee_backend（C++）
    │  写回修改后的 .dll
    ▼
后续编译步骤
```

`bee_backend` 对每一个需要处理的 .dll 发一次 gRPC 请求，拿回处理后的版本。

这就是为什么你在 Unity 编译日志里（开启 Verbose 模式）能看到大量 `PostProcessAssembly` 条目——每条都是一次 gRPC 往返。

### 这个设计的优缺点

优点：
- `bee_backend` 和 `ILPP` 处理器完全解耦，可以独立升级
- .NET 侧的处理器可以用完整的 .NET 生态（Mono.Cecil、LINQ 等）
- 进程崩溃隔离：某个处理器崩溃不会直接杀死构建系统主进程

缺点：
- 每个 .dll 都要走一次进程间通信，有序列化/反序列化开销
- 处理器之间是串行的（按注册顺序），无法并行

---

## ILPP 对编译速度的影响

知道了 `ILPP` 的机制，编译慢的原因就清楚了。

大型项目的 asmdef 结构通常有几十到几百个程序集。**每个 .dll 都要经过一次完整的 gRPC 往返**，每次往返包括：

- 序列化 .dll 字节流
- 跨进程传输
- 所有已注册处理器依次执行
- 反序列化返回结果

Burst 的 `BurstILPostProcessor` 本身也有一定的分析开销，因为它需要扫描每个类型的所有方法，找出 `[BurstCompile]` 标记。

**常见的编译卡死场景**：某个处理器内部出现死锁或无限循环 → `bee_backend` 等待 gRPC 响应超时 → 整个编译挂起。这时候 Unity 编辑器看上去没有任何反应，但实际上是 `ILPP` 服务进程卡住了。

减少 `ILPP` 耗时的实用建议：

- 合理拆分 asmdef，把不依赖 Burst/Jobs 的程序集单独隔离，减少需要处理的 .dll 数量
- 启用 `Burst AOT` 打包，把部分 Burst 编译移到打包阶段，减轻编辑器编译压力
- 关注 `BurstCompile` 的使用范围，不必要的标记会增加 `ILPP` 扫描时间

---

## 小结

1. `ILPP`（IL Post Processing）在 Roslyn 编译完成后介入，用 Mono.Cecil 修改 .dll 里的 IL 字节码，结果对用户完全透明。
2. 相比 Source Generator、运行时反射、自定义编译器，`ILPP` 的核心优势是对用户透明、编译器无关、可做任意 IL 变换。
3. `JobsILPostProcessor` 负责在 `Execute()` 前后注入 `NativeContainer` 安全检查代码；`BurstILPostProcessor` 负责标记和准备 `[BurstCompile]` 方法的编译入口。
4. `ILPP` 处理器运行在独立的 ASP.NET Core 进程里，`bee_backend`（C++）通过 gRPC 发送 .dll、拿回处理后的版本，这是两种语言运行时天然跨进程的架构决策。
5. 每个 .dll 都要走一次 gRPC 往返，程序集数量越多，编译越慢；`ILPP` 处理器卡住会导致整个编译挂起。

---

- 上一篇：[Unity 脚本编译管线 01｜你改了一行 C#，Unity 在背后做了什么]({{< relref "engine-toolchain/unity-script-compilation-pipeline-01-overview.md" >}})
- 下一篇：[Unity 脚本编译管线 03｜Domain Reload：为什么改一行代码要等那么久]({{< relref "engine-toolchain/unity-script-compilation-pipeline-03-domain-reload.md" >}})
