# Piccolo 渲染补充 02：从 GLSL 到 GPU

> 执行层提纲与证据记录。篇级归属和状态由 `piccolo-engine-series-plan.md` 维护，不另建系列。

## 定位与范围

- 用户希望用 Piccolo 源码讲清渲染引擎、着色语言、图形 API、操作系统、驱动和 GPU 的关系。本篇采用原理篇结构，以实际实现承接通用分层模型。
- 基线为 `BoomingTech/Piccolo@f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`；阅读副本 `.codex_tmp/piccolo-source/Piccolo` 保持只读。
- GFX-01 已有一帧总图，主线 07 已有 Wall_1 的批次、缓冲和提交追踪。本篇负责它们之间的语言/编译契约，以及 API 之外的系统边界，不重复完整 Draw 调用链。
- 本篇属于“Piccolo → 引擎无关设计 → Unreal / Unity”的第一阶段。以 GLSL 与 Vulkan 为源码主线，保留语言/API 非一一对应的必要说明；完整 HLSL 对照后置，不声称 Piccolo 已实现 HLSL 或多个图形后端。
- 系统部分先以 Windows WDDM 解释职责；不将其冒充 Piccolo 源码或 Linux/Android 的统一实现。驱动最终代码、硬件 wave 和显示时序需另取证据。
- 本轮交付正文、提纲、必要系列导航和规划状态；不提交或推送，不修补 Piccolo 构建依赖。

## 提纲

1. 同一个材质问题为什么需要跨层定位：区分程序、数据与任务三个输入。
2. 引擎组织、Shader 编译和系统执行的关系图；箭头表示交接/依赖，不是逐帧严格串行时序。
3. 从 `mesh.vert` 到 SPIR-V 和 C++ 嵌入头；解释 `#version 310 es` 与 `-V100` 分属不同约束，CPU 编译器不会把 Shader 翻成 CPU 版光照函数。
4. Shader module 怎样与入口、pipeline layout、顶点格式和固定功能状态共同组成 graphics pipeline。
5. 以模型矩阵和纹理说明 `location`、`set/binding`、动态偏移与数据布局，形成 Piccolo 接口检查表；完整 HLSL/GLSL 对照留到后续引擎扩展。
6. 录制、提交和呈现的不同边界；Loader、驱动与 Windows 内存/调度职责，区分 API queue、系统调度与 GPU 内部调度。
7. GPU 可编程与固定功能如何协作；源码不能给出特定设备的寄存器、wave 利用率和瓶颈结论。
8. 两个 Shader 的独立编译与头文件嵌入验证；列出后续设备运行、资源捕获与系统时间线的最小任务。
9. 与现有 Piccolo 及通用文章组成阅读路线。

## 源码与资料证据

| 文件 / 文档 | 核对点 |
| --- | --- |
| `engine/shader/CMakeLists.txt` | Shader 源文件、include 目录与生成目标 |
| `cmake/ShaderCompile.cmake` | glslangValidator 的 `-V100`、SPIR-V 与 C++ 头生成命令 |
| `cmake/GenerateShaderCPPFile.cmake` | 二进制字节嵌入 `std::vector<unsigned char>` |
| `engine/CMakeLists.txt`、`engine/source/runtime/CMakeLists.txt` | 固定平台工具/库路径及 Runtime 链接 |
| `engine/shader/glsl/mesh.vert`、`mesh_gbuffer.frag` | 阶段输入输出、实例矩阵、资源组与纹理采样 |
| `engine/shader/include/structures.h` | GLSL 实例结构 |
| `engine/source/runtime/function/render/passes/main_camera_pass.cpp` | 模块、pipeline layout、图形状态、实例写入与动态绑定 |
| `engine/source/runtime/function/render/interface/vulkan/vulkan_util.cpp` | codeSize、pCode、vkCreateShaderModule |
| `engine/source/runtime/function/render/interface/vulkan/vulkan_rhi.cpp` | GLFW surface、函数分派、Draw 录制、提交与 Present |
| Khronos HLSL、Loader、Pipelines、Queues、Present 文档 | 跨语言编译、API/实现边界、逻辑流水线及呈现语义 |
| Microsoft WDDM 内存/调度、DXGI flip model 文档 | Windows 责任分工与显示路径的条件；DXGI 文档不证明本例 Vulkan 使用了特定内部路径 |

## 验证安排

- 回读固定源码 SHA 与工作树状态。
- 用仓库自带 glslangValidator 对 `mesh.vert` 和 `mesh_gbuffer.frag` 执行原脚本对应的 `-V100` 编译；输出位于独立临时目录，不改源码。
- 用原嵌入脚本生成两份 C++ 头，比较头内字节与 SPIR-V；核对魔数、入口、资源装饰，不把它当作 GPU 运行证明。
- 核对正文节选、固定源码链接、站内 relref、系列顺序与生成页；运行 Hugo。
- 本篇没有设备程序执行、GPU 抓帧、原生 ISA 分析、系统时间线或显示延迟实测。

## 完成记录

- 2026-10-25 后续路线调整：用户明确先写 Piccolo，再提炼引擎无关设计，最后扩展 Unreal/Unity；已收敛本篇语言对照表、回写两份 canonical plan 与阅读入口。此调整未改变编译输入，不新增运行验证结论。
- 本轮固定源码回读为 `f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，阅读副本工作树干净。
- 仓库自带 Windows 编译器输出 `Glslang Version: 10:11.7.0`。按原 `-V100` 参数分别编译 `mesh.vert` 与 `mesh_gbuffer.frag`，退出码均为 0，产物大小分别为 10,348 / 6,688 字节；SPIR-V 魔数 `0x07230203`，版本字段 `0x00010000`。
- 原 CMake 嵌入脚本退出码均为 0，头文件中的完整字节序列与对应 SPIR-V 一致。`-H` 检查输出的二进制也与原产物一致。
- SPIR-V 文本核对了 Vertex/Fragment 的 main 入口、set/binding 和 location。顶点产物中模型矩阵为 RowMajor、Offset 16、MatrixStride 16，实例数组 ArrayStride 80；与公共头显式 row_major 声明对应，不推广为语言默认值。
- SHA-256：mesh.vert 为 `61F91DAEBCE87F389CC6DC3FE7ACAED69CCB83DB76B7053BCFDB019132A74641`；mesh_gbuffer.frag 为 `4DA9FD3EADEE358A58568230253119E9368FA285EF6C04C48D8F184AE7AB0718`。产物、日志及 JSON 回执在 `%TEMP%/TechStackShow-piccolo-stack-20261025`。
- 首次尝试向现有 `.codex_tmp` 写实验目录被本机权限拒绝，改用临时目录。系统安装的 Hugo 在沙箱内不可执行，改用仓库已有 `.codex-bin/hugo.exe`；没有提权或安装组件。
- Hugo v0.158.0 构建退出码 0、ERROR 为零；默认站点 baseURL 构建通过，随后为浏览器检查用 localhost baseURL 生成临时预览。共享工作区同时有其他文章产出，页面总数不作为本任务增量指标。
- 新篇 4 个 GLSL/CMake 节选与固定源码匹配；15 个固定源码文件链接与 7 个唯一正文 relref 目标存在。新篇、GFX-01、主线 07、Piccolo 首页、游戏图形系统索引共 5 页的 277 处生成后站内链接/锚点/资源引用检查通过。
- 浏览器确认 reading-series 样式、5 张表、1 张 Mermaid SVG；图中文字和箭头可读，1265 像素视口下页面 scrollWidth 同为 1265，没有整体横向溢出。桌面目视不代替移动端测试。
- 本轮把新篇作为 GFX-02 追加在主线之后；共享工作区其他任务补齐 PCPP-2/3 后，保留其顺序变动，当前 series_order 为 0—16 且唯一。通过 GFX-01、主线 07、Piccolo 首页与游戏图形系统索引提供主题入口；根路由只同步系列概况。
- 本轮没有运行 Piccolo、执行 GPU 捕获、分析原生 ISA 或测量显示时序；没有修补引擎依赖，没有提交、推送或检查线上部署。此前跨系列审查中的旧文修正清单未在本轮批量实施。
