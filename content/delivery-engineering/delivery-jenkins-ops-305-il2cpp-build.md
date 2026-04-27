---
date: "2026-04-27"
title: "IL2CPP 构建的时间与内存特征"
description: 'IL2CPP 构建机是游戏团队最容易挂的机器——内存峰值高、构建时长长、磁盘 I/O 密集。本篇拆解 IL2CPP 各阶段的资源特征，给出 Agent 配置基线和五类故障的诊断路径。系列收尾。'
slug: "delivery-jenkins-ops-305-il2cpp-build"
weight: 1586
featured: false
tags:
  - "Delivery Engineering"
  - "CI/CD"
  - "Jenkins"
  - "Unity"
  - "IL2CPP"
series: "游戏团队 Jenkins 实战"
series_id: "delivery-jenkins-ops"
series_role: "article"
series_order: 160
delivery_layer: "practice"
delivery_volume: "V16"
delivery_parent_series: "delivery-cicd-pipeline"
delivery_reading_lines:
  - "L1"
  - "L2"
  - "L4"
leader_pick: true
---

## 在本篇你会读到

- **IL2CPP 是什么、为什么贵** —— 三步流程的资源开销
- **构建时长特征** —— 每阶段的耗时分布
- **内存特征** —— 峰值与持续，OOM 高发期
- **磁盘 I/O 特征** —— 中间产物的体积爆发
- **Agent 配置基线** —— 给定项目规模的硬件配置
- **五类故障与诊断** —— OOM / 磁盘 / 卡死 / 错误 / 慢
- **系列收尾** —— 17 篇地图回顾

---

## IL2CPP 是什么、为什么贵

IL2CPP（Intermediate Language To C++）是 Unity 的"AOT 编译"工具链：

```
C# 源码
   ↓ Roslyn 编译
.NET IL（中间语言）
   ↓ IL2CPP 转译
C++ 源码
   ↓ 平台 C++ 编译器（clang / msvc）
原生机器码（.so / .dylib / .a）
```

为什么这一套贵？

- **C# 项目大** → IL 大 → C++ 大 → 编译时间长
- **每个泛型实例化展开成具体 C++ 类** → 代码膨胀
- **链接所有 C++ 文件** → 内存峰值高
- **三层流程串联** → 单步失败要从头来

游戏团队 Unity 项目的 IL2CPP 构建：**1-3 小时不是稀奇事**。

---

## 构建时长特征：每阶段耗时分布

典型 Unity 游戏（C# 代码 50-200K 行）的 IL2CPP build 阶段拆解：

| 阶段 | 内容 | 时长占比 |
|------|------|---------|
| C# 编译 | Roslyn → IL（含 asmdef 编译域） | 5-15% |
| IL2CPP 转译 | IL → C++ | 15-30% |
| C++ 编译 | clang / msvc 编译每个 cpp | **40-60%（大头）** |
| 链接 | 链接所有 .o 成 .so / .dylib | 10-20% |
| 后处理 | 签名、对齐、压缩 | 5-10% |

### C++ 编译为什么慢

IL2CPP 转译输出的 C++ 文件非常多：

- 一个中等 Unity 项目：几万个 .cpp 文件
- 每个 .cpp 单独编译（即使用了 Bee 增量也只是 cache 命中率优化）
- 编译速度受 CPU 核数线性影响

**配 16 核机器编译 = 4 核机器 1/4 时间**——CPU 是瓶颈。

### 链接慢的根因

链接时把几万个 .o 合并成一个 .so / .dylib。这一步：

- **不能并行**（ld / lld 单线程）
- **内存峰值高**（可能 30+ GB）
- **磁盘 I/O 密集**（写最终 binary）

链接是 IL2CPP build 的**单点瓶颈**——加 CPU 没用，只能加内存和换更快的 SSD。

---

## 内存特征：峰值与持续

### 阶段性内存使用

```
内存使用（GB）
20  │                                ▲
18  │                              ▲ │
16  │               ▲▲▲           ▲   │
14  │            ▲▲    ▲▲       ▲     │
12  │         ▲▲         ▲    ▲       │
10  │      ▲▲              ▲▲         │
 8  │   ▲▲                            │
 6  │ ▲                               │
 4  │▲                                │
    └─────────────────────────────────┐
     C#  IL2CPP  C++ 编译        链接
     5min 10min  20-60min        15min
```

各阶段内存特征：

- **C# 编译**：低（2-4 GB）
- **IL2CPP 转译**：中（4-8 GB）—— 解析 IL + 生成 C++
- **C++ 编译**：中等（6-12 GB）—— 多 .cpp 并行，每个进程几百 MB
- **链接**：**最高峰值（15-30 GB）** —— 全部 .o 加载

OOM 高发在**链接阶段**——这一步不能拆。

### Mono / Bee 增量构建

Unity 2022.3+ 的 Bee 增量构建能省一些：

- C# 编译：增量
- IL2CPP 转译：部分增量
- C++ 编译：每个 .cpp 单独 cache（hash 一致就跳过）
- 链接：**仍然全量**（链接结果不能增量）

所以"修一行代码"的 incremental build 仍然要 5-15 分钟链接 → 这是最低时长。

### 进程数控制

C++ 编译并发数控制：

```bash
# 不限制（默认）：用满 CPU 核数
unity -batchmode -executeMethod Build.iOS

# 限制：避免 OOM（每个 cpp 编译进程 500MB-1GB）
export UNITY_IL2CPP_PARALLEL_JOBS=4
unity -batchmode -executeMethod Build.iOS
```

公式：`并发数 ≤ 可用内存 / 1GB`。16 GB 机器最多 16 并发，但要给链接预留内存——实际 8-12 比较安全。

---

## 磁盘 I/O 特征：中间产物的体积爆发

### 中间产物体积

```
Unity 项目 IL2CPP build 期间：
Library/                       50 GB     # 平台缓存
Build/iOS/                     20 GB     # Xcode 工程 + 中间产物
Build/iOS/.../il2cpp_data/     5-15 GB   # 转译出来的 C++ 源码
Build/iOS/.../o-files/         10-20 GB  # C++ 编译的 .o 文件
Build/iOS/Intermediates.../    5-10 GB   # Xcode 中间产物
最终产物（.ipa + dSYM）         200MB-2GB
```

**中间产物总量 100-150 GB / 单平台 build**——这是为什么 IL2CPP 构建机磁盘必须大。

### I/O 密集时段

链接阶段：

- 读：所有 .o（10-20 GB）
- 写：最终 .so / .dylib（200 MB-2 GB）
- **持续读写时长 5-15 分钟**

IOPS 不够（机械盘 / 网络存储）→ 链接时长延长 2-3 倍。

### 清理时机

build 完成后中间产物可以删——但要保留**部分 cache** 以加速下次：

```bash
# 保留：il2cpp 转译 cache（Library/Bee/）
# 删除：Build/iOS/.../o-files/  Build/iOS/Intermediates.noindex/
```

清理脚本要小心区分——删错了下次全量重 build 多花 30 分钟。

---

## Agent 配置基线

按项目规模给出推荐配置：

### 小项目（C# < 50K 行）

| 维度 | 配置 |
|------|------|
| CPU | 8 核 |
| 内存 | 32 GB |
| 磁盘 | 500 GB NVMe SSD（IOPS 5000+） |
| OS | Linux for Android, macOS for iOS |
| 单 Agent executors | 2（同时跑 2 个 build） |
| 预期 build 时长 | 30-45 分钟（增量），1-2 小时（全量） |

### 中型项目（C# 50-200K 行）

| 维度 | 配置 |
|------|------|
| CPU | 16 核 |
| 内存 | 64 GB |
| 磁盘 | 1 TB NVMe SSD（IOPS 20000+） |
| 单 Agent executors | **1** ←关键 |
| 预期 build 时长 | 45-90 分钟（增量），2-4 小时（全量） |

**executor = 1**：IL2CPP 链接内存峰值 30 GB+，同 Agent 跑两个 IL2CPP build → OOM 必然。

### 大型项目（C# > 200K 行）

| 维度 | 配置 |
|------|------|
| CPU | 32 核 |
| 内存 | 128 GB |
| 磁盘 | 2 TB NVMe SSD（独立挂载） |
| 网络 | 10 Gbps（产物分发用） |
| 单 Agent executors | 1 |
| 预期 build 时长 | 1-2 小时（增量），4-8 小时（全量） |

### 配置陷阱

- **CPU 数 / 内存比例**：保持 1:4 (8 核 / 32GB) 到 1:8 (16 核 / 128GB)。CPU 多内存少 → OOM；内存多 CPU 少 → 链接是瓶颈剩余资源浪费
- **不要用网络存储**：JENKINS_HOME 和 workspace 都不能放 NFS / SMB（详见 201）。IL2CPP 的中间产物 IO 极密集，网络存储会成为瓶颈
- **macOS Agent 的特殊问题**：Mac Mini 的 SSD 容量小（500GB-1TB），中型以上项目要外接 NVMe

---

## 五类故障与诊断

### 故障 1：OOM（链接阶段）

**信号**：build 在 80% 进度处突然失败，错误日志含 `linker exit code -9` 或 `Killed`。

**dmesg 看**：

```
Out of memory: Killed process 12345 (clang)
```

**诊断**：

- 看 free / vmstat，链接时刻内存是不是飙到 95%+
- 计算项目规模 vs 内存配置（C++ 编译 .o 总大小 / 2 ≈ 链接峰值）

**修复**：

- 短期：减少 IL2CPP_PARALLEL_JOBS
- 长期：升级 Agent 内存（IL2CPP 没有"省内存模式"）

### 故障 2：磁盘满（编译中段）

**信号**：build 在 50% 进度卡住，`No space left on device`。

**诊断**：

- `df -h` 看磁盘
- 看 `Build/iOS/.../o-files/` 体积

**修复**：

- 短期：清理空间，重跑（中间产物丢了要全量重 build）
- 长期：换更大磁盘 + 自动清理脚本（详见 203）

### 故障 3：卡死（无明显错误）

**信号**：build 在某个阶段不输出任何日志，CPU 0%，内存稳定。挂了 1-2 小时。

**最常见原因**：clang 进程死锁（IL2CPP 转译生成的 C++ 触发了 clang bug）。

**诊断**：

- `ps -ef | grep clang` 看是否有进程
- `strace -p <pid>` 看是不是卡在某个 syscall
- 看 IL2CPP 转译输出的 C++ 代码是否有"超大泛型实例"

**修复**：

- 短期：升级 Unity 版本（小版本通常修了已知 IL2CPP bug）
- 长期：减少泛型滥用（C# 代码层面）

### 故障 4：编译错误（IL2CPP 失败）

**信号**：build 在 IL2CPP 转译阶段失败，错误如 `IL2CPP error: unsupported feature`。

**诊断**：

- 看错误信息中的 C# 类 / 方法
- 通常是某些 C# 语法 / 反射 / 泛型组合 IL2CPP 不支持

**修复**：

- 改 C# 代码（避开 IL2CPP 限制）
- 加 `link.xml` 配置保留特定类型
- 升级 Unity（部分限制随版本放宽）

### 故障 5：单纯慢（无错误）

**信号**：build 成功但时长比上周长 30-50%。

**诊断检查清单**：

- 监控 Agent CPU 使用率：是不是低于 80%？低 → I/O 瓶颈或并发数不对
- 监控 Agent 内存：是不是接近上限？接近 → 在 swap，瓶颈是内存
- 看 Library 是否被频繁 reimport：检查 `Library/` 修改时间，看是不是 build 开头就大批 reimport
- 看 IL2CPP cache 命中率：`Library/Bee/Stats/` 下有 cache hit 数据
- 看磁盘 IOPS：`iostat` 看 `await` 列，>10ms 是慢

**最常见根因**：Library 缓存失效（详见 001 总论"主角反转"）。

---

## 系列收尾：17 篇地图回顾

写到这里，**游戏团队 Jenkins 实战**系列 17 篇全部交付：

### Part 0 · 导航
- 000 · 系列索引：阅读路径与读者画像
- 001 · 总论：四类结构性差异

### Part 1 · 流水线架构
- 101 · Declarative vs Scripted 选型
- 102 · Shared Library 设计
- 103 · 多产品矩阵
- 104 · 多分支流水线
- 105 · 并行模式

### Part 2 · 稳定性运维
- 201 · Master 三类瓶颈
- 202 · Agent 调度与标签
- 203 · 磁盘治理
- 204 · 升级踩坑
- 205 · 可观测性

### Part 3 · Unity 特化集成
- 301 · License 池
- 302 · 大仓库 Workspace
- 303 · 符号表与崩溃栈
- 304 · 多平台并行打包
- 305 · IL2CPP 构建特征（本篇）

### 三条阅读线再标定

- **L1 · 游戏构建侧（TA / 构建工程师）**：001 → Part 3 → Part 1 → Part 2
- **L2 · DevOps / SRE 侧**：001 → Part 2 → Part 1 → Part 3
- **L3 · 技术 Leader 侧（精选）**：001 + 102 + 201 + 203 + 301 + 303 + 305

---

## 全系列总结

四类结构性差异（001 总论）穿透全系列：

```
[物理约束：体积与时长]
  → 流水线架构（101-105）：怎么组织
  → 稳定性运维（201-205）：怎么不挂
  → Unity 特化（302 / 305）：怎么吃下规模

[资源稀缺：License + 平台矩阵]
  → 流水线架构（102 / 105）：抽象与并行
  → 稳定性运维（202）：调度策略
  → Unity 特化（301 / 304）：池化治理 + 平台隔离

[反向链路：符号表与崩溃栈]
  → 稳定性运维（203）：归档与保留
  → Unity 特化（303）：符号化链路

[主角反转：资源 vs 代码]
  → 流水线架构（102 / 105）：构建原语 + 并行
  → Unity 特化（302 / 304 / 305）：Library / Workspace / IL2CPP
```

**四类差异不是孤立技术问题，是"游戏团队工程化的 4 个维度"**——任何一维做不好，都会让 CI 整体表现失常。

### 写给 L3 面试官线读者

如果你是面试官，读完整个系列你应该能判断：

- 作者是否真的踩过这些坑（具体数字、具体故障模式、具体救火经过）
- 作者是否能区分"普遍知识"和"团队特化经验"
- 作者是否能从单维度问题推导到系统性认知

这是百万岗位需要的判断力。

### 写给 L1 / L2 读者

如果你是构建工程师 / DevOps，读完应该能：

- 立刻识别自己团队当前所处的阶段（Part 1 的演进表）
- 在出事故时知道该看哪类信号（Part 2 的诊断路径）
- 在做架构决策时知道哪些是治理边界、哪些是技术取舍

---

## 文末导读

系列正式收官。后续可能新增的内容：

- **附篇**：从 Jenkins 迁移到其他 CI 工具的取舍（Jenkins X / Tekton / GitHub Actions）
- **附篇**：游戏团队 build farm 的成本核算
- **附篇**：跨地域团队的 CI 架构

如有反馈或事故复盘想法，欢迎沿着系列入口（[000 索引]({{< relref "delivery-engineering/delivery-jenkins-ops-series-index.md" >}})）回流。
