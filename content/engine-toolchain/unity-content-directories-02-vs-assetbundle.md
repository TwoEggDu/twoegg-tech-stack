---
title: 'Unity 6.6 Content Directories 02｜与 AssetBundle 对比：依赖、内存与迁移取舍'
slug: "unity-content-directories-02-vs-assetbundle"
date: "2026-10-22"
description: "沿构建范围、存储布局、依赖与生命周期、远程交付四层比较 Content Directories 和 AssetBundle，解释 Addressables 的位置，以及迁移前需要验证的工程条件。"
tags:
  - "Unity"
  - "Content Directories"
  - "AssetBundle"
  - "Addressables"
series: "Unity 资产系统与序列化"
weight: 56
---

> 版本说明：本篇讨论 Unity 6.6（6000.6），资料核查于 2026-10-22。以下共享资源案例为解释模型，不是项目实测。

项目要不要从 AssetBundle 迁移到 Content Directories，不能只看一张性能对比图。

单机游戏可能主要关心内容迭代与场景切换；持续运营的手游还依赖远程目录、资源版本、断点续传和回滚。即使两者使用同一套资源，迁移问题也不一样。

[上一篇]({{< relref "engine-toolchain/unity-content-directories-01-model-and-workflow.md" >}})讲了新系统的入口与用法。这篇用同一组问题比较两种方案：**哪些工作仍要做，哪些边界发生了变化。**

## 1. 比较前，先把 Addressables 放回正确位置

AssetBundle 是一套底层内容构建与加载方案；Addressables 在上层提供寻址、组织、操作和生命周期管理。

在支持 Content Directories 的 Addressables 版本中，业务仍可通过 Addressables 操作资源，而内容后端发生变化。所以，“Addressables 还是 Content Directories”通常不是同层选项。

```text
业务：打开界面 / 进入场景 / 切换角色
                  ↓
管理：Addressables 或项目资源管理层
                  ↓
内容后端：AssetBundle 或 Content Directories
                  ↓
平台存储与运行时加载
```

Unity 的官方介绍展示了已有 Addressables 项目更换后端、保留业务调用的案例。但这个案例不能证明所有项目都可以无条件迁移：自定义构建脚本、Provider、打包回调与发布工具仍要检查。[官方后端切换案例](https://unity.com/blog/content-directories-beyond-the-assetbundle)

## 2. 用同一张表比较四层边界

| 比较项 | AssetBundle | Content Directories（6.6） |
|---|---|---|
| 定义构建入口 | 明确的 Bundle 归属与构建输入 | Root Asset 及其可追踪引用 |
| 产物组织 | 每个 Bundle 对应归档容器 | 细粒度内容文件，可进一步归档 |
| 依赖管理 | 容器依赖需要调用方或管理层满足 | 内容系统按更细粒度追踪并处理依赖 |
| 同次构建共享资源 | 通过合理归属、共享包等方式避免重复 | 在一次内容构建内自动去重 |
| 按需使用 | 先取得容器，再加载目标资产 | 注册目录，再通过 Loadable 加载目标 |
| 释放责任 | 容器与已加载对象的生命周期需分别治理 | 每个加载所有者维护自己的 Loadable 引用 |
| 远程内容 | 已有下载、缓存和框架生态可用 | 内建工作流面向本地内容，远程机制需另行提供 |

这张表概括的是系统边界，具体加载代价还取决于布局和目标平台。[Content Directories 官方对照](https://docs.unity.com/en-us/engine/6000.6/manual/assets-and-media/assets-managing-runtime/content-directories/introduction)

## 3. 同一个共享依赖案例，两边分别承担什么责任

假设内容关系如下：

```text
角色 A → 材质 A → 共享贴图 T
角色 B → 材质 B → 共享贴图 T
```

### AssetBundle：先解决资源归属

如果 A、B 分别进两个 Bundle，而 T 没有明确的独立归属，它可能分别被带入两份构建产物。常见处理是给 T 安排共享 Bundle，再维护角色 Bundle 到共享 Bundle 的依赖。

原生 AssetBundle API 不替调用方完成依赖容器的加载；使用 Addressables 或其他管理层时，这部分工作由对应系统组织。[AssetBundle 依赖与重复规则](https://docs.unity3d.com/6000.6/Documentation/Manual/AssetBundles-Dependencies.html)

设计者仍需考虑：共享包是否过大，里面的资产是否拥有相近生命周期，以及常驻界面是否会长期保留其中一小部分内容。

### Content Directories：先确定内容构建的边界

把 A、B 放进同一次内容构建，系统可以处理共享产物，项目不必为了 T 再人为设计一个共享 Bundle。

但如果把 A、B 拆进两次独立的 Content Directory 构建，每次又都引用 T，两次输出各自独立，不能由此推断全项目只存在一份 T。

因此迁移时不应机械地把每个旧 Bundle 转成一个独立内容构建。旧布局中一些包只是为了共享依赖而存在，新的构建边界应重新按交付需求判断。

这也是官方创建文档建议先考虑一个根入口、按实际独立内容需求扩展的原因。[Root Asset 的组织建议](https://docs.unity.com/en-us/engine/6000.6/manual/assets-and-media/assets-managing-runtime/content-directories/create)

## 4. 必须纠正：加载 Bundle 不等于加载全部资产

这是讨论新旧方案时最容易夸大的地方。

AssetBundle 有容器加载与资产加载两个步骤。`LoadFromFile` 得到容器后，仍需调用 `LoadAsset` 等接口取得资产，不能据此写成“加载 Bundle 就把里面所有纹理全部读进内存”。压缩方式也会改变容器打开的实际成本。[LoadFromFile 的加载行为](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/AssetBundle.LoadFromFile.html)

Content Directories 改善的重点，是让依赖和释放边界少受预先设计的容器布局牵制，而不是发明了 AssetBundle 完全没有的“按需读取资产”。

反过来，调用 `Loadable<Prefab>.Load()` 也不意味着只加载一个孤立对象。Prefab 的直接依赖仍需满足；如果材质直接引用大贴图，贴图不会因为入口换成 Loadable 就失去加载成本。

## 5. 内存对比，要区分容器、资产和使用者

旧方案的典型难点是：共享 Bundle 中的一份内容还在使用，其他已经用过的内容却希望尽快回收。项目需要让容器引用、已加载对象与实例使用状态保持一致。

这里不能省略 `AssetBundle.Unload` 的契约：

- `Unload(false)` 释放容器相关数据，保留已加载对象。
- `Unload(true)` 还会销毁从中加载的对象，不能在仍有使用者时随意调用。

因此，保留 Bundle 与保留每个资产不是完全相同的状态；旧方案也存在额外的对象回收策略。[Unload API](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/AssetBundle.Unload.html)

Content Directories 提供更直接的加载所有权表达：多个独立 Loadable 实例可以持有同一目标，最后一份加载引用释放后，系统才结束对应持有关系。共享数据仍在使用时，同样不能被提前释放。

迁移的价值要看布局是否存在大量生命周期牵连。若一组资源本来总是一起使用、一起退出，拆得更细可能没有多少收益。

## 6. 存储归档与加载单元可以分开讨论

看到 Content Directories 输出很多文件，很容易得出“它更快只是因为没有压缩”的判断；看到它也能归档，又容易认为“最后还是一个 Bundle”。

两种判断都忽略了内层结构。归档负责文件组织与压缩，内层内容负责依赖与加载关系。Content File 与归档不是同一个概念；不能只数磁盘上有几个文件，就推断有几个运行时加载单元。

格式本身也在演进，工具不宜把内部 manifest 结构写死成长期契约。[UnityDataTools 的内容与归档说明](https://github.com/Unity-Technologies/UnityDataTools/blob/main/Documentation/contentdirectory-format.md)

## 7. 本地与远程混用，需要付出什么代价

对于既有远程内容的项目，一种过渡方案是：本地内容试用 Content Directories，远程内容继续走 AssetBundle。

这条路技术上可行，但两个后端之间的共享依赖可能分别进入两套输出。项目还要维护两套构建产物和故障定位路径，不能只验证本地加载成功。

做一个具体的迁移清单会更有用：

| 核查对象 | 要回答的问题 |
|---|---|
| 业务调用 | 是否直接依赖 AssetBundle API 或物理包名 |
| 构建扩展 | 是否假定每个 Group 都生成 Bundle、读取旧 manifest |
| 本地/远程边界 | 公共 Shader、Material、配置是否被重复构建 |
| 版本与兼容 | 新内容依赖的脚本类型、序列化布局、裁剪信息是否与 Player 匹配 |
| 部署与恢复 | 发布工具怎样复制完整输出，怎样发现缺文件并恢复 |
| 生命周期 | 场景退出、对象池、常驻缓存是否仍持有加载引用 |

其中脚本兼容、裁剪、发布完整性是项目必须验证的责任，内容构建能独立运行，并不等于内容可以与任何 Player 任意组合。

## 8. 哪些项目值得先试

以下是基于上述边界的工程判断：

- 本地内容占主体、资源迭代频繁、旧 Bundle 布局经常制造依赖与驻留问题的项目，适合做一段真实业务的对照验证。
- 远程热更新是核心、已有成熟缓存与回滚体系的项目，应先验证分发替代方案和混用成本。
- 瓶颈集中在脚本初始化、Shader 首次使用或实例化的项目，需要先定位关键路径，再估计内容后端能改善的部分。

这里的“先试”不等于直接迁移全项目。选一个角色预览、一个场景切换或一个独立内容包，保留原后端基线，再观察完整流程。

<!-- DATA-TODO: 补充同一业务片段的新旧后端构建布局、目标 Player 结果和内存快照；至少覆盖公共依赖、重复进入退出、常驻缓存与混用输出。 -->

下一篇继续拆[为什么可能更快]({{< relref "engine-toolchain/unity-content-directories-03-why-faster.md" >}})，把构建、加载、驻留和释放的收益分别归因。

**比较这两套方案，要比较依赖、生命周期与交付责任怎样变化，再决定迁移是否值得。**
