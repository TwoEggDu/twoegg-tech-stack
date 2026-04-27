---
date: "2026-04-27"
title: "多分支流水线：Dev / QA / Release 自动化策略"
description: 'Multibranch Pipeline 不是把每个分支映射成一条独立流水线，而是把分支策略映射成流水线行为。游戏团队的 Dev / QA / Release / Hotfix 各自需要不同的触发、构建、归档策略——以及最容易踩的"分支爆炸"陷阱。'
slug: "delivery-jenkins-ops-104-multi-branch"
weight: 1575
featured: false
tags:
  - "Delivery Engineering"
  - "CI/CD"
  - "Jenkins"
  - "Multibranch"
series: "游戏团队 Jenkins 实战"
series_id: "delivery-jenkins-ops"
series_role: "article"
series_order: 50
delivery_layer: "practice"
delivery_volume: "V16"
delivery_parent_series: "delivery-cicd-pipeline"
delivery_reading_lines:
  - "L1"
  - "L2"
---

## 在本篇你会读到

- **多分支不是"每分支一条流水线"** —— 是分支策略到流水线行为的映射
- **Multibranch Pipeline 的能力与边界** —— 自动发现 vs 手动配置的取舍
- **Folder + Multibranch 组合** —— 多产品 × 多分支的真实结构
- **分支类型映射** —— Dev / QA / Release / Hotfix 各自的策略差异
- **PR / MR 的特殊处理** —— 何时跑、跑什么、产物保留多久
- **分支爆炸与清理策略** —— 几百个 feature 分支会让 Jenkins 卡死

---

## 多分支不是"每分支一条流水线"

很多团队在阶段 1 时给每个分支手动配一条 Jenkins job——`projectA-dev-build`、`projectA-release-build`、`projectA-hotfix-build`。分支多了之后陷入维护地狱。

正确的认知是：**多分支策略不是"每分支一条流水线"，而是"一个流水线模板，根据分支类型表现出不同行为"**。

| 分支类型 | 触发方式 | 构建配置 | 产物归档策略 | 通知策略 |
|---------|---------|---------|-------------|---------|
| feature/* | 提交触发 | Debug | 保留 7 天 | 仅作者 |
| dev | 提交触发 | Debug | 保留 30 天 | 整个团队 |
| qa | 提交触发 | Release | 保留 60 天 | QA + 业务方 |
| release/* | 提交触发 + 人工确认 | Release + 签名 | 永久 | 发版群 |
| hotfix/* | 提交触发 | Release + 签名 | 永久 | 发版群 + 紧急 |

这张表是**流水线行为的真值表**——同一个 Jenkinsfile 模板根据 `env.BRANCH_NAME` 表现出不同行为。

---

## Multibranch Pipeline 的能力与边界

Jenkins Multibranch Pipeline（MBP）是这件事的官方工具：

- 配置一次（指向 Git 仓库）
- 自动发现仓库里所有匹配规则的分支
- 每个分支自动创建一个"子 Pipeline"
- 分支删除时自动清理对应的 Pipeline

### MBP 能撑住的事

- **自动发现新分支**——开发新建一个 feature 分支，几分钟后 Jenkins 自动出现对应的 Pipeline
- **自动清理老分支**——分支删除后 Jenkins 自动移除（可配置宽限期）
- **PR 集成**——和 GitHub / GitLab / Bitbucket 集成，能自动发现 PR 并跑流水线
- **每分支独立日志和产物**——查 history 时按分支组织

### MBP 的能力边界

#### 1. 分支扫描的延迟

MBP 默认每隔几分钟（或 webhook 触发）扫描一次。**新分支的第一次构建有几分钟延迟**——不是 push 即跑。游戏团队希望"提交后立刻有反馈"的话，要配 webhook 而不是依赖定时扫描。

#### 2. 分支配置不能完全独立

所有分支共用一个 Jenkinsfile（来自 Git）。如果某个分支需要完全不同的流水线（比如 release 分支要做特殊签名步骤），只能在 Jenkinsfile 里用 `when {}` / `if` 判断分支名——不能给某个分支单独换 Jenkinsfile。

#### 3. 历史构建数 vs 磁盘占用

每个分支默认保留多少次构建？这是 MBP 的关键容量参数。配置失误的真实事故：

> 某项目 200+ feature 分支 × 默认保留 50 次构建 × 每次 3 GB 产物 = 30 TB 磁盘占用，Jenkins 磁盘报警。

需要在 MBP 配置里**针对分支类型**设不同保留策略：

- feature/*：保留 5 次，最多 7 天
- dev：保留 30 次，最多 30 天
- release/*：永久保留

---

## Folder + Multibranch 组合：多产品 × 多分支

游戏团队多产品 + 多分支的笛卡尔积怎么组织？用 **Folder** 做产品维度，**Multibranch** 做分支维度：

```
Jenkins 顶层
├─ Folder: ProjectTopHero/
│   ├─ Multibranch: tophero-build/
│   │   ├─ branch: dev
│   │   ├─ branch: qa
│   │   └─ branch: release/v2.4
│   └─ Multibranch: tophero-tests/
├─ Folder: ProjectSGI/
│   └─ Multibranch: sgi-build/
└─ Folder: ProjectZuma/
    └─ Multibranch: zuma-build/
```

### 这种结构的好处

- **权限隔离**：可以给每个 Folder 独立的访问权限（QA 只能看 qa 分支、business 只能看 release 分支）
- **配额管理**：可以给每个 Folder 限制并发数（防止某产品占满 build farm）
- **视图独立**：每个产品的看板独立

### 这种结构的代价

- **Jenkins 侧的 UI 变深**——业务方点进去要点几层
- **Pipeline 间引用变长**——`build job: '../ProjectSGI/sgi-build/dev'` 这种相对路径
- **配置漂移风险**——三个 Folder 的配置不完全一致时容易出"为什么 SGI 行 TopHero 不行"

### 命名规范的重要性

`ProjectTopHero` vs `tophero` vs `TopHero` —— 这种命名混乱在 Jenkins 上 6 个月后会让你绝望（因为 path 是字符串）。**第一天就定命名规范**：

- Folder：PascalCase（`ProjectTopHero`）
- Multibranch / Job：kebab-case（`tophero-build`）
- 分支名：用项目 git 规范（不是 Jenkins 的事，但 MBP 路径会用）

---

## 分支类型映射：四类分支的策略差异

### feature/*：开发分支，最快反馈

```groovy
when {
    branch pattern: 'feature/.*', comparator: 'REGEXP'
}
steps {
    // Debug 构建，跳过资源烘焙以加速
    sh 'unity -batchmode -executeMethod Build.iOSDebug -skip-bake'
}
```

策略：
- **触发**：每次 push 都跑
- **构建**：Debug 配置，可以跳过部分耗时步骤（资源烘焙、shader 全量编译）
- **产物**：可下载用于本地测试，但 7 天后自动清理
- **通知**：仅 commit 作者

### dev：集成分支，全流程冒烟

```groovy
when { branch 'dev' }
steps {
    sh 'unity -batchmode -executeMethod Build.iOSDebug'
    sh 'run_smoke_tests.sh'  // dev 必跑冒烟
}
```

策略：
- **触发**：每次合并都跑
- **构建**：Debug + 冒烟测试
- **产物**：保留 30 天供测试组拉
- **通知**：研发群

### qa：测试分支，QA 入口

```groovy
when { branch 'qa' }
steps {
    sh 'unity -batchmode -executeMethod Build.iOSRelease'
    sh 'run_full_test_suite.sh'
    archiveArtifacts artifacts: '**/*.ipa, **/*.apk, **/*.dSYM.zip'
}
```

策略：
- **触发**：QA 主导，可以延迟（不必每提交跑）
- **构建**：Release 配置 + 完整测试
- **产物**：含符号表，保留 60 天
- **通知**：QA + 业务方

### release/*：发版分支，最严

```groovy
when { branch pattern: 'release/.*', comparator: 'REGEXP' }
steps {
    input message: '确认开始 Release 构建？'  // 人工确认
    sh 'unity -batchmode -executeMethod Build.iOSRelease'
    sh 'sign_with_production_cert.sh'
    archiveArtifacts artifacts: '**/*.ipa, **/*.apk, **/*.dSYM.zip'
}
```

策略：
- **触发**：每次提交触发，但**需要人工确认**才执行（防止误触发）
- **构建**：Release 配置 + 生产签名
- **产物**：永久保留 + 异地备份
- **通知**：发版群

### hotfix/*：紧急修复，发版的"快速通道"

策略和 release 类似，但跳过部分慢测试（保留冒烟和回归），通知里加紧急标记。

---

## PR / MR 的特殊处理

PR（Pull Request）/ MR（Merge Request）是 feature → dev 之间的中间状态。MBP 能自动发现 PR 并跑流水线，但需要专门策略：

### PR 应该跑什么

- **必须跑**：编译 + 静态检查 + 单元测试
- **可以跑**：冒烟测试
- **不该跑**：完整 QA 测试套（太慢，PR 阶段不值得）、Release 构建（不该有 release 工件出现在 PR 上）

### PR 产物保留策略

- **保留 7 天足够**——PR 合并或关闭后产物没价值
- **不要永久保留**——一个项目可能有几百 PR，永久保留 = 磁盘地狱

### PR 中的并发策略

同一 PR 多次 push 会触发多次构建。配置"取消上一次进行中的构建"：

```groovy
options {
    disableConcurrentBuilds(abortPrevious: true)
}
```

否则 build farm 会被同一个 PR 的多次 push 占满（开发者快速 push 修正小问题时）。

---

## 分支爆炸与清理策略

最常见的多分支事故：**feature 分支无序滋生 → Jenkins 卡死**。

### 真实事故

某项目用了 18 个月，开发者养成"每个 task 开一个 feature 分支"习惯，**累积了 800+ 个 feature 分支**。即使每个分支只保留最近 5 次构建 × 100 MB，也是 400 GB。Jenkins Master 扫描分支耗时从秒级涨到分钟级（每次扫描要拉所有分支的 Jenkinsfile），最终触发 Master 内存溢出。

### 防爆炸的三层策略

#### 第一层：MBP 配置自动清理

```
Pipeline → Configure → Branch Sources → Behaviors:
- Discard old items: keep 5 builds for feature/*, 30 for dev, ∞ for release/*
- Orphaned Item Strategy: delete after 7 days from branch removal
```

#### 第二层：feature 分支生命周期纪律

git 侧约定：

- feature 分支必须从 dev 分出
- 合并到 dev 后**立刻删除**（不要留"备份"）
- 超过 30 天未合并的 feature 分支由工程负责人定期清理

#### 第三层：MBP 的"白名单 + 黑名单"

如果实在控制不住分支数量，可以在 MBP 里配过滤规则：

```
Branch Sources → Filter by name (with regular expression):
- Include: ^(dev|qa|release/.*|hotfix/.*|feature/[A-Z]+-\d+.*)$
- Exclude: ^(feature/temp.*|feature/test.*)$
```

只让"符合命名规范的分支"进 Jenkins，临时分支不入库。

### 配额监控

定期检查（每周）：

- Jenkins 总分支数
- 每个 Multibranch 的分支数 Top 10
- 占磁盘最大的分支 Top 10

数据进监控后能提前预警，详见 205 Jenkins 自身的可观测性。

---

## 文末导读

下一步进 105 Jenkins 下的并行模式：Parallel / Matrix / Triggers 实战——多分支流水线内部，stages 之间的并行机制。

L3 面试官线读者：本篇核心是"分支策略真值表"那一节——多分支不是技术问题，是治理问题。流水线行为的差异化是治理意图的工程化体现。
