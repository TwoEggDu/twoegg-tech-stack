# iOS 专项：生命周期与恢复增补记录

日期：2026-10-31。对应[原文](../content/performance/mobile-unity-ios.md)与[系列计划](mobile-hardware-and-optimization-series-plan.md)。本文件为执行记录，不新增正文、篇号或篇级目录。

## 范围与复用

按用户确认原位增补 App、UIKit Scene、进程和游戏会话的生命周期、保存恢复责任及验证证据。保留原标题、slug、date、lastmod、series、weight 和原 1—6 节身份，新增一节并更新摘要与总结；系列计划只更新 iOS 对应行，另新增本记录。不修改 Android、异步、内存正文、根路由、缺口对照表或其他工作区改动。

复用《Unity 异步运行时 06》的对象／页面／场景与任务收口，以及内存记账文章的 Apple 指标模型。明确 UIKit Scene 不等于 Unity Scene；不复制 Android Activity／Surface 的事件表，不将已有 Unity 6 工具例子当作本篇 2022.3 行为。原文已有后台任务、Metal、内存压力和报告分类继续承担其职责。

## 官方核对与版本条件

| 增补内容 | 官方依据 | 保留的条件与限制 |
| --- | --- | --- |
| App／Scene／进程分工，失活／后台／挂起 | [Managing your app’s life cycle](https://developer.apple.com/documentation/uikit/managing-your-app-s-life-cycle)、[Preparing your UI to run in the background](https://developer.apple.com/documentation/uikit/preparing-your-ui-to-run-in-the-background) | iOS 13+ 启用 Scene 时按相应 delegate 分发界面事件；应用级启动仍需处理。状态模型不冒充统一 API 枚举；系统快照不证明实际恢复完成 |
| Scene 支持与多窗口 | [Specifying the scenes your app supports](https://developer.apple.com/documentation/uikit/specifying-the-scenes-your-app-supports) | 核对主 App 配置、入口与实际实现；不因系统版本或配置键存在就宣称 Unity 支持多窗口，不替代目标 SDK 的适配要求 |
| Unity 事件接入 | [Unity 2022.3 导出工程](https://docs.unity3d.com/2022.3/Documentation/Manual/StructureOfXcodeProject.html)、[OnApplicationPause](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/MonoBehaviour.OnApplicationPause.html)、[OnApplicationFocus](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/MonoBehaviour.OnApplicationFocus.html) | 沿用 2022.3 LTS，不外推全部补丁／宿主／SDK；不建立未经验证的原生事件与 C# 回调逐项对应表，初始化通知不冒充恢复 |
| UI 状态与业务持久化 | [Preserving your app’s UI across launches](https://developer.apple.com/documentation/uikit/preserving-your-app-s-ui-across-launches)、[Restoring your app’s state](https://developer.apple.com/documentation/uikit/restoring-your-app-s-state) | UI 恢复资料可被丢弃，不能替代业务持久化；Scene 路线 NSUserActivity 不自动保存 Unity 对象、纹理或服务端会话 |
| 终止与后台时间 | [applicationWillTerminate](https://developer.apple.com/documentation/uikit/uiapplicationdelegate/applicationwillterminate(_:))、[后台执行时间](https://developer.apple.com/documentation/uikit/extending-your-app-s-background-execution-time)、[Unity OnApplicationQuit](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/MonoBehaviour.OnApplicationQuit.html) | 不依赖必达的退出保存；已有短时任务、系统后台任务及传输分开。额外时间按任务需要提前安排，不预填固定完成窗口；未推荐修改挂起／退出设置 |
| 渲染目标及压力下的恢复 | [Preparing your Metal app to run in the background](https://developer.apple.com/documentation/metal/preparing-your-metal-app-to-run-in-the-background)、原内存记账正文 | 原生插件配合引擎遵守后台 GPU 使用约束；active 不等于新画面已经显示，Scene 重连不等于所有资源均失效；Apple 口径与 Unity 版本分别核对 |
| 退出原因证据 | [watchdog](https://developer.apple.com/documentation/xcode/addressing-watchdog-terminations)、[jetsam 报告](https://developer.apple.com/documentation/xcode/identifying-high-memory-use-with-jetsam-event-reports) | 按构建、时间、进程与复现场景关联；普通崩溃、内存终止及响应性终止分开，缺报告保留未确认，工具注入不冒充真实系统事件 |

Apple 文档通过官方文档 JSON 正文读取，原网页入口经网页工具核对；原始材料缓存位于 `.tmp/ios-lifecycle-20261031/official/`，不作为发布产物。新增责任表、检查点与上下文编号、职责链和六项验证矩阵均为工程建议／未执行设计，不是已实现系统或作者项目成果。

## 验证与未核验范围

- 官方资料及上述版本条件已核对；没有目标 Unity 工程的主 App 配置与原生入口，没有执行 iOS 构建、Xcode、模拟器或真机恢复／故障注入实验。
- 新增 DATA-TODO 要求补实际设备、配置、事件、有效检查点、新画面／交互和报告，区分工具模拟、真实事件、失败样本与未覆盖分支。
- 本地回归：`python -B -m unittest discover -s tests -p test_reading_layouts.py -v`，19 项通过。
- 构建：`hugo --gc --minify --destination .tmp/ios-lifecycle-20261031/publish-check`，退出码 0，生成 1443 页。
- 生成页：`python -B scripts/check-reading-pages.py --public .tmp/ios-lifecycle-20261031/publish-check`，检查 1377 个阅读页，0 错误。
- 目标 HTML 已检查：原标题、原 1—6 节与新增小节均生成，全文 6 张表格（新增 3 张），异步 06 与内存记账链接及带括号的 Apple 终止通知链接完整；验证矩阵已渲染，无残留 shortcode，DATA-TODO 不显示为正文。没有改样式／模板，本次未做浏览器视觉验收。
- 差异与身份：保留原文件 CRLF；`git -c core.whitespace=cr-at-eol diff --check -- content/performance/mobile-unity-ios.md` 通过。title／slug／series／weight／date／lastmod 与增补前一致，暂存区未改变；本次只写入正文、系列计划 iOS 对应行和本记录。
- 工作区存在其他任务同期更新的热管理正文、系列／FDE 入口及同一计划的热管理条目；均保留，不纳入本次。`.tmp/ios-lifecycle-20261031/` 单独保存 iOS 条目增量与范围核对，不将整份计划的既有草稿视为本次新增。
- 本次系列计划交付仅在已入库内容上增加 iOS 本篇条目，工作区中其他篇目的草稿及并行更新不随本次提交。
- 2026-10-31 用户授权仅提交并推送本次正文、系列计划增量与本记录；提交／推送状态以 Git 历史与远端回读为准，线上部署未验证。
