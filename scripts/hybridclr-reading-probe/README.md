# HybridCLR source-reading probe

自有最小样本，服务于源码阅读起步和调用链篇。只用于新建的独立实验工程；`ProbeBuild.PrepareAndBuild` 会更改该工程的构建设置、场景列表和 HybridCLR 设置，不应直接放进业务工程执行。

## 已验证组合

| 组件 | 固定版本 / commit |
|---|---|
| Editor | 本机自定义 `2022.3.62f3_96770f904ca7`，不是标准发行版兼容承诺 |
| hybridclr_unity | v9.0.3 / `6cadd24d101f4cbd00cd80190106f6621132d24d` |
| hybridclr | v9.0.1 / `83929787581bce6c7219418f62348a10c33582ee` |
| il2cpp_plus | v2022.3.x-9.0.1 / `ba3a76c9979d14cee404fb5d44301773e0768866` |
| Player | Windows x86_64、IL2CPP、Development、C++ Debug；Script Debugging 关闭 |

后三者按 package 的 `Data~/hybridclr_version.json` 配套。新版实验无 runtime 源码补丁，840 个配套 libil2cpp 文件、79 个 HybridCLR 非生成文件逐文件哈希一致。正常生成的桥接等文件单独作为构建产物。

## 准备

1. 建立短路径实验根目录，其下新建 Unity 工程（如 `p`）。把本目录的 `AOT`、`HotUpdate`、`Editor` 复制进 `p/Assets/`，创建 `Assets/StreamingAssets/`。
2. 安装上表锁定的 package。模块依赖使用 Unity 对应的 jsonserialize、unitywebrequest、imgui、assetbundle；这些内置模块版本为 1.0.0。
3. 在工程的同级准备 `combined-libil2cpp`：以上表 il2cpp_plus 的 `libil2cpp` 为根，把上表 hybridclr 的 `hybridclr` 目录放进去。不要带入旧实验兼容补丁；源码和 Unity 文件不随样本分发。
4. 确认 Windows IL2CPP 支持和 C++ 工具链。设置 `$editor`、`$project`、`$buildLog` 为自己的绝对路径，在 PowerShell 脚本中运行：

```powershell
& $editor -batchmode -nographics -quit -projectPath $project -executeMethod ProbeBuild.PrepareAndBuild -logFile $buildLog
```

5. 等待 Editor 退出，核对日志 `PROBE_BUILD_RESULT Succeeded` 和退出码。脚本安装项目本地 runtime，执行 GenerateAll，复制热更 DLL 到 StreamingAssets，再构建。输出在工程同级 `Player/`。

v9 本地 runtime 位于 `Library/HybridCLR/LocalIl2CppData-WindowsEditor/il2cpp`，热更 DLL 位于 `Library/HybridCLR/HotUpdateDlls/StandaloneWindows64`。不要套用 v6 的 `HybridCLRData` 路径。工程已有本地 runtime 时脚本不重新安装；重跑前必须核对版本和本地差异。

## 原生跟踪

`trace-v9.cdb` 是本次实际使用的 CDB 指令，限 Windows x64、此 v9 runtime 与本例两个 int 参数；它依赖 x64 调用约定，不能直接复用于 ARM 或任意签名。v6 Transform 的参数形式不同，不应混用。

设置 `$cdb`、`$symbols`、`$trace`、`$sample`、`$player`、`$playerLog` 为调试器、PDB 目录、输出日志、样本目录、Player exe、Player 日志的路径：

```powershell
& $cdb -G -y $symbols -logo $trace -cf "$sample/trace-v9.cdb" $player -batchmode -nographics -logFile $playerLog
```

PDB 目录为 Player 输出下的 `HybridCLRReadingProbe_BackUpThisFolder_ButDontShipItWithYourGame`。调试器在进程启动时设置延迟符号断点，避免错过自动执行的样本入口。日志应显示正确模块路径、`private pdb symbols`，并核对本次 DLL 哈希。

## 2026-10-25 实测

- `HotUpdate.dll` 4608 字节，未进入最终 `ManagedStripped` 的 AOT 程序集；AOT 启动代码通过字节加载它。
- 同一进程、同一 `MethodInfo*`：invoker 首调 `interpData=null`，GetInterpMethodInfo 与 Transform 各命中一次。
- Execute 命中两次，参数分别为 `(2,3)`、`(7,8)`；第二次 invoker 复用首次生成的同一 interpData。
- Player 日志 `PROBE_FIRST=5`、`PROBE_SECOND=15`、`PROBE_PASS`。同一二进制脱离调试器运行也通过，进程退出码 0。

样本没有业务泛型或补充 AOT metadata 调用，不代表一般项目不需要补充元数据；也不验证 GC、线程、移动端或性能。v6.1.0 在该自定义 Editor 上的失败属于旧实验，仍保留在执行记录中，不把本次 v9 成功回填成 v6 成功。

原始日志、二进制、PDB 和 runtime 留在本地临时目录；公开文章只保留自有样本、观测值与符号路径，不分发 Unity C++ 实现。身份哈希与结果见 `docs/hybridclr-player-reading-execution-plan.md`。
