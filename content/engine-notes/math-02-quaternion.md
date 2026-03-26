+++
title = "图形数学 02｜四元数：为什么旋转不用欧拉角，Slerp 插值原理"
slug = "math-02-quaternion"
date = 2026-03-26
description = "欧拉角直观但有万向节死锁问题。四元数用 4 个分量表示旋转，避免万向节死锁，且支持平滑插值（Slerp）。这篇讲清楚四元数的几何含义、与旋转矩阵的相互转换、以及骨骼动画插值中的实际用法。"
weight = 610
[taxonomies]
tags = ["数学", "图形学", "四元数", "旋转", "Slerp", "骨骼动画"]
[extra]
series = "图形数学"
+++

## 欧拉角的问题：万向节死锁

欧拉角用三个角度（pitch/yaw/roll，或 X/Y/Z）描述旋转，直观但存在结构性缺陷。

旋转是有顺序的。以 Z-X-Y 顺序为例（Unity 内部用这个顺序）：先绕 Z 旋转，再绕 X，最后绕 Y。当 X 轴旋转 90° 后，原本独立的 Z 轴旋转和 Y 轴旋转变得平行——两个轴表达的是同一个旋转效果，系统丢失了一个自由度，这就是 **Gimbal Lock（万向节死锁）**。

实际效果：飞行器俯仰 90° 后，偏航和横滚操作变得相同，飞机在三维空间里无法绕某个方向转动。

Unity 里 Euler → Quaternion → Euler 的来回转换经常得到不同的欧拉角值，原因就在这里：多组欧拉角可以对应同一个四元数（旋转），反转时系统只能给出其中一个"规范解"。

---

## 四元数的几何含义

四元数 `q = (w, x, y, z)` 表示一个旋转：

```
w = cos(θ / 2)
x = sin(θ / 2) * axis.x
y = sin(θ / 2) * axis.y
z = sin(θ / 2) * axis.z
```

其中 `axis` 是旋转轴的单位向量，`θ` 是旋转角度。单位四元数满足 `w² + x² + y² + z² = 1`。

几何直觉：四元数把旋转轴和旋转量"编码"进一个 4D 单位球上的点。旋转 0°（恒等旋转）对应 `q = (1, 0, 0, 0)`；旋转 180° 绕 Y 轴对应 `q = (0, 0, 1, 0)`。

共轭四元数 `q* = (w, -x, -y, -z)` 表示反向旋转（逆旋转）。对单位四元数，`q^-1 = q*`。

---

## 四元数乘法与复合旋转

两个旋转的复合通过四元数乘法实现：

```
q_total = q2 * q1   // 先应用 q1，再应用 q2
```

四元数乘法**不满足交换律**，`q1 * q2 ≠ q2 * q1`，顺序就是变换应用的顺序（从右到左，和矩阵乘法一致）。

乘法公式：

```cpp
// C++ 实现
Quaternion multiply(Quaternion q1, Quaternion q2)
{
    return Quaternion(
        q1.w * q2.w - q1.x * q2.x - q1.y * q2.y - q1.z * q2.z,  // w
        q1.w * q2.x + q1.x * q2.w + q1.y * q2.z - q1.z * q2.y,  // x
        q1.w * q2.y - q1.x * q2.z + q1.y * q2.w + q1.z * q2.x,  // y
        q1.w * q2.z + q1.x * q2.y - q1.y * q2.x + q1.z * q2.w   // z
    );
}
```

用四元数旋转一个向量 `v`：`v' = q * (0, v) * q*`（把向量嵌入纯四元数，左乘 q，右乘共轭）。代码里通常直接用 `q * v` 的重载形式。

---

## 与旋转矩阵互转

**四元数 → 旋转矩阵**（3×3）：

```cpp
// 给定单位四元数 q = (w, x, y, z)
float xx = x*x, yy = y*y, zz = z*z;
float xy = x*y, xz = x*z, yz = y*z;
float wx = w*x, wy = w*y, wz = w*z;

Matrix3x3 R = {
    { 1-2*(yy+zz),   2*(xy-wz),   2*(xz+wy) },
    {   2*(xy+wz), 1-2*(xx+zz),   2*(yz-wx) },
    {   2*(xz-wy),   2*(yz+wx), 1-2*(xx+yy) }
};
```

**旋转矩阵 → 四元数**（Shepperd 方法，避免除以接近 0 的值）：

```cpp
Quaternion fromMatrix(Matrix3x3 R)
{
    float trace = R[0][0] + R[1][1] + R[2][2];
    if (trace > 0)
    {
        float s = 0.5f / sqrtf(trace + 1.0f);
        return { 0.25f / s,
                 (R[2][1] - R[1][2]) * s,
                 (R[0][2] - R[2][0]) * s,
                 (R[1][0] - R[0][1]) * s };
    }
    else if (R[0][0] > R[1][1] && R[0][0] > R[2][2])
    {
        float s = 2.0f * sqrtf(1.0f + R[0][0] - R[1][1] - R[2][2]);
        return { (R[2][1] - R[1][2]) / s,
                 0.25f * s,
                 (R[0][1] + R[1][0]) / s,
                 (R[0][2] + R[2][0]) / s };
    }
    // ... 另外两个分支类似，选最大对角元素避免精度损失
}
```

---

## Lerp、Slerp、Nlerp

这三种插值都用于在两个旋转之间平滑过渡，差别在精度和性能上。

**Lerp（线性插值）**：直接对 4 个分量线性插值，结果不是单位四元数，需要 normalize。插值路径在 4D 超球面上是"弦"而非弧，角速度不均匀。

```cpp
Quaternion lerp(Quaternion a, Quaternion b, float t)
{
    return normalize(a * (1 - t) + b * t);
}
```

**Slerp（球面线性插值）**：沿 4D 球面上的大圆弧插值，角速度均匀：

```cpp
Quaternion slerp(Quaternion a, Quaternion b, float t)
{
    float cosTheta = dot(a, b);
    // 确保走最短路径
    if (cosTheta < 0) { b = -b; cosTheta = -cosTheta; }
    // 当夹角极小时退化为 Lerp（避免 sin(0) 除以 0）
    if (cosTheta > 0.9995f) return normalize(a + t * (b - a));

    float theta    = acosf(cosTheta);
    float sinTheta = sinf(theta);
    float w1 = sinf((1 - t) * theta) / sinTheta;
    float w2 = sinf(t * theta) / sinTheta;
    return a * w1 + b * w2;
}
```

**Nlerp（归一化线性插值）**：直接 Lerp 后 normalize，比 Slerp 快（省去两次 `sinf`/`acosf`），在夹角小于 45° 时误差极小，骨骼动画里通常够用：

```cpp
Quaternion nlerp(Quaternion a, Quaternion b, float t)
{
    if (dot(a, b) < 0) b = -b;  // 最短路径
    return normalize(a + t * (b - a));
}
```

实际选择：**骨骼动画每帧插值用 Nlerp**（误差可接受，帧率高时两帧间夹角很小）；**关键帧间的慢速旋转用 Slerp**（需要严格匀速，如摄像机旋转动画）；**纯 Lerp** 基本只用于不在乎角速度的场景。

---

## Unity 的 Quaternion API

```csharp
// 从欧拉角创建
Quaternion q = Quaternion.Euler(0, 90, 0);

// 球面插值，用于摄像机跟随、角色转向
Quaternion rot = Quaternion.Slerp(from, to, Time.deltaTime * speed);

// 从当前方向转向目标方向（自动找最短路径旋转）
Quaternion look = Quaternion.LookRotation(targetDir, Vector3.up);

// 从向量 A 旋转到向量 B
Quaternion delta = Quaternion.FromToRotation(Vector3.forward, hitNormal);

// 旋转一个向量（等价于 q * (0,v) * q*）
Vector3 rotated = q * Vector3.forward;

// 复合旋转：先 local 旋转，再 world 旋转
transform.rotation = worldRot * localRot;
```

注意 `Quaternion * Vector3` 的 `*` 运算符是 Unity 对四元数-向量乘积的重载，内部自动做了 `q * (0, v) * q*`。

---

## 骨骼动画为什么存四元数而不是矩阵

每根骨骼的 local 旋转如果存 4×4 矩阵需要 16 个 float（64 字节）；存四元数只需要 4 个 float（16 字节）。一个有 60 根骨骼的角色，每帧每根骨骼存矩阵需要 3840 字节，存四元数只需 960 字节，压缩率 4x。

更重要的是插值：两个矩阵之间直接 Lerp 没有几何意义（结果可能不是旋转矩阵），必须先分解再插值；两个四元数之间 Slerp/Nlerp 可以直接进行，结果仍然是有效的单位四元数。

动画压缩格式（如 Unity 的 Animation Compression）通常把四元数的 `w` 分量舍去存储（因为 `w = sqrt(1 - x² - y² - z²)` 可以运行时重算），并对 xyz 做量化（quantization）——用 16-bit int 存储，解包时乘以 `1/32767.0`，内存占用再降一半。

---

## 小结

- 欧拉角直观但有 Gimbal Lock；四元数不直观但没有奇异点，支持平滑插值。
- `q = (cos(θ/2), sin(θ/2)*axis)`，单位四元数表示旋转，共轭表示逆旋转。
- 复合旋转用四元数乘法，注意顺序（从右到左应用）。
- 高帧率骨骼动画用 Nlerp 够用；慢速或精度敏感场景用 Slerp。
- 骨骼动画存四元数比存矩阵节省 4x 内存，且插值更稳定。
