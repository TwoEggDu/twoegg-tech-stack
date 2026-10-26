# HybridCLR 资源挂载实验执行记录

日期：2026-10-26。用户确认沿用 hybridclr-runtime-lab 和已验证的自定义 Unity 固定组合，完成代码基线、AssetBundle Prefab 与失败对照，回填 HCLR-4 和三个索引。初始实现阶段未包含提交或推送；2026-10-26 用户追加授权按实验仓库、文稿、Pages 的顺序交付。

## 验证问题

1. DLL 可执行与资源组件可恢复是否分别成立？
2. 同一热更 MonoBehaviour 的数值、文本、子 Transform 和 TextAsset 引用能否恢复？
3. DLL 未在资源使用前装载、启动程序集清单缺项时，实际在哪个阶段出现差异？

## 设计与证据

实验仓库设计与执行记录：`docs/superpowers/specs/2026-10-26-resource-mounting-design.md`、`docs/superpowers/plans/2026-10-26-resource-mounting.md`。
新样本仅写 `projects/resource-mounting`，产物与原始日志在忽略的 `artifacts/resource-mounting`。保留 call-chain、泛型、GC 与线程的旧源码和记录。

Player 为 Windows x64 Development／IL2CPP C++ Debug／Minimal stripping，固定自定义 Unity2022.3.62f3_96770f904ca7、package9.0.3/runtime9.0.1。正向资源为本地 AssetBundle，保留 TypeTree；直接 Resources Prefab 作为额外观察，不预设其错误文案。

每组新进程。采证实际类型/程序集、序列化值与引用身份、LoadAsset/Instantiate 阶段和 Awake/OnEnable/Start/OnDisable/OnDestroy。清单缺项组只修改复制出的 Player 的 ScriptingAssemblies.json；其他运行文件必须与原包哈希相同。

## 进度

验收器先RED后GREEN；初版16项通过，增加阶段/引用身份和混合编码日志回归后21项通过。第一次Player编译完成，但回执读取因编译器混合编码中止；原始日志保留。修正为在原始字节上检查ASCII成功标记，再重新构建。运行结果待实际采证后填写，不将单测或构建成功当作挂载链路通过。

## 完成结果

最终build_id为7adc9fed-3322-4778-8a64-c7e6bb2018eb。code/bundle各三进程恢复类型、值、引用及完整生命周期；late/unregistered/resources各三进程缺组件。清单缺项组DLL/Ping仍成功；副本仅启动JSON有差异。共有46输入、256产物哈希绑定；118测试通过。资源名验收假设已根据Unity实际保存名称纠正并保留探索日志。公开15份自有事件与原始日志重解析摘要一致。未采placeholder原生对象身份，不作GC/性能/跨平台结论。

本轮正文、三个索引及canonical计划同步实验结果。实验仓库已提交并推送 [74da063](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/74da063e0fe2a4cbac894acfa77d3b0df4074356)，远端 main SHA 回读一致；[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/74da063e0fe2a4cbac894acfa77d3b0df4074356/evidence/2026-10-26-resource-mounting/README.md)与[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/74da063e0fe2a4cbac894acfa77d3b0df4074356/evidence/2026-10-26-resource-mounting/results.json)使用固定链接。基线64938d5只包含前轮样本。

## 最终验证

独立最终审查确认输入、产物、原始日志、公开事件与副本控制一致，指出两项P2验收器缺口：阶段时序和资源计数。13项失败回归已RED→GREEN修复，完整测试131/131；15份原始日志哈希未变，重解析仍为6 restored、9 missing_at_asset_and_instance，没有新增Player或修改运行时源。负向意外恢复保留，其他阶段转换/零missing槽标needs_investigation。

隔离候选Hugo v0.158.0构建1414页、零ERROR；四个发布页263个站内链接/锚点通过，固定旧URL不变。浏览器实际渲染478×632流程图无错误，5行结果表可读，索引点击可直达本轮小节。文稿已补齐实验固定链接；随本次文档提交交付。Pages 部署及线上四页在推送后另行核验，不以本地构建代替部署结果。
