# HybridCLR 源码跟读：Player 实验执行计划

日期：2026-10-25。状态：新版隔离实验已完成。保留 v6.1.0 历史源码说明；实际使用 package v9.0.3 / runtime v9.0.1 的原始配套源码，在本机自定义 Unity 中完成 Player 构建、PDB 加载与两次目标调用断点验证。

本文是执行层，服务于 [HybridCLR 系列计划](./hybridclr-series-plan.md) 和 [IL2CPP 系列计划](./il2cpp-series-plan.md)，不另立篇级目录、不增加正文数量。

## 本轮目标与交付

先加强现有两篇，让读者能复现“加载一个热更 DLL，同一方法调用两次”的完整链路：

| 现有正文 | 本轮补充 | 完成标准 |
|---|---|---|
| `content/engine-toolchain/hybridclr-source-reading-start.md` | 实验工程准备、版本与构建身份、启动步骤和符号检查 | 按记录能生成并运行目标 Player，确认阅读源码对应实际模块 |
| `content/engine-toolchain/hybridclr-call-chain-follow-a-hotfix-method.md` | 固定版本下的逐断点输入输出、首次／再次调用对照，修正旧路径 | 同一目标方法的装载、转换、解释执行和返回值形成一条可核对的证据链 |

共用一个独立实验工程。首轮选择 Windows x86_64、IL2CPP、可调试的 native 配置；不引入业务项目依赖，不用 Editor Play 或桌面 .NET 结果代替 Player 结果。公开文章保留原 URL，正文各承担准备与跟踪职责，避免复制整套教程。

## 已知条件与基线选择

现有公开源码样本已经固定：

| 组件 | tag | commit |
|---|---|---|
| hybridclr_unity | v6.1.0 | `88e20505f87d57cbff5695be5f7baa35d626f619` |
| hybridclr | v6.1.0 | `5567f367502d916c6742873d8ed2ab13d0f6900a` |
| il2cpp_plus | v2022-6.1.0 | `ce2e34097e099e458e14192bed96c28bc761392b` |

本机预检发现 Unity Hub 手动登记的 Editor，产品版本为 `2022.3.62f3_96770f904ca7`，Windows 支持目录存在；vswhere 能定位带 C++ 工具组件的 Visual Studio 安装。这些只是环境发现，不证明 IL2CPP 支持模块完整、许可证可用、工具链兼容或该版本组合能成功构建。

用户已明确选择这套本机自定义 Editor。它是 Unity Hub 手动登记的 `2022.3.62f3_96770f904ca7`，不作为已验证的标准发行版或公开推荐组合。若需调整 HybridCLR 基线，要重新核对 package 映射与断点符号，不能只替换版本号。

本地隔离工程位于 `.codex_tmp/hybridclr-player-20261025/Project`；日志在同级 `logs/`。预检执行方法正常返回，日志记录 `version=2022.3.62f3`、Windows 目标支持为 True，HybridCLR.Runtime 成功加载，Unity 退出码为 0。预检只证明 Editor 能加载工程与包。实验 runtime 安装到项目的 `HybridCLRData/LocalIl2CppData-WindowsEditor`，不修改 Editor 安装目录。

### 旧版实验记录（失败证据保留）

自有样本已保存至 `scripts/hybridclr-reading-probe/`。日志与受限源码、PDB、二进制只保留在临时目录。

| 回合 | 日志 | 结果 |
|---|---|---|
| 原版 v6.1.0 配套 runtime | `logs/build.log` | 热更 DLL 与 GenerateAll 完成；最终 native 编译因异常辅助接口参数不匹配报 C2660 |
| 补入本机 Editor 对应的两个重载 | `logs/build-compat.log` | 暴露旧版 Field::GetName 的 const 签名差异，报 C2664 |
| 适配已核对为只读的字段名称访问 | `logs/build-compat2.log` | `PROBE_BUILD_RESULT Succeeded`，Unity 退出码 0 |
| CDB 启动上述 Player | `logs/native-trace.log`、`logs/player.log` | 加载实验目录 GameAssembly 与完整 PDB；报两个缺失导出，未进入样本 |

兼容补丁仅修改项目本地 `codegen/il2cpp-codegen-il2cpp.h` 和 `.cpp`，补入两个 marshal 异常辅助重载，并适配旧版只读字段名称访问；原文件以 `.before-probe-compat` 留存。不改 HybridCLR interpreter/transform，不向公开仓复制补丁中的 Unity 实现。安装时另补齐了旧安装器因 255 字符限制跳过的 Mono 支持文件。

最终启动阻塞为 `il2cpp_memory_pool_set_region_size` 与 `il2cpp_memory_pool_get_region_size` 导出缺失。没有添加空函数绕过，也没有进入方法级调试。实验 Player 已结束，日志已保留。当前只能说明这个原版组合不能直接工作、局部接口适配可完成构建，不能说明运行时兼容。

| 身份项 | SHA-256 |
|---|---|
| HotUpdate.dll | `709FF359610B24298C64CEE1F03B857FC192F5C89D57CC0EBBB92B55E5CAFFD4` |
| GameAssembly.dll | `2F401DCC8EAE2FD1C0BE872C4A368D49E451FBFDB59DE30503C82F3E021E16B7` |
| GameAssembly.pdb | `48FB6BF9780327E90FB165ACBDDE8C04D1125F867C334AEB288F0FCF13D898F9` |
| 本地补丁后的 codegen header | `EAEE249E2DB193DF07CADE856462C101EBA2A240B2CF7FD78B640BA41038E668` |
| 本地补丁后的 codegen cpp | `55985727C4665FAE2F16B77469B0BDB757F98F77877B7FF045016AB59567C5AC` |

CDB 的 `lmv m GameAssembly` 显示 `private pdb symbols`，模块和 PDB 均指向本次 Player 输出；`CheckSum: 00000000` 引起的 checksum 警告不是本次缺失导出的原因。首次／再次调用没有产生任何验收结果，不能把预期的 5、15 填入实测栏。

## 新版实验基线（保留 v6.1.0 阅读说明）

用户已选择升级实际实验、保留旧版源码阅读。新实验单独放在 `.codex_tmp/h9/p`，旧实验目录与失败证据保留。Editor 仍为同一套本机自定义版本，不修改安装目录；新 runtime 不沿用旧兼容补丁。

| 组件 | tag | commit |
|---|---|---|
| hybridclr_unity | v9.0.3 | `6cadd24d101f4cbd00cd80190106f6621132d24d` |
| hybridclr | v9.0.1 | `83929787581bce6c7219418f62348a10c33582ee` |
| il2cpp_plus | v2022.3.x-9.0.1 | `ba3a76c9979d14cee404fb5d44301773e0768866` |

映射取自该 package 的 `Data~/hybridclr_version.json`，不是将三个版本号强行设成相同。自有构建脚本适配了 v9 的命名空间与 `HybridCLRSettings.instance`；未改 package 源码。

| 入口 | v6.1.0 阅读位置 | v9.0.1 实验位置 |
|---|---|---|
| LoadFromBytes / Create | Assembly.cpp:97 / 107 | Assembly.cpp:124 / 131 |
| InterpreterInvoke | InterpreterModule.cpp:373 | InterpreterModule.cpp:409 |
| GetInterpMethodInfo | InterpreterModule.cpp:588，读取 body 后调用 Transform | InterpreterModule.cpp:521，调用返回 InterpMethodInfo 的 Transform |
| HiTransform::Transform | Transform.cpp:150，接收 image、method、body、result | Transform.cpp:34，内部经 MethodBodyCache 读取 body |
| Interpreter::Execute | Interpreter_Execute.cpp:1645 | Interpreter_Execute.cpp:1812 |

### 新版实际结果

- `logs/build.log`：自有构建脚本尚用旧版 `HybridCLRSettings.Instance`，编译失败；随后改为 v9 的 `instance`。不属于 runtime 补丁。
- `logs/build2.log`：GenerateAll 与最终 Player 构建完成，`PROBE_BUILD_RESULT Succeeded`，Editor 退出码 0。
- `logs/native-trace.log`：CDB 加载本次 GameAssembly 的完整 PDB，装载断点 1 次、invoker 2 次、GetInterpMethodInfo 1 次、Transform 1 次、Execute 2 次。计数仅匹配实际命中行，未计入断点设置命令。
- `logs/player.log`：目标进程 136020，加载 DLL 长度 4608，返回 5、15，PROBE_PASS。
- `logs/player-standalone.log`、`logs/player-exit.json`：同一二进制正常直接启动，返回 5、15，进程 139176 退出码 0。缓存证据来自前一次 CDB 跟踪。

新工程 runtime 实际路径为 `p/Library/HybridCLR/LocalIl2CppData-WindowsEditor/il2cpp`。840 个配套 libil2cpp 文件、79 个 HybridCLR 非生成文件与锁定源码逐文件哈希一致，无缺失、无差异；生成目录按 GenerateAll 处理。三个源码 checkout 均无本地修改。新实验没有使用旧 marshal 兼容补丁，也没有补空内存池导出。

最终 `ManagedStripped` 包含 Probe.AOT.dll、不含 HotUpdate.dll；StreamingAssets 的 DLL 字节与本次编译输出完全一致。编译输入、分发字节、模块与符号身份由各自证据核对。

| 新版产物 | SHA-256 |
|---|---|
| HotUpdate.dll | `BA0096D4CA773599ED664FD1D47CC2507D7C55F36EF0BD0CF44349EE46A9645C` |
| GameAssembly.dll | `7DF37EDA71C9FFE42B16031120C392CC2FA1A3513783CCE5DAC8A8765FB48F38` |
| GameAssembly.pdb | `C06FBE2F4471D435779A7BFE09464704733D331FEA8876D346BA226111CF322F` |

## 共用样本

- AOT 启动程序集：负责读取明确来源的热更 DLL、加载、反射取方法和触发两次调用；不直接引用热更类型。
- HotUpdate 程序集：只包含 `HotUpdate.ReadingProbe.Add(int, int)`，不放显式静态构造、模块初始化、业务资源或预热逻辑。
- 同一 Player 进程、同一 MethodInfo，依次使用 `(2, 3)` 和 `(7, 8)`，预期返回 `5`、`15`。
- 明确 HotUpdate 的热更配置和生成输入，核验它未作为 AOT 业务程序集进入主包。
- 每轮启动只装载一次样本 DLL。当前样本自动执行，通过 CDB 创建进程时设置延迟断点，避免事后附加错过首次调用。
- 若当前配置需要补充元数据，按配套流程准备并记录返回码；不通过故意遗漏生成步骤制造故障。

首轮只研究加载、方法准备和解释执行。工作线程、native 回调、泛型复杂实例和性能测量留给后续阶段。

## 执行顺序与通过条件

| 步骤 | 动作 | 必须留下的证据 | 状态 |
|---|---|---|---|
| 1. 冻结环境 | 确认 Editor 来源、patch、平台、构建工具和三份源码 | 版本表、源码 commit、本地差异、包配置 | 完成；自定义 Editor 与 v9 配套组合单列 |
| 2. 准备工程 | 新建隔离工程，配置 AOT／HotUpdate 与两次调用 | 自有脚本、程序集配置、操作步骤 | 已建立独立 asmdef、AOT 入口与热更 Add 样本 |
| 3. 生成与构建 | 按同一平台与 Development 配置生成并构建 | 构建日志、DLL 哈希、实际 runtime 路径、构建结果 | 新版原始配套源码构建成功 |
| 4. 确认进程与符号 | 启动 Player，确认模块与 PDB 对应 | 进程、GameAssembly 路径、符号加载状态 | 新版模块与完整 PDB 已确认，样本运行通过 |
| 5. 首次调用 | 新进程中加载样本并触发第一次调用 | 方法身份、初始 interpData、转换入口、Execute 参数与返回值 | null → Transform → Execute(2,3) → 5 |
| 6. 再次调用 | 同一方法对象执行第二组参数 | 缓存状态、目标 Transform 是否再次进入、Execute 参数与返回值 | 同一缓存，无再次 Transform，Execute(7,8) → 15 |
| 7. 回写正文 | 用实测记录替换相应预期段落，核对旧符号 | 两篇修改、来源与边界、Hugo／链接检查 | 完成；Hugo 1369 页零 ERROR，3 页 82 个 relref、123 个生成站内链接通过，旧文章 URL 保持 |

构建失败时先记录具体失败阶段和非敏感错误，不把“已安装工具”“成功导出工程”写成 Player 构建成功。断点未绑定时先处理模块与符号，不能据此推断调用没有发生。

Hugo 构建是站点检查，不属于 Unity runtime 验证；Git 推送和 Pages 部署状态以各自发布回执为准。

公开样本的 Editor 构建脚本、CDB 指令与实际运行副本逐字节一致；运行日志校验确认装载 1 次、invoker 2 次、方法准备 1 次、Transform 1 次、Execute 2 次，并校验同一方法指针、缓存地址、两组参数与返回值。原始调试和构建材料不进入公开仓。

## 两次调用的证据表

以下为新版 CDB 进程 136020 的实际结果。地址仅在本次进程内有意义。

| 字段 | 首次调用 | 再次调用 |
|---|---|---|
| Player 进程／构建标识 | 136020，模块哈希见上表 | 同一进程／构建 |
| DLL SHA-256、程序集与类型名 | BA0096D4…46A9645C，HotUpdate.ReadingProbe | 同一次装载 |
| 目标 MethodInfo 与方法签名 | 0x00000208f01353e8；Int32 Add(Int32, Int32) | 同一地址与签名 |
| InterpreterInvoke 入口 interpData | null | 0x00000208f0140ac8 |
| GetInterpMethodInfo／Transform | 各命中一次，目标方法地址一致 | 均未再次命中 |
| Interpreter::Execute | 参数 2、3，缓存 0x00000208f0140ac8 | 参数 7、8，缓存同一地址 |
| 返回值 | 5 | 15 |
| 调用栈记录 | native-trace.log，保留每个入口的 5 层栈 | 同一日志；无受限源码截图 |

若首调已有缓存，先找预热与先前触发；若再次转换，先核对进程、方法身份和构建版本。第二次返回正确不证明缓存命中；Execute 命中也不能证明发生了首次转换。

## 证据公开范围

可以发布自有 C# 示例、实验步骤、脱敏日志和不展示受限代码的观察表。Unity C++ 源码只保留必要符号与路径指引，不复制函数体、结构体或内部注释；调试截图应检查编辑器源码窗口、私人路径和项目标识。商业源码与业务工程不作为这轮可复现样本的组成部分。

原始完整日志、导出工程、native 二进制和调试符号留在本地实验输出。只有确有复现用途且符合许可的材料才进入公开仓。

## 后续顺序

完成前两篇的同一 Player 样本后，再依次推进：

1. **三栏泛型调用综合实验（已完成）**：同一输入连通 ECMA 签名、IL2CPP 泛型与 HybridCLR 执行；两份 Player、四组独立运行及匹配 PDB 断点完成。详见 [泛型实验执行记录](./runtime-generic-call-experiment-outline.md)，正文 `runtime-generic-call-experiment.md`。
2. **GC 与线程接入（部分完成）**：对象保留对照与托管 worker 的方法缓存／退出清理调用链已完成；原生线程回调与在途停止正常路径也已完成；原生回调的GC保留和有界分配压力也已完成；对象快照和活跃根区间也已采证；完整动态根路径归因、持续跨片段引用修改及长期压力仍待补。证据见 `hybridclr-thread-attachment-outline.md` 的 Player 补测，以及实验仓库 [固定提交 `a387d28`](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/a387d2807f4251fce0e5fc24e8d1debddb4ca79d) 的[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/a387d2807f4251fce0e5fc24e8d1debddb4ca79d/evidence/2026-10-25-lifetime-threads/README.md)。
原生回调与在途停止对应 [固定提交 `b915632`](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/b9156325820312f4d290c4af8158f76a3a6f9beb) 的[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/b9156325820312f4d290c4af8158f76a3a6f9beb/evidence/2026-10-25-native-callbacks/README.md)；上面的 a387d28 保留为对象保留与托管线程实验版本。

3. **性能与预热篇**：固定设备、输入和采样方法，分别测首次、预热、稳定执行及跨边界成本。

商业能力篇继续保留待复核状态。上述工作是否完成以各系列计划和具体实验记录为准，不由本执行计划存在来推定。

本轮GC回调实验记录：见实验仓库固定提交 [449a994](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/449a9949122e791010624f4c799495d493cca9f4) 的[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/449a9949122e791010624f4c799495d493cca9f4/evidence/2026-10-25-gc-callbacks/README.md)，构建148235df-910b-441f-94b2-b7348eb02d8c；三栏计划和线程outline分别登记现状。

## 对象快照与活跃根区间验证（2026-10-25）

- 独立 `projects/gc-roots`，build_id `993e5b9e-1f71-4949-b339-39ff10d359b1`；环境锁不变，旧gc-callbacks源码与产物回执保留。
- 最终24个Player进程退出0：handle/stack/idle各3个无快照对照、handle/stack各3个快照进程、三组各3个CDB进程。额外ABI检查仅作调试器布局验证，不算业务通过进程。
- 实际GC调用栈含GC_push_roots，GetGCRootData入口/返回缓冲成对采样。每组9个返回：活跃handle与stack均36槽/288字节，目标偏移24与48/264；idle线程仍存活且backing容量131072槽，但解释帧/stackTop0，实际返回null/0。根注册/注销完整匹配。
- 21份ManagedObjects快照：handle四阶段强目标1/1/1/0，同一Context→Slots64槽数组路径可见；stack三阶段无热更强句柄目标。所有ManagedStacks条目0，未提供解释器动态根边，不作唯一根归因。
- 独立Editor分析工程使用官方Memory Profiler1.1.0，21份快照共189组条目数量和缓冲哈希与自有读取器一致。包仅作分析依赖，不修改Player配置。57项Python测试通过。
- 实验仓库已推送固定提交 [4861bed](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/4861bedd35f9785038a15d34c8de38f261746867)，入口为[GC根区间样本](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/4861bedd35f9785038a15d34c8de38f261746867/projects/gc-roots)、[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/4861bedd35f9785038a15d34c8de38f261746867/evidence/2026-10-25-gc-roots/README.md)、[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/4861bedd35f9785038a15d34c8de38f261746867/evidence/2026-10-25-gc-roots/results.json)与同目录三组自有root-events；真实.snap、PDB和原始调用栈仅在忽略目录。449a994仍是前轮保留/压力基线。
- 三篇正文和三个索引补入实际返回区间与对象快照的证据边界。完整动态根路径归因、持续跨增量片段引用修改、性能、长期压力及其他平台仍待补；正文篇数不变。
