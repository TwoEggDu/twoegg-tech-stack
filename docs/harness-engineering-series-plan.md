# Harness Engineering 系列计划

## 这份计划解决什么

这份计划是 `Harness Engineering` 系列的 canonical 篇级目录。

它负责回答：

1. 这个系列到底在解决什么问题
2. 8 篇正文各自承担什么职责
3. 跟 `AI 赋能游戏开发` 系列（尤其是 08 那篇）怎么接续
4. 系列索引页、栏目入口和正文更新时应该回写哪里
5. 哪些篇可以先写、哪些必须等真实任务记录补完再写

根 `doc-plan.md` 只保留系列级路由，不维护本系列篇级清单。
立项背景与未采纳方案见 [docs/harness-engineering-series-positioning.md](./harness-engineering-series-positioning.md)（只读历史快照，本计划稳定后不再回写）。

## 一句话定位

`Harness Engineering 不是讲怎么搭一个 v0，而是讲：当 v0 跑起来之后，怎样让它在游戏引擎客户端这种带历史包袱的场景下活下来、长出来、瘦下去。`

## 目标读者

- 游戏引擎 / 客户端方向工程师，Unity / Unreal 都覆盖
- 已经搭过 v0 Harness，想知道下一步该怎么演化的人
- 同一份代码 vendor 到多个项目的 SDK / Package 作者
- 想把 AI Coding 实践写进求职作品集的资深工程师

## 系列边界

### 属于这条线的内容

- 游戏引擎客户端独有约束下的 Harness 设计（C++ license / 生成代码 / Shader Variant / 多平台构建 / IL2CPP / HybridCLR）
- Harness 的生命周期：Bootstrap → Growth → Bloat → Drift → Sunset
- Harness 的诊断指标和反模式
- 跨仓库 / SDK vendor 场景下的 Harness 作用域分层
- Harness 跟交付工程、长线运营的联动
- AI Coding 任务的复盘与指标（基于真实执行记录）

### 不属于这条线的内容

- Prompt 工程技巧（属于上一代范式，不重讲）
- Spec-Driven Development 方法论（外部"5 人 7 天"已占位）
- Skill 标准模板与设计模式（外部"Agent Skill 规范"已占位）
- "AI Coding 率 90%"这类百分比叙事（容易被算虚，跟阿里那篇撞坐标）
- 金字塔 6 层基础概念（wiki ai-collaboration 系列覆盖，本系列假设已懂）
- CLAUDE.md / Skill / MCP 的入门解释（属于 `AI 赋能游戏开发` 05/06 的职责）
- 需要私仓 Unity C++ 源码才能成立的实现细节

## 与 `AI 赋能游戏开发` 系列的关系

- `AI 赋能游戏开发 08` ([content/ai-empowerment/ai-empowerment-08-ai-coding-harness-engineering.md](../content/ai-empowerment/ai-empowerment-08-ai-coding-harness-engineering.md)) 是**通用 v0 实施篇**——五层模型、状态机、五指标，留在 ai-empowerment 系列不动
- 本系列默认读者**已读过 08**，从"v0 跑起来之后"接续
- 本系列首篇会反向引用 08，作为"快速入门版"
- 本系列不重讲 08 已讲过的内容

## 与其他系列的关系

- `代码质量到工程质量`：负责质量判断、CI、review、Quality Gate；本系列只讨论 Harness 跟这些机制的接口，不重讲
- `质量护栏 / Quality Guardrails`：负责验证门禁分层；本系列引用，不重讲
- `交付工程旗舰专栏`：负责多端交付闭环；本系列有专篇讨论 Harness 在交付场景的位置
- `长线运营工程专栏`：负责长线运营场景；本系列有专篇讨论 Harness 在运营场景的位置
- `engine-toolchain / rendering / problem-solving`：本系列文章可反向引用这些栏目作具体案例

## 核心模型

```text
v0 实施（已在 ai-empowerment 08 覆盖）
        │
        ▼
游戏引擎客户端的独有约束
        │
        ▼
五阶段生命周期：Bootstrap → Growth → Bloat → Drift → Sunset
        │
        ├── Bloat 反模式与瘦身
        ├── Drift 与文档腐烂
        ├── 跨仓库作用域（SDK vendor）
        ├── 与交付工程联动
        ├── 与长线运营联动
        └── 复盘与指标
```

## 栏目与目录结构

- 栏目目录：`content/harness-engineering/`
- 栏目入口：`content/harness-engineering/_index.md`
- 系列索引页：`content/harness-engineering/harness-engineering-series-index.md`
- weight 区间：`2100-2180`（索引页 1，正文 2110-2180，步长 10）
- 子组规约：

| 子组 | weight 范围 |
|------|------------|
| 索引页 | 1 |
| 立意与领域差异化 | 2110 |
| 演化与诊断主轴 | 2120-2140 |
| 跨仓库作用域 | 2150 |
| 跟其他栏目联动 | 2160-2170 |
| 复盘与指标 | 2180 |

同子组内按 10 递增，插入时用 5 或 2 递增。

## 文章目录

### 01｜为什么游戏引擎客户端的 AI Coding 需要重新设计 Harness

- 正文：`content/harness-engineering/harness-engineering-01-why-game-engine-needs-rethink.md`
- 状态：`已写完整正文（首篇）`
- weight：`2110`，series_order：`10`
- 职责：建立"游戏引擎客户端"作为差异化领域，告诉读者这个系列在 ai-empowerment 08 之后接什么
- 必须回答：通用 v0 Harness 在游戏引擎客户端场景会遇到什么独有约束
- 不展开：五层 Harness 模型本身（由 08 覆盖）

### 02｜v0 之后——Harness 的五阶段生命周期

- 正文：`content/harness-engineering/harness-engineering-02-five-stage-lifecycle.md`
- 状态：`已写骨架，待补真实演化记录`
- weight：`2120`，series_order：`20`
- 职责：把 Bootstrap / Growth / Bloat / Drift / Sunset 五个阶段讲清楚，给出四个可操作诊断指标
- 必须回答：怎样判断 Harness 现在所处的阶段、什么信号说明该升级或瘦身
- 不展开：每个反模式的具体处理（由 03 / 04 覆盖）
- 关键 TODO：`EXPERIENCE-TODO` 需要补 1-2 个真实项目的阶段演化截图或日志

### 03｜Bloat 反模式与瘦身

- 正文：`content/harness-engineering/harness-engineering-03-bloat-and-slimming.md`
- 状态：`已写骨架，待补真实瘦身案例`
- weight：`2130`，series_order：`30`
- 职责：识别 Bloat 信号、给出瘦身手术清单、说明"主动放弃"为什么是 Harness 健康的指标
- 必须回答：什么时候 Harness 该停止扩展、规则与上下文该怎么裁
- 不展开：跟 Drift 的边界（由 04 覆盖）
- 关键 TODO：`EXPERIENCE-TODO` 需要补一次真实瘦身前后对比

### 04｜Drift 与文档腐烂

- 正文：`content/harness-engineering/harness-engineering-04-drift-and-rot.md`
- 状态：`已写骨架，待补真实 drift 案例`
- weight：`2140`，series_order：`40`
- 职责：讲 Harness 的规则与代码同步腐烂的机制、机械化门禁 vs 人工 review 的边界
- 必须回答：怎样在游戏项目快节奏迭代中防止 Harness 跟代码脱节
- 不展开：通用 CI/CD 实践（由 code-quality / quality-guardrails 系列覆盖）
- 关键 TODO：`DATA-TODO` 需要补一次真实"规则失效导致 AI 写错代码"的事故记录

### 05｜跨仓库 Harness：SDK vendor 视角

- 正文：`content/harness-engineering/harness-engineering-05-cross-repo-sdk-vendor.md`
- 状态：`已写骨架，待补真实 SDK 案例`
- weight：`2150`，series_order：`50`
- 职责：当同一份 SDK / Package vendor 到多个宿主项目时，Harness 信息归谁、规则冲突怎么解
- 必须回答：五层作用域（User-global / Org-Team / Host project / Package / Session）的判断流程
- 不展开：Package 自身的构建发布机制
- 关键 TODO：`EXPERIENCE-TODO` 大量留白，待作者决定能公开多少 SH7.SDK / Zhulong / ShanHai 的真实配置

### 06｜Harness 在交付工程里的位置

- 正文：`content/harness-engineering/harness-engineering-06-in-delivery-engineering.md`
- 状态：`已写骨架，待补真实交付场景`
- weight：`2160`，series_order：`60`
- 职责：在多端交付闭环里，Harness 跟构建脚本、产物校验、发布门禁怎么衔接
- 必须回答：交付工程视角下，AI 能接手哪些可重复任务、哪些必须人接
- 不展开：交付工程主线（由 `delivery-engineering` 栏目覆盖）
- 跟 `delivery-engineering` 栏目至少形成 2 处 relref

### 07｜Harness 在长线运营里的位置

- 正文：`content/harness-engineering/harness-engineering-07-in-live-ops.md`
- 状态：`已写骨架，待补真实运营场景`
- weight：`2170`，series_order：`70`
- 职责：在长线运营（活动配置、热更、A/B、玩家反馈）里，Harness 怎样把 AI 接进来
- 必须回答：运营期高频低风险任务的 Harness 化判断
- 不展开：运营工程主线（由 `live-ops-engineering` 栏目覆盖）
- 跟 `live-ops-engineering` 栏目至少形成 2 处 relref

### 08｜Harness 复盘与指标

- 正文：`content/harness-engineering/harness-engineering-08-retrospective-and-metrics.md`
- 状态：`已写骨架，强依赖真实执行记录`
- weight：`2180`，series_order：`80`
- 职责：在跑过 N 次 Harness 任务之后，怎样度量 Harness 真的让系统变好了
- 必须回答：五项最小指标的实操方法（一次交付可用率 / 人工返工点 / 规则违例数 / 验证通过率 / 反馈沉淀率）
- 不展开：通用 AI Coding 度量学（外部已有大量讨论）
- 关键 TODO：本篇是**最强依赖真实数据**的一篇，发布前必须有至少 3 次真实 Harness 任务记录

## 推荐阅读顺序

### 已读过 ai-empowerment 08 的人

1. 01：理解领域差异化
2. 02：建立生命周期心智模型
3. 03 / 04：理解 Bloat / Drift 两种主要失败模式
4. 05：如果你是 SDK / Package 作者，看跨仓库视角
5. 06 / 07：根据你当前关心的场景挑读
6. 08：等真实数据补完后再读

### 没读过 ai-empowerment 08 的人

1. 先回去读 [ai-empowerment-08](../content/ai-empowerment/ai-empowerment-08-ai-coding-harness-engineering.md)
2. 再按上面顺序回到本系列

### 主要做交付 / 运营的人

- 01 → 06（交付）或 01 → 07（运营），其余按需

## 当前状态

| 篇 | 状态 | 主要待补 |
|----|------|---------|
| 01 | 已写完整正文 | 等作者人工 review |
| 02 | 已写骨架 | 真实演化记录、阶段判断案例 |
| 03 | 已写骨架 | 真实瘦身前后对比 |
| 04 | 已写骨架 | 真实 drift 事故记录 |
| 05 | 已写骨架 | SH7.SDK / Zhulong / ShanHai 可公开配置范围确认 |
| 06 | 已写骨架 | 真实交付场景案例 |
| 07 | 已写骨架 | 真实长线运营场景案例 |
| 08 | 已写骨架 | 强依赖至少 3 次真实 Harness 任务记录 |

## 维护规则

1. 本文件是 `Harness Engineering` 的 canonical 篇级计划
2. 根 `doc-plan.md` 只维护本系列路由，不回填篇级清单
3. `content/harness-engineering/harness-engineering-series-index.md` 是发布侧索引页，应与本文件保持方向一致，但不替代本文件
4. 每新增或完成一篇文章，优先回写本文件的"当前状态"表格，再按需更新系列索引页
5. `docs/harness-engineering-series-positioning.md` 是立项备忘，本计划稳定后不再回写
6. 写作过程中如发现立意走偏，回去读 positioning 备忘，决定是修立意还是修执行
7. 跟 `AI 赋能游戏开发` 系列的接续点是 08 篇，**不得**在本系列重讲五层模型 / 状态机 / 五指标基础概念
