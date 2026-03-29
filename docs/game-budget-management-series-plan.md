# 游戏预算管理系列计划

## 这份计划解决什么

这组文章要解决的，不是再补一轮零散的“优化技巧”，而是把 `包体预算`、`内存预算`、`CPU 预算`、`GPU 预算`，再连同 `帧预算拆账`、`场景预算`、`角色预算`、`特效预算`，收成同一种写法和同一套工程语言。

它主要回答五件事：

1. 游戏里的“预算”到底是什么，为什么它不是一个抽象 KPI。
2. 包体、内存、CPU、GPU 这四类预算各自该怎么拆层，而不是只盯一个总数字。
3. 为什么很多项目不是“不会优化”，而是从一开始就没把预算边界立清。
4. 为什么一个局部优化，常常会把成本转嫁到另一层预算。
5. 预算怎样进入设备分档、资产规范、Baseline 和 CI，而不是只停在经验口号。
6. 怎么判断一个现有项目的预算体系到底合不合理，而不是只会“从零设计”。
7. 文章里的判断依据、论点和事实应该来自哪里，而不是只给建议不给根据。

## 一句话定位

`游戏预算管理，不是在超线以后补救，而是先把交付成本和运行时成本拆清，再让内容、代码、资源和流程共同待在正确边界里。`

## 目标读者

- 有 Unity 项目经验的客户端 / 图形程序 / TA / Tech Lead
- 已经碰到“性能、包体、内存总在发布前临时救火”的团队
- 想把“优化”从一次性冲刺改成长期治理的人

## 为什么值得单独成系列

这些主题表面上分属不同问题域，但它们其实共享同一条主线：

- 都在回答“成本对象是什么”
- 都在回答“预算线怎么定”
- 都在回答“超线会先显形在哪里”
- 都在回答“动作应该落在内容、代码、资源还是流程”

如果把它们分别散在 `包体优化`、`CPU 优化`、`GPU 优化`、`性能判断` 里，读者能学到局部动作，但不容易形成“预算管理”这套总框架。

所以更稳的做法是：

- 单独立一个 `游戏预算管理` 系列
- 用 `总预算层` 先讲上限和判断语言
- 用 `分账预算层` 把项目、场景、角色、特效和帧链路拆开
- 用 `落地层` 把预算接进规格、分档、Baseline 和 CI

## 系列边界

### 属于这条线的内容

- 预算对象的拆层方法：总量、峰值、工作集、阶段成本、关键路径成本
- 预算线的制定逻辑：从目标帧率、目标机型、目标安装体验反推
- 预算超线的显形方式：卡顿、抖动、OOM、热降频、下载成本上涨、安装流失
- 预算之间的转嫁关系：包体换内存、内存换 CPU、CPU 换 GPU、画质换带宽
- 预算怎样进入设备分档、资产规格、监控、Baseline、CI 和回归验证

### 不属于这条线的内容

- 具体优化技巧大全
- 单一工具教程，例如纯 Profiler、RenderDoc、Xcode 面板操作说明
- 只讲 Unity / Unreal API 用法、不沉淀成预算方法的文章
- 纯项目私有事故复盘，且无法抽象成通用预算规则的细节

## 与现有内容的关系

### 与 `游戏性能判断` 的关系

- `游戏性能判断` 解决的是：问题已经出现后，怎样判断到底卡在哪。
- `游戏预算管理` 解决的是：问题出现前，预算应该怎样立；问题出现后，为什么总是同一类地方先超线。

### 与 `包体大小优化` 的关系

- `包体大小优化` 讲的是：已经确认包体有问题后，可以用哪些分层动作去减。
- `包体预算` 这一篇要讲的是：首包、全量包、热更包、常驻包到底该怎么分别立线，谁对哪一层负责。

### 与 `Baseline：性能 / 包体 / 加载 / Crash 预算怎样立线并进 CI` 的关系

- `Baseline` 那篇讲的是：预算怎样进入门禁和自动化流程。
- 本系列讲的是：预算本体怎样被定义，为什么这些预算值得被门禁化。

### 与 `CPU / GPU 优化` 文章的关系

- 现有 `CPU / GPU 优化` 文章更偏动作与定位。
- 本系列里的 `CPU 预算` / `GPU 预算` 更偏预算模型、指标选择和边界制定。

## 三层结构

### 第一层：总预算层

- 项目总预算是否合理
- 包体预算
- 内存预算
- CPU 预算
- GPU 预算

### 第二层：分账预算层

- 帧预算拆账
- 场景预算
- 角色预算
- 特效预算

### 第三层：落地层

- 资产规格、设备分档、Baseline、CI、回归验证

## 正式目录（11 篇）

| 编号 | 标题 | 核心问题 | 状态 |
|------|------|---------|------|
| 预算-00 | 游戏预算总论：为什么很多性能和交付问题，本质上是预算管理失败 | 为什么“优化”之前要先学会“立线”和“分账” | 已写（首稿） |
| 预算-01 | 预算体检：怎么判断你现在的项目预算体系到底合不合理 | 一个现有项目到底是没预算、预算失真，还是预算没接进执行 | 已写（首稿） |
| 预算-02 | 包体预算怎么定：首包、全量包、热更和常驻资源为什么不能混成一个数字 | 下载与交付成本到底该拆成哪几层 | 已写（首稿） |
| 预算-03 | 内存预算怎么定：常驻、峰值、工作集、显存和 OOM 风险要分开管理 | “内存够不够”为什么是个假问题 | 已写（首稿） |
| 预算-04 | CPU 预算怎么定：帧时间、主线程尖峰、异步任务和危险时机 | CPU 预算到底在管理平均值，还是关键帧 | 已写（首稿） |
| 预算-05 | GPU 预算怎么定：像素、带宽、RT、阴影、后处理和分辨率缩放 | GPU 成本应该按什么模型拆才有行动性 | 已写（首稿） |
| 预算-06 | 帧预算拆账：逻辑时间、渲染提交、GPU 时间、加载尖峰和同步等待怎么分账 | 一帧为什么不是一个数字，而是一串要分别记账的链路 | 已写（首稿） |
| 预算-07 | 场景预算怎么定：可见物、灯光、阴影、Streaming、触发器和大世界边界 | 为什么“场景做重了”其实是多个预算桶一起超线 | 已写（首稿） |
| 预算-08 | 角色预算怎么定：面数、骨骼、材质、动画、蒙皮和同屏数量怎么一起算 | 角色规格为什么不能只看面数一个数字 | 已写（首稿） |
| 预算-09 | 特效预算怎么定：发射器、活跃粒子、屏占比、透明叠层和技能时机 | 特效为什么最容易同时打爆 CPU、GPU 和内存 | 已写（首稿） |
| 预算-10 | 预算怎样进入规格、分档、Baseline 和 CI：从规则表到门禁和回归 | 如果预算不进入资产规范和流程，它为什么一定会失真 | 已写（首稿） |

## 建议文件落地

### 系列入口

- `content/engine-notes/game-budget-management-series-index.md`

### 正文文件

- `content/engine-notes/game-budget-00-overview-why-budget-management-fails.md`
- `content/engine-notes/game-budget-01-budget-healthcheck.md`
- `content/engine-notes/game-budget-02-package-budget.md`
- `content/engine-notes/game-budget-03-memory-budget.md`
- `content/engine-notes/game-budget-04-cpu-budget.md`
- `content/engine-notes/game-budget-05-gpu-budget.md`
- `content/engine-notes/game-budget-06-frame-budget-breakdown.md`
- `content/engine-notes/game-budget-07-scene-budget.md`
- `content/engine-notes/game-budget-08-character-budget.md`
- `content/engine-notes/game-budget-09-vfx-budget.md`
- `content/engine-notes/game-budget-10-rules-tiering-baseline-ci.md`

## 第二阶段补充计划

当前 `11 篇主线` 已经把预算语言、总预算、分账预算和流程落地站住了。

下一阶段最需要补的，不是再写几篇泛泛而谈的“优化心得”，而是把这条线补成：

- 能覆盖真实平台差异
- 能直接对照项目做预算体检
- 能发给团队做日常规格执行

### 第二阶段目标

1. 把 `预算-01` 从概念体检文补成可直接审项目的检查框架。
2. 把 `Android / Apple / Web / 小程序平台` 这些容器差异单独写清。
3. 把“知道预算”继续推进到“能填表、能验收、能回归”的模板层。

### 第二阶段新增内容（10 篇）

#### 平台附录（5 篇）

- `content/engine-notes/game-budget-11-android-memory-budgets-2gb-to-6gb.md`
- `content/engine-notes/game-budget-12-apple-memory-budgets-legacy-and-current.md`
- `content/engine-notes/game-budget-13-android-ios-budget-smoothing.md`
- `content/engine-notes/game-budget-14-web-platform-budget-browser-wasm-cache.md`
- `content/engine-notes/game-budget-15-miniapp-platform-budget-wechat-alipay-douyin.md`

#### 模板与检查表（5 篇）

- `content/engine-notes/game-budget-16-budget-master-sheet-template.md`
- `content/engine-notes/game-budget-17-budget-healthcheck-template.md`
- `content/engine-notes/game-budget-18-scene-budget-template.md`
- `content/engine-notes/game-budget-19-character-budget-template.md`
- `content/engine-notes/game-budget-20-vfx-budget-template.md`

### 为什么优先补这 10 篇

- `Android` 和 `Apple` 预算不补，主线里的内存预算会缺少最关键的平台现实。
- `Android / iOS 抹平方法` 不补，团队很容易在平台差异里反复走到“高配平台把基础内容做胖”的老路。
- `Web / 小程序` 不补，预算语言会默认只服务原生移动端，覆盖面不完整。
- `模板` 不补，`预算-10` 这篇就很容易停在“应该进入流程”，而不是“到底怎么进”。

### 第二阶段推荐写作顺序

1. `game-budget-17-budget-healthcheck-template.md`
2. `game-budget-11-android-memory-budgets-2gb-to-6gb.md`
3. `game-budget-12-apple-memory-budgets-legacy-and-current.md`
4. `game-budget-13-android-ios-budget-smoothing.md`
5. `game-budget-16-budget-master-sheet-template.md`
6. `game-budget-18-scene-budget-template.md`
7. `game-budget-19-character-budget-template.md`
8. `game-budget-20-vfx-budget-template.md`
9. `game-budget-14-web-platform-budget-browser-wasm-cache.md`
10. `game-budget-15-miniapp-platform-budget-wechat-alipay-douyin.md`

## 推荐阅读顺序

1. `预算-00`
2. `预算-01`
3. `预算-02`
4. `预算-03`
5. `预算-04`
6. `预算-05`
7. `预算-06`
8. `预算-07`
9. `预算-08`
10. `预算-09`
11. `预算-10`

## 推荐写作顺序

1. `预算-00`
原因：先把“预算不是技巧清单，而是边界管理和判断语言”这层骨架立住。

2. `预算-01`
原因：把“现有项目怎么判断合理不合理”提前，读者才能把后续每篇都拿去照项目。

3. `预算-02`
原因：你已经有 `包体优化` 相关内容资产，最容易先拉出一篇“预算版”而不是“技巧版”。

4. `预算-03`
原因：你已有“内存不是够不够，而是行为稳不稳”的判断基础，适合顺势改写成预算语言。

5. `预算-06`
原因：先把一帧里的链路分账写清，后面的 CPU / GPU / 场景 / 特效篇会更稳。

6. `预算-04`
原因：CPU 预算最适合接在帧预算拆账之后，把逻辑时间和尖峰时机讲透。

7. `预算-05`
原因：GPU 预算也依赖帧链路视角，放在 CPU 后面能自然对照。

8. `预算-07`
原因：场景预算是第一个真正把多桶预算收成内容对象的篇目。

9. `预算-08`
原因：角色预算最适合承接场景预算，把“单体对象规格”讲实。

10. `预算-09`
原因：特效预算是最典型的多预算耦合案例，适合作为内容预算收束。

11. `预算-10`
原因：最后再把预算接回规格、分档、Baseline 和 CI，形成工程闭环。

## 每篇统一写法

为了避免这组文章后面写散，每篇建议都按同一骨架写：

1. 先定义这类预算到底在管什么，不在管什么。
2. 再拆这类预算的层次，不要只给一个总数字。
3. 再解释这类预算最常见的超线形态，读者在现场会看到什么。
4. 再给出判断依据：为什么这条线这样立，它背后的物理事实、平台事实、运行时事实是什么。
5. 再说明最常见的误判和“假优化”，包括预算转嫁会把账转到哪里。
6. 最后给出落地方式：指标、告警、责任边界、验证动作。

也就是统一回答：

- 成本对象是什么
- 预算线怎么定
- 判断依据是什么
- 超线会先显形在哪里
- 常见误判是什么
- 怎样进入流程

## 每篇都必须有的“证据块”

为了避免文章只停在“作者经验”，每篇都建议固定带三类证据：

1. `物理或系统事实`
例如帧时间上限、移动端热收缩、LMK / jetsam、带宽与分辨率关系、包体下载与安装流失关系。

2. `运行时链路事实`
例如资源会同时存在磁盘副本、CPU 副本、GPU 副本；透明特效会带来 overdraw；Read/Write 会抬高双份驻留；对象池会省 GC 但抬高稳态常驻。

3. `工程治理事实`
例如预算如果没有规格表、负责人、超线动作、回归门禁，最后一定会退回口头约定。

## 预算体检文要回答什么

`预算-01` 不只是目录补丁，它是整组里很关键的一篇。它至少要回答下面这些诊断问题：

- 项目有没有明确区分首包、热更包、常驻内存、峰值内存、CPU 帧时间、GPU 帧时间。
- 项目有没有“硬线、告警线、观察线”，还是只有模糊愿望。
- 预算是不是只存在于主程脑子里，还是已经变成角色 / 场景 / 特效 / 贴图的可执行规则。
- 项目是不是只在旗舰机和开发机场景里看起来成立。
- 超线以后有没有固定动作：阻断、降档、返工、例外审批、回归验证。

如果一套系统回答不了这些问题，它就还不能算真正有预算体系。

## 对现有旧文的调整建议

### 建议改写后并入本系列的文章

- `content/code-quality/package-size-optimization-stripping-split-packages-and-asset-trimming.md`
  当前更像“优化手段综述”；改写后更适合承担 `预算-02` 的前身，重点从“怎么瘦”切到“预算层次怎么立”。
  建议修改方向：
  - 标题从“包体大小优化”改成更像“包体预算与交付分层”的入口或支撑文。
  - 开篇先拆 `首包 / 全量包 / 热更包 / 常驻资源 / 平台差异包`，再讲 stripping、分包和资源治理分别站在哪一层。
  - 文末增加“预算责任边界”和“超线动作”，不要停在手段清单。

- `content/engine-notes/cpu-opt-05-memory-budget.md`
  当前标题挂在 `CPU 性能优化` 下有点窄；改写后更适合独立承担 `预算-03`，把“内存预算不是 CPU 子问题”这层边界拉正。
  建议修改方向：
  - 标题去掉 `CPU 性能优化 05` 这个前缀，避免把内存预算误收成 CPU 子篇。
  - 保留现有 `稳态线 / 峰值线 / 红线 / 内存桶` 结构，但再补 `显存 / 驱动内存 / 双驻留 / 工作集` 这几层。
  - 明确“项目体检怎么判断内存预算合理”，让它能和 `预算-01`、`预算-10` 形成回链。

### 建议保留为支撑文、不直接并入目录的文章

- `content/code-quality/baseline-budgets-in-ci.md`
- `content/engine-notes/game-performance-methodology-summary.md`
- `content/engine-notes/device-tier-asset-spec-texture-and-package.md`
- `content/engine-notes/device-tier-visual-tradeoff-priority.md`
- `content/engine-notes/urp-platform-05-content-tiering.md`

这些更适合作为入口、证据或落地延伸，而不是硬塞进主目录。

## 现有内容可复用

### 可直接复用为前置或引用的文章

- `content/code-quality/package-size-optimization-stripping-split-packages-and-asset-trimming.md`
- `content/code-quality/baseline-budgets-in-ci.md`
- `content/engine-notes/game-performance-methodology-summary.md`
- `content/engine-notes/device-tiering-series-index.md`
- `content/engine-notes/cpu-opt-05-memory-budget.md`

### 可复用为下游延伸的文章群

- `content/engine-notes/cpu-opt-*.md`
- `content/engine-notes/gpu-opt-*.md`
- `content/engine-notes/mobile-tool-06-read-gpu-counter.md`
- `content/engine-notes/urp-platform-05-content-tiering.md`

## 当前最短结论

如果你现在就要开写，我建议把这组文章正式命名成：

`游戏预算管理`

并把它看成一条独立的小主线，而不是把四篇文章分别塞回 `包体优化`、`性能判断`、`CPU/GPU 优化` 或 `Code Quality`。

因为这组文章真正值钱的地方，不是单篇技巧，而是它能不能把“预算”变成你整套内容体系里一门独立、稳定、可复用的判断语言。
