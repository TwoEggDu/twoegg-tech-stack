---
title: "案例与模板 03｜模板集——CI 管线、质量门配置、发布报告、事故复盘"
slug: "delivery-cases-templates-03-templates"
date: "2026-04-14"
description: "四套可直接复用的模板：CI 管线 Stage 定义、质量门配置、版本发布报告、事故复盘 Postmortem。每套模板附字段说明和对应 Volume 来源。"
tags:
  - "Delivery Engineering"
  - "Template"
  - "CI/CD"
  - "Quality Gate"
  - "Postmortem"
series: "案例与模板"
primary_series: "delivery-cases-templates"
series_role: "article"
series_order: 30
weight: 1830
delivery_layer: "case"
delivery_volume: "V19"
delivery_reading_lines:
  - "L1"
  - "L3"
  - "L5"
---

## 这篇解决什么问题

"这个流程我理解了，但落地时该写什么格式？"——这是读完技术文章后最常见的问题。本篇提供四套标准模板的骨架和字段说明。模板不是教条——根据项目实际情况裁剪和扩展，但有一个起点比从零开始好得多。

## 使用说明

- 每套模板给出关键字段、字段说明、填写示例
- 字段分为"必填"和"选填"——必填字段构成模板的最小完整集
- 每套模板标注了对应的 Volume，需要理解字段含义时回溯

---

## 模板一：CI 管线 Stage 定义

定义 CI/CD 管线的每个 Stage：执行什么、成功条件、失败处理。

### 模板字段

| 字段 | 必填 | 说明 |
|------|------|------|
| stage_name | 必填 | Stage 名称，如 `compile`、`unit_test`、`build` |
| stage_order | 必填 | 执行顺序编号 |
| trigger | 必填 | 触发条件（push、merge、manual、schedule） |
| executor | 必填 | 执行环境（Docker 镜像名称或构建机标识） |
| commands | 必填 | 执行的命令列表 |
| success_condition | 必填 | 判断成功的条件（exit code = 0、测试通过率 100%） |
| failure_action | 必填 | 失败时的处理（block、warn、notify） |
| timeout | 必填 | 超时时间（超时 = 失败） |
| artifacts | 选填 | 产出的制品（构建包、测试报告、日志） |
| notification | 选填 | 通知规则（成功/失败分别通知谁） |
| retry | 选填 | 自动重试策略（次数、间隔） |

### 填写示例

```yaml
stages:
  - stage_name: "compile"
    stage_order: 1
    trigger: "push"
    executor: "unity-2022.3-il2cpp:latest"
    commands:
      - "unity-cli -buildTarget Android -executeMethod Build.Compile"
    success_condition: "exit_code == 0"
    failure_action: "block"
    timeout: "30m"
    artifacts:
      - "build/compile_log.txt"
    notification:
      on_failure: ["@toolchain-team"]

  - stage_name: "unit_test"
    stage_order: 2
    trigger: "after:compile"
    executor: "unity-2022.3-il2cpp:latest"
    commands:
      - "unity-cli -runTests -testPlatform EditMode"
      - "unity-cli -runTests -testPlatform PlayMode"
    success_condition: "test_pass_rate == 100%"
    failure_action: "block"
    timeout: "20m"
    artifacts:
      - "build/test_results.xml"
    notification:
      on_failure: ["@dev-team"]

  - stage_name: "quality_gate"
    stage_order: 3
    trigger: "after:unit_test"
    executor: "quality-checker:latest"
    commands:
      - "check-bundle-size --max 1.2GB"
      - "check-shader-variants --max 512"
      - "check-texture-size --max 2048"
    success_condition: "all_checks_passed"
    failure_action: "block"
    timeout: "10m"
    notification:
      on_failure: ["@ta-team", "@dev-team"]

  - stage_name: "approval"
    stage_order: 4
    trigger: "after:quality_gate"
    executor: "manual"
    commands: []
    success_condition: "dual_approval"
    failure_action: "block"
    timeout: "48h"
    notification:
      on_pending: ["@release-manager", "@tech-lead"]
```

**来源 Volume**：V16 CI/CD 管线系列

---

## 模板二：质量门配置

定义每个质量检查项的名称、阈值、严重级别和负责人。

### 模板字段

| 字段 | 必填 | 说明 |
|------|------|------|
| gate_name | 必填 | 检查项名称 |
| category | 必填 | 分类（functional / performance / stability / resource / security） |
| metric | 必填 | 检查的指标名称 |
| operator | 必填 | 比较运算符（>=、<=、==、!=） |
| threshold | 必填 | 阈值 |
| severity | 必填 | 严重级别（block = 不通过则阻塞，warn = 不通过但继续） |
| owner | 必填 | 负责人角色（检查不通过时通知谁修复） |
| measurement | 选填 | 度量方式说明 |
| exception_rule | 选填 | 例外规则（什么情况下允许豁免） |

### 填写示例

```yaml
quality_gates:
  # --- 功能质量 ---
  - gate_name: "单元测试通过率"
    category: "functional"
    metric: "unit_test_pass_rate"
    operator: "=="
    threshold: "100%"
    severity: "block"
    owner: "Dev"
    measurement: "EditMode + PlayMode 测试总通过率"

  - gate_name: "功能测试通过率"
    category: "functional"
    metric: "functional_test_pass_rate"
    operator: ">="
    threshold: "100%"
    severity: "block"
    owner: "QA"

  # --- 性能质量 ---
  - gate_name: "包体大小"
    category: "performance"
    metric: "apk_size_mb"
    operator: "<="
    threshold: "1200"
    severity: "block"
    owner: "TA"
    exception_rule: "大版本更新允许 Release Mgr 审批后放宽 10%"

  - gate_name: "Shader Variant 数量"
    category: "performance"
    metric: "shader_variant_count"
    operator: "<="
    threshold: "512"
    severity: "warn"
    owner: "TA"

  # --- 稳定性 ---
  - gate_name: "Crash-free Rate"
    category: "stability"
    metric: "crash_free_rate"
    operator: ">="
    threshold: "99.5%"
    severity: "block"
    owner: "Dev"
    measurement: "基于灰度期间实际数据"

  # --- 资源规范 ---
  - gate_name: "贴图最大尺寸"
    category: "resource"
    metric: "max_texture_dimension"
    operator: "<="
    threshold: "2048"
    severity: "warn"
    owner: "TA"
    exception_rule: "UI Atlas 允许 4096，需 TA 审批"
```

**来源 Volume**：V13 验证与测试系列、V14 性能与稳定性系列

---

## 模板三：版本发布报告

版本全量上线后产出的发布报告，记录本版本的完整交付信息。

### 模板字段

| 字段 | 必填 | 说明 |
|------|------|------|
| version | 必填 | 版本号（语义版本 + 构建号） |
| release_date | 必填 | 发布日期 |
| release_type | 必填 | 版本类型（常规/功能/大版本/热修复） |
| platforms | 必填 | 发布平台列表 |
| change_summary | 必填 | 核心变更摘要（按模块分组） |
| quality_metrics | 必填 | 质量指标对比（本版本 vs 上一版本） |
| known_issues | 必填 | 已知问题及影响评估 |
| approval_record | 必填 | 审批记录（审批人、时间、结果） |
| canary_record | 选填 | 灰度记录（每阶段的指标和决策） |
| incident_summary | 选填 | 发布过程中的事故摘要 |
| rollback_status | 选填 | 回滚方案及验证状态 |

### 填写示例

```markdown
# 版本发布报告

## 基本信息
- 版本号：2.1.0 (Build 3847)
- 发布日期：2026-04-10
- 版本类型：功能版本
- 发布平台：Android, iOS

## 核心变更
### 新功能
- [战斗] 新增公会副本第三章
- [社交] 好友系统增加亲密度机制

### Bug 修复
- [UI] 修复商城页面在 iPad 上的布局异常 (#4521)
- [战斗] 修复特定技能组合导致的伤害计算错误 (#4498)

### 技术改进
- [性能] 优化战斗场景 Draw Call，中端设备帧率提升 12%
- [资源] AB 分组策略调整，首包缩小 45MB

## 质量指标

| 指标 | 上一版本 | 本版本 | 变化 |
|------|---------|--------|------|
| Crash-free Rate | 99.62% | 99.71% | +0.09% |
| ANR 率 | 0.32% | 0.28% | -0.04% |
| 平均帧率（中端） | 27.3fps | 28.1fps | +0.8fps |
| 包体大小（Android） | 1.08GB | 1.03GB | -45MB |

## 已知问题
- [低优先级] 特定 Android 12 设备偶现截图功能异常，
  影响范围 < 0.1%，计划下版本修复

## 审批记录
- 技术审批：张三（Tech Lead），2026-04-09 14:30，通过
- 流程审批：李四（Release Mgr），2026-04-09 15:00，通过
```

**来源 Volume**：V09 平台上架系列、V17 灰度上线系列、V18-02 发布审批

---

## 模板四：事故复盘 Postmortem

线上事故的标准复盘文档。

### 模板字段

| 字段 | 必填 | 说明 |
|------|------|------|
| incident_id | 必填 | 事故编号 |
| severity | 必填 | 严重级别（P0/P1/P2/P3） |
| title | 必填 | 一句话描述事故 |
| timeline | 必填 | 完整时间线（发现-响应-定位-修复-恢复） |
| impact | 必填 | 影响范围（用户数、持续时间、业务影响） |
| root_cause | 必填 | 根因分析（直接原因 + 深层原因） |
| fix_description | 必填 | 修复方案及执行过程 |
| prevention | 必填 | 预防措施（每条有 Owner + Deadline） |
| detection_gap | 选填 | 为什么没提前发现（测试/监控的盲区） |
| response_review | 选填 | 响应过程评估（哪里做得好、哪里可以更快） |
| lessons_learned | 选填 | 经验教训（超出本次事故的通用洞察） |

### 填写示例

```markdown
# 事故复盘：商城道具价格显示异常

## 基本信息
- 事故编号：INC-2026-0042
- 严重级别：P1
- 影响时间：2026-04-08 10:15 ~ 11:30（75 分钟）
- 影响范围：全部用户的商城功能不可用

## Timeline
| 时间 | 事件 |
|------|------|
| 10:15 | 监控告警：商城接口错误率从 0.1% 飙升到 45% |
| 10:18 | On-call 确认告警，开始排查 |
| 10:25 | 定位到服务端配置加载失败 |
| 10:35 | 确认根因：配置表价格字段格式错误 |
| 10:45 | 修复配置表，提交热加载 |
| 10:50 | 预发布环境验证通过 |
| 11:00 | 线上热加载修复后的配置 |
| 11:30 | 确认商城功能恢复，错误率回到正常水平 |

## 根因分析
- 直接原因：配置表道具价格列存在 "1,000" 格式（带千位分隔符），
  服务端解析为字符串而非整数
- 深层原因：配置验证管线缺少数值格式校验，
  只检查了"非空"和"范围"

## 预防措施

| # | 措施 | Owner | Deadline | 验收标准 |
|---|------|-------|----------|---------|
| 1 | 配置验证增加数值格式检查 | 王五 | 2026-04-15 | CI 中自动拦截非法格式 |
| 2 | Excel 模板锁定数值列格式 | 赵六 | 2026-04-12 | 模板中数值列不允许文本输入 |
| 3 | 配置变更增加冒烟测试 | 王五 | 2026-04-22 | 核心流程自动走一遍 |

## 经验教训
- 配置验证不能只检查"有没有值"，还要检查"值的格式对不对"
- 策划直接编辑 Excel 的场景需要更强的格式约束
```

**来源 Volume**：V15 缺陷闭环系列（尤其 V15-06 防复发）、V17-05 版本复盘

---

## 模板使用建议

| 原则 | 说明 |
|------|------|
| 从最小集开始 | 先用必填字段跑起来，选填字段根据需要逐步加入 |
| 模板放在仓库里 | 模板是代码的一部分，走版本管理，不是放在 Wiki 上自由编辑 |
| 定期迭代 | 每季度回顾模板——有没有总是空着的字段（考虑移除）？有没有反复想填但没有的字段（考虑添加）？ |
| 自动化预填 | CI 数据、测试报告数据应该自动填入模板，减少手动复制 |

## 检查清单

- [ ] 四套模板已在团队协作平台上创建
- [ ] 必填字段和选填字段已根据团队实际情况裁剪
- [ ] CI 管线模板已用于实际管线配置
- [ ] 质量门配置模板已用于实际质量检查
- [ ] Postmortem 模板已用于最近一次事故复盘

---

**下一步应读**：[成熟度评估与改进路线图]({{< relref "delivery-engineering/delivery-cases-templates-04-maturity-assessment.md" >}}) — 用评估矩阵量化团队水平，规划改进路径

**扩展阅读**：V16 CI/CD 管线系列 — CI Stage 定义的技术实现细节
