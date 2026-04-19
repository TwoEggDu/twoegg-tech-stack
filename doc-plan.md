# 游戏开发全栈知识体系 · AI 入口索引

> 角色：根入口 / 路由页 / 状态页
> 目标：让人和 AI 都先定位主题，再跳到唯一的 canonical plan
> 版本：2026-04-18（v27，结构重构版）

---

## 这份文件现在负责什么

- 只保留系列级信息：主题范围、canonical 文件、状态、迁移情况
- 作为全站规划的唯一入口，不再承担篇级总表职责
- 帮人和 AI 快速判断“现在应该读哪份 plan”

## 这份文件不再负责什么

- 不再维护 `600+` 条篇级标题
- 不再重复系列专属 `*-series-plan.md` 的详细目录
- 不再承担 batch、执行排期、单篇 outline 的细节说明

---

## 读取规则

1. 先看“站点级总控文档”，判断当前站点优先级和主线。
2. 再看“系列路由表”，跳到对应的 canonical plan。
3. 只有在需要旧编号、历史篇级明细、或尚未拆出的主题时，才去读归档。
4. 如果一个主题已经有独立 `*-series-plan.md`，那份文件才是 source of truth。

---

## Canonical Source 规则

- 根 `doc-plan.md`：只存系列级路由信息。
- 独立 `*-series-plan.md`：该系列的唯一 canonical plan。
- `*-execution-plan.md` / `*-outline.md` / `*-workorders*.md`：执行层，不反向覆盖系列 plan。
- 历史篇级总表已经迁到 [docs/doc-plan-archive-v26.md](./docs/doc-plan-archive-v26.md)。
- [docs/doc-plan.md](./docs/doc-plan.md) 是更早的历史快照，不再作为入口使用。

---

## 站点级总控文档

| 文档 | 用途 | 何时优先读 |
|------|------|-----------|
| [docs/site-entry-and-audience-plan.md](./docs/site-entry-and-audience-plan.md) | 站点定位、入口设计、目标读者 | 需要判断“站点先服务谁”时 |
| [docs/site-full-expansion-plan-2026-04.md](./docs/site-full-expansion-plan-2026-04.md) | 全站扩张地图 | 需要看全局版图时 |
| [docs/tech-foundation-priority-plan.md](./docs/tech-foundation-priority-plan.md) | 技术底座优先级 | 需要决定“先补哪条底座”时 |
| [docs/execution-priority-plan-2026-04.md](./docs/execution-priority-plan-2026-04.md) | 近期执行顺序 | 需要排近期写作顺序时 |
| [docs/series-planning-method.md](./docs/series-planning-method.md) | 系列规划方法论 | 需要新建系列或重构系列时 |

---

## 系列路由表

### 基础能力与运行时

| 主题 | Canonical 文件 | 状态 | 备注 |
|------|----------------|------|------|
| 代码质量到工程质量 | [docs/code-quality-to-engineering-quality-series-plan.md](./docs/code-quality-to-engineering-quality-series-plan.md) | 已拆出 | 软件工程与工程质量主线 |
| 数据导向运行时 | [docs/data-oriented-runtime-series-plan.md](./docs/data-oriented-runtime-series-plan.md) | 已拆出 | `DOD-00~06` 架构哲学层 |
| DOTS / Mass / 硬件深挖 | [docs/dots-mass-hardware-deep-series-plan.md](./docs/dots-mass-hardware-deep-series-plan.md) | 已拆出 | 承接数据导向运行时 |
| 自研 ECS 落地 | [docs/self-built-ecs-implementation-plan.md](./docs/self-built-ecs-implementation-plan.md) | 已拆出 | 偏实现与工程落地 |
| 从 C# 到 CLR | [docs/csharp-to-clr-series-plan.md](./docs/csharp-to-clr-series-plan.md) | 已拆出 | 串联 ECMA-335 / CoreCLR / Mono / IL2CPP / HybridCLR / LeanCLR |
| .NET Runtime 生态总图 | [docs/dotnet-runtime-ecosystem-master-plan.md](./docs/dotnet-runtime-ecosystem-master-plan.md) | 已拆出 | 运行时家族总导航 |
| HybridCLR 结构诊断与补桥 | [docs/hybridclr-structural-diagnosis-and-bridge-articles-plan.md](./docs/hybridclr-structural-diagnosis-and-bridge-articles-plan.md) | 已拆出 | 对应旧 `十九·B` |
| IL2CPP 运行时地图 | [docs/il2cpp-runtime-map-article-plan.md](./docs/il2cpp-runtime-map-article-plan.md) | 已拆出 | 独立专题已升级为系列 plan |
| Unity 异步 / Task / UniTask | [docs/unity-async-runtime-task-to-unitask-series-plan.md](./docs/unity-async-runtime-task-to-unitask-series-plan.md) | 已拆出 | 线程、任务、continuation 主线 |

### 引擎架构、模式与算法

| 主题 | Canonical 文件 | 状态 | 备注 |
|------|----------------|------|------|
| 游戏引擎架构地图 | [docs/game-engine-architecture-series-plan.md](./docs/game-engine-architecture-series-plan.md) | 已拆出 | 系列定义层 |
| 游戏引擎架构地图执行计划 | [docs/game-engine-architecture-series-execution-plan.md](./docs/game-engine-architecture-series-execution-plan.md) | 执行层已拆出 | 6 周执行顺序与证据需求 |
| 游戏引擎渲染栈 | [docs/game-engine-rendering-stack-series-plan.md](./docs/game-engine-rendering-stack-series-plan.md) | 已拆出 | 引擎到 GPU 的链路主线 |
| 游戏编程模式 | [docs/game-programming-patterns-plan.md](./docs/game-programming-patterns-plan.md) | 已拆出 | 对应旧 `系列七·B` |
| 游戏与引擎算法 | [docs/game-engine-algorithms-plan.md](./docs/game-engine-algorithms-plan.md) | 已拆出 | 对应旧 `系列七·D` |
| 游戏性能方法论 | [docs/game-performance-series-plan.md](./docs/game-performance-series-plan.md) | 已拆出 | 性能认知与方法论主线 |
| 引擎源码阅读到自研引擎 | [docs/engine-source-reading-to-self-built-engine-series-plan.md](./docs/engine-source-reading-to-self-built-engine-series-plan.md) | 已拆出 | 从源码阅读过渡到自研 |

### 资产、加载、裁剪与版本升级

| 主题 | Canonical 文件 | 状态 | 备注 |
|------|----------------|------|------|
| Unity 资产系统与序列化 | [docs/unity-asset-system-and-serialization-series-plan.md](./docs/unity-asset-system-and-serialization-series-plan.md) | 已拆出 | 对应旧 `系列八·D` |
| Unity 打包、加载与流式系统 | [docs/unity-packaging-loading-streaming-series-plan.md](./docs/unity-packaging-loading-streaming-series-plan.md) | 已拆出 | 对应旧 `系列八` |
| Addressables / YooAsset 源码阅读 | [docs/addressables-yooasset-source-reading-series-plan.md](./docs/addressables-yooasset-source-reading-series-plan.md) | 已拆出 | 偏资源系统源码视角 |
| Unity 代码与资源裁剪 | [docs/unity-stripping-series-plan.md](./docs/unity-stripping-series-plan.md) | 已拆出 | 对应旧 `系列八·E` |
| Unity DOTS 后续延伸 | [docs/unity-dots-follow-up-series-plan.md](./docs/unity-dots-follow-up-series-plan.md) | 已拆出 | 承接 `DOTS-E01~E18` |
| Unity 6 渲染升级 | [docs/unity6-rendering-upgrade-series-plan.md](./docs/unity6-rendering-upgrade-series-plan.md) | 已拆出 | Unity 6 专项 |
| Unity 6 运行时与工具链 | [docs/unity6-runtime-toolchain-series-plan.md](./docs/unity6-runtime-toolchain-series-plan.md) | 已拆出 | Unity 6 专项 |
| Unity 2022 → 6 升级指南 | [docs/unity6-upgrade-guide-series-plan.md](./docs/unity6-upgrade-guide-series-plan.md) | 已拆出 | 决策导向 |

### 服务端、ET 与业务系统

| 主题 | Canonical 文件 | 状态 | 备注 |
|------|----------------|------|------|
| ET 框架前置知识 | [docs/et-framework-prerequisites-series-plan.md](./docs/et-framework-prerequisites-series-plan.md) | 已拆出 | 服务于 ET 正文主线 |
| ET 框架正文主线 | [docs/et-framework-series-plan.md](./docs/et-framework-series-plan.md) | 已拆出 | ET 系列总规划 |
| 游戏服务端主题总图 | [docs/game-server-topic-plan.md](./docs/game-server-topic-plan.md) | 已拆出 | 服务端总入口 |
| 高性能服务端 ECS | [docs/game-server-ecs-high-performance-series-plan.md](./docs/game-server-ecs-high-performance-series-plan.md) | 已拆出 | 服务端 ECS 专项 |
| 技能系统 | [docs/skill-system-series-plan.md](./docs/skill-system-series-plan.md) | 已拆出 | 游戏核心系统专项 |
| 游戏预算管理 | [docs/game-budget-management-series-plan.md](./docs/game-budget-management-series-plan.md) | 已拆出 | 业务与工程边界主题 |

### 交付、运营、质量与验证

| 主题 | Canonical 文件 | 状态 | 备注 |
|------|----------------|------|------|
| 交付工程旗舰专栏 | [docs/delivery-engineering-column-plan.md](./docs/delivery-engineering-column-plan.md) | 已拆出 | 多端交付闭环 |
| 长线运营工程专栏 | [docs/live-ops-engineering-column-plan.md](./docs/live-ops-engineering-column-plan.md) | 已拆出 | 产品生命周期视角 |
| 质量护栏 / Quality Guardrails | [docs/quality-guardrails-series-plan.md](./docs/quality-guardrails-series-plan.md) | 已拆出 | 质量门、验证与回归 |
| Route B 验证路线 | [docs/route-b-verification-plan.md](./docs/route-b-verification-plan.md) | 已拆出 | 验证路径专项 |
| SEO / AI / 个人品牌执行 | [docs/site-seo-ai-personal-brand-execution-plan.md](./docs/site-seo-ai-personal-brand-execution-plan.md) | 已拆出 | 站点增长执行层 |
| 角色化选题规划 | [docs/role-based-topic-planning-2026-03.md](./docs/role-based-topic-planning-2026-03.md) | 已拆出 | 角色视角的专题规划 |

---

## 仍未拆出的历史总表主题

下列主题目前还没有独立的 canonical `*-series-plan.md`，历史篇级明细仍暂存在 [docs/doc-plan-archive-v26.md](./docs/doc-plan-archive-v26.md)：

| 历史主题 | 当前处理方式 |
|----------|-------------|
| 系列零：背景与历史（含零·A / 零·B / 零·C） | 暂存归档，后续按主题逐步拆分 |
| 零·D：游戏图形系统全貌 | 暂存归档 |
| 系列一：底层基础（数学 / C++ / 图形 API / 网络 / IO / CPU 内存） | 暂存归档 |
| 系列二：Unity 渲染 | 暂存归档 |
| 系列二·A：URP 深度 | 暂存归档 |
| 系列三：移动端硬件与 GPU/CPU 优化 | 暂存归档 |
| 系列四：Shader 技法与游戏效果 | 暂存归档 |
| 系列四·F：Unity Shader 变体工程 | 暂存归档 |
| 系列五：动画系统 | 暂存归档 |
| 系列六：AI 与游戏逻辑 | 暂存归档 |
| 系列七·A / 七·C：软件工程基础、通用数据结构与算法 | 暂存归档 |
| 系列九：UI / UX 系统 | 暂存归档 |
| 系列十：美术协作桥梁 | 暂存归档 |
| 系列十一：Unreal 架构 / GAS / 网络 / 编辑器扩展 | 暂存归档 |
| 系列十二：Unity 网络 | 暂存归档 |
| 系列十五 / 十五·B：本地化与 Accessibility | 暂存归档 |
| 系列十六：安全与反外挂 | 暂存归档 |
| 系列十七：CI / CD 与工程质量 | 暂存归档 |
| 系列十九：插件框架 | 暂存归档 |
| 崩溃分析系列 | 暂存归档 |
| 系列二十：Unity 源码解读（待定） | 暂存归档 |
| 系列二十二：DLSS 进化论 | 暂存归档 |
| 跨系统深挖计划 | 暂存归档 |

---

## 推荐的下一步拆分顺序

- 第一优先级：`系列二（Unity 渲染）`、`系列二·A（URP 深度）`、`系列四·F（Shader 变体工程）`
- 第二优先级：`系列十一（Unreal 主线）`、`系列十七（CI / CD）`、`系列十六（安全）`
- 第三优先级：`系列零（历史总论）`、`系列九（UI / UX）`、`系列十（美术协作桥梁）`

这样拆分的原则是：

- 先拆“已经形成稳定主线、且被频繁引用”的系列
- 再拆“跨站点主线的高耦合主题”
- 最后拆“阅读价值高但当前执行优先级较低”的系列

---

## 维护规则

1. 新增系列时，优先新建独立 `*-series-plan.md`，不要先把篇级目录塞回根 `doc-plan.md`。
2. 一个系列只保留一个 canonical 文件；执行层和 batch 文件只做下钻。
3. 文章写完后，优先回写系列 plan，不要只改根入口页。
4. 旧编号、历史篇级明细、未拆主题，请统一去 [docs/doc-plan-archive-v26.md](./docs/doc-plan-archive-v26.md) 查询。
5. `docs/doc-plan.md` 仅作为历史快照保留，不再更新。

---

## 旧编号查询入口

- 历史编号如 `DS-21`、`HCLR-12`、`CCLR-03`
- 旧版“系列几·几”下的篇级标题
- 早期总体统计和跨系统深挖列表

统一查：[docs/doc-plan-archive-v26.md](./docs/doc-plan-archive-v26.md)

