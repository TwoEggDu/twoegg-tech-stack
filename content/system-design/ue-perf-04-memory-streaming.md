---
title: "Unreal 性能 04｜内存与流送：资产预算、Texture Streaming、PSO Cache 与 GC 调优"
slug: "ue-perf-04-memory-streaming"
date: "2026-03-28"
description: "内存超限和流送卡顿是 Unreal 项目上线后最难排查的问题。本篇覆盖资产内存预算、Texture Streaming 机制、PSO Cache 的预热流程、GC 调优，以及在移动端的内存保守策略。"
tags:
  - "Unreal"
  - "性能优化"
  - "内存"
  - "Texture Streaming"
  - "PSO Cache"
series: "Unreal Engine 架构与系统"
weight: 6260
---

内存问题通常在上线之后才暴露：低内存设备 OOM 崩溃、首次进入场景的 PSO 卡顿、Texture Streaming 导致的贴图糊。本篇建立系统化的内存和流送管理体系。

---

## 内存分类与诊断

### stat memory

```bash
stat memory
# 输出示例：
#   TextureMemory:     312 MB
#   MeshMemory:        89 MB
#   AnimationMemory:   24 MB
#   AudioMemory:       18 MB
#   LevelMemory:       156 MB
#   TotalMemory:       1.2 GB
```

### memreport 完整报告

```bash
# 在游戏控制台执行（生产 MemReport 文件）
memreport -full

# 文件位置：Saved/Profiling/MemReports/
# 内容：按类型列出所有资产的内存占用，包括引用路径
```

**关键关注项**：
- `TextureMemory` 超过总预算 50% → 审计纹理分辨率和格式
- 出现多个相同名字的资产 → 可能有重复加载
- 引用链异常长的资产 → 可能是意外强引用导致常驻

---

## Texture Streaming 机制

### 工作原理

```
Texture Streaming 的目标：
  根据 Texture 在屏幕上占的面积（Screen Size），
  动态加载 / 卸载 Mip 级别，
  让 GPU 内存中只保留必要精度的 Mip。

Mip 级别与距离的关系：
  Mip 0（最高精度，4096×4096）：近景
  Mip 1（2048×2048）
  Mip 2（1024×1024）
  Mip 3（512×512）：中景
  Mip N（最低精度）：远景

Pool 管理：
  r.Streaming.PoolSize 控制流送池大小（MB）
  当所有 Texture 需要的 Mip 超过 Pool 时，
  距离最远的 Texture 会被降级
```

### 关键配置

```bash
# 流送池大小（移动端建议 200-512 MB）
r.Streaming.PoolSize 512

# 调试流送状态
stat streaming
# 关注：
#   Streaming Pool Used: 480 MB（接近上限 = 贴图糊）
#   Wanted Mips != Resident Mips（还未加载完成）

# 强制加载所有 Mip（测试用，不要用于生产）
r.Streaming.FullyLoadUsedTextures 1
```

### Texture Streaming 问题排查

```bash
# 可视化 Streaming Mip 状态
r.TextureStreaming.ShowStatus 1
# 绿色 = 已加载正确 Mip，黄色 = 过低 Mip，红色 = 流送中

# 强制刷新（场景加载后调用，避免贴图糊一段时间）
UTexture2D::ForceFullyResident = true; // C++ 代码
// 或
r.Streaming.ForceFullyResidentTextures 1 // 控制台
```

### Texture Group 优先级

```
Engine/Config/BaseEngine.ini 中配置各 Texture Group 的流送优先级：

[/Script/Engine.Engine]
+TextureGroupProfiles=(Group=TEXTUREGROUP_Character,MipGenSettings=TMGS_SimpleAverage,...,MinLODSize=128,MaxLODSize=2048)
+TextureGroupProfiles=(Group=TEXTUREGROUP_World,MipGenSettings=TMGS_SimpleAverage,...,MaxLODSize=4096)

// 角色纹理优先级最高（玩家最关注的对象）
// 世界背景纹理可以容忍较低 Mip
```

---

## 软引用与异步加载

### 强引用 vs 软引用的内存行为

```cpp
// ❌ 强引用：被引用的资产永远常驻内存
UPROPERTY(EditDefaultsOnly)
TObjectPtr<UStaticMesh> HeavyMesh;  // 只要这个 Actor 在内存里，HeavyMesh 就在

// ✅ 软引用：只保存路径，按需加载
UPROPERTY(EditDefaultsOnly)
TSoftObjectPtr<UStaticMesh> HeavyMesh;  // 不强制加载

// 需要时异步加载
void AMyActor::LoadMeshAsync()
{
    FStreamableManager& Streamable = UAssetManager::Get().GetStreamableManager();
    Streamable.RequestAsyncLoad(
        HeavyMesh.ToSoftObjectPath(),
        FStreamableDelegate::CreateUObject(this, &AMyActor::OnMeshLoaded)
    );
}

void AMyActor::OnMeshLoaded()
{
    if (UStaticMesh* Mesh = HeavyMesh.Get())
    {
        GetStaticMeshComponent()->SetStaticMesh(Mesh);
    }
}
```

### 意外常驻的常见原因

```
资产意外常驻（无法被 GC）的排查方法：

1. memreport -full → 找到目标资产
2. 查看 Outer Chain（引用链）
3. 常见意外引用：
   a. TObjectPtr / UPROPERTY 引用未置 null
   b. TArray<UObject*> 未清空
   c. Delegate 持有的 Lambda 捕获了 UObject*
   d. Timer 回调持有的 WeakObjectPtr 已失效但 Delegate 未解绑
```

---

## PSO Cache 预热

### 什么是 PSO 卡顿

```
PSO（Pipeline State Object）= GPU 管线状态的编译结果

首次遇到新 PSO 时（新材质 + 新 Shader 组合）：
  1. 驱动编译这个 PSO（同步操作）
  2. 编译时间：100ms - 3000ms（取决于 Shader 复杂度和设备）
  3. 编译期间游戏卡住

表现：玩家第一次走进新区域时卡一下，持续 1-3 秒
```

### 收集 PSO

```bash
# 方法一：开发阶段录制
# Player Settings → Enable Shader Pipeline Cache Enabled
r.ShaderPipelineCache.Enabled 1
r.ShaderPipelineCache.LogEnabled 1

# 运行游戏，覆盖所有关卡和场景

# 收集的 PSO 数据保存在：
# Saved/CollectedPSOs/
```

```cpp
// 方法二：代码触发录制开始/结束
FShaderPipelineCache::OpenPipelineFileCache("MyGame", EShaderPlatform::SP_PCD3D_SM5);
// 遍历场景...
FShaderPipelineCache::SavePipelineFileCache(
    EShaderPipelineCache::SaveMode::BoundPSOsOnly);
```

### 预热 PSO

```bash
# 在 DefaultEngine.ini 中配置预热
[/Script/Engine.Engine]
bOptimizeForUAVPerformance=True

[ShaderPipelineCache.CacheFile]
# 把收集到的 PSO 文件放入打包
```

```cpp
// 代码中触发预热（在 Loading Screen 期间）
void AMyGameMode::BeginPlay()
{
    // 开始后台预热（不阻塞游戏线程）
    FShaderPipelineCache::SetBatchMode(FShaderPipelineCache::BatchMode::Background);
    FShaderPipelineCache::ResumeBatching();
}

// 预热完成的通知
FShaderPipelineCache::GetShaderCachePreCompileDelegate().AddLambda(
    [](int32 Remaining)
    {
        if (Remaining == 0)
        {
            UE_LOG(LogGame, Log, TEXT("PSO Precompile Done"));
        }
    }
);
```

---

## GC 调优

### GC 触发时机与表现

```
Unreal 的 GC（Mark-and-Sweep）触发条件：
  1. 手动调用 GEngine->ForceGarbageCollection()
  2. 定时触发（默认 60 秒一次）
  3. 内存压力（低内存设备更频繁）

GC 的代价：
  标记阶段（Mark）：遍历所有 UObject，约 1-30ms
  清除阶段（Sweep）：销毁标记为死亡的对象，约 0.5-5ms

表现：Unreal Insights 中出现 GarbageCollect 事件，GameThread 完全暂停
```

### GC 调优参数

```ini
# DefaultEngine.ini
[/Script/Engine.GarbageCollectionSettings]

# GC 最小触发间隔（秒），增大可减少 GC 频率
gc.TimeBetweenPurgingPendingKillObjects=60.0

# 每帧最多销毁的对象数（分散 Sweep 开销）
gc.MaxObjectsToUnhashUnreachableObjects=2000

# 增量 GC（把 GC 分散到多帧）
gc.AllowIncrementalReachabilityAnalysis=1
gc.IncrementalGCTimeSeconds=0.002  # 每帧最多用 2ms 做 GC
```

### 减少 GC 压力

```cpp
// ❌ 频繁 SpawnActor / DestroyActor（每次都触发分配/GC）
for (int i = 0; i < 100; i++)
{
    AMyBullet* Bullet = GetWorld()->SpawnActor<AMyBullet>(BulletClass, Location, Rotation);
    // 子弹飞出后 DestroyActor → 等待 GC 回收
}

// ✅ 对象池（复用 Actor，不走 GC）
class FBulletPool
{
    TArray<AMyBullet*> _pool;
public:
    AMyBullet* Get()
    {
        if (_pool.Num() > 0)
        {
            AMyBullet* Bullet = _pool.Pop();
            Bullet->SetActorHiddenInGame(false);
            Bullet->SetActorEnableCollision(true);
            return Bullet;
        }
        return GetWorld()->SpawnActor<AMyBullet>(BulletClass, ...);
    }

    void Return(AMyBullet* Bullet)
    {
        Bullet->SetActorHiddenInGame(true);
        Bullet->SetActorEnableCollision(false);
        Bullet->SetActorLocation(FVector::ZeroVector);
        _pool.Push(Bullet);
    }
};
```

---

## 移动端内存保守策略

```
移动端内存预算建议（2GB 设备为例）：
  系统保留：400MB
  Unity/Unreal 运行时：300MB
  代码 + 逻辑：200MB
  ————————————————————
  可用资产内存：约 1.1GB

  Texture：550MB（50%）
  Mesh：220MB（20%）
  Audio：110MB（10%）
  Animation：110MB（10%）
  其他：110MB（10%）
```

### 关键常驻资产 vs 流送资产

```cpp
// 关键资产（角色、UI）：常驻，防止加载延迟
// 场景资产（背景建筑、环境）：按需流送

// 在 Unreal 的 Primary Asset Label 系统中标记
// Content → 右键 → Create Asset Bundle Label
// 设置 ChunkID 和加载优先级

// 代码中管理加载
UAssetManager& AM = UAssetManager::Get();
AM.LoadPrimaryAsset(PlayerAssetId, {FName("Core")});  // 核心 Bundle，随游戏启动加载
AM.LoadPrimaryAsset(LevelAssetId, {FName("Level1")}); // Level Bundle，进关卡时加载
```

### 低内存事件响应

```cpp
// 注册低内存回调
FCoreDelegates::GetMemoryTrimDelegate().AddLambda([]()
{
    // 触发时机：系统内存紧张，通知 App 释放非必要内存
    UE_LOG(LogGame, Warning, TEXT("Low memory warning! Releasing non-critical assets."));

    // 释放非关键资产
    UAssetManager::Get().UnloadPrimaryAssetsWithType(FPrimaryAssetType("Level"));

    // 强制 GC
    GEngine->ForceGarbageCollection(true);
});
```
