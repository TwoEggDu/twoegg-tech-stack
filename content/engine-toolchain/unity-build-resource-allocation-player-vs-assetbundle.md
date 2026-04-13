---
date: "2026-04-13"
title: "Unity 构建时的资源归属：哪些对象进 Player Build、哪些进 AssetBundle、边界怎么划"
description: "从构建机制层面拆清 Player Build 和 AssetBundle Build 各自的资源收集规则，解释两条路径为什么互不感知、重复打入怎么发生，以及项目该怎样按资源类型划分归属边界。"
slug: "unity-build-resource-allocation-player-vs-assetbundle"
weight: 63
featured: false
tags:
  - "Unity"
  - "Player Build"
  - "AssetBundle"
  - "Build Pipeline"
  - "Packaging"
series: "Unity 资产系统与序列化"
---
写到这里，这条系列已经讲过 [Resources、StreamingAssets、AssetBundle、Addressables 到底各自该在什么场景下用]({{< relref "engine-toolchain/unity-resources-streamingassets-assetbundle-addressables-when-to-use.md" >}})，也讲过 [为什么 AssetBundle 总让项目变复杂]({{< relref "engine-toolchain/unity-why-assetbundle-gets-complex-granularity-duplication-shared-dependencies.md" >}})。

但在实际项目里，有一个更基础的困惑经常出现：

`我明明觉得这个资源应该只出现在 AssetBundle 里，为什么它还是出现在了 Player 构建产物中？`

或者反过来：

`我以为这个资源已经进了 Player，为什么运行时从 AB 加载时发现缺依赖？`

这两类问题背后，其实是同一个机制在起作用：

`Player Build 和 AssetBundle Build 各自有一套独立的资源收集规则，它们互不感知。`

所以这篇我只做一件事：

`把 Player Build 和 AssetBundle Build 的资源收集规则放在一起对照，讲清它们各自从哪些入口出发、怎么展开依赖、以及两条路径交叉时会发生什么。`

这不是在讲"你该怎么选路径"——那件事 [资产-20]({{< relref "engine-toolchain/unity-resources-streamingassets-assetbundle-addressables-when-to-use.md" >}}) 已经讲过了。这篇讲的是更下面一层：选完路径之后，引擎在构建时到底怎么收集资源。

## 一、Player Build 的资源收集规则

当你点击 Build 或执行 `BuildPipeline.BuildPlayer` 时，Unity 会从以下六个入口出发收集资源，将它们序列化写入 Player 数据文件（`globalgamemanagers.assets`、`sharedassets*.assets`、`level*` 等）。

### 入口一：Build Settings Scene List

`EditorBuildSettings.scenes` 中所有 enabled 的场景，是 Player 数据的第一大来源。

引擎会遍历每个场景内的所有 `GameObject` 和 `Component`，然后沿着 PPtr 引用链递归展开——意思是，每个 Component 引用的 Material、Texture、Mesh、AnimationClip、ScriptableObject 等，都会被一路追到底。

最终的结果是：凡是被 Scene List 场景直接或间接引用到的资产对象，全部进入 Player 序列化数据。

### 入口二：Resources/ 文件夹

项目中所有名为 `Resources` 的文件夹（包括子目录），其下的全部资产都会被无条件收集进 Player。

注意这里的"无条件"：引擎不检查你的代码里是否真的调用了 `Resources.Load`。只要文件在 `Resources/` 目录下，不管运行时用不用，构建时一律收集。

### 入口三：StreamingAssets/

`StreamingAssets/` 下的文件也会进入最终安装包，但行为和前两个入口完全不同：它是 **raw file copy**。

引擎不读这些文件的内容，不解析引用关系，不展开依赖，不写入 Player 序列化数据。它们被原样拷贝到输出目录，运行时需要你自己用文件 IO 或 `AssetBundle.LoadFromFile` 去读。

这是唯一一个"进安装包但不参与 Player 资源序列化"的入口，必须和前两个明确区分。

### 入口四：PlayerSettings 引用

`PlayerSettings` 中显式引用的资产也会被收集，包括：

- Splash Screen 图片
- Default Cursor 贴图
- App Icon 各尺寸图标
- `QualitySettings` 中引用的资产

这些通常体积不大，但它们是隐式进入 Player 的，很容易被忽略。

### 入口五：Always Included Shaders

`GraphicsSettings.always_included_shaders` 列表中的每一个 Shader，连同其所有未被 strip 的变体，都会进入 Player。

这个入口的特殊性在于：它带进 Player 的不只是一个 Shader 文件，而是这个 Shader 在当前 stripping 设置下保留的所有变体。一个看起来不大的 Shader，展开变体后可能占几 MB 甚至几十 MB。

关于 Always Included Shaders 和 Shader Variant Stripping 的完整机制，可以看 [Shader Variant 构建账单]({{< relref "rendering/unity-shader-variant-build-receipts-player-vs-ab.md" >}})，这里不再展开。

### 入口六：Preloaded Assets

`PlayerSettings.preloadedAssets` 列表中显式指定的资产，加上它们的依赖链，也会进入 Player。

这个入口在日常开发中不太常见，但一些插件和框架会用它来确保某些资产在启动时可用。

### 六个入口的共同特征

这六个入口有一个很重要的共同点：

`它们的收集范围和 AssetBundle 构建完全无关。`

不管你有没有给某个资产标 `assetBundleName`，不管你的 Addressables Group 怎么配，Player Build 的收集逻辑不看这些——它只看上面六个入口。

## 二、AssetBundle Build 的资源收集规则

和 Player Build 的"六入口汇聚"不同，AssetBundle Build 只有一个收集起点：**开发者显式指定的根资产列表**。

具体来说：

- 传统 API：通过 `assetBundleName` 标记，或构建时传入 `AssetBundleBuild[]` 数组指定
- SBP（Scriptable Build Pipeline）：通过 `IBundleBuildContent` 显式指定
- Addressables：通过 Group 中的 Entry 列表指定

不管用哪种方式，构建时的核心动作是一样的：从每个 bundle 的根资产出发，递归调用 `AssetDatabase.GetDependencies` 展开完整的依赖闭包，然后把闭包内所有资产序列化写入对应的 `.bundle` 文件。

### 去重规则只在 bundle 之间生效

AB 构建在展开依赖时，会检查每个依赖资产是否已经被**其他 bundle** 显式指定为根资产：

- 如果是——记为外部依赖，不重复写入当前 bundle，只在 manifest 里记录依赖关系
- 如果不是——作为隐式依赖写入当前 bundle

第二种情况就是跨 bundle 重复的来源：如果一个共享贴图没有被任何 bundle 显式指定为根资产，那么每个引用它的 bundle 都会各自把它写一份。

这个问题的治理属于 [AB 打包粒度]({{< relref "engine-toolchain/unity-assetbundle-pack-granularity-coarse-vs-fine-dependencies-redundancy.md" >}}) 的范畴，这里只点明机制。

### 和 Player Build 的结构性差异

把两条路径放在一起看，差异很明显：

- **Player Build** 是"多入口汇聚到一个输出"——六个入口各自收集，最终合并写入一组 Player 数据文件
- **AssetBundle Build** 是"每个 bundle 各自独立做闭包"——每个 bundle 从自己的根资产出发，独立展开依赖

而最关键的一点是：

`这两条路径之间没有全局视图。Player Build 不知道 AB Build 会收集什么，AB Build 也不知道 Player Build 已经收集了什么。`

## 三、两条路径的交叉地带：同一资源被 Player 和 AB 同时引用

理解了前两节的收集规则之后，交叉地带的问题就很自然了：

`如果同一个资产同时被 Player Build 的某个入口引用，又被某个 AssetBundle 的依赖闭包覆盖，它会怎样？`

答案是：**两边各存一份，互不去重。**

### 重复发生的典型场景

最常见的情况是共享 Material。

比如你有一个 `SharedUI.mat`，它被 Scene List 里的启动场景引用（进 Player），同时被某个 AB 里的 UI Prefab 引用（进 bundle）。结果就是：

- Player 数据里有一份 `SharedUI.mat` + 它依赖的 Shader + Texture
- 那个 bundle 文件里也有一份完全相同的 `SharedUI.mat` + Shader + Texture

两份独立序列化，各自占包体大小。

### 运行时的代价

包体重复只是第一层代价。运行时还有第二层：

如果 Player 内置版本和 AB 加载版本同时存在于内存，引擎不会自动合并它们。也就是说，同一张贴图可能在 GPU 内存中存在两份——一份来自 Player 启动时加载的场景，一份来自后来从 AB 加载的 Prefab。

### 怎么检测重复

三种方式，从简单到完整：

**1. Addressables 的 BuildLayout 报告**

如果你用 Addressables，构建后生成的 `BuildLayout.txt` 会列出每个 bundle 中包含的资产。把这份列表和 Player 的 BuildReport 做交集，就能找到重复。

关于 BuildLayout 各字段的含义，可以看 [怎么看 Unity 资源构建产物]({{< relref "engine-toolchain/unity-how-to-read-resource-build-artifacts-manifest-buildlayout-catalog-cache.md" >}})。

**2. AssetBundle Manifest 对比 Player BuildReport**

传统 AB 构建产出的 `.manifest` 文件会列出每个 bundle 的资产路径。将这些路径和 `BuildReport`（`UnityEditor.Build.Reporting.BuildReport`）中记录的 Player 资产做对比，交集部分就是重复。

**3. 自定义脚本检测**

用 `AssetDatabase.GetDependencies` 分别对 Scene List 场景和 AB 根资产展开依赖，在 Editor 阶段就能预判哪些资产会落入两条路径。

### 重复不是 bug，是设计上的必然

需要强调的是：两条路径互不去重不是 Unity 的 bug。它是两套独立构建流程各自保证依赖闭包完整性的结果。

如果 AB 假设"Player 已经带了这个资源所以我不放了"，那么当 AB 脱离这个 Player 版本单独分发时，就会缺依赖。AB 的独立交付能力，恰恰建立在"它自己的依赖闭包是完整的"这个前提上。

所以问题不是"怎么消除所有重复"，而是"哪些重复是可以接受的，哪些重复的代价已经超出了收益"。

## 四、分配决策框架：什么留 Player、什么走 AB

理解了收集规则之后，分配决策就可以落到具体操作上了。

### 判断维度

每个资源的归属，可以从三个维度判断：

- **是否首启必需**——用户从安装到第一次"在玩"的链路上，这个资源缺了会不会断
- **是否需要独立更新**——上线后是否需要不重新出包就能更新这个内容
- **是否被多条路径共享**——是否同时被 Player 场景和 AB 内容引用，存在重复风险

### 各资源类型归属建议

以下是按资源类型给出的默认建议。这不是硬性规则，项目规模和热更需求不同时需要调整。

| 资源类型 | 默认归属 | 理由 | 例外条件 | 重复风险 |
|---------|---------|------|---------|---------|
| Scene | 首启场景进 Player，其余进 AB | 首启场景必须安装即可用；后续关卡需要按需下载和独立更新 | 如果项目不做热更且关卡数量少，全部进 Player 也可以 | 低——场景通常不会同时出现在 Scene List 和 AB 中 |
| Shader | Always Included 的基础 Shader 进 Player，其余跟 AB 走 | 首屏可见的 Shader 必须在 Player 中，否则首帧会触发运行时编译卡顿 | 如果项目 Shader 总量很少且不热更，全部 Always Included 也可以 | **高**——最常见的重复打入类型 |
| Material | 看引用来源 | 被首启场景引用的进 Player，被 AB Prefab 引用的进 AB | 共享 Material 需要明确归属一边，避免两边都收集 | **高** |
| Texture / Mesh | 跟随引用链 | 体积大、变化频繁，适合走 AB 增量分发 | 首启 UI 的基础贴图跟 Player 走 | 中高 |
| Audio | 跟随引用链 | 大文件（BGM）适合 AB 按需加载 | 启动音效和关键提示音可跟 Player | 中 |
| ScriptableObject | 框架配置进 Player，玩法数据进 AB | 框架配置启动时必读；玩法数据运营期高频更新 | — | 中——配置表最容易两边都放 |
| Script (C#) | **必定进 Player** | 脚本编译为 IL2CPP/Mono 程序集，只存在于 Player 中；AB 里只存 `MonoScript` 引用（Assembly 名 + 类全名），不含代码 | 无例外 | 无 |
| Font | 默认语言字体进 Player，其余进 AB | 首屏 UI 渲染必需；中文字体体积大，非默认语言适合分包 | — | 中 |
| AnimationClip | 跟随 Prefab 归属 | 通常和角色 Prefab 绑定，一起进 AB | 首启角色的基础动画可跟 Player | 中——共享动画容易被两边收集 |

### Player 侧的最小集合原则

Player 里应该只放两类东西：

1. **首启链路必需**——从启动到第一次"在玩"的完整链路上不能缺的资源
2. **不需要热更的基础层**——引擎底座、框架配置、基础 Shader

其余内容，默认走 AB。

## 五、常见误区与现场诊断

### 误区一："标了 assetBundleName 就不会进 Player"

**实际情况：** `assetBundleName` 只影响 AB 构建的收集范围，完全不影响 Player Build 的收集逻辑。如果这个资产同时被 Scene List 场景的依赖链覆盖、在 `Resources/` 目录下、或在 Preloaded Assets 列表中，Player Build 照样收集。

**验证方法：** 对比 Player 构建的 `BuildReport` 和 AB 的 `.manifest` 文件，检查是否有交集。

### 误区二："StreamingAssets/ 里的文件会被引擎解析依赖"

**实际情况：** `StreamingAssets/` 是纯文件拷贝通道。引擎构建时只做 raw copy，不读内容、不展开依赖、不参与序列化。你可以在里面放 AB 文件、JSON、视频——引擎不关心格式。

**验证方法：** 在 `StreamingAssets/` 放一个故意损坏的二进制文件，执行 Build，构建不会报错。

### 误区三："Always Included Shaders 只影响 Player Build"

**实际情况：** AB 构建时也会参考 Always Included Shaders 列表。具体影响的是 Shader Variant Stripping 决策——构建系统在决定 AB 中某个 Shader 保留哪些变体时，会考虑 Always Included 列表中已有的变体作为基线。

**验证方法：** 修改 `GraphicsSettings.always_included_shaders` 列表后，在不改任何 AB 内容的情况下重新构建 AB，对比产物大小变化。

### 误区四："Resources/ 下的资产标了 AB 就只进 AB"

**实际情况：** `Resources/` 的收集是无条件的。只要资产在 `Resources/` 目录下，Player Build 一定会收集它，不管你有没有同时给它标 `assetBundleName` 或放进 Addressables Group。标了 AB 只是让 AB 构建也收集它——结果就是两边都有一份。

**验证方法：** 检查 Player 构建产物中 `globalgamemanagers.assets` 的大小。把一个大贴图放进 `Resources/`，构建一次；再把它移出 `Resources/`（只保留 AB 标记），构建一次。对比两次产物大小。

### 误区五："AB 里包含了 .cs 脚本的代码"

**实际情况：** `AssetDatabase.GetDependencies` 的返回列表确实包含 `.cs` 文件路径，但脚本代码不会被序列化进 bundle。AB 里只存 `MonoScript` 引用——记录的是 Assembly 名和类全名，运行时靠 Player 中已编译的程序集解析实际类型。

---

回到开头的问题：Unity 构建时按什么规则把资源分进 Player 和 AssetBundle？

答案的核心就一句话：`Player Build 从六个入口出发做依赖闭包，AssetBundle Build 从显式根资产出发做依赖闭包，两条路径各自独立、互不感知。`

理解了这一点，后面所有关于"资源为什么出现在不该出现的地方"的问题，都有了判断起点。

接下来可以看 [首包体积优化：分包策略、按需下载、差量更新]({{< relref "engine-toolchain/unity-first-package-size-split-ondemand-download-patch.md" >}})，那篇从首进体验边界出发，讲的是理解了收集规则之后怎么做分配决策。

如果想进一步了解 Player 收集规则中"引擎内置资源"这一类的完整分层，可以看 [Unity 内置资源到底是什么]({{< relref "engine-toolchain/unity-builtin-resources-default-resources-always-included-built-in-bundles.md" >}})。
