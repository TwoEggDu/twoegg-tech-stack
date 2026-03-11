# 证据映射

| 能力维度 | 证据文件 | 能证明什么 | 面试时怎么讲 | 还缺什么 |
| --- | --- | --- | --- | --- |
| 构建与发布 | `E:\HT\Projects\DP\Data\Tools\Jenkins\dp-package.jenkinsfile` | 你不只是写客户端工具，而是能接住服务器制品打包、归档、部署触发 | 我负责过从代码同步、分支切换到制品归档和部署联动的完整打包链路 | 打包耗时、失败率、上线频次 |
| Unity 资源构建 | `E:\HT\Projects\DP\TopHeroUnity\Assets\TEngine\Editor\ReleaseTools\ReleaseTools.cs` | 你熟悉命令行打包、增量构建、StreamingAssets 拷贝与版本输出 | 我关注的是资源构建和发布结果，而不是只点 Unity 菜单 | 增量构建节省时间、包体变化 |
| 配表与数据导出 | `E:\HT\Projects\DP\TopHeroUnity\Assets\TEngine\Editor\LubanTools\LubanTools.cs` `E:\HT\Projects\DP\TopHeroSLN\Code\DP.Tools\Program.cs` | 你把数据生产链打通到了工程 | 我做的是“配置到产物”的自动化，而不是孤立的编辑器工具 | 导表频率、减少的人为错误 |
| 第三方平台与 SDK 接入 | `E:\HT\Projects\DP\TopHeroUnity\Packages\CommonSDK\README.md` `E:\HT\Projects\DP\TopHeroUnity\Packages\CommonSDK\Facade\UnifiedSDK.cs` `E:\HT\Projects\DP\TopHeroUnity\Packages\CommonSDK\Facade\Adapters\USDK\MarketResearchUSDKAdapter.cs` `E:\HT\Projects\DP\TopHeroUnity\Assets\Firebase\Editor\AnalyticsDependencies.xml` `E:\HT\Projects\DP\TopHeroUnity\Assets\appsflyer\AppsFlyer.cs` | 你不只是接过 SDK 包，而是做过统一接入层，覆盖登录、账号、客服、问券、运营能力和 BI 埋点，并考虑多平台初始化与依赖边界 | 我做的不是把 SDK 丢进项目里，而是把登录、客服、埋点和平台配置收敛成统一接入层，让业务和平台能力解耦 | 接入周期、初始化成功率、登录成功率、平台问题工单量 |
| Android / iOS 客户端打包发布 | `E:\HT\docs\projects-registry.md` `E:\HT\Projects\DP\TopHeroUnity\Assets\TEngine\Editor\ReleaseTools\ReleaseTools.cs` `E:\HT\Projects\PX\ProjectX\Assets\NFEditorTools\Editor\BuildTool\BuildUtility.cs` `E:\HT\Projects\PX\ProjectX\Assets\NFEditorTools\Editor\BuildTool\BuildCMDParse.cs` `E:\HT\Projects\PX\ProjectX\Assets\NFEditorTools\Editor\BuildTool\BuildUtility.Context.cs` | 你不只是会点编辑器出包，而是处理过 Android / iOS 的多平台构建目标、AAB 开关、SDK 开关、命令行发布入口和整包发布流程 | 我做过 Android / iOS 客户端出包，重点不是生成包本身，而是把构建目标、资源版本、SDK 开关和发布方式收敛成稳定的多平台链路 | 出包耗时、失败率、提测频率、渠道数量 |
| 多平台补丁包构建 | `E:\HT\Projects\DP\TopHeroSLN\Code\DP.Tools\Program.cs` `E:\HT\Projects\PX\XEngineProject\X\X.Test\Program.cs` | 你做过 PC/Android/iOS 或多平台的补丁与更新包生产 | 我做的是面向发布的资源包生产，不是单平台脚本 | 平台数、补丁体积、构建时间 |
| 补丁差分与版本对比 | `E:\HT\Projects\PX\ProjectX\Assets\NFEditorTools\Editor\ABDiff\NFBundlePatchUtil.cs` `E:\HT\Projects\PX\ProjectX\Assets\Editor\YooExt\PackageComparator\PackageComparatorWindow.cs` | 你理解资源版本差异和补丁包治理 | 我不仅会生成包，也会做版本差异识别和补丁校验 | 补丁体积优化数据 |
| Bundle 差量更新 / 二进制差分补丁 | `E:\HT\Projects\PX\ProjectX\Assets\NFEditorTools\Editor\ABDiff\NFBundlePatchUtil.cs` `E:\HT\Projects\PX\ProjectX\Packages\GameFramework\GameFramework\NFExt\NFResource\BundleInfo\BundleDiffUtil.cs` `E:\HT\Projects\PX\ProjectX\Packages\GameFramework\GameFramework\NFExt\NFResource\BundleInfo\NFBundleInfo.cs` `E:\HT\Projects\PX\ProjectX\Assets\ThirdParty\BinaryPatch\Scripts\BinaryPatchUtility.cs` | 你不只是做版本比对，还理解 patch 描述、旧块复用、补丁合成和结果校验的完整链路 | 我参与过 Bundle 差量更新，目标不是简单替换整包，而是尽量复用旧 Bundle 中未变化的块，只传输 patch 数据并做 hash 校验 | 差量补丁体积、下载成本下降比例、应用成功率 |
| 补丁导入与验证 | `E:\HT\Projects\PX\ProjectX\Assets\Editor\YooExt\PackageImporter\PackageImporterWindow.cs` | 你考虑的是发布后验证，不是只看构建成功 | 我做过补丁导入与回归辅助工具，帮助定位版本问题 | 导入验证缩短多少时间 |
| 数据表代码生成 | `E:\HT\Projects\PX\ProjectX\Assets\NFEditorTools\Editor\DataTableGenerator\DataTableGenerator.cs` | 你做过结构化的数据到代码产物生成 | 我做过数据定义、代码模板和二进制产物生成链路 | 表数量、生成耗时、错误率 |
| 自定义编译能力 | `E:\HT\Projects\PX\ProjectX\Assets\NFEditorTools\Editor\CodeCompile\CompileCSharpUtil.cs` | 你对脚本编译体系有深入理解 | 我关注脚本编译链，而不只是 Unity 默认编译行为 | 编译时间、使用场景 |
| 资产格式检查与导入治理 | `E:\HT\Projects\DP\TopHeroUnity\Assets\GameScripts\BuildToos\Editor\FairyGUI\FairyGUIAssetsEditor.cs` `E:\HT\Projects\PX\ProjectX\Assets\NFEditorTools\Editor\NFStudio\AssetPipeline\NFCustomMetaData.cs` `E:\HT\Projects\PX\ProjectX\Assets\NFEditorTools\Editor\AssetPostProcess\ModelPostProcessor.cs` | 你不只是做资源导入，还在治理贴图、模型等资产的导入规则、平台压缩格式和可读性开关 | 我做过资产格式检查和导入规范落地，覆盖 ASTC、MipMap、sRGB、Read/Write、max size、模型材质导入等规则，目标是把资源问题前置到导入阶段 | 规则覆盖率、误配置减少量、资源内存收益 |
| 发布资产瘦身与包体优化 | `E:\HT\Projects\DP\TopHeroUnity\Assets\GameScripts\BuildToos\Editor\FairyGUI\FairyGUIAssetsEditor.cs` `E:\HT\Projects\PX\ProjectX\Assets\NFEditorTools\Editor\BuildTool\BuildMenu.cs` `E:\HT\Projects\DP\TopHeroUnity\Assets\ArtTools\MonsterBakingTool\Editor\TextureBaker.cs` `E:\HT\Projects\DP\TopHeroUnity\Assets\ArtTools\MonsterBakingTool\Editor\README_MaterialBakingTool.md` `E:\HT\Projects\DP\TopHeroUnity\Assets\ArtTools\MonsterBakingTool\MetallicRoughnessMap通道使用统计.md` | 你不只是校验资源，还主动治理发布资产大小和运行时纹理内存成本 | 我做过贴图压缩格式、Max Size、MipMap、材质烘焙、贴图降采样和通道压缩，把发布包体和运行时内存一起优化 | 包体下降数据、下载体积、纹理内存节省比例 |
| 特效性能检查器 / 特效资源门禁 | `E:\HT\Projects\PX\ProjectX\Assets\NFEditorTools\Editor\NFStudio\ResProcess\ResourceReport.VFX.cs` `E:\HT\Projects\PX\ProjectX\Assets\NFEditorTools\Editor\NFStudio\ResCheck\CheckNodes\Prefab\Prefab_MaxParticlesCheck.cs` `E:\HT\Projects\PX\ProjectX\Assets\NFEditorTools\Editor\NFStudio\ResCheck\CheckNodes\Prefab\Prefab_ParticleMeshMissing.cs` `E:\HT\Projects\PX\ProjectX\Assets\NFEditorTools\Editor\NFStudio\ResCheck\CheckNodes\Prefab\PrefabHelper.cs` `E:\HT\docs\reference\PX项目-资源检查系统分析.md` | 你不只是做特效优化，而是把特效 Layer、粒子 Mesh 丢失、最大粒子数、粒子系统 Mesh 依赖 FBX 可读写等问题做成发布前资源检查和部分自动修复能力 | 我做过特效性能检查器，本质不是看特效好不好看，而是把高成本和高风险的特效问题前置到资源门禁里，避免它们拖到联调、提测或线上才暴露 | 检查覆盖资源量、拦截问题数、自动修复率、减少返工时长 |
| UI 资源导入规范 | `E:\HT\Projects\PX\ProjectX\Assets\NFEditorTools\Editor\FairyGUIAssetsEditor.cs` | 你会做资源导入规则、压缩策略和批处理 | 我在工具链里考虑资源规范与平台落地，不只写 UI 功能 | 资源数量、压缩收益 |
| 版本协作与提交流程 | `E:\HT\Projects\PX\ProjectX\Assets\NFEditorTools\SvnTools\PXSvnExt\WiseSVNExt.cs` | 你把依赖关系和版本控制流程做进了工具 | 我做过依赖感知提交，目标是减少漏提和协作事故 | 资源协作事故下降数据 |
| 流程文档与团队规范 | E:\HT\Projects\PX\Doc\Unity\引擎更新SOP.pdf E:\HT\Projects\PX\Doc\Unity\本地构建环境搭建.pdf E:\HT\Projects\PX\Doc\Artist\Wwise\* | 你不是单点开发者，而是会沉淀团队规范 | 我做工具时会同步把流程变成团队可执行的 SOP | 新人上手时间、问题单减少量 |
| 快速跨栈学习与桌面启动器交付 | 结合你的补充信息：《天谕》项目 / Electron Windows 启动器（当前无 E:\HT 本地仓库证据） | 你不只是会在熟悉栈里做 Unity 工具，也能在成熟项目环境里快速补齐陌生技术栈并交付关键工具 | 我不把学习能力理解成看过多少资料，而是能不能在业务需要的时候快速补齐陌生技术栈并把东西交付出来。在《天谕》里我入职 1 个月，从 0 基础上手 Electron，做出了 Windows 启动器 | 启动器职责边界、上线背景、是否独立负责、版本迭代次数 |
| 跨职能协同与项目推进 | `E:\HT\docs\projects-registry.md` `E:\HT\Projects\DP\TopHeroUnity\Assets\TEngine\Editor\LubanTools\LubanTools.cs` `E:\HT\Projects\DP\TopHeroUnity\Assets\GameScripts\BuildToos\Editor\FairyGUI\FairyGUIAssetsEditor.cs` `E:\HT\Projects\PX\Doc\Unity\本地构建环境搭建.pdf` | 结合你的补充信息，可以证明你不是只写工具，还长期处在美术、策划、程序的交叉点推动项目进度 | 我经常协调美术、策划和程序，目标不是开会本身，而是让资源规范、内容边界、发布时间点和问题回归真正落地 | 具体案例、团队规模、一次典型推进闭环 |

## 证据怎么转成更高薪岗位叙事

### 如果面试官是技术负责人

重点讲：

- 工具链如何减少研发不确定性
- 你如何理解 Unity 资源与编译边界
- 你如何处理补丁、热更新和多版本问题
- 你如何把登录、客服、BI 和平台配置收敛成统一接入层，而不是散落在业务代码里
- 你如何把 Android / iOS 的构建目标、AAB、SDK 开关和资源版本整合进同一条打包链路
- 你如何权衡全量更新、Bundle 差量更新和补丁校验复杂度
- 你如何把资产格式问题前置到导入管线，而不是等线上或包体阶段再暴露
- 你如何同时治理发布包体和运行时纹理内存，而不是只做单点压缩
- 你如何在陌生技术栈里快速补齐关键知识，并把桌面启动器这类入口层工具真正交付出来

### 如果面试官是业务负责人

重点讲：

- 你做的不是工具，而是项目生产效率
- 你能让版本更稳、更快上线
- 你能直接降低客户端下载体积和更新成本
- 你能降低第三方平台接入和版本合规变更带来的返工成本
- 你能降低多平台提测和出包过程中的人工切换成本
- 你能减少跨岗位协作成本
- 你能降低资源错误、压缩不一致和导入配置漂移带来的返工
- 你能把包体和资源成本控制成可持续的工程流程，而不是版本末期突击优化
- 你能推动多角色在同一个版本目标下对齐，而不是只交付自己的模块
- 你能在新业务需要的时候快速进入陌生栈并把关键交付补出来

### 如果面试官是 HR

重点讲：

- 你负责的是完整链路，不是单点模块
- 你做过成功上线项目
- 你具备主程和技术负责人的成长潜力
- 你不仅懂代码，也懂资源规范、平台接入、协作流程和交付稳定性
