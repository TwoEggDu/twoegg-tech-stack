# Piccolo PCPP-2：窗口与编辑器回调怎样绑定、调用和注销

> 执行层提纲与证据记录；篇级状态统一在 `piccolo-engine-series-plan.md`。

## 定位与范围

- 用户指定 PCPP-2，延续先补文章、后做引擎运行调试的安排。类型为原理篇，主线是事件源、转接、注册表、目标对象和退出边界。
- 先建立“保存可调用对象不等于拥有接收者”的模型，再展开成员函数指针、std::bind、std::function 和捕获；不替代公共 CXX-05 的完整课程。
- 固定公开源码 `BoomingTech/Piccolo@f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`；随仓 GLFW 头文件为 3.3.5，ImGui 以该提交中的后端为准，不套用最新版后端实现。
- 网页插在 PCPP-1 后、GFX-01 前，series_order 为 5；主线编号和旧 URL 不改。
- 完成正文、相关阅读、canonical plan 和根系列路由；不修改引擎源码、公共模板或 CSS，不提交推送。

## 正文提纲

1. 为什么已注册不等于会收到事件，回调仍存在不等于接收者仍存活。
2. 五个角色模型；注册阶段保存调用方式，分发阶段才提供事件参数。
3. WindowSystem 的 std::function 签名、vector 追加；成员函数指针需要对象，bind 固定裸 this 并映射占位符。鼠标三参数到两参数的适配。
4. GLFW user pointer 和 static trampoline；运行时先注册、编辑器后注册；ImGui 安装新入口并链到旧入口，而非凭空覆盖失联。
5. 键盘事件从 pollEvents → ImGui → WindowSystem → 两个输入接收者；模式在处理函数内部过滤，编辑器命令在后续 tick 消费；不承诺所有回调只发生在 pollEvents。
6. 编辑器属性 UI 的 Lambda 表：按字段类型同步调用；this 捕获不保活，引用捕获 asset_folder 追到 ConfigManager 的成员引用，不能仅因构造函数结束就断言悬空。
7. 注册与真实调用的反例：close 只设标志、size 只更新尺寸、reset 无触发入口；分别区分已存回调与可达事件链。
8. 注销事实：没有按订阅移除接口，clear 并非注销；正常退出停止主循环后删编辑器接收者，随后 shutdown；WindowSystem 析构终止窗口源。ImGui 后端有恢复接口，但项目主源码没有调用证据。
9. 设计边界：先停事件/移除订阅再删接收者；weak 捕获与 token 注销各自解决什么；不能在分发 vector 时直接增删，不能在 GLFW 回调里直接销毁窗口。
10. 独立 C++17 正常程序：bind 参数、包装复制不保活、引用别名捕获、弱引用过期跳过与订阅仍在、强捕获保活及显式注销释放。失效指针不调用。
11. 源码练习和未来断点观察表，分别登记源码证据、独立模型验证和引擎运行状态。

## 固定证据

| Piccolo 相对路径 | 核对内容 |
| --- | --- |
| `engine/source/runtime/function/render/window_system.h`、`.cpp` | 类型别名、注册列表、static 转接、分发、窗口关闭与析构 |
| `engine/source/runtime/function/input/input_system.h`、`.cpp` | 游戏输入注册、模式过滤与 clear 的真实作用 |
| `engine/source/editor/include/editor_input_manager.h`、`source/editor_input_manager.cpp` | 编辑器注册、鼠标参数、命令标记与窗口关闭处理 |
| `engine/source/editor/include/editor_ui.h`、`source/editor_ui.cpp` | Lambda 表、字段类型调用、asset_folder 捕获与 DPI 直接回调 |
| `engine/source/runtime/resource/config_manager/config_manager.h`、`.cpp` | getAssetFolder 返回成员 const 引用 |
| `engine/source/runtime/function/render/passes/ui_pass.cpp` | 初始化 ImGui GLFW 后端并启用安装回调 |
| `engine/3rdparty/imgui/backends/imgui_impl_glfw.cpp` | 保存旧入口、先转调旧键盘入口、Shutdown 恢复 |
| `engine/3rdparty/glfw/include/GLFW/glfw3.h`、`src/window.c` | 3.3.5 版本、窗口销毁清空回调、回调内销毁限制 |
| `engine/source/editor/source/main.cpp`、`editor.cpp`、`editor_global_context.cpp` | 初始化、主循环与 clear 时的 delete |
| `engine/source/runtime/engine.cpp`、`function/global/global_context.cpp` | pollEvents 时点、关闭标志与 shutdown 次序 |

语言与库规则对照 C++ 标准工作草案的 func.bind.bind、func.wrap.func、expr.prim.lambda.capture，以及 GLFW 3.3 官方指南；仅使用适用于 C++17 的规则。

## 验证记录

- 回读源码 SHA 为 `f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，克隆工作区干净；正文 13 个原文 C++ 片段去除空白后匹配固定源码，18 个固定版本链接和全部 relref 目标存在。
- 专门核对了关闭入口只设置标志、尺寸入口不调用列表、reset 无实际调用点、WindowSystem 无按订阅移除接口、项目主源码中无 ImGui_ImplGlfw_Shutdown 调用，以及 getAssetFolder 返回成员引用；未将这些静态结果登记成运行观察。
- 从正文直接提取完整教学程序到 `%TEMP%/TechStackShow-piccolo-callbacks-pcpp2-20261025/main.cpp`。Windows x64、MSVC 14.51.36231 工具集，使用 `/std:c++17 /utf-8 /EHsc /W4 /Od /MDd /D_ITERATOR_DEBUG_LEVEL=2`；编译退出 0、无警告，运行退出 0，断言和五行输出均通过。
- 独立程序验证 bind 参数映射与多余参数不转发、函数包装复制不保活裸指针目标、引用别名捕获追到实际对象、弱捕获过期跳过但订阅仍在、强捕获保活及显式注销释放。没有调用任何失效指针，没有运行 GLFW、ImGui 或 Piccolo。
- Hugo v0.158.0 构建退出 0，ERROR 为零，生成 1366 页、54 个静态文件；构建输出和缓存使用上述系统临时目录。全站页数包含共享工作区其他新增内容，不将页数增量都归于本篇。
- 15 篇 Piccolo 正文的 series_order 连续唯一为 0～14；实际生成的上一篇/下一篇为 PCPP-1 ↔ PCPP-2 ↔ GFX-01，双向检查通过。新页含一段 Mermaid 图源，沿用共享 series-reading.css。
- 15 篇正文、两个阅读首页和公共 C++ 入口共 18 个生成页，1064 处站内链接、锚点与资源引用通过；三个入口均链接新篇，03 与 08 有按需回链。
- 本轮变更为 16 个已有文件与 2 个新文件；其中 7 篇原文仅调整 series_order 和 weight，其余为正文回链、首页和规划。已对写作前快照检查改动范围，限定路径的 git diff --check 通过，暂存区为空，源码克隆保持干净。
- 公共 CXX-04/05 保持待写，PCPP-3 保持待补桥；根 doc-plan 只更新 Piccolo 系列汇总为 15 篇，不回填篇级清单。引擎构建/运行、实际事件和退出断点、浏览器目视与 Mermaid 实际渲染仍待验证。
- 本次未修改引擎源码、公共模板/CSS，未暂存、提交或推送；保留共享工作区无关改动。

## A12 原始回执复核（2026-10-25）

只读复核既有`%TEMP%/TechStackShow-piccolo-baseline-01-20261025/verification-receipt.json`及`build-clean-debug.log`。固定revision仍f5053707fed4d3f94d270a436fb0d3a8ae54e3e5；历史干净Debug配置0、生成目标0、完整编辑器构建1，原始日志仍含Jolt的C3688 us字面量和fmt的C2653/checked_array_iterator错误。四份代表reflection/serializer/shader生成文件的当前SHA256均与回执相同。

定向检查原source/bin与build-vs18-debug-clean下没有PiccoloEditor.exe，故没有新的引擎运行、事件退出、线程/Trace或GPU捕获。这里是复核旧回执，不声称本轮又做了一次构建。未改源码/工具链时重跑相同已知失败不会产生新证据；不改第三方依赖或安装系统组件来制造通过。下一步是单独解决该固定revision与工具链兼容，再新建build目录重建并保留完整身份，之后执行PCPP各运行矩阵。
