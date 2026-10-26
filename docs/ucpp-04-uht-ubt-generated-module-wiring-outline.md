# UCPP-4 执行提纲与证据

类型：源码映射/故障案例。问题：声明存在、生成文件存在、链接成功和反射调用生效不是同一层。

主线：自有UWiringToken声明 → UBT扫描/manifest → UHT生成 → C++消费 → TwinArena/ProbeMath链接 → 运行反射FindFunction/ProcessEvent → Value=8。

完整自有文件位于`examples/unreal/ucpp-04/Source`，README给出原TwinArena的Build.cs和uproject合并点；不覆盖原文件、不新建工程。Public暴露头、Public/Private依赖传播、API导出和模块加载分别解释。正文不复制UHT生成实现。

## 2026-10-25核对

- Epic5.8 UHT、Modules和Module API Specifiers官方文档核对。UHT输入/生成与C++编译分阶段；导出只在实际modular配置下测试。
- 本机M0未确认TwinArena路径/源码revision、UE构建工具；没有可记录的真实UHT manifest、生成物hash、编译/链接日志或反射执行结果。
- 自有文件静态核对：generated.h最后include；UCLASS/GENERATED_BODY、UPROPERTY/UFUNCTION配套；无参数UFUNCTION通过ProcessEvent调用；跨模块Next是非内联且带PROBEMATH_API；Core公开依赖和ProbeMath私有依赖相容。

| 场景 | 待执行操作 | 阶段/预期 | 实际 |
|---|---|---|---|
| 正常基线 | Editor Win64 Development构建/运行 | FindFunction命中，ProcessEvent后8 | 未执行 |
| UHT故障 | 仅移除generated.h | UHT报告缺生成include；恢复该行 | 未执行 |
| 头可见性故障 | 仅把ProbeMath/Public/ProbeMath.h移到ProbeMath/Private/ProbeMath.h | 消费者编译找不到头；恢复Public | 未执行 |
| 导出故障 | 仅移除PROBEMATH_API，固定modular DLL目标 | 消费者链接无法解析Next；恢复宏 | 未执行 |
| 增量 | 恢复后仅改自有Next加数，再构建 | 日志显示受影响消费，运行值匹配 | 未执行 |
| 全新 | 独立工作副本首次构建 | 完整阶段及身份对应 | 未执行 |

故障命令、日志原文、exit code、source_revision/build_id/run_id都需真实发生后补，当前没有伪造错误文本。monolithic下删导出宏可能不失败，不能将其冒充跨DLL验证。
## A08 环境复核（2026-10-25）

M0仍未确认TwinArena的实际工程目录、输入revision或build_id。`Get-Command UnrealEditor,UnrealBuildTool,RunUBT`未返回已在PATH中的入口；这不证明全机没有其他安装。`E:\workspace\UnrealEngineSrc`同名目录存在，但其根下`Engine/Build/Build.version`与`Engine/Build/BatchFiles/Build.bat`不存在，不能据名称认定合法、完整或UE5.8.2可构建源码。源码登记仍TODO，不升级READY。

本轮没有下载/安装引擎、创建另一个UE工程或运行UHT/UBT/PIE/独立包；自有文件、官方核对与静态责任审查可完成，所有实际引擎观察继续待验。必须先取得实际版本/工程位置和工具链，再按本提纲矩阵逐项取证，不用普通C++/.NET通过替代。
