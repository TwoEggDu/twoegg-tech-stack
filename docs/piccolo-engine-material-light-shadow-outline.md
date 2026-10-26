# Piccolo 16：材质、光照与阴影怎样共同决定表面颜色

> 执行层提纲与证据记录；篇级状态由 `piccolo-engine-series-plan.md` 统一维护。

## 定位与边界

- 接续用户已确认的 11 相机、15 GPU 资源，写完本组第三篇 16。以引擎无关模型补齐必要概念，再落固定 Piccolo 的默认延迟路径。
- 固定源码 `BoomingTech/Piccolo@f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，保持引擎副本只读；不修补 PBR、材质功能或阴影实现。
- 唯一主问题：一个可见表面位置怎样综合表面属性、视角、入射光与遮挡，得到后处理前的场景颜色。
- 默认非蒙皮、不透明、DEFAULT_LIT 路径；方向光阴影走完整路径，点光源只说明差异，IBL 解释职责与输入，不扩写完整预积分/烘焙教程。
- 不重复 15 上传与资源身份、07 绑定与提交、11 相机更新；输出止于场景颜色，tone mapping/颜色分级留给 17。
- 新正文 series_order=19，接 15，保留既有顺序；计入共 20 篇，09—18 中 11/15/16 已写，其余七篇保持待写。

## 执行清单

- [x] 核对材质 CPU 默认值、创建时常量写入、G-buffer 字段与格式、法线空间。
- [x] 核对延迟深度重建、N/V/L、BRDF 与直接光/La/IBL 的实际汇合。
- [x] 沿方向光矩阵、可见节点、R32 深度颜色附件、采样与偏置比较走完整链，核对实际绘制开关。
- [x] 标明 normalScale/AO/自发光、BRDF 特定表达、精度等实现边界，不把声明当成功能闭环。
- [x] 独立编译相关 GLSL，执行限定条件的 CPU 数值检查，分别登记与 GPU 运行的边界。
- [x] 完成正文、两类关系图、输入变化表和断点表；回写导航/规划，运行 Hugo 与生成页检查。

## 正文提纲

1. 同一表面为什么随灯光、视角和遮挡变化；表面属性、入射光、反射响应和可见性分工。
2. MaterialRes 的纹理引用、RenderEntity 默认 factor、创建时 UBO 与 set=2；不是任意 JSON 参数都自动生效。
3. mesh_gbuffer.frag 读取线性表面输入、构造 TBN 法线、打包三张附件；格式与量化，emissive/AO/normalScale 实际未闭环。
4. deferred_lighting.frag 读取 G-buffer/深度，用逆 P*V 重建位置，建立 N/V/L 与 F0；D/G/F 职责，保留 Piccolo 的确切表达与前提。
5. 方向光相机矩阵与独立可见列表；阴影 Pass 写 R32 颜色附件，depth test 选近点；set=0,binding=7 采样并加偏置比较，阴影只控制该直接光贡献。
6. 简单环境项、IBL 三类输入、粗糙度选 LOD、Lo+La+Libl 输出浮点场景颜色，停止在后处理之前。
7. 固定 sampled surface / normal / light / visibility 的数值练习；区分近高光峰值与全画面亮度，区分 CPU 算式、Shader 编译与运行。
8. 后续断点、附件与 GPU 捕获表，读者能定位某个输入在哪一站丢失或未使用。

## 证据入口

- Shader：mesh_gbuffer.frag、deferred_lighting.frag、mesh_directional_light_shadow.vert/.frag、mesh_point_light_shadow.frag；include/gbuffer.h、mesh_lighting.h、mesh_lighting.inl。
- C++：MaterialRes、RenderEntity、RenderResource::getOrCreateVulkanMaterial/updatePerFrameBuffer、RenderSystem::processSwapData、RenderScene::updateVisibleObjectsDirectionalLight、CalculateDirectionalLightCamera、DirectionalLightShadowPass、MainCameraPass、RenderPipeline::deferredRender、VulkanRHI 初始阴影开关。
- Vulkan 官方 Formats 文档核对 SRGB/UNORM 语义；不以 include 文件名或注释证明实际算法闭环。

## 验证记录

- 2026-10-25 回读固定 SHA；引擎阅读副本工作区干净。正文 9 段 GLSL 节选去空白后匹配原文件，19 个固定版本链接均对应源文件、目录或许可证。Vulkan framebuffer/格式文档用于核对附件编码语义；未把新扩展能力套入固定实现。
- 核对 MaterialRes 五个路径、RenderEntity 默认 factor、创建时 UBO 写入及材质缓存；normalScale 在当前 normal 计算中未使用，AO 未进入 G-buffer/照明，自发光 Le 未接最终输出。明确字段、绑定、编译与功能生效分别取证。
- 核对 G-buffer A/B 为 RGBA8 UNORM、C 为 RGBA8 SRGB，DecodeNormal 不重归一化；specular 固定 0.5 会经历存储量化，CPU 条件练习明确忽略此误差，不伪装成实际像素。
- 沿 CalculateDirectionalLightCamera → 两份每帧矩阵 → 方向光可见列表 → Shadow Pass → R32 颜色附件 → nearest sampler → 光源深度比较走完整路径；区分深度测试附件与实际采样图像。方向光网格 draw 也受 isPointLightShadowEnabled 控制，GNU/Mach 初始化分支关闭该标记，正文保留平台/状态条件。
- 核对方向光直接项 NoL 在 BRDF 外乘入，shadow ±1 由分支使用，不是负值乘色；BRDF 使用 dotNV 的 Fresnel 输入与实际组合表达，不替换成其他教材公式。La 与 Libl 独立保留，方向光阴影未自动遮蔽全部环境贡献；点光源仅说明其独立衰减/双抛物面距离比较路径。
- 产物目录 `%TEMP%/TechStackShow-piccolo-lighting-16-20261025/`。固定仓库自带 glslangValidator，按原 CMake 的 `-I<shader/include> -V100 -o <temp产物> <原Shader>` 编译 mesh_gbuffer.frag、deferred_lighting.frag、mesh_directional_light_shadow.vert/.frag，四次退出 0；产物均为 SPIR-V 1.0、魔数 0x07230203，大小依次为 6688、21896、6936、464 字节。未运行 spirv-val 或实际 Vulkan pipeline 创建。
- 原 mesh_lighting.h 在 ndcxy_to_uv 之前的函数文本完整保存为 `lighting_functions.inc`，回读确认逐字匹配。`lighting_checks.cpp` 仅用最小 vec3 运算与 highp 空宏适配 GLSL 计算，GCC 8.3.0、`-std=c++17 -O0 -Wall -Wextra` 编译退出 0、运行退出 0。程序 SHA-256 为 `a41520196495d02cc78c71111b17e1db699c70a052ea9c722bab1bd3ecd5aeed`。
- 数值练习固定理想 N=V=L、basecolor/metallic/roughness/F0、En 和 ambient；Libl 为给定输入，不模拟环境贴图采样。得到直接光 Lo≈(0.492996,0.248535,0.126304)，总色≈(0.622996,0.328535,0.176304)，移除该光后保留 (0.13,0.08,0.05)。检查正入射粗糙度变化、0.05 下限、阴影偏置分支和理想 UNORM8 算式；不宣称覆盖全部方向、能量守恒或 GPU 精度。
- SPIR-V SHA-256：mesh_gbuffer.frag `4da9fd3eadee358a58568230253119e9368fa285ef6c04c48d8f184ae7ab0718`；deferred_lighting.frag `fdaef4613b34ecd4469385e83b73ffd5f9bf74223a35b6d607be82d601cd1e74`；shadow.vert `1eb0e11d61c30c56e81379b584e1f27000def4d7a678a6ac0f444ba2a246c18f`；shadow.frag `27da92c7d453ce022ba9b3c9814dae42ad03a556afffcf157564b70039e79f65`。
- Hugo v0.158.0 构建退出 0、ERROR 为零，生成 1374 页、54 个静态文件；输出/缓存位于上述目录 public/cache。共享工作区另有同期文章，整站页数不是本任务新增篇数。
- 20 篇 Piccolo 加两级阅读入口与游戏图形系统索引，共 23 个生成页，1354 处站内链接、锚点和资源引用检查通过。series_order 唯一连续为 0～19；15 下一篇与 16 上一篇双向对应，16 末页返回目录 chapters，三个入口均可到达 16；新页沿用 series-reading.css，含两段 Mermaid 图源。
- canonical 表登记 16 已写、09—18 其余七篇待写，11/15/16 三篇齐备不等于全部 Piccolo 选题或运行实践齐备。根 doc-plan 仅更新系列汇总；11、15、07、GFX-01/02 和相关入口均已回链。
- 未运行完整 Piccolo、GPU 附件/阴影生成/IBL 采样、RenderDoc 捕获或浏览器目视。静态页面与 Mermaid 图源检查不等于图形渲染验收；未改引擎、共享模板/CSS，未暂存、提交或推送本轮文件。
## GBuffer 像素与延迟光照实测安排（2026-10-26）

承接07已有的同进程前后捕获，沿Wall_1目标Draw追到实际像素。先从目标实例的投影与深度/事件归属确认采样点，再读取GBuffer A/B/C、深度以及延迟光照input attachments与浮点输出；前后各自确认覆盖，不能假定相同屏幕坐标仍属同一对象。以捕获事件/资源/像素及数值为证据，按工具能力决定是否用pixel history/debug pixel补充，不把工具不支持当成引擎失败。保留原始捕获并新增独立材料和回执，回填07、16及导航/规划；本轮不提交推送、不扩大为后处理/最终显示、全部光照贡献或完整同步验收。


## GBuffer 像素与延迟光照实测结果（2026-10-26）

- 复用此前两份同进程RDC，检查指纹；未新增引擎运行、代码或构建。实际viewport289/60/668/448，从目标slot0顶点选内部点(663,242)/(650,266)，深度误差约4.09e-7/6.80e-7，片元世界坐标对几何插值最大误差约1.01e-4；反查同批七实例时均唯一匹配slot0/triangle4。
- 修改后旧xy仍属slot0，但转为triangle3，UV变化；未预设它必然换对象，也不以两个主样本比较同一材质点的颜色变化。
- 事件177输出247/251/255/D32-145，事件186的set1/binding0—3输入相同；四份原始像素字节及调试输入吻合。后者pipeline289/FS288/subpass1，RGBA16F输出259；反射核对perframe偏移与688字节范围。
- 主样本B均[0,127,190,1]，后样本A RGB[128,255,126]、C SRGB[96,84,60]。保留raw byte、picked linear与shader变量；不解释A/C alpha。实际specular为127/255，不以理想0.5代替。
- 点光源数0、环境光0.03、方向光约(0.57735,-0.57735,0.57735)。NoL负，实际阴影closest/current/shadow分支变量未执行；Lo0，La+Libl有值。调试分量和实际FP16分列，求和误差<7e-9、调试到存储最大差0.692个half间隔，未归因成单一舍入步骤。
- PixelHistory虽列出事件，但preMod/postMod/shaderOut均IsValid=False、primitive字段全0；不消费无效字节、不以该字段定位primitive。几何索引、深度、片元输入与附件原始字节组成证据链。
- 保存最终已重跑脚本gbuffer-lighting-replay.py、完整报告、三个样本CSV和独立回执；不改先前运行回执。中间API探测保留在实验目录：VKViewState的实际字段为viewportScissors，最终使用PipeState.GetViewport(0)；先前面积优先候选未过深度检查，改为先筛depth_error<1e-5再按面积选点，没有把失败候选写成目标证据。
- 回填07/16、两个阅读入口、canonical系列规划和根系列摘要；本轮止于目标单点延迟光照，不验收实际阴影、全部IBL/光源、17后处理与最终显示。原始RDC留本机，不暂存、提交或推送。
- 捕获内GBuffer/延迟光照shader的SPIR-V SHA-256分别为4da9fd3eadee358a58568230253119e9368fa285ef6c04c48d8f184ae7ab0718/fdaef4613b34ecd4469385e83b73ffd5f9bf74223a35b6d607be82d601cd1e74，与此前独立编译结果逐字节指纹一致。附件字节是回放GPU图像读回，不是原始执行另存显存快照。
- 本地Hugo v0.157.0退出0、ERROR为零，1425页/57静态文件；27篇Piccolo正文与两个阅读入口共29页、1887处链接/锚点/资源检查通过，新材料指纹匹配且前轮回执未变。未验收线上部署或浏览器像素级效果；不暂存、提交或推送。
