---
title: "Harness Engineering 05｜跨仓库 Harness：SDK vendor 视角"
slug: "harness-engineering-05-cross-repo-sdk-vendor"
date: "2026-05-13"
description: "当同一份 SDK / Package vendor 到多个宿主项目，Harness 信息归谁、规则冲突怎么解。外部 4 篇文章全是单仓库，跨仓库这条线是独家空白。"
tags:
  - "Harness Engineering"
  - "SDK"
  - "Cross-Repository"
  - "AI Engineering"
series: "Harness Engineering"
primary_series: "harness-engineering"
series_role: "article"
series_order: 50
weight: 2150
---

> **读这篇之前**：本篇可以独立读，但如果你不是 SDK / Package / 共享工具链作者，可以跳过。建议先读 [01｜为什么游戏引擎客户端的 AI Coding 需要重新设计 Harness]({{< relref "harness-engineering/harness-engineering-01-why-game-engine-needs-rethink.md" >}}) 建立领域感。

## 这篇解决什么问题

外部讲 Harness 的文章基本都假设一个工程师在一个仓库里工作。但真实做工具链或者 SDK 的工程师不是这样——他维护一份代码，这份代码被 vendor 到三个、四个、甚至更多宿主项目里。

这时候 Harness 的几个基础问题会突然变得复杂：

- CLAUDE.md 写在 SDK 仓里还是宿主仓里
- SDK 的约定跟宿主的约定冲突时听谁的
- 同一条规则在 SDK 里是硬规则、在宿主里是建议——这种情况怎么写
- SDK 升级时，依赖它的 5 个宿主项目的 Harness 要不要同步升级、谁负责通知

这些问题外部 4 篇文章一篇都没碰。这是独家空白。

这篇要回答的是：

- Harness 信息的五层作用域分别意味着什么
- 三步判断流程：一条信息该放哪一层
- 四种 SDK + 宿主 Harness 协作模式
- 规则冲突的优先级裁决

## 五层作用域

```text
User-global    （用户级，跨所有项目）
    │
Org-Team       （团队级，跨同公司项目）
    │
Host project   （宿主项目级）
    │
Package        （SDK / Package 自身）
    │
Session        （单次对话）
```

### User-global

- 例：个人偏好（commit 不带 Co-Authored-By、输出中文）
- 放哪里：`~/.claude/CLAUDE.md`
- 不放什么：项目相关任何细节

### Org-Team

- 例：公司级 commit message 规范、命名约定、跨项目通用门禁
- 放哪里：内部共享文档库 / 跨项目 standards 仓
- 不放什么：具体项目的细节

### Host project

- 例：本宿主项目的目录结构、构建脚本、Unity 版本、业务约定
- 放哪里：宿主仓的 `CLAUDE.md` / `AGENTS.md`
- 不放什么：SDK 自身的规则（应该由 SDK 自带）

### Package（SDK / 共享 Package 自身）

- 例：SDK 的 API 约定、不可手改的生成代码目录、命名空间规则
- 放哪里：Package 内部带的 `CLAUDE.md` / Skill 描述
- 不放什么：使用这个 SDK 的宿主特定规则

### Session

- 例：单次任务的临时偏好（"这次重构请保留中文注释"）
- 放哪里：当下对话里说一句
- 不放什么：任何应该长期生效的规则

## 三步判断流程

每当你要写一条新规则，按这个流程跑：

### Step 1：这条规则跨几个项目有效？

- 跨所有项目 → User-global
- 跨同公司项目 → Org-Team
- 只在当前项目 → 继续 Step 2

### Step 2：这条规则跟哪个代码资产绑定？

- 跟 SDK / Package 代码绑定（SDK 改了规则也要改）→ Package
- 跟宿主项目业务绑定 → Host project
- 都没绑定（一次性偏好） → Session

### Step 3：这条规则下次有人用 SDK 时还需要知道吗？

- 需要 → 放 Package 内
- 不需要 → 放 Host project

<!-- EXPERIENCE-TODO: 配一张简单的判断流程图——mermaid 即可。 -->

## 四种 SDK + 宿主协作模式

不同的 SDK 跟宿主关系不一样，Harness 协作模式也不同。

### 模式 A：SDK 自带完整 Harness

- 特征：SDK 内带 `CLAUDE.md` / Skill，宿主只需引用
- 适合：SDK 自身有强约束（命名、目录、API 用法）、宿主对它无修改权
- 优点：升级 SDK 时 Harness 自动更新
- 缺点：宿主无法 override SDK 的部分规则

### 模式 B：SDK 不带 Harness，宿主完全负责

- 特征：SDK 只是代码资产，所有规则都在宿主仓
- 适合：SDK 简单稳定、几乎不需要规则
- 优点：宿主完全自由
- 缺点：每个宿主都要重写一遍 SDK 用法

### 模式 C：宿主主导，SDK 只提供建议

- 特征：SDK 内有"建议规则"文档，但不强制；宿主可全盘接受、修改、拒绝
- 适合：SDK 的规则跟宿主业务强耦合，必须 case by case
- 优点：灵活
- 缺点：升级 SDK 时宿主要重新审"建议"是否还有效

### 模式 D：混合——硬规则在 SDK、软建议在宿主

- 特征：SDK 自带"绝对不能改"的硬规则（机械化门禁优先），软建议留给宿主
- 适合：多数现实场景
- 优点：硬规则不会被宿主误删，软建议可以 case by case
- 缺点：需要明确区分硬 / 软，初期设计成本高

<!-- EXPERIENCE-TODO: 在 SH7.SDK / Zhulong / ShanHai 的真实场景里挑一个能公开化的，写它属于 A/B/C/D 哪种、为什么这么选。如果哪个都不便公开，就用泛化的"一个 Unity Package vendor 到三个游戏项目"作为例子。 -->

## 规则冲突的优先级裁决

宿主和 SDK 规则冲突时听谁的？默认优先级：

```text
Org-Team > Host project > Package > User-global > Session
```

- Org-Team 在最上：公司级规则不能被项目级覆盖
- Host project 第二：宿主有权拒绝 SDK 的某条规则（用模式 C/D）
- Package 第三：SDK 自带规则在没有宿主反对时生效
- User-global 第四：个人偏好不能覆盖项目约束
- Session 最低：临时偏好不能改变长期规则

### 例外情况

- SDK 的"机械化硬规则"（git hook、CI check）可以高于 Host project 文本规则——因为机械化已经不可绕过
- 安全相关规则（不能 commit 密钥、不能改 license）任何层级都不能覆盖

<!-- DATA-TODO: 补一份"真实跨仓库冲突的案例"——某次 SDK 升级时宿主项目规则跟 SDK 新版本规则冲突，最后怎么裁决的。 -->

## SDK 升级时的 Harness 同步

SDK 升版本时，依赖它的所有宿主项目的 Harness 都可能需要同步。流程：

### Step 1：SDK 自带 changelog 里标注"Harness 影响"

- 哪些规则变了
- 哪些新增了
- 哪些被弃用

### Step 2：宿主项目升 SDK 时走一次 Harness audit

- 对照 SDK changelog，更新宿主的 Harness
- 标记哪些规则需要本宿主自己 override

### Step 3：审 audit 结果

- 是否有规则被宿主和 SDK 都漏掉了
- 是否有规则只是"看起来没冲突"实际有冲突

<!-- EXPERIENCE-TODO: 等真实做过一次 SDK 升级伴随 Harness audit 后，补完整流程的真实节奏（花了多久、最难的是哪一步）。 -->

## 收束

跨仓库 Harness 是单仓库 Harness 的复杂化，但不是它的简单延伸——五层作用域、四种协作模式、优先级裁决，每一项都是独立设计。

最短结论是：

**单仓库时 Harness 是规则集合。跨仓库时 Harness 变成规则的层级架构——你需要先回答"这条规则归哪一层"，再回答"这条规则是什么"。**

下一篇 [06｜Harness 在交付工程里的位置]({{< relref "harness-engineering/harness-engineering-06-in-delivery-engineering.md" >}}) 切到场景联动线。

<!-- DATA-TODO: 这篇最依赖 SH7.SDK / Zhulong / ShanHai 的真实案例。发布前先确认哪些细节可以公开。不能公开的部分用"一个 Unity Package vendor 到三个游戏项目"作为等价匿名化例子。 -->
