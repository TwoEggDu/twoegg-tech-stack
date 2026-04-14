---
title: "版本与分支管理 02｜分支策略——从开发到发布的代码流动"
slug: "delivery-version-management-02-branching"
date: "2026-04-14"
description: "分支策略不是 Git 操作问题，而是交付节奏问题。Main/Develop/Release/Hotfix 各自的角色、多团队怎么避免分支冲突、选型怎么做。"
tags:
  - "Delivery Engineering"
  - "Version Management"
  - "Branch Strategy"
  - "Git"
series: "版本与分支管理"
primary_series: "delivery-version-management"
series_role: "article"
series_order: 20
weight: 320
delivery_layer: "principle"
delivery_volume: "V04"
delivery_reading_lines:
  - "L1"
  - "L2"
  - "L3"
---

## 这篇解决什么问题

分支策略决定了代码怎样从开发流向发布。选错策略，团队会陷入合并冲突、发布阻塞或版本混乱。

## 为什么这个问题重要

分支策略出问题的典型场景：

- 所有人都往 main 分支直接提交，某次提交引入了崩溃 Bug，所有人的工作都被阻塞
- Release 分支拉出后，有人在 Develop 上修了一个紧急 Bug 但忘记 Cherry-Pick 到 Release，发版后 Bug 仍然存在
- 三个团队各自有 feature 分支，合入 Develop 时冲突量巨大，每次合入像一场战役
- Hotfix 分支修复了线上问题，但修复没有合回 Develop，下个版本又把同样的 Bug 带了回来

## 本质是什么

分支策略的本质是回答：**代码从写完到上线，经过哪些阶段，每个阶段在哪个分支上完成。**

### 四种角色分支

无论选择哪种具体的分支模型，以下四种分支角色是通用的：

| 分支角色 | 职责 | 生命周期 |
|---------|------|---------|
| **主干（Main/Master）** | 始终代表最新的已发布状态 | 永久 |
| **开发线（Develop）** | 集成所有已完成功能，是下一次发布的候选 | 永久 |
| **发布线（Release）** | 从 Develop 拉出，只接受 Bug 修复，准备发布 | 版本周期 |
| **热修复线（Hotfix）** | 从 Main 拉出，修复线上紧急问题 | 天级 |

加上按功能拆分的 **Feature 分支**（从 Develop 拉出，功能完成后合回 Develop），这就是经典的 GitFlow 模型。

### 三种主流策略

| 策略 | 适用场景 | 复杂度 | 发版节奏 |
|------|---------|--------|---------|
| **GitFlow** | 有明确发版周期的项目 | 高 | 2-4 周一次 |
| **GitHub Flow** | 持续部署、Web 项目 | 低 | 随时 |
| **简化 GitFlow** | 游戏项目（推荐） | 中 | 1-2 周一次 |

#### 简化 GitFlow（游戏项目推荐）

游戏项目通常不需要完整的 GitFlow 复杂度，但也不适合 GitHub Flow 的"随时发布"——因为游戏版本有审核周期（iOS 审核 1-3 天），不能真正做到持续部署。

推荐的简化模型：

```
main ──────●─────────────●────────────── (已发布版本)
           ↑             ↑
release ───┼──●──●──●────┘              (发布准备 + Bug 修复)
           │  ↑  ↑  ↑
develop ───┼──┼──┼──┼──●──●──●──●────── (日常开发集成)
           │     │
feature/A ─┘     │                       (功能开发)
feature/B ───────┘
```

规则：
- Feature 分支从 Develop 拉出，完成后合回 Develop
- Release 分支从 Develop 拉出，只接受 Bug 修复（不接受新功能）
- Release 测试通过后合入 Main，打 tag，发版
- Release 的 Bug 修复同时合回 Develop
- Hotfix 从 Main 拉出，修复后合入 Main + Develop

## 多团队分支协作

当团队规模从一个团队扩展到多个团队时，分支策略需要额外的协调机制：

### 命名规范

多团队的 feature 分支必须有命名规范，否则分支列表变成一团乱麻：

```
feature/{team}/{ticket-id}-{short-description}
例：feature/combat/PROJ-1234-skill-cooldown-fix
    feature/ui/PROJ-5678-new-shop-layout
```

### 合入节奏

多个 feature 分支合入 Develop 时的冲突管理：

**方案一：每日合入**。每个团队每天至少合入一次 Develop。冲突小、频率高。适合团队间代码耦合度低的项目。

**方案二：周合入窗口**。每周固定一个合入窗口（如周三下午），所有团队在窗口期集中合入。有人值班处理冲突。适合团队间代码耦合度高的项目。

**方案三：集成分支**。每个团队有自己的集成分支（team/combat-develop），先在团队内集成，再定期合入总 Develop。增加了一层缓冲，但也增加了管理成本。

### 代码所有权

多团队场景下，必须明确哪些目录/模块归哪个团队负责：

```
# CODEOWNERS 文件
/Assets/Modules/Combat/    @team-combat
/Assets/Modules/UI/        @team-ui
/Assets/Core/              @team-platform
```

跨团队修改（修改不属于自己的目录）必须经过对方团队的 Code Review。CODEOWNERS 通过 CI 自动执行，不是靠口头约定。

## 分支策略与发版节奏的关系

分支策略和发版节奏必须匹配：

| 发版节奏 | 推荐策略 | Release 分支存在时间 |
|---------|---------|-------------------|
| 每周发版 | 简化 GitFlow，Release 分支存在 2-3 天 | 短 |
| 双周发版 | 简化 GitFlow，Release 分支存在 3-5 天 | 中 |
| 月度发版 | 完整 GitFlow，Release 分支存在 1-2 周 | 长 |
| 按需发版 | GitHub Flow 变体，main 即 release | 无 |

Release 分支存在时间越长，合回 Develop 时的冲突越大。缩短 Release 分支的生命周期是减少合并痛苦的关键。

## 常见错误做法

**没有 Release 分支，直接从 Develop 发版**。Develop 上可能有未完成的功能，发版时只能靠 Feature Flag 关闭——增加了复杂度和风险。Release 分支的存在就是为了隔离"已完成待发布"和"正在开发"。

**Hotfix 只合入 Main 不合回 Develop**。线上紧急修复通常在 Hotfix 分支上完成后合入 Main 发版。但如果不同时合回 Develop，下个版本的 Release 分支就会再次包含这个 Bug。

**Feature 分支存在太久**。一个 feature 分支开发了两周不合入，和 Develop 的差距越来越大，最终合入时的冲突量爆炸。Feature 分支的生命周期应该控制在 3-5 天内。如果功能需要更长时间，用 Feature Flag 分段合入。

## 小结与检查清单

- [ ] 是否有明确的分支策略文档（不是"大家都知道"）
- [ ] Feature 分支是否有命名规范
- [ ] Feature 分支的生命周期是否控制在一周以内
- [ ] Release 分支的 Bug 修复是否同步合回 Develop
- [ ] Hotfix 是否同时合入 Main 和 Develop
- [ ] 多团队场景是否有 CODEOWNERS 和跨团队 Review 流程
- [ ] 分支策略是否和发版节奏匹配

---

**下一步应读**：[内容冻结与发布列车]({{< relref "delivery-engineering/delivery-version-management-03-content-freeze.md" >}}) — 分支策略确定后，内容冻结和发布节奏怎么协调

**扩展阅读**：[案例：多团队工程治理]({{< relref "projects/case-multi-team-governance.md" >}}) — 从 1 个团队到 3 个团队，分支策略和 CI 门禁怎样重新设计
