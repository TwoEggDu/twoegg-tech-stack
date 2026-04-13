# HybridCLR 专栏结构诊断与补桥计划

> 诊断日期：2026-04-13
> 当前状态：31 篇（HCLR-0 索引 + HCLR-1~30 正文）
> 目标：补 6 篇前置/过渡文章，从"高级"推到"专家级"

---

## 一、诊断结论

### 核心问题

专栏目标读者是"熟悉 Unity，不一定熟悉 .NET runtime"的高级工程师。但从 HCLR-1 第一句开始，系列就默认读者已经理解 CLR metadata、CIL 指令集、ABI 调用约定等 .NET runtime 底层概念。

这导致 4 个主要断层：

| 断层 | 位置 | 影响 |
|------|------|------|
| metadata 一词贯穿 31 篇但从未定义其内容 | HCLR-1 起 | 读者用模糊概念搭建全部地图 |
| CIL 栈机模型在 HCLR-27 才首次解释 | HCLR-1~9 用"IL"但不解释模型 | HCLR-27 形成概念墙 |
| IL2CPP generic sharing 在 HCLR-10 突然出现 | HCLR-2 只讲 metadata 层泛型 | 打碎读者已建立的心智模型 |
| GC 内部在 HCLR-28 突然出现 | 前 24 篇从未涉及 | HCLR-28/30 缺少前置 |

### 当前成熟度

| 维度 | 评分 |
|------|------|
| 技术深度 | 9/10 |
| 覆盖完整性 | 9/10 |
| 工程实用性 | 9.5/10 |
| 前置知识衔接 | 6/10 ← 主要短板 |
| 阅读顺畅度 | 7/10 |

---

## 二、6 篇补桥文章计划

### Pre-A：CLI Metadata 基础｜TypeDef、MethodDef、Token、Stream 到底是什么

**优先级：P0**
**插入位置：HCLR-1 之前（前置层）**
**解决问题：** 给"metadata"这个贯穿全系列的核心词一个实体定义

| 必须覆盖 | 明确不展开 |
|---------|-----------|
| ECMA-335 的 5 个 metadata stream（#Strings、#US、#Blob、#GUID、#~） | 完整 ECMA-335 规范翻译 |
| 核心 metadata 表：TypeDef、MethodDef、FieldDef、MemberRef、TypeRef | 所有 45 张 metadata 表 |
| metadata token 结构：高 8 bit = 表编号，低 24 bit = 行号 | PE 文件格式完整解析 |
| method body 在 PE 文件中的物理位置 | CLI 异常处理表完整结构 |
| 用 ildasm 看一个简单类的 metadata 布局 | |

**读完后读者能回答：**
- "补充 metadata" 补的到底是哪些表、哪些 stream
- `(image, token)` 为什么能唯一定位一个方法
- `AOTHomologousImage` 按 token/row 对齐到底在对齐什么

---

### Pre-B：CIL 指令集与栈机模型｜ldloc、add、call 到底在做什么

**优先级：P0**
**插入位置：Pre-A 之后，HCLR-1 之前（前置层）**
**解决问题：** 给 HCLR-27（指令优化）提供前置，同时让 HCLR-1/5 的"method body → transform → execute"链条更具象

| 必须覆盖 | 明确不展开 |
|---------|-----------|
| CIL 是栈机：eval stack、local variable table、argument table | 所有 200+ CIL opcode |
| 核心指令：ldloc/stloc、ldfld/stfld、call/callvirt、newobj、box/unbox | CIL 验证规则 |
| 一个最小方法体的 IL 逐行拆解（`int Add(int a, int b)` 级别） | JIT 编译器内部 |
| 为什么栈机直接解释慢（每条指令隐含 push/pop） | |
| 引出"为什么 HybridCLR 要先 transform 成寄存器 IR" | |

**读完后读者能回答：**
- HCLR-27 里的 `ldloc.0; ldloc.1; add` → `BinOpVarVarVar_Add_i4` 到底发生了什么
- 为什么 HybridCLR 不直接解释 CIL

---

### Bridge-C：ABI 与跨边界调用｜为什么 interpreter 调 AOT 需要一层 bridge

**优先级：P1**
**插入位置：HCLR-3（工具链）之后，HCLR-5（调用链）之前**
**解决问题：** 给 MethodBridge 的存在理由一个底层解释

| 必须覆盖 | 明确不展开 |
|---------|-----------|
| 什么是 ABI（Application Binary Interface）和调用约定 | 所有平台的 ABI 规范 |
| ARM64 AAPCS 参数传递规则（x0-x7 整型、d0-d7 浮点） | x86/MIPS/RISC-V 的 ABI |
| interpreter 的 StackObject 布局 vs native 函数的寄存器布局 | MethodBridge 生成器源码细节 |
| 为什么跨边界必须做参数搬运 | ReversePInvoke 完整实现 |
| MethodBridge stub 到底在做什么 | |

**读完后读者能回答：**
- 为什么 MethodBridge.cpp 要按签名生成，缺了不是慢而是调不通
- HCLR-3 说的"ABI 边界"到底指什么
- HCLR-26 说的"函数注入"为什么和 ABI 有关

---

### Bridge-D：IL2CPP 泛型共享规则｜引用类型共享 object，值类型为什么不能

**优先级：P1**
**插入位置：HCLR-2（AOT 泛型）之后，HCLR-10（FGS）之前**
**解决问题：** 补上 HCLR-2 到 HCLR-10 之间"IL2CPP 原本就做了部分 generic sharing"的断层

| 必须覆盖 | 明确不展开 |
|---------|-----------|
| IL2CPP 对引用类型泛型的共享策略：List\<string\> 和 List\<MyClass\> 共享 List\<object\> 的 native 实现 | 完整的 IL2CPP 泛型代码生成器源码 |
| 为什么值类型不能共享：内存布局不同 | ECMA-335 泛型约束完整规范 |
| 这件事对 DisStripCode 写法的直接影响（object 替代引用类型 vs 精确保留值类型） | |
| 引出 FGS 为什么是"改调用模型"而不是"改共享规则" | |

**读完后读者能回答：**
- HCLR-10 说的"old generic sharing"到底是什么
- HCLR-20/21 里 DisStripCode 用 object 替代引用类型的 ABI 层原因
- 为什么值类型泛型是 AOT 泛型问题的重灾区

---

### Bridge-E：解释器基础｜dispatch loop、stack vs register、IR 是什么

**优先级：P2**
**插入位置：HCLR-9（性能）之后，HCLR-27（指令优化）之前**
**解决问题：** 给 HCLR-27 搭过渡桥，避免概念墙

| 必须覆盖 | 明确不展开 |
|---------|-----------|
| 解释器的三种 dispatch 方式：switch、computed goto（threaded）、JIT | 完整的 JIT 编译器实现 |
| stack machine vs register machine 的区别 | 学术级寄存器分配算法 |
| 什么是 IR（Intermediate Representation） | LLVM IR / GCC GIMPLE |
| HybridCLR 选了 switch dispatch + register-style IR 的设计理由 | |
| 用一个 3 指令示例（ldloc.0; ldloc.1; add）对比栈式 vs 寄存器式执行过程 | |

**读完后读者能回答：**
- HCLR-27 里"peephole optimization"和"dead branch elimination"是什么
- 为什么 HybridCLR 的 transform 不是"预处理小功能"而是核心链路
- 商业版的"指令分派优化"可能改的是哪个 dispatch 方式

---

### Bridge-F：IL2CPP GC 模型｜BoehmGC、GC root、write barrier 与解释器的关系

**优先级：P2**
**插入位置：HCLR-24（回归防线）之后，HCLR-25（DHE 内部）之前**
**解决问题：** 给 HCLR-28（热重载）和 HCLR-30（Incremental GC）提供前置

| 必须覆盖 | 明确不展开 |
|---------|-----------|
| Unity/IL2CPP 使用 BoehmGC（保守式 GC）| BoehmGC 源码实现 |
| GC root 注册：全局变量、栈、静态字段 | 分代 GC 完整算法 |
| write barrier：为什么 Incremental/Generational GC 需要它 | 其他 GC 实现（SGen、ZGC） |
| 解释器的 StackObject 为什么给 GC 带来问题 | |
| MachineState::RegisterDynamicRoot 的意义 | |

**读完后读者能回答：**
- HCLR-28 里"IL2CPP 为什么不能卸载程序集"背后的 GC 层原因
- HCLR-30 里"Incremental GC 需要 write barrier"的具体含义
- 为什么 HybridCLR 从 v4.0.0 起要在 Instruction.h 里加 WriteBarrier 变体

---

## 三、补桥后的完整阅读顺序

```
═══ 前置层 ═══

Pre-A.  CLI Metadata 基础（新增 P0）
Pre-B.  CIL 指令集与栈机模型（新增 P0）
Pre-0.  IL2CPP 运行时地图（已有）

═══ 基础层 ═══

HCLR-1.  原理拆解
HCLR-2.  AOT 泛型与补充元数据

Bridge-D. IL2CPP 泛型共享规则（新增 P1）

HCLR-3.  工具链拆解

Bridge-C. ABI 与跨边界调用（新增 P1）

HCLR-4.  MonoBehaviour 资源挂载链路
HCLR-5.  调用链实战

═══ 收束层 ═══

HCLR-6.  边界与 trade-off
HCLR-7.  最佳实践
HCLR-8.  故障诊断手册
HCLR-9.  性能与预热策略

═══ 进阶层 ═══

HCLR-10. Full Generic Sharing
HCLR-11. DHE
HCLR-12. 高级能力选型
HCLR-13. FAQ

═══ 案例层 ═══

HCLR-14~18. 案例 + 崩溃定位 + CI

═══ AOT 泛型工程化子系列 ═══

HCLR-19~24. 修法决策 → DisStripCode → 配合 → 坑型录 → 防线

═══ 深度层 ═══

Bridge-E. 解释器基础（新增 P2）
Bridge-F. IL2CPP GC 模型（新增 P2）

HCLR-25. DHE 内部机制
HCLR-26. 函数注入与脏函数传染
HCLR-27. 解释器指令优化
HCLR-28. 程序集热重载
HCLR-29. 代码加密与访问控制
HCLR-30. 加载加速与内存优化
```

---

## 四、建议写作顺序

按优先级和阻塞关系：

| 顺序 | 文章 | 优先级 | 理由 |
|------|------|--------|------|
| 1 | Pre-A CLI Metadata 基础 | P0 | 影响全系列 31 篇，最高优先 |
| 2 | Pre-B CIL 指令集与栈机模型 | P0 | 与 Pre-A 同属前置层，一起写 |
| 3 | Bridge-D IL2CPP 泛型共享规则 | P1 | 修复 HCLR-2→10 断层 |
| 4 | Bridge-C ABI 与跨边界调用 | P1 | 修复 MethodBridge 理解基础 |
| 5 | Bridge-E 解释器基础 | P2 | 给 HCLR-27 搭桥 |
| 6 | Bridge-F IL2CPP GC 模型 | P2 | 给 HCLR-28/30 搭桥 |

---

## 五、预期效果

| 维度 | 当前 | 补桥后 |
|------|------|--------|
| 前置知识衔接 | 6/10 | 9/10 |
| 阅读顺畅度 | 7/10 | 9/10 |
| 整体定位 | 高级 | **专家级** |

补完后，这个系列将是中文互联网上 HybridCLR 方向唯一一个从 .NET runtime 底层到商业功能实现的全链路技术专栏。
