# Piccolo 渲染前置 01：从场景对象到一帧画面

> 执行层提纲与证据记录；篇级状态统一在 `piccolo-engine-series-plan.md`。

## 定位与范围

- 用户已明确标题为《读懂渲染管线：从场景对象到一帧画面》，要求结合 Piccolo 实际代码并更新导航。覆盖完整画面链路，不缩成单独的位置变化案例。
- 类型：原理篇。问题空间为场景状态怎样经过 CPU 组织、GPU 执行与呈现成为画面；先建立数据与阶段模型，再落到固定源码。
- 承接三篇 C++ 前置，以 GFX-01 加入补充目录，网页 series_order 为 4；主线 06 的位置追踪案例与 07 的 Pass/Vulkan 深挖仍保留待写。
- 源码基线：`BoomingTech/Piccolo@f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，保持只读。
- 案例选择：默认初始化为延迟渲染的主相机正常路径；注明前向路径、交换链重建及粒子附加工作，不能把主图当成全帧唯一提交序列。
- 边界：不展开完整 BRDF、同步审计、光栅化公式推导和各 Pass 实现教程；不把 C++ Pass 类、Vulkan render pass/subpass 和 graphics pipeline 混为同一个对象。
- 产物：正文与本提纲、CPP-03 继续阅读、系列 plan、两个阅读首页及根路由中的 Piccolo 行。复用 reading-series 样式，不改共享模板/CSS/JS，不提交推送。

## 正文提纲

1. 对象变化与屏幕变化之间缺少哪些步骤；场景数据、CPU 命令与 GPU 图像三层模型。
2. Mesh、Material、Shader、RenderTarget、Draw Call 与 Pass 的最小词汇表。
3. 整体链路图；区分引擎渲染管线与 GPU 内部图形流水线。
4. MeshComponent 脏描述进入 RenderSwapContext；固定 tickOneFrame 明确是单线程顺序，交换上下文本身不证明独立线程。
5. processSwapData 建立或更新 RenderEntity，按需加载与上传共享网格材质资源；一个对象可有多个渲染 part。
6. RenderScene 视锥/包围盒测试形成不同观察用途的可见列表，CPU 筛选不等于最终像素可见性。
7. 从默认 deferredRender 读阴影、主相机 G-buffer/光照、粒子、后处理、UI 合成；解释前向与延迟的职责差异。
8. Pass、render pass/subpass、graphics pipeline 三种层级；附件流图说明颜色、深度和 G-buffer 的生产与消费。
9. drawMeshGbuffer 按材质与网格分组，绑定资源、写实例数据、录制 drawIndexed；一个对象与一个 Draw Call 不一一对应。
10. mesh.vert、mesh_gbuffer.frag、deferred_lighting.frag 说明顶点变换、光栅化/片元与最终图像的关系。
11. acquire → record → submit → present；信号量与 fence 的不同用途，CPU 返回不证明 GPU 已完成或已经扫屏。
12. 四对象的纸上批次推导与静态源码任务，继续阅读。没有模拟 GPU 的打印程序，避免把教学日志当渲染验证。

## 固定源码证据

路径相对于 Piccolo 仓库。

| 入口 | 已核对范围 |
| --- | --- |
| `engine/source/runtime/engine.cpp` | tickOneFrame：logicalTick、swapLogicRenderData、rendererTick 顺序及 single thread 注释 |
| `engine/source/runtime/function/framework/component/mesh/mesh_component.cpp` | 读取变换与动画，整理 part 描述，addDirtyGameObject，清脏标记 |
| `engine/source/runtime/function/render/render_swap_context.h`、`.cpp` | 两份交换数据、索引交换、就绪检查与消费后重置 |
| `engine/source/runtime/function/render/render_system.h`、`.cpp` | 默认 deferred；processSwapData、资源准备、可见性、Pass 数据与渲染入口 |
| `engine/source/runtime/function/render/render_entity.h`、`render_common.h`、`render_resource.cpp` | 实例矩阵/资源 ID、VulkanMesh 与材质资源、按需取得或创建资源 |
| `engine/source/runtime/function/render/render_scene.cpp` | 主相机和光源分别产生可见节点；主相机视锥与变换包围盒相交测试 |
| `engine/source/runtime/function/render/render_pipeline.cpp` | wait/reset/acquire，阴影和主相机 draw，debugdraw、submitRendering；提交后还有粒子复制与模拟 |
| `engine/source/runtime/function/render/passes/main_camera_pass.cpp` | 附件、subpass、shader/graphics pipeline，分组、实例批次、绑定和 draw；UI 合成写交换链附件 |
| `engine/source/runtime/function/render/passes/combine_ui_pass.cpp` | 输入附件与全屏三角形命令 |
| `engine/shader/glsl/mesh.vert`、`mesh_gbuffer.frag`、`deferred_lighting.frag` | 实例 model matrix、proj_view、G-buffer 编码、input attachment 读取与光照 |
| `engine/source/runtime/function/render/interface/vulkan/vulkan_rhi.cpp` | acquire/begin/end，vkCmdDrawIndexed、vkQueueSubmit、等待呈现信号量、vkQueuePresentKHR、fence |
| `engine/source/runtime/function/render/render_type.h` | RHI_SUCCESS 为 true，避免把 prepareBeforePass 的 bool 语义与 VkResult 混读 |

## 外部规则资料

使用 Khronos Vulkan 规范及 refpages 核对图形流水线、光栅化、片元操作、render pass/subpass、DrawIndexed、Acquire、Submit 与 Present。解释固定 Piccolo 实际使用的 API，不迁移为新版扩展教程。

## 验证记录

- 固定源码 SHA 已回读为 `f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，源码工作区未修改。逐项核对实例与资源、可见节点、批次、主相机阶段、附件与提交呈现；API 语义与 Khronos 一手规范对照。
- 正文 4 个 C++/GLSL 源码块，在去除空白与省略的行注释后均匹配固定源码。4 条顺序核对通过：tickOneFrame、deferredRender、MainCameraPass::draw 和 submitRendering。顺序检查包括主画面提交后仍有粒子复制与模拟。
- 19 个固定源码引用对应存在的文件或目录，7 个正文 relref 均存在。新篇、三篇 C++、架构地图与两个首页共 224 处生成后本地链接、锚点与资源引用检查通过。
- Hugo v0.158.0 本地构建退出码 0，生成 1348 页、52 个静态文件，ERROR 为零。
- 浏览器确认原有 reading-series 样式、引擎与 Piccolo 面包屑、4 张表和 2 张 Mermaid SVG；图中文字与箭头可读，无图语法错误、无页面整体横向溢出。
- 新篇上一篇指向 CPP-03，CPP-03 下一篇指向渲染新篇；返回系列仍为 Piccolo 首页 chapters 锚点。Piccolo 与引擎首页均有实际新链接，原先同题待写占位已移除。
- 纸上案例只依据指定假设推导主相机 G-buffer 段的两个实例批次，不作为全帧绘制计数或 GPU 运行证据。未编写打印日志模拟渲染的程序。
- 自查区分了逻辑与渲染表示、上下文交换与交换链、对象/实体/Draw Call、引擎 Pass/render pass/subpass/graphics pipeline、CPU 录制/GPU 执行/屏幕呈现；不宣称通过本次源码核对证明同步正确性。
- Piccolo 未构建、未运行或捕获图形帧；本次仅本地文章与导航修改，未暂存、提交或推送。其他任务及前几篇的未提交成果保留。
