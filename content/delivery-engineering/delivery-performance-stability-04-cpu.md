---
title: "性能与稳定性工程 04｜CPU 性能工程——GC、调度、物理与 IL2CPP"
slug: "delivery-performance-stability-04-cpu"
date: "2026-04-14"
description: "CPU 性能工程不只是'减少 Update 调用'——从交付视角看，核心是建立可预算、可度量、可门禁的 CPU 治理体系。"
tags:
  - "Delivery Engineering"
  - "Performance"
  - "CPU"
  - "IL2CPP"
series: "性能与稳定性工程"
primary_series: "delivery-performance-stability"
series_role: "article"
series_order: 40
weight: 1340
delivery_layer: "practice"
delivery_volume: "V14"
delivery_reading_lines:
  - "L1"
  - "L2"
---

## 这篇解决什么问题

V14-02 定了帧时间预算，V14-03 按设备分了档。现在进入具体的治理维度——CPU。

这一篇不是 CPU 优化手册（那些在性能工程专栏的 CPU 优化系列里有 6 篇完整覆盖）。这里从交付工程视角讲：**CPU 维度的性能应该预算什么、度量什么、在 CI 中拦截什么。**

## CPU 帧时间的构成

一帧的 CPU 耗时分布在多个系统中：

```
脚本逻辑（Update/LateUpdate/FixedUpdate）
    ↓
物理模拟（PhysX FixedUpdate）
    ↓
动画求值（Animator.Update）
    ↓
渲染提交（Culling + Batching + CommandBuffer）
    ↓
UI 布局与渲染（Canvas.BuildBatch）
    ↓
GC（不定时，但一旦触发影响大）
```

## GC 压力管理

GC（垃圾回收）是 CPU 帧时间的最大不确定因素。一次 GC 可能从 0.1ms 到 10ms+，直接导致卡顿。

### 分配来源

| 来源 | 典型分配量 | 治理方式 |
|------|-----------|---------|
| 字符串拼接 | 每次 `"a" + "b"` 都分配 | StringBuilder / string.Format |
| LINQ 查询 | 闭包 + 迭代器分配 | 显式循环替代 |
| 装箱 | 值类型转 object | 泛型替代 |
| 容器扩容 | List/Dictionary 扩容时分配 | 预分配容量 |
| 委托创建 | 每次 `new Action(...)` | 缓存委托实例 |
| 协程 | `yield return new WaitForSeconds()` | 缓存 YieldInstruction |

### GC 预算

| 设备档位 | 每帧分配预算 | 说明 |
|---------|------------|------|
| 高档 | ≤1 KB/帧 | 增量 GC 可消化 |
| 中档 | ≤512 B/帧 | 减少 GC 触发频率 |
| 低档 | ≤256 B/帧 | 低内存设备 GC 更频繁 |

**度量方式**：`Profiler.GetMonoUsedSizeLong()` 逐帧记录，计算每帧分配增量。CI 中自动化跑测时采集。

### 对象池

对象池是减少 GC 分配的核心手段：

| 池化对象 | 说明 |
|---------|------|
| 子弹、特效、伤害数字 | 战斗中高频创建销毁 |
| UI 列表 Item | 滚动列表中复用 |
| 网络消息对象 | 避免每次反序列化都分配 |
| 临时容器 | `List<T>` 的租借/归还 |

## Update 调度优化

Unity 的 `Update()` 是最大的 CPU 消耗来源之一——不是因为单次调用慢，而是因为数量多。

### 问题

```
1000 个 MonoBehaviour 都有 Update()
→ 每帧 1000 次 C# → C++ 的调用开销
→ 即使每个 Update 只做 if 判断，累计开销也有 1-2ms
```

### 治理方式

| 方式 | 说明 | 适用场景 |
|------|------|---------|
| 手动 Tick 管理 | 用一个 Manager 统一驱动所有逻辑 | 大量同类对象 |
| 分帧执行 | 不是每个对象每帧都 Update | AI、寻路等非实时逻辑 |
| 距离分频 | 离相机远的对象降低 Update 频率 | 开放世界 |
| 事件驱动 | 状态变化时才执行，不用轮询 | UI 更新、条件检查 |

### 度量指标

| 指标 | CI 检查方式 |
|------|-----------|
| Update 调用次数 | Profiler 采集 `MonoBehaviour.Update` 调用量 |
| 脚本逻辑帧时间 | Profiler 采集 `ScriptRunBehaviourUpdate` 耗时 |

## 物理优化

物理模拟（PhysX）是另一个 CPU 大户，特别在战斗场景中。

| 优化方向 | 说明 |
|---------|------|
| 碰撞体简化 | MeshCollider → BoxCollider/SphereCollider |
| Layer Matrix | 只检测需要碰撞的 Layer 对 |
| FixedUpdate 频率 | 从 50Hz 降到 30Hz（`Time.fixedDeltaTime = 0.033f`） |
| 射线检测 | NonAlloc 版本替代分配版本 |
| 休眠策略 | 静止物体自动休眠，减少模拟量 |

**物理预算**：战斗场景中物理模拟的帧时间不应超过总 CPU 预算的 10-15%。

## IL2CPP vs Mono 性能特征

| 维度 | Mono | IL2CPP |
|------|------|--------|
| 执行速度 | JIT 编译，首次调用有编译开销 | AOT 编译，执行速度接近原生 C++ |
| GC | Boehm GC，暂停时间较长 | 同样使用 Boehm GC |
| 启动时间 | 较快（不需要 AOT 编译） | 较慢（需要加载更大的二进制） |
| 泛型 | 运行时 JIT 生成 | 构建时 AOT 生成，未覆盖的需要解释执行 |
| 内存 | 托管堆 + JIT 代码缓存 | 托管堆 + 原生代码段 |

**交付影响**：

- IL2CPP 是移动端发布的标配（iOS 强制要求，Android 推荐）
- IL2CPP 的 AOT 编译使得构建时间更长 → 影响 CI 效率
- IL2CPP 的 Stripping 可能裁掉运行时需要的类型 → 需要 link.xml 保护
- HybridCLR 热更新的代码在解释器中执行，性能低于 AOT → 热更代码不宜有重计算逻辑

## CPU 性能作为 CI 门禁

| 门禁项 | 数据来源 | 拦截条件 |
|--------|---------|---------|
| 帧时间回归 | 自动化性能跑测 | 主场景帧时间退化 > 10% |
| GC 分配回归 | Profiler 自动化采集 | 每帧分配量退化 > 20% |
| Update 调用数增长 | Profiler 统计 | 新增 Update 超过阈值 |
| 脚本编译时间 | 构建日志 | 编译时间退化 > 15% |

**关键原则**：CI 不需要检测所有 CPU 问题——只检测回归。绝对值的问题由人工 Profiler 定位，CI 负责的是"不让指标变差"。

## 小结与检查清单

- [ ] 是否有每帧 GC 分配预算
- [ ] 高频分配源（字符串拼接、LINQ、装箱）是否已治理
- [ ] 是否有对象池覆盖高频创建/销毁的对象
- [ ] 是否评估了 Update 调用数量并有缩减计划
- [ ] 物理碰撞的 Layer Matrix 是否正确配置
- [ ] 是否理解 IL2CPP 的性能特征和交付影响
- [ ] CI 是否有帧时间和 GC 分配的回归检测

---

**下一步应读**：[GPU 性能工程]({{< relref "delivery-engineering/delivery-performance-stability-05-gpu.md" >}}) — CPU 之后看 GPU：Draw Call、带宽、Shader 和移动端特殊考量

**扩展阅读**：CPU 优化系列（6 篇）— GC 分析、调度优化、物理优化的完整技术深挖
