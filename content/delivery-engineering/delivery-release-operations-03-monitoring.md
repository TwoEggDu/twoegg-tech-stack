---
title: "灰度上线与线上运营 03｜线上监控——Crash 率、ANR、卡顿、加载与留存"
slug: "delivery-release-operations-03-monitoring"
date: "2026-04-14"
description: "版本健康看板设计、核心指标定义与目标值、三类告警规则、告警疲劳防治策略、发布后监控窗口规范。"
tags:
  - "Delivery Engineering"
  - "Monitoring"
  - "Crash Rate"
  - "Observability"
series: "灰度上线与线上运营"
primary_series: "delivery-release-operations"
series_role: "article"
series_order: 30
weight: 1630
delivery_layer: "principle"
delivery_volume: "V17"
delivery_reading_lines:
  - "L1"
  - "L2"
  - "L4"
---

## 这篇解决什么问题

灰度放量了、全量上线了——怎么知道版本是健康的？靠用户反馈太慢，靠直觉更不靠谱。这篇建立一套版本健康监控体系：看哪些指标、目标值是多少、告警规则怎么设、告警太多怎么治、发布后监控多久才算安全。

## 版本健康核心指标

| 指标 | 定义 | 目标值 | 采集来源 |
|------|------|--------|---------|
| Crash-free rate | 无崩溃用户占总活跃用户比例 | ≥ 99.9% | Crashlytics / Bugly / Sentry |
| ANR 率 | 应用无响应事件数 / 总会话数 | < 0.05% | Android Vitals / 自研采集 |
| 帧率分布 | 低于目标帧率（30fps/60fps）的帧占比 | P95 ≥ 目标帧率 | 自研性能采集 SDK |
| 加载时间 P50 | 主场景加载耗时中位数 | 按业务定义（如 < 5s） | 自研性能采集 SDK |
| 加载时间 P95 | 主场景加载耗时 95 分位 | 按业务定义（如 < 12s） | 自研性能采集 SDK |
| 内存峰值 | 运行期间最大内存占用 | 不超过设备物理内存 80% | 自研性能采集 SDK |
| 首日留存 | 新版本用户次日回访率 | 不低于前一版本 | 数据分析平台 |
| 资源下载成功率 | 热更新 / 补丁资源下载完成率 | ≥ 99% | CDN + 客户端上报 |

**重点**：指标的目标值不是拍脑袋定的——应该基于历史版本基线。如果过去五个版本的 Crash-free rate 平均 99.85%，那目标值设 99.9% 是合理的进步方向，设 99.99% 是不切实际的。

## 版本健康看板设计

一个有效的版本健康看板应该让任何人在 10 秒内判断"这个版本是否健康"。

**看板分层**：

| 层级 | 展示内容 | 目标用户 |
|------|---------|---------|
| 概览层 | 红黄绿灯 + 核心指标当前值 vs 基线 | 所有人（10 秒判断） |
| 趋势层 | 各指标的时间曲线（小时/天维度） | 版本负责人（1 分钟定位趋势） |
| 明细层 | Top Crash 列表、设备分布、地域分布 | 开发/QA（深入排查） |

**版本对比**：看板上必须能同时显示当前版本和前一版本的同期数据——不看对比，单独的数字没有意义。

## 三类告警规则

### 1. 阈值告警（绝对值）

当指标超过预设阈值时触发：

| 指标 | 告警阈值 | 严重程度 |
|------|---------|---------|
| Crash-free rate | < 99.5% | Critical |
| Crash-free rate | < 99.8% | Warning |
| ANR 率 | > 0.5% | Critical |
| ANR 率 | > 0.2% | Warning |
| 启动失败率 | > 1.0% | Critical |
| 资源下载失败率 | > 3.0% | Warning |

### 2. 趋势告警（版本对比）

当指标相比前一版本同期出现显著劣化时触发：

- Crash-free rate 低于前一版本同期 0.3 个百分点以上 → Warning
- 加载时间 P50 比前一版本同期慢 20% 以上 → Warning
- 首日留存低于前一版本 2 个百分点以上 → Critical

### 3. 异常告警（突增检测）

当指标在短时间内出现异常波动时触发：

- 任意 1 小时内 Crash 数量是前 24 小时同时段平均值的 3 倍以上 → Critical
- 某个特定 Crash 签名在 30 分钟内新增 100+ 次 → Warning
- 资源下载失败率在 15 分钟内从 < 1% 跳到 > 5% → Critical（可能是 CDN 故障）

### Prometheus 风格的告警规则示例

以下是实际使用的告警规则配置示例，供参考：

```yaml
# 阈值告警：Crash-free rate 低于 99.5%
- alert: CrashFreeRateCritical
  expr: |
    1 - (
      sum(rate(app_crash_total{version="current"}[1h]))
      /
      sum(rate(app_session_total{version="current"}[1h]))
    ) < 0.995
  for: 10m
  labels:
    severity: critical
  annotations:
    summary: "Crash-free rate 低于 99.5%"
    description: "当前版本 Crash-free rate {{ $value | humanizePercentage }}，持续 10 分钟"
    runbook: "https://wiki/runbook/crash-rate-critical"

# 趋势告警：Crash 率比前一版本同期高 0.3 个百分点
- alert: CrashRateRegression
  expr: |
    (
      sum(rate(app_crash_total{version="current"}[1h]))
      /
      sum(rate(app_session_total{version="current"}[1h]))
    )
    -
    (
      sum(rate(app_crash_total{version="previous"}[1h]))
      /
      sum(rate(app_session_total{version="previous"}[1h]))
    ) > 0.003
  for: 30m
  labels:
    severity: warning
  annotations:
    summary: "Crash 率劣化超过 0.3 个百分点"

# 突增告警：Crash 数量是过去同时段的 3 倍
- alert: CrashSpike
  expr: |
    sum(rate(app_crash_total{version="current"}[30m]))
    >
    3 * avg_over_time(
      sum(rate(app_crash_total{version="current"}[30m]))[24h:30m]
    )
  for: 5m
  labels:
    severity: critical
  annotations:
    summary: "Crash 数量突增（是过去 24h 同时段均值的 3 倍以上）"

# 资源下载失败率突增
- alert: ResourceDownloadFailure
  expr: |
    1 - (
      sum(rate(resource_download_success_total[15m]))
      /
      sum(rate(resource_download_total[15m]))
    ) > 0.05
  for: 5m
  labels:
    severity: critical
  annotations:
    summary: "资源下载失败率超过 5%，可能 CDN 故障"
```

**关于 `for` 持续时间的经验**：

- Critical 告警的 `for` 设 5-10 分钟，不要设太短（瞬时波动触发误报）也不要设太长（延迟响应）
- Warning 告警的 `for` 设 15-30 分钟，给足观察窗口
- 某次我们把 Crash 率的 `for` 从 15 分钟改到了 5 分钟——因为复盘发现一次事故中，告警延迟了 15 分钟才触发，而那 15 分钟里已经有数千用户受到影响

## 告警严重程度与升级机制

| 级别 | 含义 | 响应要求 | 通知方式 |
|------|------|---------|---------|
| Info | 指标波动但在正常范围内 | 记录，无需立即处理 | 看板标记 |
| Warning | 指标接近阈值或出现轻微劣化 | 15 分钟内有人确认 | IM 群通知 |
| Critical | 指标超标，版本可能存在严重问题 | 5 分钟内有人响应 | IM 群 + 电话通知 On-call |
| Page | 大面积用户受影响 | 立即拉起应急响应 | 电话 + 短信 + 升级到管理层 |

**升级规则**：Warning 超过 30 分钟无人确认 → 自动升级为 Critical。Critical 超过 15 分钟无人响应 → 自动 Page。

## 告警疲劳防治

告警太多 = 没有告警。当团队每天收到 50+ 条告警时，真正重要的告警会被淹没。

**一个真实的告警疲劳案例**：某个项目在上线初期配置了 35 条告警规则，每天平均触发 60+ 条告警。两个月后出了一次严重的数据库连接池耗尽事故，事后复盘发现——告警确实触发了（P1 级别），但 On-call 的企业微信群里每天有 60 多条告警，这条 P1 被淹没在了一堆 P3 告警里，没人注意到。

更讽刺的是，那 60 条告警中有超过 40 条是同一类问题（日志磁盘使用率反复在阈值附近波动），但没有配置聚合规则，每次波动都触发一条新告警。

这次事故后我们做了三件事：
1. 把告警规则从 35 条砍到 15 条——删掉了所有"看了也不知道该干什么"的告警
2. 给所有告警加了聚合窗口——同一规则 1 小时内最多触发 1 次
3. Critical 级别告警单独走电话通知，和 IM 频道完全分开

效果：每天告警数从 60+ 降到了 8-12 条，Critical 告警的平均响应时间从 25 分钟降到了 4 分钟。

**防治策略**：

| 策略 | 做法 |
|------|------|
| 调准阈值 | 每个版本周期复盘告警记录，把误报率 > 50% 的规则调宽 |
| 抑制已知问题 | 已确认且在修复中的问题，临时抑制其告警（设过期时间） |
| 聚合同类 | 同一 Crash 签名 1 小时内只告警一次，附带计数 |
| 分级路由 | Info/Warning 只发看板和异步频道，Critical 以上才电话 |
| 定期审计 | 每月检查：哪些告警从未被 actionable？考虑删除或降级 |

### 告警规则的"黄金比例"

经验上来看，一个健康的告警体系应该满足以下比例：

- **每日告警总数 < 20 条**（超过 20 条人脑就开始选择性忽视）
- **Critical 占比 < 5%**（如果经常有 Critical 要么是系统真的不稳定，要么是阈值设得太严）
- **告警被 actionable 的比例 > 80%**（收到告警后确实需要做点什么的比例）
- **误报率 < 10%**（超过 10% 团队就开始不信任告警系统）

一个简单的判断方法：如果 On-call 值班人员在看到告警通知后的第一反应是"又来了，不用管"，那这条告警规则需要被删掉或重新配置。

## 发布后监控窗口

| 阶段 | 时长 | 监控密度 | 说明 |
|------|------|---------|------|
| 密集监控 | 发布后 0~48h | 每小时检查看板 | 灰度种子期 + 验证期，On-call 必须在线 |
| 标准监控 | 发布后 48h~7d | 每日检查看板 | 扩大期 + 全量初期，关注留存和趋势 |
| 常规监控 | 7d 后 | 靠告警驱动 | 版本已稳定，只关注异常告警 |

**交接要求**：密集监控期间 On-call 人员不能出差、不能休假。如果灰度跨周末，On-call 排班必须覆盖周末。

## 监控体系检查清单

- [ ] 核心指标的采集 SDK 已集成且数据上报正常
- [ ] 版本健康看板已搭建，包含概览层 + 趋势层 + 明细层
- [ ] 告警规则已配置，覆盖阈值、趋势、异常三类
- [ ] 告警升级机制已与 On-call 系统打通
- [ ] 告警抑制规则已配置（已知问题不重复告警）
- [ ] 版本对比基线已设置（前一版本同期数据）
- [ ] On-call 轮转表已排好，覆盖发布后 7 天

---

**上一篇**：[回滚与应急]({{< relref "delivery-engineering/delivery-release-operations-02-rollback.md" >}}) — 发现问题后的三种应急响应手段

**下一步应读**：[热修复流程]({{< relref "delivery-engineering/delivery-release-operations-04-hotfix.md" >}}) — 紧急修复怎么走加速验证通道
