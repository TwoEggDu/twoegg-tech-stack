# Piccolo 主线 05：资源、类型信息与序列化怎样共同恢复对象

> 执行层提纲与证据记录；篇级状态统一在 `piccolo-engine-series-plan.md`。

## 定位与范围

- 用户指定主线 05，结合实际代码写正文并更新相关导航；延续先补阅读文章、后做引擎构建与调试的安排。
- 类型：原理篇。问题为文件中的字段与类型名，怎样经生成代码、注册函数与运行时分派恢复为具体组件。
- 最小模型：资源定位、静态已知类型的字段读写、多态类型的动态选择，以及恢复后的业务接线；同时区分构建生成、编译接入和运行时注册。
- 固定源码：`BoomingTech/Piccolo@f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，保持只读。日期 2026-10-25；主线 05，网页 series_order 为 8。
- 案例：继续使用默认关卡 Wall_1 的实例 Transform，完整追踪 LevelRes → ObjectInstanceRes → ReflectionPtr<Component> → TransformComponent → Transform → Vector3/float。
- 与 CPP-03 的分工：前置篇解释语言与生成器词汇；本篇解释完整资源恢复链及读写、失败边界。与 04 的分工：不重复完整对象组装，接回 GObject::load/postLoadResource。编辑器保存工作流和持久化范围留给 08。
- 产物：本提纲、新正文、04 继续阅读、两个阅读首页、系列 plan 与根路由 Piccolo 行；不修改共享模板或样式，不提交推送。

## 正文提纲

1. 问题空间：JSON 解析器不负责发现 C++ 类型，路径、字段键和类型名解决不同问题。
2. AssetManager 的边界：调用者给定 AssetType、读文本/解析 JSON/调用 Serializer，不把它当成已完成所有 GPU 资源或业务接线的证明。
3. 两类分派：已知类型的 Serializer 重载/特化与 ReflectionPtr 的运行时类型名查找；沿真实 Wall_1 组件逐层连接。
4. 两类生成产物：Serializer 特化直接读写字段，反射产物提供字段/数组访问器与按类型构造/写出回调。不是统一遍历运行时 FieldAccessor 完成所有序列化。
5. 元信息怎样选字段与生成键：Fields/WhiteListFields/Enable、m_ 去前缀；Transform 的持久字段与双缓冲运行状态分开。
6. 构建与启动连接：PiccoloPreCompile 声明运行解析器，生成 .h/.ipp 接入 runtime；startEngine 在读取系统资源前执行 metaRegister。当前 _generated 不存在，不能把手工展开示意记作真实生成。
7. ReflectionPtr read/readPointer、ClassFunctionTuple 和 newFromNameAndJson 的实际查表及构造；含 '*' 静态类型分支说明但不冒充默认组件路径。
8. 生成构造函数 new 具体类型并调用 Serializer，递归走 Transform/Vector3/数值读取；随后对象组装与 postLoad 才恢复运行缓冲与系统依赖。
9. 写出路径：ReflectionPtr 类型名、writeByName、生成 write、AssetManager 输出 JSON；语义字段输出不等于原文件字节、未知字段保留或自动恢复实例/定义来源。
10. 缺失/null 字段跳过、未知字段不自动迁移、类型名未注册导致空指针；文件成功、类型恢复和业务可用的证据分别取。
11. 静态练习与后续断点表；版本兼容、字段更名与生成产物一致性的工程判断明确为判断。

## 固定证据

| 路径（相对 Piccolo） | 核对内容 |
| --- | --- |
| `engine/asset/level/1-1.level.json` | Wall_1 的实例组件与具体字段 |
| `engine/source/runtime/resource/asset_manager/asset_manager.h`、`.cpp` | loadAsset/saveAsset、路径与成功范围 |
| `engine/source/runtime/resource/res_type/common/level.h`、`object.h` | 静态资源结构与多态组件容器 |
| `engine/source/runtime/core/meta/serializer/serializer.h`、`.cpp` | 反射指针与普通指针读写、基础类型特化、未实现模板 |
| `engine/source/runtime/core/meta/reflection/reflection.h`、`.cpp` | 回调 tuple、类/字段/数组注册表、查找、未知类返回 |
| `engine/template/allSerializer.ipp.mustache`、`commonSerializerGenFile.mustache` | 字段直接读写、基类、数组与特化声明 |
| `engine/template/commonReflectionFile.mustache`、`allReflectionFile.mustache` | 构造回调、写出回调、注册与总入口 |
| `engine/source/meta_parser/parser/language_types/field.cpp`、`meta/meta_utils.cpp`、`generator/generator.cpp` | 字段筛选、m_ 去前缀、模板数据 |
| `engine/source/precompile/precompile.cmake` | 构建生成步骤声明，不执行解析器 |
| `engine/source/runtime/core/meta/reflection/reflection_register.cpp`、`engine.cpp` | 生成产物编译接入与启动注册顺序 |
| `engine/source/runtime/function/framework/component/transform/transform_component.h`、`.cpp` | m_transform 持久化标记与 postLoad 的缓冲初始化 |
| `engine/source/runtime/core/math/transform.h`、`vector3.h` | 嵌套静态类型与默认字段 |
| `engine/source/editor/source/editor_ui.cpp` | FieldAccessor/ArrayAccessor 的实际编辑器消费者 |
| `engine/source/runtime/function/framework/object/object.cpp` | 加载组装、postLoad 与 save 的输出选择 |
| `engine/3rdparty/json11/json11.cpp` | 缺失键返回 null，和显式 null 的字段跳过关系 |

## 验证记录

- 写作前已回读固定 SHA，源码工作区干净；当前 clone 没有 engine/source/_generated，未运行 PiccoloParser。
- 基于原模板手工展开字段分支时明确标注示意；它不属于生成器执行、编译或引擎运行证据。
- 6 个 C++ 代码块中，5 处实际源码节选在去除空白后与固定源码匹配；另 1 处字段分支为明确标注的手工展开示意，与原模板的非数组分支替换 m_transform/transform 后一致，没有把该核对记成完整解析器生成。
- Wall_1 组件 JSON 示例与默认关卡中的完整实例组件记录逐字段一致。29 个固定源码、资源与模板链接目标存在，7 个正文 relref 目标存在。
- 7 项顺序核对通过：资源读取、启动注册、反射指针读取、按名构造、按名写出、资源写出与构造模板。另核对生成头/.ipp 的包含位置、总注册入口到文件 wrapper、类回调登记及 EditorUI 的字段列表消费。
- Hugo v0.158.0 构建退出码 0，生成 1352 页、53 个静态文件，ERROR 为零。
- 新篇、04、03、02、渲染篇、三篇 C++、架构地图和两个首页共 11 页、348 处生成后站内链接、锚点与资源引用检查通过。
- 生成页含 4 张 HTML 表和 2 段 Mermaid 图源，沿用 series-reading.css。静态确认 04 下一篇指向 05、05 上一篇指向 04、返回系列指向 chapters；两个阅读首页均有新入口，原两条资源链/序列化待写占位移除，无未解析 relref。
- 本次只修改 7 个任务文件，源码工作区和暂存区未改变；其他文章及共享模板的已有改动保留。生成页静态检查不代替浏览器目视与 Mermaid SVG 实际渲染检查。
- 浏览器本地预览访问在前文工作中被 URL 安全策略拒绝；本次不改换地址或执行通道绕过，目视检查保持待验证。
- 未构建或运行 Piccolo、未执行断点与 GPU 捕获，不暂存、提交或推送。
