# Piccolo PCPP-1：对象容器修改后，持有者、借用与遍历怎样变化

> 执行层提纲与证据记录；篇级目录统一在 `piccolo-engine-series-plan.md`。

## 定位与边界

- 用户指定的 Piccolo 专项前置 PCPP-1，原理篇；解决容器结构变化、所有权变化、借用有效性被混为一谈的问题。
- 最小判断：先确定访问落在容器槽位、指针包装还是实际对象上，再分别追迭代器有效性和销毁责任。
- 承接 CPP-01 的持有图、CPP-03 的 ReflectionPtr、主线 04 的对象组装；插在网页 CPP-03 与 GFX-01 之间，series_order 为 4。
- 不替代公共 CXX-04 的复制/移动、Rule of Zero、异常保证等完整单元；公共课仍保持待写。必要的容器规则在本篇就地补桥，不要求读者等待公共课完成。
- 固定公开源码：`BoomingTech/Piccolo@f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，源码只读。
- 只运行独立 C++17 教学例，不构建/运行 Piccolo，不补 exe/PDB 断点验收，不提交推送。

## 正文提纲

1. 删除后还能读取 ID、复制列表却仍能改原组件：先建立实际问题。
2. 容器、元素包装、实际对象、访问路径四层模型；成员资格和对象存活分开。
3. LevelObjectsMap 的 shared_ptr 元素、const 引用返回与 weak_ptr 查找；const 不深层冻结。
4. 编辑器删除路径：调用者先 lock 保活，Level 擦除前解除活动角色引用，擦除后调用者继续读取 ID，渲染侧通过队列另行删除。
5. unordered_map 的 rehash 与 erase 对迭代器、槽位引用和所指对象的不同影响；重查 ID 只证明当前成员资格。
6. getComponents 按值返回；ReflectionPtr 复制裸指针、析构不删组件；auto&& 延长返回临时 vector 寿命而不持有组件。
7. vector<ReflectionPtr> 搬迁包装与销毁实际组件分开；erase/clear 不会替代显式释放，原 GObject 析构才调用删除宏。
8. 范围 for 的隐藏迭代器；保活 shared_ptr 不保护迭代器，reserve 也不等于允许边遍历边追加。给出受控 erase 与延迟修改两种契约。
9. 独立 C++17 示例：成员删除/存活、rehash 槽位稳定、包装数组搬迁、列表副本非深拷贝、安全迭代删除。失效路径只推导不执行 UB。
10. 源码练习与未来调试表；局部保活不保证已卸载世界和物理上下文有效，容器副本不等于线程安全快照。

## 固定证据

| Piccolo 相对路径 | 核对内容 |
| --- | --- |
| `engine/source/runtime/function/framework/level/level.h`、`.cpp` | 对象表类型、只读引用、查找、创建、tick、erase 与活动角色解除 |
| `engine/source/editor/source/editor_scene_manager.cpp` | onDeleteSelectedGObject 局部强持有与删除消息 |
| `engine/source/editor/source/editor_ui.cpp` | 对象列表按引用遍历与局部 shared_ptr、组件列表按值返回后 auto&& 绑定 |
| `engine/source/runtime/function/framework/object/object.h`、`.cpp` | getComponents、裸指针查询、加载追加、tick、保存复制与显式析构 |
| `engine/source/runtime/core/meta/reflection/reflection.h` | ReflectionPtr 复制/赋值及删除宏 |
| `engine/source/runtime/function/character/character.h`、`.cpp` | 活动角色强持有与 setObject(nullptr) |
| `engine/source/runtime/function/framework/component/rigidbody/rigidbody_component.cpp` | 组件清理依赖仍有效的物理上下文 |

语言规则核对 C++ 标准工作草案的 unord.req、vector.capacity、vector.modifiers、class.temporary；链接是当前草案，只使用 C++17 已成立的相关规则。

## 验证记录

- 固定源码 SHA 已回读为 `f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，原克隆工作区保持干净；11 个原文 C++ 片段去除空白后匹配源码，11 个固定版本链接和正文全部 relref 目标存在。
- 从最终正文直接提取完整 C++17 程序到系统临时目录 `TechStackShow-piccolo-containers-pcpp1-20261025/main.cpp`；Windows x64、MSVC 14.51.36231 工具集，`/std:c++17 /utf-8 /EHsc /W4 /Od /MDd /D_ITERATOR_DEBUG_LEVEL=2`。编译退出 0，无警告；运行退出 0，断言与四行输出全部符合预期。
- 独立程序覆盖 rehash 后元素槽位仍可访问、vector 包装重分配后所指组件未搬迁、组件列表浅复制、对象表成员删除与实际析构分离、受控迭代删除。失效迭代器和释放后的组件指针没有被读取，不以执行 UB 验证边界。
- 首次编译已成功，但输出脚本使用错误的文本编码，打印编译器输出时失败，尚未运行程序；修正为显式 UTF-8 输入与适当输出解码后重新编译、运行，以上结果取自这次完整成功记录。
- Hugo v0.158.0 构建退出 0，ERROR 为零，生成 1360 页和 53 个静态文件。输出与缓存位于上述系统临时目录，不写入共享发布目录。
- 14 篇 Piccolo 正文的 series_order 为连续唯一的 0～13；PCPP-1 插在 CPP-03 与 GFX-01 之间，后续文章保持原先顺序。新页含一段 Mermaid 图源并沿用现有 series-reading.css。
- 14 篇正文、两个阅读首页与公共 C++ 入口共 17 个生成页，995 处站内链接、锚点和资源引用检查通过；三个入口均已链接新篇。生成页静态检查不等于浏览器视觉或 Mermaid 实际渲染验收。
- 导航更新覆盖专项入口、引擎首页、公共 C++ 入口、CPP-03 接续、主线 04 前置、系列目录和根系列路由；公共 CXX-04 保持待写，PCPP-2/3 继续待补。共享工作区的无关改动保留，不修改公共模板和样式。
- 本次没有构建或运行 Piccolo，没有执行引擎断点；完整引擎构建、运行与浏览器目视边界沿用原记录。未暂存、提交或推送本次文章。

## A12 原始回执复核（2026-10-25）

只读复核既有`%TEMP%/TechStackShow-piccolo-baseline-01-20261025/verification-receipt.json`及`build-clean-debug.log`。固定revision仍f5053707fed4d3f94d270a436fb0d3a8ae54e3e5；历史干净Debug配置0、生成目标0、完整编辑器构建1，原始日志仍含Jolt的C3688 us字面量和fmt的C2653/checked_array_iterator错误。四份代表reflection/serializer/shader生成文件的当前SHA256均与回执相同。

定向检查原source/bin与build-vs18-debug-clean下没有PiccoloEditor.exe，故没有新的引擎运行、事件退出、线程/Trace或GPU捕获。这里是复核旧回执，不声称本轮又做了一次构建。未改源码/工具链时重跑相同已知失败不会产生新证据；不改第三方依赖或安装系统组件来制造通过。下一步是单独解决该固定revision与工具链兼容，再新建build目录重建并保留完整身份，之后执行PCPP各运行矩阵。
