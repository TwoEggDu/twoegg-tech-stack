# CXX-04：复制、移动和容器修改以后，哪些访问还有效

> 执行层提纲与验证记录。篇级目录统一维护在 [C++ 公共基础系列计划](cpp-foundations-series-plan.md)。

## 定位与复用

- 类型：原理篇，依赖 CXX-01～03；按补写顺序接 CXX-06，随后整理 CXX-07。
- 复用 UFS-02 的 move/vector 综合练习与 Piccolo PCPP-1 的容器元素、包装、目标对象、成员资格和局部保活模型。旧文保留综合代码、项目证据与各自的原验证记录。
- 核心判断：操作的是新对象还是已有对象、表达式允许哪种重载、容器修改影响哪条访问路径，以及业务身份由谁维持。
- 四个自有 C++17 程序：复制/移动构造与赋值、const 与命名右值引用；move_if_noexcept 与 vector 搬迁的观察；vector 重新分配/追加/擦除、索引身份及独占目标；unordered_map rehash、元素擦除与局部保活。
- 两个预期编译失败：对 const unique_ptr 做 move、含 unique_ptr 的类因用户声明析构而没有隐式移动。
- 边界：不实现完整容器或分配器，不展开转发引用推导、多线程、引擎容器契约、GC 搬迁或泛型内部表示。不执行任何失效迭代器/指针访问。

## 正文提纲

1. 从扩容后的旧引用、擦除后的旧索引和对象表删除后仍存活的问题切入。
2. 对象/包装/目标/业务身份；构造与赋值的不同，值类别属于表达式。
3. 程序一：四种特殊成员、const 来源与命名 T&&；Rule of Zero 与成员自己的复制/移动契约。
4. moved-from 的有效但未指定状态，unique_ptr 的具体保证，move 不保证资源转移/地址变化/性能收益。
5. 程序二：显式 move_if_noexcept 的分支与两类 vector 的本机搬迁观察；异常保证与实现策略分开。
6. 两个失败用例；析构、复制和移动的生成关系，不能把 trait 名字当作实际调用了移动构造的证明。
7. 程序三：强制重新分配后重新取得访问、容量足够追加、erase 返回值、ID 重查与不移动目标的独占包装。
8. 常见 vector 失效表；reserve/resize、end、emplace、clear、shrink_to_fit 的边界。
9. 程序四：unordered_map rehash 的迭代器与引用区别、erase 结束成员资格但局部 shared_ptr 继续保活；无效路径不执行。
10. 四条源码路线和自测、真实验证记录；更新目录/规划、CXX-06 后续与两篇复用旧文回链。

## 资料依据

核对日 2026-10-25，C++17 工作草案 N4659：

- [复制/移动构造](https://timsong-cpp.github.io/cppwp/n4659/class.copy.ctor)、[复制/移动赋值](https://timsong-cpp.github.io/cppwp/n4659/class.copy.assign)、[move 与 move_if_noexcept](https://timsong-cpp.github.io/cppwp/n4659/forward)、[标准库 moved-from 状态](https://timsong-cpp.github.io/cppwp/n4659/lib.types.movedfrom)。
- [vector 容量](https://timsong-cpp.github.io/cppwp/n4659/vector.capacity)、[vector 修改](https://timsong-cpp.github.io/cppwp/n4659/vector.modifiers)、[无序容器](https://timsong-cpp.github.io/cppwp/n4659/unord.req)、[容器通用要求](https://timsong-cpp.github.io/cppwp/n4659/container.requirements.general)、[unique_ptr 移动构造](https://timsong-cpp.github.io/cppwp/n4659/unique.ptr.single.ctor)。
- [C++ Core Guidelines C.20](https://isocpp.github.io/CppCoreGuidelines/CppCoreGuidelines#Rc-zero)：Rule of Zero 的设计建议；与语言的隐式生成规则分别解释。
- [值类别](https://timsong-cpp.github.io/cppwp/n4659/basic.lval)、[复制消除](https://timsong-cpp.github.io/cppwp/n4659/class.copy.elision)：表达式分类与返回时的操作次数不应混同。

## 验证记录

- 2026-10-25：从正文提取六个完整 C++ 代码块，分别保存。环境为 Windows x64、GCC 8.3.0（x86_64-posix-seh）；正常参数 `-std=c++17 -O0 -g -Wall -Wextra -Wpedantic`，不定义 NDEBUG。四个程序编译和运行退出码均为 0，在所列选项下无警告，全部断言通过。
- copy_move_selection 的 8 行输出通过：四种操作、const 来源复制、命名右值引用按左值使用、Document 的独立成员复制和来源重新赋值。
- noexcept_relocation 的 4 行输出通过：显式 move_if_noexcept 分别复制 1 次/移动 0 次，以及复制 0 次/移动 1 次；本机 vector 搬迁观察也分别为 1/0 与 0/1。前者有接口规则断言，后者只记录实现计数，未测抛出或回滚。
- vector_access 的 5 行输出通过：扩容后重取访问、无重新分配追加、ID 20 从索引 1 到 0 而旧索引变成 ID 30、erase 返回值推进、不可移动的 Entity 在独占包装移动后仍被正确拥有。
- map_membership 的 3 行输出通过：rehash 后映射值引用与目标仍有效、erase 移除成员但局部 keeper 继续保活、最后一个强拥有者释放后 weak 过期。
- 两个失败文件仅以 `-std=c++17 -Wall -Wextra -Wpedantic -fsyntax-only` 检查，退出码均为 1，分别诊断 const unique_ptr 来源无法移动而尝试已删除复制、Owner 因 unique_ptr 成员而隐式复制被删除。没有运行失败产物。
- vector_access 和 map_membership 另外以 `-D_GLIBCXX_DEBUG` 构建运行，两个编译/运行退出码均为 0，无调试迭代器错误。未执行任何失效指针或迭代器访问。
- 从当时 HEAD `c3ec9989d8e40036f09fc5afb2426023fedb86af` 导出站点，叠加本次八个正文/目录/规划/回链文件，隔离其他并行编辑。Hugo v0.157.0 构建 1402 页，退出码为 0，无 ERROR；该结果对应本次副本，不代表全部工作区改动通过。
- 新正文、公共索引、CXX-06、UFS-02、Piccolo PCPP-1 共 5 个生成页面，261 处本地链接、资源和锚点检查通过，旧文回链存在。最终正文的六个程序与测试文件逐字一致。
- Canonical plan 与索引已回写七篇公共正文已写，下一篇 CXX-07；根 doc-plan 仅改 C++ 系列状态，其他并行改动保留。本篇没有新增引擎运行、并发、GC、异常回滚或性能验证结论。
