---
date: "2026-03-28"
title: "Unity 脚本编译管线 05｜点击 Build 之后：Mono 与 IL2CPP 的编译路径分叉"
description: "从点击 Build 出发，拆解 Unity 打包时的脚本编译链：Scripting Backend 如何决定走 Mono 还是 IL2CPP，IL2CPP 路径里 IL 是怎么变成 C++ 再变成机器码的，以及为什么打包比编辑器内编译慢得多。"
slug: "unity-script-compilation-pipeline-05-player-build"
weight: 66
featured: false
tags:
  - "Unity"
  - "Build"
  - "IL2CPP"
  - "Mono"
  - "Compilation"
series: "Unity 脚本编译管线"
series_order: 5
---

> 编辑器编译的目标是让你能在编辑器里用，打包编译的目标是让代码能在设备上跑。这两个目标导致了完全不同的链路长度——打包要在 Roslyn 之后再走一大截，`Scripting Backend` 决定走哪条路。

## 这篇要回答什么

1. 编辑器编译和打包编译，核心区别在哪？
2. `Mono` 和 `IL2CPP` 作为 `Scripting Backend`，各自经历了哪几步？
3. `IL2CPP` 路径里，IL 是怎么一步步变成设备上能跑的机器码的？
4. 为什么打包比编辑器内编译慢那么多？

---

## 编辑器编译 vs 打包编译

前面几篇讲的编辑器编译流程是这样的：

```
C# 源码
  → Roslyn（编译器）→ IL .dll
  → ILPP（IL 后处理）
  → Domain Reload（重载脚本域）
  → 编辑器可用
```

整个过程快则几秒，改一个文件大概 5 秒左右能看到效果。

打包时，Roslyn 和 ILPP 这两步照样跑，**但目标不同**：编辑器编译产出的 `.dll` 是给编辑器进程用的；打包产出的代码要能在 Android、iOS、PC 等目标设备上独立运行，不依赖编辑器环境。

这一目标差异，让打包在 ILPP 之后多出了一整段"交付链"。

`Scripting Backend` 设置（`Project Settings → Player → Configuration → Scripting Backend`）决定这段交付链走哪条路。

---

## 两条路的分叉点

```
C# 源码
  → Roslyn → IL .dll
  → ILPP
  ↓
  ┌─────────────────────┬──────────────────────────┐
  │       Mono          │         IL2CPP           │
  └─────────────────────┴──────────────────────────┘
  .dll 直接进包体         IL → C++ → 机器码
  设备上 Mono 运行时解释  设备上直接执行原生代码
```

---

## Mono 路径

Mono 路径很短：

1. Roslyn 产出 `.dll`（IL 字节码）
2. `.dll` 直接打进包体
3. 设备上附带 `Mono 运行时`，在运行时 JIT 执行（Android / Windows / Mac）
4. iOS 不允许 JIT，改用 Full AOT（提前编译全部方法）

**特点：构建快，包体里带着 Mono 运行时，性能不如 IL2CPP。**

---

## IL2CPP 路径（重点）

IL2CPP 路径要长得多。完整流程：

```
.dll（IL 字节码）
  → Stripping（裁剪未使用的类型和方法）
  → il2cpp.exe（IL → C++ 代码生成）
  → C++ 源文件（GenCPP 目录，数百个 .cpp 文件）
  → 原生编译器（clang / Android NDK / Xcode / MSVC）
  → .so / .a / .dylib（原生二进制）
  → 最终打包进 APK / IPA / EXE
```

另外单独提取：

- `global-metadata.dat`：运行时需要的类型元数据（类名、方法名、字段信息等），在 IL2CPP 打包时从 IL 里提取出来，独立存放

### il2cpp.exe 做了什么

`il2cpp.exe` 是 Unity 自己的工具，把 IL 字节码翻译成等价的 C++ 代码。每一个 C# 类大致对应一批 `.cpp` / `.h` 文件，放在 `Temp/StagingArea/Il2Cpp/il2cppOutput`（也叫 GenCPP 目录）。

这些 C++ 文件不是给人读的，是给原生编译器吃的。

### 原生编译器阶段

Unity 把这批 C++ 文件交给目标平台的原生编译器：

| 目标平台 | 原生编译器 |
|---------|-----------|
| Android | Android NDK（clang） |
| iOS / macOS | Xcode（clang） |
| Windows | MSVC 或 clang-cl |
| PS / Xbox | 平台 SDK 自带编译器 |

原生编译器逐文件编译，最终链接成一个（或多个）原生库，Android 上是 `libil2cpp.so`，iOS 上是静态库，最终合入包体。

### 增量编译

Unity 2021 起，IL2CPP 引入了增量编译：如果某个 C# 文件没有改动，对应的 C++ 文件不会重新生成，原生编译器也跳过这些文件。增量打包能显著缩短时间，但第一次全量打包还是很慢。

---

## 为什么打包比编辑器内编译慢得多

| 阶段 | 编辑器编译 | IL2CPP 打包 |
|------|-----------|------------|
| Roslyn（C# → IL） | ✓ | ✓ |
| ILPP（IL 后处理） | ✓ | ✓（独立进程运行） |
| Stripping（裁剪） | ✗ | ✓ |
| IL → C++（il2cpp.exe） | ✗ | ✓（主要耗时之一） |
| 原生编译器（C++ → 机器码） | ✗ | ✓（主要耗时之一） |
| Domain Reload | ✓（快） | ✗ |

编辑器编译在 Roslyn + ILPP 之后就结束了，后面没有原生编译。IL2CPP 打包多出了 Stripping、`il2cpp.exe`、原生编译三段，其中原生编译器处理几百个 C++ 文件是主要耗时来源。

一个中型项目 IL2CPP 全量打包 10-30 分钟是正常的，编辑器内编译通常 5-30 秒。

---

## Mono vs IL2CPP 对比总结

| 维度 | Mono | IL2CPP |
|------|------|--------|
| 构建速度 | 快（没有原生编译） | 慢（有原生编译） |
| 运行性能 | 中（JIT / AOT） | 高（原生机器码） |
| 包体大小 | 较小（无 C++ 产物） | 较大（含原生库） |
| iOS 支持 | Full AOT（限制多） | 原生支持 |
| 调试 | 较方便 | 需要符号文件 |
| 代码保护 | 弱（.dll 可反编译） | 较强（IL 已转为机器码） |

---

## 这条链和其他文章的关系

**`global-metadata.dat` 和 `GameAssembly`** 是 IL2CPP 打包的两大产物：前者存类型元数据，后者是原生代码库。两者在运行时的分工见 [IL2CPP 运行时地图]({{< relref "engine-notes/il2cpp-runtime-map-global-metadata-gameassembly-libil2cpp.md" >}})。

**Stripping** 发生在 IL2CPP 路径的 IL 阶段（`il2cpp.exe` 运行前），Mono 路径也有 Stripping，但力度不同。Stripping 规则和踩坑见 Stripping 系列专栏。

**HybridCLR** 通过修改这条链来支持热更：它让 Unity 在打包时保留一部分程序集不走 IL2CPP，而是在运行时用自己的解释器执行。原理见 HybridCLR 系列专栏。

---

## 小结

- 编辑器编译和打包编译前半段相同（Roslyn → IL → ILPP），目标不同导致后半段完全不一样
- `Scripting Backend` 是分叉点：`Mono` 把 `.dll` 直接打包，设备上运行时执行；`IL2CPP` 把 IL 转成 C++ 再原生编译，设备上跑机器码
- IL2CPP 路径：`.dll` → Stripping → `il2cpp.exe`（IL → C++）→ 原生编译器 → 原生库 → 打包
- 打包慢的根本原因：多出了 IL → C++ 转换和原生编译器这两段，后者要处理数百个 C++ 文件
- `global-metadata.dat` 是 IL2CPP 打包时提取的类型元数据，运行时与 `GameAssembly` 配合使用

---

- 上一篇：[Unity 脚本编译管线 04｜编译卡死怎么看：从日志定位卡在哪一环]({{< relref "engine-notes/unity-script-compilation-pipeline-04-debug-hang.md" >}})
- 下一篇：[Unity 脚本编译管线 06｜.asmdef 设计：如何分包才能让增量编译更快]({{< relref "engine-notes/unity-script-compilation-pipeline-06-asmdef-design.md" >}})
- 延伸阅读：[IL2CPP 运行时地图｜global-metadata.dat、GameAssembly、libil2cpp 到底各管什么]({{< relref "engine-notes/il2cpp-runtime-map-global-metadata-gameassembly-libil2cpp.md" >}})
