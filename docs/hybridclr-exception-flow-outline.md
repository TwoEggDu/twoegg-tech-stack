# 跨 AOT／解释器异常传播实验记录

日期2026-10-26。用户授权沿用原环境查清内部catch逃逸，回填文章、记录与三个索引；实现阶段未要求提交推送；2026-10-26用户追加授权发布实验仓库、补齐固定链接并提交推送文稿。

## 身份与阶段

设计/计划位于实验仓库 docs/superpowers/specs/2026-10-26-exception-flow-design.md 与 plans/2026-10-26-exception-flow.md。独立projects/exception-flow，原始产物在ignored artifacts/exception-flow；公开记录evidence/2026-10-26-exception-flow。基线Git46ac71c仅含前轮类型初始化实验。本轮固定提交为 [f5ba510](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/f5ba510382e170ac06d2fd7033db363e6c58f62e)，见[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/f5ba510382e170ac06d2fd7033db363e6c58f62e/evidence/2026-10-26-exception-flow/README.md)、[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/f5ba510382e170ac06d2fd7033db363e6c58f62e/evidence/2026-10-26-exception-flow/results.json)与[公开最小补丁](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/f5ba510382e170ac06d2fd7033db363e6c58f62e/patches/exception-flow-delay-active-imi.patch)。复现工程见[固定目录](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/f5ba510382e170ac06d2fd7033db363e6c58f62e/projects/exception-flow)。

自定义Unity2022.3.62f3_96770f904ca7、package9.0.3/runtime9.0.1、Windows x64 Development IL2CPP Debug。最终原始build0628a07c-8e75-40ed-bb91-c295c40414c7，隔离反事实c811dcee-5771-4a89-86f9-ed53d862be1d。两版43输入、最终自有AOT/hot DLL与4生成文件相同，各227产物；919运行时输入只改公开Interpreter_Execute.cpp建帧宏，新工程Library以外的旧运行时/仓库/产物不改。

## 观测与因果

四方向aa/hh/ha/ah，AOT进入hot用缓存委托、反射只建委托。四种抛出/初始化形状各3新进程、每进程两次尝试；两版96普通进程。原始45handled，3个hh/cctor-ret两次escape且finally0；反事实48handled且finally1。.cctor每进程仅执行一次。驱动exit0只是采集完成，不把原始escape计作语义通过。

原始类型初始化Player只读现场RunFailed活动EH0/callerEH1、偏移88在[88,136)。新最终样本每版3匹配PDB进程，共6现场/版；caller RunOwn token0x0600000A，原始活动imi等于PingRet缓存，activeEH0/callerEH3，偏移304在typed[48,328)、finally[48,448)内。反事实保持caller imi，EH3/3，相同偏移，PingRet缓存同样已准备。CIL call87在try[17,96)，不与转换后304直接比较。

反事实用nextImi临时保存准备结果，初始化成功才赋给活动imi；保留准备早于cctor的顺序，避免混入其他变量。当前路径的错误状态、缺失handler与最小干预后的恢复形成因果链，不声称所有异常或其他平台都修复。

每版另1native短void控制，实际停在InitClassStaticCtor、offset248，活动caller一致，解释了内联形状未触发错配。C# NoInlining不能单独证明这个版本的内部调用是否内联；不把void/int返回差异独立当成因果。

## 探索保留与边界

首次C#字符转义导致编译失败，日志保留未算Player样本；CDB换行生成错误经RED→GREEN回归后重采。早期9d11ae2f仅短void样本36组全通过，发现内联路径后新增明确分支排除内联并重建，探索记录保留于ignored目录，不计入最终矩阵。最终公开104份自有事件，原始日志/PDB/Unity C++/二进制不公开。

## 文稿与验证

异常处理与类型初始化为主，ECMA调用篇、源码起步/runtime篇只补对应边界和结果链接，三个索引及canonical计划回写。纠正异常对比篇“完全不走native”和无测量性能倍数。完整161项Python测试、725公开文件边界检查通过。独立最终审查确认原始哈希/重解析104份自有事件、同输入单点反事实与文章结论一致，无P1/P2问题。隔离候选Hugo v0.158.0构建1414页、零ERROR；八页500个站内链接/锚点通过；浏览器确认矩阵和原生状态表可读，索引可直达实测小节。实验仓库已提交推送，远端main SHA回读一致；文稿随本次文档提交交付，Pages部署与线上内容另行核验。
