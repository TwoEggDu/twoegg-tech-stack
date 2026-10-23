# ECMA-335 系列计划：IL2CPP 与 HybridCLR 前置层

更新：2026-10-23。本文是 ECMA-335 系列唯一的 canonical 篇级目录；生态总规划仅保留模块路由。

## 定位与边界

目标读者能写 C#，需要理解 IL2CPP、HybridCLR 的 metadata、AOT 泛型、解释执行和调用边界。按问题组织规范知识，不追求 ECMA-335 全覆盖。规范基线为第六版，具体运行时能力标注版本，不引用受限 Unity C++ 源码。

已确认方案：复用旧地址，合并基础内容，分离必读、实现桥接、按需分支与历史材料；新增签名解析、类型初始化两篇。ABI、解释器、GC 实现、裁剪和 async 复用已有专题。

## 主线目录

以下文件均位于 `content/engine-toolchain/`。已写表示本地正文已落盘，不代表已发布或真机验证。

| 顺序 | 文件 | 来源与状态 |
|---|---|---|
| 导读 | `ecma335-terminology-conventions.md` | 原 A0，已写 |
| 1 | `hybridclr-pre-cli-metadata-typedef-methoddef-token-stream.md` | 原 A1／Pre-A，合并 A13 的基础定位路径 |
| 2 | `ecma335-type-system-value-ref-generic-interface.md` | 原 A3，补强泛型语义与引用边界 |
| 3 | `ecma335-signatures-and-reference-resolution.md` | 新增，已写 |
| 4 | `hybridclr-pre-cil-instruction-set-stack-machine-model.md` | 原 A2／Pre-B，合并 A8 的最小验证规则 |
| 5 | `ecma335-execution-model-calling-convention-exception-handling.md` | 原 A4，补强 constrained. 与装箱 |
| 6 | `ecma335-assembly-model-identity-versioning-loading.md` | 原 A5，收拢到身份、依赖、加载 |
| 7 | `ecma335-type-loading-and-initialization.md` | 新增，已写 |
| 8 | `ecma335-memory-model-object-layout-gc-contract-finalization.md` | 原 A6，收拢到布局、GC 与终结契约 |
| 9 | `ecma335-custom-attributes-reflection-encoding.md` | 原 A9，改为反射可见性与可调用性主线 |

## 分支与旧文迁移

| 旧编号 | 文件 | 新职责 |
|---|---|---|
| A7 | `hybridclr-bridge-il2cpp-generic-sharing-rules.md` | 泛型实现桥接，保留原地址和所属系列 |
| A8 | `ecma335-cli-verification-il-type-safety.md` | IL 重写与验证附录 |
| A10 | `ecma335-pinvoke-native-interop-marshaling-spec.md` | 原生互操作分支 |
| A11 | `ecma335-cli-security-strong-name-cas.md` | 历史背景，退出主线 |
| A12 | `ecma335-threading-memory-model-volatile-barriers.md` | 并发进阶分支 |
| A13 | `ecma335-cli-file-format-pe-cli-header.md` | PE／CLI 文件格式查阅附录 |

不删除旧 URL，不重新分配旧 A 编号。顺序以索引和本文为准：导读 1 篇、主线 9 篇、桥接 1 篇、按需及历史 5 篇；索引另计。跨系列复用文章只维护一个源文件。

## 两篇新文的执行提纲

**签名解析（原理篇）**：从同名方法和局部 token 的歧义出发，建立模块作用域 → 引用／定义 → 签名 → 类型／方法实参 → 实现的链路。用两个 DLL 中的 `Box<T>.Echo<U>(T, U)` 调用核验 MemberRef、TypeSpec、MethodSpec；区分签名与约束记录，不展开 RGCTX 内部结构。

**类型初始化（原理篇）**：区分程序集加载、类型解析／准备、CLI 类型初始化和业务初始化。用精确／宽松初始化、泛型静态字段和失败的 .cctor 做实验；一次日志次序不作为规范承诺。运行时 Class::Init 的名字不能替代对 .cctor 执行状态的检查。

## 验证与维护

依据为 ECMA-335 第六版、Microsoft 的 C#／CLI 说明与 HybridCLR 官方文档，直接在正文附出处。示例区分语义示意、预期观察与本机实测，不虚构 Unity 或设备结果。

完成后检查 relref、slug、前后篇、生态入口、YAML 和 Hugo 构建。保留无关工作区改动，不把本地修改等同于提交、发布或运行时验收。

## 本次验证记录（2026-10-23）

- .NET SDK 10.0.402、Release：编译 Shared 与 HotEntry 两个类库，核验 Echo 签名、MethodSpec 实参、MemberRef → TypeSpec 关系及调用结果。编译器在该例中生成 call，正文已按实际产物说明。
- 同版本控制台实验：核验 BeforeFieldInit 标志、精确初始化顺序、封闭泛型静态字段独立，以及失败 .cctor 不因第二次调用而重试。
- Hugo 0.157.0 extended：全站构建成功，生成 1345 页，零 ERROR；输出放在临时目录。
- 本次涉及的 20 个内容页面：YAML、代码围栏和 230 个 relref 检查通过；生成 HTML 中 250 个本地引擎工具链链接可解析，标题与源文件一致。
- 原 Pre-A、Pre-B、Bridge-D 没有显式 slug，网址由旧标题生成。本次固定旧 slug，并用旧稿与新稿的 Hugo permalink 对比确认 3 个地址完全一致。
- 未执行 Unity Player／HybridCLR 真机验证；本地修订尚未提交或发布。
