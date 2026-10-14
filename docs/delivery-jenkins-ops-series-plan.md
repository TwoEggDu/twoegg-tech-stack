# 游戏团队 Jenkins 实战 · 系列规划

> 版本：v1.2 | 核对日期：2026-10-14
> 状态：索引 + 16 篇正文实际存在；203 / 305 已完成全文文档审查与必要修订，运行验证及全系列技术验证仍未完成
> 所属：交付工程旗舰专栏 V16（CI/CD 管线）的深度下钻子系列

---

## 一、系列定位

### 一句话定位

> 当团队已经选定 Jenkins 作为 CI 工具，并且承担着游戏项目的构建产物时，如何把 Jenkins 做好。

### 核心问题

V16.6 CI 工具选型已经给出"什么场景选 Jenkins"的决策依据，但选定之后的"怎么做好"——流水线怎么设计、Master 怎么不崩、Unity 的特殊性怎么吃下——这些问题 V16 没展开。本系列填补这个空白。

### 差异化

网上通用 Jenkins 教程很多，但聚焦"游戏团队 Jenkins 实战"的系列几乎没有。游戏团队的构建产物体积、License 稀缺性、平台矩阵、构建时长都让通用 CI 经验大面积失效。系列总论《为什么游戏团队的 Jenkins 是另一个物种》会系统论证这一点。

### 本系列不是什么

- 不是 Jenkins 入门教程（读者应已用过）
- 不是 Jenkins vs GitHub Actions 的再次选型对比（V16.6 已覆盖）
- 不是 CI/CD 方法论通论（V16 已覆盖）
- 不是 Unity 构建本身的细节（交付 V07 多端构建已覆盖）

---

## 二、目标读者分层

| 读者层 | 核心诉求 |
|--------|---------|
| TA / 构建工程师（游戏团队） | 自己团队的 Jenkins 怎么撑住 Unity 构建 |
| DevOps / SRE | Jenkins 在游戏项目场景下的运维特殊性 |
| 技术负责人 | 架构决策、维护边界与证据判断 |

---

## 三、三条阅读线

本系列内部提供三条阅读路径，同一批 17 篇文章不同入口：

| 线 | 目标读者 | 路径特点 | 推荐顺序 |
|---|---------|---------|---------|
| **L1 · 游戏构建侧** | TA / 构建工程师 | 从 Unity 集成痛点切入 | 索引 → 总论 → Part 3 → Part 1 → Part 2 |
| **L2 · DevOps / SRE 侧** | 平台 / 运维工程师 | 从稳定性底座切入 | 索引 → 总论 → Part 2 → Part 1 → Part 3 |
| **L3 · 技术 Leader 侧** | 技术负责人 / 技术决策者 | 决策依据与待核案例边界 | 索引 → 总论 → 各组 `leader_pick: true` 的精选篇 |

L3 线的精选机制通过文章 frontmatter 的 `leader_pick: true` 字段实现。

---

## 四、文章地图（17 篇）

### Part 0 · 导航（2 篇）

| # | weight | 标题 | 核心问题 |
|---|--------|------|---------|
| 000 | 1570 | 游戏团队 Jenkins 实战 · 系列索引 | 我是不是目标读者、从哪条阅读线进 |
| 001 | 1571 | 为什么游戏团队的 Jenkins 是另一个物种 | 和 Web / 服务端团队比，游戏团队 Jenkins 的结构性差异在哪 |

### Part 1 · 流水线架构（5 篇）

| # | weight | 标题 | 核心问题 |
|---|--------|------|---------|
| 101 | 1572 | Declarative vs Scripted Pipeline：游戏团队的选型取舍 | 两种语法各自撑住什么规模、什么边界必须切换 |
| 102 | 1573 | Shared Library 设计：抽象出可复用的游戏构建原语 | 哪些步骤值得下沉到共享库、边界怎么切 |
| 103 | 1574 | 多产品矩阵：一套模板支撑 N 个产品线 | 参数化的三个阶段演进与常见过度抽象 |
| 104 | 1575 | 多分支流水线：Dev / QA / Release 自动化策略 | 分支策略怎么映射到 Multibranch / Folder |
| 105 | 1576 | Jenkins 下的并行模式：Parallel / Matrix / Triggers 实战 | 扇入扇出在 Jenkins 的三种实现与失败隔离 |

### Part 2 · 稳定性运维（5 篇）

| # | weight | 标题 | 核心问题 |
|---|--------|------|---------|
| 201 | 1577 | Master 的三类瓶颈：连接、内存、磁盘 I/O | Master 为什么会在"大作业 + 大产物"场景下崩 |
| 202 | 1578 | Agent 调度与标签体系 | 标签怎样匹配真实能力，节点怎样按清单重建并验收 |
| 203 | 1579 | Workspace 与产物的磁盘治理 | 工作目录、构建记录与归档产物怎样分别保留和清理 |
| 204 | 1580 | Jenkins 升级踩坑：JVM / 插件 / 迁移 | 升级路径的四个失败模式与回滚预案 |
| 205 | 1581 | Jenkins 自身的可观测性：监控与告警 | Jenkins 自己挂了谁来发现、怎么告警 |

### Part 3 · Unity 特化集成（5 篇）

| # | weight | 标题 | 核心问题 |
|---|--------|------|---------|
| 301 | 1582 | Unity License 池管理：稀缺资源的治理 | 激活、浮动租约与 Jenkins 并发锁如何分责并核对异常 |
| 302 | 1583 | 大仓库在 Jenkins 下的 Workspace 策略 | Git LFS / 浅克隆 / 缓存仓在真实大仓下的取舍 |
| 303 | 1584 | 符号表与崩溃栈：IL2CPP 产物的符号链路 | 线上崩溃栈还原的完整链路怎么自动化 |
| 304 | 1585 | 多平台并行打包与隔离 | iOS / Android / WebGL 同时出包的资源冲突与隔离 |
| 305 | 1586 | IL2CPP 构建的时间与内存特征 | 怎样按阶段和实际资源记录诊断构建瓶颈 |

---

## 五、与相邻系列的边界

| 相邻系列 | 重叠风险点 | 本系列处理原则 |
|---------|-----------|--------------|
| **V16 CI/CD 管线（6 篇）** | CI/CD 通论、管线架构通论、工具选型 | V16 已覆盖通论 + 选型，本系列从"选定 Jenkins 之后"接力，不重讲五阶段模型、不重讲工具对比 |
| **V07 多端构建（7 篇）** | Unity 构建流程、多平台构建 | V07 讲 Unity 构建本身（BuildPipeline / SBP / 平台差异），本系列讲"Unity 构建在 Jenkins 上怎么跑稳"（License / Workspace / 并行隔离） |
| **V08 脚本热更新（6 篇）** | HybridCLR CI 集成 | V08 讲热更工程化与 HybridCLR，本系列的 IL2CPP 篇（303/305）只关注"IL2CPP 构建机的时间和内存特征"，不涉及热更产物 |
| **V03 工程基建** | 编译管线、依赖管理 | V03 讲项目结构与编译域，本系列聚焦 CI 侧（触发/调度/产物），两者视角不同 |
| **已有单篇 `code-quality/game-project-build-pipeline-jenkins-github-actions.md`** | Jenkins 相关 | 那篇是"选型参考"视角，本系列是"运维实践"视角；在扩展阅读里引用 |

---

## 六、Weight 与编号约定

- **区间**：1570-1586（V16 1500-1560 之后、V17 1600-1650 之前）
- **间隔**：1 单位（17 篇紧密排布，预留 1587-1599 给将来扩展）
- **编号规则**：
  - 1570 · 索引
  - 1571 · 总论
  - 1572-1576 · Part 1 流水线架构
  - 1577-1581 · Part 2 稳定性运维
  - 1582-1586 · Part 3 Unity 特化集成

- **文件命名**：`content/delivery-engineering/delivery-jenkins-ops-{nn}-{name}.md`
- **series_id**：`delivery-jenkins-ops`
- **series 名**：`游戏团队 Jenkins 实战`

---

## 七、当前修订与证据优先级

本轮实际核对文件后，原“索引与总论先行”的创作状态已过时。继续维护现有 17 篇，不新增一套 Jenkins 教程。逐篇待核记录与本轮检查结果见[四项补强记录](build-delivery-strengthening-report.md)。

### 既有 301 / 204 修订：许可与升级边界

- **候选标题 / 落点**：沿用[301 许可池](../content/delivery-engineering/delivery-jenkins-ops-301-license-pool.md)与[204 升级](../content/delivery-engineering/delivery-jenkins-ops-204-upgrade-pitfalls.md)原题，各自维护授权与升级问题。
- **主问题**：哪些状态由许可系统负责，哪些由 Jenkins 负责？哪些版本与备份必须一起恢复？
- **责任边界**：只引用公开官方资料，不代替授权权益核对，不把教程片段视为真实运维记录。
- **前置**：Agent、任务状态与版本清单；不以全部 Jenkins 系列为硬前置。
- **关键小节**：激活、租约与调度锁怎样区分？断线后如何核对清理结果？Java、core、插件与配置怎样形成可回退组合？怎样阻止演练实例写入生产？
- **可复用文章**：[001 总论](../content/delivery-engineering/delivery-jenkins-ops-001-why-different.md)、[102 共享库](../content/delivery-engineering/delivery-jenkins-ops-102-shared-library.md)、[202 节点](../content/delivery-engineering/delivery-jenkins-ops-202-agent-scheduling.md)。本轮同步修正许可摘要与封装建议。
- **所需证据**：官方版本资料；后续需获准环境的授权模式、版本组合、取消/断线/回退结果，公开只保留脱敏摘要。
- **当前状态**：重点勘误已写入正文，官方资料核对于 2026-10-14；旧事故来源未确认，未执行实验。

### 既有 202 增量：构建节点重建验收

- **候选标题 / 落点**：沿用《Agent 调度与标签体系》，原位增加[构建节点重建验收](../content/delivery-engineering/delivery-jenkins-ops-202-agent-scheduling.md#agent-rebuild-acceptance)。
- **主问题**：旧机器不可用后，新节点能否按清单恢复到可交付状态？
- **责任边界**：节点环境与代表性链路验收；控制器恢复归 204，游戏功能与设备验收归质量/验证系列。
- **前置**：构建输入身份、Agent 与 executor、缓存和不可重建输入的区别。
- **关键小节**：环境清单如何涵盖 OS、Editor、SDK 与受控凭据引用？干净环境首构建和缓存复用分别证明什么？失败、取消与重启后怎样核对状态？谁确认产物可交接？
- **可复用文章**：[V16 构建自动化](../content/delivery-engineering/delivery-cicd-pipeline-03-build-automation.md)、[多平台隔离](../content/delivery-engineering/delivery-jenkins-ops-304-multi-platform-isolation.md)、[204 升级](../content/delivery-engineering/delivery-jenkins-ops-204-upgrade-pitfalls.md)。
- **所需证据**：可公开的环境清单摘要、固定输入、冷/热缓存构建记录、清理结果、产物身份与接收回执、实测恢复耗时。
- **当前状态**：已补正文中的验收设计，未实施节点重建或验收；原标签演进故事仍待核。

### 既有 203 / 305 修订：清理边界与构建诊断

- **候选标题 / 落点**：沿用[203《Workspace 与产物的磁盘治理》](../content/delivery-engineering/delivery-jenkins-ops-203-disk-governance.md)与[305《IL2CPP 构建的时间与内存特征》](../content/delivery-engineering/delivery-jenkins-ops-305-il2cpp-build.md)；不新增篇号或系列。
- **主问题**：如何确认数据的保留与占用关系？如何用阶段记录区分 IL2CPP 构建现象与原因？
- **责任边界**：203 维护工作目录、记录和产物的保留边界；305 维护构建资源观测与诊断。控制器灾备、运行时性能及真实项目成果不由本次文档修订验证。
- **前置**：Jenkins Job、构建记录与 workspace；305 另需了解托管编译和本机工具链。202 / 204 / 205 为按问题延伸，不全部列为硬前置。
- **关键小节**：清理怎样避免影响在用目录和固定保留产物？Orphaned Item 与 build retention 怎样区分？并发、缓存与链接器设置怎样改变观测？哪些信号只能提出假设，哪些记录可以支持归因？
- **可复用文章**：202 节点重建、204 升级恢复、205 可观测性，以及 203 / 305 相互衔接；邻篇的待核内容不由本次审查背书。
- **所需证据**：固定 Jenkins / 插件版本的隔离保留验证；固定 Unity、平台工具链与输入的干净/增量构建、同机并发观测和产物验收。公开只保留脱敏结果。
- **当前状态**：2026-10-14 完成两篇全文文档审查，撤下无依据数字、事故、直接删除脚本和未核实接口示例，补官方出处与诊断边界。尚未执行 Jenkins 清理、Unity 构建或压力测试；检查范围见[后续审查记录](build-delivery-strengthening-report.md#jenkins-203-305-review)。

后续继续核实 302 / 304 的性能材料及其他篇章的待核项。203 / 305 的运行验证单独登记，不能因文档修订而标为实测完成。

---

## 八、写作模板要点

参照交付专栏总规划第九节"统一写作模板"：

- 原理层（长青）在前 → 实践层（Jenkins + Unity）在后
- 必须有"本环节的常见故障"一节
- 必须给出"如何验证"的可执行步骤
- 平台差异（macOS Agent vs Linux Agent vs Windows Agent）单独一节处理
