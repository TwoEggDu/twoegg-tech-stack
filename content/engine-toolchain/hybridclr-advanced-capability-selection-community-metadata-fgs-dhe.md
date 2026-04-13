---
date: "2026-03-23"
title: "HybridCLR 高级能力选型｜社区版主线、补 metadata、Full Generic Sharing、DHE 分别该在什么时候上"
description: "把四条路线重新压成一个选型问题：社区版主线、补充 metadata、Full Generic Sharing、DHE 分别在解决什么，什么时候该继续加码，什么时候反而该克制。"
weight: 41
featured: false
tags:
  - "Unity"
  - "IL2CPP"
  - "HybridCLR"
  - "Architecture"
  - "Tradeoff"
series: "HybridCLR"
---
> 这篇不再补新的底层细节，而是把前面几篇已经拆开的能力重新收回来，变成一个项目里真的能用的选型判断。因为一旦进入 `Full Generic Sharing` 和 `DHE` 这类高级能力，最容易出错的往往不是“不懂原理”，而是“在还没把问题分层之前，就太早上了更重的解”。

这是 HybridCLR 系列第 12 篇，也是当前这一轮最适合收口的一篇。  
前面我们已经把下面几条线分别拆开了：

- 社区版主线：动态程序集装载、解释执行、MethodBridge、资源挂载身份链
- 补充 metadata：AOT assembly metadata 可见性与 interpreter 兜底
- `Full Generic Sharing`：generic runtime 共享上限
- `DHE`：已进包 AOT 程序集的函数级差分执行

到这里，最自然的下一步就不再是继续加概念，而是回答一个更像项目问题的问题：

`这四条路线到底该怎么选？`

## 这篇要回答什么

这篇主要回答 5 个问题：

1. 社区版主线、补充 metadata、`Full Generic Sharing`、`DHE` 分别在解决什么。
2. 这四条路线为什么不是同级替代关系。
3. 当你遇到 AOT 泛型、包体、内存、性能或现有 AOT 模块热改问题时，最稳的进入顺序是什么。
4. 什么情况下应该继续往更高级能力上走，什么情况下反而该停下来。
5. 如果只想记住一条决策路径，最稳的版本是什么。

## 收束

HybridCLR 的高级能力选型，不是”哪个更强就上哪个”，而是先判断你现在缺的是哪一层：

- 动态装载主链
- metadata 可见性
- generic 覆盖率上限
- 修改现有 AOT 模块且保住未改动部分 AOT 性能

这四种缺口不是一层事。  
只要你先把问题分层，选型就会很自然。

## 先把四条路线放回正确位置

如果不先立坐标系，后面“怎么选”这个问题根本没法答。

### 1. 社区版主线：先把热更真正跑起来

这条线解决的是：

- 新程序集能不能进 `IL2CPP` runtime
- 方法能不能解释执行
- 跨 AOT / native 边界时桥接能不能补齐
- 资源上挂着的热更脚本能不能正确接回程序集身份链

它的核心价值不是“性能最强”，而是：

`先把全平台热更这件事工程化地跑通。`

所以如果你的项目现在还停留在：

- 程序集边界不稳
- 加载顺序不稳
- `MethodBridge`、`AOTGenericReference`、资源挂载链还没吃透

那你还没到该纠结 `FGS` 或 `DHE` 的阶段。

### 2. 补充 metadata：把 runtime 看不见的 AOT metadata 补回来

这条线解决的是：

- 某些 AOT assembly 的 method body、签名、泛型 metadata 在运行时看不见
- interpreter 需要拿到这些 metadata 才能继续执行

它的核心语义是：

`把问题接回 metadata 可见性 + interpreter 兜底。`

所以它适合处理的是：

- 很多 AOT 泛型问题里“runtime 看不见、解释不下去”的那一层

它不适合替你回答的是：

- 这个 generic 场景能不能不依赖 metadata dll
- 能不能不让 generic 调用掉进 interpreter
- 能不能直接热改现有 AOT 模块但保住未改动部分 AOT 性能

### 3. `Full Generic Sharing`：把 generic 覆盖率上限往上抬

这条线解决的是：

- 旧 generic sharing 覆盖率不够
- 补 metadata 虽然能救场，但 workflow、包体、内存和解释执行比例都还不够理想

它的核心语义是：

`尽量让更多 generic 调用不必再退回“补 metadata + interpreter”的兜底路线。`

所以它真正补的是：

- generic runtime 共享上限

它不是真正意义上的“更强补 metadata”，也不是在回答：

- 现有 AOT 模块到底怎么按函数做差分热改

### 4. `DHE`：把“修改现有 AOT 模块”这件事做成函数级混合执行

这条线解决的是：

- 你不只是新增热更逻辑
- 你是要直接改一个已经打进包体的 AOT 程序集
- 你又不想让未改动部分也一起退回解释执行

它的核心语义是：

`用离线差分得到的变化集，在运行时把“未变函数继续走 AOT、变更函数切到最新解释执行”这件事立起来。`

所以它真正补的是：

- 已进包 AOT 程序集的函数级分流能力

它不在回答：

- metadata 可见性够不够
- generic sharing 上限够不够

## 这四条路线为什么不是同级替代关系

把它们混成“4 选 1”是最常见、也最危险的误判。

更准确的关系其实是：

- 社区版主线是底座
- 补充 metadata 是社区版主线上处理 AOT metadata 缺口的一条关键能力
- `Full Generic Sharing` 是把 generic runtime 模型往下一层继续扩展
- `DHE` 是把“已进包 AOT 程序集怎么热改”往另一条轴继续扩展

也就是说，它们更像这样：

- 一条基础主线
- 一条围绕 AOT metadata / generic 可执行性的增强线
- 一条围绕 generic 覆盖率上限的高级线
- 一条围绕现有 AOT 模块差分热改的高级线

把成本和收益展开：

| 路线 | 解决的核心缺口 | 典型成本 | 许可 |
|---|---|---|---|
| 社区版主线 | 全平台热更跑通 | `Generate/All` 维护、MethodBridge 桥接表对齐 | 社区版 MIT |
| 补 metadata | AOT metadata 运行时可见性 | 每个 AOT assembly 多带一份裁剪后 dll；`LoadMetadataForAOTAssembly` 的内存与加载时间 | 社区版 MIT |
| Full Generic Sharing | generic 覆盖率上限 | 泛型函数调用走共享路径带来的性能折价（完全共享版本经 `Il2CppFullySharedGenericAny` 间接调用）；强依赖 Unity/IL2CPP 版本 | 社区版即可使用（v4.0.0+ 开始支持） |
| DHE | 已进包 AOT 程序集函数级差分热改 | 必须维护 `dhao` 产物链一致性；加载顺序更严格；不支持 extern 方法变更 | 需要商业许可 |

> 注意：Full Generic Sharing 从 HybridCLR v4.0.0 起在社区版即可使用；DHE 属于商业版能力，需要获取商业许可后才能接入。

所以最稳的工程判断不是：

`我要不要在四个里选一个最强的`

而是：

`我现在的问题到底落在哪一轴。`

## 真正决定选型的 4 个判断轴

如果只保留最有用的判断，项目里永远先看这 4 条。

### 第一轴：你是在“新增热更模块”，还是在“修改现有 AOT 模块”

这是第一分叉，而且通常已经能砍掉一半误判。

如果你主要是在：

- 新增玩法
- 新增活动
- 新增 UI 流程
- 新增逻辑模块

那默认应该先走社区版主线。  
很多项目在这里就已经够了。

如果你主要是在：

- 直接改一个已经打进包体的 AOT 模块
- 改动只占一小部分，但没改的部分又很重要

那你看的就是 `DHE` 这条轴，不是 `FGS`，也不是“再多补一点 metadata”。

### 第二轴：你缺的是 metadata 可见性，还是 generic 覆盖率上限

这条轴主要用来区分：

- 补充 metadata
- `Full Generic Sharing`

如果你遇到的问题更像：

- runtime 需要 method body / 泛型 metadata / 签名
- `LoadMetadataForAOTAssembly` 能明显救场

那你缺的是：

`metadata 可见性`

如果你遇到的问题更像：

- metadata 工作流越来越重
- 包体和内存对 metadata dll 很敏感
- 想减少 generic 场景退回 interpreter 的比例

那你缺的更像是：

`generic 覆盖率上限`

这时才轮到 `Full Generic Sharing` 真正进入方案表。

### 第三轴：你更敏感的是包体/内存，还是 generic 性能，还是整体执行路径

这条轴决定你是不是该继续加码。

如果你更敏感的是：

- metadata dll 带来的包体和内存
- generic 场景过多导致解释器兜底比例太高

那 `FGS` 的价值会更明显。

如果你更敏感的是：

- 一个已经进包的 AOT 模块里，只有少量函数被改
- 未改动函数必须尽量继续保留 AOT 表现

那 `DHE` 的价值会更明显。

如果你更敏感的是：

- 项目架构清晰
- 功能热更为主
- 能接受补充 metadata 的成本

那继续留在社区版主线，通常反而是最稳的选择。

### 第四轴：你的 Unity 版本、加载顺序和资源组织，能不能承受高级能力前提

这是很多团队最容易忽略的一条。  
不是“想上”就能上。

如果是 `FGS`，你至少要看：

- Unity 版本是不是在可用区间
- 你能不能接受 generic function 的性能折价

如果是 `DHE`，你至少要看：

- 对应 AOT 程序集会不会在热更加载前抢跑
- 能不能稳定维护 `AOT dll / 最新热更 dll / dhao` 三者一致性
- 相关脚本是不是还依赖打进包体资源挂载

只要这条轴不满足，高级能力就不该进入主方案。

## 最稳的默认进入顺序

最少犯错的默认顺序：

1. 先把社区版主线吃透并固化  
   程序集边界、加载顺序、metadata 装载、AOTGenericReference、MethodBridge、资源挂载身份链先稳定。

2. AOT 泛型问题先用“补 metadata + 工程约束”吃透  
   先分清楚哪些是 metadata 问题，哪些是 native instantiation 缺口。

3. 如果 generic 压力、包体或内存问题继续放大，再认真评估 `FGS`  
   这是“generic 覆盖率上限”那条轴，不要拿它解决别的问题。

4. 如果真正痛点变成“直接改现有 AOT 模块且要保住未改动函数 AOT 性能”，再评估 `DHE`  
   这是“函数级差分执行”那条轴，也不要拿它来替代社区版主线。

这个顺序看起来保守，但工程上最稳。

## 什么时候该继续往上走，什么时候该停下来

下面是一个很实际的判断。

### 继续留在社区版主线，通常是对的

下面这些场景，默认先别上更重能力：

- 主要是新增业务逻辑
- 热更模块边界还能继续优化
- 补 metadata 工作流已经稳定
- 资源挂载链是核心工作流
- 团队还没把加载顺序和排障顺序固化下来

这时候最该做的不是上更高级能力，而是把当前主线做稳。

### 认真考虑 `FGS`，通常是这些信号

- generic 场景很多，尤其值类型和复杂组合明显增多
- metadata dll 的包体、内存和下载链越来越重
- 你想减少 generic 场景退回 interpreter 的比例
- Unity 版本已经进入可用区间

### 认真考虑 `DHE`，通常是这些信号

- 真正要热改的是现有 AOT 模块本身
- 改动只占其中一小部分
- 未改动部分又明确是性能热点
- 你能严格控制加载顺序
- 你能把 `dhao` 产物链纳入稳定构建流程

## 最容易犯的 4 个选型错误

最后把最常见的误判收一下。

### 错误一：把补 metadata 当成 generic 问题的总解

它能解决很多问题，但它解决的是 metadata 可见性，不是 generic runtime 上限。

### 错误二：把 `FGS` 当成“更强补 metadata”

它真正改的是 generic 调用模型和共享上限，不是 metadata 文件多一点少一点。

### 错误三：把 `DHE` 当成“解释器更快一点”

它真正改的是“已进包 AOT 程序集内部，哪些函数继续走 AOT、哪些函数切解释执行”。

### 错误四：在社区版主线还没稳定前，就急着上高级能力

这样通常只会把原本能排清楚的问题，变成更复杂的混合问题。

## 收束

四条路线各管一层：

- 社区版主线：把热更真正跑通
- 补 metadata：把 AOT metadata 可见性补回来
- Full Generic Sharing：把 generic 覆盖率上限往上抬
- DHE：把”修改现有 AOT 模块”做成函数级混合执行

真正稳的选型顺序，不是先挑最强能力，而是先判断你现在到底缺的是哪一层。

## 系列位置

- 上一篇：<a href="{{< relref "engine-toolchain/hybridclr-dhe-why-not-just-faster-interpreter.md" >}}">HybridCLR DHE｜为什么它不是普通解释执行更快一点</a>
- 下一篇：<a href="{{< relref "engine-toolchain/hybridclr-faq-10-most-confused-judgments.md" >}}">HybridCLR 高频误解 FAQ｜10 个最容易混掉的判断</a>
