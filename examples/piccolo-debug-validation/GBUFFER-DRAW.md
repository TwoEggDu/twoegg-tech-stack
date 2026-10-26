# Wall_1：从实例矩阵到 GBuffer Draw 与顶点输出

2026-10-26，实验身份 `piccolo-gbuffer-draw-20261026`。承接 [06 位置链](POSITION-CHAIN.md)，复用 [01 Debug 基线](README.md) 的固定提交及两处兼容补丁产物。本次没有重编译、修改关卡或新增引擎补丁；独立结果存入 `gbuffer-receipt.json`，不改写先前两轮回执。

## 实验与证据边界

RenderDoc 1.43 启动 PID 187472，在同一进程捕获修改前帧 6348、修改后帧 28131。两者之间，CDB 在主线程、`engine.cpp:70` 的 `logicalTick` 前调用一次真实 Transform setter，使 Wall_1 的 x 从约 1.94597 变为 2.94597。CPU 样本的 `m_frame_count=11659`，不是 RenderDoc 帧号，也不是显示器刷新编号。两份捕获并非相邻帧。

目标身份为 GObjectID=2、part=0、渲染实例ID=5、mesh asset=5、material asset=2。本次目标批次有 7 个实例，Wall_1 位于槽 0；渲染实例 ID 不等于 shader 的实例数组下标。

本轮接通了模型矩阵、可见节点引用、批次、实际缓冲、绑定、Indexed Draw 和回放所得 post-VS 输出。没有验收编辑器鼠标/面板、附件和片元、逐像素最终合成、完整同步时间线、重建分支、性能、泄漏或正常退出。

## 重新采样

1. 按基线 README 配好固定源码、兼容补丁、exe/PDB/ini，核对 SHA-256；在独立实验副本操作，保持默认关卡文件不变。用 RenderDoc 启动 `source/bin/PiccoloEditor.exe`，工作目录设为 `source/bin`，先捕获一帧默认场景，保留 `.rdc`。
2. 使用 CDB 附加到**刚才的实际 PID**，不要另启编辑器。以下路径是占位示例，在交互终端执行：

```powershell
$cdb = 'C:/Program Files (x86)/Windows Kits/10/Debuggers/x64/cdb.exe'
$labRoot = Join-Path $env:TEMP 'PiccoloDebug20261026'
$evidenceDir = 'E:/sources/twoegg-tech-stack/examples/piccolo-debug-validation'
$piccoloPid = 123456 # 替换为刚才通过 RenderDoc 启动的真实 PID
& $cdb -p $piccoloPid -lines -y "$labRoot/source/bin" -srcpath "$labRoot/source" `
  -logo "$labRoot/debug-gbuffer-chain.log" -cf "$evidenceDir/gbuffer-attach.cdb"
```

3. 在安全断点重新识别 Wall_1 与组件，核对旧位置和对应渲染实体；查询过程见 `debug-gbuffer-chain.log` 的 `GBUFFER_TARGET_LOOKUP`。把实际 Transform 指针放入 `$t0`，实际 VulkanMesh 指针放入 `$t4`。不能复制上次进程的地址或依赖对象表排序。
4. 按位置链材料的安全调用方式，用 `.dvalloc 0x1000` 分配参数暂存页，返回地址放入 `$t9`，`ef @$t9 2.94597 -0.38554 0.0` 写入 Vector3，然后 `.call /v PiccoloEditor!Piccolo::TransformComponent::setPosition(@$t0, @$t9)`、`g`。这是调试器控制输入；不要把人工调用栈当成原生编辑器入口。
5. 用 `$><` 载入 `gbuffer-watch.cdb`，逐站继续。在 `main_camera_pass.cpp:2252` 核对矩阵已写入，确认 `mesh_nodes` 中目标矩阵指针所在槽位；在 2318 核对动态偏移和 Draw 参数。该脚本整理自本轮实际断点，global descriptor 下标改为日志中随后手动核对正确的 **1**；`_per_mesh=0`、`_mesh_global=1`。它依赖 `$t4`，不是自动定位全部目标的通用脚本。
6. 记录 `sizeof(VulkanMeshInstance)=80`、模型矩阵相对偏移16，以及映射基址、perdraw 动态偏移和有效实例数。导出有效记录，而不是把整个 ring buffer 当成目标数据。示例 `.writemem <目标文件> <实际起点> L0x230` 对应本次 `7×80=560` 字节；自己的批次数量变化时须重新计算长度。
7. `vulkan_rhi.cpp:2033` 观察真实 RHI 包装调用；本次为 indexCount36、instanceCount7、firstIndex0、vertexOffset0、firstInstance0。494 观察 submit/present VkResult，再记录引擎帧计数、线程并继续运行。
8. 在同一 RenderDoc 进程捕获修改后帧。CDB 附加后，本次窗口激活失败，改用官方 `CreateTargetControl(..., forceConnection=False)`，核对 `GetPID()==187472` 后 `TriggerCapture(1)`。连接初期可能重发**已有** NewCapture 消息，必须按 captureId、帧号和路径区分新旧；第一次收到消息不能直接当作新捕获。随后独立查询收到 id0/frame6348、id1/frame28131，二者指纹见回执。

本次结束使用 CDB `q`，附加目标随调试器终止；这是采样终止，不是正常关闭验收。EXE/PDB/ini 与关卡字节在结束后重新核对，均保持基线值。没有保存位置修改。

原始日志保留失败探测：`.dx` 拼写错误、对 raw pointer 使用 `_Ptr`、先查 `m_descriptor_infos[0]` 得到 null，随后分别纠正。这些调试器表达式错误不是引擎运行失败；以随后正确字段和捕获绑定为准。Vulkan 包装句柄与 RenderDoc ResourceId 属于不同命名空间，不能按数字直接相等。

## 离线回放

本机原始捕获仍在 `%TEMP%/PiccoloDebug20261026/captures/`，两文件分别为 `gbuffer_frame6348.rdc`（64,458,199 字节）、`gbuffer_frame28131.rdc`（64,818,419 字节）。公开仓库不存这两份大二进制；没有原文件时，可以检查导出数据和计算关系，但不能独立重放本次捕获。重新采样后需调整脚本中的文件名与预期 x，并重新定位身份。

从本目录运行已安装 RenderDoc 的嵌入式 Python，无需另外下载绑定：

```powershell
Set-Location 'E:/sources/twoegg-tech-stack/examples/piccolo-debug-validation'
$env:PICCOLO_GBUFFER_LAB = Join-Path $env:TEMP 'PiccoloDebug20261026'
& 'C:/Program Files/RenderDoc/qrenderdoc.exe' --python (Join-Path $PWD 'gbuffer-replay.py')
```

脚本依赖当前目录中的 `gbuffer-cpu-instance-data.json`；也可通过 `PICCOLO_GBUFFER_CPU` 指定完整路径。qrenderdoc 的 `--python` 执行环境没有 `__file__`，不能依赖它定位旁置文件。Windows GUI 程序可能在调用终端先返回；等待报告更新时间变化、读取完整 JSON 并确认无 `error`，**退出码 0 本身不足以认定回放完成**。输出在实验目录的 `gbuffer-replay-report.json`，本次验证后的报告也已保存到本目录。

脚本按 `Mesh GBuffer` marker 下 Draw 的实际实例矩阵定位，事件177是结果，不是硬编码选择条件。它读取 Vulkan pipeline/descriptor、VB/IB，导出目标实例 post-VS，并验证 `world=M×position`、`clip=PV×world`。RenderDoc 通用查询将本次 SSBO 放入 `GetReadWriteResources`；脚本合并只读与读写列表，再按 descriptor store/binding 选取。这个 API 分类不能证明 GLSL 的 readonly 缓冲被 shader 写入。

本次 post-VS 输出 stride80 的解码来自反射与实际格式：20个 float 中，clip为0—3，world为4—6，normal为8—10，tangent为12—14，UV为16—17。它与 CPU 每实例记录的 stride80 恰好相同，字段布局不同。索引最大值35，因此导出36条顶点记录；不是24条，也不是7个实例合计36条。

## 结果与可复核文件

| 检查 | 实际结果 |
| --- | --- |
| GBuffer Draw | `/BasePass/Mesh GBuffer`，event177，subpass0，36索引×7实例，目标槽0 |
| binding | set0/binding1，Resource149，effective offset4314128，range5120 |
| CPU 样本 | mapped base `0x22230380000`；record start `0x2223079d410`；有效560字节 |
| 前后控制项 | pipeline/VS、VB/IB绑定与输入字节、相机矩阵相同；其余六份实例矩阵不变 |
| CPU/捕获交叉核对 | 修改后全部七份实例矩阵与 CPU 样本逐元素相同，最大差0 |
| 顶点数值 | 36条 world 坐标差均为(1,0,0)，最大差值误差0 |
| 变换计算 | world等式最大误差约9.71e-8；clip等式最大误差约1.23e-7 |
| setter 同轮提交 | queue submit / present 均VK_SUCCESS；不据此宣称所有GPU工作完成 |

- `debug-gbuffer-chain.log`：CDB 实际日志，只归一化 `<lab>` 和 `<user>` 路径。
- `gbuffer-attach.cdb`：实际附加命令；`gbuffer-watch.cdb`：由日志整理并纳入 descriptor 下标修正的断点脚本。
- `gbuffer-cpu-instance-data.json`：七份真实记录的语义字段；忽略 padding，原560字节样本哈希另存，原始 `.bin` 留在实验目录。
- `gbuffer-replay.py`、`gbuffer-replay-report.json`：可复跑脚本与本次实际导出；含绑定、矩阵、反射、索引、顶点与误差。
- `gbuffer-vertices.csv`：前后各36条输入/world/clip记录，方便逐行比较。
- `gbuffer-receipt.json`：产物、捕获、脚本、导出数据与原始缩略图指纹，以及限制和站点检查状态。

缩略图由 `renderdoccmd thumb` 原样导出，放在 `static/images/piccolo-gbuffer-wall-{before,after}-20261026.png`。它们用于场景背景，未做逐像素差异归因。post-VS 是回放生成的输出，不是原始 GPU 硬件轨迹；没有验证显示器扫描输出。

捕获的 `GetDebugMessages()` 均为空，但启动控制台实际出现过 `UNASSIGNED-API-Version-Violation(ERROR/SPEC)`，本机还启用了系统 Vulkan Configurator 验证层设置。不能把捕获中消息为空解释成零验证错误；本轮没有改动这些系统设置，也没有完成该启动告警修复。

RenderDoc 脚本接口和 `--python` 入口以安装版本与[官方源代码](https://github.com/baldurk/renderdoc/blob/v1.x/qrenderdoc/Code/qrenderdoc.cpp)、[Python API 文档源](https://github.com/baldurk/renderdoc/blob/v1.x/docs/python_api/index.rst)为依据。Piccolo 源码许可见基线 README。
