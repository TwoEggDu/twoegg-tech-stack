# UCPP-3 执行提纲与证据

类型：映射/案例；责任：CXX-02/05公共语言之后，UE原生Delegate的订阅与退出协议。问题：解绑之后已经存入队列的任务仍在，接收者可能存在却已结束业务。

1. 问题空间：绑定、派发、执行和完成是四个时刻。
2. 模型：源拥有订阅列表；FDelegateHandle标识该表中的条目；接收者强拥有与弱观察另画图。
3. 自有正常样例：一个源/一个共享接收者/一个handle；正常广播、重复Open/Close、延迟弱任务、接收者释放；额外受限Raw和UObject用法对照。
4. 边界：原生/动态和单播/多播；广播顺序不作为协议；对象存活不代替业务资格；Remove不是取消屏障。
5. 验收：下表各格记录实际引擎/构建/run身份与日志；没有环境不能填通过。

## 资料与静态核对（2026-10-25）

- Epic5.8 Delegates、Multicast Delegates文档核对AddSP/AddRaw/AddUObject、弱引用与Remove协议。代码为自有实现，没有复制引擎函数体。
- 完整文件：`examples/unreal/ucpp-03/UcppDelegateProbe.h/.cpp`；宏名TWINARENA_API依赖实际模块名。只供接入既有TwinArena，不额外创建UE项目。
- 静态核对：所有Add对应保存handle/Remove；Raw在作用域内解绑；队列只捕获TWeakPtr；Close先停业务再移除订阅；源寿命长于原生接收者；重复清理不重入释放。
- 本机没有已登记的TwinArena路径或UE5.8.2构建入口；UHT、编译、PIE、独立进程均未执行。

| 验收 | 预期 | 实际 |
|---|---|---|
| 正常与重复Open | Total=2，只订阅一次 | 待UE编译运行 |
| 显式Close后广播 | Total仍2 | 待UE运行 |
| Close后的已保存任务 | 队列任务仍执行外壳，业务门拒绝更新 | 待UE运行 |
| 最后强持有结束 | Weak.Pin失败，不发生悬垂调用 | 待UE运行 |
| Raw生命周期 | 先Remove，后离开作用域 | 待UE运行 |
| UObject Stop与EndPlay | Calls=1；重复Stop合法 | 待UE运行 |

跨线程竞争、正在执行任务的等待、完整动态委托/蓝图事件、性能均未验收；下一篇任务交接另建立完成关系。
## A08 环境复核（2026-10-25）

M0仍未确认TwinArena的实际工程目录、输入revision或build_id。`Get-Command UnrealEditor,UnrealBuildTool,RunUBT`未返回已在PATH中的入口；这不证明全机没有其他安装。`E:\workspace\UnrealEngineSrc`同名目录存在，但其根下`Engine/Build/Build.version`与`Engine/Build/BatchFiles/Build.bat`不存在，不能据名称认定合法、完整或UE5.8.2可构建源码。源码登记仍TODO，不升级READY。

本轮没有下载/安装引擎、创建另一个UE工程或运行UHT/UBT/PIE/独立包；自有文件、官方核对与静态责任审查可完成，所有实际引擎观察继续待验。必须先取得实际版本/工程位置和工具链，再按本提纲矩阵逐项取证，不用普通C++/.NET通过替代。
