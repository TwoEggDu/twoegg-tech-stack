# 性能工作流与平台关联审校记录

> 2026-10-31。主文为《从现象到方法：把游戏性能判断连成一套工作流》，关联文为《手机和 PC 为什么要用不同的性能直觉》。保留标题、URL、系列归属与视觉样式，本轮未提交、推送或部署。

## 范围与复用决定

复用已有工作流正文，按作者确认的收束目标整理为可回退流程、决策交接表和首次技能教学推演；不再重复已审校基础。基础与技术结论分别回查帧链路、帧预算、诊断方法、线程调度、内存记账、资源可用及功耗与发热。既有 Unity／Unreal 现场文章只作为专项入口，不认定本轮全面审校。

平台篇只处理影响共同工作流的比较与归因：平台名称不能规定根因、发生率或用户偏好，持续约束和环境差异两端都要核对，读取后处理不统称 I/O，分档／默认策略都要验收。不新增平台 API、驱动或硬件深挖。

共六文件：两篇正文、性能系列索引、性能 canonical plan、FDE 基础与案例执行对照表、本记录。性能 plan 中原有客户端基础提纲段落不属于本轮增量；根 doc-plan、M01／M02 材料和持续性能 05 正文不修改。

## 已确认问题与落地

| 原文问题 | 本次处理 |
| --- | --- |
| 八步单向顺序，平台条件最后才纳入，动作之后缺少验收与回退 | 按信息依赖组织目标、观察、假设、取证、方案、复测与复用；条件从开始记录，明确失败返回位置 |
| 按现象归六类，I/O 含后处理，memory 泛指 GC／工作集，PSO 首次准备泛归 GPU | 六类改为调查问题，沿已审校诊断表核对执行阶段，不复制不一致定义 |
| 内部区间缩短即可选择动作 | 区分任务目标、内部区间与可见反馈，补目标不变、条件漂移、质量／资源代价不可接受的分支 |
| 只讲个人排查顺序，团队怎样接收决定不清 | 交接表列材料、责任、接受条件和退回位置；角色为建议职能，不认领作者团队已采用 |
| PC 环境、手机热约束的固定原因排序与几分钟后降频暗示 | 共同记录设备、供电、散热、内容、采集和持续窗口，信号与根因分开；不引入跨平台发生率 |
| PC 用户愿意调设置、手机用户不愿管理性能等无来源假定 | 改为产品可选策略及待验证的接受目标，不据平台替用户做偏好判断 |

## 官方依据与边界

本轮于记录日期重新访问以下官方资料，正文就近链接。其余技术机制复用已审校专题，不引用受限 Unity C++ 源码。

- [Unity 2022.3 采集说明](https://docs.unity3d.com/2022.3/Documentation/Manual/profiler-profiling-applications.html)：连接 Player 的 Profiler 采集要求 Development Build，采集和目标交付配置不能直接等同；未执行引擎采样。
- [Android Frame Pacing](https://developer.android.com/games/sdk/frame-pacing)：排队与节奏等待的关系，不据等待存在判定 GPU 超载。
- [Android Thermal API 指南](https://developer.android.com/games/optimize/adpf/thermal)：热约束调查背景，不作为所有手机固定降频时刻或性能变化的证据。
- [Windows 处理器电源管理](https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/configure-processor-power-management-options)：Windows 10 文档包含核心停放、性能状态与平台约束，有限支持 PC 也需考虑运行能力变化；不外推所有 PC 系统的具体策略。

工作流、角色表、方案比较及首次技能分支均为工程建议／教学模型，没有真实事件、原始 trace、设备对照、输入到显示延迟、收益或团队采用实测。进入等待、驻留、退出与持续表现都是后续需采集项目，未填假数据。

## 本地验证与交付

本轮实际结果：

- `python -m unittest discover -s tests -p test_reading_layouts.py -v`：19 项通过。
- `hugo --gc --minify --destination .tmp/performance-workflow-audit-20261031/public`：1443 页构建成功，正文行尾空白修正后已刷新最终产物。
- `python scripts/check-reading-pages.py --public .tmp/performance-workflow-audit-20261031/public`：1377 个阅读页面，0 错误。
- 主文 11 个不同站内引用目标、平台篇 5 个不同站内引用目标均存在；差异空白检查通过。
- 浏览器已目视检查决策交接表与教学分支表，验证系列索引 → 工作流 → 平台直觉 → 工作流 → 系列索引的回路。样式未改，本轮未做窄屏验证。
- 工作区中并行的版本管理文章 `content/delivery-engineering/delivery-version-management-01-versioning.md` 与规划 `docs/delivery-engineering-column-plan.md` 存在外部变化，本轮未写入这些文件。相关版本检查只对本轮六文件增量负责。
- `.tmp/performance-workflow-audit-20261031/` 保存本轮范围、前后快照、哈希及增量，性能 plan 原有客户端基础提纲段落仍保留且排除在本轮增量之外。暂存区未动。

本地站点检查基于当前工作区，包含其他尚未交付内容，后续提交应隔离精确候选。内容、实验、站点检查、提交与线上部署分别记录；本地通过不表示案例或持续门禁已经实现。本轮未提交、推送或部署。
