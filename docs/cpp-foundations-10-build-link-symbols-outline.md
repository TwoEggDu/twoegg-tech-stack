# CXX-10：源文件怎样成为正在调试的程序

> 执行层提纲与验证记录。篇级目录统一维护在 [C++ 公共基础系列计划](cpp-foundations-series-plan.md)。

## 定位与复用

- 类型：原理篇。按补写顺序接在 CXX-03 后；编号表示知识组织，最小构建从 CXX-01 起即可练习。
- 复用 UFS-02 的声明/定义、多文件构建和故障分层模型，以及构建调试前置 02/02b 的原生/托管链、符号与源码匹配模型。原文保留综合程序、MSVC 操作和托管调试专题，新增公共课回链。
- 目标：读者能从一个调用追到所属翻译单元、定义所在目标文件/库、实际运行模块及其调试信息，并分别解释编译失败、链接失败和符号不匹配。
- 使用自有三文件 C++17 工程；正文完整提供代码。正常构建、三个失败变体、静态库和 GDB 都围绕同一工程，不引入引擎源码。
- 边界：不展开完整编译器、动态装载器、ABI 兼容矩阵、CMake/UBT 配置、托管符号协议和性能测量；动态库与导出先建立模型，具体平台操作交给专项。

## 正文提纲

1. 现象：IDE 能跳到声明，链接却失败；改了源码，运行/断点还在旧程序。
2. 抽象链：源码与头文件 → 翻译单元 → 目标文件 → 库/可执行模块 → 装载与调试映射；区分链接符号、导出符号、调试信息。
3. 三文件工程 `Frame.h`、`Frame.cpp`、`main.cpp`：extern 变量声明、单一定义、inline 函数/变量、文件内 static helper 和 C linkage 包装。
4. 分步编译、链接与运行；区分 -I/-L/-l，说明编译驱动器；按对象文件观察符号的引用和定义。
5. 一次一个失败：未声明调用、漏链目标文件、移除头文件函数 inline；恢复与独立输出目录。
6. ODR、包含保护、inline 语义及 static 的不同语境；不能把链接成功当作 ODR 正确证明。
7. 同一实现打入静态库；动态库/导入库/运行时模块、C ABI 与导出的分界。
8. 用 GDB 核对当前文件、断点、参数、调用栈与返回；优化与调试信息独立，PDB 必须匹配产物，源文件路径也须核对。
9. 四路线分别接到构建基线、UE 工程地图、HybridCLR 起步、IL2CPP 产物地图；项目版本/宏/生成文件继续按原路线核验。
10. 自测和验证边界；更新 canonical plan、公共索引、根路由、CXX-01/03 后续入口和三篇复用旧文；下一篇 CXX-05。

## 资料依据

核对日：2026-10-25。

- C++17 工作草案 N4659：[翻译单元](https://timsong-cpp.github.io/cppwp/n4659/lex.separate)、[声明与定义](https://timsong-cpp.github.io/cppwp/n4659/basic.def)、[ODR](https://timsong-cpp.github.io/cppwp/n4659/basic.def.odr)、[inline](https://timsong-cpp.github.io/cppwp/n4659/dcl.inline)、[名字的链接属性](https://timsong-cpp.github.io/cppwp/n4659/basic.link)、[语言链接](https://timsong-cpp.github.io/cppwp/n4659/dcl.link)。
- GCC 8.3 官方手册：[阶段选项](https://gcc.gnu.org/onlinedocs/gcc-8.3.0/gcc/Overall-Options.html)、[链接选项](https://gcc.gnu.org/onlinedocs/gcc-8.3.0/gcc/Link-Options.html)、[调试选项](https://gcc.gnu.org/onlinedocs/gcc-8.3.0/gcc/Debugging-Options.html)。
- GNU 官方手册：[nm](https://sourceware.org/binutils/docs/binutils/nm.html)、[ar](https://sourceware.org/binutils/docs/binutils/ar.html)、[GDB 文件](https://sourceware.org/gdb/current/onlinedocs/gdb.html/Files.html)、[继续与单步](https://sourceware.org/gdb/current/onlinedocs/gdb.html/Continuing-and-Stepping.html)。
- Microsoft：[DLL 导出](https://learn.microsoft.com/en-us/cpp/build/exporting-from-a-dll?view=msvc-170)、[PDB 与源码匹配](https://learn.microsoft.com/en-us/visualstudio/debugger/specify-symbol-dot-pdb-and-source-files-in-the-visual-studio-debugger?view=visualstudio)。

## 验证记录

- 2026-10-25：从正文提取三个完整代码块，Windows x64、GCC 8.3.0（x86_64-posix-seh），以 `-std=c++17 -O0 -g -Wall -Wextra -Wpedantic` 分别编译两个翻译单元，再链接。退出码均为 0，所列警告选项下无警告；输出为 `first=12`、`second=22`、`calls=2`，断言全部通过。
- 三个独立失败副本：未声明 `Advanc` 编译退出 1；漏链 Frame.o 的两个编译成功、链接退出 1，报告 undefined reference；移除 IsEven 的 inline 后两个编译成功、链接退出 1，报告 multiple definition。三个副本分别恢复、重编、重链、运行后全部成功，输出与正常版本一致。未运行失败产物。
- Binutils 2.32：nm 核对引用 U、对外函数 T、内部函数 t；原始名包含 `_ZN6lesson7AdvanceEi` 与 `lesson_step`。ar 归档列出 Frame.o；main.o 经 `-Lbuild -lframe` 链接成功，输出和断言保持一致。
- GDB 8.2.1：将正文交互命令写入命令文件后批量执行，退出 0，无命令错误。info files / info source 核对当前 exe 和源码；命中 Frame.cpp:14，current=10，计数 0→1；bt 回到 main.cpp:7，finish 返回 12；继续执行后正常退出。调试信息报告 DWARF 2。
- Hugo v0.157.0 工作区本地构建通过：1383 页，退出 0，无 ERROR。检查新文、目录、CXX-01/03、UFS 和构建调试 02/02b 共 7 个生成页面，309 处本地链接、资源和锚点均可达；所有旧文回链均存在。
- 动态库构建/装载、跨语言调用、MSVC/PDB 操作、四条源码路线实际构建与断点、跨平台 ABI 和性能均未在本篇执行。语言规则、GNU 实验结果和后续项目阅读任务分别标明。
