---
date: "2026-04-27"
title: "Agent 调度与标签体系"
description: '标签不是给 Agent 贴名字，是把"调度意图"工程化。游戏团队的 Agent 标签设计有三个维度：能力、角色、容量——任何一维设计失败都会让调度变成排队地狱。'
slug: "delivery-jenkins-ops-202-agent-scheduling"
weight: 1578
featured: false
tags:
  - "Delivery Engineering"
  - "CI/CD"
  - "Jenkins"
  - "Agent"
  - "Stability"
series: "游戏团队 Jenkins 实战"
series_id: "delivery-jenkins-ops"
series_role: "article"
series_order: 80
delivery_layer: "practice"
delivery_volume: "V16"
delivery_parent_series: "delivery-cicd-pipeline"
delivery_reading_lines:
  - "L1"
  - "L2"
---

> **证据状态（2026-10-14）**：标签从 5 台演进到 40 台的故事尚未核实；其数字不是已确认经历。新增环境重建部分是验收设计，本轮未执行。旧稿中“真实”字样不代表已核验；未确认来源的内容也不自动改称虚构示例。

## 在本篇你会读到

- **标签不是名字** —— 标签是调度策略的输入
- **三个维度：能力 / 角色 / 容量** —— 缺一不可
- **标签设计反例** —— 一个团队的"标签爆炸"故事
- **调度策略** —— 排他、亲和、优先级
- **离线监控与自动恢复** —— Agent 掉线该怎么处理
- **维护与重建验收** —— 维护窗口、环境清单和新节点交接

---

## 标签不是给 Agent 贴名字

Jenkins Agent 的标签（label）是字符串，但**它的语义是"调度过滤条件"**，不是"Agent 的别名"。

错误用法：

```
agent { label 'mac-mini-3' }   # 用机器名做标签
```

这种用法等于"我要 mac-mini-3 这一台机器"，调度灵活性归零——这台机器忙了 build 就排队，即使 mac-mini-1 / mac-mini-2 空着也用不上。

正确用法：把标签理解为**Pipeline 对 Agent 的能力要求**：

```
agent { label 'macos && unity-2022.3 && xcode-15' }
```

这告诉调度器：找一台**满足这些能力**的 Agent 来执行——具体哪台机器调度器决定。

---

## 三个维度：能力 / 角色 / 容量

游戏团队的 Agent 标签必须覆盖三个维度，缺一不可：

### 维度 1：能力（Capability）

Agent 上**装了什么、能做什么**：

- 操作系统：`linux` / `macos` / `windows`
- Unity 版本：`unity-2022.3` / `unity-6.0`
- 平台 SDK：`xcode-15` / `android-sdk-34` / `ndk-r25`
- 特殊工具：`hybridclr` / `gradle-8` / `node-18`

```
labels: linux unity-2022.3 android-sdk-34 ndk-r25 gradle-8
```

### 维度 2：角色（Role）

Agent 在 build farm 中**扮演的功能角色**：

- `unity-builder`：跑 Unity 构建任务的主力
- `test-runner`：跑自动化测试
- `archive-uploader`：归档与产物上传
- `monitoring`：监控类任务

为什么角色和能力分开？同一个 Agent 可能既能跑 build 也能跑测试，但你**不希望测试任务和长 build 任务争资源**——靠角色标签做调度隔离。

```
labels: linux unity-2022.3 unity-builder
```

### 维度 3：容量（Capacity）

Agent 的**资源规格分级**：

- `mem-32g` / `mem-64g`：内存等级
- `cpu-8` / `cpu-16`：CPU 等级
- `disk-large`：超大磁盘（专门跑大产物 build）

容量标签要来自代表性任务的峰值测量，并为并发和系统进程留出余量。不能按 IL2CPP 名称直接规定所有项目的最低内存。

```
labels: linux unity-2022.3 unity-builder mem-64g cpu-16
```

### 完整标签示例

一台 Agent 的完整标签集：

```
linux                     # OS
unity-2022.3 unity-6.0    # Unity 版本（共存安装）
android-sdk-34            # Android SDK
ndk-r25
gradle-8
hybridclr                 # HybridCLR 已配置
unity-builder             # 角色
mem-64g cpu-16 disk-large # 容量
```

Pipeline 申请 Agent：

```groovy
agent { label 'linux && unity-2022.3 && android-sdk-34 && unity-builder && mem-64g' }
```

---

## 标签设计反例：一个团队的"标签爆炸"故事

某游戏团队 18 个月演进的真实路径：

### 初期（5 台 Agent）：每台一个标签

```
agent-1 → mac-1
agent-2 → mac-2
agent-3 → linux-1
agent-4 → linux-2
agent-5 → win-1
```

Pipeline 写死 `agent { label 'mac-1' }`。问题立刻显现：mac-1 忙时 mac-2 空着也用不上。

### 中期（15 台 Agent）：按能力打标签

```
agent-1 → macos unity
agent-2 → macos unity
...
agent-10 → linux unity
...
```

调度灵活了，但出现"测试任务和构建任务挤一起"问题——测试 30 分钟跑不完是因为构建占了 8 个 Agent。

### 中后期（25 台 Agent）：加角色标签

```
agent-1 → macos unity unity-builder
agent-12 → linux unity test-runner
agent-15 → linux unity archive-uploader
...
```

测试和构建隔离了。但 Unity 升级后又出现新问题——某些 Pipeline 要 Unity 2021、某些要 Unity 2022.3，无法调度到对的 Agent。

### 现在（40 台 Agent）：加版本标签

```
agent-X → macos unity-2021.3 unity-builder
agent-Y → macos unity-2022.3 unity-builder
agent-Z → macos unity-2022.3 unity-6.0 unity-builder  # 双版本机
```

### 教训

- **标签设计要随团队规模演进**，但每次演进都要**统一所有 Agent 的标签格式**——不能新加的 Agent 用新规范，老 Agent 留旧标签
- **不要用机器名做标签**——一开始就要按"能力 / 角色 / 容量"打
- **每个维度独立**——能力和角色不要混（不要 `mac-builder` 这种合体标签）

---

## 调度策略

### 策略 1：排他（exclusive）

峰值资源需求高或工作目录尚未隔离的任务，可先使用单 executor 节点。是否允许并发，要按目标项目实测；不能断言同机两个 IL2CPP 构建一定 OOM。

Jenkins Pipeline 配置：

```groovy
pipeline {
    agent { label 'unity-builder && mem-64g' }
    options {
        // 该 Agent 在该 build 期间不接受其他 build
        // 通过减少 Agent 的 executors 数到 1 实现
    }
}
```

实际做法：把 IL2CPP 类 Agent 的 executors 配为 1（每台 Agent 同一时间只跑一个任务）。

### 策略 2：亲和（affinity）

希望同一个产品的 build **尽量调度到同一台 Agent**——为了利用 workspace 缓存。

`customWorkspace` 可以指定工作目录，但不能据此选择某一台节点；是否复用缓存仍取决于调度结果和输入是否兼容：

```groovy
agent {
    node {
        label 'unity-builder'
        customWorkspace "/data/jenkins-workspaces/${env.JOB_NAME}"
    }
}
```

`customWorkspace` 不是节点亲和策略。同一固定路径还需要互斥或按并发任务隔离；能否复用 Library 必须另外核对 Editor、平台和项目输入。

### 策略 3：优先级

发版分支的 build 优先级高于 dev 分支。Jenkins 的优先级插件：

- [Priority Sorter Plugin](https://plugins.jenkins.io/PrioritySorter/)
- 配置：每个 Job 设置 priority（数字越小越高）
- release/* 设 1，dev 设 5，feature/* 设 10

队列里 release build 永远排前面。

### 策略 4：限流（throttle）

防止某类任务占满 Agent 池：

- 全局：feature 分支 build 同时最多 5 个
- 单产品：TopHero 的 build 同时最多 3 个
- 单平台：iOS build 同时最多 2 个（macOS Agent 稀缺）

通过 [Throttle Concurrent Builds Plugin](https://plugins.jenkins.io/throttle-concurrents/) 实现。

---

## 离线监控与自动恢复

Agent 掉线是常态——网络抖动、Agent 重启、Master 重启都会导致 Agent 短暂离线。

### 离线信号

Master 把 Agent 标记为 offline 的几种情况：

- **连接短暂中断后恢复** → 核对 Pipeline 步骤是否支持续跑以及进程是否仍在，不预先保证构建不受影响
- **JNLP 通道断开** → 长时间未重连 → Agent 标记 offline → 进行中的 build 失败
- **Agent 心跳超时** → 标记 offline → 同上

### 自动恢复

节点启动方式应使用当前 Jenkins 节点页面给出的连接方法，并核对 agent 与 controller 的 Java 兼容要求。节点凭据由受控配置提供，避免写进公开启动脚本或日志；自签证书环境应配置可信证书链，不以关闭 HTTPS 证书校验作为默认解决方式。

进程可由对应操作系统的服务管理机制托管，但“服务自动重启”只表示尝试恢复连接，不代表原构建已经安全续跑。应设置重启退避和离线告警，并在重连后核对在途任务与残留进程。

### 监控告警

Jenkins 自身提供 Agent 离线状态 API：

```
GET /computer/api/json
```

外部监控（Prometheus / Grafana）拉这个 API，对 offline 状态发告警。

### 进行中 build 的丢失风险

Agent 长时间离线时，正在跑的 build 会失败。处理策略：

- **关键 build（release / hotfix）**：用专用稳定 Agent，不调度到不稳定 Agent
- **其他 build**：失败后自动 retry（在 Jenkinsfile 里加 retry 逻辑，但只对 transient 故障 retry）

**绝对不要做**：在 Jenkinsfile 全局 `retry(3)`——这会让 license 占用 / OOM 之类的非 transient 故障被无意义重试，浪费 Agent 时间（[详见 001 总论的"失败重试盲目化"]({{< relref "delivery-engineering/delivery-jenkins-ops-001-why-different.md" >}})）。

---

## 平台特化运维：自动更新与强制重启

系统补丁、Editor、SDK 和编译工具升级都应进入维护流程。旧稿按团队规模建议永久禁用更新，忽略了补丁维护职责；本轮撤下相关停用服务和注册表命令，改为明确维护窗口、验证节点和回退条件。

Windows 需要确认实际生效的更新与重启策略；macOS 还要记录所选 Xcode、SDK 和签名访问方式；Linux 需要核对包更新与服务重启行为。具体设置随系统版本和组织管理方式变化，本篇不提供未经对应环境验证的通用关闭命令。

维护时先停止给目标节点分配新任务，等待已有任务结束或按约定取消，再更新并重启。恢复连接后先完成环境探测和代表性构建，最后恢复调度。避免使用“切换在线状态”代替设置明确目标状态：维护脚本重复执行时，不应意外把待修机器重新放回池中。

## 构建节点重建验收 {#agent-rebuild-acceptance}

一台节点上的工具“都装好了”，还不足以证明它能替代旧节点。需要从版本清单重建环境，再用固定输入证明资源检查、导出、构建和交接仍然可用。以下是待执行的工程方案，本轮没有重建构建机。

### 环境清单应留下什么

| 层 | 应记录的可重建输入 | 恢复后的核对方式 |
| --- | --- | --- |
| 基础环境 | OS 与架构、补丁基线、服务账号权限、磁盘布局、区域与时区 | 环境探测记录与清单比对 |
| Jenkins 连接 | agent 启动方式、运行 Java、标签、executor 数 | 连接与调度检查；标签与实际能力一致 |
| 游戏工具链 | Editor 精确版本、平台模块、SDK/NDK、JDK、Xcode、插件锁定信息 | 检查实际执行路径和版本，避免只检查安装目录 |
| 项目输入 | 代码、资源、配置与构建脚本的固定版本 | 检出版本和输入清单比对 |
| 外部依赖 | 依赖源、下载校验、代理/证书配置、所需服务 | 使用测试范围内的权限验证可达与可用 |
| 敏感能力 | 签名与许可模式、凭据引用、注入与轮换负责人 | 受控注入后做权限检查；镜像和公开清单不保存秘密 |

基础镜像、安装脚本和环境清单也需要版本身份。缓存单独管理：先在干净 workspace、缺少可再生缓存的情况下完成链路，再测缓存复用；不能通过复制旧机器的全部目录来掩盖缺失依赖。

### 从重建到恢复接单

1. 选一台隔离的新节点，固定环境清单与项目输入。记录开始时间和操作者，定义团队可接受的恢复时间。
2. 按清单安装并注入测试所需的受控配置；不连接正式发布、通知或生产写入步骤。
3. 执行环境探测，再跑资源检查、导出与构建。保留每段结果，定位是环境失败还是内容失败。
4. 核对产物身份、结构与签名信息，并完成约定的安装/启动检查；测试方另行确认能否进入其测试流程。
5. 按场景验证任务失败、取消和节点重启后的状态核对，检查残留进程、workspace、调度名额和许可状态。
6. 达到约定验收条件后恢复接单，并记录失败回退方式、维护人和下次复核时间。

### 怎样判断“恢复了”

| 验收层 | 需要留下的结果 | 仍不能据此推断的事 |
| --- | --- | --- |
| 环境恢复 | 清单一致性、实际工具版本、连接与权限检查 | 所有项目都能构建 |
| 链路恢复 | 固定输入的检查、导出、构建和清理结果 | 游戏功能与设备性能已通过 |
| 产物交接 | 构建身份、文件清单、校验值、测试接收回执 | 线上发布与玩家体验已通过 |
| 恢复能力 | 实测耗时、人工补步骤、失败点与修订 | 下一次环境变化后仍自动有效 |

同一输入的两次产物哈希是否必须相同，取决于是否建立了确定性构建约束。签名、时间戳等可变字段存在时，应事先确定比较范围；不能把整包哈希不同直接判为恢复失败，也不能只凭构建成功就放行。

控制器恢复另见 [Jenkins 升级与回退]({{< relref "delivery-engineering/delivery-jenkins-ops-204-upgrade-pitfalls.md" >}})。一次节点重建只能覆盖该环境与代表任务，不替代完整构建系统的灾备演练。

---

## 文末导读

下一步进 203 Workspace 与产物的磁盘治理——Agent 调度对了，但 Agent 上的磁盘还是会被产物撑爆。

技术负责人：本篇核心是"三维度标签"那一节——调度策略的工程化是治理边界的工程化。
