---
title: "资源管线 02｜资源序列化——资产怎么变成字节"
slug: "delivery-resource-pipeline-02-serialization"
date: "2026-04-14"
description: "资源序列化决定了资产在磁盘和内存中的形态。文本还是二进制、自描述还是 Schema 驱动、可读还是高效——选型影响构建速度、加载性能和调试便利性。"
tags:
  - "Delivery Engineering"
  - "Resource Pipeline"
  - "Serialization"
series: "资源管线"
primary_series: "delivery-resource-pipeline"
series_role: "article"
series_order: 20
weight: 420
delivery_layer: "principle"
delivery_volume: "V05"
delivery_reading_lines:
  - "L1"
  - "L2"
---

## 这篇解决什么问题

资源管线的第一步是把引擎中的资产对象转换成可以存储和传输的字节序列——序列化。序列化格式的选择直接影响构建产物的体积、加载速度和调试便利性。

## 本质是什么

序列化要做的事情很简单：把内存中的对象图（Object Graph）转换成字节流，使其可以写入文件或通过网络传输；反序列化则是逆过程。

但在游戏项目中，序列化不只是"转换格式"。它需要处理以下工程问题：

### 对象图中的引用关系

游戏资源不是孤立的。一个 Prefab 引用了多个 Material，Material 引用了 Texture 和 Shader，Texture 可能被多个 Material 共享。

序列化必须处理这些引用关系：
- **内部引用**：同一个包内的对象互相引用——序列化时用局部 ID 表示
- **外部引用**：跨包的对象引用——序列化时用全局标识符（GUID + fileID）表示
- **循环引用**：A 引用 B、B 引用 A——需要检测并正确处理
- **共享对象**：同一个 Texture 被多个 Material 引用——序列化时只存一份，用引用指向

### 版本兼容性

序列化格式会随引擎版本演进。一个用引擎 v2023 序列化的资源文件，在引擎 v2025 中能不能正确反序列化？

两种策略：
- **向前兼容**：新版本可以读旧格式。通过版本号字段和默认值处理缺失字段
- **重新序列化**：升级引擎版本时强制重新序列化所有资源。干净但耗时

### 平台适配

同一份资源在不同平台上的序列化结果可能不同：
- 字节序（Big Endian vs Little Endian）
- 指针大小（32 位 vs 64 位）
- 纹理压缩格式（ASTC vs ETC2）
- 对齐要求（某些平台要求数据对齐到特定边界）

序列化必须在构建时针对目标平台生成正确的格式，不能运行时再转换。

## 序列化格式的选型维度

| 维度 | 文本格式（JSON/YAML） | 二进制格式（自定义/FlatBuffers） |
|------|---------------------|-------------------------------|
| 可读性 | 好，可以直接查看和编辑 | 差，需要工具解析 |
| 文件体积 | 大（文本冗余） | 小（紧凑编码） |
| 解析速度 | 慢（需要词法/语法分析） | 快（直接内存映射或顺序读取） |
| Diff 友好 | 好（文本 diff 可读） | 差（二进制 diff 不可读） |
| 版本兼容 | 容易（字段缺失用默认值） | 需要设计（版本号 + 迁移逻辑） |

### 开发期和发布期用不同格式

最佳实践是在不同阶段使用不同的格式：

**开发期**用文本格式：
- Version Control 中的 diff 可读
- 合并冲突可以手动解决
- 可以用文本编辑器快速查看和修改

**发布期**用二进制格式：
- 构建产物体积最小
- 加载速度最快
- 不可逆向（一定程度的安全性）

Unity 的做法正是如此：编辑器中资源用 YAML 格式存储（.meta + 文本序列化），构建时转换为二进制格式打入 AssetBundle。

## Unity 的序列化系统

Unity 有自己的序列化系统，理解它的特点对资源管线设计很重要：

### GUID + fileID 引用体系

Unity 用 GUID 标识资产文件，用 fileID 标识文件内的子对象。每个引用关系都通过 `{guid, fileID}` 对表示。

```yaml
# Unity YAML 序列化中的引用
m_Material: {fileID: 2100000, guid: abc123def456, type: 2}
```

这套引用体系的工程影响：
- 移动或重命名文件不会破坏引用（GUID 不变）
- 但删除 .meta 文件会丢失 GUID，导致所有引用断裂
- 跨项目复制资源时必须保留 .meta 文件

### AssetBundle 序列化

AssetBundle 的序列化在 Unity 的标准序列化基础上增加了：
- **压缩**：LZ4（快速解压，适合运行时）或 LZMA（高压缩比，适合分发）
- **依赖记录**：Bundle 的 Manifest 记录了外部依赖列表
- **类型树**（TypeTree）：可选的自描述信息，用于跨版本兼容

TypeTree 的工程选择：

| 选项 | 包体影响 | 兼容性 |
|------|---------|--------|
| 包含 TypeTree | 增大约 5-15% | 跨 Unity 版本可读 |
| 不含 TypeTree | 更小 | 必须用相同 Unity 版本构建和读取 |

如果项目不需要跨 Unity 版本的 Bundle 兼容（绝大多数项目不需要），关闭 TypeTree 可以减少包体。

## 序列化与交付链路的关系

| 交付环节 | 序列化的影响 |
|---------|------------|
| 构建段 | 序列化格式决定构建产物体积和构建时间 |
| 验证段 | 序列化正确性影响加载测试结果 |
| 发布段 | 压缩方式影响下载大小和首次解压时间 |
| 版本管理 | 开发期的文本序列化影响 diff 可读性和合并冲突处理 |
| 热更新 | 序列化格式一致性影响热更包能否被正确加载 |

## 常见事故与排障

**事故：Unity 版本升级后旧 Bundle 无法加载**。项目升级了 Unity 版本，但 CDN 上还有旧版本构建的 Bundle。旧 Bundle 的序列化格式和新版本的反序列化逻辑不兼容。如果 TypeTree 被关闭了，连兼容回退都没有。

**排查路径**：确认 Bundle 的构建 Unity 版本和运行时 Unity 版本是否一致。如果不一致且 TypeTree 关闭，必须用对应版本重新构建。

**事故：.meta 文件被删除导致大面积引用断裂**。某次 Git 操作不慎删除了一批 .meta 文件。GUID 丢失后，所有引用该资源的 Material、Prefab 的引用字段变为 Missing。

**排查路径**：从 Git 历史中恢复 .meta 文件。预防手段：CI 中加入 .meta 文件完整性检查——每个资源文件必须有对应的 .meta。

## 小结与检查清单

- [ ] 开发期是否使用文本序列化（便于 diff 和冲突解决）
- [ ] 发布期是否使用二进制序列化 + 压缩
- [ ] AssetBundle 的 TypeTree 选项是否明确（是否需要跨版本兼容）
- [ ] .meta 文件是否入版本库且有完整性检查
- [ ] 序列化格式是否在 Unity 版本升级时评估兼容性
- [ ] 压缩方式是否按分发场景选择（LZ4 运行时 / LZMA 分发）

---

**下一步应读**：[打包策略设计]({{< relref "delivery-engineering/delivery-resource-pipeline-03-bundling-strategy.md" >}}) — 序列化解决了"单个资源怎么变成字节"，打包解决"多个资源怎么组合成包"

**扩展阅读**：[Unity 资产系统与序列化系列]({{< relref "engine-toolchain/unity-asset-system-and-serialization-series-index.md" >}}) — GUID/fileID、Importer、序列化对象图的完整技术深挖
