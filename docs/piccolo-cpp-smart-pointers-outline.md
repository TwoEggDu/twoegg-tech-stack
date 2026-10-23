# Piccolo C++ 前置 01：智能指针与对象生命周期

> 执行层提纲与证据记录；篇级状态统一在 `piccolo-engine-series-plan.md`。

## 定位与范围

- 用户已确认标题与方向：结合 Piccolo 实际代码，解释智能指针与对象生命周期。
- 类型：原理与具体实现桥接篇；先说明拥有、观察和使用，再进入固定源码。
- 读者熟悉调试器，可以按需回看普通 C++ 所有权课；不假定已理解引用计数与引擎对象的逻辑生命周期。
- 源码：`BoomingTech/Piccolo@f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，只读。
- 白名单：新增本提纲和 `content/engine-toolchain/piccolo-cpp-01-smart-pointers-lifetime.md`；更新 `docs/piccolo-engine-series-plan.md`、`content/reading/piccolo-engine.md`、`content/reading/engine.md` 与根 `doc-plan.md` 的 Piccolo 相关段落。不提交或发布。
- 复用已确认的 reading-series 页面样式，不修改共享 CSS / JS / 模板。

## 正文提纲

1. 从关卡删除对象后，为什么仍要继续找持有者。
2. 最小模型：被管理对象、指针包装、共享所有权组与逻辑状态。
3. Piccolo 持有图：世界、活动关卡、对象、角色与组件。
4. 创建与复制 shared_ptr，weak_ptr 观察与 lock 临时保活。
5. enable_shared_from_this 与加载时建立父对象观察关系。
6. ReflectionPtr 及显式删除，解释项目自己的指针语义。
7. reset、clear、从世界移除和析构的不同时间点。
8. MeshComponent 多次 lock 的源码与一次局部保活的教学写法。
9. 完整小程序验证复制、移动、弱引用、析构和停止状态。
10. 源码调试问题与适用边界。

## 固定源码证据

路径相对于 Piccolo 仓库。

| 入口 | 证明范围 |
| --- | --- |
| `engine/source/runtime/function/global/global_context.h`、`.cpp` | 系统 shared_ptr 成员、make_shared、clear/reset 顺序 |
| `engine/source/runtime/function/framework/world/world_manager.h`、`.cpp` | 已加载关卡容器强持有，活动关卡弱观察，tick 局部 lock |
| `engine/source/runtime/function/framework/level/level.h`、`.cpp` | 对象容器、make_shared 后 load、删除角色关联与对象条目 |
| `engine/source/runtime/function/framework/object/object.h`、`.cpp` | enable_shared_from_this、weak_from_this、组件显式删除 |
| `engine/source/runtime/function/framework/component/component.h` | parent weak_ptr，postLoadResource 接收弱引用 |
| `engine/source/runtime/function/framework/component/mesh/mesh_component.cpp` | 实际多次 lock 与借用的组件裸指针 |
| `engine/source/runtime/function/character/character.h`、`.cpp` | 角色对 GObject 的额外强持有 |
| `engine/source/runtime/core/meta/reflection/reflection.h` | ReflectionPtr 保存类型名与裸指针，删除宏显式 delete |

语言规则按 C++ 工作草案 shared_ptr 构造/析构/reset、weak_ptr::lock、enable_shared_from_this 条款核对。仅使用 C++17 已有机制，不把在线草案的新接口写入示例。

## 验证清单

- [x] 核对源码基线与实际持有/清理入口。
- [x] 完成正文，接入前置补篇导航与唯一系列规划。
- [x] 从正文提取完整教学程序，使用已有 GCC 编译运行并核对输出。
- [x] 核验源码引用、站内链接，Hugo 构建并检查页面与图形。
- [x] 回写验证记录；分别说明小程序验证与 Piccolo 运行未验证。

## 验证记录

- 编译器：Windows x64，`g++.exe (x86_64-posix-seh, Built by strawberryperl.com project) 8.3.0`，使用已有环境，没有安装依赖。
- 从正文的完整 `lifetime.cpp` 代码块提取源文件，参数为 `-std=c++17 -O0 -g -Wall -Wextra -pedantic`。编译及运行退出码均为 0，无编译诊断；全部断言通过。
- 实际九行输出与正文逐行一致：复制后强计数 2，lock 后 3，移动后仍为 3；停止后 active 为 false，pin 期间弱引用未过期；最后依次打印 Actor、Component 析构，再确认弱引用过期。
- 15 个固定提交源码引用对应存在的文件或提交目录，3 个 relref 存在。新篇、架构地图与两个阅读入口共 135 处本地链接及资源路径检查通过。
- Hugo v0.158.0 本地构建退出码 0，生成 1344 页，ERROR 为零。
- 浏览器确认 reading-series 样式、正确的引擎面包屑、上一篇到架构地图、返回目录锚点、两张表和一张持有关系 SVG；无图语法错误或页面整体横向溢出。
- 独立只读技术审稿未发现重要或明确需修订的问题。实际 Piccolo 源码保持未修改，未构建、未运行或执行引擎断点实验；未提交或发布本站。
