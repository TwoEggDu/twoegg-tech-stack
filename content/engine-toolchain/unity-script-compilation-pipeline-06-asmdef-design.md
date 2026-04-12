---
date: "2026-03-28"
title: "Unity 脚本编译管线 06｜.asmdef 设计：如何分包让增量编译更快"
description: "从增量编译的工作原理出发，解释为什么 .asmdef 的分包方式直接决定每次改代码要等多久。给出常见的慢编译模式和对应的改法，以及 Editor-only 程序集、测试程序集、第三方库的分包原则。"
slug: "unity-script-compilation-pipeline-06-asmdef-design"
weight: 67
featured: false
tags:
  - "Unity"
  - "Assembly Definition"
  - "Compilation"
  - "Performance"
  - "asmdef"
series: "Unity 脚本编译管线"
series_order: 6
---

> `.asmdef` 不是可选的工程规范，是编译速度的开关。分包越合理，每次改代码要等的时间越少。

## 这篇要回答什么

- 为什么改一个工具函数要等 30 秒？
- `bee_backend` 的增量编译逻辑是什么？
- `.asmdef` 怎么分包才能让这个等待时间缩到最短？
- Editor 代码、第三方库、测试代码分别该怎么处理？

---

## 1. 增量编译的工作原理回顾

`bee_backend` 是 Unity 编译管线的核心调度器（详见本系列第 02 篇）。它按**程序集为单位**做增量判断：

1. 某个 `.cs` 文件发生变化
2. `bee_backend` 找到它所属的程序集，标记该程序集为"需要重编"
3. **所有依赖该程序集的程序集**，也递归标记为"需要重编"
4. 没有被标记的程序集跳过，直接使用缓存

关键规律：**依赖链越短、影响范围越小，增量编译越快**。

```
改动 A → 重编 A → 重编依赖 A 的 B → 重编依赖 B 的 C
```

如果所有代码都在同一个程序集里，这条链就是整个项目。

---

## 2. 最常见的慢编译根源：一切进 Assembly-CSharp

没有任何 `.asmdef` 的项目，所有脚本都会被 Unity 编译进 `Assembly-CSharp`。

| 情况 | 程序集结构 | 改一行代码的结果 |
|---|---|---|
| 无 .asmdef | 全部在 Assembly-CSharp | 整包重编 |
| 合理分包 | 功能各自成包 | 只重编当前包及其下游 |

`Assembly-CSharp` 包含游戏主逻辑、UI、网络、战斗、工具……改任何一处，整个 `Assembly-CSharp` 重新编译，等价于**每次改一行，全量重编**。

这就是"改一个工具函数等 30 秒"的根源。

---

## 3. 四条分包原则

### 原则一：Editor-only 代码单独成包

所有放在 `Editor/` 目录下的代码，都应该单独创建 `.asmdef`，并在 Inspector 中只勾选 `Editor` 平台（即标记 `Editor Only`）。

**为什么？**

Editor 代码不进入 Player build，和运行时代码完全隔离。如果把 Editor 工具和运行时代码混在一起，每次修改运行时代码都会连带重编 Editor 工具；反过来，修改 Editor 工具也会触发运行时代码的重编（如果它们在同一个程序集）。

**效果：**

```
改 Editor 自定义窗口 → 只重编 Game.Editor.asmdef
改战斗逻辑           → Game.Editor.asmdef 不动
```

**做法：**

在 `Assets/Editor/` 目录下新建 `Game.Editor.asmdef`，Inspector 里 Platforms 只选 Editor。

---

### 原则二：稳定的第三方库单独成包

通过 Package Manager 引入的包（`YooAsset`、`UniTask`、`Cinemachine` 等）天然自带 `.asmdef`，Unity 已经为你做好了隔离。

需要注意的是**手动拷进 Assets/ 的第三方代码**。如果直接扔进项目目录，这些代码会混入 `Assembly-CSharp`，但它们几乎不会改动，每次改自己的业务代码都带着它们一起重编，纯粹浪费时间。

**做法：**

在第三方代码的根目录放一个 `.asmdef`，命名如 `ThirdParty.DoTween.asmdef`。之后这部分代码只在第一次编译，后续永不重编。

---

### 原则三：按功能模块切分运行时代码

这是收益最大、也最需要设计的一步。

**目标结构示例：**

```
Game.Foundation.asmdef   ← 基础工具，被其他模块依赖
Game.Network.asmdef      ← 依赖 Foundation
Game.UI.asmdef           ← 依赖 Foundation
Game.Battle.asmdef       ← 依赖 Foundation
Assembly-CSharp          ← 理想情况只剩入口脚本
```

**改战斗逻辑时发生什么：**

```
Game.Battle.asmdef 重编
↓
依赖 Battle 的程序集重编（如果有）
↓
Game.UI / Game.Network / Game.Foundation 不动
```

**注意粒度平衡：**

切分过细有一个代价：ILPP（IL Post-Processing，见本系列第 03 篇）需要处理每一个 DLL，DLL 数量越多，ILPP 阶段的开销越高。

实践建议：以**业务功能边界**为切分依据，不要为了追求极致粒度把一个模块拆成三四个包。5 到 15 个运行时程序集对大多数项目来说是合理区间。

---

### 原则四：测试代码单独成包

测试脚本放在独立的 `.asmdef` 中，并在 `asmdef` 文件里设置 `"testAssemblies": true`（对应 Inspector 中的 "Test Assemblies" 选项）。

| 设置 | 效果 |
|---|---|
| testAssemblies: true | 只在编辑器下编译，不进入 Player build |
| 不设置 | 测试代码混入运行时，增大包体，影响打包时间 |

测试程序集通常依赖被测模块，但被测模块不依赖测试程序集，依赖方向是单向的，不会污染运行时的编译链。

---

## 4. 依赖方向的重要性

`.asmdef` 之间的依赖只能**单向**：高层依赖低层，不能反向，更不能循环。

```
✅ Game.Battle 依赖 Game.Foundation
❌ Game.Foundation 依赖 Game.Battle
❌ Game.UI 依赖 Game.Battle，Game.Battle 依赖 Game.UI（循环）
```

循环依赖会导致 Unity 编译报错，并且无法通过任何方式绕过，只能重新整理结构。

**基础工具库的分包收益最大：**

`Game.Foundation` 被所有模块依赖，本身依赖最少。把它单独成包后，只要基础工具不改，所有模块的增量编译都不会因为它而产生额外开销。这类"被依赖多、自身改动少"的包，是分包优先级最高的候选。

---

## 5. Define Symbols 的特殊性

这是一个容易忽视的陷阱：**修改 Player Settings 里的 Define Symbols，会触发所有程序集重编**，不管你的分包有多合理。

这是 CI 流程中"明明只改了一个环境变量，却触发了全量编译"的根本原因。

**替代方案：使用 `.asmdef` 的 Version Defines**

`.asmdef` 支持基于 Package 版本条件定义符号：

```json
"versionDefines": [
  {
    "name": "com.unity.addressables",
    "expression": "1.0.0",
    "define": "USE_ADDRESSABLES"
  }
]
```

这类 Define 只影响当前程序集，不会扩散到其他程序集，不会触发全局重编。

原则：**能用 Version Defines 解决的，不用全局 Define Symbols。**

---

## 6. 一个典型项目的分包示意

```
ThirdParty.UniTask.asmdef        稳定，几乎不重编
ThirdParty.YooAsset.asmdef       稳定
─────────────────────────────────────────────
Game.Foundation.asmdef           基础工具，改动少，被所有人依赖
Game.Network.asmdef              依赖 Foundation
Game.UI.asmdef                   依赖 Foundation
Game.Battle.asmdef               依赖 Foundation
─────────────────────────────────────────────
Assembly-CSharp                  理想情况：只有入口 Bootstrap 脚本
─────────────────────────────────────────────
Game.Editor.asmdef               Editor only，不影响运行时
Game.Tests.asmdef                testAssemblies，不进 Player build
```

这个结构的目标是：**改任意一个模块，重编范围不超过该模块及其下游，第三方库和 Editor 工具永远不参与。**

---

## 小结

| 分包决策 | 影响 |
|---|---|
| 不加 .asmdef | 全量在 Assembly-CSharp，每次全编 |
| Editor 代码单独成包 | 改 Editor 工具不触发运行时重编 |
| 第三方库单独成包 | 稳定代码永不重编 |
| 按功能模块切分 | 改一个模块只重编它和下游 |
| 测试代码单独成包 | 不进 Player build，不影响打包 |
| 避免全局 Define Symbols | 避免意外全量重编 |

**给你的一个具体建议：**

如果项目现在没有任何 `.asmdef`，第一步不是规划完整结构，而是做两件事：

1. 在所有 `Editor/` 目录下加 `.asmdef`，标记 Editor Only
2. 把项目内手动引入的第三方代码单独加 `.asmdef`

这两步不需要修改任何业务代码，也不会引入循环依赖风险，但能立刻减少大量不必要的重编。之后再逐步按功能模块拆分运行时代码。

---

- 上一篇：[Unity 脚本编译管线 05｜点击 Build 之后：Mono 与 IL2CPP 的编译路径分叉]({{< relref "engine-toolchain/unity-script-compilation-pipeline-05-player-build.md" >}})
- 下一篇：[Unity 脚本编译管线 07｜CI 编译缓存：Library 哪些能缓存、哪些不能]({{< relref "engine-toolchain/unity-script-compilation-pipeline-07-ci-cache.md" >}})
