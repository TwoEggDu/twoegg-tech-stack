---
title: "性能与稳定性工程 07｜Crash 治理——上报、符号化、IL2CPP 与平台差异"
slug: "delivery-performance-stability-07-crash"
date: "2026-04-14"
description: "Crash 是用户体验的底线。这篇讲 Crash 上报基础设施、符号化还原、IL2CPP 崩溃的特殊处理、平台差异和 Crash 率收敛流程。"
tags:
  - "Delivery Engineering"
  - "Stability"
  - "Crash"
  - "IL2CPP"
series: "性能与稳定性工程"
primary_series: "delivery-performance-stability"
series_role: "article"
series_order: 70
weight: 1370
delivery_layer: "principle"
delivery_volume: "V14"
delivery_reading_lines:
  - "L1"
  - "L2"
---

## 这篇解决什么问题

帧率低是体验差，内存高是有风险，Crash 是直接出局——用户看到的是"闪退"，然后可能就卸载了。

Crash 治理不是"出了 Crash 就修"——需要完整的基础设施：上报怎么做、崩溃堆栈怎么还原成可读代码、IL2CPP 生成的原生崩溃怎么查、不同平台的崩溃模式有什么差异。

## Crash 上报基础设施

### 主流方案

| 方案 | 平台支持 | 说明 |
|------|---------|------|
| Firebase Crashlytics | iOS, Android | Google 的免费方案，集成简单 |
| Bugly | iOS, Android | 腾讯方案，国内用户多，支持微信小游戏 |
| Sentry | 全平台 | 开源方案，可自建 |
| Unity Cloud Diagnostics | Unity 内置 | 简单但功能有限 |
| 自建 | 全平台 | 完全可控但维护成本高 |

### 上报内容

一次 Crash 上报应该包含：

| 字段 | 说明 |
|------|------|
| 崩溃堆栈 | 原始的 Native Stack Trace |
| 设备信息 | 型号、OS 版本、内存、GPU |
| App 版本 | 首包版本 + 热更版本 |
| 场景上下文 | 崩溃时在哪个场景、做什么操作 |
| 内存快照 | 崩溃时的内存使用情况 |
| 用户日志 | 崩溃前 N 秒的关键日志 |
| 复现路径 | 用户操作序列（如果有埋点） |

### 上报时机

| 时机 | 说明 |
|------|------|
| 下次启动时上报 | Crash 导致进程终止，无法在崩溃时上报 |
| 后台线程上报 | 不阻塞启动流程 |
| 失败重试 | 网络不可用时缓存，下次有网时上报 |

## 符号化（Symbolication）

### 为什么需要符号化

原始崩溃堆栈是内存地址，不是代码行号：

```
0   libil2cpp.so  0x006a3f20
1   libil2cpp.so  0x006a2e80
2   libil2cpp.so  0x00523c40
3   libunity.so   0x001a4f60
```

符号化把地址还原成可读的函数名和行号：

```
0   GameManager.OnBattleEnd() at GameManager.cs:142
1   BattleSystem.Finish() at BattleSystem.cs:87
2   EventDispatcher.Dispatch() at EventDispatcher.cs:23
3   UnityEngine.PlayerLoop.Update()
```

### 符号文件

| 平台 | 符号文件 | 生成方式 |
|------|---------|---------|
| iOS | dSYM 文件 | Xcode 构建时自动生成 |
| Android | mapping.txt（Java）+ symbols.zip（Native） | Gradle 构建时生成 |
| IL2CPP | LineNumberMappings.json + .sym 文件 | IL2CPP 构建时生成 |

### 符号文件管理

**关键原则**：每次构建的符号文件必须归档，并和构建版本号绑定。

| 要求 | 说明 |
|------|------|
| 自动归档 | CI 构建完成后自动上传符号文件到归档服务 |
| 版本关联 | 符号文件与 App 版本号一一对应 |
| 保留策略 | 至少保留最近 6 个月的符号文件 |
| 自动上传到 Crash 平台 | 构建后自动把符号文件上传到 Crashlytics/Bugly |

**常见事故**：符号文件丢失——线上 Crash 无法符号化，只能看到内存地址，无法定位问题。预防方法：CI 中校验符号文件是否成功归档。

## IL2CPP 崩溃的特殊处理

Unity 移动端使用 IL2CPP 把 C# 编译成 C++，再编译成原生代码。IL2CPP 崩溃的堆栈是原生堆栈，定位难度更高。

### IL2CPP 崩溃的特点

| 特点 | 说明 |
|------|------|
| 堆栈是 C++ 函数名 | 看到的是 `GameManager_OnBattleEnd_m12345` 而非 `GameManager.OnBattleEnd()` |
| 需要 IL2CPP 符号 | 标准 dSYM 不够，还需要 IL2CPP 生成的映射文件 |
| 崩溃可能在生成代码中 | 不是你写的代码的 Bug，而是 IL2CPP 代码生成的问题 |
| 空引用表现不同 | C# 的 NullReferenceException 变成原生的 SIGSEGV |

### IL2CPP 崩溃排查流程

```
1. 获取原生崩溃堆栈
   ↓
2. 用 IL2CPP 符号文件符号化
   ↓
3. 还原出 C# 方法名和大致行号
   ↓
4. 判断崩溃类型：
   - SIGSEGV at null → 空引用
   - SIGABRT → 断言失败或内存损坏
   - EXC_BAD_ACCESS → 野指针
   ↓
5. 结合上下文日志定位根因
```

### HybridCLR 热更代码的崩溃

热更新代码运行在 HybridCLR 解释器中，崩溃表现又不同：

| 场景 | 崩溃表现 |
|------|---------|
| 热更代码空引用 | 解释器抛出 NullReferenceException（托管异常，不是原生崩溃） |
| 热更代码调用不存在的 AOT 方法 | ExecutionEngineException |
| 解释器本身的 Bug | 原生崩溃在 HybridCLR 的 C++ 代码中 |

## 平台特定的崩溃模式

### iOS

| 崩溃类型 | 说明 | 识别方式 |
|---------|------|---------|
| Watchdog Kill | 启动超时（App 在 20 秒内未完成启动） | 异常代码 `0x8badf00d` |
| Jetsam Kill | 内存超限被系统杀掉 | 异常代码 `EXC_RESOURCE` |
| 主线程卡死 | 主线程阻塞超时 | 异常代码 `0x8badf00d`（同 Watchdog） |
| Metal 错误 | GPU 命令缓冲区异常 | `MTLCommandBuffer` 错误日志 |

### Android

| 崩溃类型 | 说明 | 识别方式 |
|---------|------|---------|
| ANR | 主线程阻塞超过 5 秒 | ANR trace 文件 |
| Native Crash | 原生代码崩溃（SIGSEGV, SIGABRT） | Tombstone 文件 |
| OOM | 内存不足被 LMK 杀掉 | 通常没有崩溃堆栈，只有 logcat 中的 LMK 日志 |
| JNI 错误 | Java/Native 接口调用错误 | `JNI DETECTED ERROR` 日志 |

### WebGL

| 崩溃类型 | 说明 | 识别方式 |
|---------|------|---------|
| OOM | 浏览器内存限制 | `Out of memory` 错误 |
| WebGL 上下文丢失 | GPU 资源被回收 | `WebGL context lost` 事件 |
| 异步编译超时 | Shader 编译阻塞主线程 | 白屏 + 浏览器卡死 |

## Crash 率作为版本健康指标

### 指标定义

| 指标 | 计算方式 | 说明 |
|------|---------|------|
| Crash-free Rate | (1 - 崩溃用户数/总活跃用户数) * 100% | 核心指标 |
| Crash 率 | 崩溃次数/总会话数 * 100% | 辅助指标 |
| ANR 率 | ANR 次数/总会话数 * 100% | Android 特有 |

### 发布阈值

| 阈值 | 决策 |
|------|------|
| Crash-free Rate ≥ 99.9% | 正常发布 |
| 99.5% ≤ Crash-free Rate < 99.9% | 评估后决定 |
| Crash-free Rate < 99.5% | 暂停发布，定位并修复 |

### 灰度阶段的 Crash 监控

```
灰度 1%（观察 2 小时）
  → Crash-free Rate ≥ 99.9% → 扩量到 10%
  → Crash-free Rate < 99.5% → 回滚
     ↓
灰度 10%（观察 4 小时）
  → Crash-free Rate ≥ 99.9% → 扩量到 50%
  → Crash-free Rate < 99.5% → 回滚
     ↓
灰度 50% → 全量
```

## Crash 分诊与收敛流程

```
1. Crash 聚类（按堆栈指纹自动分组）
   ↓
2. 按影响用户数排序 → Top 10 优先处理
   ↓
3. 分配责任人（按模块归属）
   ↓
4. 排查 → 修复 → 提交
   ↓
5. 验证（修复后 Crash 率是否下降）
   ↓
6. 发布修复（热更或版本更新）
```

**关键实践**：

- 每日 Crash 看板——Top 10 崩溃问题的趋势
- 新增崩溃告警——新出现的崩溃集群立即通知
- Crash 率周报——作为版本质量的关键指标

## V14 系列总结

七篇文章覆盖了性能与稳定性工程的完整体系：

| 篇 | 主题 | 核心输出 |
|----|------|---------|
| 01 | 工程循环 | Budget → Measure → Govern → Verify |
| 02 | 预算体系 | 三维预算矩阵（系统 x 场景 x 设备） |
| 03 | 设备分档 | 能力指纹 + 质量配置 + 动态降级 |
| 04 | CPU 治理 | GC 预算 + Update 调度 + 物理优化 |
| 05 | GPU 治理 | Draw Call + 带宽 + Shader 变体 |
| 06 | 内存治理 | 内存预算 + OOM 防护 + 资源瘦身 |
| 07 | Crash 治理 | 上报 + 符号化 + 平台差异 + 收敛流程 |

## 小结与检查清单

- [ ] 是否集成了 Crash 上报 SDK（Crashlytics / Bugly / Sentry）
- [ ] 上报内容是否包含设备信息、场景上下文和用户日志
- [ ] 每次构建的符号文件是否自动归档并上传到 Crash 平台
- [ ] 是否了解 IL2CPP 崩溃的特殊性（原生堆栈、需要 IL2CPP 符号）
- [ ] 是否了解各平台特定的崩溃模式（iOS Watchdog/Jetsam、Android ANR）
- [ ] 是否有 Crash-free Rate 的发布阈值（≥99.9% 才发布）
- [ ] 灰度阶段是否有实时 Crash 监控和自动回滚机制
- [ ] 是否有 Crash 分诊流程（聚类 → 排序 → 分配 → 修复 → 验证）
- [ ] 是否有每日 Crash 看板和新增崩溃告警

---

V14 性能与稳定性工程到这里结束。

七篇文章覆盖了：工程循环（01）、预算体系（02）、设备分档（03）、CPU 治理（04）、GPU 治理（05）、内存治理（06）和 Crash 治理（07）。

**推荐下一步**：V15 数据与分析 — 从性能度量延伸到更广泛的数据体系：埋点、BI、AB 测试

**扩展阅读**：性能工程专栏（110 篇）— CPU/GPU/内存/渲染/预算/设备分档的完整技术深挖
