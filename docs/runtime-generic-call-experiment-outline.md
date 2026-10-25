# 泛型调用综合实验：执行提纲

2026-10-25。已有参考：`hybridclr-player-reading-execution-plan.md` 已验证本机自定义 Unity 的原生调试链；本次补同一个泛型调用从 metadata 到 native／解释执行的证据。

正文唯一目标：`content/engine-toolchain/runtime-generic-call-experiment.md`，归 HybridCLR 系列，ECMA-335 和 IL2CPP 索引共同引用。本文是执行层，完成后回写三份 canonical 系列计划。

## 验证块 1：调用的规范身份

- 结论：三个热更调用引用同一 AOT 泛型定义，通过不同 MethodSpec 指定 string、int、自有 Payload 实参。
- 最小场景：Generic.AOT 中的静态 Echo<T>(T)，HotUpdate 中三个无参数包装入口。
- 控制：同一 Editor、源码与编译配置，两个 Player 最终投放同一份热更 DLL；方法使用 NoInlining，但单独验证解释器是否内联，Payload 含 int 与 long 字段。
- 指标：定义 token／签名、MemberRef、MethodSpec blob、实际 call 操作数、程序集依赖、两份热更 DLL 哈希。
- 工具：System.Reflection.Metadata 只读分析实际 DLL；完整解码 IL 操作数宽度后定位 call，避免枚举到未使用的 MethodSpec 就当成调用。
- 不符时：先比较 DLL 身份，再核对编译器指令和 metadata 表；不照搬规范示例的 token 数值。

## 验证块 2：AOT 输入与 native 产物

- 结论：显式 AOT 实例引用是否改变生成实现，由产物和符号验证，不从 typeof 或保留规则推定。
- 场景：两个独立工程，无 seeds／有 seeds；有 seeds 在 AOT 启动阶段实际调用三种 Echo 实例。
- 控制：固定 Windows x64、IL2CPP、Development、C++ Debug，明确代码生成与裁剪配置。正常执行 GenerateAll，不改 runtime、不伪造缺失文件。
- 指标：最终裁剪后的 Generic.AOT.dll、HotUpdate 排除、生成 Echo 实现名称与 native 符号。保留完整输出于本地，公仓不转载 Unity 受限实现。
- 工具：Unity 构建日志、metadata 读取器、CDB 符号；核对共享实例指针而不是只数文件。
- 不符时：检查生成工具是否已间接保留闭合实例、是否使用 fully shared 实现、裁剪与桥接输入是否一致。

## 验证块 3：运行时对照

- 结论：补充元数据是否改变调用路径、结果和缓存状态，由同一构建的独立进程对照确认。
- 场景：每个构建分别启动不补／补元数据进程；补入同次构建裁剪后的 Generic.AOT.dll，使用 Consistent 并记录返回码。每种输入调用两次。
- 控制：四个独立进程，不在同进程先补后假装未补；不做预热 API，不用性能数据解释功能结果。
- 指标：string="echo-string"，int=42，Payload(X=7,Y=11) 回传后校验编码值 7000011；异常类型、补充返回码、Echo 的实际 native／解释路径、方法身份及缓存。结果不必全部成功，预期失败必须完整记录。
- 工具：Player 日志、退出码、CDB 原生断点与调用栈。区分 HotUpdate 包装方法进入解释器与 Echo 本身进入解释器。
- 不符时：先核对方法所属程序集、闭合泛型实参和 native pointer，再判断共享或解释回退；不预设“不补 metadata 必失败”。

## 固定环境与边界

本机自定义 Editor `2022.3.62f3_96770f904ca7`；package v9.0.3 `6cadd24d101f4cbd00cd80190106f6621132d24d`，runtime v9.0.1 `83929787581bce6c7219418f62348a10c33582ee`，il2cpp_plus v2022.3.x-9.0.1 `ba3a76c9979d14cee404fb5d44301773e0768866`。不修改 Editor 安装目录，不推广为标准发行版或其他平台结论。

## 首次隔离实验完成记录

文章类型：案例篇，问题是同一泛型定义的三种调用为何有不同执行能力。先分描述／产物／执行三层，再给控制变量、四组结果与路径证据；不另写三篇重复教程。

- P0/P1 均构建成功，Bee 最终输入记录 C++ Debug、Minimal stripping、EnableFullGenericSharing=false。HotUpdate 不在最终裁剪备份或原生 code registration 中；ScriptingAssemblies.json 的热更名称由 package 补写，不能作为 AOT 转换证据。
- 独立编译的热更 DLL 哈希不同，但三处调用 IL／签名一致。最终统一投放 P0 DLL，SHA-256 `D943566A385907793BD4A49F2F5A666A4DC0958FD60AE5BD1E6A187EFD55CC20`，重跑受影响的 P1 两组。
- 每份补充的 Generic.AOT.dll 均与其最终 Player 的裁剪备份哈希一致；两个 M1 返回 OK、4608 bytes。
- P0/M0：string 两次成功，int/Payload 各两次 MissingMethodException；退出 2。P0/M1、P1/M0、P1/M1 均六次结果正确，退出 0。
- 四组均为独立进程。每组三个包装方法各 Transform 一次、Execute 两次，同一方法第二次使用相同非空 interpData。失败组也有包装方法 Execute，不能把入口命中等同调用成功。
- P0 string 命中 FullySharedGenericAny 原生实现；P0/M1 的 int/Payload 命中 Echo 的 TransformSubMethodBody，方法体来自 ConsistentAOTHomologousImage，再由调用者解释执行，无独立 Echo 解释帧。NoInlining 不作为该版解释器禁止内联的保证。
- P1 三个共享实现各命中 3 次：1 次 seeds、2 次热更调用。补充元数据没有强制改为解释路径。seeds 同时预先调用目标，故本轮不比较冷启动性能。
- `scripts/runtime-generic-probe/verify_results.py` 已通过两份构建、四组 CDB 运行和四组无调试器运行的结果／退出码／哈希／路径／缓存检查。摘要与签名输出在同目录 evidence/，无 Unity C++ 实现或私人路径。
- 自有脚本、CDB 指令和元数据工具已归档；正文及三栏索引、canonical plans 已回写。Hugo／生成站内链接在交付前单独验证，不算 runtime 证据。
- 交付检查：Hugo 0.157.0 extended 构建 1373 页、零 ERROR；正文和三份索引的 124 个 relref、177 个生成站内链接均通过。公开 C#／asmdef 与两个实际构建工程逐字节一致，三份依赖源码 HEAD 与上述固定提交一致且工作树干净。未提交、推送或核验 Pages 部署。

原始构建、PDB、trace 与 Player 日志留在本地 `.codex_tmp/g9/`，不进入公仓。尚未验证性能、其他平台、Release、热更定义的值类型或商业能力。

## 公开实验仓库基线

上述旧脚本路径与 DLL 哈希属于首次隔离实验记录。当前复现入口为 [hybridclr-runtime-lab 固定提交 e0220099](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/e0220099d293f7a1a4601ee9931b49a10dd519f9)，原样本目录不作为新的发布依赖。新仓库已重新构建并完成同样四组运行与原生断点验证，记录见 [公开证据](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/e0220099d293f7a1a4601ee9931b49a10dd519f9/evidence/2026-10-25/README.md)。正文已切换到新仓库的复现命令和构建哈希，保留本节之前的历史记录用于区分两次实验。
