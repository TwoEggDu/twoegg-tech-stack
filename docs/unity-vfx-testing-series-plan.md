# Unity 特效测试：从指标口径到项目验收

> 2026-11-03 立项。本文件是本系列唯一篇级规划，属于 `content/performance/` 的特效测试专题。当前完成公开仓库下载和首轮静态分析，正文 0 篇，Unity / 真机实验 0 次。

> 同日范围调整：作者明确先学习观测点、预算与阈值、设备分档和测量准确性，并将基础扩展到 UI、场景、角色、动画和特效。公共学习顺序见[客户端性能工程能力路线](client-performance-budget-tiering-monitoring-outline.md)。本文件保留特效专项篇级目录，工具研究后置到必要基础之后；本轮没有开始工具开发。

## 1. 要解决的问题

一个特效在编辑器里能播放、粒子数不多，怎样判断它在目标设备、同屏数量和玩法时点下能够交付？这需要先定义测试条件，解释指标，再把静态检查、隔离测量与场景验收连起来。

目标读者是客户端程序、TA、工具开发者及需要参与资源验收的特效负责人。主线以测量机制、因果判断和交接标准为主，Unity 操作步骤服务于复现实验。

用 [ParticleEffectProfiler](https://github.com/sunbrando/ParticleEffectProfiler) 作为可读的公开实现，说明一个数字怎样产生，再通过校准识别边界。后续改造只服务已确认的测试问题，避免先建设通用平台。

系列支撑的工程判断包括：一个结果是否可信、该追查模拟还是渲染、该修改资源还是播放时机、某项规则能否进入验收，以及超预算由谁复核。

## 2. 范围与版本

- 首期覆盖 Particle System 的资源检查、生命周期、覆盖 / 重叠测量、工具扰动、首放与稳态、同屏叠加和设备验收。
- 特效的挂点、触发、取消、自然结束和视觉可读性属于验收输入；制作教程复用原系列。
- VFX Graph、复杂技能系统、完整 Shader 教程和全站资源平台不纳入首期；涉及交点时解释范围，后续有证据再扩展。
- 源码分析固定到 `1a32eac25ae52ee2ad982fc3681432014b590ba3`，上游工程 Unity 2017.4.10f1。现代版本实验的精确 Unity / 管线 / 包版本待实际环境选定后固定，不默认原代码可运行于 Unity 6 / URP。
- 公开开源 C# / Shader 可按 MIT 条件使用。独立教学实验不冒充作者历史项目；不引入私仓受限引擎源码或公司资产。

## 3. 旧文核对与复用决定

本轮从根路由进入性能规划，并跨 `docs/`、`content/` 检索“特效测试、特效性能、粒子、ParticleEffectProfiler”，实际读取下列正文或相关段落。

| 已有材料 | 已有覆盖 | 本次处理与增量 |
| --- | --- | --- |
| [特效叠加与首放](../content/performance/game-performance-vfx-stacking-and-first-play.md) | 面向美术解释透明、屏占比、持续时间与首放风险 | 作为场景动机；本系列新增可校准的测量方法和证据，不重写同义风险清单 |
| [Unity 粒子渲染机制](../content/rendering/unity-rendering-04-particles-vfx.md) | 粒子几何、材质、透明与 VFX Graph 概览 | 按需复用；其中“不写深度就不能 Early Z”“每系统一个 DrawCall”等绝对化句子需单独审校，不把旧文作为新结论的证明 |
| [性能诊断方法](../content/performance/game-performance-diagnosis-method.md) | 现象、假设、证据、对照与测量条件 | 直接复用方法；本系列落到特效输入、采集路径和生命周期窗口 |
| [美术生产 08：特效](../content/delivery-engineering/game-art-production-08-vfx.md) | 素材、粒子组装、挂点、触发与结束的教学方案 | 制作与接入归原文；该文尚未完成实际素材和 Unity 验证，不认作现成实验资产 |
| [特效性能检查器案例](../content/projects/vfx-checker-case.md) | 历史资源规则与经历线索 | 保留独立身份；现文有包装性表述和待核验材料，不与本次开源工具拼接为一次亲历成果 |

独立开系列的理由：测试口径、采集实现、实验校准、真机验证和持续验收构成稳定的内部主线，现有正文没有提供这条可复现链路。它补充性能方法论，并与美术协作、质量门禁建立交叉关系。

## 4. 篇级目录

以下标题为规划标题，全部待写。编号 VFX-T 用于本系列维护，不代表已有页面。

| 编号 | 规划标题与核心问题 | 必要基础 / 主要增量 | 当前状态 |
| --- | --- | --- | --- |
| VFX-T01 | **特效测试究竟要测什么：从单个 Prefab 到场景预算** | 复用成本直觉；区分资源合法性、表现正确性、隔离成本和场景预算，定义对象、条件、时间窗口与报告契约 | 待写；可先写方法，实验结果留空 |
| VFX-T02 | **ParticleEffectProfiler 源码分析：面板里的数字从哪里来** | 沿入口→采集→聚合→显示解释；重点是主贴图范围、全局批次、替换渲染与累计曲线 | 待写；[首轮源码分析](particle-effect-profiler-analysis-2026-11-03.md)已具备 |
| VFX-T03 | **Overdraw 怎么测才有意义：屏占比、重叠、深度与采样误差** | 单层 / 多层、透明轮廓、遮挡、颜色编码和时间窗口校准；区分覆盖诊断与真实 GPU 成本 | 待写；实验待执行 |
| VFX-T04 | **搭建可重复的特效测试场景：相机、随机性与测量扰动** | 固定版本、镜头、种子、播放协议、对象隔离和失败退出；校准采集开销及数据有效性 | 待写；[首个任务的协议、事件/CPU 来源及相应校准设计准备](unity-vfx-repeatable-playback-protocol-teaching.md)已补，场景/采集/现代管线适配仍待验证 |
| VFX-T05 | **首放、稳态与同屏叠加：怎样在目标设备验证特效成本** | 复用 CPU / GPU 诊断；比较同条件 Player 原渲染，定义冷启动状态、稳态窗口、峰值与重复运行 | 待写；设备和运行证据待补 |
| VFX-T06 | **从测试报告到修改决定：做一次完整的特效优化对照** | 固定视觉职责，基线→假设→单项改动→复测→恢复；检查视觉信息与目标成本是否同时满足 | 待写；独立教学实验，尚无收益结论 |
| VFX-T07 | **把特效测试接进生产：预算、分工、回归与例外处理** | 美术自检、TA / 程序复核、QA 场景验收；报告身份、规则版本、误报、豁免及持续维护 | 待写；方法可先成稿，流程采用与效果另取证 |

## 5. 阅读与执行顺序

阅读顺序为 01 → 02 → 03 → 04 → 05 → 06 → 07；需要处理眼前资源问题的读者可从 01 的测试契约进入相应指标。粒子基础与通用诊断按需回看，不以完整渲染、硬件或 OS 课程为前置。

实际执行先沿公共能力路线补齐成本、观测点、预算/分档与测量可信度，再回到 01 的测试契约和 02 的源码证据，按需求准备 03 / 04 的最小校准场景。确定运行基线并验证采集后，再收集 05 / 06 的原始数据。07 可先写协作方法，不能提前写成已运行的团队门禁。七篇为专项应用的暂定目录，不承载全部公共基础。

第一轮最小实验及记录字段见 [分析文档第 7 节](particle-effect-profiler-analysis-2026-11-03.md#7-第一轮最小验证设计全部待执行)。执行记录用于保存条件、结果和失败样本，不另维护篇级状态。

## 6. 成稿和验证要求

- 每篇说明帮助读者理解什么，以及它支撑哪个工程判断。分清公开源码事实、机制资料、推导、教学设计和实测。
- 测量文章给出计数对象、单位、分母、时间窗、采样扰动和不支持项；不能仅贴工具截图。
- 验证篇保存版本身份、原始数据、可复现条件、失败记录与对照；没有目标设备证据时不写“性能达标”。
- 优化篇同时检查视觉职责与性能，不能只用粒子数减少代表成功。
- 生产篇说明使用入口、接收人、复核、异常处置、规则维护与退出条件；采用、运行和收益分别举证。
- 正文落盘后按站点要求运行模板回归、Hugo 构建及生成页检查，再回写本文件。当前只有准备文档，不计为公开文章、完成实验或已发布。

## 7. 当前接续点

当前先推进[公共基础能力路线](client-performance-budget-tiering-monitoring-outline.md)的指标、预算和分档解释；回到特效专项时，再写 VFX-T01 的“资源检查→单体测量→场景及设备验收”契约。VFX-T02 的静态分析保留为后续材料。Unity 实验另从上游基线或明确的移植副本开展，先做已知答案的校准，不把首次导入成功当作指标正确。

2026-11-06 E1 接续：复读 CPE-01/02、预算 09 生命周期与制作 08 退出职责后，将首开奖励界面/特效的[纸面播放协议](unity-vfx-repeatable-playback-protocol-teaching.md)放在 VFX-T04 准备层。增量为初始条件、开始/必要事件、正常/取消/超时/缺失分支及退出确认，不重写公共身份、统计或控制器。四条合成轨迹与 Unity 6.0 四份接口契约仅支撑设计和教学，没有新增正文篇数或工程/设备/校准执行。局部 AI 编辑自审及本地检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#vfx-playback-protocol-20261106)。下一项核对具体指标来源与最小校准设计；作者反馈和知识阶段出口仍待通过。

2026-11-06 E2 首批：在同一准备材料[增补来源候选与观察时刻](unity-vfx-repeatable-playback-protocol-teaching.md#vfx-event-source-design)，将业务接受/代理、应用循环、组状态查询和退出清单分开；四份 Unity 6.0 时钟/帧/Marker 契约与新合成读取/轮询例子只支撑设计，不重做 E1 四轨迹。精确业务事件、线程、版本平台与校准仍待确定，VFX-T04 和其余六篇仍待写；审校/检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#vfx-event-source-design-20261106)。

2026-11-06 E3 首批：同一材料新增[事件最小校准设计](unity-vfx-repeatable-playback-protocol-teaching.md#vfx-event-calibration-design)，限记录完整性/归属、时间/查询范围和开关扰动三块；九条合成参考及失败对照不代表真实采集已校准。覆盖/原渲染与 CPU/GPU/内存仍按原专项后续展开。VFX-T04 与七篇正文状态保持待写，场景/工具/实际校准未执行；局部 AI 编辑自审及检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#vfx-event-calibration-design-20261106)。

2026-11-06 E2 CPU 接续：同一材料补[CPU 区间来源与边界](unity-vfx-repeatable-playback-protocol-teaching.md#vfx-cpu-source-design)，连接请求/原始采样、含子/Self、独立调度状态和异步提交/执行/续体；虚构 8/3 ms 采样与 4/3/1 ms 状态不作 Unity 实测。复核两份既存及两份新 Unity 6.0 直接契约，开关/扰动沿 E3 限用；VFX-T04 与其余六篇仍待写。审校/检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#vfx-cpu-source-design-20261106)；下一项设计 CPU 范围校准，不执行采集器或调度追踪。

2026-11-06 E3 CPU 接续：同一材料补[CPU 范围校准设计](unity-vfx-repeatable-playback-protocol-teaching.md#vfx-cpu-calibration-design)，只含父子/跨线程与缺项、异步阶段终点、配置改变 Self 三块；复用原采样与事件扰动设计，新增六事件和配置对照均为合成输入。VFX-T04 与其余六篇仍待写，真实参考、采集/异步适配与校准未执行；局部 AI 自审/检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#vfx-cpu-calibration-design-20261106)。下一项按需核 GPU 来源，不扩展工具实现。

2026-11-06 E2 GPU 接续：同一材料补[GPU 来源与结果身份](unity-vfx-repeatable-playback-protocol-teaching.md#vfx-gpu-source-design)，核对 Unity 6.0 的高层/详细入口、完成帧返回顺序、来源特定支持与报告范围；四次合成读取用于核对重复、数量和归属，不是 API 实测或自动业务字段。VFX-T04 与其余六篇仍待写；局部 AI 编辑自审/检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#vfx-gpu-source-design-20261106)。下一项为本来源的最小校准设计，实际 GPU/参考/采集器未执行。

2026-11-06 E3 GPU 接续：同一材料新增[GPU 校准三块设计](unity-vfx-repeatable-playback-protocol-teaching.md#vfx-gpu-calibration-design)，复用 R1–R4，补返回范围/源身份、逐字段状态及开关/参考恢复的负对照，区分记录处理与真实计时准确。新残留槽及 CPU/GPU 状态均为合成输入，七篇正文仍待写；局部 AI 编辑自审与检查见[执行记录](client-performance-budget-tiering-monitoring-execution-plan.md#vfx-gpu-calibration-design-20261106)。下一项回 E2 内存来源，实际采集/校准、参考与工具未执行。
