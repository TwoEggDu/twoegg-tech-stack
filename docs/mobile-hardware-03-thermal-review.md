# 硬件 03 功耗与发热审校记录

2026-10-31。正文：[硬件 03](../content/performance/mobile-hardware-03-power-thermal.md)。篇级归属：[移动硬件系列计划](mobile-hardware-and-optimization-series-plan.md)。

## 范围与复用

用户授权审校硬件 03，补持续性能的工程取舍、团队责任和教学场景，同步硬件 02b 的必要关联修正及规划、阅读入口。保留两篇 URL、series、weight 与原日期，追加 lastmod；没有新建同义正文。

复用资源可用链、内存预算、Android 与 iOS 专项。Android Provider 与恢复接入不重复展开，硬件 02b 不另建控制策略。新增现场证据、设备实验与真实团队案例均未执行。

范围冻结为七个文件：两篇正文、移动硬件索引、FDE 索引、移动硬件 plan、FDE plan 和本记录。索引与移动硬件 plan 已有其他工作区增量，按本轮开始前快照保留，不随本轮归入提交。根路由、其他旧文、模板和 CSS 不修改。

## 发现与修订

| 原问题 | 处理与边界 |
| --- | --- |
| 固定瓦数、温度／频率表、几分钟必然降频与机型优势被写成实测 | 移除无法追溯的数字，按工作负载、环境、时间窗口与热路径解释；不补造替代数据 |
| ThermalHeadroom 被写成 1 安全、0 危险 | 依 API 参考修正方向、SEVERE 阈值、可超过 1、无效值与预测含义；区分 API 29 状态与 API 30 headroom |
| 每帧采样、固定节点或单个信号暗示通用诊断 | 补能力、时间戳、采样与设备条件；频率下降和长帧均不是单独的热归因证据 |
| 低内存信号被当成 iOS 降频信号 | 分开内存与热压力，补 Apple 系统热状态和通知契约 |
| 35fps 必然优于波动高帧率、P95 固定合格线 | 改为明确对象、窗口、响应与最低表现；删去不完整的监控代码，不将帧推进间隔当呈现或 CPU 时间 |
| Provider 支持和直接修改画质缺边界 | 区分 Provider 与业务策略，改为决策伪代码；补无效信号、统一仲裁、恢复门槛、冷却和退出 |
| 优化只有收益，缺生产接受责任 | 加入策略代价表、五类团队职责与技能战斗教学场景 |
| 02b 的留存率和体验表与修正后的 03 冲突 | 删除无来源机型对照、固定内存/帧率目标与“旗舰无需降质”等推导；同时移除支撑这些推导的无来源面积、导热倍数、带宽、制程功率及退出时间数字，保留定性硬件视角 |

02b 的关联修正涉及多个段落，因为旧稿通过硬件数字反复推出相同档次结论；不是新增整机散热或 DRAM 实现课程。没有进行具体机型拆解、材料规格、内存控制器或持续性能的实测认证。

## 核对来源

- [Android PowerManager](https://developer.android.com/reference/android/os/PowerManager#getThermalHeadroom(int))：API 版本、headroom 含义、预测与无效值。API 参考与游戏指南采样建议存在差异，正文不写死统一周期。
- [Android Thermal API 指南](https://developer.android.com/games/optimize/adpf/thermal)：热条件与应用降载；不把指导方案当成本项目效果。
- [Unity ADPF 接入](https://developer.android.com/games/engines/unity/unity-adpf)：主包／Provider 的版本与功能边界。
- [Apple thermalState](https://developer.apple.com/documentation/foundation/processinfo/thermalstate-swift.property)、[枚举](https://developer.apple.com/documentation/foundation/processinfo/thermalstate-swift.enum)、[通知](https://developer.apple.com/documentation/foundation/processinfo/thermalstatedidchangenotification)：iOS 11 起的热状态与通知；通过官方提供的 Markdown 文档核对。
- [Unity 2022.3 lowMemory](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Application-lowMemory.html)：低内存通知，不是热状态。

## 验证状态

- 模板回归：`python -m unittest discover -s tests -p test_reading_layouts.py -v`，19 项通过。
- Hugo：`hugo --gc --minify --destination .tmp/thermal-audit-20261031/public`，1443 页，退出码 0。
- 生成页：`python scripts/check-reading-pages.py --public .tmp/thermal-audit-20261031/public`，1377 个阅读页，0 errors。
- 本地浏览器：责任表排版正常；硬件 03 → 02b → 系列索引，以及 FDE 精选入口 → 硬件 03 均能正确到达；保持现有阅读风格。
- 差异核对：范围外既有文件哈希未变；系列 plan 在任务期间另有 iOS 生命周期状态行与 CRLF 写入，本轮保留、不纳入增量。Windows 换行按 `git -c core.whitespace=cr-at-eol diff --check` 检查通过。快照与本轮精确增量保存于 `.tmp/thermal-audit-20261031/`，后续提交需按增量选取，不能整文件暂存索引或系列 plan。

以上检查针对本地工作区站点。无设备采集、原生桥接／Unity 编译、策略运行或收益验证；正文 DATA-TODO 保留。尚未提交、推送或部署。
