---
title: "Harness Engineering 04｜Drift 与文档腐烂"
slug: "harness-engineering-04-drift-and-rot"
date: "2026-05-13"
description: "Harness 的规则会跟代码慢慢脱节——可能是版本升级、可能是约定变更、可能是约定本来就没人维护。Drift 比 Bloat 更隐蔽、危害更大。这篇讲怎样发现、怎样修。"
tags:
  - "Harness Engineering"
  - "AI Engineering"
  - "Documentation Rot"
series: "Harness Engineering"
primary_series: "harness-engineering"
series_role: "article"
series_order: 40
weight: 2140
---

> **读这篇之前**：建议先读 [02｜v0 之后——Harness 的五阶段生命周期]({{< relref "harness-engineering/harness-engineering-02-five-stage-lifecycle.md" >}}) 和 [03｜Bloat 反模式与瘦身]({{< relref "harness-engineering/harness-engineering-03-bloat-and-slimming.md" >}})。这一篇假设你能区分 Bloat 和 Drift。

## 这篇解决什么问题

Bloat 是可见的——CLAUDE.md 变长、Skill 变多，你打开文件就能看到。

Drift 是不可见的——CLAUDE.md 看起来还是那么长，规则也都还在，但它们已经悄悄地跟代码不一致了。AI 按这些规则做事，写出的代码**编译能过、测试也能过、但语义已经错了**。

这种"看起来对、其实错"的代码最难发现。

这篇要回答的是：

- Drift 发生的具体机制
- 怎样在快节奏迭代里主动发现 Drift
- 机械化门禁 vs 人工 review 的边界
- 游戏项目里特别容易 Drift 的几个地方

## Drift 发生的四种机制

### 机制一：版本升级带来的语义漂移

- 例：Unity 2022 → Unity 6，某 API 行为静默变化
- CLAUDE.md 里写"用 X API 处理 Y 场景"——升级后 X 不再适合 Y，但规则没改
- 触发条件：大版本升级、Package 大版本升级、第三方 SDK 升级

### 机制二：代码先改、规则没跟

- 例：项目重构了配置表 schema，CLAUDE.md 里还在描述旧 schema
- 触发条件：重构没走过 Harness audit、约定变更没人通知

### 机制三：规则本来就没人维护

- 例：CLAUDE.md 里有一条"按照 X 文档处理 Y"，X 文档三年没动了，代码早就不这么干
- 触发条件：CLAUDE.md 引用外部链接、外部文档腐烂

### 机制四：约定从隐式变显式（或反过来）

- 例：原本"团队习惯都不改生成代码目录"是口头约定，后来变成 git hook 强制；CLAUDE.md 里关于这件事的描述没更新——可能太详细（重复了 hook 的工作），可能不再准确
- 触发条件：基础设施变更但文档没同步

<!-- EXPERIENCE-TODO: 每种机制后面补一个真实例子。机制一最容易写（Unity 大版本升级），机制三最危险（隐蔽）。 -->

## 怎样在快节奏迭代里主动发现 Drift

被动发现 Drift 的代价很大——通常是 AI 写错代码、QA 发现 bug、回头才查到规则失效。

主动发现的几个抓手：

### 抓手一：把"AI 按规则做了但人审下来错了"作为信号

- 每次代码 review 时，如果发现 AI 写的代码是"按 CLAUDE.md 做的但跟现状不符"——立即标记
- 一周内累计标记数超过阈值 → 触发 audit
- <!-- DATA-TODO: 给一个具体阈值建议，比如"3 个/周触发 audit"——需要真实运行后调 -->

### 抓手二：定期 audit 关键章节

- 把 CLAUDE.md 拆成"高频引用"和"低频引用"
- 高频部分每月 audit 一次
- 低频部分每季度 audit 一次
- audit 时只问一个问题：这条规则现在在代码里还真的成立吗

### 抓手三：大版本升级走专用 audit 流程

- Unity / Unreal 大版本升级、HybridCLR / Addressables 等核心 Package 大版本升级 → 必须走一次 Harness audit
- 流程：把 CLAUDE.md 全部规则过一遍，分三类（仍成立 / 需要改 / 直接删）
- audit 完之前，AI 不能跑新版本的写代码任务

### 抓手四：用"问题预存"主动暴露 Drift

- 把已知容易 Drift 的点写成具体问题，定期让 AI 回答
- 例如："本项目当前用 IL2CPP 还是 Mono？" "Addressables catalog 当前的更新策略是什么？"
- 答错或答含糊 → Drift 信号

<!-- EXPERIENCE-TODO: 补一份"问题预存"清单的真实样例——不必很多，5-10 条具体的、可验证的问题就够。 -->

## 机械化门禁 vs 人工 review 的边界

Drift 修复有两条路：让规则变成机械化门禁（git hook、CI check、lint 规则），或者保留为文本规则但靠人定期 audit。

### 适合机械化的规则

- 形态固定的（文件路径、命名约定、字段格式）
- 违反代价大的（破坏生成代码、破坏 GUID 引用）
- 检查代价小的（grep / lint 能跑出来）

### 适合保留文本 + 人审的规则

- 形态灵活的（"业务语义要保持一致"这类）
- 违反代价小的（不优雅但能跑）
- 检查代价大的（要读懂上下文才知道有没有违反）

### 一个常见误判

- 把"形态灵活但违反代价大"的规则强行机械化 → 误杀多、维护成本高
- 把"形态固定但违反代价小"的规则留着人审 → 没人审、必然 Drift

<!-- DATA-TODO: 补一份"我自己项目里的机械化门禁清单"——具体每条门禁的实现方式（git hook / CI check / lint），以及一份"靠人 audit 的清单"。 -->

## 游戏项目里特别容易 Drift 的几个地方

### 地方一：Unity / Unreal 版本相关的最佳实践

- 例：Coroutine 在 disabled GameObject 上的行为、UnityWebRequest 默认超时、Shader 编译 keyword 限制
- Drift 原因：版本升级时没人系统地复盘这些规则

### 地方二：Addressables / AssetBundle 的策略

- 例：分组策略、catalog 更新机制、加载 API
- Drift 原因：构建脚本改了但 CLAUDE.md 没改

### 地方三：协议 / 配置表 / 生成代码的目录约定

- 例："不要改 Generated/ 目录"——但目录名变成 `_Generated_` 之后规则没更新
- Drift 原因：基础设施重构

### 地方四：跨平台行为差异

- 例："iOS / Android 这两个 Shader keyword 不能用"——后来某个 Shader 优化把限制解除了
- Drift 原因：低频问题，没人主动复盘

### 地方五：团队约定 vs 公司约定

- 例：项目级 CLAUDE.md 说一种事、公司级 AGENTS.md 说另一种
- Drift 原因：两份文档不同节奏维护

<!-- EXPERIENCE-TODO: 五个地方每个补一段真实事故。"地方五"如果不便公开，可以匿名化处理或换成等价例子。 -->

## 收束

Drift 是 Harness 长期运营的最大隐患。它不像 Bloat 那样视觉上显眼，它需要主动发现的机制。

最短结论是：

**Bloat 让你写错；Drift 让你写对的代码做错的事。**

下一篇 [05｜跨仓库 Harness：SDK vendor 视角]({{< relref "harness-engineering/harness-engineering-05-cross-repo-sdk-vendor.md" >}}) 切到另一条独立线——当同一份代码 vendor 到多个项目时，Harness 信息归谁。

<!-- DATA-TODO: 等真实经历过一次大版本升级或一次配置表 schema 重构后，把这篇所有 TODO 都填实。"问题预存"清单和"机械化门禁清单"是这篇最有价值的两份附件。 -->
