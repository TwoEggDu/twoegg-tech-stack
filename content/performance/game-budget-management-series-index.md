---
date: "2026-03-29"
title: "游戏预算管理系列索引｜先立预算语言，再做分账、体检和规则落地"
description: "给游戏预算管理补一个稳定入口：先分清预算到底在管什么，再把包体、内存、CPU、GPU、帧链路、场景、角色、特效和流程治理接成一条完整主线。"
slug: "game-budget-management-series-index"
weight: 2050
featured: false
tags:
  - "Performance"
  - "Budget"
  - "Unity"
  - "Mobile"
  - "Index"
series: "游戏预算管理"
series_id: "game-budget-management"
series_role: "index"
series_order: 0
series_nav_order: 146
series_title: "游戏预算管理"
series_entry: true
series_audience:
  - "客户端 / 引擎开发"
  - "TA / 图形程序"
  - "Tech Lead"
series_level: "进阶"
series_best_for: "当你想把性能、包体、内容规格和验证门禁统一拉回同一套预算语言"
series_summary: "把预算从抽象 KPI 变成可执行的边界系统：先立线，再分账，再回写到规则、分档、Baseline 和 CI。"
series_intro: "这组文章关心的不是“记几个优化技巧”，而是先建立一套预算管理语言：什么叫预算，为什么预算不是单个数字，怎样判断一个项目有没有预算体系，以及预算怎样从总盘子一路落到场景、角色、特效和日常规则。预算做得稳，本质上不是更会救火，而是更早把边界立住。"
series_reading_hint: "第一次系统读，建议先看总论和体检，再走包体、内存、CPU、GPU 和帧预算拆账；如果你现在正带着具体项目问题来，可以直接从“预算体检”和对应的对象预算进入。"
---
> 这页是“游戏预算管理”的专门入口。它处理的不是某一项优化技巧，而是同一个项目里最容易彼此打架的几条线：包体、内存、CPU、GPU、场景规格、角色规格、特效规模和流程门禁。

很多团队其实不是完全没有优化动作，而是始终缺一件事：

`没有一套能让内容、代码、资源和流程说同一种话的预算语言。`

这会导致几种很熟悉的状态：

- 主程盯 CPU，TA 盯 GPU，美术只看到贴图规格，但没人对“总账”负责
- 包体已经很紧了，却还在用增加包体的方式换运行时稳定
- 低档机型已经降到了极限，但角色、场景、特效还没有真正跟着分层
- 线上已经反复掉帧、强杀、发热、更新膨胀，但团队仍然只会单点救火

这组文章要收的，就是这件事。

## 先看哪几篇

### 1. 先把预算语言立住

1. [游戏预算总论：为什么很多性能和交付问题，本质上是预算管理失败]({{< relref "performance/game-budget-00-overview-why-budget-management-fails.md" >}})
2. [预算体检：怎么判断你现在的项目预算体系到底合不合理]({{< relref "performance/game-budget-01-budget-healthcheck.md" >}})

这一段先解决两个根问题：

- 预算到底在管什么，不在管什么。
- 一个项目到底是没有预算、预算失真，还是预算根本没接进执行。

### 2. 再看四条总预算主线

1. [包体预算怎么定：首包、全量包、热更和常驻资源为什么不能混成一个数字]({{< relref "performance/game-budget-02-package-budget.md" >}})
2. [内存预算怎么定：常驻、峰值、工作集、显存和 OOM 风险要分开管理]({{< relref "performance/game-budget-03-memory-budget.md" >}})
3. [CPU 预算怎么定：帧时间、主线程尖峰、异步任务和危险时机]({{< relref "performance/game-budget-04-cpu-budget.md" >}})
4. [GPU 预算怎么定：像素、带宽、RT、阴影、后处理和分辨率缩放]({{< relref "performance/game-budget-05-gpu-budget.md" >}})

这一段处理的是项目总账：

- 下载和交付的账怎么立
- 运行时内存的账怎么立
- CPU 和 GPU 的帧预算怎么立

### 3. 再把“一帧”和“内容对象”拆开

1. [帧预算拆账：逻辑时间、渲染提交、GPU 时间、加载尖峰和同步等待怎么分账]({{< relref "performance/game-budget-06-frame-budget-breakdown.md" >}})
2. [场景预算怎么定：可见物、灯光、阴影、Streaming、触发器和大世界边界]({{< relref "performance/game-budget-07-scene-budget.md" >}})
3. [角色预算怎么定：面数、骨骼、材质、动画、蒙皮和同屏数量怎么一起算]({{< relref "performance/game-budget-08-character-budget.md" >}})
4. [特效预算怎么定：发射器、活跃粒子、屏占比、透明叠层和技能时机]({{< relref "performance/game-budget-09-vfx-budget.md" >}})

这一段处理的是“怎么分账”：

- 一帧内部到底是谁在结账
- 场景、角色、特效这些内容对象到底在吃哪几个预算桶

### 4. 最后看规则和门禁

1. [预算怎样进入规格、分档、Baseline 和 CI：从规则表到门禁和回归]({{< relref "performance/game-budget-10-rules-tiering-baseline-ci.md" >}})

这一篇处理的是最后一步：

- 预算怎样从“知道了”变成“做得出来”

### 5. 平台附录：把预算放回真实容器

1. [Android 内存预算附录：从 2GB、3GB、4GB 到 6GB+ 怎么立线]({{< relref "performance/game-budget-11-android-memory-budgets-2gb-to-6gb.md" >}})
2. [Apple 内存预算附录：legacy 1GB / 2GB 与 current 3GB / 4GB / 6GB+ 怎么区分]({{< relref "performance/game-budget-12-apple-memory-budgets-legacy-and-current.md" >}})
3. [同一项目怎么抹平 Android 和 iOS 的预算差]({{< relref "performance/game-budget-13-android-ios-budget-smoothing.md" >}})
4. [Web 平台预算：浏览器页签、WASM、纹理上传和缓存怎么一起算]({{< relref "performance/game-budget-14-web-platform-budget-browser-wasm-cache.md" >}})
5. [小程序 / 小游戏平台预算：微信、支付宝、抖音这些容器为什么不能直接照搬手游预算]({{< relref "performance/game-budget-15-miniapp-platform-budget-wechat-alipay-douyin.md" >}})

这一段处理的是：

- 同一套预算语言怎样进入不同平台容器
- 为什么 Android、Apple、Web 和小游戏平台不能共用一张简单预算表

### 6. 模板入口：把预算变成可执行表格

1. [总预算总表模板：包体、内存、CPU、GPU、硬线和超线动作怎么收成一张主账本]({{< relref "performance/game-budget-16-budget-master-sheet-template.md" >}})
2. [预算体检模板：怎么把“项目预算是否合理”审成一张可执行检查表]({{< relref "performance/game-budget-17-budget-healthcheck-template.md" >}})
3. [场景预算模板：固定机位、Streaming 峰值和场景切换该怎么验]({{< relref "performance/game-budget-18-scene-budget-template.md" >}})
4. [角色预算模板：单体规格、同屏数量和目标档位怎么一起填]({{< relref "performance/game-budget-19-character-budget-template.md" >}})
5. [特效预算模板：发射器、屏占比、对象池和技能时机怎么验收]({{< relref "performance/game-budget-20-vfx-budget-template.md" >}})

这一段处理的是：

- 预算怎样从文章语言变成日常执行语言
- TA、美术、客户端、制作怎么围着同一张表说话

## 平台附录

主线 11 篇先把预算语言、总预算、分账预算和门禁逻辑立住。  
如果你要把它继续落到真实平台差异上，建议接着读这 5 篇附录：

1. [Android 内存预算：从 2GB 到 6GB+ 怎么定]({{< relref "performance/game-budget-11-android-memory-budgets-2gb-to-6gb.md" >}})
2. [Apple 内存预算：legacy 1GB / 2GB 和 current 3GB / 4GB / 6GB+ 怎么分]({{< relref "performance/game-budget-12-apple-memory-budgets-legacy-and-current.md" >}})
3. [Android / iOS 预算抹平：共同底线、平台上浮和差异包怎么分工]({{< relref "performance/game-budget-13-android-ios-budget-smoothing.md" >}})
4. [Web 平台预算：浏览器、WASM 和缓存怎么一起看]({{< relref "performance/game-budget-14-web-platform-budget-browser-wasm-cache.md" >}})
5. [小程序 / 小游戏平台预算：微信、支付宝、抖音等容器边界怎么定]({{< relref "performance/game-budget-15-miniapp-platform-budget-wechat-alipay-douyin.md" >}})

这一段不是补充“优化技巧”，而是补真实容器里的预算边界。

## 模板入口

如果你已经开始想把预算从“文章”落到“团队日常规则”，可以直接看这 5 篇模板文：

1. [预算总表模板：把包体、内存、CPU、GPU 和门禁动作放进同一张表]({{< relref "performance/game-budget-16-budget-master-sheet-template.md" >}})
2. [预算体检模板：把项目审计写成可直接打分的检查表]({{< relref "performance/game-budget-17-budget-healthcheck-template.md" >}})
3. [场景预算模板：把可见物、灯光、阴影和 Streaming 写成验收口径]({{< relref "performance/game-budget-18-scene-budget-template.md" >}})
4. [角色预算模板：把面数、骨骼、材质和同屏数写成交付标准]({{< relref "performance/game-budget-19-character-budget-template.md" >}})
5. [特效预算模板：把发射器、粒子、屏占比和技能时机写成规范]({{< relref "performance/game-budget-20-vfx-budget-template.md" >}})

模板文的作用只有一个：

`把预算从“知道”变成“所有人都按同一张表执行”。`

## 为什么现在还要补平台附录和模板

主线 11 篇主要解决的是：

- 预算语言怎么立
- 总账和分账怎么拆
- 规则和门禁怎么接

但一个项目真的开始落地后，最容易卡住的两件事是：

- 不同平台容器差异太大，不知道该按哪条现实来收线
- 团队虽然认同预算，但没有可直接填、可直接验的执行表

所以这组附录和模板，处理的不是“补几篇支线”，而是把这条主线补成：

`可跨平台、可审项目、可日常执行。`

## 如果你带着具体问题来

- 不知道现在项目到底算不算“有预算体系”：
  先看 [预算体检]({{< relref "performance/game-budget-01-budget-healthcheck.md" >}})。
- 包体每版都在涨，但团队总在发版前临时瘦身：
  先看 [包体预算]({{< relref "performance/game-budget-02-package-budget.md" >}})。
- 明明总内存没爆，但低端机还是长玩后卡、切场景抖、回前台死：
  先看 [内存预算]({{< relref "performance/game-budget-03-memory-budget.md" >}})。
- 帧率不稳，但团队总在 CPU、GPU、加载之间互相甩锅：
  先看 [帧预算拆账]({{< relref "performance/game-budget-06-frame-budget-breakdown.md" >}})。
- 美术和 TA 需要知道场景、角色、特效到底该怎么收规格：
  先看 [场景预算]({{< relref "performance/game-budget-07-scene-budget.md" >}})、[角色预算]({{< relref "performance/game-budget-08-character-budget.md" >}})、[特效预算]({{< relref "performance/game-budget-09-vfx-budget.md" >}})。
- 预算写过几版文档，但最后总是落不进规则和 CI：
  先看 [预算怎样进入规格、分档、Baseline 和 CI]({{< relref "performance/game-budget-10-rules-tiering-baseline-ci.md" >}})。
- 最低支持设备已经压到 2GB Android，或者你在纠结 Apple 到底该不该从 1GB 起算：
  先看 [Android 内存预算附录]({{< relref "performance/game-budget-11-android-memory-budgets-2gb-to-6gb.md" >}}) 和 [Apple 内存预算附录]({{< relref "performance/game-budget-12-apple-memory-budgets-legacy-and-current.md" >}})。
- 同一个项目在 Android 和 iOS 上总是越做越分裂：
  先看 [Android / iOS 预算抹平方法]({{< relref "performance/game-budget-13-android-ios-budget-smoothing.md" >}})。
- 你已经理解了预算概念，但团队还是落不到表和验收：
  先看 [总预算总表模板]({{< relref "performance/game-budget-16-budget-master-sheet-template.md" >}}) 和 [预算体检模板]({{< relref "performance/game-budget-17-budget-healthcheck-template.md" >}})。

## 这组文章和哪些旧文互补

如果你已经读过下面这些旧文，这组会更容易连起来：

- [从现象到方法：把游戏性能判断连成一套工作流]({{< relref "performance/game-performance-methodology-summary.md" >}})
- [内存不是够不够，而是行为稳不稳]({{< relref "performance/game-performance-memory-behavior.md" >}})
- [包体大小优化：IL2CPP Managed Stripping、Split APK/AAB、iOS On-Demand Resources、资产精简策略]({{< relref "code-quality/package-size-optimization-stripping-split-packages-and-asset-trimming.md" >}})
- [Baseline：性能 / 包体 / 加载 / Crash 预算怎样立线并进 CI]({{< relref "code-quality/baseline-budgets-in-ci.md" >}})

它们已经分别讲了诊断、内存行为、包体分层和流程门禁。

这组文章要补的是中间那层：

`怎样把这些局部判断，统一收成一套项目预算体系。`

{{< series-directory >}}
