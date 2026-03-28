# 数据导向工程深度系列规划

> 本文件是 `data-oriented-runtime-series-plan.md`（已写完的 DOD-00~06 架构哲学层）的**下一层计划**。
> 那一组 7 篇解决的是"为什么这类系统会长成这样"；这一组解决的是"怎样真正用它、读懂它、判断它"。

---

## 系列定位

这组文章分四个层次，缺一不可：

1. **硬件基础层**：搞清楚 cache miss / SIMD / 内存带宽的真实代价，才能理解为什么 SoA、Chunk、Burst 会有效果。没有这层，"cache-friendly"只是咒语。
2. **DOTS 工程层**：Unity Entities 1.x 的完整工程实践，从 ECS 核心到 Baking、Jobs、Burst、边界管理。
3. **Unreal Mass 深度层**：Mass 和 DOTS 是两种不同答案，并列讲才能真正理解各自的取舍。
4. **行业横向对比层**：Overwatch、id Tech、Flecs/EnTT——看清楚 ECS 架构价值和性能价值可以分开。

**一句话定位**：从硅到框架，把数据导向彻底讲透，让读者能做工程判断，而不只会背 API。

---

## 目标读者

- 已经读完 `data-oriented-runtime-series-plan.md`（DOD-00~06），想进入工程层的人
- 在项目里考虑引入 DOTS 或 Mass，需要做技术选型判断的技术负责人
- 想理解"Burst 为什么快"背后硬件原理的渲染/引擎开发者
- 对 ECS 有概念但从没写过完整的 Query / Baking / NativeCollection 代码的人

---

## 基线版本

| 引擎/框架 | 基线版本 |
|-----------|---------|
| Unity DOTS | Entities 1.x（Unity 6），Burst 1.8.x，Collections 2.x |
| Unreal Mass | UE 5.4 / 5.5 MassEntity + MassGameplay |
| Flecs | 4.x（用于行业对比章节） |
| 硬件基础 | x86-64（Intel/AMD）+ ARM64（Apple Silicon / Adreno / Mali），以实测数字为锚 |

---

## 上游依赖

阅读本系列之前，读者需要先通过：

- `data-oriented-runtime-series-plan.md` 的 DOD-00~06（架构哲学层，已全部写完）
- 零·C：计算机基础——进程、线程、虚拟内存模型（本系列硬件层的前置概念）

本系列内部的依赖顺序：硬件基础 → DOTS 工程 → Mass 深度 → 行业横向对比

---

## Part 1：硬件基础——为什么数据布局决定性能

> 这 6 篇独立成一个子系列，**也是整个技术专栏"系列一·F"的补充层**。
> 它们不只服务于 DOTS/Mass，同样是 GPU 优化、Shader 优化、服务端高性能开发的底座。

| 编号 | 标题 | 核心问题 |
|------|------|---------|
| 硬件-F01 | CPU 流水线与乱序执行：一条指令怎样被执行，分支预测为什么有代价 | 为什么热路径里的 if 昂贵，为什么数据驱动可以规避 |
| 硬件-F02 | Cache 体系全景：L1/L2/L3 延迟数字、cache line 64B、prefetch 机制 | Chunk 为什么是 16KB，SoA 比 AoS 快在哪 |
| 硬件-F03 | SIMD 指令集：SSE2 / AVX2 / AVX-512 / NEON，向量宽度的演进史 | Burst 背后在做什么，为什么同样的 C# 代码快 4~8 倍 |
| 硬件-F04 | 内存带宽与延迟：LPDDR5 vs DDR5，UMA 架构，带宽瓶颈在哪里 | 移动端 DOD 的约束与桌面的本质差异 |
| 硬件-F05 | 多核与并行陷阱：False Sharing、Memory Ordering、原子操作代价 | Job Safety System 为什么要这样设计 |
| 硬件-F06 | 数据布局实战：AoS vs SoA vs AoSoA，对齐、填充、stride 的实测对比 | 用 BenchmarkDotNet / perf 跑出真实数字，不靠直觉 |

---

## Part 2：Unity DOTS 工程实践

> 18 篇，覆盖从"第一行 ECS 代码"到"能处理边界问题的工程能力"。
> 不是 API 手册，每篇聚焦一个真实的工程决策点。

### 2.1 入门：世界观切换（2篇）

| 编号 | 标题 | 核心问题 |
|------|------|---------|
| DOTS-E01 | 从 GameObject 到 Entity：数据模型的本质转变，什么该留在 OOP | 不是"DOTS 好 OOP 坏"，而是它们擅长的场景不同 |
| DOTS-E02 | 第一个完整 ECS 程序：World、EntityManager、IComponentData、ISystem 的最小组合 | 先走通一遍，再讲为什么这样设计 |

### 2.2 ECS 核心（6篇）

| 编号 | 标题 | 核心问题 |
|------|------|---------|
| DOTS-E03 | SystemBase vs ISystem：两种写法的本质差异与选择依据 | Managed vs Unmanaged 边界对系统设计的影响 |
| DOTS-E04 | EntityQuery 完整语法：过滤器、变更检测、EnabledMask、缓存 | Query 为什么要缓存，变更检测的实现原理 |
| DOTS-E05 | ComponentLookup 与随机访问：在 Job 里安全地查别的 Entity | 为什么随机访问打破了 SoA 的优势，什么时候值得 |
| DOTS-E06 | IBufferElementData：动态缓冲区替代 List\<T\> 的时机与写法 | Buffer 内存如何存在 Chunk 里，容量超限时发生什么 |
| DOTS-E07 | ISharedComponentData：分组的代价与用途，Chunk 碎片化的风险 | 为什么 Shared Component 值改变会触发 Archetype 迁移 |
| DOTS-E08 | Enableable Component：不改 Archetype 的开关方案及其代价 | EnabledMask 在 Query 里怎样工作，适合替代哪类 Tag |

### 2.3 Baking 系统（3篇）

| 编号 | 标题 | 核心问题 |
|------|------|---------|
| DOTS-E09 | Baking Pipeline 全景：Authoring → Baker → Runtime Data，为什么需要这一层 | 构建期前移的本质是把不确定性消灭在离线阶段 |
| DOTS-E10 | SubScene 与流式加载：大世界的内容单元、生命周期与内存管理 | SubScene 和传统 AddressableScene 的根本区别 |
| DOTS-E11 | Blob Asset：只读数据的高效打包、引用计数与访问方式 | 为什么 Blob 不能包含托管引用，序列化边界在哪 |

### 2.4 Jobs 与 Burst（3篇）

| 编号 | 标题 | 核心问题 |
|------|------|---------|
| DOTS-E12 | IJobEntity vs IJobChunk vs IJob：三种 Job 的适用边界与性能差异 | 什么时候用哪种，Chunk 级 Job 能做哪些 IJobEntity 做不到的事 |
| DOTS-E13 | Burst 编译规则全景：什么代码能过、限制来自哪里、常见报错原因 | Burst 不是魔法，它是受约束的 LLVM，理解约束才能用好它 |
| DOTS-E14 | NativeCollection 选型：Array / List / HashMap / Queue / MultiHashMap / Stream | 每种容器的内存布局、分配器选择与 Dispose 时机 |

### 2.5 进阶与边界（3篇）

| 编号 | 标题 | 核心问题 |
|------|------|---------|
| DOTS-E15 | EntityCommandBuffer：延迟结构变更的正确用法、并发 ECB 与常见踩坑 | ECB 不是异步，它是"结构变更要在同步点外批量提交"的机制 |
| DOTS-E16 | Entities.Graphics：Hybrid Renderer、MaterialMeshInfo、GPU Instancing 与 Mesh 替换 | 渲染端怎样接入 ECS 世界，为什么不能直接用 Renderer 组件 |
| DOTS-E17 | MonoBehaviour ↔ ECS 边界：Managed 与 Unmanaged 世界的数据传递模式 | 没有纯 ECS 项目，边界设计决定了混合架构的可维护性 |

### 2.6 调试与性能分析（1篇）

| 编号 | 标题 | 核心问题 |
|------|------|---------|
| DOTS-E18 | DOTS 调试工具全景：Entities Hierarchy、Chunk Utilization、Job Debugger、Burst Inspector | 光会写不够，能读懂 Profiler 里的 ECS 数据才算真正掌握 |

---

## Part 3：Unreal Mass 深度

> 7 篇，不是 Mass 的 API 手册，而是和 DOTS 并列对照，讲清楚两套系统在同一问题上的不同答案。

| 编号 | 标题 | 核心问题 |
|------|------|---------|
| Mass-01 | Mass Framework 架构全景：Fragment、Tag、Trait、EntityHandle、EntityManager | 和 DOTS 概念的对照表——相同问题，不同命名与粒度 |
| Mass-02 | UMassProcessor 执行模型：Query、依赖声明、Pipeline、ExecutionFlags | Mass 的调度为什么比 DOTS System 更"自动"，代价是什么 |
| Mass-03 | Mass Structural Change：FMassCommandBuffer、Deferred Add/Remove、Flush 时机 | 同一个问题（结构变更贵），Mass 怎样处理 |
| Mass-04 | Mass LOD：Fragment 分级激活、FMassLODFragment、距离驱动的精度切换 | 这是 Mass 比 DOTS 内置得更完整的地方 |
| Mass-05 | Mass Signals：跨 Entity 的异步事件机制，解决纯 DOD 里的突发事件问题 | DOTS 没有直接对应物，这是 Mass 架构上有意识的补充 |
| Mass-06 | Mass 与 Actor 世界的边界：Representation Fragment、ISM、Niagara、LOD 联动 | 为什么 Mass 比 DOTS 更容易和现有 Unreal 项目混用 |
| Mass-07 | Mass 实战案例拆解：City Sample 人群 + Mass Traffic 的架构决策 | 从官方案例反推设计动机，比读文档更有价值 |

---

## Part 4：行业横向对比

> 4 篇，让读者在 DOTS 和 Mass 之外，看见这个问题空间的全貌。

| 编号 | 标题 | 核心问题 |
|------|------|---------|
| DOD-行业-01 | Overwatch ECS（GDC 2017）：为什么 ECS 的架构价值和性能价值可以分开 | Blizzard 的 ECS 是 Managed 的，目标是逻辑隔离而不是 cache-friendly |
| DOD-行业-02 | id Tech 7 / DOOM Eternal：不用 ECS 框架，用 Job Graph 手工管数据流 | 证明 DOD 不等于必须有 ECS 框架 |
| DOD-行业-03 | Flecs 与 EnTT：服务端与跨平台独立 ECS，Minecraft Bedrock 为什么选 EnTT | 客户端引擎的 ECS 之外，服务端 ECS 的问题和取舍 |
| DOD-行业-04 | 选型决策地图：DOTS / Mass / 自研 / Flecs，什么项目该选哪条路 | 不同游戏类型、团队规模、引擎绑定程度下的判断框架 |

---

## Part 5：实战案例

> 3 篇，把前面所有内容收回到真实工程问题。

| 编号 | 标题 | 核心问题 |
|------|------|---------|
| DOD-案例-01 | 大规模单位调度（RTS）：ECS + Jobs 完整实现，从 1000 到 100000 单位的扩展路径 | Archetype 设计、Query 优化、LOD 触发、渲染端接入 |
| DOD-案例-02 | 弹幕系统（5000+ 子弹）：碰撞检测、生命周期、VFX 同步的 ECS 实现 | 这是最常见的 DOTS 入门案例，但深度实现涉及大量边界问题 |
| DOD-案例-03 | 混合架构设计：ECS 仿真层 + GameObject 表现层的稳定边界策略 | 没有纯 ECS 项目，真实项目永远是混合的，边界怎样设计才能长期维护 |

---

## 总体篇数

| Part | 内容 | 篇数 |
|------|------|------|
| Part 1 | 硬件基础 | 6 |
| Part 2 | Unity DOTS 工程 | 18 |
| Part 3 | Unreal Mass 深度 | 7 |
| Part 4 | 行业横向对比 | 4 |
| Part 5 | 实战案例 | 3 |
| **合计** | | **38 篇** |

加上已完成的 `DOD-00~06`（架构哲学层 7 篇），整个数据导向主题共 **45 篇**。

---

## 与其他系列的关系

| 关联系列 | 关系说明 |
|---------|---------|
| `data-oriented-runtime-series-plan.md`（DOD-00~06） | 本系列的上游，架构哲学层，已全部写完 |
| 系列一·F（硬件基础 Part 1）| Part 1 的 6 篇同时挂在"系列一·底层基础"下，对其他系列也有价值 |
| 系列三（移动端 GPU/CPU 优化） | 硬件-F02（Cache）、硬件-F04（内存带宽）与移动端优化深度交叉 |
| 系列十一·B（GAS）| Mass-06 涉及 Mass 与 GAS 在大规模 AI 场景下的协作边界 |
| 系列十三（游戏后端） | DOD-行业-03（Flecs/EnTT）与服务端高性能架构有直接关联 |
| 系列十四（引擎架构自研） | DOD-案例-03 和系列十四的 ECS/场景图设计高度重叠 |

---

## 写作约束

继承自 `data-oriented-runtime-series-plan.md`，额外补充：

- **硬件层必须有实测数字**：不能只说"cache miss 贵"，必须给出量级（如"L3 miss ≈ 200 cycle vs L1 hit ≈ 4 cycle"）
- **DOTS 层每篇必须有可运行代码示例**：读者应能直接验证文中结论
- **Mass 层必须有 DOTS 对照**：每篇 Mass 文章都应在同一问题上对比 DOTS 的处理方式
- **行业对比层不写成"谁更好"**：结论必须落在"在什么约束下，哪条路更合理"

---

## 推荐推进顺序

### 第一阶段（硬件基础，优先级最高）

先写 Part 1 的 6 篇，原因：
- 它们是所有后续文章的理解底座
- 独立阅读价值高，不依赖 DOTS/Mass 知识
- 可以立即复用到移动端优化、Shader 优化系列

推荐顺序：硬件-F02（Cache，最核心）→ 硬件-F03（SIMD）→ 硬件-F01（流水线）→ 硬件-F06（布局实战）→ 硬件-F05（多核陷阱）→ 硬件-F04（带宽）

### 第二阶段（DOTS 工程主干）

Part 2 按编号顺序推进，但可以跳过 E10/E11（SubScene/Blob）先写 E12~E14（Jobs/Burst），因为后者阅读需求更迫切。

### 第三阶段（Mass + 行业对比）

Mass-01 → Mass-02 → Mass-03 → DOD-行业-01 → DOD-行业-04，其余按需补充。

### 第四阶段（实战案例）

等 Part 2 和 Part 3 主干写完后再写案例，否则案例会变成 API 演示而不是工程决策展示。

---

## 当前状态

| Part | 状态 |
|------|------|
| 上游 DOD-00~06 | ✅ 已全部完成 |
| Part 1 硬件基础 | ❌ 全部待写 |
| Part 2 DOTS 工程 | ❌ 全部待写 |
| Part 3 Unreal Mass | ❌ 全部待写 |
| Part 4 行业对比 | ❌ 全部待写 |
| Part 5 实战案例 | ❌ 全部待写 |

---

## 最后压成一句话

读完这 38 篇，读者应该能回答：`一个高规模对象仿真问题，在 Unity、Unreal 和自研三条路上，从 CPU 微架构到框架设计，它的代价分别落在哪，我的项目该选哪条。`
