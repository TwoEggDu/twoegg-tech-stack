---
title: "ECMA-335 历史背景｜Strong Name、CAS 与现代运行边界"
slug: "ecma335-cli-security-strong-name-cas"
date: "2026-04-15"
lastmod: "2026-10-23"
description: "保留 strong name 与 CAS 的历史背景；程序集身份已并入主线，不把旧权限模型当作 IL2CPP 或 HybridCLR 的安全承诺。"
weight: 19
series: "dotnet-runtime-ecosystem"
series_id: "ecma335"
tags: ["ECMA-335", "StrongName", "CAS", "History"]
---

> **阅读位置**：本文保留原 A11 地址，已移出必读目录。理解 IL2CPP 和 HybridCLR 时，先读 [程序集身份、引用与加载]({{< relref "engine-toolchain/ecma335-assembly-model-identity-versioning-loading.md" >}})，需要维护旧 .NET 代码时再回看这里。

“程序集有签名”与“程序集被允许执行什么”是两类问题。旧资料把 strong name、证据、权限集和加载策略放在一起讨论，容易让今天的读者误以为它们仍是一套默认生效的防护体系。

## Strong Name 主要解决身份问题

strong name 在程序集身份中加入公钥相关信息，历史上与 .NET Framework 的程序集部署、版本和 GAC 使用有关。

不能把它作为现代热更包可信性的充分条件。Microsoft 的 [Strong-named assemblies](https://learn.microsoft.com/en-us/dotnet/standard/assembly/strong-named)明确区分现代 .NET：运行时不验证 strong-name 签名，也不以 strong name 进行程序集绑定。旧版本中的验证策略也有具体前提，不能泛化成“所有 CLR 都会验签后才加载”。

文件完整性、发布者信任、传输和回滚控制，需要自己的交付机制。给程序集加上 strong name 不会自动完成这些职责。

## CAS 曾试图解决什么

Code Access Security 是旧 .NET Framework 中按代码来源、权限与调用关系限制执行能力的一套机制。其资料常涉及 evidence、permission set、Demand 和栈检查。

它与静态访问控制、CLI 类型安全验证不同：private／public 限制成员访问，verification 检查类型安全，CAS 则涉及权限判断。三者不能互相替代。

现代 .NET 不支持用 CAS 建立旧式部分信任沙箱。旧权限 API 的迁移表现可能是过时警告、异常或无实际效果，应逐项查兼容性，不能笼统承诺“全都忽略且不报错”。参考 Microsoft 的 [CAS API obsolete 说明](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/5.0/code-access-security-apis-obsolete)。

## 与 Unity 热更新的关系

不要因为热更 DLL 使用托管代码，就假定它在隔离沙箱里执行；也不要因为某个产品提供访问控制功能，就把它等同于 CAS 的完整模型。

评估具体 HybridCLR 功能时，要按版本确认允许的访问范围、检查阶段和限制。IL2CPP／HybridCLR 的执行模型本身，不代替应用的发布授权、包校验和插件隔离设计。

**身份、类型安全与执行权限分别回答不同问题；历史 CAS 机制不应成为现代热更新系统的默认前提。**

## 返回主线

- [程序集身份、引用与加载]({{< relref "engine-toolchain/ecma335-assembly-model-identity-versioning-loading.md" >}})
- [ECMA-335 索引]({{< relref "engine-toolchain/ecma335-series-index.md" >}})
