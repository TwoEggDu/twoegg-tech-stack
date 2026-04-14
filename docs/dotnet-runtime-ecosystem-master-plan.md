# .NET Runtime 生态全景系列 — 总体规划

> 从 ECMA-335 规范出发，系统拆解 CoreCLR、Mono、IL2CPP、HybridCLR、LeanCLR 五大 CLR 实现的架构决策与工程 trade-off。
> 目标：中文技术社区第一个覆盖所有主流 CLR 实现的全链路知识体系。

---

## 一、系列定位

### 这个系列要做什么

同一份 ECMA-335 规范，5 个 runtime 做了 5 套不同的实现决策。把这些决策拆开对比，就是整个系列的主线。

```
                    ECMA-335 规范层
                         │
           ┌─────────────┼─────────────┐
           │             │             │
      Type System    Metadata     Execution
      (类型系统)     (元数据)     (执行模型)
           │             │             │
     ┌─────┴─────┐ ┌─────┴─────┐ ┌─────┴─────┐
     │           │ │           │ │           │
  CoreCLR    Mono  IL2CPP  HybridCLR  LeanCLR
     │           │ │           │ │           │
     └─────┬─────┘ └─────┬─────┘ └─────┬─────┘
           │             │             │
        JIT 路线     AOT 路线     Interpreter 路线
```

### 目标读者

- Unity 高级工程师（IL2CPP / HybridCLR 方向）
- .NET Runtime 工程师（CoreCLR / Mono 方向）
- H5 / 小游戏开发者（LeanCLR / WebAssembly 方向）
- 做技术选型的架构师（横切对比方向）

### 四条阅读线

| 线 | 读者 | 入口 → 路径 |
|----|------|-------------|
| **线 1：Unity 工程师线** | 做热更新的人 | ECMA-335 → IL2CPP → HybridCLR |
| **线 2：Runtime 工程师线** | 想理解 CLR 实现的人 | ECMA-335 → CoreCLR → Mono → LeanCLR |
| **线 3：H5/小游戏线** | WebAssembly/小游戏开发者 | ECMA-335 → LeanCLR 接入与工程化 |
| **线 4：架构对比线** | 做技术选型的人 | 横切：同一个概念在 5 个 runtime 里的不同实现 |

所有线共享同一个 ECMA-335 基础层。

---

## 二、模块划分与预计规模

### 模块 A：ECMA-335 基础层（公共地基）

**定位：** 所有阅读线的公共入口。不讲任何 runtime 实现，只讲规范本身。

| 编号 | 主题 | 状态 | 说明 |
|------|------|------|------|
| ECMA-A1 | CLI Metadata 基础：TypeDef、MethodDef、Token、Stream | ✅ 已有（Pre-A） | 对应 ECMA-335 Partition II §22-24 |
| ECMA-A2 | CIL 指令集与栈机模型：ldloc、add、call | ✅ 已有（Pre-B） | 对应 ECMA-335 Partition III |
| ECMA-A3 | CLI Type System：值类型 vs 引用类型、泛型、接口、约束 | 待写 | 对应 ECMA-335 Partition I §8 |
| ECMA-A4 | CLI Execution Model：方法调用约定、虚分派、异常处理模型 | 待写 | 对应 ECMA-335 Partition I §12 |
| ECMA-A5 | CLI Assembly Model：程序集身份、strong naming、版本策略 | 待写 | 对应 ECMA-335 Partition II §6 |
| ECMA-A6 | CLI Memory Model：对象布局、GC 契约、finalization 语义 | 待写 | 对应 ECMA-335 Partition I §12.6 |
| ECMA-A7 | CLI 泛型实例化模型：开放类型 vs 封闭类型、共享与特化 | ✅ 部分已有（Bridge-D） | 需要从 IL2CPP 视角扩展为规范视角 |

**小计：7 篇（3 篇已有，4 篇待写）**

---

### 模块 B：CoreCLR 实现分析

**定位：** 最主流的 .NET runtime，JIT 路线的标杆实现。开源（dotnet/runtime）。

| 编号 | 主题 | 说明 |
|------|------|------|
| CLR-B1 | CoreCLR 架构总览：从 dotnet run 到 JIT 执行 | 全景地图 |
| CLR-B2 | 程序集加载：AssemblyLoadContext、Fusion、Binder | Assembly identity 的完整实现 |
| CLR-B3 | 类型系统：MethodTable、EEClass、TypeHandle | 与 IL2CPP 的 Il2CppClass 对比 |
| CLR-B4 | JIT 编译器（RyuJIT）：IL → IR → native code | JIT 编译管线 |
| CLR-B5 | GC：分代式精确 GC、Workstation vs Server、Pinned Object Heap | 与 BoehmGC 对比 |
| CLR-B6 | 异常处理：两遍扫描模型、SEH 集成 | 与 IL2CPP 异常处理对比 |
| CLR-B7 | 泛型实现：代码共享（reference types）vs 特化（value types） | 与 IL2CPP generic sharing 对比 |
| CLR-B8 | 线程与同步：Thread、Monitor、ThreadPool | 与 IL2CPP 线程模型对比 |
| CLR-B9 | Reflection 与 Emit：运行时代码生成 | IL2CPP 为什么没有 Emit |
| CLR-B10 | Tiered Compilation：多级 JIT 与 PGO | 性能优化策略 |

**小计：10 篇**

---

### 模块 C：Mono 实现分析

**定位：** Unity 的老 runtime，现已合并到 dotnet/runtime。跨平台先驱。

| 编号 | 主题 | 说明 |
|------|------|------|
| MONO-C1 | Mono 架构总览：从嵌入式 runtime 到 Unity 集成 | 全景地图 |
| MONO-C2 | Mono 解释器（mint/interp）：与 LeanCLR 双解释器的对比 | 解释器实现 |
| MONO-C3 | Mono JIT（Mini）：IL → SSA → native | JIT 编译管线 |
| MONO-C4 | Mono AOT：Full AOT 与 LLVM 后端 | AOT 策略与 IL2CPP 对比 |
| MONO-C5 | SGen GC：精确式分代 GC | 与 CoreCLR GC、BoehmGC 对比 |
| MONO-C6 | Mono 在 Unity 中的角色：为什么 Unity 最终转向 IL2CPP | 历史与技术决策 |

**小计：6 篇**

---

### 模块 D：IL2CPP 独立分析

**定位：** Unity 当前的 AOT runtime。闭源但通过 HybridCLR 间接可观测。

| 编号 | 主题 | 说明 |
|------|------|------|
| IL2CPP-D1 | IL2CPP 架构总览：从 C# → C++ → native 的完整管线 | 全景地图 |
| IL2CPP-D2 | il2cpp.exe 转换器：IL → C++ 代码生成策略 | 转换器内部（已有部分前置） |
| IL2CPP-D3 | libil2cpp runtime：MetadataCache、Class、Runtime 三层 | runtime 内部结构 |
| IL2CPP-D4 | global-metadata.dat：格式、加载、与 runtime 的绑定 | 已有前置文章 |
| IL2CPP-D5 | IL2CPP 泛型代码生成：共享、特化、全泛型共享 | 泛型实现细节 |
| IL2CPP-D6 | IL2CPP GC 集成：BoehmGC 的接入层 | GC 层（已有 Bridge-F 部分内容） |
| IL2CPP-D7 | IL2CPP 的 ECMA-335 覆盖度：哪些支持、哪些不支持 | 规范合规性分析 |
| IL2CPP-D8 | IL2CPP 与 managed code stripping：裁剪策略与 link.xml | 裁剪机制 |

**小计：8 篇**

---

### 模块 E：HybridCLR 系列（已完成）

**定位：** IL2CPP 的热更补丁方案。

| 状态 | 篇数 |
|------|------|
| 主线 HCLR-0~24 | 25 篇 ✅ |
| 商业功能 HCLR-25~30 | 6 篇 ✅ |
| 前置篇 Pre-A/B | 2 篇 ✅（将归入 ECMA-335 模块共享） |
| 桥接篇 Bridge-C/D/E/F | 4 篇 ✅（部分将归入 ECMA-335 或 IL2CPP 模块共享） |
| **合计** | **37 篇 ✅** |

---

### 模块 F：LeanCLR 实现分析

**定位：** 轻量级独立 CLR，零依赖嵌入式方案。开源 MIT。

| 编号 | 主题 | 说明 |
|------|------|------|
| LEAN-F1 | LeanCLR 调研报告：架构总览与源码地图 | 全景地图（73K LOC） |
| LEAN-F2 | Metadata 解析：CliImage、RtModuleDef 与 ECMA-335 表 | 与 IL2CPP RawImageBase 对比 |
| LEAN-F3 | 双解释器架构：HL-IL(182) → LL-IL(298) 的三级 transform | 核心创新点 |
| LEAN-F4 | 对象模型：RtObject、RtClass、VTable、单指针头 | 与 IL2CPP Il2CppObject 对比 |
| LEAN-F5 | 类型系统：泛型膨胀、接口分派、值类型 boxing | 与 CoreCLR MethodTable 对比 |
| LEAN-F6 | 方法调用链：从 Assembly.Load 到 Interpreter::execute | 与 HybridCLR 调用链对比 |
| LEAN-F7 | 内存管理：MemPool arena + GC 接口设计 | GC stub 的架构意图 |
| LEAN-F8 | Internal Calls 与 Intrinsics：61 个 icall 实现分析 | BCL 适配策略 |
| LEAN-F9 | WebAssembly 构建与 H5 小游戏嵌入 | 工程落地 |
| LEAN-F10 | LeanCLR vs HybridCLR：同一团队的两条技术路线 | 架构对比 |

**小计：10 篇**

---

### 模块 G：横切对比篇

**定位：** 同一个 ECMA-335 概念，在 5 个 runtime 里的不同实现决策。架构对比线的核心内容。

| 编号 | 主题 | 对比维度 |
|------|------|----------|
| CROSS-G1 | Metadata 解析：5 个 runtime 怎么读 .NET DLL | 解析策略、缓存策略、惰性加载 |
| CROSS-G2 | 类型系统实现：MethodTable vs Il2CppClass vs RtClass | 内存布局、VTable 设计、泛型膨胀 |
| CROSS-G3 | 方法执行：JIT vs AOT vs Interpreter vs 混合 | 编译/解释策略的 trade-off |
| CROSS-G4 | GC 实现：分代精确 vs 保守式 vs 协作式 vs stub | 4 种 GC 策略的工程对比 |
| CROSS-G5 | 泛型实现：共享 vs 特化 vs 全泛型共享 | 代码膨胀 vs 运行时性能 |
| CROSS-G6 | 异常处理：两遍扫描 vs setjmp/longjmp vs 解释器展开 | 异常模型对比 |
| CROSS-G7 | 程序集加载与热更新：静态绑定 vs 动态加载 vs 卸载 | 热更新能力对比 |
| CROSS-G8 | 体积与嵌入性：从 50MB CoreCLR 到 300KB LeanCLR | 裁剪策略与嵌入成本 |

**小计：8 篇**

---

## 三、总规模

| 模块 | 篇数 | 状态 |
|------|------|------|
| A. ECMA-335 基础层 | 7 | 3 已有 + 4 待写 |
| B. CoreCLR | 10 | 待写 |
| C. Mono | 6 | 待写 |
| D. IL2CPP | 8 | 待写（部分可复用已有前置） |
| E. HybridCLR | 37 | ✅ 已完成 |
| F. LeanCLR | 10 | 待写 |
| G. 横切对比 | 8 | 待写 |
| **总计** | **~86** | **37 已有 + ~49 待写** |

---

## 四、分阶段推进计划

### Phase 1：地基 + LeanCLR（当前优先）

| 优先级 | 内容 | 篇数 | 理由 |
|--------|------|------|------|
| P0 | ECMA-335 基础层补齐（ECMA-A3~A6） | 4 | 所有模块的公共地基 |
| P0 | LeanCLR 核心分析（LEAN-F1~F6） | 6 | 源码可读、已 clone 编译跑通 |
| P1 | 首批横切对比（CROSS-G1~G3） | 3 | 把已有知识串起来 |
| **Phase 1 小计** | | **13 篇** | |

### Phase 2：IL2CPP 独立化 + LeanCLR 收尾

| 优先级 | 内容 | 篇数 | 理由 |
|--------|------|------|------|
| P1 | IL2CPP 独立分析（IL2CPP-D1~D5） | 5 | 从 HybridCLR 系列提取并独立化 |
| P1 | LeanCLR 收尾（LEAN-F7~F10） | 4 | 完成 LeanCLR 系列 |
| P2 | 横切对比续（CROSS-G4~G5） | 2 | 继续串联 |
| **Phase 2 小计** | | **11 篇** | |

### Phase 3：CoreCLR

| 优先级 | 内容 | 篇数 | 理由 |
|--------|------|------|------|
| P2 | CoreCLR 系列（CLR-B1~B10） | 10 | 最主流 runtime，源码最复杂 |
| P2 | 横切对比续（CROSS-G6~G7） | 2 | |
| **Phase 3 小计** | | **12 篇** | |

### Phase 4：Mono + 收尾

| 优先级 | 内容 | 篇数 | 理由 |
|--------|------|------|------|
| P3 | Mono 系列（MONO-C1~C6） | 6 | 历史意义 + Unity 对比 |
| P3 | IL2CPP 收尾（IL2CPP-D6~D8） | 3 | |
| P3 | 横切对比收尾（CROSS-G8） | 1 | |
| **Phase 4 小计** | | **10 篇** | |

---

## 五、索引结构设计

### 总入口页

```
.NET Runtime 生态全景系列
│
├─ ECMA-335 基础层（7 篇）— 所有线的公共入口
│
├─ 阅读线 1：Unity 工程师线
│  ├─ IL2CPP 实现分析（8 篇）
│  └─ HybridCLR 系列（37 篇）
│
├─ 阅读线 2：Runtime 工程师线
│  ├─ CoreCLR 实现分析（10 篇）
│  ├─ Mono 实现分析（6 篇）
│  └─ LeanCLR 实现分析（10 篇）
│
├─ 阅读线 3：H5/小游戏线
│  └─ LeanCLR 接入与工程化（LEAN-F9 + 相关）
│
└─ 阅读线 4：架构对比线
   └─ 横切对比篇（8 篇）
```

### 每个模块独立索引页

每个模块（A~G）有自己的索引页，格式沿用 HybridCLR 系列索引的结构：推荐阅读顺序 + 按问题查。

---

## 六、命名规范

| 模块 | 文件前缀 | 示例 |
|------|---------|------|
| ECMA-335 | `ecma335-` | `ecma335-type-system-value-ref-generic.md` |
| CoreCLR | `coreclr-` | `coreclr-architecture-overview-dotnet-run-to-jit.md` |
| Mono | `mono-` | `mono-interpreter-mint-vs-leanclr.md` |
| IL2CPP | `il2cpp-` | `il2cpp-architecture-csharp-to-cpp-to-native.md` |
| HybridCLR | `hybridclr-` | （已有，不变） |
| LeanCLR | `leanclr-` | `leanclr-survey-architecture-source-map.md` |
| 横切对比 | `runtime-cross-` | `runtime-cross-metadata-parsing-five-runtimes.md` |

---

## 七、对求职展示的价值

| 维度 | 展示的能力 |
|------|-----------|
| 技术深度 | 能读懂并对比 5 个 CLR 实现的源码 |
| 系统视野 | 从 ECMA-335 规范到多个实现的完整知识体系 |
| 架构判断力 | 同一个问题的 5 种解法和 trade-off |
| 技术前瞻性 | LeanCLR（新兴方案）、CoreCLR（.NET 主线）、Unity 技术栈演进 |
| 写作与表达 | 86 篇系统性技术专栏的规模和质量 |

这个知识体系如果完整落地，在中文技术社区是唯一的。

---

## 八、风险与约束

| 风险 | 缓解 |
|------|------|
| 规模太大，做不完 | 分 4 个 Phase，每个 Phase 独立可交付 |
| CoreCLR 源码太复杂 | 只选核心模块（JIT/GC/TypeSystem），不做全覆盖 |
| IL2CPP 闭源 | 通过 HybridCLR 间接观测 + 公开文档 + 逆向推导 |
| Mono 资料老旧 | 聚焦与 Unity 相关的部分，不做完整考古 |
| LeanCLR 还在开发中 | 声明版本基线，后续跟进更新 |
