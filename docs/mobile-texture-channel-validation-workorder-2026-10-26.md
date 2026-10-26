# 纹理细节、格式与通道消费：V-T / V-C 最小验证工作单

> 日期：2026-10-26。状态：环境只读预检与原创源素材准备完成，Unity 导入、Android 构建和设备画面对照未执行。
> 已有参考：[跨栏目盘点](art-asset-cost-coverage-and-strengthening-audit-2026-10-26.md)第 5 节定义 V-T / V-C；[总览提纲](mobile-art-asset-cost-overview-outline.md)已有引用与绘制检查卡。本单补固定素材、变量、观察入口与回填规则，不新增文章、canonical plan 或完整性能工程。

## 1. 本轮问题与明确范围

让读者能用同一组输入解释：目标屏幕需要哪些纹理细节，平台配置实际形成了什么格式，以及 Alpha/各通道究竟被谁使用。

只设计三个验证块：尺寸、格式、通道消费者。首先取得配置、实际表示和匹配画面的对应，不测整帧瓶颈，不建立最佳尺寸/ASTC 块大小标准，不将一台设备或诊断图案推广到全部美术资产。法线、HDR、图集、透明排序和 GPU 时间留到对应问题，不混入本轮。

本单中的版本、设备预检是已取得的静态/ADB 信息；材质、相机与候选矩阵是执行方案；预期现象均是待验证假设。三类不得互换。

## 2. 已确认环境与执行选择

| 项目 | 当前信息 | 证据与边界 |
| --- | --- | --- |
| Unity | 本机已有 `2022.3.62f3 (96770f904ca7)` | 已核对编辑器文件版本及现有实验工程版本文件；本轮未启动或验证许可/编译，不视为官方全部发行版保证 |
| URP | 本机包、现有工程 manifest/lock 指向 `14.0.12` | 已核对本机 Package manifest；不冒充文章参考的 Unity 6000.0 / URP 17 实验 |
| Android | `samsung / SM-S916U`，Android 13，API 33 | ADB 只读查询，有且仅有一台已授权设备；没有安装或运行本实验 APK |
| 屏幕 | ADB 报物理尺寸 `1080×2340` | 不等于横屏 Player 的渲染目标、Screen 尺寸或对象覆盖 |
| 电脑 | NVIDIA GeForce RTX 2060，另有虚拟显示适配器 | 只作宿主信息，不能证明 Android ASTC 能力，也不能用桌面截图替代真机画面 |
| 现有素材 | 已检查的本地实验仍是几何/纯色占位，未找到可直接承接的源纹理/真实模型 | 不复用私有仓原稿或复制其场景；本轮使用本仓原创输入 |

执行选择：先沿已有的 2022.3 / URP 14 环境，在独立纹理实验场景使用原创输入，避开原场景生成器和其他任务的材质。Android 首轮拟固定 Vulkan、Linear 色彩空间；启动后必须记录实际 `graphicsDeviceType` 和能力。不支持或启动失败就记录失败，不静默换 API；GLES3 若另测，使用独立 run ID。

Unity 6 的对应检查仍待另跑。可跨版本解释的原理与本版本观察要分别写，不能为了和正文版本一致而改写实际版本记录。

字段与平台配置分别参考 Unity 2022.3 的 [Texture Import Settings][texture-import]和[平台纹理格式资料][platform-formats]，实际结果仍以选定构建和 Player 的记录为准。

## 3. 已准备的源输入与材质契约

源文件、生成方式和来源见 [fixtures README](../examples/mobile-texture-validation/README.md)，精确 SHA256 与解码通道摘要见 [source-manifest.json](../examples/mobile-texture-validation/fixtures/source-manifest.json)。生成器只使用标准库和数值规则，不含第三方图片、私有工程素材或 AI 生成贴图。

| 输入 | 已完成内容 | Unity 中还需做什么 |
| --- | --- | --- |
| `uv-board.obj` | 2×2 源单位平面，4 个位置、2 个三角形、0—1 UV | 记录导入顶点/属性、朝向和最终屏幕覆盖；源计数不冒充运行数据 |
| `base-alpha.png` | 1024² RGBA8，条纹/色块/斜线/梯度，Alpha 横向变化 | Texture Type=Default、sRGB 开启；显式记录 Alpha Source、尺寸、格式、Mip |
| `base-opaque.png` | 与上一张解码后的 RGB 完全相同，Alpha 全 255 | 仍为 RGBA 源容器；不称“移除 Alpha 后的纹理格式” |
| `metal-ao-smooth.png` | R/G/A 为 metallic/occlusion/smoothness 候选，B 为诊断信号 | 作为数据贴图关闭 sRGB；通道含义只有接入相应 Shader 输入后才成立 |

材质方案采用 URP/Lit 的 Metallic 工作流，Surface=Opaque、Alpha Clipping 关闭、Base Color 为白。分开保存下列状态，不在一组对照中暗改多个状态：

| 状态 | 纹理与参数 | 本轮要确认的消费者 |
| --- | --- | --- |
| `M_BaseFixed` | Base Map 使用颜色纹理；无 Metallic Map，Smoothness Source=Metallic Alpha，固定 Smoothness | Base RGB 提供颜色；Base A 不预设为透明，也需排查其他 pass/功能 |
| `M_BaseAlphaSmooth` | Base Map 不变；Smoothness Source=Albedo Alpha，Smoothness 固定为 1 | Base A 参与 smoothness；这是一组独立固定状态，不将其与上一状态的差异当作单纯纹理收益 |
| `M_MaskReference` | `_MetallicGlossMap` 使用数据纹理，Smoothness Source=Metallic Alpha；可单独指定 `_OcclusionMap` | R/A 分别用于 metallic/smoothness；G 只有接入 Occlusion 采样路径才有该用途；B 不据名字自动获得语义 |

这些材质尚未创建。URP 14 的 [Lit 说明][urp-lit]及[公开 LitInput.hlsl][lit-input]支持上述语义检查；本机 `LitInput.hlsl` SHA256 为 `b24b978e05dc41594240b8e4f56dbec6babe41d72675633d38aaa8b5d6308b38`。这是静态文件指纹，不证明所有变体已编译/保留，也不宣称可变 GitHub 分支与本机文件完全一致。

## 4. 共用控制与记录

| 变量 | 本轮控制方式 | 不控制的干扰 |
| --- | --- | --- |
| 场景 | 一块测试板、一台正交相机、一盏固定方向光；无额外对象遮挡 | 变化可能来自覆盖、几何或照明 |
| 画面路径 | 固定 URP Asset/Renderer、Linear、Render Scale=1；关闭动态分辨率、后处理、MSAA 和阴影 | 分辨率、调色、抗锯齿或额外 pass 掩盖差异 |
| 目标尺寸 | 拟申请横屏 1280×720；以 Player 实际 Screen、相机 pixelRect、渲染目标和截图尺寸为准 | 物理屏幕分辨率不保证内部渲染尺寸；条件不同则不配对比较 |
| 观察视角 | 固定两个覆盖：测试板高约 512 px / 128 px，以实际目标校准；保持 UV、朝向与采样位置 | 用世界距离代替屏幕需求会混入投影差异 |
| 纹理其他设置 | Bilinear、Aniso=1、Clamp；Mip 开启、Streaming 关闭、无全局 Mip 限制、Read/Write 关闭 | 过滤、驻留或 CPU 副本变化与尺寸/格式混淆 |
| 引用与加载 | 一次显示一张候选；所有驻留候选记录完整，不以进程总量差作为单纹理占用 | 同时保留原图和候选可能改变总驻留 |
| 捕获 | 同 run 内匹配对象状态、视角、光照、输出与原始 PNG；不对比较图自动锐化或重采样 | 截图处理本身产生差异 |

方向光角度和相机先在基线中校准：必须看得到 smoothness 极端值的不同，否则通道“有消费”的正对照无效。校准后记录具体参数并固定，不能在候选之间调整以追求某种结果。

每次执行记录 `runId / caseId / 源素材哈希 / Unity完整版本 / 包锁 / 场景与材质状态 / APK身份 / 设备型号与OS / 实际API与GPU / 实际渲染条件 / 观察和附件 / 未覆盖项`。不公开 ADB 序列号、账号、许可或原始日志中的本机信息。

## 5. 验证块一：尺寸与屏幕需求（V-T-S）

### 要验证的假设与最小场景

在同一显示条件下，降低实际导入尺寸可能损失细线等可见细节；损失取决于采样尺度，不应只由源图尺寸下结论。使用测试板、`base-alpha.png` 和 `M_BaseFixed`。

### 控制变量与候选

只改变目标平台 Max Size：1024 基线 → 512；第一对完成后再考虑 256。格式固定 RGBA32，保持本单共用设置；两种覆盖各拍一对。每个候选确认实际导入尺寸，不把设定值当结果。

### 指标与工具

| 指标 | 入口 | 待观察项与限制 |
| --- | --- | --- |
| 目标配置与导入表示 | Inspector 的 Texture Import Settings、Android 覆盖与预览；必要时读 [GetPlatformTextureSettings][platform-settings] | Max Size、有效尺寸、格式、Mip、sRGB、Alpha；Editor 预览不代表 Player |
| Player 表示 | `width/height/mipmapCount`、[Texture2D.format][texture-format]、[Texture.graphicsFormat][graphics-format] | 所有实际值待记录；不能用 Inspector 的 Android 预期格式填 Player 栏 |
| 匹配画面 | 原始截图、固定条纹/斜线/梯度区域 | 哪些特征仍可辨识，是否出现模糊/闪烁；不先写“512 足够” |
| 字节层 | 源 PNG 文件、可取得的构建资源统计、[GetRuntimeMemorySizeLong][runtime-size] 分开记录 | 对象估计不是纯 GPU 显存或进程总量；理论载荷与下载/构建大小不同 |

### 预期现象

如果实际尺寸按候选降低，理论 texel 数据量会下降；画面可能在近覆盖先损失高频细节。若画面不变，可能是采样已落在共同的较低 Mip，也可能是观察区域/条件不敏感；不自动判定候选无损或性能更快。

### 不符合时查什么

确认是否真正重导入、是否仍引用基线、有效 Max Size 是否生效，以及实际 Mip/全局限制、输出分辨率和截图尺度。先核对这些，再决定增加观察视角或检查采样路径。

### 结果与完成条件

结果：**待执行**。最低产物为两种覆盖下 1024/512 的配置、Player 表示和匹配截图；取得观察后只讨论该素材与条件，不给通用尺寸标准。

## 6. 验证块二：目标压缩格式（V-T-F）

### 要验证的假设与最小场景

若 Player 保持 ASTC 块格式，位率由块大小决定；目标能力和实际格式必须确认，配置不能单独证明原生采样或不存在回退。复用上块，尺寸固定 1024。

### 控制变量与候选

第一对仅 RGBA32 → ASTC 6×6；确认流程后扩为 4×4、8×8。纹理内容、尺寸/Mip、材质、渲染条件不变，ASTC 编码质量设置固定并记录；与尺寸实验分开。能力不支持时记录支持/回退路径，不把解压后的结果贴成 ASTC 原生结果。

### 指标与工具

| 指标 | 入口 | 待观察项与限制 |
| --- | --- | --- |
| 导入预期 | Texture Import Settings Android 覆盖；[GetAutomaticFormat][automatic-format]仅用于自动配置时辅助解释 | 显式/自动模式、有效格式、编码质量、尺寸/Mip |
| 实际能力与表示 | Player 的 [IsFormatSupported][format-support]，按实际 `GraphicsFormat` 查询 Sample 用途；再记录纹理实际格式与目标 API | 型号、Android/API 名称或一次能力查询，都不单独证明最终纹理使用路径 |
| 理论载荷 | 每级 `ceil(w/bw) × ceil(h/bh) × 16 bytes` 后求和 | ASTC 固定每块 128 bit；仅为理论数据量，不含驱动/CPU副本/回退 |
| 构建与运行对象 | 构建报告中可取得的资源统计、对象估计；实际 GPU 分配若工具不能取得就标缺 | 不将 APK 总大小、PNG 字节或对象估计互换；没有资源级统计就不填资源体积收益 |
| 压缩误差 | 真机匹配截图与同尺寸 RGBA32 对照 | 条纹、斜线、色块、梯度分别看；不把诊断图案推广到法线或自然材质 |

### 预期现象

在实际保持相应格式/尺寸的前提下，块变大使理论位率降低，但不同内容的误差不保证单调或同样可接受。配置正确、能加载和画面可接受是不同判断；首轮不预测 GPU 时间或整帧收益。

### 不符合时查什么

检查 build target/覆盖是否生效、Player 是否使用另一份纹理、所运行 APK 是否匹配、实际 API/格式支持、日志中的回退或解压，以及 Editor 与 Android 是否混记。

### 结果与完成条件

结果：**待执行**。第一对须有 APK 身份、设备/API/能力、实际格式、匹配画面；缺任一项不宣称 ASTC 真机对照完成。4×4/8×8 可后续补，不为凑矩阵预填结果。

## 7. 验证块三：Alpha 与通道消费者（V-C-A）

### 要验证的假设与最小场景

同一张 Base Map 的 Alpha 是否影响画面，取决于当前 Shader 路径；Opaque 不足以证明 Alpha 无用，因为它也可能承载 smoothness。复用测试板与两张 RGB 相同、A 不同的颜色纹理。

### 控制变量与候选

先固定 RGBA32、1024 和 Alpha Source=Input，隔离块编码误差。分别在两个固定材质状态中，只替换 `base-alpha` / `base-opaque`：先 `M_BaseFixed`，再 `M_BaseAlphaSmooth`。不将同时改变材质状态与纹理的一对称作单变量对照。

取得消费记录后，可另比较 Alpha Source=None/Input，保持其余项不变；压缩影响另回验证块二。不能因为把 A 全设为 255 就声称已去掉 Alpha 存储。

### 指标与工具

| 指标 | 入口 | 待观察项与限制 |
| --- | --- | --- |
| 输入确实只有 A 不同 | 源 PNG 独立解码、清单通道摘要 | RGB 逐字节相同已经在源准备阶段核对；这是源输入证据，非 GPU 结果 |
| 消费者与状态 | 材质 Inspector、实际关键词；Frame Debugger 的目标绘制/pass；公开 Package 的 Lit 输入逻辑 | 记录 Surface、Alpha Clip、Smoothness Source 和纹理槽；一次绘制不能覆盖所有动态材质/其他 pass |
| 正/负对照画面 | 固定光照/相机，极端 smoothness 正对照先可见，再拍两种输入 | 无差异可能是正对照没起作用，不能直接证明通道无用 |
| 实际表示 | 导入/Player 尺寸、格式、Mip，以及可取得的资源/对象记录 | 同尺寸、同 ASTC 块格式下不能预期去掉 Alpha 自动使每块字节减半 |

### 预期现象

`M_BaseFixed` 的两输入在已确认未使用 Base A 的当前路径中预计不改变可见输出；`M_BaseAlphaSmooth` 中改变 A 预计改变高光分布。二者都只是待观察假设，只有对照与实际状态一致才能解释。

通道表还要登记数据纹理的 R/A 和可选 G 消费者；本轮只执行 Base A 的最小反例，不据此宣布“所有通道已验证”或“可以全项目删除 Alpha”。共享贴图、其他材质/质量档/动态功能仍需另查。若随后要验证 G/B，应新增明确的单通道变体与有效正对照，不用现有素材名称代替证据。

### 不符合时查什么

先查 RGB 是否在压缩后受联动、引用/关键词是否正确、Alpha Source 是否保留输入、Smoothness 乘数和高光条件是否有效；再查其他 pass、材质状态或通道消费者，不将异常直接写成引擎错误。

### 结果与完成条件

结果：**待执行**。须保留两组固定材质状态各自的输入对照、状态与 pass 记录；只限定当前路径的结论，不给通用删除清单。

## 8. 记录、回填与下一执行批次

每个 case 都填：当前状态、输入与设置、实际表示、画面观察、附件、能支持的结论、未覆盖条件；未执行项写“待执行”，不能填预计数值。原始日志/大产物先放本地忽略目录，公开回填只保留可公开来源、必要设置和经检查的证据摘要。

| 回填位置 | 需要哪一类证据后才回填 |
| --- | --- |
| [成本总览](../content/performance/mobile-art-asset-cost-overview.md) | 同一输入沿导入、使用到观察的完整一条记录；不声称是已经制作好的法杖项目 |
| [存储基础](../content/rendering/unity-rendering-supp-a-mesh-texture-storage.md) | 配置、实际格式/Mip与理论载荷对照；其他占用单列 |
| [带宽文](../content/performance/gpu-opt-02-bandwidth.md) | 目标能力、实际格式与回退记录；没有 GPU 访问/时间证据就不补带宽收益 |
| [Shader 文](../content/performance/gpu-opt-03-shader.md) | Alpha/通道消费者与画面对照；不从本轮推导打包减少采样次数 |
| [资产规格](../content/performance/device-tier-asset-spec-texture-and-package.md) | 该素材/镜头的接受与不接受条件，不升级为通用预算 |

下一执行批次只做：建立独立场景和三个材质状态 → 用 RGBA32 完成尺寸第一对与 Alpha 正/负对照 → Android 构建并记录实际运行身份 → 加入 ASTC 6×6 第一对。场景/材质尚未创建、尚无 APK 或设备截图，不扩大到其他八类验证和新增正文。

## 9. 本轮准备阶段的实际记录

- 已只读核对本机 Editor 文件版本、URP 包与现有工程 manifest/lock；未修改现有私有实验工程，未启动其生成器。
- 已用 ADB 查询唯一授权真机的型号、OS/API 与物理尺寸；未安装/运行实验应用，GPU/API/格式能力仍待 Player 记录。
- 已生成原创三个 PNG、一个 OBJ 与源清单。源通道关系和文件完整性是准备阶段检查，不能登记为 V-T/V-C 运行通过。
- 三个 PNG 的 CRC/头部/独立解码、文件与通道 SHA256、RGB 相同而 Alpha 不同及全不透明输入检查通过；OBJ 源计数/摘要检查通过，重新生成的五个源输出逐字节一致。单张源颜色图目视检查只确认图案，不是压缩或设备画面对照。
- 本工作单、两个盘点与素材 README 的 73 处本地链接、引用标签、表格列数、围栏、空白与字符检查通过，范围内 `git diff --check` 通过。Hugo 完整本地构建退出码为 0、无 ERROR；这些仅确认文件与文档。
- 当前没有 Unity 导入、场景/材质、Android 构建、APK 安装/运行、实际采样/Mip、压缩画面或 CPU/GPU 时间结果。本轮未修改正文、提交或推送。

[texture-import]: https://docs.unity3d.com/2022.3/Documentation/Manual/class-TextureImporter.html
[platform-formats]: https://docs.unity3d.com/2022.3/Documentation/Manual/class-TextureImporterOverride.html
[platform-settings]: https://docs.unity3d.com/2022.3/Documentation/ScriptReference/TextureImporter.GetPlatformTextureSettings.html
[automatic-format]: https://docs.unity3d.com/2022.3/Documentation/ScriptReference/TextureImporter.GetAutomaticFormat.html
[texture-format]: https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Texture2D-format.html
[graphics-format]: https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Texture-graphicsFormat.html
[format-support]: https://docs.unity3d.com/2022.3/Documentation/ScriptReference/SystemInfo.IsFormatSupported.html
[runtime-size]: https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Profiling.Profiler.GetRuntimeMemorySizeLong.html
[urp-lit]: https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/lit-shader.html
[lit-input]: https://github.com/Unity-Technologies/Graphics/blob/2022.3/staging/Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl
