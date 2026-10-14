---
title: "游戏开发笔记"
description: "记录引擎、工具开发和产品交付中的问题，整理 Unity / Unreal、渲染、资产系统与运行时的阅读路径。"
home_feature:
  page: "performance/game-performance-read-does-not-mean-ready.md"
  title_lines:
    - "读盘完成，"
    - "为什么资源还不能用？"
  label: "资源加载 · 性能"
  summary: "文件已经读进内存，第一次显示时却仍然卡顿。接下来发生的解压、对象创建和 GPU 上传，才是这篇文章要拆开的部分。"
home_articles:
  - page: "engine-toolchain/unity-importer-what-does-it-do-and-why-source-file-is-not-just-a-file.md"
    title: "Unity 的 Importer 做了什么"
    label: "Unity · 资产系统"
    summary: "同一份 FBX 进入工程后，为什么会变成模型、动画和一组引用？先把源文件与导入对象分开看。"
  - page: "system-design/skill-system-11-multiplayer-sync.md"
    title: "多人技能里的状态与表现"
    label: "玩法系统 · 服务端"
    summary: "按下按键、服务器接受请求、技能生效，并不是同一件事。同步从这些边界开始。"
  - page: "code-quality/static-analysis-and-asset-quality-tools.md"
    title: "哪些资源问题应该交给工具检查"
    label: "工具开发 · 质量"
    summary: "命名、引用和导入参数可以写成规则；规则怎样返回错误、进入构建，是另一半工作。"
home_about: "过去主要做引擎和工具，现在更多参与跨工种的产品交付。服务端运行和数据分析，也在逐步学习。"
---

记录引擎、工具开发和产品交付中的问题。<br>
写清楚事情怎么发生，也记下还没弄明白的地方。
