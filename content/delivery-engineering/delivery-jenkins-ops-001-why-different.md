---
date: "2026-04-26"
title: "为什么游戏团队的 Jenkins 是另一个物种"
description: "通用 CI 的最佳实践搬到 Unity 第一周就会教做人。游戏团队 CI 在产物体积、资源稀缺、反向链路、构建主角四个维度上和 Web/服务端跨数量级——本篇用一篇文章论证差异，给整个系列定调。"
slug: "delivery-jenkins-ops-01-why-different"
weight: 1571
featured: false
tags:
  - "Delivery Engineering"
  - "CI/CD"
  - "Jenkins"
  - "Unity"
series: "游戏团队 Jenkins 实战"
series_id: "delivery-jenkins-ops"
series_role: "article"
series_order: 10
delivery_layer: "principle"
delivery_volume: "V16"
delivery_parent_series: "delivery-cicd-pipeline"
delivery_reading_lines:
  - "L1"
  - "L2"
  - "L4"
leader_pick: true
---

## 通用 CI 经验在哪里失效

> 服务端 CI 挂了重跑代价是 5 分钟。我们这边重跑一次相当于今天发版没了。

新来的 DevOps 同事第一次接手游戏团队的 Jenkins，往往第一周就会遇到三个困惑：

- 流水线为什么跑这么久？
- License 是什么——为什么调度器排了一堆队？
- 改了一张图，构建为什么全量重来？

这些不是教程能教会的细节，也不是 Jenkins 配置层面的问题。这是**两类工程的物理差异**——通用 Web/服务端 CI 假设的世界，和游戏团队 CI 实际运行的世界，在四个维度上跨数量级不同：

1. **物理约束**：产物体积和构建时长跨 10²-10³ 倍
2. **资源稀缺**：Unity License 是受激活配额限制的稀缺资源，不是任意并行
3. **反向链路**：CI 不再是"构建到部署"的单向流程，要支撑"线上 crash → 反向找符号表"的反向定位
4. **主角反转**：构建里代码编译只是配角，资源构建（AssetBundle / 烘焙 / Shader 变体）才是大头

通用 CI 的最佳实践——`actions/cache` 缓存依赖、`retry(3)` 自动重试、`archiveArtifacts` 全量归档——搬到 Unity 第一周就会教做人。这一篇用四组维度的对比，把"为什么不能照搬"系统化论证一次。论证完，你才有耐心看后面 15 篇具体怎么做。

---

## 物理约束：体积与时长

先看数字。

| 指标 | 通用 Web/服务端 CI | 游戏团队 Unity CI | 跨越 |
|------|-------------------|------------------|-----|
| 单次构建产物 | jar / Docker：10MB-1GB | 单平台 build：1-10 GB；多平台合并：10-50 GB | 10²-10³ 倍 |
| 单次构建时长 | Maven / npm / Docker：1-15 分钟 | Unity 全量 + IL2CPP + 烘焙：30-120 分钟；多平台串行：2-8 小时 | 10-100 倍 |
| Workspace 占用 | 源码 + node_modules：100MB-2GB | 源码 + Library + 中间产物：5-50 GB；含归档：50-200 GB | 10-100 倍 |
| 失败重试代价 | 1-15 分钟（成本可忽略） | 30-180 分钟（半个工作日延迟） | 10-50 倍 |

跨数量级的差异不是"麻烦一点"，而是把通用经验从对的变成错的。三个典型场景：

**第一个：缓存命中策略错位。** 通用 CI 教你用 `actions/cache`，key 用 `package-lock.json` 的 hash。直接搬到 Unity——把 `Library/` 目录的 hash 当 key——你会发现命中率从期望的 90% 跌到 5%。原因是 Unity Library 里包含 GUID + 时间戳 + 绝对路径，**不可重现**：同样的源码两次 reimport 出来的 Library 二进制不一致。结果是缓存不命中、缓存上传带宽反而拖慢构建。

**第二个：失败重试盲目化。** 通用 CI 教你 `retry(3)`，因为 flaky 测试和网络抖动是常态。搬到 Unity：build stage 加 `retry(3)`。问题是 Unity build 失败往往是 license 占用 / 磁盘 / OOM——这些不是 transient 错误。重试 3 次相当于把同一个故障乘以 3 倍代价：90 分钟变 4.5 小时。最严重的版本：流水线半夜挂，第二天发现 retry 把 build farm 占了一晚上，全队上午无法发版。

**第三个：产物归档无清理。** 通用 CI 默认 `archiveArtifacts` 全量归档，磁盘策略是"无所谓"。搬到游戏团队：每次 build 归档 5-20 GB 产物，`JENKINS_HOME/jobs/<job>/builds/` 在两周内涨到 TB 级。然后某一天 Master 写 `build.xml` 失败——磁盘满了——整个 Jenkins 卡死。"清理 5 年前的 build artifacts 花了一整个周末"是大公司 build farm 的常见故事。

这些故障的根因都是同一个：**产物体积大 → 缓存策略复杂 → 归档清理压力大 → 失败代价高**。具体的治理方案在 203 Workspace 与产物的磁盘治理 和 205 Jenkins 自身的可观测性 展开。

---

## 资源稀缺：License 与平台矩阵

通用 CI 的并发模型是水平扩展的——Agent 不够就加机器，没什么神秘的。游戏团队不行。

| 指标 | 通用 CI | 游戏团队 |
|------|--------|---------|
| 并发上限 | Agent 数决定，水平扩展（20-200 并发常见） | 受 Unity license seat 约束（典型 10-50 seat） |
| License 复杂度 | 编译器免费，Docker 免费 | Unity Pro 是激活式 license（不是 floating），有锁、不释放问题 |
| 平台矩阵 | 一份 Docker 跑所有 Linux | iOS / Android / WebGL / Console 各自独立产物，**资源不能共享 workspace** |
| 平台 Agent 要求 | 任意 Linux runner | iOS 必须 macOS（且 Xcode 版本绑定）；Console 平台需要专属机和 NDA SDK |

这些约束让 Agent 调度从通用 CI 的"找空闲机器"变成"调度受约束的稀缺资源"。三个典型故障：

**License 泄漏。** Unity license 是激活式的——你每次启动 Unity，license 服务器记录一个 seat 占用；正常退出会释放，但 build 进程崩溃 / Agent 强杀时 seat 不会释放。结果是 build farm seat 100% 占用，但实际只有 30% 在跑 build，其余是泄漏的"幽灵激活"。新 build 排队几小时，全队卡住。半夜 license 全部被泄漏 job 占了，第二天全组发不了版本——这是大公司游戏团队都踩过的坑。解药是流水线 `post { always { unityReturnLicense() } }`，但 Unity 官方文档把"Returning Activations"章节藏在角落，很多教程不告诉你这一点。

**Workspace 资源冲突。** 通用 CI 的 `parallel { stage('iOS')...; stage('Android')... }` 在共享 workspace 下没问题——Linux 文件系统 + 编译器是无状态的。但 Unity 不是：iOS 和 Android build 的 `Library/` 内容不同（Texture 压缩格式不同、Player Settings 不同），同 workspace 跑两个平台 → Library 来回 reimport → 一次 30 分钟的 build 变成 2 小时。新人配 `parallel` 期望加速，结果**比串行慢 40%**，用户投诉构建变慢——这是参数化模板里最常见的错误。

**macOS Agent 的"假并发"。** 通用 CI 经验：标签 `mac` 一打，调度器自动分配。Unity 现实：macOS 机贵（Mac Mini M2 ~ ¥10000）+ Apple 限制虚拟化 → 团队通常只有 2-5 台物理 Mac → 同台机不能并发跑 iOS build（Xcode 锁、license seat、磁盘）。标签是 `mac` 但实际可用并发是 1/机，调度器把多个 job 排到同一台 → 排队 / 卡死。

具体的 License 池治理见 301 Unity License 池管理，Agent 调度策略见 202 Agent 调度与标签体系，多平台并行隔离见 304 多平台并行打包与隔离。

---

## 反向链路：符号表与崩溃栈

通用 CI 的产物链路是单向的：构建 → 部署 → 用户。出了错看服务端日志，traceback 自带文件名行号——一切线上调试问题在服务端就能闭环。

游戏团队不一样。产物部署到用户设备后，CI 还要继续负责一件事：**线上崩溃栈的反向定位**。

| 指标 | 通用 Web/服务端 | 游戏团队 |
|------|---------------|---------|
| 错误诊断链路 | 服务端日志 + traceback（自带文件名行号） | 用户设备 crash → 上报 stripped 栈帧 → CI 归档符号表反向还原 → 才有文件名 |
| 符号文件大小 | 通常无 / 几 MB | iOS dSYM：200MB-5GB / 单平台 / 单次 build；Android symbols：100-500 MB |
| 符号保留期 | 通常不保留 | 6-12 个月（线上 crash 可能在发版几周后才暴露） |
| CI ↔ 监控耦合 | 弱（监控独立） | 强（监控系统拉 CI 归档的符号表） |

三个典型故障：

**符号表丢失。** 通用 CI 经验：archive 主程序就够了，调试信息可以剥离。Unity 现实：IL2CPP 把 C# 转 C++ 后再编译，链路是 `C# → cpp → bin + dSYM/symbols.zip`。如果 dSYM 没归档，线上 crash 栈完全是地址——`0x00007fff8c2b1234`。用户报"游戏闪退"，永远不知道是哪行代码。最常见的触发：pipeline 默认 archive `*.ipa` / `*.apk`，没人记得加 `*.dSYM.zip` / `symbols.zip`。

**符号表与产物版本错位。** 通用 CI 用 build number 标识 artifact 就够了。Unity 现实：dSYM 必须和**完全相同的 binary** 对应，**rebuild 同一个 commit 生成的 dSYM 不一样**——地址会变。发版后为了修一个小问题重新 rebuild 同 commit，新 dSYM 覆盖旧的 → 老用户的 crash 栈从此无法还原。我们花了一周才搞清楚为什么同版本号的 dSYM 不能互换。解药是：dSYM 必须和 binary 在同一次 build 内归档 + 永久不可覆盖。

**历史符号表清理过度。** 通用 CI 配 "超过 30 天 artifact 自动清理"。游戏团队踩坑：玩家可能 2 个月不更新 → 旧版本 crash 2 个月后才上来；自动清理已经把符号表删了。结果是发现一个老版本严重 bug，符号表已被清理，无法定位。Crashlytics 后台的 "Symbol Files" 页面经常有一片灰着——那是没传上去的符号表对应的 build。

具体的符号归档链路见 303 符号表与崩溃栈：IL2CPP 产物的符号链路。

---

## 主角反转：资源 vs 代码

通用 CI 里"构建"约等于"代码编译"——70-90% 的构建时间是 javac / tsc / cargo。游戏团队不是。

| 阶段时长占比 | 通用 Web | Unity 游戏 |
|------------|---------|----------|
| 代码编译 | **70-90%** | 30-50%（含 IL2CPP） |
| 资源处理 | 0-10% | **40-60%**（AssetBundle、烘焙、Texture 压缩、Sprite 打包） |
| Shader 变体编译 | N/A | 5-15%（变体多时飙升） |
| 平台后处理 | 5-10%（Docker layering） | 5-10%（IPA 签名、APK 对齐） |

构建主角是资源，不是代码——这一个反转改变了 CI 设计的几乎全部决策：

**"什么算构建变化"重新定义。** 通用 CI 里 `git diff` 决定要不要重新 build——代码没变就跳过。Unity 不行：改一个 Prefab 可能让上千 AssetBundle 的依赖图重算，看起来"代码没变"但 build 必须全量。CI 触发条件按"代码变化"配置，结果资源变更被遗漏，跑出来的 build 用旧资源——美术觉得 CI 慢是程序员不会写代码，程序员觉得 CI 慢是美术不会做 prefab。最后发现是 import settings 写得有问题。

**"什么进缓存"重新定义。** 通用 CI 缓存 `node_modules`，命中率几乎 100%。Unity 想缓存 Library 共享出去——工程师机器上好用，build 机拉下来全量 reimport，比不缓存还慢。原因是 Library 含绝对路径和机器特定 metadata。**远程共享 Library 几乎不可行**，只能本地持久化。

**"增量构建"在 CI 上不一定是增量。** Library 缓存对 agent 切换 / workspace 路径变化敏感——一次切换 → Library 失效 → 全量 reimport（1-2 小时）。CI 时长上下波动，team 怀疑机器有问题，实际是缓存随机失效。

**Shader 变体爆炸是最隐蔽的版本。** 同 commit 不同环境（player settings）出来的 shader 数量可以差 10 倍。同一份代码，build 时长在不动代码的情况下从 30 分钟变 90 分钟，磁盘也跟着膨胀——直到我们写了 variant analyzer 才能解释。

具体的 Shared Library 抽象见 102 Shared Library 设计，并行模式与依赖管理见 105 Jenkins 下的并行模式，资源构建隔离见 304 多平台并行打包与隔离。

---

## 这些差异系统化之后：本系列怎么响应

读到这里你应该意识到：上面四类差异不是孤立的。它们之间相互放大，构成一组反馈环：

```
[资源是构建主角]
   │
   ├─→ [产物巨大]
   │     │
   │     ├─→ workspace 巨大
   │     ├─→ 缓存策略复杂
   │     └─→ 归档清理压力大 ──→ [符号归档压力进一步放大]
   │
   ├─→ [资源平台敏感]
   │     │
   │     └─→ workspace 不能共享
   │            │
   │            └─→ 平台矩阵 = 真实并发 ─→ [License seat 消耗放大]
   │                                         │
   │                                         └─→ [构建时长 × seat 占用 = 吞吐瓶颈]
   │
   └─→ [增量构建不可靠] ──→ [构建时长高方差]
```

三个系统层面的不变量：

1. **吞吐瓶颈 = License × 时长**：单 seat × 1-2 小时单次 build → 一天最多 8-12 build/seat。10 人团队、20 个产品分支、每天 50 次提交——必然排队。
2. **磁盘 = 产物 × 平台数 × 历史保留期**：10 GB × 5 平台 × 30 天 = 1.5 TB / 单 job 的 build 历史。
3. **符号归档 = 平台 × 月数 × 版本数**：6 个月线上版本生命周期 × 5 平台 × 2 个 hotpatch/月 = 60 套独立符号表必须可访问。

为什么单维度优化都不够：

- **只优化时长**（多 agent 并发）→ 被 License 限制
- **只优化磁盘**（激进清理）→ 丢符号表 → 线上 crash 无法定位
- **只优化 License**（买更多 seat）→ 被 macOS 物理机数量 + workspace 资源冲突限制
- **只优化平台矩阵**（多 workspace）→ 磁盘指数膨胀

这正是本系列三子组划分的依据：

- **Part 1 流水线架构**（5 篇）—— 让 Jenkins 的逻辑能撑住"多产品 × 多分支 × 多平台"的矩阵复杂度
- **Part 2 稳定性运维**（5 篇）—— 让 Jenkins 这台机器自己别先挂（Master 瓶颈、磁盘、升级、可观测性）
- **Part 3 Unity 特化集成**（5 篇）—— 让 Unity 的特殊性吃下来（License、大仓库、符号表、并行隔离、IL2CPP）

下一步从 101 Declarative vs Scripted Pipeline：游戏团队的选型取舍 开始进 Part 1。如果你是 TA / 构建工程师，可以直接跳到 301 Unity License 池管理——那是大多数游戏团队最早撞墙的地方。
