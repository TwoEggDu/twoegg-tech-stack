# Piccolo C++ 前置 03：模板、宏与反射代码生成

> 执行层提纲与证据记录；篇级状态统一在 `piccolo-engine-series-plan.md`。

## 定位与范围

- 承接 CPP-02。用户要求继续写下一篇，沿已讨论的顺序补模板、宏与代码生成，继续采用先文章、后 Piccolo 调试的安排。
- 类型：原理篇。目标是读懂组件查询、字段访问、序列化和生成代码之间的接线，不写完整模板元编程或反射库教程。
- 源码基线：`BoomingTech/Piccolo@f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，保持只读。
- 最小模型：宏改写 token；模板参与编译期类型处理；外部解析器读取注解与 AST 后生成 C++；运行时注册表按名字选择预先编译的访问或构造函数。
- 产物：新增正文与本提纲，更新 CPP-02 继续阅读、系列计划、Piccolo 与引擎阅读首页和根路由的 Piccolo 行。保留前两篇未提交成果与其他任务的工作区改动，复用 reading-series 样式。
- 验证：从正文提取完整 C++17 教学程序，编译运行并检查预处理展开；固定源码引用、relref、Hugo 构建、页面图形与前后篇导航。PiccoloParser 与 Piccolo 引擎均不编译、不运行；文中推导的生成片段明确标注。

## 正文提纲

1. 为什么一个组件查询同时出现类型参数、字符串与宏；为什么字段不是 public 也能被生成代码读写。
2. 用阶段表区分宏、模板、代码生成、注册表与虚调用，建立最小模型。
3. 拆开 ReflectionPtr<Component> 与 vector 嵌套，读模板形参、类型实参和函数模板推导。
4. 从 tryGetComponent 宏展开到类型参数加名字字符串，再核对字符串匹配与 static_cast；别名不自动成为注册名。
5. CLASS/META 的解析分支与普通分支；# 字符串化、## 拼标识符，REFLECTION_TYPE/BODY 的声明与 friend。
6. TransformComponent 的 WhiteListFields/Enable，按实际 Field::isAccessible 规则解释字段选择。
7. libclang 读取注解和 AST，Mustache 模板生成访问器、注册函数和 Serializer 显式特化；推导片段与真实生成产物分开。
8. CMake PiccoloParser → PiccoloPreCompile → PiccoloRuntime，输出路径与 all_serializer.h/.ipp 的接入点。
9. 引擎 startEngine 注册；Serializer 用 $typeName/$context 接入按名字构造，不把模板与 JSON 字符串混为类型转换。
10. 完整教学程序验证宏展开、模板查询、别名名字差异、friend 访问器与运行时查表；静态阅读练习与边界。

## 固定源码证据

路径相对于 Piccolo 仓库。

| 入口 | 核对范围 |
| --- | --- |
| `engine/source/runtime/function/framework/object/object.h` | 查询宏、函数模板、名字比较、static_cast，组件包装容器 |
| `engine/source/runtime/core/meta/reflection/reflection.h` | ReflectionPtr<T> 的名字与 T*；宏的两分支；前置声明、friend、# 与 ## |
| `engine/source/runtime/function/framework/component/mesh/mesh_component.cpp` | 实际 tryGetComponent(TransformComponent) 调用 |
| `engine/source/runtime/function/framework/component/transform/transform_component.h` | WhiteListFields 与 m_transform 的 Enable，未标注缓冲与索引 |
| `engine/source/meta_parser/parser/parser/parser.h`、`.cpp` | libclang 参数定义 __REFLECTION_PARSER__，构造 AST、按文件组织 schema，调用生成器 |
| `engine/source/meta_parser/parser/meta/meta_info.cpp` | 读取 CXCursor_AnnotateAttr 并解析属性 |
| `engine/source/meta_parser/parser/language_types/class.cpp`、`field.cpp` | 类与字段筛选规则 |
| `engine/source/meta_parser/parser/generator/generator.cpp`、`reflection_generator.cpp`、`serializer_generator.cpp` | 字段数据组织、模板渲染及 source/_generated 输出路径 |
| `engine/template/commonReflectionFile.mustache`、`allReflectionFile.mustache` | 具体字段访问器、创建与读写桥、注册函数及 metaRegister 定义 |
| `engine/template/commonSerializerGenFile.mustache`、`allSerializer.h.mustache`、`allSerializer.ipp.mustache` | 显式特化声明、汇总 include 与特化定义 |
| `engine/source/precompile/precompile.cmake`、`precompile.json.in`、`engine/CMakeLists.txt` | 参数文件、parser_header.h、构建依赖与源目录参数 |
| `engine/source/runtime/core/meta/reflection/reflection_register.cpp`、`reflection.cpp`、`engine/source/runtime/engine.cpp` | 生成文件接入、启动注册、名字查表与关闭注销 |
| `engine/source/runtime/core/meta/serializer/serializer.h`、`engine/source/runtime/resource/asset_manager/asset_manager.h` | loadAsset 模板、Serializer 回退断言与 ReflectionPtr 序列化协议 |

## 语言规则与工具资料

核对 C++ 草案的 cpp.replace、cpp.stringize、cpp.concat、cpp.rescan、temp.inst、temp.deduct.call、temp.expl.spec、expr.static.cast 和 class.friend；GCC 文档核对 -E/-P 的意义。正文限于示例需要的 C++17 规则。

## 验证记录

- 源码 SHA 回读为 `f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，工作区无改动。核对了组件查询、字段筛选、AST 解析、生成模板、输出路径、编译接入和运行时注册使用。
- 从正文完整代码块提取 `meta_bridge.cpp`，使用已有 Windows x64 GCC 8.3.0，参数 `-std=c++17 -O0 -g -Wall -Wextra -pedantic`。编译与运行退出码均为 0，无编译诊断；全部断言通过，输出与正文六行一致。
- 用 `-std=c++17 -E -P` 输出预处理结果，逐项核对正常查询展开、别名查询的不同字符串、拼接后的友元声明。教学程序中的 tryGetComponent 宏定义与固定源码逐字一致。
- 25 个固定源码引用对应实际存在的文件或目录，正文 5 个 relref 均存在；新篇、CPP-02、CPP-01、架构地图与两个阅读首页共 194 处生成后本地链接、锚点和资源引用检查通过。
- Hugo v0.158.0 本地构建退出码 0，生成 1346 页、52 个静态文件，ERROR 为零。
- 浏览器确认 reading-series 原有样式、正确面包屑、三张表与一张生成注册接线图 SVG；图中文字与箭头可读，无图语法错误和页面整体横向溢出。新篇上一篇指向 CPP-02，返回系列指向 Piccolo 首页 chapters 锚点。
- 自查区分了类型参数与名字字符串、普通编译与解析器分支、C++ 模板与 Mustache 文本模板、显式特化与注册表、虚调用与名字工厂。推导的生成片段与手写模拟程序均明确标注，未当成真实生成产物。
- PiccoloParser 与 Piccolo 本身未编译、未执行或调试；本次只做本地写作与验证，未提交或推送。前两篇文章与其他任务的未提交改动保留。
