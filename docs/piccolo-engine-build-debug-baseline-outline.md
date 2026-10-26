# Piccolo 主线 01：建立构建与调试基线

> 执行层提纲与证据记录；篇级状态统一在 `piccolo-engine-series-plan.md`。

## 定位与范围

- 用户要求继续完成剩余文章，对应 01《建立构建与调试基线：源码、生成文件、配置与程序怎样对应》。复用已有文章方法，原理篇，以实际构建尝试贯穿源码、生成、编译、运行与调试证据的对应关系。
- 固定输入为 `BoomingTech/Piccolo@f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`。2026-10-25 回读 SHA、原克隆工作区干净；从 Git archive 展开到 `%TEMP%/TechStackShow-piccolo-baseline-01-20261025/source`，不构建或改写阅读用克隆。
- 环境实测：CMake 4.3.1，Visual Studio Community 2026（18.11.12315.225），MSVC 编译器 19.51.36260，工具目录 14.51.36231，Windows SDK 10.0.26100.0，x64，Debug；系统存在 Vulkan SDK，但该固定 CMake 的 Vulkan 编译输入使用仓库内 SDK。
- 文章交付与引擎验收分开。已有成功的配置、反射/序列化生成和 shader 生成回执；完整编辑器编译遇到第三方源码/新工具链兼容错误。没有可启动编辑器，没有引擎断点、首帧或画面证据；不修补上游依赖或安装系统组件来制造通过结论。
- 网页将 01 插入四篇前置之后、02 之前：series_order=5、weight=499；02—08 的 series_order 和 weight 各递增 1，保留原主线编号。更新首页、系列 plan、根路由，并在 02 与 08 衔接本篇。

## 正文提纲

1. 问题空间：代码能打开、工程能生成、exe 能启动和断点能解释当前源码，是四个不同的状态。
2. 最小证据模型：固定源码/依赖 → 工具链与构建配置 → 反射与 shader 生成 → 编译/链接产物 → 配置/资源 → 实际模块与符号 → 断点现场。
3. 核对工具而不是只看 PATH：vswhere 与 cl 绝对路径、CMake generator/architecture、原仓库所携带工具。
4. 当前配置过程的真实分层错误：沙箱 MSBuild FileTracker E_ACCESSDENIED；常规权限重试可识别编译器；CMake 4.x 对旧第三方最低 policy 版本的报错；命令行外部兼容参数；新目录控制缓存残留。
5. CMake 多配置选 Debug，build_windows.bat 硬编码 Release；不根据一条第三方 Build type: Release 日志推断整个目标配置。
6. 目标依赖：PiccoloParser → PiccoloPreCompile → PiccoloRuntime；PiccoloShaderCompile → PiccoloRuntime；PiccoloEditor 链接 Runtime。分别记录生成文件、Parser/libclang、SPIR-V/C++ 头。
7. 实际生成结果：39 个类对应 40 个 reflection 文件、41 个 serializer 文件，29 个 SPIR-V 和 29 个 shader 头；Transform/LevelRes 写出代码可核对到 05/08 的模板推导。
8. Debug 全量编译边界：旧 fmt 的 stdext::checked_array_iterator 错误，Jolt 的 us 字面量错误；不把生成目标成功等同整个编辑器编译通过，不给未验证的依赖升级修复方案。
9. 产物与路径：源码根/bin 与构建目标目录、开发/部署 ini、main 以 argv[0] 父目录选 ini、ConfigManager 以 ini 父目录解析 BinaryRootFolder。兄弟 build 目录可能破坏 development ini 固定相对层级，明确计算并示范应核对绝对路径。
10. 调试验收设计：exe/PDB 同次构建、实际模块路径、符号状态、main 配置路径断点、startEngine 与首个 WorldManager tick；当前均待执行，提供明确停止和失败分类。
11. 提交前保留 source_revision、build_id、run_id、编译器/生成器/配置、文件 hash 与日志回执；读者可沿 02—08 继续取证。

## 证据入口

| 路径（相对 Piccolo） | 核对内容 |
| --- | --- |
| `README.md`、`build_windows.bat`、根/engine 的 `CMakeLists.txt` | 上游工具说明、Release 脚本、生成目录和目标依赖 |
| `engine/source/precompile/precompile.cmake`、`precompile.json.in` | Parser 参数与生成头入口 |
| `engine/source/meta_parser/CMakeLists.txt`、`parser/main.cpp` | Parser/libclang 输出与 Debug 优化参数 |
| `engine/shader/CMakeLists.txt`、`cmake/ShaderCompile.cmake` | shader 编译、SPIR-V 与 C++ 头输出 |
| `engine/source/runtime/CMakeLists.txt`、`engine/source/editor/CMakeLists.txt` | Runtime 依赖、Editor 链接和后处理复制/资源目标 |
| `engine/source/editor/source/main.cpp`、`runtime/resource/config_manager/config_manager.cpp` | exe 邻接配置和 root folder 解析 |
| `engine/configs/development/PiccoloEditor.ini`、`deployment/PiccoloEditor.ini` | 两种布局的路径差异 |
| `engine/3rdparty/spdlog/include/spdlog/fmt/bundled/format.h`、Jolt `Core/JobSystemThreadPool.cpp` | 本机完整编译的错误入口 |
| 隔离实验的 CMakeCache、生成文件、构建日志和 verification-receipt.json | 实测阶段、工具版本与退出码 |
| CMake 官方构建配置/最低 policy 版本文档、Microsoft 官方符号文档 | 多配置语义、兼容开关与模块/PDB 验证方法 |

## 验证记录

- 初次沙箱配置失败，CMakeConfigureLog 显示 MSBuild FileTracker 的 TypeInitializationException/UnauthorizedAccessException、HRESULT 0x80070005；cl 绝对路径可输出版本。工具授权后的常规权限配置能识别同一编译器，未修改系统或工具安装。
- 常规权限配置遇 tinyobjloader 的 CMake 最低 policy 兼容错误。使用官方支持的 `'-DCMAKE_POLICY_VERSION_MINIMUM=3.5'` 外部参数后配置/生成退出码为 0。第一次未整体加引号时参数被拆成 3 和 .5，已改正；最终文章只提供完整引号写法。
- 初次配置失败目录出现空 Debug flags 缓存，另建 `build-vs18-debug-clean` 重新配置以排除该残留；不复用为成功调试基线。
- `PiccoloPreCompile PiccoloShaderCompile` 首次构建退出码 0；观察到 Parser 完成 39 个类的生成，实际文件数量与上述记录一致。原源码克隆保持干净。
- 首次 `PiccoloEditor --config Debug` 返回 1；fmt checked_array_iterator 与 Jolt us 字面量错误已保存，尚无编辑器 exe。全新目录 `build-vs18-debug-clean` 复核：配置退出码 0，Debug flags 为 `/Zi /Ob0 /Od /RTC1`；生成目标再次完成，完整编辑器构建仍返回 1，两类第三方错误仍在。配置输出中的 spdlog Build type: Release 不作为多配置目标的实际构建配置依据。
- 正文已完成，包含本机环境、阶段退出码、目标依赖、生成数量和两个实际生成片段，完整编译与后续运行的边界在首屏、实测表和后续断点设计中明确区分。
- 4 个 C++/bat 节选与固定源码或本次实际生成文件匹配；17 个固定源码/许可链接和 8 个正文 relref 目标存在；4 个 PowerShell 代码块通过 AST 语法解析。
- 全部 2,995 个 archive 来源文件经过 Git blob 核对：2,302 个文件仅有 LF/CRLF 差异，换行归一化后无源码内容变化；没有第三方修补或系统组件安装。原阅读用克隆仍干净。
- 实际路径计算确认：并列 build 的 development ini 指向实验根的 bin，构建脚本资源目标为 source/bin，两者不同。该结果是配置/路径推导，因无可启动编辑器，没有把它记成运行故障。代表生成文件的 SHA-256 与阶段结果已保存于隔离实验根 `verification-receipt.json`。
- Hugo v0.158.0 构建退出码 0，生成 1357 页、53 个静态文件，ERROR 为零；输出为 `%TEMP%/TechStackShow-piccolo-baseline-01-site-20261025/public`。
- 新篇、此前 Piccolo 正文及两个首页共 15 页、887 处生成后站内链接/锚点/资源引用检查通过。本篇 7 张 HTML 表、1 段 Mermaid 图源，沿用 series-reading.css；图源检查不代替浏览器目视。
- 系列 `series_order` 为 0—12 且唯一；已确认 GFX-01 → 01 → 02、02 返回 01、返回 chapters 与两个首页入口。01 已写入 canonical plan；05/CPP-03 的实际生成状态同步引用本篇，其他文章原有运行待验证状态保留。
- 本次共 13 个任务文件：两份新文件、02—08 七份正文的顺序元数据及相关衔接、两个阅读首页、系列 plan 与根路由。导航与规划范围 `git diff --check` 通过，暂存区为空；共享工作区无关改动保留。
- 不提交/推送；不把本篇写完登记成引擎构建、符号加载或首帧调试通过。

## A12 原始回执复核（2026-10-25）

只读复核既有`%TEMP%/TechStackShow-piccolo-baseline-01-20261025/verification-receipt.json`及`build-clean-debug.log`。固定revision仍f5053707fed4d3f94d270a436fb0d3a8ae54e3e5；历史干净Debug配置0、生成目标0、完整编辑器构建1，原始日志仍含Jolt的C3688 us字面量和fmt的C2653/checked_array_iterator错误。四份代表reflection/serializer/shader生成文件的当前SHA256均与回执相同。

定向检查原source/bin与build-vs18-debug-clean下没有PiccoloEditor.exe，故没有新的引擎运行、事件退出、线程/Trace或GPU捕获。这里是复核旧回执，不声称本轮又做了一次构建。未改源码/工具链时重跑相同已知失败不会产生新证据；不改第三方依赖或安装系统组件来制造通过。下一步是单独解决该固定revision与工具链兼容，再新建build目录重建并保留完整身份，之后执行PCPP各运行矩阵。


## Debug 首帧实测补充（2026-10-26）

原失败已分别用最小探针复现，fmt/Jolt 修复前各退出1、修复后各退出0；带两处补丁完整 Debug 目标退出0。模块、PDB、部署 ini 与路径已实测，01 首屏和阶段表改为当前结果，原失败过程保留。

固定输入仍为 f5053707fed4d3f94d270a436fb0d3a8ae54e3e5，另加两处明确补丁；与此前仅源码/配置生成阶段区分。详见 [共享执行记录](piccolo-debug-first-frame-validation-outline.md) 和 [复现材料](../examples/piccolo-debug-validation/README.md)。历史验证记录保持其原时间范围，本节更新当前状态。
