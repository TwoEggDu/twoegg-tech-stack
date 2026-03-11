# 求职定位

## 最新结论

把 `DP`、`PX`、`UnitySrcCode` 和整个 `E:\HT` 连起来看，你最适合主打的已经不是“单项目工具开发”，而是下面这条路线：

`Unity 客户端基础架构负责人 -> 工具链 / 构建发布 / 工程效率 / 平台接入治理 / 资源成本治理主轴 + 渲染性能 / 引擎理解副轴`

原因很直接：

- 你的项目证据集中在工具链、资源生产、补丁、SDK 平台接入、编译检查、构建发布和上线链路。
- `E:\HT` 里的 BuildFlow、Zhulong、ShanHai、Haotian 文档说明你已经在做跨项目平台化和方法论沉淀。
- `PX/DP` 里还能看到 URP 自定义 RenderFeature、离屏 VFX、Bloom/Blur、阴影、质量调优和运行时图形诊断的证据。
- 这条线比“纯管理型技术总监”更容易被代码、流程和文档直接证明，也比“单一工具开发”更有技术含量。

更完整的反推见：

- [docs/work-history-inference.md](work-history-inference.md)
- [docs/rendering-positioning.md](rendering-positioning.md)

## 一句话标签

最推荐的版本：

`资深 Unity 客户端基础架构工程师 / 负责人，长期负责工具链、资源生产、补丁、SDK 平台接入、Android/iOS 客户端打包、编译检查、构建发布、发布资产瘦身和上线流程，并经常协调美术、策划和程序推进关键事项落地，持续把项目经验沉淀为跨项目可复用的平台能力。`

如果想更偏基础架构一点，可以改成：

`偏工具链与渲染性能方向的客户端基础架构工程师/负责人，擅长围绕 Unity 统一 SDK 接入层、Android/iOS 多平台打包发布、资源导入、资产格式治理、发布资产瘦身、Bundle 差量更新、脚本编译、热更新体系和 URP 渲染扩展做工程化建设。`

## 你的 T 型能力结构

### 主轴：最强、最稳、最能卖钱的能力

- 工具链
- 构建发布
- 工程效率
- 第三方平台与 SDK 接入治理
- Android / iOS 多平台客户端打包发布
- 资源生产与热更新链路
- 发布资产瘦身与资源成本治理
- CI/CD 与交付稳定性
- 跨职能协同与项目推进

### 副轴：最能拉高技术含量的能力

- PX 项目的渲染与性能优化
- URP 自定义 RenderFeature / RenderPass
- 离屏 VFX、后处理、阴影和质量调优
- Unity 引擎源码与图形链路理解

这个结构比“我方向很多”强很多，因为它既保留了你的市场竞争力，也解释了你为什么比一般工具链工程师更偏基础架构。

## 从 E:\HT 反推出来的职业轨迹

### 第一阶段：项目内工具开发者

这一阶段的代表是 `PX`。

你做的是：

- 配表自动化
- 资源差分、Bundle 差量更新和补丁导入
- 自定义编译封装
- 资产格式检查与导入规范治理
- 发布资产瘦身与压缩策略落地
- UI / 资源导入规则
- SVN 协作工具
- MPQ 发布工具

这已经不是简单的编辑器扩展，而是典型的研发生产工具。

### 第二阶段：项目级交付链路负责人

这一阶段的代表是 `DP/TopHero`，再加上 `SGI`、`KOI` 的维护范围。

`E:\HT\docs\projects-registry.md` 已经把你的职责写得很清楚：

- CI/CD 流水线
- AssetBundle 打包
- 资源导入设置
- 资源验证检查
- 发布资产瘦身、资产格式、压缩策略和导入规则治理
- 构建流水线
- Android / iOS / 小程序多平台打包
- 部分自动化测试

这说明你已经从“工具开发”升级到“项目研发生产线负责人”，而且不仅接资源链路，也接 Android / iOS 这类客户端出包交付。
同时，你补充提到自己经常协调美术、策划和程序，确保项目进度。这说明你的职责已经不只是技术实现，还包括跨职能推进和节奏控制。

### 第三阶段：跨项目平台建设者

这是你现在最值钱的一层。

从 `BuildFlow -> Zhulong -> ShanHai` 的文档可以看出：

- 你在把项目脚本抽象成可复用的工作流和 Step
- 你在推动配置驱动、统一日志、上下文建模、可测试性和稳定性治理
- 你已经在拆分通用能力模块，例如 `Tiangong`、`Bixie`、`Zhuque`、`Kunlun`

这意味着你的包装可以升级成：

`跨项目工具链平台建设者`

### 第四阶段：渲染与性能优化参与者

这是你新增后最重要的一层增强。

从 `PX/DP` 可以看到：

- 自定义 `ScriptableRendererFeature` 和 `ScriptableRenderPass`
- 后处理和效果链路的定制
- 离屏 VFX 与 render scale 优化
- Bloom / Blur / SDF Shadow / Sky Atmosphere 等图形能力
- 图形质量参数和运行时诊断支持

这让你的定位不再只是“工具链负责人”，而是更接近：

`客户端基础架构负责人`

## 最适合冲击的岗位

### 第一优先级

- Unity 客户端基础架构负责人
- 游戏工具链负责人
- 客户端工程效率负责人
- 构建发布 / 资源热更新负责人
- 偏渲染与工具链方向的客户端主程

### 第二优先级

- 引擎工具开发负责人
- 引擎开发工程师（偏编辑器、构建、资源管线）
- 中台工具链开发主管
- 客户端技术负责人
- 客户端渲染性能优化负责人

### 不建议作为第一标题的岗位

- 技术总监
- 纯渲染引擎专家

前者需要更强的组织管理证据，后者则需要更纯、更深的图形领域可证明成果。你当前最强的是“基础架构 + 交付 + 渲染优化参与”组合。

## 项目证据

下面这些文件足够支撑你把自己包装成“客户端基础架构 / 工具链负责人”，而不是“写过几个编辑器脚本的人”。

### DP 项目

- `E:\HT\Projects\DP\Data\Tools\Jenkins\dp-package.jenkinsfile`
  说明你可以主打完整的服务端构建、制品归档、部署联动、通知反馈链路。
- `E:\HT\Projects\DP\TopHeroUnity\Assets\TEngine\Editor\ReleaseTools\ReleaseTools.cs`
  说明你熟悉 Unity 侧的资源构建工具，能做命令行打包、增量构建、StreamingAssets 落盘和发布目录整理。
- `E:\HT\Projects\DP\TopHeroUnity\Assets\TEngine\Editor\LubanTools\LubanTools.cs`
  说明你不是只做资源，还打通过配表工具到客户端工程的入口。
- `E:\HT\Projects\DP\TopHeroSLN\Code\DP.Tools\Program.cs` 
  说明你做过配置数据导出、Json/Bin 生成、多平台 MPQ 更新包构建。
- `E:\HT\Projects\DP\TopHeroUnity\Packages\CommonSDK\README.md`
- `E:\HT\Projects\DP\TopHeroUnity\Packages\CommonSDK\Facade\UnifiedSDK.cs`
- `E:\HT\Projects\DP\TopHeroUnity\Packages\CommonSDK\Facade\Adapters\USDK\MarketResearchUSDKAdapter.cs`
- `E:\HT\Projects\DP\TopHeroUnity\Assets\Firebase\Editor\AnalyticsDependencies.xml`
- `E:\HT\Projects\DP\TopHeroUnity\Assets\appsflyer\AppsFlyer.cs`
  说明你不只是把第三方 SDK 丢进项目，而是做过统一 SDK 接入层，围绕 USDK、Peapod、Firebase、AppsFlyer 等能力处理初始化配置、登录与账号、客服/问券、运营能力和 BI 埋点，并考虑 iOS/Android 多平台接入边界。
- `E:\HT\docs\projects-registry.md`
- `E:\HT\Projects\DP\TopHeroUnity\Assets\TEngine\Editor\ReleaseTools\ReleaseTools.cs`
- `E:\HT\Projects\PX\ProjectX\Assets\NFEditorTools\Editor\BuildTool\BuildUtility.cs`
- `E:\HT\Projects\PX\ProjectX\Assets\NFEditorTools\Editor\BuildTool\BuildCMDParse.cs`
- `E:\HT\Projects\PX\ProjectX\Assets\NFEditorTools\Editor\BuildTool\BuildUtility.Context.cs`
  说明你不只是做资源构建，还接过 Android APK/AAB、iOS IPA/Xcode 工程这类多平台客户端出包链路，覆盖构建目标切换、AAB 开关、SDK 开关、整包/资源包发布和命令行发布入口。
- `E:\HT\Projects\DP\TopHeroUnity\Assets\GameScripts\BuildToos\Editor\FairyGUI\FairyGUIAssetsEditor.cs` 
  说明你做过 UI 资产的格式检查、MipMap/sRGB/ReadWrite 规则治理，以及 iOS/Android 平台 ASTC 压缩格式控制。
- `E:\HT\Projects\DP\TopHeroUnity\Assets\ArtTools\MonsterBakingTool\Editor\TextureBaker.cs`
- `E:\HT\Projects\DP\TopHeroUnity\Assets\ArtTools\MonsterBakingTool\Editor\README_MaterialBakingTool.md`
- `E:\HT\Projects\DP\TopHeroUnity\Assets\ArtTools\MonsterBakingTool\MetallicRoughnessMap通道使用统计.md`
- `E:\HT\Projects\DP\TopHeroUnity\Assets\ArtTools\MonsterBakingTool\通道压缩技术方案_Voronoi编码.md`
  说明你不只是做导入规范，还深入做过发布资产瘦身，包括材质烘焙、贴图降采样、ASTC 压缩、通道压缩和贴图内存优化，把包体和运行时纹理成本一起纳入治理。
- `E:\HT\Projects\DP\TopHeroUnity\Assets\ArtTools\OffScreenVFX\OffScreenVFXFeature.cs`
- `E:\HT\Projects\DP\TopHeroUnity\Assets\ArtTools\OffScreenVFX\OffScreenVFXPass.cs`
  说明你在项目里接触过离屏特效渲染、render scale 和透明队列渲染优化。

### PX 项目

- `E:\HT\Projects\PX\ProjectX\Assets\NFEditorTools\Editor\ABDiff\NFBundlePatchUtil.cs` 
  说明你做过资源包差分、Block 级复用和补丁生成逻辑。
- `E:\HT\Projects\PX\ProjectX\Packages\GameFramework\GameFramework\NFExt\NFResource\BundleInfo\BundleDiffUtil.cs`
- `E:\HT\Projects\PX\ProjectX\Packages\GameFramework\GameFramework\NFExt\NFResource\BundleInfo\NFBundleInfo.cs`
- `E:\HT\Projects\PX\ProjectX\Assets\ThirdParty\BinaryPatch\Scripts\BinaryPatchUtility.cs`
  说明你不只是做补丁包比对，还参与过 Bundle 差量更新链路，覆盖 patch info 描述、旧块复用、补丁块写回、哈希校验，以及 bsdiff 二进制补丁能力接入。
- `E:\HT\Projects\PX\ProjectX\Assets\NFEditorTools\Editor\NFStudio\AssetPipeline\NFCustomMetaData.cs` 
- `E:\HT\Projects\PX\ProjectX\Assets\NFEditorTools\Editor\AssetPostProcess\ModelPostProcessor.cs` 
  说明你做过面向贴图和模型的资产格式检查与导入治理，覆盖 ASTC、StreamingMipMaps、GenerateMipMaps、sRGB、Read/Write、max size 和模型材质导入策略。
- `E:\HT\Projects\PX\ProjectX\Assets\NFEditorTools\Editor\BuildTool\BuildMenu.cs`
  说明你在项目里实际落地过面向发布资产的压缩策略切换，例如针对 Android 贴图批量设置 ASTC 10x10 等包体优化动作。
- `E:\HT\Projects\PX\ProjectX\Assets\Editor\YooExt\PackageComparator\PackageComparatorWindow.cs`
- `E:\HT\Projects\PX\ProjectX\Assets\Editor\YooExt\PackageImporter\PackageImporterWindow.cs`
  说明你在补丁包比对、导入、回归验证侧有直接落地能力。
- `E:\HT\Projects\PX\ProjectX\Assets\_GammaUIFix\GammaUIFix.cs`
  说明你碰过 URP 自定义渲染扩展与 UI / 场景颜色空间合成问题。
- `E:\HT\Projects\PX\ProjectX\Assets\ArtTools\Scripts\SkillEffectFeature.cs`
  说明你接触过效果层渲染控制和 RenderPass 注入。
- `E:\HT\Projects\PX\ProjectX\Assets\ArtTools\AtlasBloom\Src\AtlasBloomRenderFeature.cs`
- `E:\HT\Projects\PX\ProjectX\Assets\ArtTools\AtlasBlur\AtlasBlurRenderFeature.cs`
  说明你深入参与过项目级后处理和性能 / 效果平衡。
- `E:\HT\Projects\PX\ProjectX\Assets\ArtTools\ShadowsB\Runtime\SDFHeightShadowRenderPass.cs`
- `E:\HT\Projects\PX\ProjectX\Assets\ArtTools\APSkyAtmosphere\Runtime\SkyAtmosphereRendererPass.cs`
  说明你不是只会项目工具，还进入过更偏图形工程和计算链路的区域。

### E:\HT 平台化证据

- `E:\HT\docs\projects-registry.md`
  直接定义了你的项目维护范围和主要职责。
- `E:\HT\Projects\BuildFlow\docs\sdd\0001-buildflow-architecture-spec.md`
  明确写了 BuildFlow 是生产使用中的构建流水线工具集，落地于 `PX/TopHero`。
- `E:\HT\Projects\BuildFlow\docs\rfc\0001-pipeline-refactoring.md`
  说明你已经在思考从全局状态、重复逻辑、配置分散，升级到 Pipeline + DI + 配置统一的架构。
- `E:\HT\Projects\Zhulong\docs\guides\migration-from-buildflow.md`
  说明你在推动从“每项目硬编码脚本”迁移到“配置驱动平台”。
- `E:\HT\Projects\ShanHai\docs\tiangong\sdd\0001-tiangongci-specification.md`
  说明你正在把 Unity 内部构建入口和 Pipeline 机制产品化。
- `E:\HT\Projects\ShanHai\docs\bixie\README.md`
  说明你在把编译检查这类经验沉淀成独立能力模块。

## 简历主叙事

简历首页不要写成“Unity 客户端开发，做过一些编辑器工具”。

更建议写成：

`长期负责大型 Unity 项目的客户端基础架构建设，覆盖工具链、统一 SDK 接入、配表与资源生产、补丁包差分、编译检查、构建发布、发布资产瘦身、制品归档、部署联动和研发流程规范，并深度参与项目级渲染与性能优化。`

再往下接一句：

`具备从 Unity 编辑器侧工具开发延伸到项目级交付链路，再进一步沉淀为跨项目工具链平台，并结合 URP 渲染扩展与运行时性能优化推动项目整体工程能力提升的完整经验。`

## 1 分钟自我介绍

我这几年最核心的方向其实不是传统业务功能开发，而是 Unity 项目的客户端基础架构建设。我的主线一直是工具链和工程效率，覆盖统一 SDK 接入层、配表导出、资源导入规则、资产格式检查、发布资产瘦身、压缩与导入规范治理、补丁包处理、Bundle 差量更新、编译检查、Jenkins 打包、制品归档和上线流程。同时我经常要协调美术、策划和程序，推动资源规范、版本内容和发布时间点对齐。另一方面，我也深入参与过 PX 这类项目的渲染与性能优化，接触过 URP 自定义 RenderFeature、离屏 VFX、后处理、阴影和质量调优。另外在《天谕》项目里，我入职 1 个月内从 0 到 1 学习 Electron 并做出了 Windows 启动器，这让我更倾向于把学习能力理解成“快速补齐陌生技术栈并完成关键交付”。所以我现在更希望找的是偏客户端基础架构、工具链、工程效率，或者带渲染优化能力的主程/负责人岗位。

## 简历可用 Bullet

- 负责大型 Unity 项目的客户端基础架构建设，覆盖配表导出、资源构建、资产格式检查、补丁包处理、编译检查、构建发布与上线流程串联。
- 设计并维护 Unity 侧资源构建工具，支持命令行打包、增量构建、StreamingAssets 组织及多平台发布目录整理。
- 参与并落地 Jenkins 打包与部署流程，打通 SVN/Git 分支切换、子模块同步、制品归档、部署触发与结果通知。
- 构建资源包差分与补丁包分析工具，支持版本差异定位、补丁内容校验和导入验证。
- 参与 Bundle 差量更新链路建设，围绕 Block 复用、PatchInfo 描述、补丁块回填和哈希校验降低更新下载成本。
- 负责第三方平台与 SDK 接入落地，围绕统一接入层封装 USDK / Peapod / Firebase / AppsFlyer 等能力，处理初始化配置、登录与账号、客服/运营能力、BI 埋点及多平台集成。
- 负责 Android / iOS 客户端出包与提测流程，覆盖 BuildTarget 切换、AAB 开关、资源版本、SDK 开关和命令行发布入口。
- 建立贴图和模型的资产格式检查与导入规则，覆盖 ASTC、MipMap、sRGB、Read/Write、max size 和材质导入策略，将资源问题前置到导入阶段。
- 负责发布资产瘦身与资源成本治理，围绕贴图压缩格式、Max Size、MipMap/StreamingMipMaps、材质烘焙、贴图降采样和通道压缩降低包体与运行时纹理内存。
- 参与特效性能检查器与资源检查规则建设，覆盖特效 Layer 规范、粒子 Mesh 丢失检查、最大粒子数检查、粒子系统依赖 FBX 可读写检查和部分自动修复能力，把特效问题前置到发布前资源门禁。
- 经常协调美术、策划和程序围绕资源规范、版本内容、问题回归和发布时间点达成一致，推动关键事项按节奏落地。
- 深度参与 PX 项目的渲染与性能优化，接触 URP 自定义 RenderFeature、离屏 VFX、后处理、阴影与图形质量调优。
- 在《天谕》项目中入职 1 个月内从 0 到 1 学习 Electron 并完成 Windows 启动器开发，证明自己具备快速补齐陌生技术栈并交付关键工具的能力。
- 推动项目脚本向跨项目平台迁移，沉淀配置驱动的工作流、统一日志和通用检测能力。
- 持续沉淀构建环境、引擎更新、音频接入和图形诊断等 SOP 与工具，提升团队可复制性和新成员上手效率。

## 面试观点

- 工具链岗位的价值不在“写工具”，而在“缩短团队从改动到可发布的距离”。
- 真正能承担负责人价值的人，不只是会做系统，还要能协调美术、策划和程序，让规范、内容和发布时间点对齐。
- 高价值客户端基础架构，不只是交付效率，还包括运行时性能和渲染成本治理。
- 大型 Unity 项目里，真正昂贵的不是一个功能点的开发时间，而是整个研发链路和运行时表现的不确定性。
- 渲染优化不是只调参数，真正值钱的是能改渲染接入点、质量策略和资源约束方式。
- 高价值工具链的上限，不是一个项目里的脚本集合，而是跨项目可复用的平台能力。
- SDK 接入真正值钱的不是“接进去”，而是把登录、客服、埋点和平台配置收敛成统一接入层，避免业务代码和平台能力强耦合。
- Android/iOS 出包的难点不是点一次 Build，而是把构建目标、资源版本、SDK 开关和发布环境放进同一条可重复执行的链路。
- 热更新体系不是越新越好，关键是你有没有把资源版本、补丁校验、导入验证和回滚路径设计清楚。
- 差量更新真正难的不是生成 patch，而是保证旧包复用、补丁应用和结果校验整条链路可回滚、可验证。
- 资源格式检查的价值，不是多拦几张图，而是把包体、内存和协作问题前置到资产导入阶段。
- 减小发布资产大小不是单点压缩，而是资源导入规则、材质表达、压缩格式和发布策略的组合治理。
- 做过上线流程的人，技术判断会自然更偏稳健；做过渲染优化的人，会更理解运行时成本。两者合起来才更像基础架构负责人。
- 学习能力如果不能落到陌生技术栈的实际交付上，就很难形成市场说服力。像《天谕》里 0 基础 1 个月做出 Electron 启动器，这种证据才有价值。

## 待补充的关键量化指标

你现在最缺的不是故事，而是数字。至少补下面这些：

- 打包耗时从多少降到多少
- 资源构建失败率下降多少
- 补丁包体积减少多少
- 差量补丁下载体积减少多少
- 发布资产大小减少多少
- 跨部门问题闭环时效提升多少
- 策划/美术日常导出或提交流程节省多少人时
- 上线前人工检查项减少多少
- 新同学环境搭建时间从多少降到多少
- 版本事故或资源遗漏问题减少多少
- PX 渲染优化里帧率、GPU 时间、DrawCall、后处理成本或特效成本改善多少
- 支撑过多大团队规模、多大资源量级、多频繁版本节奏
- 从 0 到 1 补齐陌生技术栈并交付首版的实际周期

## 天谕经历怎么用

你提到自己参与过《天谕》这样的成功项目，而且补充了一条很关键的证据：入职 1 个月内，在 0 基础的情况下用 Electron 做出了 Windows 启动器。

这条经历真正值钱的点，不是“我待过成功项目”，而是：

- 我能在成熟项目环境里快速补齐陌生技术栈，并把关键工具真正交付出来。
- 我不是只会沿着熟悉的 Unity 编辑器边界工作，也具备跨到桌面客户端和启动器这类交付入口层的能力。
- 我参与过成熟项目的上线与长期运维环境，知道真正影响团队效率、版本稳定性和运行时表现的环节在哪。

简历里更好的写法是：

- 在《天谕》项目中入职 1 个月内从 0 到 1 学习 Electron，并完成 Windows 启动器开发，支撑项目桌面端启动入口建设。

面试里更好的讲法是：

- 我不把学习能力理解成看过多少资料，而是能不能在业务需要的时候，快速补齐陌生技术栈并把东西交付出来。比如在《天谕》入职一个月，我从 0 基础上手 Electron，做出了 Windows 启动器。

需要注意的是，这条目前是基于你的补充信息，不是 `E:\HT` 里的本地仓库证据。后面如果你能补一些代码截图、职责边界或当时的上线背景，这条会更强。