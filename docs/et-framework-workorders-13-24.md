# ET 主系列章节级工作单 Batch B（ET-13 ~ ET-24）

这份工作单面向已经完成 `ET-Pre-01 ~ ET-Pre-10` 的读者。目标不是重复总计划，而是把 `ET-13 ~ ET-24` 每篇的写作边界、源码锚点、重复风险和正文结构一次性压实，方便后续直接开写正文。

---

## ET-13
**选题：** `ETTask`、`Fiber` 与 `FiberManager`：ET9 的并发骨架到底是什么  
**系列 / 位置：** ET 框架源码解析 / Part 2 第 7 篇  

### 1. 系列职责
本篇承接前置中的并发语义与 `async/await` 桥接，正式把 ET9 的运行时并发骨架讲清楚，回答“ET 不是在换一个异步 API，而是在把调度边界收回框架层”。

### 2. 与前一篇 / 后一篇的重复风险与处理方式
| 相邻文章 | 重复风险点 | 本文处理方式 |
|---|---|---|
| ET-12 `EventSystem` | 容易把事件分发和调度骨架混为一谈 | 只讲调度容器，不讲事件系统内部派发细节 |
| ET-14 `Session` | 容易把网络会话和 Fiber 调度混成一条线 | 只交代会话如何被 Fiber 承载，不展开网络 API |

### 3. 本文必须回答的核心问题
ET9 为什么要把多线程能力包装成纤程模型，而不是只提供 `Task` 或线程池？

### 4. 本文明确不展开的内容
- 不展开 `ETTask` 全部 awaitable 接口细节
- 不展开线程池、锁、原子操作的通识教程
- 不展开网络会话、消息分发、ActorLocation 的实现

### 5. 推荐二级标题结构
1. `ET 为什么需要 Fiber` - 解释它解决的不是“异步语法”，而是“运行时边界”
2. `ETTask 在这里扮演什么角色` - 说明它如何承接上下文与调度结果
3. `FiberManager 如何组织 Fiber` - 说明创建、挂载、销毁与归属关系
4. `主线程、线程池、独立线程三种调度` - 说明不同调度方式各自的用途
5. `这套骨架带来的收益与代价` - 说明为什么它适合 MMO 但也更硬

### 6. 读者前置知识
- 必须掌握：`线程 / 任务 / async / continuation`
- 必须掌握：前置篇 `ET-Pre-01`、`ET-Pre-02`
- 了解即可：Unity 主线程、消息循环、协程

### 7. 关键源码或文档锚点
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.core\Scripts\Core\Share\Fiber\Fiber.cs`
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.core\Scripts\Core\Share\World\Fiber\FiberManager.cs`
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.core\Scripts\Core\Share\World\World.cs`
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.core\Scripts\Core\Share\ETTask\ETTask.cs`

### 8. 文末导读建议
- 下一篇应读：`ET-14`
- 理由：先把并发骨架立住，再看会话对象如何挂到运行时上

### 9. 证据标签
`源码拆解`

---

## ET-14
**选题：** `Session` 是什么：ET 网络层的最小会话对象  
**系列 / 位置：** ET 框架源码解析 / Part 3 第 1 篇  

### 1. 系列职责
把网络连接从“底层 Socket”抬升成“可管理的会话对象”，为后续 `Call / Send / RpcInfo` 和登录链路建立最小语义。

### 2. 与前一篇 / 后一篇的重复风险与处理方式
| 相邻文章 | 重复风险点 | 本文处理方式 |
|---|---|---|
| ET-13 `Fiber` | 会话和 Fiber 都像运行时容器 | 只讲会话生命周期，不讲调度模型 |
| ET-15 `Call / Send` | 容易提前展开 RPC 语义 | 只定义 Session 边界，不讲调用闭环 |

### 3. 本文必须回答的核心问题
ET 为什么要把网络连接包装成 `Session`，而不是直接暴露 Socket 或 Channel？

### 4. 本文明确不展开的内容
- 不展开 KCP/TCP/WebSocket 的协议差异
- 不展开完整 RPC 调用链
- 不展开登录业务或 ActorLocation 逻辑

### 5. 推荐二级标题结构
1. `Session 在网络层解决什么问题`
2. `连接、状态、生命周期为什么要统一管理`
3. `Session 与消息发送的关系`
4. `超时、断线、心跳为什么都要挂在 Session 上`
5. `ET 里 Session 不是工具类，而是边界对象`

### 6. 读者前置知识
- 必须掌握：前置篇 `ET-Pre-05`
- 必须掌握：请求响应、超时、心跳、会话
- 了解即可：Socket、连接状态机

### 7. 关键源码或文档锚点
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.core\Scripts\Model\Share\Message\Session.cs`
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.core\Scripts\Hotfix\Share\Message\SessionSystem.cs`
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.loader\Scripts\Loader\Client\Init.cs`

### 8. 文末导读建议
- 下一篇应读：`ET-15`
- 理由：`Session` 立住后，才好解释 `Call / Send` 为什么是两种不同语义

### 9. 证据标签
`源码拆解`

---

## ET-15
**选题：** `Call / Send / RpcInfo`：一次请求响应在 ET 里怎样闭环  
**系列 / 位置：** ET 框架源码解析 / Part 3 第 2 篇  

### 1. 系列职责
把会话之上的网络交互语义讲清楚，说明 ET 如何把“发消息”分成“只发”和“等待结果”两种不同边界。

### 2. 与前一篇 / 后一篇的重复风险与处理方式
| 相邻文章 | 重复风险点 | 本文处理方式 |
|---|---|---|
| ET-14 `Session` | 容易重复会话生命周期 | 只使用 Session 作为承载前提，不再重讲边界 |
| ET-16 `MailBoxComponent` | 容易把 RPC 直接讲成 Actor 消息 | 只讲网络请求闭环，不讲 Actor 语义 |

### 3. 本文必须回答的核心问题
ET 的 `Call` 和 `Send` 为什么要分开，而 `RpcInfo` 在闭环里承担什么责任？

### 4. 本文明确不展开的内容
- 不展开消息协议生成器细节
- 不展开完整异常与重试策略
- 不展开 ActorLocation 和登录业务实现

### 5. 推荐二级标题结构
1. `Send 和 Call 的边界`
2. `RpcInfo 管理什么状态`
3. `请求、响应、超时如何闭环`
4. `为什么网络 API 不能只剩一个统一入口`
5. `这套设计对业务代码的影响`

### 6. 读者前置知识
- 必须掌握：`ET-Pre-05`
- 必须掌握：Session、请求响应、超时
- 了解即可：RPC、消息号、协议派发

### 7. 关键源码或文档锚点
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.core\Scripts\Model\Share\Message\Session.cs`
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.core\Scripts\Hotfix\Share\Message\SessionSystem.cs`
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.login\Scripts\Hotfix\Client\NetClient\Main2NetClient_LoginHandler.cs`

### 8. 文末导读建议
- 下一篇应读：`ET-16`
- 理由：网络请求闭环已经立住，接下来要看消息如何进入 Actor 化处理

### 9. 证据标签
`源码拆解`

---

## ET-16
**选题：** `MailBoxComponent` 为什么让一个 Entity 变成 Actor  
**系列 / 位置：** ET 框架源码解析 / Part 3 第 3 篇  

### 1. 系列职责
把 Actor 的最小落点落到 ET 的对象模型里，说明“邮箱 + 串行处理 + 位置透明”如何在 Entity 上成立。

### 2. 与前一篇 / 后一篇的重复风险与处理方式
| 相邻文章 | 重复风险点 | 本文处理方式 |
|---|---|---|
| ET-15 `Call / Send` | 容易把网络 RPC 与 Actor 消息混起来 | 只讲消息进入 Actor 的入口，不讲 RPC 闭环 |
| ET-17 `MessageDispatcher` | 容易把邮箱和分发器混为一体 | 本文只讲 Actor 容器，分发器留给下一篇 |

### 3. 本文必须回答的核心问题
为什么给一个 Entity 挂上 `MailBoxComponent` 后，它就具备了 Actor 的消息处理边界？

### 4. 本文明确不展开的内容
- 不展开完整 Actor 理论史
- 不展开 `MessageDispatcher` 细节
- 不展开 Location 迁移协议细节

### 5. 推荐二级标题结构
1. `Actor 最小模型回顾`
2. `MailBoxComponent 解决什么边界问题`
3. `消息如何进入 Actor`
4. `串行处理为什么是核心约束`
5. `ET 的 Actor 与纯理论 Actor 的差别`

### 6. 读者前置知识
- 必须掌握：`ET-Pre-06`
- 必须掌握：邮箱、串行处理、位置透明
- 了解即可：对象消息队列、Actor 理论

### 7. 关键源码或文档锚点
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.core\Scripts\Model\Share\Mailbox\MailBoxComponent.cs`
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.core\Scripts\Model\Share\Actor\MessageDispatcher.cs`

### 8. 文末导读建议
- 下一篇应读：`ET-17`
- 理由：Actor 容器立住后，必须看消息怎么被正确派发到处理器

### 9. 证据标签
`源码拆解`

---

## ET-17
**选题：** `MessageDispatcher` 怎样把消息类型分发到正确处理器  
**系列 / 位置：** ET 框架源码解析 / Part 3 第 4 篇  

### 1. 系列职责
解释 ET 如何把“收到消息”变成“进入正确业务处理器”，把消息系统和业务系统之间的连接打通。

### 2. 与前一篇 / 后一篇的重复风险与处理方式
| 相邻文章 | 重复风险点 | 本文处理方式 |
|---|---|---|
| ET-16 `MailBoxComponent` | 容易重复 Actor 容器职责 | 只承接消息派发，不回头解释邮箱边界 |
| ET-18 `ActorId / Address / Fiber 归属` | 容易提前讲寻址和归属 | 这里只讲分发，不讲物理位置与归属映射 |

### 3. 本文必须回答的核心问题
ET 是如何把消息类型、处理器和业务调用链稳定连起来的？

### 4. 本文明确不展开的内容
- 不展开完整协议生成流程
- 不展开 ActorLocation 寻址
- 不展开客户端登录链路

### 5. 推荐二级标题结构
1. `分发器在运行时承担什么角色`
2. `消息类型如何映射到处理器`
3. `为什么分发器必须和 Actor 语义配合`
4. `分发失败和边界错误如何处理`
5. `这套分发链对业务编写的意义`

### 6. 读者前置知识
- 必须掌握：`ET-Pre-06`
- 必须掌握：Mailbox、Actor、串行处理
- 了解即可：消息路由、处理器注册

### 7. 关键源码或文档锚点
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.core\Scripts\Model\Share\Actor\MessageDispatcher.cs`
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.core\Scripts\Model\Share\Mailbox\MailBoxComponent.cs`

### 8. 文末导读建议
- 下一篇应读：`ET-18`
- 理由：消息派发之后，下一层就是“对象与地址如何绑定”

### 9. 证据标签
`源码拆解`

---

## ET-18
**选题：** `ActorId`、`Address`、Fiber 归属：ET 的消息寻址到底依赖什么  
**系列 / 位置：** ET 框架源码解析 / Part 3 第 5 篇  

### 1. 系列职责
把“对象寻址”与“进程位置”分开，说明 ET 为什么需要 ActorId、Address 和 Fiber 归属这一组概念。

### 2. 与前一篇 / 后一篇的重复风险与处理方式
| 相邻文章 | 重复风险点 | 本文处理方式 |
|---|---|---|
| ET-17 `MessageDispatcher` | 容易重复消息路由 | 只讲寻址前提，不讲派发过程 |
| ET-19 客户端 NetClient Fiber | 容易把客户端独立纤程和寻址搞混 | 只讲寻址与归属，客户端网络留给下一篇 |

### 3. 本文必须回答的核心问题
ET 的消息和对象为什么不是靠“直接引用”而是靠地址与归属去连接？

### 4. 本文明确不展开的内容
- 不展开完整 ActorLocation 实现
- 不展开登录流程
- 不展开路由器和场景迁移的全部细节

### 5. 推荐二级标题结构
1. `ActorId 和 Address 分别在解决什么`
2. `Fiber 归属为什么会影响消息寻址`
3. `逻辑对象和物理位置为什么要分开看`
4. `跨进程消息为什么一定需要寻址层`
5. `ET 的对象化寻址思想`

### 6. 读者前置知识
- 必须掌握：`ET-Pre-06`
- 必须掌握：Actor、邮箱、位置透明
- 了解即可：地址映射、进程间通信

### 7. 关键源码或文档锚点
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.actorlocation\Scripts\Model\Server\LocationComponent.cs`
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.actorlocation\Scripts\Hotfix\Server\LocationProxyComponentSystem.cs`
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.actorlocation\Scripts\Hotfix\Server\LocationOneTypeSystem.cs`

### 8. 文末导读建议
- 下一篇应读：`ET-19`
- 理由：寻址讲清后，开始看客户端为什么要单独起网络 Fiber

### 9. 证据标签
`源码拆解`

---

## ET-19
**选题：** 客户端为什么要单独拉起一个 NetClient Fiber  
**系列 / 位置：** ET 框架源码解析 / Part 3 第 6 篇  

### 1. 系列职责
解释客户端网络不跟主逻辑混跑的原因，把独立网络纤程的工程动机讲清。

### 2. 与前一篇 / 后一篇的重复风险与处理方式
| 相邻文章 | 重复风险点 | 本文处理方式 |
|---|---|---|
| ET-18 `ActorId / Address` | 容易把寻址和客户端网络线程混在一起 | 只讲客户端网络独立运行的原因 |
| ET-20 双阶段登录 | 容易把登录链路和网络线程职责混掉 | 只讲独立 Fiber，不讲完整登录流程 |

### 3. 本文必须回答的核心问题
为什么客户端网络要从主逻辑里拆出去，单独跑在自己的 Fiber 上？

### 4. 本文明确不展开的内容
- 不展开登录协议细节
- 不展开消息处理器内部逻辑
- 不展开战斗/移动等业务系统

### 5. 推荐二级标题结构
1. `客户端网络独立化的动机`
2. `NetClient Fiber 与主逻辑 Fiber 的边界`
3. `为什么这能降低主线程干扰`
4. `独立网络纤程带来的维护收益`
5. `ET 为什么会这么组织客户端`

### 6. 读者前置知识
- 必须掌握：`ET-Pre-01`、`ET-Pre-05`
- 必须掌握：Fiber、Session、请求响应
- 了解即可：Unity 主线程和网络线程分离

### 7. 关键源码或文档锚点
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.login\Scripts\Hotfix\Client\Login\ClientSenderComponentSystem.cs`
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.login\Scripts\Hotfix\Client\NetClient\Main2NetClient_LoginHandler.cs`
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.loader\Scripts\Loader\Client\Init.cs`

### 8. 文末导读建议
- 下一篇应读：`ET-20`
- 理由：客户端网络边界立住后，才能看登录流程为何拆成两段

### 9. 证据标签
`源码拆解`

---

## ET-20
**选题：** `Main2NetClient_LoginHandler`：ET 的双阶段登录为什么先连 Realm 再连 Gate  
**系列 / 位置：** ET 框架源码解析 / Part 3 第 7 篇  

### 1. 系列职责
用一条真实登录链路把前面 Session、Actor、NetClient Fiber 的概念串起来。

### 2. 与前一篇 / 后一篇的重复风险与处理方式
| 相邻文章 | 重复风险点 | 本文处理方式 |
|---|---|---|
| ET-19 客户端网络 Fiber | 容易重复网络线程边界 | 只讲登录链路如何使用网络 Fiber |
| ET-21 Actor Location | 容易提前把登录讲成分布式寻址 | 只讲 Realm/Gate 两段登录，不讲 Location 机制本体 |

### 3. 本文必须回答的核心问题
ET 为什么要把登录拆成 Realm 和 Gate 两段，而不是一次性直连最终场景服？

### 4. 本文明确不展开的内容
- 不展开账号系统完整实现
- 不展开场景进入后的业务逻辑
- 不展开 ActorLocation 的内部寻址协议

### 5. 推荐二级标题结构
1. `登录为什么要分阶段`
2. `Realm 这一段负责什么`
3. `Gate 这一段负责什么`
4. `为什么这比直连更适合 MMO`
5. `登录链路如何落到客户端代码`

### 6. 读者前置知识
- 必须掌握：`ET-Pre-05`、`ET-Pre-08`
- 必须掌握：Session、Call、登录角色地图
- 了解即可：分服、认证、网关

### 7. 关键源码或文档锚点
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.login\Scripts\Hotfix\Client\Login\ClientSenderComponentSystem.cs`
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.login\Scripts\Hotfix\Client\NetClient\Main2NetClient_LoginHandler.cs`
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.login\Scripts\Hotfix\Server\Realm\C2R_LoginHandler.cs`
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.login\Scripts\Hotfix\Server\Gate\C2G_LoginGateHandler.cs`

### 8. 文末导读建议
- 下一篇应读：`ET-21`
- 理由：登录链路已经通了，接下来要正式进入 ActorLocation 的核心能力

### 9. 证据标签
`源码拆解`

---

## ET-21
**选题：** Actor Location 为什么是 ET 的中轴能力  
**系列 / 位置：** ET 框架源码解析 / Part 4 第 1 篇  

### 1. 系列职责
把“逻辑对象寻址”和“物理部署位置”分开，说明 ET 为什么会把 Location 放到中轴位置。

### 2. 与前一篇 / 后一篇的重复风险与处理方式
| 相邻文章 | 重复风险点 | 本文处理方式 |
|---|---|---|
| ET-20 登录链路 | 容易把登录与寻址搅在一起 | 只把登录当作进入分布式体系的入口，不展开登录协议 |
| ET-22 `LocationProxyComponent` | 容易重复代理调用细节 | 只讲 Location 的总能力，不讲代理实现细节 |

### 3. 本文必须回答的核心问题
为什么说不理解 Location，就不算真正理解 ET 的分布式设计？

### 4. 本文明确不展开的内容
- 不展开完整代理对象源码
- 不展开路由/迁移的全部实现
- 不展开同步模型与战斗系统

### 5. 推荐二级标题结构
1. `Location 在解决什么问题`
2. `逻辑身份与物理位置为什么要解耦`
3. `跨进程寻址为什么必须成为框架能力`
4. `Location 和 Actor 的关系`
5. `为什么它是 ET 的中轴能力`

### 6. 读者前置知识
- 必须掌握：`ET-Pre-06`、`ET-Pre-08`
- 必须掌握：Actor、位置透明、登录角色地图
- 了解即可：跨进程通信、对象定位

### 7. 关键源码或文档锚点
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.actorlocation\Scripts\Model\Server\LocationComponent.cs`
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.actorlocation\Scripts\Hotfix\Server\LocationProxyComponentSystem.cs`
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.actorlocation\Scripts\Hotfix\Server\LocationOneTypeSystem.cs`

### 8. 文末导读建议
- 下一篇应读：`ET-22`
- 理由：中轴概念立住后，再拆代理层实现才不会失焦

### 9. 证据标签
`源码拆解`

---

## ET-22
**选题：** `LocationProxyComponent` 提供的到底是什么能力  
**系列 / 位置：** ET 框架源码解析 / Part 4 第 2 篇  

### 1. 系列职责
把跨进程对象调用包装成业务可调用 API，说明 ET 怎样把分布式复杂度隐藏起来。

### 2. 与前一篇 / 后一篇的重复风险与处理方式
| 相邻文章 | 重复风险点 | 本文处理方式 |
|---|---|---|
| ET-21 Location 中轴能力 | 容易重复 Location 总论 | 只讲代理层提供了什么能力 |
| ET-23 `LocationOneTypeSystem` | 容易提前讲锁和迁移 | 只讲代理调用，不讲并发控制协议 |

### 3. 本文必须回答的核心问题
ET 是怎样把“对象在别的进程里”这件事，包装成看起来像本地调用的体验？

### 4. 本文明确不展开的内容
- 不展开锁实现与并发控制
- 不展开登录链路
- 不展开战斗业务

### 5. 推荐二级标题结构
1. `代理层在语义上解决什么`
2. `本地调用体验和远程调用真实边界`
3. `为什么代理层是框架而不是业务代码`
4. `调用失败与位置变化如何处理`
5. `ET 为什么要做这一层抽象`

### 6. 读者前置知识
- 必须掌握：`ET-Pre-06`、`ET-Pre-08`
- 必须掌握：Location、Actor、远程对象
- 了解即可：代理模式、RPC 包装

### 7. 关键源码或文档锚点
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.actorlocation\Scripts\Hotfix\Server\LocationProxyComponentSystem.cs`
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.actorlocation\Scripts\Model\Server\LocationComponent.cs`

### 8. 文末导读建议
- 下一篇应读：`ET-23`
- 理由：代理能力说清后，再看锁与迁移才能知道它在保护什么

### 9. 证据标签
`源码拆解`

---

## ET-23
**选题：** `LocationOneTypeSystem` 里的加锁、迁移、解锁说明了什么  
**系列 / 位置：** ET 框架源码解析 / Part 4 第 3 篇  

### 1. 系列职责
解释 ActorLocation 为什么天然和并发控制、迁移协议、解锁语义绑在一起。

### 2. 与前一篇 / 后一篇的重复风险与处理方式
| 相邻文章 | 重复风险点 | 本文处理方式 |
|---|---|---|
| ET-22 代理层 | 容易重复远程调用体验 | 只讲位置变化时的控制与迁移 |
| ET-24 Router | 容易提前讲路由层 | 只讲 Location 迁移，不讲软路由体系 |

### 3. 本文必须回答的核心问题
为什么 ActorLocation 的实现离不开加锁、迁移和解锁？

### 4. 本文明确不展开的内容
- 不展开完整锁框架通用教程
- 不展开 Router 和网络攻击防护
- 不展开业务消息分发

### 5. 推荐二级标题结构
1. `为什么寻址系统需要锁`
2. `迁移与解锁在保护什么`
3. `位置变化和一致性边界`
4. `为什么 Location 天然会和并发冲突打交道`
5. `这套协议对框架设计的反作用`

### 6. 读者前置知识
- 必须掌握：`ET-Pre-06`、`ET-Pre-08`
- 必须掌握：Location、代理、跨进程对象
- 了解即可：锁、状态迁移、一致性

### 7. 关键源码或文档锚点
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.actorlocation\Scripts\Hotfix\Server\LocationOneTypeSystem.cs`
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.actorlocation\Scripts\Hotfix\Server\LocationProxyComponentSystem.cs`

### 8. 文末导读建议
- 下一篇应读：`ET-24`
- 理由：Location 的控制协议理解后，再看 Router 才能分清职责

### 9. 证据标签
`源码拆解`

---

## ET-24
**选题：** ET 的 Router/软路由应该怎样理解  
**系列 / 位置：** ET 框架源码解析 / Part 4 第 4 篇  

### 1. 系列职责
把路由从“网络转发器”抬升成“分布式部署与安全边界”的一部分，和 AOI、预测、回滚留出接口。

### 2. 与前一篇 / 后一篇的重复风险与处理方式
| 相邻文章 | 重复风险点 | 本文处理方式 |
|---|---|---|
| ET-23 Location 锁与迁移 | 容易把路由和迁移协议搅在一起 | 只讲路由层职责，不讲寻址内部协议 |
| ET-25 StartConfig / SceneType | 容易把路由和进程编排混为一体 | 只讲路由角色，不讲进程组织总表 |

### 3. 本文必须回答的核心问题
ET 的 Router 是网络中间件，还是分布式 MMO 运行时能力的一部分？

### 4. 本文明确不展开的内容
- 不展开全部软路由实现细节
- 不展开云原生与服务发现总论
- 不展开状态同步 Demo 代码

### 5. 推荐二级标题结构
1. `Router 先解决什么`
2. `软路由与对象寻址的边界`
3. `为什么 Router 会和 AOI、预测、回滚同时出现`
4. `Router 对分布式部署的意义`
5. `把 Router 放回 ET 的全局图`

### 6. 读者前置知识
- 必须掌握：`ET-Pre-10`
- 必须掌握：Location、AOI、预测、回滚
- 了解即可：网络路由、进程分发

### 7. 关键源码或文档锚点
- `E:\NHT\workspace\TechStackShow\.tmp\ET\Book\8.2ET Package目录.md`
- `E:\NHT\workspace\TechStackShow\.tmp\ET\README.md`
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.login\Scripts\Hotfix\Client\NetClient\Router\RouterHelper.cs`
- `E:\NHT\workspace\TechStackShow\.tmp\ET-Packages\cn.etetet.login\Scripts\Model\Client\NetClient\Router\RouterAddressComponent.cs`

### 8. 文末导读建议
- 下一篇应读：`ET-25`
- 理由：路由能力讲完后，需要把它落到服务端角色和进程编排上

### 9. 证据标签
`包目录解析`

---

## 交付注意
这批工作单的写作边界重点放在三条线上：
1. `ET-13 ~ ET-18` 只围绕并发、网络会话、Actor 和消息分发。
2. `ET-19 ~ ET-20` 只围绕客户端网络纤程和双阶段登录。
3. `ET-21 ~ ET-24` 只围绕 ActorLocation 与 Router，避免提前进入进程编排和同步 Demo 细节。

