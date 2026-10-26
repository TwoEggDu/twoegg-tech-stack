# Piccolo 主线 07：从渲染场景进入 Pass 与 Vulkan 提交

> 执行层提纲与证据记录；篇级状态统一在 `piccolo-engine-series-plan.md`。

## 定位与范围

- 用户指定 07，结合实际源码完成文章和导航；延续先写阅读文章、后开展引擎调试的安排。
- 类型：原理篇。承接 06 的 Wall_1 RenderEntity，回答它怎样进入主相机 GBuffer 绘制，以及命令怎样记录、提交和请求呈现。
- 基线：`BoomingTech/Piccolo@f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，2026-10-25 回读 SHA，源码工作区干净。公开 MIT 源码。
- 条件：已加载且资源准备成功、主相机视锥测试通过、选择 deferred 路径、获取交换链图像与 Vulkan 调用成功、不发生重建。默认管线成员初值为 deferred，但运行分支仍须观察。
- 核心模型：渲染表示 → 可见输入 → 批次和缓冲数据 → 命令记录 → 队列提交 → 呈现请求；每一步分别说明证据边界。
- 不扩展为 Vulkan 初始化教程、完整同步正确性审计或动画/粒子专题；不运行引擎，不把静态检查写成运行结果。
- 新篇网页 `series_order: 10`、`weight: 504`，沿用 reading-series 模板；只修改正文、06 继续阅读、两个阅读入口、canonical plan、根路由的系列摘要及本提纲，不动样式或共享模板。

## 正文提纲

1. 为什么实体中矩阵正确还不足以解释画面；把输入、资源、命令和执行拆开。
2. 最小模型及术语表：引擎 Pass 类、Vulkan render pass/subpass、graphics pipeline、command buffer。
3. RenderSystem::tick：消费后准备上下文、逐帧 CPU 数据、可见列表和 Pass 数据，最后选管线。
4. Wall_1 进入主相机可见列表，节点引用模型矩阵和网格/材质资源；阴影有独立输入。
5. deferredRender 的围栏等待、命令池重置、图像获取、阴影、主相机和 debug draw、提交及粒子后续调用。
6. MainCameraPass 的八个 subpass，GBuffer → 延迟光照 → 粒子 → 后处理 → UI → 合成；阶段图明确为 CPU 记录顺序。
7. drawMeshGbuffer 按材质/网格指针分组，最多 64 实例一批；模型矩阵写入映射的 storage ring buffer，通过 descriptor 和 dynamic offset 连接 shader。
8. 绑定 pipeline、descriptor、顶点/索引缓冲并记录 indexed draw；gl_InstanceIndex 选矩阵，firstInstance=0，不能凭调用栈找到对象身份。
9. GBuffer 的数据经 input attachment 供全屏三角形延迟光照使用；不宣称每个对象对应一条 draw。
10. Vulkan submit 的 acquire semaphore、两个 signal semaphore、graphics fence、present wait；frame slot 与 image index 分开，三份 frame slot 不等于交换链必有三图。
11. 具体重建分支与 bool 语义：RHI_SUCCESS 为 true，prepareBeforePass 正常录制返回 false；错误路径也可能 false，不能把 false 当成功证明。
12. 分层阅读任务、运行时断点与图形捕获观察表；声明均待执行。

## 证据入口

| 路径（相对 Piccolo） | 核对内容 |
| --- | --- |
| `engine/source/runtime/function/render/render_system.cpp`、`.h` | tick 顺序、默认 deferred 管线 |
| `engine/source/runtime/function/render/render_scene.cpp` | 可见列表建立、Pass 静态引用、节点资源和矩阵 |
| `engine/source/runtime/function/render/render_pipeline_base.cpp` | 各 Pass preparePassData |
| `engine/source/runtime/function/render/render_pipeline.cpp` | 正向/延迟分支、阴影和主相机、debug draw、提交及粒子尾部 |
| `engine/source/runtime/function/render/render_pass.h` | 八个 subpass 枚举 |
| `engine/source/runtime/function/render/passes/main_camera_pass.cpp` | attachments、subpass dependencies、pipeline 创建、draw、批次、动态偏移与 draw 参数 |
| `engine/source/runtime/function/render/render_common.h` | 64 实例上限、每帧与每 draw 数据 |
| `engine/source/runtime/function/render/render_resource.cpp` | storage ring buffer 的 host visible/coherent 分配、映射和槽位游标 |
| `engine/source/runtime/function/render/interface/vulkan/vulkan_rhi.cpp`、`.h` | context、fence、acquire、记录、submit、present、重建、三份槽位 |
| `engine/source/runtime/function/render/render_type.h` | `RHI_SUCCESS` 是 true，与 VkResult 数值不能混同 |
| `engine/shader/glsl/mesh.vert`、`deferred_lighting.frag` | descriptor 布局、实例矩阵、投影、GBuffer 输入 |
| Khronos Vulkan 官方 Command Buffers、vkAcquireNextImageKHR、vkQueueSubmit、vkQueuePresentKHR 文档 | 记录/执行、获取/同步、提交 fence 与呈现语义 |

## 验证记录

- 正文 6 个 C++/GLSL 节选去除空白后均匹配固定源码；17 个固定源码/许可链接目标、7 个正文 relref 目标存在，源码克隆仍干净。
- 逐项核对 RenderSystem 阶段顺序、主相机八个 subpass、材质/网格分组、64 实例容量、模型矩阵复制、descriptor 动态偏移、shader 实例索引、indexed draw 参数、submit/present 同步对象与重建分支。额外确认 RHI_SUCCESS 为 true，正常准备与部分错误都可能返回 false，正文未把 bool 当成完整成功状态。
- Khronos 官方 Command Buffers、vkAcquireNextImageKHR、vkQueueSubmit 和 vkQueuePresentKHR 文档已读取，用于区分记录、执行、获取、提交完成与呈现请求；链接在正文相关段落旁。
- Hugo v0.158.0 构建退出码 0，生成 1355 页、53 个静态文件，ERROR 为零。`.codex_tmp` 新建本次构建目录被拒绝，改用允许写入的系统临时目录，不提升权限；输出为 `%TEMP%/TechStackShow-piccolo-pass-07-20261025/public`，缓存为同级 `cache`。
- 新篇、此前 Piccolo 文章及两个阅读首页共 13 页、770 处生成后站内链接/锚点/资源引用检查通过，无缺失目标；正文生成 6 张 HTML 表和 2 段 Mermaid 图源，沿用 series-reading.css。
- 静态确认 06 下一篇指向 07、07 上一篇指向 06，返回系列指向 chapters，两个首页都有新入口；Piccolo 首页移除旧渲染阶段待写占位，系列 plan 和根系列摘要同步。图源检查不代替 Mermaid 实际渲染或浏览器目视。
- `git diff --check` 在本次导航和规划文件范围通过；暂存区仍为空。只修改正文、本提纲、06、两个首页、canonical plan 和根路由，共 7 个任务文件，保留其他已有改动。
- 浏览器目视、Piccolo 构建/运行、断点和 GPU 捕获均未执行。此前本地预览访问被 URL 安全策略拒绝，保持该限制，不换通道绕过。
- 不暂存、提交或推送；保留共享工作区的已有改动。


## GBuffer 实际调试安排（2026-10-26）

用户确认承接06，沿Wall_1在主相机GBuffer批次中的实例槽位继续验证：节点矩阵指针→ring buffer写入地址/动态偏移→Draw参数与descriptor/VB/IB→RenderDoc实际绑定字节和post-VS顶点。复用01产物及真实setPosition控制入口，重新确认对象/实例ID；同一RenderDoc进程另取前后帧，断点样本与捕获帧分别标识，不宣称同一帧或同一offset。保持场景文件不变；本轮不提交/推送，不扩大为所有Pass或同步正确性验收。

## GBuffer 实测结果（2026-10-26）

- 实际采样已完成，复用原Debug产物，无新增引擎补丁。EXE/PDB/ini SHA-256与首帧回执匹配，部署关卡SHA-256保持51a7d1cc2ed20fb68758c7b5663d354fae1a29acd6c7e65443918a9442c0169c。
- 同一PID187472：修改前RenderDoc帧6348 → CDB真实setter与CPU帧计数11659 → 修改后捕获28131。TargetControl初次连接重发已有捕获，另行查询确认id0/id1；未将第一条NewCapture消息误记为新帧证据。
- GObjectID2/part0/实例5，网格5/材质2，GBuffer批7实例且目标槽0。模型矩阵指针与节点/实体关联；实例stride80、matrixoffset16，dynamic offsets=[4277600,4314128,0]。CPU映射起点0x2223079d410，导出7×80=560字节。
- CDB实际vkCmdDrawIndexed路径为deferredRender→MainCameraPass::draw→drawMeshGbuffer→cmdDrawIndexedPFN，36×7，三个起始参数0。RenderDoc两帧的/BasePass/Mesh GBuffer事件177/subpass0与之一致；实例descriptor Resource149，effective offset4314128/range5120。进程地址/资源号/事件号不推广为固定常量。
- 回放两帧的pipeline285、VS283、FS284、VB716/717/718与IB732相同，输入字节和PV矩阵相同；另六份实例矩阵不变。修改后七份实例矩阵与CPU样本最大差0。目标36条post-VS world delta=(1,0,0)误差0，world等式误差9.71e-8、clip最大1.23e-7。
- 保存脚本曾因qrenderdoc无__file__失败，改用工作目录/显式PICCOLO_GBUFFER_CPU定位语义样本后，重新执行同一回放，两份结果成功导出。检查JSON无error及所有数值，不只检查GUI进程启动退出码。
- CDB日志保留raw pointer误用_Ptr、descriptor下标0和.dx探测错误及纠正。watch脚本纳入实际手动确认的global descriptor下标1。RenderDoc查询的读写资源分类不当作shader写入证据。
- 本轮queue submit/present成功；捕获debug messages空不消除启动API-Version-Violation。CDB q终止，不计正常退出；不验收原生编辑器交互、GBuffer片元/附件、完整同步、重建或性能。
- 正文07新增实测，06只增独立后续证据入口；两个阅读入口与canonical系列状态同步，根plan只改系列摘要。独立材料见[GBUFFER-DRAW](../examples/piccolo-debug-validation/GBUFFER-DRAW.md)，原始RDC与padding字节不入库。
- 本轮本地Hugo v0.157.0退出0、ERROR为零，生成1425页/57静态文件；Piccolo的27篇正文与两个阅读入口共29页、1876处站内链接/锚点/资源检查通过，证据文件SHA-256均匹配。未作线上部署验收或浏览器像素级检查；保留共享工作区无关改动，暂存区未改，不提交/推送。

## 衔接单点GBuffer与延迟光照（2026-10-26）

沿同两份捕获继续验证目标像素：实际viewport投影→唯一slot0/triangle4→GBuffer A/B/C与深度原始字节→事件186的input attachments与shader输入→RGBA16F读回。具体表面量化、Lo/La/Libl及未执行阴影分支集中回填16；07只补边界衔接和入口。旧坐标改落triangle3而非必然换对象，保留此实际观察。详见[16执行记录](piccolo-engine-material-light-shadow-outline.md)和[独立材料](../examples/piccolo-debug-validation/GBUFFER-LIGHTING.md)，不将单点证据升级为全部Pass/同步/最终合成。
