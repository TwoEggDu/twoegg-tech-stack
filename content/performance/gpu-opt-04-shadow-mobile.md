---
title: "GPU 优化 04｜移动端阴影：Shadow Map 代价、CSM 配置与软阴影替代方案"
slug: "gpu-opt-04-shadow-mobile"
date: "2026-03-28"
lastmod: "2026-10-26"
description: "解释 Shadow Map 的投射与接收成本、级联和距离的取舍，分清烘焙、Blob 与 Screen Space Shadows，并设计目标设备上的对照检查。"
tags:
  - "Mobile"
  - "GPU"
  - "Shadow"
  - "性能优化"
  - "URP"
series: "移动端硬件与优化"
weight: 2120
---

阴影会影响落地感、空间关系和可读性，也会增加几何绘制、深度存储和接收端采样工作。代价需要放回当前镜头、投射者和设备条件中判断。

> 版本与证据范围：本轮以 Unity 6（6000.0）/ URP 17 官方资料修订。旧稿中的设备耗时与百分比未找到对应测试记录，已移除；配置数字只用于展示比较变量，本文没有阴影性能实测或已通过的设备选型。

---

## Shadow Map 的 GPU 代价构成

### 为什么阴影这么贵

```
以常规每帧更新的 Shadow Map 路径为例：

主方向光（Directional Light）+ CSM 2 级：
  投射端：为每个级联筛选投射者，绘制到阴影图集的对应区域
  接收端：按接收点选择级联、变换坐标、比较深度并按设置过滤
  级联边界与过渡路径可能增加采样，不能概括成每个像素总采样两张图

成本分别观察：CPU 剔除与提交、阴影绘制、附件、接收端采样
```

```
点光源（Point Light）阴影代价更高：
  通常需要六个方向的阴影视图，URP 可将这些面放入阴影图集
  六个方向的可见几何不相同，不能直接换算为方向光的六倍毫秒数
  是否保留点光源阴影，依据影响范围、画面需求与设备预算判断
```

---

## Cascade Shadow Map（CSM）移动端配置

下面用 Unity URP 的 CSM 参数演示一组候选配置；位置以实际版本 Inspector 为准：

```
URP Asset 的主光阴影配置（示例，不是推荐阈值）：

Shadow Distance: 50           ← 阴影渲染距离（米）
Cascade Count: 2              ← 本次候选，移动端没有统一“最多 2”限制
Cascade Split: 0.25           ← 示例分割比例；实际覆盖以投影和管线计算为准

Shadow Resolution:
  Main Light Shadow Resolution: 1024 ← 图集配置；每级联区域另受布局影响
```

### Cascade 数量的权衡

```
Cascade 1（单级）：
  特点：单一覆盖区域，可作为成本对照
  缺点：远近阴影精度一致，近景阴影可能有锯齿

Cascade 2（双级）：
  特点：将深度范围分区，通常能改善近景质量
  代价：增加区域与投射工作，但不能直接推算为单级的 2 倍

Cascade 4（四级）：
  特点：更多分区；收益与成本取决于视距、投射者重叠和图集分配
  是否适用于目标手机，必须用该场景的预算与采样判断

比较方法：固定镜头与投射者，只改变级联数，再分别比较分辨率
记录近景锯齿、切换、图集实际尺寸与 GPU 时间，避免一次改多项
```

### Shadow Distance 的影响

```
Shadow Distance 与 GPU 代价的关系：
  Shadow Distance 越大 → 需要投入 Shadow Map 的几何体越多
  → Shadow Pass 的 DrawCall 数量增加

待执行对照：按目标镜头分别设置 30m / 50m / 100m
记录：实际投射者与 Draw、阴影质量、阴影 pass 与总 GPU 时间
这些距离只是比较点；减少距离也不保证当前帧耗时线性下降
```

---

## URP 阴影参数详解

```csharp
// 教学配置入口：参数由项目已验证的质量档提供
// 不依据 RAM 容量直接推断 GPU 档次；未执行 Unity 编译与运行
public class ShadowQualityController : MonoBehaviour
{
    [SerializeField] UniversalRenderPipelineAsset _urpAsset;

    public void ApplyProfile(float distance, int cascades, int resolution)
    {
        if (_urpAsset == null) return;
        // 调用前确认参数在当前 URP 版本的有效范围内
        _urpAsset.shadowDistance = distance;
        _urpAsset.shadowCascadeCount = cascades;
        _urpAsset.mainLightShadowmapResolution = resolution;
    }
}
```

### PCF（Percentage Closer Filtering）配置

```
URP Asset → Shadows → Soft Shadows：

Hard Shadows：较少的过滤工作，需观察锯齿与稳定性
Soft Shadows：增加过滤；质量级、GPU 比较采样与实现共同决定实际代价

Shadow Filter Quality 选项：
  Low / Medium / High：按当前版本的滤波实现理解
  滤波覆盖的 texel 范围，不等于实际纹理采样指令数

比较时只改变过滤设置，记录细节、闪烁和接收端 GPU 成本
不将某一档写成所有手机的足够质量或禁用项
```

---

## Shadow Bias 调优

Shadow Bias 是阴影中最难调的参数，偏差过大导致"悬浮阴影"，偏差过小导致"自阴影锯齿（Shadow Acne）"：

```
URP Light 组件 → Bias 参数：

Depth Bias：调整投射几何/比较所用深度，具体方式取决于管线
  过小：Shadow Acne（物体表面出现条纹噪声）
  过大：Peter Panning（阴影与物体分离，像在飘）

Normal Bias：沿表面法线调整投射位置；不能只理解成移动整张贴图
  通常与 Depth Bias 配合使用

调节起点：当前版本默认值与项目尺度，不套用通用的 1.0/1.0

如果仍然出现 Shadow Acne：
  小幅调节 Depth Bias，观察副作用
  同时检查分辨率、投射者尺度与级联覆盖

如果出现 Peter Panning：
  小幅减小 Normal Bias，并确认是否重新出现 Acne
  调整 Shadow Near Plane Distance
```

---

## 阴影替代方案与采样路径的取舍

### 方案一：Blob Shadow（面片阴影）

```
原理：在角色脚下放一张半透明圆形贴图
代价：增加面片与混合工作；是否合批、屏幕覆盖和叠层决定实际成本
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

### 方案二：Screen Space Shadows（屏幕空间采样路径）

```
原理：从主方向光的阴影数据计算屏幕空间阴影纹理，供不透明对象使用
仍需 MainLightShadow；不是仅从屏幕深度推导接触阴影的替代算法

Unity 6 URP 的成本变化：
  可能减少对象着色时重复访问多个级联阴影纹理
  增加深度预通道与一张屏幕空间阴影纹理
  tile-based 移动 GPU 上可能更慢，需要比较实际 pass 和 GPU 时间

开启方式：
  在 Universal Renderer Data 添加 Screen Space Shadows Renderer Feature

检查：Frame Debugger 中的 MainLightShadow、ScreenSpaceShadows、深度预通道
透明对象仍使用 Shadow Map；不要套用“屏幕外投射者一定无效”的接触阴影限制
```

以上行为依据 [Unity 6 URP Screen Space Shadows](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/renderer-feature-screen-space-shadows.html)，不包含本项目的性能测量。

### 方案三：烘焙阴影（Static Shadow）

```
适用：静态场景（背景建筑、地形）
代价：减少对应实时求解，仍有光照数据驻留、采样与解码
限制：静态物体不能移动，无法反映动态变化

Unity 工作流：
  配置参与烘焙与以 Lightmaps 接收 GI 的表面，并检查光照 UV
  Light → Mode → Baked；若用 Mixed，另明确场景 Lighting Mode
  Window → Rendering → Lighting → Generate Lighting

烘焙阴影 + 运行时动态阴影组合：
  静态表面的烘焙贡献与动态物体的探针照明、实时阴影分别配置
  Mixed 模式可能仍让静态物体参与实时阴影，不能自动推导“只画动态物体”
```

### 方案四：按距离动态开关阴影

```csharp
// 只示意按距离切换投射；不改变 receiveShadows，也不构成完整质量系统
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

## 待执行的设备对照

```
固定：Unity/URP 版本、Renderer/API、设备/GPU、场景与资产版本、相机路线
控制：实际分辨率、动态分辨率、帧率上限、预热与热状态
分别改变：投射者、距离、级联数、分辨率、软阴影级别
单独比较：Screen Space Shadows 开关、烘焙/Blob 等不同表现方案
记录：阴影与总 GPU 时间的重复采样、CPU 提交、实际附件、pass 与画面
结果：待执行；不预填设备耗时或最终选型
```

<!-- DATA-TODO: 使用可公开教学工程，在目标设备执行上述对照，保留 capture、采样记录、配置与截图；取得记录后再填写结果，不能将旧稿数字恢复为实测。 -->

---

## 阴影优化检查清单

```
□ 按画面目标和设备预算决定哪些光源需要实时阴影
□ 固定其他条件比较级联数、分辨率、距离与过滤
□ 核对静态表面与动态对象的照明和投射职责
□ 对无画面贡献的投射者检查后再关闭；不透明物体通常仍可投影
□ 对 UI、特效逐项确认是否需要 ShadowCaster pass
□ Screen Space Shadows 是否增加了预通道、纹理与总成本
□ 记录异常、修改与重复测量，不以一次帧率代替结果
```

光照模式的官方分工见 [Unity Light Modes](https://docs.unity3d.com/6000.0/Documentation/Manual/LightModes-introduction.html)。
