# CXX-06：从一段字节读到一个值

> 执行层提纲与验证记录。篇级目录统一维护在 [C++ 公共基础系列计划](cpp-foundations-series-plan.md)。

## 定位与复用

- 类型：原理篇。依赖 CXX-01～02，类转换按需回看 CXX-03；承接已写 CXX-05，下一篇 CXX-04。
- 复用硬件 F06 的布局组织问题、HybridCLR ABI 篇的跨边界表示模型和 Piccolo 状态缓冲篇的数据交接问题；公共课先讲 C++ 语言前提，原文继续承担性能、平台 ABI 与项目证据。
- 核心模型：范围、对象及其寿命、类型访问、对齐、数据表示分别核对；能保存/转换地址不等于能按目标类型读取。
- 自有 C++17 程序：标准布局结构的 sizeof/alignof/offsetof 和数组步长；合法对象表示复制、数值转换与 void* 恢复；显式小端读取及边界失败。两个预期编译失败：static_cast 不相关对象指针、reinterpret_cast 丢 const。
- 不运行未对齐解引用、类型不匹配读写、越界或失效对象访问。不用单机布局推断跨平台 ABI，不把 C++20 对象隐式创建或 bit_cast 规则混入 C++17。

## 正文提纲

1. 从字段地址、缓冲与 metadata 字节的误读提出问题，先确认对象访问还是格式解码。
2. sizeof/alignof/offsetof、成员与尾部填充、标准布局与可平凡复制的不同作用。
3. 程序一：两种字段顺序、本机偏移与尺寸、alignas、同一数组内的指针运算；输出明确属于本工具链。
4. 对象指针按元素移动、one-past 不可解引用；void* 不记录动态类型，也不能按标准 C++ 做 void* 算术。
5. 程序二：uint32_t 字节复制及恢复、float 表示复制与数值转换、已有对象的 void* 往返与合法 const_cast。
6. memcpy 前提、类型访问规则、padding/memcmp 与序列化；对齐存储不等于构造对象，详细寿命交给 CXX-09。
7. 四种命名 cast 与 C 风格转换的阅读边界；两个编译失败与不执行的 UB 对照。
8. 程序三：按长度检查并显式解码小端 uint32_t，失败不改变输出；测试截断、过大偏移和空指针。
9. 四路线分别追类型/尺寸来源、对象与文件边界、生成代码/运行时布局、引擎缓冲和平台 ABI。
10. 自测与实测记录；回写公共目录/规划、根路由、CXX-05 后续及三篇复用文章回链。

## 资料依据

资料核对日：2026-10-25。C++17 工作草案 N4659：

- [对象表示与平凡复制](https://timsong-cpp.github.io/cppwp/n4659/basic.types)、[对齐](https://timsong-cpp.github.io/cppwp/n4659/basic.align)、[sizeof](https://timsong-cpp.github.io/cppwp/n4659/expr.sizeof)、[offsetof](https://timsong-cpp.github.io/cppwp/n4659/support.types.layout)。
- [指针运算](https://timsong-cpp.github.io/cppwp/n4659/expr.add)、[类型访问](https://timsong-cpp.github.io/cppwp/n4659/basic.lval)、[static_cast](https://timsong-cpp.github.io/cppwp/n4659/expr.static.cast)、[reinterpret_cast](https://timsong-cpp.github.io/cppwp/n4659/expr.reinterpret.cast)。
- [const_cast](https://timsong-cpp.github.io/cppwp/n4659/expr.const.cast)、[dynamic_cast](https://timsong-cpp.github.io/cppwp/n4659/expr.dynamic.cast)、[alignas](https://timsong-cpp.github.io/cppwp/n4659/dcl.align)、[精确宽度整数](https://timsong-cpp.github.io/cppwp/n4659/cstdint.syn)、[浮点到整数](https://timsong-cpp.github.io/cppwp/n4659/conv.fpint)。

## 验证记录

- 2026-10-25：从正文提取五个完整代码块，分别保存。Windows x64、GCC 8.3.0（x86_64-posix-seh），正常参数 `-std=c++17 -O0 -g -Wall -Wextra -Wpedantic`；三个编译/运行退出码均为 0，所列选项下无警告，断言全部通过，最终正文代码与测试文件一致。
- layout_probe 的本机结果：Record 为 12 字节、对齐 4、偏移 0/4/8；Compact 为 8 字节、对齐 4、偏移 0/4/6；Block 为 16/16；两元素数组的步长为 12。四行输出核对通过；不把这些尺寸写成跨平台断言。
- representation_copy：uint32_t 表示为 `04 03 02 01`，往返值 16909060；float 表示往返为 3.75、数值转换为 3；合法 void* 往返及对原本可变对象去 const 写入后为 9。四行输出一致。
- decode_little_endian：三组成功输入（常规字段、恰好四字节、最大 uint32_t）与五组失败输入（截断、尾后偏移、超范围偏移、最大 size_t、空指针）全部符合断言；失败保留输出 99，四行输出一致。未做大端主机实测。
- 两个失败用例以 `-std=c++17 -Wall -Wextra -Wpedantic -fsyntax-only` 检查，退出码均为 1：分别命中不相关对象指针的 invalid static_cast、reinterpret_cast 丢弃资格的诊断。未运行任何非法解引用或失败产物。
- 完整工作区 Hugo 构建遇到无关并行编辑：`game-art-production-02-concept-art.md` 引用了尚不存在的 `art-concept-variants` shortcode，未修改该文章或模板。
- 为验证本次内容，从当时 HEAD `be9669199fbce5f36737fed4fadfc0a5d039bb87` 导出独立站点，叠加本篇、目录/规划、旧文回链及其必要的已有依赖页面，共覆盖 22 个文件。Hugo v0.157.0 构建 1401 页，退出码为 0，无 ERROR；这是该副本的验证，不代表全部工作区改动通过。
- 检查新正文、公共索引、CXX-05、ABI 桥接、硬件 F06、Piccolo PCPP-3 共 6 个生成页面，298 处本地链接、资源和锚点通过；ABI 原有按中文标题生成的 URL 保留，所有旧文回链存在。
- Canonical plan、索引与根系列路由已回写为六篇公共正文已写，下一篇 CXX-04。原有 CXX-05 及其他并行改动保留；没有新增引擎构建、跨平台 ABI、GPU 布局或性能验证结论。
