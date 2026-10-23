---
title: 'Unity 6.6 Content Directories 02｜与 AssetBundle 对比：依赖、内存与迁移取舍'
slug: "unity-content-directories-02-vs-assetbundle"
date: "2026-10-22"
description: "区分引擎内容后端、资源管理框架与业务调用，以共享依赖和同包不同生命周期两个案例，解释成熟 AssetBundle 管线之后 Content Directories 仍改变了什么。"
tags:
  - "Unity"
  - "Content Directories"
  - "AssetBundle"
  - "Addressables"
series: "Unity 资产系统与序列化"
weight: 56
---

> 版本说明：Content Directories 依据 Unity 6.6（6000.6），资料复核于 2026-10-23；释放策略的对照明确采用 Addressables 2.7.6 文档中的 AssetBundle 后端行为。案例用于解释机制，未在本仓复测。

项目已经有成熟的 YooAsset 或 Addressables 管线，寻址、句柄、引用计数和依赖调度都能工作，为什么还要关注 Content Directories？

单机游戏可能主要关心内容迭代与场景切换；持续运营的手游还依赖远程目录、资源版本、断点续传和回滚。即使两者使用同一套资源，迁移问题也不一样。

[上一篇]({{< relref "engine-toolchain/unity-content-directories-01-model-and-workflow.md" >}})讲了新系统的入口与用法。这篇用同一组问题比较两种方案：**哪些工作仍要做，哪些边界发生了变化。**

## 1. 比较前，先把 Addressables 放回正确位置

先分清三个层次。业务表达“我要哪个角色、何时结束使用”；资源框架维护地址、操作句柄、缓存与引用关系；引擎内容后端决定产物怎样组织、依赖在哪一层追踪，以及哪些内容能独立加载和释放。

在支持 Content Directories 的 Addressables 版本中，业务仍可通过 Addressables 操作资源，而内容后端发生变化。所以，“Addressables 还是 Content Directories”通常不是同层选项。

```text
业务：打开界面 / 进入场景 / 切换角色
                  ↓
框架：YooAsset / Addressables / 项目资源管理层
                  ↓
引擎内容后端：本次比较 AssetBundle 与 Content Directories
                  ↓
平台存储与运行时加载
```

**Content Directories 改变的是第三层，句柄、引用计数和自动调度早已存在于第二层。** 上层框架可以把 Bundle 操作包装得很好，但仍需按后端提供的容器和对象能力工作。

这里用 Addressables 说明分层：Unity 的官方案例保留了业务调用，替换内容后端；框架接入了新后端，业务便不必直接操作 Root Asset 或 Loadable。它与直接调用引擎 API 是两种接入层次。[官方后端切换案例](https://unity.com/blog/content-directories-beyond-the-assetbundle)

## 2. 在引擎内容后端这一层比较

| 比较项 | AssetBundle | Content Directories（6.6） |
|---|---|---|
| 定义构建入口 | 明确的 Bundle 归属与构建输入 | Root Asset 及其可追踪引用 |
| 产物组织 | 每个 Bundle 对应归档容器 | 细粒度内容文件，可进一步归档 |
| 依赖表示 | Bundle 汇总依赖，另有对象加载所需的引用信息 | 内容文件／资产的细粒度依赖 |
| 同次构建共享资源 | 通过合理归属、共享包等方式避免重复 | 在一次内容构建内自动去重 |
| 按需使用 | 先取得容器，再加载目标资产 | 注册目录，再通过 Loadable 加载目标 |
| 原生释放接口 | `Unload(bool)`，区分容器与已加载对象 | `Loadable.Release()`，结束该实例持有的加载引用 |
| 远程内容 | 已有下载、缓存和框架生态可用 | 内建工作流面向本地内容，远程机制需另行提供 |

框架会在这些能力之上建立自己的策略。例如，Addressables 已经替用户处理 Bundle 依赖的加载与引用计数，这项便利不应拿来当作 CD 相对“成熟 AB 管线”的独占优势。[引擎层官方对照](https://docs.unity3d.com/6000.6/Documentation/Manual/content-directories-introduction.html)

## 3. 同一个共享依赖案例，两边分别承担什么责任

假设内容关系如下：

```text
角色 A → 材质 A → 共享贴图 T
角色 B → 材质 B → 共享贴图 T
```

### AssetBundle：先解决资源归属

如果 A、B 分别进两个 Bundle，而 T 没有明确的独立归属，它可能分别被带入两份构建产物。常见处理是给 T 安排共享 Bundle，再维护角色 Bundle 到共享 Bundle 的依赖。

原生 AssetBundle API 不替调用方完成依赖容器的加载；使用 Addressables 或其他管理层时，这部分工作由对应系统组织。[AssetBundle 依赖与重复规则](https://docs.unity3d.com/6000.6/Documentation/Manual/AssetBundles-Dependencies.html)

共享包也可以由构建工具自动分析、提取，不必靠人工逐个设置。成熟旧管线同样可以消除这种重复。差别在于，这套治理最终仍要输出一份具体的 Bundle 布局，团队要承担布局的生成、诊断和生命周期取舍。[Addressables 的重复依赖治理](https://docs.unity3d.com/Packages/com.unity.addressables@2.7/manual/AssetDependencies.html)

设计者仍需考虑：共享包是否过大，里面的资产是否拥有相近生命周期，以及常驻界面是否会长期保留其中一小部分内容。

### Content Directories：先确定内容构建的边界

把 A、B 放进同一次内容构建，系统可以处理共享产物，项目不必为了 T 再人为设计一个共享 Bundle。

但如果把 A、B 拆进两次独立的 Content Directory 构建，每次又都引用 T，两次输出各自独立，不能由此推断全项目只存在一份 T。

因此迁移时不应机械地把每个旧 Bundle 转成一个独立内容构建。旧布局中一些包只是为了共享依赖而存在，新的构建边界应重新按交付需求判断。

这也是官方创建文档建议先考虑一个根入口、按实际独立内容需求扩展的原因。[Root Asset 的组织建议](https://docs.unity.com/en-us/engine/6000.6/manual/assets-and-media/assets-managing-runtime/content-directories/create)

**这个案例解释的是构建去重。** A 退出而 B 还使用 T 时，两套系统都应保留 T；它不能用来证明 CD 释放得更细。

## 4. 打开容器与加载资产，分别发生了什么

这是讨论新旧方案时最容易夸大的地方。

AssetBundle 有容器加载与资产加载两个步骤。`LoadFromFile` 得到容器后，仍需调用 `LoadAsset` 等接口取得资产，不能据此写成“加载 Bundle 就把里面所有纹理全部读进内存”。压缩方式也会改变容器打开的实际成本。[LoadFromFile 的加载行为](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/AssetBundle.LoadFromFile.html)

Content Directories 改善的重点，是让依赖和释放边界少受预先设计的容器布局牵制，而不是发明了 AssetBundle 完全没有的“按需读取资产”。

反过来，调用 `Loadable<Prefab>.Load()` 也不意味着只加载一个孤立对象。Prefab 的直接依赖仍需满足；如果材质直接引用大贴图，贴图不会因为入口换成 Loadable 就失去加载成本。

## 5. 同包、不同生命周期：释放边界真正发生变化的地方

换一个案例：一张常驻大厅的 `LobbyIcon`，和一张只在活动页使用的 `EventPoster`。两张纹理互不依赖，但构建规则把它们放进同一个 `ui-textures` Bundle。两者都已加载，随后活动页关闭，大厅继续显示。

### Addressables 2.7.6 的 AB 策略

以该版本文档描述的正常引用计数路径为基线，不额外调用全局回收或自定义卸载逻辑：

1. 两份业务加载句柄都存活时，两张纹理已加载，Bundle 也被持有。
2. 释放 `EventPoster` 的业务句柄，其操作引用计数归零。
3. `LobbyIcon` 还在使用，所属 Bundle 的引用计数没有归零。按这套策略，海报不会仅因自己的业务句柄被释放便从内存卸载。

这是**指定框架版本与策略**的行为，不是断言所有 AssetBundle 使用方式都只能如此。旧管线可以将两张纹理按生命周期拆包，或增加对象回收策略；代价是更多布局与回收治理。[Addressables 2.7.6 的内存清理规则与同包案例](https://docs.unity3d.com/Packages/com.unity.addressables@2.7/manual/memory-assets.html)

### CD 的变化发生在底层持有单元

如果两张纹理落在独立内容产物中，彼此没有直接依赖，也没有其他使用者持有海报，释放海报的最后一份 Loadable 引用，就能结束它的加载生命周期；大厅图标继续保留。

这项能力减少了“仅为卸载边界而拆物理包”的需要。CD 也必须保留真实直接依赖与共享持有的内容；循环直接引用还会把独立单元重新关联起来。细粒度的价值，是避免**无依赖的同处一个容器**成为额外牵连。

### 为什么 Unload(false) 不能直接解决

`Unload(false)` 接收的是整个 Bundle，不接收某张无用纹理。它释放容器跟踪数据和缓冲，保留已经加载的对象，因此调用之后，海报仍可能存在。后续要怎样识别与回收无用对象、何时重新加载 Bundle、怎样维护依赖关系，都需要另行治理；重新加载还可能产生与保留对象重复的实例。

`Unload(true)` 会销毁从该 Bundle 加载的对象，也会影响仍在显示的大厅图标。因此这两个开关都没有提供“保留大厅图标、直接选择海报卸载”的通用接口。[Unload API 的对象保留、重载与依赖说明](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/AssetBundle.Unload.html)

观察结果时，把四个时刻分开：**业务句柄释放 → 底层内容具备卸载条件 → 对象实际销毁 → 进程内存变化**。引用计数只解释前两者的关系；对象销毁后，Unity 分配器或图形驱动仍可能保留已申请的内存。

## 6. 把 AB 拆小，能获得多少相同的收益

把大厅图标和活动海报分别放进两个 AB，旧管线同样可以在保留图标时卸载海报。按整包下载和更新的分发策略，还能只获取海报所在的小包。也就是说，合理拆包确实能获得一部分细粒度收益。

代价发生在这些小包又必须一起使用时：所需资产没有减少，却要打开更多容器、维护更多依赖和元数据。若把两个角色引用的同一贴图分别打进各自的包，还会产生重复；提取共享包能消除重复，但又要管理它的持有期。Unity 官方也将“大包减少容器开销、小包降低不必要驻留”列为需要权衡的两面。[Addressables 的包粒度与内存取舍](https://docs.unity3d.com/Packages/com.unity.addressables@2.7/manual/memory-assetbundles.html)

CD 的变化是把这个取舍进一步拆开：**归档负责物理文件组织与压缩，内层内容单元负责依赖和加载生命周期。** 图标和海报即使被装入同一个归档，也不必仅因物理上相邻就共同驻留。AB 也可以自建外层容器，利用 `LoadFromFile` 的 `offset` 从指定位置打开内层 Bundle，但索引、对齐和分发由项目额外处理；每个内层 AB 仍保留自己的加载与卸载边界。[AB 文件偏移 API](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/AssetBundle.LoadFromFile.html)

UnityDataTools 的[固定提交格式说明](https://github.com/Unity-Technologies/UnityDataTools/blob/db19dffbf40008e3a61e7566a5ddd6888eb68717/Documentation/contentdirectory-format.md)也明确区分了 6.6 与 6.7 的布局细节。这里采用的是内容文件与归档分层这一模型，不把后续版本的内部字段当成 6.6 契约。

## 7. 远程交付有三种选择

Unity 6.6 没有提供完整的 CD 远程交付工作流，**不等于这些文件不能放到 CDN 上下载**。运行时注册需要可用的本地内容；把一份完整、一致的内容构建下载并部署到本地，再注册，是自建分发层可以承担的工作。

| 方案 | 保留或获得什么 | 额外承担什么 |
|---|---|---|
| 继续 AB | 沿用现有远程目录、下载、缓存与回滚体系 | 继续治理 Bundle 布局与生命周期 |
| 本地 CD＋远程 AB | 本地使用新后端，远程沿用既有方案 | 两套构建和加载路径，跨后端共享依赖可能重复 |
| CD＋自建分发层 | 自己决定版本、下载与本地部署，再使用 CD 加载 | 完整性校验、原子切换、断线恢复、缓存淘汰与回滚 |

第三种方案的网络层不由 `RegisterContentDirectory` 代劳。第一篇的最小 Windows 例子只演示内容已经在本地时怎样接入。[6.6 的分发边界与混用说明](https://docs.unity3d.com/6000.6/Documentation/Manual/content-directories-introduction.html)

### 细粒度内容会不会下载更慢

如果同样约 100 MB 的内容全部需要，10 个约 10 MB 的包与 1,000 个约 100 KB 的包，传输字节数接近，请求数量却差了两个数量级。后者会增加请求调度、响应等待与缓存文件处理的次数，在高延迟网络和有限并发下尤其容易拖慢完成时间。Addressables 的 `Max Concurrent Web Requests` 就会把超过并发上限的请求排队。连接复用和并发能缓解等待，因此也不能直接按“文件数乘一次网络延迟”计算总耗时。[下载并发与排队设置](https://docs.unity3d.com/Packages/com.unity.addressables@2.7/manual/AddressableAssetSettings.html#downloads)

反过来，若当前只需要其中 5 MB，细粒度分发就可能通过少下载 95 MB 获益。这里分别比较的是“传完相同内容”和“只传当前需要的内容”，不能混用结果。

自建分发层可以把细粒度内容按使用时机组合成较大的下载单元；若选择每次整份替换归档，改动一个内部产物也可能需要重新下载整个归档。下载单位、更新单位与运行时加载单位要分别设计，归档本身不会自动提供细粒度远程补丁。

## 8. 哪些项目值得先试

以下是基于上述边界的工程判断：

- 本地内容占主体、资源迭代频繁、旧 Bundle 布局经常制造依赖与驻留问题的项目，适合做一段真实业务的对照验证。
- 远程热更新是核心、已有成熟缓存与回滚体系的项目，应先验证分发替代方案和混用成本。
- 瓶颈集中在脚本初始化、Shader 首次使用或实例化的项目，需要先定位关键路径，再估计内容后端能改善的部分。

这里的“先试”不等于直接迁移全项目。选一个角色预览、一个场景切换或一个独立内容包，保留原后端基线，再观察完整流程。

<!-- DATA-TODO: 补充同一业务片段的新旧后端构建布局、目标 Player 结果和内存快照；至少覆盖公共依赖、重复进入退出、常驻缓存与混用输出。 -->

下一篇继续拆[为什么可能更快]({{< relref "engine-toolchain/unity-content-directories-03-why-faster.md" >}})，把构建、加载、驻留和释放的收益分别归因。

**比较这两套方案，要比较依赖、生命周期与交付责任怎样变化，再决定迁移是否值得。**
