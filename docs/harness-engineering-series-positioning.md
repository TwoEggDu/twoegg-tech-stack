# Harness Engineering 系列 · 定位备忘

> 角色：系列启动前的"错错可查点"
> 状态：备忘（非 canonical plan）
> 版本：2026-05-12（v1，定位首版）
> 后续：定位稳定后，转写为 `docs/harness-engineering-series-plan.md`

---

## 这份备忘负责什么

记录"为什么这个系列要这样定位"的全部判断依据。以后写到一半发现方向不对、或者想加一篇但不确定该不该加，回来读这份备忘。

不负责：
- 篇级目录（那是未来 series-plan 的事）
- 写作大纲（定位稳定后再做）
- 跟 doc-plan.md 的路由对接（系列正式启动时再补）

---

## 一句话定位

**Harness Engineering 不是讲怎么搭一个 v0，而是讲：当 v0 跑起来之后，怎样让它在游戏引擎客户端这种带历史包袱的场景下活下来、长出来、瘦下去。**

副标题候选：

- 从 Unity/Unreal 客户端的视角，重新设计 AI Coding Harness
- v0 之后——Harness 该长肌肉还是该减肥
- 给游戏引擎工程师的 Harness 演化与诊断

---

## 立意：A + B 混合

骨干 A，主轴 B，C 作为后期单篇。

### A：游戏引擎客户端 Harness（骨干）

- 受众：游戏引擎 / 客户端方向工程师，Unity / Unreal 都覆盖
- 独家约束：C++ 引擎源码 license 限制、生成代码目录、Shader Variant、AssetBundle、多平台构建、IL2CPP / HybridCLR 等运行时
- 跟 TechStackShow 现有栏目（rendering / engine-toolchain / delivery-engineering / live-ops-engineering / problem-solving）形成网状引用
- 跟作者求职定位（资深架构 / Tech Lead）天然对齐

### B：演化与诊断（主轴）

- 五阶段生命周期：Bootstrap → Growth → Bloat → Drift → Sunset
- 四诊断指标：Context bloat 比 / Skill 复用率 / Memory 沉淀率 / Repetition rate
- 这是外部 4 篇全部没碰到的空白
- 防止系列退化成"我搭了个 Harness，效率提升 90%" 的复述

### C：跨仓库作用域（后期单篇，不做主轴）

- SH7.SDK 嵌套在 Zuma、Zhulong 跨 4 个项目这类真实场景
- 五层作用域 + 三步判断流程 + 四种 SDK 模式
- 单独成篇，不撑起整个系列骨干（受众太窄）

---

## 为什么是 A+B 而不是 A / B / C 任一种

### 纯 A 的问题

立意：游戏引擎客户端 Harness 实践。

- 优点：领域差异化最强，跟外部 4 篇完全错位
- 缺点：会跟外部"个人搭建实录"类文章状态接近——只是换了个领域
- 风险：写成"我在 Unity 项目里搭了个 Harness"——还是 v0 故事

### 纯 B 的问题

立意：Harness 的演化、诊断、瘦身、退役。

- 优点：独家空白最大，外部 0 篇覆盖
- 缺点：需要作者手上至少有 1-2 个"走完完整生命周期"的真实案例支撑
- 风险：现阶段作者的 Harness 主要在 v0 / Growth 阶段，Bloat / Drift / Sunset 没有亲历素材，写成纯理论

### 纯 C 的问题

立意：跨仓库 / SDK vendor 视角的 Harness 分层。

- 优点：跟作者的真实工作场景天然对齐
- 缺点：受众太窄（只服务 SDK / 共享工具链作者）
- 风险：撑不起一个完整系列，更像是 1-2 篇深度文章

### A+B 的取舍

- A 提供具体场景和案例支撑（防止 B 退化为纯理论）
- B 提供元方法论主轴（防止 A 退化为又一份 v0 故事）
- C 留到系列中后期某一篇专门写（SH7.SDK / Zhulong 视角的跨仓库 Harness）

---

## 跟既有素材的关系

### TechStackShow 已发布的 ai-empowerment-08

- 路径：[content/ai-empowerment/ai-empowerment-08-ai-coding-harness-engineering.md](../content/ai-empowerment/ai-empowerment-08-ai-coding-harness-engineering.md)
- 已在 `ai-empowerment` 系列内（weight 180）
- 处理方式：**留在原位不动**
  - 08 仍然是 ai-empowerment 系列的"v0 实施篇"
  - 新 Harness Engineering 系列把 08 当成"快速入门版"反向引用
  - 新系列首篇不重讲五层模型、不重讲状态机，直接从"游戏引擎客户端的 v0 长什么样"切入
  - 当读者想看通用 v0 实施指南，回 08；想看演化诊断 + 游戏引擎案例，看新系列

### E:/workspace/wiki/content/guides/harness-engineering/ 系列（4 篇）

| 文件 | 处理方式 |
|------|---------|
| 00-why.md（范式跃迁 + Agent 翻车 + 机械化门禁） | 核心论点搬入新系列首篇，但需重写（去掉内部坐标）|
| 01-evolution.md（五阶段 + 四诊断指标） | **直接作为新系列的主轴骨架**，重写为公开版本 |
| 02-scope.md（作用域分层 + 四种 SDK 模式） | 留到中后期作为 C 视角的单篇素材 |
| 03-wiki-practice.md（wiki 自身 Bootstrap → Growth 实录） | 作为结构模板参考，但场景必须换成游戏引擎案例 |

### E:/workspace/wiki/content/guides/ai-collaboration/ 系列（7 篇）

- 这是新系列的**前置基础**，不重复内容
- 新系列首篇假设读者已经懂"金字塔 6 层"概念，或者反向引用到 ai-collaboration 系列
- DP 项目案例（Unity MMO）可以在新系列里被引用，但要标注是哪个项目

### E:/harness/ 4 篇外部参考

- 不直接引用，但用作"差异化坐标系"
- 在系列首篇或元方法论篇里，可以列出"外部生态已经讲了什么 / 这个系列不重讲什么"

---

## 系列的"不写什么"

明确列出不写的，比写什么更重要。

- **不写 Prompt 工程技巧** ——外部文章普遍认为这是上一代范式
- **不写 Skill 标准模板和设计模式** ——外部"Agent Skill 规范"已占
- **不写 Spec-Driven Development** ——外部"5 人 7 天" 已占，且与 Harness 不是同一抽象层
- **不写"AI Coding 率 90%"这类百分比** ——容易被算虚，且容易跟阿里那篇撞坐标
- **不重讲金字塔 6 层** —— ai-collaboration 系列覆盖，新系列假设读者已经懂
- **不重讲 CLAUDE.md / Skill / MCP 的基础概念** ——这是 ai-empowerment 05/06 和 ai-collaboration 01-03 的事
- **不写企业级团队提效故事** ——这是外部 4 篇的主战场，作者也没这个素材

---

## 系列的"差异化卖点"清单

- **领域**：游戏引擎客户端 + Unity / Unreal + C++ 引擎 license 边界
- **元方法论**：Harness 的演化、诊断、瘦身、退役（外部空白）
- **反模式与失败**：Bloat / Drift / 过度工程化陷阱（外部空白）
- **跨仓库**：SDK vendor 到多宿主的 Harness 分层（外部空白）
- **个人 / 小团队视角**：不是"团队 20 人 7 天" 而是"一个工程师 + AI 把活做完"
- **跟现有栏目联动**：rendering / engine-toolchain / delivery / live-ops / problem-solving 都能反向引用 Harness 系列

---

## 篇级骨架（初稿，不约束最终结构）

> 这里只是为了让"立意"看起来不悬空——具体篇级目录留给后续 series-plan。

候选篇序：

1. **为什么游戏引擎客户端的 AI Coding 需要重新设计 Harness**
   - 引擎源码 license / 生成代码 / Shader Variant / 多平台构建带来的独有约束
   - 接上 ai-empowerment-08，告诉读者新系列站在哪
2. **v0 之后——Harness 的五阶段生命周期**
   - Bootstrap → Growth → Bloat → Drift → Sunset
   - 四诊断指标的可操作定义
3. **Bloat 反模式与瘦身**
   - 什么时候 Harness 该停下来不再扩展
   - "好 Harness 不只是加什么，更是主动放弃什么"
4. **Drift 与文档腐烂**
   - Harness 的规则和代码不同步时怎么办
   - 机械化门禁 vs 人工 review 的边界
5. **跨仓库 Harness：SDK vendor 视角**
   - C 立意的承载篇
   - SH7.SDK / Zhulong / ShanHai 的真实案例（如可公开）
6. **Harness 在交付与运营场景里的位置**
   - 跟 delivery-engineering / live-ops-engineering 栏目联动
7. **可选：Harness 复盘与指标**
   - 接 ai-empowerment 系列 plan 里挂着的"09 指标与复盘"
   - 等到至少有 2-3 次真实任务记录后再写

---

## 启动前的两个待确认项

1. **C 视角（跨仓库）那一篇要不要在第一轮系列里写**
   - 取决于 SH7.SDK / Zhulong 的工作场景能否公开化
   - 不能公开化的话，从首轮去掉，留到二期补
2. **是否新建栏目 `content/harness-engineering/`，还是挂在现有 `code-quality/` 或 `engine-toolchain/` 下**
   - 新建栏目：独立性强、首页易设计、但跟 AI 赋能、代码质量栏目语义重叠
   - 挂现有栏目：复用栏目入口，但 Harness 主题被淹没
   - 倾向新建，但要等启动 series-plan 时再定

---

## 维护规则

1. 本文件是定位备忘，不是 canonical plan
2. 启动 `docs/harness-engineering-series-plan.md` 时，本文件转为只读历史快照，**不再更新**
3. 篇级标题、weight、状态等信息**永远不要回填到本文件**
4. 如果走到一半发现立意走偏，回来读这份备忘 → 决定是修立意还是修执行

---

## 参考资料指针（不复述内容）

- 自己已写：
  - [content/ai-empowerment/ai-empowerment-08-ai-coding-harness-engineering.md](../content/ai-empowerment/ai-empowerment-08-ai-coding-harness-engineering.md)
  - [docs/ai-empowerment-series-plan.md](./ai-empowerment-series-plan.md)
- 内部工作笔记（wiki）：
  - `E:/workspace/wiki/content/guides/harness-engineering/` 4 篇 + _index
  - `E:/workspace/wiki/content/guides/ai-collaboration/` 7 篇 + _index
- 外部参考：
  - `E:/harness/` 4 篇 HTML（article1.txt / article2.txt 已有纯文本）
