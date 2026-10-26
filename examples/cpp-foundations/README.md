# 公共C++补桥：自有可复做样例

本目录只集中本次CXX-01/02/03/10与失败协议的完整自有片段，既有11篇中的其他练习继续按原文复做，不声称全量迁移。C++17；不依赖引擎。失败代码不进入默认构建，不执行UB。

| 目标 | 对应课 | 验收 |
|---|---|---|
| alias | CXX-01 | 指针别名上的const与函数签名 |
| observer | CXX-02 | 只读借用、显式转换、存活与业务状态 |
| member_order | CXX-03 | 声明顺序与初始化依赖 |
| failure_protocol | CXX-02；04/09回链 | 返回/异常/非法输入保留旧值；资源计数0；成功提交 |
| handle | CXX-10；02回链 | 前向声明/完整定义、移动赋值与析构位置 |

## 正常构建与运行

使用自己实际安装的生成器/编译器，固定身份。本次Windows x64实际使用GCC8.3.0（Strawberry，x86_64-posix-seh）、CMake4.3.1和MinGW Makefiles；命令中的绝对编译器与make路径只属于本机。

```powershell
cmake -S examples/cpp-foundations -B .cpp-adjustment-validation/cpp -G "MinGW Makefiles" -DCMAKE_BUILD_TYPE=Debug -DCMAKE_CXX_COMPILER=C:/Strawberry/c/bin/g++.exe -DCMAKE_MAKE_PROGRAM=C:/TDM-GCC-64/bin/mingw32-make.exe
if ($LASTEXITCODE -ne 0) { throw "CMake configure failed" }
cmake --build .cpp-adjustment-validation/cpp
if ($LASTEXITCODE -ne 0) { throw "Normal examples build failed" }
ctest --test-dir .cpp-adjustment-validation/cpp --output-on-failure
if ($LASTEXITCODE -ne 0) { throw "Example assertions failed" }
```

默认只含五个正常目标。GNU/Clang添加-O0与警告并取消NDEBUG，保留断言；MSVC/Clang配置虽有相应表达，本轮未运行，不声称跨平台通过。

## 独立编译拒绝

从仓库根运行，各文件应分别非零；只做语法检查，不产生或运行失败产物：

```powershell
$cases = @('alias_const', 'observer_implicit', 'observer_write', 'incomplete_value', 'inline_destructor')
foreach ($case in $cases) {
    g++ -std=c++17 -Wall -Wextra -Wpedantic -fsyntax-only "examples/cpp-foundations/fail/$case.cpp"
    if ($LASTEXITCODE -eq 0) { throw "Expected compile rejection: $case" }
}
```

本轮五项均退出1：只读指针变量赋值、explicit隐式构造、只读目标写入、不完整类型按值成员、在不完整Impl处实例化默认删除器。诊断文本与标准库实现相关；完整定义的正确三文件工程由正常目标handle回归。

本轮直接g++五正常编译/运行0、五拒绝1；统一CMake/CTest结果见执行报告。未运行UB、Sanitizer、动态库装载或引擎；没有新增跨平台CI/性能框架。
