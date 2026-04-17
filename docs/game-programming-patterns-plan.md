# 设计模式系列 — 完整写作计划

> 覆盖 GoF 经典、游戏特有模式、引擎架构模式、UI 架构、并发异步。
> 分两条线：**教科书线**（引擎无关、语言中立）+ **应用线**（按场景扩展）。
> 主计划在 [doc-plan.md § 系列七·B](../doc-plan.md) 中登记。

---

## 总体结构

```
┌────────────────────────────────────────┐
│  教科书线（引擎无关、语言中立）            │
│  - 纯 C# 例子                           │
│  - 讲模式本质、变体、对比                 │
│  - 任何场景都能引用                      │
│  路径: content/system-design/patterns/  │
└────────────────────────────────────────┘
              ↑
              │ 引用
              │
┌────────────────────────────────────────┐
│  应用线（按场景扩展）                     │
│  1. Unity 游戏应用（现有系列七·B 7 篇）   │
│  2. 引擎工程应用（构建系统案例）          │
│  3. 未来：渲染 / 后端 / AI 等              │
└────────────────────────────────────────┘
```

**两条线的分工**：
- **教科书**：讲清楚"这个模式是什么、本质在哪、何时适用"，纯 C# 代码，不绑定具体引擎
- **应用**：讲清楚"在这个具体场景里怎么用、为什么选/不选、取舍过程"，可以依赖 Unity/引擎/项目代码

**相互引用**：
- 教科书末尾：**在实际工程里怎么用** → 链接到各应用文章
- 应用开头：**模式基础** → 链接到教科书

---

## 文章结构模板

### 教科书（约 2500 ~ 3500 字）

```
1. 先看问题            普通业务场景切入（不限游戏）
2. 模式的解法           纯 C# 代码
3. 变体与兄弟模式        MacroCommand / 参数化 Strategy / ...
4. 对比其他模式          和相似模式的区别
5. 常见坑              滥用、反模式、典型误解
6. 性能考量（按需）      对性能类模式必加
7. 何时用 / 何时不用     适用边界
8. 相关模式             链接到教科书内其他模式
9. 在实际工程里          链接到应用线文章
```

### 应用（约 1500 ~ 2500 字）

```
1. 场景背景            这个工程/系统的问题
2. 遇到的真实问题        不用模式/用错模式的代价
3. 为什么选/不选这个模式  决策过程
4. 具体代码实现          真实项目代码
5. 取舍记录            讨论过的备选方案、拒绝的理由
6. 规模变化时会怎么演进   未来演进路径
7. 小结               3 个关键决策点
```

---

## 文件组织

### 教科书线

```
content/system-design/patterns/
├── _index.md                          # 教科书系列导航
├── patterns-01-why-patterns-matter.md # 导论
├── patterns-02-template-method.md
├── patterns-03-strategy.md
├── patterns-04-builder.md
├── patterns-05-facade.md
├── patterns-06-command.md
├── patterns-07-observer.md
├── patterns-08-chain-of-responsibility.md
├── ...
```

### 应用线

```
content/system-design/                 # Unity 游戏应用（现有系列七·B，保留）
├── pattern-01-why-games-need-patterns.md  ✅
├── pattern-02-command.md              ✅
├── pattern-03-observer-event-bus.md   ✅
├── pattern-04-object-pool.md          ✅
├── pattern-05-service-locator-di.md   ✅
├── pattern-06-state-fsm-behavior-tree.md  ✅
├── pattern-07-data-oriented-design.md ✅
└── ...

content/engine-toolchain/build-system/ # 构建系统工程案例
├── buildconfig-template-method.md
├── strategy-vs-template-method.md
├── buildplan-builder.md
├── buildcommand-facade.md
├── why-we-rejected-di.md
└── ...
```

---

# 教科书线（目标 44 篇）

*引擎无关、语言中立、可被任意应用场景引用。*

## Batch T-A：基础模式补齐（8 篇）

| 编号 | 标题 | Slug | 状态 |
|------|------|------|------|
| T-01 | Template Method：基类定流程，子类填细节 | patterns-02-template-method | 待写 |
| T-02 | Strategy：算法即对象，运行时替换 | patterns-03-strategy | 待写 |
| T-03 | Builder：分步构造复杂对象 | patterns-04-builder | 待写 |
| T-04 | Facade：简化复杂子系统的入口 | patterns-05-facade | 待写 |
| T-05 | Factory Method 与 Abstract Factory | patterns-09-factory | 待写 |
| T-06 | Decorator：动态叠加职责 | patterns-10-decorator | 待写 |
| T-07 | Adapter：接口翻译 | patterns-11-adapter | 待写 |
| T-08 | Chain of Responsibility：请求沿链传递 | patterns-08-chain-of-responsibility | 待写 |

## Batch T-B：GoF 行为型补齐（6 篇）

| 编号 | 标题 | Slug | 状态 |
|------|------|------|------|
| T-09 | Command：操作对象化 | patterns-06-command | 待写（教科书版本） |
| T-10 | Observer：一对多的事件通知 | patterns-07-observer | 待写（教科书版本） |
| T-11 | Memento：状态快照 | patterns-12-memento | 待写 |
| T-12 | Visitor：给数据结构加操作 | patterns-13-visitor | 待写 |
| T-13 | Mediator：多对多通信解耦 | patterns-14-mediator | 待写 |
| T-14 | Iterator：顺序访问集合 | patterns-15-iterator | 待写 |

## Batch T-C：GoF 结构型补齐（5 篇）

| 编号 | 标题 | Slug | 状态 |
|------|------|------|------|
| T-15 | Composite：树形结构统一接口 | patterns-16-composite | 待写 |
| T-16 | Flyweight：共享对象省内存 | patterns-17-flyweight | 待写 |
| T-17 | Proxy：替身控制访问 | patterns-18-proxy | 待写 |
| T-18 | Bridge：抽象与实现解耦 | patterns-19-bridge | 待写 |
| T-19 | Prototype：克隆复制 | patterns-20-prototype | 待写 |

## Batch T-D：并发 / 异步（4 篇）

| 编号 | 标题 | Slug | 状态 |
|------|------|------|------|
| T-20 | Promise / Future / async-await | patterns-21-async-await | 待写 |
| T-21 | Coroutine 的本质 | patterns-22-coroutine | 待写 |
| T-22 | Actor Model | patterns-23-actor-model | 待写 |
| T-23 | Pipeline / Pipes and Filters | patterns-24-pipeline | 待写 |

## Batch T-E：架构风格（4 篇）

| 编号 | 标题 | Slug | 状态 |
|------|------|------|------|
| T-24 | MVC / MVP / MVVM 对比 | patterns-25-mvc-mvp-mvvm | 待写 |
| T-25 | Pub/Sub vs Observer | patterns-26-pub-sub-vs-observer | 待写 |
| T-26 | 依赖注入与 Service Locator | patterns-27-di-vs-service-locator | 待写（教科书版本） |
| T-27 | Plugin 系统架构 | patterns-28-plugin-architecture | 待写 |

## Batch T-F：游戏引擎核心模式（10 篇）

*游戏/实时系统特有的架构模式，来自《Game Programming Patterns》。引擎无关，讲原理。*

| 编号 | 标题 | Slug | 状态 |
|------|------|------|------|
| T-28 | Game Loop：引擎的心跳 | patterns-29-game-loop | 待写 |
| T-29 | Update Method：组件怎么驱动更新 | patterns-30-update-method | 待写 |
| T-30 | Component：组合优于继承 | patterns-31-component | 待写 |
| T-31 | Dirty Flag：延迟计算与变化检测 | patterns-32-dirty-flag | 待写 |
| T-32 | Double Buffering：避免读写冲突 | patterns-33-double-buffering | 待写 |
| T-33 | Spatial Partition：空间分区 | patterns-34-spatial-partition | 待写 |
| T-34 | Event Queue：解耦时间维度的 Observer | patterns-35-event-queue | 待写 |
| T-35 | Type Object：数据驱动类型 | patterns-36-type-object | 待写 |
| T-36 | Subclass Sandbox：父类定义工具箱 | patterns-37-subclass-sandbox | 待写 |
| T-37 | Bytecode：脚本虚拟机与 DSL | patterns-38-bytecode | 待写 |

## Batch T-G：引擎架构模式（8 篇）

*自研引擎或深度引擎定制时的核心架构模式。*

| 编号 | 标题 | Slug | 状态 |
|------|------|------|------|
| T-38 | ECS 架构：Entity/Component/System 三段式 | patterns-39-ecs-architecture | 待写 |
| T-39 | Scene Graph：场景图与变换继承 | patterns-40-scene-graph | 待写 |
| T-40 | Render Pipeline 架构 | patterns-41-render-pipeline | 待写 |
| T-41 | Render Pass / Render Feature：可扩展渲染 | patterns-42-render-pass-feature | 待写 |
| T-42 | Command Buffer：GPU 命令延迟提交 | patterns-43-command-buffer | 待写 |
| T-43 | Resource Management 架构：引用计数与生命周期 | patterns-44-resource-management | 待写 |
| T-44 | Hot Reload 架构：HybridCLR / ILRuntime 的本质 | patterns-45-hot-reload | 待写 |
| T-45 | Shader Variant 管理：变体生命周期 | patterns-46-shader-variant | 待写 |

## Batch T-H：存量主题补教科书（3 篇）

*存量 Unity 应用线已覆盖的主题，补写对应的教科书版本，让教科书线主题完整。*

| 编号 | 标题 | Slug | 状态 |
|------|------|------|------|
| T-46 | Object Pool：对象池化通用原理 | patterns-47-object-pool | 待写（应用线已有 Unity 版） |
| T-47 | State：状态模式与状态机 | patterns-48-state | 待写（应用线已有 FSM/行为树版） |
| T-48 | 数据导向设计（DOD）：从 OOP 到 DOP | patterns-49-data-oriented-design | 待写（应用线已有 DOTS 版） |

---

# 应用线

## A-1：Unity 游戏应用（系列七·B，7 篇已完成）

*原有系列，保持 Unity/游戏视角。*

| 编号 | 标题 | Slug | 状态 |
|------|------|------|------|
| ✅ 模式-01 | 为什么游戏需要设计模式 | pattern-01-why-games-need-patterns | 已完成 |
| ✅ 模式-02 | Command 模式在游戏中的应用 | pattern-02-command | 已完成 |
| ✅ 模式-03 | Observer / Event Bus 在 Unity 中的实现 | pattern-03-observer-event-bus | 已完成 |
| ✅ 模式-04 | Object Pool：游戏里的对象池化 | pattern-04-object-pool | 已完成 |
| ✅ 模式-05 | Service Locator 与 DI 在 Unity 项目中 | pattern-05-service-locator-di | 已完成 |
| ✅ 模式-06 | State / FSM / 行为树 在游戏 AI 中 | pattern-06-state-fsm-behavior-tree | 已完成 |
| ✅ 模式-07 | Data-Oriented Design 与 DOTS | pattern-07-data-oriented-design | 已完成 |

详细改进方案见下方"存量处理策略"。

## 存量处理策略

*关于系列七·B 原有 7 篇（pattern-01 ~ 07）和新增教科书线如何共存。*

### 核心决策

1. **存量保留，不替换**：7 篇保持原有 Unity/游戏定位，不删除、不重写核心内容
2. **教科书主题补齐**：存量覆盖但教科书线缺失的主题（Object Pool / State / DOD），通过 Batch T-H 补齐教科书版
3. **存量改进回流**：教科书写完后，反过来给存量补"对比其他做法"和"常见坑"章节，并加上引用教科书版本的链接
4. **命名与分类明确化**：通过目录、文件名前缀和 Hugo `series` 标签清晰区分两条线

### 主题映射表

| 存量（Unity 游戏应用线）| 教科书线对应 | 关系 |
|-------------------------|-------------|------|
| pattern-01（为什么游戏需要模式）| 无 | 导论类，不需要教科书版 |
| pattern-02（Command）| T-09 patterns-06-command | 教科书讲本质、应用讲 Unity 落地 |
| pattern-03（Observer / Event Bus）| T-10 patterns-07-observer | 同上 |
| pattern-04（Object Pool）| **T-46 patterns-47-object-pool** | Batch T-H 新增 |
| pattern-05（Service Locator / DI）| T-26 patterns-27-di-vs-service-locator | 教科书综合讲 DI + SL |
| pattern-06（State / FSM / 行为树）| **T-47 patterns-48-state** | Batch T-H 新增，FSM/行为树留在应用线 |
| pattern-07（DOD / DOTS）| **T-48 patterns-49-data-oriented-design** | Batch T-H 新增教科书讲 DOP 原理 |

### 目录组织

```
content/system-design/
├── pattern-01-*.md ~ pattern-07-*.md   # 存量 Unity 应用线（保持）
│
└── patterns/                            # 新教科书线
    ├── _index.md
    ├── patterns-02-template-method.md
    ├── patterns-03-strategy.md
    ├── ... (45 ~ 48 篇)
```

**Hugo 分类：**
- 存量 7 篇：`series: "游戏编程设计模式"`
- 教科书 48 篇：`series: "设计模式教科书"`
- weight 区间：存量 729~734，教科书 902~949

### 存量改进阶段（教科书写完后回流）

待教科书线全部完成后，对 7 篇存量做二次改造：

**每篇补充：**
1. 开头加"**模式基础**：本文涉及 XXX 模式，纯理论请见 [XXX 教科书](../patterns/patterns-XX-xxx.md)"
2. 中段加"**对比其他做法**"章节（如 Command vs Memento 的撤销对比）
3. 末尾加"**常见坑**"章节
4. pattern-04（Object Pool）加实测数据（GC 压力对比）

工作量估计：7 篇 × 每篇 1000 字 ≈ 7000 字补充。

### 命名冲突避免

- 存量文件名前缀：`pattern-01-` ~ `pattern-07-`
- 教科书文件名前缀：`patterns-02-` ~ `patterns-49-`（注意 `patterns` 带 s）
- 通过目录（`./` vs `./patterns/`）和前缀（`pattern-` vs `patterns-`）双重区分
- Slug 也保持一致，避免 Hugo 路由冲突

---

## A-2：构建系统工程应用（5 篇）

*TopHero + Zuma 共享构建系统的真实架构决策记录。*

| 编号 | 标题 | Slug | 状态 |
|------|------|------|------|
| C-01 | BuildConfig 为什么是 Template Method：一次构建系统的核心决策 | buildconfig-template-method | 待写 |
| C-02 | Strategy vs Template Method：我们为什么没拆成策略接口 | strategy-vs-template-method | 待写 |
| C-03 | BuildPlanBuilder：Builder 模式在复杂构建计划中的应用 | buildplan-builder | 待写 |
| C-04 | BuildCommand：用 Facade 把五个组件包装成一个命令 | buildcommand-facade | 待写 |
| C-05 | 为什么我们拒绝了依赖注入：一次架构取舍 | why-we-rejected-di | 待写 |

## A-3：未来扩展（预留，不急）

| 应用场景 | 预想主题 |
|---------|---------|
| 渲染架构 | URP 里的责任链、RenderPass 里的 Strategy、Feature 的 Decorator |
| 后端架构 | ET 框架的 Actor、服务端的 Pub/Sub、DB 的 Repository |
| AI 架构 | 行为树的 Composite、GOAP 的 Chain of Responsibility |
| 热更架构 | HybridCLR 的 Bridge、AOT/热更代码的 Adapter |

---

## 写作顺序建议（本期重点）

### 第一轮：Template Method 成套（2 篇）

**先写最紧迫的一对：**
1. **T-01** 教科书：`patterns-02-template-method.md`
2. **C-01** 应用：`buildconfig-template-method.md`（引用 T-01）

目标：让教科书 + 应用的双线模式跑通一次，验证互相引用的体验。

### 第二轮：Strategy 成套（2 篇）

3. **T-02** 教科书：`patterns-03-strategy.md`
4. **C-02** 应用：`strategy-vs-template-method.md`（引用 T-01 和 T-02）

这一轮的价值：对比，讲清楚"为什么我们没拆成 Strategy"。

### 第三轮：Builder + Facade（4 篇）

5. **T-03** 教科书：`patterns-04-builder.md`
6. **C-03** 应用：`buildplan-builder.md`
7. **T-04** 教科书：`patterns-05-facade.md`
8. **C-04** 应用：`buildcommand-facade.md`

### 第四轮：DI 决策综合篇（1-2 篇）

9. **T-26** 教科书：`patterns-27-di-vs-service-locator.md`（综合讲 DI 和 Service Locator）
10. **C-05** 应用：`why-we-rejected-di.md`

**本期目标：写完 9-10 篇，完成构建系统决策记录**

---

## 跨篇引用规划

### 教科书内部引用

每个教科书文章末尾列"**相关模式**"：
- Template Method ↔ Strategy：近亲模式
- Template Method ↔ Hook Method：嵌套关系
- Builder ↔ Factory：构造对比
- Facade ↔ Mediator：职责对比
- Command ↔ Memento：撤销的两种思路
- DI ↔ Service Locator：反向对比

### 教科书 → 应用的引用

每个教科书文章末尾"**在实际工程里怎么用**"：
- Template Method 教科书 → BuildConfig 案例、游戏 Update 方法案例
- Strategy 教科书 → Strategy vs Template 案例、AI 行为切换案例
- Builder 教科书 → BuildPlanBuilder 案例、装备 Builder 案例
- Facade 教科书 → BuildCommand 案例、AudioManager 案例

### 应用 → 教科书的引用

每个应用文章开头"**模式基础**"：
- 构建系统 Template Method → 链接到教科书 Template Method
- 游戏 Command 模式 → 链接到教科书 Command

---

## 和实际工程的映射

写作时可以从这些真实代码里取例子：

| 模式 | 工程例子 |
|------|---------|
| Template Method | `BuildConfig`（共享构建系统）、Unity `MonoBehaviour.Awake/Update` |
| Strategy | Codex 建议的 `IVersionStrategy`（我们讨论过的备选）、AI 行为切换 |
| Builder | `BuildPlanBuilder`、Unity UI Builder、装备合成 |
| Facade | `BuildCommand`、`AudioManager`、`ResourceManager` |
| Command | `ITiangongCommand`、游戏技能系统、`DPJenkinsBuild` |
| Chain of Responsibility | URP `ScriptableRenderPass`、`CommandDispatcher`、输入事件 |
| Factory | `BuildConfigDiscovery` 反射扫描、宠物/梦灵生成、装备工厂 |
| Adapter | `DPAssetConfigAdapter`、第三方 SDK 接入 |
| Observer | `S_GameEventModule`、`OnPreBuild/OnPostBuild` 钩子 |
| Object Pool | 子弹池、特效池、伤害飘字池 |
| State / FSM | 战斗单位状态、UI 状态、`PetStateMachine` |
| ECS | TEngine 模块系统、未来 DOTS |
| Hot Reload | HybridCLR（DP 项目已用）、AOT meta DLL 同步 |
| Pipeline | 构建 Pipeline、Zhulong Python Step 链 |
| Actor Model | ET 框架 |

---

## 完成度追踪

**教科书线**：
- 已完成：0 / 48
- Batch T-A：0 / 8（优先，GoF 基础补齐）
- Batch T-B：0 / 6（GoF 行为型补齐）
- Batch T-C：0 / 5（GoF 结构型补齐）
- Batch T-D：0 / 4（并发/异步）
- Batch T-E：0 / 4（架构风格）
- Batch T-F：0 / 10（游戏引擎核心模式）
- Batch T-G：0 / 8（引擎架构模式）
- Batch T-H：0 / 3（存量主题补教科书：Object Pool / State / DOD）

**存量改进**：
- 存量 Unity 应用线 7 篇：已完成基础版本
- 存量改进（对比/反模式/引用教科书）：待教科书线完成后回流

**应用线**：
- Unity 游戏应用：7 / 7 ✅（保持现状）
- 构建系统案例：0 / 5（本期目标）
- 未来扩展：0 / ?

**本期（DP+Zuma 构建系统）目标**：9-10 篇（4 教科书 + 5 应用）

---

## 文档位置

- **本文件**：`docs/game-programming-patterns-plan.md`（本计划）
- **主计划**：`doc-plan.md § 系列七·B`（含引用）
- **教科书目录**：`content/system-design/patterns/`
- **Unity 游戏应用目录**：`content/system-design/`（系列七·B）
- **构建系统案例目录**：`content/engine-toolchain/build-system/`
