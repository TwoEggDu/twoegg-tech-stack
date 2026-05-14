---
title: "Harness Engineering 01｜为什么游戏引擎客户端的 AI Coding 需要重新设计 Harness"
slug: "harness-engineering-01-why-game-engine-needs-rethink"
date: "2026-05-13"
description: "外部讲 Harness 的文章基本都是 Java/Web 后端场景。游戏引擎客户端有 C++ 引擎 license、生成代码、Shader Variant、多平台构建这些独有约束，通用 v0 套在这里不够用。"
tags:
  - "Harness Engineering"
  - "AI Engineering"
  - "Game Engine"
  - "Unity"
series: "Harness Engineering"
primary_series: "harness-engineering"
series_role: "article"
series_order: 10
weight: 2110
---

> **读这篇之前**：本篇默认你已经读过 [AI 赋能 08｜我如何搭建自己的 AI Coding Harness Engineering]({{< relref "ai-empowerment/ai-empowerment-08-ai-coding-harness-engineering.md" >}})，懂五层模型（Context / Rules / Workflow / Checks / Memory）、状态机和五项最小指标。这篇不重复讲这些，讲的是当 v0 跑在游戏引擎客户端时会撞到什么额外的墙。

## 这篇解决什么问题

如果你最近读过两三篇讲 AI Coding Harness 的文章，会有一种很顺的感觉：上下文怎么放、Skill 怎么切、状态机怎么走、指标怎么算，每一步都讲得清楚，作者最后给一个 90% AI Coding 率的数字，文章就结束了。

读完这种文章去自己的项目里照抄一遍，第一周大概也确实顺——把 CLAUDE.md 写起来、几个 Skill 拆出来、状态机搭起来，AI 接住了一些原本要手写的代码。

然后第二周，问题开始出现。

不是 AI 突然变笨，也不是 Harness 设计有 bug。是项目本身——一个真实在运行的游戏引擎客户端项目——在向 Harness 索取它根本没有准备好回答的问题：

- AI 想读引擎层的 C++ 源码去理解一个 GameObject 的生命周期，但那部分代码受 Unity license 限制不能进公开仓库
- AI 改了一个 UI 脚本，下一次美术拉项目跑代码生成器，AI 的改动被覆盖了
- AI 写了一个新功能，没人告诉它这个功能要同时跑 iOS / Android / WebGL 三个平台、每个平台的 Shader Variant 列表都不一样
- AI 改了一个 AssetBundle 加载流程的代码，但它不知道这个改动会影响下一次 Addressables 构建的依赖图
- 项目升 Unity 大版本，CLAUDE.md 里写的"用 SerializedReference"突然变成 best practice 而不是约束

这些问题不是"AI 没读到上下文"——CLAUDE.md 里写得再详细，AI 读了也不一定能在每次操作前都把全部约束串起来。它们也不是"流程没设计好"——状态机就算严格按 Intake → Verify 走，Verify 那一关也没办法在五分钟内跑完一次多平台 Shader 编译和 AssetBundle 重建。

这是**领域**的问题。游戏引擎客户端这个领域，给 AI Coding Harness 加了一组通用方法论没准备处理的约束。

这篇文章要回答的就是：这些独有约束具体是什么、它们对 Harness 的五层模型分别意味着什么、在通用 v0 之上还需要补什么。

## 通用 Harness 在游戏引擎客户端会撞到的五堵墙

我把这些约束归成五堵墙。它们不是 Unity 或 Unreal 独有——任何带 C++ 引擎、带资源系统、带多平台发布的客户端工程都会遇到——但在游戏开发里它们集中出现，叠加在一起就成了通用方法论的盲区。

### 第一堵墙：引擎源码 license 边界

通用 AI Coding 的一个隐含前提是 AI 可以读到它要修改的代码。在 Web 后端项目里这是真的——你的整个仓库就是你的全部代码，加上几个 package 的 node_modules，AI 全部能读。

游戏引擎客户端不是。

Unity 的 C++ 引擎源码是受 license 限制的非公开物。你的项目仓库里只有 C# 业务层、Package 层（URP、Addressables、Input System 这些是公开的）、和编辑器扩展。当 AI 想理解 `Resources.Load` 为什么有时返回 null、`OnDestroy` 跟 GC 是什么时序关系、`Animator` 内部状态机怎么 tick 的时候，它能读到的最多是 Package 层的 wrapper，再往下就是黑盒。

这堵墙带来三个具体后果：

第一，**AI 倾向于编造合理解释**。读不到源码的时候，AI 倾向于按"如果我来设计这个 API 大概会怎么实现"来推断行为。多数时候它猜对了，但碰到 Unity 引擎那些反直觉的细节（比如 Awake 在 Inactive GameObject 上的触发时机、prefab variant 的 serialization 顺序），它会自信地猜错。

第二，**Harness 必须显式禁止 AI 把"我读了源码"作为论据**。哪怕私仓里有完整 Unity 源码，公仓输出的文章和代码也不能假设读者能验证那些源码。这件事在 TechStackShow 的项目规范里已经写明（见 [CLAUDE.md](../../../CLAUDE.md)），但通用 Harness 没有这个概念。

第三，**Verify 阶段的验证手段变了**。Web 后端可以靠跑单测验证一个改动的行为；游戏引擎客户端有大量行为只能靠"在编辑器里实测 + Profiler 看数据 + 真机跑一遍"——这些都不是 AI 能闭环跑的，必须有人在中间接一棒。

通用 v0 在这里需要补的是：在 Rules 层加一组关于"AI 不能基于私仓 / 闭源源码做断言"的硬约束，在 Verify 层把"AI 可闭环验证"和"必须人接棒"的任务显式分开。

### 第二堵墙：生成代码与人手代码混居

Web 后端项目里也有代码生成（OpenAPI 客户端、protobuf、ORM 类型），但它们通常被关在很明确的目录里，工程师不会去手改，AI 也容易识别"不要碰这里"。

Unity 项目里，生成代码经常和手写代码紧紧混在一起：

- UI 框架（FairyGUI、UGUI、UI Toolkit 各自的 binding 生成器）会在跟你手写脚本相同的目录下放生成产物
- 配置表反序列化代码、协议序列化代码、Localization key 类，都是生成的，但又必须被业务层引用
- ScriptableObject 的 GUID 引用关系经常出现在 prefab / scene 的 yaml 里，AI 改 prefab 时极容易破坏这些引用
- Addressables 的 catalog 是构建产物，但又被部分序列化进项目

AI 在这种环境里默认不知道哪些文件是生成物。它读到 `*.cs` 就当成可以改的脚本，读到 prefab 就当成可以编辑的资源。后果是：

- 修改了一个生成出来的 binding 类，下一次跑生成器全部回滚
- 改了一个 prefab 里的字段名，scene 里所有引用这个 prefab 的 GUID 失效
- 改了 Addressables 配置的某个标签，下一次构建 catalog 时依赖关系全乱

通用 v0 在这里需要补的是：在 Context 层显式列出本项目所有生成代码目录、所有不可手改的资源类型，在 Rules 层加机械化门禁（最好是 git hook 或 CI check）阻止 AI 修改这些位置。

### 第三堵墙：多平台 / 多 Variant 的笛卡尔积

通用 AI Coding 任务的一次 Verify 是"跑一次单测"。游戏引擎客户端的一次 Verify 经常是"在 iOS / Android / WebGL 三个平台下跑一次 Shader 编译、AssetBundle 构建、烟测场景启动"。

这不是慢一点的问题，是**根本性的不可闭环**。一个 Shader 变体可能在 Android Vulkan 上跑对、Android GLES3 上跑错；一个 AssetBundle 在 iOS 上加载成功、WebGL 上因为内存限制 OOM。这些差异在 AI 改完代码的当下，没有任何快速验证手段能告诉它"你这次改动是不是又踩到了某个平台的 corner case"。

这里有两个具体后果：

第一，**Harness 必须接受"一次任务的 Verify 是异步的"**。不像 Web 后端那样写完跑测试当场就能 verify，游戏项目里一次 AI 改动的 verify 可能要等下一次夜构、下一次出包、甚至下一次玩家上报。Harness 状态机里必须有"等异步 verify"这个状态。

第二，**Memory 层必须沉淀平台/Variant 相关的事故**。AI 改完一个 Shader 不知道 Android GLES3 会炸，是因为它没有"上一次改这个 Shader 时炸过"的记忆。必须把这类事故按"平台 + Variant + 触发条件"沉淀进领域知识，下次类似改动时主动加 warning。

通用 v0 在这里需要补的是：Verify 层显式分"同步可闭环"和"异步必须等"两类，Memory 层按平台维度组织事故库。

### 第四堵墙：资源系统的隐式依赖图

游戏项目里有一个 Web 后端没有的概念：**资源依赖图**。一个 prefab 引用一个 ScriptableObject，那个 SO 引用一组 Material，Material 引用 Shader 和 Texture，Texture 在 Addressables 里被分到某个 group——这一整套关系不是写在代码里的，是写在 meta 文件的 GUID 引用和 Addressables 配置里。

AI 改代码的时候，看不到这张依赖图。它改了一个脚本的字段名，IDE 帮它修了所有引用这个字段的地方——但没人告诉它，有 12 个 prefab 上挂着这个脚本，每个 prefab 里这个字段都需要在 Inspector 上重新挂值。它改了一个 Shader 的 property 名字，Material 里的引用也跟着失效，但 Material 是二进制 yaml，AI 默认不会去更新。

这堵墙的特点是：**违反它的代价滞后出现**。AI 改完代码当场 build 还能过——因为字段名变了，Inspector 上原值变成默认值，C# 编译没问题。问题要等运行到那个 prefab 的时候才暴露，可能是 QA 测了一周才发现某个 UI 的颜色突然不对。

通用 v0 在这里需要补的是：Rules 层加一类"涉及 SerializedField 改名"的特殊任务流程，强制 AI 在改这类字段前先做"影响面扫描"——列出所有引用这个字段的 prefab / scene / SO，提醒人去手动重新挂值或写迁移脚本。

### 第五堵墙：Unity / Unreal 大版本的语义漂移

通用 Web 后端项目升级框架版本，多数时候是 deps 升一下、跑一遍测试、改几处 deprecated 调用。

游戏项目升 Unity / Unreal 大版本，是另一个量级的事：

- API 行为可能从 v 到 v+1 静默变化（Coroutine 在 disabled GameObject 上的行为、UnityWebRequest 的默认超时、Shader 编译的 keyword 限制）
- Best practice 在版本间漂移（Unity 2022 推荐用 IL2CPP，Unity 6 推荐 IL2CPP + GraphicsBuffer 替代 ComputeBuffer 的某些用法）
- 第三方 Package 的最低版本要求漂移（HybridCLR 跟 Unity 版本绑得很紧，URP 14 / 17 之间的 RenderGraph 行为大不一样）

Harness 的 CLAUDE.md 和 Skill 里写的规则，本质上是**对某一个 Unity 版本 + Package 版本组合的快照**。版本一升，这些规则有一半要回头审。AI 不会主动告诉你"这条规则在 Unity 6 下可能不再适用"——它只会按你写的规则做事，然后写出在新版本下莫名其妙报错的代码。

通用 v0 在这里需要补的是：CLAUDE.md 顶部必须显式声明"基于哪个 Unity 版本 + Package 版本"，每次大版本升级要走一次专门的 Harness 审计流程（这是后面文章会展开的 Drift 主题）。

## 五层模型在游戏引擎客户端的补丁

把五堵墙映射回 [AI 赋能 08]({{< relref "ai-empowerment/ai-empowerment-08-ai-coding-harness-engineering.md" >}}) 的五层模型：

| 层 | 通用 v0 关注的 | 游戏引擎客户端补丁 |
|----|----------------|------------------|
| Context | CLAUDE.md / 项目结构 | 显式声明 Unity / Package 版本快照；显式列出所有生成代码目录 |
| Rules | 不改生成代码、不编造数据 | 不基于私仓 / 闭源源码做断言；改 SerializedField 前先扫影响面 |
| Workflow | Intake → Verify | Verify 显式分"同步可闭环"和"异步等下次构建" |
| Checks | 单测 / 构建 | 多平台 / 多 Variant 的 verify 不能强求一次跑全；接受异步反馈 |
| Memory | 通用事故沉淀 | 按平台 / Variant 维度组织事故库；记录大版本升级时哪些规则要审 |

这五条补丁不是要替换通用 v0——是要加在 v0 之上。如果你还没有 v0，先去搭 v0；如果 v0 已经在跑，就把这五条补丁加进去。

## 这个系列后面要讲什么

第一堵墙到第五堵墙描述的是**静态约束**——它们一直存在、一直需要处理。但 Harness 还有另一组挑战是**动态**的：随着项目演进、随着 Harness 自身长大，它会变臃肿、变腐烂、最后变成新人入职第一周就想绕过的累赘。

这一组动态挑战，外部讲 Harness 的文章基本没碰。下一篇 [v0 之后——Harness 的五阶段生命周期]({{< relref "harness-engineering/harness-engineering-02-five-stage-lifecycle.md" >}}) 开始展开 Bootstrap → Growth → Bloat → Drift → Sunset 这条主线，给出四个可操作的诊断指标。

如果你做 SDK / Package，05 [跨仓库 Harness：SDK vendor 视角]({{< relref "harness-engineering/harness-engineering-05-cross-repo-sdk-vendor.md" >}}) 是另一条独立线，可以跳读。

## 收束

游戏引擎客户端不是 AI Coding 的禁区。AI 在这个领域能接的活其实比想象的多——UI 业务逻辑、配置表处理、协议层、工具脚本、Editor 扩展、测试用例。

但**通用方法论搭起来的 Harness 在这个领域是不够用的**。五堵墙不会因为你换了一个更强的模型就消失，也不会因为你把 CLAUDE.md 写得更长就被自动绕过。它们是领域本身带的、需要在 Harness 设计阶段就显式应对的东西。

最短结论是：

**先按通用 v0 把 Harness 搭起来。然后用这五堵墙做一次审计，看看你的 Harness 在每堵墙前面有没有显式的应对。没有的话，补上。**

<!-- DATA-TODO: 补 1-2 个真实事故的简短叙述——可以匿名化——展示这五堵墙的某一堵在没做 Harness 应对时怎样造成生产事故。建议从 AssetBundle / Shader Variant / SerializedField 改名 这三类里挑。 -->

<!-- EXPERIENCE-TODO: 在"五层模型在游戏引擎客户端的补丁"那个表格之后，补一段"我自己的项目当前在每堵墙上的应对状态"，作为活的诊断范例。可以参考 ai-empowerment 08 的写法。 -->
