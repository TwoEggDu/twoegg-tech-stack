# Piccolo Debug 与启动到首帧：2026-10-26 实测材料

后续专项：[Wall_1 位置修改：Transform 到渲染表示](POSITION-CHAIN.md)，记录独立的 setter、缓冲、描述、实体和可见节点运行证据，不改变本页首帧实验的回执。

输入为 BoomingTech/Piccolo `f5053707fed4d3f94d270a436fb0d3a8ae54e3e5` 加本目录的两处兼容补丁。配置和完整 `PiccoloEditor` Debug 目标均退出 0；CDB 加载对应 PDB，走到第一次 `vkQueuePresentKHR` 返回并正常关闭，进程退出码 0。详见 `verification-receipt.json`。

## 复现构建

在独立实验副本中应用补丁；不要在正在阅读或另有修改的源码树里直接运行。下例的 `$piccoloClone` 指向读者已有的 Piccolo Git 克隆，`$patchFile` 指向本目录补丁的绝对路径。实验目录应事先不存在。

```powershell
$piccoloClone = 'E:/sources/Piccolo'
$patchFile = 'E:/sources/twoegg-tech-stack/examples/piccolo-debug-validation/compat-msvc1951.patch'
$labRoot = 'E:/piccolo-debug-20261026'
New-Item -ItemType Directory -Path $labRoot
git -C $piccoloClone archive --format=tar --output="$labRoot/source.tar" f5053707fed4d3f94d270a436fb0d3a8ae54e3e5
New-Item -ItemType Directory -Path "$labRoot/source"
tar -xf "$labRoot/source.tar" -C "$labRoot/source"
git -C "$labRoot/source" apply --check $patchFile
git -C "$labRoot/source" apply $patchFile
cmake -S "$labRoot/source" -B "$labRoot/build" `
  -G 'Visual Studio 18 2026' -A x64 '-DCMAKE_POLICY_VERSION_MINIMUM=3.5'
cmake --build "$labRoot/build" --config Debug --target PiccoloEditor --parallel 8
```

每步检查 `$LASTEXITCODE`，失败时停止。工具链实测为 CMake 4.3.1、VS Community 2026、MSVC 19.51.36260（工具目录 14.51.36231）、Windows SDK 10.0.26100.0、x64。fmt 补丁仅令 MSVC 19.51 及以后走其现有指针分支；Jolt 补丁显式包含 `<chrono>`。没有关闭全局 Debug 检查或升级第三方版本。

本机实验实际放在 `%TEMP%/PiccoloDebug20261026`；这里只给稳定目录示例。构建后启动 `source/bin/PiccoloEditor.exe`，使用同目录部署 ini（`BinaryRootFolder=.`）、PDB、asset 和 resource。并列 build 目录中的开发 ini 有固定相对层级，不应直接混用。

## CDB 断点

`first-frame-original.cdb` 与 `frame-handoff.cdb` 是实际执行的命令文件。CDB 需 Windows SDK Debugging Tools；本机版本为 10.0.19041.685 AMD64。以下在交互终端执行，保留标准输入；非交互调用的 stdin 关闭可能使调试器提前终止。

```powershell
$cdb = 'C:/Program Files (x86)/Windows Kits/10/Debuggers/x64/cdb.exe'
$commands = 'E:/sources/twoegg-tech-stack/examples/piccolo-debug-validation/first-frame-original.cdb'
& $cdb -lines -y "$labRoot/source/bin" -srcpath "$labRoot/source" `
  -logo "$labRoot/debug-first-frame.log" -cf $commands "$labRoot/source/bin/PiccoloEditor.exe"
```

第一次 present 后暂停在固定源码 `vulkan_rhi.cpp:494`，确认 `res_queue_submit`、`present_result` 都为 `VK_SUCCESS`。再执行：

```text
dx Piccolo::g_runtime_global_context.m_world_manager._Ptr->m_is_world_loaded
dx Piccolo::g_runtime_global_context.m_world_manager._Ptr->m_loaded_levels
dx -r1 Piccolo::g_runtime_global_context.m_config_manager._Ptr->m_root_folder
dx -r1 Piccolo::g_runtime_global_context.m_config_manager._Ptr->m_asset_folder
g
```

本次 CDB 的工作目录为站点仓库，RenderDoc 捕获的工作目录为 `source/bin`；两次都以绝对 exe 路径定位 ini。复现时记录自己的工作目录，ImGui 也可能在其中写出 `imgui.ini`，不要据此误认配置根目录。正常关闭窗口后用 `.lastevent` 核对退出码，再 `q` 退出调试器。不要用 `q` 杀进程代替正常退出验收。原始脚本在个别函数入口还有 `dv/dx/da` 探索性输出：入口可能尚未执行栈帧序言，此时局部变量出现 `0xcccc` 或读取失败，不是有效状态证据；以 present 后从全局对象读取的结果为准。`_Ptr` 是本次 MSVC 的实现字段，其他标准库/版本可能不同。行号也只对应固定源码。

第二次 `frame-handoff.cdb` 记录同一产物的首次交换与消费；到 `TRACE_PAUSED` 后按需继续。实际第二次运行在完成采样后由调试器终止，日志中的 Control-C/断点中断属于调试操作，**不作为正常退出记录**。正常退出证据来自第一次日志。

## 证据文件和边界

- `configure.log`、`build-debug.log`：本轮配置与完整目标日志。
- `fmt_probe-*.log`、`jolt_probe-*.log`：最小编译探针修复前失败、修复后成功。Jolt 探针检查 `100us` 所需的显式 chrono 包含；完整目标另行验证真实第三方 cpp 的修补。
- `debug-first-frame.log`：模块/PDB、启动断点、首次世界加载、第一次提交/呈现结果和正常退出 0。
- `debug-frame-handoff.log`：逻辑/输入/交换/消费/提交/轮询的采样、槽位 0/1→1/0、对象描述有值→nullopt、线程列表。没有跨帧并行时间线或输入延迟测量。
- `verification-receipt.json`：产物、补丁、日志和捕获的身份/指纹；哈希用于对应这次实验，不要求不同机器重新链接后相同。

日志保留原有输出，仅将实验根路径归一化为 `<lab>`、用户目录归一化为 `<user>`。原始日志、exe/PDB、构建目录及 64,267,488 字节的 `captures/piccolo_frame17931.rdc` 留在本机实验目录；未把大二进制放进站点仓库。补丁适用性另用固定提交重新展开的输入核对。源码文件回执同时列原始字节 SHA-256 和归一化 LF 的文本 SHA-256；本机 archive 为 CRLF，不将两种指纹混用。本目录 `.gitattributes` 对日志和补丁保留原始字节、不作换行转换，并豁免其格式性空格检查，避免改变日志指纹或补丁的空白上下文；正文仍按常规规则检查。

RenderDoc 1.43 单独启动同一 exe，以 F12 捕获第 17931 帧；`renderdoccmd thumb` 导出的原始缩略图存于 `static/images/piccolo-debug-default-scene-20261026.png`。图中有默认关卡、角色、墙体与编辑器面板。它是另一次工具介入运行的后续帧，不能称为 CDB 第一次 present 的截图；未做捕获重放、逐 Pass 验证或显示器扫描输出验证。

本轮不宣称零警告：仍有 LNK4098（MSVCRT 默认库冲突）、MSB8029（临时目录增量构建）和原有 Lua `get<bool>` 的 C4715 等。运行时实际加载的验证层 DLL 来自本机 `D:/VulkanSDK/1.3.261.1/Bin`；不能由仓库的编译输入配置推断实际加载模块。也未验证游戏输入、角色/相机运动、粒子、编辑操作与保存重载，未进行性能或泄漏验收。

Piccolo 源码及补丁涉及片段：Copyright (c) 2022 Contributors to the Piccolo Engine，MIT License，见固定提交的 [LICENSE](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/LICENSE)。
