# 全站专题清单与评估

> 统计日期：2026-04-13
> 统计口径：按 frontmatter `series` 字段分组，含 series 为空但按文件名可归属的文章
> 评估维度：纵深、边缘覆盖、决策框架、闭环程度（详见文末评估标准）

---

## 数据总览

| 指标 | 数值 |
|------|------|
| 命名系列数 | 47 |
| 有索引页的系列 | 39 |
| 缺索引页的系列 | 5 |
| 待建系列（已规划未写） | 3 |
| 总文章数（含索引） | 700+ |
| series 字段为空的文章 | 30 |

---

## 一、渲染管线（10 个系列，191 篇）

### 1. Shader 手写技法

| 字段 | 值 |
|------|-----|
| 篇数 | 75 |
| 索引页 | `rendering/shader-handwriting-series-index.md` |
| 评级 | **B** |

全站最大系列。从基础语法、光照模型到 Decal 等高级技法逐篇展开。
量大但单篇偏短（平均 ~200 行），更接近教程集而非深度系统拆解。
适合做"Shader 入门到进阶"的学习路径，但不是展示工程判断力的最佳选择。

### 2. Unity 渲染系统

| 字段 | 值 |
|------|-----|
| 篇数 | 36 |
| 索引页 | `rendering/unity-rendering-series-index.md` |
| 评级 | **B** |

渲染方向的主干线，从 Mesh/Material/Texture 到 Draw Call、Batching、Frame Debugger、HDRP 定位。
广度好，是读者进入渲染板块的第一条路径。纵深中等 — 每个子话题讲到了"是什么、为什么"，
但不常深入到"引擎内部怎么实现、出了问题怎么查"。

### 3. URP 深度

| 字段 | 值 |
|------|-----|
| 篇数 | 22 |
| 索引页 | `rendering/urp-deep-dive-series-index.md` |
| 评级 | **B+** |

从 CommandBuffer、RTHandle 到 RenderGraph (Unity 6)，覆盖 Forward/Deferred/Forward+、
Pipeline Asset 全参数、Camera Stack、Lighting、Shadow、SSAO、Renderer Feature 开发、
Post-processing、移动端配置、多平台 Quality 分级。
广度全面。短板在于缺少 mobile GPU driver 层的边缘 case（比如某个 Mali 上 depth priming 花屏），
导致更像"URP 全参数手册"而不是"URP 踩坑实录"。

### 4. Shader Variant 治理

| 字段 | 值 |
|------|-----|
| 篇数 | 25（其中大部分 series 字段为空，按文件名归属） |
| 索引页 | `rendering/unity-shader-variants-series-index.md` |
| 评级 | **A** |

从 GPU Warp Divergence 执行模型讲起，不是从 Unity 按钮讲起。
全生命周期覆盖：源码 → multi_compile/shader_feature → 编译 → 保留链 → stripping 三阶段 →
runtime hit 机制 → 缺失诊断流程 → CI 回归监控。
有 SceneObject.OnEnable pipeline 污染、AB 交互爆炸点等边缘 case。
纵深最硬的渲染子系列。

**已知问题**：25 篇文章中大部分 `series` 字段为空或仅标为索引页，导致 Hugo 无法正确聚合。

### 5. 游戏图形系统

| 字段 | 值 |
|------|-----|
| 篇数 | 14 |
| 索引页 | `rendering/game-graphics-stack-series-index.md` |
| 评级 | **B-** |

图形系统的框架性概述，偏架构层。连接渲染与引擎的桥梁角色。
适合作为读者进入渲染板块前的全景扫描，但单独作为系列区分度不高。

### 6. CachedShadows 阴影缓存

| 字段 | 值 |
|------|-----|
| 篇数 | 9 |
| 索引页 | `rendering/cachedshadows-series-index.md` |
| 评级 | **A** |

全站闭环最完整的系列。9 篇从原理（URP 阴影链路替代段）→ 帧流程 → 激活链 →
Shader 交付边界 → 症状总表 → 验证方法 → 视觉调试 → Tradeoff 与分档策略。
篇数少但每篇 300-400 行，问题驱动，读完能独立排查 CachedShadows 相关问题。

### 7. DLSS 进化论

| 字段 | 值 |
|------|-----|
| 篇数 | 9 |
| 索引页 | `rendering/dlss-evolution-series-index.md` |
| 评级 | **B-** |

从 DLSS 1.0 到 3.5 的技术演化线，定位清晰，独立成系列。
偏技术科普，不涉及项目集成的工程判断。

### 8. 图形 API 基础

| 字段 | 值 |
|------|-----|
| 篇数 | 8 |
| 索引页 | `rendering/graphics-api-series-index.md` |
| 评级 | **C+** |

Vulkan/DX/Metal 的基础概念桥接。前置补课性质，不是独立深度系列。

### 9. 图形数学

| 字段 | 值 |
|------|-----|
| 篇数 | 6 |
| 索引页 | `rendering/graphics-math-series-index.md` |
| 评级 | **C+** |

向量、矩阵、空间变换。纯前置补课系列。

### 10. 零·B 深度补充

| 字段 | 值 |
|------|-----|
| 篇数 | 3 |
| 索引页 | `rendering/zero-b-deep-series-index.md` |
| 评级 | **C** |

篇数太少，补充性质。

---

## 二、系统设计（14 个系列，266 篇）

### 11. 游戏后端基础

| 字段 | 值 |
|------|-----|
| 篇数 | 59 |
| 索引页 | **无** |
| 评级 | **B** |

全站第二大系列，涵盖反作弊（威胁模型/客户端完整性/行为检测）、网络同步等后端主题。
**最大问题**：59 篇文章没有索引页，读者无法系统进入。

### 12. Unreal Engine 架构与系统

| 字段 | 值 |
|------|-----|
| 篇数 | 26 |
| 索引页 | `system-design/unreal-engine-series-index.md` |
| 评级 | **B-** |

UObject、蓝图、网络复制等 UE 核心系统拆解。量够，但更偏系统介绍而非深度拆解。

### 13. 数据结构与算法

| 字段 | 值 |
|------|-----|
| 篇数 | 24 |
| 索引页 | `system-design/data-structures-and-algorithms-series-index.md` |
| 评级 | **B-** |

经典 DS&A 内容，更偏基础教学。在技术博客中区分度低。

### 14. Unity DOTS 工程实践

| 字段 | 值 |
|------|-----|
| 篇数 | 19 |
| 索引页 | `system-design/dots-engineering-index.md` |
| 评级 | **B-** |

ECS 写法、Job System、Burst 的工程侧内容。实用但 DOTS API 变化快容易过时。

### 15. 技能系统深度

| 字段 | 值 |
|------|-----|
| 篇数 | 16 |
| 索引页 | `system-design/skill-system-series-index.md` |
| 评级 | **A-** |

架构思维最强的系列。SkillDef vs SkillInstance 分层、7 种技能类型（Instant/Cast/Channel/
Charge/Toggle/Passive/Combo）的统一生命周期模型、Effect 作为独立执行层、
Buff 管理（叠加/刷新/覆盖/快照/实时重算）、多人同步（Server Authority/预测/回滚/命中确认）。
自建 vs GAS 的思维映射不是 API 对比而是设计决策对比。
短板：combo 系统和网络回滚算法只有框架没有展开，缺平衡框架。

### 16. 高性能游戏服务端 ECS

| 字段 | 值 |
|------|-----|
| 篇数 | 14 |
| 索引页 | `system-design/server-ecs-series-index.md` |
| 评级 | **B** |

服务端 ECS 的设计与实现。与 ET 框架系列有交叉。

### 17. 软件工程基础与 SOLID

| 字段 | 值 |
|------|-----|
| 篇数 | 14 |
| 索引页 | `system-design/software-engineering-solid-series-index.md` |
| 评级 | **C+** |

SOLID 原则 + 工程实践。理论教学性质，在技术博客中区分度不高。

### 18. 游戏引擎架构地图

| 字段 | 值 |
|------|-----|
| 篇数 | 10 |
| 索引页 | `system-design/game-engine-architecture-series-index.md` |
| 评级 | **B-** |

引擎架构全景图。偏知识地图而非实现细节。

### 19. 游戏编程设计模式

| 字段 | 值 |
|------|-----|
| 篇数 | 8 |
| 索引页 | `system-design/game-programming-patterns-series-index.md` |
| 评级 | **C+** |

Game Programming Patterns 的解读系列。原书本身已是经典，解读的区分度取决于是否有项目实例。

### 20. 数据导向运行时

| 字段 | 值 |
|------|-----|
| 篇数 | 8 |
| 索引页 | `engine-toolchain/data-oriented-runtime-series-index.md` |
| 评级 | **B-** |

DOD 运行时概念和实践。与 DOTS 系列有交叉。

### 21. Unreal Mass 深度

| 字段 | 值 |
|------|-----|
| 篇数 | 8 |
| 索引页 | `system-design/unreal-mass-series-index.md` |
| 评级 | **B-** |

UE5 Mass Entity 系统拆解。篇数中等，领域较窄。

### 22. Unity DOTS Physics

| 字段 | 值 |
|------|-----|
| 篇数 | 8 |
| 索引页 | `system-design/dots-physics-index.md` |
| 评级 | **C+** |

DOTS 物理系统。篇数少，更像入门/前置。

### 23. Unity DOTS NetCode

| 字段 | 值 |
|------|-----|
| 篇数 | 8 |
| 索引页 | `system-design/dots-netcode-index.md` |
| 评级 | **C+** |

DOTS 网络同步。篇数少，更像入门/前置。

### 24. Unity DOTS 项目落地与迁移

| 字段 | 值 |
|------|-----|
| 篇数 | 7 |
| 索引页 | `system-design/dots-project-migration-index.md` |
| 评级 | **C+** |

DOTS 迁移的工程决策。偏决策框架，实现细节少。

### 25. 数据导向行业横向对比

| 字段 | 值 |
|------|-----|
| 篇数 | 4 |
| 索引页 | **无** |
| 评级 | **B-** |

Overwatch ECS、idTech7 (DOOM)、Flecs/EnTT 横向对比。内容有价值但缺索引入口。

### 26. 数据导向实战案例

| 字段 | 值 |
|------|-----|
| 篇数 | 3 |
| 索引页 | **无** |
| 评级 | **C+** |

RTS 单位、弹幕系统、混合架构三个案例。篇数太少。

---

## 三、引擎工具链（10 个系列，149 篇）

### 27. Unity 资产系统与序列化

| 字段 | 值 |
|------|-----|
| 篇数 | 42 |
| 索引页 | `engine-toolchain/unity-asset-system-and-serialization-series-index.md` |
| 评级 | **B+** |

AB 内部结构（Header/Block/Directory/SerializedFile 四层）、运行时加载链路（download →
cache → dependencies → unload）、压缩策略（LZMA vs LZ4）、版本/Hash/CDN/回滚治理、
Shader 集成问题、Addressables 选型、Build-time 分配（Player vs AB 边界）。
量大且深。短板：缺生产事故 case study（"线上出了什么、怎么查到的"），缺迁移方案。

### 28. HybridCLR

| 字段 | 值 |
|------|-----|
| 篇数 | 24（21 篇标记 + 3 篇 series 为空但按内容归属） |
| 索引页 | `engine-toolchain/hybridclr-series-index.md` |
| 评级 | **S** |

全站综合最强。评级理由：

- **纵深**：到 `RuntimeApi → Interpreter::Execute` 调用链层级，不是 API 使用层
- **边缘覆盖**：5 类 AOT 泛型陷阱模式（UniTask/LINQ/Dictionary/Delegate/自定义容器），
  TypeLoadException + async crash 的真实 native crash 还原
- **决策框架**：metadata vs FGS vs DHE 选型 tradeoff，DisStripCode 4 种写法模板，
  Community vs Pro 能力矩阵
- **闭环**：crash → 分层定位 → 修复 → CI pipeline 防回归

**已知问题**：3 篇 case study 的 `series` 字段为空。

### 29. Unity 异步运行时

| 字段 | 值 |
|------|-----|
| 篇数 | 16 |
| 索引页 | `engine-toolchain/unity-async-runtime-series-index.md` |
| 评级 | **B** |

async/await、UniTask、协程的运行时拆解。深度可以，但边缘覆盖和闭环偏弱。

### 30. Unity 脚本编译管线

| 字段 | 值 |
|------|-----|
| 篇数 | 10 |
| 索引页 | `engine-toolchain/unity-script-compilation-pipeline-series-index.md` |
| 评级 | **B-** |

asmdef 分割、编译顺序、IL2CPP 前置知识。HybridCLR 和 Stripping 的前置系列。

### 31. 数据导向运行时（工具链侧）

| 字段 | 值 |
|------|-----|
| 篇数 | 8 |
| 索引页 | `engine-toolchain/data-oriented-runtime-series-index.md` |
| 评级 | **B-** |

与系统设计板块的 DOD 系列部分重叠。

### 32. Unity 裁剪

| 字段 | 值 |
|------|-----|
| 篇数 | 7 |
| 索引页 | `engine-toolchain/unity-stripping-series-index.md` |
| 评级 | **B** |

Managed/Native stripping、link.xml 配置。是 HybridCLR 的重要前置。

### 33. 存储设备与 IO 基础

| 字段 | 值 |
|------|-----|
| 篇数 | 7 |
| 索引页 | `engine-toolchain/storage-io-series-index.md` |
| 评级 | **C+** |

文件系统、IO 调度、序列化格式。前置补课性质。

### 34. CrashAnalysis

| 字段 | 值 |
|------|-----|
| 篇数 | 6 |
| 索引页 | `engine-toolchain/crash-analysis-series-index.md` |
| 评级 | **B** |

崩溃分析方法论和工具链。内容实用，篇数偏少。

### 35. 构建与调试前置

| 字段 | 值 |
|------|-----|
| 篇数 | 6 |
| 索引页 | `engine-toolchain/build-debug-prereqs-series-index.md` |
| 评级 | **C+** |

构建流程、调试工具的基础知识。前置性质。

### 36. Unity Android 发布与包体

| 字段 | 值 |
|------|-----|
| 篇数 | 6 |
| 索引页 | `engine-toolchain/unity-android-aab-pad-series-index.md` |
| 评级 | **B-** |

AAB、PAD、包体优化。实用但领域较窄。

---

## 四、性能工程（6 个系列，107 篇）

### 37. 移动端硬件与优化

| 字段 | 值 |
|------|-----|
| 篇数 | 37 |
| 索引页 | `performance/mobile-hardware-and-optimization-series-index.md` |
| 评级 | **B+** |

Android OEM 定制（CPU 调度/内存管理/功耗策略）、GPU 架构（Adreno/Mali/PowerVR）、
散热策略。覆盖面广，实战性强，边缘 case 中等。

### 38. 游戏性能判断

| 字段 | 值 |
|------|-----|
| 篇数 | 34 |
| 索引页 | `performance/game-performance-judgment-series-index.md` |
| 评级 | **B+** |

性能判断框架，分职能入口（TA/美术/策划各自的性能职责），美术资源预算优先级。
导航设计好。短板：偏框架和方法论，缺具体的量化数据和 profiling 实例。

### 39. 游戏预算管理

| 字段 | 值 |
|------|-----|
| 篇数 | 22 |
| 索引页 | `performance/game-budget-management-series-index.md` |
| 评级 | **B** |

Draw Call / 内存 / 包体预算的管理方法。十条规则、分档基线、CI 集成。
实用性好，偏方法论缺实测数据。

### 40. 底层硬件·CPU 与内存体系

| 字段 | 值 |
|------|-----|
| 篇数 | 7 |
| 索引页 | `performance/hardware-cpu-index.md` |
| 评级 | **B-** |

CPU 缓存层次、内存带宽、分支预测。性能优化的底层前置知识。

### 41. 渲染系统分档设计

| 字段 | 值 |
|------|-----|
| 篇数 | 6 |
| 索引页 | **无**（与机型分档索引页有交叉） |
| 评级 | **B** |

分档的渲染配置矩阵（01-contracts → 02-health-model → 03-pipeline-structure →
04-feature-matrix → 05-material-shader-governance → 06-validation-loop）。
6 篇有完整的闭环结构，但 series 归属混乱。

### 42. 机型分档

| 字段 | 值 |
|------|-----|
| 篇数 | 1（名存实散，实际内容散落在 37/38/41 中） |
| 索引页 | `performance/device-tiering-series-index.md` |
| 评级 | **结构性问题** |

索引页存在，但文章分别打标到了"游戏性能判断"和"渲染系统分档设计"，
导致该系列 Hugo 聚合后只有 1 篇。需要统一 series 字段。

---

## 五、ET 框架（2 个系列，16 篇）

### 43. ET 前置桥接

| 字段 | 值 |
|------|-----|
| 篇数 | 11 |
| 索引页 | `et-framework-prerequisites/_index.md` |
| 评级 | **B** |

线程/任务/协程/纤程概念辨析。桥接做得好，帮读者在进入 ET 框架前建立统一词汇表。

### 44. ET 框架源码解析

| 字段 | 值 |
|------|-----|
| 篇数 | 5 |
| 索引页 | `et-framework/_index.md` |
| 评级 | **C+** |

未完成系列。ET9 包体架构变化分析有深度，但篇数不够形成完整系列。

---

## 六、独立板块（无系列索引）

### Code Quality

| 字段 | 值 |
|------|-----|
| 篇数 | 28 |
| 索引页 | **无** |
| 评级 | **B** |

AI Code Review、CI 门禁、测试策略、渐进发布、调试工具选型。
内容实用，28 篇量不小，但没有索引页导致读者无法系统进入。

---

## 评估标准说明

### 四个评估维度

| 维度 | 含义 | 判断方法 |
|------|------|---------|
| **纵深** | 一个知识点能往下钻几层 | 文章的起点在哪 — 从 Unity 按钮开始还是从运行时/GPU 层开始 |
| **边缘覆盖** | 有没有处理正常路径之外的情况 | 是否有真实 crash case、pipeline 污染、陷阱模式等边缘内容 |
| **决策框架** | 读者读完能不能自己做判断 | 是否有 A vs B 的 tradeoff 分析，而不只是"推荐这样做" |
| **闭环程度** | 从发现问题到验证修复的链路是否完整 | 是否覆盖了"怎么发现 → 怎么定位 → 怎么修 → 怎么验证没改坏" |

### 评级定义

| 评级 | 含义 |
|------|------|
| **S** | 四个维度均强，可作为业内参考级内容 |
| **A / A-** | 三个维度强，一个维度有明确短板 |
| **B+ / B / B-** | 一到两个维度强，其余中等或有缺口 |
| **C+ / C** | 前置补课或未完成系列，有存在价值但不适合作为代表作展示 |

---

## 结构性问题汇总

### 1. 缺索引页的系列（5 个）

| 系列 | 篇数 | 影响 |
|------|------|------|
| 游戏后端基础 | 59 | 全站第二大系列无入口 |
| Code Quality | 28 | 实用内容无系统导航 |
| 渲染系统分档设计 | 6 | 完整闭环但无入口 |
| 数据导向行业横向对比 | 4 | 有价值的横比缺入口 |
| 数据导向实战案例 | 3 | 案例内容缺入口 |

### 2. series 字段为空的文章（30 篇）

主要分布：
- HybridCLR 的 3 篇 case study（hybridclr-case-*.md、hybridclr-ci-*.md、hybridclr-fix-*.md）
- Shader Variant 的多篇文章（unity-shader-variant-build-*.md、unity-shader-keyword-*.md 等）
- 资产系统的部分文章

这些文章实际属于对应系列，但因 `series` 字段为空，Hugo 无法正确聚合。

### 3. 机型分档系列名存实散

索引页 `device-tiering-series-index.md` 存在，但文章分别打标到了：
- `series: "游戏性能判断"` — 通用性能类文章
- `series: "渲染系统分档设计"` — 渲染配置类文章
- `primary_series: "device-tiering"` — 使用了非标准字段

需要统一 series 字段或建立 primary_series 的聚合逻辑。

---

## 综合排名

| 排名 | 系列 | 篇数 | 评级 | 一句话理由 |
|------|------|------|------|-----------|
| 1 | HybridCLR | 24 | S | 调用链级纵深 + 5 类陷阱 + 真实 crash + CI 闭环 |
| 2 | Shader Variant 治理 | 25 | A | GPU 模型起步 + pipeline 污染 + 诊断→修复链 |
| 3 | CachedShadows | 9 | A | 篇数少但闭环最完整 |
| 4 | 技能系统深度 | 16 | A- | 架构分层最清晰，combo/回滚未展开 |
| 5 | Unity 资产系统 | 42 | B+ | 量大且深，缺事故 case |
| 6 | URP 深度 | 22 | B+ | 广度全面，单点纵深不突出 |
| 7 | 游戏性能判断 | 34 | B+ | 导航好，缺量化数据 |
| 8 | 移动端硬件与优化 | 37 | B+ | 覆盖面广，实战性好 |
| 9 | Shader 手写技法 | 75 | B | 量最大，单篇偏短 |
| 10 | Unity 渲染系统 | 36 | B | 主干线，纵深中等 |
| 11 | 游戏后端基础 | 59 | B | 量大但无索引 |
| 12 | Code Quality | 28 | B | 实用但无索引 |
| 13 | 游戏预算管理 | 22 | B | 实用偏方法论 |
| 14 | Unity 异步运行时 | 16 | B | 深度可以，闭环弱 |
| 15+ | 其余系列 | — | B- ~ C | 前置/教学/未完成 |

---

## 新增系列（已规划，待写）

以下三个系列于 2026-04-13 完成规划，尚无已发布文章。

### Unity 6 渲染管线升级实战

| 字段 | 值 |
|------|-----|
| 计划篇数 | 8 |
| series_id | `unity6-rendering-upgrade` |
| 系列 plan | `docs/unity6-rendering-upgrade-series-plan.md` |
| 评级 | 待评（规划完成，文章未写） |

从 Unity 2022 到 Unity 6 渲染管线的结构性变化。GPU Resident Drawer 调度模型、BIRP deprecated 迁移策略、HDRP 停更后选型决策、URP 14→17 API 破坏性变更。
前置依赖："Unity 渲染系统"系列 + "URP 深度"系列。

### Unity 6 运行时与工具链变化

| 字段 | 值 |
|------|-----|
| 计划篇数 | 6 |
| series_id | `unity6-runtime-toolchain` |
| 系列 plan | `docs/unity6-runtime-toolchain-series-plan.md` |
| 评级 | 待评（规划完成，文章未写） |

Awaitable vs UniTask vs Coroutine 架构对比、Content Pipeline 后台化、Android GameActivity、6.x 滚动更新策略、Sentis 推理引擎。
前置依赖："Unity 异步运行时"系列。

### Unity 2022 → Unity 6 升级决策指南

| 字段 | 值 |
|------|-----|
| 计划篇数 | 4 |
| series_id | `unity6-upgrade-guide` |
| 系列 plan | `docs/unity6-upgrade-guide-series-plan.md` |
| 评级 | 待评（规划完成，文章未写） |

面向技术决策者的升级成本收益分析框架、按模块兼容性排查 Checklist、版本锁定策略、第三方依赖审计。
前置依赖：上面两个 Unity 6 系列提供事实基础。
