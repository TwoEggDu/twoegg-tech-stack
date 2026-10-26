# async／UniTask 跨边界实验与补稿记录

用户授权沿用实验仓库和当前环境，完成实验后补现有文章与索引；实现阶段未要求提交推送；2026-10-26用户追加授权发布实验、文稿与三个索引，并补齐固定提交链接。

## 编辑定位

案例补强：HCLR17集中放问题、调度模型、矩阵与native路径；14保留历史事实与边界，23更新场景覆盖；三索引只提供阅读入口，不重复展开。旧slug、标题、编号和正文篇数不变。

## async／UniTask 跨边界验证（2026-10-26）

固定自定义 Editor／v9／Windows x64，额外锁定 UniTask2.5.10/7c0f199。独立 projects/async-unitask，build_id c2a81f08-12d1-4f70-b333-d4bd26705388，44输入／264产物，919runtime与原始prepared一致。60普通Player：AOT class、hot class/struct的同步、两次跨帧、前后异常按实际事件完成；30结果／30预期异常，finally各一次。12匹配PDB进程验证hot MoveNext三Execute/一Transform及恢复缓存复用。

同一Player的metadata on/off均能运行；class命中native引用共享builder，struct命中带FullySharedGenericAny名字的native入口，补充不强制替换。样本显式preserve整个UniTask，off不等于无可用native实例；无metadata缺失失败对照，也不宣布历史Android根因闭合。HCLR17集中补结果，14保留历史边界与回链，23同步场景覆盖；三索引复用入口，不新增正文。本轮固定提交为 [b34ac10](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/b34ac104fa72504d1d2a480ca936ea57a7a0fb22)，见[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/b34ac104fa72504d1d2a480ca936ea57a7a0fb22/evidence/2026-10-26-async-unitask/README.md)与[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/b34ac104fa72504d1d2a480ca936ea57a7a0fb22/evidence/2026-10-26-async-unitask/results.json)。实验已提交推送；文稿随本次文档提交交付，Pages另行核验；执行记录见[async实验](./hybridclr-async-unitask-outline.md)。

## 证据与验收

实验仓库设计/计划见docs/superpowers/{specs,plans}/2026-10-26-async-unitask*.md；详细记录evidence/2026-10-26-async-unitask/README.md与results.json，72份自有事件，普通与原生分别重解析、哈希绑定。真实PDB、原始日志、生成代码和第三方源码不进入公仓。

工具探索：混合编码日志用ASCII bytes marker验收；metadata读取器字段与引用枚举在PE释放前物化，并修正嵌套awaiter名称，真实C# fixture经RED→GREEN；CDB MASM条件和模块限定类型修正后重新采集。探索与诊断不计最终进程。

完整 182 项 Python 测试、856 项公共文件边界检查通过；60 普通＋12 native 原始日志与公开事件均重解析、哈希一致。独立审查发现的 P2（额外 UniTask 解释转换／补充体读取未被拒绝）已由两个 RED→GREEN 破坏测试和零事件断言修正，复核对 12 份真实日志分别注入两类事件的 24 次破坏均被拒绝，无剩余 P1/P2。隔离候选 Hugo v0.158.0 构建 1414 页、零 ERROR；六页 355 处站内链接／锚点通过，浏览器已确认 Mermaid 和结果表可读。实验仓库已推送并核验远端；文稿随本次文档提交交付。
