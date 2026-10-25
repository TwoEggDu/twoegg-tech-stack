# 14 Lua 脚本怎样读写组件并调用引擎方法：执行提纲

## 定位与边界

- 类型：Piccolo 源码分析。由默认 Player 脚本读取运动状态、修改跳跃高度和调用方法，解释 Lua/宿主绑定、反射路径与业务行为之间的交接。
- 基线：`BoomingTech/Piccolo@f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`。只读核对公开源码，Lua 5.4.4 与 sol2 3.3.0 以随附代码为准。
- 先讲执行环境、绑定入口、对象引用、类型元信息和变更语义，再进入三个绑定的真实实现。复用 04/05、CPP-01/03、09 和 08，不展开 Lua 语法大全或完整热更新框架。
- 14 插在 13 后，order=22；15/16/17/18 顺移为23～26，总27篇。00—18 主线正文齐备，前置/专项与完整运行验证分别记账。

## 核心问题与正文提纲

1. 脚本文本、每组件 VM、宿主绑定、对象身份与反射成员分别负责什么。
2. Player JSON 内嵌源码如何经序列化与组件恢复进入 m_lua_script；postLoad 只开 base 库、绑定三个函数与 GameObject，tick 直接执行整个文本。
3. 默认组件顺序、游戏/编辑模式门槛、Motor先于Lua的参数消费时序；不把 dt、OnUpdate 或文件监视热更写成现有能力。
4. find_component_field 按组件名与点分字段逐级查元信息和实例地址；嵌套路径中的 owner 与最终字段地址。
5. set_float/get_bool 的模板实例与 void* 读写协议，字段原名与 JSON 名差别，私有字段白名单和类型/非法路径边界。
6. invoke 拆分目标与方法，反射无参 void 调用；默认方法仅日志；嵌套分支传错完整路径的实际边界。
7. 写字段与调用 setter/dirty/资源保存分别对待，Lua全局状态、弱引用、错误传播和销毁次序。
8. 随附 Lua/sol2 与原 LuaComponent/reflection 的独立宿主实验；桩对象、手动元组注册与完整引擎的范围明确分开。
9. 运行观察表：资源文本、组件 tick、成员地址/类型、参数消费、方法与错误，不以独立实验替代引擎实测。

## 执行清单

- [x] 核对 LuaComponent、默认脚本、反射模板、Motor 与组件调度。
- [x] 编译随附 Lua，并执行原脚本桥接的独立宿主检查。
- [x] 写正文、图表、代码节选和边界自审。
- [x] 更新导航、canonical plan、根路由与排序。
- [x] Hugo、源码节选/链接/分页与状态检查，补证据记录。

## 验证记录

2026-10-25 完成。独立 Lua 宿主不等于 Piccolo 编辑器、角色或 GPU 运行验证。

### 固定源码与实际资源

- 源码 HEAD 为 `f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，核对后源码工作树 `git status --porcelain` 为空。17个固定 blob 引用均有本地文件，六段 C++ 节选忽略空白后匹配原文。
- 唯一 Lua 代码块与真实 Player JSON 的 lua_script 解码值忽略空白后相同；实验也直接使用该解码文本，不用另写的近似脚本替代。
- 实际组件顺序为 Transform、Animation、Mesh、Motor、Camera、Lua；实例合并可能改变顺序，因此不泛化为所有对象。核对默认编辑 tick 注册及游戏模式门槛。
- 核对每组件 sol::state、base 库、三个绑定、GameObject 弱引用、逐 tick 执行文本且不注入 dt、脚本文本序列化白名单及 VM RAII。
- 核对点分路径逐层 owner/leaf 地址、字段名与 JSON display name、生成访问器的 void* 协议、Motor 字段/方法白名单、无参 void 方法调用与默认方法仅日志。
- 明确 get 缺失返回、类型不核对、根组件名缺少字段、弱引用失效、嵌套 invoke 传入完整方法路径等边界；没有执行可能导致未定义行为的坏读取、错误类型写入或失效对象解引用。
- 核对 Motor 在默认顺序先于 Lua、起跳阶段读取高度，以及字段赋值不替代 Transform setter/dirty、文件保存或热重载系统。

### 独立宿主与验证范围

产物保存在 `%TEMP%/TechStackShow-piccolo-lua-14-20261025/`，未加入仓库。

- 从原 `engine/3rdparty/lua.cmake` 提取未注释的32个 C 源文件，以 GCC8.3.0、C99/O0 编译并用 ar 生成 lua_static.a；不编译独立 lua.c 解释器和 onelua.c 聚合入口。
- C++17/O0/Wall/Wextra/Wno-unused-parameter，强制 include algorithm，include 顺序为临时 shadow、原 engine/source、json11、sol2、Lua。直接编译原 lua_component.cpp、reflection.cpp、json11.cpp，并链接 Lua 静态库与独立检查程序。
- shadow 仅替换 GObject 头与日志宏：对象返回原 ReflectionPtr<Component> 包装列表；Motor 使用带 bool/资源成员/计数方法的桩；按原 FieldFunctionTuple/MethodFunctionTuple 接口手动注册元信息。没有运行 PiccoloParser/生成式序列化、原 Motor、Level 或编辑器。
- 最终编译和程序运行均返回0。原 get<bool> 有非 void 函数缺失返回警告，reflection.cpp 有未使用局部变量警告；不记为零警告或完整引擎构建。
- 真实脚本文本在 moving=false/true 时分别写高度5/10，方法计数各增加1；确认的是原桥接分派，真实 getOffStuckDead 仍只记录日志。
- 原 find_component_field 返回最终所属地址为资源成员，最终 getter 地址为高度 float；查错字段返回false。set 错字段与不存在方法安全返回并记日志；嵌套资源虽在桩元信息注册 touch，原 invoke 仍走找不到字段，不发生方法调用。
- 同一 state 的 counter 两次执行为2；顶层 local scratch 每次为1；另一个组件中 counter 缺失。未注入 delta_time，math/io/os/package/coroutine 缺失，type/pcall/load 存在。
- `error('probe failure')` 在该配置产生预期 traceback 并抛 sol::error，由实验宿主 catch；随后原 tick 可继续执行并将 counter 增至3。没有把宿主的 catch 当作 LuaComponent 的错误策略。
- 释放对象唯一强引用后，宿主 weak_ptr 与 Lua 中保存的 weak_ptr 都失效；没有继续通过失效引用调用原桥。先销毁 LuaProbe，再清理反射字段注册，不据此认证引擎的全局注销、回调或退出顺序。
- 初版宿主用 sol 的 table_proxy.get_type 与 nil 比较检查缺失全局，断言失败；查随附实现确认代理对缺失项返回 none，改为 valid 检查。只修正实验断言与进程错误框设置，未改原 LuaComponent、反射或 Lua/sol2 源码。

| 临时产物 | SHA-256 |
| --- | --- |
| lua_bridge_checks.cpp | `96051c95fbc9688e540d03a6ed75f2bdac930e79a2b2eda8ca52e4f8b324e8a0` |
| player.lua | `9fedda06f947141c84b8f9db2ff1a6662df763afa7861e0ae88fceede8ab7bcf` |
| run_checks.py | `d0a2bcac9e6872394a04e66a344dd7383c3a7f1031877c314a4a9d9d2b1f2ce6` |
| bridge-checks.json | `9569816d36c513bb9b357d5cbd47bd872ab028cfe292cd0f9a6acb14cb67ef0a` |

### 站点与导航

- Hugo0.158.0 使用临时 destination/cache 构建返回0，无 ERROR；共享工作树生成1400页、54静态文件，含其他任务内容，不全部归因于14。
- `final_checks.py` 核对27篇 series_order 连续0～26；14位于13后、15前，15/16/17/18顺移至23～26，URL不变。14的 weight 为516，15由516改518，避免新增排序冲突；不重排其他历史 weight。
- 全系列上一页/下一页与末篇回目录检查通过。27篇与三个入口共30个生成页，1833个本地链接/资源/HTML锚点存在。
- 阅读首页、引擎首页及CPP-03/05/08/09/10/12/13/18正文有14入口；阅读首页新增条目和状态说明各一份。canonical plan 的09—18均为已写，00—18主线正文齐备，构建/运行状态保持原边界；根 doc-plan 只更新系列级概况。
- 一份 Mermaid 图源码与共享 series-reading.css 引用通过静态检查；未做浏览器目视或实际 Mermaid 渲染验收。
- 未运行完整编辑器、真实角色/Motor、Jolt 或 GPU，未改引擎/资源/共享布局/CSS，未暂存、提交或推送。
