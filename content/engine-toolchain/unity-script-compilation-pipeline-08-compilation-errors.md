---
date: "2026-03-28"
lastmod: "2026-10-31"
title: "Unity 脚本编译管线 08｜编译报错排查：从错误信息定位根因"
description: "沿编译阶段与程序集依赖排查 C#、引用、循环依赖和 ILPP 错误，并区分编译失败与运行时类型加载问题。"
slug: "unity-script-compilation-pipeline-08-compilation-errors"
weight: 69
featured: false
tags:
  - "Unity"
  - "Compilation"
  - "Debugging"
  - "Error"
  - "Assembly"
series: "Unity 脚本编译管线"
series_order: 8
---

> 一个上游失败可能引出多条下游错误。先保留完整日志，沿相关任务和程序集找先发生的诊断；并行日志里最上面一条不一定是根因。

> 版本范围：程序集规则与裁剪机制参照 Unity 2022.3 公开手册，Burst 参照 1.8 文档。实际诊断还需核对项目版本、后端与目标平台；本文提供排查方法，没有把下列方向逐项运行验证。

## 这篇要回答什么

有明确错误诊断和长时间没有进展，需要不同的排查入口。编译失败可能由工具非零退出、阶段失败或异常终止报告；不能用“进程正常退出”定义报错。本文先处理能够取得失败诊断的情况。

下面按四类编译相关问题展开，并单独列出容易混淆的运行时类型加载问题：

1. **C# 语法 / 语义错误**（`CS` 开头的错误码）
2. **找不到程序集**（`.asmdef` 引用关系断裂）
3. **循环依赖**（两个程序集互相引用）
4. **运行时类型缺失**（编译通过，运行时崩溃）
5. **ILPP 处理失败**（后处理器出了问题）

这些是排查入口，不是穷尽所有故障的分类。团队收到 Jenkins 提醒后的处理与共享版本复检，见[客户端编译检查]({{< relref "engineering-journey/production-client-compile.md" >}})。

---

## 第一类：C# 语法 / 语义错误（CS xxxx）

### 识别特征

错误码以 `CS` 开头，例如：

- `error CS0246: The type or namespace name 'Foo' could not be found`
- `error CS1061: 'Bar' does not contain a definition for 'Baz'`
- `error CS0117: 'MyClass' does not contain a definition for 'Method'`

### 常见原因

先定位文件和行号，再核对定义、调用与引用。拼写或类型用法可能有误，缺少源文件、生成代码或程序集引用也会导致错误。以 CS0246 为例，补 `using` 只适用于类型存在且引用关系正确、命名空间未引入的情形；见 [Microsoft 程序集引用诊断](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/assembly-references)。

例如，基类未能编译可能引出使用方的错误。应沿依赖确认这一关系，再修上游；错误展示顺序本身不能证明因果。

### 处理

1. 确认本次输入版本、目标平台和相关编译日志。
2. 定位报错文件，沿类型来源与程序集依赖查看上游诊断。
3. 修正已确认的原因，再执行相同范围的编译。
4. 检查原错误及剩余诊断，不把消除一条报错当作整个工程恢复。

---

## 第二类：找不到程序集（Assembly not found）

### 识别特征

- `Assembly 'XXX' not found`
- `The type or namespace 'XXX' could not be found`（但代码里的拼写是对的）
- 某个 Package 里的类型在 IDE 里有提示，但编译时就是找不到

### 常见原因

**原因一：`.asmdef` 的 References 列表没有加对应依赖**

对于 `.asmdef` 定义的程序集，使用另一个自定义程序集的类型，需要相应的引用。预定义程序集与预编译插件还有自动引用等规则，不能将所有关系都概括成手动填 References。

**原因二：被引用的 Package 没有安装**

有时候代码使用了未安装的包，或者本地与检查机恢复了不同版本。还要核对相关程序集是否因平台选择、Define Constraints 等条件未参与本次编译。

**原因三：代码文件放错了目录**

程序集归属与目录层级有关，但子目录的 `.asmdef` 或 `.asmref` 会改变归属。脚本移出一个范围后，也不一定归入 `Assembly-CSharp`，还需查看其他定义与预定义程序集规则。上述引用、归属和条件编译规则见 [Unity 2022.3 Assembly definitions](https://docs.unity3d.com/2022.3/Documentation/Manual/ScriptCompilationAssemblyDefinitionFiles.html)。

### 处理

1. 在 Project 窗口找到对应的 `.asmdef` 文件，点击查看 Inspector
2. 检查 Assembly Definition References 列表，把缺失的依赖加进去
3. 如果是 Package 问题，去 Package Manager 确认包是否已安装
4. 检查报错的脚本文件路径，确认它在正确的目录树下

---

## 第三类：循环依赖（Circular reference）

### 识别特征

错误信息里出现 `Cyclic assembly reference detected`，通常还会列出形成环的程序集名称。

### 常见原因

A 的 `.asmdef` 引用了 B，B 又引用了 A，或者经过更多程序集形成环。这样的程序集依赖不受支持，需要修正引用关系；不能据此推断其他独立编译任务是否执行。

循环依赖往往出现在项目重构不彻底的情况下：本来 A 和 B 是独立的，后来 B 里加了一个类需要用到 A 里的接口，就顺手加了依赖，但没有意识到 A 早就依赖了 B。

### 处理

先判断是否存在不必要的反向引用。若双方确实需要共享契约，一种做法是**提取第三个程序集**：

1. 新建一个 `Common`（或 `Shared`）程序集
2. 把 A 和 B 共用的类型、接口移到 `Common` 里
3. A 和 B 的 `.asmdef` 都改为引用 `Common`，同时去掉互相之间的引用

这样依赖关系变成了 `A → Common ← B`，环消失了。

---

## 第四类：运行时类型缺失（编译通过，运行时崩溃）

### 识别特征

编译没有报错，但运行时（包括进入 Play 模式或在设备上运行）抛出：

- `TypeLoadException: Could not load type 'XXX'`
- `MissingMethodException: Method 'XXX' not found`
- `EntryPointNotFoundException`

### 常见原因

这类错误发生在运行阶段，可能涉及实际加载版本、裁剪、插件平台或原生符号等因素，不能直接归因于编译链。

**一个需要验证的方向：Managed Stripping**

托管代码裁剪按可达性分析等规则决定保留范围。动态引用可能需要额外保留声明，但不能认定所有反射目标都会被删除。先核对实际加载的程序集、构建设置和保留规则，再判断目标是否缺失，见 [Unity 2022.3 Managed code stripping](https://docs.unity3d.com/2022.3/Documentation/Manual/ManagedCodeStripping.html)。

**HybridCLR 场景下的额外情况**

热更代码调用的 AOT 方法如果未保留，可能导致运行失败。补充元数据也不能替代对所需代码的保留；具体要求还要按项目锁定的 HybridCLR 版本核对。

### 处理

1. 核对包、更新资源、程序集及平台插件的实际版本，先排除加载错配；原生入口错误还要检查插件导出符号。
2. 有裁剪线索时，在相同输入与运行路径下做保留规则或裁剪级别的对照。可选级别依赖 Unity 版本与脚本后端。
3. 降低级别后异常消失只是缩小范围的证据；进一步核对缺失目标，并用精确保留规则复验。不要直接给所有程序集加全量保留。

---

## 第五类：ILPP 处理失败

### 识别特征

这类错误的报错位置比较隐蔽，不一定直接出现在 Console 里，需要去看详细的编译日志：

- 总构建工具报告失败后，任务日志进一步指向某个后处理器
- Console 里出现 `error: ... BurstILPostProcessor` 或 `JobsILPostProcessor` 字样
- 编译日志里有 `ILPostProcessor` 相关异常堆栈

ILPP（IL Post Processing）处理编译生成的程序集。应根据日志确认究竟哪个处理器失败；单独看到 `bee_backend` 非零退出，无法判断失败属于 C# 编译、后处理还是其他构建步骤。

### 常见原因

处理器加载、依赖解析、版本兼容，以及被处理代码不满足要求，都可能导致失败。若诊断明确指向 Burst 不支持的用法，再按项目所用包版本检查语言限制，参考 [Burst 1.8 C# 支持范围](https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/csharp-language-support.html)。普通 C# 接口实现错误可能在更早阶段就报出，不能全部归入 ILPP。

### 处理

1. 找到失败任务、处理器名与完整异常，判断失败发生在加载、解析还是处理代码时。
2. 核对依赖及版本；只有诊断指向具体代码时，才检查对应方法用法。
3. 可在隔离的实验改动中缩小输入或关闭相关处理路径，记录结果；通过只说明与该路径有关，不能单独证明是语法问题。
4. 完成修正后恢复原有编译配置，再验证编译与受影响功能。

---

## 快速排查 Checklist

遇到编译报错时，按这个顺序过一遍：

1. **先对齐版本与目标**，再沿相关任务的日志定位失败阶段。
2. **CS 诊断** → 查看文件、定义与引用，追查可能的上游错误。
3. **类型或程序集找不到** → 核对文件、生成代码、包版本、程序集引用及编译条件。
4. **依赖成环** → 删除不必要的反向引用，或调整契约与程序集边界。
5. **运行时加载失败** → 核对实际加载内容，再验证裁剪或平台插件等假设。
6. **构建工具失败** → 找到具体失败任务；只有指向 ILPP 时才进入后处理排查。

---

## 小结

把诊断放回对应阶段，才能确定下一步查什么：

| 类型 | 识别特征 | 根因方向 |
|------|----------|----------|
| C# 语法 / 语义 | `CS` 开头错误码 | 源码、生成物、引用与编译条件 |
| 找不到程序集 | "not found" 类错误 | 缺文件、引用、包版本或目标条件 |
| 循环依赖 | "Cyclic" 字样 | 依赖关系有环，需要重构 |
| 运行时类型缺失 | TypeLoadException 等 | 实际加载版本、裁剪或插件兼容等 |
| ILPP 失败 | 后处理器诊断与异常 | 处理器、依赖、版本及输入代码 |

如果仅靠日志无法定位，可在独立工作副本中对比已知可编译版本与失败版本，逐步缩小变更范围，同时保持依赖与环境条件可比。不要在共享分支盲目回滚多人的改动。最终仍以修正后的共享输入完成检查、受影响路径通过验证为准。

---

- 上一篇：[Unity 脚本编译管线 07｜CI 编译缓存：Library 哪些能缓存、哪些不能]({{< relref "engine-toolchain/unity-script-compilation-pipeline-07-ci-cache.md" >}})
- 下一篇：[Unity 脚本编译管线 09｜编译机器人实践：从触发到通知的全链路]({{< relref "engine-toolchain/unity-script-compilation-pipeline-09-build-robot.md" >}})
