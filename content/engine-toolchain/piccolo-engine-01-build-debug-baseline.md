---
title: "建立构建与调试基线：源码、生成文件、配置与程序怎样对应"
slug: "piccolo-engine-01-build-debug-baseline"
date: "2026-10-25"
description: "从固定 Piccolo 提交出发，实际验证 Windows 工程配置、反射和 shader 生成，记录完整 Debug 构建的兼容阻塞，并建立程序、配置、符号与断点的核对方法。"
tags:
  - "Piccolo"
  - "构建与调试"
  - "CMake"
  - "Visual Studio"
series: "Piccolo 源码阅读与调试"
primary_series: "piccolo-engine"
series_order: 5
type: "reading-series"
topic_key: "engine"
content_stage: "article"
reading_title: "建立构建与调试基线"
reading_summary: "把源码提交、工具链、生成文件、构建产物与配置路径连起来，区分本机实测结果和后续符号、断点验收。"
reading_index: "reading/piccolo-engine.md"
weight: 499
---

阅读引擎源码之前，最好先回答一个问题：调试器将要运行的程序，究竟对应哪一份源码、哪次生成结果，以及哪组配置和资源？

能打开工程，只说明开发工具认识这组文件。生成解决方案、执行代码生成、编译出编辑器、加载正确资源和命中源码断点，分别跨过不同边界。如果这些身份没有固定，后面看似是引擎逻辑的问题，也可能只是旧产物、错误路径或不匹配的符号。

本篇用一次实际 Windows 构建尝试，把这条关系逐层核对。读者已经熟悉调试器，这里重点关注陌生工程中的输入、产物和验证终点。

> **当前实测范围**：固定提交 `f5053707fed4` 的 CMake 配置、反射/序列化生成和 shader 生成已通过；完整 Debug 编辑器构建遇到第三方代码与当前工具链的兼容错误，未生成可启动的 PiccoloEditor。本文没有引擎启动、首帧或断点命中的成功记录；后半部分的运行与调试检查是待执行步骤。
>
> 源码基线：[BoomingTech/Piccolo，完整提交 `f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`][revision]。本篇为主线 01，在阅读顺序中放于前置知识之后、[02 启动篇]({{< relref "engine-toolchain/piccolo-engine-02-startup-first-frame.md" >}}) 之前。本文记录 2026-10-25 的本机实验，不代表其他工具版本或平台已验证。

## 最小基线，是一条能够核对身份的链

可以把基线拆成七层，每层留下独立证据：

| 层次 | 必须固定或记录的内容 | 不能直接推出的结论 |
| --- | --- | --- |
| 源码输入 | 提交、工作区改动、仓库内第三方版本 | 当前工具链能编译它 |
| 工具与配置 | CMake、编译器、生成器、架构、构建配置 | 生成工具和全部目标都成功 |
| 生成结果 | 反射、序列化、shader 中间文件及日志 | 运行时已编译或注册这些结果 |
| 编译产物 | 实际目标、退出码、exe/PDB 与构建身份 | 调试器会启动这份 exe |
| 配置与资源 | 实际 ini、root folder、世界和资源文件 | 场景已被完整加载 |
| 进程与符号 | 进程加载的模块、PDB 与源码路径 | 业务状态已经正确 |
| 断点现场 | 调用栈、参数、对象与关键状态 | 后续一帧或画面已完成 |

后面的工作都围绕这张表进行。一次失败也有价值，前提是能指出失败发生在哪一层，而不是把所有未通过的步骤压成一句“环境没配好”。

## 固定源码，同时给生成输出留出位置

[根 CMake][root-cmake] 拒绝 source dir 与 binary dir 完全相同的构建，但这不意味着所有生成文件都会进入 binary dir。

这份工程还会写入源码树内的几个位置：

- `engine/bin`：PiccoloParser、libclang 和预编译输入。
- `engine/source/_generated`：反射与序列化输出。
- `engine/shader/generated`：SPIR-V 与嵌入用 C++ 头。
- 源码根的 `bin`：编辑器构建后的程序、配置与资源布局。

这些路径由 [Parser 配置][parser-cmake]、[预编译目标][precompile]、[shader 生成][shader-cmake] 和 [Editor 后处理][editor-cmake] 分别决定。只在 CMake 命令里换一个 `-B`，仍可能改变阅读副本中的生成文件。

本次先确认阅读用克隆干净，再从固定提交的 Git archive 展开独立实验副本。源码与构建目录位于系统临时目录下，后续生成没有修改原克隆。读者复现时也应固定一份实验副本，并记录：

```powershell
git rev-parse HEAD
git status --short
```

如果实验副本来自 archive，它没有自己的 Git 元数据，需要由展开时的提交记录证明来源。不要因为找不到 `.git` 就丢掉版本身份，也不要把临时目录当成长期可追溯的产物库。

## 先发现实际工具，不从 PATH 缺项推断未安装

本次使用的环境如下：

| 项目 | 实测值 |
| --- | --- |
| CMake | 4.3.1 |
| Visual Studio | Community 2026，安装版本 18.11.12315.225 |
| MSVC 编译器 | 19.51.36260；工具目录版本 14.51.36231 |
| Windows SDK | CMake 选中 10.0.26100.0 |
| Generator / 架构 | Visual Studio 18 2026 / x64 |
| 请求构建配置 | Debug |

普通 PowerShell 中没有直接找到 `cl`，并不能证明编译器不存在。本次通过 Visual Studio Installer 的 `vswhere` 查到 C++ 工具安装，再用 `cl.exe` 的绝对路径核对版本；CMake 后续也识别到了同一编译器。

[固定提交的 README][readme] 给出 Windows 工具要求；它是工程起点，而本机使用的新工具组合仍需要实际验证。工具版本越新，不能据此推断旧第三方代码一定兼容。

还要区分系统 SDK 与工程真正使用的输入。[engine/CMakeLists.txt][engine-cmake] 明确选择仓库内 Vulkan 头文件、Windows 导入库、glslangValidator 和验证层路径。本机虽然存在系统 Vulkan SDK，但本次 shader 生成使用的是仓库所带的程序。不要仅凭 `VULKAN_SDK` 环境变量，就认定编译输入已经切换。

## 配置阶段：从错误摘要追到实际失败边界

本次第一次受限执行环境中的配置，摘要是：

```text
No CMAKE_C_COMPILER could be found.
No CMAKE_CXX_COMPILER could be found.
```

直接调用 cl 能输出版本。继续检查 `CMakeFiles/CMakeConfigureLog.yaml`，发现 MSBuild 在编译器识别项目中执行 CL 任务时，FileTracker 初始化抛出了 UnauthorizedAccessException，HRESULT 为 `0x80070005`，即 E_ACCESSDENIED。

经过工具权限审核，在常规权限环境执行同一隔离实验的配置后，CMake 正常识别 MSVC。这支持一个有限结论：前一次摘要不足以证明没有安装编译器；本次失败与受限环境下的文件跟踪组件有关。它不构成所有类似错误都应提升权限的通用修法。

越过这一层后，配置又停在 tinyobjloader 的最低 CMake policy 兼容要求：CMake 4.x 已移除对旧 policy 版本的兼容。根据 [CMake 官方说明][cmake-policy]，本次从命令行传入 `CMAKE_POLICY_VERSION_MINIMUM=3.5`，没有改第三方 CMake 文件，随后配置与工程生成通过。

下面的命令展示本次使用的参数结构。`$labRoot` 是读者自己的实验根目录，`source` 应事先准备为固定提交副本；生成器名称也必须与实际安装匹配。

```powershell
$labRoot = 'E:/piccolo-lab'
$piccoloSource = Join-Path $labRoot 'source'
$piccoloBuild = Join-Path $labRoot 'build-vs18-debug'

cmake -S $piccoloSource -B $piccoloBuild `
  -G 'Visual Studio 18 2026' -A x64 `
  '-DCMAKE_POLICY_VERSION_MINIMUM=3.5'
```

最后一个参数在 PowerShell 中整体加引号。本次一度遇到未整体加引号的值被拆开，最终执行和上面的示例均采用完整引号写法。

兼容开关只帮助配置阶段继续，不等于第三方源码兼容性已经修复。生成器、编译器、架构改变，或者前一次失败留下可疑缓存时，应使用新的构建目录。本次就另建了全新目录复核，避免让首次识别失败留下的空 Debug flags 影响判断。

## 选中 Debug，要看实际目标和参数

[build_windows.bat][build-bat] 执行的是：

```bat
cmake -S . -B build
cmake --build build --config Release
```

脚本里的构建配置固定为 Release，不能因为自己想做调试，就把该脚本的产物记成 Debug。

Visual Studio 是多配置生成器，具体配置在构建时选择。本次使用 `--config Debug`；单独设置 `CMAKE_BUILD_TYPE=Debug` 不能替代这一步。[官方配置说明][cmake-config]

生成日志中还会出现第三方项目的 `Build type: Release`。判断实际构建时，应对照请求的 `--config`、生成项目的条件和编译命令，而不能只取其中一条提示。

全新目录的 CMakeCache 中，Debug flags 为 `/Zi /Ob0 /Od /RTC1`。但目标或子目录仍然可能覆盖参数。例如 [PiccoloParser 的 CMake][parser-cmake] 在 Windows 下把该目录的 Debug 参数设成 `/O2 /Ob2`。这说明 Debug 是配置名称；某个工具能否稳定查看局部变量、是否包含相应符号，还要继续检查实际编译与链接结果。

## 先看目标依赖，再分辨生成工具与引擎程序

这份工程中几条关键依赖如下。箭头表示生成或构建依赖，不表示游戏运行期间的函数调用。

```mermaid
flowchart TD
    A[PiccoloParser 与 libclang] --> B[PiccoloPreCompile]
    C[标记过的头文件与模板] --> B
    B --> D[反射与序列化生成文件]
    E[GLSL 与仓库内 glslangValidator] --> F[PiccoloShaderCompile]
    F --> G[SPIR-V 和嵌入用 C++ 头]
    D --> H[PiccoloRuntime 编译与静态库]
    G --> H
    H --> I[PiccoloEditor 编译链接]
    I --> J[程序配置资源的构建后复制]
```

[engine 的依赖声明][engine-cmake] 让 PiccoloRuntime 等待 PiccoloPreCompile，后者依赖 PiccoloParser；[Runtime 目标][runtime-cmake] 还依赖 shader 生成，并被 Editor 链接。PiccoloParser.exe 是构建工具，它成功运行不代表 PiccoloEditor.exe 已经生成。

为了分开观察，本次先执行：

```powershell
cmake --build $piccoloBuild --config Debug `
  --target PiccoloPreCompile PiccoloShaderCompile --parallel 2
```

该次命令退出码为 0。日志记录 Parser 完成 39 个类的生成，随后完成 shader 中间文件与 C++ 头生成。按实际输出目录统计：

| 产物目录 | 实际文件数量 | 代表文件 |
| --- | --- | --- |
| engine/source/_generated/reflection | 40 | 各类反射头及 all_reflection.h |
| engine/source/_generated/serializer | 41 | 各类声明头、all_serializer.h、all_serializer.ipp |
| engine/shader/generated/spv | 29 | mesh.vert.spv |
| engine/shader/generated/cpp | 29 | mesh_vert.h |

数量只说明本次产物集合，不是所有版本都应相同的硬编码指标。还需要检查文件内容是否对应预期类型和 shader。

例如，本次生成的 `all_serializer.ipp` 中，TransformComponent 写出函数包含：

```cpp
ret_context.insert_or_assign("transform", Serializer::write(instance.m_transform));
```

LevelRes 的写出函数则包含：

```cpp
ret_context.insert_or_assign("gravity", Serializer::write(instance.m_gravity));
ret_context.insert_or_assign("character_name", Serializer::write(instance.m_character_name));
```

这两段来自本次实际生成文件，可以与 [05 资源和类型恢复]({{< relref "engine-toolchain/piccolo-engine-05-resource-type-serialization.md" >}}) 及 [08 保存重载]({{< relref "engine-toolchain/piccolo-engine-08-editor-save-reload.md" >}}) 中的声明、模板推导对照。它们证明写出代码已经生成，仍不能证明引擎已编译、运行注册或执行过保存。

## 完整 Debug 构建，实际停在了哪里

生成目标成功后，本次继续执行：

```powershell
cmake --build $piccoloBuild --config Debug `
  --target PiccoloEditor --parallel 2
```

这次命令退出码为 1。在全新构建目录复核时，生成步骤仍然执行成功，完整编辑器构建仍然失败。日志中至少有两组需要分别处理的编译问题：

| 位置 | 实际错误标识 | 当前能下的结论 |
| --- | --- | --- |
| 仓库内 spdlog/fmt 的 format.h | C2653：stdext；C2061：checked_array_iterator | 旧依赖的这条代码路径未通过当前工具链编译 |
| Jolt 的 JobSystemThreadPool.cpp | C3688：us 字面量；伴随 sleep_for 的 C2672 | 另一个第三方目标也有独立的编译阻塞 |

表格摘录错误码和标识符，没有把随后的一串语法错误全部当成独立根因。[fmt 对应代码][fmt-header]、[Jolt 对应代码][jolt-threadpool] 可以用来定位当前失败入口；最终修法还需要独立验证。

本次没有升级依赖、修改标准库相关宏或修补这些第三方文件，也没有安装另一套工具链。因此，文章能够交付一份有证据的阶段记录，但**完整构建与调试基线仍未验收通过**。

目前各阶段的结果应记为：

| 阶段 | 本次结果 |
| --- | --- |
| 固定提交与隔离副本 | 已完成，原阅读用克隆保持干净 |
| 工具发现与 CMake 配置 | 已完成；使用外部 policy 兼容参数 |
| 反射/序列化与 shader 生成 | 已完成，有实际文件与成功退出码 |
| PiccoloEditor Debug 全量编译 | 未通过，第三方编译阻塞 |
| 可启动 exe 与匹配符号 | 尚无可验收的编辑器产物 |
| 实际启动、断点和首帧 | 未执行 |

本次输出目录在系统临时目录中，MSBuild 还报告了 MSB8029 增量构建警告。正式保留的开发工程应使用稳定位置，继续验证时保留当前失败日志即可，不必把一次临时实验变成长期工作目录。

## 编译通过之后，先核对程序和资源布局

以下布局来自构建脚本核对，尚不是本次成功部署的目录快照。[Editor 的构建后命令][editor-cmake] 主要做四件事：

1. 创建源码根的 bin，并复制编辑器界面资源。
2. 把目标输出目录中的文件复制到 bin。
3. 给 bin 放部署版 ini，给目标输出目录放开发版 ini。
4. 更新 bin 下的 asset 副本。

这意味着，至少有两处可能找到编辑器程序：构建树内的配置输出目录，以及源码根的 bin。调试器启动哪一处，不能靠文件名判断。

[main.cpp][main] 对配置文件的选择是：

```cpp
std::filesystem::path executable_path(argv[0]);
std::filesystem::path config_file_path = executable_path.parent_path() / "PiccoloEditor.ini";
```

随后，[ConfigManager][config] 用配置路径父目录和 BinaryRootFolder 的值组合 root folder；[AssetManager][asset-cpp] 再将资源 URL 解析为绝对路径。

两种 ini 的关键区别是：

| 配置 | BinaryRootFolder | 原脚本预期的用途 |
| --- | --- | --- |
| 开发版 | ../../../../../bin | 从常见构建树目标目录回到源码根的 bin |
| 部署版 | . | 使用程序与 ini 所在的 bin 布局 |

具体内容可看[开发版 ini][development-ini] 和[部署版 ini][deployment-ini]。

这里有一个容易被复制命令掩盖的条件。设源码位于 `<lab>/source`：

```text
常见构建位置：
  <lab>/source/build/engine/source/editor/Debug/
  开发版相对路径 → <lab>/source/bin

本次实验采用的并列构建位置：
  <lab>/build-vs18-debug-clean/engine/source/editor/Debug/
  同一相对路径   → <lab>/bin

构建脚本实际准备的资源目标：
  <lab>/source/bin
```

这组路径计算已在本次实验中核对；第二种布局的两个根目录不同。由于完整编译失败，尚未观察到由此产生的实际启动错误，不能把路径推导写成已复现的资源加载故障。

它给出的操作原则很明确：**换构建目录后，要重新计算运行配置中的相对路径。** 后续可以选择保持默认目录层级，或在实验配置中明确使用正确 root folder；如果启动构建后准备的 bin 副本，则应核对其部署 ini、资源和匹配符号。任何调整都要作为运行输入记录。

工作目录也应记录，但不能用“把 Working Directory 改对”替代上面这条路径追踪。这里的配置选择直接依赖 argv[0]，root folder 又依赖配置路径；使用绝对程序路径启动，有助于让这些输入明确。

## 把符号与断点验收，留到实际产物之后

完整编译通过后，才进入这一组检查。本次没有执行以下调试操作。

首先给准备运行的编辑器产物分配构建身份，记录完整 exe 路径及对应 PDB。源码提交相同，不代表不同次链接得到的符号可以混用；Visual Studio 要求符号与对应构建精确匹配。[Microsoft 符号说明][vs-symbols]

在 VS 中将调试 Command 明确指向选定的绝对 exe 路径，记录工作目录和参数。启动后，在 Modules 窗口检查实际模块路径、符号加载状态及符号文件位置，避免项目配置指向 A，实际观察的却是另一份 B。

不要只根据目录里“有一个 PDB”就认定匹配，也不要在源码已变化时关闭匹配检查来掩盖问题。先回到本次构建产物及其原始源码路径，确认调试器正在解释正确的二进制。

然后分三个观察终点推进：

| 观察终点 | 断点位置与应记录的数据 | 通过后能证明什么 |
| --- | --- | --- |
| 入口对应 | main；实际模块路径、argv[0]、config_file_path | 当前程序、符号与配置入口能够对应 |
| 系统启动 | startEngine、ConfigManager::initialize；root folder、默认世界和全局资源路径 | 配置实际进入了这次启动 |
| 首次世界加载 | WorldManager::tick / loadWorld / Level::load；加载条件、URL、结果与调用栈 | 启动链走到了内容恢复，并能解释关键状态 |

这些位置的后续细节由 [02 启动篇]({{< relref "engine-toolchain/piccolo-engine-02-startup-first-frame.md" >}}) 展开。进入 main 不能证明 Vulkan 初始化完成；世界加载函数返回，也不能证明首帧图形工作已经完成。观察终点应随着验证问题逐步后移。

若在 Vulkan 初始化处失败，再核对实际运行机器的设备、驱动、验证层和加载路径。仓库内存在编译头文件与导入库，不等于该进程已经成功取得可用的图形运行环境。本篇没有运行验证，因而不报告设备或驱动可用。

## 以后复现时，留下怎样的一张记录

不必把所有日志贴进正文，但应能从文章回到明确的实验身份。建议至少记录下面这些字段：

```text
source_revision: 固定提交
source_changes: 是否有额外修改及对应补丁
build_attempt_id: 本次配置/编译尝试的唯一记录
toolchain: CMake、生成器、编译器、SDK、架构
configuration: Debug / Release / 其他
generation_result: 目标退出码、代表文件和内容指纹
build_result: 全量目标退出码与首个有效错误
artifact_identity: 成功产物的 exe/PDB 路径与指纹
run_id: 实际启动一次进程后再记录
runtime_inputs: 程序、ini、root folder 和资源路径
debug_evidence: 模块、符号、调用栈与状态
```

本次有源码身份、环境记录、配置/生成回执和两轮完整编译失败记录；没有可用编辑器产物，也没有 run_id 或断点现场。生成结果中的代表文件已经保留内容指纹，便于后续判断重新生成是否改变了输入。

下一次继续，应先解决已记录的第三方编译兼容问题，取得完整目标成功回执，再开展上述程序、配置和符号检查。若改工具链或补丁，给新尝试分配新身份，不能沿用这次记录的结论。

<!-- DATA-TODO: 完整编辑器构建通过后补编译工具/补丁身份、exe 与 PDB 指纹、实际启动路径、Modules 符号状态、main 配置路径、首次 WorldManager tick 的调用栈与状态；当前配置及生成已验证，Debug 全量编译未通过，未运行编辑器。 -->

## 继续阅读

- [从启动走到第一帧：初始化、世界加载与退出怎样衔接]({{< relref "engine-toolchain/piccolo-engine-02-startup-first-frame.md" >}})：用本篇固定的程序和配置身份，继续核对启动链。
- [模板、宏与反射代码生成]({{< relref "engine-toolchain/piccolo-cpp-03-templates-macros-reflection.md" >}})：理解构建阶段为何需要 Parser，以及生成代码怎样参与运行时注册。
- [编辑器怎样修改、保存并恢复场景状态]({{< relref "engine-toolchain/piccolo-engine-08-editor-save-reload.md" >}})：实际保存前，先确认程序解析出的资源路径，避免核对错误的文件副本。
- [Piccolo 阅读入口]({{< relref "reading/piccolo-engine.md" >}})：返回目录，区分已有正文与尚待完成的运行验证。

Piccolo 源码、脚本与实际生成片段版权：Copyright (c) 2022 Contributors to the Piccolo Engine，按 [MIT License][license] 提供。本文环境和退出码来自本次实验；布局图与后续断点表分别为源码/路径核对和验证设计。

**可复现的调试基线，要把源码、生成结果、程序、配置、符号和现场逐项对应，并如实标出停在哪一层。**

[revision]: https://github.com/BoomingTech/Piccolo/tree/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5
[readme]: https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/README.md
[root-cmake]: https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/CMakeLists.txt
[engine-cmake]: https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/CMakeLists.txt
[build-bat]: https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/build_windows.bat
[parser-cmake]: https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/meta_parser/CMakeLists.txt
[precompile]: https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/precompile/precompile.cmake
[shader-cmake]: https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/shader/CMakeLists.txt
[runtime-cmake]: https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/CMakeLists.txt
[editor-cmake]: https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/editor/CMakeLists.txt
[main]: https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/editor/source/main.cpp
[config]: https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/resource/config_manager/config_manager.cpp
[asset-cpp]: https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/resource/asset_manager/asset_manager.cpp
[development-ini]: https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/configs/development/PiccoloEditor.ini
[deployment-ini]: https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/configs/deployment/PiccoloEditor.ini
[fmt-header]: https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/3rdparty/spdlog/include/spdlog/fmt/bundled/format.h
[jolt-threadpool]: https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/3rdparty/JoltPhysics/Jolt/Core/JobSystemThreadPool.cpp
[license]: https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/LICENSE
[cmake-policy]: https://cmake.org/cmake/help/v4.3/variable/CMAKE_POLICY_VERSION_MINIMUM.html
[cmake-config]: https://cmake.org/cmake/help/v4.3/manual/cmake-buildsystem.7.html#build-configurations
[vs-symbols]: https://learn.microsoft.com/en-us/visualstudio/debugger/specify-symbol-dot-pdb-and-source-files-in-the-visual-studio-debugger?view=visualstudio
