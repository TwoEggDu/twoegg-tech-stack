---
date: "2026-03-26"
title: "IL2CPP 运行时地图｜global-metadata.dat、GameAssembly、libil2cpp 到底各管什么"
description: "把 IL2CPP 先还原成三样具体产物：global-metadata.dat、GameAssembly 和 libil2cpp。解释 metadata 文件到底存了什么、不存什么，runtime 怎么靠 metadata 和 native method pointer 把一个托管方法接成可运行代码，以及这套结构为什么会直接影响 stripping、AOT 泛型和 HybridCLR。"
slug: "il2cpp-runtime-map-global-metadata-gameassembly-libil2cpp"
weight: 42
featured: false
tags:
  - Unity
  - IL2CPP
  - Runtime
  - Architecture
---

> 如果只用一句话概括 IL2CPP 最终交付给包体的东西，我会这样说：它不是一份“能直接运行的 C# 产物”，而是三层互相配合的结果：`global-metadata.dat` 负责描述托管世界，`GameAssembly` 负责承载 AOT 后的 native 代码，`libil2cpp` 负责把前两者拼成一个真正可运行的 runtime。

很多人第一次接触 `IL2CPP` 时，脑子里留下的印象往往只有一句话：

`C# 会被转成 C++，最后再编译成原生代码。`

这句话当然没错，但它最大的问题，是太容易把读者带到“代码转换流程”里，却没先回答一个更接近真实工程的问题：

`Unity 用 IL2CPP 打包之后，最终到底交付了什么？`

因为只要你真的去看包体、看崩溃栈、看 stripping、看 HybridCLR，最后总会反复遇到三个名字：

- `global-metadata.dat`
- `GameAssembly.dll`
- `libil2cpp`

如果这三个东西在脑子里还是一团模糊，那后面很多问题都会越看越乱：

- `global-metadata.dat` 到底是不是代码文件
- 明明“metadata 还在”，为什么运行时还是可能报没有实现
- 反射、裁剪、AOT 泛型、HybridCLR 为什么总会绕回 metadata

所以这一篇不先讲优化，也不先讲接入，而是先把 IL2CPP 的运行时地图立起来。

## 这篇要回答什么

这篇文章想回答 6 个问题：

1. Unity 用 IL2CPP 打包后，最终到底交付了什么。
2. `global-metadata.dat` 到底是什么，里面放了什么，不放什么。
3. 为什么有了 metadata 文件还不够，为什么还需要 `GameAssembly`。
4. `libil2cpp` 在中间到底负责什么。
5. 一个托管方法是怎么从 metadata 被接到 native method pointer 上的。
6. 这套结构为什么会直接影响 stripping、反射、AOT 泛型和 HybridCLR。

如果先给一个直觉版答案，那就是：

`IL2CPP 最终交付的不是一个黑盒文件，而是一套分工明确的结构：metadata 负责描述托管世界，native code 负责执行逻辑，runtime 负责把两者接起来。`

## 先给一张最小地图

如果先不管平台差异，只看概念层，IL2CPP 最值得先盯住的通常是三样东西：

| 产物 | 你最该把它当成什么 | 它主要回答 | 它不负责 |
| --- | --- | --- | --- |
| `global-metadata.dat` | 托管世界说明书 | 这个世界里有哪些程序集、类型、方法、字段、签名和关系 | 直接承载可执行的 native 机器码 |
| `GameAssembly` | AOT 执行载体 | 这些托管方法最终怎么以原生代码形式执行 | 独立完成类型系统和 metadata 解释 |
| `libil2cpp` | 运行时拼接层 | 怎么把 metadata 和 AOT 代码接成真正可运行的 runtime | 替代前两者本身 |

这里有一个边界要先说明。

`GameAssembly.dll` 这个名字更像 Windows 视角下最常见的例子。不同平台上的文件名和链接形式会变，但职责不会因为文件名变化而变：

- 一层负责描述托管世界
- 一层负责承载 AOT 代码
- 一层负责把两者接起来

只要这个分工坐标系先立住，后面再看平台目录差异，就不容易被外观带偏。

## 一、为什么 IL2CPP 最终会拆成这三样

如果先不掉进术语，只抓住 IL2CPP 的本质，那它做的其实是两件事：

1. 把原本托管世界里的可执行逻辑，提前 AOT 成 native 代码。
2. 把原本运行时还需要继续查询、反射、解释的那部分结构信息，保留下来给 runtime 使用。

这两个目标天然会把最终产物拆成两层：

- 一层偏“描述”
- 一层偏“执行”

而只要描述层和执行层一分开，就一定还需要第三层去负责组装。

所以 IL2CPP 最终不是在交付一份“更快的 DLL”，而是在交付一套更偏静态、更偏 AOT 的运行时结构。

换句话说，IL2CPP 真正交付的从来不是：

`一份单体黑盒，里面什么都有。`

而是：

`一套把“这是谁”和“它怎么跑”拆开，再由 runtime 接起来的结构。`

## 二、`global-metadata.dat` 到底是什么

先把最容易混的地方说清楚。

`global-metadata.dat` 不是机器码。

它不是你最终会跳进去执行的那段 native implementation，也不是一份可以被 CLR 直接当成程序集装载的普通 DLL。它更像是一组元数据表，负责告诉 runtime：

- 有哪些程序集
- 有哪些类型
- 有哪些方法、字段、属性、参数
- 它们各自叫什么、签名是什么、token 是什么
- 它们之间的继承、引用和泛型定义关系是什么
- 还需要保留哪些字符串、默认值、自定义属性等结构信息

如果把它压成一句更口语的话，那就是：

`global-metadata.dat` 回答的是“这是谁、叫什么、长什么样”，不是“这段代码怎么执行”。`

### 它更像一份“托管世界说明书”

很多人第一次看到这个文件名时，会下意识把它理解成：

- 一个缓存文件
- 一份配置文件
- 一组调试信息

但这些理解都不够准确。

更稳的理解方式是把它当成：

`IL2CPP runtime 认识托管世界时要查的一份只读说明书。`

为什么运行时还需要这份说明书？

因为就算代码已经 AOT 成 native 了，runtime 也还是要继续回答很多“描述层”问题，比如：

- 这个对象属于什么类型
- 这个类型有哪些字段和方法
- 这个方法的签名是什么
- 这段反射查询应该返回哪个成员
- 这条泛型关系在元数据层到底长什么样

也就是说，AOT 并不会消灭“类型系统”和“元数据查询”这层需求。

### 它不等于“完整代码重新打包”

这一点特别值得单独强调。

`global-metadata.dat` 里当然会有方法定义这类信息，但“有方法定义”不等于“有这段方法最终要跳去执行的 native 函数实现”。

同理，`token` 也更接近 metadata 世界里的标识符，不是你可以直接当成机器码地址去理解的东西。

如果把这里的边界再压缩一下，我会这样写：

- metadata 里的“方法”更像定义对象
- AOT world 里的“方法”才会进一步落到可执行实现

而只要不先把这两层分开，后面看任何运行时问题都很容易混。

## 三、为什么有了 metadata 文件还不够

如果 `global-metadata.dat` 已经把类型、方法、字段这些信息都描述出来了，那一个非常自然的问题就是：

`为什么还需要 GameAssembly？`

答案是：因为 metadata 只解决“识别”和“描述”的问题，不解决“执行”问题。

你可以先把它理解成两层：

- 一层回答“方法是谁”
- 一层回答“方法怎么跑”

在 IL2CPP 里，后者主要落在 AOT 编出来的 native code 上，也就是 `GameAssembly` 这类原生二进制承载的部分。

所以如果只从职责上划分，可以先记住这个最小模型：

- `global-metadata.dat`：描述托管世界
- `GameAssembly`：承载真正执行的 AOT 代码

### 一个最小例子：`Player.TakeDamage(int amount)`

假设你项目里有这样一个方法：

```csharp
public class Player
{
    public void TakeDamage(int amount)
    {
        hp -= amount;
    }
}
```

在 IL2CPP 世界里，runtime 至少要知道两件事：

1. `Player.TakeDamage(int)` 这个方法定义是谁。
2. 真正调用发生时，应该跳到哪段 native 代码去执行 `hp -= amount`。

第一件事靠 metadata。

第二件事靠 AOT 代码。

如果只有第一件事，runtime 最多只能说：

`我认识这个方法。`

但它还不能完成真正调用。

如果只有第二件事，runtime 又没法稳定地把“这个实现属于谁”接回类型系统、反射系统和方法系统里。

所以 `GameAssembly` 这层不是可选配件，而是 IL2CPP 执行层的主体。

## 四、`libil2cpp` 在中间到底做什么

到这里，第三个名字就该出现了：`libil2cpp`。

很多时候它会被一句很含糊的话带过去：

`它是 IL2CPP 的运行时。`

这句话没错，但还不够有用。

如果从本文关心的主线看，`libil2cpp` 更值得被理解成：

`把 metadata 和 native code 组装成可运行 runtime 的那一层。`

它至少要负责几件和本文直接相关的事：

- 读取和解释 metadata
- 初始化运行时里的类型、方法、字段对象
- 建立托管概念和原生实现之间的映射
- 在真正调用发生时，把“这个方法是谁”和“它该跳到哪里执行”接起来

如果继续往 runtime 结构里看，你会发现这里最终会落成很多运行时对象，比如类型对象、方法对象、字段对象、反射对象。它们的存在价值，就是把静态产物变成 runtime 真能操作的世界。

继续往更底下一层走，还会看到 code registration、metadata registration 这类静态登记结构。它们提醒我们的不是“名词变多了”，而是：

`IL2CPP runtime 从来不是只读一个 dat 文件就结束，而是在把文件里的描述信息和原生产物里的执行登记一起组织成类型系统和调用系统。`

所以 `libil2cpp` 既不是 `global-metadata.dat` 本身，也不是 `GameAssembly` 本身。

它是中间那层真正把世界拼起来的运行时结构。

## 五、跟一条最小调用链：一个方法是怎么被接起来的

这篇文章最关键的地方，不是知道三个名字，而是知道它们为什么必须一起出现。

如果只用一条最小调用链来理解，我会建议按下面这个顺序去想：

1. 构建阶段先生成 metadata 结果和 AOT 代码结果。
2. runtime 启动时把 metadata 读进来。
3. runtime 再把程序集、类型、方法组织成它自己的运行时对象。
4. 当某个调用真的发生时，runtime 需要把这个方法对象和它对应的 native method pointer 接起来。
5. 最后控制流才会真正跳进 AOT 代码去执行。

如果把它再压缩一下，就是：

`metadata 标识方法 -> runtime 生成方法对象 -> runtime 找到 method pointer -> 调用跳进 AOT code`

### 这里最容易混的三个东西

到这一步，最容易混掉的其实是下面三个概念：

- 方法定义
- 运行时方法对象
- 真正的执行地址

它们看起来都在说“方法”，但在运行时里不是一回事。

更直白一点：

- metadata 里的方法定义，回答“它是谁”
- runtime 里的方法对象，回答“现在该怎么以运行时可操作的形式看待它”
- native method pointer，回答“调用时最终跳哪”

只要你把这三层重新分开，后面很多问题都会自然很多。

### 为什么这条链比“IL2CPP 会生成 C++”更重要

因为“会生成 C++”只是构建时事实。

但真正帮你理解 runtime 问题的，是这条链：

`方法如何从 metadata 进入运行时对象，再落到 native 执行入口。`

只有这条链立住了，你后面看到下面这些说法时才不会混：

- “metadata 还在”
- “反射还能看到”
- “调用时没有实现”
- “这个泛型实例没有被 AOT 出来”

它们都不是一句“生成过 C++”就能解释清楚的。

## 六、为什么“看得见”不等于“调得到”

这可能是整篇里最重要的一层。

很多人第一次遇到 IL2CPP 的运行时问题时，都会下意识把它理解成：

`是不是 metadata 没了？`

但真实项目里，更常见的情况其实是另一种：

`metadata 层还认得出来，但真正落到调用时，具体实现并不在。`

### 普通方法和泛型方法的差别为什么会在这里爆出来

对一个普通、简单、构建期明确可见的方法来说，这种分层不一定那么刺眼。

但一旦你进入下面这些场景，问题就会突然变得尖锐：

- 泛型类型
- 泛型方法
- 反射驱动的调用
- 构建期不够可见的依赖路径

尤其是泛型。

metadata 可能完全知道：

- 有一个泛型方法定义
- 它的类型参数长什么样
- 它跟哪个类型、哪个签名有关

但这还不等于：

`某个具体实例，比如 Foo<int>.Bar<string>，已经有一份可直接调用的 AOT native implementation。`

这就是为什么我一直觉得，理解 IL2CPP 时必须先把两句话拆开：

- 看得见
- 调得到

前者更偏 metadata。

后者更偏 implementation。

而这两个问题不在同一层。

## 七、为什么真实工程问题总会落到这里

只要上面的坐标系立住了，很多平时看起来不相干的问题就会自动连起来。

### 1. 为什么 stripping 会影响反射

反射依赖的恰恰是“运行时还能不能看到并正确解释这套 metadata”。

如果某些信息在构建时被裁掉，或者相关依赖对构建器不可见，结果往往不是“代码本身坏了”，而是 runtime 再也拿不到它需要的那层描述信息。

也就是说，反射问题很多时候不是“它不会反射”，而是：

`你让它在最终 player 里根本没什么可反射。`

### 2. 为什么有些问题不是“metadata 丢了”，而是“实现没了”

这类问题最容易在 AOT 泛型上出现。

你可能还能认出某个泛型方法定义，也能理解签名和实例化关系，但真正落到调用边界时，却发现没有对应的 native implementation。

这时问题就已经不是“认不认识”，而是“调不调得到”。

### 3. 为什么运行时问题最好先判断属于哪一层

只要这套结构一分层，很多排查顺序就会更清楚。

你至少应该先问：

- 这是 metadata 不可见吗
- 还是 metadata 可见，但 implementation 不存在
- 还是 runtime 把两者接起来的那一层出了问题

如果第一步不先分层，后面的排查很容易一路混到底。

## 八、为什么这会让 HybridCLR 更好理解

这篇文章虽然不是写 HybridCLR，但它其实正好能把 HybridCLR 最容易混掉的那层前置误解拆开。

因为 HybridCLR 不是凭空创造一套完全独立的新 runtime，而是在一个原本依赖静态 metadata 和 AOT native code 的世界上，补进动态 metadata 可见性和解释执行能力。

这句话很重要。

如果你前面没有先理解：

- IL2CPP 本来的 metadata 在哪
- metadata 和 native implementation 为什么本来就是两层

那后面再看 `supplementary metadata`，就很容易误会成“又来了一份神秘代码文件”。

### `global-metadata.dat` 和 supplementary metadata 不是一回事

可以先把这两者压成一个非常直观的对照：

- `global-metadata.dat` 更像最终 player 自带的那份基础 metadata 快照
- HybridCLR 的 supplementary metadata 更像 runtime 额外挂进来的、为特定 AOT assembly 补可见性和可解释性的 metadata 视图

它们解决的问题不一样。

前者是 IL2CPP player 本来的基础描述层。

后者是 HybridCLR 在某些路径下，为了让 runtime 重新看得见更多 metadata、重新拿到解释和解析所需信息而补进来的能力。

所以如果把它们混成一回事，后面最容易发生两个误判：

- 误判一：以为 supplementary metadata 就是在“执行热更 DLL”
- 误判二：以为补了 supplementary metadata，就自动补出了原本不存在的 native generic implementation

这两个判断都不对。

更准确的说法应该是：

`HybridCLR 补的是 runtime 的 metadata 可见性和解释能力，不是自动替 IL2CPP 生成所有缺失的 native 实现。`

## 九、常见误解

### 误解一：`global-metadata.dat` 就是代码文件

不对。

它更像“托管世界说明书”，不是最终执行的机器码。

### 误解二：有 metadata，就一定能把方法调起来

也不对。

metadata 解决的是“认得出来”，不自动等于“有可执行实现”。

### 误解三：`GameAssembly` 只是把 DLL 换了个壳

不准确。

它承载的是 AOT 后的 native code，不是把托管 DLL 原样封进去。

### 误解四：`libil2cpp` 就等于 `GameAssembly`

也不对。

一个更偏执行载体，一个更偏运行时拼接层，它们在职责上不是同一个东西。

### 误解五：HybridCLR 补 metadata，就等于补出了所有 native implementation

还是不对。

它补的是 runtime 对 metadata 的可见性和解释能力，不是自动替 AOT 世界生成原本不存在的 native 实现。

## 先记住一个最简单的判断框架

如果你后面只想带走一个最小排查入口，我建议先记住这一版：

- `认不认识它`，先想 metadata 层
- `调不调得到它`，先想 implementation 层
- `两边明明都像在，但运行时还是接不起来`，再看 runtime 组装层

这套判断不能替代细节，但它能先把你从“所有问题都叫 IL2CPP 黑盒”这种状态里拉出来。

## 最后压成一句话

如果只允许我用一句话收这篇文章，我会这样写：

`IL2CPP 真正交付的不是一份单体黑盒，而是一套“metadata 描述世界、AOT 代码执行世界、runtime 负责把两者接起来”的结构；而只要这个基本坐标系立住了，后面你再看 stripping、反射、AOT 泛型和 HybridCLR，很多问题都会先自动降一个复杂度。`

## 相关阅读

- [HybridCLR 原理拆解｜从 RuntimeApi 到 Interpreter::Execute]({{< relref "engine-toolchain/hybridclr-principle-from-runtimeapi-to-interpreter-execute.md" >}})
- [HybridCLR AOT 泛型与补充元数据｜为什么代码能编译，到了 IL2CPP 运行时却不一定能跑]({{< relref "engine-toolchain/hybridclr-aot-generics-and-supplementary-metadata.md" >}})
- [Unity 裁剪 01｜Unity 的裁剪到底分几层]({{< relref "engine-toolchain/unity-stripping-01-what-gets-stripped.md" >}})
