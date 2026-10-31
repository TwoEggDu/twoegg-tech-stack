# 代表专栏：基础与案例衔接执行对照表

> 2026-10-31 · 执行层快照。方向以[站点中心思想](site-entry-and-audience-plan.md#column-core-principles)为准，主题问题见[生产管线叙事约定](game-knowledge/production-pipeline-writing-principles.md#production-questions)。本表核对相关正文、提纲与原规划，不建立第二份篇级目录，也不代表本轮新增实验、完成正文或发布。

## 1. 范围与判断口径

本轮围绕“组织生产与跨工种配合、客户端与服务端协作、性能判断、发现卡点与能力沉淀”核对基础和候选案例。范围是下表引用的现有材料，不是全站知识覆盖审计。`G01` 等只是本表的问题定位标识，不是新文章编号。

本表依据盘点时的本地工作区，包含尚未提交的正文修订与支持材料；此次只交付方向、读取规则、技能及对照表，不代表被引用文章的工作区修订也已交付。M01、M02 与客户端基础提纲尚未随本次提交入库，以下以本地路径标记，待独立交付后再转为仓库链接；其他正文链接可能仍显示其已提交版本，内容与证据状态须回原文件核对。

- **基础覆盖**：本地有可读正文、只有提纲或仍需补桥；没有 `content_stage` 的旧文结合正文判断，不仅按字段统计。
- **解释缺口**：已有知识之间还需要怎样的引导或应用衔接，属于编辑判断，不直接判定必须新增独立文章。
- **案例证据**：作者回忆、教学情景、独立实验与实际工程验证分别说明。缺案例不等于缺理论，未找到同次记录不等于功能不存在。
- **任务归属**：沿[根路由](../doc-plan.md)进入各原 plan；本表提供关系与下一步建议，不改变它们的目录、状态或学习顺序。开做前重读原 plan 和正文，处理快照之后的变化。

## 2. 基础与案例对照

| 问题 | 必要基础与现有正文 | 本轮覆盖判断及基础工作 | 可承接的案例／证据缺口 | 原归属与下一步产物 |
| --- | --- | --- | --- | --- |
| G01 各工种怎样把内容接成可验收版本？ | [生产体系总览](../content/engineering-journey/production-pipeline-overview.md)、[美术生产全貌](../content/delivery-engineering/game-art-production-01-overview.md)、[客户端接入](../content/delivery-engineering/game-art-production-10-client-integration.md)、[联合验收](../content/delivery-engineering/game-art-production-12-joint-acceptance.md)；输入、导出物、引擎资产、运行实例与接受责任 | 基础已有，优先复用现有交接表与教学路线；需要把资源、配置、代码三路的阅读去向放在读者能理解的位置，不另建同义入门篇 | 庭院为教学方案，软件实操、素材、运行与联合验收未完成；总览中的已有工作也不能代替这套教学工程的验证 | [美术协作 plan](game-art-production-collaboration-series-plan.md)、[工程实践 plan](engineering-journey-column-plan.md)；产物：交接阅读说明与按现有方案列出的验证缺项 |
| G02 如何发现卡点、判断原因与投入先后？ | [FDE 基础](../content/delivery-engineering/fde-01-foundations.md)、[交付方法](../content/delivery-engineering/fde-02-delivery-method.md)；观察任务、区分原话与假设、热点合并拆分、优先级与复验 | 两篇已具备正文，不判为缺少发现问题的方法；补桥候选是把这些方法对应到资源、配置和性能各自的技术证据。先复用教学记录，再决定是否需原位增补 | 方法篇的反馈与排期是教学推演；[Missing 历史案例](../content/engineering-journey/production-resource-completeness.md)可以承接工具演进，不能用它替所有方法证明普遍效果 | [FDE plan](fde-methods-series-plan.md)、工程实践 plan；产物：受阻步骤→假设→所需证据→方案选择的主题映射，不重写一套 FDE 总论 |
| G03 本地有文件，为什么共享工程或包内仍缺内容？ | [GUID／fileID／PPtr](../content/engine-toolchain/unity-guid-fileid-pptr-what-do-they-reference.md)、Missing 案例的就近机制说明；文件、资产、对象标识、依赖、提交范围与进包范围 | 基础已有；可复用身份模型和共享副本对照。推荐前按原篇核对公开来源与版本，进一步分清序列化依赖、字符串路径和收集规则，不直接归因于某一个机制 | Missing 有作者确认的定位、仲裁机、范围调整、提交触发和依赖工具过程；结果为历史定性观察，缺同口径次数／耗时统计，不预填改善比例 | [资产系统 plan](unity-asset-system-and-serialization-series-plan.md)、工程实践 plan；产物：依赖与收集边界的必要补桥、引用审校项 |
| G04 配置怎样从源表变成两端实际使用的数据？ | [策划数据管道](../content/system-design/skill-system-27-designer-data-pipeline.md)、[配置一致性](../content/engineering-journey/production-config-consistency.md)；字段语义、共享 ID、资源引用、生成代码／数据、消费程序 | 管道与契约已有解释。优先补“同一源输入→生成集合→消费程序集→实际使用”的身份衔接；旧文项目示例的身份与依据推荐前核对，不认领为作者亲历 | 配置一致性正文使用不同阶段材料；M01（本地 `docs/b01-m01-config-release-material-status-2026-10-30.md`）仍缺同次输入、候选、双端实际消费与复验记录。不能拼成一条已经验收的事件 | [技能系统 plan](skill-system-series-plan.md)、交付工程与工程实践 plan；产物：原文局部补桥／证据清单，基础讲解可先继续 |
| G05 客户端、服务端与内容负责人怎样分工和联调？ | [权威与同步概念](../content/et-framework-prerequisites/et-pre-10-state-sync-lockstep-and-authoritative-server.md)、[技能多人同步](../content/system-design/skill-system-11-multiplayer-sync.md)、[跨端版本协调](../content/delivery-engineering/delivery-server-versioning-01-coordination.md)；状态裁决、请求与表现、协议／配置配套 | 已有不同层的正文，不需先写完整网络或 ET 课程；需说明运行时权威、共享定义和版本交接分别解决什么问题。旧实现与代码示例按各原文基线核查，概念存在不代表工程跑通 | C17→C18 是教学错配情景，不是真实双端联合验收；可沿 M01 准备一个实际覆盖范围明确的事件，不给单端事件强加服务端过程 | [ET 前置 plan](et-framework-prerequisites-series-plan.md)、技能系统 plan、[交付工程 plan](delivery-engineering-column-plan.md)；产物：职责与兼容／验收关系的阅读补桥 |
| G06 怎样固定任务、构建和测试所对应的版本？ | [版本控制模型](../content/delivery-engineering/version-management-01-model.md)、[任务提交](../content/delivery-engineering/version-management-05-task-submission.md)、[VM-09 构建基线提纲](../content/delivery-engineering/version-management-09-build-baseline.md)；工作区、候选、依赖、版本身份与工具链 | VM-01／05 已有正文；VM-09 明确为提纲，正文与实验待写。可先用已有共同模型解释候选身份，再按原系列顺序推进 VM-09，不据有文件或 `draft: false` 算成稿 | VM-05 记录的是同机 Git 教学实验，不能代替真实引擎交接或所有 SVN／P4 操作。多版本与可发布案例的单次对应记录仍归 M01 | [版本管理 plan](version-management-series-plan.md)、工程实践 plan；产物：原篇补桥，VM-09 的后续起草／验证任务按原 plan 登记 |
| G07 构建成功，为什么运行仍会失败？ | [AOT 漏包案例](../content/engineering-journey/production-build-runtime.md)、[HybridCLR 生成与消费链](../content/engine-toolchain/hybridclr-toolchain-what-generate-buttons-do.md)；产物生成顺序、消费位置、归档与运行配套 | 基础和局部历史案例已有，先复用阶段与产物关系；机制深读沿原版本与实验材料审校，不扩为整套运行时课程 | 历史漏包定位与修复已有作者说明；新实验的范围和历史事故版本分开，固定实验仓库提交／链接的待办仍按原记录处理，不当成本轮已完成 | [HybridCLR plan](hybridclr-series-plan.md)、工程实践 plan；产物：必要的生成／收集顺序说明和来源核对，不重讲所有按钮 |
| G08 优化需要哪些基础，怎样从现象找到可验证的改动？ | [性能判断](../content/performance/game-performance-judgment-framework.md)、[诊断方法](../content/performance/game-performance-diagnosis-method.md)、[资源可用链](../content/performance/game-performance-read-does-not-mean-ready.md)、[调度与等待](../content/performance/game-performance-thread-scheduling-waits.md)；指标、关键路径、运行／等待／排队、内存口径 | 多项基础已有；新调度正文是概念与教学算例，没有设备 trace。按客户端基础提纲（本地 `docs/client-performance-foundations-outline.md`）及当前性能 plan，后续先补虚拟内存与内存占用口径，再审校 OS I/O；不宣称通用 OS 阶段已全部完成 | [持续性能 05](../content/engineering-journey/production-continuous-performance.md)仍为提纲。M02（本地 `docs/b01-m02-continuous-performance-material-status-2026-10-30.md`）记录的 Android 纹理实验范围有限，缺同场景基线→诊断→改动→复测，不支持卡顿治理或持续门禁已运行 | [性能 plan](game-performance-series-plan.md)、工程实践 plan；相关专题归属开做前沿原路线核对；产物：沿原路线补知识与必要修订，案例测量后续独立推进 |
| G09 一次解决怎样进入日常使用、规范和持续检查？ | FDE 两篇方法、Missing 的文档／宣讲／跟进过程、[分档验证与基线](../content/code-quality/device-tier-validation-matrix-baseline-and-visual-regression.md)；入口、维护责任、触发、复检、适用条件与回流 | 方法已有，补桥重点是把具体技术判断对应到谁使用、何时执行、失败退回与何时重看规则；不将新增脚本数量视为沉淀完成，不另写泛化平台故事 | Missing 能支持其局部采用过程；nightly／release gate 文章是方法，不证明团队已有持续执行。具体触发、失败处理、误报和规则退出仍需对应记录 | FDE、[工程判断 plan](engineering-judgment-series-plan.md)、[预算 plan](game-budget-management-series-plan.md)及工程实践 plan；产物：从已有问题到使用／维护／复验的对应说明 |

## 3. 当前推进建议

以下是本表的执行建议，不是新篇级目录；不要求九项依次全部完成，既有专业路线保留自己的依赖顺序。

1. **先复用与补阅读桥梁**：G01／G02 将已有交接、问题发现和能力沉淀说明接起来；G03／G04／G05 明确各自机制怎样服务工程判断。先处理一个具体读者问题，再决定原文补节还是只需准确链接，不自动新增同义文章。
2. **沿原计划补已识别的基础缺项**：G08 的虚拟内存与内存口径、OS I/O 审校按性能原路线推进；G06 的 VM-09 按版本管理系列的前序安排推进。两条是可独立选择的任务，不互设门槛，不提前标为完成。
3. **接上方法与个人判断**：基础足以解释问题后，写清方案选择、取舍和限制。基础原理可以独立成文；只有具体段落需要亲历时才用 col-voice，方向与缺口讨论不启动采访。
4. **再充实案例及证据**：优先复用 G03／G07 的既有局部案例，配置／双端及性能沿 M01／M02 补材料。取材可以并行准备，但不能把缺案例变成阻止基础写作的理由，也不能预填成果。

## 4. 完成与回写规则

- 每次执行只选明确任务，先读取原 plan／正文并核对最新状态。新增独立文章须先在原 canonical plan 登记，本表不自行分配篇号。
- 基础工作的完成以问题是否解释清楚、来源和条件是否明确、必要阅读关系是否成立为准；实验结果、真实案例与发布状态另行验收。
- 内容写完先回原 plan 和必要索引，本表只更新对应缺口与引用；不向根 `doc-plan.md` 回填篇级列表。
- 本轮只核对本地材料与原记录，没有重新运行 Git／Unity／设备实验，没有核实线上内容状态。正文引用的旧实验仍属于原记录，不计为本轮复验。
- 正文、案例、验证、提交与部署不合并成一个“完成”。本表不修改页面、URL 或公开阅读顺序；后续写作再按仓库规定验证。
