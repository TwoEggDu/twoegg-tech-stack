---
date: "2026-04-18"
title: "构建系统的设计模式选择 — 我们用了什么，为什么这么选"
description: "DP + Zuma 共享构建系统的架构决策记录。讲清楚我们主动采用了哪 6 个设计模式、间接涉及了哪 4 个、又明确拒绝了哪 3 个。每一条都配上教科书文章的阅读链接，让读者既看到决策本身，也看到决策背后的推理过程。"
slug: "build-system-pattern-reading-guide"
weight: 800
tags:
  - 设计模式
  - 构建系统
  - 架构决策
  - Unity
  - CI/CD
series: "构建系统工程实践"
---

> 配合 DP + Zuma 共享构建系统的架构决策。对于每个设计选择，列出对应的教科书文章，说明为什么选、为什么不选别的。

---

## 前言：我们用这些模式解决了什么问题

在引入这套设计之前，DP 和 Zuma 各自维护一套独立的构建代码，重复度很高但又有细微差异。随着项目增加、需求增加，出现了 5 类具体的工程痛点。每类痛点都是我们后面选用某些模式的原因。

### 问题 1：两个项目的构建代码重复 90%，但没法直接复用

**症状：**
- DP 有 `DPJenkinsBuild.cs`、`DPBuildCore.cs`、`DPCiOptions.cs`、整套 Pipeline 类
- Zuma 有 `ZumaJenkinsBuild.cs`、`ZumaBuildCore.cs`、手写 `GetArg()`
- 改一个 bug 要两边同时改，改完经常忘记同步
- 新加一个功能（比如 Firebase 符号表上传）要实现两遍

**用什么模式解决：**
- **Template Method**（BuildConfig 基类）— 把共享流程放到基类，项目特有部分 override
- **Facade**（BuildCommand）— 统一外部入口，无论哪个项目进来路径一致

**达到的效果：**
- 90% 的构建逻辑集中到 `com.shanhai.toolchain/Tiangong` 一个包里
- 每个项目只需一个 `XxxBuildConfig.cs`（约 300 行）就能接入
- 新增项目从"写一套构建系统"变成"填一个配置类"

---

### 问题 2：CI 命令入口散乱，每加一个功能都要改 Jenkins Job 配置

**症状：**
- `DPJenkinsBuild.Build` 只管构建
- `CompileCheckRunner.Run` 只管编译检查
- 未来还会有 publish、vfx-perf、unit-test、asset-policy-scan...
- 每加一个功能要在 Jenkins 里新配一个 Job，全项目扩散

**用什么模式解决：**
- **Command**（ITiangongCommand）— 每个功能是一个 Command 对象
- **Chain of Responsibility**（CommandDispatcher）— 按名字路由到 Command
- **Factory Method**（反射发现 Command 实现）— 加命令不改 Dispatcher

**达到的效果：**
- Jenkins 只调一个入口：`-executeMethod CIEntryPoint.Run --command xxx`
- 加新功能 = 加一个 `XxxCommand.cs` 文件，零框架改动
- 两个项目用完全一样的命令行参数，只是 projectPath 不同

---

### 问题 3：BuildPlan 有十几个字段相互依赖，构造代码炸成一坨

**症状：**
- 版本号影响输出路径
- 输出路径影响 manifest
- manifest 影响 publish payload
- 如果直接 `new BuildPlan(a, b, c, d, e, f, g, h, ...)`，一行 200 字符
- 加字段时所有地方都要改

**用什么模式解决：**
- **Builder**（BuildPlanBuilder）— 分步骤构造，每步负责一部分字段

**达到的效果：**
- 构造过程变成 8 个独立函数，每个单独可测试
- 新字段只需加一个构造步骤
- 不同场景（CI vs 本地）可以走不同的步骤组合（Build / BuildLocal）

---

### 问题 4：外部编排工具（Zhulong Python）不好对接

**症状：**
- Zhulong Python 用 ci_context.json 传参，和 DP 的手动 CLI 参数不兼容
- 未来还要对接其他编排工具（Jenkins Shared Library、内部自研的 Scheduler）
- 每种外部调用方式都要写一套适配代码

**用什么模式解决：**
- **Command + CLI 统一参数**（所有调用方都通过同一套 `--xxx` 传参）
- **Adapter**（DPAssetConfigAdapter 这类胶水层做第三方 API 适配）

**达到的效果：**
- Zhulong Python、Jenkins、开发机手动调用，全部走同一条命令行
- 切换编排工具不影响 Unity 端
- 第三方 SDK 升级只影响 Adapter 一个文件

---

### 问题 5：多项目并行开发时，构建基础设施会互相污染

**症状：**
- 如果两个项目共享同一个版本号计数器（`uids.json`），版本号会串号
- 如果共享同一个 nginx 发布目录，APK 会互相覆盖
- 如果共享同一个 SVN manifest，更新策略会冲突

**用什么模式解决：**
- **Template Method**（BuildConfig.CountersFilePath / HostDataDirectory 等属性）— 每个项目显式指定自己的基础设施路径
- **Null Object**（BuildConfig 的 virtual 空实现）— 项目不关心的钩子直接跳过

**达到的效果：**
- `BuildConfig.CountersFilePath` 默认为 `null`，**强制项目必须显式指定**
- 每个项目有独立的版本号计数器、HostData 目录、keystore
- Nginx/S3 路径通过 `{projectKey}` 自动隔离

---

### 另外：我们拒绝某些模式解决了什么问题

拒绝 Strategy 拆分、拒绝 DI 容器、拒绝 JSON 配置传参 —— 这些拒绝本身也在解决问题：

| 拒绝的模式 | 避免了什么问题 |
|-----------|---------------|
| Strategy 接口拆分 | 避免"为了抽象而抽象"，避免每个项目写 5 个策略类 |
| 依赖注入容器 | 避免跳转困难、避免初始化负担、避免调试成本 |
| JSON 配置传参 | 避免文件 I/O、避免参数不透明、方便 AI 直接调用 |

这些决策本质都是一件事：**在我们当前规模下，保持简单比追求抽象完美更重要**。

---

## 总览：决策地图

我们的共享构建系统在 shanhai 的 `com.shanhai.toolchain` 包里，命名空间 `Shanhai.Tiangong`。核心形状是：

```
CIEntryPoint.Run
  → CommandDispatcher
    → BuildCommand   （Facade：简化入口）
      → BuildConfig   （Template Method：项目差异）
      → BuildArgs     （数据对象）
      → BuildContextFactory
      → BuildPlanBuilder   （Builder：分步构造 BuildPlan）
      → BuildExecutor
      → PublisherCoordinator
```

每个节点的选择都有设计模式在背后。我们做了**6 个主动选择**，也做了**3 个明确拒绝**。

---

## 一、主动使用的模式（6 个）

### 1. Template Method — `BuildConfig` 基类

**我们怎么用的：**

```csharp
public class BuildConfig
{
    public virtual string LauncherScenePath => "Assets/Scenes/Launcher.unity";
    public virtual void CopyRawFilesToAssets(BuildTarget target) { }
    public virtual void EnsureCollectorPackages() { }
    public virtual void ApplySceneSettings(BuildPlan plan) { }
    // ...
}

public class DPBuildConfig : BuildConfig
{
    public override string LauncherScenePath => "Assets/Scenes/Launcher.unity";
    public override void CopyRawFilesToAssets(BuildTarget target)
    {
        // DP 特有的 Wwise/HotfixDll/RawData/AOT DLL 同步
    }
}

public class ZumaBuildConfig : BuildConfig
{
    public override string LauncherScenePath => "Assets/Scenes/GameLauncher.unity";
    public override void CopyRawFilesToAssets(BuildTarget target)
    {
        // Zuma 的 GameEditor data 拷贝
    }
}
```

BuildExecutor 里是固定流程骨架，调用 `config.xxx()` 的位置就是各项目填空的地方。

**为什么选 Template Method：**
- 两个项目构建流程 **90% 相同，10% 不同**
- 差异点有限（约 8 个方法、8 个字符串属性）
- 一个项目只需写一个 `XxxBuildConfig.cs`，跳转友好
- 代码从 `config.xxx()` 直接可读，不需要查容器配置

**教科书参考：**[`patterns-02-template-method.md`]({{< relref "system-design/patterns/patterns-02-template-method.md" >}})

**为什么不选 Strategy（下面 3.1 详述）**：Codex 审核时建议拆成 IVersionStrategy、IManifestStrategy、IHotfixBridge 等策略接口。我们拒绝了，原因见下。

---

### 2. Builder — `BuildPlanBuilder`

**我们怎么用的：**

```csharp
public static class BuildPlanBuilder
{
    public static BuildPlan Build(BuildContext context)
    {
        var steps = DeriveStepSelection(context);
        var versions = AllocateVersions(context);
        var policy = ResolveResourcePolicyUpdate(context);
        var hostData = ResolveHostData(context, versions);
        var outputs = DeriveOutputLayout(context, versions);
        var buildInfo = BuildInfoBuilder.Build(context, versions, hostData);
        var manifest = ManifestMutationBuilder.Build(context, versions);
        var payload = PublishPayloadBuilder.Build(context, outputs, manifest);

        return new BuildPlan(context, steps, versions, policy, hostData, outputs, buildInfo, manifest, payload);
    }
}
```

BuildPlan 是个**有 9 个字段**的不可变数据结构，每个字段的构造都需要依赖前面的结果。直接 `new BuildPlan(...)` 会变成一行几百个字符的参数列表。

**为什么选 Builder：**
- BuildPlan 是复杂对象，字段之间有**构造依赖**（版本号影响输出路径、manifest 依赖版本号）
- 每一步逻辑清晰，可以单独测试
- 新项目想要略微不同的构造顺序（比如 Zuma 早期可能跳过 manifest 步骤），在 Builder 里加分支比在构造函数里加 if/else 清爽

**教科书参考：**[`patterns-04-builder.md`]({{< relref "system-design/patterns/patterns-04-builder.md" >}})

**为什么不选 Factory（见下面 3.2）**：BuildPlan 不是"根据类型创建不同子类"，而是"分步骤构造同一个对象"，Factory 不合适。

---

### 3. Command — `ITiangongCommand` / `BuildCommand`

**我们怎么用的：**

```csharp
public interface ITiangongCommand
{
    string Name { get; }
    CommandExecutionResult Execute(CommandContext context);
}

public sealed class BuildCommand : ITiangongCommand
{
    public string Name => "build";
    public CommandExecutionResult Execute(CommandContext context) { ... }
}

public sealed class CompileCheckCommand : ITiangongCommand { ... }
public sealed class AssetPolicyScanCommand : ITiangongCommand { ... }
// ...
```

所有 CI 功能（build、compile-check、asset-policy-scan、vfx-perf...）都是一个 Command。Jenkins 统一调用 `-executeMethod CIEntryPoint.Run --command xxx`，CommandDispatcher 路由到对应的 Command。

**为什么选 Command：**
- shanhai 的 Tiangong 模块**已经用这个模式**（CompileCheck / Doctor / AssetPolicy 等都是 Command），我们只是在现有体系里加一个 BuildCommand
- 加新命令零框架改动（反射扫描 ITiangongCommand 实现）
- Zhulong Python 外层调用只需切换 `--command` 参数，不需要换 `-executeMethod`
- 符合"统一入口 + 按名路由"的 CI 惯例

**教科书参考：**[`patterns-06-command.md`]({{< relref "system-design/patterns/patterns-06-command.md" >}})

**延伸：**BuildCommand 内部其实还同时用了 **Facade 模式**（见下一条），典型的"外层 Command、内部 Facade"组合。

---

### 4. Facade — `BuildCommand` 对 Pipeline 的封装

**我们怎么用的：**

```csharp
public CommandExecutionResult Execute(CommandContext context)
{
    var buildArgs = Parse<BuildArgs>(context);
    var config = BuildConfigDiscovery.Find();

    var buildContext = BuildContextFactory.Create(buildArgs, config, context.Request.Jenkins);
    var plan = context.Request.Jenkins.IsJenkins
        ? BuildPlanBuilder.Build(buildContext)
        : BuildPlanBuilder.BuildLocal(buildContext);
    var outputPath = BuildExecutor.Run(plan, config);

    if (context.Request.Jenkins.IsJenkins)
    {
        PublisherCoordinator.Publish(plan.PublishPayload, config);
        BuildResultWriter.Write(buildArgs.ResultPath, plan, outputPath);
    }

    return CommandExecutionResult.Success($"Build: {outputPath}");
}
```

调用方（Jenkins、Zhulong Python、本地菜单）不需要认识 `BuildContextFactory`、`BuildPlanBuilder`、`BuildExecutor`、`PublisherCoordinator`、`BuildResultWriter` 这 5 个类，只需要通过一个 Command 入口。

**为什么选 Facade：**
- Pipeline 里的组件数量多（8+），对外曝光会让调用方疲于应对
- 调用方关心的是"构建一次"这个业务目的，不是"哪几步"
- 让菜单、CI、Python 都能走同一个门面，行为一致

**教科书参考：**[`patterns-05-facade.md`]({{< relref "system-design/patterns/patterns-05-facade.md" >}})

---

### 5. Chain of Responsibility — `CommandDispatcher`

**我们怎么用的：**

```csharp
public static class CommandDispatcher
{
    private static readonly ITiangongCommand[] Commands = DiscoverCommands();

    public static CommandExecutionResult Dispatch(CommandContext context)
    {
        foreach (var command in Commands)
        {
            if (command.Name == context.CommandName || command.Aliases.Contains(context.CommandName))
                return command.Execute(context);
        }
        return CommandExecutionResult.Fail(CommandExitCode.ArgumentError,
            $"Unknown command: {context.CommandName}");
    }
}
```

入口看到 `--command build`，沿着已知 Command 数组找到第一个"认领"的。

**为什么选 Chain of Responsibility：**
- 命令是开放集合（未来会加 publish、vfx-perf、unit-test...）
- 每个 Command 自己决定是否匹配，不需要中心注册表
- 结合反射扫描实现完全**零中心化**：写一个 Command，自动被发现和路由

**教科书参考：**[`patterns-08-chain-of-responsibility.md`]({{< relref "system-design/patterns/patterns-08-chain-of-responsibility.md" >}})

**变体说明：**严格来说我们这是"单次匹配"而不是"逐级处理"，更接近 **Strategy + 发现机制**。但心智模型和维护方式和 CoR 一致，放在这里便于类比。

---

### 6. Adapter — `DPAssetConfigAdapter`（保留在 DP 胶水层）

**我们怎么用的：**

DP 项目有 `DPAssetConfigAdapter.cs`，它把 DP 特有的 YooAsset Collector 配置逻辑封装起来，通过统一接口对接共享的 `BuildConfig`。

```csharp
// DP 胶水层
public class DPBuildConfig : BuildConfig
{
    public override void EnsureCollectorPackages()
    {
        DPAssetConfigAdapter.EnsureWwisePackage();
        DPAssetConfigAdapter.EnsureHotfixDllPackage();
        DPAssetConfigAdapter.EnsureRawDataPackage();
    }
}
```

`DPAssetConfigAdapter` 把 YooAsset 的 `AssetBundleCollectorSettingData` 复杂 API 适配成 DP 期望的简单接口。

**为什么选 Adapter：**
- YooAsset 的 Collector API 粒度太细，直接调用一大堆
- DP 有多个地方需要"确保三个包存在"这个操作，Adapter 让这个操作变成一行调用
- 如果未来 YooAsset 升级改 API，只改 Adapter 一个文件

**教科书参考：**[`patterns-11-adapter.md`]({{< relref "system-design/patterns/patterns-11-adapter.md" >}})

---

## 二、间接相关的模式（4 个）

这些不是我们主动选的，但系统里自然出现，值得知道：

### 7. Observer — `OnPreBuild / OnPostBuild` 钩子

BuildConfig 上的 `OnPreBuild(BuildPlan)` / `OnPostBuild(BuildPlan, string outputPath)` 是简化版的 Observer：允许子类"订阅"构建过程的关键时刻。

和真正的 Observer 区别：我们这里是单一监听者（BuildConfig 子类），没有多播。所以本质更像 Template Method 的钩子方法，但命名习惯和 Observer 相同。

**参考：**[`patterns-07-observer.md`]({{< relref "system-design/patterns/patterns-07-observer.md" >}})

### 8. Factory Method — `BuildConfigDiscovery.Find()`

反射扫描唯一的 `BuildConfig` 子类并实例化：

```csharp
public static class BuildConfigDiscovery
{
    public static BuildConfig Find()
    {
        var baseType = typeof(BuildConfig);
        Type found = null;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in asm.GetTypes())
            {
                if (type.IsAbstract || type == baseType || !baseType.IsAssignableFrom(type))
                    continue;
                if (found != null)
                    throw new Exception($"Found multiple BuildConfig subclasses: {found.Name}, {type.Name}");
                found = type;
            }
        }
        return (BuildConfig)Activator.CreateInstance(found);
    }
}
```

这是一个"运行时决定创建什么类型"的工厂，本质是 Factory Method。

**参考：**[`patterns-09-factory.md`]({{< relref "system-design/patterns/patterns-09-factory.md" >}})

### 9. Pipeline — 构建流程本身

BuildContext → BuildPlan → BuildExecutor → PublisherCoordinator 是一条线性管线。在 Zhulong Python 端更明显：GitSync → SvnSync → CSharpBuild → Unity → Notify 也是一条管线。

**参考：**[`patterns-24-pipeline.md`]({{< relref "system-design/patterns/patterns-24-pipeline.md" >}})

### 10. Null Object — 默认空实现的 virtual 方法

BuildConfig 基类里 `CopyRawFilesToAssets`、`EnsureCollectorPackages`、`OnPreBuild`、`OnPostBuild` 都是空方法体。子类不覆盖就什么都不做，对调用方透明。这是 Null Object 的标准用法。

**参考（教科书未单独成篇，在其他文章有提）：**[`patterns-18-proxy.md`]({{< relref "system-design/patterns/patterns-18-proxy.md" >}}) 讲到过这个变体。

---

## 三、明确拒绝的模式（3 个）

### 1. 拒绝：Strategy 策略接口拆分

**Codex 的建议：**

把 BuildConfig 拆成多个策略接口：
```csharp
public interface IPlatformBuildAdapter { ... }
public interface IVersionStrategy { ... }
public interface IManifestStrategy { ... }
public interface IHotfixBridge { ... }
```

每个项目提供 4-5 个策略实现类。

**为什么拒绝：**

1. **差异维度不够多**：只有 2-3 个项目，8 个差异点，不需要 4-5 个独立策略维度
2. **跳转负担**：Pipeline 代码需要 `_version.AllocatePackageNum()` 这种调用，看不到实现在哪、需要跳四五个文件才能理解一次构建
3. **引入 DI 压力**：策略实例从哪来？如果每次 new 就浪费，如果注入就要 DI 容器，而 DI 容器就是我们**明确拒绝**的（见下一条）
4. **过度抽象的代价**：策略接口好处在"正交维度自由组合"，但我们没这个需求——DP 的版本策略和 manifest 策略是绑在一起的，硬拆反而增加错配风险

**什么时候会重新考虑：**
- 项目数超过 5 个
- 差异维度超过 6 个
- 真的出现"同一项目里需要切换 VersionStrategy"的场景

**相关教科书（对比学习用）：**
- [`patterns-03-strategy.md`]({{< relref "system-design/patterns/patterns-03-strategy.md" >}}) — Strategy 本身
- [`patterns-02-template-method.md`]({{< relref "system-design/patterns/patterns-02-template-method.md" >}}) — Template Method vs Strategy 对比

---

### 2. 拒绝：依赖注入容器（DI Container）

**可选的方案：**

引入 Autofac / Microsoft.Extensions.DependencyInjection / Zenject：
```csharp
var services = new ServiceCollection();
services.AddSingleton<IVersionStrategy, DPVersionStrategy>();
services.AddSingleton<IManifestStrategy, DPManifestStrategy>();
services.AddSingleton<BuildExecutor>();
var provider = services.BuildServiceProvider();
var executor = provider.GetRequiredService<BuildExecutor>();
```

**为什么拒绝：**

1. **可读性至上**：我们的第一优先级是"读代码时能直接跳到实现"，DI 容器把实现和调用分开，跳转要先查容器配置
2. **规模不够**：DI 的价值在"组合多变、测试替换频繁"的大型应用，我们是构建工具，流程固定
3. **项目生命周期短**：Unity Editor 的 executeMethod 是短时进程，DI 容器的初始化/生命周期管理带来的是负担不是收益
4. **调试成本**：反射魔法多了以后，Stack Trace 看不懂，错误排查时间成倍

**我们用什么代替：**
- `BuildConfig config` 作为方法参数传递（最直白）
- 反射发现唯一子类（只做一次，发现失败直接抛错）
- 静态类 + 纯函数（`BuildPlanBuilder.Build(context)` 这种）

**什么时候会重新考虑：**
- 真的需要 Mock 注入做单元测试（目前我们是集成测试为主）
- 项目数爆炸到 10+，差异维度正交化
- 出现第三方插件生态需要接入

**相关教科书：**
- [`patterns-27-di-vs-service-locator.md`]({{< relref "system-design/patterns/patterns-27-di-vs-service-locator.md" >}}) — DI vs Service Locator 对比

---

### 3. 拒绝：基于 JSON 配置传参（ci_context.json）

**可选的方案：**

Zhulong Python 已有的做法是写一个 `ci_context.json`，通过 `-ci_context path` 传给 Unity：
```json
{
  "build_number": 456,
  "build_scope": "All",
  "net_domain": "DeptIntranet",
  ...
}
```

**为什么拒绝：**

1. **AI 友好**：LLM 直接生成命令行参数比生成 + 写文件方便一个量级
2. **调试直观**：命令行参数在日志里一眼看到，JSON 要 cat 文件
3. **Jenkins 直调**：不经过 Zhulong 时（开发机手动跑），JSON 方式需要先创建文件，CLI 直接写命令
4. **已有参数机制**：Tiangong 已经有 `CommandRequestBuilder` 自动把所有 `--xxx` 收进 `Args` 字典，零改动就能加参数

**PX 项目验证：**

PX 项目（我们的参考项目）有 **43 个 CLI 参数**，跑了好多年没问题。我们的需求规模远没到需要 JSON 的程度。

**什么时候会重新考虑：**
- 参数里需要传**嵌套对象**或**大数组**（如变更文件列表 1000+ 条）
- 需要传**配置模板**被多个调用共享

**相关教科书：无（这是工程决策，不是模式选择）**

---

## 四、阅读顺序推荐

如果你想**快速理解我们的系统**，推荐顺序：

1. **先读 [Template Method]({{< relref "system-design/patterns/patterns-02-template-method.md" >}})** — 理解 BuildConfig 的设计哲学（核心）
2. **再读 [Strategy]({{< relref "system-design/patterns/patterns-03-strategy.md" >}})** — 理解为什么不用它（对比）
3. **读 [Facade]({{< relref "system-design/patterns/patterns-05-facade.md" >}})** — 理解 BuildCommand 的角色
4. **读 [Builder]({{< relref "system-design/patterns/patterns-04-builder.md" >}})** — 理解 BuildPlanBuilder
5. **读 [Command]({{< relref "system-design/patterns/patterns-06-command.md" >}})** — 理解 ITiangongCommand 体系
6. **读 [DI vs Service Locator]({{< relref "system-design/patterns/patterns-27-di-vs-service-locator.md" >}})** — 理解为什么不用 DI

如果你想**深入某个决策**，直接跳到对应章节的"为什么选/为什么拒绝"。

---

## 五、方案演进锚点

以下**信号**出现时应重新评估架构：

| 信号 | 可能重新评估的决策 |
|------|-------------------|
| 项目数 > 5 | 改用 Strategy 策略接口 |
| 差异点 > 15 个 | 考虑拆分 BuildConfig |
| 插件生态形成 | 引入 DI |
| 变更文件列表需传入 | 引入 ci_context.json |
| 出现复杂正交组合（如"DP 的版本 + Zuma 的发布"） | Strategy 必选 |
| BuildConfig.cs 超过 1000 行 | 拆成多个策略对象 |
| 要做单元测试 Mock | 引入 DI |

目前没有任何一个信号触发，所以方案保持简单是正确的。

---

## 六、文件位置

- **本文件**：`docs/build-system-pattern-reading-guide.md`
- **方案总文档**：`C:/Users/IGG/.claude/plans/swift-crunching-parnas.md`
- **审核记录**：Codex 审核了完整方案，报告在对话里
- **教科书系列**：`content/system-design/patterns/*.md`（48 篇）
- **构建系统应用线**（待写）：`content/engine-toolchain/build-system/*.md`（5 篇规划中）
