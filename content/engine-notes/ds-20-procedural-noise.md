---
title: "数据结构与算法 20｜程序化噪声：Perlin、Simplex、Worley 与游戏生成"
description: "程序化生成的核心是噪声函数——它把坐标映射为连续的随机值，生成地形、云彩、纹理、动画抖动。这篇讲清楚三种主流噪声（Perlin、Simplex、Worley）的原理、分形叠加（fBM）、以及在地形生成和程序化纹理中的具体用法。"
slug: "ds-20-procedural-noise"
weight: 779
tags:
  - 软件工程
  - 算法
  - 程序化生成
  - 噪声
  - 地形生成
  - 游戏架构
series: "数据结构与算法"
---

> `Random.value` 每次调用都完全随机，不连续，无法用来生成地形——相邻的两个格子高度差天差地别。噪声函数解决"连续随机"问题：输入相邻的坐标，输出相近的值；输入差别大的坐标，输出差别也大。

---

## 噪声 vs 随机数

```csharp
// 随机数：不连续，不可重现
float h1 = Random.value;   // 0.72
float h2 = Random.value;   // 0.14（与 h1 毫无关系）

// 噪声：连续，可重现（相同输入 → 相同输出）
float h1 = Mathf.PerlinNoise(0.0f, 0.0f);  // 0.54
float h2 = Mathf.PerlinNoise(0.01f, 0.0f); // 0.55（相邻，值相近）
float h3 = Mathf.PerlinNoise(1.0f, 0.0f);  // 0.21（距离远，值不同）
```

---

## Perlin 噪声

Ken Perlin 1983 年为电影《Tron》发明，是游戏地形生成的最常用噪声。

**原理**（简化版）：
1. 把空间划分成整数格子
2. 为每个格子角点分配一个随机梯度向量（单位向量）
3. 对查询点，计算它与各角点的偏移向量，点乘梯度向量
4. 用光滑插值函数（`6t⁵ - 15t⁴ + 10t³`）混合各角点的贡献值

```csharp
// Unity 内置 Perlin 噪声（2D）
float Noise2D(float x, float y)
{
    return Mathf.PerlinNoise(x, y);  // 返回 [0, 1]（近似，可能略超出）
}

// 生成高度图
float[,] GenerateHeightMap(int width, int height, float scale, int seed)
{
    float[,] map = new float[width, height];
    float offsetX = seed * 1000f;  // 用 seed 偏移，实现不同种子
    float offsetY = seed * 1000f + 1000f;

    for (int x = 0; x < width; x++)
    for (int y = 0; y < height; y++)
    {
        float nx = (float)x / width  * scale + offsetX;
        float ny = (float)y / height * scale + offsetY;
        map[x, y] = Mathf.PerlinNoise(nx, ny);
    }
    return map;
}
```

**Perlin 噪声的问题**：
- 轴对齐方向上有可见的方格感（各向异性）
- 值域不均匀（更容易靠近 0 和 1）
- 3D 以上性能差

---

## fBM（分形布朗运动）：叠加多个频率的噪声

单层 Perlin 噪声太"平滑"，真实地形是多尺度的——有大山脉，也有小石头。把多层不同频率的噪声叠加（**分形叠加**），就能模拟多尺度的自然纹理：

```csharp
// fBM（Fractal Brownian Motion）：叠加 N 倍频的噪声（Octave）
float FBM(float x, float y, int octaves, float lacunarity = 2f, float persistence = 0.5f)
{
    float value      = 0f;
    float amplitude  = 1f;
    float frequency  = 1f;
    float maxValue   = 0f;

    for (int i = 0; i < octaves; i++)
    {
        value    += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
        maxValue += amplitude;

        amplitude *= persistence;  // 每层振幅减半（高频细节越来越弱）
        frequency *= lacunarity;   // 每层频率翻倍（越来越细腻）
    }

    return value / maxValue;  // 归一化到 [0, 1]
}

// 参数说明：
// octaves（层数）：越多越细致，越慢。通常 4~8 层
// lacunarity（空隙度）：频率增长倍数，通常 2.0
// persistence（持续性）：振幅衰减比例，通常 0.5
//   → 高 persistence：高频细节明显（粗糙地形）
//   → 低 persistence：高频细节弱（平滑丘陵）
```

```csharp
// 不同参数的视觉效果对比
FBM(x, y, octaves: 4, persistence: 0.5f)  // 标准丘陵地形
FBM(x, y, octaves: 6, persistence: 0.7f)  // 崎岖山地
FBM(x, y, octaves: 2, persistence: 0.3f)  // 平缓起伏
```

---

## Simplex 噪声

Ken Perlin 2001 年改进版，解决了 Perlin 的各向异性问题：

```
Perlin vs Simplex：
  Perlin（2D）：在正方形格子上插值，轴对齐方向有方格感
  Simplex（2D）：在三角形格子上插值，各向同性，无方格感

  Simplex 在 3D/4D 时优势明显：
    Perlin 3D：8 个角点插值
    Simplex 3D：4 个角点（四面体）插值，快得多
```

Unity 没有内置 Simplex 噪声，需要用第三方库（如 `FastNoiseLite`）：

```csharp
// 用 FastNoiseLite（推荐，MIT 协议，单文件，高性能）
var noise = new FastNoiseLite();
noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
noise.SetSeed(1337);
noise.SetFrequency(0.01f);

float height = noise.GetNoise(x, y);  // 返回 [-1, 1]
```

---

## Worley 噪声（细胞噪声，Cellular Noise）

生成"细胞"或"石头"纹理。原理：把空间分成网格，每个格子里随机放一个特征点，查询点的值 = 到最近特征点的距离。

```csharp
// 2D Worley 噪声
float Worley(float x, float y)
{
    int cellX = Mathf.FloorToInt(x);
    int cellY = Mathf.FloorToInt(y);

    float minDist = float.MaxValue;

    // 检查 3x3 的邻近格子（特征点可能在邻格）
    for (int dx = -1; dx <= 1; dx++)
    for (int dy = -1; dy <= 1; dy++)
    {
        int    nx = cellX + dx, ny = cellY + dy;
        // 用格子坐标生成确定性随机特征点位置
        float  rx = nx + PseudoRandom(nx, ny, 0);
        float  ry = ny + PseudoRandom(nx, ny, 1);
        float  dist = Mathf.Sqrt((x - rx) * (x - rx) + (y - ry) * (y - ry));
        minDist = Mathf.Min(minDist, dist);
    }
    return minDist;
}

// 确定性随机（同一格子坐标总是返回同样的随机值）
float PseudoRandom(int x, int y, int offset)
{
    int hash = x * 374761393 + y * 668265263 + offset * 1274126177;
    hash = (hash ^ (hash >> 13)) * 1274126177;
    return (float)((hash & 0x7FFFFFFF) % 10000) / 10000f;
}
```

**视觉效果**：
- `minDist`：各向同性的细胞边界（类似石头纹理、皮革纹理）
- `dist2 - dist1`（第二近 - 最近）：更细腻的细胞边界效果
- 取反 `1 - minDist`：每个细胞中心亮、边界暗（类似泡泡）

---

## 游戏应用

### 地形高度图生成

```csharp
// 基础地形 + 山脉叠加
void GenerateTerrain(Terrain terrain, int size, int seed)
{
    var heights = new float[size, size];
    float scale = 0.003f;

    for (int x = 0; x < size; x++)
    for (int y = 0; y < size; y++)
    {
        float nx = x * scale + seed;
        float ny = y * scale + seed * 1.5f;

        // 多层噪声叠加
        float h = FBM(nx, ny, octaves: 6, persistence: 0.5f);

        // 幂运算：让低地更平，高山更陡
        h = Mathf.Pow(h, 2.0f);

        heights[x, y] = h;
    }

    terrain.terrainData.SetHeights(0, 0, heights);
}
```

### 生物群落（Biome）分布

```csharp
// 用两个独立的噪声值决定生物群落类型
// temperature + humidity → biome

float temperature = FBM(x * 0.002f, y * 0.002f, octaves: 3, persistence: 0.6f);
float humidity    = FBM(x * 0.002f + 1000f, y * 0.002f + 1000f, octaves: 3, persistence: 0.6f);

Biome biome = temperature switch
{
    > 0.7f when humidity > 0.6f => Biome.TropicalRainforest,
    > 0.7f when humidity < 0.3f => Biome.Desert,
    > 0.3f when humidity > 0.5f => Biome.TemperateForest,
    < 0.3f                      => Biome.Tundra,
    _                           => Biome.Grassland,
};
```

### 程序化纹理（Shader 里的噪声）

```hlsl
// Shader 里直接用 Simplex 噪声生成云彩、水面、溶解效果
// （GLSL/HLSL 实现，无法用 Unity C# 的 PerlinNoise）

// 溶解效果：噪声值 < 溶解阈值的像素被 clip 掉
float dissolveThreshold = _DissolveAmount;
float noise = snoise(i.uv * _NoiseScale);
clip(noise - dissolveThreshold);
```

### 摄像机抖动（Camera Shake）

```csharp
// 用 Perlin 噪声生成平滑的摄像机抖动（比随机数更自然）
public class CameraShake : MonoBehaviour
{
    private float shakeTime;
    private float shakeIntensity;
    private float seed;

    public void Shake(float duration, float intensity)
    {
        shakeTime     = duration;
        shakeIntensity = intensity;
        seed          = Random.value * 100f;
    }

    void LateUpdate()
    {
        if (shakeTime <= 0) return;

        float t = Time.time * 20f;  // 噪声采样频率（控制抖动速度）
        float x = (Mathf.PerlinNoise(seed + t, 0) - 0.5f) * 2f * shakeIntensity;
        float y = (Mathf.PerlinNoise(0, seed + t) - 0.5f) * 2f * shakeIntensity;

        transform.localPosition = new Vector3(x, y, 0);
        shakeTime -= Time.deltaTime;
        shakeIntensity *= (1 - Time.deltaTime * 5f);  // 逐渐衰减
    }
}
```

---

## 小结

| 噪声类型 | 特点 | 适用场景 |
|---|---|---|
| Perlin | 最普及，Unity 内置，轴向稍有方格感 | 地形、云彩、动画扰动 |
| Simplex | Perlin 改进版，各向同性，3D 性能好 | 高质量地形、3D 噪声纹理 |
| Worley | 细胞感，距离场 | 石头、皮革、生物纹理 |

- **fBM**：多层噪声叠加，模拟多尺度的自然纹理；octaves 越多越细腻，persistence 控制高频比重
- **Seed 控制**：相同 seed + 坐标 = 相同结果（可重现世界），不同 seed 生成不同世界
- **实践工具**：`FastNoiseLite`（开源，单文件，支持多种噪声和分形模式，有可视化工具）
- **Shader 里的噪声**：直接在 GPU 上计算（`snoise` / `cnoise`），零 CPU 开销，用于溶解、水面、云彩特效
