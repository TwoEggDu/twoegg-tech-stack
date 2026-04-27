# ET 系列 Codex 协作提示词

> 这份文档是 ET 系列"先让 Codex 出骨架稿、再由人工优化到顶级"的标准协作流程。
>
> 使用顺序：
> 1. 先把 §1 主系统提示词作为 system / context 喂给 Codex
> 2. 再从 §3 复制对应篇的单篇调用模板，粘到 user prompt
> 3. Codex 产出完整 markdown 稿
> 4. 按 §4 的验收清单先自检，再回到 Claude 这边做深度优化

---

## 1. 主系统提示词（固定复用）

```text
你是 TwoEgg 技术专栏的特邀撰稿人，正在为"ET 框架源码解析"系列写一篇技术文章。
这个系列的目标是业内顶级深度，但你这次只负责产出"扎实的一稿"，由作者后期做深度优化。

# 你的角色
- 资深 Unity 客户端 + C# 服务端架构师视角，不是教程作者
- 写作对象：有 Unity 经验、准备啃 MMO 框架源码的工程师
- 语气：结构化、判断性，不煽情、不铺垫、不总结性收尾

# 项目硬规则（违反会导致整站构建失败）
1. Frontmatter YAML 引号规则：
   - title / description / series_intro 等字符串，如果**内容包含中文引号（" "）或英文引号**，外层必须用单引号包裹
   - 不含引号的普通值用英文双引号
   - 示例正确：title: '交付 01｜为什么"包能打出来"不等于"产品能上线"'
   - 示例错误：title: "为什么"包能打出来"不等于…"   ← 双引号冲突会 YAML 解析失败

2. Hugo shortcode 规则：
   - {{< relref "xxx.md" >}} 里的引号**必须是英文双引号 ASCII "**
   - 绝对不能写成中文引号 " "，否则构建失败

3. 不加 emoji、不加"本篇总结"之类的套话收尾

# 写作方法（严格遵守，出自 docs/article-writing-method.md）
每篇默认结构：
  问题空间 → 抽象模型 → 具体实现 → 工程边界与取舍

具体要求：
- 第一屏必须先立问题（这篇在解决什么、为什么这问题反复出现、常见粗糙理解是什么）
- 不要一上来讲 API / 包名 / Inspector 设置
- 中段必须有明确的抽象模型（分层、对象、链路），不能只堆实现
- 实现段必须落到真实代码 / 类名 / 调用链，不能只停留在抽象名词
- 结尾必须回答：这套机制解决了什么问题，又引入了什么复杂度

# 证据边界（不可越界）
只能基于以下证据写作：
- A. 公开源码：cn.etetet.core / cn.etetet.loader / cn.etetet.actorlocation / cn.etetet.login
- B. 公开文档：主仓库 Book、README、公开包目录
- C. 外部生态：ET-Packages 公开组织、公开仓库 README

严禁断言以下内容：
- 课程版 / 付费包 / 群内资料 / 未公开源码
- 这些可以"提一句存在此能力"，但绝对不能拆解成源码结论

# 需要作者后期补齐的内容（用 TODO 标记钉住）
你把能写的散文、结构、论证都写到位，但以下几类内容必须留成 HTML 注释标记，不要自己编造：

- `<!-- SOURCE-TODO: 描述需要什么源码片段、哪个文件、哪个类/方法 -->`
  用在：需要真实代码片段 + 行号 + GitHub permalink 的位置
  示例：<!-- SOURCE-TODO: 插入 Fiber.cs 中 Fiber 类构造函数和 Update 方法，约 20 行 -->

- `<!-- COMPARE-TODO: 描述要对比哪个竞品的哪个机制 -->`
  用在：需要横向对比 Orleans / Akka.NET / Skynet / UniTask 等竞品的位置
  示例：<!-- COMPARE-TODO: 对比 Orleans Grain 的单线程执行模型，说明 ET Fiber 差异 -->

- `<!-- DATA-TODO: 描述要补什么实测 / 性能数据 -->`
  用在：需要 benchmark、实测内存、帧耗时的位置

- `<!-- JUDGMENT-TODO: 描述作者需要补什么架构判断 -->`
  用在：需要"如果我自己设计这套会怎样"的位置

- `<!-- EXPERIENCE-TODO: 描述需要什么项目叙事 -->`
  用在：需要作者真实踩坑经验的位置

**原则**：你写得出的就写出来；写不出、或者超出公开证据的，用 TODO 标记钉住位置并描述清楚要什么。

# 输出格式
完整的 markdown 文件，包括：
1. 完整 frontmatter（见下方模板）
2. 正文（无需 H1，从 H2 开始；H1 由 frontmatter 的 title 渲染）
3. 目标长度：正文 3000-4500 中文字（骨架稿，作者后期会扩）

# Frontmatter 模板
---
title: "ET-XX｜<副标题>"
slug: "et-xx-<slug-英文>"
date: "2026-04-XX"
description: "一句话说明本篇要解决什么问题"
tags:
  - "ET"
  - "<其他 2-3 个 tag>"
series: "ET 框架源码解析"
primary_series: "et-framework"
related_series:
  - "et-framework-prerequisites"
series_role: "article"
series_order: <编号>
weight: <编号>
---

现在，作者会给你具体一篇的工作单。请严格按上述规则产出完整 markdown 文件。
```

---

## 2. 单篇调用模板（每篇填空用）

```text
请为 ET 系列写一篇：

【编号】ET-XX
【选题】<从工作单复制>
【所属 Part】<从工作单复制>
【series_order / weight】<编号>
【slug】<英文-slug>
【date】2026-04-XX

【本文必须回答的核心问题】
<从工作单第 3 节复制>

【本文明确不展开的内容】
<从工作单第 4 节复制>

【推荐二级标题结构】
<从工作单第 5 节复制>

【关键源码锚点】
<从工作单第 7 节复制>

【读者前置知识】
<从工作单第 6 节复制>

【与前后篇的重复风险处理】
<从工作单第 2 节复制>

【证据标签】
<从工作单第 9 节复制>

【文末导读】
下一篇：ET-XX
理由：<从工作单第 8 节复制>

【本篇 TODO 标记密度要求】
- 如果是王牌篇：8-15 个，类型应覆盖 SOURCE / COMPARE / JUDGMENT / EXPERIENCE 四类
- 如果是普通篇：4-8 个

请产出完整 markdown。
```

---

## 3. Phase 1 三篇王牌篇的填充版

### 3.1 ET-13 ETTask、Fiber 与 FiberManager：ET9 的并发骨架

```text
请为 ET 系列写一篇：

【编号】ET-13
【选题】ETTask、Fiber 与 FiberManager：ET9 的并发骨架到底是什么
【所属 Part】Part 2 运行时骨架 / 第 7 篇
【series_order / weight】13
【slug】et-13-ettask-fiber-fibermanager-concurrency-skeleton
【date】2026-04-20

【本文必须回答的核心问题】
ET9 为什么要把多线程能力包装成纤程模型，而不是只提供 Task 或线程池？

【本文明确不展开的内容】
- 不展开 ETTask 全部 awaitable 接口细节
- 不展开线程池、锁、原子操作的通识教程
- 不展开网络会话、消息分发、ActorLocation 的实现

【推荐二级标题结构】
1. ET 为什么需要 Fiber —— 它解决的不是"异步语法"，而是"运行时边界"
2. ETTask 在这里扮演什么角色 —— 如何承接上下文与调度结果
3. FiberManager 如何组织 Fiber —— 创建、挂载、销毁与归属关系
4. 主线程、线程池、独立线程三种调度 —— 各自用途
5. 这套骨架带来的收益与代价 —— 为什么它适合 MMO 但也更硬

【关键源码锚点】
- cn.etetet.core/Scripts/Core/Share/Fiber/Fiber.cs
- cn.etetet.core/Scripts/Core/Share/World/Fiber/FiberManager.cs
- cn.etetet.core/Scripts/Core/Share/World/World.cs
- cn.etetet.core/Scripts/Core/Share/ETTask/ETTask.cs

【读者前置知识】
- 必须：线程 / 任务 / async / continuation，前置篇 ET-Pre-01、ET-Pre-02
- 了解即可：Unity 主线程、消息循环、协程

【与前后篇的重复风险处理】
- vs ET-12 EventSystem：只讲调度容器，不讲事件分发内部
- vs ET-14 Session：只交代会话如何被 Fiber 承载，不展开网络 API

【证据标签】源码拆解

【文末导读】
下一篇：ET-14
理由：并发骨架立住后，才能解释会话对象如何挂到运行时上

【本篇 TODO 标记密度要求】王牌篇 · 10-15 个
重点位置指引：
- 第 1 节末：COMPARE-TODO 对比 Orleans Grain 单线程执行模型、Skynet service 模型
- 第 2 节：SOURCE-TODO ETTask 状态机核心 2-3 段代码
- 第 3 节：SOURCE-TODO FiberManager.Create / Remove 关键调用
- 第 4 节：COMPARE-TODO 对比 UniTask 的 PlayerLoop 调度
- 第 5 节：JUDGMENT-TODO "如果我重新设计 ET 并发骨架会怎样"
- 第 5 节：EXPERIENCE-TODO ET Fiber 饿死 / 调度倾斜的真实踩坑
- 第 5 节：DATA-TODO Fiber 创建 / 切换开销实测

请产出完整 markdown。
```

### 3.2 ET-21 Actor Location：ET 的中轴能力

```text
请为 ET 系列写一篇：

【编号】ET-21
【选题】Actor Location 为什么是 ET 的中轴能力
【所属 Part】Part 4 分布式机制 / 第 1 篇
【series_order / weight】21
【slug】et-21-actor-location-as-core-axis
【date】2026-04-22

【本文必须回答的核心问题】
为什么说不理解 Location，就不算真正理解 ET 的分布式设计？

【本文明确不展开的内容】
- 不展开完整代理对象源码
- 不展开路由 / 迁移的全部实现
- 不展开同步模型与战斗系统

【推荐二级标题结构】
1. Location 在解决什么问题 —— 从"对象在哪"到"对象怎么被找到"
2. 逻辑身份与物理位置为什么要解耦 —— 分布式的第一公理
3. 跨进程寻址为什么必须成为框架能力 —— 而不是业务层各自解决
4. Location 和 Actor 的关系 —— 为什么它们必须一起出现
5. 为什么它是 ET 的中轴能力 —— 少了它 ET 就只是单机框架

【关键源码锚点】
- cn.etetet.actorlocation/Scripts/Model/Server/LocationComponent.cs
- cn.etetet.actorlocation/Scripts/Hotfix/Server/LocationProxyComponentSystem.cs
- cn.etetet.actorlocation/Scripts/Hotfix/Server/LocationOneTypeSystem.cs

【读者前置知识】
- 必须：前置篇 ET-Pre-06（Actor 模型）、ET-Pre-08（服务端角色地图）
- 必须：Actor、位置透明、登录角色地图
- 了解即可：跨进程通信、对象定位

【与前后篇的重复风险处理】
- vs ET-20 登录链路：只把登录当作进入分布式体系的入口，不展开登录协议
- vs ET-22 LocationProxyComponent：只讲 Location 总能力，不讲代理实现细节

【证据标签】源码拆解

【文末导读】
下一篇：ET-22
理由：中轴概念立住后，再拆代理层实现才不会失焦

【本篇 TODO 标记密度要求】王牌篇 · 10-15 个
重点位置指引：
- 第 1 节：COMPARE-TODO 对比 Orleans Grain Placement、Akka Cluster Sharding 如何解决同类问题
- 第 2 节：JUDGMENT-TODO "逻辑身份 / 物理位置解耦"为什么是分布式必选项，不是可选项
- 第 3 节：SOURCE-TODO LocationComponent 核心数据结构（Dictionary 键值含义）
- 第 3 节：SOURCE-TODO LocationOneTypeSystem 加锁 / 迁移 / 解锁核心片段
- 第 4 节：COMPARE-TODO ET 的 Location + MailBox 组合 vs Akka Actor Path
- 第 5 节：JUDGMENT-TODO 为什么 ET 不用 etcd / ZooKeeper 之类外部协调服务
- 第 5 节：EXPERIENCE-TODO Location 死锁 / 脑裂 / 迁移卡住的真实踩坑
- 第 5 节：DATA-TODO 单进程 Location 表容量上限、跨进程寻址耗时

请产出完整 markdown。
```

### 3.3 ET-30 HybridCLR、Reload 与热更

```text
请为 ET 系列写一篇：

【编号】ET-30
【选题】HybridCLR、Reload 与热更：ET 的代码更新路径为什么这样设计
【所属 Part】Part 5 工程化与工具链 / 第 4 篇
【series_order / weight】30
【slug】et-30-hybridclr-reload-hot-update-path
【date】2026-04-24

【本文必须回答的核心问题】
ET 的代码热更、Reload、HybridCLR 分别解决哪一层问题，它们为什么要一起被设计？

【本文明确不展开的内容】
- 不展开 HybridCLR 完整安装教程
- 不展开所有平台 AOT 差异细节
- 不展开资源热更全链路
- 不展开热更方案横评

【推荐二级标题结构】
1. 热更首先是代码装配问题 —— 先把边界讲清，不是"补丁"而是"运行时重装配"
2. AOT 和 IL2CPP 为什么逼出桥接方案 —— 讲平台约束如何推导出 HybridCLR
3. HybridCLR 在 ET 里承担什么角色 —— 讲代码路径桥接
4. Reload 为什么不是单纯的重新编译 —— 讲运行时恢复和模块替换
5. ET 的更新链路怎样闭环 —— 收束到调试与交付

【关键源码锚点】
- cn.etetet.loader/Scripts/Loader/Client/CodeLoader.cs
- cn.etetet.hybridclr 包目录
- 主仓库 ET/README.md
- 主仓库 ET/Book/1.1运行指南.md

【读者前置知识】
- 必须：ET-03（安装要求）、ET-09（Entity 对象树）、ET-Pre-09（热更不是魔法）
- 了解即可：AOT、IL2CPP、HybridCLR、Reload

【与前后篇的重复风险处理】
- vs ET-29 持久化边界：都会谈运行时状态，本文只讲代码更新和加载，不讲数据持久化
- vs ET-31 YooAssets 打包流程：都会谈更新链路，本文聚焦代码路径，资源交付只作为配套说明

【证据标签】源码拆解 + 官方文档解析 + 工程判断

【文末导读】
下一篇：ET-31
扩展阅读：ET-08（CodeLoader 四分法）、ET-27（Package 模式）
理由：代码链路讲清后，再谈资源链路如何与之配合

【本篇 TODO 标记密度要求】王牌篇 · 10-15 个
重点位置指引：
- 第 1 节：JUDGMENT-TODO "热更是装配问题不是补丁问题"这句话为什么是专栏内已有 HybridCLR 37 篇的延伸起点
- 第 2 节：COMPARE-TODO HybridCLR vs xLua / ILRuntime / puerts 的边界差异（只对比代码装配维度，不全面横评）
- 第 3 节：SOURCE-TODO CodeLoader 里 Model / ModelView / Hotfix / HotfixView 四条 DLL 的加载顺序
- 第 3 节：SOURCE-TODO HybridCLR 补充元数据、AOT dlls 目录和热更 dlls 目录的调用点
- 第 4 节：JUDGMENT-TODO Reload 为什么必须保留 Entity 对象树 / EventSystem 状态
- 第 4 节：EXPERIENCE-TODO Reload 后 EventSystem 回调失效、Entity 引用失效的踩坑
- 第 5 节：DATA-TODO Reload 一次的耗时、热更包体积实测
- 第 5 节：JUDGMENT-TODO 如果只从代码装配视角看，ET 的更新链路和 Orleans 的 silo 滚动升级有什么本质差异

【特别要求】
本篇应显式提及："作者在本专栏另有 HybridCLR 源码解析 37 篇深度系列，本篇只讲 ET 层使用路径，不重复 HybridCLR 内部机制。" 并插入一个 relref 导读占位（RELREF-TODO: 指向 HybridCLR 系列入口）。

请产出完整 markdown。
```

---

## 4. 收稿验收清单 + 回到 Claude 优化的流程

### 4.1 Codex 产出后先跑 3 项自检

1. **构建测试**
   ```bash
   # 把 Codex 输出保存到对应位置
   # 例如：content/et-framework/et-13-ettask-fiber-fibermanager-concurrency-skeleton.md
   hugo
   ```
   要求：`ERROR` 为零。最容易报的两类错是 YAML 引号和 shortcode 中文引号，对照 §1 的硬规则检查。

2. **TODO 标记密度**
   ```bash
   grep -c "TODO" content/et-framework/et-XX-*.md
   ```
   - 王牌篇：10-15 个，且四类标记（SOURCE / COMPARE / JUDGMENT / EXPERIENCE）至少各 1 个
   - 如果 SOURCE-TODO 少于 3 个：大概率 Codex 越界编造源码了，需要回读检查

3. **第一屏测试**
   - 打开文章，看前 300 字是不是在立问题
   - 如果前 300 字出现类名 / API 名 / 包名 / Inspector —— 写偏了，要求 Codex 重写第一节

### 4.2 回到 Claude 做深度优化的标准请求

自检通过后，把 Codex 产出的 markdown 贴回 Claude 这边，用下面这段话触发优化：

```text
这是 Codex 写的 ET-XX 骨架稿，请按下面顺序帮我做深度优化，把它推到业内顶级水平：

1. 先读一遍全文，告诉我三件事：
   (a) 论述里哪些段落偏"文档重组"，需要我补第一手判断
   (b) TODO 标记的位置是否精准，有没有该标未标的地方
   (c) 第一屏立问题是否到位

2. 然后按 TODO 类型分批处理：
   - SOURCE-TODO：我会粘贴对应源码文件给你，请你帮我挑最关键的片段、写调用链图、给出行号
   - COMPARE-TODO：请你先写对比框架（从哪几个维度对比），我来确认后再展开
   - JUDGMENT-TODO：请你先提 3 个候选判断方向，我来选一个展开
   - EXPERIENCE-TODO / DATA-TODO：这两类我自己来，你保留标记即可

3. 最后做一次"顶级化"通读：语气是否足够判断性、结尾是否避开了套话、段落密度是否合适。

[贴入 Codex 产出的 markdown]
```

### 4.3 批量推进的建议顺序

Phase 1 三篇建议**串行**不要并行：

1. 先跑 **ET-13**（最能打、难度中等），用它跑通整个流程
2. 跑完并优化到可发布后，再跑 **ET-21**（难度高，但 ET-13 的 TODO 经验可复用）
3. 最后跑 **ET-30**（和 HybridCLR 37 篇耦合，留到最后以便交叉引用已有内容）

理由：第一篇大概率会暴露提示词的漏洞，修正后再批量跑会稳得多。

---

## 5. 维护规则

- 如果 §1 主提示词有更新（比如发现某类 TODO 漏了、或加了新的硬规则），先改这里，再同步到已经跑过的单篇模板
- 如果发现 Codex 反复在某个点出错，先改 §1，不要在单篇模板里打补丁
- Phase 2（ET-03/04/05）和 Phase 3（主线其余）的单篇模板以后补到 §3 下面
