# Piccolo Debug 构建与启动首帧验证（2026-10-26）

## 目标与输入

按用户明确授权，解决完整 Debug 编辑器构建并实际调试启动到首次主画面提交/呈现，回填现有 01、02、03 与规划；不新增主线编号，不提交或推送。

- 原阅读克隆：`.codex_tmp/piccolo-source/Piccolo`，固定 `f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，完成后再次核对干净。
- 隔离实验：`%TEMP%/PiccoloDebug20261026/source`，由固定提交 archive 展开；构建目录为同根 `build`。仅改两处第三方兼容代码，没有改原克隆或安装系统组件。
- 工具：CMake 4.3.1、VS Community 2026、MSVC 19.51.36260（toolset 14.51.36231）、Windows SDK 10.0.26100.0、Visual Studio 18 2026 / x64 / Debug；实际 cache 为 `/Zi /Ob0 /Od /RTC1`。代码生成工具自身的优化参数仍按原 CMake，不声称所有第三方/生成工具都无优化。
- 本机设备清单有 NVIDIA RTX 2060、驱动 32.0.15.6094；不把设备清单单独当成 Vulkan 设备选择证据。调试器为 CDB 10.0.19041.685 AMD64，捕获工具为已有 RenderDoc 1.43。

## 根因与修补

1. 先用最小编译探针分别复现 fmt 的 C2653/C2061 和 Jolt 的 `us` C3688；修复前各退出 1。
2. fmt `format.h` 的 checked iterator 分支增加 `_MSC_VER < 1951` 条件，较新 MSVC 使用原有普通指针分支。依据 Microsoft 明示的 Build Tools 14.51 移除行为，不全局关闭 `_SECURE_SCL` 或 Debug 检查。
3. Jolt `JobSystemThreadPool.cpp` 显式包含 `<chrono>`，消除对间接包含的依赖；没有改变等待或任务策略。
4. 相应探针修复后各退出 0；全新目录配置 0，完整 `PiccoloEditor --config Debug --parallel 8` 构建 0，后处理复制成功。
5. 从原提交重新 archive 两个文件，`git apply --check` 和实际应用均退出 0；应用后的原始字节 SHA-256 与真实构建输入一致。回执同时区分 CRLF 原始字节和 LF 归一化文本指纹，避免混用。

补丁及 8 份路径归一化日志、CDB 命令与 JSON 回执保留在 [复现材料](../examples/piccolo-debug-validation/README.md)。原始日志、EXE/PDB、资源及完整 capture 仍在上述本机实验目录；临时目录不是长期产物存储。

## 第一次交互调试：正常启动与退出

- 运行 `source/bin/PiccoloEditor.exe`，部署 ini 的 `BinaryRootFolder=.`；未修改开发 ini。CDB `lmvm` 显示对应 private PDB symbols；EXE/PDB/ini 字节 hash 已回读确认。
- 一次性断点实际到达 main、startEngine、startSystems、配置初始化、editor.initialize、editor.run、tickOneFrame、WorldManager.tick/loadWorld、交换、deferredRender、submitRendering。
- 第一次世界 tick 入口 `m_is_world_loaded=0`；第一次 present 后读取全局对象：标志 true，loaded_levels 大小 1，键 `asset/level/1-1.level.json`。默认 world 为部署 ini 的 `asset/world/hello.world.json`，root/asset 路径与 bin 布局对应。
- 在 `vulkan_rhi.cpp:494`、首次 `vkQueuePresentKHR` 返回后暂停：end command buffer、reset fence、queue submit、present 的局部结果均 VK_SUCCESS；在途帧资源/交换链图像索引分别为 0/0。调用栈包含默认 deferredRender 与编辑器入口。
- 继续后正常关闭窗口，命中 main 调用的 shutdownEngine，卸载默认关卡；CDB `.lastevent` 为进程退出码 0。这是正常退出记录，不用调试器杀进程代替。
- 入口探索性局部读取出现过序言前 `0xcccc`/NatVis 读取失败，未用于结论；以 present 后的有效对象读取为准。首次 delta 受断点影响约 5.45 秒，不报告为性能。
- 原先非交互 CDB 尝试的 stdin 关闭导致调试提前停止，未列为正常退出成功记录；归档的 `debug-first-frame.log` 来自后续交互运行。

## 第二次断点采样：槽位和外层线程

- `engine.cpp:75` 前索引 0/1，`:77` 后 1/0；processSwapData 入口槽0对象 optional 有值，返回后 `render_system.cpp:103` 读为 nullopt。
- logicalTick、InputSystem.tick、交换前、消费入口、首次 present 后、事件轮询前均记录主线程 `0x2784c`；不把采样点一致推广为全部引擎无并行。线程列表有其他线程与 Worker 1～4，但未定位任务体或并行重叠。
- 采样运行恢复后，Windows 自动化接口两次无法激活该窗口，未继续盲试。最后由 CDB 中断并终止该实验，终止回执不记为正常退出；正常退出 0 来自上一节。
- 没有逐项七 optional、对象记录内容、拒绝交换条件、实际输入消费、Jolt 任务/等待或 GPU 时间线验证。

## 独立图像证据

Windows 截图接口两次超时，没有把标题 FPS 当作图像。RenderDoc 另启同一 Debug exe，以 F12 捕获第17931帧，`renderdoccmd thumb` 导出未加工缩略图；已实际查看默认关卡、角色、墙体和编辑器面板。图保留在 [静态资源](../static/images/piccolo-debug-default-scene-20261026.png)，大小/哈希及 64,267,488 字节 `.rdc` 身份在 JSON 中。

这不是 CDB 首次 present 截图，没有捕获重放、逐 Pass 或显示器扫描输出验证。保留工具介入与不同进程/帧身份；RenderDoc 图像不能用于原生性能对比。

## 遗留与下一步

- 保留 LNK4098（MSVCRT 冲突）、MSB8029（临时目录增量构建）及原有 Lua `get<bool>` C4715 等，未加掩盖性链接参数。
- 运行时验证层 DLL 实际来自系统 `D:/VulkanSDK/1.3.261.1/Bin`，没有把仓库 SDK 编译配置当成加载路径。没有做零验证消息或全平台兼容承诺。
- 本轮只升级 01/02/03 对应的基线、正常启动/退出与首轮交接证据；角色输入、碰撞、相机、动画、脚本、粒子、资源/着色/后处理分项和编辑器拾取/保存等继续按各篇矩阵验证。优先用一个对象的 Transform→渲染表示最小改动接下一次实验。
- 当前原始实验路径在临时目录，需长期使用时按 README 在稳定位置重建；不能直接搬动 CMake cache 后宣称路径仍匹配。

## 站点回归

- Hugo v0.157.0 本轮实际构建退出 0，ERROR 为零；共享工作区生成 1414 页、55 个静态文件，页数不全部归因于本轮。
- 27 篇正文与两个阅读首页共29个生成页、1856处本地链接/锚点/资源检查通过（851处锚点引用）；首页仍覆盖全部27篇，series_order 为0～26且唯一。
- 新图使用相对资源地址，核对在 GitHub Pages 的 `/twoegg-tech-stack/` 项目前缀下仍解析正确。桌面预览实际加载1280×720图像，截图可见场景、图注和实测说明，页面无整体横向溢出；本轮未新增或修改 Mermaid 图和公共模板。
- 01/02/03 的模型、原失败历史和专项未验收边界分别保留。两个阅读首页、原三份outline、首页执行记录、canonical篇级计划和根系列路由已同步；根路由没有回填篇级清单。
- 本轮相关已跟踪改动 `git diff --check` 通过；没有暂存、提交或推送。保留其他任务的未提交内容和生成文件。
