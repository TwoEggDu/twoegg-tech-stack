# Addressables 与 YooAsset ���码解读系列规划

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

## 系列结构（18 篇）

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

### 主线 B：YooAsset 源码解读（5 篇）

职责：沿 YooAsset 源码，把同一组问题从 YooAsset 的视角拆到类/方法级别。

- `Yoo-01` **YooAsset 运行时四阶段与 LoadAssetAsync 完整链路**
  ResourcePackage / PackageManifest / BundleLoaderBase / AssetOperationHandle，从 address 到对象就绪的全链

- `Yoo-02` **PackageManifest 到底存了什么：版本校验、文件索引和运行时查找**
  PackageManifest 的序列化结构（AssetInfo / BundleInfo / dependBundleIDs），版本号 + CRC + 文件 Hash 三层校验，和 Addressables Catalog 的结构对比

- `Yoo-03` **YooAsset 的下载器和缓存系统：ResourceDownloaderOperation、CacheFileSystem 和断点续传**
  下载队列、并发控制、校验重试、断点续传的实现路径，CacheFileSystem 的磁盘结构和版本清理

- `Yoo-04` **YooAsset 构建期到底做了什么：从 Collector 到 bundle 产物**
  AssetBundleCollector / CollectMode / FilterRule / 依赖分析 / bundle 命名 / 构建报告，和 Addressables Group Schema / SBP 的对应关系

- `Yoo-05` **YooAsset 的边界：它接不住什么、项目必须自己补什么**
  Addressables 式的官方工具链集成、Editor 内 Analyze 规则、和 Unity 新特性的跟进节奏

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

## 推荐推进顺序

```
Phase 1 - 运行时链路:
  Addr-01  已完成
  Yoo-01   下一篇
  Cmp-01   两篇运行时都写完后立即对比
  Case-04  Handle 泄漏案例（紧跟运行时链路）

Phase 2 - 数据层深度:
  Addr-02（Catalog 深度）
  Yoo-02（Manifest 深度）
  Cmp-02（构建与产物对比）
  Case-02  半更新状态案例（紧跟 Catalog/Manifest）

Phase 3 - 生命周期与缓存:
  Addr-03（引用计数和生命周期）
  Yoo-03（下载器和缓存系统）
  Case-01  断点续传失败案例（紧跟下载器）

Phase 4 - 构建期:
  Addr-04（构建期）
  Yoo-04（构建期）

Phase 5 - 边界与治理:
  Addr-05（Addressables 边界）
  Yoo-05（YooAsset 边界）
  Cmp-03（治理能力对比）
  Case-03  线上回滚案例（紧跟治理对比）

Phase 6 - 迁移:
  Case-05  框架迁移案例（系列收尾篇）
```

这个顺序的好处是：
- 先各自追运行时链路（最核心的能力层）
- 马上做一次对比 + 一个案例，第一时间建立"同层问题不同解法"的认知
- 再各自深入 Catalog/Manifest、生命周期/缓存，每层配一个对应案例
- 最后收到构建期和边界，让"选型判断"有完整证据
- 迁移案例放最后，作为系列收尾

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
- 标注 YooAsset 版本（当前主力：2.x）
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
