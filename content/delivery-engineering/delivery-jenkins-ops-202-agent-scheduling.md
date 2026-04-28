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

## 在本篇你会读到

- **标签不是名字** —— 标签是调度策略的输入
- **三个维度：能力 / 角色 / 容量** —— 缺一不可
- **标签设计反例** —— 一个团队的"标签爆炸"故事
- **调度策略** —— 排他、亲和、优先级
- **离线监控与自动恢复** —— Agent 掉线该怎么处理
- **平台特化运维：自动更新与强制重启** —— Windows / macOS / Linux 三类系统的"自动操作"杀手

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

游戏团队 IL2CPP 构建机至少要 `mem-32g`——见 305。配 IL2CPP 任务时调度到资源不足的 Agent 会 OOM。

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

某些任务**必须独占 Agent 整机**——比如 IL2CPP 构建（[详见 305]({{< relref "delivery-engineering/delivery-jenkins-ops-305-il2cpp-build.md" >}})），同 Agent 跑两个 IL2CPP 会 OOM。

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

Jenkins 不直接支持"亲和性调度"，但可以**通过 workspace 路径管理近似实现**：

```groovy
agent {
    node {
        label 'unity-builder'
        customWorkspace "/data/jenkins-workspaces/${env.JOB_NAME}"
    }
}
```

`customWorkspace` 让多次同名 build 用同一个工作目录——只要调度到同一台 Agent，Library 缓存就能复用。

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

- **JNLP 通道断开** → 几秒内重连成功 → 不影响 build
- **JNLP 通道断开** → 长时间未重连 → Agent 标记 offline → 进行中的 build 失败
- **Agent 心跳超时** → 标记 offline → 同上

### 自动恢复

Agent 配置层面：

```bash
# JNLP Agent 启动参数
-jnlpUrl http://master:8080/computer/agent-name/slave-agent.jnlp
-secret xxx
-workDir /home/jenkins
-disableHttpsCertValidation        # 自签证书时
-jvmargs -XX:+UseG1GC
```

启动脚本封装为 systemd / launchd 服务，挂掉自动重启：

```ini
# /etc/systemd/system/jenkins-agent.service
[Unit]
Description=Jenkins Agent
After=network.target

[Service]
ExecStart=/path/to/agent.sh
Restart=always
RestartSec=10
User=jenkins
```

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

调度策略和离线恢复都做对了，还有一个 build farm 常见杀手藏在你不太管的地方——**操作系统自己的自动更新策略**。Windows / macOS / Linux 都有"我觉得现在该重启了"的机制，半夜触发 = 凌晨 build 全死 = 早班 QA 拿不到包。

**小团队（5-10 台 Agent）受到的相对冲击更大——单机重启就是 10-20% 产能掉线。** 这一节讲三大系统各自的"自动操作"陷阱和治理方式。

### Windows：自动更新 + 强制重启（最常见杀手）

Windows 默认更新策略一句话总结：**它会按它想的时间重启你的机器**。半夜撞上的概率非常高——Windows 倾向于"用户不活跃时间"重启，而那正是游戏团队夜间 build 的高峰。

#### 三层治理

**第一层：GPO / 注册表锁定重启窗口**

域机器走组策略：

```
gpedit.msc → 计算机配置 → 管理模板 → Windows 组件 → Windows 更新
  ├─ 配置自动更新 → 已启用 + "通知下载并通知安装"
  ├─ 没有登录用户时不自动重启 → 已启用
  └─ 始终自动重启计划时间 → 改成你能控制的窗口（比如周六 04:00）
```

非域机器改注册表：

```
HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU
  AUOptions = 2                       # 仅通知，不自动下载
  NoAutoRebootWithLoggedOnUsers = 1
  ScheduledInstallDay = 7             # 周六
  ScheduledInstallTime = 4            # 04:00
```

**第二层：Agent 装成 Windows Service**

阻止不了重启的话，至少让重启后自动恢复：

```powershell
# 用 nssm 把 jenkins-agent 装成系统服务
nssm install JenkinsAgent "C:\Program Files\Java\jdk-17\bin\java.exe"
nssm set JenkinsAgent AppParameters "-jar C:\jenkins\agent.jar -jnlpUrl <url> -secret xxx -workDir C:\jenkins"
nssm set JenkinsAgent Start SERVICE_AUTO_START
nssm set JenkinsAgent AppRestartDelay 10000
```

机器重启 → 服务自启 → Agent 重连 Master。**正在跑的 build 仍会丢，只能减少损失。**

**第三层：主动维护窗口 + Agent 排空**

终极方案：与其被动接受重启，不如**主动把更新和重启放在你能控制的窗口**。脚本化流程：

```powershell
# 1. 通过 Jenkins API 排空 Agent（不再调度新 build）
Invoke-RestMethod -Uri "http://master:8080/computer/agent-1/toggleOffline?offlineMessage=scheduled-update" `
    -Method POST -Credential $jenkinsCred

# 2. 等正在跑的 build 完成（轮询 /computer/agent-1/api/json）

# 3. 跑 Windows Update（用 PSWindowsUpdate 模块）
Install-WindowsUpdate -AcceptAll -AutoReboot

# 4. 重启后 Agent 服务自启 → 再 toggle 回 online
```

#### 极端选项：完全禁用更新服务

```powershell
Stop-Service wuauserv -Force; Set-Service wuauserv -StartupType Disabled
Stop-Service UsoSvc -Force; Set-Service UsoSvc -StartupType Disabled
Stop-Service WaaSMedicSvc -Force; Set-Service WaaSMedicSvc -StartupType Disabled
```

**代价**：build 机不再收安全补丁。**只在 build 机完全在内网（不上公网）的前提下接受**。否则要配手动维护窗口定期补丁。

### macOS：System Update / Xcode 升级

macOS Agent（iOS 构建机）有两类自动操作问题：

#### 系统级 Software Update

游戏团队推荐**全部关掉自动**：

```bash
sudo softwareupdate --schedule off
sudo defaults write /Library/Preferences/com.apple.SoftwareUpdate AutomaticCheckEnabled -bool false
sudo defaults write /Library/Preferences/com.apple.SoftwareUpdate AutomaticDownload -bool false
sudo defaults write /Library/Preferences/com.apple.SoftwareUpdate AutomaticallyInstallMacOSUpdates -bool false
```

#### Xcode 自动升级（**这条最坑**）

Mac App Store 默认会自动升级 Xcode。问题：

- 新版 Xcode 改 build settings 默认值 → build 突然失败
- 升级期间锁住磁盘 → 进行中的 build 全 fail
- iOS 项目对 Xcode 版本敏感（Unity 项目在 Xcode 14 / 15 行为不同）

```bash
sudo defaults write /Library/Preferences/com.apple.commerce AutoUpdate -bool false
sudo defaults write /Library/Preferences/com.apple.SoftwareUpdate AutomaticallyInstallAppUpdates -bool false
```

**Xcode 升级必须是有计划的工程动作**：先在一台 sandbox Mac 试，验证 Unity 项目能 build，再灰度推到生产 Agent。

### Linux：unattended-upgrades 与 needrestart

#### Ubuntu / Debian：unattended-upgrades

默认装的 `unattended-upgrades` 自动装安全更新。问题：

- 内核更新后 needrestart 提示，**有些配置下会自动重启**
- 包升级中途锁住 dpkg → apt 操作中的 build 步骤失败

```bash
# 关掉自动重启
sudo sed -i 's|//Unattended-Upgrade::Automatic-Reboot ".*";|Unattended-Upgrade::Automatic-Reboot "false";|' \
    /etc/apt/apt.conf.d/50unattended-upgrades

# 或完全关掉 unattended-upgrades
sudo systemctl disable unattended-upgrades
```

#### needrestart 的自动重启提示

Ubuntu 22.04+ 默认装 `needrestart`，apt 操作后会问"是否重启服务"。在 build 机上改为 silent：

```bash
sudo sed -i "s/#\$nrconf{restart} = 'i';/\$nrconf{restart} = 'l';/" /etc/needrestart/needrestart.conf
# 'l' = list only（推荐）, 'a' = auto restart, 'i' = interactive
```

### 通用模式：维护窗口 + Agent 排空

不管哪个系统，都遵循同一个治理模式：

1. **关闭"系统自己决定"的自动操作**（更新、重启、升级）
2. **明确一个维护窗口**（每周 / 每月一次，避开发版高峰）
3. **维护窗口自动化**：脚本依次做"排空 Agent → 跑更新 → 重启 → 恢复"
4. **加监控告警**：Agent 离线超过预期时长就告警，不要靠人记得

### 小团队的简化路径

如果你只有 5-10 台 Agent，**直接关掉所有自动更新机制**，每月人工挑一天集中维护——比维护"完美的自动维护窗口脚本"更省事。脚本化的维护窗口适合 30+ Agent 规模才值得投入。

---

## 文末导读

下一步进 203 Workspace 与产物的磁盘治理——Agent 调度对了，但 Agent 上的磁盘还是会被产物撑爆。

L3 面试官线读者：本篇核心是"三维度标签"那一节——调度策略的工程化是治理边界的工程化。
