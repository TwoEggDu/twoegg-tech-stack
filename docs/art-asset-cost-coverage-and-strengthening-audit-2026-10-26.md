# 资产成本内容覆盖、缺口与补强盘点

> 日期：2026-10-26。范围：纹理、音频、模型/动画、Shader 四个方向。
> 目标读者：客户端程序，先理解原理和因果关系，能判断下一步该查什么，配少量 Unity 检查。
> 当前进度：五篇 P0 及两组 P1 共十篇旧文已完成文字修订并纳入已授权交付。下一步 V-T / V-C 已完成工作单、环境只读预检和原创源素材准备，实际 Unity/Android 对照尚未执行，见第 12 节。统计/算法示例仅有有限合成检查，不写新增文章。这是跨栏目执行记录，不是新的 canonical series plan。

## 1. 先确定用什么标准判断“写过”

本次不按关键词出现次数统计完成度，而是检查读者能否回答：

1. **对象是什么**：源文件、导入数据、构建产物、运行对象分别是什么？
2. **为什么有成本**：哪些数据被哪些功能使用，形成什么存储、加载或运行工作？
3. **怎样取舍**：减少哪项数据或功能，会转移哪些成本，改变什么表现？
4. **该查什么**：下一步的数据、设置、引用、工具或观察入口是什么？
5. **证据到哪一步**：资料说明、理论计算、静态代码、待执行检查和运行结果是否分清？

覆盖状态使用以下口径：

| 状态 | 含义 |
| --- | --- |
| 有连续解释 | 已有正文能承接主要因果，不代表所有事实正确或完成设备验证 |
| 部分覆盖 | 有段落、示例或方法，但问题到判断之间仍有缺环 |
| 缺连续解释 | 在本次相关正文中尚未形成可独立使用的解释链；不声称全站没有任何提及 |
| 待纠错/补证 | 已写内容存在口径、版本、泛化或证据问题，需要先处理 |

检索覆盖 `content/`，再阅读命中的相关正文与前序盘点；没有逐篇审完所有算法、平台与工具文章。下列结论以本地正文为对象，不等于线上发布状态或工程验收状态。涉及具体机制的核对采用 Unity 6000.0 与表列官方资料，未拿不同版本的数字直接替换旧结论。

与前序工作的关系：Samsung/Arm 导入专题、七篇旧文修订与两篇新稿见[原盘点](mobile-art-asset-cost-content-audit-2026-10-26.md)。本次增加音频、动画与跨栏目承接审查，不把它们说成 Samsung 几何、纹理、材质或光照指南已覆盖的内容。

## 2. 当前结论与现有内容分工

| 方向 | 已经有的基础 | 当前主要缺口 | 编辑判断 |
| --- | --- | --- | --- |
| 纹理 | 存储/格式/Mip、带宽、通道打包、分档配置，以及两篇新稿中的使用条件 | V-T / V-C 工作单与原创源素材已准备；从目标镜头到导入/Player 的实际贯通对照仍缺 | 先执行尺寸、格式与 Alpha 最小对照，暂不重开纹理总论 |
| 音频 | 源文件与播放策略、多语言拆包、平台参数、三种 Load Type 已有内容 | 缺按声音用途连接频率内容、声道、编码、加载、并发、首播和听感；内存、质量规则与体验文的相关表述已修，素材与结果仍缺 | 继续补强承接与证据；独立原理篇有候选价值，尚不定题 |
| 模型/动画 | 几何、属性与动画压缩原理；Importer 已补消费者对照，算法文已区分版本/入口和最终姿态误差 | 已有“沿必要功能确认数据”的方法入口，仍缺真实资产与压缩前后对照，不能据表直接删除 | 后续先补素材和观察，再判断旧文是否仍无法承接，不预设独立篇 |
| Shader | 变体来源、裁剪层次、动态使用契约、数量基线与 CI 已有专文 | CI 已区分观察点、缓存完整性与可比口径；构建产物和运行覆盖证据仍未闭合 | 内容数量充足，后续补真实采集/回执与动态路线，不增加同题总论 |

### 2.1 已有文章各自应继续承担什么

| 主要承接 | 保留职责 | 本次定位 |
| --- | --- | --- |
| [成本总览][overview]、[几何成本][geometry] | 程序理解数据、用法、工作与证据，后者展开几何取舍 | 两篇本地新稿已形成原理入口；配套素材、检查和采样仍待执行 |
| [存储基础][storage]、[带宽][bandwidth]、[Shader 优化][shader-opt] | 数据布局、GPU 格式、Mip、数据访问与着色输入 | 继续作为机制主要解释位置，补应用例子而非再复制基础概念 |
| [资产规格][asset-spec] | 候选规格、质量分档与交付边界 | 已明确教学候选；不应让其他旧文又把这些候选写成固定标准 |
| [Importer][importer]、[专用资产输入][asset-input] | 源数据怎样转为各子系统的输入 | Importer 已补属性/骨架/曲线/事件消费者与三步检查，区分导入、构建和运行；实际移除/保留对照未执行 |
| [内存预算][memory] §Audio | 音频驻留、加载与播放阶段的内存问题 | 已有专节，示例说明和证据边界已修；尚无运行与听感结果 |
| [包体控制][package]、[资产精简与分包][trimming]、[多端差异][multiplatform] | 包体贡献、分发范围、平台规则与多语言按需交付 | 负责语言包与打包引用检查；不承担完整音频 DSP 或听感课程 |
| [动画压缩算法][anim-algo] | 误差、关键帧简化、量化与运行表示 | 已核对 Unity 6000.0 入口/Rig 条件，限定第三方指标和代码演示；独立 .NET 合成检查不等于真实动作验证 |
| [绑定蒙皮][rig]、[动画制作][animation]、[联合验收][acceptance] | 输出层级、挂点、片段、混合、动作与画面接受条件 | 已有压缩关闭/目标配置的对照步骤；它们仍是待执行方案 |
| [变体数量监控][variant-ci]、[动态覆盖契约][variant-contract]、[关键字设计][keywords]、[裁剪层次][stripping] | 数量、使用需求、声明机制与构建过滤 | CI 已补统计阶段、范围和基线边界，与保留/交付/命中文互链；实际采集器、回执与动态验证仍缺 |
| [资源质量规则][quality]、[体验一致性][experience] | 如何约束与评估质量 | 固定参数、普遍听感判断和统一 LOD 要求已改为有条件的候选与接受问题；没有门禁、设备或体验验证结果 |
| [给美术的成本直觉][artist-intuition] | 为美术解释覆盖、叠层、首次出现和组合风险 | 与程序总览受众不同，应互链；不据共同例子就合并成一篇 |

## 3. 逐项覆盖与缺口对应表

表中区分已执行的 P0/P1 文字修订与后续建议，不把其余建议视为已授权或已完成。V 编号是第 5 节的验证需求，不复用原盘点的 V1—V10，以免两个编号体系混淆。

### 3.1 纹理

| 编号 | 读者要能回答的问题 | 现有承接与状态 | 还缺什么 | 优先承接与证据 |
| --- | --- | --- | --- | --- |
| T1 | 目标画面需要多少纹理细节？ | [存储][storage]解释采样足迹/Mip；[总览][overview]解释镜头条件；有连续基础 | 从匹配镜头、UV 分配、渲染分辨率到候选尺寸，缺一组贯通的画面对照 | 补存储/规格文例子；V-T |
| T2 | 平台导入配置与目标能力是否吻合？ | [带宽][bandwidth]、[规格][asset-spec]已有边界；[多端差异][multiplatform]已修 WebGL/ASTC 与回退口径 | 导入预期、实际 GPU 格式、能力与交付回退没有共用记录 | R4 文字已修；后续补检查记录，V-T |
| T3 | Alpha 和各通道是否真的需要？ | [带宽][bandwidth]区分 Alpha 存储与混合，[Shader][shader-opt]给出打包条件；部分覆盖 | 谁读取每个通道、什么动态功能会用到，以及未使用时是否真的改变产物，缺完整例子 | 补 Shader/带宽既有小节；V-C |
| T4 | 缩小的是包体、驻留还是实际访问？ | [存储][storage]与[总览][overview]已分层；[包体文][package]已区分 Read/Write 运行副本与构建体积 | 理论存储量与实际产物、设备访问仍缺对应观察 | R1/R2 文字已修；后续按需安排 V-T，不拿公式代替计数 |
| T5 | 压缩后的误差能否接受？ | [规格][asset-spec]列了候选与待补数据；已有检查方向 | 固定镜头下颜色、透明边缘、法线/参数与最终材质的对照材料不足 | 补一组可复用素材和记录，不另建固定格式表；V-T/V-C |

### 3.2 音频

| 编号 | 读者要能回答的问题 | 现有承接与状态 | 还缺什么 | 优先承接与证据 |
| --- | --- | --- | --- | --- |
| A1 | 是否携带当前交付不需要的语言？ | [包体][package]、[精简/分包][trimming]已讲语言按需；部分覆盖 | 默认语言、切换、回退、依赖闭包与实际产物重复的检查未串起来 | 补包体文的一组交付对照；V-L |
| A2 | 为什么选某个采样率与声道？ | [多端差异][multiplatform]已限定参数；[质量规则][quality]已撤下通用频率、音乐编码/质量与文件上限 | 频率内容、用途、空间/立体声需求与混音结果仍缺贯通素材和对照 | R6 文字已修；V-A 待执行，再判断是否需要独立篇 |
| A3 | 编码与 Load Type 分别改变什么？ | [Importer][importer]、[资产输入][asset-input]和[内存][memory]已有内容；内存音频节已纠错 | 首播、稳定播放、并发与各项实际开销仍缺素材和记录 | R7/R8 文字已修；V-A/V-P 待执行 |
| A4 | 单个文件不大，为什么同时播放仍需检查？ | [内存][memory]涉及播放路径，但未形成按并发和用途的连续解释 | 缺声音数量、解码/混音、缓冲与负载变化的关联；不能仅按片长定策略 | 音频原理篇候选的核心缺口之一；V-P |
| A5 | 改动后听感和响应是否可接受？ | [体验一致性][experience]已撤下普遍收益/听感与耳机/外放判断，列出接受条件 | 缺音量匹配、素材/输出设备、首播、循环接缝及目标玩法条件的实际比较 | R13 文字已修；V-A/V-P 待执行 |

### 3.3 模型与动画

| 编号 | 读者要能回答的问题 | 现有承接与状态 | 还缺什么 | 优先承接与证据 |
| --- | --- | --- | --- | --- |
| M1 | 顶点/属性/索引为什么形成不同数据量？ | [几何][geometry]、[存储][storage]有连续解释 | 主要缺实际导入对照，不能再按“没有原理文”立新题 | 复用已有新稿与原验证计划，不重写 |
| M2 | 法线、切线、UV、颜色或 BlendShape 能否不保留？ | [Importer][importer]已列 Shader/pass、光照、变形与动态功能等消费者；[几何][geometry]解释用途 | 缺将真实资产、全部必要功能、移除前后结果和实际产物对应的记录 | Importer 解释桥已补；V-M 待执行，是否成篇仍待定 |
| M3 | 哪些骨骼、层级、挂点或动画数据仍被消费？ | [Importer][importer]已统一列蒙皮、Avatar/重定向、IK、脚本、挂点、状态与事件；[绑定][rig]/[动画][animation]承接细节 | 缺真实消费者清单与功能路线；默认轨道省略、层级优化或一段动作没动都不能证明可删骨骼 | 文字边界已补，避免通用自动删除清单；V-M/V-N 待执行 |
| M4 | 压缩参数和误差具体改变什么？ | [算法][anim-algo]已区分录制过滤/模型导入、Rig 条件、参数单位与理论误差；[存储][storage]区分 Mesh/Vertex Compression | 缺选定真实片段的压缩、文件/驻留、采样和最终姿态对照；合成离散检查不足以证明接受 | Unity 6000.0 映射已修；V-N 待执行 |
| M5 | 压缩后的视觉和玩法能否接受？ | [动画][animation]已有关闭/目标压缩配置对照与轨迹/循环检查，[验收][acceptance]有方法 | 当前局部手臂材料不能覆盖全身脚接触、武器/挂点、重定向、根运动和混合误差 | 补明确材料与观察范围，不把“待执行”标成缺方法或已验证；V-N |

### 3.4 Shader

| 编号 | 读者要能回答的问题 | 现有承接与状态 | 还缺什么 | 优先承接与证据 |
| --- | --- | --- | --- | --- |
| S1 | 变体数量是否异常，比较的是什么数量？ | [CI 监控][variant-ci]已补程序阶段/观察点、计数单位、版本/产物范围、缓存完整性及基线审查 | 缺真实采集、明细/汇总、构建回执与产物对照；合成 JSON 只验证对比逻辑 | 报告口径文字已补，V-S 待执行，不新增数量监控篇 |
| S2 | 裁剪是否漏掉动态使用？ | [契约][variant-contract]已有动态路径；已区分需求、保留、交付与命中，撤下确定性保证 | 缺将具体需求接入保留规则，再对照真实构建和运行的记录 | R9/R11 文字已修；V-D 待执行 |
| S3 | 声明方式、裁剪阶段和 SVC 各负责什么？ | [关键字][keywords]、[包体][package]已修候选/最终保留和 SVC 职责，链接[裁剪][stripping] | 具体版本数量上限仍需选版核对，实际回调与保留结果待查 | R3/R9/R10 文字已修；不填未经核对的替代上限，不再写第四套机制解释 |
| S4 | 测试覆盖如何证明？ | [契约][variant-contract]解释静态与运行追踪的互补；部分覆盖 | 缺“配置×内容来源×动态状态×目标 API”的实际路线、结果与回执对应 | 复用已有验收方法，补最小矩阵；V-D |
| S5 | 数量变少后，功能与体验是否仍正确？ | [CI][variant-ci]关注数量，[验收][acceptance]关注结果，两者尚未闭合 | 缺将构建变化与 Player/Bundle 组合、运行命中和首用表现关联的具体记录 | 作为修订验证需求，不自动扩成新教程；V-S/V-D |

## 4. 旧文中需要先处理的高风险表述

以下保留**修订前的审查快照**，不是七篇已修旧文的当前引文。共定位 **13 处**高风险表述，均已完成对应文字处理：P0 的 10 项见第 8 节，P1 的 R6/R12/R13 见第 9 节。该状态不表示相关工程验证完成，也不表示整个栏目已无其他风险；不将“未核实”直接等同于“已经证伪”。

| # | 原句与位置 | 风险类型 | 依据与问题 | 最小修订方向 | 仍需补查 |
| --- | --- | --- | --- | --- | --- |
| R1 | [包体控制][package]：“Read/Write Enabled 导致内存和包体双份” | 度量混淆 | [TextureImporter.isReadable][u-readable]说明的是额外 CPU 可寻址副本；不能据此将构建文件也定为双份 | 改为“Read/Write 可能增加运行时 CPU 副本；构建体积变化另看构建报告。” | 构建体积的具体差异待观察 |
| R2 | [包体控制][package]：“UI 贴图关闭 Mipmap” | 条件缺失 | UI 的实际缩放、投影、过滤与用途需要确认；已有存储文已解释采样尺度 | 改为“按 UI 的实际显示与缩放条件比较是否需要 Mip，并检查画质与占用。” | 固定/缩放/空间 UI 的实际材料 |
| R3 | [包体控制][package]：“使用 IPreprocessShaders 或 ShaderVariantCollection 裁剪未使用的变体” | 职责混淆 | [SVC][u-svc]记录变体并提供集合/预热接口；[构建回调][u-shader-callback]才是自定义处理入口，二者不能并列成等效裁剪器 | 分开写自定义裁剪、需求集合、构建保留与预热，不承诺集合自身完成通用裁剪 | 具体保留配置与回调执行结果 |
| R4 | [多端差异][multiplatform]纹理表：“ASTC（WebGL 2.0+）” | 平台能力泛化 | ASTC 在 WebGL 中有[独立扩展规范][u-webgl-astc]；WebGL 版本不能单独证明目标设备支持该格式，微信运行环境还需自身证据 | 改为“按目标运行环境的实际 ASTC 能力与交付回退配置。” | 微信环境、SDK、设备和实际格式 |
| R5 | [多端差异][multiplatform]：“模型差异通过 LOD（Level of Detail）系统管理，不需要维护多套模型。” | 与新文口径冲突 | [几何新稿][geometry]与[LODGroup 文档][u-lod]都要求区分级别、Renderer 和对应数据；LOD 管理选择，不消除不同细节资产的制作/维护 | 改为“可用 LOD 管理不同细节版本的选择，仍需维护相应网格、配置和回归。” | 实际资产与资源驻留 |
| R6 | [质量规则][quality]：“音效采样率不超过 44100Hz，大部分 SFX 用 22050Hz 就够” | 裸数字/用途不足 | [音频导入文档][u-audio]给出按频率内容和配置选择的机制，不能推出统一上限和多数素材的听感结论 | 改为“按频率内容、用途与目标播放条件比较采样率，候选参数需经听感与运行验证。” | 代表音效与听感比较 |
| R7 | [内存预算][memory]：“背景音乐 → Streaming（内存几乎为零）” | 过度概括/数字缺证据 | [官方音频文档][u-audio]明确有每个 Streaming Clip 的基础开销及缓冲；同节“约 0.1 MB”没有测量条件，不能当通用结果 | 改为“Streaming 可减少整段解码常驻，但仍有 Clip、缓冲与解码等开销；按并发和设备观察。” | 具体素材、并发、内存与 I/O |
| R8 | [内存预算][memory]代码注释：“代码中动态加载 AudioClip 时指定 Load Type” | 示例不支持说明 | 其代码只请求音频并调用 `LoadAudioData`；[该方法][u-load-audio]加载数据，不是 Load Type 设置器；下载流式行为另见 [streamAudio][u-stream-audio] | 改名为“请求并加载音频数据”的示意；如讲流式，单独列出下载与播放准备条件 | 代码运行、首播和断流行为 |
| R9 | [关键字设计][keywords]：“无论项目里有没有材质使用这些 keyword 组合，所有排列都会被编进构建。” | 阶段混淆 | 声明方式影响候选，仍可经过[内置/管线/项目裁剪][u-stripping]；同站[裁剪层次文][stripping]已解释这些阶段 | 改为“multi_compile 的候选不依赖同样的材质使用过滤，但最终保留还取决于后续构建处理。” | 选定管线与构建回执 |
| R10 | [关键字设计][keywords]：“Unity 对每个 shader 程序的全局 keyword 数量有上限（在不同平台和 Unity 版本上约为 256 个）。” | 版本基线不足 | 混合多个版本、全局/局部范围和计数对象，却给单一近似数值；本次未完成该上限的选版核对 | 先去掉未限定上限，明确版本、范围与对象后，再补对应官方数值 | **需按选定版本重新核对** |
| R11 | [动态覆盖契约][variant-contract]：“声明了就会进包，没声明就不进包，行为完全可预测。” | 把需求当结果 | 同文说明数据库是项目自定义工具；登记不是引擎保留保证，且仍有裁剪/构建/交付边界 | 改为“登记使动态需求可审查，是否保留和交付仍须接入构建规则，并用产物和运行证据确认。” | 构建规则、回执与实际命中 |
| R12 | [质量规则][quality]主角模型预算：“至少 3 级 LOD” | 参考值缺少条件 | 同文虽说数字可调整，表格仍易被直接当规则；新几何稿已解释应先比较简化收益、额外数据与维护 | 改为“LOD 是否启用、级数与阈值由对象贡献和比较结果确定；若列数字，标明教学候选条件。” | 使用镜头、对象规模与候选收益 |
| R13 | [体验一致性][experience]：“这些操作大部分情况下省的 CPU / 内存极其有限，但感知代价很大。” | 无据经验泛化 | 未给素材、加载/编码、并发、设备、输出和听感条件；不能只由听觉身份观点推导成本大小 | 改为“应同时比较运行成本与听感；重要音效的辨识需求先列为接受条件。” | 素材、运行采样及听感依据 |

补充线索的当前状态：质量文的统一动画时长、音乐编码/质量、贴图/几何预算和体验文的耳机/外放、动画更新/骨骼、LUT 与反馈成本判断已在 P1 文字修订中处理。质量文开篇的商店固定限制、包体增量和 OOM 叙事已撤下，替换为区分理论像素载荷的教学情境；未取得实际产物与设备结果。其余栏目中的平台商店、代码裁剪及内存非音频内容仍不由本轮代为背书。

## 5. 缺的验证材料，而非缺一个“实测数字”

以下均为**实际对照待执行需求**。V-T / V-C 的工作单、只读环境预检与源输入已准备，见[最小验证工作单](mobile-texture-channel-validation-workorder-2026-10-26.md)；不据准备状态登记运行通过。先有素材、变量和观察问题，才决定是否值得进一步测量，不要求一次启动完整实验工程。

| 验证标识 | 要回答的问题 | 最小材料与控制条件 | 证据产物与结论限制 |
| --- | --- | --- | --- |
| V-T 纹理使用与格式 | 实际需要的细节和目标配置是否吻合？ | 一个可公开纹理/模型，固定镜头、UV、渲染分辨率和材质；分别比较尺寸、块格式或过滤，每次聚焦一项 | 匹配视角、实际格式、Mip/占用与能力记录；理论字节不代替 GPU 访问或时间 |
| V-C 通道消费 | 某个通道是否可以不携带，打包是否值得？ | 一份通道语义表；枚举静态和动态材质用途；比较前后各通道压缩误差及最终材质，记录格式/尺寸/采样变化 | 消费者与效果对照；不能把当前画面没用到等同永远无用，也不把通道少直接等同块格式变小 |
| V-L 语言交付 | 是否有不需要的语言或同资源重复进入目标交付？ | 至少两种语言的可识别素材，定义默认、切换和回退；固定目标构建与首包/按需包，检查实际依赖/清单 | 资源身份、产物归属、重复依据与语言切换记录；合理多语言需求不判成重复；不只按文件名删除 |
| V-A 音频表示与质量 | 采样率、声道和编码怎样影响候选？ | 有权公开的短音效、对白/长片段，记录源频率内容、声道、用途、编码与候选参数；听感比较匹配音量和输出条件 | 源/导入/产物信息与听感记录；不套统一采样率、质量或“听不出”结论 |
| V-P 音频加载与播放 | Load Type、并发和首播分别影响什么？ | 固定一个表示后比较加载路径；再独立改变并发，分别记录加载、首次播放、循环和稳定播放；明确网络/本地来源 | 内存、解码/混音或 Streaming CPU、I/O 与响应记录；不以单 Clip 结果保证全部设备或流式零开销 |
| V-M 模型数据保留 | 哪些数据真的没有必要？ | 对法线/切线/UV/颜色/BlendShape/层级逐项列消费者与功能；使用副本，一次改变一类数据，检查材质、光照、变形和挂点 | 保留理由、导入结果与完整必要功能记录；数据变少但功能丢失不算通过，无法确认消费时保留待查 |
| V-N 动画清理与压缩 | 哪些轨道、精度和级别满足使用？ | 版本与 Rig 类型固定，原始/候选分别检查；除局部手臂外，若要讲脚接触或根运动，需真实全身材料和相应路线 | 曲线/片段、局部及最终姿态、混合、重定向、根运动/挂点记录；局部误差不能自动代表最终画面或玩法误差 |
| V-S Shader 数量与产物 | 统计是否可比，异常发生在哪一层？ | 固定 Unity/管线/API、目标与设置；标记候选、回调顺序/阶段、裁剪及最终可取得的构建报告，区分 Player/Bundle | 带阶段和构建身份的报告/回执；某回调快照不是全部最终变体总数，数量变化本身也不是故障结论 |
| V-D Shader 动态覆盖 | 声明的动态需求是否真的可用？ | 选最小天气/质量档/动态材质/热更路线；将需求、裁剪规则、Player/Bundle 版本与目标 API 对应 | 声明→构建保留证据→交付→运行结果；记录没跑到的路径，不能从一次 Editor 运行推导全覆盖 |

共用记录字段可复用[联合验收][acceptance]，本次不再复制一套大型验收模板。算法公开 benchmark、理论字节与示意图都不能替代这些观察结果。

## 6. 建议先补强什么，再决定写什么

### 第一层：P0 与两组 P1 均已完成本地文字修订

| 优先组 | 建议承接 | 最小目标 | 本次状态 |
| --- | --- | --- | --- |
| P0 成本与能力边界 | [包体控制][package]、[多端差异][multiplatform] | 修正运行副本/包体、SVC 职责、WebGL/ASTC 能力与 LOD 维护口径；链接已有主要解释 | 本地文字已修；实际产物、格式与画面对照待执行 |
| P0 音频加载 | [内存预算][memory]仅 Audio 节及对应检查项 | 纠正近零内存、未限定数字、代码说明与 Load Type 的关系；保留原有内存职责 | 本地文字已修；代码未编译运行，内存/首播/听感待验证 |
| P0 Shader 保证 | [关键字设计][keywords]、[动态覆盖契约][variant-contract] | 分清声明候选、保留规则、构建结果与交付命中；移除未限定上限，核对作用范围 | 本地文字已修；没有发布新的通用上限，构建/运行验证待执行 |
| P1 规格与体验条件 | [质量规则][quality]、[体验一致性][experience] | 把固定参数改成有条件候选；无据群体/听感判断补证或限定 | 两篇本地文字已修，R6/R12/R13 已处理；规则/CI 与设备、画面/听感验证未执行 |
| P1 统计与验证桥 | [变体 CI][variant-ci]、[动画算法][anim-algo]、[Importer][importer] | 补统计阶段、版本与数据消费者的解释；不扩成新框架或工具项目 | 三篇本地文字已修；Python/.NET 合成检查通过，实际采集、导入、产物和运行验证未执行 |

P0 指优先消除误导，不表示都需要跑设备才能改文字。能由官方定义或文内逻辑确认的边界可先纠正；性能、听感、画面与真实产物保留结论继续等证据。

### 第二层：只把确实缺的连续解释列成候选

| 候选方向，标题未定 | 独立成篇的理由 | 与旧文的边界 | 进入提纲前的条件 |
| --- | --- | --- | --- |
| 程序理解音频资产成本 | 按声音用途连接表示、导入、加载、并发和响应，目前跨文仍缺连续因果 | 内存专文保留驻留与工具细节；包体文保留语言/分发；不扩成 HRTF、卷积混响或完整音频引擎教程 | 先修音频承接文；明确代表素材和 V-A/V-P 要回答的问题；若补旧文即可完整承接，可不新增 |
| 模型/动画哪些数据应交给运行时 | “不需要”必须沿消费者与功能确认，数据量原理与实际清理之间尚缺判断方法 | 不重写顶点、LOD、动画压缩算法或 DCC 制作；绑定/挂点/动画关系仍由旧文主要解释 | 先试补 Importer/存储/动画文；若例子仍过碎，再讨论共用原理篇或更窄选题，不预设把模型和动画硬并成一篇 |
| 纹理从屏幕需求到配置对照 | 现有原理已够，缺应用证据；是否成篇取决于例子是否有独立问题 | 不重写 ASTC 表、Mip 与通道打包定义 | 优先完成 V-T/V-C 并补旧文，暂不安排新总论 |
| Shader 再写一篇总论 | 当前没有充分理由 | 已有系列覆盖来源、裁剪、覆盖、交付和 CI | 本次不建议新增；先完成口径修订与最小回执/运行矩阵 |

### 第三层：再组织阅读路线

事实与边界统一后，再在现有移动性能/资源管线入口组织：成本总览 → 对应机制 → 使用条件与检查 → 预算/验收。原有美术成本直觉文继续面向美术；两篇新稿面向程序，以交叉链接连接。无需为了四个资产类别新建一个重复系列索引。

本次结论是：**优先修正已写内容的边界，再补数据消费与验证之间的解释桥，最后凭剩余缺口决定新增篇目。** 不把“有标题”当作覆盖完成，也不为了填目录先承诺文章数量。

## 7. 资料与复核边界

| 官方依据 | 本次用途 | 不能据此宣称 |
| --- | --- | --- |
| [Audio Clip Import Settings][u-audio]、[LoadAudioData][u-load-audio]、[DownloadHandlerAudioClip.streamAudio][u-stream-audio] | 核对加载/采样率/声道与下载流式职责 | 已确定所有素材的最佳参数、实际内存与听感 |
| [Model Import Settings][u-model]、[Animation Import Settings][u-animation] | 核对可保留数据、压缩选项、Rig 条件与误差解释 | 已确认项目某数据可以删除，或某压缩误差能保证完整视觉效果 |
| [TextureImporter.isReadable][u-readable]、[WebGL ASTC 扩展][u-webgl-astc]、[LODGroup][u-lod] | 区分运行副本、图形能力和 LOD 资产/选择 | 已验证微信运行环境、包体变化或 LOD 收益 |
| [ShaderVariantCollection][u-svc]、[OnProcessShader][u-shader-callback]、[Shader stripping][u-stripping] | 区分集合、构建处理与后续剔除；支持统计阶段审查 | 已确定所有版本 keyword 上限、项目实际保留与运行覆盖 |

旧链接若不能取得对应版本内容，不把搜索到的其他版本直接当成替代证据；例如 R10 的 keyword 上限仍列待核。对外部公开算法和平台数值，本次只登记复核需求，不据二手转述填结果。

初次盘点交付范围：本文件与原盘点的一处执行层链接。当时正文保持原状，新增篇目仍为候选，未提交、推送或发布。后续用户确认的 P0 修订见第 8 节。

初次盘点检查：两份文档的 53 处本地链接、引用标签、表格列数、围栏与空白检查通过；20 项覆盖编号、13 条引文与当时正文逐条匹配，9 项验证需求编号检查通过。Hugo 本地构建退出码为 0、无 ERROR。以上是初次盘点阶段的记录，不代表本轮修订后的检查，也不代表工程实验、设备测量或线上验收。

## 8. 2026 年 10 月 26 日 P0 旧文事实与边界修订

按用户确认顺序完成五篇旧文的本地文字修订，不写新增文章、不推进 P1、不提交或推送。第 4 节保留原问题快照，避免修订后丢失追踪依据。

| 修订承接 | 已处理风险 | 当前文字变化 | 尚未完成的证据 |
| --- | --- | --- | --- |
| [包体控制][package] | R1/R2/R3 | 区分 CPU 可寻址副本与构建体积；Mip 按使用条件比较；分开 SVC、裁剪与保留；贡献度比例改为待统计 | 实际构建占比、Read/Write 产物差异、Mip 画质/占用、Shader 保留回执 |
| [多端差异][multiplatform] | R4/R5 | WebGL 版本不等于 ASTC 支持，尺寸或块大小不代替格式回退；LOD 仍需网格/配置/维护，阈值区分相对屏幕高度；音频参数限定为待比较示例 | 目标宿主/SDK/设备能力、实际格式与回退、分档产物、LOD/动画和听感；小游戏配额另行选版核对 |
| [内存预算][memory] Audio 节及对应检查项 | R7/R8 | 撤下近零内存与虚构的压缩/流式结果；区分导入 Load Type、下载 API 与 LoadAudioData；仅保留指定 PCM16 表示的理论载荷 | 教学代码编译/运行、加载/首播/稳定播放、并发、内存/CPU/I/O 与听感 |
| [关键字设计][keywords] | R9/R10 | 区分声明候选与后续保留；修正 `_local` 的全局覆盖语义，移除未限定的历史上限；三组选项理论计数改为 3×2×2=12 | 选定版本和计数对象的具体上限、管线/API 构建回执、动态组合命中 |
| [动态覆盖契约][variant-contract] | R11 | 需求登记不保证进包，未登记也不保证不存在；Editor 追踪记录使用路线，不代表完整覆盖；分别确认保留、交付和运行 | 需求数据库接入、SVC/裁剪配置、Player/Bundle 产物与目标 API 路线 |

P0 阶段对应 **10 个风险编号的文字处理已完成**，不等于 10 项工程验证完成。当时 R6/R12/R13 留待 P1，其后续修订见第 9 节；第 5 节 **9 类验证需求全部保持待执行**，没有选择新的音频采样率、格式、LOD 级数或 keyword 数字作为通用标准。

本轮依据 Unity 6000.0 的 Audio Clip、isReadable、Shader 声明/脚本关键字/集合/Graphics Settings/裁剪资料及 WebGL ASTC 扩展区分职责；作用范围与候选数量分开解释，不用文档说明冒充本项目结果。PCM 示例仅为 31,752,000 bytes = 31.752 MB ≈ 30.28 MiB 的理论有效载荷，不是 Unity 进程内存。

本轮修订检查：五篇文章的 16 处 `relref`、两份盘点的 54 处本地链接、引用标签、表格列数、围栏、空白及字符检查通过；20 项覆盖、13 个历史风险与 9 类待执行验证编号完整。按 Git 基线比较，内存长文除 Audio 节、对应检查项和 lastmod 外无改动；理论组合数与 PCM 算式核对通过，范围内 `git diff --check` 通过。

Hugo 完整本地构建退出码为 0、无 ERROR；五个生成页面及 16 个文章链接目标存在，DATA-TODO 没有进入页面可见文字。这些检查确认文档结构、范围、算式和站点构建，不是 Unity 编译/运行、实际构建产物、设备性能、画面/听感或线上部署验收。本轮未做浏览器人工检查。

## 9. 2026 年 10 月 26 日 P1 规格与体验条件修订

按用户确认范围，只推进[资源质量规则][quality]和[体验一致性][experience]两篇中的固定参数、泛化结论及相关证据边界。保留原有章节与职责，不新增文章，不修改 P1“统计与验证桥”的变体 CI、动画算法与 Importer 专文，不提交或推送。

| 承接与风险 | 本地文字处理 | 尚未执行的检查 |
| --- | --- | --- |
| 质量规则：R6 | 撤下统一音效采样率、动画秒数、音乐编码/质量和音频文件上限；以用途、目标支持、交付与播放条件确定规则 | 代表素材、听感、首播/并发和实际产物，V-A/V-P；片段与玩法条件按 V-N |
| 质量规则：R12 | 撤下固定面数/LOD 级数与纹理规格表，改列预算依据和接受条件；配置数字仅作为教学候选，确认后才接入门禁 | 投影、几何/属性、LOD、画面与成本，V-T/V-M；检查器、回调、PR/CI 和豁免流程仍需真实执行 |
| 体验一致性：R13 | 分别比较采样率、层数、声道、编码、加载与空间处理；不再承诺收益极小、听感损失很大或某类玩家不敏感 | 匹配音量与输出、素材、混音和目标负载下的听感/运行记录，V-A/V-P |
| 两篇同源表述 | 纹理格式按能力/用途而非平台名判定；区分文件、包体与驻留。修正导入回调只能处理单资产的绝对说法，区分职责建议与 API 能力 | 格式/回退与引用/重复的实际产物；回调与规则实验，不能从文档推导项目门禁有效 |
| 体验文其他成本与接受条件 | 撤下 LUT/命中粒子近零或低成本、角色最低尺寸、固定远景动画率/骨骼减半与“次级对象任意降”；类型表改列保护目标和候选 | 目标设备的画面、动画/玩法及运行比较；不由类型、距离或对象身份预判结果 |
| 教学情境与检查方法 | 质量文开篇仅保留 RGBA32 基础层理论算例；ARPG 的稳帧率、包体与社区收益不再写成结果；截图检查仅作静态初筛 | 算例之外的占用/产物、目标路线、动态反馈与真实受试记录；本篇均未执行 |

本阶段将 R6/R12/R13 标为**文字已修、工程验证待执行**，初次盘点的 13 个风险编号至此都有文字处理记录。当时 P1“统计与验证桥”尚未推进，其后续记录见第 10 节；9 类验证需求不因文字修订而改变状态，新增篇目也仍只是候选。

文档核对依据包括 [Audio Clip][u-audio]、[LODGroup][u-lod]、[URP Asset][u-urp-asset]、[Rig Import Settings][u-rig]及 [AssetPostprocessor][u-postprocessor]/[批次回调][u-post-all]。这些资料用于解释配置与能力边界，不支持通用性能、群体偏好或项目收益。两篇中的配置和操作仍是未执行方案，DATA-TODO 保留。

本轮检查：两篇文章的 6 处 `relref`、两份盘点的 57 处本地链接、引用标签、表格列数、围栏、空白与字符检查通过；20 项覆盖、13 个历史风险和 9 类待执行验证编号完整。两篇的章节层级与顺序保留，指定旧结论已撤下；RGBA32 基础层理论像素载荷的算式核对通过，范围内 `git diff --check` 通过。

Hugo 完整本地构建退出码为 0、无 ERROR；两个生成页面的 2/8 张表、证据说明与 6 个文章链接目标检查通过，DATA-TODO 仅保留为注释。这些是文档与生成页面检查，没有执行浏览器人工检查、Unity/CI 实验、设备采样、画面/听感/玩法或受试测试，也没有线上部署验收。不得把本轮文字状态计作任何一类工程验证通过。

## 10. 2026 年 10 月 26 日 P1 统计与验证桥修订

按用户确认范围修订[变体 CI][variant-ci]、[动画算法][anim-algo]和[Importer][importer]三篇。只补统计、版本、消费者与验证边界，不新增文章或工程框架，不提交或推送。Importer 文末已有的“资源交付路径 · 2/7”导航改动保留，不归入本轮修订成果。

| 承接 | 本地文字变化 | 仍待执行的实际验证 |
| --- | --- | --- |
| 变体 CI | 区分理论组合、回调观察条目、构建保留与运行命中；程序阶段与观察点分别记录，明确 Player/Bundle、版本、范围、顺序、缓存和完整性；缺失不计零，基线归档不自动提升 | 实现采集器并验证 clean/增量/缓存、失败/并行与重复记录；明细/汇总、保留回执和目标动态路线，V-S/V-D |
| 动画算法 | 核对 Unity 6000.0 的录制过滤/模型导入与 Rig 条件；区分百分比、角度和教学绝对误差；局部/离散简化不保证连续、混合或最终姿态；第三方指标按库/引擎版本和度量归属 | Unity 真实片段的导入/压缩、完整 codec 往返、连续区间与最终动作；构建、驻留、采样和玩法记录，V-N |
| Importer | 修正概念链中交付与运行的顺序，区分导入对象/缓存、构建与运行；列属性、骨骼、曲线、事件的静态/动态消费者，不以一帧未用或单次扫描证明可删 | 项目特有消费者、必要路线与真实原始/候选资产；移除前后导入、产物及目标功能/占用，V-M/V-N；音频继续接 V-A/V-P |

同源示例修正：CI 对比骨架处理零基线、身份新增/缺失、下降和口径/完整性错误，阈值由项目传入；通知示例的明细与总量对齐。动画片段修正“仅首尾相等即判常量”、用 float 容器冒充量化码字及重复求范围的问题，保留码字范围/位数；明确 ushort 并未实现任意位宽打包，片段没有完整二进制 codec，也未证明 Unity 兼容性。

本轮取得的有限证据：抽取正文 Python 对比代码，以合成 JSON 检查 **13 组**情况并核对通知算式；抽取正文 C# 在独立 **.NET 10** 编译，检查 **8 组**合成情况，包含 **101 个标量量化往返点**。两类检查均通过，但没有 Unity、实际 Shader 构建、真实动作、设备或 CI 结果，不将其计作 V-S/V-D/V-M/V-N 完成；第 5 节 **9 类实际验证需求仍保持待执行**。

依据：[OnProcessShader][u-shader-callback]及其缓存说明、[ShaderSnippetData][u-snippet]、[ShaderCompilerData][u-compiler-data]；[CurveFilterOptions][u-curve-filter]、[ModelImporterAnimationCompression][u-model-animation-compression]与[Animation tab][u-animation]；[Asset Database][u-asset-database]、[刷新与依赖][u-asset-refresh]和既有 Model/Rig 文档。第三方数据明确属于 ACL 插件 README 所列 2.1.0/UE 5.2.0 对比及 [ozz 0.15.0][ozz-015] 的 keyframe 结构，不移植为 Unity 收益。

本轮文档与构建检查：三篇文章的 18 处 `relref`、两份盘点的 58 处本地链接、引用标签、表格列数、围栏与字符检查通过；空白检查保留原文 Markdown 双空格换行。20 项覆盖、13 个历史风险与 9 类实际验证编号完整；三篇原有章节标题和顺序保留，Importer 既有路径导航逐字保留，范围内 `git diff --check` 通过。

Hugo 完整本地构建退出码为 0、无 ERROR。三个生成页面、各一张表、证据说明、18 个文章链接目标及动画文三个 Mermaid 源块检查通过，DATA-TODO 未进入可见文字。未做浏览器人工/图示渲染检查、Unity 实验、实际构建/CI、设备或线上验收；上述合成代码检查与文档检查各自限定范围，不替代第 5 节实际验证。

## 11. 本次授权的源码交付范围

用户确认提交并推送本会话的资产成本工作：七篇初始机制修订、五篇 P0 和两组 P1 共五篇，即 **17 篇旧文**；此前已完成的 **2 篇正文**；本盘点、原盘点与 **2 份提纲**，合计 **23 个文件**。这些是本次源码范围，不新增选题或正文。

目标为 `origin/main`，采用明确文件与差异范围提交。Importer 文末其他任务已有的资源交付路径导航不纳入；工作区其余栏目、入口和规划改动保留。前面各节“未提交/推送”的文字是当时阶段记录，不作为当前交付状态；实际提交与远端状态以 Git 记录和本次回读为准。

复用本轮已通过的 Hugo、静态、生成页面和有限合成示例检查；不将源码推送视为线上部署、Unity/CI 或设备验收。第 5 节九类实际验证需求继续待执行。

## 12. V-T / V-C 最小对照准备

用户选择先推进纹理与通道的一组小对照，再根据材料判断后续补强。已形成[最小验证工作单](mobile-texture-channel-validation-workorder-2026-10-26.md)，分尺寸、目标格式、Alpha 消费者三个验证块；每次只改一项，首批先做相邻尺寸、RGBA32/ASTC 6×6 和两组固定材质状态，不立即铺开所有参数组合。

已只读核对本机 Unity 2022.3.62f3 / URP 14.0.12 环境与相关公开 Package 逻辑；ADB 识别唯一授权的 Samsung SM-S916U（Android 13 / API 33），读取物理尺寸，尚未安装或运行本实验 APK。实际 Player 的 GPU、API、格式支持和渲染尺寸仍待确认，不把机型或物理屏幕信息当作这些能力的结果，也不将 2022.3 实验映射为 Unity 6 实测。

本仓新建[原创源素材目录](../examples/mobile-texture-validation/README.md)：三个数值图案 PNG、一块 UV 测试板 OBJ、生成器与源清单。没有复制私有实验仓的场景/素材；现有工程仅作只读环境参照。两张颜色图解码 RGB 相同而 Alpha 不同，数据图另列候选通道语义；这些仍是源输入，不是已导入或压缩后的资产。

本轮只推进作者执行材料，不新增正文、不回填运行结果，不提交或推送。V-T/V-C 为**准备完成、实际对照待执行**，其余七类实际验证保持待执行；Alpha 最小反例也不代表所有通道与动态功能已覆盖。

准备阶段检查：三个 PNG 的 CRC、头部、独立解码、尺寸/通道摘要和文件 SHA256 通过；两颜色图 RGB 逐字节一致，Alpha 不同，全不透明候选 A 为 255。OBJ 的源位置/三角形计数和摘要通过；重复生成的五个源输出逐字节一致，未被 Git 忽略。目视检查仅覆盖一张源图的图案，不是设备画面。

工作单、两个盘点与素材 README 的 73 处本地链接、引用标签、表格/围栏/空白/字符检查及范围内 `git diff --check` 通过；Hugo 本地构建退出码为 0、无 ERROR。没有执行 Unity 导入、场景/材质搭建、APK 构建/安装/运行或实际 V-T/V-C 对照，未将这些文件与文档检查登记为工程通过。

[overview]: ../content/performance/mobile-art-asset-cost-overview.md
[geometry]: ../content/performance/mobile-geometry-cost-vertices-screen-triangles-lod.md
[storage]: ../content/rendering/unity-rendering-supp-a-mesh-texture-storage.md
[bandwidth]: ../content/performance/gpu-opt-02-bandwidth.md
[shader-opt]: ../content/performance/gpu-opt-03-shader.md
[asset-spec]: ../content/performance/device-tier-asset-spec-texture-and-package.md
[importer]: ../content/engine-toolchain/unity-importer-what-does-it-do-and-why-source-file-is-not-just-a-file.md
[asset-input]: ../content/engine-toolchain/unity-scriptableobject-material-animationclip-why-they-feel-like-different-assets.md
[memory]: ../content/performance/cpu-opt-05-memory-budget.md
[package]: ../content/delivery-engineering/delivery-package-distribution-02-size-control.md
[trimming]: ../content/code-quality/package-size-optimization-stripping-split-packages-and-asset-trimming.md
[multiplatform]: ../content/delivery-engineering/delivery-content-pipeline-06-multiplatform-assets.md
[quality]: ../content/code-quality/game-quality-04-asset-quality.md
[experience]: ../content/performance/device-tier-experience-consistency.md
[anim-algo]: ../content/system-design/algorithms/algo-11-animation-compression.md
[rig]: ../content/delivery-engineering/game-art-production-04-rigging-skinning.md
[animation]: ../content/delivery-engineering/game-art-production-05-animation.md
[acceptance]: ../content/delivery-engineering/game-art-production-12-joint-acceptance.md
[variant-ci]: ../content/rendering/unity-shader-variant-ci-monitoring.md
[variant-contract]: ../content/rendering/unity-shader-variant-collection-coverage-and-keyword-contract.md
[keywords]: ../content/rendering/unity-shader-keyword-design-multi-compile-vs-shader-feature.md
[stripping]: ../content/rendering/unity-shader-variant-stripping-stages-prefiltering-builtin-custom.md
[artist-intuition]: ../content/performance/game-performance-art-hidden-cost-intuition.md
[u-audio]: https://docs.unity3d.com/6000.0/Documentation/Manual/class-AudioClip.html
[u-model]: https://docs.unity3d.com/6000.0/Documentation/Manual/FBXImporter-Model.html
[u-animation]: https://docs.unity3d.com/6000.0/Documentation/Manual/class-AnimationClip.html
[u-readable]: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/TextureImporter-isReadable.html
[u-webgl-astc]: https://registry.khronos.org/webgl/extensions/WEBGL_compressed_texture_astc/
[u-lod]: https://docs.unity3d.com/6000.0/Documentation/Manual/class-LODGroup.html
[u-svc]: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ShaderVariantCollection.html
[u-stripping]: https://docs.unity3d.com/6000.0/Documentation/Manual/shader-variant-stripping.html
[u-shader-callback]: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Build.IPreprocessShaders.OnProcessShader.html
[u-load-audio]: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AudioClip.LoadAudioData.html
[u-stream-audio]: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Networking.DownloadHandlerAudioClip-streamAudio.html
[u-urp-asset]: https://docs.unity3d.com/6000.0/Documentation/Manual/urp/universalrp-asset.html
[u-rig]: https://docs.unity3d.com/6000.0/Documentation/Manual/FBXImporter-Rig.html
[u-postprocessor]: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AssetPostprocessor.html
[u-post-all]: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AssetPostprocessor.OnPostprocessAllAssets.html
[u-snippet]: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.ShaderSnippetData.html
[u-compiler-data]: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.ShaderCompilerData.html
[u-curve-filter]: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Animations.CurveFilterOptions.html
[u-model-animation-compression]: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ModelImporterAnimationCompression.html
[u-asset-database]: https://docs.unity3d.com/6000.0/Documentation/Manual/AssetDatabase.html
[u-asset-refresh]: https://docs.unity3d.com/6000.0/Documentation/Manual/AssetDatabaseRefreshing.html
[ozz-015]: https://github.com/guillaumeblanc/ozz-animation/releases/tag/0.15.0
