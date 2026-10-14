---
date: "2026-04-27"
title: "Jenkins 升级踩坑：JVM / 插件 / 迁移"
description: '围绕 Java、Jenkins core、插件和配置的兼容关系设计升级与回退。区分控制器恢复、构建节点重建和实际产物验收。'
slug: "delivery-jenkins-ops-204-upgrade-pitfalls"
weight: 1580
featured: false
tags:
  - "Delivery Engineering"
  - "CI/CD"
  - "Jenkins"
  - "Upgrade"
  - "Stability"
series: "游戏团队 Jenkins 实战"
series_id: "delivery-jenkins-ops"
series_role: "article"
series_order: 100
delivery_layer: "practice"
delivery_volume: "V16"
delivery_parent_series: "delivery-cicd-pipeline"
delivery_reading_lines:
  - "L1"
  - "L2"
  - "L4"
---

> **核对说明（2026-10-14）**：本篇已修正原 Java 版本表。旧稿的三小时停机、具体插件 API 变化与固定三十分钟恢复目标未找到依据，已撤回为案例或通用要求；本轮只核对官方资料并补充演练设计，没有执行 Jenkins 升级。

## 在本篇你会读到

- Java、Jenkins core、插件与配置应按一组兼容版本评估。
- 升级前怎样留下能恢复的版本与状态。
- 怎样在隔离环境验证升级和回退，并确认产物仍可交接。

## 升级不是“点 Update 按钮”

升级会同时影响运行 Jenkins 的 Java、core、插件、Job 配置、Jenkinsfile 和 Shared Library。先列出当前与目标组合，再制定中间步骤。单独换回一个 war 文件不能证明整套系统已回到升级前。

## 失败模式 1：JVM 版本不兼容

### Jenkins 与 JVM 的版本依赖

旧稿把 weekly 与 LTS 边界混在一起。核对时应使用完整版本号与对应升级指南；“2.427+ 一直支持 Java 17”不成立。官方支持表同时覆盖 controller 与 agent 等 Jenkins 组件，构建业务代码所用的 JDK 则需要另行配置与验证。[Java 支持策略](https://www.jenkins.io/doc/book/platform-information/support-policy-java/)

一个已核对的具体边界：**升级到 Jenkins LTS 2.555.1 时，controller 和 agent JVM 均要求 Java 21 或 25**。若 Android 等业务构建仍需要其他 JDK，可在节点上分别指定运行 Jenkins 与执行业务工具的 Java，不能只改全局 `JAVA_HOME` 后便宣告完成。[2.555.x 官方升级指南](https://www.jenkins.io/doc/upgrade-guide/2.555/)

### 防范方法

先检查旧 core 是否支持计划中的新 Java，再检查目标 core 和插件要求。存在共同支持的版本时，可分步迁移；不存在时，需要设计受支持的中间版本或隔离迁移路径。“先升 JVM 再升 Jenkins”不是无条件适用的顺序。

验收记录至少包括 controller 和每类 agent 的实际 Java 版本、启动参数、core 版本，以及项目构建 JDK 的选择结果。启动失败时按具体错误与版本查证，不把所有 `UnsupportedClassVersionError` 都归因于同一段旧故事。

## 失败模式 2：插件兼容性

记录插件及其传递依赖的完整版本集合，列出关键 Pipeline 使用的步骤、配置项和 Shared Library 接口。升级指南和依赖声明用于筛查候选组合；仍需用代表性 Job 验证行为。

不要为了“每批只升几个”拆散有依赖关系的插件集合。每一批应形成可安装、可启动、可执行的组合，并保留与之匹配的配置快照。旧稿的某插件 API、Sandbox 开关和 `sh` 返回值变更没有具体版本或出处，本篇不再将它们写成已发生事实。

## 失败模式 3：配置迁移

备份范围需覆盖恢复所需的配置、Job 记录、插件版本与必要产物；可重建的 workspace 和缓存是否备份，由恢复目标决定。对运行中的系统随意复制目录，不能保证拿到同一时刻的一致状态。

Jenkins 官方建议把 `master.key` 与常规备份分开保护，并实际验证备份能否恢复。这里仅讨论保管职责，公开材料不包含密钥或生产备份内容。[备份与恢复文档](https://www.jenkins.io/doc/book/system-administration/backing-up/)

恢复验收同时检查：Job 是否加载、关键配置是否迁移、凭据是否在授权范围内可用、节点是否能连接，以及历史产物与记录是否满足团队的追溯需求。登录页面能打开只是第一项信号。

## 失败模式 4：Pipeline 语法 / API 变化

把核心行为写成升级前后的对照清单：参数解析、分支过滤、环境选择、失败传播、取消与清理、归档命名、测试回执。分别检查 Jenkinsfile、插件步骤与共享库的变化，避免把所有差异统称为“Jenkins 升级问题”。

### 隔离演练

演练实例在启动前就应隔离生产触发和外部写入：关闭计划触发、Webhook、下游生产 Job 与发布步骤，替换通知目标，使用仅供测试的凭据和存储。只删掉 secrets 既不能证明业务步骤可执行，也不能防止其他配置误连生产。

先用固定源码版本与固定输入跑代表性链路，再检查升级后的结果。共享库单元检查可以提前发现部分问题，但不能代替实际插件、agent 和工具链的集成验证。

## 回滚预案：按约定的恢复目标验收

回退单应记录旧版本组合、备份身份、恢复路径、执行人、触发条件和验收人。旧稿“三十分钟以内，否则不合格”的说法没有业务依据；恢复目标应由可接受停工时间决定，并通过演练测量。

1. 暂停新调度，确认在途任务与外部产物状态，防止恢复后重复执行。
2. 使用与旧 Java、core、插件匹配的配置和备份；避免让旧组件继续读取已经迁移且不兼容的配置。
3. 先在隔离环境确认节点连接、权限与代表性流水线，再逐项恢复触发。
4. 记录恢复耗时、缺失状态、重复或丢失的任务，以及测试方能否接受产物。

“退回旧版本”也可能影响升级期间新增的配置、Job 记录和产物；这些变化必须有保留与对账办法。哪些失败立即回退、哪些暂停排查，由事先约定的影响范围和恢复条件决定。

## 升级演练流程

| 阶段 | 主要交付物 | 验收问题 |
| --- | --- | --- |
| 核对当前与目标组合 | 版本清单、升级指南、依赖差异 | 每类 agent 与业务工具链是否兼容？ |
| 隔离恢复与升级 | 备份恢复记录、代表性 Job 结果 | 是否恢复了所需状态，且无生产写入？ |
| 升级后回退演练 | 旧组合恢复记录、数据对账 | 是否满足团队约定的恢复目标？ |
| 受控切换 | 调度恢复顺序、监控与责任人 | 失败时是否有可用的退路？ |

同一 controller 升级后，其下的 Job 共享 core；不能宣称“只让几个 Job 升级到新 core”。需要并行比较时，使用隔离 controller；同机升级后分批恢复 Job 调度只能降低初期业务暴露。

## 文末导读

控制器备份不能替代操作系统、SDK、Editor 和签名环境的重建。节点侧继续读 [构建节点重建验收]({{< relref "delivery-engineering/delivery-jenkins-ops-202-agent-scheduling.md#agent-rebuild-acceptance" >}})；运行后的信号见 [Jenkins 可观测性]({{< relref "delivery-engineering/delivery-jenkins-ops-205-observability.md" >}})，其旧阈值与案例仍待核实。
