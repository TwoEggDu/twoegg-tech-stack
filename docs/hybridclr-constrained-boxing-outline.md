# constrained.／装箱调用实验与补稿记录

用户已确认方案并授权沿用现有实验仓库和环境；实现阶段不提交推送；2026-10-26用户追加授权发布实验、文稿与三个索引，并补齐固定提交链接。

## 编辑定位

沿用原理＋案例补强：类型系统负责值、箱内副本与身份；调用语义负责完整CIL和实验；AOT泛型篇负责可用实现选择，调用链篇负责准备与实际执行区分。三索引复用入口，4正文旧URL和主线篇数不变。根doc-plan不动。

## constrained.／装箱调用实测（2026-10-26）

独立projects/constrained-boxing，沿用固定自定义Editor/v9/Windows x64；build_id 9e084001-a41b-42c2-8fbd-62efc0d147d4，48输入／236产物、919原始runtime。72普通Player：69完成，3个cross/off/value在GenericCalls.ByValue<HotCounter>缺可用AOT实例处停止，补充最终匹配Probe.AOT metadata后恢复。ref on/off均命中native ByRef共享入口，不推广普遍免补metadata；cross/box只作非泛型语义控制。

27个匹配PDB独立进程：原21加value on/off各3。hot箱对象两次接口调用指向同payload，unbox.any副本this不同；override无自有接收者Box命中，fallback有PlainCounter Box；按值on进入ByValue解释帧，ref仍native。on/ref准备期也会读取ByValue body，不当作实际执行证明。四篇既有正文分工：类型系统维护副本身份、调用语义集中完整实验、AOT泛型与调用链篇回链实际路径；三索引复用入口，主线篇数及URL不变。本轮固定提交为 [5638131](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/563813195813cc2d618bdf301e12ab1af0556d79)，见[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/563813195813cc2d618bdf301e12ab1af0556d79/evidence/2026-10-26-constrained-boxing/README.md)与[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/563813195813cc2d618bdf301e12ab1af0556d79/evidence/2026-10-26-constrained-boxing/results.json)。实验已推送；文稿随本次文档提交交付，Pages另行核验。执行记录见[本轮记录](./hybridclr-constrained-boxing-outline.md)。

## 原始与公开证据

设计／计划在实验仓库 docs/superpowers/{specs,plans}/2026-10-26-constrained-boxing*.md。独立 artifacts/constrained-boxing 保存原始日志、PDB、编译产物；evidence/2026-10-26-constrained-boxing/README.md、results.json及99份自有事件可公开，逐项哈希与重解析。

工具真实编译fixture验证constrained.、box、unbox.any与T&签名；原生破坏测试拒绝错误token、payload布局、箱内别名、拆箱副本地址、额外native helper及off组补充体读取。普通矩阵发现按值失败后才追加6个native，早期21进程不重复计数；Player与运行时未改。

## 最终检查

完整206项Python测试、1016项公共文件边界检查通过；72普通＋27native＝99总进程/事件与原始哈希重解析一致。独立审查的两项P2已由错误assembly/方法/实参及override异址/缺TextLocal五个RED→GREEN用例修正，真实日志同类破坏均被拒绝，无剩余P1/P2。隔离候选站内7页440链接/锚点通过，浏览器结果表可读。实验已推送并核验远端，文稿随本次文档提交交付。

实现阶段候选基于共享仓库当时已提交基线54b971e，Hugo v0.158.0构建1423页、零ERROR；本轮7页440链接/锚点复检通过。期间无关提交不触及本轮11文件，原工作区其他改动均保留。
