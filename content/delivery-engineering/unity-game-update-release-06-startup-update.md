---
title: "06｜玩家启动后发生什么：强更新与热更新的执行流程（提纲）"
slug: "unity-game-update-release-06-startup-update"
date: "2026-10-23"
description: "提纲，正文待写：客户端怎样把远端版本策略落实为一次可继续或被阻止的启动？"
draft: false
content_stage: "outline"
weight: 71
tags: ["Unity", "YooAsset", "交付工程", "热更新"]
series: "Unity 游戏更新与发布"
primary_series: "unity-game-update-release"
series_order: 6
type: "reading-series"
reading_title: "玩家启动后发生什么：强更新与热更新的执行流程"
reading_summary: "客户端怎样把远端版本策略落实为一次可继续或被阻止的启动？"
---

> 贯穿案例：分别跟踪首次启动、有旧缓存和必须强更的玩家。

## 1. 从本地身份到远端策略

- 说明读取 BuildInfo、请求清单、解析和选择策略的顺序。
- 记录本次启动采用的版本快照，避免运行中无条件追逐最新清单。

## 2. 什么时候阻止启动并要求整包更新

- 判断最低兼容条件及逐包强更目标，解析可取得的目标客户端。
- 区分 Android 跳转更新与小游戏平台更新机制；提示退出不代表平台新包已经应用。

## 3. 无需强更时怎样准备内容

- 按已选版本加载各 Package Manifest，再计算所需内容和缓存差异。
- 区分当前下载操作零待下载与全部游戏内容就绪。

## 4. 下载策略怎样按平台变化

- 比较 Android 启动完整准备与 WebGL 保留按需加载的路径。
- 解释下载量提示、用户确认、缓存命中和重试各自解决什么问题。

## 5. 代码和数据何时可以进入业务

- 说明 DLL 字节读取、元数据补充、程序集加载与业务入口的依赖。
- 区分可重试的下载失败与不可逆加载后的失败，明确何时需要重启。

## 6. 失败怎样结束或恢复

- 覆盖断网、清单缺失、目标内容不存在、用户取消和 DLL 加载失败。
- 按三类玩家画出成功、拒绝与恢复分支，并列出待执行的验证用例。

## 延伸阅读

- [HybridCLR 系列]({{< relref "engine-toolchain/hybridclr-series-index.md" >}})：深入 C# 热更执行、AOT 元数据和兼容机制。
- [脚本热更新]({{< relref "delivery-engineering/delivery-hot-update-series-index.md" >}})：继续讨论热更架构、验证与故障处理。

## 计划配图与表格

启动时序图；三类玩家路径图；失败与恢复分支表。

## 成稿前需要补齐

成稿前固定 YooAsset API 与平台版本；提纲不代表 Unity、网络故障或真机实验已执行。
