# CXX-03：一次成员调用执行谁

> 执行层提纲与验证记录。篇级目录统一维护在 [C++ 公共基础系列计划](cpp-foundations-series-plan.md)。

## 定位与复用

- 类型：原理篇。承接 CXX-01 的访问路径与 CXX-02 的释放责任，解释有效对象上的成员选择、实际执行和析构链。
- 复用 [Piccolo 类、继承与虚函数](../content/engine-toolchain/piccolo-cpp-02-classes-inheritance-virtual-functions.md) 的静态/动态类型、覆盖、切片、虚析构和构造析构阶段模型；保留原文的 Component / RHI 源码路径与原验证记录。
- 新公共正文使用独立、自有 C++17 程序，不复制引擎受限代码。补强原文未独立展开的名字隐藏、using 恢复候选、重载与覆盖的先后关系，以及基类值参数切片。
- 范围：普通单继承、公开基类访问、非静态成员函数、默认复制和普通单对象删除。不展开多继承布局、虚继承、协变细则、RTTI 转换实现、完整重载排序、vtable ABI 或托管虚调用实现。
- 目标：读者能写出“表达式静态类型 → 查找与重载选择 → 是否虚调用 → 对象来源与阶段 → 实际实现”，并区分借用基类部分、创建基类值和通过基类释放目标。

## 正文提纲

1. 从“跳到基类声明却找不到实际工作”提出问题；对象有效只是前提，执行谁还需分层判断。
2. 类与对象、基类子对象、静态与动态类型；公开继承、访问区与声明/定义的最低语法桥梁。
3. 查找、重载选择和虚分派模型；名字隐藏不是覆盖，成员 const 参与匹配。
4. 完整程序一：隐藏基类重载、using 恢复候选、基类引用的虚/非虚调用、显式限定调用；逐行解释结果。
5. override 编译失败用例；final、纯虚、空实现的边界，不把虚函数机制当成固定布局或性能结论。
6. 完整程序二：借用保持原对象、基类值复制与按值参数产生切片，原对象和副本各自修改；容器/接口选择的后果。
7. 完整程序三：基类/成员/派生构造与析构顺序、构造析构阶段的虚调用、unique_ptr<Base> 默认删除器与虚析构。
8. 不运行非虚基类删除派生对象的 UB；用 protected 非虚析构的预期编译失败，演示禁止外部经基类删除的接口。
9. 四路线分别追对象来源、项目调度和运行时分派；不把托管虚调用等同于 C++ 虚函数。
10. 自测、验证与下一篇 CXX-10（按现有补写顺序）；回写 canonical plan、目录、根路由、CXX-02 后续链接和 Piccolo CPP-02 回链。

## 资料依据

核对日：2026-10-25。采用 C++17 工作草案 N4659 的公开 HTML 文本，避免将后续版本的删除规则扩展混入本课。

- [class.member.lookup](https://timsong-cpp.github.io/cppwp/n4659/class.member.lookup)、[over.match](https://timsong-cpp.github.io/cppwp/n4659/over.match)、[expr.call](https://timsong-cpp.github.io/cppwp/n4659/expr.call)：查找、重载选择、非虚/显式限定调用与最终覆盖实现。
- [class.virtual](https://timsong-cpp.github.io/cppwp/n4659/class.virtual)、[class.abstract](https://timsong-cpp.github.io/cppwp/n4659/class.abstract)：覆盖匹配、override/final 检查、纯虚与抽象类。
- [class.copy.ctor](https://timsong-cpp.github.io/cppwp/n4659/class.copy.ctor)：隐式复制构造、基类和成员的逐项复制；切片示例使用普通基类值复制，不假设按字节拷贝。
- [class.base.init](https://timsong-cpp.github.io/cppwp/n4659/class.base.init)、[class.cdtor](https://timsong-cpp.github.io/cppwp/n4659/class.cdtor)、[class.dtor](https://timsong-cpp.github.io/cppwp/n4659/class.dtor)：基类/成员初始化和销毁顺序、构造析构期间的调用范围。
- [expr.delete](https://timsong-cpp.github.io/cppwp/n4659/expr.delete)：C++17 普通单对象删除时，静态/动态类型不同的基类删除条件。

## 验证记录

- 2026-10-25：从正文提取三个正常程序，Windows x64、GCC 8.3.0（x86_64-posix-seh），参数 `-std=c++17 -O0 -g -Wall -Wextra -Wpedantic`。三个编译退出码均为 0，在所列选项下无警告；运行退出码均为 0，全部断言通过。
- `lookup_dispatch.cpp` 的 7 行、`slicing.cpp` 的 6 行、`lifecycle.cpp` 的 12 行输出均与正文逐行一致；最后一个程序的成员存活计数归零。
- 错误 override、外部访问 protected 析构两个用例分别使用 `-std=c++17 -Wall -Wextra -Wpedantic -fsyntax-only` 检查，退出码均为 1；分别命中 `does not override`、`protected` 诊断。未生成/运行失败用例，未执行非虚基类删除派生对象等 UB。
- Hugo v0.157.0 工作区本地构建通过：1378 页，退出码为 0，无 ERROR；输出使用本轮独立临时目录，检查后清理。
- 检查公共前三篇、公共目录、Piccolo CPP-02 五个生成页的 281 个站内链接/资源引用及锚点，缺失为 0；正文 relref 均有源文件。确认 01↔02↔03 阅读导航、目录第三篇入口、旧文回链，以及目录的三篇已写/下一步 CXX-10 状态。
- 最终正文中的五个 C++ 代码块与已验证源文件逐一比对一致。对应文档的差异检查通过。
- 更新范围：新正文与本提纲；canonical plan、公共索引、根 doc-plan 的 C++ 系列行；CXX-02 后续链接与 Piccolo CPP-02 前置提示。共享文件中其他任务的改动保留，不视作本篇产出。
- 不包含：Piccolo / Unreal / HybridCLR / IL2CPP 工程运行、调试、ABI 布局检查、跨平台与性能测量。
