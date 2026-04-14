# ET 主系列章节级工作单 Batch C

> 覆盖范围：`ET-25` ~ `ET-36`
> 前置假设：读者已完成 `ET-Pre-01` ~ `ET-Pre-10`
> 用途：把主系列后半段从“章节题目”落到“可直接开写正文的工单”

---

## ET-25｜`StartConfig`、SceneType、进程角色：ET 怎么把服务组织成一组

**系列 / 位置：** ET 框架源码解析 / Part 4：分布式机制（第 5 篇）

### 1. 系列职责

本篇负责把 ET 的服务端从“有哪些进程”讲成“这些进程各自承担什么角色、靠什么配置被组织起来”。

### 2. 与相邻文章的重复风险与处理方式

| 相邻文章 | 重复风险点 | 本文处理方式 |
|---|---|---|
| ET-24：ET 的 Router/软路由应该怎样理解 | 都会碰到跨进程与分发 | 本文只讲进程编排与角色，不展开路由协议和寻址细节 |
| ET-26：Watcher、服务拉起与进程守护 | 都会谈服务端运行态 | 本文聚焦“角色定义与编排”，Watcher 只作为被编排的一环 |

### 3. 本文必须回答的核心问题

`ET 是怎样把 Realm、Gate、Scene 这类角色组织成一组可启动、可配置、可切换的进程体系？`

### 4. 本文明确不展开的内容

- 不展开 Watcher 的守护逻辑实现
- 不展开 Router 的消息转发协议
- 不展开登录流程的完整业务链
- 不展开任何单个 Demo 的端到端代码

### 5. 推荐二级标题结构

1. `为什么服务端不能只靠一个进程`：交代多角色拆分的必要性
2. `StartConfig 在解决什么问题`：讲配置如何定义进程身份与启动方式
3. `SceneType、Realm、Gate 的角色边界`：讲不同进程承担的功能定位
4. `进程角色如何和代码装配接轨`：讲配置如何映射到实际运行入口
5. `ET 为什么要把角色组织成一组`：收束到可维护性、扩展性与运维成本

### 6. 读者前置知识

- 必须掌握：ET-Pre-08 的服务端角色地图、ET-Pre-04 的 Package/asmdef 概念
- 了解即可：分布式服务常见的网关、场景服、守护进程概念

### 7. 关键源码或文档锚点

- `.tmp/ET/Book/8.2ET Package目录.md`
- `.tmp/ET-Packages/cn.etetet.login/Scripts/Hotfix/Server/Realm/C2R_LoginHandler.cs`
- `.tmp/ET-Packages/cn.etetet.login/Scripts/Hotfix/Server/Gate/C2G_LoginGateHandler.cs`
- `.tmp/ET-Packages/cn.etetet.loader/Scripts/Loader/Server/Init.cs`
- `.tmp/ET-Packages/cn.etetet.core/Scripts/Core/Share/World/World.cs`

### 8. 文末导读建议

- 下一篇应读：ET-26
- 扩展阅读：ET-21、ET-24

### 9. 证据标签

- 官方文档解析
- 包目录解析
- 源码拆解

---

## ET-26｜Watcher、服务拉起与进程守护：ET 为什么重视运行态治理

**系列 / 位置：** ET 框架源码解析 / Part 4：分布式机制（第 6 篇）

### 1. 系列职责

本篇负责把 ET 的进程治理从“一个能跑的服务”讲成“一个能被持续拉起、监控、恢复的运行态系统”。

### 2. 与相邻文章的重复风险与处理方式

| 相邻文章 | 重复风险点 | 本文处理方式 |
|---|---|---|
| ET-25：进程角色与配置组织 | 都会谈服务端角色 | 本文只讲守护与运行态治理，不再重新定义角色地图 |
| ET-27：ET9 的 Package 模式到底改变了什么 | 都会谈工程化结构 | 本文只谈运行态治理，不上升到包化组织方式 |

### 3. 本文必须回答的核心问题

`Watcher 为什么不是一个附属工具，而是 MMO 框架里必须有的运行态治理层？`

### 4. 本文明确不展开的内容

- 不展开云原生编排平台细节
- 不展开完整服务发现系统实现
- 不展开具体 watchdog 的底层 API
- 不展开进程间通信协议全景

### 5. 推荐二级标题结构

1. `为什么游戏服务端需要守护进程`：讲拉起、恢复、巡检的现实需求
2. `Watcher 的职责边界`：讲它管什么、不管什么
3. `服务失活后的恢复链路`：讲进程掉线、重启、告警和回收
4. `Watcher 与 StartConfig 的关系`：讲配置如何被治理层消费
5. `运行态治理为何影响框架选择`：收束到工程判断

### 6. 读者前置知识

- 必须掌握：ET-25 的服务端角色地图、ET-Pre-08 的最小服务端角色认知
- 了解即可：守护进程、健康检查、服务发现、重启策略

### 7. 关键源码或文档锚点

- `.tmp/ET/Book/8.2ET Package目录.md`
- `.tmp/ET-Packages/cn.etetet.watcher`（包目录）
- `.tmp/ET-Packages/cn.etetet.startconfig`（包目录）
- `.tmp/ET-Packages/cn.etetet.login`（包目录）

### 8. 文末导读建议

- 下一篇应读：ET-27
- 扩展阅读：ET-25、ET-31

### 9. 证据标签

- 包目录解析
- 官方文档解析
- 工程判断

---

## ET-27｜ET9 的 Package 模式到底改变了什么

**系列 / 位置：** ET 框架源码解析 / Part 5：工程化与工具链（第 1 篇）

### 1. 系列职责

本篇负责把 ET9 的包化迁移从“目录变化”提升为“工程边界、分发边界、阅读边界变化”。

### 2. 与相邻文章的重复风险与处理方式

| 相邻文章 | 重复风险点 | 本文处理方式 |
|---|---|---|
| ET-26：Watcher 与运行态治理 | 都会碰工程化组织 | 本文聚焦包模式，不讲治理层职责 |
| ET-28：SourceGenerator 与分析器 | 都会谈工程约束 | 本文只讲模块边界变化，不展开代码生成机制 |

### 3. 本文必须回答的核心问题

`ET9 改成 Package 模式后，主仓库、公开包和阅读入口分别发生了什么变化？`

### 4. 本文明确不展开的内容

- 不展开完整 Unity Package Manager 教程
- 不展开所有包的详细源码
- 不展开收费包和课程包的具体实现
- 不展开安装问题排障手册

### 5. 推荐二级标题结构

1. `ET9 为什么不能再按单仓库理解`：先纠偏
2. `Package 模式在工程上切了哪些边界`：讲分发、版本、依赖
3. `主仓库现在更像什么`：讲入口工程与初始化工程
4. `公开包如何变成阅读主线`：讲 `cn.etetet.*` 的阅读方法
5. `Package 模式对团队协作的实际影响`：收束到工程收益与代价

### 6. 读者前置知识

- 必须掌握：ET-01、ET-Pre-04
- 了解即可：Unity Package、GitHub Packages、asmdef

### 7. 关键源码或文档锚点

- `.tmp/ET/README.md`
- `.tmp/ET/Book/8.2ET Package目录.md`
- `.tmp/ET/Packages/com.etetet.init/Editor/GitDependencyResolver/DependencyResolver.cs`
- `.tmp/ET/Packages/com.etetet.init/Editor/GitDependencyResolver/MoveToPackages.ps1`

### 8. 文末导读建议

- 下一篇应读：ET-28
- 扩展阅读：ET-01、ET-04

### 9. 证据标签

- 官方文档解析
- 源码拆解
- 工程判断

---

## ET-28｜SourceGenerator 与分析器为什么是 ET 风格的一部分

**系列 / 位置：** ET 框架源码解析 / Part 5：工程化与工具链（第 2 篇）

### 1. 系列职责

本篇负责解释 ET 为什么把“框架约束”前移到生成器和分析器，而不是完全留给手写代码自觉遵守。

### 2. 与相邻文章的重复风险与处理方式

| 相邻文章 | 重复风险点 | 本文处理方式 |
|---|---|---|
| ET-27：Package 模式改变什么 | 都会谈工程化 | 本文只讲约束前移，不讲包化边界本身 |
| ET-29：MemoryPack、MongoBson 与序列化树 | 都会谈工具链 | 本文只讲生成与约束机制，不讲具体序列化格式 |

### 3. 本文必须回答的核心问题

`为什么 ET 要用 SourceGenerator 和分析器把很多模式提前固化，而不是让业务自己手写？`

### 4. 本文明确不展开的内容

- 不展开 Roslyn 底层机制教学
- 不展开所有生成器实现细节
- 不展开源码格式化工具链
- 不展开编译器完整原理

### 5. 推荐二级标题结构

1. `为什么框架会倾向约束前移`：先讲动机
2. `SourceGenerator 解决什么重复劳动`：讲模板化与强约束
3. `分析器解决什么错误输入`：讲静态校验与开发时反馈
4. `ET 的系统类生成思路`：讲系统类、消息类、映射类
5. `生成器与运行时约束如何闭环`：收束到工程纪律

### 6. 读者前置知识

- 必须掌握：ET-Pre-03 的程序集与动态加载、ET-Pre-04 的 Package 边界
- 了解即可：Roslyn、SourceGenerator、静态分析器

### 7. 关键源码或文档锚点

- `.tmp/ET/Book/8.2ET Package目录.md`
- `.tmp/ET/README.md`
- `.tmp/ET-Packages/cn.etetet.core/Scripts/Core/Share/Entity/EntitySystem.cs`
- `.tmp/ET-Packages/cn.etetet.core/Scripts/Core/Share/Entity/EntitySystemOf.cs`

### 8. 文末导读建议

- 下一篇应读：ET-29
- 扩展阅读：ET-11、ET-12

### 9. 证据标签

- 包目录解析
- 源码拆解
- 工程判断

---

## ET-29｜MemoryPack、MongoBson 与序列化树：ET 的数据持久化边界怎么切

**系列 / 位置：** ET 框架源码解析 / Part 5：工程化与工具链（第 3 篇）

### 1. 系列职责

本篇负责把 ET 的数据持久化问题从“用哪个库”改写成“运行时数据树、持久化树、引用恢复边界如何划分”。

### 2. 与相邻文章的重复风险与处理方式

| 相邻文章 | 重复风险点 | 本文处理方式 |
|---|---|---|
| ET-28：SourceGenerator 与分析器 | 都会谈工程化工具 | 本文只讲序列化和持久化边界，不讲生成器机制 |
| ET-30：HybridCLR、Reload 与热更 | 都会谈运行时状态恢复 | 本文只讲数据保存，不讲代码更新 |

### 3. 本文必须回答的核心问题

`ET 为什么要同时面对 MemoryPack、MongoBson 和对象树序列化边界这几类问题？`

### 4. 本文明确不展开的内容

- 不展开 MongoDB 全套使用教程
- 不展开 MemoryPack 协议实现细节
- 不展开数据库 schema 设计总论
- 不展开整套 ORM 对比

### 5. 推荐二级标题结构

1. `持久化首先是边界问题`：先讲为什么不是“存起来”那么简单
2. `MemoryPack 适合回答什么`：讲高性能结构化序列化
3. `MongoBson 适合回答什么`：讲数据库树和补丁视角
4. `对象树如何影响持久化结果`：讲父子、组件、恢复
5. `ET 的序列化边界为什么要单独设计`：收束到框架判断

### 6. 读者前置知识

- 必须掌握：ET-09 / ET-10 的对象树边界、ET-Pre-07
- 了解即可：MemoryPack、MongoDB/BSON、持久化快照

### 7. 关键源码或文档锚点

- `.tmp/ET-Packages/cn.etetet.core/Scripts/Core/Share/Entity/Entity.cs`
- `.tmp/ET/Book/8.2ET Package目录.md`
- `.tmp/ET/README.md`

### 8. 文末导读建议

- 下一篇应读：ET-30
- 扩展阅读：ET-09、ET-10

### 9. 证据标签

- 源码拆解
- 包目录解析
- 工程判断

---

## ET-30｜HybridCLR、Reload 与热更：ET 的代码更新路径为什么这样设计

**系列 / 位置：** ET 框架源码解析 / Part 5：工程化与工具链（第 4 篇）

### 1. 系列职责

本篇负责讲清 ET 的代码更新链路：AOT、HybridCLR、Reload、热更 DLL 与运行时装配之间的关系。

### 2. 与相邻文章的重复风险与处理方式

| 相邻文章 | 重复风险点 | 本文处理方式 |
|---|---|---|
| ET-29：持久化边界 | 都会谈运行时状态 | 本文只讲代码更新和加载，不讲数据持久化 |
| ET-31：YooAssets、Loader、打包流程 | 都会谈更新链路 | 本文聚焦代码路径，资源交付只作为配套说明 |

### 3. 本文必须回答的核心问题

`ET 的代码热更、Reload、HybridCLR 分别解决哪一层问题，它们为什么要一起被设计？`

### 4. 本文明确不展开的内容

- 不展开 HybridCLR 完整安装教程
- 不展开所有平台 AOT 差异细节
- 不展开资源热更全链路
- 不展开热更方案横评

### 5. 推荐二级标题结构

1. `热更首先是代码装配问题`：先把边界讲清
2. `AOT 和 IL2CPP 为什么逼出桥接方案`：讲平台约束
3. `HybridCLR 在 ET 里承担什么角色`：讲代码路径桥接
4. `Reload 为什么不是单纯的重新编译`：讲运行时恢复和模块替换
5. `ET 的更新链路怎样闭环`：收束到调试与交付

### 6. 读者前置知识

- 必须掌握：ET-03、ET-09、ET-Pre-09
- 了解即可：AOT、IL2CPP、HybridCLR、Reload

### 7. 关键源码或文档锚点

- `.tmp/ET/README.md`
- `.tmp/ET/Book/1.1运行指南.md`
- `.tmp/ET-Packages/cn.etetet.loader/Scripts/Loader/Client/CodeLoader.cs`
- `.tmp/ET-Packages/cn.etetet.hybridclr`（包目录）

### 8. 文末导读建议

- 下一篇应读：ET-31
- 扩展阅读：ET-08、ET-27

### 9. 证据标签

- 源码拆解
- 官方文档解析
- 工程判断

---

## ET-31｜YooAssets、Loader、打包流程：ET 的资源交付链路怎么和代码装配配合

**系列 / 位置：** ET 框架源码解析 / Part 5：工程化与工具链（第 5 篇）

### 1. 系列职责

本篇负责把资源交付链路和代码装配链路放在一起讲，避免把 ET 的资源系统误看成单独插件。

### 2. 与相邻文章的重复风险与处理方式

| 相邻文章 | 重复风险点 | 本文处理方式 |
|---|---|---|
| ET-30：代码更新路径 | 都会谈热更 | 本文只讲资源交付与 Loader 配合，不讲代码更新机制 |
| ET-32：登录样板 | 都会接触初始化流程 | 本文不讲业务登录，只讲资源与装配对接 |

### 3. 本文必须回答的核心问题

`ET 为什么要把 YooAssets、Loader、资源打包和代码装配放在同一条工程链上？`

### 4. 本文明确不展开的内容

- 不展开 YooAssets 全套使用教程
- 不展开资源包拆分策略大全
- 不展开下载器/缓存器底层实现
- 不展开具体美术资源管线

### 5. 推荐二级标题结构

1. `资源交付首先是工程链路问题`：先讲为什么不能只看资源加载 API
2. `Loader 在 ET 里承担什么`：讲 model/modelview/hotfix/hotfixview 的接入
3. `YooAssets 解决哪一层问题`：讲资源分发与缓存
4. `资源链路和代码链路如何对齐`：讲启动时机与装配顺序
5. `为什么这会影响框架阅读`：收束到读者视角

### 6. 读者前置知识

- 必须掌握：ET-04、ET-08、ET-Pre-04、ET-Pre-09
- 了解即可：YooAssets、资源分包、下载缓存

### 7. 关键源码或文档锚点

- `.tmp/ET-Packages/cn.etetet.loader/Scripts/Loader/Client/CodeLoader.cs`
- `.tmp/ET-Packages/cn.etetet.loader/Scripts/Loader/Client/Init.cs`
- `.tmp/ET-Packages/cn.etetet.loader/Scripts/Loader/Server/Init.cs`
- `.tmp/ET/Book/1.1运行指南.md`
- `.tmp/ET/Book/8.2ET Package目录.md`

### 8. 文末导读建议

- 下一篇应读：ET-32
- 扩展阅读：ET-30、ET-01

### 9. 证据标签

- 源码拆解
- 官方文档解析
- 工程判断

---

## ET-32｜`login` 包为什么是理解 ET 的最佳最小业务样板

**系列 / 位置：** ET 框架源码解析 / Part 6：业务样板与扩展层（第 1 篇）

### 1. 系列职责

本篇负责把 ET 的抽象落到一个最小、最可读、最容易串起来的业务样板上，说明为什么登录链路是最适合入门的样板。

### 2. 与相邻文章的重复风险与处理方式

| 相邻文章 | 重复风险点 | 本文处理方式 |
|---|---|---|
| ET-31：资源交付链路 | 都会碰启动和初始化 | 本文只讲业务样板，不讲资源加载细节 |
| ET-33：move/unit/aoi | 都会碰场景和玩家 | 本文只讲登录样板，不讲场景实体扩展 |

### 3. 本文必须回答的核心问题

`为什么读 ET 时，最应该先看 login 包，而不是直接跳去战斗、移动或同步 Demo？`

### 4. 本文明确不展开的内容

- 不展开完整账号系统
- 不展开战斗逻辑
- 不展开 AOI 或移动实现
- 不展开帧同步和状态同步细节

### 5. 推荐二级标题结构

1. `为什么登录是最小业务样板`：讲样板价值
2. `客户端登录链路怎么走`：讲客户端视角
3. `Realm 到 Gate 的服务端链路怎么接`：讲服务端分发视角
4. `登录里已经包含了哪些 ET 核心抽象`：讲 Login 样板和前文衔接
5. `为什么这比战斗 Demo 更适合作为起点`：收束到阅读策略

### 6. 读者前置知识

- 必须掌握：ET-14、ET-15、ET-20、ET-Pre-05、ET-Pre-08
- 了解即可：账号登录、网关转发、会话管理

### 7. 关键源码或文档锚点

- `.tmp/ET-Packages/cn.etetet.login/Scripts/Hotfix/Client/Login/LoginHelper.cs`
- `.tmp/ET-Packages/cn.etetet.login/Scripts/Hotfix/Client/Login/ClientSenderComponentSystem.cs`
- `.tmp/ET-Packages/cn.etetet.login/Scripts/Hotfix/Client/NetClient/Main2NetClient_LoginHandler.cs`
- `.tmp/ET-Packages/cn.etetet.login/Scripts/Hotfix/Server/Realm/C2R_LoginHandler.cs`
- `.tmp/ET-Packages/cn.etetet.login/Scripts/Hotfix/Server/Gate/C2G_LoginGateHandler.cs`

### 8. 文末导读建议

- 下一篇应读：ET-33
- 扩展阅读：ET-06、ET-20

### 9. 证据标签

- 源码拆解
- 官方文档解析
- 工程判断

---

## ET-33｜`move / unit / aoi` 这些包说明了 ET 怎样承载 MMO 场景逻辑

**系列 / 位置：** ET 框架源码解析 / Part 6：业务样板与扩展层（第 2 篇）

### 1. 系列职责

本篇负责说明 ET 的世界实体层、移动和可见性结构如何承载 MMO 场景逻辑，而不是只停留在抽象框架层。

### 2. 与相邻文章的重复风险与处理方式

| 相邻文章 | 重复风险点 | 本文处理方式 |
|---|---|---|
| ET-32：login 样板 | 都会碰玩家与场景 | 本文从登录完成后的场景承载开始，不再讲接入流程 |
| ET-34：statesync Demo | 都会碰场景同步 | 本文只讲场景承载，不讲同步模型 |

### 3. 本文必须回答的核心问题

`ET 的 MMO 场景逻辑为什么会被拆成 move、unit、aoi 这一类能力包？`

### 4. 本文明确不展开的内容

- 不展开完整寻路算法教学
- 不展开 AOI 算法全景
- 不展开战斗数值和技能系统
- 不展开同步 Demo 的细节

### 5. 推荐二级标题结构

1. `场景逻辑首先是实体组织问题`：讲 unit 的职责
2. `move 解决什么`：讲移动与坐标推进
3. `aoi 解决什么`：讲视野、可见集和消息收敛
4. `这些能力如何和登录后的 Player/Scene 接上`：讲承载方式
5. `为什么这类包是 MMO 框架必需品`：收束到场景层判断

### 6. 读者前置知识

- 必须掌握：ET-07、ET-09、ET-21、ET-22、ET-Pre-07
- 了解即可：寻路、AOI、场景服概念

### 7. 关键源码或文档锚点

- `.tmp/ET/Book/8.2ET Package目录.md`
- `.tmp/ET-Packages/cn.etetet.move`（包目录）
- `.tmp/ET-Packages/cn.etetet.unit`（包目录）
- `.tmp/ET-Packages/cn.etetet.aoi`（包目录）

### 8. 文末导读建议

- 下一篇应读：ET-34
- 扩展阅读：ET-21、ET-32

### 9. 证据标签

- 包目录解析
- 源码拆解
- 工程判断

---

## ET-34｜`statesync` Demo 暴露了 ET 的哪一类权威同步思路

**系列 / 位置：** ET 框架源码解析 / Part 6：业务样板与扩展层（第 3 篇）

### 1. 系列职责

本篇负责从 `statesync` 这个公开 Demo 里抽出 ET 的状态同步思路，说明它不是单独演示，而是框架能力落点。

### 2. 与相邻文章的重复风险与处理方式

| 相邻文章 | 重复风险点 | 本文处理方式 |
|---|---|---|
| ET-33：move/unit/aoi | 都会碰场景逻辑 | 本文从同步视角切入，不讲场景结构本身 |
| ET-35：lockstep / lsentity / truesync | 都会讲同步 | 本文只讲状态同步思路，不讲帧同步路线 |

### 3. 本文必须回答的核心问题

`statesync Demo 具体在展示哪种权威同步思路，它为什么和 MMO 场景天然相关？`

### 4. 本文明确不展开的内容

- 不展开完整 Demo 操作步骤
- 不展开帧同步实现细节
- 不展开预测回滚全链路
- 不展开所有同步方案横评

### 5. 推荐二级标题结构

1. `状态同步先解决什么问题`：讲权威状态下发
2. `Demo 里真正值得看什么`：讲公开样板暴露的思路
3. `状态同步和 AOI 的关系`：讲可见性与更新频率
4. `为什么它更适合 MMO`：讲对象规模和视野结构
5. `它不能替代什么`：讲边界与代价

### 6. 读者前置知识

- 必须掌握：ET-Pre-10、ET-21、ET-33
- 了解即可：状态同步、权威服、AOI

### 7. 关键源码或文档锚点

- `.tmp/ET/Book/8.2ET Package目录.md`
- `.tmp/ET-Packages/cn.etetet.statesync`（包目录）
- `.tmp/ET-Packages/cn.etetet.router`（包目录）
- `.tmp/ET-Packages/cn.etetet.aoi`（包目录）

### 8. 文末导读建议

- 下一篇应读：ET-35
- 扩展阅读：ET-24、ET-33

### 9. 证据标签

- 包目录解析
- 官方文档解析
- 工程判断

---

## ET-35｜`lockstep / lsentity / truesync` 这一线说明了 ET 的另一条路线

**系列 / 位置：** ET 框架源码解析 / Part 6：业务样板与扩展层（第 4 篇）

### 1. 系列职责

本篇负责把 ET 的帧同步路线单独拉出来，说明它与状态同步不是同一条问题线。

### 2. 与相邻文章的重复风险与处理方式

| 相邻文章 | 重复风险点 | 本文处理方式 |
|---|---|---|
| ET-34：statesync Demo | 都会讲同步 | 本文专讲帧同步路线，不再重复状态同步的讨论 |
| ET-36：ai / numeric / config / robotcase | 都会谈 Demo 承载 | 本文只谈同步路线，不谈生态延伸 |

### 3. 本文必须回答的核心问题

`lockstep、lsentity、truesync 这一线在 ET 里承担的到底是哪类同步问题？`

### 4. 本文明确不展开的内容

- 不展开完整帧同步算法教学
- 不展开预测回滚实现全景
- 不展开定点数数学基础科普
- 不展开与其他引擎同步方案横评

### 5. 推荐二级标题结构

1. `帧同步先解决什么问题`：讲输入驱动与一致性
2. `lsentity / truesync 解决什么`：讲同步模拟的具体承载
3. `为什么 ET 要单独保留这一条路线`：讲玩法分层
4. `它和 statesync 的分界`：讲模型差别
5. `这条路线适合什么项目`：收束到工程选型

### 6. 读者前置知识

- 必须掌握：ET-Pre-10、ET-34
- 了解即可：帧同步、预测回滚、定点数

### 7. 关键源码或文档锚点

- `.tmp/ET/Book/8.2ET Package目录.md`
- `.tmp/ET-Packages/cn.etetet.lockstep`（包目录）
- `.tmp/ET-Packages/cn.etetet.lsentity`（包目录）
- `.tmp/ET-Packages/cn.etetet.truesync`（包目录）

### 8. 文末导读建议

- 下一篇应读：ET-36
- 扩展阅读：ET-34、ET-24

### 9. 证据标签

- 包目录解析
- 源码拆解
- 工程判断

---

## ET-36｜`ai / numeric / config / robotcase`：ET 怎样把框架延伸成完整工程生态

**系列 / 位置：** ET 框架源码解析 / Part 6：业务样板与扩展层（第 5 篇）

### 1. 系列职责

本篇负责把 ET 的能力从框架主链收束到生态层，说明它怎样向 AI、数值、配置、测试用例和工程自动化延展。

### 2. 与相邻文章的重复风险与处理方式

| 相邻文章 | 重复风险点 | 本文处理方式 |
|---|---|---|
| ET-35：lockstep 路线 | 都会谈 Demo 与扩展 | 本文不讲同步细节，只讲生态延伸 |
| ET-31：资源交付链路 | 都会谈工程化 | 本文只讲业务生态包，不讲资源加载 |

### 3. 本文必须回答的核心问题

`ET 什么时候不再只是框架，而开始变成可继续扩展的工程生态？`

### 4. 本文明确不展开的内容

- 不展开 AI 行为树完整教程
- 不展开 Numeric 全部数值模型设计
- 不展开配置系统的产品化细节
- 不展开机器人测试完整框架实现

### 5. 推荐二级标题结构

1. `为什么生态包是框架的延伸，而不是附属`：先定性
2. `ai / numeric / config 各自补了什么能力`：讲三类延伸
3. `robotcase 为什么重要`：讲测试与自动化落点
4. `ET 怎样从框架走向平台`：讲能力堆叠后的结果
5. `我们读完整个系列后该怎么判断 ET`：收束全系列

### 6. 读者前置知识

- 必须掌握：ET-26、ET-27、ET-34、ET-35
- 了解即可：行为树、数值系统、配置管理、机器人测试

### 7. 关键源码或文档锚点

- `.tmp/ET/Book/8.2ET Package目录.md`
- `.tmp/ET-Packages/cn.etetet.ai`（包目录）
- `.tmp/ET-Packages/cn.etetet.numeric`（包目录）
- `.tmp/ET-Packages/cn.etetet.configauto`（包目录）
- `.tmp/ET-Packages/cn.etetet.robotcase`（包目录）

### 8. 文末导读建议

- 下一篇应读：无，作为主系列收束篇
- 扩展阅读：ET-27、ET-31、ET-34、ET-35

### 9. 证据标签

- 包目录解析
- 官方文档解析
- 工程判断

---

## 本批次总收口

`ET-25 ~ ET-36` 这一批工作单的重点，不是再把 ET 讲成一个功能树，而是把它后半段真正落到“分布式组织、工程化、同步路线、生态延伸”四条线。这样后续正文起草时，相邻章节才不会互相抢内容。


