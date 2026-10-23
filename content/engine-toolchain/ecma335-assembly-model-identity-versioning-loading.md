---
title: "ECMA-335 基础｜程序集身份、引用与加载边界"
slug: "ecma335-assembly-model-identity-versioning-loading"
date: "2026-04-14"
lastmod: "2026-10-23"
description: "区分程序集身份、模块、依赖解析和执行实现，解释 IL2CPP 主包、HybridCLR 热更 DLL 与补充元数据各自承担的职责。"
weight: 14
featured: false
tags: ["ECMA-335", "IL2CPP", "HybridCLR", "Assembly"]
series: "dotnet-runtime-ecosystem"
series_id: "ecma335"
---

两个文件都叫 Game.dll，是否可以互换？热更 DLL 加载成功，为什么里面某个类型仍然解析失败？这些问题要求我们把文件名、程序集身份、依赖引用和运行时状态分开。

这篇讨论程序集作为交付与解析单元的边界。原 A11 中与程序集身份直接相关的 strong name 内容收在这里；GAC、publisher policy、CAS 等历史机制不作为 Unity 热更新前置。

## 文件、模块与程序集

| 对象 | 负责什么 |
|---|---|
| 文件 | 一组可传输、可校验的字节 |
| 模块（module） | 持有 metadata、类型定义、token 和方法体的单元 |
| 程序集（assembly） | 由 manifest 描述的身份、文件与引用关系，是常见的部署和版本管理单元 |

Unity 常见的托管 DLL 是单模块程序集，因此三者经常被口头混说。CLI 仍区分这些概念：token 属于模块，Assembly 表描述程序集身份，AssemblyRef 表描述对其他程序集的引用。

类型的完整名字也不总能唯一标识运行时类型。相同命名空间与类型名如果来自不同程序集或加载上下文，不能直接视为同一类型。Unity／HybridCLR 支持哪些加载隔离能力，应查对应版本，不能照搬 CoreCLR 的 AssemblyLoadContext 经验。

## 身份不是文件名，也不是内容哈希

程序集身份通常涉及简单名、版本、文化信息，以及公钥相关信息。把文件改名，不会自动改掉 manifest 中的身份；把 AssemblyVersion 保持不变，也不保证类型成员和 IL 内容完全不变。

因此发布系统还需要自己的产物标识，例如主包版本、构建号与文件哈希。它们回答“到底是哪份字节”，不由程序集身份自动代替。

strong name 可以提供带公钥信息的程序集标识，但不要把它当作热更包的信任与防篡改方案。Microsoft 明确说明现代 .NET 不验证 strong-name 签名，也不以其进行程序集绑定，见 [Strong-named assemblies](https://learn.microsoft.com/en-us/dotnet/standard/assembly/strong-named)。Unity／HybridCLR 的实际校验路径须单独确认，不能从“DLL 有签名”推导“运行时一定拒绝被篡改文件”。

## AssemblyRef 描述依赖，不是下载清单

假设 HotLogic.dll 使用 Shared.dll 中的一个类型：

```text
HotLogic 的 AssemblyRef → Shared 的程序集身份
  → TypeRef → Shared 中的类型
  → MemberRef + 签名 → 具体成员
```

这个关系告诉运行时“引用了谁”，没有规定去哪台服务器下载文件，也不规定资源包如何解压或缓存。实际绑定还需要宿主的加载策略和可用程序集集合。

版本号相同也可能因为成员删除、签名变更而不兼容。相反，某些运行时可以根据策略接受不同版本。**依赖声明、绑定策略、API 兼容性是三个问题。** 完整成员解析见 [签名与引用解析]({{< relref "engine-toolchain/ecma335-signatures-and-reference-resolution.md" >}})。

## IL2CPP 主包与热更程序集的分工

IL2CPP 构建时处理托管程序集，经过裁剪与转换，再形成 native 程序和运行时需要的数据。主包里保留程序集身份，并不意味着运行时仍保存每个原始 DLL 的完整 IL。

HybridCLR 增加热更程序集的解析与解释执行能力。热更 DLL 提供新增类型、成员及相应方法体；主包已有代码和热更代码可以通过支持的调用路径协作。

普通 HybridCLR 项目的编译期依赖应避免 AOT 程序集直接引用热更程序集。官方 [配置程序集](https://www.hybridclr.cn/docs/basic/hotupdateassemblysetting)对此有明确要求。需要主包触发热更逻辑时，可以通过入口反射、共享接口或委托等组织调用，但具体选择仍需遵守对应调用支持边界。

```text
编译期：HotLogic → AOT 中的 SharedContract
运行时：AOT 启动入口 → 加载 HotLogic → 建立接口／委托等连接
```

这两条箭头表达不同的关系。不能用“运行时能调用”证明“编译期可以反向直接引用”。

## 补充元数据不是再加载一份 AOT 业务程序集

`Assembly.Load` 与 `LoadMetadataForAOTAssembly` 的职责不同：

| 操作 | 在这里解决的问题 |
|---|---|
| 加载热更程序集 | 使热更 DLL 中的类型和方法进入运行时 |
| 为 AOT 程序集补充 metadata | 为某些泛型解释执行路径提供原始泛型方法体等信息 |
| 执行业务入口 | 开始注册、配置加载、资源初始化等业务流程 |

补充元数据需要匹配相应主包产物。官方 [AOT 泛型文档](https://www.hybridclr.cn/docs/basic/aotgeneric)区分 Consistent 与 SuperSet 等匹配模式，并要求保留打包对应的 AOT 产物；不能简单理解为“文件名相同，随便拿最新 DLL 补上”。

补充操作也不会自动替换全部 native 方法、清空静态字段或重启业务。它解决的是特定执行路径的信息缺口。

## 加载成功还要继续确认什么

加载过程可能提前解析部分依赖，也可能把工作推迟到首次使用。不要依赖“所有依赖错误一定在 Assembly.Load 时报告”这样的假设。

诊断时分开记录：

1. 输入 DLL 是否完整，是否与目标主包对应。
2. 引用的程序集和类型能否找到。
3. 目标成员签名与泛型参数是否匹配。
4. 执行实现是否可用。
5. 类型静态初始化及业务入口是否完成。

第五点尤其容易被省略：拿到 Assembly 或 Type 对象，不代表其中所有 .cctor 都执行过。下一篇 [类型加载与初始化]({{< relref "engine-toolchain/ecma335-type-loading-and-initialization.md" >}}) 专门讨论这个边界。

## 卸载和重载需要另一份契约

CLI 的程序集描述本身不承诺“把同名文件再加载一次就替换旧代码”。能否卸载、哪些引用会阻止回收、旧对象如何处理、静态状态何时重建，属于具体运行时机制。

涉及 HybridCLR 的相关能力时，继续看 [程序集卸载与热重载专题]({{< relref "engine-toolchain/hybridclr-assembly-hot-reload-unload-metadata-reclaim.md" >}})，并核对版本与功能范围。普通加载、补充元数据与热重载不能互相替代。

规范入口：[ECMA-335](https://ecma-international.org/publications-and-standards/standards/ecma-335/) Partition II 的 Assemblies、Assembly／AssemblyRef 表。

**程序集身份说明引用对象是谁，构建产物说明实际字节是什么，加载和执行状态则需要继续分别确认。**

## 系列位置

- 上一篇：[方法调用与异常处理]({{< relref "engine-toolchain/ecma335-execution-model-calling-convention-exception-handling.md" >}})
- 下一篇：[类型加载与初始化]({{< relref "engine-toolchain/ecma335-type-loading-and-initialization.md" >}})
- 返回：[ECMA-335 主线索引]({{< relref "engine-toolchain/ecma335-series-index.md" >}})
