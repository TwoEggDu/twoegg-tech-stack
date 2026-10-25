# CXX-01：值、指针、引用与函数签名

> 执行层提纲与验证记录。唯一篇级目录为 [C++ 公共基础系列计划](cpp-foundations-series-plan.md)。

## 定位与边界

- 类型：原理篇。按已确认的补写顺序交付第一篇独立公共课，不改变四条源码路线的分工。
- 读者：会基本编程、以 C# / Unity 经验为主；不要求已有 C++ 工程或引擎源码。
- 问题：同样把变量交给函数，为什么有时只改副本，有时改原对象，有时只改指针的局部副本？
- 最小模型：对象 / 保存地址的指针对象 / 引用别名 / 访问权限 / 有效访问时间。先分清这些关系，再判断所有权。
- 复用：抽取 UFS-02 的 Score 按值/引用练习和访问与所有权分层；Piccolo CPP-01 的“复制包装不等于复制目标”作为项目延伸。独立程序重新编写、重新验证，不沿用旧文的实验结论。
- 范围：值与引用初始化、指针访问与空值、指针形参和指针引用、const 的层级、auto 与 auto&、返回值/借用、成员函数尾部 const、最小编译及编译失败练习。
- 后续：CXX-02 承担 RAII 与智能指针；CXX-03 承担类与分派；CXX-04 承担复制/移动细节；CXX-05/06 承担函数指针与布局；CXX-10 承担完整链接和符号。
- 基线：自有标准 C++17 教学代码；四路线只给阅读方法和已有文章入口，不新增具体引擎实现或运行断言，不引用 Unity 受限源码。

## 正文提纲

1. 从“调用后谁变了”提出问题，建立对象、访问路径与寿命模型。
2. 用同一个 Score 区分值复制、引用别名与指针变量；解释声明和表达式中的 & / *、点和箭头。
3. 从形参初始化理解传参；指针按值传递仍能修改目标，改变调用者的指针需要另一层访问。
4. const 限定当前访问路径；分开 const T*、T* const 和成员函数尾部 const，说明浅层只读的边界。
5. auto 从初始化推导类型；普通对象、引用来源、指针来源分别判断，防止把 auto 当自动借用。
6. 返回独立值或借用；对象销毁后的访问无效，临时对象的延寿有特定条件。
7. 一个完整程序同时验证按值、引用、指针改绑、auto、const 视图与按值包装共享目标。
8. 两个应编译失败的完整片段；只作静态说明的悬空例子不运行。
9. 用自有接口练习签名阅读；映射到 Piccolo、Unreal、HybridCLR、IL2CPP 各自仍需补足的知识。
10. 回写公共索引、canonical plan、根路由状态和 UFS 原文回链，保留原综合练习及验证历史。

## 资料与证据

资料核对日：2026-10-25。C++ 工作草案链接持续更新，正文只采用 C++17 已有规则。

| 资料 | 用途 |
| --- | --- |
| [dcl.init.ref](https://eel.is/c++draft/dcl.init.ref)、[dcl.ref](https://eel.is/c++draft/dcl.ref) | 引用初始化、不可改绑、引用参数 |
| [expr.call](https://eel.is/c++draft/expr.call) | 实参初始化形参；不把传值缩成对任意目标的深复制 |
| [dcl.ptr](https://eel.is/c++draft/dcl.ptr)、[dcl.type.cv](https://eel.is/c++draft/dcl.type.cv) | 指针本身与目标的 const 分层、只读访问路径 |
| [dcl.type.auto.deduct](https://eel.is/c++draft/dcl.type.auto.deduct)、[temp.deduct.call](https://eel.is/c++draft/temp.deduct.call) | auto 按值推导与引用/const 的区别 |
| [class.temporary](https://eel.is/c++draft/class.temporary) | 直接绑定的延寿与引用形参的完整表达式边界 |
| [expr.prim.this](https://eel.is/c++draft/expr.prim.this) | C++17 普通成员函数中的 this 与尾部 const |
| [C++ Core Guidelines F.16](https://isocpp.github.io/CppCoreGuidelines/CppCoreGuidelines#Rf-in) | 小值按值、较大输入按 const 引用的接口建议；不是性能实测 |

## 验证记录

- 2026-10-25，从正文完整代码块提取 `main.cpp`；已有 Windows x64 GCC 8.3.0（Strawberry，x86_64-posix-seh），参数 `-std=c++17 -O0 -g -Wall -Wextra -Wpedantic`。编译退出 0、无诊断，运行退出 0，全部断言通过，十行输出与正文逐行一致。
- 从正文表格提取七种 auto 声明，以 `std::is_same_v` 做编译期类型断言；从正文提取 Board 及调用片段，验证值接收结果为 10、引用接收结果为 20。编译无诊断、运行退出 0。
- 从正文提取 `readonly-error.cpp`、`binding-error.cpp`，使用上述 C++17 警告参数和 `-fsyntax-only`，均退出 1；有效诊断分别为 `increment of member 'Score::value' in read-only object` 与 `cannot bind non-const lvalue reference ... to an rvalue`。悬空访问反例不执行。
- Hugo v0.157.0 在独立输出目录构建退出 0，1369 页、54 个静态文件，无 ERROR。正文、公共索引与 UFS 原文的 40 个 relref、生成页 134 个本地链接/锚点/资源引用均通过；公共索引补 `chapters` 锚点供正文返回目录。
- 浏览器核对第一课标题、01 / 01 的已写篇数、面包屑、右侧目录与公共索引状态表；阅读样式正常。索引明确显示 1 个公共正文、5 个待整理、5 个待写，下一篇为 CXX-02。
- 自查区分了对象复制与指针/包装复制、const 路径与目标不可变、按值 auto 与引用接收、声明可读出的信息与项目契约、直接临时绑定与经形参返回的悬空风险。UFS 仅收拢通用解释，其综合程序和历史实验记录保留。
- 未进行：四个引擎/运行时的构建、源码调试、性能测试或跨编译器验证。普通语言实验不代表项目验证。
