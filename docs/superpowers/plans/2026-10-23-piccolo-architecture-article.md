# Piccolo 首篇架构地图执行计划

> 执行方式：当前会话直接完成文章、入口回写与验证。本文只记录首篇生产，不另建篇级状态系统。

**Goal:** 将已讨论的 Piccolo 架构地图落成一篇可阅读、可定位源码、可继续设计调试实验的正文。

**Architecture:** 按问题空间、通用模型、模块职责图、构建图、运行数据流和验证入口展开。使用固定开源提交证明实现事实；工程判断单独说明，实际运行观察保留待验证状态。

**Tech Stack:** Hugo、Markdown、Mermaid；Piccolo C++17 / CMake 源码作为只读证据。

**Spec:** 本会话确认的方向：读者熟悉 C++ 调试；目标是学会读懂并调试一台引擎；先有架构总览，再进入执行链。

## 范围与约束

- 固定源码：`BoomingTech/Piccolo@f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`。
- 类型：架构原理篇，系列首篇 `00`；正文放在 `content/system-design/`。
- 三层规划：根路由只增加系列行，篇级状态只在 `docs/piccolo-engine-series-plan.md` 维护。
- 共享工作区保留所有已有改动，仅修改下列白名单；不提交、推送或发布。
- 不构建或修改 Piccolo；不把拟定断点实验写成已完成观察。
- 文档改动用构建、链接和图形渲染核验，不增加重复实现的单元测试。

## 文件白名单

1. 新增 `content/system-design/piccolo-engine-00-architecture-map.md`：首篇正文。
2. 新增 `docs/piccolo-engine-series-plan.md`：唯一系列规划。
3. 新增本执行计划：提纲、证据入口与验证记录。
4. 修改 `doc-plan.md`：增加系列级路由。
5. 修改 `content/reading/piccolo-engine.md`：接入首篇并调整已有内容说明。
6. 修改 `content/reading/engine.md`：同步 Piccolo 入口状态。
7. 修改 `data/reading_paths.yaml`：移除 Piccolo 已过时的筹备标签。

## 正文提纲

1. 读陌生引擎时，先分清职责、构建和执行三种关系。
2. 用编辑、资源描述、世界对象、渲染表示建立最小模型。
3. 模块图：编辑器驱动、引擎调度、世界与渲染协作、全局系统生命周期。
4. 构建图：反射与序列化生成、Shader 生成、运行时库与编辑器程序。
5. 数据流：JSON 恢复资源描述，创建世界对象，再交换渲染描述。
6. 一帧执行顺序与编辑模式条件，提供下一轮断点问题。
7. 工程取舍与阅读路线；不展开完整物理、动画或图形 API 教程。

## 源码证据入口

路径均相对于上述固定 Piccolo 仓库。

| 主题 | 入口 | 核对重点 |
| --- | --- | --- |
| 入口与帧循环 | `engine/source/editor/source/main.cpp`、`editor.cpp`；`engine/source/runtime/engine.cpp` | 编辑器实际驱动循环；逻辑、交换、渲染顺序 |
| 全局系统 | `engine/source/runtime/function/global/global_context.cpp` | 系统创建与销毁；通过全局上下文访问服务 |
| 世界与对象 | `engine/source/runtime/function/framework/world/world_manager.cpp`、`level/level.cpp`、`object/object.cpp` | 首次 tick 加载世界；组件恢复、更新与编辑模式条件 |
| 资源与序列化 | `engine/source/runtime/resource/asset_manager/asset_manager.h`；`core/meta/serializer/serializer.h` | JSON 到资源描述；多态类型与字段恢复 |
| 编辑状态变化 | `engine/source/editor/source/editor_scene_manager.cpp`；`function/framework/component/transform/transform_component.cpp` | 变换修改与脏标记 |
| 逻辑到渲染 | `engine/source/runtime/function/framework/component/mesh/mesh_component.cpp`；`function/render/render_swap_context.cpp`、`render_system.cpp` | 脏对象描述、交换、创建或更新 RenderEntity |
| 构建与产物 | 根与 `engine/`、`source/runtime/`、`source/editor/` 的 `CMakeLists.txt` | Runtime 库、Editor 程序、资源与配置复制 |
| 生成链 | `engine/source/precompile/precompile.cmake`、`meta_parser/parser/generator/`；`cmake/ShaderCompile.cmake` | 类型代码生成；SPIR-V 与嵌入头文件 |

## 执行清单

- [x] 核对源码版本、入口和既有导航，冻结文件白名单。
- [x] 完成正文及唯一系列规划，回写三处导航与根路由。
- [x] 核验固定源码链接、站内链接、图中关系及 YAML / shortcode。
- [x] 执行 Hugo 本地构建，检查页面和 Mermaid 图渲染。
- [x] 记录结果与未验证范围，交付本地正文。

## 验证记录

- Hugo v0.158.0 最终本地构建退出码 0，生成 1341 页，ERROR 为零；输出到忽略目录 `.codex_tmp/piccolo-article-20261023/public`。
- 正文 24 个固定提交源码引用均对应本地文件或提交根目录；4 个 `relref` 目标存在。
- 正文、Piccolo 阅读入口、引擎研发入口三页共 102 处本地链接或资源路径检查通过。
- 浏览器确认三张 Mermaid 图均生成 SVG，无图语法错误；页面无横向整体溢出。首张图曾过宽，已精简节点和连线。
- 独立只读审稿对照固定源码，未发现重要或明确需修改的轻微问题。
- `git diff --check` 通过；相对本轮开始时的内容核对四个已有文件，仅增加或修改 Piccolo 对应段落与标签。
- Piccolo 源码工作树仍干净，提交未变。未执行 Piccolo 构建、运行、断点实验或图形捕获；未提交、推送或发布本站。

## 2026-10-23：与首页和二级首页统一阅读样式

用户反馈正文页与一级、二级首页视觉不一致。本轮复用现有阅读系列模板，不改正文和文章 URL。

- 首篇通过 frontmatter 接入 `reading-series`，指定引擎主题和既有 Piccolo 阅读首页作为系列入口。
- 阅读首页补系列索引身份与 `chapters` 返回锚点，保留原有章节锚点。
- 本轮白名单增加 `layouts/reading-series/single.html`、`static/css/series-reading.css`、`static/js/site.js`。模板支持显式导航页索引及按主题生成面包屑；旧系列保留默认交付主题。
- Mermaid 在阅读页上复用纸色、蓝灰和字体变量；其他旧页面保留原主题。图形与表格使用独立横向滚动区域。
- 发现 `content/reading/` 配置 `list: never`，不能从 `site.RegularPages` 搜索导航页；改用显式 `reading_index`，并校验其系列身份，构建恢复通过。
- Hugo v0.158.0 构建退出码 0，1341 页；共享 JS 语法检查、五页 152 处本地链接与资源路径检查、`git diff --check` 通过。
- 对比首页和引擎二级页，背景均为 `rgb(251, 250, 246)`，标题使用同一衬线字体体系。浏览器确认正确的引擎面包屑、目录、三张 SVG、图形蓝灰主题及页面无整体横向溢出。
- 正文内容逐字比较未变；独立只读审阅未发现重要或明确需修订的轻微问题。未提交或发布。
