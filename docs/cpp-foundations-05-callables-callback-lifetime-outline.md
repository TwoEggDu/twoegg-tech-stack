# CXX-05：行为怎样被保存并稍后调用

> 执行层提纲与验证记录。篇级目录统一维护在 [C++ 公共基础系列计划](cpp-foundations-series-plan.md)。

## 定位与复用

- 类型：原理篇；依赖 CXX-01～03，构建操作回看 CXX-10。接入既定补写顺序，后续 CXX-06 → 04。
- 复用设计模式前置 04 的行为注入动机、Piccolo PCPP-2 的绑定/转发/接收者寿命模型，以及 CXX-02 的强弱持有模型；原文章保留自己的 C# 示例和固定版本项目证据，不把项目专用约定改成标准 C++ 保证。
- 公共模型：调用接口、保存位置、捕获关系、执行资格与取消协议。回调不自带异步或线程语义。
- 自有 C++17 独立程序：调用形式与签名；值/引用/this/对象副本捕获及包装复制；单槽订阅、弱捕获与已复制任务的取消边界。预期失败：成员函数指针不能当普通函数指针、move-only 闭包不能放进 C++17 std::function。
- 范围：不做通用事件总线，不假定无分配或固定闭包布局，不执行悬空调用，不展开多播重入/跨线程取消、引擎 Delegate 与托管委托内部实现。

## 正文提纲

1. 保存行为后“谁来调用、靠什么找到目标、当时是否有效”的问题。
2. 普通/静态/成员函数指针、using 别名、函数对象、Lambda 与 std::function 各负责什么。
3. 程序一：普通指针、成员 const、std::invoke、仿函数、无捕获转换、bind 与 Lambda 适配、空包装异常。
4. 闭包包含状态的模型；按值、引用、指针值、this、C++17 *this、mutable 与初始化捕获；引用别名追被引用对象。
5. 程序二：快照与借用、独立复制的 mutable 状态、this 与对象副本、引用别名、独占捕获；不运行返回局部引用的错误。
6. std::function 的复制、非空含义、C++17 CopyConstructible 要求与开销边界；两个失败练习。
7. 程序三：单槽注册 ID、裸目标先取消再销毁、weak 过期跳过但槽仍在、强捕获保活、取消无法撤回副本。
8. 注册/已排队/正在执行/完成的边界；RAII connection 的前提、环引用、逻辑停用与线程同步。
9. 四条路线的阅读记录与跳转；不把 C++ 包装等同于 UE Delegate 或托管委托。
10. 自测、真实验证记录；更新目录/规划、CXX-10 后续、CXX-02 和两篇复用文章回链。

## 资料依据

2026-10-25 核对 C++17 工作草案 N4659：

- [闭包类型](https://timsong-cpp.github.io/cppwp/n4659/expr.prim.lambda.closure)、[捕获](https://timsong-cpp.github.io/cppwp/n4659/expr.prim.lambda.capture)、[成员指针运算](https://timsong-cpp.github.io/cppwp/n4659/expr.mptr.oper)、[invoke](https://timsong-cpp.github.io/cppwp/n4659/func.invoke)。
- [function](https://timsong-cpp.github.io/cppwp/n4659/func.wrap.func)、[包装构造/复制](https://timsong-cpp.github.io/cppwp/n4659/func.wrap.func.con)、[调用与空包装异常](https://timsong-cpp.github.io/cppwp/n4659/func.wrap.func.inv)、[weak_ptr::lock](https://timsong-cpp.github.io/cppwp/n4659/util.smartptr.weak.obs)。
- [bind](https://timsong-cpp.github.io/cppwp/n4659/func.bind.bind)：保存绑定参数与调用时的占位符映射。

## 验证记录

- 2026-10-25：从正文直接提取五个完整 C++ 代码块，分别保存。Windows x64、GCC 8.3.0（x86_64-posix-seh），正常程序参数为 `-std=c++17 -O0 -g -Wall -Wextra -Wpedantic`，不定义 NDEBUG。
- callable_forms、capture_state、callback_lifetime 三个程序编译和运行退出码均为 0；所列警告选项下无警告，全部断言及各 4 行输出通过。最终正文代码与测试文件逐字核对一致。
- member_pointer_error 使用 `-std=c++17 -Wall -Wextra -Wpedantic -fsyntax-only`，编译退出码为 1，诊断指出 `void (Receiver::*)(int)` 不能转换为 `void (*)(int)`。
- move_only_function_error 同样仅做语法/实例化检查，编译退出码为 1，诊断涉及闭包复制及 unique_ptr 已删除的复制构造。没有运行失败产物，也没有执行悬空引用、失效 this 或数据竞争。
- Hugo v0.157.0 工作区本地构建：1398 页，退出码为 0，无 ERROR。新正文、公共索引、CXX-02/10、Piccolo PCPP-2、设计模式前置 04 共 6 个生成页面，335 处本地链接、资源和锚点检查通过，旧文回链存在。
- 已同步 canonical 目录、公开索引、根系列路由和相关旧文；五篇公共正文已写，下一篇 CXX-06，再写 CXX-04。没有把单槽模型登记成通用事件总线，也没有新增四条源码路线的工程/运行结论。
- 本篇未执行引擎构建、实际窗口事件、托管跨边界调用、跨线程取消或性能实验。原 Piccolo 材料的源码基线及原 MSVC 教学验证记录保留；本次公共程序的 GCC 结果单独记录。
