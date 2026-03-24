---
title: "数据结构与算法 21｜GC 通用原理与各平台实现横评：Java、.NET、iOS、Unreal"
description: "垃圾回收是每个游戏开发者都会遇到的性能瓶颈，但很少人真正理解它。这篇讲清楚 GC 的核心算法原理，然后横向对比 Android/Java ART、.NET/Unity、iOS/Swift ARC、Unreal 自实现 GC 四条路线的设计取舍，建立全局认知框架。"
slug: "ds-21-gc-overview"
weight: 781
tags:
  - 软件工程
  - 内存管理
  - GC
  - 性能优化
  - 游戏架构
series: "数据结构与算法"
---

> 同一款游戏，Android 上偶尔卡一下，iOS 上很流畅，Unity 版本偶有帧刺，Unreal 版本几乎没有——背后的原因是四套完全不同的内存管理哲学。理解它们，才能真正理解"为什么在这个平台上这样优化"。

---

## GC 要解决的问题

手动管理内存（C 的 malloc/free，C++ 的 new/delete）要求程序员精确地知道"这块内存什么时候不再需要"。这很难做对：

```
释放太早（Use-After-Free）：
  Enemy* e = new Enemy();
  delete e;
  e->TakeDamage(10);  // 访问已释放内存，行为未定义，可能崩溃

忘记释放（Memory Leak）：
  void SpawnEnemy() {
      Enemy* e = new Enemy();
      // 函数结束，e 丢失，但内存没有释放
      // 每次调用就泄漏一块内存
  }
```

**垃圾回收（GC，Garbage Collection）**：运行时自动追踪哪些内存"不再被引用"，自动释放——程序员不需要手动 free/delete。

代价是：GC 本身需要时间运行，而且这个时间往往不可预测。

---

## 核心算法一：引用计数（Reference Counting）

每个对象维护一个计数器，记录"有多少个引用指向我"。计数归零时立即释放。

```
对象 A 被 B 和 C 引用：refCount = 2

C 不再引用 A：refCount = 1

B 也不再引用 A：refCount = 0 → 立即释放 A
```

```cpp
// C++ 的 shared_ptr 就是引用计数
auto enemy = std::make_shared<Enemy>();  // refCount = 1
auto alias = enemy;                       // refCount = 2
// alias 析构：refCount = 1
// enemy 析构：refCount = 0 → Enemy 被释放
```

**优点**：释放时机确定（计数归零立刻释放），无 Stop-the-World 暂停，内存均匀释放。

**致命缺点：循环引用**

```
A 引用 B，B 引用 A：
  A.refCount = 1（B 引用它）
  B.refCount = 1（A 引用它）
  即使外部没有任何人引用 A 和 B，它们的计数永远不会归零 → 内存泄漏
```

```swift
// Swift/ObjC ARC 的循环引用典型场景
class Node {
    var next: Node?   // 强引用
    var prev: Node?   // 强引用
}
// 双向链表节点互相强引用 → 循环引用
// 解决：把其中一个改为 weak（弱引用，不增加计数）
class Node {
    var next: Node?
    weak var prev: Node?  // weak 不持有引用，计数不增加
}
```

---

## 核心算法二：标记清除（Mark and Sweep）

不维护计数，而是周期性地"找出所有可达对象，清理其余的"。

**两阶段**：

```
标记阶段（Mark）：
  从"根集合"出发（全局变量、栈上的局部变量、寄存器）
  遍历所有引用，标记所有可达对象

清除阶段（Sweep）：
  扫描整个堆，释放所有未被标记的对象

根集合 → A → B → C   （都被标记，保留）
           ↘ D        （被标记，保留）
         E → F        （E 不可达 → E、F 都未标记 → 释放）
```

**优点**：天然处理循环引用（循环但不可达的对象会被清理）。

**缺点**：Stop-the-World——标记阶段必须暂停所有应用线程（否则对象关系在标记过程中被修改，结果不正确）。这就是游戏里 GC 卡顿的根本原因。

---

## 核心算法三：三色标记（Tri-Color Marking）

把"标记清除"改造成增量或并发版本，减少暂停时间。

```
每个对象染三种颜色：
  白色：未访问（初始状态；清除阶段结束时白色对象被释放）
  灰色：已发现但子对象未全部扫描
  黑色：已完全扫描（本身 + 所有子对象）

流程：
  1. 把根集合里的对象标灰
  2. 取出一个灰色对象，把它的子对象标灰，自己变黑
  3. 重复，直到没有灰色对象
  4. 剩余白色对象 = 不可达 → 释放
```

关键改进：步骤 2 可以拆成很多小步，**分散到多帧执行**（增量 GC），每帧只做一点点标记工作，避免长时间暂停。

**写屏障（Write Barrier）**：增量/并发标记期间，应用代码还在运行修改引用关系。写屏障是一段插入在每次"写引用"操作前的代码，维护三色不变式（不允许黑色对象直接引用白色对象）：

```csharp
// 概念性写屏障（运行时自动插入，不是手写代码）
void WriteRef(object owner, ref object field, object newValue)
{
    // 如果 owner 是黑色，newValue 可能是白色，需要重新标灰
    if (IsBlack(owner) && IsWhite(newValue))
        MakeGray(newValue);  // 防止漏标
    field = newValue;
}
```

写屏障有性能开销——每次写引用都多执行几条指令。这是增量/并发 GC 的代价。

---

## 核心概念四：分代假设（Generational Hypothesis）

**观察**：大多数对象"要么很快死，要么活很久"（短命的临时变量 vs 长期存活的游戏对象）。

**分代 GC**：把堆分成"年轻代"和"老年代"，优先频繁回收年轻代（大多数垃圾在这里），减少扫描整个堆的次数：

```
年轻代（Young Generation）：新分配的对象
  Minor GC：只扫描年轻代，快（年轻代小），频繁
  大多数对象在 Minor GC 时就被回收（短命对象）

老年代（Old Generation）：经历多次 Minor GC 仍存活的对象
  Major GC / Full GC：扫描整个堆，慢，不频繁

晋升（Promotion）：年轻代存活超过 N 次 GC → 移到老年代
```

---

## 各平台实现对比

### Android / Java（ART 虚拟机）

ART（Android Runtime，Android 5.0 起替代 Dalvik）使用**分代并发标记清除**：

```
堆结构：
  年轻代（Young Space）：Eden + Survivor（两个）
  老年代（Old Space / Tenured）
  大对象空间（Large Object Space，直接进老年代）

GC 模式（ART）：
  Concurrent Copying GC（Android 8+）：
    标记和复制阶段大部分并发执行，STW 窗口极短（< 1ms）

  历史问题（Android 4 Dalvik 时代）：
    简单标记清除，STW 动辄 50~100ms
    早期 Android 游戏卡顿严重的根本原因
```

**游戏里的坑**：
```java
// 每帧 new 对象会频繁触发年轻代 GC
void Update() {
    Vector2 dir = new Vector2(dx, dy);  // 每帧分配临时对象
    // 年轻代很快填满 → Minor GC → 短暂卡顿
}
// 解决：复用对象，避免每帧分配
```

---

### .NET / Unity

.NET 使用**分代标记清除**，Unity 历史上用的是 **Boehm GC**（保守式 GC），Unity 2019+ 可选**增量 GC**（基于三色标记）。

**Boehm GC 的特殊之处**：

保守式 GC 不需要运行时提供精确的类型信息，它把内存里**所有看起来像指针的值**都当作潜在引用。

```
内存里有个值 0x12345678 → 这是个整数？还是指针？
保守式 GC：不确定，就当它是指针，指向的对象不回收
→ 可能导致少量内存泄漏（false positive），但安全

精确式 GC（.NET CLR）：有类型元数据，确切知道哪些是引用
→ 没有误判，但需要语言运行时配合
```

详见 DS-22。

---

### iOS / Swift / Objective-C（ARC）

苹果平台使用 **ARC（Automatic Reference Counting）**——编译器自动插入 retain/release 调用，本质是编译期的引用计数：

```swift
// 你写的代码
var enemy: Enemy? = Enemy()
var alias = enemy

// 编译器插入后（概念）
var enemy: Enemy? = Enemy()  // retain → refCount = 1
var alias = enemy             // retain → refCount = 2
alias = nil                   // release → refCount = 1
enemy = nil                   // release → refCount = 0 → deinit() 调用
```

**ARC 的核心优势**：**没有 Stop-the-World 暂停**。引用计数在赋值时同步更新，对象在最后一个引用消失时**立即**析构——不需要 GC 线程扫描堆。

这是 iOS 游戏流畅性普遍好于 Android 早期版本的重要原因之一。

**ARC 的主要问题**：循环引用（用 `weak` / `unowned` 打破）+ retain/release 的 CPU 开销（每次赋值都要原子操作更新计数）。

**Swift 的改进**：
- 值类型（struct、enum）直接在栈上分配，完全不需要 ARC
- 大量使用 struct 可以消除引用计数开销
- Swift 5.7+ 引入 `~Copyable` 进一步减少不必要的复制

---

### Unreal Engine（自实现 GC）

Unreal 完全不依赖语言运行时的 GC，对 `UObject` 体系自己实现了一套**标记清除 GC**：

```
为什么 C++ 项目要自己实现 GC？

C++ 没有运行时，没有托管堆，没有 GC。
但游戏对象（Actor、Component）的生命周期非常复杂：
  - 对象之间互相引用
  - 编辑器需要序列化/反序列化对象图
  - 热重载需要替换对象但保持引用关系

手动 RAII（unique_ptr / shared_ptr）管不住这么复杂的引用图
→ Unreal 选择：为 UObject 建立自己的内存管理体系
```

Unreal GC 的核心：
- `UObject` 都在 Unreal 托管堆上分配，GC 知道所有 UObject
- 定期（默认约 60 秒，或手动触发）执行标记清除
- 通过 `UPROPERTY()` 宏标记的引用参与 GC 追踪

详见 DS-23。

---

## 四种路线的对比

| | Android/ART | .NET/Unity | iOS/ARC | Unreal |
|---|---|---|---|---|
| 算法 | 分代并发标记清除 | 分代标记清除（增量可选）| 引用计数（编译期）| 标记清除（自实现）|
| STW 暂停 | 极短（Android 8+）| 有（增量可减轻）| 无 | 有（较长，不频繁）|
| 循环引用 | 自动处理 | 自动处理 | 需手动 weak | 自动处理（UPROPERTY）|
| 释放时机 | 不确定（GC 决定）| 不确定（GC 决定）| 确定（引用归零立刻）| 不确定（GC 周期）|
| 内存碎片 | GC 压缩处理 | 有（Boehm 不压缩）| 无（栈/立即释放）| 有 |
| 对游戏的影响 | 早期卡顿，现代已好 | 需要主动规避 Alloc | 流畅，需防循环引用 | GC 帧开销可感知 |
| 开发者可控性 | 低 | 中（NativeArray 绕开）| 中（weak/struct）| 高（手动控制更多）|

---

## 游戏对 GC 的共同敌人：分配频率

无论哪种 GC 方案，**减少分配频率**是最通用的优化手段：

```
GC 触发条件（各平台）：
  Android ART：年轻代填满 → Minor GC
  Unity：托管堆分配超过阈值 → GC.Collect
  Unreal：定时或手动 → UObject GC

共同规律：分配越少 → GC 越少触发 → 帧时间越稳定
```

```csharp
// 通用原则：不要在 Update / 高频路径上分配堆内存
void Update()
{
    // 坏：每帧 new，触发 GC
    var list = new List<Enemy>();

    // 好：复用缓存的 List，每帧 Clear
    cachedList.Clear();
}
```

---

## 小结

- **引用计数（ARC）**：立即释放，无暂停，但有循环引用问题和原子操作开销——iOS 的选择
- **标记清除**：自动处理循环引用，但有 STW 暂停——Java/Android 和 .NET/Unity 的基础
- **三色标记 + 增量**：把 STW 分散到多帧，写屏障有额外开销——现代 GC 的主流方向
- **分代假设**：区分短命对象和长寿对象，小 GC 频繁、大 GC 少发——所有现代托管运行时都在用
- **Unreal 自实现**：C++ 没有运行时，为 UObject 手写标记清除，代价是周期性 GC 帧
- **下一篇（DS-22）**：Unity GC 的历史演进、常见 Alloc 热点、零 GC 编程实践
- **DS-23**：Unreal UObject 体系、智能指针、两阶段销毁的完整流程
