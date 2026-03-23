# 数据导向运行时系列规划

## 定位

这组文章不是 `DOTS 教程`，也不是 `Mass 入门课`，更不是“从零手写一个 ECS 框架”的代码连载。

它真正想做的事情是：

`把 Unity DOTS、Unreal Mass 和自研 ECS 放到同一张问题地图里，讲清楚它们分别在解决什么、回避什么、代价落在哪。`

这组文章的目标读者不是只想记住几个 API 名字的人，而是想回答下面这些问题的人：

- 为什么现代引擎都在做数据导向的“特区”或“孤岛”
- Unity 和 Unreal 明明都在做大规模仿真，为什么最后长成了两种很不一样的系统
- 如果自己手搓一套，哪些东西是第一版必须有的，哪些东西是后面才值得做的
- 哪些结论是源码能直接证明的，哪些只是工程上的合理推断

一句话说，这套系列的重点不是“会不会用”，而是“看不看得懂这类系统为什么会这样长出来”。

## 为什么要把三条线写在一起

如果只写 `Unity DOTS`，文章很容易滑成 package 教程。

如果只写 `Unreal Mass`，文章很容易滑成插件介绍。

如果只写“自己手搓 ECS”，文章又很容易滑成玩具框架实现。

把三条线放在一起的价值，反而更大：

- `Unity DOTS` 更像“试着把一大块运行时重新建成 ECS 世界”
- `Unreal Mass` 更像“在 Actor 引擎内部划出一块高规模数据导向仿真区”
- `自研 ECS` 则会逼你把引擎名词剥掉，只剩真正的结构约束

这三条线并列之后，很多平时看起来像“框架差异”的东西，会露出更稳定的本质：

- 大规模同构对象到底怎样存
- 结构变化为什么一定贵
- 调度为什么必须显式声明读写边界
- 为什么表示层通常不能和仿真层完全混在一起
- 为什么构建期转换会越来越重

## 基线版本

为避免把早期预览版 DOTS、老的 Unreal 插件和现在的系统混在一起，这组文章默认按下面这条基线写：

- `Unity`：以 `Unity 6` 时代的 `Entities 1.x`、`Burst 1.8.x`、`Jobs`、`Collections`、`Mathematics` 为主
- `Unreal`：以 `Unreal Engine 5.6 / 5.7` 的 `MassEntity`、`MassGameplay` 为主
- `自研`：只讨论第一版真正值得做的最小系统，不假装一步做出“开源版 DOTS / Mass”

其中真正要避免的一个误区是：

`不要拿 Unity DOTS 0.x 时代的印象，去理解现在的 Entities 1.x；也不要拿“Unreal 一直是 Actor 世界”的旧直觉，忽略 Mass 这套数据导向仿真层。`

## 源码与证据范围

这组系列的证据默认分成 4 层。

### 1. Unity 引擎源码

当前已确认可用：

- `E:\HT\Projects\UnitySrcCode`

这一层主要负责回答：

- Unity 的运行时、构建期和引擎边界到底怎样接上 DOTS 相关能力
- 哪些结论来自 engine 层现有机制，而不只是 package 表面 API

### 2. Unity package 源码

当前已定位到的本地 package 缓存包括：

- `E:\HT\Projects\DP\TopHeroUnity\Library\PackageCache\com.unity.burst@1.8.23`
- `E:\HT\Projects\DP\TopHeroUnity\Library\PackageCache\com.unity.collections@2.5.7`
- `E:\HT\Projects\DP\TopHeroUnity\Library\PackageCache\com.unity.jobs@0.70.0-preview.7`
- `E:\HT\Projects\DP\TopHeroUnity\Library\PackageCache\com.unity.mathematics@1.3.2`
- `E:\HT\Projects\PX\ProjectX\Library\PackageCache\com.unity.burst@1.8.23`
- `E:\HT\Projects\PX\ProjectX\Library\PackageCache\com.unity.collections@2.5.7`
- `E:\HT\Projects\PX\ProjectX\Library\PackageCache\com.unity.jobs@0.70.0-preview.7`
- `E:\HT\Projects\PX\ProjectX\Library\PackageCache\com.unity.mathematics@1.3.2`

当前还没有在本地直接定位到稳定可引用的 `com.unity.entities@1.x` 路径。

所以现阶段的策略是：

- `Burst / Collections / Jobs / Mathematics` 可以先按本地 package 源码写
- `Entities 1.x` 在没有本地路径前，先以官方文档和当前项目实际接入为主证据
- 一旦本地补齐 `Entities` package，再把 archetype、chunk、query、structural change 这些文章升级成更硬的源码导读

### 3. Unreal 证据

当前还没有在本机快速定位到稳定可引用的 `Unreal Engine` 源码路径。

所以 Unreal 这一侧，第一阶段默认以官方文档和 API 为主证据，重点盯：

- `MassEntity`
- `MassGameplay`
- `FMassEntityManager`
- `FMassEntityQuery`
- `UMassProcessor`
- `FMassCommandBuffer`

如果后面补齐本地 Unreal 源码路径，再把 Unreal 这部分升级成源码级对照。

### 4. 项目实证与自研实现

这组系列不只讲引擎框架，还要回到两类更硬的材料：

- 真实项目里为什么会需要这类系统
- 如果自己手搓，第一版到底应该做到哪里

也就是说，这组文章最后不是停在“框架结构图”，而是要收回成工程判断。

## 统一写法约束

整组系列统一遵守下面这些约束。

- 每篇只回答一个主问题，不把存储、调度、表示层、网络复制全塞到一篇里
- 先给问题地图，再进源码和实现细节
- 明确区分“源码直接证明的事实”和“从事实反推的工程判断”
- 不按目录平铺源码，只追一条主链
- 结尾必须收回到工程动作，而不是停在概念解释
- 每篇都要专门写“常见误解”

固定骨架建议如下：

1. 这篇要回答什么
2. 先给源码地图或结构地图
3. 先讲问题，不先讲接口
4. 关键数据结构或关键主链是什么
5. 这一层到底解决了什么问题
6. 从源码里能直接看见什么
7. 从这些事实能推回什么工程判断
8. 常见误解
9. 最后压成一句话

## 总主线

这组系列不按引擎分三条线平行写，而按问题拆。

更适合的总主线是：

`高规模对象与规则 -> 运行时数据布局 -> 查询与调度 -> 结构变化 -> 表示层边界 -> 构建期转换 -> 项目选型`

这个主线有一个很重要的好处：

- Unity、Unreal、自研可以在同一问题下直接对照
- 读者不会被引擎术语带偏
- 后面真要扩到网络复制、LOD、AI、群体仿真，也还有自然扩展位

## 系列结构

建议这组系列做成 `1 篇总论 + 7 篇主线 + 若干补篇`。

### 00｜总论：为什么现代引擎都在做“数据导向孤岛”

核心问题：
为什么现在值得把 `Unity DOTS`、`Unreal Mass` 和 `自研 ECS` 放在一起看。

这一篇负责做的事：

- 先把问题空间立住
- 讲清楚“数据导向”不是世界观口号，而是成本模型变化
- 讲清楚 Unity 和 Unreal 的选择其实不一样
- 说明为什么自己手搓一版最容易看见不变量

### 01｜Unity DOTS、Unreal Mass 与自研 ECS：问题空间怎么对齐

核心问题：
这三套东西到底像在哪里，不像在哪里。

重点对照：

- 世界观和边界
- 是否试图替代原有对象模型
- 仿真层与表示层怎么切
- 构建期和运行期各承担什么

这一篇的核心判断应该压成一句话：

`Unity 更像重建运行时，Unreal 更像划出特区，自研最适合把两者都压回结构问题。`

### 02｜Archetype、Chunk、Fragment：性能到底建在什么地方

核心问题：
为什么这类系统几乎都会收敛到“按相同组件组合聚类 + 连续存储 + 查询缓存”。

重点对照：

- EntityId 的组织方式
- archetype 的角色
- chunk 或 chunked storage 的意义
- fragment / component / tag 的边界
- query 为什么要缓存 archetype 集合

### 03｜Structural Change、Command Buffer 与同步点：为什么改结构总是贵

核心问题：
为什么给实体加减组件、迁移 archetype、延迟提交命令，这些东西几乎总会出现。

重点对照：

- Unity 的 structural change / sync point
- Unreal 的 `command buffer` / `Defer`
- 自研第一版为什么必须做 deferred structural change

这一篇要明确写出工程判断：

`数据导向系统真正怕的不是组件多，而是高频结构变化和边界打断。`

### 04｜调度怎么做：Burst/Jobs、Mass Processor、自己手搓执行图

核心问题：
为什么“数据连续”还不够，最后还是要走到显式调度。

重点对照：

- Unity 的 `Jobs + Burst + System` 协作关系
- Unreal 的 `Processor + Query + Task` 关系
- 自研执行图如何从单线程 phase 逐步演化

这一篇要避免的误区：

- 不要把 `Burst` 写成“自动优化器”
- 不要把并行写成“把 for 循环丢到多线程”

### 05｜构建期前移怎么做：Baking、Traits / Templates / Spawn、离线转换

核心问题：
为什么这些系统越来越喜欢把运行时代价前移到构建期。

重点对照：

- Unity 的 `Authoring -> Baking -> Runtime Data`
- Unreal 的 `Traits / Template / Spawn Data`
- 自研系统为什么也最好做一层离线转换，而不是运行时把对象拼出来

### 06｜表示层边界怎么切：GameObject、Actor、ISM 与 ECS 世界

核心问题：
为什么仿真层和表示层通常不能彻底合并。

重点对照：

- Unity 里 `GameObject/MonoBehaviour` 和 `Entities` 的共存边界
- Unreal 里 `Actor`、`Mass Representation`、`LOD`、`ISM` 的关系
- 自研时为什么最好一开始就做表示桥，而不是让 ECS 直接托管全部表现

### 07｜如果自己手搓，第一版最小系统应该做到哪里

核心问题：
如果不抄整套 DOTS / Mass，第一版应该先做什么。

建议第一版只做到：

- `EntityId(index + generation)`
- `Component/Fragment TypeId`
- `Archetype`
- `Chunk`
- `All/Any/None Query`
- `Deferred Structural Change`
- `Phase Scheduler`
- `Representation Bridge`
- `基础调试工具`

不要第一版就做：

- 复杂并行调度
- 网络复制
- 代码生成
- 反射型自动序列化
- “Burst 级”编译器优化

### 08｜什么时候值得上这套东西，什么时候不值得

核心问题：
什么场景值得引入这套成本，什么场景根本不值得。

重点对照：

- 高规模同构仿真
- 大量规则分层处理
- 需要强工具链和构建期转换
- 表示层与仿真层天然分离

不值得上的典型场景包括：

- 实体规模不大
- 行为强耦合、异构程度高
- 团队没有能力维护新工具链
- 只是为了“追新”

## 补篇方向

主线写稳之后，再补下面这些题会更顺：

- 网络复制与数据导向运行时
- 大规模群体 AI 和 StateTree / Utility 系统的结合
- 数据导向系统里的调试、可视化和 profiling
- 为什么“混合架构”几乎总是现实答案

## 首批推进顺序

这一轮不建议一口气把 8 篇都起满。

最稳的推进顺序是：

1. 先写 `00 总论`
2. 再写 `01 Unity / Unreal / 自研对照`
3. 再写 `03 Structural Change / Command Buffer`
4. 再写 `07 自研第一版应该做到哪里`

这样做的好处是：

- 先把地图立起来
- 再把三条线拉到同一问题空间
- 再把最关键的代价讲透
- 最后把抽象收回成可执行的工程动作

## 当前已知缺口

现阶段需要明确承认两件事：

1. `Entities 1.x` 本地 package 源码路径还没有直接固定下来  
2. `Unreal Mass` 本地源码路径也还没有直接固定下来

这不影响第一批文章起稿，但会影响后面两类文章的硬度：

- archetype / chunk / query 的源码级导读
- Unreal Mass 的实现级对照

所以第一阶段的处理方式是：

- 能用本地源码的地方，尽量用本地源码
- 还缺本地路径的地方，先用官方文档和 API 作为主证据
- 在文章里明确标注哪些结论是“当前文档级证据”，哪些是“本地源码级证据”

## 这一组文章最该避免的写法

- 不要写成 DOTS 教程连载
- 不要写成 Unreal 插件导览
- 不要把自研部分写成玩具框架炫技
- 不要把“数据导向”写成抽象口号
- 不要把 `ECS = 并行 = 高性能` 混成一句话
- 不要把表示层、调度、存储、构建期全部塞进一篇

## 最后压成一句话

如果这组系列最后只能让读者记住一句话，那应该是：

`现代引擎做数据导向，不是因为它们突然讨厌对象，而是因为在高规模同构仿真问题上，数据布局、结构变化、调度边界和表示层分离，迟早会把系统推向类似的形状。`
