# 自研 ECS / 数据导向运行时实现计划

## 这份计划解决什么

前面的 `docs/data-oriented-runtime-series-plan.md` 已经回答了“这组文章写什么”。

这份计划专门回答另一件事：

`如果我们真的要自己实现一套最小数据导向运行时，第一版到底先做什么，后做什么，哪些事情故意不做。`

它不是“如何复刻 Unity DOTS”的计划。
也不是“照着 Unreal Mass 抄一个插件”的计划。

它真正想做的，是把文章里已经反复确认的几个不变量，压成一条可以落代码的实现路线。

## 总目标

第一版只证明 4 件事：

1. 大量同构对象能否按结构稳定存进 `archetype + chunk`。
2. 查询能否命中“结构集合”，而不是每次从零扫所有实体。
3. 结构变化能否通过延迟提交稳定完成，而不是把世界随时打碎。
4. 逻辑执行能否先以显式 phase 站住，再逐步演化到更复杂调度。

如果这 4 件事没站住，后面再谈并行、代码生成、复制、可视化编辑器，基本都会变成堆复杂度。

## 不要把目标写错

这份计划里最重要的一条约束是：

`我们要实现的是“最小数据导向运行时”，不是“开源版 DOTS”。`

所以第一版故意不追求下面这些东西：

- 完整编辑器工作流
- 自动代码生成
- 复杂并行调度
- 网络复制
- 反射型自动序列化
- 热更新
- “Burst 级”编译器优化
- 完整渲染、动画、物理桥接

这些能力当然都重要。
但它们不该出现在第一阶段。

## 参考系

实现时要借鉴 `Unity DOTS` 和 `Unreal Mass`，但只借结构，不借外观。

建议固定 3 条参考原则：

1. 向 `Unity DOTS` 借 `authoring/runtime separation`、`archetype + chunk` 和 query 思维。
2. 向 `Unreal Mass` 借 `混合架构边界`、`representation bridge` 和 `deferred command` 的现实感。
3. 自研时不抄命名，不追求兼容 API，只看结构问题有没有被解决。

换句话说：

- 可以学习它们为什么长成这样
- 不要为了“看起来像”去复制它们的外壳

## 仓库与载体建议

当前仓库是内容站点。

所以更合理的做法是：

- 这份文档继续放在当前内容仓库里
- 真正的实现代码放到单独的 demo / experiment 工程里

原因很简单：

- 内容仓库适合沉淀判断和计划
- 代码仓库适合放测试、基准、实验数据和迭代实现

如果后面你决定正式开实现，我建议直接单开一个最小 demo 仓库，而不是把实验代码塞进这个站点仓库里。

## 第一版的最小结构

如果把第一版压成最小系统，我会把边界定成下面 9 个部分。

### 1. `World`

负责承载：

- entity 槽位表
- archetype 表
- query 缓存表
- command buffer
- phase scheduler

原则：

- 一个 `World` 就是一套完整实体空间
- 任何 entity id 只在自己的 world 里有效

### 2. `EntityId(index + generation)`

第一版就固定成：

- `index`
- `generation`

需要满足：

- 槽位可复用
- 实体删除后旧 id 失效
- 任何 location lookup 都能先做有效性检查

### 3. `ComponentTypeId`

第一版不需要复杂反射。

只要做到：

- 每种组件类型有稳定的 type id
- 能拿到 size / alignment / category
- 能参与 archetype signature 构建

建议一开始就预留 3 类分类：

- `Regular Component`
- `Tag`
- `Shared / Chunk-level Data`

即使第一版只完整实现第一类，结构上也要把另外两类留出位置。

### 4. `Archetype`

archetype 的 key 就是组件类型集合。

它至少要负责：

- 维护 signature
- 挂接自己的 chunk 列表
- 维护每种组件在 chunk 中的列偏移信息
- 暴露给 query 做结构匹配

原则：

- 相同结构的实体一定进入同一个 archetype
- 结构变化一定表现为 archetype 迁移

### 5. `Chunk`

第一版直接用“固定大小数据页”思路。

不必一开始就执着于 Unity 的 `16KiB`，但必须满足：

- chunk 容量固定
- 每个组件列在 chunk 中连续排布
- 同一行索引能对应同一个实体的各列数据
- chunk 能知道当前 entity count / capacity
- 删除实体时支持补洞

建议 chunk 内至少维护：

- `EntityId[]`
- 每种 regular component 的列数据
- occupancy / free count

### 6. `EntityLocation`

第一版一定要单独做这层，不要偷懒。

它至少需要能从 `EntityId` 查到：

- archetype
- chunk
- row index

否则你后面做：

- destroy
- add/remove component
- set/get component

都会越来越难看。

### 7. `Query(All / Any / None)`

第一版不要做复杂 DSL。

只做：

- `All`
- `Any`
- `None`

但必须做到：

- query 结果命中 archetype 集合，而不是对象集合
- system 执行时遍历的是 chunks
- 新 archetype 出现时，query cache 可以增量更新

### 8. `Deferred Structural Change`

这部分不要放到太后面。

原因不是它更酷，而是：

`只要 archetype + chunk 真的站住了，结构变化就一定会成为核心成本。`

第一版建议支持：

- `CreateEntity`
- `DestroyEntity`
- `AddComponent`
- `RemoveComponent`

但这些操作默认不直接改世界，而是先进 command buffer，在安全阶段统一 flush。

### 9. `Phase Scheduler`

第一版调度器先别碰并行。

只做 phase 和显式顺序：

- `PreSim`
- `Sim`
- `PostSim`
- `Presentation`

system 至少要声明：

- 读哪些组件
- 写哪些组件
- 属于哪个 phase

第一版先 phase 内串行。
第二版再考虑 phase 内依赖图。

## 第一版故意不做什么

下面这些东西，第一版要明确写成 `Not Now`：

- 自动并行调度
- job system
- SIMD / JIT / AOT 级代码优化
- 蓝图式可视化脚本
- 网络复制
- 完整 prefab / baking 系统
- 复杂事件总线
- 全自动 inspector / editor

写进计划的意义很大。

因为一旦不把这些排除掉，项目会快速失焦。

## 建议的 4 个里程碑

我建议把实现拆成 4 个阶段，每个阶段都只验证一种核心判断。

### Milestone 1：存储内核站住

目标：

- `World`
- `EntityId`
- `ComponentTypeId`
- `Archetype`
- `Chunk`
- `EntityLocation`
- `CreateEntity`
- `DestroyEntity`
- `All Query`

完成标准：

- 能创建多种 archetype
- 能插入至少数万实体
- `All Query` 不扫描全体 entity，而是直接命中 matching archetypes
- 删除实体后旧 id 失效
- chunk 内补洞后 location 仍然正确

这一阶段只回答一个问题：

`数据布局能不能站住。`

### Milestone 2：结构变化站住

目标：

- `Any / None Query`
- `AddComponent`
- `RemoveComponent`
- `CommandBuffer`
- `FlushStructuralChanges`

完成标准：

- 实体加减组件时能迁移到目标 archetype
- 迁移后 component 数据正确搬运
- query cache 在 archetype 变化后仍然正确
- 结构变化默认走 deferred path，而不是散落在系统执行过程中

这一阶段只回答一个问题：

`结构变化能不能不把世界打碎。`

### Milestone 3：执行骨架站住

目标：

- `System`
- `Phase Scheduler`
- read/write 声明
- phase 内稳定顺序

完成标准：

- 同一 phase 内的 system 顺序可控
- system 迭代的是 query -> chunks
- command buffer 只在安全点 flush
- 至少能跑一条简单仿真链，比如：
  - movement
  - lifetime decay
  - spawn / destroy

这一阶段只回答一个问题：

`执行模型有没有真正围绕数据布局重建。`

### Milestone 4：表示桥与调试工具站住

目标：

- `Representation Bridge`
- 基础调试面板或日志导出
- archetype / chunk / query 命中可视化

完成标准：

- 可以把仿真层状态桥接给外部表示对象
- 能看见 archetype 数量、chunk 占用、query 命中、structural change 次数
- 出问题时能定位 entity 当前在哪个 archetype / chunk / row

这一阶段只回答一个问题：

`系统有没有最基本的工程可见性。`

## 每个阶段的验证方式

不要只做代码，要强制每阶段都带验证。

### 对 Milestone 1

至少做：

- `EntityId` 复用与失效测试
- chunk 补洞测试
- query 正确性测试
- archetype 命中测试

### 对 Milestone 2

至少做：

- add/remove component 迁移测试
- command buffer flush 顺序测试
- 结构变化后 query cache 更新测试

### 对 Milestone 3

至少做：

- phase 执行顺序测试
- 写后读依赖测试
- flush 时机测试

### 对 Milestone 4

至少做：

- archetype / chunk 统计输出
- entity location 调试输出
- structural change 计数输出

## 建议的数据结构草图

第一版不需要把实现写死，但建议往这个方向收敛：

### `EntityId`

```text
struct EntityId {
    uint32 index;
    uint32 generation;
}
```

### `EntityLocation`

```text
struct EntityLocation {
    ArchetypeId archetype;
    ChunkId chunk;
    uint32 row;
}
```

### `Archetype`

```text
struct Archetype {
    Signature signature;
    ChunkList chunks;
    ColumnLayout columns;
}
```

### `Chunk`

```text
struct Chunk {
    uint32 count;
    uint32 capacity;
    EntityId entities[capacity];
    ColumnBuffer columns[];
}
```

### `Query`

```text
struct Query {
    TypeSet all;
    TypeSet any;
    TypeSet none;
    CachedArchetypeList matched;
}
```

这里只给结构方向，不给语言绑定。

原因是第一版最重要的不是语法，而是：

- 这些结构关系有没有站住
- 数据移动路径有没有想清楚

## 第一版 demo 场景建议

不要上来就做“能玩”的 demo。

第一版更适合做一个专门证明结构的场景，比如：

- `50k` 个移动实体
- `10k` 个带生命周期衰减的实体
- 每隔固定帧数做一批 spawn / destroy
- 部分实体在状态变化时 add/remove tag

这个场景足够回答：

- chunk 能不能扛密集遍历
- structural change 会不会把系统打乱
- query cache 有没有意义

## 和文章主线的关系

这份实现计划可以和文章主线同步推进，但不要混成一件事。

建议对应关系如下：

- `02` 负责讲存储为什么长成 `archetype + chunk + query cache`
- `03` 负责讲 structural change / command buffer 为什么几乎必然出现
- `04` 负责讲 phase / system / 显式调度为什么要跟上
- `07` 再把这份实现计划压成“第一版到底先做什么”的系统总结

这样做的好处是：

- 文章继续讲判断
- 实现计划继续讲动作
- 两条线互相支撑，但不互相污染

## 我建议的下一步

如果现在要真正开始推进，我建议顺序是：

1. 先把这份计划定稿。
2. 再写文章 `03｜Structural Change、Command Buffer 与同步点：为什么改结构总是贵`。
3. 同时单开一个 demo 仓库，只做 `Milestone 1`。

这个顺序比较稳。

因为它同时满足三件事：

- 系列文章主线不断
- 实现不抢过早的高级目标
- 结构变化那篇文章能反过来给代码实现提供判断边界
