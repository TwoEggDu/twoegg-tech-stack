---
title: "CPU 性能优化 05｜内存预算管理：按系统分配上限、Texture Streaming 与 OOM 防护"
slug: "cpu-opt-05-memory-budget"
date: "2026-03-28"
description: "移动端 OOM（内存不足）崩溃是上架后最难排查的问题之一。本篇建立系统化的内存预算框架：按资产类型分配预算、配置 Texture Streaming 参数、实现 OOM 预警机制。"
tags: ["Unity", "内存", "Texture Streaming", "OOM", "性能优化"]
series: "移动端硬件与优化"
weight: 2180
---

## 移动端内存预算的建立

在优化内存之前，必须先建立**预算意识**：每个系统有多少内存可以用，超出了要报警。没有预算的优化是无效的——你不知道什么时候"够了"。

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

### Android：`Application.lowMemory` 事件

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

不等系统发出 lowMemory 警告，主动检测当前内存用量：

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
