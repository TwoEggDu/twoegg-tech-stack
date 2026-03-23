# IL2CPP 文章规划

## 定位

这篇文章不是逆向入门，也不是 IL2CPP 全景教程。

它的目标是：

`把读者最终能看到的 IL2CPP 产物先还原成一张运行时地图：global-metadata.dat、GameAssembly 和 libil2cpp 分别负责什么，它们怎么一起把托管世界接成可运行的 AOT runtime。`

这篇写完之后，读者至少应该得到四样东西：

- 知道 `global-metadata.dat` 到底是什么文件，里面有什么，不有什么
- 知道 `GameAssembly` 和 `libil2cpp` 分别负责什么，不再把它们混成一个黑盒
- 能用一条具体的方法解析链，理解 metadata、runtime object、method pointer 之间的关系
- 能把这套结构和 stripping、反射、AOT 泛型、HybridCLR 的问题空间连起来

## 为什么值得单独写

当前 HybridCLR 系列里，IL2CPP 只被当作一段“够用背景”。

这本身没有问题，但它天然会留下一个缺口：

`读者会很快听到 metadata、AOT、MethodBody、method pointer、supplementary metadata 这些词，却还没有先把 IL2CPP 最终产物层的坐标系立住。`

于是很多问题会被问成：

- `global-metadata.dat` 到底是干啥的
- 有 metadata 文件了，为什么还会说“没有实现”
- 为什么 metadata 和 native code 要分开
- HybridCLR 说的“补 metadata”到底是在补哪一层

所以这篇文章要承担的任务，不是替代 HybridCLR 系列，而是给它补一层更稳的公共底座。

## 风格约束

按站点现有 `engine-notes` 的写法，这篇要保持下面这些特征：

- 开头先用一句话压缩全文判断
- 先给最终产物地图，再进源码或运行时概念
- 以问题驱动，不按目录或平台文件平铺
- 每节都回答“这一层到底解决了什么问题”
- 不炫术语，不把文章写成术语表
- 结尾要把整条链压成一句话，并补“常见误解”

## 标题方向

首选标题：

- `IL2CPP 运行时地图｜global-metadata.dat、GameAssembly、libil2cpp 到底各管什么`

备选标题：

- `IL2CPP 到底交付了什么｜为什么一个 metadata 文件还不够`
- `从 global-metadata.dat 到 method pointer｜IL2CPP runtime 是怎么把托管代码接起来的`

更推荐第一种，因为它最直接回答读者已经在问的问题。

## 建议描述文案

这篇文章的 `description` 应该直接把三个产物点出来，不能写泛。

建议版本：

`把 IL2CPP 先还原成三样具体产物：global-metadata.dat、GameAssembly 和 libil2cpp。解释 metadata 文件到底存了什么、不存什么，runtime 怎么靠 metadata 和 native method pointer 把一个托管方法接成可运行代码，以及这套结构为什么会直接影响 stripping、AOT 泛型和 HybridCLR。`

## 目标字数

建议控制在：

- 最低可用线：`3500` 字
- 标准完成线：`4500-6000` 字
- 尽量不要超过：`7000` 字

这个题目如果低于三千字，很容易变成“概念都提了，但没有一条线真的走通”。

## 核心问题

全文围绕下面 6 个问题展开：

1. Unity 用 IL2CPP 打包后，最终到底交付了什么
2. `global-metadata.dat` 到底存了什么，它为什么不是“代码文件”
3. 为什么有了 metadata 还不够，为什么还需要 `GameAssembly`
4. `libil2cpp` 在中间到底做了什么
5. 一个托管方法是怎么从 metadata 被接到 native method pointer 上的
6. 这套结构为什么会直接影响 stripping、反射、AOT 泛型和 HybridCLR

## 核心判断

如果只允许用一句话概括全文，我建议写成：

`IL2CPP 交付给最终包体的，不是一份“更快的 C# DLL”，而是三样互相配合的东西：global-metadata.dat 负责描述托管世界，GameAssembly 负责执行 AOT 代码，libil2cpp 负责把前两者拼成一个真正可运行的 runtime。`

## 推荐结构

### 1. 先看最终产物：IL2CPP 到底交付了什么

这一节只做一件事：把读者最先会看到的三个东西摆出来。

- `global-metadata.dat`
- `GameAssembly.dll` 或平台对应原生二进制
- `libil2cpp`

这节的重点不是平台差异，而是先建立一个直觉：

`IL2CPP 不是“把 C# DLL 直接变快”，而是把托管世界拆成“描述信息 + 原生实现 + runtime 拼接层”。`

### 2. `global-metadata.dat` 到底是什么

这节要正面回答读者问题。

必须讲清楚：

- 它存的是程序集、类型、方法、字段、签名、字符串、token、泛型定义等 metadata 表
- 它本质上更像“托管世界说明书”
- 它不是机器码
- 它不是一份可直接执行的 DLL
- 它也不等于“完整 IL 代码重新打包”

这节最重要的一句可以压成：

`global-metadata.dat` 回答的是“这是谁、叫什么、长什么样”，不是“这段代码怎么执行”。`

### 3. 为什么有 metadata 文件还不够

这一节专门引出 `GameAssembly`。

要讲清楚：

- metadata 只解决“识别”问题
- 真正执行还需要 AOT 产物里的 native code
- 这也是为什么“看得见 metadata”不等于“有实现可调”

这一节实际上是在给后面 AOT 泛型问题做最小铺垫。

### 4. `libil2cpp` 在中间到底做什么

这节不要展开到所有子系统，只讲和主线直接相关的职责：

- 读取 metadata
- 初始化 runtime object
- 建立类型、方法、字段在运行时里的表示
- 把 metadata 和 native method pointer 接起来

要避免把它写成“万能胶”，而应该写成：

`libil2cpp` 是组装层和运行层，它不是 metadata 文件本身，也不是 AOT 代码本身。`

### 5. 跟一条最小调用链：一个方法怎么被接起来

这节是全文真正的主线。

建议不要上来就讲泛型或反射，先跟一个普通方法。

建议回答下面几个问题：

- metadata 里怎么描述“这个方法是谁”
- runtime 里为什么会出现 `MethodInfo`
- `MethodInfo` 和 method pointer 之间是什么关系
- 为什么 metadata 和 native implementation 必须同时存在

这一节不要求深挖源码实现细节，但必须让读者在脑子里形成一条因果链：

`metadata 标识方法 -> runtime 生成方法对象 -> runtime 找到 native method pointer -> 最终发生调用`

### 6. 这套结构为什么会影响真实工程问题

这一节把抽象概念收回工程。

建议只收四类问题：

- stripping 为什么会影响反射
- 为什么有些问题看起来是 metadata 问题，实际是 native implementation 缺口
- AOT 泛型为什么天然会落在“识别”和“实现”两层之间
- HybridCLR 为什么要补 runtime metadata 能力，而不是只“加载 DLL”

这里要特别注意：

不要把 HybridCLR 写成主角，只把它作为一个最直接的应用场景。

### 7. 常见误解

这一节至少要收下面几条：

- 误解一：`global-metadata.dat` 就是“代码文件”
- 误解二：有 metadata，就一定能把方法调起来
- 误解三：`GameAssembly` 只是“把 DLL 换了个壳”
- 误解四：HybridCLR 补 metadata，就等于补出了所有 native implementation

## 开头建议

开头第一段建议直接写：

> 如果只用一句话概括 IL2CPP 最终交付给包体的东西，我会这样说：它不是一份“能直接运行的 C# 产物”，而是三层互相配合的结果：`global-metadata.dat` 负责描述托管世界，`GameAssembly` 负责承载 AOT 后的 native 代码，`libil2cpp` 负责把前两者拼成一个真正可运行的 runtime。

这句之后，立刻抛问题：

`为什么会拆成这三样？`

这样文章会从第一段就进入主问题，不会先掉进泛泛的“IL2CPP 会把 IL 转成 C++”。

## 中段最关键的桥

全文最重要的桥接判断，我建议明确写出来：

`IL2CPP 里最容易被混淆的，不是“有没有 metadata”，而是“metadata 负责识别，native code 负责执行，runtime 负责把两者接起来”。`

这句话应该在正文里至少出现两次：

- 第一次出现在解释 `global-metadata.dat` 的时候
- 第二次出现在回收到 AOT 泛型和 HybridCLR 的时候

## 这篇故意不讲什么

为了让文章保持聚焦，我建议明确不展开下面这些内容：

- 不系统讲 il2cpp 代码生成细节
- 不展开平台差异和包体目录差异
- 不写逆向教程
- 不写加密、保护或 dump 路线
- 不展开 GC、异常、线程、反射实现全貌
- 不把文章写成 libil2cpp 源码索引

## 和现有内容的关系

这篇完成后，最适合在下面几处作为前置阅读出现：

- `HybridCLR 系列索引`
- `HybridCLR 原理拆解｜从 RuntimeApi 到 Interpreter::Execute`
- `HybridCLR AOT 泛型与补充元数据`
- `Unity 裁剪` 系列后续涉及 `IL2CPP` 和反射可见性的部分

最推荐的衔接方式不是把它塞进 HybridCLR 系列编号里，而是把它当作：

`一篇服务 HybridCLR、Unity stripping、runtime 理解的公共底座文章。`

## 收尾句建议

如果全文最后要压成一句话，我建议这样收：

`IL2CPP 真正交付的不是一份单体黑盒，而是一套“metadata 描述世界、AOT 代码执行世界、runtime 负责把两者接起来”的结构；而只要这个基本坐标系立住了，后面你再看 stripping、AOT 泛型、反射和 HybridCLR，很多问题都会先自动降一个复杂度。`
