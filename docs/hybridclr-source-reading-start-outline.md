# HybridCLR 源码阅读起步：执行提纲与证据

日期：2026-10-25。类型：映射篇；系列位置：源码跟读入口，不占用旧 HCLR-1～30 编号。

## 问题与边界

读者知道 metadata、transform、interpreter，却无法确认自己看的源码是否就是 Player 编译的代码。本篇建立源码、构建产物、运行进程三者的对应关系；不重复原理篇，不写完整接入手册，不引用 Unity 受限 C++ 函数体。

## 提纲

1. 从断点不命中引出三份地图：源码、产物、进程。
2. 固定源码基线，单独记录尚未验证的 Unity patch、平台和构建配置。
3. 先读 package 的版本映射与生成入口，再读装载、方法转换、执行。
4. 准备 Windows IL2CPP native 调试，区分 Editor、Script Debugging 与 C++ 调试。
5. 两个程序集的最小 Add 方法；同一 MethodInfo 连续调用两次。
6. 每个断点的输入、输出、观察字段；首次转换与缓存复用。
7. 不命中的排查顺序，结束条件和后续阅读。

## 已核对证据

- hybridclr v6.1.0：`5567f367502d916c6742873d8ed2ab13d0f6900a`，从官方仓库浅克隆后核对源码。
- hybridclr_unity v6.1.0：`88e20505f87d57cbff5695be5f7baa35d626f619`，核对 `Data~/hybridclr_version.json`、SettingsUtil、PrebuildCommand、RuntimeApi。
- package 的 Unity 2022 映射为 hybridclr v6.1.0 / il2cpp_plus v2022-6.1.0。后者远端 tag 为 `ce2e34097e099e458e14192bed96c28bc761392b`；本次未执行其 native 构建。
- v6.1.0 InterpreterModule.cpp:373 的 InterpreterInvoke、588 的 GetInterpMethodInfo；Assembly.cpp:97 的 LoadFromBytes；Transform.cpp:150 的 Transform；Interpreter_Execute.cpp:1645 的 Execute。
- 此版本从 image->GetMethodBody 取 IL，转换后存入 methodInfo->interpData；不是旧文里笼统提及的 MethodBodyCache 路径。
- 官方调试说明：https://www.hybridclr.cn/docs/basic/sourceinspect （查阅时页面为 9.x；只用于调试准备，符号定位以固定 commit 为准）。
- Unity 2022.3 Windows 构建设置：https://docs.unity3d.com/cn/2022.3/Manual/WindowsStandaloneBinaries.html 。

## 验证边界

源码路径和控制流已静态核验。正文实验是操作方案与预期观察，未运行 Unity Player、未确认断点命中、未采集设备性能数据。不得写成已实测案例。

## 文档回归（2026-10-25）

- Hugo 0.157.0 extended 构建成功，零 ERROR；当前共享工作区生成 1355 页，含其他任务的内容，不作为本次新增篇数。
- 60 篇 runtime 正文与 3 个索引的 3472 个站内链接／锚点检查通过；旧 HybridCLR 索引的阅读目标均保留，仅去掉指向索引自身的冗余入口。
- 两篇原 IL2CPP 文章 URL 保留，新标题已分工；泛型标题在 HTML 中显示完整。
- 新篇沿用 runtime 阅读模板；浏览器确认索引入口可打开新篇，面包屑、目录与正文样式正常。
- 本次范围共 16 个文件；`doc-plan.md` 仅新增本轮两条系列路由修改，其余已存在改动不属于本轮。未提交或推送。
