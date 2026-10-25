# Addressables 与 YooAsset 源码解读系列规划

## 三个阅读入口（2026-10-21）

原来的混合系列按阅读目的分成三个入口，仍由本文件统一维护篇级规划；不另建相互重复的系列规划文件。

| 首页 | 主线归属 | 职责 |
|---|---|---|
| [YooAsset](../content/engine-toolchain/unity-yooasset-series-index.md) | Yoo-01～08，8 篇专题；Yoo-09～10，2 篇导读 | 独立组织 YooAsset 的加载、清单、下载缓存、构建和能力边界 |
| [Addressables](../content/engine-toolchain/unity-addressables-series-index.md) | Addr-01～05，5 篇 | 独立组织 Addressables 的加载、Catalog、引用计数、构建和能力边界 |
| [两者对比与选型](../content/engine-toolchain/unity-addressables-yooasset-comparison-series-index.md) | Cmp-01～03 与 Case-01～05，8 篇 | 连接两个系统的独立首页，组织同层对照、选型和故障案例 |

三页互相链接；18 篇原有正文增加相应首页的返回入口，原文件路径和 slug 保持不变。前置知识和其他系列的选型文章通过相关链接进入，不改变其原系列归属。

内容元数据使用各自的 `series`、`primary_series` 与 `series_order`；首页使用站点已有的 `series_id`、`series_role`、`series_entry` 等字段，在引擎工具链栏目显示三个独立入口。跨栏目的五篇问题解决案例由对比首页显式列出。

## 3.0.6 内容基线与本轮状态

- YooAsset 主线采用官方 tag `3.0.6`，固定提交 `3b4cfb36cc2e81b9558ab74d65fee2cf2eac2804`。本地源码位于忽略目录 `.codex_tmp/yooasset-source/3.0.6/`。
- 原 Yoo-01～05 已按固定源码重审；新增 Yoo-06 整体架构、Yoo-07 文件系统、Yoo-08 生命周期。稳定编号保留，首页阅读顺序改为 06 → 07 → 01 → 02 → 03 → 08 → 04 → 05。
- Addressables 五篇、对比三篇与案例五篇保留原内容基线；本轮未将它们声明为 3.0.6 对比实证。共同首页标注版本范围。
- 本轮交付为源码核对正文、代码示例与可复现验证方案；Unity 构建、播放器、设备、网络和性能实验尚待执行，文中没有伪造实测数据。
- 后续主题：完整更新与失败恢复、异步操作与时间片、调试与构建报告实验、WebGL 与小游戏平台验证。

## 系列定位

这个系列不写成 "Addressables 配置教程" 或 "YooAsset 接入指南"。

它真正要解决的问题是：

`把 Addressables 和 YooAsset 的运行时、构建期和治理层分别拆到源码级别，让读者能从"会用框架"推进到"能读懂这两套系统为什么这样设计、各自强在哪、弱在哪"。`

一句话说，这个系列的重心是：

`两套资源交付框架的源码结构理解 + 工程判断能力。`

---

## 为什么要单独开一个系列

现有的 "Unity 资产系统与序列化" 系列已经覆盖了：

- AssetBundle 底层格式和运行时加载链（源码级）
- Addressables vs YooAsset 的对比和选型（概念级）
- Group 布局和打包配置（实操级）

但缺的恰恰是中间层：

`这两套框架内部到底怎么工作的？`

没有这一层，"官方抽象一致性"和"交付工程控制力"就永远停在标签上——读者知道该选谁，但不知道为什么。

这个系列补的就是这块：两个框架各自的源码结构、关键链路和设计取舍。

---

## 和现有系列的关系

### 相对 "Unity 资产系统与序列化"

那个系列负责解释：
- 资产系统底层（引用、序列化、恢复链）
- AssetBundle 格式和引擎层机制
- 框架选型的概念判断

这个系列负责继续往下走：
- Addressables 内部到底怎么调度
- YooAsset 内部到底怎么调度
- 两套系统在同层问题上的设计差异
- 项目应该在哪些具体环节做取舍

### 相对 "Unity 异步运行时"

异步运行时系列讲的是 Task/UniTask/PlayerLoop 层。
本系列里涉及 AsyncOperationHandle 时只讲 Addressables 怎么用它，不重讲异步原语本身。

---

## 目标读者

- 已经在用 Addressables 或 YooAsset，但遇到问题只能靠官方文档和猜的人
- 需要在两套系统之间做技术选型，但不想只看功能表的人
- 做客户端基础架构、工具链、热更资源链的人
- 计划自研资源管理系统，想先看两套成熟方案怎么做的人

---

## 系列结构（23 篇正文，含加载与分包两篇流程导读）

### 主线 A：Addressables 源码解读（5 篇）

职责：沿 Addressables package 源码，把运行时、构建期和更新链路拆到类/方法级别。

- `Addr-01` **Addressables 运行时四角色与 LoadAssetAsync 完整链路**
  ResourceManager / IResourceLocator / IResourceProvider / AsyncOperationHandle，从 key 到对象就绪的全链
  **状态：已完成**

- `Addr-02` **ContentCatalogData 到底存了什么：编码格式、加载成本和更新机制**
  m_KeyDataString / m_BucketDataString / m_EntryDataString 的 Base64 编码结构，catalog 加载的主线程成本，catalog.json vs catalog.bin（Unity 6），远程 catalog 更新的完整流程和失败恢复

- `Addr-03` **Addressables 的引用计数和生命周期：为什么 Release 比 Load 更难做对**
  AsyncOperationHandle 的 refcount 机制、operation cache、Release 触发 Unload 的条件链、常见泄漏模式和 Event Viewer 诊断

- `Addr-04` **Addressables 构建期到底做了什么：从 Group Schema 到 bundle 产物**
  BuildScriptPackedMode / SBP task chain / BundleWriteData / Catalog 生成 / content_state.bin，Content Update Build 的差量机制

- `Addr-05` **Addressables 的边界：它接不住什么、项目必须自己补什么**
  下载队列控制、断点续传、灰度发布、多环境版本共存、审核包 / 线上包分离

### 主线 B：YooAsset 资源管理与交付（8 篇专题与 2 篇导读）

职责：按 3.0.6 固定源码解释模块职责、调用链与条件，再提供可复现实验。保留稳定编号，以单次加载导读连接基础接口与加载复用专题，以分包导读连接具体资源与构建产物专题。

- `Yoo-06` **[整体架构与初始化](../content/engine-toolchain/unity-yooasset-architecture-package-initialization.md)**
  Package、清单、文件、对象如何协作；初始化之后还需要哪些条件。

- `Yoo-07` **[文件系统与运行模式](../content/engine-toolchain/unity-yooasset-filesystems-playmodes.md)**
  理解文件系统组合、Bundle 路由、主文件系统与 BundleCache。

- `Yoo-09` **[LoadAssetAsync 单次加载流程导读](../content/engine-toolchain/unity-yooasset-loadassetasync-single-request-walkthrough.md)**
  沿一次普通 Prefab 请求串起地址解析、Handle 返回、逐帧推进、Bundle 加载与 Asset 提取；对应 Unity 的两个异步加载接口。
  2026-10-22 补充；放在文件系统与加载复用专题之间（`series_order: 2.5`）。本文为固定源码导读，断点步骤尚未附运行记录。

- `Yoo-01` **[运行时加载与请求复用](../content/engine-toolchain/unity-yooasset-runtime-internals-loadassetasync-to-bundle-loaded.md)**
  追踪 Provider 和 Loader 的复用、依赖等待与对象提取。

- `Yoo-02` **[Manifest、内容身份与完整性](../content/engine-toolchain/unity-yooasset-manifest-version-fileindex-lookup.md)**
  拆开定位索引、资源与 Bundle 依赖、清单和文件的验证职责。

- `Yoo-03` **[下载调度与磁盘缓存](../content/engine-toolchain/unity-yooasset-downloader-cache-system-resume-verify.md)**
  核对下载集合、续传条件、缓存提交和启动校验。

- `Yoo-08` **[资源生命周期与卸载](../content/engine-toolchain/unity-yooasset-handle-lifetime-unload.md)**
  明确实例、Handle、Provider、Loader 和文件缓存各自的持有期。

- `Yoo-10` **[分包流程：从一个 Prefab 到共享 Bundle](../content/engine-toolchain/unity-yooasset-bundle-layout-prefab-to-shared-bundle.md)**
  沿单 Prefab、跨包共用与 AB/CD 引用组合推演目录分包和两个构建开关，说明引用包集合分组的自定义扩展点与工程代价。
  2026-10-23 补充；放在资源生命周期与构建专题之间（`series_order: 6.5`）。正文为 3.0.6 固定源码推演，含 Mermaid 流程图；补充引用集合分组的前置索引、稳定命名、构建任务替换与入口接入参考代码。示例未在 Unity 中编译运行或接入项目，对照构建与设备性能尚未验证。

  2026-10-25 页面修订：分包导读及 YooAsset 首页复用 `reading-series` 样式，栏目指引为全栈框架 → 客户端框架 → YooAsset；正文与文章 URL 保持。首页增加目录返回锚点，YooAsset 的显示页码按真实阅读位置计算，保留既有排序值；共享模板的父级入口、阅读路线及按位置编号均为可选设置，其余系列沿用原默认行为。

- `Yoo-04` **[资源收集、构建与产物](../content/engine-toolchain/unity-yooasset-build-time-collector-to-bundle-output.md)**
  分清收集规划、构建缓存复用、完整版本产物与差异交付。

- `Yoo-05` **[框架能力与项目职责](../content/engine-toolchain/unity-yooasset-boundaries-what-projects-must-build.md)**
  把工具、发布选择、灰度、CI 和长期监控按契约分开。

### 对比线 C：同层问题对照（3 篇）

职责：在读完两条主线后，把同一组问题放到一起对比，让选型判断从"印象"变成"证据"。

- `Cmp-01` **运行时调度对比：ResourceManager vs ResourcePackage，谁的调度模型更适合你的项目**
  定位机制、Provider 链 vs Loader 链、引用计数 vs 手动管理、异步模型差异

- `Cmp-02` **构建与产物对比：Catalog vs PackageManifest，Group vs Collector，content_state vs 版本快照**
  构建输入定义、产物格式、增量构建、内容更新机制的结构对比

- `Cmp-03` **治理能力对比：谁的版本控制、缓存管理、下载治理和回滚机制更成熟**
  版本号体系、缓存清理、下载队列、断点续传、回滚快照、多环境发布——逐项对比两者的默认能力和扩展空间

### 实战线 D：生产踩坑案例（5 篇）

职责：用真实生产场景串联前面三条线的源码知识，每篇追一个高频事故从"现象"到"源码根因"到"修复路径"。

- `Case-01` **热更资源下载到一半断了：两个框架的断点续传与失败恢复机制对比**
  Addressables 的 UnityWebRequestAssetBundle 没有原生断点续传 / YooAsset 的 ResourceDownloaderOperation 内建重试和校验 / 两者在网络异常场景下的表现差异 / 最小恢复方案

- `Case-02` **Catalog / Manifest 更新成功但 bundle 没下完：半更新状态的诊断和恢复**
  Addressables UpdateCatalogs 替换 IResourceLocator 但新 bundle 还在 CDN / LoadAssetAsync 返回 error / 诊断方法 / YooAsset 三步分离为什么更安全

- `Case-03` **线上版本需要紧急回滚：两个框架的回滚路径和代价**
  Addressables 回滚 = 回退 catalog.hash / 旧 bundle 可能已被 ClearOtherCachedVersions 清掉 / YooAsset 回滚 = 切换 PackageVersion / 回滚粒度和缓存策略差异

- `Case-04` **Handle 忘记 Release 导致内存持续增长：Addressables 引用计数泄漏的定位和修复**
  Event Viewer 看 refcount 不归零 / 追调用栈找泄漏点 / 常见模式 / 自动化检测方案

- `Case-05` **从 Addressables 迁移到 YooAsset（或反过来）：迁移路径、兼容层和典型翻车点**
  地址系统迁移 / 构建产物迁移 / 运行时 API 适配层 / 缓存兼容性 / 最容易翻车的三个点

---

## 推荐阅读与维护顺序

1. 系统学习单个框架时，从对应首页进入，沿运行时、索引、生命周期或下载缓存、构建、边界阅读。
2. 做技术选型时，从共同首页进入，先看问题空间，再对照运行时、构建产物和交付治理。
3. 处理事故时，从共同首页的案例表进入；需要补机制时返回各自首页。
4. 新增单框架文章时更新对应首页和本规划；新增比较或跨框架案例时更新共同首页和本规划。根 `doc-plan.md` 继续只维护系列路由。

---

## S 级质量标准

要把这个系列做到 S 级，每篇文章必须满足：

### 1. 源码级证据
- 每个关键判断必须落到具体的类名、方法名和调用链
- 给出 package 源码路径（如 `com.unity.addressables/Runtime/ResourceManager/ResourceManager.cs`）
- 不能只说"Addressables 用 Provider 模式"，要说"ResourceManager.ProvideResource 从 m_ResourceProviders 字典里按 location.ProviderId 查找 IResourceProvider 实例"

### 2. Mermaid 流程图
- 每篇至少 2 张 Mermaid 图
- 运行时链路文章：调用链流程图 + 状态机图
- 对比文章：左右并列对照图

### 3. 工程判断收束
- 每篇末尾必须收到"这个机制对项目意味着什么"
- 不能停在"源码是这样的"，必须推到"所以你该怎么用 / 怎么避坑"

### 4. 版本标注
- 标注 Addressables package 版本（当前主力：1.21.x，Unity 6 随附 2.x）
- YooAsset 主线标注 `3.0.6` 与固定提交；比较和历史案例另行声明版本，不混用 2.x 兼容层与 3.x 原生 API。
- 在机制有版本差异的地方加 Unity 6 注记

### 5. 可复现性
- 关键链路配最小代码示例（调用代码 + 预期输出）
- 对比文章配决策表（项目条件 / 推荐方案 / 原因）

---

## 每篇文章的统一写法约束

- 不写成 API 文档或配置手册
- 每篇只追一条主链路，不平铺所有 API
- 先给地图（这条链路有哪些阶段），再钻每个阶段的关键实现
- 源码引用必须标注版本和文件路径
- 对比文章不做好坏判断，只做结构差异 + 适用场景映射
- 文末必须落一张判断表或检查表

---

## 证据来源

| 优先级 | 来源 | 说明 |
|--------|------|------|
| 1 | Addressables package 源码 | `com.unity.addressables` 和 `com.unity.scriptablebuildpipeline` |
| 2 | YooAsset GitHub 源码 | `https://github.com/tuyoogame/YooAsset` |
| 3 | 构建产物实证 | Catalog / Manifest / BuildLayout / 缓存目录 |
| 4 | 官方文档 | Unity Manual + YooAsset README/Wiki |
| 5 | 项目实测 | 实际设备上的加载耗时和内存数据（后续补） |
