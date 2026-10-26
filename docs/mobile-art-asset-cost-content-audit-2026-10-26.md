# 移动端美术资产成本的内容盘点与修订建议

> 日期：2026-10-26。目标读者：客户端程序，重点是理解美术资产怎样形成运行时成本。
> 用户已选方案：补强旧文，再建立专题阅读线。本文是知识点对应表与执行记录，不是新的 canonical series plan。2026-10-26 已推进七篇旧文的事实与证据修订、两篇详细提纲及 N1/N2 正文本地成稿；工程与设备实验待执行。

建议新增 **2 篇正文**，修订 **7 篇旧文**，在 **2 处现有入口**组织阅读顺序。原先讨论的六个问题域继续保留，其中纹理、图集与打包、透明效果、光照由已有文章承担。美术制作系列保留制作职责，以例子和延伸链接连接本专题。

读者需要能沿着这条关系解释问题：

**制作选择 → 导入后的数据与配置 → 实际参与的绘制和采样 → CPU、GPU、内存等成本 → 目标条件下的观察与取舍。**

这条关系用于分析因果，不表示运行时严格串行，也不把各项成本直接相加。最终希望程序能回答：一个资产为何有这些数据、使用时付出什么成本、有哪些替代选择、需要什么证据才能决定。

## 1. 来源与阅读范围

| 标识 | 第一手材料与版本 | 本次使用范围 |
| --- | --- | --- |
| S0 | [Samsung 导读，2021-05-19][samsung] | 确认四组主题与 Arm 来源归属；导读不替代详细指南 |
| G | Arm 与 Unity 合作课程：[Triangle and Polygon Usage][geometry]、[Level of Detail][lod]、[Additional Geometry Best Practices][geometry-extra]；页面标注 Unity 2019.4 | 已读对应几何课程正文。Arm 几何指南旧链接迁移后未完整取得，不能标成已逐页读完几何指南 |
| T | [Arm Texturing，Version 1.0，2020-03-31][texturing] | 图集、过滤、Mip、尺寸与色彩空间、压缩、UV、视觉贡献、通道打包、Alpha、法线与烘焙 |
| S | [Arm Materials and Shaders，Version 1.0，2020-03-31][materials] | 材质与着色、移动端 Shader、纹理需求、Lit/Unlit、透明实现、复杂度与 profiling |
| L | [Arm Lighting，Issue 01，2020-05-05][lighting] | 光照模式、烘焙与光照贴图、替代光影、探针、Renderer 设置与实时光 |
| A | [Arm 当前 Unity 资源入口][arm-unity] | 用于追踪新版。当前入口链接的纹理、材质、几何指南有 2.0 路径；本次未逐页复核新版，不以旧版代称最新版 |
| U | Unity 6（6000.0）官方文档，见第 4 节逐项链接 | 核对本次指出的 ASTC、批处理、光照模式和 URP Screen Space Shadows 行为；不是引擎运行结果 |

表中“原文知识点”只概括对应材料；缺口、编排和实验是本站的编辑判断。旧版中的固定数值、工具菜单和硬件建议，开写时仍须按选定版本核对。

## 2. 已有内容及其职责

| 已有材料 | 当前可承接的内容 | 本专题的处理 |
| --- | --- | --- |
| [几何与表面][mesh-material] | 顶点、索引、Sub-mesh、材质参数、纹理语义与绘制路径 | 作为公共前置复用；新增篇不重讲完整数据流 |
| [Mesh 与 Texture 存储基础][storage] | 顶点布局、纹理格式、Mip 与占用表 | 核心修订，负责数据存储和采样基础 |
| [Draw Call 与 Overdraw][draw-overdraw] | 批处理、透明排序、覆盖与观察工具 | 核心修订，接住图集与透明的运行影响 |
| [带宽优化][bandwidth] | 压缩格式、RT、Resolve 与分析入口 | 核心修订，负责数据搬运；本专题重点在纹理，其他已有机制疑点一并列入复核 |
| [Shader 优化][shader] | 精度、分支、采样与通道打包 | 核心修订，接住着色工作和打包条件 |
| [移动端阴影][shadow] | Shadow Map、级联、距离、滤波与替代方案 | 核心修订，先处理机制与“实测”证据问题 |
| [光照资产][light-assets] | 实时光、Lightmap、Light Probe、Reflection Probe | 核心修订，明确数据内容和运行时成本 |
| [每档资产规格清单][asset-spec] | 分辨率、压缩、LOD、密度与包体分层 | 核心修订，把示例配置与适用条件写清 |
| [GPU 预算][gpu-budget]、[材质与 Shader 治理][shader-governance] | 项目预算、模板、关键词、变体与质量规则 | 作为预算与工程延伸复用；本专题不建立第二套治理体系 |
| [法线贴图][normal-map] | 切线空间、TBN 与解码 | 按需选读；成本比较不扩成完整法线制作课程 |
| [资源规范与准入][asset-standards] | 从预算到规则、导入与检查 | 按需复用当前工作区内容；文件已有其他修改，不据此判断已发布状态 |
| 美术制作 [03 角色][art03]、[06 场景资产][art06]、[07 地编][art07]、[08 特效][art08] | 法杖、庭院构件、镜头路线和圆环例子 | 提供制作背景；只在专题正文具备后补必要延伸链接 |
| [12 联合验收][acceptance] | 固定候选与观察条件、性能证据和变更回归 | 复用记录方法；该文明确实际工程和设备采样待补 |

“有正文”表示本地文件已有相应内容，不代表机制正确、已经部署或经过设备验证。以下缺口判断基于上述文章正文与相关段落盘点，不声称全站所有文章均已审查。

## 3. 原文知识点与站内承接对应表

验证编号对应第 5 节；表中验证均为待执行设计。“新增”指第 6 节的两篇候选正文；“修订”指调整原有归属文章，不另开同题新文。

### 3.1 几何

| 编号与原文定位 | 原文知识点 | 已有承接 | 程序读者仍缺什么 | 建议与验证需求 |
| --- | --- | --- | --- | --- |
| G1，G Triangle：Details in Areas That Matter | 轮廓与可见细节 | [几何与表面][mesh-material]、[角色制作][art03]已有数据与形体基础 | 缺少从最终镜头解释“保留哪些几何”的连续分析；面数与视觉贡献不是同一指标 | **新增几何篇**；V2 比较几何密度和实际画面贡献 |
| G2，G Triangle：Avoiding Micro Triangles | 微小与狭长三角形 | [GPU 预算][gpu-budget]已有几何成本分类 | 缺少投影后尺度、几何处理与片元效率的联系；原文像素范围只能作经验提示 | **新增几何篇**；V2 固定分辨率、相机和 Shader 后抓取证据 |
| G3，G LOD：When Not to Use LODs 等 | LOD 的收益与维护成本 | [场景资产][art06]已有“不为简单方盒强加 LOD”的边界；[分档规格][asset-spec]有 LOD Bias | 缺少简化收益、切换观感、额外网格和制作成本的一次完整比较 | **新增几何篇**，修订分档文引用；V3 |
| G4，G Additional：Smoothing Groups / Model Settings | 平滑、法线与导入设置 | [几何与表面][mesh-material]有顶点属性；[存储基础][storage]有布局；[角色制作][art03]有软硬边和 UV | 缺少 DCC 统计与导入顶点数、属性、不连续边界之间的桥接；顶点拆分属于本站延伸，不能写成原文已证明 | **新增几何篇**，修订存储说明；V1，核对导入器设置 |
| G5，G Additional：Combining Meshes | 合并网格的取舍 | [Draw Call][draw-overdraw]有合批策略 | 现文混淆静态批处理与手动合并的剔除粒度；共享材质也不保证最终只有一次绘制 | **修订 Draw Call 文**；V6；几何新篇只给决策入口 |

### 3.2 纹理

| 编号与原文定位 | 原文知识点 | 已有承接 | 程序读者仍缺什么 | 建议与验证需求 |
| --- | --- | --- | --- | --- |
| T1，T §2 | 图集 | [Draw Call][draw-overdraw]有批处理，[场景资产][art06]有材质复用 | 缺少图集与批处理条件、Mip 边距和资源共用范围的关系；减少图片数量不能直接推导占用下降 | **修订 Draw Call 文**，存储文解释边距；V6，加载范围是本站延伸 |
| T2，T §3—4 | 过滤与 Mip | [存储基础][storage]已有 Mip 原理、AF 和 streaming | 缺少离散过滤方式与倾斜采样对照；“AF 倍数等于固定采样数”“Mip Bias 必然省内存”等表述待核对 | **修订存储文**；V4；区分选择低 Mip 与实际不驻留高 Mip |
| T3，T §5.2 | 颜色与数据的色彩空间 | [几何与表面][mesh-material]已有贴图语义，[角色制作][art03]有导入步骤 | 缺少同一数据因导入解释不同而改变着色输入的对照；参数通道约定也须与 Shader 一致 | **修订 Shader 文**，存储文交叉引用；V5、V7 |
| T4，T §5.1、5.3—5.4 | 尺寸与 GPU 压缩 | [存储基础][storage]、[带宽][bandwidth]、[分档规格][asset-spec]均有覆盖 | ASTC 表存在计算错误；源文件、传输压缩、GPU 格式混用风险；API 名称不能替代格式能力检测 | **修订三篇旧文**；V5 先算理论字节，再查实际格式与占用 |
| T5，T §6—7 | UV 与视觉贡献 | [角色制作][art03]、[场景资产][art06]有 UV 操作 | 程序需要理解 UV 面积分配、拉伸和目标视角的关系；不需要在这里重写 UV 操作教程 | **修订存储文**补一组示意，复用制作文；V4，V1 观察边界 |
| T6，T §10—11 | 法线与烘焙 | [法线贴图][normal-map]有 TBN，[角色制作][art03]介绍高低模与可选工具 | 缺少几何与法线方案的成本比较；cage、分组烘焙、切线/硬边不一致导致的异常需要区分 | **新增几何篇**承接成本，制作细节链接选读；V1、V2、V7；复杂烘焙实操暂后置 |
| T7，T §8 | 通道打包 | [Shader 优化][shader]已有 Mask 打包代码 | 缺少尺寸、UV、采样频率、色彩空间、共同使用和压缩误差的兼容条件 | **修订 Shader 文**；V7；记录合并后是否真的减少采样和驻留字节 |
| T8，T §9 | Alpha 存储 | [带宽文][bandwidth]有 Alpha 拆图建议 | 缺少固定块格式下的比较口径；“增加 Alpha 必然翻倍”和“拆出一张图必省”都不能泛化 | **修订带宽文**，存储文补格式对照；V5、V7；存储 Alpha 与启用混合分开 |

### 3.3 材质与 Shader

| 编号与原文定位 | 原文知识点 | 已有承接 | 程序读者仍缺什么 | 建议与验证需求 |
| --- | --- | --- | --- | --- |
| S1，S §2—5 | 材质、Lit/Unlit 与所需纹理 | [几何与表面][mesh-material]、[Shader 优化][shader]、[TA 制作][art09] | 缺少“想表达哪些光照变化”到“实际执行哪些功能”的桥；旧 Mobile Shader 类目不能套到 URP | **新增成本总览**解释选择，**修订 Shader 文**承接执行；V9 |
| S2，S §6—7 | Alpha Test 与 Alpha Blend | [Draw Call 与 Overdraw][draw-overdraw]、[特效制作][art08] | 已有透明篇无需再新写；应区分排序、深度、屏幕覆盖和硬件差异，消除两种实现的固定优劣排序 | **修订 Draw Call 文**；V8；Scene View 可视化不能直接当硬件计数 |
| S3，S §8.1—8.4 | 简化着色与阶段分工 | [Shader 优化][shader]已有 half、分支和低频信息移到顶点 | 缺少插值误差、新增 varying、顶点密度与编译产物的对照；“half 固定快几倍”“lerp 必更快”等应限定 | **修订 Shader 文**；V9；原文数学函数列表作检查线索，不作禁用表 |
| S4，S §8.5 | profiling 与前后比较 | [GPU 预算][gpu-budget]、[联合验收][acceptance]已有方法 | 需要把资产版本、编译变体、屏幕覆盖与工具证据关联；静态编译报告不代表整帧性能 | **新增成本总览**给记录骨架，工具细节复用；V1—V10 共用记录字段 |

### 3.4 光照

| 编号与原文定位 | 原文知识点 | 已有承接 | 程序读者仍缺什么 | 建议与验证需求 |
| --- | --- | --- | --- | --- |
| L1，L §2—4 | Baked / Mixed / Realtime | [光照资产][light-assets]已有四条路径 | Lightmap 被过度概括成间接光；需要按 Light Mode、Lighting Mode 与管线解释内容，保留采样成本 | **修订光照资产文**；V10；当前官方资料已支持事实纠正 |
| L2，L §5 | Lightmap 与 texel 分配 | [场景资产][art06]有光照 UV，[光照资产][light-assets]有数据结构 | 缺少对象尺度、UV 打包、光照贴图占用与视觉需求的联系；原文分辨率起始值不转成统一预算 | **修订光照资产文**；V10；记录每次烘焙的资源清单与配置 |
| L3，L §7 | 探针服务动态对象 | [光照资产][light-assets]已有 SH、插值，[地编][art07]有环境例子 | 补动态对象进入烘焙场景后的照明连续性；明确探针不能自动提供全部动态遮挡 | **修订光照资产文**；V10；传统探针与 APV 按所选版本分开说明 |
| L4，L §6 | 替代光照与阴影 | [移动端阴影][shadow]、[特效制作][art08]已有方案 | 现文把 URP Screen Space Shadows 当作低成本接触阴影替代；Blob 的覆盖、地形适应和排序成本也需保留 | **修订阴影文**；V8、V10；不同表现目标不强行当作等画质比较 |
| L5，L §8—9 | Renderer 开关与实时光 | [阴影][shadow]、[光照资产][light-assets]已有参数和成本 | 缺少“对象还参与哪些 pass”的观察；投射者、覆盖和光源重叠比单一数量更能解释使用条件 | **修订阴影文**，总览给定位问题；V10；数量与时间不直接线性等同 |

## 4. 初次盘点的事实与证据问题

本节保留修订前的静态审查快照，旧句和表中状态不代表修订后的正文。2026-10-26 的实际处理见第 8 节；公式、官方行为与设备实验分别记录，不以文档修订代替运行验证。

| 项目 | 本地位置与问题 | 依据与修订要求 | 状态 |
| --- | --- | --- | --- |
| ASTC 计算 | [存储文][storage]“ASTC 块大小选择”把 8 bpp 写成相对 RGBA 的 8:1；[带宽文][bandwidth]也有比例错误及“8×8 为 1 bit/pixel” | [4×4 官方定义][astc4]与[8×8 官方定义][astc8]均为每块 128 bit。由此推算：4×4 = 8 bpp、相对 RGBA32 为 4:1；8×8 = 2 bpp、相对 RGBA32 为 16:1。统一未压缩基准、bpp、比例方向和 MiB/MB | 已确认算术错误；待修订 |
| 静态批处理与手动合并 | [Draw Call 文][draw-overdraw]“Static Batching”写为整体剔除、一次 Draw 绘制整个合并 Mesh | [Unity 批处理说明][batching]区分内置批处理与手动合并，手动合并会失去原对象独立剔除。修订时补官方版本依据及 V6，不再把二者合并描述 | 已确认混淆；待修订 |
| 光照数据 | [光照资产文][light-assets]将 Lightmap 定义成间接光，并称零实时计算开销 | [Unity Light Modes][light-modes]说明 Baked 可将直接和间接光烘焙到 lightmaps；使用结果仍有成本。按 Baked/Mixed 与具体模式改写，补数据内存与采样 | 已确认过度概括；待修订 |
| Screen Space Shadows | [阴影文][shadow]“方案二”称其为从深度近似接触阴影、比 Shadow Map 便宜的替代 | [Unity 6 URP 文档][screen-shadows]仍列出 MainLightShadow 和 ScreenSpaceShadows，并说明额外深度预通道与纹理。应改为既有阴影结果的屏幕空间使用路径，不能归为免 Shadow Map 的接触阴影替代 | 已确认机制错误；待修订 |
| 阴影“实测”来源 | [阴影文][shadow]开头、距离示例及“实测数据参考”含具体设备、耗时和选型结论，段落未给测试工程或记录 | 本次未取得这些数字的原始证据。后续先追溯；无法追溯则移除“实测”及具体结果，改成待执行比较，不能由 Arm 示例代为背书 | 来源未核实；待追溯或移除 |
| 几何与纹理存储边界 | [存储文][storage]的 Mesh Compression、索引升级、AF 倍数、Mip Bias/streaming 结论，以及[带宽文][bandwidth]的“每次从系统内存读”“压缩比等于实际带宽降幅” | 区分文件体积、运行数据、缓存与计数器；导入器行为查对应版本；V1、V4、V5 验证。RT/Resolve 的普遍化结论也需在该文修订时核对实际渲染路径，不由纹理实验代证 | 待按版本逐项核对 |
| 透明与 Shader 硬件描述 | [Draw Call 文][draw-overdraw]称 clip 令 HSR 完全失效、透明无法写深度；[Shader 文][shader]将 Quad 与执行组混用、给出固定 half 提速，并称时间乘 10 几秒即超出 65504 | 先区分可配置行为、常用路径与架构条件；例如若时间以秒计，65504/10 约 6550 秒，精度损失与溢出另行说明。硬件机制须核对原始资料，V8/V9 仅证明选定配置 | 时间示例算术已确认；其余待核对 |
| 分档规格的证据强度 | [资产规格文][asset-spec]存在“30m 看不出区别”“UI 直接 PNG/无损”“运行时代价接近零”等概括；规格表的来源要求藏在 DATA-TODO 中 | 改为有适用条件的教学配置，明确 GPU 格式与原稿格式；删除无法追溯的项目来源暗示。按视觉贡献、支持能力和测量选档，不发布统一最优数字 | 已确认需补条件与来源；效果待验证 |

Arm 旧指南中的“绿色通道通常更精细”“增加 Alpha 改变位深”“尽量把计算移到顶点”等也必须保留格式和使用条件。本专题采用它们提出比较问题，不把这些建议扩成跨格式、跨 GPU 的保证。

## 5. 验证需求

### 5.1 共用条件与产物

后续示例优先衔接已有法杖、庭院构件和圆环，但这些教学素材与工程目前待制作。没有实际资产时可用清楚标注的最小替代物；简单方盒不能证明复杂角色、植被或蒙皮负载的结论。涉及自定义 Shader 或探针类型时固定实现与版本。

建议以 Unity 6000.0 / URP 17 作为首轮对照环境，与美术制作系列保持衔接；这是执行建议，不是已配置好的工程。具体补丁、包版本、渲染路径、API 和设备型号须在实验开始前记录。

每项至少保留：资产与配置版本、改动变量、相机与实际渲染分辨率、对象规模和屏幕覆盖、Shader 变体、光照与阴影、设备/GPU/系统/API、采样方式、截图或 capture、实际结果、结论范围。性能比较还要记录预热、帧率限制、动态分辨率和热状态；每次只改变一类因素，多次采样并保留波动。单帧 capture 用于解释工作，不直接当稳定耗时基准。

### 5.2 待执行项目

| 编号 | 要回答的问题 | 最小对照与观察项 | 能证明什么及限制 |
| --- | --- | --- | --- |
| V1 导入数据 | 同一形体为什么导入后数据不同？ | 制作软硬边/UV 接缝不同的副本；固定三角化与导入设置；记录源统计、Mesh.vertexCount、属性格式、stride、索引格式、运行时副本；检查法线/切线 | 证明选定导入路径的数据差别；不由顶点数直接推断帧率，不预填立方体一定导入为某个数量 |
| V2 几何与画面 | 细节是否在实际镜头有贡献，是否造成额外工作？ | 固定相机/覆盖/Shader，比不同密度模型；另做独立距离扫描，记录投影轮廓、闪烁、三角形和可用 GPU 计数。法线方案另记录采样与视觉差异 | 区分质量比较与成本比较；距离扫描会同时改变覆盖，不当成纯几何 A/B；计数器以硬件定义为准 |
| V3 LOD | 简化收益能否覆盖额外资产成本？ | 无 LOD 与少量明显简化的 LOD；固定镜头路线、切换设置与阴影，记录切换观感、各档数据、资源体积、驻留与 GPU 负载 | 证明该对象/路线的取舍；降低档位不保证当前瓶颈改善，不套用固定 50% 或固定层数 |
| V4 纹理采样 | Mip 与过滤怎样改变清晰度和访问？ | 同一纹理/尺寸/格式与倾斜地面；分别切换 Mip、过滤、AF；观察远景闪烁、斜视细节、实际占用与可用采样计数；streaming 单独测试 | 区分画质、采样选择和驻留；保留完整 Mip 的存储成本，不能由模糊直接推断省内存 |
| V5 格式与占用 | 文件大小与运行时字节差在哪？ | 固定尺寸与内容，比较 GPU 格式、ASTC 块、Alpha；逐级 Mip 按块向上取整核算，再检查实际格式/副本/回退和占用。另以色彩空间副本观察数据解释 | 理论核算可先独立完成；运行占用、质量和速度要另测。磁盘 PNG 大小、包压缩率不能代替 GPU 占用或带宽 |
| V6 图集与合并 | 共享纹理/网格何时帮助提交，付出什么代价？ | 固定 texel 密度、画面与对象数量；分别比较独立纹理/图集、手动合并/静态批处理；检查材质/变体、CPU 提交、Draw、边缘/Mip 串色、可见范围、资源共用和卸载引用 | 每种机制单独切换；Frame Debugger 说明绘制组织，CPU profile 才能支持提交成本；占用变化须计入 padding 和共用范围 |
| V7 通道与法线 | 合并 Mask 或增加法线，节省了什么？ | 固定数据含义；分图/打包图使用相同 UV、尺寸、过滤和兼容色彩空间，检查 Shader 实际采样、误差、驻留与耗时；另测试某通道不用的情况 | 节省取决于共同使用、压缩和 Shader；不把“图少”直接写成更省；不以一次读取 RGBA 等同固定带宽 |
| V8 透明覆盖 | 是粒子数、覆盖还是混合方式在主导？ | 固定粒子数改大小；再固定覆盖改叠层；另比较可接受画质的 Opaque/Cutout/Blend 方案。记录深度状态、排序、pass、GPU 时间与可用计数 | 不把不同视觉目标强行当等价方案；保留透明混合的必要工作；Scene View 只作定位，硬件计数按平台解释 |
| V9 着色工作 | 精度、功能和阶段调整是否有收益？ | 同一工作集分别比较功能开关、float/half、顶点/片元计算；固定变体和渲染路径；检查输出误差、编译报告、插值与真机耗时 | 静态编译报告用于解释候选瓶颈；真机结论只覆盖目标架构与实际负载；画质不可接受的版本不列为可采用方案 |
| V10 光照与阴影 | 各方案到底保留哪些数据和 pass？ | 同一庭院分别使用实时、Baked 与明确 Lighting Mode 的 Mixed；动态占位体走固定路线；分别改变 lightmap 密度、投射者、级联、Blob 与 Screen Space Shadows；记录数据清单、内存、pass 与画面 | 先确认功能和 pass，再独立测量。探针与实时阴影职责分开；烘焙成本与运行成本分开；不预设固定毫秒或线性倍率 |

没有 GPU timing 或硬件计数支持的设备，可以完成数据、画面和绘制结构观察；对应性能栏保留待测，不能换成 Editor FPS 来宣布真机优化成功。跨 Mali、Adreno、Apple 的共同结论需分别取证，首轮单设备结果不能直接推广。

## 6. 哪些新增 哪些修订

### 6.1 新增两篇正文

| 候选 | 主问题与结构 | 与旧文分工 | 写作入口条件 |
| --- | --- | --- | --- |
| [N1 程序怎样理解美术资产成本](../content/performance/mobile-art-asset-cost-overview.md) | 从“拿到资产后该查什么”立问题；用制作/导入/使用/测量四层模型，沿法杖、庭院、圆环定位几何、采样、覆盖、额外 pass 和驻留；给出比较记录骨架 | 原理篇。预算制定链接 GPU 预算，数据结构链接几何与表面，制作操作链接美术系列；不重写工具教程或完整美术流程 | 正文已本地成稿，未提交/发布；[提纲](mobile-art-asset-cost-overview-outline.md)保留作者执行信息。教学情境不冒充工程观察，检查待执行 |
| [N2 模型面数之外的成本：顶点、屏幕三角形与 LOD](../content/performance/mobile-geometry-cost-vertices-screen-triangles-lod.md) | 从“知道面数后还该查什么”立问题；分清源网格/导入属性、投影几何/屏幕贡献、多次 pass 的使用；比较几何、法线与 LOD 的收益和限制 | 原理篇配 Unity 映射。顶点属性字节细节归存储文；完整剔除/实例化、法线烘焙工具课程分别选读 | 正文已本地成稿，未提交/发布；[提纲](mobile-geometry-cost-vertices-screen-triangles-lod-outline.md)保留作者执行信息。V1—V3 的材料与运行、设备验证待执行 |

两篇正文均已落在 `content/performance/`，沿用“移动端硬件与优化”归属；实际文件名、slug、weight 与成稿记录见第 10、11 节。本地成稿不代表已提交、线上发布或实验完成。

### 6.2 七篇核心修订

| 文章 | 修订重点 | 保留职责 |
| --- | --- | --- |
| [Mesh 与 Texture 存储基础][storage] | 公式与单位；导入数据/运行数据；UV 边界；过滤、Mip 与驻留条件 | 存储与采样基础 |
| [Draw Call 与 Overdraw][draw-overdraw] | 批处理机制纠错；图集条件；透明覆盖/深度/排序与架构限制；观察证据边界 | 提交与覆盖成本 |
| [带宽优化][bandwidth] | GPU 格式能力与 ASTC；Alpha；缓存/存储字节/实际流量区分；RT/Resolve 的版本与路径复核 | 带宽分析 |
| [Shader 优化][shader] | 纹理语义与 sRGB；通道打包条件；half、执行组、分支与阶段迁移的证据范围 | 着色工作 |
| [光照资产][light-assets] | 按模式解释烘焙内容；lightmap 密度与资源；探针与动态对象的适用条件；采样成本 | 光照数据与使用路径 |
| [移动端阴影][shadow] | 追溯或移除无依据实测；修正 Screen Space Shadows；限定级联/投射/替代方案的比较 | 阴影工作与取舍 |
| [每档资产规格清单][asset-spec] | 固定配置改为有条件的示例；源格式/GPU 格式分清；与预算和验证记录相连 | 质量分档与资产规格 |

用户已确认七篇范围；事实与证据修订已执行，见第 8 节。后续解释补充和实验仍按各篇边界推进，并保留其他任务的已有改动。

### 6.3 复用与导航

修订 [移动端系列入口][mobile-index]和[客户端移动平台入口][mobile-entry]，各加入“程序理解美术资产成本”的路径或链接；本次建议不再创建一个重复的索引页。

建议阅读顺序：

1. N1 成本总览；不熟悉数据对象时按需读[几何与表面][mesh-material]。
2. N2 几何成本与 LOD，按需进入[存储基础][storage]。
3. [带宽][bandwidth]与[Shader][shader]，分别理解 GPU 格式和纹理使用。
4. [Draw Call 与 Overdraw][draw-overdraw]，理解图集、提交和透明覆盖。
5. [光照资产][light-assets]与[移动端阴影][shadow]。
6. [GPU 预算][gpu-budget]与[资产规格][asset-spec]，将判断反馈到项目约束；[联合验收][acceptance]按需查记录方法。

美术制作 03、06、07、08、12 仅在对应正文可读后补延伸链接，列为后续可选导航工作，不计入七篇核心修订。不新增图集专文、透明专文或完整光照系列；若后续实验显示某个问题必须独立讲述，再凭材料重新判断。

## 7. 建议执行顺序与维护边界

| 顺序 | 工作 | 完成依据 |
| --- | --- | --- |
| 1 | 对第 4 节的公式、批处理、Light Mode、Screen Space Shadows 与来源问题先纠错 | 每项有计算或选定版本的第一手资料；未经追溯的数字不继续标实测 |
| 2 | 冻结 N1/N2 范围与 V1—V10 所需材料；先做公式和导入检查，再选择值得测量的方案 | 资产/设置/变量/观察项能对应；没有设备结果时不填写性能收益 |
| 3 | 写 N1，再写 N2；同步修订相关旧文的解释和例子 | 各篇主问题、因果和证据边界完整；同一机制保持唯一主要解释位置 |
| 4 | 完成剩余旧文修订，再接两个入口的阅读路线 | 实际页面可读、链接和导航可达、Hugo 无阻断错误 |

本盘点放在 `docs/` 执行层。父主题“移动端硬件与 GPU/CPU 优化”在根路由中仍指向历史归档，尚未独立拆出 canonical plan；这轮不修改只读归档，也不在根 `doc-plan.md` 塞入篇级清单。若进入持续写作阶段，先明确父系列 canonical 的拆分范围，再登记新增篇与修订状态；不能让本盘点成为第二套 canonical。

首轮盘点校验记录：33 个引用定义、20 个本地文件链接、表格列数、22 条知识点编号和 10 项验证设计检查通过；当时本地 `hugo` 退出码为 0。本轮正文修订另按第 8 节记录，历史构建结果不替代新改动的验证。

## 8. 2026 年 10 月 26 日旧文修订记录

本轮只推进已确认的七篇旧文事实与证据修订，保留主线和文章归属，未创建新增正文或调整网站入口。各文补充 `lastmod` 与必要的版本/证据说明；下列“已修订”指本地文本完成，不代表已提交或线上部署。

| 文章 | 已修订内容 | 证据及剩余范围 |
| --- | --- | --- |
| [存储基础][storage] | ASTC 位率/压缩比、完整 Mip 的块数取整、MiB 单位；Mesh/Vertex Compression、索引 Auto 与脚本 Mesh 区分；AF 与采样 Bias/驻留区分 | 官方格式定义与压缩文档、独立公式核算；V1/V4/V5 待执行 |
| [带宽][bandwidth] | 压缩表和 Alpha；支持能力与交付回退；存储字节/缓存/外部流量区分；RT 表改理论有效载荷；移除 Resolve 零成本、固定功能必触发 Resolve 的结论 | 格式资料及 Apple MSAA 路径说明；实际附件、pass、流量和时间待设备验证 |
| [Draw Call 与 Overdraw][draw-overdraw] | 静态批处理独立剔除、动态批处理属性限制；传统实例化与 SRP Batcher 路径；移除 HSR 全失效、透明无法写深度、固定 Overdraw 阈值及通用精确计数入口 | Unity 批处理与 StaticBatchingUtility 文档；架构差异改为带条件判断，V6/V8 待执行 |
| [光照资产][light-assets] | Lightmap/探针内容按模式解释；保留采样与驻留成本；校正 SH 已打包系数展开；完整 Shader 改为教学框架；区分 Meta Pass 与运行时 Lightmap 使用 | Unity Light Modes 与公开 Package `SphericalHarmonics.hlsl` 静态核对；传统探针外边界等未实测行为不再写固定结论，V10 待执行 |
| [阴影][shadow] | 撤下无来源的设备毫秒数、百分比和已选配置；纠正级联/点光源固定倍率；修正 Screen Space Shadows；移除 RAM 即 GPU 档次及统一阈值；保留待执行对照 | Unity 6 URP 文档；未取得旧数值的原始记录，未以其他文章的数字替代。没有新的性能结果 |
| [Shader][shader] | half 的范围与有效数字、时间示例；quad/wave 区分；分支与变体成本；通道打包条件；候选代码统一法线坐标系并说明未用变量可被剔除 | Unity 精度文档与 Microsoft HLSL 术语；未编译完整 Shader，V7/V9 待执行 |
| [资产规格][asset-spec] | 表格改为公开可见的教学候选；移除隐藏注释中的虚构来源要求；UI 源格式/GPU 格式区分；2048 ASTC 4x4 基础层改为 4 MiB；撤下统一贴图预算与无条件画质/帧率承诺 | 公式与格式资料；分层交付文字区分 label、打包依赖与资源映射，下载代码未运行，四档质量与驻留待验收 |

旧文修订阶段保留的工作：V1—V10 的材料与实验、旧文的进一步例子和解释补桥、父系列 canonical 拆分与入口阅读路径。当时新增 N1/N2 暂不执行；后续用户已授权整理提纲，见第 9 节。内容审查未扩到全站，其他文章中的同类数字和机制仍需单独核查，不能由本轮为其背书。

本轮验证：七篇文章的代码围栏、表格列数、站内 `relref` 与空白检查通过；ASTC 位率、2048×2048 ASTC 4x4 基础层以及三组 ASTC 6x6 完整 Mip 数据量已独立核算。`git diff --check` 通过，本地 Hugo 构建退出码为 0、无 ERROR。未执行 Unity 编译、烘焙、GPU capture、真机采样或线上发布。

## 9. 2026 年 10 月 26 日两篇详细提纲

用户确认：先以读懂原理、能判断该查什么为主，配少量 Unity 检查步骤。此次只整理详细提纲，不起草发布正文。

| 提纲 | 正文主线 | Unity 映射与材料状态 |
| --- | --- | --- |
| [N1 程序怎样理解美术资产成本](mobile-art-asset-cost-overview-outline.md) | 五节：接到什么对象 → 参数怎样成为工作 → 使用条件怎样改变问题 → 怎样比较修改 → 怎样形成检查记录 | 两张短卡：资源引用、相关绘制。法杖为主，庭院和圆环补使用情境；教学素材与检查结果待制作/执行 |
| [N2 模型面数之外的成本](mobile-geometry-cost-vertices-screen-triangles-lod-outline.md) | 六节：计数口径 → 导入属性 → 屏幕贡献 → 绘制用途 → 几何/法线/LOD → 候选判断 | 三张短卡：Mesh 数据、绘制用途、LOD 切换。复用法杖金属框身份，新增对照材料待制作；数据和性能结果留空 |

两份提纲均写明每节要解释的因果关系、建议小节、图示用途、读者应能作出的判断、旧文分工与证据需求。第一篇集中建立成本模型，第二篇完整展开几何取舍；Unity 检查用于观察数据和工作，不扩展为完整工具教程或设备实验。

已核对 Unity 6000.0 的 Mesh 属性、数据预览、Frame Debugger 与 LODGroup 文档。文档依据、教学推演与未执行观察分别标注；没有新增 Unity 运行或设备结果。本次不创建 `content/` 占位文章，不调整入口，不拆分 canonical plan，不提交或推送。

提纲阶段验证：两篇提纲与本盘点的 60 处本地链接、引用标签、表格列数、代码围栏和空白检查通过；正文骨架/检查卡分别为 5/2 与 6/3。本地 Hugo 构建退出码为 0、无 ERROR；此结果只说明文档和站点构建检查通过。

## 10. 2026 年 10 月 26 日 N1 正文本地成稿

按用户确认的提纲完成[《程序怎样理解美术资产成本》](../content/performance/mobile-art-asset-cost-overview.md)。五节围绕资产对象、四层模型、使用条件、方案比较和检查记录展开；用两幅关系示意连接数据转换、引用和使用，Unity 检查保留两张三步短卡。

文件与 slug 为 `mobile-art-asset-cost-overview`，series 为“移动端硬件与优化”，primary_series 为 `mobile-hardware-and-optimization`，weight 为 2205。该父系列仍未拆出独立 canonical plan，遵循前述边界，本轮只回写本执行记录和单篇提纲，不改只读归档或根路由，不新增第二套系列 plan；入口专题编排留待后续集中完成。

资料补充包括 Unity 6000.0 的资产工作流、Prefab、GPU 纹理格式、渲染优化、Mipmap Streaming、批处理与 Frame Debugger。正文明确把教学设定、原理推导和实际观察分开；未制作配套资产、运行检查卡或测量性能。法杖比较记录保留 DATA-TODO，不填写顶点数、pass 或耗时结果。

本轮仅写 N1 正文并同步其执行状态，不写 N2 正文，不提交、推送或线上发布。

本轮验证：正文 17 处 `relref`、两份同步文档的 44 处本地链接、表格、围栏、空白与唯一 slug 检查通过；生成页面的 23 处本地链接目标可达，原有系列目录自动收录该文。浏览器确认两幅 Mermaid 图已渲染，检查了首屏、图示和表格；DATA-TODO 保留为 HTML 注释，不出现在渲染文字中。最终 Hugo 完整预览构建退出码为 0、无 ERROR。以上是文档/页面验证，不代表 Unity 检查、设备性能或线上部署验证。

## 11. 2026 年 10 月 26 日 N2 正文本地成稿

按用户确认提纲完成[《模型面数之外的成本：顶点、屏幕三角形与 LOD》](../content/performance/mobile-geometry-cost-vertices-screen-triangles-lod.md)。六节依次解释计数口径、完整顶点属性、屏幕贡献、绘制用途、几何/法线/LOD 与候选判断，保留三张三步 Unity 短卡。用两幅内嵌 SVG 表达三角化/索引及投影尺度，一幅 Mermaid 表达待确认绘制用途；所有图均为教学示意。

文件与 slug 为 `mobile-geometry-cost-vertices-screen-triangles-lod`，series 为“移动端硬件与优化”，primary_series 为 `mobile-hardware-and-optimization`，weight 为 2207。为 N1 补一处到 N2 的正文链接，N2 回链 N1；不扩大入口改编或 canonical 拆分范围，根路由与只读归档不变。

核对 Unity 6000.0 的 Mesh 顶点/索引、Mesh 预览、LODGroup、法线映射与 Frame Debugger 文档；补充 Arm 对 Mali 索引复用、缓存模型及细小三角形分析的材料。文中区分一般数据关系、具体实现资料与待观察内容，不把厂商缓存模型、像素经验范围或旧课程演示泛化成硬件保证。

法杖的属性对照、几何/法线候选、简化 LOD 与配套工程均待制作；没有执行 Unity 检查、GPU 捕获或设备采样。保留 DATA-TODO，不填导入结果、绘制事件或性能收益。本轮不提交、推送或线上发布。

本轮验证：两篇正文的 27 处 `relref`、两份同步文档的 48 处本地链接、表格/围栏/空白与唯一 slug 检查通过；N2 为六节、三张三步卡，两个 SVG 的 XML 与标识符检查通过。生成页面有两个 SVG、一段 Mermaid，15 处本地链接目标存在，系列目录及 N1 到 N2 的链接成立。浏览器检查两幅几何图与用途图均正常渲染，DATA-TODO 只在 HTML 注释中，不出现在页面文字。最终 Hugo 完整预览构建退出码为 0、无 ERROR；这些结果仅覆盖文档与页面。

## 12. 四类资产的后续覆盖盘点

用户要求先确认纹理、音频、模型/动画、Shader 当前缺口与补强项，再决定新增选题。独立执行记录见[资产成本内容覆盖、缺口与补强盘点](art-asset-cost-coverage-and-strengthening-audit-2026-10-26.md)。该盘点扩到跨栏目承接与音频/动画，不把这些内容归入 Samsung 原文；区分已有原理、待纠错、解释桥与验证需求。本阶段不修改正文、不确定新增篇目，也不启动工程或设备实验。

## 13. 2026 年 10 月 26 日跨栏目 P0 旧文修订

按用户确认范围修订包体控制、多端资源差异、内存预算的 Audio 节及对应检查项、Shader 关键字设计和动态覆盖契约，共五篇旧文。处理运行副本与包体、Mip 使用条件、ASTC 能力与回退、LOD 维护、音频加载/编码和 Shader 声明/保留/交付的边界；撤下无据结果与未限定的数字，不写新增文章。

逐项状态见[跨栏目盘点第 8 节](art-asset-cost-coverage-and-strengthening-audit-2026-10-26.md#8-2026-年-10-月-26-日-p0-旧文事实与边界修订)：10 个 P0 风险编号已完成本地文字处理，R6/R12/R13 留待 P1，9 类工程验证需求仍全部待执行。本次未编译运行 Unity 示例、制作构建产物或取得设备/听感结果，未提交、推送或发布。

本轮文档静态检查与生成页面检查通过；Hugo 完整本地构建退出码为 0、无 ERROR。详细检查范围见上述修订记录，不将站点构建通过视为工程验证完成。

## 14. 2026 年 10 月 26 日 P1 规格与体验条件修订

按用户确认范围修订[资源质量规则](../content/code-quality/game-quality-04-asset-quality.md)与[体验一致性](../content/performance/device-tier-experience-consistency.md)，不写新增文章。撤下统一音频参数、动画时长、纹理/几何规格与 LOD 级数，限定示例配置；将群体听感、LUT/反馈成本和动画降档的泛化结论改为接受条件与待比较方案。两篇缺少原始记录的事故/收益叙事均改为明确的教学情境，不填写真实帧率、设备占用或玩家反馈。

逐项状态见[跨栏目盘点第 9 节](art-asset-cost-coverage-and-strengthening-audit-2026-10-26.md#9-2026-年-10-月-26-日-p1-规格与体验条件修订)。R6/R12/R13 已完成文字处理；第 13 节是此前 P0 阶段的历史状态。P1“统计与验证桥”仍待处理，9 类验证需求继续待执行；没有运行 Unity/CI 检查、目标设备或画面/听感/玩法测试，未提交、推送或发布。

本轮静态与生成页面检查通过；Hugo 完整本地构建退出码为 0、无 ERROR。具体范围见上述第 9 节，不将文档检查通过视为实际门禁、性能或体验验证结果。

## 15. 2026 年 10 月 26 日 P1 统计与验证桥修订

按用户确认范围修订变体 CI、动画算法与 Importer 三篇：区分候选/观察条目/保留/命中、缓存与完整性、Unity 版本/入口/Rig 和误差度量，并把数据保留沿 Shader、动画、蒙皮、脚本、挂点、事件等消费者展开。没有新增文章；Importer 文末原有的跨系列路径导航保留。

详见[跨栏目盘点第 10 节](art-asset-cost-coverage-and-strengthening-audit-2026-10-26.md#10-2026-年-10-月-26-日-p1-统计与验证桥修订)。P1 两组共五篇及 P0 五篇的本地文字修订均已有记录；此前各节是对应阶段的历史状态。Python 对比骨架和独立 .NET 10 动画片段仅以合成输入做有限检查，未运行 Unity 采集/导入、真实构建/CI 或设备/动作/画面测试；9 类实际验证需求仍待执行。未提交、推送或发布。

本轮静态、示例与生成页面检查按各自范围通过；Hugo 完整本地构建退出码为 0、无 ERROR。详细范围及未执行项保留在上述第 10 节，不据此声称真实资产、动态覆盖或设备收益已经验证。

[samsung]: https://developer.samsung.com/galaxy-gamedev/blog/en/2021/05/19/best-practices-for-mobile-game-developers-and-artists
[geometry]: https://learn.unity.com/course/3d-art-optimization-for-mobile-gaming-5474/unit/geometry-7415/tutorial/triangle-and-polygon-usage-8140?version=2019.4
[lod]: https://learn.unity.com/tutorial/level-of-detail-3653
[geometry-extra]: https://learn.unity.com/tutorial/additional-geometry-best-practices-7882
[texturing]: https://developer.arm.com/-/media/Arm%20Developer%20Community/PDF/Unity/RealTime3dArtBestPractices_Texturing.pdf?revision=8c95226c-24cf-425b-b9c9-0197de13fb99
[materials]: https://developer.arm.com/-/media/Arm%20Developer%20Community/PDF/Unity/RealTime3dArtBestPractices_Materials.pdf?revision=350b4fcc-804b-4591-ac70-e42dda69feac
[lighting]: https://developer.arm.com/-/media/Arm%20Developer%20Community/PDF/Unity/102109_0100_01_RealTime3dArtBestPractices_Lighting.pdf?revision=d17227e1-53df-43e9-8f2a-89b5687b275d
[arm-unity]: https://developer.arm.com/mobile-graphics-and-gaming/unity
[astc4]: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/TextureFormat.ASTC_4x4.html
[astc8]: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/TextureFormat.ASTC_8x8.html
[batching]: https://docs.unity3d.com/6000.0/Documentation/Manual/DrawCallBatching.html
[light-modes]: https://docs.unity3d.com/6000.0/Documentation/Manual/LightModes-introduction.html
[screen-shadows]: https://docs.unity3d.com/6000.0/Documentation/Manual/urp/renderer-feature-screen-space-shadows.html
[mesh-material]: ../content/rendering/unity-rendering-01-mesh-material-texture.md
[storage]: ../content/rendering/unity-rendering-supp-a-mesh-texture-storage.md
[draw-overdraw]: ../content/performance/gpu-opt-01-drawcall-overdraw.md
[bandwidth]: ../content/performance/gpu-opt-02-bandwidth.md
[shader]: ../content/performance/gpu-opt-03-shader.md
[shadow]: ../content/performance/gpu-opt-04-shadow-mobile.md
[light-assets]: ../content/rendering/unity-rendering-02-lighting-assets.md
[asset-spec]: ../content/performance/device-tier-asset-spec-texture-and-package.md
[gpu-budget]: ../content/performance/game-budget-05-gpu-budget.md
[shader-governance]: ../content/performance/rendering-tier-design-05-material-shader-governance.md
[normal-map]: ../content/rendering/shader-lighting-02-normal-map.md
[asset-standards]: ../content/delivery-engineering/delivery-content-pipeline-03-asset-standards.md
[art03]: ../content/delivery-engineering/game-art-production-03-character-assets.md
[art06]: ../content/delivery-engineering/game-art-production-06-environment-assets.md
[art07]: ../content/delivery-engineering/game-art-production-07-level-art.md
[art08]: ../content/delivery-engineering/game-art-production-08-vfx.md
[art09]: ../content/delivery-engineering/game-art-production-09-technical-art.md
[acceptance]: ../content/delivery-engineering/game-art-production-12-joint-acceptance.md
[mobile-index]: ../content/performance/mobile-hardware-and-optimization-series-index.md
[mobile-entry]: ../content/performance/mobile-platform-reading-entry.md
