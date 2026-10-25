# CXX-02：对象与资源由谁结束

> 执行层提纲与验证记录。篇级目录统一维护在 [C++ 公共基础系列计划](cpp-foundations-series-plan.md)。

## 定位与复用

- 类型：原理篇；承接 CXX-01 的对象、访问路径与借用，回答谁让目标存活、谁最终执行释放。
- 用户已指定按顺序写第二篇，复用既有 RAII 与智能指针文章，同步目录和规划；本轮不提交推送。
- 复用 UFS-02 的 RAII、独占责任和移动练习；复用 Piccolo CPP-01 的共享持有、weak lock、逻辑停用与析构分离模型。原文章的项目案例、代码与验证历史保留。
- 公共正文使用自有 C++17 程序，不复制引擎源码。普通语言、库契约、实现可能性、实际实验分别表述。
- 最小模型：名字的作用域、对象的生命周期、资源的有效区间、拥有关系和借用关系；业务状态、析构过程与存储释放分别判断。
- 边界：解释普通自动对象与成员清理、构造失败、RAII、unique/shared/weak、删除器、引用环；不展开分配器实现、placement new、完整移动语义、虚析构、并发算法或托管 GC。

## 正文提纲

1. 从“离开函数、容器 erase、调用 clear 为什么不一定结束同一件事”提出问题。
2. 分开作用域、对象、资源和业务状态；给出生命周期与存储的最小界限。
3. RAII 把已取得资源交给对象管理；正常退出与异常展开的析构顺序、未完成构造对象和已完成成员的区别。
4. 完整 RAII 程序：局部对象逆序结束；只读打开自身源文件并交给 unique_ptr 自定义删除器；正常返回与构造中途失败都核对 fclose；析构清理失败通过状态记录而不抛异常。
5. unique_ptr 说明独占责任与 get / move / reset / release 的区别；借用不增持，复制独占指针应被拒绝。
6. shared_ptr 复制共享责任，weak_ptr 不保活，lock 结果持有期间才保活；控制信息、对象、存储和业务状态分开。
7. 完整智能指针程序：转移独占拥有者、共享副本释放、弱引用提升后最后持有者释放；不访问悬空裸指针。
8. 引用环的完整失败与修复程序：外部拥有者离开后仍未析构，显式断环后清理；解释弱回指的设计作用。
9. 将责任写进接口和项目约定；四路线分别回到现有对象系统/运行时文章，不将 C++ RAII 当作 GC 或业务可用性证明。
10. 自测、验证范围、下一篇 CXX-03；更新索引、计划、CXX-01 后续链接和两篇旧文回链。

## 资料依据

核对日：2026-10-25；工作草案持续更新，仅使用 C++17 范围内的规则。

- [basic.life](https://eel.is/c++draft/basic.life)：对象寿命与存储的区别，析构调用开始与销毁过程不能混成一个完成日志。
- [stmt.dcl](https://eel.is/c++draft/stmt.dcl)、[class.dtor](https://eel.is/c++draft/class.dtor)、[except.ctor](https://eel.is/c++draft/except.ctor)：离开作用域、成员析构、异常展开及构造未完成时的清理。
- [Core Guidelines R.1](https://isocpp.github.io/CppCoreGuidelines/CppCoreGuidelines#Rr-raii)：资源管理的设计原则，不替代实际释放接口契约。
- [unique.ptr.single.ctor](https://eel.is/c++draft/unique.ptr.single.ctor)、[unique.ptr.single.modifiers](https://eel.is/c++draft/unique.ptr.single.modifiers)、[unique.ptr.single.dtor](https://eel.is/c++draft/unique.ptr.single.dtor)：不可复制、移动、reset/release 与删除器。
- [util.smartptr.shared](https://eel.is/c++draft/util.smartptr.shared)、[util.smartptr.shared.dest](https://eel.is/c++draft/util.smartptr.shared.dest)、[util.smartptr.weak.obs](https://eel.is/c++draft/util.smartptr.weak.obs)、[util.smartptr.shared.create](https://eel.is/c++draft/util.smartptr.shared.create)：共享责任、最后释放、弱提升与合并分配的边界。

## 验证记录

- 2026-10-25：从正文提取三个完整 C++17 程序；Windows x64、GCC 8.3.0（x86_64-posix-seh），编译参数 `-std=c++17 -O0 -g -Wall -Wextra -Wpedantic`。三个编译退出码均为 0，均无警告；三个运行退出码均为 0，断言全部通过。
- `raii.cpp` 的 14 行、`ownership.cpp` 的 9 行、`cycle.cpp` 的 2 行输出，均与正文逐行一致；两次 fclose 返回成功，断环后 Node::alive 为 0。
- `unique_copy_error.cpp` 以 `-std=c++17 -Wall -Wextra -Wpedantic -fsyntax-only` 编译，退出码为 1，诊断包含被删除的复制构造函数；符合预期。不执行双重释放、悬空访问或遗留泄漏。
- 初版 tmpfile 在本机返回空指针，未把这次失败写成成功；最终示例改为只读打开自身 `raii.cpp`，明确运行目录前提。不据此推断所有 Windows 环境的 tmpfile 行为。
- Hugo v0.157.0 最终本地构建通过：1373 页，退出码为 0，无 ERROR。输出位于本轮独立临时目录，检查后清理。
- 检查新文、公共目录、CXX-01、UFS-02、Piccolo CPP-01 五个生成页的 246 个站内链接/资源引用及锚点，缺失为 0；对应正文 relref 均有源文件。确认 CXX-01/02 互相可达、两篇旧文回链到公共第二篇、目录显示 2 篇已写且下一篇为 CXX-03。
- 更新范围：新正文与本执行记录；canonical plan、公共索引、根 doc-plan 的 C++ 系列行；CXX-01 后续链接、UFS RAII 解释、Piccolo CPP-01 公共前置提示。保留共享文件中其他任务的修改。
- 不包含：四个引擎/运行时的构建或调试、跨平台测试、性能测量、内存分配器或 GC 回收验证。
