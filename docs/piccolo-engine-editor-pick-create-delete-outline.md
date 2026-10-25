# Piccolo 18：编辑器怎样拾取、创建和删除场景对象

> 单篇执行提纲与证据记录；canonical 篇级目录为 `piccolo-engine-series-plan.md`。

## 定位与边界

- 类型：Piccolo 源码分析，先解释选择、实例化和删除分别改变什么状态，再沿固定实现展开。
- 解决问题：点击像素如何返回对象；点击资源如何成为新实例；删除逻辑对象后渲染端如何获知，以及哪些清理并不完整。
- 固定只读源码：`BoomingTech/Piccolo@f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`。
- 前置：04 对象恢复、06 逻辑到渲染、08 保存重载、15 GPU 资源；复用 PCPP-1 生命周期结论，不重复完整 C++ 容器课程。
- 不扩展 Undo/Redo、通用编辑器框架或物理系统；不修改源码缺陷，不把 Shader/CPU 独立检查记成引擎运行。
- 正文 series_order=21，接 17。完成后系列共 22 篇；09/10/12/13/14 五篇保持待写。

## 提纲

1. 问题与最小状态模型：文件定义、Level 对象、渲染 part/instance、选择 ID、持久化状态。
2. 两个选择入口：对象列表直接使用 GObjectID；视口鼠标经 UV、viewport 与 ID 图像回读后反查对象。
3. 拾取绘制：主相机可见节点、R32_UINT、深度和整数输出，framebuffer extent 与内容 viewport 的区别，蒙皮最终位置限制。
4. 同步回读：绘制提交/fence、整幅图像复制/staging、single-time queue idle、索引读数；不宣称异步一像素读取。
5. ID 域及错误边界：instance GUID 的无效值 0；GObjectID 无效值 size_t 最大值；查找缺失实际返回 0，不能写成自动取消选择。
6. 从 object 资源创建：名字计数、空实例覆盖、定义路径、create/load/postLoad/入表/选择；原点默认与资源定义相关，不是鼠标落点放置。
7. 从逻辑创建到可见：Transform dirty、Mesh tick、交换数据、part ID 与资源缓存；不保证即刻显示或即刻可拾取。
8. 删除：命令位、Level erase/角色关系、局部 shared_ptr、删除描述、交换消费、清选择；源代码只删除 part 0/首项并保留 allocator，单/多 part 分开。
9. 保存与验证：接回 08，列断点/观察任务及独立 Shader、坐标、原函数边界检查。

## 执行清单

- [x] 核对 UI、输入、scene manager、Level/GObject 与组件创建/删除路径。
- [x] 核对 PickPass/Shader、ID 映射、深度、坐标、回读和同步源码。
- [x] 对两份原始拾取 Shader 独立编译，检查资源样例与原函数的 ID/删除边界。
- [x] 完成正文、图表、代码节选与固定提交来源。
- [x] 更新 canonical 目录、入口、相关阅读与根系列路由。
- [x] Hugo 构建、引用/源码节选、章节顺序与生成页链接检查。

## 证据入口

- editor_input_manager.cpp、editor_ui.cpp、editor_scene_manager.cpp、editor_file_service.cpp、editor.cpp。
- framework/level/level.cpp、object/object.cpp、object_id_allocator.h/.cpp、mesh_component.cpp、transform_component.cpp。
- render_scene.cpp、render_system.cpp、render_pipeline.cpp、render_guid_allocator.h、render_swap_context.cpp、passes/pick_pass.cpp。
- mesh_inefficient_pick.vert/.frag；wall.object.json；Vulkan VkFormat、vkCmdCopyImageToBuffer、vkWaitForFences 官方资料。

## 验证记录

2026-10-25 完成正文、图表与导航。源码、CPU 条件、Shader 编译和设备运行分层记录。

### 固定源码与正文审查

- 本地源码 HEAD 为 `f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，工作树只读且 `git status --porcelain` 为空。
- 正文 23 个固定提交 blob 链接均有本地文件（含许可证），八段 GLSL/C++ 节选忽略空白后与对应文件匹配。
- 核对两个选择入口、内容区域/viewport/extent 换算、左键 action 未过滤、可见节点到 instance ID、R32_UINT 输出、深度与整幅 staging 回读、fence/queue idle 路径。
- 核对三个关键限制：映射缺失返回逻辑 ID 0 而非 size_t 最大值；拾取 gl_Position 未消费蒙皮 model_position；渲染删除只删首个映射和 part 0 实体，并保留 allocator 记录。
- 创建路径核对 object 类型、文件名计数、定义路径、空实例覆盖、load 后入表、成功后选择、后续 dirty/交换消费；没有将该入口描述为鼠标落点放置、自动保存或全局唯一名字协议。
- 删除路径核对 Delete 命令位、局部强引用、角色对象解绑、逻辑表移除、删除描述、清选择与渲染端消费；保存回链 08，不扩写 Undo/Redo 或完整资源释放。

### 独立 Shader 与 CPU 检查

全部临时文件置于 `%TEMP%/TechStackShow-piccolo-editor-18-20261025/`，不加入仓库。

1. Piccolo 随附 glslangValidator 使用原 shader/include、`-V100` 编译两份原始 GLSL，均返回 0。SPIR-V 文件头 magic 为 `0x07230203`、版本 1.0；未运行 GPU pipeline 或独立 SPIR-V 验证器。

| Shader | SPIR-V 字节 | SHA-256 |
| --- | --- | --- |
| mesh_inefficient_pick.vert | 6844 | `e398fc8754396a707e1869ec58bb8d8ca5bf1e36da776f7fd0526e580f9b6aa6` |
| mesh_inefficient_pick.frag | 464 | `1bc403d03711ffeacbeab896a10e61eb19e734c0041054d1ecba62da7a0993cf` |

2. GCC 8.3.0，`-std=c++17 -O0 -Wall -Wextra`：两个最小适配程序均编译、运行返回 0。RenderScene 两个原函数、GameObjectPartId 类型与坐标前缀按原文本提取，统一换行后逐字核对；GUID 分配器和逻辑 ID 类型直接包含原头文件。替身仅提供所需容器、实体 ID、viewport/RHI 返回结构，未编译整个 RenderScene/PickPass。
3. 原映射函数：有效 instance 返回目标对象；缺失/0 instance 返回 GObjectID 0，确认其不同于逻辑无效值。
4. 原删除函数：单 part 0 时实体/映射清空而 allocator 保留；双 part 0/1 时只移除 part 0 实体和一个映射，重复删除后 part 1 实体仍在。不依赖无序映射的遍历顺序，不把容器结果说成已复现 GPU 残影。
5. 坐标前缀：viewport `(320,100,800,600)`、extent `1920×1080`、UV `(0.5,0.5)` 得索引 768720；验证内容右下边界仍可在整图范围内，以及整图越界的提前返回。未测试负输入强转、真实 DPI/窗口事件或 GPU 图像内容。
6. 读取实际 wall.object.json，确认 Transform/Mesh/RigidBody、原点/单位变换与一个网格 part；未执行反序列化器、postLoad 或刚体创建。

| 产物 | SHA-256 |
| --- | --- |
| editor_checks.cpp | `587e67a40f4c10e1c92511eb475d5ebccdfd3e28aba440d9139cc8044603aa3d` |
| coordinate_checks.cpp | `e5d9dda54ab9d0f6c512ea3883cb249c1c0010f923671f06556b46c291fb217f` |
| scene_functions.inc | `3b61bdff6295fd182c35e8a0c9df9366c407ead7833a7bf7fd3272cc22060773` |
| coordinate_prefix.inc | `0227701dc1c42017a3d4c5ce05004d9dba639fdcff6c728f34824c01428f5e03` |

### 站点与后续验证

- Hugo 0.158.0 构建返回 0，无 ERROR。共享工作树本次生成 1376 页、54 个静态文件；输出/cache 在临时目录，数量包含其他任务已有内容。
- 22 篇正文的 series_order 连续为 0～21。检查正文与三个入口共 25 个生成页，1479 个本地链接/资源引用及 HTML 锚点存在；八段源码节选、一份 Mermaid 图源码与共享 series-reading.css 引用通过静态检查。
- 确认 17 下一篇指向 18、18 上一篇返回 17、18 末尾回目录，三个入口及 08/06/PCPP-1 均链接新正文。根 doc-plan 仅更新系列概况，篇级状态由 canonical plan 维护。
- 主线 09、10、12、13、14 五篇保持待写；新正文与入口没有将选题编号空缺视为已有文章。
- 完整引擎构建、GPU 拾取/回读、实际创建/删除/保存和浏览器目视未验证。正文保留断点与观察表；Mermaid 仅检查生成源码，未宣称实际图形渲染通过。
- 未修改 Piccolo 源码、资源样例、共享布局/CSS；未暂存、提交或推送本轮内容。
