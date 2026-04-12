---
title: "Shader 进阶技法 02｜Stencil 模板缓冲：传送门、遮罩与渲染分层"
slug: "shader-advanced-02-stencil"
date: "2026-03-26"
description: "Stencil Buffer（模板缓冲）是渲染管线里一个被低估的工具。理解 Ref/Comp/Pass/Fail 四个参数，用 Stencil 实现传送门、选中高亮遮罩、镜面反射裁剪、UI 遮罩等效果。"
tags:
  - "Shader"
  - "HLSL"
  - "URP"
  - "进阶"
  - "Stencil"
  - "模板缓冲"
  - "传送门"
series: "Shader 手写技法"
weight: 4300
---
Stencil Buffer（模板缓冲）是与深度缓冲并列的一块逐像素存储区域，每个像素 8 位（0~255）。它的作用是：**在某些像素上做标记，后续的渲染 Pass 可以根据这个标记决定是否绘制**。

---

## Stencil 的四个参数

```hlsl
Stencil
{
    Ref   1        // 参考值，用于比较和写入
    Comp  Equal    // 比较函数：当前 Stencil 值与 Ref 如何比较
    Pass  Replace  // 比较通过时，对 Stencil 缓冲做什么
    Fail  Keep     // 比较失败时，对 Stencil 缓冲做什么
    ZFail Keep     // 深度测试失败时，对 Stencil 缓冲做什么
}
```

**Comp（比较函数）常用值：**

| 值 | 含义 |
|----|------|
| `Always` | 总是通过（忽略 Stencil 值） |
| `Equal` | Stencil 值 == Ref 时通过 |
| `NotEqual` | Stencil 值 != Ref 时通过 |
| `Less` | Stencil 值 < Ref 时通过 |
| `Greater` | Stencil 值 > Ref 时通过 |
| `Never` | 从不通过 |

**Pass（操作）常用值：**

| 值 | 含义 |
|----|------|
| `Keep` | 保持当前 Stencil 值不变 |
| `Replace` | 把 Stencil 值替换为 Ref |
| `Zero` | 把 Stencil 值清零 |
| `Increment` | Stencil 值 +1（上限 255 饱和） |
| `Decrement` | Stencil 值 -1（下限 0 饱和） |
| `Invert` | 按位取反 |

---

## 用法一：选中高亮遮罩

最常见的用法：选中物体时显示轮廓描边。

**Pass 1（物体本体）：写入 Stencil**

```hlsl
Pass
{
    Name "ForwardLit"
    Tags { "LightMode" = "UniversalForward" }

    Stencil
    {
        Ref  1
        Comp Always
        Pass Replace   // 无论如何，把 Stencil 写为 1
    }
    // ... 正常光照渲染 ...
}
```

**Pass 2（描边）：只在 Stencil != 1 的区域绘制**

```hlsl
Pass
{
    Name "Outline"
    Tags { "LightMode" = "SRPDefaultUnlit" }

    Stencil
    {
        Ref  1
        Comp NotEqual  // 只在 Stencil != 1 的地方画（即物体轮廓外）
        Pass Keep
    }
    Cull Off
    ZWrite Off
    // ... 外扩顶点，输出描边颜色 ...
}
```

效果：描边只出现在物体轮廓外侧，不覆盖物体本身。

---

## 用法二：传送门 / 镜子裁剪

传送门效果的核心：门框内的区域渲染"另一侧"的场景，门框外正常渲染。

**步骤 1：渲染门框形状，写入 Stencil**

```hlsl
// 门框 Shader（只写 Stencil，不写颜色）
Pass
{
    ColorMask 0      // 不写颜色
    ZWrite    Off    // 不写深度

    Stencil
    {
        Ref  2
        Comp Always
        Pass Replace  // 门框区域的 Stencil = 2
    }
}
```

**步骤 2：渲染"另一侧"场景，只在 Stencil == 2 的区域绘制**

```hlsl
// 另一侧的物体 Shader
Stencil
{
    Ref  2
    Comp Equal   // 只在门框区域（Stencil == 2）绘制
    Pass Keep
}
```

**步骤 3：渲染正常场景，跳过门框区域**

```hlsl
// 正常场景 Shader
Stencil
{
    Ref  2
    Comp NotEqual  // 跳过门框区域
    Pass Keep
}
```

---

## 用法三：UI 裁剪遮罩（Scroll View）

Unity UI 的 Mask 组件就是用 Stencil 实现的：

- Mask 组件的矩形写入 Stencil = 1
- 子元素只在 Stencil == 1 的区域渲染

URP 的 UI Shader 里可以看到：

```hlsl
Stencil
{
    Ref   [_Stencil]
    Comp  [_StencilComp]
    Pass  [_StencilOp]
    ReadMask  [_StencilReadMask]
    WriteMask [_StencilWriteMask]
}
```

Unity UI 通过材质属性动态配置 Stencil，支持嵌套遮罩（每层 Stencil 值递增）。

---

## 用法四：多层遮罩（嵌套）

Stencil 值有 8 位（0~255），可以实现多层嵌套：

- 第 1 层 Mask：Stencil = 1
- 第 2 层 Mask（嵌套在第 1 层内）：Stencil = 2
- 第 3 层 Mask：Stencil = 3

使用 `ReadMask` 和 `WriteMask` 可以用位掩码精细控制哪些位参与比较：

```hlsl
Stencil
{
    Ref       3
    ReadMask  3   // 只看低 2 位
    WriteMask 3   // 只写低 2 位
    Comp      Equal
}
```

---

## 渲染顺序

Stencil 效果依赖渲染顺序——写入 Stencil 的 Pass 必须先执行，读取 Stencil 的 Pass 后执行。控制方式：

1. **Queue**：写入 Stencil 的物体用更小的 Queue 值（先渲染）
2. **Renderer Feature**：自定义 Renderer Feature 可以精确控制 Pass 的执行时机
3. **同一物体内**：同一 SubShader 里，Pass 按声明顺序执行

---

## 调试 Stencil

Frame Debugger 里可以看到每个 Draw Call 后 Stencil 缓冲的状态变化，但 Unity 的 Frame Debugger 不直接显示 Stencil 图。用 RenderDoc 可以查看：

`RenderDoc → Texture Viewer → 选择 Depth/Stencil Buffer → Stencil 通道`。

---

## 小结

| 参数 | 作用 |
|------|------|
| `Ref` | 参考值（用于比较和写入） |
| `Comp` | 比较函数（Equal / NotEqual / Always...） |
| `Pass` | 比较通过时的操作（Replace / Keep / Zero...） |
| `ColorMask 0` + `ZWrite Off` | 只写 Stencil，不写颜色和深度 |

| 用法 | 策略 |
|------|------|
| 选中描边 | Pass 1 写 Stencil=1，Pass 2 在 NotEqual 区域画描边 |
| 传送门 | 门框写 Stencil=2，内容在 Equal 区域画，外部在 NotEqual 区域画 |
| UI 遮罩 | 遮罩写 Stencil，子元素在 Equal 区域画 |

下一篇：自定义后处理 Renderer Feature——在 URP 里插入全屏后处理效果的标准写法。
