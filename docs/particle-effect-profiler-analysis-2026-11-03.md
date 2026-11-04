# ParticleEffectProfiler：源码分析与特效测试准备

> 2026-11-03。已下载并静态阅读公开源码；未打开 Unity，未执行导入、编译、播放、真机采样或性能对照。本文是写作准备材料，篇级目录唯一维护在 [Unity 特效测试系列规划](unity-vfx-testing-series-plan.md)。

## 1. 本轮结论与版本身份

这个仓库适合作为特效测量工具的教学切入点：代码量小，能够沿编辑器入口追到采集、统计与显示。最值得解释的是每个数字的生成过程，以及它支持什么工程判断。现有实现不能直接承担现代项目的真机性能验收。

| 项目 | 本次核对结果 |
| --- | --- |
| 上游 | <https://github.com/sunbrando/ParticleEffectProfiler> |
| 本地副本 | `E:\workspace\ParticleEffectProfiler`，与文章仓库并列的独立 Git 仓库 |
| 固定提交 | `1a32eac25ae52ee2ad982fc3681432014b590ba3` |
| 提交日期及说明 | 2022-09-16，`修复：gamma 空间下计算错误的问题`；指本次取得的 HEAD，不据此推断作者后续活动 |
| 工程版本 | `ProjectSettings/ProjectVersion.txt`：Unity `2017.4.10f1` |
| 渲染配置 | `GraphicsSettings.asset` 中自定义渲染管线引用为空；采集实现使用 `Camera.SetReplacementShader` |
| README 声称支持 | Unity 5.x、2017.x、2019.x；本轮没有复验这些版本，也没有验证 Unity 2022 / Unity 6 / URP / HDRP |
| 许可证 | MIT，Copyright (c) 2019 sunbrando；分发源码或其实质部分时保留版权与许可声明 |
| 示例 | `Assets/ParticleEffectProfiler/Examples/EffectEvla.unity`、`Effect/skillfx1_5.prefab` |
| 修改与运行 | 上游副本保持干净；未升级、未改造、未运行 |

下文源码链接均固定到该提交。README 对历史 UWA 集成的描述只属于上游介绍，不代表本轮验证了当前 UWA 产品能力。

## 2. 实际执行链

```text
GameObject/特效/测试
  → 检查选中对象下有 ParticleSystemRenderer
  → 请求进入 Play，并添加 ParticleEffectScript
  → Awake：目标帧率设为 30，取得 Camera.main
  → EffectEvla：替换相机 Shader，准备 512 × 512 RenderTexture
  → Start：缓存粒子系统，反射取得粒子统计方法
  → 每次 LateUpdate
      → 统计粒子数量
      → 相机额外 Render → ReadPixels → GetPixels → 遍历像素
      → 累加覆盖像素与估算绘制次数
      → 写粒子数、DrawCall、累计 Overdraw 曲线
  → 自定义 Inspector / Scene GUI 显示规则提示和数据
```

入口见 [TestParticleEffect.cs](https://github.com/sunbrando/ParticleEffectProfiler/blob/1a32eac25ae52ee2ad982fc3681432014b590ba3/Assets/ParticleEffectProfiler/Tools/Scripts/Editor/TestParticleEffect.cs)，调度见 [ParticleEffectScript.cs:35–116](https://github.com/sunbrando/ParticleEffectProfiler/blob/1a32eac25ae52ee2ad982fc3681432014b590ba3/Assets/ParticleEffectProfiler/Tools/Scripts/ParticleEffectScript.cs#L35-L116)，显示见 [MyParticleEffectUI.cs](https://github.com/sunbrando/ParticleEffectProfiler/blob/1a32eac25ae52ee2ad982fc3681432014b590ba3/Assets/ParticleEffectProfiler/Tools/Scripts/Editor/MyParticleEffectUI.cs)。

目标对象只约束部分资源和粒子枚举。Overdraw 来自主相机，DrawCall 来自编辑器全局统计，源码没有把这两项自动隔离到所选对象。示例相机的 Culling Mask 为全部层；把工具直接放进复杂场景时，不能把结果自动归因给目标特效。

## 3. 指标名与实际口径

| 面板或曲线 | 源码实际做什么 | 能支持的判断与边界 |
| --- | --- | --- |
| 贴图内存、贴图数量 | 枚举子级 `ParticleSystemRenderer(true)`，取每个 `sharedMaterial.mainTexture`，按 Texture 对象去重，反射调用 `TextureUtil.GetStorageMemorySize` | 这是所枚举主贴图的存储大小统计。没有枚举全部纹理属性、多材质槽、Trail 材质，也不含 Mesh、粒子缓冲等；不等于整个特效运行内存或目标设备显存 |
| 粒子系统数量 | `GetComponentsInChildren<ParticleSystem>(true)` | 层级组件数，包含 inactive 对象；不等于当前正在发射或可见的系统数 |
| 粒子数量 | Start 缓存默认子级枚举结果，随后反射内部 `CalculateEffectUIData`；旧版本分支使用 `CountSubEmitterParticles` | 需要校准子发射器、inactive 子级与动态增删的统计范围；不能未经实验判定有无重复计数，更不是 VFX Graph 粒子计数器 |
| DrawCall | `UnityStats.batches / 2`，整数结果；作者注释用额外采样渲染解释除以二 | 没有对象归因或空场景扣除，也没有证明两个渲染路径的批次完全相同。多相机、额外物体和替换材质都需要对照核验 |
| 原填充像素点 | 512 × 512 采样图中非黑像素数，跨有效采样帧累加后求整数平均 | 相机和采样分辨率下的覆盖计数；只有背景、对象隔离成立时才能归因于该特效 |
| 实际填充像素点 | 根据替换图绿色通道反推出每像素绘制次数，再累加并求整数平均 | 替换渲染的估算值，不是硬件 Fragment Invocation 计数，也没有保留原 Shader 的完整语义 |
| Overdraw | 两个累计平均值相除，使用整数除法 | 是截至当前的累计统计量；曲线点不是该帧独立的 Overdraw，尖峰会被稀释 |
| 自动剔除提示 | 手写模块检查；曲线合法性辅助函数固定返回 false | 只能作为有限的规则提示，不能当作当前引擎完整剔除判定或实际节省耗时的证据 |

资源、DrawCall 和规则入口见 [GetParticleEffectData.cs](https://github.com/sunbrando/ParticleEffectProfiler/blob/1a32eac25ae52ee2ad982fc3681432014b590ba3/Assets/ParticleEffectProfiler/Tools/Scripts/GetParticleEffectData.cs)。该文件没有调用导入的 `Profiler` API 测量整个特效运行内存；函数名 `GetRuntimeMemorySize` 不应代替实现作为口径证据。

红绿阈值也不是行业预算：代码内有贴图约 1000 KiB、主贴图数 5、系统数 5、DrawCall 10、粒子数 50、Overdraw 4 等硬编码值。正式验收须按设备、镜头、同屏数量、品质和玩法职责重新定义。

## 4. Overdraw 应怎样解释

### 4.1 替换渲染测到的是另一套绘制

[OverDraw.shader](https://github.com/sunbrando/ParticleEffectProfiler/blob/1a32eac25ae52ee2ad982fc3681432014b590ba3/Assets/ParticleEffectProfiler/Tools/Shaders/OverDraw.shader) 使用 `ZWrite Off`、`ZTest Always`、`Blend One One`，每次输出固定颜色 `(0.1, 0.04, 0.02, 0)`。它没有 UV、纹理采样、原材质 Alpha / clip，也没有原来的顶点动画逻辑。

因此结果更适合描述“此替换规则下的几何覆盖与重叠”。它不直接代表原材质的可见轮廓、深度遮挡、Shader 复杂度、带宽或 GPU 耗时。同样的几何体换成更贵的 Shader，计数可能不变；Alpha 更透明也不保证原绘制成本更低。

README 用 Mipmap 解释远距离有效像素减少，不能直接照抄为当前替换统计的机制：该替换 Shader 根本没有采样原贴图。相机距离改变投影面积是需要单独控制的变量。

### 4.2 分母与时间窗口决定结论

令有效帧中非黑像素数为 `C_t`，反解绘制次数之和为 `F_t`，累计有效帧数为 `N`。源码计算：

```text
平均覆盖像素 = floor(ΣC_t / N)
平均估算填充 = floor(ΣF_t / N)
显示 Overdraw = floor(平均估算填充 / 平均覆盖像素)
```

覆盖为零且填充为零的帧会被跳过。分母是覆盖像素，不是全部屏幕像素；也不是先逐帧求比值再做算术平均。见 [SingleEffectEvla.cs:28–37](https://github.com/sunbrando/ParticleEffectProfiler/blob/1a32eac25ae52ee2ad982fc3681432014b590ba3/Assets/ParticleEffectProfiler/Tools/Scripts/SingleEffectEvla.cs#L28-L37) 与 [EffectEvlaData.cs:17–46](https://github.com/sunbrando/ParticleEffectProfiler/blob/1a32eac25ae52ee2ad982fc3681432014b590ba3/Assets/ParticleEffectProfiler/Tools/Scripts/EffectEvlaData.cs#L17-L46)。

教学算例（不是实验结果）：连续两帧都覆盖 100 个像素，估算填充分别为 100 和 1000，第二个曲线点是 `550 / 100 → 5`，而该帧独立比值是 10。若只看曲线，会低估这个时点的突发重叠。

后续应同时记录覆盖率 `C_t / (W × H)`、覆盖区平均层数 `F_t / C_t` 和全采样图平均填充 `F_t / (W × H)`，再保留峰值及时间位置。小范围十层与大范围十层的覆盖区平均层数相同，总填充量却不同。

### 4.3 颜色编码的范围需要校准

[EffectEvla.cs:125–127](https://github.com/sunbrando/ParticleEffectProfiler/blob/1a32eac25ae52ee2ad982fc3681432014b590ba3/Assets/ParticleEffectProfiler/Tools/Scripts/EffectEvla.cs#L125-L127) 使用 `Convert.ToInt32(g / 0.039)`，Shader 每层绿色输出为 0.04。ARGB32 通道有量化和饱和边界，不能无限累加；当读回绿色达到 1 时，公式最多给出约 26。这里是源码公式的数值边界推导，实际在哪一层开始偏离仍需在对应颜色空间和图形 API 下校准。

先用已知层数的同面积几何体验证 1、2、4、8、16、32 层，再比较 Gamma / Linear；不能把上游修复提交当作所有环境均准确的证明。

## 5. 测量工具自身会改变什么

[采集路径](https://github.com/sunbrando/ParticleEffectProfiler/blob/1a32eac25ae52ee2ad982fc3681432014b590ba3/Assets/ParticleEffectProfiler/Tools/Scripts/EffectEvla.cs#L70-L87) 每次会额外渲染、新建 Texture2D、ReadPixels、GetPixels、遍历 262144 个像素、销毁纹理并释放 RT。它有额外 CPU / GPU 工作、分配和同步读回风险。影响幅度未测量。

这给专栏一个必要的实验分工：

- **覆盖诊断运行**：开启替换绘制，研究几何覆盖、编码误差和相机条件。
- **真实耗时运行**：恢复原材质与原渲染路径，关闭这套采集，以独立 Profiler / GPU 工具在目标 Player 中取证。
- **工具扰动对照**：固定任务，比较工具开关及必要的分阶段开销；明确观察对象发生了哪些变化。

曲线的“3 秒”实际是最多 90 次更新：X 轴是样本序号，`Application.targetFrameRate = 30` 不保证实际达到 30 FPS。非循环曲线填满后，`LateUpdate` 中的采集与累计仍继续；循环模式仅滚动曲线点，没有把底层累计 Overdraw 重置为滑动窗口。见 [ParticleEffectCurve.cs](https://github.com/sunbrando/ParticleEffectProfiler/blob/1a32eac25ae52ee2ad982fc3681432014b590ba3/Assets/ParticleEffectProfiler/Tools/Scripts/ParticleEffectCurve.cs)。

## 6. 接入与改造时优先解决的问题

以下是源码暴露的接入风险和改造建议，不是本轮已经重现的故障：

1. **明确测试环境**：先固定 Unity 精确版本、渲染管线及包版本。内置管线替换相机方案不能直接认定兼容 URP / HDRP；现代管线采集应独立设计并验证。
2. **隔离测试对象**：独立场景或专用相机 / Layer，显式使用黑色清屏，固定构图、分辨率及可见对象集合；不要依赖当前主场景恰好干净。
3. **保护运行状态**：检查 selection、MainCamera、Shader 和反射方法是否可用；用异常安全的清理流程恢复相机、targetTexture 与目标帧率。现实现把 targetTexture 设回 null，而非恢复原引用，未见完整恢复生命周期。
4. **修正数据模型**：保存真实时间戳和每帧原始值；累计、窗口和峰值单独命名；区分粒子系统对象数、活跃数与存活粒子数。
5. **标记无效样本**：内部纹理方法找不到时现代码可返回 0；正式报告应明确记录“不支持 / 采集失败”，不能以绿色零值代表通过。
6. **对照原渲染事件**：逐项核验 Batches、Pass、Renderer 与材质关系，不把 `/ 2` 扩写成通用公式。
7. **补可追溯输出**：本轮所读工具源码中未见批量调度、CSV / JSON 导出、Player 真机采集或 CI 入口；这些是后续建设内容。

`SingleEffectEvla` 虽建有 High / Middle / Low 三个数据槽，但当前调用固定写入第一个槽，也未切换项目品质设置；不能据此声称自动完成三档测试。

## 7. 第一轮最小验证设计（全部待执行）

先制作原创几何与简单材质校准测量，再用仓库示例粒子观察生命周期。先校准指标，之后才比较复杂效果。

| 实验 | 控制与单项变化 | 观察与判定问题 |
| --- | --- | --- |
| 空场景 / 单层 | 固定相机、黑色清屏与 RT；加入单张已知覆盖的面片 | 背景是否计数？覆盖数与预期范围是否一致？ |
| 已知重叠层数 | 同样面积，依次 1、2、4、8、16、32 层 | 反解值是否增长，何时量化或饱和？记录颜色空间 |
| 贴图轮廓 / 深度 | 固定几何，改变 Alpha / clip，或加不透明遮挡物 | 替换结果与原渲染差异；验证采集是否保留所需语义 |
| 屏占比 | 层数不变，改变受控投影面积 | 覆盖区层数相近时，总填充如何变化？ |
| 时序尖峰 | 固定种子，基础层数中插入短时高层数 | 原始帧计数与累计曲线差异；空白帧是否应进入目标窗口？ |
| 对象归因 | 目标特效不变，加入无关物体 / 第二相机 | DrawCall 和覆盖值是否受干扰；隔离是否有效？ |
| 粒子与纹理范围 | 子发射器、inactive 子级；主贴图之外增加 Mask / Trail 纹理 | 对照人工已知集合，确认漏计或重复计数的条件 |
| 关闭采集的 Player 对照 | 固定原材质、相机、实例数和设备，分别采集首放与重复播放 | CPU / GPU 证据是否可用？单体通过后，同屏叠加是否仍满足预算？ |

每次保留：仓库 SHA、Unity / 管线 / 包版本、场景和资产身份、随机种子、相机与分辨率、图形 API、构建选项、设备、采样时间与阶段、工具开关、原始值、异常与截图。冷启动须说明进程、资源与缓存状态，不能仅以“重新点播放”命名。

运行报告应分别列数据有效性、内容表现和性能判断；空场景校准失败、反射失败、GPU 指标不可用都不算通过。真机条件未确定时，可以先完成本机工具校准，不能提前填入手机性能结果。

## 8. 本轮交付状态

- 下载与来源固定：完成；上游副本保持干净。
- 静态分析：完成入口、全部工具 C# 文件、替换 Shader、示例 Shader、相机及工程版本的首轮核对。
- 实验：设计完成，未运行 Unity 或设备实验；未确认版本兼容性。
- 专栏：建立 canonical plan 与系列级路由，未新增公开正文。
- Git 与网站：未提交、推送或发布；本轮只有规划和分析文档变动，没有把未执行实验写为实测。
