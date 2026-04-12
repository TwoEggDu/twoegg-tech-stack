---
title: "GPU 优化 04｜移动端阴影：Shadow Map 代价、CSM 配置与软阴影替代方案"
slug: "gpu-opt-04-shadow-mobile"
date: "2026-03-28"
description: "阴影是移动端 GPU 预算消耗最大的单项功能之一。本篇覆盖 Shadow Map 的 GPU 代价构成、Cascade Shadow Map 的移动端配置、阴影距离与分辨率的权衡，以及低功耗的替代阴影方案。"
tags:
  - "Mobile"
  - "GPU"
  - "Shadow"
  - "性能优化"
  - "URP"
series: "移动端硬件与优化"
weight: 2120
---

阴影是玩家感知最强的视觉功能之一，也是移动端 GPU 开销最难控制的模块。错误的阴影配置可以消耗 30-50% 的 GPU 预算。

---

## Shadow Map 的 GPU 代价构成

### 为什么阴影这么贵

```
每个投射阴影的光源，每帧需要额外的渲染 Pass：

主方向光（Directional Light）+ CSM 2 级：
  Shadow Pass 1（近景 Cascade）：渲染所有投影物体到 1024×1024 深度图
  Shadow Pass 2（远景 Cascade）：再渲染一次到另一张深度图
  Main Pass：采样 2 张阴影纹理 + PCF 过滤

代价拆解（骁龙 8 Gen 2，1080p，中等场景）：
  Shadow Pass 渲染：~1.5ms（DrawCall + 深度写入）
  Shadow Map 采样（Main Pass）：~0.8ms（纹理读取 + 过滤）
  总计：~2.3ms（占 16.7ms 预算的 14%）
```

```
点光源（Point Light）阴影代价更高：
  需要渲染 Cube Map = 6 个方向各 1 个 Shadow Pass
  1 个点光源阴影 ≈ 6 × 主方向光 Shadow Pass 代价
  → 移动端一般禁止使用点光源动态阴影
```

---

## Cascade Shadow Map（CSM）移动端配置

Unity URP 的 CSM 配置：

```
URP Asset → Lighting：

Shadow Distance: 50           ← 阴影渲染距离（米）
Cascade Count: 2              ← 级联数量（移动端最多 2，推荐 1-2）
Cascade Split: 0.25           ← 第一级占 Shadow Distance 的 25%（12.5 米）

Shadow Resolution:
  Main Light Shadow Resolution: 1024 ← 移动端推荐（而非默认 2048）
```

### Cascade 数量的权衡

```
Cascade 1（单级）：
  优点：只有 1 个 Shadow Pass，代价最低
  缺点：远近阴影精度一致，近景阴影可能有锯齿

Cascade 2（双级）：
  优点：近景用高精度，远景用低精度，视觉效果平衡
  缺点：2 个 Shadow Pass，代价是单级的 2 倍
  → 移动端高性能设备的推荐配置

Cascade 4（四级）：
  适合 PC，移动端一般不使用
  4 个 Shadow Pass = 约 4-6ms（严重超预算）

移动端建议：
  低端设备（< 骁龙 7 系列）：1 Cascade，512 分辨率
  中端设备（骁龙 7/8 系列）：2 Cascades，1024 分辨率
  高端设备（骁龙 8 Gen 2+）：2 Cascades，2048 分辨率（如果预算允许）
```

### Shadow Distance 的影响

```
Shadow Distance 与 GPU 代价的关系：
  Shadow Distance 越大 → 需要投入 Shadow Map 的几何体越多
  → Shadow Pass 的 DrawCall 数量增加

实测（骁龙 8 Gen 2，室外场景）：
  Shadow Distance = 100m：Shadow Pass 约 2.8ms
  Shadow Distance = 50m：Shadow Pass 约 1.8ms（-35%）
  Shadow Distance = 30m：Shadow Pass 约 1.2ms（-57%）

建议：
  角色扮演 / 射击游戏（关注近景细节）：30-50m
  开放世界（关注远景视效）：50-100m
  顶视角策略游戏：20-30m
```

---

## URP 阴影参数详解

```csharp
// 在 URP Asset 中配置（或通过代码动态调整）

// 运行时按设备等级动态调整阴影质量
public class ShadowQualityController : MonoBehaviour
{
    [SerializeField] UniversalRenderPipelineAsset _urpAsset;

    void Start()
    {
        // 根据设备内存档次决定阴影质量
        int memoryGB = SystemInfo.systemMemorySize / 1024;

        if (memoryGB >= 8) // 高端设备
        {
            _urpAsset.shadowDistance = 60f;
            _urpAsset.shadowCascadeCount = 2;
            _urpAsset.mainLightShadowmapResolution = 2048;
        }
        else if (memoryGB >= 4) // 中端设备
        {
            _urpAsset.shadowDistance = 40f;
            _urpAsset.shadowCascadeCount = 2;
            _urpAsset.mainLightShadowmapResolution = 1024;
        }
        else // 低端设备
        {
            _urpAsset.shadowDistance = 20f;
            _urpAsset.shadowCascadeCount = 1;
            _urpAsset.mainLightShadowmapResolution = 512;
        }
    }
}
```

### PCF（Percentage Closer Filtering）配置

```
URP Asset → Shadows → Soft Shadows：

Hard Shadows（无 PCF）：最快，有明显锯齿
Soft Shadows（PCF）：会有 4-16 次额外纹理采样，移动端代价显著

Shadow Filter Quality 选项：
  Low（2×2 PCF）：采样 4 次，轻微软化边缘
  Medium（3×3 PCF）：采样 9 次
  High（5×5 PCF）：采样 25 次，效果好但很贵

移动端建议：
  Low 端：Hard Shadows（无滤波）
  中高端：Soft Shadows Low（4 次采样足够）
  不建议在移动端使用 Medium/High
```

---

## Shadow Bias 调优

Shadow Bias 是阴影中最难调的参数，偏差过大导致"悬浮阴影"，偏差过小导致"自阴影锯齿（Shadow Acne）"：

```
URP Light 组件 → Bias 参数：

Depth Bias：沿光线方向推移 Shadow Map 采样深度
  过小：Shadow Acne（物体表面出现条纹噪声）
  过大：Peter Panning（阴影与物体分离，像在飘）

Normal Bias：沿法线方向推移 Shadow Map
  通常与 Depth Bias 配合使用

移动端推荐起始值：
  Depth Bias: 1.0
  Normal Bias: 1.0

如果仍然出现 Shadow Acne：
  增大 Depth Bias 到 2.0-3.0
  增大 Shadow Map 分辨率（更高精度 = 更少 Acne）

如果出现 Peter Panning：
  减小 Normal Bias 到 0.5
  调整 Shadow Near Plane Distance
```

---

## 低功耗替代阴影方案

### 方案一：Blob Shadow（面片阴影）

```
原理：在角色脚下放一张半透明圆形贴图
代价：1 次 DrawCall + 1 次透明面片渲染（极低）
效果：简单但有效，卡通风格游戏常用

实现：
  创建一个 Quad，挂载在角色骨骼根部
  材质：黑色半透明圆形纹理，Alpha Blend
  动态缩放：根据角色与地面距离调整大小和透明度

// 伪代码
void Update() {
    float height = transform.position.y - groundHeight;
    shadowQuad.transform.localScale = Vector3.one * Mathf.Lerp(1.2f, 0.5f, height / 5f);
    shadowMaterial.color = new Color(0, 0, 0, Mathf.Lerp(0.7f, 0.1f, height / 5f));
}
```

### 方案二：Screen Space Shadow（屏幕空间阴影）

```
原理：在后处理阶段，基于深度缓冲计算近似阴影
适用：不需要主光源完整阴影，但需要接触阴影的场景

URP 的 Screen Space Shadow（SSS）：
  URP 14+ 支持
  质量优于 Blob Shadow，低于 Shadow Map
  代价：约 0.3-0.8ms（比 Shadow Map 低很多）

开启方式：
  URP Asset → Rendering → Screen Space Shadows

注意：Screen Space Shadow 只能处理屏幕内的遮挡，
      视角外的阴影遮挡不会被计算（视角移动时可能有阴影弹跳）
```

### 方案三：烘焙阴影（Static Shadow）

```
适用：静态场景（背景建筑、地形）
代价：Runtime 接近 0（只是一张贴图采样）
限制：静态物体不能移动，无法反映动态变化

Unity 工作流：
  标记物体为 Static（含 Contribute GI）
  Light → Mode → Mixed 或 Baked
  Window → Rendering → Lighting → Generate Lighting

烘焙阴影 + 运行时动态阴影组合：
  静态物体（树木、建筑）：使用烘焙阴影（0 代价）
  动态物体（角色、NPC）：使用实时 Shadow Map（只处理动态物体，代价大幅降低）
```

### 方案四：按距离动态开关阴影

```csharp
// 超出一定距离的角色不接收也不投射阴影
public class DistanceShadowController : MonoBehaviour
{
    [SerializeField] float _shadowDistance = 20f;
    Renderer[] _renderers;

    void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
    }

    void Update()
    {
        float dist = Vector3.Distance(transform.position, Camera.main.transform.position);
        bool showShadow = dist < _shadowDistance;

        foreach (var r in _renderers)
        {
            r.shadowCastingMode = showShadow
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
```

---

## 实测数据参考

```
测试场景：Unity URP，室外开放场景，50 个动态角色，骁龙 8 Gen 2

配置                    | Shadow GPU 时间 | 总 GPU 时间
------------------------|----------------|-------------
No Shadow               | 0ms            | 6.8ms
1 Cascade, 512, Hard    | 0.9ms          | 7.7ms
1 Cascade, 1024, Hard   | 1.1ms          | 8.0ms
2 Cascade, 1024, Hard   | 1.8ms          | 8.6ms
2 Cascade, 1024, Soft Low | 2.4ms        | 9.2ms
2 Cascade, 2048, Soft Low | 3.1ms        | 10.0ms
4 Cascade, 2048, Soft Med | 6.8ms        | 13.6ms  ← 已超预算

实际选择：2 Cascade, 1024, Hard（1.8ms，在预算内，视觉可接受）
```

---

## 阴影优化检查清单

```
□ 移除所有 Point/Spot Light 的实时阴影（改用烘焙或无阴影）
□ 主方向光 Cascade 数量 ≤ 2（移动端）
□ Shadow Map 分辨率 ≤ 1024（低端设备 512）
□ Shadow Distance ≤ 50m（大多数场景）
□ 静态物体使用烘焙阴影
□ 超出 20m 的动态角色关闭投影
□ 不透明不透过光的物体关闭 Shadow Casting（如地形底面）
□ UI 元素和特效不参与阴影投射
□ Soft Shadows 使用 Low 而非 Medium/High
```
