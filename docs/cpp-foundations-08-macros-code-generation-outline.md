# CXX-08：眼前的代码由谁产生：宏、条件编译、外部生成与运行时注册

> 执行层提纲与验证记录。篇级目录统一维护在 [C++ 公共基础系列计划](cpp-foundations-series-plan.md)。

## 定位与复用

- 类型：原理篇，依赖 CXX-03、07；编译与链接回接 CXX-10，函数指针和寿命回接 CXX-05。
- 复用 Piccolo CPP-03 的宏/解析器/文本模板/运行时注册分层，UE 工程地图的 UHT 与模块构建，HybridCLR 工具链的输入/产物/消费者，IL2CPP 转换器的生成与平台编译分工。
- 公共模型：每段代码或表项都找输入、生产者、配置、输出、消费者和生效时刻；区分源码 token、外部文件、编译链接与运行时状态。
- 自有实验：宏的字符串化/拼接与预处理输出；条件开关的未定义/0/1；Python 从显式 JSON 清单生成 C++，CMake 建立文件依赖，再显式注册函数表。
- 边界：教学生成器不解析 C++/AST，不模拟完整反射、序列化、GC 或托管执行；不复制 Unity 受限源码。项目版本、标准规则、工具观察与引擎运行分别记载。

## 正文与实验顺序

1. 第一屏从“声明看见了却找不到定义”“普通编译为空的标记仍影响生成”进入。
2. 阶段模型与生产/消费关系；宏展开不是自动注册，生成成功不是已经链接/启动。
3. 宏独立程序：对象式/函数式宏、# 与 ## 的两层展开、括号/多语句边界、模板参数逗号和重复求值风险。
4. 条件程序：defined 与值的区别，-D 与 include 环境；#if、普通 if 和 if constexpr 各自能排除什么；配置不一致的 ODR/布局风险不执行 UB。
5. JSON→Python→generated cpp→目标文件→可执行程序→注册表的小工程，定义可见的文件依赖与显式启动入口。
6. CMake 全新构建、无输入变化、改清单与改生成器的增量检查；手动生成的快照也能确定性重现。
7. 失败/边界：模板逗号宏参数失败、运行时 false 不能隐藏类型错误、漏链接生成 cpp、漏注册、重复名、未知名、清理后查询；校验坏清单并保留旧输出。
8. 四路线落点与静态初始化、裁剪/保留、动态库寿命等工程边界；现有项目文章保留各自证据与验证状态。
9. 更新目录、canonical plan、根系列行、CXX-07/10 与四条路线旧文回链；下一篇 CXX-09。

## 资料依据

2026-10-25 核对 C++17 N4659 的 [宏替换](https://timsong-cpp.github.io/cppwp/n4659/cpp.replace)、[条件包含](https://timsong-cpp.github.io/cppwp/n4659/cpp.cond)、[#](https://timsong-cpp.github.io/cppwp/n4659/cpp.stringize)、[##](https://timsong-cpp.github.io/cppwp/n4659/cpp.concat)；GCC 8.3 的 [预处理选项](https://gcc.gnu.org/onlinedocs/gcc-8.3.0/gcc/Preprocessor-Options.html)、[参数预展开](https://gcc.gnu.org/onlinedocs/gcc-8.3.0/cpp/Argument-Prescan.html)、[多语句宏](https://gcc.gnu.org/onlinedocs/gcc-8.3.0/cpp/Swallowing-the-Semicolon.html)、[重复求值](https://gcc.gnu.org/onlinedocs/gcc-8.3.0/cpp/Duplication-of-Side-Effects.html)。

构建依据为 [CMake add_custom_command](https://cmake.org/cmake/help/v4.3/command/add_custom_command.html)。项目映射只补职责与入口，参考 [Epic UHT](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-header-tool-for-unreal-engine)、[HybridCLR Package 9.x 手册](https://www.hybridclr.cn/docs/basic/com.code-philosophy.hybridclr) 与 Unity 官方 IL2CPP 介绍；具体菜单、路径、版本兼容与运行结论仍由各路线文章负责。

## 验证记录

- 2026-10-25：Windows x64，GCC 8.3.0（x86_64-posix-seh）、Python 3.12.4、CMake 4.3.1、MinGW Makefiles / GNU Make 3.82.90。正常 C++ 参数 `-std=c++17 -O0 -g -Wall -Wextra -Wpedantic`，未定义 NDEBUG。
- 宏程序编译运行通过，3 行输出与正文一致；-E -P 结果包含 make_Actor、原始/展开字符串及未加括号的运算展开。条件程序的未定义、显式 0、显式 1 三组构建/运行通过，输出分别为 defined=0/enabled=0/result=10、1/0/10、1/1/11；预处理输出核对 step 分支及 MissingType 不再出现。
- Python 实际从 JSON 生成 C++，重复执行文本一致。手动构建与 CMake 全新构建均通过，4 行运行输出与正文一致，断言全部通过；正常编译在所列选项下无诊断。
- 增量实测：无变化时生成命令没有再次执行且输出修改时间不变；修改 jump delta 后重生成并运行得到 13；旧 manual.exe 保持 12；修改生成脚本注释后重新生成但文本内容不变。还原输入后恢复 12，手动与 CMake 生成内容一致。registry.h 增加显式 cstddef 包含后，消费者重编译、生成器不重跑；最终正常/漏注册程序重编译运行通过。
- 两个单文件失败以 -fsyntax-only 检查，均退出 1，分别诊断宏接收到两个实参与 MissingType 未声明；类型别名修复变体通过。main.cpp 独立编译成功，遗漏生成目标文件的链接退出 1，诊断缺少 register_generated。没有执行失败产物。
- 漏注册程序与生成 cpp 一起编译链接成功，运行确认空表；完整程序确认重复注册拒绝、未知名为空、clear 后查询为空。坏清单在写入前因重复名抛 ValueError，退出 1，原正常输出字节不变；未测试 I/O 中断或批量回滚。
- 最终正文 11 个 C++/Python/CMake/JSON/生成输出代码块与实测文件逐字一致。项目生成器、引擎运行、跨平台、自动静态注册、链接裁剪、动态库卸载、并发和性能均未验证。
- 从当时 HEAD `58d9a2b7084f8011825666cac2d9b589ec1cd592` 导出站点，叠加本次 11 个正文/规划/目录/回链文件，根 doc-plan 只叠加 C++ 系列行，隔离其他并行修改。Hugo v0.157.0 生成 1406 页，退出码 0、无 ERROR；不代表整个脏工作区都已通过。
- 新正文、公共索引、CXX-07、CXX-10 以及四路线旧文，共 8 个生成页面，518 处本地链接、资源和锚点检查通过；七个已有页面均有新正文入口，正常代码与输出在 HTML 中可见。HybridCLR 旧页沿用原有中文 URL，没有更改 slug。
- Canonical plan 与索引回写九篇正文已写、两篇待写；下一篇 CXX-09，随后 CXX-11。根 doc-plan 仅更新 C++ 系列状态，六篇旧文保留原 URL、归属、项目材料和验证记录。提交/推送状态以 Git 记录为准。
