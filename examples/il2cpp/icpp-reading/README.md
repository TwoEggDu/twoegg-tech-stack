# ICPP-1/2/3 共用自有程序

ReadingProbe.cs供三项训练使用，静态加法、实例字段/方法、虚调用和泛型沿同一个对象连续阅读。目标Player基线为本机自定义Unity2022.3.62f3_96770f904ca7、Windows x64、IL2CPP Development/C++ Debug；该新样本尚未完成Unity构建、裁剪后IL、生成物或原生断点验证。

DesktopCheck只做.NET桌面输入自测和实际IL读取，不冒充Unity编译器输出、裁剪后IL或IL2CPP验证。SDK10.0.402，net10.0，Debug、Optimize=false；receipt与desktop-observation.txt记录本次真实结果。

```powershell
dotnet run --project DesktopCheck/DesktopCheck.csproj
if ($LASTEXITCODE -ne 0) { throw "Desktop source/IL check failed" }
```

ReadingProbe.cs不引用Unity，可放入既有实验工程的自有AOT程序集并从现有入口显式调用Run；保持此输入不进入HotUpdate来读取AOT转换。按实际构建规则保留方法（显式调用/精确link.xml）；不能只用NoInlining推定Unity裁剪或平台内联一定关闭。不要覆盖或并发写正在使用的hybridclr-runtime-lab工程，本轮只读复用该仓旧证据。

连续Player采证待执行：保存源/程序集/stripped程序集/生成目录/模块/PDB hash及build_id；跟Add、Step/Value、CallVirtual/Evaluate、Echo两实例。生成物公开边界先核对，只发布自有输入与行为观察，不复制受限引擎实现。静态Held置空只证明解除这一条持有；不能证明立刻回收，存活/根归因复用原GC证据并保留其独立身份。
