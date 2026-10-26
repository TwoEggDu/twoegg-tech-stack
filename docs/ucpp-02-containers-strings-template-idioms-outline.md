# UCPP-2｜容器、字符串与模板惯用法的阅读

更新：2026-10-25。类型：映射篇。Canonical 归属为 [UE 全栈路线的 C++ 专项表](ue-fullstack-expert-series-plan.md#cpp-source-prerequisites)。

## 定位与复用

- 前置：CXX-04、CXX-07、UCPP-1；动手沿用已有 TwinArena 与 UFS-03B 的 Actor 入口。
- 共用模型：容器/直接元素/间接目标/业务身份分开；再判断拥有还是借用、修改是否使访问失效、模板选择了什么。
- 复用 CXX-04 的移动与访问有效性、CXX-07 的推导/转发，不重写公共课，也不把标准库名字替换成 UE 类型当作映射完成。
- 复用 UFS-03B 的日志字符串、UE-01 的对象数组、UE-02 的属性/函数名字查询；补 UE 协议和旧文回链。
- 正文：`content/engine-toolchain/ucpp-02-containers-strings-template-idioms.md`；属于 UE 全栈专项，不增加公共 CXX 或 UFS 编号。

## 文章骨架

1. 可用的下标、非空的地址，为什么已经不对应原来的业务对象。
2. TArray：元素拥有、Num/Max、修改与顺序、Add/Emplace、可重定位契约；索引不充当身份。
3. TMap：Find/FindRef/FindOrAdd/operator[] 的不同协议；新增与访问失效、唯一键、散列与无稳定遍历顺序。
4. FString/FName/FText：字符处理、标识比较、可本地化显示；字符借用与转换损失。
5. MoveTemp/Forward：转换与实际移动分开，const/右值诊断、推导/引用折叠、参数包与 allocator 类型。
6. 自有两个文件：顺序表身份重查、映射查询/修改、字符串/名字、类别转发与文本格式；两种受控编译失败变体。
7. 验证步骤与未实测范围，接 UCPP-3。

## 官方依据与限制

- 目标仍为 UE 5.8.2；实际核对的 Epic 指南/API 页显示 5.8。TArray、TMap、FString、FName、FText、本地化、MoveTemp 和 Forward 见正文链接。
- 特别核对 TMap 新增可能使已有元素指针失效、FindRef 返回值、operator[] 不负责缺失插入、MoveTemp 对 const/右值的拒绝，以及 TArray 的可重定位前提。
- 文档站的部分 API 索引返回空页或跳到旧版/其他模块，未将其当成本版证据；容器语义以可访问的 5.8 指南和 TArray API 为依据。
- Unreal 源码登记仍为 TODO、路径为空；未找到 UnrealEditor/UnrealBuildTool 命令入口。没有本机 UE 编译、运行、分配/布局、文化切换、文本收集或性能验证。
- 不用标准库或模拟 UE 类型编译来证明引擎代码通过；正常与失败变体均写清文档预期、实际未运行。

## 配套与验证

- 同步 UE canonical 表、公共课依赖说明、UE/引擎阅读入口、架构索引与根系列路由。
- UCPP-1 前向导读、CXX-04/07 与 UFS-03B、UE-01/02 回链；旧 URL、原归属与原验证记录保持。
- 保留共享工作区无关改动；写作前快照：本机临时目录 `ucpp02-before-6fupv07k`。
- 验证通过：隔离副本 Hugo v0.157.0 构建 1411 页、零 ERROR；15 个范围内文件格式/shortcode 检查，238 个 relref 目标存在；11 个相关生成页的 524 处本站链接、资源与锚点检查通过，10 个旧页面回链与样例/类型表显示通过。
- 随回链修复 UFS-03B 与引擎入口中已有的单层花括号 relref；公共课/UE 工程与 M0/个人能力状态不因成稿改为通过。UE 正常样例、两个失败变体、本地化与性能均未编译或运行。

## A08 环境复核（2026-10-25）

M0仍未确认TwinArena的实际工程目录、输入revision或build_id。`Get-Command UnrealEditor,UnrealBuildTool,RunUBT`未返回已在PATH中的入口；这不证明全机没有其他安装。`E:\workspace\UnrealEngineSrc`同名目录存在，但其根下`Engine/Build/Build.version`与`Engine/Build/BatchFiles/Build.bat`不存在，不能据名称认定合法、完整或UE5.8.2可构建源码。源码登记仍TODO，不升级READY。

本轮没有下载/安装引擎、创建另一个UE工程或运行UHT/UBT/PIE/独立包；自有文件、官方核对与静态责任审查可完成，所有实际引擎观察继续待验。必须先取得实际版本/工程位置和工具链，再按本提纲矩阵逐项取证，不用普通C++/.NET通过替代。
