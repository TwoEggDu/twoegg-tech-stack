---
title: "项目实战 03｜写实水面：Gerstner 顶点波浪 + 完整着色"
slug: "shader-project-03-realistic-water"
date: "2026-03-26"
description: "进阶层的水面 Shader 解决了着色问题（折射/反射/深度/泡沫），但水面网格是平的。Gerstner Wave 在顶点阶段模拟真实海浪的尖峰形状，配合法线更新，让水面从视觉到几何都像真实水体。"
tags:
  - "Shader"
  - "HLSL"
  - "URP"
  - "项目实战"
  - "水面"
  - "Gerstner Wave"
  - "顶点动画"
series: "Shader 手写技法"
weight: 4420
---
进阶层的 `shader-advanced-07-water` 解决了水面的着色（折射/反射/深度/泡沫），但顶点是静止的平面网格。真实海浪的形状是尖峰状的——波峰比正弦波更尖锐，波谷更平坦。**Gerstner Wave** 是模拟这种形状的经典模型。

---

## Gerstner Wave 原理

标准正弦波只在 Y 轴方向位移。Gerstner Wave 同时在 XZ 平面横向位移，让顶点向波峰方向靠拢，产生尖峰效果：

```
顶点 P 的位移：
    X += Q * A * D.x * cos(dot(D, XZ) * w + t)
    Y += A * sin(dot(D, XZ) * w + t)
    Z += Q * A * D.z * cos(dot(D, XZ) * w + t)

其中：
    D = 波浪方向（单位向量）
    A = 振幅
    w = 角频率（2π / 波长）
    Q = 陡峭度（0=正弦，1=Gerstner 最大陡峭，>1 产生翻转波）
    t = _Time.y * 速度
```

Q 越大，波峰越尖、波谷越宽——接近真实海浪的不对称形状。

---

## 多波叠加

单个 Gerstner Wave 形状单调。叠加 4 个方向和参数略有差异的波，产生复杂自然的海面：

```hlsl
struct GerstnerWave
{
    float2 direction;   // 传播方向（归一化 XZ）
    float  amplitude;   // 振幅
    float  steepness;   // 陡峭度 Q
    float  wavelength;  // 波长
    float  speed;       // 传播速度
};

// 计算单个 Gerstner Wave 的顶点位移和法线贡献
void GerstnerWaveOffset(GerstnerWave wave, float3 posWS, float time,
                        inout float3 displacement, inout float3 tangent, inout float3 binormal)
{
    float w   = 2.0 * PI / wave.wavelength;
    float phi = wave.speed * w;
    float q   = wave.steepness / (w * wave.amplitude + 0.001);  // 归一化陡峭度

    float2 d  = normalize(wave.direction);
    float  f  = w * dot(d, posWS.xz) + phi * time;
    float  cosF = cos(f);
    float  sinF = sin(f);

    // 顶点位移
    displacement.x += q * wave.amplitude * d.x * cosF;
    displacement.y +=     wave.amplitude         * sinF;
    displacement.z += q * wave.amplitude * d.y * cosF;

    // 法线切线（用于精确法线计算，避免只用法线贴图）
    float wqa = w * q * wave.amplitude;
    tangent  += float3(-wqa * d.x * d.x * sinF,   wqa * d.x * cosF,  -wqa * d.x * d.y * sinF);
    binormal += float3(-wqa * d.x * d.y * sinF,   wqa * d.y * cosF,  -wqa * d.y * d.y * sinF);
}
```

---

## 顶点着色器：4 波叠加

```hlsl
Varyings vert(Attributes i)
{
    Varyings o;
    float3 posWS = TransformObjectToWorld(i.pos.xyz);

    // 4 个波浪的参数
    GerstnerWave waves[4];
    waves[0] = (GerstnerWave){ float2(1, 0),    _Amplitude,        _Steepness, _WaveLength,       _Speed       };
    waves[1] = (GerstnerWave){ float2(0.7, 0.7),_Amplitude * 0.6,  _Steepness, _WaveLength * 0.6, _Speed * 1.2 };
    waves[2] = (GerstnerWave){ float2(-0.4, 0.9),_Amplitude * 0.4, _Steepness, _WaveLength * 1.5, _Speed * 0.8 };
    waves[3] = (GerstnerWave){ float2(0.2,-0.8), _Amplitude * 0.3, _Steepness, _WaveLength * 0.8, _Speed * 1.5 };

    // 累加位移和法线
    float3 displacement = 0;
    float3 tangent  = float3(1, 0, 0);
    float3 binormal = float3(0, 0, 1);

    for (int w = 0; w < 4; w++)
    {
        GerstnerWaveOffset(waves[w], posWS, _Time.y, displacement, tangent, binormal);
    }

    posWS += displacement;
    float3 normalWS = normalize(cross(binormal, tangent));

    VertexPositionInputs pi = GetVertexPositionInputs(
        TransformWorldToObject(posWS));   // 回转回物体空间给 GetVertexPositionInputs

    o.hcs       = TransformWorldToHClip(posWS);
    o.screenPos = ComputeScreenPos(o.hcs);
    o.posWS     = posWS;
    o.normalWS  = normalWS;
    o.uv        = i.uv;

    // TBN（用计算得到的精确切线）
    o.tangentWS   = normalize(tangent);
    o.bitangentWS = normalize(binormal);
    o.shadowCoord = GetShadowCoord(pi);
    return o;
}
```

---

## Fragment 着色器：整合着色模块

Fragment 部分在进阶层水面基础上无大变化，主要使用顶点阶段计算的精确法线替代纯法线贴图：

```hlsl
half4 frag(Varyings input) : SV_Target
{
    float2 screenUV = input.screenPos.xy / input.screenPos.w;
    float3 viewDir  = normalize(GetWorldSpaceViewDir(input.posWS));
    Light  light    = GetMainLight(input.shadowCoord);

    // ── 法线：Gerstner 几何法线 + 双层细节法线贴图 ────────
    float2 uv1 = input.uv * _WaveScale + float2( 0.04, 0.03) * _Time.y;
    float2 uv2 = input.uv * _WaveScale + float2(-0.02, 0.05) * _Time.y + 0.5;
    float3 dn1 = UnpackNormal(SAMPLE_TEXTURE2D(_WaveNormal, sampler_WaveNormal, uv1));
    float3 dn2 = UnpackNormal(SAMPLE_TEXTURE2D(_WaveNormal, sampler_WaveNormal, uv2));
    float3 detailNormalTS = normalize(dn1 + dn2);

    // 把细节法线混合到 Gerstner 几何法线上
    float3 geoNormalWS = normalize(input.normalWS);
    float3 detailNormalWS = normalize(
        input.tangentWS   * detailNormalTS.x +
        input.bitangentWS * detailNormalTS.y +
        input.normalWS    * detailNormalTS.z);
    // 按比例混合几何法线和细节法线
    float3 normalWS = normalize(lerp(geoNormalWS, detailNormalWS, _DetailNormalStrength));

    // ── 深度 ──────────────────────────────────────────────
    float rawDepth   = SampleSceneDepth(screenUV);
    float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
    float waterDepth = input.screenPos.w;
    float depthDiff  = max(0, sceneDepth - waterDepth);
    float depthFade  = saturate(depthDiff / _DepthMaxDistance);

    // ── 折射 ──────────────────────────────────────────────
    float2 refractUV = saturate(screenUV + detailNormalTS.xy * _RefractionStrength);
    half3  refraction = SampleSceneColor(refractUV);

    // ── 水体颜色 ───────────────────────────────────────────
    half3 waterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthFade);
    half  waterAlpha = lerp(_ShallowAlpha, 1.0, depthFade);

    // ── Fresnel ────────────────────────────────────────────
    float NdotV   = saturate(dot(normalWS, viewDir));
    float fresnel = pow(1.0 - NdotV, 4.0);

    // ── 反射（探针）──────────────────────────────────────
    float3 reflDir = reflect(-viewDir, normalWS);
    half4  envS    = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflDir, 0);
    half3  envColor = DecodeHDREnvironment(envS, unity_SpecCube0_HDR);

    // ── 镜面高光 ──────────────────────────────────────────
    float3 halfDir = normalize(light.direction + viewDir);
    half   NdotH   = saturate(dot(normalWS, halfDir));
    half3  specular = pow(NdotH, 512.0) * _Specular * light.color;

    // ── 合并 ──────────────────────────────────────────────
    half3 underwater = lerp(refraction, waterColor, depthFade * 0.5);
    half3 surface    = lerp(underwater, envColor, fresnel) + specular;

    // ── 泡沫 ──────────────────────────────────────────────
    float foamMask = saturate(1.0 - depthDiff / _FoamDistance);
    float2 foamUV  = input.uv * _FoamScale + float2(_Time.y * 0.02, 0);
    half   foam    = step(_FoamThreshold,
        SAMPLE_TEXTURE2D(_FoamTex, sampler_FoamTex, foamUV).r) * foamMask;
    surface = lerp(surface, half3(1,1,1), foam);

    return half4(surface, waterAlpha);
}
```

---

## Gerstner 参数调节指南

| 参数 | 效果 | 典型值 |
|------|------|--------|
| `_Amplitude` | 波高 | 0.3~2.0（海面）0.05~0.3（湖面）|
| `_WaveLength` | 波长（越大波越稀疏） | 10~50 |
| `_Speed` | 传播速度 | 1~5 |
| `_Steepness` | 陡峭度（越大波峰越尖） | 0.3~0.8 |
| `_DetailNormalStrength` | 细节法线占比 | 0.3~0.7 |

**注意**：`Steepness` 过高（接近 1 / 超过 1）会导致波浪翻转、网格自相交，产生瑕疵。实际场景中 0.5~0.7 已有明显的尖峰效果。

---

## 网格要求

Gerstner Wave 对网格密度有要求——顶点越密，波浪形状越平滑：

| 水面类型 | 建议顶点密度 |
|---------|------------|
| 小水潭（<10m） | 32×32 ~ 64×64 |
| 河流/湖面（10~100m） | 64×64 ~ 128×128 |
| 大型海面（100m+） | LOD 网格（近处密，远处稀） |

---

## 小结

写实水面 = Gerstner Wave（顶点层几何波浪，尖峰形状）+ 双层细节法线贴图（表面细节）+ 折射/反射/深度颜色/泡沫（着色层，复用进阶水面方案）。顶点精确法线让高光反射更真实，不依赖法线贴图近似。

下一篇：草地系统 Shader——顶点色控制风吹权重，顶点动画模拟草的摇摆，与碰撞体的交互弯曲。
