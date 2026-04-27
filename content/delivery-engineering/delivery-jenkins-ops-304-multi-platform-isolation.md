---
date: "2026-04-27"
title: "多平台并行打包与隔离"
description: '同时出 iOS / Android / WebGL 看似简单 parallel，实际是 Library 平台敏感 + workspace 不能共享的隔离问题。本篇讲三类隔离方案（workspace / Agent / Library cache 分层）的取舍，以及一个真实"假并发反而变慢"的案例。'
slug: "delivery-jenkins-ops-304-multi-platform-isolation"
weight: 1585
featured: false
tags:
  - "Delivery Engineering"
  - "CI/CD"
  - "Jenkins"
  - "Unity"
  - "Multi-platform"
series: "游戏团队 Jenkins 实战"
series_id: "delivery-jenkins-ops"
series_role: "article"
series_order: 150
delivery_layer: "practice"
delivery_volume: "V16"
delivery_parent_series: "delivery-cicd-pipeline"
delivery_reading_lines:
  - "L1"
  - "L2"
---

## 在本篇你会读到

- **"并行打包"的两个误解** —— 不是 parallel 块就能并行
- **资源冲突的本质** —— Library 是平台敏感的
- **隔离方案 1：Workspace 隔离** —— 每平台独立目录
- **隔离方案 2：Agent 隔离** —— 每平台独立机器
- **隔离方案 3：Library Cache 分层** —— 共享源码、独立 Library
- **真实组合方案** —— 三平台并行的可行架构

---

## "并行打包"的两个误解

游戏团队最常见的两个错误认知：

### 误解 1：parallel 块就是并行

```groovy
parallel {
    stage('iOS') { steps { sh 'build_ios.sh' } }
    stage('Android') { steps { sh 'build_android.sh' } }
}
```

这段代码**写法是并行**，但**执行可能是串行**——如果两个 stage 调度到同一台 Agent 的同一个 workspace，第二个会等第一个的 Library reimport 完才开始。详见 001 总论"假并发"。

### 误解 2：iOS 和 Android 互不干扰

很多人以为 iOS 构建只动 iOS 相关文件，Android 构建只动 Android 相关——错。

Unity 的 Library 缓存里包含**所有资源的导入产物**，平台不同导入产物不同：

```
Library/
├─ artifacts/         # 资源 import 缓存
│   ├─ <hash-iOS>     # iOS 平台导入的纹理（ASTC 压缩）
│   ├─ <hash-Android> # Android 平台导入的纹理（ETC2 压缩）
│   └─ <hash-WebGL>   # WebGL 平台导入的纹理（DXT5 压缩）
├─ ScriptAssemblies/  # 脚本编译产物（含 platform define）
└─ ...
```

切换平台时 Unity 重新 import 大量资源——这是"切平台慢"的根因。

**所以**：iOS 构建动了 Library，Android 构建运行时**整个 Library 都被认定不是 Android 平台的**，要重新 import。

---

## 资源冲突的本质：Library 平台敏感

### Library 平台敏感的具体表现

```
# 当前 Library 是 iOS 平台
$ unity -buildTarget Android ...
# Unity 输出：
# Application.RequestUserAuthorization
# ... (一堆 reimport)
# Reimported 4823 assets, took 18:34
```

切换 buildTarget 触发**全量 reimport**——18 分钟。

### 为什么不能"双 platform Library 共存"

理论上可以——但 Unity Library 的设计是"单一 active platform"。要让 iOS 和 Android Library 共存，要么：

- **方案 A**：完全独立的 workspace（两份 Library 各自存）
- **方案 B**：在 Library 之外维护"平台缓存归档"，build 前 swap

方案 A 简单粗暴；方案 B 复杂但磁盘省。游戏团队大多选 A。

### 资源冲突外的隐藏冲突

除了 Library，还有几个平台敏感的目录：

- `Temp/`：每次 build 都用，多 build 同时跑互相覆盖
- `Build/`：输出目录，多 build 同时写会乱
- `obj/`：C# 编译中间产物，build target 不同则不同
- `Logs/`：Unity 日志（小，但混在一起难调试）

任意一个目录冲突都会让 build 失败或产生错误产物。

---

## 隔离方案 1：Workspace 隔离

最简单：每个平台独立 workspace。

### 配置

```groovy
parallel {
    stage('iOS') {
        agent { label 'unity-builder && macos' }
        options { skipDefaultCheckout() }
        steps {
            ws("${env.WORKSPACE}-ios") {
                checkout scm
                sh 'unity -batchmode -buildTarget iOS -executeMethod Build.iOS'
            }
        }
    }
    stage('Android') {
        agent { label 'unity-builder && linux' }
        options { skipDefaultCheckout() }
        steps {
            ws("${env.WORKSPACE}-android") {
                checkout scm
                sh 'unity -batchmode -buildTarget Android -executeMethod Build.Android'
            }
        }
    }
}
```

`ws()` step 显式指定 workspace 路径，每个平台独立目录。

### 代价

- **磁盘占用 3 倍**：每个平台一份 workspace = 三份 50 GB workspace = 150 GB / Agent
- **首次 checkout 慢**：每个平台都要拉一份代码（详见 302 用 reference repo 优化）

### 适用规模

- 中小团队（产品 < 10 个），磁盘够 → 这是最简单的方案

---

## 隔离方案 2：Agent 隔离

按平台划分 Agent 池，每平台用专门的 Agent。

### 配置

```
Agents:
  - mac-1, mac-2 → labels: unity-builder, macos, ios
  - linux-1, linux-2, linux-3 → labels: unity-builder, linux, android
  - linux-4 → labels: unity-builder, linux, webgl
```

Pipeline：

```groovy
parallel {
    stage('iOS') {
        agent { label 'unity-builder && ios' }
        steps { sh 'unity -batchmode -buildTarget iOS ...' }
    }
    stage('Android') {
        agent { label 'unity-builder && android' }
        steps { sh 'unity -batchmode -buildTarget Android ...' }
    }
}
```

每个平台调度到不同 Agent，物理隔离。

### 优势

- **完全没冲突**——不同 Agent 的 workspace 天然独立
- **每台 Agent 的 Library 始终是同一平台**——没有切平台的 reimport
- **平台特化能力可以专门配置**——iOS Agent 装 Xcode，Android Agent 装 NDK

### 代价

- **Agent 池规模翻倍**：5 平台 × 2 台冗余 = 10 台 Agent 起步
- **平台间负载不均**：iOS 是发版瓶颈（macOS 贵）但 build 频率高，Android 便宜但需求量看产品组成
- **License 池要按平台分配**（详见 301）

### 适用规模

- 中大团队（产品 ≥ 10 个）
- 有专门 macOS 集群（不是 1-2 台 Mac Mini）
- License seat 充足

---

## 隔离方案 3：Library Cache 分层

进阶方案：源码共享，Library 按平台独立缓存。

### 思路

```
Workspace 主目录：
├─ src/             # 源码（共享）
├─ Library-iOS/     # iOS 专用 Library
├─ Library-Android/ # Android 专用 Library
└─ Library-WebGL/   # WebGL 专用 Library
```

build 前根据 platform，把对应 Library 软链或拷贝到 `Library/`：

```bash
ln -sfn Library-iOS Library
unity -batchmode -buildTarget iOS ...
```

### 实现

```groovy
def platformLibrary(String platform) {
    sh """
        rm -f Library
        ln -sfn Library-${platform} Library
        # 如果 Library-iOS 不存在，第一次 build 时 Unity 自己创建
    """
}

parallel {
    stage('iOS') {
        agent { label 'unity-builder' }
        steps {
            platformLibrary('iOS')
            sh 'unity -batchmode -buildTarget iOS ...'
        }
    }
    stage('Android') {
        agent { label 'unity-builder' }
        steps {
            platformLibrary('Android')
            sh 'unity -batchmode -buildTarget Android ...'
        }
    }
}
```

### 关键点

- **必须串行执行同 Agent 的多个平台 build**（不能并行，因为 ln 是切换 Library 软链）
- **如果一定要并行**，要用独立 workspace（回到方案 1）
- 适合"多平台串行 build"场景，节省每次切平台的 reimport

### 优势

- **磁盘比方案 1 省**——源码只一份，Library 多份但总量比方案 1 小
- **不需要 Agent 隔离**——同 Agent 串行跑多平台，每次切换是 ln（毫秒级）

### 代价

- **配置复杂**——要管理软链、清理过期 Library
- **Library 目录命名规则容易出错**

### 适用规模

- 中等团队
- 同 Agent 串行多平台 build（不强求并行）

---

## 真实组合方案：三平台并行架构

某游戏团队 5 产品 × 3 平台的稳定方案：

### 整体架构

```
[Build Farm]
├─ 4 台 macOS Agent  → labels: ios, unity-builder, mac
├─ 6 台 Linux Agent  → labels: android, unity-builder, linux
├─ 2 台 Linux Agent  → labels: webgl, unity-builder, linux
└─ 共享：reference repo cache（每 Agent 一份本地 git mirror）
```

### Pipeline 形态

```groovy
@Library('game-pipeline-lib@v1.5.0') _

pipeline {
    agent none  // 顶层不绑定 Agent
    stages {
        stage('Build All Platforms') {
            parallel {
                stage('iOS') {
                    agent { label 'ios && unity-builder' }
                    steps {
                        gameBuildPlatform('iOS')   // Shared Library 函数
                    }
                }
                stage('Android') {
                    agent { label 'android && unity-builder' }
                    steps {
                        gameBuildPlatform('Android')
                    }
                }
                stage('WebGL') {
                    agent { label 'webgl && unity-builder' }
                    steps {
                        gameBuildPlatform('WebGL')
                    }
                }
            }
        }
    }
}
```

### 隔离机制总结

- **Agent 隔离**：iOS / Android / WebGL 各自专属 Agent 池（方案 2）
- **每 Agent 的 workspace**：用 customWorkspace 持久化（一份），不切平台所以不需要分层
- **License**：每平台 Agent 池有自己的 license 配额

### 性能数据

- iOS / Android / WebGL 真并行
- 三平台并行总时长 ≈ 单平台最长那个 + 5 分钟（调度开销）
- 串行总时长 ≈ 三平台时长之和
- **节省**：单平台 60 分钟，三平台串行 180 分钟，并行 65 分钟，节省 115 分钟

---

## 文末导读

下一步进 305 IL2CPP 构建的时间与内存特征——本系列最后一篇，讲游戏团队 Jenkins 最痛的 Agent：IL2CPP 构建机。

L3 面试官线读者：本篇核心是"假并发"那一节——并行不是写出 parallel 块就成立，是要消除资源冲突。游戏团队的"资源"是 Library 缓存这种隐形资源，不是 CPU/内存。
