# Android 专项：生命周期与恢复增补记录

日期：2026-10-31。对应[原文](../content/performance/mobile-unity-android.md)与[唯一系列计划](mobile-hardware-and-optimization-series-plan.md)。这是执行记录，不新建正文或篇级目录。

## 范围与复用

用户授权在既有 Android 专项中补清进程、Activity、Surface 和游戏会话的寿命、保存恢复责任与验证证据。保留标题、slug、日期、系列、weight 及原 1—6 节的身份；新增一节，更新摘要与总结的相应表述。另仅更新系列计划的本篇状态，新增本记录。其他文章、历史工作区修改、根路由与缺口对照表均不修改。

复用《Unity 异步运行时 06》的对象／页面／场景上下文与取消收口，不复制其实现课程；阶段终点与证据关联复用 IO-06。平台恢复不直接等同于对象销毁或重新启动。没有新增项目经历、事故、性能收益或通过的设备案例。

## 官方核对与条件

| 新增内容 | 核对依据 | 正文保留的边界 |
| --- | --- | --- |
| 进程与 Activity 寿命、可见性和恢复 | [Activity 生命周期](https://developer.android.com/guide/components/activities/activity-lifecycle)、[进程生命周期](https://developer.android.com/guide/components/activities/process-lifecycle) | 组件重建不等于进程重启，系统终止不保证 onDestroy；Android 13 缓存进程的执行约束不泛化为全部后台组件 |
| 配置变化与重建 | [Handle configuration changes](https://developer.android.com/guide/topics/resources/runtime-changes) | 核对变化类型、Manifest 与窗口条件，旋转不自动算作已覆盖 Activity 重建 |
| Surface 状态与使用范围 | [AOSP SurfaceView 与 GLSurfaceView](https://source.android.com/docs/core/graphics/arch-sv-glsv)、[SurfaceHolder.Callback](https://developer.android.com/reference/android/view/SurfaceHolder.Callback) | 两组相关状态机；停止使用旧目标的契约限定其接口模型，不写成所有 Unity 后端内部顺序，不推导所有资源均失效 |
| Unity 入口、暂停、失焦与退出通知 | [2022.3 默认 Activity](https://docs.unity3d.com/2022.3/Documentation/Manual/AndroidUnityPlayerActivity.html)、[OnApplicationPause](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/MonoBehaviour.OnApplicationPause.html)、[OnApplicationFocus](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/MonoBehaviour.OnApplicationFocus.html)、[OnApplicationQuit](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/MonoBehaviour.OnApplicationQuit.html) | 固定 2022.3 文档基线，不外推所有补丁、入口与插件；软键盘可能失焦，初始化通知不等于后台恢复，退出通知不是必达保存机会 |
| 保存与恢复责任 | [Save UI states](https://developer.android.com/topic/libraries/architecture/saving-states)、上述生命周期文档 | 少量界面重建状态与持久业务记录分开；不把 saved state 视为 Unity 对象图或完整存档，不承诺未经验证的写盘一致性 |
| 受控测试与真实终止 | [开发选项](https://developer.android.com/studio/debug/dev-options)、[ADB activity manager](https://developer.android.com/tools/adb) | Don't keep activities、强制停止与真实内存压力回收分别标注；没有执行终止命令或修改设备设置 |
| 退出证据 | [ActivityManager](https://developer.android.com/reference/android/app/ActivityManager#getHistoricalProcessExitReasons(java.lang.String,int,int))、[ApplicationExitInfo](https://developer.android.com/reference/android/app/ApplicationExitInfo) | API 30 起；近期环形记录、跨 UID 权限与低内存报告支持条件；缺记录不推成正常退出，不把 SIGKILL 一律判为 LMK |

恢复协调、检查点、上下文编号、职责表和六项测试矩阵为工程建议与未执行的验证设计，不是 Android／Unity 自动提供的系统或本项目既有实现。

## 验证与未核验范围

- 官方资料与上述版本条件已核对；未获取目标 Unity 项目的入口／Manifest，未运行 Unity 或 Android 真机／模拟器，未执行保存故障注入、恢复或终止实验。
- 新增 DATA-TODO 说明实际矩阵、原始事件、检查点与画面／交互结果仍待采集；未覆盖分支及失败样本需独立记录。
- 本地回归：`python -B -m unittest discover -s tests -p test_reading_layouts.py -v`，19 项通过。
- 构建：`hugo --gc --minify --destination .tmp/android-lifecycle-20261031/publish-check`，退出码 0，生成 1443 页。
- 生成页：`python -B scripts/check-reading-pages.py --public .tmp/android-lifecycle-20261031/publish-check`，1377 个阅读页，0 错误。
- 目标 HTML 已检查：标题、原 1—6 节和新增小节均生成，全文 5 张表格（新增 3 张），异步 06 与 IO-06 两条复用链接正确生成，退出原因 API 链接完整，无残留 shortcode，DATA-TODO 不显示为正文。没有改样式／模板，本次未做浏览器视觉验收。
- 差异与身份：保留原文件 CRLF；`git -c core.whitespace=cr-at-eol diff --check -- content/performance/mobile-unity-android.md` 通过。title／slug／series／weight／date／lastmod 与增补前一致；本次写入仅正文、系列计划对应行及本记录，暂存区未改变。
- 范围核对观察到 `docs/game-performance-resource-ready-review.md` 有其他工作同期更新；未操作该文件，不纳入本次修改。其余既有工作区内容保留。
- 系列计划原为未跟踪草稿，含本轮之前的其他篇目登记。本次提交仅登记 Android 本篇及必要表头，其余计划内容留在原工作区，不随本次交付。
- 2026-10-31 用户授权仅提交并推送本次正文、系列计划增量与本记录；提交／推送状态以 Git 历史与远端回读为准，线上部署未验证。
