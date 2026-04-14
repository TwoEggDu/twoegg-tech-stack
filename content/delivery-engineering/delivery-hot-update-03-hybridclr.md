---
title: "脚本热更新 03｜HybridCLR 工程化——GenerateAll、AOT 泛型与 CI 集成"
slug: "delivery-hot-update-03-hybridclr"
date: "2026-04-14"
description: "HybridCLR 的接入不只是'导入 Package'。GenerateAll 必须进 CI、AOT 泛型补充必须自动化、元数据生成必须与首包构建一致——这些工程细节决定了热更新是否可靠。"
tags:
  - "Delivery Engineering"
  - "Hot Update"
  - "HybridCLR"
  - "CI/CD"
  - "Unity"
series: "脚本热更新"
primary_series: "delivery-hot-update"
series_role: "article"
series_order: 30
weight: 730
delivery_layer: "practice"
delivery_volume: "V08"
delivery_reading_lines:
  - "L2"
---

## 这篇解决什么问题

HybridCLR 的技术原理在本站的 HybridCLR 系列（48 篇）中有完整覆盖。这一篇只聚焦工程化：怎么把 HybridCLR 接进 CI、GenerateAll 怎么管、AOT 泛型补充怎么自动化、构建一致性怎么保证。

## HybridCLR 的构建步骤

HybridCLR 在标准 Unity 构建流程中增加了额外步骤：

```
标准构建流程：
  脚本编译 → 资源打包 → Player 构建 → 签名归档

HybridCLR 构建流程：
  脚本编译 → GenerateAll → AOT 泛型补充 → 资源打包 → Player 构建（首包）
                                                        ↓
  热更代码编译 → 元数据生成 → 热更 DLL 打包 → 部署到 CDN（热更包）
```

### GenerateAll

`HybridCLR.Editor.Commands.PrebuildCommand.GenerateAll()` 是 HybridCLR 构建的核心步骤。它完成：

- 生成桥接函数（Method Bridge）
- 生成 AOT 泛型引用
- 生成解释器所需的适配代码

**GenerateAll 必须进 CI**。如果只在本地执行 GenerateAll 然后把产物提交到版本库，会出现：
- 不同开发者本地环境不同，GenerateAll 产出不一致
- 版本库中的 Generated 代码和实际代码不同步
- CI 构建使用的是过期的 Generated 代码

正确做法：**CI 每次构建前自动执行 GenerateAll，不在版本库中保存 Generated 代码。**

### AOT 泛型补充

IL2CPP 的 AOT 编译在构建时为已知的泛型实例化生成代码。热更代码可能使用首包构建时不存在的泛型组合，需要补充。

HybridCLR 提供了 `补充元数据` 机制——把首包中 AOT Assembly 的元数据（.dll）随热更包一起下发，运行时注册到解释器中。

**工程化要点**：

1. **元数据 DLL 的来源**：必须从首包构建时的 IL2CPP 输出目录中提取，不能手动管理
2. **版本一致性**：元数据 DLL 必须和首包的 IL2CPP 构建产物版本一致
3. **自动化提取**：CI 构建首包后自动提取元数据 DLL 并归档，热更构建时使用同一份

```
首包构建后：
  提取 mscorlib.dll, System.dll 等 AOT 元数据 → 归档到 artifacts/aot-metadata/

热更构建时：
  从 artifacts/aot-metadata/ 获取元数据 DLL → 打入热更包
```

### Development Build 一致性

HybridCLR 在 Development Build 和 Release Build 中的行为可能不同：

| 差异 | Development | Release |
|------|------------|---------|
| Stripping | 不执行或低级别 | 执行高级别 |
| 泛型实例化 | 可能保留更多 | 可能裁剪更多 |
| 元数据 | 更完整 | 可能缺失 |

**如果用 Development Build 测试热更新通过，Release Build 可能失败**——因为 Release 的 Stripping 裁掉了热更代码需要的类型。

解法：热更新的验证必须在 Release 配置下执行。

## CI 集成方案

### 首包构建管线

```
1. Checkout 代码
2. 恢复 Library 缓存
3. 执行 GenerateAll（自动生成桥接和泛型代码）
4. 构建 Player（IL2CPP, Release 配置）
5. 提取 AOT 元数据 DLL → 归档
6. 归档首包产物（.ipa / .apk / .aab）
7. 记录首包的 commit hash 和 Unity 版本
```

### 热更包构建管线

```
1. Checkout 代码（热更分支或 commit）
2. 恢复首包构建时的 AOT 元数据
3. 编译热更 Assembly（只编译标记为热更的 asmdef）
4. 生成热更 DLL + 元数据补充包
5. 打包为热更资源包
6. 部署到 CDN
```

**关键约束**：热更包构建必须使用和首包相同的 Unity 版本、相同的 IL2CPP 版本。否则元数据格式不兼容。

### Launcher-only 场景

部分团队使用 "Launcher-only" 模式：首包只包含一个 Launcher 场景和热更下载逻辑。所有游戏代码通过热更下发。

这种模式的工程优势：
- 首包极小（只有引擎运行时 + Launcher）
- 所有游戏逻辑都可以热更新

工程风险：
- 首次启动需要下载大量热更代码（用户等待时间长）
- 运行时性能全部依赖解释器（无 AOT 加速）
- Launcher 本身的 Bug 无法热更（需要版本更新）

## 常见事故与排障

**事故：热更后某个泛型方法调用崩溃**。`ExecutionEngineException: Attempting to call method 'X' for which no ahead of time compilation was done.`

排查路径：
1. 确认 AOT 元数据 DLL 是否正确加载（启动日志中有注册记录）
2. 确认崩溃的泛型方法是否在 AOT 编译时存在
3. 如果不存在，检查补充元数据是否包含该 Assembly
4. 检查首包和热更包的构建是否使用同一份元数据

**事故：热更后 MonoBehaviour 挂载失败**。热更代码中的 MonoBehaviour 子类无法通过 AddComponent 挂载到 GameObject 上。

排查路径：
1. 确认热更 Assembly 是否已加载（Assembly.Load 成功）
2. 确认 MonoBehaviour 类的 Assembly 是否在热更清单中
3. HybridCLR 需要特殊处理 MonoBehaviour 的注册——确认 RegisterCrossBindings 是否执行

**事故：GenerateAll 在 CI 上的产出和本地不同**。CI 使用了不同的 Unity 版本或不同的 HybridCLR 版本。

排查路径：CI 环境的 Unity 版本和 HybridCLR Package 版本必须和本地一致。在 CI 日志中记录版本信息。

## 小结与检查清单

- [ ] GenerateAll 是否在 CI 中自动执行（不在版本库中保存 Generated 代码）
- [ ] AOT 元数据 DLL 是否从首包构建中自动提取并归档
- [ ] 热更包构建是否使用和首包相同的 Unity 版本和 IL2CPP 版本
- [ ] 热更新验证是否在 Release 配置下执行
- [ ] CI 日志是否记录了 Unity 版本和 HybridCLR 版本
- [ ] MonoBehaviour 挂载是否有专门的测试用例

---

**下一步应读**：[DHE 进阶]({{< relref "delivery-engineering/delivery-hot-update-04-dhe.md" >}}) — Differential Hybrid Execution 的原理和工程约束

**扩展阅读**：[HybridCLR 打包工程化]({{< relref "engine-toolchain/hybridclr-ci-pipeline-generate-all-and-development-flag.md" >}}) — GenerateAll 进 CI 和 Development 一致性的完整技术文章
