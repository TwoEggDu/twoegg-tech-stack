# Unity 渲染系统专栏规划

## 专栏定位

这组文章不写成 Shader 编写教程，也不写成 URP 配置手册。

它真正要解决的问题是：

`把 Unity 渲染系统拆成一张稳定地图——游戏里各类渲染资产分别是什么数据、各自在管线哪个阶段介入、固定渲染管线和可编程渲染管线在架构上有什么根本区别、SRP 给了什么扩展空间——让读者能从"会用 URP"进一步理解"URP 为什么这样设计"。`

一句话说，这个专栏的重点不是参数调节，而是：

`Unity 渲染系统的结构理解：资产的渲染角色 + 管线的架构逻辑。`

---

## 目标读者

- 用 Unity 做了一段时间，但对"材质、Shader、贴图、光照贴图"的关系还没有系统认知的程序员或美术
- 想从 Built-in 迁移到 URP / HDRP 的项目成员，需要理解"迁移了什么，为什么要迁"
- 想在 URP 里写自定义渲染特性（RendererFeature），但不清楚整体架构的开发者
- 配合阅读"游戏图形系统"系列、"Unity 资产系统与序列化"系列、"Unity Shader Variant 治理"系列的读者

---

## 专栏在整体内容地图里的位置

```
[游戏图形系统]
  讲这套系统由哪些层组成：GPU 硬件、图形管线、渲染 API、OS/驱动、引擎架构
        ↓
[Unity 渲染系统]          ← 本专栏
  讲 Unity 里各类渲染资产是什么、怎么产生像素
  讲固定渲染管线和 SRP 的架构逻辑
        ↓
[Unity 资产系统与序列化]
  讲这些资产在 Unity 里怎么被打包（AssetBundle）、加载进内存
        ↓
[Unity Shader Variant 治理]
  讲 Shader 这个特殊资产在打包和运行时的变体问题
```

本专栏是"资产在内存里之后怎么变成像素"，上游是资产交付，下游是 Shader Variant 工程治理。

---

## 文章规划

### 第一组：渲染资产篇

讲各类渲染资产是什么数据，以及各自在管线哪个阶段介入、对最终像素起什么作用。

| slug 前缀 | 标题方向 | 核心问题 |
|---|---|---|
| `unity-rendering-00-asset-overview` | 综述：游戏渲染资产全景图 | 游戏里有哪些渲染资产，各自在管线哪个阶段介入，共同决定一个像素；以一帧画面为主线串联全部资产类型 |
| `unity-rendering-01-mesh-material-texture` | 几何与表面：Mesh、Material、Texture | 顶点数据（Position/UV/Normal/Tangent）→ MVP 变换 → 光栅化插值 → Fragment Shader 采样贴图 → PBR 计算；Material 和 Shader 的关系 |
| `unity-rendering-01b-draw-call-and-batching` | Draw Call 与批处理：CPU 每次向 GPU 发出什么请求 | Draw Call 的内容组成（Mesh/Material/Transform）、静态合批/动态合批/GPU Instancing 的条件与代价；Frame Debugger 里每一行是什么 |
| `unity-rendering-01c-render-target-and-framebuffer` | Render Target 与帧缓冲区：GPU 把结果写到哪里 | Color Buffer、Depth Buffer、Stencil Buffer、G-Buffer、MRT 各自的作用；理解 Frame Debugger 里的 RT 切换和 RenderDoc 里的 Output Merger |
| `unity-rendering-01d-frame-debugger` | Frame Debugger 使用指南 | 逐 Draw Call 回放一帧，读懂 URP Pass 顺序（DepthPrepass/OpaqueForward/Skybox/Transparent/PostProcessing），检查材质参数和 Shader Keyword，定位渲染顺序和批处理问题 |
| `unity-rendering-01e-renderdoc-basics` | RenderDoc 入门：捕获第一帧并读懂它 | 安装配置、从 RenderDoc 启动 Unity、捕获帧、Event List 导航、Texture Viewer 查看 RT 内容，与 Frame Debugger 的定位差异 |
| `unity-rendering-01f-renderdoc-advanced` | RenderDoc 进阶：顶点数据、贴图采样、Pipeline State | Mesh Viewer 读顶点缓冲（验证 Position/UV/Normal 数据）、Texture Viewer 查 mip 层级和采样结果、Pipeline State 各项含义（Blend/Depth/Stencil State）、Shader Debugger 逐像素追踪 |
| `unity-rendering-02-lighting-assets` | 光照资产：实时光、Lightmap、Light Probe、Reflection Probe | 四条光照路径（直接光/烘焙间接光/动态间接光/环境反射）怎么在 Fragment Shader 里合并成最终颜色 |
| `unity-rendering-03-skeletal-animation` | 动画变形：骨骼蒙皮与 Blend Shape | 骨骼权重如何在顶点阶段驱动 Skinning，Blend Shape 的顶点偏移原理，两者如何改变最终覆盖像素的范围和法线朝向 |
| `unity-rendering-04-particles-vfx` | 粒子与特效 | Particle System 的几何生成机制（Billboard/Mesh/Trail），批量渲染与普通 Mesh 的路径异同，VFX Graph 和 Particle System 的架构差异 |
| `unity-rendering-05-postprocessing` | 后处理资产 | Volume 系统的覆盖机制，全屏 Pass 对帧缓冲区的操作原理，Bloom/Tonemapping/SSAO/DOF 的像素级逻辑 |

**与已有文章的关系：**
- `game-graphics-stack-02b`（Mesh/Material/Texture → 像素）是 `unity-rendering-01` 的前身，内容可迁移至 01，或 01 作为扩充版本、02b 保留为"游戏图形系统"系列的一篇

---

### 第二组：渲染管线篇

讲 Unity 渲染管线的架构演进，以及 SRP 给了什么扩展空间。

| slug 前缀 | 标题方向 | 核心问题 |
|---|---|---|
| `unity-rendering-06-builtin-pipeline` | 固定渲染管线：Built-in 的渲染流程与限制 | Camera 排序和 Culling，Forward/Deferred 路径的选择逻辑，OnPreRender/CommandBuffer 有限的扩展点；Built-in 为什么越来越难用 |
| `unity-rendering-07-why-srp` | 为什么需要可编程渲染管线 | Built-in 的根本限制：Shader 模型固定、Lighting Model 难改、多平台差异难处理；SRP 解决了什么，代价是什么 |
| `unity-rendering-08-srp-core-concepts` | SRP 核心概念 | RenderPipelineAsset（配置数据）、RenderPipeline（执行入口）、ScriptableRenderContext（向 GPU 提交命令的接口）三者的关系；CommandBuffer 的角色 |
| `unity-rendering-09-urp-architecture` | URP 架构详解 | UniversalRenderPipelineAsset → Renderer → RendererFeature → RenderPass 的层级；URP 的具体 Pass 顺序（Depth Pre-pass、Opaque Forward、Skybox、Transparent、Post-processing）；每个 Pass 在做什么 |
| `unity-rendering-10-urp-extend` | 怎么在 URP 里扩展渲染流程 | ScriptableRendererFeature + ScriptableRenderPass 的正确写法；RTHandle 和 RenderGraph API 的区别；常见扩展场景（描边、屏幕特效、自定义 Pass 注入） |
| `unity-rendering-11-hdrp-positioning` | HDRP 的定位与取舍 | 和 URP 的核心架构差异（Deferred-first、Lit Shader 模型、更完整的 Volume 系统）；适合什么项目，不适合什么项目；从 URP 迁移到 HDRP 的代价 |

---

## 写作顺序建议

按依赖关系，建议的写作顺序：

```
00 综述（先建立地图）
  → 01 几何与表面（最核心的一条路）
  → 01b Draw Call 与批处理（理解 CPU→GPU 的工作单元）
  → 01c Render Target 与帧缓冲区（理解 GPU 的输出目标）
  → 01d Frame Debugger（用工具验证 01/01b/01c 学到的东西）
  → 01e RenderDoc 入门
  → 01f RenderDoc 进阶
  → 02 光照资产（补完表面计算的另一半）
  → 06 固定管线（建立 Built-in 认知基础）
  → 07 为什么需要 SRP（承接 06 的问题）
  → 08 SRP 核心概念（承接 07）
  → 09 URP 架构（最重要的 SRP 实现）
  → 03 动画变形
  → 04 粒子与特效
  → 05 后处理（和 09/10 关联）
  → 10 URP 扩展
  → 11 HDRP 定位（放最后，读者已有完整认知基础）
```

---

## 与其他系列的接口

| 本系列文章 | 关联系列 | 关联内容 |
|---|---|---|
| 01 几何与表面 | Unity Shader Variant 治理 | Shader 在打包时的变体问题（为什么材质对应多个编译产物） |
| 09 URP 架构 | Unity Shader Variant 治理 | URP Shader 的预过滤机制（`unity-urp-shader-variant-prefiltering`） |
| 01-05 各篇 | Unity 资产系统与序列化 | 这些资产在进入渲染管线之前，怎么被打包和加载进内存 |
| 02 光照资产 | 游戏图形系统 | Lightmap 本质上是一张贴图，采样方式和普通贴图相同 |

---

## 写作规范（延续现有系列风格）

- Front matter 使用 TOML 格式（`+++`）
- series 字段填 `"Unity 渲染系统"`
- weight 按 100 步长递增：unity-rendering-00 = 100，01 = 200，以此类推（留出插入空间）
- slug 格式：`unity-rendering-NN-keyword-keyword`
- 每篇开头一句"如果只用一句话概括这篇"的定场句
- 承接关系明确：每篇第一段说清楚"从上一篇的哪个问题出发"
- 不写成 API 手册：概念优先，代码只用于说明原理，不追求完整可运行示例
