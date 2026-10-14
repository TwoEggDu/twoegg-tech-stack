---
title: "技术目录"
slug: "technical-topics"
layout: "technical-topics"
description: "按技术领域进入已有系列：Unity、Unreal、渲染、运行时、计算基础、玩法、服务端、交付与 Agent 工程。"
topic_groups:
  - id: "unity"
    title: "Unity 与工具链"
    hint: "资产、序列化、编译、异步与裁剪"
    entries:
      - { title: "资产系统与序列化", page: "engine-toolchain/unity-asset-system-and-serialization-series-index.md" }
      - { title: "脚本编译管线", page: "engine-toolchain/unity-script-compilation-pipeline-series-index.md" }
      - { title: "Task / UniTask 与异步运行时", page: "engine-toolchain/unity-async-runtime-series-index.md" }
      - { title: "代码与资源裁剪", page: "engine-toolchain/unity-stripping-series-index.md" }
      - { title: "Android AAB / PAD", page: "engine-toolchain/unity-android-aab-pad-series-index.md" }
      - { title: "构建与调试前置", page: "engine-toolchain/build-debug-prereqs-series-index.md" }
      - { title: "更多工具链文章", page: "engine-toolchain/_index.md" }
  - id: "unreal"
    title: "Unreal Engine"
    hint: "UObject、GAS、网络与 Mass"
    entries:
      - { title: "Unreal 架构与系统", page: "system-design/unreal-engine-series-index.md" }
      - { title: "Unreal Mass", page: "system-design/unreal-mass-series-index.md" }
  - id: "rendering"
    title: "渲染与 GPU"
    hint: "URP、Shader、图形 API 与硬件"
    entries:
      - { title: "游戏图形栈", page: "rendering/game-graphics-stack-series-index.md" }
      - { title: "Unity 渲染系统", page: "rendering/unity-rendering-series-index.md" }
      - { title: "URP 深度", page: "rendering/urp-deep-dive-series-index.md" }
      - { title: "Shader 手写入门", page: "rendering/shader-handwriting-series-index.md" }
      - { title: "Shader 变体工程", page: "rendering/unity-shader-variants-series-index.md" }
      - { title: "图形 API", page: "rendering/graphics-api-series-index.md" }
      - { title: "GPU 架构与执行模型", page: "rendering/zero-b-deep-series-index.md" }
      - { title: "CachedShadows 阴影缓存", page: "rendering/cachedshadows-series-index.md" }
      - { title: "DLSS 进化论", page: "rendering/dlss-evolution-series-index.md" }
      - { title: "更多渲染文章", page: "rendering/_index.md" }
  - id: "runtime"
    title: "C# 与运行时"
    hint: "CLI、CoreCLR、Mono、IL2CPP、GC"
    entries:
      - { title: "从 C# 到 CLR", page: "engine-toolchain/cclr-00-what-this-series-is-about.md" }
      - { title: ".NET Runtime 生态总图", page: "engine-toolchain/dotnet-runtime-ecosystem-series-index.md" }
      - { title: "ECMA-335 / CLI", page: "engine-toolchain/ecma335-series-index.md" }
      - { title: "CoreCLR", page: "engine-toolchain/coreclr-series-index.md" }
      - { title: "Mono", page: "engine-toolchain/mono-series-index.md" }
      - { title: "IL2CPP", page: "engine-toolchain/il2cpp-series-index.md" }
      - { title: "HybridCLR", page: "engine-toolchain/hybridclr-series-index.md" }
      - { title: "LeanCLR", page: "engine-toolchain/leanclr-series-index.md" }
      - { title: "运行时横切对比", page: "engine-toolchain/runtime-cross-series-index.md" }
  - id: "foundations"
    title: "计算基础与性能"
    hint: "CPU、内存、OS I/O、数学与诊断"
    entries:
      - { title: "CPU 与内存体系", page: "performance/hardware-cpu-index.md" }
      - { title: "存储、文件系统与 OS I/O", page: "engine-toolchain/storage-io-series-index.md" }
      - { title: "图形数学", page: "rendering/graphics-math-series-index.md" }
      - { title: "内存专题", page: "performance/memory-topic-index.md" }
      - { title: "移动端硬件与优化", page: "performance/mobile-hardware-and-optimization-series-index.md" }
      - { title: "游戏性能判断", page: "performance/game-performance-judgment-series-index.md" }
      - { title: "机型分档", page: "performance/device-tiering-series-index.md" }
      - { title: "渲染系统分档设计", page: "performance/rendering-tier-design-series-index.md" }
      - { title: "游戏预算管理", page: "performance/game-budget-management-series-index.md" }
      - { title: "更多性能文章", page: "performance/_index.md" }
  - id: "systems"
    title: "引擎架构与玩法"
    hint: "技能、DOTS、算法与设计模式"
    entries:
      - { title: "游戏引擎架构", page: "system-design/game-engine-architecture-series-index.md" }
      - { title: "技能系统", page: "system-design/skill-system-series-index.md" }
      - { title: "数据导向运行时", page: "engine-toolchain/data-oriented-runtime-series-index.md" }
      - { title: "Unity DOTS 工程实践", page: "system-design/dots-engineering-index.md" }
      - { title: "数据导向实战案例", page: "system-design/dod-case-series-index.md" }
      - { title: "数据导向行业案例对比", page: "system-design/dod-industry-series-index.md" }
      - { title: "数据结构与算法", page: "system-design/data-structures-and-algorithms-series-index.md" }
      - { title: "设计模式前置", page: "system-design/pattern-prerequisites-series-index.md" }
      - { title: "设计模式教科书", page: "system-design/design-patterns-textbook-series-index.md" }
      - { title: "游戏编程模式", page: "system-design/game-programming-patterns-series-index.md" }
      - { title: "更多系统设计文章", page: "system-design/_index.md" }
  - id: "server"
    title: "网络与服务端"
    hint: "ET、ECS、同步、部署与运维"
    entries:
      - { title: "游戏后端基础", page: "system-design/game-backend-series-index.md" }
      - { title: "高性能服务端 ECS", page: "system-design/server-ecs-series-index.md" }
      - { title: "ET 前置桥接", page: "et-framework-prerequisites/_index.md" }
      - { title: "ET9 框架历史文章", page: "et-framework/_index.md" }
      - { title: "服务端架构与构建", page: "delivery-engineering/delivery-server-architecture-series-index.md" }
      - { title: "服务端部署与运维", page: "delivery-engineering/delivery-server-operations-series-index.md" }
      - { title: "服务端版本与热更新", page: "delivery-engineering/delivery-server-versioning-series-index.md" }
  - id: "delivery"
    title: "交付与线上运营"
    hint: "内容、构建、CI、发布与反馈"
    entries:
      - { title: "交付总论", page: "delivery-engineering/delivery-overview-series-index.md" }
      - { title: "工程基础", page: "delivery-engineering/delivery-engineering-foundation-series-index.md" }
      - { title: "内容生产与配置", page: "delivery-engineering/delivery-content-pipeline-series-index.md" }
      - { title: "资源管线", page: "delivery-engineering/delivery-resource-pipeline-series-index.md" }
      - { title: "多端构建", page: "delivery-engineering/delivery-multiplatform-build-series-index.md" }
      - { title: "包体与资源分发", page: "delivery-engineering/delivery-package-distribution-series-index.md" }
      - { title: "脚本热更新", page: "delivery-engineering/delivery-hot-update-series-index.md" }
      - { title: "版本与分支管理", page: "delivery-engineering/delivery-version-management-series-index.md" }
      - { title: "CI/CD 流水线", page: "delivery-engineering/delivery-cicd-pipeline-series-index.md" }
      - { title: "Jenkins 实战", page: "delivery-engineering/delivery-jenkins-ops-series-index.md" }
      - { title: "平台发布", page: "delivery-engineering/delivery-platform-publishing-series-index.md" }
      - { title: "灰度与发布运营", page: "delivery-engineering/delivery-release-operations-series-index.md" }
      - { title: "组织与治理", page: "delivery-engineering/delivery-org-governance-series-index.md" }
      - { title: "案例与模板", page: "delivery-engineering/delivery-cases-templates-series-index.md" }
      - { title: "长线运营与 AI 集成", page: "live-ops-engineering/liveops-ai-series-index.md" }
  - id: "quality"
    title: "质量与问题排查"
    hint: "测试、回归、Crash 与工程判断"
    entries:
      - { title: "代码质量", page: "code-quality/code-quality-series-index.md" }
      - { title: "游戏质量管理", page: "code-quality/game-quality-series-index.md" }
      - { title: "软件工程基础与 SOLID", page: "system-design/software-engineering-solid-series-index.md" }
      - { title: "验证与测试", page: "delivery-engineering/delivery-verification-testing-series-index.md" }
      - { title: "性能与稳定性工程", page: "delivery-engineering/delivery-performance-stability-series-index.md" }
      - { title: "缺陷闭环", page: "delivery-engineering/delivery-defect-lifecycle-series-index.md" }
      - { title: "崩溃分析", page: "engine-toolchain/crash-analysis-series-index.md" }
      - { title: "问题拆解", page: "problem-solving/_index.md" }
      - { title: "工程判断", page: "essays/engineering-judgment-series-index.md" }
      - { title: "工程案例阅读入口", page: "portfolio.md" }
  - id: "agent"
    title: "AI 与 Agent 工程"
    hint: "Agent、Context、Harness 与验证"
    entries:
      - { title: "Agent Engineering", page: "ai-empowerment/agent-engineering-series-index.md" }
      - { title: "Harness Engineering", page: "harness-engineering/harness-engineering-series-index.md" }
      - { title: "AI 赋能游戏开发", page: "ai-empowerment/ai-empowerment-series-index.md" }
---

知道自己要找什么技术，可以从这里直接进入系列索引，再按系列目录读正文。也可以用页首搜索查具体术语。

想从游戏交付的过程开始，转到[研发地图]({{< relref "game-development.md" >}})。这里只维护阅读入口；文章顺序、版本与验证说明仍以各系列和正文为准。
