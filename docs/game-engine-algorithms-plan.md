# 游戏与引擎算法系列 — 完整写作计划

> 游戏和引擎开发中特有的专用算法。系列七·D。
> 与系列七·C（通用数据结构与算法基础）互补：七·C 讲通用，七·D 讲游戏/引擎专用。
> 主计划在 [doc-plan.md § 系列七·D](../doc-plan.md) 中登记。

---

## 系列定位

**七·C 的定位**：通用数据结构与算法（任何工程都可能用到）
- 复杂度、缓存、排序、哈希、GC、基础图论、基础空间结构

**七·D 的定位**：游戏与引擎专用算法（只在游戏/仿真/图形领域才有意义）
- 物理模拟、骨骼动画、网络同步、高级寻路、游戏 AI、过程化生成、音频、引擎数学

**和系列四（Shader）的关系**：系列四讲**效果**（Shader 实现、视觉风格），七·D 讲**算法原理**（图形算法、全局光照算法等）。有交叉时以"解决什么问题"为准。

---

## 总览

- **已写**：0 篇
- **待写**：44 篇，分 9 个批次（A-A ~ A-I）
- **总计**：44 篇
- **目录**：`content/system-design/algorithms/`（或 `content/engine-algorithms/`）
- **Hugo series 标签**：`游戏与引擎算法`

---

## 每篇文章的骨架（和设计模式教科书系列完全一致）

```
1. 先看问题            为什么需要这个算法（具体场景）
2. 数学基础            涉及的核心数学原理
3. 算法推导            从问题到解法的思考过程
4. 算法实现            完整可运行的 C# / 伪代码
5. 结构图 / 流程图      Mermaid 图示（必需）
6. 复杂度分析          时间、空间复杂度
7. 变体与优化          常见变体、工程优化技巧
8. 对比其他算法         和相似算法的取舍
9. 批判性讨论          算法的局限、何时不适用、现代替代方案
10. 跨学科视角          数学/物理/信号处理/统计学的渊源
11. 真实案例           Unity/Unreal/Box2D/Bullet/Godot 源码引用（至少 2 个）
12. 量化数据           基准测试、性能数据
13. 常见坑             工程实现的坑（数值稳定性、浮点误差、边界条件）
14. 何时用 / 何时不用
15. 相关算法           本系列内链接
16. 小结              3 个关键价值点
```

---

## Batch A-A：物理模拟算法（6 篇）

| 编号 | 标题 | Slug |
|------|------|------|
| A-01 | 数值积分：Euler、Verlet、RK4 的稳定性与适用场景 | algo-01-numerical-integration |
| A-02 | 刚体动力学：力、冲量、线速度与角速度的积分 | algo-02-rigid-body-dynamics |
| A-03 | 约束求解：Sequential Impulse 与 Position-Based Dynamics | algo-03-constraint-solver |
| A-04 | 连续碰撞检测（CCD）：Time of Impact 与隧穿问题 | algo-04-continuous-collision |
| A-05 | 柔体模拟：质点弹簧与 PBD 布料 | algo-05-soft-body |
| A-06 | 流体模拟入门：SPH 平滑粒子流体力学 | algo-06-sph-fluid |

## Batch A-B：骨骼动画算法（5 篇）

| 编号 | 标题 | Slug |
|------|------|------|
| A-07 | 骨骼蒙皮：LBS 线性混合蒙皮与权重分配 | algo-07-linear-blend-skinning |
| A-08 | DQS 对偶四元数蒙皮：避免 LBS 的塌陷问题 | algo-08-dual-quaternion-skinning |
| A-09 | 旋转插值：Quaternion Slerp、Nlerp、Squad | algo-09-rotation-interpolation |
| A-10 | 逆向运动学：CCD、FABRIK、Jacobian IK | algo-10-inverse-kinematics |
| A-11 | 动画压缩：曲线简化、量化、关键帧抽取 | algo-11-animation-compression |

## Batch A-C：网络同步算法（6 篇）

| 编号 | 标题 | Slug |
|------|------|------|
| A-12 | 帧同步 vs 状态同步：两种网络模型的取舍 | algo-12-lockstep-vs-state-sync |
| A-13 | 客户端预测与服务器回滚（Rollback Netcode） | algo-13-client-prediction-rollback |
| A-14 | Snapshot Interpolation：Valve 式状态同步 | algo-14-snapshot-interpolation |
| A-15 | Delta Compression：只传变化字段 | algo-15-delta-compression |
| A-16 | 可靠 UDP 原理：KCP、QUIC、ENet 对比 | algo-16-reliable-udp |
| A-17 | 延迟补偿：Lag Compensation 与时间倒带 | algo-17-lag-compensation |

## Batch A-D：高级寻路算法（5 篇）

| 编号 | 标题 | Slug |
|------|------|------|
| A-18 | Jump Point Search（JPS）：网格 A\* 的对称性剪枝 | algo-18-jump-point-search |
| A-19 | NavMesh 原理：三角网格寻路与 Funnel 算法 | algo-19-navmesh |
| A-20 | Flow Field：RTS 大规模单位寻路 | algo-20-flow-field |
| A-21 | RVO / ORCA：多智能体避障 | algo-21-rvo-orca |
| A-22 | HPA*：层次化寻路与分层 A* | algo-22-hpa-star |

## Batch A-E：并发与调度算法（4 篇）

| 编号 | 标题 | Slug |
|------|------|------|
| A-23 | Job System 原理：数据并行与任务图 | algo-23-job-system |
| A-24 | Work Stealing 调度：Rayon/Naughty Dog Fiber | algo-24-work-stealing |
| A-25 | 无锁队列：MPMC 与 CAS 原理 | algo-25-lock-free-queue |
| A-26 | 无锁 Ring Buffer：SPSC 高性能通信 | algo-26-lock-free-ring-buffer |

## Batch A-F：游戏 AI 算法（4 篇）

| 编号 | 标题 | Slug |
|------|------|------|
| A-27 | 决策树：AI 决策的基础结构 | algo-27-decision-tree |
| A-28 | Utility AI：基于评分的决策系统 | algo-28-utility-ai |
| A-29 | GOAP：目标导向的行为规划 | algo-29-goap |
| A-30 | MCTS：蒙特卡洛树搜索（棋类 AI） | algo-30-mcts |

## Batch A-G：过程化生成（4 篇）

| 编号 | 标题 | Slug |
|------|------|------|
| A-31 | Wave Function Collapse：约束传播与图块生成 | algo-31-wave-function-collapse |
| A-32 | L-System：形式语法生成植物与分形 | algo-32-l-system |
| A-33 | Dungeon Generation：BSP、Cellular Automata、Drunkard's Walk | algo-33-dungeon-generation |
| A-34 | Poisson Disk Sampling：均匀分布采样 | algo-34-poisson-disk-sampling |

## Batch A-H：音频算法（3 篇）

| 编号 | 标题 | Slug |
|------|------|------|
| A-35 | HRTF：3D 空间音频的头部相关传输函数 | algo-35-hrtf |
| A-36 | 卷积混响：Impulse Response 与房间声学 | algo-36-convolution-reverb |
| A-37 | 多普勒效应与距离衰减：动态音源算法 | algo-37-doppler-distance-attenuation |

## Batch A-I：引擎数学深入（7 篇）

*超越七·C 的基础数学，进入游戏引擎常用的数学专题。*

| 编号 | 标题 | Slug |
|------|------|------|
| A-38 | 四元数完全指南：旋转表示、Log/Exp、奇异性 | algo-38-quaternion-deep-dive |
| A-39 | 坐标空间变换全景：Model/World/View/Projection/Clip | algo-39-coordinate-spaces |
| A-40 | 贝塞尔曲线与样条：CR、Bezier、B-Spline、NURBS | algo-40-bezier-splines |
| A-41 | 浮点精度与数值稳定性：游戏中的 Epsilon 陷阱 | algo-41-floating-point-stability |
| A-42 | Morton 编码与 Z-Order：空间填充曲线 | algo-42-morton-z-order |
| A-43 | SIMD 数学：Vector4 / Matrix4 的向量化实现 | algo-43-simd-math |
| A-44 | 视锥体与包围盒：Plane/Frustum/AABB/OBB/Sphere 测试 | algo-44-frustum-bounds-tests |

---

## 写作顺序建议

**按依赖关系和优先级：**

1. **先写引擎数学（A-I）** — 后面物理/动画/寻路都依赖它
2. **再写物理（A-A）** — 相对独立
3. **再写动画（A-B）** — 依赖四元数
4. **再写寻路（A-D）** — 独立
5. **再写并发（A-E）** — 独立
6. **再写 AI（A-F）** — 独立
7. **再写网络（A-C）** — 独立但内容量大
8. **最后过程化生成（A-G）和音频（A-H）** — 锦上添花

---

## 跨篇引用规划

### 七·D 内部交叉

- 数值积分（A-01） ↔ 约束求解（A-03）
- LBS（A-07） ↔ DQS（A-08）
- 四元数（A-38） ↔ 旋转插值（A-09）
- JPS（A-18） ↔ HPA\*（A-22） ↔ NavMesh（A-19）
- GOAP（A-29） ↔ 决策树（A-27）
- Job System（A-23） ↔ Work Stealing（A-24）
- 无锁队列（A-25） ↔ 无锁 Ring Buffer（A-26）

### 七·C ↔ 七·D 相互引用

- 七·C DS-06 Dijkstra → 七·D A-18 JPS / A-19 NavMesh（高级寻路）
- 七·C DS-11 四叉树 → 七·D A-20 Flow Field（大规模单位）
- 七·C DS-15 SAT/GJK → 七·D A-03 约束求解（窄相之后的响应）
- 七·C DS-17 LRU → 七·D A-14 Snapshot（状态缓存）
- 七·C DS-18 环形缓冲 → 七·D A-26 无锁 Ring Buffer
- 七·C DS-20 Perlin 噪声 → 七·D A-31 WFC（过程化生成）

### 和设计模式系列互相引用

- A-23 Job System → 设计模式 T-22 Actor Model
- A-13 回滚网络 → 设计模式 T-09 Command（指令可重放）
- A-29 GOAP → 设计模式 T-06 State / T-08 Chain of Responsibility

---

## 和实际工程的映射

| 算法 | 工程对应 |
|------|---------|
| 数值积分 | PhysX / Box2D / Havok / Unity Physics |
| 约束求解 | Sequential Impulse（Box2D） |
| 骨骼蒙皮 | Unity Animation / Unreal AnimBP |
| IK | Unity IK / Unreal ControlRig |
| 帧同步 | Rollcage 网络、帧同步 MOBA |
| 回滚网络 | GGPO、Rollcaster、Fortnite |
| JPS | 大地图 RPG 寻路 |
| NavMesh | Unity NavMesh / Unreal Recast |
| Flow Field | Planetary Annihilation、Supreme Commander |
| Job System | Unity DOTS Jobs、Bevy ECS |
| GOAP | F.E.A.R. AI、Horizon Zero Dawn |
| WFC | Caves of Qud、Townscaper |
| HRTF | Steam Audio、Resonance Audio |
| 四元数 | 所有 3D 引擎底层 |
| Morton | Octree 索引、GPU 排序 |

---

## 完成度追踪

- **总计**：0 / 44
- Batch A-A（物理）：0 / 6
- Batch A-B（动画）：0 / 5
- Batch A-C（网络）：0 / 6
- Batch A-D（寻路）：0 / 5
- Batch A-E（并发）：0 / 4
- Batch A-F（AI）：0 / 4
- Batch A-G（过程化生成）：0 / 4
- Batch A-H（音频）：0 / 3
- Batch A-I（数学）：0 / 7

---

## 文档位置

- **本文件**：`docs/game-engine-algorithms-plan.md`
- **主计划**：`doc-plan.md § 系列七·D`（含引用）
- **文章目录**：`content/system-design/algorithms/`（或另议）
