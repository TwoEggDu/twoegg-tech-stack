# ET 框架源码解析｜章节级工作单 Batch A（ET-01 ~ ET-12）

> 这份工作单面向已完成 `ET-Pre-01 ~ ET-Pre-10` 前置系列的读者。
> 它的目标不是再做总规划，而是把 `ET-01 ~ ET-12` 拆成可直接开写正文的章节级工单。

---

## ET-01｜ET9 还是那个 ET 吗：从单仓库框架到 Package 化框架

**系列 / 位置：** `ET 框架源码解析 / ET-01`

### 1. 系列职责

本篇负责先把 ET9 的真实交付形态讲清楚，纠正“主仓库就是全部框架”的旧理解，为后续所有源码篇建立正确入口。

### 2. 与前一篇 / 后一篇的重复风险

| 相邻篇 | 重复风险点 | 本篇处理方式 |
|---|---|---|
| 前置篇 `ET-Pre-04` | Package、asmdef、代码装配的通用解释 | 本篇只落到 ET9 的真实仓库形态，不再讲 Unity Package 通识 |
| `ET-02` | 主仓库为什么看不到核心代码 | 本篇讲“ET9 变成什么样”，不展开“为什么主仓库里少代码”这个更具体的问题 |

### 3. 本文必须回答的核心问题

ET9 的核心交付形态到底变成了什么，为什么主仓库不再等于全部框架？

### 4. 本文明确不展开的内容

- 不逐个讲 `cn.etetet.*` 包内部实现
- 不提前拆 `World / Fiber / Entity` 的运行时细节
- 不写成 ET7/ET8 与 ET9 的完整演进史

### 5. 推荐二级标题结构

1. ET9 为什么会让人第一眼找不到核心代码
2. 主仓库现在到底承担什么角色
3. `cn.etetet.*` 包为什么是框架真正的能力中心
4. Package 化改变了什么阅读方式
5. 本篇最后要收回的判断

### 6. 读者前置知识

必须掌握：

- ET-Pre-03 的程序集与动态加载语义
- ET-Pre-04 的 Package 与 asmdef 语义

了解即可：

- Unity Package Manager 的基本概念
- 旧时代“单仓库框架”的阅读习惯

### 7. 关键源码或文档锚点

- [`README.md`](E:\NHT\workspace\TechStackShow\.tmp\ET\README.md)
- [`Book/8.2ET Package目录.md`](E:\NHT\workspace\TechStackShow\.tmp\ET\Book\8.2ET%20Package目录.md)
- [`Packages/com.etetet.init/Editor/GitDependencyResolver/DependencyResolver.cs`](E:\NHT\workspace\TechStackShow\.tmp\ET\Packages\com.etetet.init\Editor\GitDependencyResolver\DependencyResolver.cs)
- [`Packages/com.etetet.init/Editor/GitDependencyResolver/PackageGit.cs`](E:\NHT\workspace\TechStackShow\.tmp\ET\Packages\com.etetet.init\Editor\GitDependencyResolver\PackageGit.cs)

### 8. 文末导读建议

- 下一篇应读：`ET-02｜主仓库为什么几乎看不到核心代码`
- 扩展阅读：`ET-Pre-04｜Unity Package、asmdef、代码装配`

### 9. 证据标签

`官方文档解析 + 包目录解析 + 工程判断`

---

## ET-02｜主仓库为什么几乎看不到核心代码

**系列 / 位置：** `ET 框架源码解析 / ET-02`

### 1. 系列职责

本篇负责把“主仓库像入口工程”这件事讲透，解释公开仓库、公开包、课程版和生态包的证据边界。

### 2. 与前一篇 / 后一篇的重复风险

| 相邻篇 | 重复风险点 | 本篇处理方式 |
|---|---|---|
| 前一篇 `ET-01` | ET9 变成 Package 化框架 | 本篇只讲“主仓库为什么少代码”，不重复解释 ET9 的形态变化 |
| 后一篇 `ET-03` | 安装要求与工程前提 | 本篇不写安装流程，只交代仓库边界与证据层级 |

### 3. 本文必须回答的核心问题

为什么 ET9 的主仓库不像旧框架那样直接暴露核心代码，真正的源码入口应该怎么看？

### 4. 本文明确不展开的内容

- 不做完整安装教程
- 不写所有包的逐个功能说明
- 不把课程版内容当成源码事实

### 5. 推荐二级标题结构

1. 为什么读者会在主仓库里找不到核心代码
2. 主仓库、Package 中心、公开包各自承担什么角色
3. 公开证据层级怎么分
4. 读 ET9 应该怎样改阅读路径
5. 本篇最后要收回的判断

### 6. 读者前置知识

必须掌握：

- ET-01 的包化框架结论
- ET-Pre-04 的工程边界语义

了解即可：

- GitHub Packages 或 Unity Package 的基本概念
- 开源项目的“入口工程”模式

### 7. 关键源码或文档锚点

- [`README.md`](E:\NHT\workspace\TechStackShow\.tmp\ET\README.md)
- [`Book/1.1运行指南.md`](E:\NHT\workspace\TechStackShow\.tmp\ET\Book\1.1运行指南.md)
- [`Book/8.2ET Package目录.md`](E:\NHT\workspace\TechStackShow\.tmp\ET\Book\8.2ET%20Package目录.md)
- [`Packages/com.etetet.init/Editor/GitDependencyResolver/DependencyResolver.cs`](E:\NHT\workspace\TechStackShow\.tmp\ET\Packages\com.etetet.init\Editor\GitDependencyResolver\DependencyResolver.cs)

### 8. 文末导读建议

- 下一篇应读：`ET-03｜安装要求本身就是架构说明书`
- 扩展阅读：`ET-Pre-04｜Unity Package、asmdef、代码装配`

### 9. 证据标签

`官方文档解析 + 包目录解析 + 工程判断`

---

## ET-03｜安装要求本身就是架构说明书：Unity 6000、.NET 8、Rider、GitHub Packages

**系列 / 位置：** `ET 框架源码解析 / ET-03`

### 1. 系列职责

本篇负责把 ET9 的运行前提解释成架构信号，而不是把它写成安装清单。

### 2. 与前一篇 / 后一篇的重复风险

| 相邻篇 | 重复风险点 | 本篇处理方式 |
|---|---|---|
| 前一篇 `ET-02` | 入口工程与证据边界 | 本篇只讲“为什么要求这么高”，不再重复仓库边界 |
| 后一篇 `ET-04` | 启动流程 | 本篇不写启动链，只解释运行前提和开发环境要求 |

### 3. 本文必须回答的核心问题

为什么 ET9 对 Unity、.NET、IDE 和包源有这么明确的前置要求？

### 4. 本文明确不展开的内容

- 不写逐步安装教程
- 不做 Unity 版本兼容大全
- 不展开所有包源与网络问题排查

### 5. 推荐二级标题结构

1. 运行前提为什么不能被看成普通安装说明
2. Unity 6000 / .NET 8 / Rider 这组要求在暗示什么
3. GitHub Packages 与包化交付的关系
4. 这些前提如何反映 ET 的工程约束
5. 本篇最后要收回的判断

### 6. 读者前置知识

必须掌握：

- ET-01 的 Package 化结论
- ET-Pre-03 的程序集与动态加载语义
- ET-Pre-04 的工程装配前提

了解即可：

- Unity 版本与脚本编译的大致关系
- .NET 运行时与 IDE 体验差异

### 7. 关键源码或文档锚点

- [`README.md`](E:\NHT\workspace\TechStackShow\.tmp\ET\README.md)
- [`Book/1.1运行指南.md`](E:\NHT\workspace\TechStackShow\.tmp\ET\Book\1.1运行指南.md)
- [`Book/8.1ET Package制作指南.md`](E:\NHT\workspace\TechStackShow\.tmp\ET\Book\8.1ET%20Package制作指南.md)

### 8. 文末导读建议

- 下一篇应读：`ET-04｜ET 工程启动总览`
- 扩展阅读：`ET-Pre-04｜Unity Package、asmdef、代码装配`

### 9. 证据标签

`官方文档解析 + 工程判断`

---

## ET-04｜ET 工程启动总览：客户端、服务端、一体化运行分别怎么启动

**系列 / 位置：** `ET 框架源码解析 / ET-04`

### 1. 系列职责

本篇负责先把 ET 的启动层地图立住，让读者知道客户端、服务端、一体化运行的总入口分别在处理什么。

### 2. 与前一篇 / 后一篇的重复风险

| 相邻篇 | 重复风险点 | 本篇处理方式 |
|---|---|---|
| 前一篇 `ET-03` | 安装和运行前提 | 本篇不再讲版本要求，只讲真正的启动路径 |
| 后一篇 `ET-05` | 包目录与模块地图 | 本篇只讲启动总览，不展开包层级 |

### 3. 本文必须回答的核心问题

ET 的客户端、服务端和一体化运行分别是如何被启动和装配起来的？

### 4. 本文明确不展开的内容

- 不拆每个启动按钮背后的全部编辑器实现
- 不提前进入某个具体包的业务逻辑
- 不展开热更细节

### 5. 推荐二级标题结构

1. 为什么 ET 的启动总览必须先讲
2. 客户端入口在做什么
3. 服务端入口在做什么
4. 一体化运行说明了什么
5. 本篇最后要收回的判断

### 6. 读者前置知识

必须掌握：

- ET-01 的框架形态
- ET-03 的运行前提
- ET-Pre-04 的工程装配边界

了解即可：

- Unity 项目启动与编辑器脚本的基本概念

### 7. 关键源码或文档锚点

- [`Book/1.1运行指南.md`](E:\NHT\workspace\TechStackShow\.tmp\ET\Book\1.1运行指南.md)
- [`Packages/cn.etetet.loader/Scripts/Loader/Client/Init.cs`](E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.loader\Scripts\Loader\Client\Init.cs)
- [`Packages/cn.etetet.loader/Scripts/Loader/Server/Init.cs`](E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.loader\Scripts\Loader\Server\Init.cs)
- [`Packages/cn.etetet.loader/Scripts/Loader/Client/CodeLoader.cs`](E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.loader\Scripts\Loader\Client\CodeLoader.cs)

### 8. 文末导读建议

- 下一篇应读：`ET-05｜cn.etetet.* 包目录怎么读`
- 扩展阅读：`ET-Pre-04｜Unity Package、asmdef、代码装配`

### 9. 证据标签

`源码拆解 + 官方文档解析`

---

## ET-05｜`cn.etetet.*` 包目录怎么读：哪些是核心，哪些是扩展，哪些是 Demo

**系列 / 位置：** `ET 框架源码解析 / ET-05`

### 1. 系列职责

本篇负责建立 ET9 的模块地图，给后续所有源码篇一个统一的包目录视角。

### 2. 与前一篇 / 后一篇的重复风险

| 相邻篇 | 重复风险点 | 本篇处理方式 |
|---|---|---|
| 前一篇 `ET-04` | 启动总览 | 本篇不再讲启动流程，只讲包目录如何分类 |
| 后一篇 `ET-06` | 登录链路总图 | 本篇不讲具体登录链路，只把模块地图立起来 |

### 3. 本文必须回答的核心问题

`cn.etetet.*` 包目录里哪些是核心、哪些是扩展、哪些是 Demo 或工具包？

### 4. 本文明确不展开的内容

- 不逐包写完整 API 说明
- 不深入包内部源码实现
- 不把收费包与公开包混写成同一证据层

### 5. 推荐二级标题结构

1. 为什么要先读包目录，而不是先追某个包内部实现
2. 核心包、扩展包、样板包、工具包怎么分
3. 包目录如何反映 ET9 的产品形态
4. 读包目录时要避免哪些误判
5. 本篇最后要收回的判断

### 6. 读者前置知识

必须掌握：

- ET-01 的 Package 化结论
- ET-04 的启动总览
- ET-Pre-04 的工程边界语义

了解即可：

- Unity Package 的基本分类方式

### 7. 关键源码或文档锚点

- [`Book/8.2ET Package目录.md`](E:\NHT\workspace\TechStackShow\.tmp\ET\Book\8.2ET%20Package目录.md)
- [`Packages`](E:\NHT\workspace\TechStackShow\.tmp\ET\Packages)
- [`README.md`](E:\NHT\workspace\TechStackShow\.tmp\ET\README.md)

### 8. 文末导读建议

- 下一篇应读：`ET-06｜第一张全局图：用一条登录链路串起 World、Fiber、Session、Mailbox、Location`
- 扩展阅读：`ET-Pre-08｜游戏服务端角色地图`

### 9. 证据标签

`包目录解析 + 官方文档解析 + 工程判断`

---

## ET-06｜第一张全局图：用一条登录链路串起 World、Fiber、Session、Mailbox、Location

**系列 / 位置：** `ET 框架源码解析 / ET-06`

### 1. 系列职责

本篇负责把前五篇拆开的入口、包、运行时、网络和 Actor 抽象，第一次收回成一张可理解的全局图。

### 2. 与前一篇 / 后一篇的重复风险

| 相邻篇 | 重复风险点 | 本篇处理方式 |
|---|---|---|
| 前一篇 `ET-05` | 包目录地图 | 本篇不再做模块分类，只用登录链路串图 |
| 后一篇 `ET-07` | `World` 细节 | 本篇只点出全局图，不展开 `World` 内部 |

### 3. 本文必须回答的核心问题

怎样用一条登录链路把 `World / Fiber / Session / Mailbox / Location` 这些抽象串起来？

### 4. 本文明确不展开的内容

- 不写登录协议每一步的业务细节
- 不提前深入 `World` 和 `Session` 的内部实现
- 不展开 ActorLocation 的迁移算法

### 5. 推荐二级标题结构

1. 为什么第一张全局图必须先从登录链路看
2. 登录链路里的对象、消息和运行时边界
3. `World / Fiber / Session / Mailbox / Location` 各自站在哪一层
4. 这一张图如何服务后续正文
5. 本篇最后要收回的判断

### 6. 读者前置知识

必须掌握：

- ET-04 的启动总览
- ET-05 的包目录地图
- ET-Pre-05 的会话语义
- ET-Pre-06 的 Actor 最小桥接
- ET-Pre-08 的服务端角色地图

了解即可：

- 登录流程的常见服务端分层

### 7. 关键源码或文档锚点

- [`Packages/cn.etetet.login/Scripts/Hotfix/Client/Login/ClientSenderComponentSystem.cs`](E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.login\Scripts\Hotfix\Client\Login\ClientSenderComponentSystem.cs)
- [`Packages/cn.etetet.login/Scripts/Hotfix/Client/NetClient/Main2NetClient_LoginHandler.cs`](E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.login\Scripts\Hotfix\Client\NetClient\Main2NetClient_LoginHandler.cs)
- [`Packages/cn.etetet.login/Scripts/Hotfix/Server/Realm/C2R_LoginHandler.cs`](E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.login\Scripts\Hotfix\Server\Realm\C2R_LoginHandler.cs)
- [`Packages/cn.etetet.login/Scripts/Hotfix/Server/Gate/C2G_LoginGateHandler.cs`](E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.login\Scripts\Hotfix\Server\Gate\C2G_LoginGateHandler.cs)
- [`Packages/cn.etetet.core/Scripts/Core/Share/World/World.cs`](E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.core\Scripts\Core\Share\World\World.cs)
- [`Packages/cn.etetet.core/Scripts/Core/Share/Fiber/Fiber.cs`](E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.core\Scripts\Core\Share\Fiber\Fiber.cs)
- [`Packages/cn.etetet.core/Scripts/Core/Share/Message/Session.cs`](E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.core\Scripts\Model\Share\Message\Session.cs)

### 8. 文末导读建议

- 下一篇应读：`ET-07｜World 是什么`
- 扩展阅读：`ET-Pre-06｜Actor 模型最小桥接`

### 9. 证据标签

`源码拆解 + 官方文档解析`

---

## ET-07｜`World` 是什么：ET 的全局运行时容器

**系列 / 位置：** `ET 框架源码解析 / ET-07`

### 1. 系列职责

本篇负责解释 ET 的全局运行时容器，为后续所有系统挂载和销毁提供统一的语义基础。

### 2. 与前一篇 / 后一篇的重复风险

| 相邻篇 | 重复风险点 | 本篇处理方式 |
|---|---|---|
| 前一篇 `ET-06` | 全局图里的 `World` 点位 | 本篇只展开 `World`，不回讲整条登录链路 |
| 后一篇 `ET-08` | 代码装配与加载 | 本篇不讲 `CodeLoader`，只讲 `World` 作为容器的职责 |

### 3. 本文必须回答的核心问题

`World` 在 ET 里到底扮演什么角色，为什么它像全局运行时容器？

### 4. 本文明确不展开的内容

- 不讲完整创建与销毁链所有分支
- 不展开具体业务包如何挂入 `World`
- 不提前进入 `FiberManager` 调度细节

### 5. 推荐二级标题结构

1. 为什么 `World` 不是“全局变量替代品”
2. `World` 负责哪些运行时边界
3. `World` 如何组织生命周期与系统挂载
4. 为什么 ET 需要这样一个容器
5. 本篇最后要收回的判断

### 6. 读者前置知识

必须掌握：

- ET-06 的全局图
- ET-Pre-01 的 Fiber 最小语义
- ET-Pre-07 的对象树与序列化树语义

了解即可：

- ECS / World 容器的一般理解

### 7. 关键源码或文档锚点

- [`Packages/cn.etetet.core/Scripts/Core/Share/World/World.cs`](E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.core\Scripts\Core\Share\World\World.cs)
- [`Packages/cn.etetet.core/Scripts/Core/Share/World/Fiber/FiberManager.cs`](E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.core\Scripts\Core\Share\World\Fiber\FiberManager.cs)
- [`Packages/cn.etetet.core/Scripts/Core/Share/World/EventSystem/EventSystem.cs`](E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.core\Scripts\Core\Share\World\EventSystem\EventSystem.cs)

### 8. 文末导读建议

- 下一篇应读：`ET-08｜CodeLoader 为什么要分 Model / ModelView / Hotfix / HotfixView`
- 扩展阅读：`ET-Pre-04｜Unity Package、asmdef、代码装配`

### 9. 证据标签

`源码拆解`

---

## ET-08｜`CodeLoader` 为什么要分 `Model / ModelView / Hotfix / HotfixView`

**系列 / 位置：** `ET 框架源码解析 / ET-08`

### 1. 系列职责

本篇负责解释 ET 的代码装配方式，为热更、程序集和运行时加载建立同一条认知链。

### 2. 与前一篇 / 后一篇的重复风险

| 相邻篇 | 重复风险点 | 本篇处理方式 |
|---|---|---|
| 前一篇 `ET-07` | 全局容器 | 本篇不讲 `World`，只讲代码装配与加载边界 |
| 后一篇 `ET-09` | Entity 模型 | 本篇不进入对象树，只讲代码如何进入运行时 |

### 3. 本文必须回答的核心问题

为什么 `CodeLoader` 要把代码分成 `Model / ModelView / Hotfix / HotfixView` 四段？

### 4. 本文明确不展开的内容

- 不写热更工具链全流程
- 不提前拆 `Entity` 的数据结构
- 不展开 Loader 的编辑器实现所有细节

### 5. 推荐二级标题结构

1. 代码装配为什么是 ET 的核心工程能力
2. 四段代码分别承担什么职责
3. `CodeLoader` 在运行时到底做了什么
4. 这种拆分如何影响热更和开发边界
5. 本篇最后要收回的判断

### 6. 读者前置知识

必须掌握：

- ET-03 的程序集与动态加载语义
- ET-07 的 `World` 容器语义
- ET-Pre-03 的 DLL 与加载边界
- ET-Pre-04 的装配边界

了解即可：

- Unity 中脚本编译与程序集分段的基本直觉

### 7. 关键源码或文档锚点

- [`Packages/cn.etetet.loader/Scripts/Loader/Client/CodeLoader.cs`](E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.loader\Scripts\Loader\Client\CodeLoader.cs)
- [`Packages/cn.etetet.loader/Scripts/Loader/Client/Init.cs`](E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.loader\Scripts\Loader\Client\Init.cs)
- [`Packages/cn.etetet.loader/Scripts/Loader/Server/Init.cs`](E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.loader\Scripts\Loader\Server\Init.cs)

### 8. 文末导读建议

- 下一篇应读：`ET-09｜Entity 不是 ECS`
- 扩展阅读：`ET-Pre-09｜热更不是魔法`

### 9. 证据标签

`源码拆解 + 工程判断`

---

## ET-09｜`Entity` 不是 ECS：ET 的对象树模型到底是什么

**系列 / 位置：** `ET 框架源码解析 / ET-09`

### 1. 系列职责

本篇负责把 ET 的核心数据结构立住，避免读者把它误读成 Unity ECS 或普通树状容器。

### 2. 与前一篇 / 后一篇的重复风险

| 相邻篇 | 重复风险点 | 本篇处理方式 |
|---|---|---|
| 前一篇 `ET-08` | 代码装配 | 本篇不讲加载，只讲装配后的对象模型 |
| 后一篇 `ET-10` | Parent/Child/Component 约束 | 本篇先定义 Entity 模型，下一篇再拆约束细节 |

### 3. 本文必须回答的核心问题

ET 的 `Entity` 到底是什么，为什么它不该被简单理解成 ECS？

### 4. 本文明确不展开的内容

- 不写 Unity ECS 教程
- 不展开持久化实现细节
- 不进入全部系统挂接方式

### 5. 推荐二级标题结构

1. 为什么 `Entity` 一上来就容易被误读
2. ET 的对象树模型到底解决什么问题
3. `Entity` 的身份、归属和生命周期边界
4. 为什么它不是普通 ECS
5. 本篇最后要收回的判断

### 6. 读者前置知识

必须掌握：

- ET-06 的全局图
- ET-07 的 `World`
- ET-Pre-07 的对象树与序列化树前置

了解即可：

- Unity ECS 的一般印象

### 7. 关键源码或文档锚点

- [`Packages/cn.etetet.core/Scripts/Core/Share/Entity/Entity.cs`](E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.core\Scripts\Core\Share\Entity\Entity.cs)
- [`Packages/cn.etetet.core/Scripts/Core/Share/World/World.cs`](E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.core\Scripts\Core\Share\World\World.cs)
- [`Book/8.2ET Package目录.md`](E:\NHT\workspace\TechStackShow\.tmp\ET\Book\8.2ET%20Package目录.md)

### 8. 文末导读建议

- 下一篇应读：`ET-10｜Parent / Child / Component / IScene 四个概念如何卡住 ET 的数据树`
- 扩展阅读：`ET-Pre-07｜对象树与序列化树`

### 9. 证据标签

`源码拆解`

---

## ET-10｜`Parent / Child / Component / IScene` 四个概念如何卡住 ET 的数据树

**系列 / 位置：** `ET 框架源码解析 / ET-10`

### 1. 系列职责

本篇负责把 ET 对对象归属、生命周期、序列化边界的强约束讲清楚。

### 2. 与前一篇 / 后一篇的重复风险

| 相邻篇 | 重复风险点 | 本篇处理方式 |
|---|---|---|
| 前一篇 `ET-09` | Entity 模型 | 本篇不再定义 `Entity` 本身，只拆四个约束概念 |
| 后一篇 `ET-11` | EntitySystem | 本篇只讲数据树约束，不讲行为挂载 |

### 3. 本文必须回答的核心问题

`Parent / Child / Component / IScene` 为什么会决定 ET 的数据树边界？

### 4. 本文明确不展开的内容

- 不讲完整序列化框架
- 不写所有系统继承关系
- 不提前进入事件系统与消息系统

### 5. 推荐二级标题结构

1. 为什么这四个概念不能当成普通字段理解
2. 它们分别在解决什么边界问题
3. 为什么 ET 要把约束前置到对象模型
4. 这套约束如何影响序列化与恢复
5. 本篇最后要收回的判断

### 6. 读者前置知识

必须掌握：

- ET-09 的 Entity 模型
- ET-Pre-07 的对象树与序列化树前置
- ET-07 的 `World`

了解即可：

- 场景、组件、宿主对象的常见概念

### 7. 关键源码或文档锚点

- [`Packages/cn.etetet.core/Scripts/Core/Share/Entity/Entity.cs`](E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.core\Scripts\Core\Share\Entity\Entity.cs)
- [`Book/8.2ET Package目录.md`](E:\NHT\workspace\TechStackShow\.tmp\ET\Book\8.2ET%20Package目录.md)
- [`README.md`](E:\NHT\workspace\TechStackShow\.tmp\ET\README.md)

### 8. 文末导读建议

- 下一篇应读：`ET-11｜EntitySystem 是怎样把方法重新挂回 Entity 的`
- 扩展阅读：`ET-Pre-07｜对象树与序列化树`

### 9. 证据标签

`源码拆解`

---

## ET-11｜`EntitySystem` 是怎样把“方法”重新挂回 `Entity` 的

**系列 / 位置：** `ET 框架源码解析 / ET-11`

### 1. 系列职责

本篇负责解释“数据和方法分离”在 ET 里到底怎么落地，以及系统方法为什么要重新挂回 Entity。

### 2. 与前一篇 / 后一篇的重复风险

| 相邻篇 | 重复风险点 | 本篇处理方式 |
|---|---|---|
| 前一篇 `ET-10` | 数据树约束 | 本篇不再讲归属边界，只讲行为如何挂接 |
| 后一篇 `ET-12` | 事件系统 | 本篇只讲 EntitySystem，不讲 Publish / Invoke |

### 3. 本文必须回答的核心问题

`EntitySystem` 是怎样把行为从对象里拆出来，再在运行时重新挂回去的？

### 4. 本文明确不展开的内容

- 不讲完整代码生成细节
- 不写分析器全流程
- 不提前展开 EventSystem

### 5. 推荐二级标题结构

1. 为什么要把方法从 Entity 里拆出去
2. `EntitySystem` 在运行时承担什么职责
3. 系统方法是如何找到目标 Entity 的
4. 这种设计如何影响可维护性
5. 本篇最后要收回的判断

### 6. 读者前置知识

必须掌握：

- ET-09 的 Entity 模型
- ET-10 的数据树约束
- ET-Pre-03 的生成与装配前置

了解即可：

- 代码生成与系统分发的常见直觉

### 7. 关键源码或文档锚点

- [`Packages/cn.etetet.core/Scripts/Core/Share/Entity/Entity.cs`](E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.core\Scripts\Core\Share\Entity\Entity.cs)
- [`Packages/cn.etetet.core/Scripts/Core/Share/Entity/EntitySystem.cs`](E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.core\Scripts\Core\Share\Entity\EntitySystem.cs)
- [`Packages/cn.etetet.core/Scripts/Core/Share/Entity/EntitySystemOf.cs`](E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.core\Scripts\Core\Share\Entity\EntitySystemOf.cs)

### 8. 文末导读建议

- 下一篇应读：`ET-12｜EventSystem 的 Publish 和 Invoke 为什么必须分开理解`
- 扩展阅读：`ET-Pre-03｜程序集、DLL、反射、动态加载`

### 9. 证据标签

`源码拆解`

---

## ET-12｜`EventSystem` 的 `Publish` 和 `Invoke` 为什么必须分开理解

**系列 / 位置：** `ET 框架源码解析 / ET-12`

### 1. 系列职责

本篇负责把 ET 的事件分发层讲清，避免把事件机制和回调分发混成一回事。

### 2. 与前一篇 / 后一篇的重复风险

| 相邻篇 | 重复风险点 | 本篇处理方式 |
|---|---|---|
| 前一篇 `ET-11` | EntitySystem | 本篇不讲系统方法挂接，只讲事件分发语义 |
| 后一篇 `ET-13` | Fiber 与并发骨架 | 本篇只讲事件系统，不进入 Fiber 调度 |

### 3. 本文必须回答的核心问题

`Publish` 和 `Invoke` 分别在解决什么问题，为什么 ET 要把它们分开？

### 4. 本文明确不展开的内容

- 不讲完整消息总线设计史
- 不展开全部事件类型注册细节
- 不进入网络消息派发的完整链路

### 5. 推荐二级标题结构

1. 为什么事件系统不能只看成“回调表”
2. `Publish` 和 `Invoke` 的语义边界
3. ET 为什么需要两种分发方式
4. 事件系统和 EntitySystem 的关系
5. 本篇最后要收回的判断

### 6. 读者前置知识

必须掌握：

- ET-11 的行为挂接语义
- ET-07 的 `World`
- ET-Pre-05 的会话与请求响应语义

了解即可：

- 事件驱动编程的一般概念

### 7. 关键源码或文档锚点

- [`Packages/cn.etetet.core/Scripts/Core/Share/World/EventSystem/EventSystem.cs`](E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.core\Scripts\Core\Share\World\EventSystem\EventSystem.cs)
- [`Packages/cn.etetet.core/Scripts/Core/Share/Entity/EntitySystem.cs`](E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.core\Scripts\Core\Share\Entity\EntitySystem.cs)
- [`Packages/cn.etetet.core/Scripts/Core/Share/World/World.cs`](E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.core\Scripts\Core\Share\World\World.cs)

### 8. 文末导读建议

- 下一篇应读：`ET-13｜ETTask、Fiber 与 FiberManager：ET9 的并发骨架到底是什么`
- 扩展阅读：`ET-Pre-02｜async/await 到底在调度什么`

### 9. 证据标签

`源码拆解`

---

## 本批落点

这 12 篇工单的目标不是一次写完所有正文，而是把 `ET-01 ~ ET-12` 的章节职责、边界、证据锚点和导读顺序先定死。

这样后续真正起草正文时，至少不会再回头争论：

- 这一篇到底该回答什么
- 它和前后篇哪里重叠
- 这一章该用源码还是文档证据
- 写完后该把读者导到哪一篇

