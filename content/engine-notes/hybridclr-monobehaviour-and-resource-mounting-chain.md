+++
title = "HybridCLR MonoBehaviour 与资源挂载链路｜为什么资源上挂着热更脚本也能正确实例化"
description = "从 ScriptingAssemblies.json、placeholder assembly 到真实热更 DLL 覆盖 placeholder，拆解 HybridCLR 为什么能让资源上挂着的热更 MonoBehaviour 正确实例化。"
weight = 33
featured = false
tags = ["Unity", "IL2CPP", "HybridCLR", "MonoBehaviour", "Asset"]
series = "HybridCLR"
+++

> HybridCLR 对热更 MonoBehaviour 的支持，难点不在 `AddComponent<T>()`，而在 Unity 资源反序列化链会提前按“程序集身份”去找脚本；HybridCLR 真正补的是这条身份链，而不是只补一个运行时 `Type`。

这是 HybridCLR 系列第 4 篇，把 Unity 资源挂脚本这条最容易被忽略的身份链单独拆出来。

但如果你真的想把 HybridCLR 理解到“项目里为什么能用”的程度，还差一篇很关键的东西：

`为什么资源上挂着的热更 MonoBehaviour，也能像普通脚本一样被正确实例化？`

这件事如果只用一句“HybridCLR 支持热更 MonoBehaviour”带过去，其实远远不够。

因为它难的根子，不在于“热更类能不能继承 `MonoBehaviour`”，而在于：

`Unity 在资源实例化时，根本不是简单做一次反射找 Type。`

## 这篇要回答什么

这篇主要回答 4 个问题：

1. 为什么热更 MonoBehaviour 比普通热更类更难。
2. 为什么 `AddComponent<T>()` 和“Prefab/Scene 上挂脚本”其实是两条不同的链。
3. HybridCLR 是怎么把“资源里的脚本引用”接回热更程序集的。
4. 一个真实项目里，这条链通常怎么落地。

## 为什么这个问题值得单独拆

因为关于 HybridCLR 支持 MonoBehaviour，这里最容易出现两种误解。

第一种误解是：

`热更脚本只要能继承 MonoBehaviour，就等于支持 MonoBehaviour。`

第二种误解是：

`Prefab 上挂热更脚本，和代码里 AddComponent 一个类型，本质没区别。`

这两种理解都不对。

如果你只看 C# 语言层面，当然会觉得：

- 热更程序集里有个 `class Foo : MonoBehaviour`
- 运行时把程序集 load 进来
- 然后 `AddComponent<Foo>()`

似乎事情就完了。

但真正难的不是这条代码路径。

真正难的是资源路径：

- 某个 prefab、scene、assetbundle 里已经挂了一个脚本
- 运行时实例化这个资源时，Unity 要在反序列化阶段把这个“脚本引用”解析回真正的组件类型

这时候 Unity 依赖的不是“你事后能不能拿到一个 `Type`”，而是：

`资源反序列化那一刻，脚本所属程序集和脚本身份能不能被正确解析。`

HybridCLR 对 MonoBehaviour 的支持，真正补的是这条链。

## 先给一个最小背景：`AddComponent` 路径和资源反序列化路径不是一回事

如果把问题压得足够粗，可以把两条链这样分：

### 代码路径

```csharp
var t = assembly.GetType("GameLogic.MyHotBehaviour");
go.AddComponent(t);
```

这条链的核心问题是：

- 程序集有没有加载
- `Type` 能不能拿到
- 运行时能不能实例化这个 `MonoBehaviour`

### 资源路径

某个 prefab 本来就挂着这个脚本，运行时只是 `Instantiate` 它。

这条链的核心问题是：

- Unity 在资源反序列化时，能不能根据资源里的脚本身份找到对应程序集
- 这个程序集在它的“脚本程序集列表”里是不是存在
- 这个程序集对象和后面真正加载进来的热更程序集对象，能不能保持同一条身份链

所以这篇文章真正要讲的，不是“热更脚本能不能 new 出来”，而是：

`资源里的脚本引用，最后是怎么重新接回真实热更程序集的。`

## 先给源码地图

这篇主要看这些文件：

- `Packages/HybridCLR/Editor/BuildProcessors/FilterHotFixAssemblies.cs`
- `Packages/HybridCLR/Editor/BuildProcessors/PatchScriptingAssemblyList.cs`
- `Packages/HybridCLR/Editor/BuildProcessors/ScriptingAssembliesJsonPatcher.cs`
- `Packages/HybridCLR/Editor/Il2CppDef/Il2CppDefGenerator.cs`
- `Packages/HybridCLR/Data~/Templates/AssemblyManifest.cpp.tpl`
- `HybridCLRData/.../hybridclr/generated/AssemblyManifest.cpp`
- `HybridCLRData/.../hybridclr/metadata/MetadataModule.cpp`
- `HybridCLRData/.../hybridclr/metadata/Assembly.cpp`
- `Assets/GameScripts/AOT/Procedure/ProcedureLoadAssembly.cs`

如果你准备跟着这篇文章进源码，这几个文件足够串起整条链。

## 第一段链路：先把真正的热更 DLL 从主包构建里拿掉

先看 `FilterHotFixAssemblies.cs`。

```csharp
internal class FilterHotFixAssemblies : IFilterBuildAssemblies
{
    public string[] OnFilterAssemblies(BuildOptions buildOptions, string[] assemblies)
    {
        ...
        return assemblies.Where(ass =>
        {
            string assName = Path.GetFileNameWithoutExtension(ass);
            bool reserved = allHotUpdateDllNames.All(dll => !assName.Equals(dll, StringComparison.Ordinal));
            return reserved;
        }).ToArray();
    }
}
```

这一步的语义很直接：

`打主包时，先把热更程序集从构建列表里过滤掉。`

为什么这一步是必要的？

因为如果不拿掉，热更 DLL 就会跟普通 AOT 程序集一样被打进主工程。

这样做当然能“看起来工作”，但它就不再是热更链路了，而是退回主包程序集。

所以 build-time 的第一步，其实是在先建立边界：

- 哪些程序集属于主包 AOT 世界
- 哪些程序集属于热更世界

这一步本身还没有碰到 MonoBehaviour 问题，但它是后面所有问题的前提。

## 第二段链路：虽然 DLL 本体被拿掉了，但脚本程序集名字还得补回构建产物

到这里最容易让人困惑的一点来了。

既然热更 DLL 已经被过滤出主包了，那 prefab/scene 里挂着这些脚本时，Unity 运行时怎么还能知道这些程序集名字？

答案在 `PatchScriptingAssemblyList.cs`。

先看它对 `ScriptingAssemblies.json` 的处理：

```csharp
private void AddHotFixAssembliesToScriptingAssembliesJson(string path)
{
    string[] jsonFiles = Directory.GetFiles(path, SettingsUtil.ScriptingAssembliesJsonFile, SearchOption.AllDirectories);
    foreach (string file in jsonFiles)
    {
        var patcher = new ScriptingAssembliesJsonPatcher();
        patcher.Load(file);
        patcher.AddScriptingAssemblies(SettingsUtil.HotUpdateAssemblyFilesIncludePreserved);
        patcher.Save(file);
    }
}
```

再看 `ScriptingAssembliesJsonPatcher.cs`：

```csharp
public void AddScriptingAssemblies(List<string> assemblies)
{
    foreach (string name in assemblies)
    {
        if (!_scriptingAssemblies.names.Contains(name))
        {
            _scriptingAssemblies.names.Add(name);
            _scriptingAssemblies.types.Add(16); // user dll type
        }
    }
}
```

这一步非常关键。

因为它说明 HybridCLR 做的不是“把 DLL 拷回去”，而是：

`把热更程序集的名字重新补回 Unity 的 scripting assembly 列表。`

为什么要做这件事？

`PatchScriptingAssemblyList.cs` 自己的注释已经把核心说出来了：  
`ScriptingAssemblies.json` 记录了所有 DLL 名称，这个列表会在游戏启动时自动加载；如果不在这个列表里，资源反序列化时就无法找到对应类型。

这句话几乎就是整篇文章的总钥匙。

也就是说，MonoBehaviour 资源挂载真正难的地方，不是“运行时有没有这个类”，而是：

`Unity 在资源反序列化那一刻，脚本所属程序集名字是否还在它认得的 scripting assembly 列表里。`

### 为什么这一步不是多余补丁

因为第一步我们已经把热更 DLL 从主包里拿掉了。

如果现在不把程序集名字补回 scripting assembly 列表，Unity 在资源反序列化时看到一个脚本引用，连“这个程序集身份”都认不出来。

那后面就不是解释器能不能执行的问题了，而是资源链一开始就断了。

### 跨版本差异也说明这一步属于 Unity 资源系统，而不只是 HybridCLR 私活

你再看 `PatchScriptingAssemblyList.cs`，会发现它对不同 Unity 版本走的是不同路径：

- `Unity 2020+`：补 `ScriptingAssemblies.json`
- 更老版本：补 `globalgamemanagers` 或 `data.unity3d`

这说明 HybridCLR 这里改的，确实是 Unity 自己的“脚本程序集清单”载体，而不是另起一套私有资源描述。

## 第三段链路：只补名字还不够，runtime 里还得真的有一个同名程序集对象

到这里，很多人会以为事情已经差不多了：

- 真实热更 DLL 被过滤出主包
- 但程序集名字又被补回 `ScriptingAssemblies.json`

是不是就够了？

还不够。

因为资源反序列化链不只是要看到“有这个名字”，还要能沿着这个名字找到一个真实的程序集对象。

这一步靠的是 placeholder assembly。

## 第四段链路：`AssemblyManifest.cpp` 先把热更程序集名单编进本地 `libil2cpp`

这一步其实埋在我们前一篇讲过的 `Il2CppDef` 里。

`Il2CppDefGenerator.cs` 除了生成 `UnityVersion.h`，还会生成 `AssemblyManifest.cpp`：

```csharp
foreach (var ass in _options.HotUpdateAssemblies)
{
    lines.Add($"\t\t\"{ass}\",");
}
```

模板本身也很直白：

```cpp
const char* g_placeHolderAssemblies[] =
{
    // PLACE_HOLDER
    nullptr,
};
```

在你这个工程里，当前生成出来的 `AssemblyManifest.cpp` 里是：

```cpp
const char* g_placeHolderAssemblies[] =
{
    "GameProto",
    "GameLogic",
    "DP.Client",
    nullptr,
};
```

这一步的语义是：

`把热更程序集名字编进本地 HybridCLR 版 libil2cpp，供 runtime 启动时创建 placeholder assembly。`

注意这里的程序集列表用的是 `HotUpdateAssembliesIncludePreserved`，不是单纯“被过滤出主包的 DLL”。  
因为对资源身份链来说，需要的是“完整的热更程序集身份集合”。

## 第五段链路：runtime 启动时，先把 placeholder assembly 注册进 metadata 世界

这一步发生在 runtime 初始化阶段。

先看 `MetadataModule.cpp`：

```cpp
void MetadataModule::Initialize()
{
    MetadataPool::Initialize();
    InterpreterImage::Initialize();
    Assembly::InitializePlaceHolderAssemblies();
}
```

再看 `Assembly.cpp`：

```cpp
static Il2CppAssembly* CreatePlaceHolderAssembly(const char* assemblyName)
{
    auto ass = new (HYBRIDCLR_MALLOC_ZERO(sizeof(Il2CppAssembly))) Il2CppAssembly;
    auto image2 = new (HYBRIDCLR_MALLOC_ZERO(sizeof(Il2CppImage))) Il2CppImage;
    ass->image = image2;
    ass->image->name = CopyString(assemblyName);
    ass->image->nameNoExt = ass->aname.name = CreateAssemblyNameWithoutExt(assemblyName);
    image2->assembly = ass;
    s_placeHolderAssembies.push_back(ass);
    return ass;
}

void Assembly::InitializePlaceHolderAssemblies()
{
    for (const char** ptrPlaceHolderName = g_placeHolderAssemblies; *ptrPlaceHolderName; ++ptrPlaceHolderName)
    {
        const char* nameWithExtension = ConcatNewString(*ptrPlaceHolderName, ".dll");
        Il2CppAssembly* placeHolderAss = CreatePlaceHolderAssembly(nameWithExtension);
        il2cpp::vm::MetadataCache::RegisterInterpreterAssembly(placeHolderAss);
    }
}
```

这段代码非常关键。

它说明 runtime 在真正加载热更 DLL 之前，就已经先把一批“只有程序集身份、还没有真实 metadata 内容”的 placeholder assembly 注册进了 `MetadataCache`。

所以到这一步，资源反序列化链已经有了两样东西：

- build 产物里有这个程序集名字
- runtime 里也已经有一个同名程序集对象

这就是 MonoBehaviour 资源挂载能成立的前半段基础。

如果你自己跟这段初始化，最值得盯的是三个点：

- `g_placeHolderAssemblies` 里到底有哪些名字，这决定哪些热更程序集会提前获得身份
- `CreatePlaceHolderAssembly` 只填了哪些字段，这能帮助你理解 placeholder 为什么“有身份但没内容”
- `RegisterInterpreterAssembly(placeHolderAss)` 为什么要发生在真实 DLL 加载之前，这决定资源反序列化时能不能先认出程序集对象

## 第六段链路：真正的热更 DLL 加载进来时，不是新建程序集，而是把 placeholder 填实

这一步才是我觉得最关键、也最值得精读的地方。

还是看 `Assembly.cpp`，在真正创建热更程序集时，它先做了一件事：

```cpp
TbAssembly data = image->GetRawImage().ReadAssembly(1);
const char* nameNoExt = image->GetStringFromRawIndex(data.name);

Il2CppAssembly* ass;
Il2CppImage* image2;
if ((ass = FindPlaceHolderAssembly(nameNoExt)) != nullptr)
{
    if (ass->token)
    {
        RaiseExecutionEngineException("reloading placeholder assembly is not supported!");
    }
    image2 = ass->image;
    HYBRIDCLR_FREE((void*)ass->image->name);
    HYBRIDCLR_FREE((void*)ass->image->nameNoExt);
}
else
{
    ass = new ... Il2CppAssembly;
    image2 = new ... Il2CppImage;
}
...
image->BuildIl2CppAssembly(ass);
ass->image = image2;
image->BuildIl2CppImage(image2);
image->InitRuntimeMetadatas();
il2cpp::vm::MetadataCache::RegisterInterpreterAssembly(ass);
```

这段控制流的意义非常大：

`真正的热更程序集加载时，HybridCLR 会先按名字找 placeholder；如果找到了，就复用这个已有的 Il2CppAssembly/Il2CppImage 外壳，而不是重新创建一个完全新的程序集对象。`

这就是整条身份链真正闭环的地方。

因为这意味着：

- 资源反序列化早先看到的程序集身份
- runtime 启动时预注册的 placeholder assembly
- 之后真正 `Assembly.Load(bytes)` 进来的热更程序集

最后会被收束到同一条程序集对象链上。

我觉得 HybridCLR 对资源挂载 MonoBehaviour 的支持，最硬核的价值就在这里。

它不是事后做一个“脚本替换器”，而是在程序集身份层面把这条链接通了。

如果你打算跟这一跳断点，我建议不要把注意力放在 `BuildIl2CppAssembly` 这些填充细节上，而是先盯三个变量：

- `nameNoExt`：它是不是你资源里记录的那个程序集名
- `ass`：这里拿到的是不是前面注册过的 placeholder
- `image2`：这里最终复用的是不是 placeholder 身上的那层 `Il2CppImage`

只要这三个变量在断点里对上，你就能亲眼看到“资源反序列化时期的程序集身份”和“真正热更 DLL 进来后的程序集对象”为什么会收敛到同一条链上。

## 把整条 MonoBehaviour 资源挂载链压成 4 步

如果把上面这些源码压成一条更好记的链，我会这样描述：

1. 打主包时，真实热更 DLL 先被 `FilterHotFixAssemblies` 过滤出构建。
2. 构建后，`PatchScriptingAssemblyList` 再把这些热更程序集名字补回 `ScriptingAssemblies.json` 或等价载体。
3. runtime 启动时，`AssemblyManifest.cpp` 提供名单，`InitializePlaceHolderAssemblies()` 先注册同名 placeholder assembly。
4. 真正热更 DLL 加载时，`Assembly::Create` 复用 placeholder，把它填成真实程序集。

这四步连起来，才是“资源上挂着热更脚本也能正确实例化”的底层链路。

## 为什么这件事不是普通热更方案天然就有的

因为大多数热更方案更擅长处理的是代码路径：

- 加载程序集
- 拿到类型
- 动态调用

但 MonoBehaviour 资源挂载的问题，本质上不是代码调用问题，而是：

`Unity 资源反序列化链和脚本程序集身份链怎么接起来。`

如果只会 `Assembly.Load`，通常只能解决：

- 代码里 `GetType`
- 手工 `AddComponent`

但 prefab/scene/assetbundle 里早就挂好的脚本引用，依赖的是另一套链。

HybridCLR 真正补的是这套链。

## 一个真实项目里，这条链通常怎么落地

如果看你这个工程，`ProcedureLoadAssembly.cs` 的顺序就很典型：先完成 AOT 辅助准备，再加载热更 DLL，最后才进入真正会批量实例化业务资源的阶段。

这一节不再重讲 `LoadMetadataForAOTAssembly` 的运行时语义。  
这里只强调一件事：placeholder 解决的是程序集身份链，但项目层面仍然应该把“代码加载完成”放在“资源世界全面启动”之前。

## 把这件事压成一句话

如果把这篇文章压成一句话，我会这样说：

`HybridCLR 支持资源上挂着的热更 MonoBehaviour，靠的不是“运行时后来能拿到 Type”，而是 build-time 先把热更程序集名字补回 Unity 的 scripting assembly 清单，runtime 再提前注册同名 placeholder assembly，最后让真正加载进来的热更 DLL 去填实这层 placeholder。`

这才是它能正确接回 Unity 资源工作流的原因。

## 常见误解

### 误解一：热更 MonoBehaviour 的核心是它能继承 `MonoBehaviour`

不对。

那只是语言层前提，不是资源挂载链能成立的关键。

### 误解二：Prefab 上挂热更脚本，和代码里 `AddComponent` 一个类型，本质一样

不对。

`AddComponent` 更偏运行时类型实例化；Prefab/Scene 挂载则更偏 Unity 资源反序列化和程序集身份解析。

### 误解三：`ScriptingAssemblies.json` 被 patch 只是为了“让名字好看”

不对。

这一步直接关系到资源反序列化时能不能找到对应脚本程序集身份。

### 误解四：placeholder assembly 只是一个缓存优化

也不对。

它的关键价值在于把“资源看到的程序集身份”和“后面真正加载进来的热更程序集”接成同一条对象链。

## 最后一句

如果上一篇工具链文章回答的是：

`那些按钮到底在给 runtime 准备什么输入`

那么这一篇回答的其实是：

`为什么 HybridCLR 不只是能跑热更代码，还能把 Unity 原本最难接回来的资源挂脚本链也接回来。`

我觉得这是它最像“原生工作流扩展”而不是“外挂热更框架”的地方。

## 系列位置

- 上一篇：<a href="{{< relref "engine-notes/hybridclr-toolchain-what-generate-buttons-do.md" >}}">HybridCLR 工具链拆解｜LinkXml、AOTDlls、MethodBridge、AOTGenericReference 到底在生成什么</a>
- 下一篇：<a href="{{< relref "engine-notes/hybridclr-call-chain-follow-a-hotfix-method.md" >}}">HybridCLR 调用链实战｜跟着一个热更方法一路走到 Interpreter::Execute</a>
