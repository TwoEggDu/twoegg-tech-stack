# ICPP-1/2/3 连续跟读执行记录

归属：IL2CPP canonical plan的D2/D3/D5；三项嵌入原正文，不增加10篇正文数，不创建第二份系列目录。

## 固定输入与证据层次

- 新自有输入：`examples/il2cpp/icpp-reading/ReadingProbe.cs`，NodeBase/Counter/ReadingProbe在同一程序中依次走静态Add、实例Step与Value、虚Evaluate、Echo<int/string>、静态Held持有/解除。
- 目标Unity组合沿现有lab：本机自定义2022.3.62f3_96770f904ca7，Windows x64、IL2CPP Development/C++ Debug、Minimal stripping、OptimizeSpeed；package v9.0.3/runtime v9.0.1/配套il2cpp_plus锁见lab。选择目标不等于新样本已构建。
- 本轮新执行：SDK10.0.402、net10.0、Debug、Optimize=false，桌面样本及反射身份断言通过；输出5,11,25,42,same；Held置空。真实桌面IL见同目录desktop-observation.txt、hash回执desktop-receipt.json。
- 本轮没有把新样本写入正在维护的外部lab，没有Unity编译/裁剪后IL、IL2CPP生成物、新native断点或Player运行。禁止把桌面IL称为Unity或stripped IL。
- 复用证据：原lab e0220099基础/泛型，build_id136f3c65-0c8b-4944-a838-39a5f4024e27；a387d28对象持有，build_id70021d1d-8d18-4bdf-8066-446dd9898b80；4861bed根区间/快照，build_id993e5b9e-1f71-4949-b339-39ff10d359b1。独立程序/进程，不拼接为新样本一次运行。

## ICPP-1 / D2

- 内容：完整同一自有输入；实际桌面CIL的Add、字段、callvirt、MethodSpec；托管身份→转换消费→模块/符号→执行的采证表。
- 已验：正常源行为；桌面方法token/参数与返回；虚调用和泛型目标的实际CIL解析。先缺实现编译拒绝，补实现后build/run0。初次Python读dotnet输出的GBK解码失败，修复为UTF-8读取后重新完成；不是样本编译错误。
- 待验：同源Unity编译程序集与裁剪后程序集、生成调用、模块/PDB和原生返回。NoInlining不保证裁剪保留或所有平台均保留独立符号。

## ICPP-2 / D3

- 内容：类型/方法描述、token/索引/句柄、实例身份、字段访问、托管存储与native包装责任观察表；复用同一Counter及Held。
- 已验：桌面Step读写Value至11，Held和counter引用相同，解除这一条持有；旧Player保留/解除结果、根快照边界已复核，未重做GC实验。
- 待验：新Counter样本的Unity字段访问、实际runtime描述与native偏移/根关系；Held清空不保证立即回收，根快照条目不能直接说明全部动态根。

## ICPP-3 / D5

- 内容：CallVirtual的托管声明与运行目标、Echo泛型身份/实例、注册/入口/上下文分层；对照既有P0/P1×M0/M1四组真实泛型证据。
- 已验：桌面CallVirtual IL调用NodeBase.Evaluate，结果来自Counter覆盖；两个Echo MethodSpec分别int/string。同一callvirt也用于非虚Step，故opcode不是native虚表证据。
- 复用：e0220099 metadata.txt的MemberRef/MethodSpec及同build的PDB/实际解释或原生共享路径，保留seeds预调用和无FGS配置限制。
- 待验：新样本的Unity虚分派/注册行与native入口，不能以桌面JIT结果或旧泛型实验代替。

## 后续最小Player采证（不在本轮伪填）

在既有lab空闲且确认输入基线后，把同一ReadingProbe.cs作为自有AOT程序集，从现有launcher显式调用；保留精确link.xml/显式调用依据。构建前固定输入hash与新build_id；保存Unity编译IL、stripped IL、生成目录、exe/GameAssembly/PDB、run_id；依次追Add→Step/Value→CallVirtual/Evaluate→Echo，记录实际入口，缺独立符号时检查共享/内联/裁剪。公开仅输入、步骤、行为表和许可允许的名称/路径，不转载受限实现。

D2定向纠正旧绝对表述：把“不是编译器/不做优化”改为IL2CPP AOT流程中的IL→C++阶段与平台机器码阶段分工，来源为Unity2022.3官方How IL2CPP works。未扩大为源码实现或性能结论。
