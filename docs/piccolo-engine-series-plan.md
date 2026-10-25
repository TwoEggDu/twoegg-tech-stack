# Piccolo 源码阅读与调试：系列规划

> 本文件是 Piccolo 系列的唯一 canonical 篇级目录。根 `doc-plan.md` 只维护系列路由；阅读入口为 `content/reading/piccolo-engine.md`。

## 定位与读者

面向具备客户端或 C++ 编程经验、熟悉调试器并希望读懂一台陌生引擎的开发者。先建立架构地图，再沿启动、一帧、对象、资源和渲染链路逐步下钻；遇到语言或图形概念卡点时补充对应前置，不要求预先学完整套基础。每篇围绕一个问题组织源码入口、关键状态和可执行的验证任务。

源码基线：公开仓库 `BoomingTech/Piccolo`，提交 `f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`。后续采用其他提交时逐篇声明。

## 边界

- 本系列负责 Piccolo 的具体实现与阅读、调试方法。
- 通用架构、C++、构建调试和图形基础复用已有专题；以问题需要为准补前置。
- 保留阅读首页的六组导航：工程启动、一帧协作、场景对象、资源元信息、渲染、编辑器与验证。篇级状态只在本文件维护。
- 固定源码事实、作者的工程判断和实际运行观察分别表述。
- 不将源码核对记成构建、断点或图形捕获完成；不扩写成完整自研引擎教程。
- 与“引擎源码阅读到自研引擎”专题并列衔接：本系列以具体引擎的可验证理解为目标，后者负责跨引擎认知与自研取舍。

## 公共基础之后的专项前置

2026-10-25 起采用“[C++ 公共基础](cpp-foundations-series-plan.md) → Piccolo 专项前置 → 本系列主线”。公共 CXX 编号与本系列 CPP-01～03 分开；原文章、URL 和系列归属保留。

公共前置优先 CXX-01～03、04、07～08、10，涉及回调与缓冲访问时补 CXX-05～06。公共课仍有待整理/待写单元，当前可按公共入口复用 UFS 首课和三篇 Piccolo C++ 材料。

现有 CPP 三篇继续负责 Piccolo 的持有图、Component/RHI 分派和反射生成实例。公共课独立成稿后，原文压缩重复语言解释并回链，保留项目特有协议、固定源码证据和练习；本轮不迁移正文。

| 补充次序 | 专项拟题 / 工作 | 状态与现有材料 | 依赖与完成标准 |
|---|---|---|---|
| PCPP-1 | 对象容器修改后，持有者、借用与遍历怎样变化 | 待写；复用 CPP-01 与主线 04 的对象关系 | CXX-02、04；在固定源码核对保存/查找/擦除，区分容器元素与所指对象寿命，补正常/失效边界练习 |
| PCPP-2 | 窗口与编辑器回调怎样绑定、调用和注销 | 待写；先定位实际回调声明与注册点，不预设实现形式 | CXX-05；追捕获或上下文对象、调用时机、退出解绑及有效访问条件 |
| PCPP-3 | 状态缓冲怎样交接，哪些地方真的跨线程 | 待补桥；主线 03 已有执行顺序，06～07 已有渲染数据路径 | CXX-04、06、11；以实际任务/线程与同步证据核对，不能由双缓冲推断并行；CPU 与 GPU 同步分开 |

这三项为专项补写队列，不计入现有 13 篇正文。顺序为 PCPP-1 → 2 → 3，分别在公共依赖完成后推进；已有构建、生成和引擎运行状态沿下表保留。

## 首批目录与推荐顺序

| 编号 | 主问题 / 拟题 | 状态 | 正文 |
| --- | --- | --- | --- |
| 00 | 架构地图：编辑器、世界、资源与渲染怎样协作 | 已写：源码核对、站点构建和图形检查通过，运行待验证 | [正文](../content/system-design/piccolo-engine-00-architecture-map.md) |
| 01 | 建立构建与调试基线：源码、生成文件、配置与程序怎样对应 | 已写：配置、反射/序列化与 shader 生成实测通过，站点检查通过；完整 Debug 编译被第三方兼容问题阻塞，运行与断点待验证 | [正文](../content/engine-toolchain/piccolo-engine-01-build-debug-baseline.md) |
| 02 | 从启动走到第一帧：初始化、世界加载与退出怎样衔接 | 已写：源码、站点构建与页面检查通过；引擎运行待验证 | [正文](../content/engine-toolchain/piccolo-engine-02-startup-first-frame.md) |
| 03 | 一帧怎样推进：调用顺序、状态交接与线程怎样核对 | 已写：源码、站点构建与生成页静态检查通过；浏览器目视及引擎运行待验证 | [正文](../content/engine-toolchain/piccolo-engine-03-frame-order-data-handoff-threads.md) |
| 04 | 场景文件怎样成为世界中的对象与组件 | 已写：源码、资源、站点构建与生成页静态检查通过；浏览器目视及引擎运行待验证 | [正文](../content/engine-toolchain/piccolo-engine-04-scene-to-objects-components.md) |
| 05 | 资源、类型信息与序列化怎样共同恢复对象 | 已写：源码、模板、站点构建与生成页静态检查通过；01 已核对实际生成片段，浏览器目视及引擎运行待验证 | [正文](../content/engine-toolchain/piccolo-engine-05-resource-type-serialization.md) |
| 06 | 改一个位置，怎样沿逻辑数据追到渲染表示 | 已写：源码、资源数值、站点构建与生成页静态检查通过；浏览器目视及引擎运行待验证 | [正文](../content/engine-toolchain/piccolo-engine-06-position-to-render-representation.md) |
| 07 | 从渲染场景进入 Pass 与 Vulkan 提交 | 已写：源码、图形接口语义、站点构建与生成页静态检查通过；浏览器目视、引擎运行及图形捕获待验证 | [正文](../content/engine-toolchain/piccolo-engine-07-render-passes-vulkan-submit.md) |
| 08 | 编辑器怎样修改、保存并恢复场景状态 | 已写：源码、资源、模板、站点构建与生成页静态检查通过；实际保存/重载、浏览器目视及引擎运行待验证 | [正文](../content/engine-toolchain/piccolo-engine-08-editor-save-reload.md) |

编号代表当前推荐顺序。后续拟题在取材时细化，不代表已有正文或实验。

## 随主线补充的前置

补篇按学习需要插入，不占用上面的主线编号。网页导航先放 00 架构地图，再补三篇 C++ 前置和渲染基础；01 插入前置之后、02 之前，随后沿 02—08 阅读启动、一帧执行、场景对象、资源恢复、位置变化、Pass/Vulkan 提交与编辑器保存重载。主线 00—08 和四篇前置现共 13 篇正文。01 已取得配置、反射/序列化和 shader 实际生成证据；完整 Debug 构建仍有第三方兼容阻塞，不能将正文齐备登记成引擎已可运行调试。

| 编号 | 主题 | 状态 | 正文 |
| --- | --- | --- | --- |
| CPP-01 | 读懂引擎 C++：智能指针与对象生命周期 | 已写：源码核对、独立程序和页面检查通过，Piccolo 运行待验证 | [正文](../content/engine-toolchain/piccolo-cpp-01-smart-pointers-lifetime.md) |
| CPP-02 | 读懂引擎 C++：类、继承与虚函数 | 已写：源码核对、独立程序和页面检查通过，Piccolo 运行待验证 | [正文](../content/engine-toolchain/piccolo-cpp-02-classes-inheritance-virtual-functions.md) |
| CPP-03 | 读懂引擎 C++：模板、宏与反射代码生成 | 已写：源码核对、独立程序、预处理与页面检查通过；01 已记录 PiccoloParser 实际生成，引擎运行待验证 | [正文](../content/engine-toolchain/piccolo-cpp-03-templates-macros-reflection.md) |
| GFX-01 | 读懂渲染管线：从场景对象到一帧画面 | 已写：源码、API 语义与页面检查通过；引擎运行与图形捕获待验证 | [正文](../content/rendering/piccolo-rendering-01-scene-to-frame.md) |

GFX-01 建立完整渲染链路的阅读模型，不替代主线 06 的具体对象变化案例与 07 的 Pass/Vulkan 深挖。06 追踪 Wall_1 的位置变化到渲染实体；07 继续沿可见输入、实例批次和缓冲绑定追到命令记录、队列提交与呈现请求。两篇均提供源码条件推导，运行与图形捕获待开展。

08 将编辑器面板和拖轴的写入路径接入前面的对象/渲染链，再追保存收集、实例组件覆盖和重载重建。当前源码的关卡保存未回填 gravity 与 character_name，正文明确给出默认值变化的推导；实际文件写出和业务恢复仍待验证，不能把 08 记成保存闭环已通过运行验收。

## 单篇完成标准

1. 先说明问题与最小模型，再定位具体源码。
2. 图中箭头明确含义，不把目录、构建依赖、调用和数据流混在一起。
3. 实现结论附固定提交的源文件链接；推断或建议说明理由。
4. 实践篇记录真实工具链、断点、调用栈和状态；缺失证据明确列为待验证。
5. 回写本表及相关阅读入口，执行 Hugo 构建与页面检查。

## 当前执行记录

- 首篇提纲、源码证据入口和验证记录：[2026-10-23 架构地图执行计划](superpowers/plans/2026-10-23-piccolo-architecture-article.md)。
- 2026-10-23：首篇正文完成，源码事实与图关系已核对，Hugo 构建和页面图形渲染检查通过；阅读入口与根路由已接入。提交、远端与站点部署状态以 Git 和 CI 实时回执为准。
- Piccolo 完整编辑器构建、调试器运行和图形捕获尚未通过；01 的配置与生成已实测，编译阻塞和后续验收边界见其执行记录。
- CPP-01 提纲与源码、小程序验证记录：[智能指针与生命周期](piccolo-cpp-smart-pointers-outline.md)。
- CPP-02 提纲与源码、小程序验证记录：[类、继承与虚函数](piccolo-cpp-classes-virtual-functions-outline.md)。
- CPP-03 提纲与源码、小程序验证记录：[模板、宏与反射代码生成](piccolo-cpp-templates-macros-reflection-outline.md)。
- GFX-01 提纲与源码、页面验证记录：[从场景对象到一帧画面](piccolo-rendering-scene-to-frame-outline.md)。
- 主线 02 提纲与源码、页面验证记录：[从启动走到第一帧](piccolo-engine-startup-first-frame-outline.md)。
- 主线 03 提纲与源码、页面验证记录：[一帧怎样推进](piccolo-engine-frame-order-data-handoff-outline.md)。
- 主线 04 提纲与源码、资源及页面验证记录：[场景文件怎样成为对象与组件](piccolo-engine-scene-to-objects-outline.md)。
- 主线 05 提纲与源码、模板及页面验证记录：[资源、类型信息与序列化](piccolo-engine-resource-type-serialization-outline.md)。
- 主线 06 提纲与源码、数值推导及页面验证记录：[从位置到渲染表示](piccolo-engine-position-to-render-outline.md)。
- 主线 07 提纲与源码、接口语义及页面验证记录：[从渲染场景进入 Pass 与 Vulkan 提交](piccolo-engine-pass-vulkan-outline.md)。
- 主线 08 提纲与源码、资源/模板及页面验证记录：[编辑器怎样修改、保存并恢复场景状态](piccolo-engine-editor-save-reload-outline.md)。
- 主线 01 提纲与工具链、配置/生成实测及页面验证记录：[建立构建与调试基线](piccolo-engine-build-debug-baseline-outline.md)。
