# 路线 B：E 系列验证数据补充计划

> 目标：把 E 系列 5 个性能主张从"参考值"升级为有可重现测试环境的实测数据。
> 每次工作节约：准备工程 30 min，每个测试点约 45–60 min，共约 5–6 小时。

---

## 工程准备（一次性，所有测试复用）

**Unity 环境要求**
- Unity 6000.0.x LTS
- com.unity.entities 最新稳定版（记录具体版本号）
- com.unity.burst 最新稳定版
- com.unity.collections 最新稳定版

**测试工程结构**

```
Assets/
  PerfTests/
    E04_QueryRebuild/      ← E04 专用场景
    E05_ComponentLookup/   ← E05 专用场景
    E07_ChunkFragment/     ← E07 专用场景
    E13_BurstFloatMode/    ← E13 专用场景
    E18_ChunkUtilization/  ← E18 专用场景
```

**每个场景的公共配置**
- 空 SubScene，纯 ECS World，无 GameObject 干扰
- 关闭 VSync，Fullscreen 窗口模式
- 连接 Unity Profiler（USB 真机 或 Editor Play Mode 均可，记录是哪种）
- 测试设备记录：CPU 型号 + 内存 + OS

---

## 测试点 1：E04 Query Rebuild（`e04-query-rebuild`）

**目标主张**：每帧重建 Query ~0.3–0.8ms；缓存 Query ~0.01ms

**场景搭建**
```
Entity 数量：10,000
Component 布局：LocalTransform + MoveSpeed（各 struct，共 ~28 bytes/entity）
```

**对照组 A（重建 Query）**
```csharp
// OnUpdate 里每帧 Build
public void OnUpdate(ref SystemState state) {
    var q = new EntityQueryBuilder(Allocator.Temp)
        .WithAll<LocalTransform, MoveSpeed>()
        .Build(ref state);
    // 遍历一次，确保 Query 不被优化掉
    q.ToEntityArray(Allocator.Temp).Dispose();
}
```

**对照组 B（缓存 Query）**
```csharp
// OnCreate 里 Build，OnUpdate 里使用缓存
EntityQuery _q;
public void OnCreate(ref SystemState state) {
    _q = new EntityQueryBuilder(state.WorldUpdateAllocator)
        .WithAll<LocalTransform, MoveSpeed>()
        .Build(ref state);
}
public void OnUpdate(ref SystemState state) {
    _q.ToEntityArray(Allocator.Temp).Dispose();
}
```

**Profiler 操作步骤**
1. 打开 Profiler → CPU Usage → Timeline 模式
2. 找到测试 System 的帧时间条目
3. 截图标注字段名（如 `EntityQueryBuilder.Build`）
4. 连跑 100 帧取中位值

**填入文章的格式**
```
| 操作 | 耗时（主线程，中位值） | 测试环境 |
|------|----------------------|---------|
| 每帧重新构建 Query | X ms | Unity 6000.0.x / Entities x.x / [CPU] / 10k Entity |
| 使用缓存 Query 迭代 | X ms | 同上 |
```

---

## 测试点 2：E05 ComponentLookup 随机访问（`e05-lookup-random`）

**目标主张**：顺序遍历 ~0.3–0.5ms；随机 Lookup ~3–8ms（100k Entity）

**场景搭建**
```
Entity 数量：100,000
Component 布局：LocalTransform（顺序组）+ Parent 关系（随机组，需构建随机引用链）
```

**对照组 A（顺序遍历）**：`IJobEntity` 线性遍历 LocalTransform
**对照组 B（随机 Lookup）**：Job 里通过随机 Entity 引用做 `ComponentLookup<LocalTransform>[]`

**注意**：随机组要保证 Entity 引用真正随机（打乱 Entity 数组），避免局部性

**Profiler 操作步骤**：同上，关注 Worker Thread 时间而非主线程

---

## 测试点 3：E13 FloatMode.Fast + Burst Inspector 截图（`e13-floatmode-fast`）

**目标主张**：FloatMode.Fast 通常带来 10–30% 提升

**场景搭建**
```
Job：float3 密集运算循环，10,000 Entity
运算：position += velocity * deltaTime（简单但可 SIMD 化）
```

**对照组**：`FloatMode.Strict` vs `FloatMode.Fast`，`OptimizeFor.Performance`

**Burst Inspector 截图步骤**
1. Window → Burst → Open Inspector
2. 选中测试 Job，设置目标平台 AVX2（Desktop）
3. 截图一：FloatMode.Strict，标注 `addss`（标量）
4. 截图二：FloatMode.Fast，标注 `vaddps` / `vfmadd`（向量）
5. 在两张图上用红框圈出关键指令行

**Profiler 性能对比**：Run 100 帧，记录 Job Worker Thread 时间中位值

---

## 测试点 4：E07 Chunk 碎片化（`e07-chunk-fragment`）

**目标主张**：碎片化 Chunk 处理成本比满 Chunk "高出数倍"

**场景搭建**
```
Entity 数量：5,000
SharedComponent 值：
  组 A（满 Chunk）：5 种值，每种 ~1,000 Entity（~8 Chunk/种）
  组 B（碎片 Chunk）：200 种值，每种 ~25 Entity（每 Chunk 仅 25% 利用率）
```

**验证点**：打开 Entities Debugger → Chunks 面板，截图显示两组的 Chunk 利用率数字

---

## 测试点 5：E18 Chunk 利用率阈值（`e18-chunk-util`）

**目标主张**：利用率低于 50% 时 System 耗时"虚高"

**与 E07 共用场景**，额外步骤：
- 用 Profiler 记录利用率 ~100%、~50%、~25% 三种状态下同一 System 的耗时
- 三张 Profiler 截图对比，标注 Chunk Utilization 数值和对应耗时

---

## 数据记录模板

每次测试完成后，在 `docs/dots-verification-backlog.md` 对应条目填入：

```
测试环境：Unity 6000.0.x · Entities x.x.x · [CPU 型号] · Editor/Device
测试日期：YYYY-MM-DD
结果：[实测数值]
Profiler 截图：[文件名或路径]
```

然后删除文章中对应的 `[待验证]` blockquote，替换为实测数据表格。

---

## 建议执行顺序

```
第一次工作（约 3 小时）
  1. 搭建测试工程基础结构（30 min）
  2. E04 测试 + 数据记录（45 min）
  3. E13 Burst Inspector 截图（45 min）  ← 视觉冲击力最强，优先

第二次工作（约 2 小时）
  4. E05 测试 + 数据记录（60 min）
  5. E07 + E18 共用场景（60 min）
```
