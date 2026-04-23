---
title: "脚本热更新 04｜DHE 进阶——函数级差分热更的原理与工程约束"
slug: "delivery-hot-update-04-dhe"
date: "2026-04-14"
description: "DHE（Differential Hybrid Execution）只把变更的函数改为解释执行，未变更的继续走 AOT。性能损失最小，但工程约束也最严格。"
tags:
  - "Delivery Engineering"
  - "Hot Update"
  - "HybridCLR"
  - "DHE"
series: "脚本热更新"
primary_series: "delivery-hot-update"
series_role: "article"
series_order: 40
weight: 740
delivery_layer: "practice"
delivery_volume: "V08"
delivery_reading_lines:
  - "L2"
---

## 这篇解决什么问题

标准 HybridCLR 模式下，整个热更 Assembly 的所有方法都通过解释器执行。DHE 模式更精细——只把实际变更的方法切换到解释器，未变更的方法继续使用 AOT 代码。

这意味着热更后的性能损失可以控制在最小范围内。但 DHE 的工程约束比标准模式严格得多。

## DHE 的工作原理

### 标准 HybridCLR vs DHE

```
标准 HybridCLR：
  首包 AOT Assembly：ClassA[AOT], ClassB[AOT]
  热更后：ClassA[解释器], ClassB[解释器]  ← 整个 Assembly 切换到解释器

DHE：
  首包 AOT Assembly：ClassA.Method1[AOT], ClassA.Method2[AOT], ClassB.Method1[AOT]
  热更后：ClassA.Method1[AOT], ClassA.Method2[解释器], ClassB.Method1[AOT]
                                ↑ 只有变更的方法切换到解释器
```

### 差分计算

DHE 在构建时比较首包 Assembly 和热更 Assembly 的 IL，找出变更的方法列表：

```
diff(首包.dll, 热更.dll) → 变更方法列表
  ClassA.Method2: 已修改
  ClassC.Method3: 新增
  其余: 未变更
```

运行时加载热更包时，只替换变更方法的执行入口——从 AOT 函数指针重定向到解释器。

### 性能对比

| 场景 | 标准 HybridCLR | DHE |
|------|---------------|-----|
| 热更后调用未变更方法 | 解释执行（慢） | AOT 执行（原生速度） |
| 热更后调用变更方法 | 解释执行 | 解释执行 |
| 整体性能影响 | 所有热更代码变慢 | 只有变更的方法变慢 |

如果一个 Assembly 有 1000 个方法，但热更只修改了 5 个方法，DHE 的性能损失只有标准模式的 0.5%。

## DHE 的工程约束

DHE 的精细度也带来了更严格的工程要求：

### 约束一：首包和热更的 Assembly 结构必须兼容

DHE 的差分计算依赖于 Assembly 的结构匹配。如果热更 Assembly 的类型布局、方法签名或继承关系发生了结构性变化（不只是方法体的修改），差分可能失败。

**安全的变更**：
- 修改方法体（函数内部逻辑）
- 新增私有方法
- 新增私有字段（需要谨慎）

**风险的变更**：
- 修改公共方法签名
- 新增公共类型
- 修改继承关系
- 修改接口实现

### 约束二：构建环境必须严格一致

DHE 的差分计算对 IL 字节级敏感。首包和热更必须使用完全相同的：
- Unity 版本（小版本号也必须一致）
- .NET 编译器版本
- 编译选项（优化级别、Debug 符号）
- HybridCLR 版本

任何不一致都可能导致差分计算的结果错误——把未变更的方法误判为变更，或把变更的方法漏掉。

### 约束三：热更包必须包含差分元数据

DHE 的热更包不只是热更 DLL，还包含：
- 变更方法列表
- 方法体 IL 字节码
- 新增类型的元数据
- 对应的 AOT Assembly 版本标识（用于校验一致性）

### 约束四：商业授权

DHE 是 HybridCLR 的商业特性，不包含在开源版本中。使用 DHE 需要获取商业授权。

## DHE 的适用场景

| 场景 | 是否适合 DHE | 理由 |
|------|------------|------|
| Bug 修复（改少量方法体） | 非常适合 | 性能损失最小 |
| 小功能调整（改几个类的逻辑） | 适合 | 变更范围可控 |
| 新增大量功能（新模块） | 不太适合 | 新增的方法全部走解释器，和标准模式无异 |
| 大规模重构 | 不适合 | 结构变化大，差分可能失败 |

**DHE 最佳实践**：把 DHE 定位为"热修复"工具而非"热更新"工具。用 DHE 修复线上 Bug（改动小、方法级），用标准 HybridCLR 模式做功能热更新（改动大、Assembly 级）。

## 与交付链路的关系

DHE 在交付链路中的定位：

```
紧急修复场景：
  线上 Bug → 定位到具体方法 → DHE 修复该方法 → 分钟级生效

常规更新场景：
  新功能开发 → 标准 HybridCLR 热更 → 或版本更新
```

DHE 是交付链路中"应急通道"的关键能力——它让紧急修复可以做到函数级精准、性能损失最小。

## 小结与检查清单

- [ ] 是否明确了 DHE 和标准 HybridCLR 的使用场景边界
- [ ] 构建环境是否严格一致（Unity 版本、编译器版本、HybridCLR 版本）
- [ ] 热更包是否包含差分元数据和版本校验信息
- [ ] 是否有商业授权（DHE 不在开源版中）
- [ ] 是否有结构性变更的检测机制（变更超出安全范围时告警）

---

**下一步应读**：[热更新验证]({{< relref "delivery-engineering/delivery-hot-update-05-verification.md" >}}) — 热更发布前要验什么

**扩展阅读**：

- [HybridCLR DHE 内部机制｜dhao 格式、差分算法与函数级分流]({{< relref "engine-toolchain/hybridclr-dhe-internal-dhao-format-diff-algorithm-function-routing.md" >}}) — dhao 文件结构、IL 级差分算法、运行时 methodPointer 切换
- [HybridCLR DHE 函数注入与脏函数传染]({{< relref "engine-toolchain/hybridclr-dhe-function-injection-dirty-contagion.md" >}}) — 为什么改一个函数会导致一片函数走解释器
- [HybridCLR 高级能力选型｜社区版、补 metadata、FGS、DHE 分别在什么时候上]({{< relref "engine-toolchain/hybridclr-advanced-capability-selection-community-metadata-fgs-dhe.md" >}}) — 把四条能力线收回项目选型判断
