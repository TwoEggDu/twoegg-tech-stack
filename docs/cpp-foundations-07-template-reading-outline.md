# CXX-07：模板调用怎样选择实现：推导、特化与常见类型工具

> 执行层提纲与验证记录。唯一篇级目录为 [C++ 公共基础系列计划](cpp-foundations-series-plan.md)。

## 定位与复用

- 类型：原理篇。依赖 CXX-01、03、04，多文件实验回接 CXX-10；不依赖引擎。
- 复用 Piccolo CPP-03 的显式类型/实参推导、类型与名字字符串、模板与运行时注册的分层；复用 UFS-02 的 Twice 与头文件组织起点。
- 公共模型：找到声明与可见候选，确定类型实参，判断替换和可行性，选择重载/类模板特化，再检查所需定义的实例化与链接。明确这是阅读步骤，不是完整编译器算法。
- 对比：函数重载与显式特化、类模板主模板/全特化/偏特化；SFINAE 的直接上下文与函数体硬错误；编译期分支与运行时分支；转发的值类别与资源/回调寿命。
- 不覆盖完整元编程、ADL 全规则、C++20 Concepts、项目生成器实现或托管泛型内部表示。四路线使用既有公开文章作入口，不复制 Unity 受限实现。

## 正文提纲与实验

1. 从同名函数、尖括号与很长的模板报错切入，区分编译器选择与运行时分派。
2. 推导不等于通用隐式转换搜索；值/引用/数组、返回类型不提供普通调用的推导、非类型参数和别名。
3. 程序一：推导断言、普通函数与函数模板、函数显式特化不单独参与重载；类模板主模板/偏特化/全特化。
4. 程序二：typename、template、decltype、traits、void_t、declval、enable_if；SFINAE 回退与 if constexpr；constexpr 不保证每次都在编译期求值。
5. 程序三：引用折叠、转发引用、命名参数是左值、forward 与 move、参数包/展开、decltype(auto) 返回引用的寿命责任。
6. 三文件工程：模板定义隐藏到 cpp 后用显式实例化提供 int 实现；extern template 不创造定义；缺少 long 实现的编译/链接分阶段失败。
7. 三个单文件失败：推导冲突、函数体错误不能回退、依赖类型缺 typename；另用普通 if 变体核对分支仍需合法。
8. 四路线对接：Piccolo 查询与序列化、UE 模板/策略专项、HybridCLR 原生实现与托管泛型、IL2CPP 生成/注册。规则、项目事实与未做实验分开。
9. 更新公共目录、canonical plan、根系列路由和四篇旧文回链；下一篇 CXX-08。

## 资料依据

以 C++17 工作草案 N4659 为基线，2026-10-25 核对：

- [调用推导](https://timsong-cpp.github.io/cppwp/n4659/temp.deduct.call)、[替换与直接上下文](https://timsong-cpp.github.io/cppwp/n4659/temp.deduct)、[类模板偏特化](https://timsong-cpp.github.io/cppwp/n4659/temp.class.spec)、[显式特化](https://timsong-cpp.github.io/cppwp/n4659/temp.expl.spec)。
- [依赖名](https://timsong-cpp.github.io/cppwp/n4659/temp.res)、[decltype](https://timsong-cpp.github.io/cppwp/n4659/dcl.type.simple)、[类型变换工具](https://timsong-cpp.github.io/cppwp/n4659/meta.trans.other)、[if constexpr](https://timsong-cpp.github.io/cppwp/n4659/stmt.if)。
- [forward/move](https://timsong-cpp.github.io/cppwp/n4659/forward)、[参数包](https://timsong-cpp.github.io/cppwp/n4659/temp.variadic)、[显式实例化](https://timsong-cpp.github.io/cppwp/n4659/temp.explicit)。

## 验证记录

- 2026-10-25：从正文提取九个 C++ 代码块（含三文件工程），在 Windows x64、GCC 8.3.0（x86_64-posix-seh，目标 x86_64-w64-mingw32）下验证。正常参数为 `-std=c++17 -O0 -g -Wall -Wextra -Wpedantic`，不定义 NDEBUG。
- template_selection、template_tools、template_forwarding 三个程序，以及 scale 三文件工程，编译/运行退出码均为 0，在所列选项下无诊断，全部断言通过，输出分别为正文记录的 4、3、3、1 行。
- 三个失败文件与普通 if 变体以 `-std=c++17 -Wall -Wextra -Wpedantic -fsyntax-only` 检查，均退出码 1。分别核对类型推导冲突、函数体缺少 size、C++17 依赖类型缺 typename、普通 if 仍检查不合法分支。
- scale_long.cpp 只把调用改为 `scale(7L)`，独立编译退出码 0；链接原 scale.o 时退出码 1，诊断缺少 `long scale<long>(long)`。明确 `combine<double>` 与补 typename 的两个修复变体均通过编译检查。
- 最终正文九个代码块与执行过的测试文件逐字一致，没有运行失败产物、悬垂引用或非法访问。没有根据单机符号/输出来推断代码大小、性能或跨编译器一致性。
- 从当时 HEAD `14f678385238a3d49515dce0920aa057fc940df3` 导出站点，叠加本次九个正文/规划/目录/回链文件；根 doc-plan 仅叠加 C++ 系列行。Hugo v0.157.0 构建 1405 页，退出码 0、无 ERROR；这是隔离副本的结果，不代表其他并行编辑均通过。
- 新正文、公共索引、CXX-04、CXX-10、Piccolo CPP-03、UFS-02 共 6 个生成页面，353 处本地链接、资源和锚点检查通过；五个已有页面均能进入新正文，正文代码与输出正常生成。
- Canonical plan 与公共索引回写八篇正文已写、一个待整理与两个待写；下一篇 CXX-08。旧文保留原综合练习/项目证据与验证记录，没有新增四条项目路线的构建、运行、泛型保留/共享或异步寿命结论。提交与推送状态以 Git 记录为准。
