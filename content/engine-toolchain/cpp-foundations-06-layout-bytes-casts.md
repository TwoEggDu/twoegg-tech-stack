---
title: "C++ 公共基础 06｜从一段字节读到一个值：布局、偏移、对齐与类型转换"
slug: "cpp-foundations-06-layout-bytes-casts"
date: "2026-10-25"
description: "用独立 C++17 程序观察结构布局、合法复制对象表示并显式解析小端字段，分清地址范围、对象寿命、类型访问、对齐与格式约定，建立四条源码路线共用的内存阅读基础。"
tags: ["C++", "源码阅读", "内存布局", "类型转换", "字节解析"]
series: "C++ 公共基础"
primary_series: "cpp-foundations"
series_order: 6
type: "reading-series"
topic_key: "engine"
content_stage: "article"
reading_title: "从一段字节读到一个值"
reading_summary: "先核对对象、范围、类型和对齐，再按明确的表示规则解释字节；转换表达式不替代这些前提。"
reading_index: "engine-toolchain/cpp-foundations-series-index.md"
weight: 496
---

在源码里遇到一个 `void*`，后面转成具体类型再读成员；在另一处看到一个偏移，代码从缓冲里取出整数。这两次“从地址读数据”，依赖的是同一套规则吗？

还有一种常见困惑：结构体成员的大小加起来只有 7，`sizeof` 却得到 12。把字段重新排一下，尺寸又变了。如果把这块内存直接写进文件，另一台机器是否就能按同一个结构读回来？

这一篇先建立一个判断：**地址告诉你去哪里，类型规定怎样访问，对象寿命和对齐限制访问是否成立；文件或网络中的字节，还需要独立的格式规则。** 强制转换并不能补齐缺少的前提。

> **读这篇之前**：掌握 [CXX-01 值与访问]({{< relref "engine-toolchain/cpp-foundations-01-values-references-signatures.md" >}}) 和 [CXX-02 生命周期与所有权]({{< relref "engine-toolchain/cpp-foundations-02-lifetime-ownership-raii.md" >}})。类层次转换按需回看 [CXX-03]({{< relref "engine-toolchain/cpp-foundations-03-classes-dispatch-destruction.md" >}})，保存字段地址的回调可回看 [CXX-05]({{< relref "engine-toolchain/cpp-foundations-05-callables-callback-lifetime.md" >}})。
>
> **复用与范围**：承接 [硬件数据布局]({{< relref "performance/hardware-cpu-f06-data-layout.md" >}}) 的排列问题、[HybridCLR ABI 桥接]({{< relref "engine-toolchain/hybridclr-bridge-abi-cross-boundary-calling-convention.md" >}}) 的跨边界表示模型，以及 Piccolo 缓冲材料的数据交接问题。这里集中讲 C++17 语言前提；性能、寄存器传参、GPU 数据布局和项目运行继续由对应专题负责。

## 1. 先判断：访问现有对象，还是解码外部格式

两类代码看起来都在读地址，证据来源却不同：

| 场景 | 类型与边界从哪里来 | 要解释什么 |
| --- | --- | --- |
| 从成员表取得字段地址 | 当前对象、成员声明、类型信息与有效阶段 | 那里是否仍有正确类型的成员对象 |
| 从数组取得第 i 项 | 数组起点、元素类型与长度 | i 是否有效，指针是否仍属于这个数组 |
| 从文件或网络读字段 | 格式定义、版本、长度与编码 | 哪几个字节表示哪个值，怎样检查非法输入 |
| 跨运行时/插件传递参数 | 双方约定的 ABI、对象/槽位布局与持有协议 | 传入内容是否满足对方的解释方式 |

至少检查五项：**范围是否足够、对象是否存在且有效、访问类型是否允许、地址是否满足对齐、表示规则是否对应。** 它们互相不能代替。即使机器能够执行某个未对齐读取，也没有替 C++ 程序证明其类型和寿命正确。

## 2. sizeof、alignof 和 offsetof 分别看哪一层

`sizeof(T)` 是对象表示占用的 C++ 字节数，类对象中可能包含填充；数组的 sizeof 是整个数组的大小。`sizeof(pointer)` 得到指针自身的大小，不能拿来推断它所指缓冲的长度。[C++17 sizeof](https://timsong-cpp.github.io/cppwp/n4659/expr.sizeof)

一个 C++ 字节是 `sizeof(char)` 对应的单位，`sizeof(char)` 为 1；不应在所有环境里直接把一个字节等同于 8 位。后面的二进制格式示例明确要求 `CHAR_BIT == 8`，并使用本实现提供的 `std::uint32_t`。精确宽度整数类型不是所有 C++ 实现都必须提供。[整数类型定义](https://timsong-cpp.github.io/cppwp/n4659/cstdint.syn)

`alignof(T)` 表示类型的对齐要求。可以把它理解为：这个类型的对象在内存中安放时，需要满足怎样的地址边界。对齐要求由实现确定，可以用 `alignas` 请求实现支持的更严格对齐；它不负责把一个任意外部地址修正成合法对象。[对齐规则](https://timsong-cpp.github.io/cppwp/n4659/basic.align)、[alignas](https://timsong-cpp.github.io/cppwp/n4659/dcl.align)

**填充（padding）**是布局中不直接承载这些成员值的部分。成员之间可能需要间隙，最后也可能有尾部填充，使连续数组中的后续对象仍满足对齐要求。字段顺序相同不等于不同平台偏移相同，也不能由字段大小之和直接得出结构体大小。

`offsetof(T, member)` 给出成员相对对象起始位置的字节偏移。本课只对标准布局类使用它；对非标准布局类，C++17 规定为条件支持，不能当作任意引擎类的通用字段探针，更不能对位域、静态成员或成员函数套用。[offsetof 的范围](https://timsong-cpp.github.io/cppwp/n4659/support.types.layout)

### 程序一：观察本机布局，不先写死答案

保存为 `layout_probe.cpp`：

```cpp
#include <cassert>
#include <cstddef>
#include <cstdint>
#include <iostream>
#include <type_traits>

struct Record
{
    char tag;
    std::uint32_t count;
    std::uint16_t flags;
};

struct Compact
{
    std::uint32_t count;
    std::uint16_t flags;
    char tag;
};

struct alignas(16) Block
{
    unsigned char storage[16];
};

int main()
{
    static_assert(std::is_standard_layout_v<Record>);
    static_assert(std::is_standard_layout_v<Compact>);
    static_assert(std::is_trivially_copyable_v<Record>);
    static_assert(alignof(Block) >= 16);
    assert(sizeof(Record) >= sizeof(char) + sizeof(std::uint32_t)
                              + sizeof(std::uint16_t));

    std::cout << "Record size=" << sizeof(Record)
              << " align=" << alignof(Record)
              << " offsets=" << offsetof(Record, tag) << ','
              << offsetof(Record, count) << ',' << offsetof(Record, flags) << '\n';
    std::cout << "Compact size=" << sizeof(Compact)
              << " align=" << alignof(Compact)
              << " offsets=" << offsetof(Compact, count) << ','
              << offsetof(Compact, flags) << ',' << offsetof(Compact, tag) << '\n';
    std::cout << "Block size=" << sizeof(Block)
              << " align=" << alignof(Block) << '\n';

    Record records[2]{};
    Record* first = records;
    Record* end = first + 2;
    first[1].count = 7;
    assert(end - first == 2 && records[1].count == 7);
    assert(sizeof(records) == 2 * sizeof(Record));
    std::cout << "array count=" << end - first
              << " step=" << sizeof(Record) << '\n';
}
```

本次 Windows x64、GCC 8.3.0 的观察结果记录于末节。程序没有断言 Record 必须为 12 字节，也没有断言换序一定减少尺寸。先观察，再结合当前实现的成员类型与对齐解释结果。

在这次验证中，Record 的 tag 位于 0、count 位于 4、flags 位于 8。前两个成员之间有间隙，结构体末尾也有填充；Compact 改变顺序后减少了间隙。这个结果只解释这组类型在这次配置中的布局，没有证明字段换序一定改善性能。改公开结构、序列化约定或跨模块接口时，还可能改变兼容性。

程序用到了两个不同的类型性质：**标准布局**适合在规则允许的范围讨论成员偏移；**可平凡复制**与后面的对象表示复制有关。两者都不表示“可以直接当作跨平台文件格式”，也不能用其中一个任意替代另一个。

## 3. 指针加一，移动的是一个元素

`Record* p` 上的 `p + 1` 指向后一个数组元素，步长由 Record 类型确定，不是移动一个字节。上例中的 `end - first` 则以元素个数计量。

这种运算有数组边界：可以形成数组尾后一位指针，不能解引用它；不能任意越过数组，也不能仅因两个独立分配的地址看起来接近就相减。程序只在同一个两元素数组内形成位置和尾后指针。[指针加减规则](https://timsong-cpp.github.io/cppwp/n4659/expr.add)

`void*` 表示类型被擦去的对象指针接口，本身不携带数组长度和目标类型元数据。标准 C++ 不允许对 void* 做这种元素算术；某些编译器扩展接受，不代表可移植规则。

字段偏移也不是任意访问的通行证。先知道某个偏移对应成员，仍要保证基址确实属于相应对象，成员仍有效，字段类型与访问方式匹配。回调保存了字段地址以后，还要继续追目标对象和容器修改，不能只检查偏移是否落在 sizeof 以内。

## 4. 复制对象表示，与转换数值是两件事

**对象表示**是一个对象占用的那组字节；其中参与表达值的部分是值表示。已有对象的合法表示，可以在明确条件下复制保存，而不必先把字节缓冲强转成它的类型。

C++17 对可平凡复制类型提供了相关保证：例如完整对象的底层字节可以复制到字符/字节数组，再复制回去恢复原值；同类型的两个合适对象也有表示复制的保证。这里刻意使用已经存在的目标对象，不把 C++20 的对象隐式创建规则提前套到 C++17。[对象表示与复制](https://timsong-cpp.github.io/cppwp/n4659/basic.types)

### 程序二：用已有对象完成表示往返

保存为独立程序 `representation_copy.cpp`：

```cpp
#include <array>
#include <cassert>
#include <climits>
#include <cstdint>
#include <cstring>
#include <iomanip>
#include <iostream>
#include <type_traits>

int main()
{
    static_assert(CHAR_BIT == 8);
    static_assert(sizeof(std::uint32_t) == 4);
    const std::uint32_t original = 0x01020304u;
    std::array<unsigned char, sizeof(original)> bytes{};
    std::memcpy(bytes.data(), &original, sizeof(original));

    std::uint32_t restored = 0;
    std::memcpy(&restored, bytes.data(), sizeof(restored));
    assert(restored == original);
    std::cout << "native=" << std::hex << std::setfill('0');
    for (auto byte : bytes)
        std::cout << ' ' << std::setw(2) << static_cast<unsigned>(byte);
    std::cout << std::dec << '\n';
    std::cout << "restored=" << restored << '\n';

    static_assert(std::is_trivially_copyable_v<float>);
    const float source = 3.75f;
    std::array<unsigned char, sizeof(source)> float_bytes{};
    std::memcpy(float_bytes.data(), &source, sizeof(source));
    float float_copy = 0.0f;
    std::memcpy(&float_copy, float_bytes.data(), sizeof(float_copy));
    const int numeric = static_cast<int>(source);
    assert(float_copy == source && numeric == 3);
    std::cout << "object-copy=" << float_copy << " numeric=" << numeric << '\n';

    std::uint32_t count = 7;
    void* opaque = &count;
    auto* typed = static_cast<std::uint32_t*>(opaque);
    *typed = 8;
    const std::uint32_t* view = typed;
    *const_cast<std::uint32_t*>(view) = 9;
    assert(count == 9);
    std::cout << "context=" << count << '\n';
}
```

第一段保存并恢复同一个整数类型的表示；打印时遍历真正的 unsigned char 数组，没有把结构体中的填充字节当成稳定数据，也没有把字节数组解引用成 uint32_t。

第二段复制 float 的合法表示，恢复后仍是 3.75；`static_cast<int>` 则做数值转换，丢弃小数部分得到 3。数值转换不负责保留原始位模式。本例使用可表示范围内的值，不能把浮点到整数转换推广成对 NaN、无穷和越界值的安全截断。[浮点与整数转换](https://timsong-cpp.github.io/cppwp/n4659/conv.fpint)

第三段从一个真实 uint32_t 对象得到 void*，再恢复原类型，因而有已知的对象、类型、对齐和寿命证据。转换没有创建新对象，也没有验证一个陌生 void* 是否真指向 uint32_t。

最后的 const_cast 修改的是**原本可变**的 count，只是访问路径曾加上 const。若底层实际声明为 const 对象，去掉访问路径上的 const 后写入仍不成立。[const_cast](https://timsong-cpp.github.io/cppwp/n4659/expr.const.cast)

### memcpy 还要求哪些前提

本例的复制区间完整、不重叠，源字节来自相同类型的合法对象，目标也已存在且可写。换成实际工程时，要分别检查：

- 源和目标范围是否足够，复制长度是否正确；重叠区间需要按对应库接口的契约处理。
- 类型是否满足相应表示复制条件；不能用 memcpy 接管 std::string、unique_ptr 等对象的复制和资源责任。
- 表示来源是否有效；任意输入字节不保证能构成合法的 bool、指针、浮点或其他类型的值。
- 复制是否暴露了进程地址、填充或平台表示；能够复制不等于适合持久化、网络传输或计算稳定哈希。

两个对象的成员值相同，其填充也不必具有相同字节；浮点等类型还可能存在多种表示。不能普遍用 `memcmp` 替代按值相等，也不能把整块结构体写出就称为稳定序列化。

## 5. 类型转换改变表达方式，不替你补对象

命名转换帮助读者识别意图，但能编译只是第一层检查：

| 转换 | 常见用途 | 仍要核对 |
| --- | --- | --- |
| static_cast | 数值转换、已知规则的类转换、从 void* 恢复对象指针 | 数值范围、真实对象类型；向下转型没有自动的运行时正确性检查 |
| dynamic_cast | 类层次中的转换；需运行时检查的情形通常依赖多态类型 | 对象本来就必须有效；检查失败的指针结果为空，引用形式会抛异常 |
| const_cast | 在规则允许范围调整 const/volatile 资格 | 底层对象是否允许相应读写，不能靠去 const 修改真正 const 的对象 |
| reinterpret_cast | 某些低层指针、引用和整数间的显式转换 | 对齐、类型访问、对象寿命、实现与 ABI 条件仍需分别成立 |

类转换的具体条件见 [static_cast](https://timsong-cpp.github.io/cppwp/n4659/expr.static.cast) 和 [dynamic_cast](https://timsong-cpp.github.io/cppwp/n4659/expr.dynamic.cast)。本篇没有用独立程序验证 RTTI 或引擎自己的类型系统。

尤其不要从 `reinterpret_cast<T*>(buffer)` 推出“buffer 中已经出现了 T 对象”。转换表达式没有执行 T 的构造函数，也没有自动证明通过 T 访问这段存储合法。[reinterpret_cast](https://timsong-cpp.github.io/cppwp/n4659/expr.reinterpret.cast)

通过不符合类型访问规则的左值读取对象，可能违反语言规则；能按 char、unsigned char 或 std::byte 等规定方式接触对象表示，不等于可以任意按另一对象类型读值。[类型访问规则](https://timsong-cpp.github.io/cppwp/n4659/basic.lval)

因此，即使给一个字符数组加了 alignas(T)，它也只是解决存储对齐这一项，并不自动提供完整的 T 对象寿命证明。原始存储、placement new、union 活跃成员和析构的完整关系，留到 CXX-09；这里先使用已存在的普通对象。

传统 `(T*)pointer` 这类 C 风格转换又可能组合多种转换能力，阅读时应拆回它究竟改变了类型、资格还是数值；不要只凭“旧代码这么写”认定它安全。

### 两个编译失败练习

分别保存，只做编译检查，不运行。

`unrelated_static_cast_error.cpp`：

```cpp
#include <cstdint>
void Read(unsigned char* bytes)
{
    auto* value = static_cast<std::uint32_t*>(bytes); // 不相关对象指针。
    (void)value;
}
```

`discard_const_error.cpp`：

```cpp
void Write(const int* source)
{
    auto* value = reinterpret_cast<int*>(source); // 不能靠它丢弃 const。
    (void)value;
}
```

把第一个改成 reinterpret_cast 也许能通过编译，仍然没有证明 bytes 指向正确类型、满足对齐且寿命有效的对象。把第二个换成 const_cast，也仍然需要核对底层是否真正可变。

未对齐解引用、把 float 按不允许的整数类型访问、越界指针运算和销毁后的成员访问，都不能靠一次“读出了预期数值”证明正确。本篇不执行这些路径。

## 6. 文件中的整数，用文件规定的顺序解码

假设格式约定一个字段由连续四个 **8 位字节**组成，低位字节在前。这个约定来自格式，不来自 Record 的 offsetof，也不来自当前机器的整数表示。

下面从字节数组偏移 1 读这个字段。代码没有要求该位置能作为 uint32_t 对象访问，而是先检查范围，再把各字节显式组合为一个 uint32_t 值。

保存为第三个独立程序 `decode_little_endian.cpp`：

```cpp
#include <array>
#include <cassert>
#include <climits>
#include <cstddef>
#include <cstdint>
#include <iostream>
#include <limits>

bool ReadU32LE(const unsigned char* data, std::size_t size,
               std::size_t offset, std::uint32_t& out)
{
    if (data == nullptr || offset > size || size - offset < 4)
        return false;
    out = std::uint32_t(data[offset])
        | (std::uint32_t(data[offset + 1]) << 8)
        | (std::uint32_t(data[offset + 2]) << 16)
        | (std::uint32_t(data[offset + 3]) << 24);
    return true;
}

int main()
{
    static_assert(CHAR_BIT == 8);
    static_assert(sizeof(std::uint32_t) == 4);
    const std::array<unsigned char, 6> bytes{{0x7f, 0x78, 0x56, 0x34, 0x12, 0xff}};
    std::uint32_t value = 0;
    const bool ok = ReadU32LE(bytes.data(), bytes.size(), 1, value);
    assert(ok && value == 0x12345678u);
    std::cout << "little-endian=" << value << '\n';

    const bool exact = ReadU32LE(bytes.data() + 1, 4, 0, value);
    assert(exact && value == 0x12345678u);
    const std::array<unsigned char, 4> high{{0xff, 0xff, 0xff, 0xff}};
    const bool full = ReadU32LE(high.data(), high.size(), 0, value);
    assert(full && value == std::numeric_limits<std::uint32_t>::max());

    value = 99;
    const bool short_read = ReadU32LE(bytes.data(), 4, 1, value);
    assert(!short_read && value == 99);
    std::cout << "truncated=unchanged\n";

    const bool at_end = ReadU32LE(bytes.data(), bytes.size(), bytes.size(), value);
    const bool beyond = ReadU32LE(bytes.data(), bytes.size(), 7, value);
    const bool huge = ReadU32LE(bytes.data(), bytes.size(),
                                std::numeric_limits<std::size_t>::max(), value);
    assert(!at_end && !beyond && !huge && value == 99);
    std::cout << "invalid-offset=unchanged\n";

    const bool null_read = ReadU32LE(nullptr, 4, 0, value);
    assert(!null_read && value == 99);
    std::cout << "null=unchanged\n";
}
```

输出应为：

```text
little-endian=305419896
truncated=unchanged
invalid-offset=unchanged
null=unchanged
```

边界检查先判断 `offset > size`，再计算 `size - offset`，避免无符号下溢；也没有直接以 `offset + 4` 判断，避免偏移相加溢出。对字节先转换成 uint32_t 再做高位移位，最终组合成明确的 32 位无符号结果。程序另外验证了恰好四字节的有效窗口，以及高位全为 1 时的最大值。

函数仍有调用者前提：data 非空时，声明的 size 必须对应实际可访问的字节范围；一个陌生指针配上伪造长度，无法被这个检查自动验证。out 也必须是有效的可写对象。本例在失败时保留原输出值，这是我们写出的接口契约。

这段代码定义了小端格式，即使机器本身使用别的字节顺序，也按这个规则计算值。当前实际运行只在一台 Windows x64 主机上完成，没有把它记成大端平台实测。若只是 memcpy 外部四字节到本机整数里，则还需要额外处理本机表示与格式顺序的对应关系。

真实文件还可能有版本、偏移表、字段宽度、长度单位和取值约束；这些需要继续验证。一个 C++ 结构声明本身没有提供这些协议。

## 7. 带回四条源码路线时，先追证据来源

| 路线 | 用本篇检查什么 | 继续阅读 |
| --- | --- | --- |
| Piccolo | 回调里的字段地址由哪个对象产生；类型、存储与借用寿命怎样对应；CPU 结构与 GPU 输入是否使用另外的布局约定 | [Piccolo 阅读入口]({{< relref "reading/piccolo-engine.md" >}})，再按对象、渲染与缓冲专项追踪 |
| Unreal | 反射字段偏移和类型从哪里来；当前实例和字段是否有效；引擎类型转换有哪些额外约定 | [Unreal 阅读入口]({{< relref "reading/unreal-engine.md" >}})，不对任意 UObject 类直接套 offsetof |
| HybridCLR | 参数槽地址、类型与长度由谁提供；解释器表示与目标平台 ABI 怎样对应；转换处的对齐和对象访问前提是什么 | [ABI 桥接]({{< relref "engine-toolchain/hybridclr-bridge-abi-cross-boundary-calling-convention.md" >}}) 与 [源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}}) |
| IL2CPP | metadata 的文件字段与运行时对象分别怎样组织；生成代码的对象类型、方法参数和原生模块版本是否对应 | [运行时产物地图]({{< relref "engine-toolchain/il2cpp-runtime-map-global-metadata-gameassembly-libil2cpp.md" >}})，文件格式和对象布局分别核对 |

这些是项目阅读任务，不是本次新增的引擎运行证据。本课的 Record、字节数组与整数解码器都是自有教学类型，没有替四个项目规定统一对象布局。

布局确认正确后，若要讨论 AoS/SoA、缓存与访问成本，再进入硬件数据布局专题；如果要讨论参数进哪些寄存器，则进入平台 ABI。结构体变小不自动等于某条路径更快，C++ 对象的对齐也不自动等于图形 API 的缓冲偏移要求。

## 8. 自测与验证边界

先回答六个问题：

1. Record 中各成员大小之和为什么可能小于 sizeof(Record)？
2. 指针加一、字段偏移加一和协议偏移加一，各自以什么单位解释？
3. 已知地址满足对齐，为什么还不能任意按 T 读取？
4. 保存合法 float 表示与 static_cast<int>，分别保留了什么？
5. 为什么 memcpy 不能普遍替代对象复制、成员比较和文件解码？
6. 小端解码函数如何检查截断与过大偏移，又无法替调用者证明什么？

三个正常程序分别保存和编译。以第一个为例，PowerShell 命令如下；其余将文件名换成对应程序名。保留断言，不定义 NDEBUG。

```powershell
g++ -std=c++17 -O0 -g -Wall -Wextra -Wpedantic layout_probe.cpp -o layout_probe.exe
if ($LASTEXITCODE -ne 0) { throw 'compile failed' }
.\layout_probe.exe
if ($LASTEXITCODE -ne 0) { throw 'run failed' }
```

两个失败用例分别以 `g++ -std=c++17 -Wall -Wextra -Wpedantic -fsyntax-only 文件名.cpp` 检查，预期非零退出，不生成或运行失败产物。

资料核对日为 2026-10-25，采用 C++17 工作草案 N4659。对象表示与寿命规则按这一基线讲解，不把后续版本的 bit_cast、endian 工具或隐式对象创建规则写成本篇已经使用的能力。

2026-10-25 本地验证环境为 Windows x64、GCC 8.3.0（`x86_64-posix-seh`）。三个正常程序分别编译，退出码为 0，所列警告选项下无警告；运行退出码均为 0，断言全部通过。

| 用例 | 本次结果 | 验证边界 |
| --- | --- | --- |
| layout_probe.cpp | Record 大小/对齐为 12/4，偏移 0/4/8；Compact 为 8/4，偏移 0/4/6；Block 为 16/16；数组计数 2、步长 12 | 4 行输出属于本机配置；断言只约束示例所需性质 |
| representation_copy.cpp | 本机 uint32_t 字节为 `04 03 02 01`；恢复值 16909060；float 表示往返 3.75、数值转换为 3；context 最后为 9 | 4 行输出一致；没有用外部任意字节构造 float，也没有修改真正 const 对象 |
| decode_little_endian.cpp | 解码 305419896，恰好四字节窗口与最大 uint32_t 通过；截断、尾后/过大偏移、最大 size_t 偏移和空指针均失败且保持输出 | 4 行输出一致；明确小端协议，未做大端主机实测 |
| unrelated_static_cast_error.cpp | 编译退出码为 1 | 诊断指出 unsigned char* 到 uint32_t* 的 static_cast 无效 |
| discard_const_error.cpp | 编译退出码为 1 | 诊断指出 reinterpret_cast 丢弃资格限定 |

本篇没有执行未对齐解引用、非法类型访问、越界或失效对象调用，也不包含四条项目路线的实际构建、跨平台布局/ABI、GPU 布局、性能测量和完整文件格式验证。

按补写顺序，下一篇进入 [CXX-04：复制、移动和容器修改以后，哪些访问还有效]({{< relref "engine-toolchain/cpp-foundations-04-move-containers-invalidation.md" >}})，继续区分对象地址、索引与身份，以及扩容、擦除后的借用失效。完整目录见 [C++ 公共基础]({{< relref "engine-toolchain/cpp-foundations-series-index.md" >}})。

**先证明访问对象的前提，再按对应的表示规则把字节解释成值。**
