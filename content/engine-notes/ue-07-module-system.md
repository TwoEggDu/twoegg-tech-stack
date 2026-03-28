---
title: "Unreal 引擎架构 07｜模块系统与构建工具：UBT、.Build.cs 与模块依赖"
slug: "ue-07-module-system"
date: "2026-03-28"
description: "Unreal 的代码组织单位是模块，每个模块是一个 DLL，由 .Build.cs 描述依赖关系，UBT 驱动编译。理解模块系统是拆分大项目、编写插件的基础。"
tags:
  - "Unreal"
  - "模块系统"
  - "UBT"
  - "Build.cs"
  - "插件"
series: "Unreal Engine 架构与系统"
weight: 6070
---

Unity 用 Assembly Definition 管理代码模块，Unreal 用 **.Build.cs** 文件。每个 .Build.cs 描述一个模块，模块编译为独立的 DLL（或静态库），模块之间通过声明依赖来访问彼此的代码。理解这套系统，是在 Unreal 中组织大型项目、编写可复用插件的前提。

---

## 模块的本质

一个 Unreal 模块 = 一个目录 + 一个 .Build.cs 文件：

```
MyGame/
  Source/
    MyGame/              ← 模块目录
      MyGame.Build.cs    ← 模块描述文件
      MyGame.h           ← 模块头文件（可选）
      MyGame.cpp         ← 模块实现
      Private/           ← 私有代码（外部不可引用）
      Public/            ← 公开代码（外部可引用）
```

编译后，这个模块变成一个独立的 DLL（Windows：`MyGame.dll`，或静态链接到主可执行文件）。

---

## .Build.cs 的结构

```csharp
// MyGame.Build.cs
using UnrealBuildTool;

public class MyGame : ModuleRules
{
    public MyGame(ReadOnlyTargetRules Target) : base(Target)
    {
        PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;

        // 公开依赖：这些模块的 Public/ 路径会暴露给依赖 MyGame 的其他模块
        PublicDependencyModuleNames.AddRange(new string[]
        {
            "Core",
            "CoreUObject",
            "Engine",
            "InputCore"
        });

        // 私有依赖：只有 MyGame 自己能用，不会传递给依赖者
        PrivateDependencyModuleNames.AddRange(new string[]
        {
            "Slate",
            "SlateCore",
            "UMG",
            "GameplayAbilities",  // GAS 模块
            "GameplayTags"
        });

        // 仅编辑器模块（不进入 Shipping 包）
        if (Target.bBuildEditor)
        {
            PrivateDependencyModuleNames.Add("UnrealEd");
        }

        // 条件依赖（平台特定）
        if (Target.Platform == UnrealTargetPlatform.Android)
        {
            PrivateDependencyModuleNames.Add("AndroidPermission");
        }
    }
}
```

**Public vs Private 依赖的区别**：

```
ModuleA (Public 依赖 ModuleB)
    ModuleC 依赖 ModuleA
    → ModuleC 自动也能访问 ModuleB 的 Public/ 头文件（传递依赖）

ModuleA (Private 依赖 ModuleB)
    ModuleC 依赖 ModuleA
    → ModuleC 看不到 ModuleB 的头文件（依赖不传递）
```

---

## UBT：Unreal Build Tool

UBT（Unreal Build Tool）是用 C# 写的构建系统，负责：

1. 读取所有 .Build.cs 和 .Target.cs 文件，构建模块依赖图
2. 调用 UHT 生成反射代码（.generated.h）
3. 生成 Makefile / MSBuild / Ninja 文件
4. 调用编译器（MSVC / Clang）编译代码

```
# 手动触发 UBT（通常由 IDE 或编辑器自动调用）
UnrealBuildTool.exe MyGame Win64 Development "E:/Projects/MyGame/MyGame.uproject"

# .Target.cs 描述构建目标
# MyGame.Target.cs → 游戏客户端
# MyGameEditor.Target.cs → 编辑器版本
# MyGameServer.Target.cs → Dedicated Server
```

---

## 模块类型

| 类型 | 加载时机 | 典型用途 |
|------|---------|---------|
| `Runtime` | 游戏运行时 | 游戏逻辑、引擎功能 |
| `Editor` | 仅编辑器模式 | 自定义编辑器工具、导入器 |
| `Developer` | 开发时（不进 Shipping）| 调试工具、性能分析 |
| `ThirdParty` | 静态或动态链接 | 第三方库封装 |
| `UncookedOnly` | 仅未 Cook 时 | 编辑器数据处理 |

---

## IModuleInterface：模块的生命周期

每个模块可以实现 `IModuleInterface`，在加载/卸载时执行初始化和清理：

```cpp
// MyGameModule.h
class FMyGameModule : public IModuleInterface
{
public:
    virtual void StartupModule() override;
    virtual void ShutdownModule() override;
};

// MyGameModule.cpp
#include "MyGameModule.h"
#include "Modules/ModuleManager.h"

// 注册模块（宏展开为模块工厂函数）
IMPLEMENT_MODULE(FMyGameModule, MyGame)

void FMyGameModule::StartupModule()
{
    UE_LOG(LogTemp, Log, TEXT("MyGame module starting up"));

    // 注册全局服务、绑定委托、预加载资源等
    FMyGlobalManager::Get().Initialize();
}

void FMyGameModule::ShutdownModule()
{
    UE_LOG(LogTemp, Log, TEXT("MyGame module shutting down"));

    FMyGlobalManager::Get().Shutdown();
}
```

---

## 插件：模块的打包形式

插件是一组模块的集合，通过 .uplugin 描述符打包，可以在不同项目间复用：

```json
// MyPlugin.uplugin
{
    "FileVersion": 3,
    "Version": 1,
    "VersionName": "1.0",
    "FriendlyName": "My Plugin",
    "Description": "A reusable game plugin",
    "Category": "Gameplay",
    "Modules": [
        {
            "Name": "MyPluginRuntime",
            "Type": "Runtime",
            "LoadingPhase": "Default"
        },
        {
            "Name": "MyPluginEditor",
            "Type": "Editor",
            "LoadingPhase": "PostEngineInit"
        }
    ]
}
```

插件目录结构：
```
MyPlugin/
  MyPlugin.uplugin
  Source/
    MyPluginRuntime/
      MyPluginRuntime.Build.cs
      Public/
      Private/
    MyPluginEditor/
      MyPluginEditor.Build.cs
  Content/           ← 插件自带资产
  Resources/         ← 图标等
```

---

## 实际项目的模块拆分策略

大型项目通常把代码拆成多个模块，按功能边界划分：

```
MyGame.uproject
Source/
  MyGameCore/          ← 基础框架（GAS配置、常用工具）
  MyGameCombat/        ← 战斗系统（依赖 Core）
  MyGameUI/            ← UI 系统（依赖 Core，不依赖 Combat）
  MyGameEditor/        ← 编辑器工具（仅编辑器）
  MyGame/              ← 主模块，整合其他模块
```

**拆模块的好处**：
- 增量编译：修改 UI 代码只重编 UI 模块，不重编战斗模块
- 强制解耦：模块边界迫使明确声明依赖，避免隐式耦合
- 插件化：Combat 模块将来可以独立打包复用
