# CXX-09：有内存为什么还没有对象：分配、构造、析构与未定义行为

> 执行层提纲与验证记录。唯一篇级目录为 [C++ 公共基础系列计划](cpp-foundations-series-plan.md)。

## 定位与复用

- 类型：原理篇；依赖 CXX-02、06，调用/容器问题回看 CXX-03～04，按补写顺序接 CXX-08。
- 复用 CXX-02 的释放责任与构造失败模型、CXX-06 的对齐/类型访问/对象表示前提、DS-19 的 Arena/Pool 策略问题；不继承旧文的笼统性能或托管/原生等同判断。
- 公共模型：存储的来源/范围/对齐、对象的创建/活跃状态/结束、资源的清理责任、访问路径的有效性分别记录。
- C++17 工作草案 N4659 为讲解基线，例子以需要构造/析构的类为主；不把较新隐式对象创建、construct_at、start_lifetime_as 等能力写作已使用机制。
- 不写完整分配器，不执行悬垂访问、错误释放、union 非活跃成员读取、越界、未对齐或非法别名访问；不复制 Unity 受限源码，不替代引擎 GC/对象创建协议。

## 正文提纲与实验

1. 从有地址却没有可用对象、析构后还能复用空间、清空 Arena 是否完成清理三个问题进入。
2. 四个层次与操作表：new 表达式 / operator new / placement new / 显式析构 / delete 表达式 / operator delete / malloc/free。
3. 程序一：对齐 byte 缓冲内构造持有 string 的对象，用仅析构的 RAII 删除器清理，再使用 placement new 返回的新指针重建。
4. 程序二：普通 new 构造抛出时的成员逆序清理、最外层析构不执行与匹配释放；对比原位构造失败时原始存储仍由调用者管理，补成功路径。
5. 程序三：int/string union 切换，显式管理 string 构造/析构与活跃状态；对照 variant，说明复制字节与类型转换不能自行激活任意对象。
6. 程序四：C++17 对齐分配/匹配释放与两层 RAII；合法整数表示往返，区别内存对齐、类型访问、表示与对象寿命。
7. 两个编译失败：非平凡 union 的隐式特殊成员被删除；const 缓冲不能通过 static_cast 丢掉 const。记录合法修复，不用强转绕过真正不可变存储。
8. 释放配对与 UB 风险表，placement new/显式析构不可与默认 delete 随意混用；launder 有前置条件，地址复用不证明旧借用有效。
9. 四路线按需对接：Piccolo 持有/清理、UE 普通对象/UObject、HybridCLR 参数槽与托管规则、IL2CPP 原生表示与 runtime 协议；项目运行与公共实验分开。
10. 同步目录、canonical plan、根系列进度和六篇旧文回链；下一篇 CXX-11。

## 资料依据

2026-10-25 核对 C++17 N4659：

- [对象寿命](https://timsong-cpp.github.io/cppwp/n4659/basic.life)、[new 表达式](https://timsong-cpp.github.io/cppwp/n4659/expr.new)、[delete 表达式](https://timsong-cpp.github.io/cppwp/n4659/expr.delete)、[标准原位形式](https://timsong-cpp.github.io/cppwp/n4659/new.delete.placement)。
- [单对象分配与释放](https://timsong-cpp.github.io/cppwp/n4659/new.delete.single)、[构造与异常](https://timsong-cpp.github.io/cppwp/n4659/except.ctor)、[union](https://timsong-cpp.github.io/cppwp/n4659/class.union)、[对象表示](https://timsong-cpp.github.io/cppwp/n4659/basic.types)。
- [类型访问](https://timsong-cpp.github.io/cppwp/n4659/basic.lval)、[launder](https://timsong-cpp.github.io/cppwp/n4659/ptr.launder)、[C 分配接口](https://timsong-cpp.github.io/cppwp/n4659/c.malloc)、[程序执行与未定义行为](https://timsong-cpp.github.io/cppwp/n4659/intro.execution)。

## 验证记录

- 2026-10-25：Windows x64、GCC 8.3.0（x86_64-posix-seh），正常参数 `-std=c++17 -O0 -g -Wall -Wextra -Wpedantic`，不定义 NDEBUG。
- placement_lifetime、construction_rollback、union_active_member、aligned_storage 四个程序编译/运行退出码均为 0，所列选项下无诊断，断言全部通过；输出分别为正文记录的 4、4、2、3 行。没有运行 UB 来证明结论。
- 原位重建确认同一数组内先后两个 Item，资源清理次数 2、最终 live=0；仅析构删除器不归还数组存储，旧借用被主动清空。
- 构造失败确认普通 new 的匹配类专用释放被调用，最外层析构未执行；placement 失败清理已完成的 Trace 成员但调用者的原始块仍存在，最后手动释放；成功路径确认完整析构与成员逆序清理。
- union 程序仅访问活跃成员；string 在切回 int 前显式 destroy_at，对照 variant 的成功切换。没有注入 string 构造失败或测试所有特殊成员/异常路径。
- 本机 Record 的 alignof 为 64，对齐分配成功；对象先析构、原始块后匹配释放，各一次。整数表示只复制到已存在 uint32_t，不复制整个非平凡 Record。
- union_default_error 与 const_storage_error 以 `-std=c++17 -Wall -Wextra -Wpedantic -fsyntax-only` 检查，退出码均为 1；诊断分别为隐式默认构造/析构被删除、const 数组无法转换为 void*。仅初始化 number 的 union 修复与去掉 const 的可变缓冲修复通过编译检查，未运行失败产物。
- 最终正文六个 C++ 代码块与实际测试文件逐字一致。没有新增 Sanitizer、真实分配失败、析构抛出、复杂透明替换、malloc 路径、跨平台、引擎/GC、并发或性能验证。
- 从当时 HEAD `cbc8038d2c438ff642c32f8431d2ebf66e188fec` 导出站点，叠加本次 11 个正文/目录/规划/回链文件；根 doc-plan 仅叠加 C++ 系列行，隔离其他并行编辑。Hugo v0.157.0 生成 1407 页，退出码 0、无 ERROR；该结果不代表全部脏工作区验证通过。
- 新正文、公共索引、CXX-08、CXX-02、CXX-06、DS-19、Piccolo CPP-01、UFS-02 共 8 个生成页面，397 处本地链接、资源和锚点检查通过；七个已有页面均能进入新正文，代码与正常输出可见。
- Canonical plan 与索引回写十篇公共正文已写、一篇待写，下一篇 CXX-11；批次 1～3 仅表示公共课完成，专项状态继续归各路线。六篇旧文保留 URL、归属和原始验证记录；DS-19 新增语言边界提示，不把 C# 策略示意当 C++ 分配器实测。提交/推送状态以 Git 记录为准。
