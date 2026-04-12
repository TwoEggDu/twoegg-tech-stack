---
title: "CPU 性能优化 05｜内存预算管理：按系统分配上限、Texture Streaming 与 OOM/LMK 防护"
slug: "cpu-opt-05-memory-budget"
date: "2026-03-28"
description: "移动端内存问题最难的往往不是分配失败本身，而是 LMK / jetsam 这类系统强杀。本篇建立系统化的内存预算框架：按目标设备倒推预算、配置 Texture Streaming、建立 OOM/LMK 预警与响应机制。"
tags: ["Unity", "内存", "Texture Streaming", "OOM", "LMK", "性能优化"]
series: "移动端硬件与优化"
weight: 2180
---

## 移动端内存预算的建立

在优化内存之前，必须先建立**预算意识**：每个系统有多少内存可以用，超出了要报警。没有预算的优化是无效的，你不知道什么时候"够了"。更重要的是，移动端所谓的"OOM"很多时候并不是代码抛出了 `OutOfMemoryException`，而是系统在 LMK / jetsam 压力下直接结束进程。

### 设备层级的内存上限

不同设备层级的物理内存（RAM）：

| 设备层级     | 代表机型                          | 物理 RAM  | 推荐应用总内存上限 |
|------------|----------------------------------|----------|--------------------|
| 低端       | 红米 A 系列、入门款手机（2GB）      | 2 GB     | 700 MB             |
| 中低端      | 主流千元机（3GB）                  | 3 GB     | 1.0 GB             |
| 中端       | 骁龙 7 系列、联发科 G 系列（4GB）   | 4 GB     | 1.4 GB             |
| 高端       | 骁龙 8 Gen 系列（8GB）             | 8 GB     | 2.5 GB             |
| 旗舰       | 骁龙 8 Gen 3（12GB+）              | 12+ GB   | 4 GB               |

**为什么只能用 40-60%**：
- Android 系统本身（Zygote、System Server、各类系统服务）占用约 600MB - 1.2GB
- Android 的 Low Memory Killer（LMK）在系统内存不足时会杀死后台进程，如果游戏占用过高，轮到杀前台进程时游戏就会被强制关闭
- iOS 没有 LMK，但有内存警告机制，超限后直接被 jetsam 杀掉，不给缓冲时间

### 预算要从目标设备矩阵倒推，不是从旗舰机正推

上面的表只是"物理 RAM 大致在哪个档位"。真正做项目时，应该先回答：**我们准备跑在哪些设备上**，然后再回答：**这批设备允许我们把内容做到多大**。

如果项目要同时覆盖 Android 和 iOS，统一内容预算通常应当按**更紧的那一侧**倒推。大多数情况下，真正卡你的往往是低内存 Android 设备，而不是高配 iPhone。iOS 多出来的余量更适合做高档差异资源、保留更多缓存，或者把 Texture Streaming 池放大一点，而不是把基础内容直接做大。

真正可执行的运行内存规划，至少要定三条线：
- **稳态线**：玩家连续跑图 / 战斗 10-20 分钟后的常驻占用
- **峰值线**：首进大场景、切场景、热更解压、后台切回前台时的瞬时峰值
- **红线**：再继续上涨就会进入 LMK / jetsam 高风险区的上限

下面给一个适合移动项目立项期使用的基线表。这里的数字不是"理论可分配上限"，而是更适合拿来做发布门槛的**工程线**：

| 最低支持设备基线 | 稳态线 | 峰值线 | 红线 | 适用策略 |
|----------------|-------|-------|------|---------|
| 4GB Android 档 | 850-950 MB | 1.1-1.2 GB | 1.3-1.4 GB | 广覆盖项目、希望保住更多中低端机 |
| 6GB Android 档 | 1.2-1.4 GB | 1.6-1.8 GB | 1.9-2.1 GB | 主流覆盖、允许中高质量资源 |
| 8GB Android 档 | 1.6-1.9 GB | 2.1-2.3 GB | 2.4-2.5 GB | 中重度项目、高配机为主 |

这张表的用法不是"测到 2.1GB 还没死就算安全"，而是反过来：
1. 先定最低支持机型。
2. 再把关卡、角色、特效、缓存、Streaming Pool 都装进对应的稳态线和峰值线里。
3. 最后把红线留给系统波动、后台 App、第三方 SDK、瞬时大分配和异常抖动。

一个很常见的误区是：测试机上有 12GB RAM，于是就默认 2GB 内容常驻也没问题。这样做最后往往不是"低端机画质差一点"，而是低端机在大场景切换、热更新解压、回前台重载时直接被系统强杀。

### 先看清应用程序的内存分布：不是只有 Texture

内存预算最容易卡住的地方，是把**包体大小**、**磁盘缓存大小**、**进程常驻大小**混成同一件事。LMK / jetsam 真正关心的是：**这个进程现在实际占住了多少内存**，而不是某张资源文件在磁盘上只有几 MB。

工程上更有用的分法，不是"美术资源 / 代码资源"这种按制作工种分，而是按**运行时驻留位置**分：

| 内存桶 | 里面有什么 | 常见观察口径 | 典型风险 |
|-------|-----------|-------------|---------|
| 代码与运行时 | `libil2cpp.so`、Unity 引擎代码、托管堆、线程栈、Native 插件 | `Managed Heap`、`Total Allocated`、Memory Profiler 的 Native 区 | 第三方 SDK 常驻、线程过多、托管对象长期存活 |
| Texture / RenderTexture | `Texture2D`、Sprite Atlas、Lightmap、Shadow Map、Bloom / Camera RT | `Graphics Driver`、GPU/Texture Memory | `Read/Write` 复制、RT 链过多、HDR/MSAA/后处理抬高峰值 |
| Mesh & Animation | 顶点/索引缓冲、Skinned Mesh 缓冲、动画片段、BlendShape 数据 | Native + Graphics 相关统计 | 高 LOD 常驻、角色同屏多、蒙皮缓存过大 |
| Shader / Shader Variant | 已加载 Shader、运行时命中的 Variant、驱动程序对象、WarmUp 相关缓存 | Build Report + Native/Driver 侧间接观察 | 变体爆炸、预热面过大、多档位 Shader 同驻 |
| Objects / Pool | GameObject、Component、脚本对象实例、对象池里的子弹/VFX/UI | 托管对象数、Native Object 数、常驻实例数 | 对象池上限太大，把瞬时对象变成常驻对象 |
| Bundle / Cache / Temp | Addressables Handle、Bundle 解压缓冲、下载缓存、场景切换重叠资源 | `Total Reserved`、切场景尖峰、下载时峰值 | 旧场景未退完新场景已进来，造成双驻留 |

这里有几个特别容易漏算的点：
- 同一份资源可能同时存在**磁盘压缩包、CPU 副本、GPU 副本、上传临时缓冲**四份，不是"文件只有 2MB，内存里也就 2MB"。
- `Texture` 和 `Mesh` 一旦开了 `Read/Write Enabled`，就很可能意味着 CPU 和 GPU 各留一份，内存会被直接抬高。
- `Shader Variant` 往往不像 Texture 那样一眼吃掉几百 MB，但它会通过更多的 shader blob、driver program、WarmUp 面，把启动、Native 内存和驱动内存一起抬高。
- `Object Pool` 省的是 `Instantiate/Destroy` 和 GC，不是白送内存；池容量本身就应该进入预算表。

所以做预算时，不要只问"贴图几张 2K"，而要问："这套内容进场之后，代码、对象、纹理、Mesh、Shader、缓存各自有多少常驻和峰值"。

如果读到这里，对对象池和 Shader Variant 这两桶还没有稳定直觉，最适合先配合看：
- [游戏编程设计模式 04｜Object Pool：对象池化原理与实践]({{< relref "system-design/pattern-04-object-pool.md" >}})
- [Unity Shader Variant 是什么：GPU 程序的编译模型]({{< relref "rendering/unity-shader-variant-what-is-a-variant-gpu-compilation-model.md" >}})

### 内存预算分配表

以中端设备（4GB RAM，应用可用约 1.4GB）为例：

| 资产类型              | 预算比例 | 绝对值    | 说明                                    |
|---------------------|---------|----------|-----------------------------------------|
| Texture（纹理）       | 40%     | ~560 MB   | 最大单项，ASTC 压缩后仍是主要来源          |
| Mesh & Animation     | 15%     | ~210 MB   | 顶点缓冲区、动画片段                       |
| Audio                | 10%     | ~140 MB   | 背景音乐、音效                             |
| Code & Runtime       | 15%     | ~210 MB   | IL2CPP 运行时、托管堆、Native 代码          |
| Addressables & Cache | 10%     | ~140 MB   | 热更新资源缓冲、Asset Bundle 缓存           |
| 安全余量              | 10%     | ~140 MB   | 应对峰值、防止 OOM                         |

**量化基准**：

```
低端设备（700 MB）：
  Texture: 280 MB | Mesh: 105 MB | Audio: 70 MB | Runtime: 105 MB | 余量: 140 MB

高端设备（2.5 GB）：
  Texture: 1000 MB | Mesh: 375 MB | Audio: 250 MB | Runtime: 375 MB | 余量: 500 MB
```

### 再往下拆一层：Texture 多少，RT 多少

上面这张表适合立项和总盘子估算，但真正落项目时，`Texture 560 MB` 这种大桶还不够用。因为最容易把你顶到峰值的，常常不是普通贴图，而是 **RenderTexture / ShadowMap / 后处理临时 RT**。

以 **4GB Android 档，应用总预算约 1.4GB** 为例，更适合执行的拆法如下：

| 运行时子桶 | 稳态预算 | 峰值上限 | 说明 |
|-----------|---------|---------|------|
| 静态 Texture / Sprite Atlas | 300-360 MB | 380 MB | 角色、场景、UI 图集、普通贴图主体 |
| Lightmap / Reflection / Cubemap | 60-100 MB | 120 MB | 容易被忽略，但大场景里会持续常驻 |
| RenderTexture / ShadowMap / 后处理临时 RT | 80-120 MB | 140 MB | 最敏感的峰值桶，切场景和开后处理时最容易抬高 |
| Mesh / Animation | 180-220 MB | 250 MB | 顶点索引、Skinned Mesh、动画片段 |
| Shader / Variant / Driver Program | 20-40 MB | 60 MB | 不一定最大，但会抬高 Native / Driver 压力 |
| Objects / Pool | 40-80 MB | 100 MB | 子弹池、VFX 池、UI 预创建、脚本对象 |
| Audio | 100-140 MB | 160 MB | 背景音乐如果不走 Streaming，很容易顶高 |
| Code / Runtime / Native 插件 | 160-220 MB | 250 MB | IL2CPP、托管堆、线程栈、SDK |
| Bundle / Cache / Temp | 80-120 MB | 160 MB | 下载、解压、反序列化、切场景缓冲 |
| 安全余量 | 140 MB | 140 MB | 留给系统波动和瞬时抖动 |

如果你现在只想先回答最核心的两个问题，可以先记这两个工程线：
- **Texture 主桶**：4GB Android 档建议稳态控制在 **360-460 MB**
- **RT 主桶**：4GB Android 档建议稳态控制在 **80-120 MB**，峰值尽量不要超过 **140 MB**

### RenderTexture 为什么要单独算

普通 `Texture2D` 往往是内容驱动，增加是渐进的；`RenderTexture` 则更像配置驱动，一开功能就可能立刻多出几十 MB：
- HDR 打开后，Color RT 可能从 `RGBA32` 变成 `RGBA16F`
- Bloom、DOF、SSAO、TAA 往往都会引入额外的中间 RT
- 阴影贴图、反射探针、相机堆叠会继续叠加
- MSAA 会进一步抬高颜色和深度缓冲的占用

所以很多项目不是"贴图做大了"，而是"后处理 + 阴影 + HDR + 多相机"一起把 RT 桶顶爆了。

### RT 的快速估算公式

```text
RenderTexture 内存 ≈ Width × Height × BytesPerPixel × BufferCount

常见参考：
1920 × 1080 RGBA32    ≈ 7.9 MB
1920 × 1080 RGBA16F   ≈ 15.8 MB
1024 × 1024 D32 阴影图 ≈ 4 MB
2048 × 2048 D32 阴影图 ≈ 16 MB
```

这只是单张 RT 的裸大小。真实项目里还要继续乘上：
- 是否有 Color + Depth 两张
- 是否有 Ping-Pong 双缓冲
- 是否有多级 Bloom Downsample / Upsample
- 是否有多 Camera / Camera Stack
- 是否有 MSAA

也就是说，**一个 HDR 主相机 + 一套 Bloom 链 + 一张 2048 阴影图**，很容易就吃掉几十 MB，甚至直接逼近 100 MB。

### 建立内存预算仪表盘

```csharp
using UnityEngine;
using UnityEngine.Profiling;
using System.Text;

public class MemoryBudgetMonitor : MonoBehaviour
{
    // 预算（字节）
    [Header("Budget (MB)")]
    [SerializeField] private float _textureBudgetMB = 560f;
    [SerializeField] private float _meshBudgetMB = 210f;
    [SerializeField] private float _audioBudgetMB = 140f;

    private StringBuilder _sb = new StringBuilder(512);

    // 每 5 秒更新一次
    private float _nextCheckTime;
    private const float CHECK_INTERVAL = 5f;

    void Update()
    {
        if (Time.time < _nextCheckTime) return;
        _nextCheckTime = Time.time + CHECK_INTERVAL;
        ReportMemory();
    }

    void ReportMemory()
    {
        _sb.Clear();

        // Profiler.GetRuntimeMemorySizeLong：获取指定 UnityEngine.Object 的运行时内存
        // Profiler.usedHeapSizeLong：托管堆已用大小
        // SystemInfo.systemMemorySize：设备总 RAM（MB）

        long textureBytes = Profiler.GetAllocatedMemoryForGraphicsDriver();
        long totalAllocated = Profiler.GetTotalAllocatedMemoryLong();
        long totalReserved = Profiler.GetTotalReservedMemoryLong();
        long managedHeap = System.GC.GetTotalMemory(false);

        float texMB = textureBytes / 1024f / 1024f;
        float allocMB = totalAllocated / 1024f / 1024f;
        float reservedMB = totalReserved / 1024f / 1024f;
        float heapMB = managedHeap / 1024f / 1024f;

        _sb.AppendLine("=== Memory Budget Report ===");
        _sb.AppendLine($"GPU/Texture Driver: {texMB:F1} MB / {_textureBudgetMB:F0} MB " +
            $"({texMB / _textureBudgetMB * 100:F0}%)");
        _sb.AppendLine($"Total Allocated: {allocMB:F1} MB");
        _sb.AppendLine($"Total Reserved: {reservedMB:F1} MB");
        _sb.AppendLine($"Managed Heap: {heapMB:F1} MB");
        _sb.AppendLine($"Device RAM: {SystemInfo.systemMemorySize} MB");

        // 超预算警告
        if (texMB > _textureBudgetMB * 0.9f)
            Debug.LogWarning($"[Memory] Texture approaching budget limit: {texMB:F1} MB");

        Debug.Log(_sb.ToString());
    }
}
```

---

## Texture 是内存的最大单项

纹理通常占应用总内存的 30-50%，也是优化空间最大的地方。

### 运行时 Texture 内存计算

```
运行时内存 = Width × Height × BytesPerPixel × MipMapFactor

MipMapFactor（开启 Mipmap 时）≈ 1.333（几何级数求和：1 + 1/4 + 1/16 + ... ≈ 4/3）

示例：
RGBA32 的 2048×2048 纹理（开启 Mipmap）：
= 2048 × 2048 × 4 bytes × 1.333
= 16,777,216 × 4 × 1.333
≈ 22.4 MB

ASTC 6×6 的 2048×2048 纹理（开启 Mipmap）：
= 2048 × 2048 × (1 bit × 6×6 / 8 /（6×6）) × 1.333
实际：ASTC 6×6 = 每块 16 bytes，覆盖 6×6 个像素 = 约 0.444 bytes/pixel
= 2048 × 2048 × 0.444 × 1.333
≈ 2.49 MB
```

**压缩比对比**：

| 格式      | 字节/像素  | 2048×2048（含Mip） | 用途                    |
|----------|------------|--------------------|-----------------------|
| RGBA32   | 4.0        | 22.4 MB            | 不压缩，仅用于 RT        |
| RGBA16   | 2.0        | 11.2 MB            | 低精度，带 Alpha        |
| ETC2     | 0.5        | 2.8 MB             | Android，不支持 Alpha   |
| ETC2+A8  | 0.625      | 3.5 MB             | Android，带独立 Alpha 通道|
| ASTC 4×4 | 1.0        | 5.6 MB             | 高质量，支持 Alpha      |
| ASTC 6×6 | 0.444      | 2.49 MB            | 标准质量，移动端主力      |
| ASTC 8×8 | 0.25       | 1.4 MB             | 低质量，UI 背景等        |

> 移动端发布必须使用 ETC2 或 ASTC 压缩，禁止使用未压缩的 RGBA32（除非是 RenderTexture）。

### Texture Import Settings 的内存影响

```
Inspector → Texture Import Settings
```

每个设置对内存的影响：

```csharp
// 以下是各个设置对内存的量化影响

// 1. Max Size
// 原始 4096×4096 → 导入 Max Size 设为 2048
// 内存减少：4096² / 2048² = 4倍
// 代价：纹素密度降低（远处/非主角贴图可以降）

// 2. Generate Mipmaps
// 关闭后：内存 ÷ 1.333
// 代价：远处纹理出现摩尔纹（适合 UI、不在 3D 空间中缩放的纹理）
// UI 纹理建议：关闭 Mipmap（UI 是 2D 空间，不会缩放）

// 3. Read/Write Enabled
// 开启后：CPU 内存（RAM）和 GPU 内存（VRAM）各保留一份，总内存 ×2
// 代价：内存翻倍
// 只有需要在 CPU 侧读取像素时才开启（如动态纹理修改）
// 检查脚本：
#if UNITY_EDITOR
using UnityEditor;
[MenuItem("Tools/Find ReadWrite Textures")]
static void FindReadWriteTextures()
{
    string[] guids = AssetDatabase.FindAssets("t:Texture2D");
    foreach (var guid in guids)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.isReadable)
            Debug.LogWarning($"Read/Write enabled: {path}");
    }
}
#endif

// 4. Compression Quality
// Normal（默认）vs High：High 质量更好但构建更慢，运行时内存相同
// 运行时内存只取决于格式（如 ASTC 6×6），与 Quality 无关
```

---

## Texture Streaming（Mip Streaming）

### 工作原理

Texture Streaming 是 Unity 的动态 Mip 级别加载系统：
- 根据相机距离计算每个纹理需要的 Mip 级别（越远需要越低的 Mip）
- 在流送池（Streaming Pool）中只保留当前需要的 Mip 级别
- 相机靠近时，异步加载更高级别的 Mip；相机远离时，释放高级 Mip，腾出内存

```
传统方式：2048×2048 纹理始终在 RAM 中占 22.4 MB（含所有 Mip）

Texture Streaming 方式：
  相机 50m 外：只加载 Mip 4（128×128），占 ~0.1 MB
  相机 10m 外：加载 Mip 2（512×512），占 ~0.6 MB
  相机 1m 内：加载 Mip 0（2048×2048），占 22.4 MB
```

### 开启方法

```
Project Settings → Quality → Texture Streaming → Enable Texture Streaming
```

或通过代码：

```csharp
void EnableTextureStreaming()
{
    QualitySettings.streamingMipmapsActive = true;
    QualitySettings.streamingMipmapsAddAllCameras = true; // 所有相机都参与计算
    QualitySettings.streamingMipmapsMemoryBudget = 512f;  // 流送池大小（MB）
    QualitySettings.streamingMipmapsMaxLevelReduction = 4; // 最多降几级 Mip
    QualitySettings.streamingMipmapsMaxFileIORequests = 1024; // 最大 IO 并发数
}
```

**必要前提**：纹理必须在 Import Settings 中开启 **Streaming Mipmaps**（独立于全局开关）：

```csharp
#if UNITY_EDITOR
using UnityEditor;
// 批量开启所有纹理的 Streaming Mipmaps
[MenuItem("Tools/Enable Streaming Mipmaps For All Textures")]
static void EnableStreamingMipmaps()
{
    string[] guids = AssetDatabase.FindAssets("t:Texture2D");
    int count = 0;
    foreach (var guid in guids)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && !importer.streamingMipmaps)
        {
            importer.streamingMipmaps = true;
            importer.streamingMipmapsPriority = 0; // 优先级（高值优先加载）
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            count++;
        }
    }
    Debug.Log($"Enabled streaming mipmaps for {count} textures.");
}
#endif
```

### 关键参数详解

**Memory Budget（流送池大小）**：

```csharp
// 流送池是 Texture Streaming 可以使用的最大内存
// 设置太小：纹理频繁被降级，出现模糊闪烁
// 设置太大：节省的内存减少

// 建议：低端设备 128-256 MB，中端 256-512 MB，高端 512 MB+
QualitySettings.streamingMipmapsMemoryBudget = 256f; // MB

// 运行时动态调整（根据当前内存压力）
void AdjustStreamingBudget()
{
    long totalFree = GetFreeMemoryBytes(); // 自定义的内存探测函数
    float budgetMB = totalFree > 500_000_000 ? 512f : 256f;
    QualitySettings.streamingMipmapsMemoryBudget = budgetMB;
}
```

**Max Level Reduction（最大 Mip 降级数）**：

```csharp
// 当流送池不够时，Unity 会降低纹理的 Mip 级别来释放内存
// maxLevelReduction = 2 意味着最多把 2048 降到 512（降 2 级）
// maxLevelReduction = 4 意味着最多降到 128（降 4 级），更省内存但可能很模糊

QualitySettings.streamingMipmapsMaxLevelReduction = 2; // 平衡质量和内存

// 对特定纹理设置优先级（高优先级的纹理优先保留高 Mip）
// 通过 TextureImporter.streamingMipmapsPriority 设置（-128 到 127）
```

### 调试 Texture Streaming

Unity 提供了多种调试手段：

**1. 代码中查询流送状态**：

```csharp
using UnityEngine;

public class TextureStreamingDebugger : MonoBehaviour
{
    [SerializeField] private Texture2D _watchTexture;

    void Update()
    {
        if (_watchTexture == null) return;

        // 当前加载的 Mip 级别（0 = 最高分辨率）
        int currentMip = _watchTexture.loadedMipmapLevel;

        // 期望加载的 Mip 级别（根据相机距离计算）
        int requestedMip = _watchTexture.requestedMipmapLevel;

        // 是否还有待加载的 Mip 请求
        bool hasPending = _watchTexture.streamingMipmapsPending;

        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"Texture '{_watchTexture.name}': " +
                $"Loaded Mip={currentMip}, Requested Mip={requestedMip}, " +
                $"Pending={hasPending}");
        }
    }

    // 强制立即加载最高 Mip（用于截图、近距离检视等场景）
    void ForceMaxMip()
    {
        _watchTexture.requestedMipmapLevel = 0;
        // 取消强制后，恢复流送控制
        // _watchTexture.ClearRequestedMipmapLevel();
    }
}
```

**2. Texture Streaming 全局统计**：

```csharp
void LogStreamingStats()
{
    // 等待加载的 Mip 数量（> 0 表示仍在加载中）
    int pending = Texture.streamingMipmapsPending;

    // 当前流送系统已激活
    bool active = Texture.streamingMipmapsActive;

    // 已加载的纹理总内存（流送池中的实际占用）
    // 注：这个 API 在 Unity 2020.2+ 才有
    // long usedBytes = Texture.currentTextureMemory;

    Debug.Log($"Streaming: active={active}, pending={pending}");
}
```

**3. Scene 视图的 Texture Streaming 叠加层**：

在 Scene 视图中，点击右上角的 **Rendering Mode** 下拉菜单，选择 **Texture Streaming**：
- **绿色**：纹理 Mip 级别符合预期
- **红色**：Mip 级别低于期望（流送池不足或还在加载）
- **蓝色**：Mip 级别高于期望（相机距离较远但 Mip 还没降级）

---

## Audio 内存优化

Audio 通常占总内存的 5-15%，优化手段主要是 Load Type 的正确选择。

### AudioClip 的三种 Load Type

```csharp
// 通过 Inspector 或代码设置
// AudioClip Import Settings → Load Type
```

| Load Type                  | 解压时机       | 内存占用       | CPU 开销  | 适用场景                    |
|---------------------------|--------------|--------------|---------|---------------------------|
| Decompress On Load        | 加载时解压     | 高（原始 PCM） | 低（播放时）| 短音效（< 5 秒），需要低延迟  |
| Compressed In Memory      | 播放时按需解压  | 低（压缩格式） | 中（播放时）| 中等长度音效，平衡内存和 CPU |
| Streaming                 | 播放时流式读取  | 极低（只缓冲）  | 高（持续IO）| 背景音乐（长音频），最省内存  |

```csharp
// 推荐配置：
// 背景音乐 → Streaming（内存几乎为零）
// 长音效（1-5秒）→ Compressed In Memory（Vorbis/MP3 压缩）
// 短音效（< 1秒）→ Decompress On Load（无延迟，内存可接受）

// 代码中动态加载 AudioClip 时指定 Load Type
IEnumerator LoadAudioClip(string path)
{
    using (var request = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(
        path, AudioType.MPEG))
    {
        yield return request.SendWebRequest();
        if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            var clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(request);
            clip.LoadAudioData(); // 触发加载
        }
    }
}
```

**内存量化示例**：

```
1 首 3 分钟背景音乐（44.1kHz, Stereo）：
  未压缩 PCM（Decompress On Load）：44100 × 180s × 2ch × 2bytes ≈ 30 MB
  Vorbis 压缩（Compressed In Memory, quality=0.5）：约 3 MB
  Streaming：约 0.1 MB（只缓冲几秒数据）

结论：背景音乐用 Streaming，节省 ~30 MB
```

---

## Asset 的加载与卸载

### Resources.UnloadUnusedAssets 的代价

`Resources.UnloadUnusedAssets()` 会卸载所有不再被引用的 Asset，但它：
1. **触发一次 GC.Collect**（扫描所有托管对象，找出哪些 Asset 不再被引用）
2. **耗时较长**（100-500 ms，取决于内存中 Asset 数量）
3. **应该只在场景切换时调用**，不能在游戏进行中频繁调用

```csharp
// 场景切换时的标准清理流程
IEnumerator LoadScene(string sceneName)
{
    // 1. 先卸载旧场景
    yield return SceneManager.UnloadSceneAsync(currentSceneName);

    // 2. 卸载未使用的 Asset（在加载新场景前）
    yield return Resources.UnloadUnusedAssets();

    // 3. 显式调用 GC
    GC.Collect();
    GC.WaitForPendingFinalizers();

    // 4. 加载新场景
    yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
}
```

### Addressables 的引用计数

Unity Addressables 使用引用计数管理 Asset 生命周期：

```csharp
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableAssetUser : MonoBehaviour
{
    private AsyncOperationHandle<Texture2D> _textureHandle;

    async void Start()
    {
        // 加载 Asset（引用计数 +1）
        _textureHandle = Addressables.LoadAssetAsync<Texture2D>("textures/enemy_idle");
        await _textureHandle.Task;

        if (_textureHandle.Status == AsyncOperationStatus.Succeeded)
        {
            GetComponent<Renderer>().material.mainTexture = _textureHandle.Result;
        }
    }

    void OnDestroy()
    {
        // 必须释放（引用计数 -1），否则 Asset 永远不会被卸载 → 内存泄漏！
        if (_textureHandle.IsValid())
            Addressables.Release(_textureHandle);
    }
}
```

**Addressables 常见内存泄漏**：

```csharp
// 坏：加载了 Asset 但没有释放 Handle
async void LoadAndForget()
{
    var handle = Addressables.LoadAssetAsync<GameObject>("prefabs/bullet");
    await handle.Task;
    Instantiate(handle.Result);
    // 忘记 Addressables.Release(handle)！
    // → handle 永远不被释放，Asset Bundle 也不会被卸载
    // → 每次调用都累积内存泄漏
}

// 好：持有 Handle 并在用完时释放
private List<AsyncOperationHandle> _loadedHandles = new();

async void LoadAndTrack(string address)
{
    var handle = Addressables.LoadAssetAsync<GameObject>(address);
    await handle.Task;
    _loadedHandles.Add(handle); // 记录
    Instantiate(handle.Result);
}

void ReleaseAll()
{
    foreach (var handle in _loadedHandles)
        if (handle.IsValid()) Addressables.Release(handle);
    _loadedHandles.Clear();
}
```

### Asset Bundle 的 Unload 参数

```csharp
// Unload(false)：卸载 Bundle，但已实例化的 Asset 继续存活
// 适用：Bundle 已加载完资源，不再需要加载新资源
assetBundle.Unload(false);
// 风险：已加载的 Asset（Texture、Mesh 等）的内存不会释放
// 它们会成为"孤立"Asset，直到 Resources.UnloadUnusedAssets 才清理

// Unload(true)：卸载 Bundle 和所有从中加载的 Asset（包括已引用的）
// 适用：切换场景，确保彻底清理
assetBundle.Unload(true);
// 风险：场景中仍在使用这些 Asset 的对象会出现 Missing Reference（粉红色）

// 最佳实践：
// 在场景切换时用 Unload(true)，确保无内存泄漏
// 在运行时动态加载/卸载时，追踪引用计数后再决定是否 Unload
```

---

## OOM 防护机制

### 先分清：移动端 OOM、LMK、jetsam 不是一回事

线上团队经常把所有"内存相关闪退"都叫 OOM，但工程上至少要分成三类：

| 现象 | 平台 | 真正原因 | 外部表现 | 处理重点 |
|------|------|----------|----------|----------|
| 分配失败型 OOM | Android / iOS | 某次 `new`、`malloc`、贴图上传或大块连续内存申请失败 | 可能有异常栈或 abort 日志 | 减少大块分配、拆分加载、避免瞬时峰值 |
| LMK 前台强杀 | Android | 系统整体可用内存过低，`lmkd` 直接结束进程 | 玩家感觉是"闪退回桌面"，Crash SDK 可能拿不到托管栈 | 压低项目峰值、看 `logcat`、关注 `PSS` 和 `onTrimMemory()` |
| jetsam 强杀 | iOS | 收到内存警告后仍未及时降下来 | 直接被系统终止，普通崩溃平台经常没有业务栈 | 快速释放缓存、控制统一内容预算、看 `jetsam_event_report` |

所以"做 OOM 防护"并不只是 try-catch `OutOfMemoryException`。真正要防的是：关卡峰值、贴图峰值、热更峰值把设备推进 LMK / jetsam 区间，而这类问题在玩家侧看起来往往只是一次毫无栈信息的闪退。

### Android 到底什么时候会触发 LMK

Android 的 LMK 不是"你的游戏超过某个固定 MB 就被杀"。真正发生的是：**系统整体已经处在高内存压力下，Page Cache 和后台进程回收后仍然不够，`lmkd` 开始按进程优先级杀进程**。所以前台游戏是**最后才会被杀**，不是**绝对不会被杀**。

这也是为什么同样是 1.2GB 占用：
- 在一台后台很干净的 8GB 设备上，可能什么事都没有
- 在一台 4GB 设备上，同时挂着微信、输入法、系统相机和一堆系统服务时，就可能在切场景瞬间直接回桌面

项目里最常见的 LMK 触发时机，不是"慢慢涨到某个值"，而是下面这些**瞬时峰值场景**：
- **切场景双驻留**：旧场景资源还没卸完，新场景 Texture / Mesh / Audio 已经开始进来
- **热更与解压峰值**：Bundle 下载、解压、反序列化、贴图上传、Shader WarmUp 同时发生
- **回前台恢复**：从后台切回时，系统本来就紧，游戏又要恢复 RT、纹理和业务缓存
- **常驻线过高**：对象池、可读纹理、可读 Mesh、过大的 RenderTexture 链让稳态线离红线太近，任何一次正常峰值都会越线

理解这一点很重要：LMK 很多时候不是"某个资源特别夸张"，而是**常驻线太高 + 峰值又重叠**。

### Android：`Application.lowMemory` 只是最后一道保险

在 Unity 里最容易接到的信号是 `Application.lowMemory`，但工程上不要把它当成"足够早的预警器"：
- 它通常意味着系统已经处在高压区，而不是还很从容
- 某些机型只会给很短的处理窗口，来不及做重清理
- 真正导致前台闪退的很多情况，不会先抛出 `OutOfMemoryException`，而是 LMK 直接把进程杀掉

如果项目需要更细的分级，最好把 Android 原生 `onTrimMemory()` 桥接到 Unity，区分 `RUNNING_LOW` 和 `RUNNING_CRITICAL`；`Application.lowMemory` 更适合作为最后一道兜底。

### Android 上我们该怎么做

如果目标设备覆盖到 4GB / 6GB Android，这几件事通常比"收到 lowMemory 再 GC 一次"更重要：

1. **先定最低支持档位，再定常驻线和峰值线**。不要用旗舰测试机的余量去估低端机的安全线。
2. **把 `onTrimMemory()`、`Application.lowMemory`、`dumpsys meminfo` 放进同一套观测链路**。只看 Unity 的托管堆，很容易漏掉 Native 和 Graphics 压力。
3. **避免切场景重叠峰值**。能先卸再载就不要先载再卸；能分批上传贴图和 RT，就不要一帧里同时做。
4. **把对象池、Read/Write、RenderTexture、Shader WarmUp 都纳入预算**。这些东西单个看都"不算大"，叠起来最容易把稳态线抬高。
5. **做分级降载梯子**。先清缓存、再缩 Streaming Pool、再降纹理/阴影/RT，最后才是激进清理；不要把所有动作都压到一次 `lowMemory` 回调里。

```csharp
using UnityEngine;
using System;

public class OOMProtector : MonoBehaviour
{
    // OOM 响应级别
    public enum MemoryPressure { Normal, Low, Critical }
    public static event Action<MemoryPressure> OnMemoryPressureChanged;

    private static MemoryPressure _currentPressure = MemoryPressure.Normal;

    void OnEnable()
    {
        // Unity 在 Android/iOS 内存警告时调用此事件
        Application.lowMemory += HandleLowMemory;
    }

    void OnDisable()
    {
        Application.lowMemory -= HandleLowMemory;
    }

    private void HandleLowMemory()
    {
        Debug.LogWarning("[OOM] Low memory warning received!");

        if (_currentPressure == MemoryPressure.Normal)
        {
            _currentPressure = MemoryPressure.Low;
            OnMemoryPressureChanged?.Invoke(MemoryPressure.Low);
        }
        else if (_currentPressure == MemoryPressure.Low)
        {
            _currentPressure = MemoryPressure.Critical;
            OnMemoryPressureChanged?.Invoke(MemoryPressure.Critical);
        }
    }
}
```

### 分级内存响应

```csharp
public class MemoryResponseSystem : MonoBehaviour
{
    void Start()
    {
        OOMProtector.OnMemoryPressureChanged += HandleMemoryPressure;
    }

    void OnDestroy()
    {
        OOMProtector.OnMemoryPressureChanged -= HandleMemoryPressure;
    }

    private void HandleMemoryPressure(OOMProtector.MemoryPressure pressure)
    {
        switch (pressure)
        {
            case OOMProtector.MemoryPressure.Low:
                StartCoroutine(RespondToLowMemory());
                break;
            case OOMProtector.MemoryPressure.Critical:
                StartCoroutine(RespondToCriticalMemory());
                break;
        }
    }

    IEnumerator RespondToLowMemory()
    {
        Debug.Log("[Memory] Low pressure response: clearing non-critical caches");

        // 降低 Texture Streaming 预算，腾出更多内存
        QualitySettings.streamingMipmapsMemoryBudget =
            QualitySettings.streamingMipmapsMemoryBudget * 0.5f;

        // 卸载已缓存但当前不在屏幕上的特效
        ClearOffscreenVFXCache();

        // 把非关键 Audio Clip 从内存中卸载
        UnloadNonCriticalAudio();

        // 触发 GC 回收（延迟一帧避免卡顿）
        yield return null;
        GC.Collect();
        yield return Resources.UnloadUnusedAssets();

        Debug.Log("[Memory] Low pressure response complete");
    }

    IEnumerator RespondToCriticalMemory()
    {
        Debug.Log("[Memory] CRITICAL pressure response: aggressive cleanup");

        // 降低纹理质量等级
        int currentQuality = QualitySettings.GetQualityLevel();
        if (currentQuality > 0)
        {
            QualitySettings.SetQualityLevel(currentQuality - 1, true);
            Debug.Log($"[Memory] Quality level reduced to {currentQuality - 1}");
        }

        // 强制关闭 Shadow（阴影占用大量纹理内存）
        QualitySettings.shadows = ShadowQuality.Disable;

        // 触发全面清理
        yield return null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        yield return Resources.UnloadUnusedAssets();

        Debug.Log("[Memory] Critical pressure response complete");
    }

    private void ClearOffscreenVFXCache() { /* 卸载不可见的特效 Pool */ }
    private void UnloadNonCriticalAudio() { /* 卸载背景 AudioClip，保留 UI 音效 */ }
}
```

### 主动检测：预警阈值

不等系统发出 `lowMemory` 警告，主动检测当前内存用量。下面这组阈值更适合把**4GB Android 作为最低支持机型**的项目；如果你的最低目标设备是 6GB 或 8GB 档，阈值要整体上移，但仍建议保留至少 10-15% 的安全余量：

```csharp
using Unity.Profiling;
using UnityEngine;

public class ProactiveMemoryWatcher : MonoBehaviour
{
    // 警戒阈值
    [SerializeField] private float _warningThresholdMB = 1200f;  // 超过 1.2GB 触发警告
    [SerializeField] private float _criticalThresholdMB = 1350f; // 超过 1.35GB 触发紧急

    private float _checkInterval = 10f; // 每 10 秒检查一次
    private float _nextCheckTime;

    void Update()
    {
        if (Time.time < _nextCheckTime) return;
        _nextCheckTime = Time.time + _checkInterval;
        CheckMemory();
    }

    void CheckMemory()
    {
        // 托管堆大小（C# 对象）
        long managedBytes = GC.GetTotalMemory(false);

        // Unity 分配的总内存（包括 Asset、引擎内部）
        long totalAllocated = Profiler.GetTotalAllocatedMemoryLong();

        // GPU 驱动内存（纹理、Mesh 等 GPU 资源）
        long graphicsDriverBytes = Profiler.GetAllocatedMemoryForGraphicsDriver();

        float totalMB = (totalAllocated + graphicsDriverBytes) / 1024f / 1024f;
        float managedMB = managedBytes / 1024f / 1024f;

        if (totalMB > _criticalThresholdMB)
        {
            Debug.LogError($"[Memory] CRITICAL: {totalMB:F1} MB > {_criticalThresholdMB} MB threshold!");
            // 可以在这里触发紧急响应，或者主动保存进度后结束游戏
        }
        else if (totalMB > _warningThresholdMB)
        {
            Debug.LogWarning($"[Memory] WARNING: {totalMB:F1} MB > {_warningThresholdMB} MB threshold");
        }

        // 托管堆超大说明有 GC 压力
        if (managedMB > 150f)
        {
            Debug.LogWarning($"[Memory] Managed heap is large: {managedMB:F1} MB. Check for GC pressure.");
        }
    }
}
```

### iOS 内存警告

iOS 的内存警告机制与 Android 不同：Unity 的 `Application.lowMemory` 同样覆盖 iOS（内部对应 `applicationDidReceiveMemoryWarning`），但 iOS 没有多次警告的概念——通常只发一次，超时不响应就被 jetsam 杀掉。

**iOS 的额外注意**：
- iOS 的 GPU 内存和 CPU 内存是共享的（统一内存架构），纹理内存同时影响两侧
- iOS 12 以前有一个非正式的内存上限（约 60% 系统 RAM），超过就被杀
- iOS 12+ 苹果调整了限制，但仍然建议控制在系统 RAM 的 50% 以内

---

## 内存优化的量化目标

在发布前，建立一个可测量的内存健康检查表：

```
低端设备（2GB RAM）目标：
  □ 总 RAM 占用 < 700 MB（Unity + 系统，用 Xcode Instruments 或 Android Profiler 测量）
  □ 纹理内存 < 280 MB（全部使用 ASTC/ETC2 压缩）
  □ 托管堆 < 30 MB（无持续 GC 分配）
  □ 无 Read/Write 开启的纹理（除 RenderTexture）
  □ 背景音乐使用 Streaming Load Type
  □ 所有纹理开启 Streaming Mipmaps
  □ 场景切换后无内存泄漏（切换 3 次后 RAM 不再增长）

测量工具：
  Android：Android Studio Profiler → Memory → Record Native Allocations
  iOS：Xcode Instruments → Allocations + VM Tracker
  Unity：Memory Profiler 包 → Capture and compare snapshots
```

系统化的内存预算管理，配合 Texture Streaming 和主动 OOM 防护，是移动端游戏稳定性的底线保障。
