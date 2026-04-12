---
title: "Unreal 引擎架构 05｜三线程模型：GameThread、RenderThread 与 RHIThread"
slug: "ue-05-threading-model"
date: "2026-03-28"
description: "Unreal 用三条线程流水线驱动每一帧：Game Thread 处理逻辑、Render Thread 构建渲染命令、RHI Thread 提交 GPU。跨线程访问是 Unreal 最常见的崩溃来源之一。"
tags:
  - "Unreal"
  - "多线程"
  - "GameThread"
  - "RenderThread"
  - "RHIThread"
series: "Unreal Engine 架构与系统"
weight: 6050
---

Unreal 引擎的每一帧由三条线程协同完成。理解它们的职责和边界，是避免多线程崩溃、正确扩展引擎的前提。许多"莫名其妙"的崩溃，根本原因都是在错误的线程访问了属于另一个线程的数据。

---

## 三线程的职责

| 线程 | 职责 | 主要数据 |
|------|------|---------|
| **Game Thread** | 游戏逻辑、物理、AI、输入处理 | UWorld、AActor、UComponent |
| **Render Thread** | 场景可见性判断、RDG Pass 构建 | FScene、FPrimitiveSceneProxy、RDG |
| **RHI Thread** | 向 GPU 提交命令、资源创建销毁 | FRHICommandList、GPU 资源句柄 |

**Worker Threads**（TaskGraph）：物理模拟、动画、NavMesh 等并行计算，不属于这三条主线程，但受 GameThread 调度。

---

## 帧流水线

三线程以**流水线方式**并行运行，GameThread 通常领先 1~2 帧：

```
Frame N:   [Game Thread ]─────────────────────►
Frame N:              [Render Thread]──────────►
Frame N:                        [RHI Thread]──►

Frame N+1: [Game Thread ]─────────────────────►
Frame N+1:            [Render Thread]──────────►
```

GameThread 在 Frame N 提交场景数据，Render Thread 在同一帧或下一帧处理，RHI Thread 再滞后一帧提交 GPU。这种设计最大化了 CPU 利用率，但也意味着渲染看到的场景数据有帧延迟。

---

## ENQUEUE_RENDER_COMMAND：跨线程通信

GameThread 向 RenderThread 发送任务的标准方式：

```cpp
// 在 GameThread 调用，向 RenderThread 投递 Lambda
void UMyComponent::UpdateRenderData(float NewValue)
{
    // 必须捕获值，不能捕获引用（GameThread 数据可能在 Lambda 执行时已改变）
    FMyRenderProxy* Proxy = SceneProxy; // 拷贝指针
    float CapturedValue = NewValue;     // 拷贝值

    ENQUEUE_RENDER_COMMAND(UpdateMyProxyData)(
        [Proxy, CapturedValue](FRHICommandListImmediate& RHICmdList)
        {
            // 这里在 RenderThread 执行
            check(IsInRenderingThread()); // 调试断言
            Proxy->UpdateData(CapturedValue);
        }
    );
}
```

**常见错误**：在 Lambda 里引用 `this`（UObject 可能在 Lambda 执行前被 GC 回收），或捕获会被 GameThread 修改的引用。

---

## 线程检查断言

Unreal 提供了一组断言宏，用于调试时检查当前线程：

```cpp
check(IsInGameThread());        // 必须在 GameThread
check(IsInRenderingThread());   // 必须在 RenderThread
check(IsInRHIThread());         // 必须在 RHIThread
check(IsInParallelRenderingThread()); // RenderThread 或其并行任务

// 示例：确保某操作只在正确的线程执行
void FMyProxy::GetDynamicMeshElements(...) const
{
    check(IsInParallelRenderingThread());
    // ...
}
```

在 Debug/Development 版本中，这些断言会在线程访问错误时立即崩溃并给出明确的错误信息，比随机的数据竞争崩溃好排查得多。

---

## FRenderCommandFence：等待 RenderThread

有时 GameThread 需要等待 RenderThread 完成特定操作（比如销毁渲染资源前）：

```cpp
class UMyComponent : public UActorComponent
{
    FRenderCommandFence ReleaseFence;

    virtual void BeginDestroy() override
    {
        Super::BeginDestroy();

        // 通知 RenderThread 释放资源
        ENQUEUE_RENDER_COMMAND(ReleaseMyResources)(
            [this](FRHICommandListImmediate& RHICmdList)
            {
                RenderData.Reset();
            }
        );

        // 设置 Fence：等待上面的命令执行完
        ReleaseFence.BeginFence();
    }

    virtual bool IsReadyForFinishDestroy() override
    {
        // 只有 RenderThread 处理完上面的命令，才允许完成销毁
        return ReleaseFence.IsFenceComplete();
    }
};
```

---

## 为什么不能直接操作 FScene

`FScene` 是 RenderThread 的场景数据，GameThread 不能直接读写它：

```cpp
// ❌ 错误：在 GameThread 直接访问 RenderThread 数据
void AMyActor::Tick(float DeltaTime)
{
    FScene* Scene = GetWorld()->Scene;
    Scene->AddPrimitive(MyProxy); // 崩溃！FScene 只能在 RenderThread 操作
}

// ✅ 正确：通过 Component 的接口，引擎内部会发送到 RenderThread
void AMyActor::Tick(float DeltaTime)
{
    MyMeshComponent->SetStaticMesh(NewMesh); // GameThread 接口
    // 内部调用 MarkRenderStateDirty()，下帧同步到 Proxy
}
```

---

## 从 GameThread 安全修改材质参数

```cpp
// 方式一：通过 MID（Material Instance Dynamic），GameThread 接口
void AMyActor::SetGlowIntensity(float Intensity)
{
    if (UMaterialInstanceDynamic* MID = Cast<UMaterialInstanceDynamic>(
        MeshComp->GetMaterial(0)))
    {
        // SetScalarParameterValue 内部会将更新排队到 RenderThread
        MID->SetScalarParameterValue(TEXT("GlowIntensity"), Intensity);
    }
}

// 方式二：直接操作 FMaterialRenderProxy（在 RenderThread 的 Proxy 更新中）
void FMySceneProxy::UpdateMaterialParameter(float NewValue)
{
    check(IsInParallelRenderingThread());
    // 直接修改 RenderThread 侧的渲染数据
    CachedGlowIntensity = NewValue;
}

// GameThread 侧触发更新
void AMyActor::SetGlowIntensity(float Intensity)
{
    FMySceneProxy* Proxy = static_cast<FMySceneProxy*>(MeshComp->SceneProxy);
    float CapturedIntensity = Intensity;

    ENQUEUE_RENDER_COMMAND(UpdateGlow)(
        [Proxy, CapturedIntensity](FRHICommandListImmediate&)
        {
            Proxy->UpdateMaterialParameter(CapturedIntensity);
        }
    );
}
```

---

## RHI Thread 的职责

RHI Thread 从 RenderThread 接收命令列表（`FRHICommandList`），实际调用底层图形 API：

```
RenderThread 生成：
  FRHICommandList::DrawIndexedPrimitive(...)
  FRHICommandList::SetGraphicsPipelineState(...)
  FRHICommandList::SetShaderTexture(...)
  ↓
RHI Thread 执行：
  ID3D12GraphicsCommandList::DrawIndexedInstanced(...)   // DX12
  vkCmdDrawIndexed(...)                                   // Vulkan
  [MTLRenderCommandEncoder drawIndexedPrimitives:...]     // Metal
```

是否启用 RHI Thread 取决于平台和配置（`r.RHIThread.Enable`）。禁用时，RenderThread 直接提交 GPU，减少一层开销但也减少并行度。
