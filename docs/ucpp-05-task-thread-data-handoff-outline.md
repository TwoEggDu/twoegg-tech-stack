# UCPP-5 执行提纲与证据

类型：映射/受控案例。问题：结果仍被拥有，不等于目标仍存在；目标仍存在，也不等于仍是当前请求。

模型：Game Thread取值快照 → FAsyncTask拥有TArray、只做有限纯数据工作 → 完成关系 → Game Thread Tick取得结果 → 弱目标/业务状态/代次验证 → 应用或丢弃。

完整自有两文件及README：`examples/unreal/ucpp-05/`。接原TwinArena，不创建外部工程。停止协议：停止接受 → EnsureCompletion(false)逐项等待 → 丢弃并销毁；worker不依赖GT，所以等待不形成GT回投环。

## 2026-10-25官方与静态检查

- Epic5.8 FAsyncTaskBase/AsyncWork API核对StartBackgroundTask、IsDone、EnsureCompletion及析构期间要求。选择受控有限工作，不声称所有游戏任务都应在EndPlay同步等待。
- 静态追踪：Worker只拥有TArray和Result，不保存UObject/this；弱目标只在GT Request/Tick使用；代次在GT递增；GetTask读取仅发生在完成确认后；每个释放路径先EnsureCompletion。
- bStopImmediately与默认流程是两个独立运行，不把同一组日志混合。正常任务固定最多1024个int32，因此int64求和在此输入界内不溢出。
- 未取得UE5.8.2编译环境、TwinArena实际目录、PIE/独立包或Trace；所有工程格待验。

| 场景 | 预期 | 实际 |
|---|---|---|
| 普通完成 | Normal=6；GT应用；记录worker线程 | 未执行 |
| 旧代次 | Stale第一代丢弃，第二代5 | 未执行 |
| 业务先结束 | 弱目标仍可能Get成功，但Ended保持0 | 未执行 |
| Actor Destroy | 目标弱观察不可用于应用 | 未执行 |
| 提前关闭 | worker全部完成、Pending清空、结果不应用 | 未执行 |
| 关闭后新请求/重复关闭 | 返回false；重复清理无遗留任务 | 未执行 |
| 结束Actor/独立包 | EndPlay不留下使用该所有者的任务；日志/符号同源 | 未执行 |

待观察中workerGT不是预设为0；平台退化/配置导致执行线程不同要如实记录。无GPU任务，无GPU完成或性能结论。
## A08 环境复核（2026-10-25）

M0仍未确认TwinArena的实际工程目录、输入revision或build_id。`Get-Command UnrealEditor,UnrealBuildTool,RunUBT`未返回已在PATH中的入口；这不证明全机没有其他安装。`E:\workspace\UnrealEngineSrc`同名目录存在，但其根下`Engine/Build/Build.version`与`Engine/Build/BatchFiles/Build.bat`不存在，不能据名称认定合法、完整或UE5.8.2可构建源码。源码登记仍TODO，不升级READY。

本轮没有下载/安装引擎、创建另一个UE工程或运行UHT/UBT/PIE/独立包；自有文件、官方核对与静态责任审查可完成，所有实际引擎观察继续待验。必须先取得实际版本/工程位置和工具链，再按本提纲矩阵逐项取证，不用普通C++/.NET通过替代。
