# Unity DOTS P01｜详细提纲：DOTS 里的物理世界到底怎样运转

## 本提纲用途

- 对应文章：`DOTS-P01`
- 本次增量类型：`详细提纲`
- 上游资料：
  - `docs/unity-dots-follow-up-batch-1-editorial-workorders.md`
  - `docs/unity-dots-follow-up-shared-glossary.md`
  - `docs/unity-dots-follow-up-article-template.md`
- 本篇定位：`DOTS Physics` 子系列入口篇，先立世界地图，不展开组件字段和 API 细节

## 文章主问题与边界

- 这篇只回答：`在 DOTS 里，物理世界和 ECS 世界到底怎样接起来。`
- 这篇不展开：`Collider / PhysicsBody / PhysicsMass 的字段拆分`
- 这篇不展开：`Raycast / ColliderCast / DistanceQuery 的具体选型`
- 这篇不展开：`CollisionEvents / TriggerEvents 的回写边界`
- 这篇不展开：`Character Controller 的实现策略`
- 本篇允许落下的判断强度：`先给出世界地图、执行主链和包定位，再把后续文章要处理的问题挂回这张地图。`

## 一句话中心判断

- `DOTS 里的物理不是 Rigidbody 的平移版，而是一套挂在 ECS 调度链上的独立 Physics World；先把这个世界地图立住，后面的 Query、Events、Baking 和 Character Controller 才不会写成碎片。`

## 行文顺序与字数预算

| 正文部分 | 目标字数 | 本段任务 |
| --- | --- | --- |
| 1. 这篇为什么存在 | 300 - 450 | 先拆掉“DOTS 物理 = Rigidbody 换皮”的直觉 |
| 2. ECS World 与 Physics World 的关系 | 700 - 900 | 讲清两个世界分别负责什么 |
| 3. Unity Physics 与 Havok Physics 各站哪一层 | 500 - 700 | 说明两条路线的定位和取舍 |
| 4. 固定步长、系统顺序与物理主链 | 700 - 900 | 用 Build / Step / Export 立住执行链 |
| 5. 常见误读为什么会反复出现 | 350 - 500 | 收掉几个最典型的误区 |
| 6. 这张地图决定后面几篇怎么读 | 250 - 400 | 把 P02~P07 的分工接出来 |

## 详细结构

### 1. 这篇为什么存在

- 开篇先写读者最容易带进来的旧直觉：
  - `有 Rigidbody / Collider / Trigger，所以 DOTS 物理应该只是 ECS 包装`
  - `既然有 DOTS，物理应该天然就跟 ECS 查询混在一起`
- 然后指出真正的问题：
  - DOTS 的难点不是“有没有物理组件”
  - 而是“物理世界到底怎样嵌进 ECS 调度链”
- 本节要完成的动作：
  - 立住唯一主问题
  - 说明这篇不教 API，不写角色控制器教程
  - 给后文埋下 `Physics World` 这个核心对象

### 2. ECS World 与 Physics World 的关系

- 先定义两个世界：
  - `ECS World`：Entity、System、调度边界
  - `Physics World`：物理构建、步进、导出和查询的工作世界
- 这节必须讲清的判断：
  - 物理系统不是“又一组 ECS Component”，它有自己的构建与步进链
  - DOTS 物理真正麻烦的地方，是两个世界如何同步而不是 API 名称
- 这一节要有一张最小链路图：
  - Authoring / Baking -> ECS 数据
  - ECS 系统组 -> Physics Build / Step
  - 结果导出 / 事件读取 / 回写
- 这一节不要做的事：
  - 不提前讲 Collider、Mass 字段布局
  - 不展开事件怎么回写结构变更

### 3. Unity Physics 与 Havok Physics 各站哪一层

- 先强调两者不是“两个完全不同的世界模型”
- 要压出的最短判断：
  - 两者共享大体的 ECS 接入方式
  - 但在运行时能力、性能目标、稳定性和适用场景上不等价
- 建议写成三段：
  - Unity Physics 解决什么
  - Havok Physics 解决什么
  - 项目里怎么判断何时值得走更重的一条路
- 这里允许用对照表：
  - 世界接入方式
  - 工程代价
  - 典型适用场景

### 4. 固定步长、系统顺序与物理主链

- 这是全文的主干段落
- 必须讲清：
  - 为什么 Physics 要放在固定步长里
  - Build / Step / Export 这条链各做什么
  - 为什么“看起来只是几组系统”，本质上却是世界间的同步点
- 这里可以放一段最小伪代码或执行顺序图：
  - 先准备输入
  - 再构建 Physics World
  - 再推进一步
  - 再把结果暴露给后续系统
- 这一节要给出一个明确边界：
  - 如果读者还没分清系统顺序，后面 Query / Events / Character Controller 一定会写乱

### 5. 常见误读为什么会反复出现

- 至少拆 3 个误解：
  - 误解 1：有物理组件就等于已经理解 DOTS 物理
  - 误解 2：Query 和 Physics Query 是一回事
  - 误解 3：只要能读到物理结果，就可以立刻改结构
- 每个误解都只点问题，不在这篇里展开修法
- 作用是把后续几篇的存在理由说明白

### 6. 这张地图决定后面几篇怎么读

- 用 1 小段收束后续路径：
  - `P02`：数据模型拆分
  - `P03`：Physics Query
  - `P04`：事件回写
  - `P06`：Baking
  - `P05`：Character Controller
  - `P07`：调试与性能
- 文末最后一句要压成：
  - `先立世界地图，再谈具体 API；否则你看到的只会是名字相似的零件，不是能工作的物理系统。`
