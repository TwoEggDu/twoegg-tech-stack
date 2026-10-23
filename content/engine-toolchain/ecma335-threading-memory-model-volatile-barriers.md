---
title: "ECMA-335 进阶｜线程内存模型：volatile、原子操作与屏障"
slug: "ecma335-threading-memory-model-volatile-barriers"
date: "2026-04-15"
lastmod: "2026-10-23"
description: "按需理解跨线程可见性、原子性和顺序约束，区分 CLI volatile 语义、运行时实现与目标平台验证。"
weight: 20
series: "dotnet-runtime-ecosystem"
series_id: "ecma335"
tags: ["ECMA-335", "IL2CPP", "HybridCLR", "Threading", "MemoryModel"]
---

> **阅读位置**：原 A12 改为并发进阶分支。它不阻塞 metadata、泛型和解释执行主线；遇到跨线程共享状态时再读。GC 写屏障与本文的线程内存屏障不同，前者见 [GC 契约]({{< relref "engine-toolchain/ecma335-memory-model-object-layout-gc-contract-finalization.md" >}})。

一个线程先填好数据，再写入 ready 标志；另一个线程看到 ready 后读取数据。源码中的先后顺序，怎样成为另一个线程能够依赖的观察顺序？这才是内存模型要回答的问题。

## 三个问题不能混在一起

| 问题 | 含义 |
|---|---|
| 原子性 | 单次操作是否会被观察为部分完成 |
| 可见性与顺序 | 一次发布与后续读取之间建立什么约束 |
| 复合操作正确性 | 读、修改、写这一组动作能否被其他线程插入 |

一个字段读取具有原子性，不代表依赖它的多字段协议就正确；给字段加 volatile，也不会让 count++ 自动成为原子的读改写。

## volatile 的 acquire／release

CLI 的 volatile 读取具有 acquire 语义，volatile 写入具有 release 语义：

- acquire 限制后续相关内存访问越过这次读取。
- release 限制前面的相关内存访问越过这次写入。

这不是“刷新所有 CPU 缓存”，也不是“普通字段默认都有 acquire／release”。对普通访问、库 API 和 runtime 的额外保证，要分别看对应契约。

下面只演示一次发布，不允许第二次重置 ready，也不允许发布后继续并发修改 payload：

```csharp
using System.Threading;

public sealed class OneShot
{
    private int payload;
    private int ready;

    public void Publish()
    {
        payload = 42;
        Volatile.Write(ref ready, 1);
    }

    public bool TryRead(out int value)
    {
        if (Volatile.Read(ref ready) == 0)
        {
            value = 0;
            return false;
        }
        value = payload;
        return true;
    }
}
```

当读取观察到这次 release 发布的值时，协议才能据此使用前面准备的数据。它不是可复用队列，也不保证读者在某个时间内一定被调度到。若需要反复发布、多个写者或复合状态，应重新设计同步协议。

## Interlocked、lock 与屏障

Interlocked 的适用操作提供原子的读改写；lock 为临界区提供互斥与相应的同步语义。Thread.MemoryBarrier 约束内存操作排序，但不会单独把多步计算变成原子操作。

优先根据协议选择机制：单次状态发布、竞争更新计数器、同时修改多个字段，需求不同。不要为了避开 lock，就把几个普通字段加上 volatile 后称为无锁算法。

具体 API 保证见 Microsoft [Volatile](https://learn.microsoft.com/en-us/dotnet/api/system.threading.volatile)、[Interlocked](https://learn.microsoft.com/en-us/dotnet/api/system.threading.interlocked)。CLI 的基础承诺与 CoreCLR 的额外实现保证也应分开，后者可查 [dotnet/runtime 内存模型说明](https://github.com/dotnet/runtime/blob/main/docs/design/specs/Memory-model.md)。

## x86 上没复现，不等于协议正确

硬件排序只是链路中的一层，还存在语言、IL 转换和编译优化。即使某个平台上的 volatile 不需要额外硬件 fence，也不能把这次访问从程序里删掉。

在典型 x86 TSO 描述中，值得注意的是较早的 store 与较后的不同地址 load 的观察顺序可能受 store buffer 影响，不能写反成“只有 load → store 会重排”。但做工程判断时，应从所用语言和 runtime 的保证出发，不能只凭一张 CPU 指令表判断 C# 程序。

## IL2CPP 与 HybridCLR 的边界

AOT 编译路径和解释路径都需要保留受支持的 volatile、同步与原子操作语义。具体使用哪条指令、哪种 helper、是否能够合并检查，取决于 runtime 版本、目标平台和编译配置。

因此这里不固定宣称 IL2CPP 一律生成某种 std::atomic 表达式，或解释器一定比 AOT 多几十倍成本。若分析一个热点，要查看对应版本产物并测量；若分析正确性，要先说明共享状态和同步关系。

同样，Unity 编辑器不总是 x86，Player 也不总是 ARM。记录实际架构与脚本后端，才能解释测试差异。

## 如何验证一个并发假设

1. 写清共享字段、读写线程、发布动作和允许的观察结果。
2. 检查每条跨线程依赖由什么同步关系保障。
3. 在编辑器、目标 IL2CPP Player、实际 HybridCLR 路径分别测试，记录架构与版本。
4. 压力测试用于发现反例；长时间未复现不能独立证明协议正确。

规范入口：[ECMA-335](https://ecma-international.org/publications-and-standards/standards/ecma-335/) Partition I §12.6，尤其 volatile 读写的语义。

**先证明共享状态的同步关系，再选择指令和优化；某台机器暂时跑得通只是测试观察。**

## 返回主线

- [类型加载与初始化]({{< relref "engine-toolchain/ecma335-type-loading-and-initialization.md" >}})
- [ECMA-335 索引]({{< relref "engine-toolchain/ecma335-series-index.md" >}})
