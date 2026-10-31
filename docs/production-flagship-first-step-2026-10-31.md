# 客户端生产体系首期：入口与开发检查整理

> 2026-10-31。执行记录；篇级归属与状态仍由[工程实践规划](engineering-journey-column-plan.md#production-flagship-v1)维护。

## 授权范围与结构

作者确认推进首期第一步：整理总览、Missing、AOT 两个成熟案例；补开发期编译报错处理、鲁班与提交前本地 hook 的配置检查，以及技术深读入口。沿用既有文章身份，不增加主线篇数，不实现或修改真实项目的检查脚本，不提交发布。

| 文章／入口 | 本轮结构与职责 |
| --- | --- |
| 总览与栏目页 | 生产交接与本人职责 → 六篇精选主线（性能仍为提纲）→ 编译／配置开发期检查 → 三个深读入口 → 选读 |
| Missing | 保留历史演进；补本地与共享副本的证据差别、依赖提交的边界和资源身份深读 |
| AOT | 保留历史根因与修复；补编译成功、产物生成、目标 Player 实际消费之间的连接及固定版本深读 |
| 客户端／服务端编译 | 检查输入 → 失败阶段 → 相关依赖与处理者 → 修正／共享版本复检 → 运行验证；区分案例事实与教学排查方法 |
| 配置检查 | 配置正确性的层次 → Luban 定义与校验 → 提交前本地 hook → 报错修正与复检 → 共享版本和实际消费；配置一致性原文只做衔接 |
| 编译错误技术选读 | 修正首条错误必为根因、运行错误归为编译错误、asmdef 依赖与目录范围的过度概括；不扩成整套编译系列重写 |

上述为已有文章的修订提纲，按已确认方案直接执行。既有正文事实保留，用户新补充和官方机制分别标注。

## 证据与待核项

- 作者本轮确认：配置使用鲁班，提交前本地 hook 为 TortoiseSVN 客户端钩子；代码编译由 Jenkins Job 做及时检查，错误在群里提醒，减少持续阻碍其他人。钩子失败是否阻断提交、实际脚本与参数仍未确认，正文不认领强制门禁。
- 历史 Missing 与 AOT 事实沿原文和主规划；不新增真实事故、量化收益或重跑结果。
- Luban 官方仓库 README 目前指向 `www.datable.cn` 文档站；本轮查阅 5.x 数据校验文档，不能据此推定历史项目版本或启用了全部校验器。正文不提供未经项目版本核对的执行命令。
- TortoiseSVN 官方说明用于区分客户端 hook 与服务端准入；本地配置、等待及错误传播影响拦截行为，官方能力不替代项目实现确认。
- Unity 2022.3 公开手册用于资产身份、程序集依赖与平台编译条件；不引用私有 C++ 引擎源码。
- HybridCLR 深读保持原文固定版本与实验边界；工具链文章的实验仓库链接固定仍待完成，不将它作为历史 AOT 事故的复现证明。

资料入口：

- [Luban 官方仓库](https://github.com/focus-creative-games/luban)
- [Luban 5.x 数据校验器](https://www.datable.cn/docs/quality/validators)
- [TortoiseSVN 客户端 hook](https://tortoisesvn.net/docs/release/TortoiseSVN_en/tsvn-dug-settings.html#tsvn-dug-settings-hooks)
- [Unity 2022.3 Assembly definitions](https://docs.unity3d.com/2022.3/Documentation/Manual/ScriptCompilationAssemblyDefinitionFiles.html)
- [Unity 2022.3 Asset Metadata](https://docs.unity3d.com/2022.3/Documentation/Manual/AssetMetadata.html)

## 检查记录

- 阅读布局回归：`python -m unittest discover -s tests -p test_reading_layouts.py -v`，19 项通过。首次沙箱无法启动本机 Hugo，获执行权限后按原命令通过；未改测试或布局文件。
- Hugo：`hugo --gc --minify --destination .tmp/flagship-first-step-publish-check`，1441 页构建成功。
- 生成页检查：`python scripts/check-reading-pages.py --public .tmp/flagship-first-step-publish-check`，1375 个阅读页，0 errors。
- 本次 10 个既有文件的修改前快照保存在 `.tmp/flagship-first-step-20261031/before.json`；相对于快照检查站内 relref 目标均存在，Missing 原六段历史过程与 AOT 主体历史／验证建议逐字保留。
- 浏览器查看本地总览、开发检查表格与配置正文，验证总览锚点、总览 → 鲁班 → 客户端编译的实际跳转；桌面阅读样式正常。本次未修改样式，未做额外窄屏或全设备视觉矩阵。
- Git diff 检查发现 Windows 编辑产生的 CRLF 差异，限定本轮 11 个文件恢复 LF；不调整仓库全局 Git 配置。

本次结果是本地文章与阅读路径整理。未运行游戏工程、鲁班、项目 hook 或设备实验；未提交、推送或核验线上部署。性能仍为提纲，配置到验收的同次事件仍待取材，钩子阻断与 HybridCLR 固定提交链接保留为明确待办。
