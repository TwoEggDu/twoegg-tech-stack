# UCPP-4：接入既有TwinArena的自有文件

目标UE5.8.2；文档核对5.8；未编译/运行。不是另一个UE工程，不覆盖原uproject/Build.cs。

1. 将Source下自有文件合入原TwinArena；先核对模块名及导出宏。
2. 在原TwinArena.Build.cs的PublicDependencyModuleNames中确保Core、CoreUObject、Engine；在PrivateDependencyModuleNames添加ProbeMath（只在Private cpp包含ProbeMath.h）。保留已有依赖/配置。
3. 原uproject的Modules数组追加对象：`{"Name":"ProbeMath","Type":"Runtime","LoadingPhase":"Default"}`；保留TwinArena及其他原条目，不替换整个文件。
4. 现有旋转Actor的cpp包含WiringToken.h，在BeginPlay调用RunWiringProbe。不要在全局静态初始化期调用。
5. 首次用正常UBT构建后从同一次引擎/目标/配置产物启动；保存UHT、C++、链接、装载和运行各阶段日志。观察反射FindFunction及ProcessEvent后Value=8。

故障一次一个，恢复后重建：移除WiringToken.generated.h（UHT）；将ProbeMath/Public/ProbeMath.h暂移到ProbeMath/Private/ProbeMath.h（头可见性）；在Windows modular Editor配置移除PROBEMATH_API（跨DLL导出/链接）。不删除既有Intermediate来冒充全新；全新验证用独立工作副本或真实未构建输入目录。完整命令与矩阵见文章/outline。
