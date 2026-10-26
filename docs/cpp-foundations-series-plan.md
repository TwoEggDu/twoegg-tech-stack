# C++ 公共基础：引擎与运行时源码阅读系列计划

更新：2026-10-25。本文件是本系列唯一的 canonical 篇级目录；公共阅读入口为 [C++ 公共基础](../content/engine-toolchain/cpp-foundations-series-index.md)。

## 定位与目标

面向会编程、主要有 C#／Unity 经验的读者，建立能跨 Piccolo、Unreal、HybridCLR、IL2CPP 复用的 C++ 阅读能力。先能解释表达式、对象、调用、内存与构建，再进入各项目的专用类型和运行时约定。

采用两层前置：**公共 C++ 基础 → 各路线专项前置 → 具体源码主线**。公共课使用不依赖引擎的自有小程序，具体路线补自己的对象协议、宏、容器、构建和执行机制。专项前置要重新解释通用规则在该项目中怎样落地，但不再复制整套语言课。

系列规划与阅读入口已建立，既定 11 个公共正文单元 CXX-01～11 均已成稿。接下来按各路线的专项前置与源码主线继续；公共成稿与普通 C++ 实验不代表完整语言体系、专项内容或四条路线的工程运行已经验证。

## 边界与归属

| 层次 | 本层负责 | 后续交给谁 |
|---|---|---|
| 公共 C++ | 语言语义、资源责任、调用与内存、编译链接、并发的最低正确性规则 | 本系列 |
| 项目专项前置 | 项目类型/宏怎样应用或额外约束公共规则；首次跟读前需要什么 | Piccolo、UE、HybridCLR、IL2CPP 各自的 plan |
| 托管运行时前置 | CLI metadata、CIL、托管泛型、加载与初始化、GC 契约 | ECMA-335 与运行时系列，不以 C++ 模板或 RAII 替代 |
| 图形与硬件 | 坐标、渲染管线、GPU 同步、Cache 与平台 ABI 的深入内容 | 现有渲染、硬件与 ABI 专题，公共课只补语言接缝 |

- 公共教学程序优先沿用已有 C++17 基线；标准版本、编译器、平台 ABI 与具体项目版本分别记录，不能由教学基线推断四个项目的工具链要求。
- 目标是读懂常见实现并能做最小验证，不以完整标准库、模板元编程、无锁算法或自制编译器作为入门条件。
- 公开内容遵守本仓源码隔离规则；Unity 受限实现不复制，优先使用自有程序、允许公开的资料与项目实验。
- 旧 `doc-plan-archive-v26.md` 的七篇 C++ 规划保留为只读历史。本表承接当前范围，不回写归档。

## 状态与复用规则

本系列当前有 **1 个已写索引、11 个公共课目标单元**。其中 11 个已写（CXX-01～11）、0 个待整理、0 个待写。原有四篇基础材料仍属于原系列，继续承担综合练习与项目案例；引用它们不会增加公共正文数量。

- **已写**：目标正文存在；源码核对、小程序、引擎运行和发布状态继续分别记载。
- **待整理**：已有主要材料，可先读；仍须抽取通用模型、补齐独立示例与入口，不等于目标公共课已写。
- **待写**：缺少完整教学单元；即使已有零散小节，也不能作为完成依据。

复用分两步：先链接原文，再编写独立公共课，复用已核实的知识与自有实验，按“通用问题 → 模型 → 独立程序 → 项目跳转”组织，不原样复制整篇。CXX-01 已抽取 UFS 首课的值与借用模型，补齐 const、auto、签名及返回借用边界；CXX-02 复用 UFS 的 RAII 与独占责任、Piccolo 的共享持有与逻辑清理模型，补独立的文件清理、所有权转交和断环程序；CXX-03 复用 Piccolo 类与虚函数的调用模型，补隐藏/using/重载选择、值切片、析构链三个独立程序及编译失败边界；CXX-10 复用 UFS 多文件构建和构建调试专题模型，补三文件工程、三个预期失败、静态库与 GDB 实测，区分链接/导出符号和调试信息。UFS 对应解释收短回链，保留综合编译/调试练习；Piccolo 原文保留项目模型速查、具体调用链和实现证据，并回链相应公共课。原 URL、原编号和 `primary_series` 保持原归属。

CXX-05 复用设计模式前置的行为注入动机、Piccolo PCPP-2 的绑定与寿命模型及 CXX-02 的强弱持有规则，新增调用形式、捕获副本、单槽取消三个独立程序和两个编译失败边界。原项目材料继续保留固定源码与验证记录。

CXX-06 复用硬件布局与 ABI 专题的问题模型，补本机结构观察、合法对象表示复制、显式小端解码三个独立程序和两个转换失败练习；区分对象访问与外部格式，不把单机输出推广为跨平台约定。

CXX-04 复用 UFS 的 move/vector 与 Piccolo PCPP-1 的持有/遍历模型，补四个独立程序与两个失败练习；分别记录重载选择、noexcept 策略、本机搬迁观察和容器失效规则，不用失效访问验证结论。

CXX-07 复用 Piccolo 模板篇与 UFS 的类型实参、推导和定义可见性模型，补三个独立程序、一个显式实例化三文件工程、四个编译失败与一个链接失败变体；区分重载/特化、替换/函数体错误、转发与寿命，不把 C++ 模板当托管泛型。

CXX-08 复用 Piccolo 的阶段模型及 UE/HybridCLR/IL2CPP 的产物入口，补宏与条件程序、实际 JSON→C++ 生成器、显式函数表和 CMake 文件依赖。验证全新/增量构建、漏链接/漏注册及清单校验，不把教学生成成功扩展为项目生成器或引擎运行成功。

CXX-09 复用所有权、布局与分配器的分层模型，补原位重建、构造失败清理、union 活跃成员、对齐分配四个正常程序和两个编译失败边界；把存储、对象、资源和访问分别追踪，不运行 UB，不以原生实验替代托管/引擎规则。

CXX-11 复用所有权/回调与 Piccolo 交接模型、硬件内存序的问题入口，补五个正常程序、两个编译失败与修复；验证 join、锁快照、可关闭消息交接、一次性原子发布和原子更新逻辑反例。以语言推导证明合法性，重复运行仅作补充，不声称项目线程/GC/GPU 或性能验证完成。

公共编号使用 **CXX**，避免与 Piccolo 原有 CPP-01～03 混淆。下表中的 11 个正文单元均已有实际文件；成稿、独立验证、引擎运行与发布状态仍分开记录，不以链接存在替代验证。

## 公共课 canonical 目录

所有目标正文放在 `content/engine-toolchain/`。编号表示知识组织，不等于实际补写顺序。

| 编号 | 问题 / 拟题与范围 | 状态与可复用材料 | 前置与最低验收 | 目标文件 |
|---|---|---|---|---|
| CXX-00 | 公共基础入口：现在能读什么，怎样进入四条路线 | 已写：[索引](../content/engine-toolchain/cpp-foundations-series-index.md) | 分清公共课、专项前置与源码主线 | `cpp-foundations-series-index.md` |
| CXX-01 | 一次传参到底复制、借用还是只读访问：值、指针、引用、const、auto 与函数签名 | 已写；复用 UFS 首课模型，补独立程序与失败练习；验证见 [执行记录](cpp-foundations-01-values-references-signatures-outline.md) | 会基本编程；解释调用者是否改变、访问路径和借用寿命 | [正文](../content/engine-toolchain/cpp-foundations-01-values-references-signatures.md) |
| CXX-02 | 对象与资源由谁结束：作用域、RAII、unique/shared/weak 与自定义释放协议 | 已写；复用 UFS 首课及 Piccolo 智能指针模型，补三个独立程序与复制失败练习；验证见 [执行记录](cpp-foundations-02-lifetime-ownership-raii-outline.md) | CXX-01；预测释放顺序，分开逻辑停用、对象析构和存储释放 | [正文](../content/engine-toolchain/cpp-foundations-02-lifetime-ownership-raii.md) |
| CXX-03 | 一次成员调用执行谁：类、继承、重载、覆盖、切片与虚析构 | 已写；复用 Piccolo 类与虚函数模型，补三个独立程序、错误 override 和析构访问失败练习；验证见 [执行记录](cpp-foundations-03-classes-dispatch-destruction-outline.md) | CXX-01～02；追创建点、静态/动态类型和覆盖实现，不假设固定虚表布局 | [正文](../content/engine-toolchain/cpp-foundations-03-classes-dispatch-destruction.md) |
| CXX-04 | 复制、移动和容器修改以后，哪些访问还有效 | 已写；复用 UFS 与 Piccolo 容器模型，补四个独立程序、两个失败用例和容器调试检查；验证见 [执行记录](cpp-foundations-04-move-containers-invalidation-outline.md) | CXX-01～03；解释值类别、四种特殊成员、Rule of Zero、noexcept、vector/无序表失效；分别追地址、拥有关系、索引与身份，不运行失效访问 | [正文](../content/engine-toolchain/cpp-foundations-04-move-containers-invalidation.md) |
| CXX-05 | 行为怎样被保存并稍后调用：函数指针、Lambda、捕获与回调寿命 | 已写；复用行为注入与 Piccolo 回调模型，补三个独立程序及两个失败练习；验证见 [执行记录](cpp-foundations-05-callables-callback-lifetime-outline.md) | CXX-01～03；解释函数/成员指针、捕获、包装复制、强弱持有和取消后的任务副本；不将单线程结果推成跨线程取消保证 | [正文](../content/engine-toolchain/cpp-foundations-05-callables-callback-lifetime.md) |
| CXX-06 | 从一段字节读到一个值：布局、偏移、对齐与类型转换 | 已写；复用布局/ABI 模型，补三个独立程序、边界输入及两个编译失败练习；验证见 [执行记录](cpp-foundations-06-layout-bytes-casts-outline.md) | CXX-01～02；解释 sizeof/alignof/offsetof、指针运算、void*、cast 与 memcpy 前提；区分对象表示、本机布局和外部格式，不运行非法解引用 | [正文](../content/engine-toolchain/cpp-foundations-06-layout-bytes-casts.md) |
| CXX-07 | 模板调用怎样选择实现：推导、特化与常见类型工具 | 已写；复用 Piccolo 与 UFS 模板材料，补三个独立程序、三文件显式实例化工程与五个失败边界；验证见 [执行记录](cpp-foundations-07-template-reading-outline.md) | CXX-01、03、04；解释推导、重载/特化、依赖名、decltype/traits、SFINAE、if constexpr、参数包与转发；区分选择、实例化、链接和寿命，不展开完整元编程体系 | [正文](../content/engine-toolchain/cpp-foundations-07-template-reading.md) |
| CXX-08 | 眼前的代码由谁产生：宏、条件编译、外部生成与运行时注册 | 已写；复用四路线阶段/产物模型，补预处理、实际生成与显式注册、CMake 增量和失败边界；验证见 [执行记录](cpp-foundations-08-macros-code-generation-outline.md) | CXX-03、07；按输入、生产者、配置、消费者和生效时刻追踪；区分生成、链接与注册，不用教学实验代替项目运行 | [正文](../content/engine-toolchain/cpp-foundations-08-macros-code-generation.md) |
| CXX-09 | 有内存为什么还没有对象：分配、构造、析构与未定义行为 | 已写；复用 CXX-02/06 与分配器策略模型，补四个正常程序、两个编译失败与合法修复；验证见 [执行记录](cpp-foundations-09-storage-object-lifetime-ub-outline.md) | CXX-02、06；分别追存储/对象/资源/访问，解释 placement new、union、对齐和匹配释放、异常退出；不执行 UB，不推导项目 GC 规则 | [正文](../content/engine-toolchain/cpp-foundations-09-storage-object-lifetime-ub.md) |
| CXX-10 | 源文件怎样成为正在调试的程序：翻译单元、链接、库与符号 | 已写；复用 UFS 和构建调试专题，补三文件工程、失败变体、静态库与 GDB；验证见 [执行记录](cpp-foundations-10-build-link-symbols-outline.md) | CXX-01 起可先练；解释声明/定义、ODR、inline/static/extern、库与导出、C ABI 和符号匹配；编译/链接失败与恢复已验证，动态库及四路线工程未实测 | [正文](../content/engine-toolchain/cpp-foundations-10-build-link-symbols.md) |
| CXX-11 | 数据交给另一个线程后，谁保证它有效且可见 | 已写；五个正常程序、两个编译失败与修复，含 100 次额外运行；验证见 [执行记录](cpp-foundations-11-concurrency-handoff-outline.md) | CXX-02、04～05；分别证明寿命、互斥、发布、完成/复用；解释 thread/async、mutex/条件变量、atomic 与 volatile，区分无数据竞争和算法正确，不运行非法竞争 | [正文](../content/engine-toolchain/cpp-foundations-11-concurrency-handoff.md) |

各篇的小程序验证记录以正文和执行记录为准。待整理/待写单元的最低验收是后续要求，不表示实验已经执行。

## 阅读依赖与分流

### 现在可读的最短路径

| 阶段 | 路径 |
|---|---|
| 共同起点 | CXX-01 → 02 → 03 → 10；最小编译从第一课开始 |
| 访问、容器、回调 | 04/05/06按系统需求选读 |
| 布局、模板、生成 | 06 → 07 → 08，结合10 |
| 对象寿命与并发 | 09依赖02/06；11依赖02/04/05 |

四路线首次进入和按需补课见公共索引阶段表；不把十一篇全部读完作为进入项目的门槛。

### 目标公共路径

1. 入门共同段：CXX-01 → 02 → 03 → 10；CXX-10 的最小编译练习从第一课开始即可使用。
2. 访问与调用段：CXX-04～06 已写；各篇依赖以 canonical 表为准。
3. 模板与生成段：CXX-07～08 已写；结合 CXX-10 回看模板定义可见性和生成文件怎样进入构建。
4. 存储与交接段：CXX-09、11 已写；按目标衔接运行时、任务与线程系统。

各路线先完成将要使用的公共知识，再读自己的专项前置。可先阅读已有项目文章，不要求所有十一课写完才允许进入源码；缺课处明确标缺口，不能声称前置已全部满足。

## 四条路线的专项前置分工

这里仅维护依赖与责任边界。专项篇级拟题、状态和验收分别在所属 plan 维护，不另建一份平行总表。

| 路线 | 公共基础优先范围 | 专项前置要补的差异 | Canonical 归属与入口 |
|---|---|---|---|
| Piccolo | 01～03、04、07～08、10；回调/缓冲场景补 05～06 | CPP项目材料与PCPP-1/2/3均已成稿；继续补构建、事件退出与真实线程/GPU证据 | [Piccolo plan](piccolo-engine-series-plan.md#公共基础之后的专项前置)、[阅读入口](../content/reading/piccolo-engine.md) |
| Unreal | 01～05、07～08、10；存储/任务系统前补 06、09、11 | UCPP-1～5对象、容器、Delegate、生成模块和任务桥梁已成稿；UE编译运行与各故障矩阵待验 | [UE plan](ue-fullstack-expert-series-plan.md#cpp-source-prerequisites)、[阅读入口](../content/reading/unreal-engine.md) |
| HybridCLR | 01～02、05～06、09～10；模板/线程按入口补 07、11 | 既有ABI、解释器、源码起步与线程正文已吸收三项训练；按具体ABI/调用形态与未闭合证据补强，CLI/IL2CPP另作前置 | [HybridCLR plan](hybridclr-series-plan.md#公共基础之后的源码专项前置)、[阅读入口](../content/engine-toolchain/hybridclr-series-index.md) |
| IL2CPP | 01～03、05～06、09～10；泛型/线程按需补 07、11 | C#→IL→生成代码、托管与原生对象、metadata/代码注册、自建方法分派、泛型语义与 C++ 模板的区别 | [IL2CPP plan](il2cpp-series-plan.md#公共基础之后的源码专项前置)、[阅读入口](../content/engine-toolchain/il2cpp-series-index.md) |

## 补写顺序

以下为本专栏的内容建设顺序，不覆盖全站优先级。已有文章的公开状态不因本计划改变。

| 批次 | 公共课工作 | 紧随其后的专项工作 | 完成判据 |
|---|---|---|---|
| 0，已完成 | 建公共索引、canonical plan、四路线回链与缺口表 | 在各 plan 登记专项责任 | 已有链接可达、拟题不做假链接、状态不虚增 |
| 1，公共课已写 | CXX-01～03、10 已写；已补多文件、静态库与符号练习 | 复用 Piccolo CPP 三篇和 UE 首课，补前后阅读提示；UCPP-1 对象边界已成稿，UE 构建/GC 对照待验证 | 公共规则不依赖引擎；旧文继续承担应用/综合练习 |
| 2，公共课已写 | CXX-04～06 已写；已补回调、布局与容器访问 | HybridCLR复用既有ABI/解释器训练；整理ICPP-1生成代码跟读内容已嵌入D2（新Unity实测待验）；PCPP-1/2已成稿，补项目证据 | 独立程序与源码入口能逐句对应，区别各自寿命协议 |
| 3，公共课已写 | CXX-07～09 已写；已补模板、生成和存储/对象规则 | UCPP-2/3/4容器、Delegate与UHT已成稿，UE实验待验证；HybridCLR复用源码起步/线程训练；ICPP-2/3内容已嵌入D3/D5（新native接线待验） | 不混淆模板与托管泛型、编译器分派与 runtime 表、存储与对象 |
| 4，公共课已写 | CXX-11 已写；既定公共 11 单元齐备 | UCPP-5已成稿；继续补UE任务、Piccolo实际交接与runtime未闭合证据 | 明确发布/消费、所有权与同步；真实线程结论附证据 |

批次 1～4 的公共课已写不代表专项工作全部完成，专项状态仍以对应 plan 为准。

每批优先完成公共知识，再写依赖它的专项桥梁；同一批中的专项项按对应 plan 排序推进，不要求四条路线同时开写。2026-10-25 按用户指定先落地 Piccolo PCPP-1/2/3，必要的容器、回调与交接规则在专项中补桥；项目正文与验证归 Piccolo plan，公共 CXX-01～11 现已成稿，后续项目专项与实际运行继续由各路线计划维护。UCPP-1～5 已成稿，正文与执行记录由 UE 路线管理，不增加公共 CXX 篇数。

## 单篇产出与维护

后续补桥、专项与验证调整按 [C++ 公共基础与四路线调整执行计划](cpp-foundations-adjustment-execution-plan.md) 分批实施。本文件继续作为公共课唯一的 canonical 目录；执行计划记录逐项实施，实际内容/待验证状态由各路线canonical回写，详见本轮执行报告。

公共课采用原理篇结构：问题、模型、自有最小程序、误解/失败边界、四路线按需跳转。专项桥梁采用映射或源码案例结构：快速回链公共规则、项目增加的约定、固定版本入口、一个调用/状态变化、验证任务。

每篇至少包含：明确前置、读后能回答的问题、可独立编译的正常示例、一个有解释价值的失败或边界示例、编译器/标准/平台记录，以及已验证与待验证范围。跨平台布局和 ABI 不由单机输出推成通用保证。

完成正文后，先更新本 canonical 表与公共索引；专项正文更新所属 plan，再同步相应路线入口。根 `doc-plan.md` 只保留系列路由。旧文中重复的公共解释仅在新正文可替代后收拢，保留原项目证据和有效 URL。
