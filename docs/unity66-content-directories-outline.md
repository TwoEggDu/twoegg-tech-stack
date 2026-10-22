# Unity 6.6 Content Directories 三篇专题：大纲与证据记录

日期：2026-10-22。类型：原理篇，附最小使用流程。

## 定位

- 系列：Unity 资产系统与序列化，子线 D 的 D07–D09；沿用现有 canonical plan，不另建系列。
- 主问题：如何把构建组织、物理存储和加载生命周期拆开，减少对预先设计 Bundle 布局的依赖。
- 前置：AssetBundle 加载链、分包粒度、Addressables 管理层。
- 边界：Unity 6000.6 公开文档；不引用私有 C++ 源码；不把远程分发路线图写成现有功能。

## 分篇与大纲

用户于 2026-10-22 补充：除功能介绍外，还要与 AssetBundle 对比，并解释性能原因。按三个独立问题展开：

| 篇目 | 主问题 | 内容边界 |
|---|---|---|
| D07 原理与使用 | 是什么、怎么接进工程 | 以下原始大纲的 1–5，最小示例与生命周期 |
| D08 与 AssetBundle 对比 | 差别在哪里、什么时候迁移 | 构建/存储/加载/交付四层对照，共享依赖案例，Addressables 与本地远程取舍 |
| D09 为什么可能更快 | 收益从何而来、如何验证 | 增量重建、稳定 ID、并行流水线、驻留粒度、性能反例和对照实验 |

原始问题清单，按上述边界分配，避免三篇重复展开：

1. 项目问题：内容只用一部分，却受到 Bundle 依赖和生命周期影响。
2. 最小模型：构建可达图、物理存储、运行时引用、交付策略各自负责什么。
3. Content Directories：Root Asset、Manifest、细粒度产物；构建入口与加载单元的区别。
4. Loadable：构建包含与运行时驻留分离；每个所有者的引用实例。
5. 最小流程：定义目录、构建、部署、注册、加载、释放、注销。
6. 与 Addressables 的关系；本地和远程交叉依赖。
7. 性能与迁移边界；设计可复现验证，不编造测量。

## 已核实的公开来源

以下来源均在本轮读取，手册固定为 6000.6，核查日为 2026-10-22。

| 来源 | 支撑范围 |
|---|---|
| [Introduction](https://docs.unity.com/en-us/engine/6000.6/manual/assets-and-media/assets-managing-runtime/content-directories/introduction) | 本地分发、增量构建、同一构建去重与跨构建重复、混用风险 |
| [Create](https://docs.unity.com/en-us/engine/6000.6/manual/assets-and-media/assets-managing-runtime/content-directories/create) | Root ScriptableObject、默认单根建议、注册时加载根、构建步骤 |
| [References](https://docs.unity.com/en-us/engine/6000.6/manual/assets-and-media/assets-managing-runtime/content-directories/references) | 可追踪引用、Loadable、字符串遗漏、独立调用者各自持有实例 |
| [Load](https://docs.unity.com/en-us/engine/6000.6/manual/assets-and-media/assets-managing-runtime/content-directories/load) | 注册、获取根、注销时机、Assets 外构建、Web 部署差异 |
| [Build API](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/BuildPipeline.BuildContentDirectory.html) | BuildContentDirectoryParameters 与 BuildReport |
| [Loadable API](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Unity.Loading.Loadable_1.html) | Load、LoadAsync、Status、Target、Release、由 ID 构造实例 |
| [Release API](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Unity.Loading.Loadable_1.Release.html) | 释放引用的契约 |
| [Unity 官方介绍](https://unity.com/blog/content-directories-beyond-the-assetbundle) | 构建框架、稳定 ID 与内容哈希、Addressables 更换后端、远程路线图 |
| [UnityDataTools 格式说明](https://github.com/Unity-Technologies/UnityDataTools/blob/main/Documentation/contentdirectory-format.md) | Content File 与归档边界、6.6/6.7 格式差异、内部 manifest 不宜硬编码 |
| [官方示例仓库](https://github.com/Unity-Technologies/UnityContentBuildExamples) | 后续实践参考；本轮未运行示例 |
| [Unity 工程师公开答复](https://discussions.unity.com/t/new-unity-6-6-contentdirectory-good-and-bad-q-a/1732493/5) | 稳定 ID 与内容哈希、归档和独立目录的隔离边界 |
| [代码裁剪与内容](https://docs.unity.com/en-us/engine/6000.6/manual/programming-environment/script-compilation/managed-code-stripping/content) | 内容先构建，Player 构建接入内容报告目录，防止类型与模块裁剪 |
| [AssetBundle 依赖](https://docs.unity3d.com/6000.6/Documentation/Manual/AssetBundles-Dependencies.html) | 原生依赖加载责任、共享依赖与重复 |
| [AssetBundle.LoadFromFile](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/AssetBundle.LoadFromFile.html) | 区分容器加载与资产加载 |
| [AssetBundle.Unload](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/AssetBundle.Unload.html) | false 保留对象，true 销毁已加载对象 |

## 版本与证据限制

- 6000.6 Introduction 当前页面的循环引用段落包含 circularrefs.json 与 64 MB 限制；UnityDataTools 文档把 circularrefs.json 明确列为 6.7 行为，并说明 6.6 合并循环。因此正文只使用双方一致的结论：循环引用会破坏独立加载边界。不把报告文件名或数值门槛当作所有 6.6 补丁的契约。
- Addressables 包手册的直接页面本轮未能通过网页工具读取。正文只写已由 Unity 6.6 手册和官方博客确认的后端关系，不写具体包最低版本、右键菜单路径或完整迁移操作。
- 官方博客的性能案例属于官方指定 Beta 版本与机器，不是本仓实测。正文不迁移其比例作为项目收益承诺。
- C# 示例仅按公开 API 核对，未在 Unity Editor 中编译执行。
- Introduction 与代码裁剪专题出现不同的 Player 构建信息接口命名；正文不硬写接口名，提示使用对应补丁版本的构建报告接入接口。GetRootAssets(handle) 已另查 6000.6 API 重载。
- 待作者补充：具体 Unity/Addressables 版本、目标平台、构建报告、Profiler/Memory Profiler、冷暖缓存记录。

## 验证计划

- Hugo 构建确认 frontmatter、relref 和页面生成。
- 最小实验由作者在 Unity 6.6 项目执行：共享贴图的两个 Prefab，分别观察构建去重、两次 Load/Release、跨构建重复、引用循环和单资产修改后的增量构建。
- 本文件仅记录该篇执行信息；篇级目录以现有 series-plan 为准。

## 本轮验证结果

- 2026-10-22，Hugo v0.157.0 extended 构建退出码 0，1291 页，未输出 ERROR。
- 命令：`hugo --destination E:/workspace/TechStackShow/.codex_tmp/content-directories-20261022/site --cacheDir E:/workspace/TechStackShow/.codex_tmp/content-directories-20261022/cache --noBuildLock`。
- 三篇生成页面均存在，生成 HTML 中均包含另两篇链接；系列索引和 canonical plan 已回写。
- `git diff --check` 检查修改的索引与 plan 通过；plan 原有未提交改动已保留。
- 未运行 Unity Editor 编译、目标 Player、Profiler 或性能对照；未提交、推送或验证线上发布。
