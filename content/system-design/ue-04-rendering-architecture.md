---
title: "Unreal 引擎架构 04｜渲染架构：RHI 抽象层与 RDG（Render Dependency Graph）"
slug: "ue-04-rendering-architecture"
date: "2026-03-28"
description: "Unreal 的渲染管线分三层：Game Thread 提交场景数据、Render Thread 构建 DrawCall、RHI Thread 提交 GPU。理解 RHI 抽象层和 RDG 的设计，是扩展 Unreal 渲染的基础。"
tags:
  - "Unreal"
  - "渲染架构"
  - "RHI"
  - "RDG"
  - "延迟渲染"
series: "Unreal Engine 架构与系统"
weight: 6040
---

Unreal 的渲染系统是引擎里最复杂的子系统之一。它不是简单地"发 DrawCall"，而是一套严格分层的多线程架构：Game Thread 负责场景逻辑，Render Thread 负责构建渲染命令，RHI Thread 负责向 GPU 提交。每一层都有明确的职责边界，跨层访问会导致崩溃。

---

## 三层渲染架构总览

```
Game Thread                 Render Thread               RHI Thread / GPU
──────────────────          ──────────────────────       ──────────────────
UWorld / ULevel             FScene                       FRHICommandList
UPrimitiveComponent  ──►    FPrimitiveSceneProxy  ──►    DrawCall 提交
ULightComponent      ──►    FLightSceneProxy             资源创建/销毁
UMaterialInstance    ──►    FMaterialRenderProxy         纹理上传
                            RDG Pass 构建
                            ──────────────────────
                            FRDGBuilder
                              ├─ SceneDepthPass
                              ├─ GBufferPass
                              ├─ LightingPass
                              └─ PostProcessPass
```

每层之间通过**命令队列**通信，不能直接访问对方的数据。

---

## RHI：跨平台图形 API 抽象层

RHI（Rendering Hardware Interface）是 Unreal 对 DirectX 12、Vulkan、Metal、OpenGL 的统一抽象。上层代码只调用 RHI 接口，不直接碰 DX12 或 Vulkan API：

```cpp
// 创建 Vertex Buffer（RHI 接口，与具体 API 无关）
FRHIResourceCreateInfo CreateInfo(TEXT("MyVB"), &VertexData);
FVertexBufferRHIRef VertexBuffer = RHICreateVertexBuffer(
    BufferSize,
    BUF_Static | BUF_VertexBuffer,
    CreateInfo
);

// 设置 RenderTarget
FRHIRenderPassInfo RPInfo(
    RenderTarget->GetRenderTargetTexture(),
    ERenderTargetActions::Clear_Store
);
RHICmdList.BeginRenderPass(RPInfo, TEXT("MyPass"));

// 设置 Shader
RHICmdList.SetGraphicsPipelineState(PipelineState, 0);
RHICmdList.DrawIndexedPrimitive(IndexBuffer, 0, 0, NumVertices, 0, NumTriangles, 1);
RHICmdList.EndRenderPass();
```

RHI 的实现类（`FD3D12DynamicRHI`、`FVulkanDynamicRHI`）在引擎启动时根据平台和配置自动选择。

---

## FScene 与 FPrimitiveSceneProxy

Game Thread 不能直接被 Render Thread 访问。为此，Unreal 为每个可渲染的 Component 设计了**双镜像模式**：

- **Game Thread 侧**：`UPrimitiveComponent`，处理逻辑（位置、可见性设置等）
- **Render Thread 侧**：`FPrimitiveSceneProxy`，持有渲染所需数据（Mesh、Material、Transform）

当 Component 注册到场景或属性变化时，Game Thread 向 Render Thread 发送命令更新 Proxy：

```cpp
// 自定义 SceneProxy（继承自 FPrimitiveSceneProxy）
class FMyMeshSceneProxy : public FPrimitiveSceneProxy
{
public:
    FMyMeshSceneProxy(UMyMeshComponent* Component)
        : FPrimitiveSceneProxy(Component)
        , VertexBuffer(Component->GetVertexData())
        , Material(Component->GetMaterial(0))
    {}

    // Render Thread 调用此函数收集 DrawCall
    virtual void GetDynamicMeshElements(
        const TArray<const FSceneView*>& Views,
        const FSceneViewFamily& ViewFamily,
        uint32 VisibilityMap,
        FMeshElementCollector& Collector) const override
    {
        FMeshBatch& Mesh = Collector.AllocateMesh();
        // 填充 Mesh 数据...
        Collector.AddMesh(ViewIndex, Mesh);
    }
};

// Component 侧创建 Proxy
FPrimitiveSceneProxy* UMyMeshComponent::CreateSceneProxy()
{
    return new FMyMeshSceneProxy(this);
}
```

---

## RDG：声明式渲染图

RDG（Render Dependency Graph）是 UE4.22 引入的现代渲染框架，取代了之前手动管理资源状态和 Pass 顺序的方式。

**核心思路**：先声明所有 Pass 及其资源依赖，RDG 自动推导执行顺序、插入资源屏障（Barrier）、剔除未使用的 Pass。

```cpp
// RDG 的工作流程
void RenderMyEffect(FRDGBuilder& GraphBuilder, FRDGTextureRef SceneColor)
{
    // 1. 创建 RDG 管理的资源（延迟创建，只有被使用时才实际分配）
    FRDGTextureDesc Desc = FRDGTextureDesc::Create2D(
        FIntPoint(1920, 1080),
        PF_FloatRGBA,
        FClearValueBinding::Black,
        TexCreate_RenderTargetable | TexCreate_ShaderResource
    );
    FRDGTextureRef OutputTexture = GraphBuilder.CreateTexture(Desc, TEXT("MyOutput"));

    // 2. 声明 Pass（不立即执行，只是记录依赖）
    FMyPassParameters* PassParams = GraphBuilder.AllocParameters<FMyPassParameters>();
    PassParams->InputTexture = SceneColor;    // 读取 SceneColor
    PassParams->OutputTexture = GraphBuilder.CreateUAV(OutputTexture);  // 写入 OutputTexture

    GraphBuilder.AddPass(
        RDG_EVENT_NAME("MyEffect"),
        PassParams,
        ERDGPassFlags::Compute,
        [PassParams](FRHIComputeCommandList& RHICmdList)
        {
            // 这里才是实际执行的代码
            TShaderMapRef<FMyComputeShader> ComputeShader(GetGlobalShaderMap(GMaxRHIFeatureLevel));
            SetComputePipelineState(RHICmdList, ComputeShader.GetComputeShader());
            SetShaderParameters(RHICmdList, ComputeShader, ComputeShader.GetComputeShader(), *PassParams);
            RHICmdList.DispatchComputeShader(1920/8, 1080/8, 1);
        }
    );

    // 3. GraphBuilder.Execute() 时才真正执行所有 Pass（由引擎在帧末调用）
}
```

**RDG 的优势**：
- **自动 Barrier**：根据读写关系自动插入 Vulkan/DX12 的资源状态转换
- **Pass 剔除**：没有被其他 Pass 依赖的 Pass 自动跳过（类似死代码消除）
- **资源复用**：RDG 管理的 Transient 资源可以在不同 Pass 间复用内存

---

## Deferred Rendering 的 Pass 结构

Unreal 默认的延迟渲染管线在 `FDeferredShadingSceneRenderer::Render()` 中组织，主要 Pass：

```
PrePass（Depth Only）
  └─ 只写深度，用于 Early Z 优化

GBuffer Pass
  ├─ BaseColor（漫反射颜色）
  ├─ Normal（世界空间法线）
  ├─ Roughness / Metallic / Specular
  └─ ShadingModel ID

Shadow Depth Pass
  └─ 各光源的 Shadow Map 生成

Lighting Pass
  ├─ Direct Lighting（读 GBuffer + Shadow Map）
  ├─ Sky Light / IBL
  └─ 输出到 HDR Scene Color

Translucency Pass
  └─ 半透明物体前向渲染

Post Process
  ├─ TAA / DLSS
  ├─ Bloom / DOF / Motion Blur
  └─ Tone Mapping → LDR 输出
```

每个 Pass 在现代 Unreal（UE5）中都通过 RDG 管理。

---

## 自定义 RDG Pass 示例

在 SceneViewExtension 或 Renderer Feature 中插入自定义 Pass：

```cpp
class FMySceneViewExtension : public FSceneViewExtensionBase
{
public:
    virtual void PostRenderBasePassDeferred_RenderThread(
        FRHICommandListImmediate& RHICmdList,
        FSceneView& InView,
        const FRenderingCompositePassContext& Context,
        const FMinimalSceneTextures& SceneTextures) override
    {
        FRDGBuilder GraphBuilder(RHICmdList);

        FRDGTextureRef SceneColor = GraphBuilder.RegisterExternalTexture(
            SceneTextures.Color.Resolve);

        // 添加自定义 Pass
        AddMyCustomPass(GraphBuilder, SceneColor);

        GraphBuilder.Execute();
    }
};
```
