# 类型初始化与首次调用综合实验记录

日期：2026-10-26。用户授权沿用 hybridclr-runtime-lab 和已验证环境，完成实验并回填三篇文章、三个索引。实现阶段未要求提交或推送；2026-10-26 用户追加授权发布实验仓库、补齐固定链接并提交推送文稿。

## 设计与当前身份

实验仓库设计与计划：`docs/superpowers/specs/2026-10-26-type-initialization-design.md`、`docs/superpowers/plans/2026-10-26-type-initialization.md`。独立 `projects/type-initialization`，原始产物在忽略的 `artifacts/type-initialization`。基线74da063属于前轮资源实验。本轮已发布固定提交 [46ac71c](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/46ac71c74cd068bc177d653dd143b7bb9dc482bd)，见[实验工程](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/46ac71c74cd068bc177d653dd143b7bb9dc482bd/projects/type-initialization)、[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/46ac71c74cd068bc177d653dd143b7bb9dc482bd/evidence/2026-10-26-type-initialization/README.md)与[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/46ac71c74cd068bc177d653dd143b7bb9dc482bd/evidence/2026-10-26-type-initialization/results.json)。

固定本机自定义 Unity2022.3.62f3_96770f904ca7、package9.0.3/runtime9.0.1、Windows x64 Development IL2CPP Debug/Minimal stripping。最终build_id为5b177115-e8ae-4e58-8808-2a0bc54b092a，43输入/227产物哈希绑定；两份场景仅命名空间不同。最终裁剪Probe.AOT与随包HotUpdate的标志/.cctor/自有CIL已只读提取，热更程序集排除AOT。

## 实际结果与差异

四组问题增加失败外层捕获对照，共30个独立普通Player。strict、relaxed、generic两侧各三轮完成。严格初始化在首次调用标记后、Ping方法体前；Relaxed也取相同的允许时机；int静态值42/43、string82/83，各构造一次。

AOT内部catch三轮各两次TypeInitializationException，构造体一次。热更内部catch三轮均在第一次初始化时逃逸到AOT反射边界，退出2，分类escaped_first_access，未到第二次访问，不能列为语义通过。两侧共用AOT外层catch的补充路径各三轮均捕获两次反射包装异常，构造体只执行一次。合计27完成/3差异。

两个最终DLL的RunFailed自有CIL：call偏移30，try[29,47)，catch为System.TypeInitializationException。差异不能归因于源码调用放在try外。没有修改上游代码；精确根因、普通抛出对照及其他版本仍待补。

同构建另取hot strict/failed两个匹配PDB进程：Ping Transform1，先于.cctor执行；.cctor Transform1/Execute入口1且含Runtime::ClassInit栈；结束时同一Ping MethodInfo缓存非空。strict自有Ping事件2，failed0。原生Execute函数入口不等于解释器内部调用次数。

## 探索与边界

初版d70cd30b首次发现异常逃逸，原始日志与自有输入保留在ignored exploration-d70cd30b；为继续观察第二次访问，增加共用外层捕获路径后重新构建最终Player并重跑矩阵。最终元数据读取器补充CIL异常区间时核对Player字节未变，不把读取器更新当新Player样本。

公开记录为 `evidence/2026-10-26-type-initialization/README.md`、results.json和32份自有事件。原始日志/PDB/二进制不公开。单线程/固定版本与平台，不宣称性能、锁竞争、循环初始化或全部异常路径已验证。

## 文稿与验证

回填类型初始化、源码阅读起步和IL2CPP runtime三篇；三个索引链接同一主实验小节，三份canonical计划更新状态。完整144项Python测试通过，563个公开文件边界检查通过。独立审查发现2项P2验收器缺口，均以RED→GREEN回归修正：允许Relaxed在唯一环境事件之前初始化，拒绝.cctor执行早于转换。原始30普通＋2native记录哈希与重解析结果不变，不增加Player次数。隔离候选Hugo v0.158.0构建1414页、零ERROR；六页428个站内链接/锚点通过。浏览器确认五行结果表、泛型名称渲染及索引直达实测小节。实验仓库已提交推送，远端main SHA回读一致；文稿随本次文档提交交付，Pages部署与线上内容另行核验。
