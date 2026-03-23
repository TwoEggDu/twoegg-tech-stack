# Unity 资产系统与序列化专栏规划

## 专栏定位

这组文章不写成 `AssetBundle 教程`，也不写成 `Unity 资源 API 速查`。

它真正要解决的问题是：

`把 Unity 里的资产系统拆成一张稳定地图，讲清楚文件怎样经过 Importer、引用系统、序列化系统、场景对象和运行时恢复链，最后变成游戏里真正可见、可用、可交付的内容。`

`AssetBundle` 只是这张地图里的一个重要子问题，它站在 `资源交付层`，不是整张地图本身。

一句话说，这个专栏的重点不是 API 记忆，而是：

`Unity 资产系统、序列化恢复链和资源交付链的结构理解。`

## 为什么要把它升成上位专栏

如果继续只按 `AssetBundle` 写，后面会有三个问题：

- `Scene / Prefab / ScriptableObject / Material / Shader / GUID / fileID / PPtr` 这些内容，其实不属于 AssetBundle 本身
- `番外` 会越来越像主线，最后说明主线范围定窄了
- 很多工程问题的根因，其实发生在 Bundle 之前的资产定义、引用和序列化层

所以更稳的做法是先立一个上位专栏：

`Unity 资产系统与序列化`

再把 `AssetBundle / Addressables / 资源交付` 放进其中一条子线。

## 目标读者

- 已经在做 Unity 项目，但对资产、引用、序列化、运行时恢复之间的关系还没有稳定地图的人
- 写过 AssetBundle / Addressables / 资源加载逻辑，但总觉得很多问题只能靠经验排查的人
- 做客户端工程、工具链、性能治理、热更资源链，需要把资源问题拉回结构语言的人

## 总地图

这个专栏最适合沿着下面这条链往下拆：

`磁盘文件 -> meta / GUID -> Importer -> 资产对象 -> 引用与序列化 -> Scene / Prefab / ScriptableObject 等资产结构 -> 运行时恢复链 -> AssetBundle / Addressables 交付 -> 缓存 / 版本 / CDN / 治理`

其中最重要的认知任务，是反复讲清下面几件事：

1. Unity 里的“资产”不是项目文件夹里的文件本身。
2. `文件`、`资产对象`、`场景对象`、`运行时实例` 不是一回事。
3. 很多资源问题的根因不在“加载 API”，而在更早的引用、序列化、构建切分边界。
4. AssetBundle 解决的首先不是“怎么加载资源”，而是“怎么交付资源”。

## 专栏结构

建议做成 `1 条总主线 + 4 条子线 + 若干索引/诊断/案例`。

### 总主线

总主线负责建立完整地图，让读者知道每一层站在哪里。

### 子线 A｜资产通识与引用系统

这条线负责回答：

- Unity 里到底有哪些资产类型
- `meta / GUID / fileID / PPtr` 分别在解决什么问题
- 文件怎样变成能被引擎识别和引用的资产对象

### 子线 B｜Scene / Prefab 等序列化资产结构

这条线负责回答：

- Scene 文件本质上是什么
- Prefab 文件本质上是什么
- 它们内部为什么更像对象图而不是单一资源

### 子线 C｜资产恢复链与脚本身份链

这条线负责回答：

- 序列化资产怎样还原成运行时对象
- 为什么脚本丢失、引用断裂、Prefab 恢复异常都爆在这条链上
- 资源系统和脚本身份链为什么要一起看

### 子线 D｜AssetBundle / Addressables / 资源交付

这条线负责回答：

- 为什么 Unity 需要 AssetBundle
- AssetBundle 怎么构建、怎么交付、怎么恢复、怎么治理
- Addressables 和 AssetBundle 的关系到底是什么

## 和现有内容的关系

这组文章不从零开始，它最适合把你已有内容重新挂到同一张图上。

当前已经可复用的文章有：

- `Unity 工具链开发真正要懂的三条引擎链路`
- `Unity Shader Variants 为什么会存在，以及它为什么总让项目变复杂`
- `Unity Shader Variant 实操：怎么知道项目用了哪些、运行时缺了哪些、以及怎么剔除不需要的`
- `一次 AssetBundle 构建后 Shader Variant 丢失问题的定位与修复`
- `HybridCLR MonoBehaviour 与资源挂载链路`

它们在新专栏里的位置可以这样放：

- `unity-engine-insights` 作为资产导入、构建和脚本编译边界的底座文章
- 两篇 `Shader Variant` 主文作为资源交付和 shader 边界治理文章
- `urp-shader-prefiltering-assetbundle` 作为 problem-solving 案例篇
- `hybridclr-monobehaviour-and-resource-mounting-chain` 作为“脚本身份链”深水区文章

也就是说，这个专栏不是替换现有文章，而是把它们重新挂回一张更大的地图。

## 前导与主线

### 前导 01｜Unity 里到底有哪些资产：文件、Importer、Object、组件、实例，资源是怎么在游戏里被看见的

核心问题：
Unity 里说的“资产”到底是什么，它和磁盘文件、导入产物、场景对象、运行时实例分别是什么关系。

这篇要完成的事：

- 先把 `文件`、`meta/GUID`、`Importer`、`UnityEngine.Object`、`Scene Object`、`Runtime Instance` 拆开
- 给出 Unity 常见资产类型地图：
  `Texture / Mesh / Material / Shader / AnimationClip / AudioClip / ScriptableObject / Prefab / Scene / FBX 导入产物 / 字体 / 视频 / 文本配置`
- 解释“资源怎么在游戏里被看见”：
  不是文件直接进游戏，而是经过导入、序列化引用、加载、实例化、脚本绑定、渲染或播放，最后变成运行时可见对象
- 提前讲清几组最容易混的边界：
  `源文件 != 导入后的资产对象`
  `Prefab 资产 != 场景里的实例`
  `Scene 文件 != 运行时世界`
  `Material != Shader`
  `AssetBundle != 资产本体`

这篇的核心结论建议压成一句话：

`Unity 里的资产，不是“项目文件夹里的文件”本身，而是文件经过 Importer 和序列化系统组织后，能被引擎识别、被场景或运行时引用、最终进入游戏世界的一组对象。`

## 子线 A｜资产通识与引用系统

### A01｜GUID、fileID、PPtr 到底在引用什么

核心问题：
Unity 为什么不直接靠文件路径引用资源，而要引入 `GUID / fileID / PPtr` 这套系统。

重点要讲：

- `meta` 文件和 GUID 的职责
- `fileID` 在子资产和对象级引用里的位置
- `PPtr` 为什么是“指向 Unity 对象”的引用，不只是路径字符串
- 为什么理解这层之后，很多“引用丢了”的问题会从玄学变回结构问题

### A02｜Importer 到底做了什么：为什么同一份源文件，进到 Unity 后不再只是“文件”

核心问题：
Importer 在 Unity 资产系统里到底站在哪，为什么它不是一个纯编辑器细节。

重点要讲：

- 源文件和导入产物的差别
- FBX、Texture、Audio、Font 这类资产为什么都有自己的导入语义
- Importer 如何塑造可引用对象、子资产和构建产物

## 子线 B｜Scene / Prefab 等序列化资产结构

### B01｜Scene 文件本质上是什么：为什么它更像一张对象图，而不是一个“大资源”

核心问题：
Scene 为什么不是简单的资源清单，而是一整张对象图。

重点要讲：

- `GameObject / Transform / Component` 为什么天然构成图结构
- 场景级对象和全局设置对象怎样挂进这张图
- Scene 加载为什么不是“读文件”，而是“恢复整张对象关系”

### B02｜Prefab 文件本质上是什么：模板对象图、嵌套、Variant 和 Override 分别站在哪

核心问题：
Prefab 为什么不是“可复用 GameObject 文件”这么简单。

重点要讲：

- Prefab 更像模板对象图
- Prefab Asset 和 Scene Instance 的区别
- Nested Prefab、Variant、Override 的结构关系

### B03｜ScriptableObject、Material、AnimationClip 这些资产的结构气质为什么不一样

核心问题：
为什么同样叫“资产”，有些更像对象配置，有些更像渲染资源，有些更像时序数据。

重点要讲：

- ScriptableObject 的数据配置角色
- Material 和 Shader 的边界
- AnimationClip / AudioClip 这类资产为什么更像专用子系统入口

## 子线 C｜资产恢复链与脚本身份链

### C01｜序列化资产怎样还原成运行时对象：从 Serialized Data 到 Native Object、Managed Binding

核心问题：
磁盘上的序列化资产，最后是怎样变成运行时可用对象的。

重点要讲：

- `object records / fileID / PPtr / external refs` 分别负责什么
- 为什么加载不是一步，而是一条恢复链
- Native Object 和 Managed Binding 的边界

### C02｜Prefab / Scene / AssetBundle 到底怎样从序列化文件还原成运行时对象

核心问题：
Scene、Prefab 和 AssetBundle 为什么共用一部分恢复链，但又不是完全同一回事。

重点要讲：

- `Scene` 更像整张对象图的恢复与激活
- `Prefab` 更像模板对象图的反序列化，再经 `Instantiate` 变成实例
- `AssetBundle` 只是装着这些序列化内容和资源数据的交付容器

### C03｜为什么资源挂脚本时问题特别多：脚本身份链、MonoScript 和程序集边界

核心问题：
为什么资源系统和脚本系统一交界，问题密度就会明显变高。

重点要讲：

- `MonoScript`、程序集身份、脚本绑定分别站在哪
- 为什么 missing script 常常不是“类没了”这么简单
- 为什么这条链正好也是 HybridCLR 这类方案最难接回去的地方

## 子线 D｜AssetBundle / Addressables / 资源交付

### D00｜为什么 Unity 需要 AssetBundle：它解决的不是“加载”，而是“交付”

核心问题：
为什么 Unity 已经有 Resources、Scene、StreamingAssets，项目还要引入 AssetBundle。

重点要讲：

- AssetBundle 是构建期交付格式
- 它服务的是内容分发、版本更新、平台差异和资源解耦
- 它站在资产系统的交付层，而不是资产系统本体

### D01｜Unity 怎么把资源编成 AssetBundle：依赖、序列化、Manifest、压缩到底发生了什么

核心问题：
从项目里的 prefab、material、shader、texture，到最后磁盘上的 bundle 文件，中间 Unity 到底做了什么。

重点要讲：

- AssetBundle Name / Build Map 怎样定义输入边界
- 资源依赖怎样被收集和切分
- Manifest 到底解决什么问题
- 一个 AssetBundle 文件里大致装了什么
- 为什么很多运行时问题，其实是构建期切分策略的延迟后果

### D02｜AssetBundle 运行时加载链：下载、缓存、依赖、反序列化、Instantiate、Unload 怎么接起来

核心问题：
运行时说“加载一个 AssetBundle”，到底不是一步，而是哪几步。

重点要讲：

- 下载和本地缓存的边界
- 加 bundle 和 load asset 不是一回事
- 依赖 bundle 为什么必须先满足
- 公共恢复链应该怎样理解：
  `bundle -> serialized data -> object records -> refs -> native object -> managed binding -> instantiate / activate`
- `Unload(false)` 和 `Unload(true)` 为什么常被误用

### D03｜为什么 AssetBundle 总让项目变复杂：切包粒度、重复资源、共享依赖和包爆炸

### D04｜AssetBundle 的性能与内存代价：LZMA/LZ4、首次加载卡顿、内存峰值、解压与 I/O

### D05｜AssetBundle 的工程治理：版本号、Hash、CDN、缓存、回滚、构建校验与回归

### D06｜Addressables 和 AssetBundle 到底是什么关系：谁是格式，谁是调度层

## 重点番外

### 番外 01｜Shader 在 AssetBundle 里到底是怎么存的：资源定义、编译产物和 Variant 边界

这篇至少拆三层：

- `资源定义层`：`Shader`、`Material`、`ShaderVariantCollection` 在序列化数据里各自存了什么
- `构建产物层`：shader 编译产物和 variant 是什么时候生成、剔除、打进 Player 或 AssetBundle 的
- `运行时命中层`：bundle 加载后，材质怎么把 shader 引用接回去，又怎样命中具体 variant

### 番外 02｜为什么 Shader Variant 问题总在 AssetBundle 上爆出来

### 番外 03｜AssetBundle 文件内部结构：Header、Block、Directory 和 SerializedFile 是怎么组织的

### 番外 04｜看到一个 Unity 资源问题时，先怀疑哪一层

这篇不再讲新原理，而是把整套地图收成一个排查入口：

`资产定义 -> 引用与序列化 -> 构建切分 -> 交付 -> 运行时恢复 -> shader / 脚本边界 -> 缓存 / 版本 / CDN`

## 后置补强文章

### 工程实践篇

建议题目：

`Unity 资源交付工程实践：分组、命名、版本、缓存、回滚和烟测基线`

这篇负责把前面的原理和治理判断收成一套项目规则，重点回答：

- 切包和分组应该先按什么边界定
- 逻辑命名、物理文件名、内容 Hash 各自负责什么身份
- 缓存、回滚和发布快照为什么必须围绕内容身份设计
- 构建校验和烟测应该最低覆盖哪些风险
- Addressables、YooAsset 和自研管理层分别适合站在哪种团队条件上

### 索引页

建议题目：

`Unity 资产系统与序列化系列索引：从资产通识到 Scene、Prefab、Shader 与 AssetBundle`

### 案例篇簇

建议至少补 3 组：

- `重复资源和依赖爆炸`
- `Scene / Prefab / 脚本引用丢失`
- `首次加载卡顿或缓存失效`

## 推荐推进顺序

1. 前导 01｜Unity 里到底有哪些资产
2. A01｜GUID、fileID、PPtr 到底在引用什么
3. B01｜Scene 文件本质上是什么
4. B02｜Prefab 文件本质上是什么
5. C01｜序列化资产怎样还原成运行时对象
6. D00｜为什么 Unity 需要 AssetBundle
7. D01｜Unity 怎么把资源编成 AssetBundle
8. D02｜AssetBundle 运行时加载链
9. D06｜Addressables 和 AssetBundle 的关系
10. 番外 01｜Shader 在 AssetBundle 里怎么存
11. D03｜为什么 AssetBundle 总让项目变复杂
12. D04｜AssetBundle 的性能与内存代价
13. 番外 02｜为什么 Shader Variant 问题总在 AssetBundle 上爆出来
14. D05｜AssetBundle 的工程治理
15. 工程实践篇｜Unity 资源交付工程实践
16. 番外 04｜看到一个 Unity 资源问题时，先怀疑哪一层
17. 索引页
18. 案例篇簇

这个顺序的好处是：

- 先立资产系统的底座
- 再立序列化资产和恢复链
- 最后再进入 Bundle 这条交付子线

## 证据范围 / 写作边界

这组文章默认分四层证据：

### 1. Unity 源码 / 官方可验证实现

适合承载这些问题：

- 引用系统、Importer、Build Pipeline、SerializedFile、对象恢复链
- Scene / Prefab / Script binding 这类需要解释“Unity 到底怎么做”的问题

### 2. 项目实证 / 真实构建产物

适合承载这些问题：

- 拆包策略会造成什么结果
- 重复资源、共享依赖、首载卡顿、缓存命中怎样显形
- Shader Variant、Addressables、热更资源链路怎样在真实项目里互相影响

### 3. 工程判断 / 结构性推论

适合承载这些问题：

- 为什么某种拆包策略更容易失控
- 为什么某些问题应该先怀疑构建期，而不是先怀疑运行时 API
- 为什么同一个表面现象，可能落在依赖、序列化、variant、缓存不同层

### 4. 边界外内容

这组文章原则上不做下面这些事：

- 不做第三方逆向工具的格式考古教程
- 不做“所有 Unity 版本 AssetBundle 二进制差异大全”
- 不把 shader 编译器内部实现展开成单独一门编译原理课
- 不把热更方案优劣比较写成 HybridCLR 专题替代品

## 统一写作约束

- 不写成“今天带你学会 X”的教程口吻
- 不按 API 平铺接口
- 每篇只回答一个主问题，但结尾必须收回工程判断
- 多讲边界和链路，少讲零散技巧
- 优先回答“为什么会复杂”“复杂落在哪”“应该先怀疑哪一层”

## 放站点时的建议位置

- 主线和前导文章放 `content/engine-notes`
- 案例复盘放 `content/problem-solving`
- 系列索引页单独放一篇 `unity-asset-system-and-serialization-series-index`

## 最后压成一句话

如果这组文章最后只能让读者记住一句话，那应该是：

`AssetBundle 不是 Unity 资源系统里一个孤立的打包功能，而只是 Unity 资产系统、序列化恢复链和资源交付链中的交付层入口。`
