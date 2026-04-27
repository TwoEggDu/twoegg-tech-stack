---
date: "2026-04-24"
title: "游戏团队 Jenkins 实战 · 系列索引"
description: "V16 讲完 CI/CD 通论和工具选型，但选定 Jenkins 之后怎么做好是另一回事。游戏团队的构建产物体积、License 稀缺性、平台矩阵让通用 CI 经验失效。本系列 17 篇，三子组覆盖流水线架构、稳定性运维、Unity 特化集成。"
slug: "delivery-jenkins-ops-series-index"
weight: 1570
featured: false
tags:
  - "Delivery Engineering"
  - "CI/CD"
  - "Jenkins"
  - "Unity"
  - "Index"
series: "游戏团队 Jenkins 实战"
series_id: "delivery-jenkins-ops"
series_role: "index"
series_order: 0
series_nav_order: 16
series_title: "游戏团队 Jenkins 实战"
series_entry: true
series_audience:
  - "TA / 构建工程师"
  - "DevOps / SRE"
  - "技术 Leader / 面试官"
series_level: "进阶到高级"
series_best_for: "团队已经选定 Jenkins 作为 CI 工具，承担游戏项目的构建产物，想把 Jenkins 真正做好"
series_summary: "从流水线架构、稳定性运维到 Unity 特化集成，17 篇覆盖游戏团队 Jenkins 的三个关键面——不讲入门、不讲选型，讲选定之后怎么撑住。"
series_intro: "V16 CI/CD 管线讲完了通论和工具选型，V16.6 给出了 Jenkins 的适用场景。本系列接在 V16.6 之后——选定 Jenkins 之后的那条路：流水线怎么设计、Master 怎么不崩、Unity 的特殊性怎么吃下。"
delivery_layer: "practice"
delivery_volume: "V16"
delivery_parent_series: "delivery-cicd-pipeline"
delivery_reading_lines:
  - "L1"
  - "L2"
  - "L4"
---

## 为什么单独开一个 Jenkins 系列

V16 CI/CD 管线系列已经讲完了通论——触发、构建、检查、部署、通知这五阶段管线模型；V16.6 工具选型也给出了"什么场景选 Jenkins"的决策依据。但这些都停在"选型"之前。

选定 Jenkins 之后，事情才真正开始：

- 流水线怎么设计才能撑住多产品线、多分支、多平台的矩阵？
- Master 节点为什么在"大作业 + 大产物"场景下容易崩？
- Unity 的构建有哪些地方是通用 CI 经验吃不住的？

这些问题放进 V16 不合适——会把一个通识系列压得变形；放进 V07 多端构建也不合适——那是讲 Unity 构建本身的。所以单独开一个系列，聚焦"选定 Jenkins 之后，怎么做好"。

本系列的差异化论证留给下一篇总论《为什么游戏团队的 Jenkins 是另一个物种》；这一页只做路由。

---

## 这个系列写给谁

| 读者 | 来这里找什么 | 建议阅读线 |
|------|------------|-----------|
| **TA / 构建工程师** | Unity 构建在 Jenkins 上怎么稳定运行、License 和大仓库怎么治理 | L1 游戏构建侧 |
| **DevOps / SRE** | Jenkins 在游戏项目场景下和 Web / 服务端有哪些不同的运维挑战 | L2 DevOps / SRE 侧 |
| **技术 Leader / 面试官** | 架构决策的判断依据、真实事故的复盘视角 | L3 技术 Leader 侧 |

如果你是 Jenkins 初学者（没配过 Job、没写过 Jenkinsfile），本系列不适合你——推荐先完成 Jenkins 官方入门教程再回来。

---

## 系列地图：三子组共 17 篇

### Part 0 · 导航

| # | 标题 | 核心问题 |
|---|------|---------|
| 000 | 游戏团队 Jenkins 实战 · 系列索引（本页） | 我是不是目标读者、从哪条阅读线进 |
| 001 | [为什么游戏团队的 Jenkins 是另一个物种]({{< relref "delivery-engineering/delivery-jenkins-ops-001-why-different.md" >}}) | 和 Web / 服务端比，游戏团队 Jenkins 的结构性差异在哪 |

### Part 1 · 流水线架构

多分支、多产品矩阵、共享库、参数化模板——"Jenkins 这台机器怎么组织起来"。

| # | 标题 | 核心问题 |
|---|------|---------|
| 101 | [Declarative vs Scripted Pipeline：游戏团队的选型取舍]({{< relref "delivery-engineering/delivery-jenkins-ops-101-declarative-vs-scripted.md" >}}) | 两种语法各自撑住什么规模、什么边界必须切换 |
| 102 | [Shared Library 设计：抽象出可复用的游戏构建原语]({{< relref "delivery-engineering/delivery-jenkins-ops-102-shared-library.md" >}}) | 哪些步骤值得下沉、边界怎么切 |
| 103 | [多产品矩阵：一套模板支撑 N 个产品线]({{< relref "delivery-engineering/delivery-jenkins-ops-103-multi-product-matrix.md" >}}) | 参数化的三个阶段演进与常见过度抽象 |
| 104 | [多分支流水线：Dev / QA / Release 自动化策略]({{< relref "delivery-engineering/delivery-jenkins-ops-104-multi-branch.md" >}}) | 分支策略怎么映射到 Multibranch / Folder |
| 105 | [Jenkins 下的并行模式：Parallel / Matrix / Triggers]({{< relref "delivery-engineering/delivery-jenkins-ops-105-parallel-modes.md" >}}) | 扇入扇出在 Jenkins 的三种实现与失败隔离 |

### Part 2 · 稳定性运维

Master 瓶颈、Agent 调度、磁盘/内存、升级踩坑、可观测性——"Jenkins 怎么别自己先挂"。

| # | 标题 | 核心问题 |
|---|------|---------|
| 201 | [Master 的三类瓶颈：连接、内存、磁盘 I/O]({{< relref "delivery-engineering/delivery-jenkins-ops-201-master-bottleneck.md" >}}) | 为什么在"大作业 + 大产物"场景下容易崩 |
| 202 | [Agent 调度与标签体系]({{< relref "delivery-engineering/delivery-jenkins-ops-202-agent-scheduling.md" >}}) | 标签设计失败会带来什么现象、怎么回到正轨 |
| 203 | [Workspace 与产物的磁盘治理]({{< relref "delivery-engineering/delivery-jenkins-ops-203-disk-governance.md" >}}) | 几十 G 产物怎么让磁盘不爆 |
| 204 | [Jenkins 升级踩坑：JVM / 插件 / 迁移]({{< relref "delivery-engineering/delivery-jenkins-ops-204-upgrade-pitfalls.md" >}}) | 升级路径的四个失败模式与回滚预案 |
| 205 | [Jenkins 自身的可观测性：监控与告警]({{< relref "delivery-engineering/delivery-jenkins-ops-205-observability.md" >}}) | Jenkins 自己挂了谁来发现 |

### Part 3 · Unity 特化集成

License、大仓库、符号表、多平台打包、IL2CPP——"游戏构建和通用构建不一样的那部分"。

| # | 标题 | 核心问题 |
|---|------|---------|
| 301 | [Unity License 池管理：稀缺资源的治理]({{< relref "delivery-engineering/delivery-jenkins-ops-301-license-pool.md" >}}) | License 不够用、泄漏、占用不释放的三类故障 |
| 302 | [大仓库在 Jenkins 下的 Workspace 策略]({{< relref "delivery-engineering/delivery-jenkins-ops-302-large-repo.md" >}}) | Git LFS / 浅克隆 / 缓存仓在真实大仓下的取舍 |
| 303 | [符号表与崩溃栈：IL2CPP 产物的符号链路]({{< relref "delivery-engineering/delivery-jenkins-ops-303-symbols-crashstack.md" >}}) | 线上崩溃栈还原怎么自动化 |
| 304 | [多平台并行打包与隔离]({{< relref "delivery-engineering/delivery-jenkins-ops-304-multi-platform-isolation.md" >}}) | 同时出 iOS / Android / WebGL 的资源冲突与隔离 |
| 305 | [IL2CPP 构建的时间与内存特征]({{< relref "delivery-engineering/delivery-jenkins-ops-305-il2cpp-build.md" >}}) | 为什么 IL2CPP 构建机最容易挂 |

---

## 三条阅读线怎么读

同一批 17 篇，不同读者从不同入口进更有效率。

### L1 · 游戏构建侧（TA / 构建工程师主力线）

**推荐顺序**：索引 → 001 总论 → Part 3（Unity 特化，5 篇）→ Part 1（流水线架构，5 篇）→ Part 2（稳定性，按需补）

**为什么这样**：你每天遇到的问题大多在 Unity 特化层——License 卡死、磁盘爆、打包互相踩——所以从 Part 3 进入立刻见效；建立问题感之后再回 Part 1 补架构思维；Part 2 是运维线的底座知识，用到再补。

### L2 · DevOps / SRE 侧

**推荐顺序**：索引 → 001 总论 → Part 2（稳定性，5 篇）→ Part 1（流水线架构，5 篇）→ Part 3（Unity 特化，5 篇）

**为什么这样**：你已经有通用 Jenkins 运维经验，Part 2 的游戏团队特殊性是直接增量；补完稳定性基础之后进 Part 1 看游戏团队的流水线抽象；最后进 Part 3 理解为什么这些特殊性存在。

### L3 · 技术 Leader 侧

**推荐顺序**：索引 → 001 总论 → 各组标记为"精选"的决策与事故复盘篇

**为什么这样**：你的时间有限，要的是判断依据而不是实施细节。精选篇会聚焦"这个决策错了会怎样"和"真实事故的链路"，跳过教程性描述。

> 精选篇标记方式：文章 frontmatter 有 `leader_pick: true` 字段；在系列页通过筛选器渲染。

---

## 作者立场与素材来源

本系列所有事故故事、架构决策、踩坑复盘都来自真实项目——不是翻译官方文档，不是复述官方 best practice，也不是把几篇英文博客拼起来。

这意味着：

- 有些结论会和 Jenkins 官方推荐不一致——因为官方推荐假设的是 Web / 服务端构建场景
- 有些"最佳实践"在游戏团队会失效——会说明失效的具体原因和替代方案
- 有些事故的真实根因会和表面症状差得很远——会展开定位链路

不适合带走就抄：每个团队的约束条件不同，可以借鉴判断框架，不要直接复制配置。

---

## 从哪里开始读

**我是 TA / 构建工程师，想快速开始解决自己团队的问题**：

1. 读完本页 + 001 总论（建立共同语境）
2. 直接跳 301 License 池管理（几乎所有游戏团队都踩过）
3. 按 Part 3 顺读，再回 Part 1

**我是 DevOps / SRE，想理解游戏团队的特殊性**：

1. 读完本页 + 001 总论
2. 直接跳 201 Master 三类瓶颈（游戏团队的"大作业"会挑战你已有的 Master 经验）
3. 按 Part 2 顺读，再进 Part 1 / Part 3

**我是技术 Leader / 面试官，想快速判断作者的技术判断力**：

1. 读完本页（约 20 分钟）
2. 读 001 总论（约 20 分钟）
3. 挑 3 篇精选篇（推荐：102 Shared Library 设计、201 Master 瓶颈、301 License 池）

---

## 与其他系列的关系

本系列是**交付工程旗舰专栏 V16（CI/CD 管线）的深度下钻子系列**。

- **前置必读**：[V16 · CI/CD 管线系列索引]({{< relref "delivery-engineering/delivery-cicd-pipeline-series-index.md" >}})——先建立 CI/CD 通识框架
- **上游接力**：[V16.6 · CI 工具选型]({{< relref "delivery-engineering/delivery-cicd-pipeline-06-tool-selection.md" >}})——选定 Jenkins 之后的具体运维就接进本系列
- **横向交叉**：
  - Unity 构建本身：[V07 · 多端构建系列]({{< relref "delivery-engineering/delivery-multiplatform-build-series-index.md" >}})
  - HybridCLR / 热更：[V08 · 脚本热更新系列]({{< relref "delivery-engineering/delivery-hot-update-series-index.md" >}})
- **扩展阅读**：[game-project-build-pipeline-jenkins-github-actions]({{< relref "code-quality/game-project-build-pipeline-jenkins-github-actions.md" >}})——单篇选型参考，和本系列的"运维实践"视角形成互补
