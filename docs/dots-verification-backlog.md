# DOTS 系列验证积压清单

> 本文件跟踪所有需要补充实测数据、API 核实、版本声明的条目。
> 行内占位格式：在文章对应位置插入一行 blockquote：
> `> **[待验证 · {id}]** 主张：{claim}。规格：Unity ? / {packages} ? / Device ?。验证：{method}。`
> 补完后删除 blockquote，并将本文件中对应条目标记为 ✅。

---

## 一、E 系列——性能数字待实测

优先补这组：这是 E 系列当前与顶级内容最大的可信度差距。

| ID | 文章 | 主张内容 | 测试规格（预设） | 验证方法 | 状态 |
|----|------|---------|----------------|---------|------|
| `e04-query-rebuild` | E04 EntityQuery | 每帧重建 Query ~0.3–0.8ms；缓存 Query ~0.01ms | Unity 6000.0.x + Entities 1.3.x，10k Entity，`LocalTransform + MoveSpeed`，Desktop (Intel i7/i9) | Profiler CPU Timeline，对比 `OnCreate` vs `OnUpdate` 构建的 `Query.CreateArchetypeChunkArray` 字段耗时 | ⬜ |
| `e05-lookup-random` | E05 ComponentLookup | 顺序遍历 100k Entity ~0.3–0.5ms；随机 ComponentLookup 100k ~3–8ms | 同上规格，100k Entity，两组 Layout：纯 `LocalTransform`（顺序）vs 随机 `Parent` 关系（随机） | Profiler，对比 `IJobEntity` 顺序 vs `ComponentLookup` 随机读取的 Worker Thread 时间轴 | ⬜ |
| `e07-chunk-fragment` | E07 SharedComponent | 碎片化 Chunk（每 Chunk ~25 Entity）处理成本比满 Chunk（128 Entity）"高出数倍" | 5000 Entity，SharedComponent 值 200 种（每种 25 Entity） vs 5 种（每种 1000 Entity） | Profiler，对比两种分组下同一 System 的 CPU 时间；Entities Debugger 查看 Chunk 利用率 | ⬜ |
| `e13-floatmode-fast` | E13 Burst | `FloatMode.Fast` 通常带来 10–30% 提升 | 一个浮点密集 Job（float3 运算循环，10k Entity），Desktop AVX2 目标 | Burst Inspector 对比 Strict vs Fast 的汇编指令密度；Profiler 对比两个模式的 Worker 时间 | ⬜ |
| `e18-chunk-util` | E18 调试工具 | Chunk 利用率低于 50% 时 System 耗时会"虚高" | 5k Entity，满 Chunk 状态 vs 通过频繁 Add/Remove Component 造成 50% 以下利用率 | Entities Debugger Chunks 面板，对比利用率数字与同一 System 在 Profiler 里的耗时 | ⬜ |

---

## 二、E 系列——机制细节待补充（不需要实测，需要来源引用或工具截图）

| ID | 文章 | 缺失内容 | 补充方式 | 状态 |
|----|------|---------|---------|------|
| `e04-changeversionbump` | E04 EntityQuery | `ComponentLookup` 声明为 `ReadWrite` 时，分配本身就会 bump ChangeVersion，哪怕没有实际写入——这是很多"为什么变更检测误触发"的根因 | 查 Entities 源码 `ComponentLookup<T>` 构造路径；或用 Burst Inspector + Profiler 复现误触发场景 | ⬜ |
| `e13-simd-screenshot` | E13 Burst | Burst Inspector 汇编截图：同一段 float3 循环，向量化成功（`vaddps`）vs 失败（`addss`）的对比 | 打开 Burst Inspector，写一个可向量化 vs 不可向量化的最小 Job，截图标注指令名 | ⬜ |
| `e15-ecb-sort-cost` | E15 ECB | ECB Playback 前会对命令按 `sortKey` 排序，命令量大时这本身是一个 O(n log n) 的主线程成本——文章只说"播放是同步的"，没说排序代价 | Profiler 下观察大量并发写入后 Playback 的主线程耗时；查 `EntityCommandBuffer.Playback` 源码 | ⬜ |

---

## 三、P 系列——API 落地与版本声明

P 系列目前是概念骨架，P02–P07 需要实际 `com.unity.physics` API 代码。

| ID | 文章 | 缺失内容 | 补充方式 | 状态 |
|----|------|---------|---------|------|
| `p-version-all` | P01–P07 全部 | 头部缺包版本声明 | 每篇 frontmatter 或文章开头补一行：`> 验证环境：Unity 6000.0.x · com.unity.physics 1.x.x · [设备]` | ⬜ |
| `p01-no-code` | P01 世界概览 | 全文无代码，`PhysicsWorldSingleton`、`SimulationSingleton` 未出现 | P01 作为地图篇可接受无代码，但应补一张"最小访问 Physics World 的入口代码"（3–5 行展示 `PhysicsWorldSingleton` 获取方式） | ⬜ |
| `p03-real-api` | P03 Query 选择 | 代码是 `enum MovementProbeMode` 伪代码，未展示 `PhysicsWorld.CastRay()` / `ColliderCastInput` / `DistanceHit` 实际用法 | 替换为 3 段实际 API 代码：`RaycastInput + CastRay`、`ColliderCastInput + CastCollider`、`PointDistanceInput + CalculateDistance`；标注哪些是 Job-safe | ⬜ |
| `p04-p07-audit` | P04–P07 | 未核查代码密度，可能与 P03 同类问题 | 逐篇审查：是否有实际 API，是否标版本 | ⬜ |

---

## 四、N 系列——Unity NetCode 实际 API 缺失（最大结构缺陷）

N 系列的概念框架是正确的，但从框架到可运行代码之间有断层。优先补 N02 和 N04。

| ID | 文章 | 缺失内容 | 补充方式 | 状态 |
|----|------|---------|---------|------|
| `n-version-all` | N01–N07 全部 | 头部缺包版本声明（NetCode API 版本敏感，0.x vs 1.x 差异显著） | 每篇补：`> 验证环境：Unity 6000.0.x · com.unity.netcode 1.x.x` | ✅ 2026-04-04 |
| `n02-real-api` | N02 CommandData | `PlayerCommand : IComponentData` 是自建结构，不是 NetCode 实际接口——真实写法是 `IInputComponentData`（Entities 1.x）或 `ICommandData`（旧版） | 替换为 `IInputComponentData` 实际写法，包含 `GhostField` attribute；标注版本差异 | ✅ 2026-04-04 |
| `n03-ghost-field` | N03 Snapshot/Ghost | 未展示 `[GhostField]` attribute 标记方式、`GhostAuthoringComponent` 配置、Snapshot 字段选择的实际代码 | 补充：最小 Ghost 定义代码（`GhostAuthoringComponent` + `[GhostField]` 标记示例） | ⬜ |
| `n04-prediction-api` | N04 Prediction/Rollback | `PredictionBuffer` 是完全自建的伪代码——Unity NetCode 预测通过 `GhostPredictionSystemGroup` + `PredictedGhostComponent` 调度，结构根本不同 | 补充：`GhostPredictionSystemGroup` 调度机制说明；`PredictedGhostComponent` 的实际作用；客户端重预测的触发路径 | ✅ 2026-04-04 |
| `n05-n07-audit` | N05–N07 | 未核查 API 密度 | 逐篇审查：是否有实际 NetCode API | ⬜ |

---

## 五、补充顺序建议

```
第一轮（可信度修复，不需要测试设备）
  1. P/N 全系列加版本声明（最快，每篇 1 分钟）
  2. N02 替换 IInputComponentData 实际代码
  3. N04 补 GhostPredictionSystemGroup 调度机制
  4. P03 替换实际 Physics Query API

第二轮（需要 Unity 工程 + Profiler）
  5. e04-query-rebuild 实测
  6. e05-lookup-random 实测
  7. e13-floatmode-fast 实测 + Burst Inspector 截图

第三轮（精细打磨）
  8. e13-simd-screenshot Burst Inspector 汇编对比图
  9. e15-ecb-sort-cost 源码引用
  10. e04-changeversionbump 机制核实
  11. e07-chunk-fragment 实测
  12. e18-chunk-util 实测
  13. P04–P07 逐篇 API 审查
  14. N05–N07 逐篇审查
```

---

## 行内占位格式说明

在文章中需要补充验证的位置，插入一行：

```markdown
> **[待验证 · {id}]** 主张：{具体数字或行为}。规格：Unity ? / {package} ? / Device ?。验证：{工具 + 字段名}。
```

示例（E04 Query rebuild）：
```markdown
> **[待验证 · e04-query-rebuild]** 主张：每帧重建 Query ~0.3–0.8ms，缓存 Query ~0.01ms（10k Entity）。规格：Unity 6000.0.x / Entities 1.x / Desktop。验证：Profiler CPU Timeline，`Query.CreateArchetypeChunkArray` 字段耗时对比。
```

补完后：删除 blockquote，在本文件对应行标记 ✅，并填入实测数据。
