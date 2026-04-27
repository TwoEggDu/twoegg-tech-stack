---
date: "2026-04-27"
title: "Jenkins 升级踩坑：JVM / 插件 / 迁移"
description: 'Jenkins 升级不是"点 Update 按钮"——它有四类失败模式：JVM 版本不兼容、插件冲突、配置迁移、Pipeline 语法变化。游戏团队规模下任何一类失败都会导致全队半天产出归零，必须有完整回滚预案。'
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

## 在本篇你会读到

- **升级不是"点 Update 按钮"** —— Jenkins 升级的四个层次
- **失败模式 1：JVM 版本不兼容** —— Jenkins 大版本对 Java 的隐式要求
- **失败模式 2：插件冲突** —— 插件依赖图比 Jenkins 本身复杂
- **失败模式 3：配置迁移** —— XML schema 漂移
- **失败模式 4：Pipeline 语法变化** —— 改 step 行为不向后兼容
- **回滚预案** —— 30 分钟内回到升级前
- **升级演练流程** —— 灰度 + Sandbox + 全量

---

## 升级不是"点 Update 按钮"

Jenkins 升级看似一个动作（替换 war 包 / 更新 Docker 镜像），实际涉及四个层次：

```
Layer 1: JVM            → 系统层，最难回滚
Layer 2: Jenkins core   → war 包替换
Layer 3: Plugins        → 几十到几百个插件
Layer 4: Configuration  → XML / Pipeline / Shared Library
```

每一层都可能出问题。"升级失败"通常是**某一层和其他层不匹配**。

游戏团队的特殊性：

- 多产品 + 多分支 + 复杂 Shared Library 让"配置面"庞大
- 单次 Master 停机的代价高（[详见 203 真实事故]({{< relref "delivery-engineering/delivery-jenkins-ops-203-disk-governance.md" >}})）
- 升级一旦失败，业务影响至少半天

不允许"试一下不行就回滚"——必须有计划、有演练、有预案。

---

## 失败模式 1：JVM 版本不兼容

### Jenkins 与 JVM 的版本依赖

Jenkins 不同大版本对 JVM 有不同要求：

| Jenkins 版本 | 最低 Java | 推荐 Java |
|------------|---------|----------|
| 2.357 之前 | Java 8 | Java 8 / 11 |
| 2.357 - 2.426 | Java 11 | Java 11 / 17 |
| 2.427+ | Java 17 | Java 17 / 21 |

跨大版本升级时**必须先升 JVM**——但很多团队 Jenkins 跑了几年没动过 JVM，跳级升级时炸。

### 真实故障

> 团队从 Jenkins 2.319 升到 2.450，没看 release notes，直接跑——启动报 `UnsupportedClassVersionError: 61.0`（Java 17 字节码）。当时 Master 跑的是 Java 11。
> 救火：先升 Java 17（涉及 OS 包管理 + JAVA_HOME 切换），再启 Jenkins，又出 GC 配置不兼容（G1 GC 参数在 Java 17 改了名）。
> 总停机 3 小时。

### 防范方法

升级前在 Jenkins release notes 里搜：

- "minimum supported version of Java" / "requires Java"
- "deprecated"

如果跨 Jenkins 大版本（比如 2.319 → 2.450 跨了一个 LTS 区间），**先单独升 JVM 再升 Jenkins**：

1. 测试环境装新 JVM，跑老版本 Jenkins，确认无问题
2. 生产升级 JVM
3. 跑几天确认 Jenkins 在新 JVM 上稳定
4. 再升 Jenkins

---

## 失败模式 2：插件兼容性

游戏团队 Jenkins 通常装 30-80 个插件——这是最容易踩雷的层。

### 插件依赖图的复杂度

每个插件声明：

- 最低 Jenkins core 版本
- 依赖的其他插件 + 版本范围

升级时 Jenkins 自动检查依赖，但**只检查"装了什么版本"，不检查"是否真的兼容"**——很多插件标的依赖范围比实际能跑的范围宽。

### 真实故障类型

#### 类型 A：插件配置漂移

老插件 v1.5 的 config.xml 和 v2.0 的 config.xml 字段不同，升级后某些配置丢失。典型：

> 升级 Pipeline plugin 后发现某些 Pipeline 的"启用 Sandbox"开关被重置为默认值——所有 Pipeline 突然又要审批 in-process script。业务方一片骂声。

#### 类型 B：API 不兼容

Shared Library 调用了某插件的 API，插件升级后 API 变了：

```groovy
// 老插件 API
def slack = SlackClient.instance
slack.send(channel: '#deploy', text: 'done')

// 新插件 API（v2.0）
slack(channel: '#deploy', message: 'done')
// 老 API 报 NoSuchMethodError
```

Shared Library 跑挂了，所有依赖它的 Pipeline 全挂。

#### 类型 C：依赖冲突

插件 A 依赖插件 X v3.0+，插件 B 依赖插件 X v2.x，**升级后 X 升到 v3.0，B 挂掉**。

### 防范方法

#### 升级前快照所有插件版本

```bash
curl -s "$JENKINS_URL/pluginManager/api/json?depth=1" \
    | jq '.plugins[] | "\(.shortName):\(.version)"' \
    > plugins-before.txt
```

升级失败时这个文件是回滚依据。

#### 分批升级

不要一次升所有插件——升关键 5-10 个，跑 1-2 天，再升下一批。

#### 关键插件单独审视

每个插件的 release notes 在升级前必读：

- workflow-cps（Pipeline 引擎）
- workflow-aggregator
- 任何 Shared Library 用到的插件

---

## 失败模式 3：配置迁移

Jenkins 的配置存在 `JENKINS_HOME` 下的 XML 文件里。Jenkins 大版本升级时，这些 XML 会被**自动迁移**——看起来无感知，但会出问题。

### 迁移内容

- 全局配置（`config.xml`）
- 节点配置（`nodes/<name>/config.xml`）
- 用户配置（`users/<name>/config.xml`）
- Job 配置（`jobs/<name>/config.xml`）

### 典型问题

#### 问题 1：废弃字段被删

老版本有 `enableSomething: true`，新版本这个字段被废弃 → 升级后 Jenkins 默默忽略，行为变成默认值。

#### 问题 2：默认值变化

某个全局开关老版本默认 false，新版本改成 true → 升级后行为悄悄变了，你才知道。

#### 问题 3：XML schema 不向后兼容

老版本能正常解析的 XML，新版本认为格式错误。Job 加载失败，Job history 看起来"消失了"——其实是没解析成功。

### 防范方法

#### 升级前完整备份 JENKINS_HOME

```bash
tar czf jenkins-home-before-upgrade.tar.gz \
    --exclude=workspace \
    --exclude='*/builds/*/archive' \
    $JENKINS_HOME
```

不带 workspace 和 archive，只备份配置 + history 元数据，几 GB 就够。

#### 升级后立刻检查

- 系统管理 → 系统信息（看 plugin 状态有没有 broken）
- 系统管理 → 节点管理（看 Agent 是不是都在）
- 抽查 5-10 个核心 Job，看 config 是不是和升级前一致
- 跑一条端到端流水线（test job），看 Pipeline 引擎工作

---

## 失败模式 4：Pipeline 语法 / API 变化

游戏团队投入最大的代码资产——Jenkinsfile 和 Shared Library——在 Jenkins 升级时可能突然不兼容。

### 真实场景

#### 场景 1：step 行为变化

某 step 在 plugin v1 行为是 A，v2 改成 B（不报错，但行为不同）。比如：

```groovy
sh script: 'build.sh', returnStdout: true
// v1: 返回 stdout（trim）
// v2: 返回 stdout（不 trim，包含末尾换行）
```

`returnStdout` 的 trim 行为变了 → 你的字符串比较挂了 → Pipeline 走错分支。**没有报错**，但产物里包错了。

#### 场景 2：环境变量行为变化

```groovy
environment {
    BUILD_NAME = "${env.JOB_NAME}-${env.BUILD_NUMBER}"
}
```

某次升级后 `env.JOB_NAME` 在 multibranch 下行为变了（包不包含 folder 路径）→ 命名规则变了 → 归档路径错位。

#### 场景 3：Groovy CPS 兼容性

CPS（Continuation Passing Style）的实现细节在升级时变化，老 Pipeline 在新版本可能 NotSerializableException。

### 防范方法

#### Sandbox 升级演练

最重要的一步。专门搞一个 sandbox Jenkins 实例：

1. 复制生产 Jenkins 的 JENKINS_HOME（去掉敏感 secrets）
2. 升级 sandbox 到目标版本
3. 跑所有核心 Pipeline 至少 1 次
4. 对照产物和 Pipeline 行为
5. 发现问题就在 sandbox 修，不影响生产

游戏团队**每次 Jenkins 升级前必须跑 sandbox 演练**——这是不可省的步骤。

#### Shared Library 测试

如果用了 JenkinsPipelineUnit 做单元测试（[详见 102]({{< relref "delivery-engineering/delivery-jenkins-ops-102-shared-library.md" >}})），升级前在 sandbox 跑测试。

---

## 回滚预案：30 分钟内回到升级前

### 回滚的难度排序

由难到易：

1. **JVM 回滚最难**——需要 OS 包管理介入，可能有依赖
2. **Jenkins core 回滚中等**——换回老 war 包
3. **插件回滚容易**——文件夹替换
4. **配置回滚容易**——从备份还原 JENKINS_HOME

### 完整回滚 Checklist

```bash
# 1. 停 Jenkins
sudo systemctl stop jenkins

# 2. 还原 JENKINS_HOME
sudo tar xzf /backup/jenkins-home-before-upgrade.tar.gz -C /var/

# 3. 换回老 war 包
sudo cp /backup/jenkins-old.war /usr/share/jenkins/jenkins.war

# 4. （如有需要）切回老 JVM
sudo update-alternatives --set java /opt/openjdk-11/bin/java

# 5. 启 Jenkins
sudo systemctl start jenkins

# 6. 检查
curl http://localhost:8080/login  # 看是否启动成功
```

**目标：30 分钟以内做完**——超过 30 分钟说明回滚预案不够熟练。

### 何时回滚

升级后立刻检查的关键信号：

- Jenkins 启动失败（startup log 报错）
- 系统信息页面显示 plugin broken（即使是单个）
- 关键 Pipeline 跑挂（哪怕只 1 条）
- Shared Library 加载失败

任何一条 → 立刻回滚，不要"先看看能不能修"。修生产是高风险动作，先回滚到稳定，再在 sandbox 慢慢修。

---

## 升级演练流程

完整的升级流程（4 周）：

### 第 1 周：调研

- 读 Jenkins core 和所有插件的 release notes
- 列出"已知不兼容"清单
- 决定升级版本（不跨太多 LTS 版本）

### 第 2 周：Sandbox 演练

- 搭 sandbox 实例
- 在 sandbox 升级
- 跑所有核心 Pipeline，对比产物
- 修复 sandbox 中发现的问题

### 第 3 周：低风险流水线灰度

- 生产 Jenkins 升级
- **只让"非关键流水线"先用新版本** —— 内部工具流水线、文档构建之类
- 观察 1 周

### 第 4 周：全量切换

- 所有流水线切到新版本
- 全员通知 + 紧急联系人 on call

### 升级窗口选择

- **避免发版前 1-2 周**（业务方紧张）
- **避免周五 / 节假日前**（出问题救火困难）
- **推荐周二 / 周三早上**（救火窗口长）

---

## 文末导读

下一步进 205 Jenkins 自身的可观测性：监控与告警——升级踩坑预防的最后一环是"早发现"。

L3 面试官线读者：本篇核心是"四层失败模式 + 30 分钟回滚预案"——升级是高风险变更，工程化体现在"演练 + 预案"而不是"升级一次成功率"。
