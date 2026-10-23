---
title: "05｜本次打什么包：整包构建与资源独立构建（提纲）"
slug: "unity-game-update-release-05-build-scope"
date: "2026-10-23"
description: "提纲，正文待写：本次构建哪些产物，改变哪些版本，又交付给哪个客户端？"
draft: false
content_stage: "outline"
weight: 70
tags: ["Unity", "YooAsset", "交付工程", "热更新"]
series: "Unity 游戏更新与发布"
primary_series: "unity-game-update-release"
series_order: 5
type: "reading-series"
reading_title: "本次打什么包：整包构建与资源独立构建"
reading_summary: "本次构建哪些产物，改变哪些版本，又交付给哪个客户端？"
---

> 贯穿案例：以 B100 和 R16 为输入，构建 HotCode 8 与 Data 21，继承 Res 12，形成 R17 候选。版本明细统一见[第 04 篇的契约表]({{< relref "delivery-engineering/unity-game-update-release-04-version-binding.md#release-examples" >}})。

## 1. 一次构建先固定哪些输入

- 固定平台、渠道、环境、源码与内容修订、SDK Profile、构建工具版本、目标客户端基线和源组合；同为 WebGL 不代表宿主平台或发布渠道相同。
- 本次计划明确使用客户端 100 的 B100、源组合 R16，不能因为 101 已注册就改为给最新客户端打补丁。
- 区分开发与正式构建。面向已发客户端的补丁构建必须取得对应的归档基线与完整源组合，缺失时停止，不从工作目录临时拼接。
- 首次 Full 根据固定输入生成 B100 与初始组合 R16，验收后归档冻结；后续 Full 明确哪些材料继承、哪些重新生成，为客户端 101 建立 B101 与 R19，不要求新基线预先存在。

## 2. 用五个选项表达构建范围

| 构建选项 | 中文名称 | 客户端程序 | 游戏资源 | 热更代码 | 游戏数据 |
|---|---|:---:|:---:|:---:|:---:|
| `Full` | 完整客户端 | ✓ | ✓ | ✓ | ✓ |
| `HotUpdate` | 全部热更内容 | — | ✓ | ✓ | ✓ |
| `Res` | 游戏资源 | — | ✓ | — | — |
| `HotCode` | 热更代码 | — | — | ✓ | — |
| `Data` | 游戏数据 | — | — | — | ✓ |

`Res` 指模型、动画、特效、UI、场景和音频等游戏资源；`HotCode` 仅指可热更新的代码；`Data` 指配置、关卡和模板等运行数据。这五个名称是本系列的构建约定，不是 YooAsset 内置枚举。

- Full 覆盖客户端与内容的完整交付集合；哪些资源内置、哪些远程交付另行决定，不等于把所有文件塞进安装包。
- HotUpdate 构建三类热更内容；单类选项负责各自产物。能独立构建不代表任何新旧组合都能独立发布。

## 3. 构建范围、缓存、上传与下载分别决定什么

| 决定 | 需要回答的问题 | R17 案例中的区别 |
|---|---|---|
| 构建范围 | 本次请求生成哪些逻辑内容 | 构建 HotCode 与 Data，继承 Res |
| 增量构建 | 构建器能否复用输入和缓存 | 影响构建工作量，不改变验收所需的完整组合 |
| 差异上传 | 哪些文件在目标存储中尚不存在或身份不同 | 按实际文件身份核对，不能只看版本号 |
| 差异下载 | 当前玩家本地缺哪些所需文件 | 因内置内容、缓存及本次准备范围而变化 |

- 用 YooAsset 的完整版本输出解释为什么“增量构建”不等于“只生成差异补丁文件”。
- 对照 Addressables 的内容更新构建与原始 content state：这是另一套构建工作流，不直接套成本系列的构建选项。

## 4. 从 R16 形成 R17 候选

1. 固定 B100、R16 和本次修复所用的代码与数据输入修订。
2. 由构建流水线分配并记录 HotCode 8、Data 21 的实际版本；未构建的 Res 继承 R16 的版本与文件身份。
3. 分别用 HotCode、Data 选项构建，保存两份回执；此时得到的是候选产物，线上策略仍可指向 R16。
4. 合并为 R17 候选记录，检查 Data 21 对 HotCode 8 的依赖，并按第 04 篇核验完整组合。
5. 验收通过后冻结 R17 的组合记录与文件身份，交给第 07 篇；客户端何时采用由发布策略决定。

- Data 21 不能单独配 HotCode 7 上线；若某次配置修改与原代码兼容，可以只构建 Data，但仍要生成并验证完整的新组合记录。
- 版本分配、构建与验证失败可以留下未使用的编号；不为了连续编号重用已发布身份，也不覆盖同一版本下的不同内容。
- R18 的新增内容可以使用 HotUpdate；R19 配套新客户端 101 的 Full 构建。具体内容版本仍以第 04 篇记录和构建回执为准。

## 5. 热更必须保留哪些客户端基线材料

- 归档客户端构建回执、Unity 与包版本、BuildTarget、Profile、裁剪与热更设置、输入修订、首包清单及相关文件校验值。
- 采用 AOT 补充元数据方案时，保存该客户端打包时的裁剪 AOT DLL；后续热更引用对应基线，不用工作目录里刚生成的一份替换，也不跨 BuildTarget 复用。
- 说明补充元数据的作用和限制：它不等于让旧客户端获得原本不存在的原生能力；B101 的底层接口变化仍需要配套客户端。
- 对照 Addressables 方案时，另列每个平台、每次原始应用发布的 `addressables_content_state.bin`；不把它当成 YooAsset 必需文件。

## 6. 输出什么，后续流程才能接手

- 交付客户端身份或目标基线、源组合、新组合、各内容版本、产物位置与校验值、构建回执、兼容检查结果，以及未验证项。
- 第 06 篇用其中的确定版本解释启动时准备什么；第 07 篇核对这些内容在目标环境可取得后再切换入口。
- 构建成功、组合验收通过、产物已上传、策略已生效分别记录；本篇不把某个构建选项等同于自动上线。

## 资料对照与延伸阅读

- [YooAsset 资源构建](https://www.yooasset.com/docs/guide-editor/AssetBundleBuilder)：增量构建、完整补丁包输出与差异上传。
- [Addressables 2.7 内容更新构建](https://docs.unity3d.com/Packages/com.unity.addressables@2.7/manual/content-update-builds-overview.html)：原始 content state 与更新工作流。
- [HybridCLR AOT 泛型](https://www.hybridclr.cn/docs/basic/aotgeneric)：补充元数据与客户端打包基线。
- [第 06 篇：启动时采用组合]({{< relref "delivery-engineering/unity-game-update-release-06-startup-update.md" >}})、[第 07 篇：发布与恢复]({{< relref "delivery-engineering/unity-game-update-release-07-publish-rollback.md" >}})。

## 计划配图与表格

构建范围矩阵；B100 与 R16 到 R17 候选的输入输出图；构建回执和基线归档样例。

## 成稿前需要补齐

核对实际构建入口、版本分配与回执，补齐失败后不污染已发身份的实验；名称约定不表示工程枚举已经迁移，教学组合不表示 Unity 构建已执行。
