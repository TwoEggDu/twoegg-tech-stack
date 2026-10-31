# 构建系统本质：完整输入与复现边界审校记录

日期：2026-10-31。对应[原文](../content/delivery-engineering/delivery-multiplatform-build-01-fundamentals.md)与[交付工程计划](delivery-engineering-column-plan.md)。本文件为执行记录，不新增文章、URL 或篇号。

## 范围与复用

按用户确认，原位补清完整构建输入、可重跑与可复现的区别及最小正反对照。保留 title／slug／date／tags／series／weight、系列与交付元数据、原 H2／H3 及相对顺序；新增 lastmod，更新 description。原四段模型保留，补输入消费证据和生成／平台步骤边界，收紧同 commit 必然同产物、固定质量阈值、固定归档月数及 CI 自动提供确定性的表述。

复用版本号设计的身份和映射，以及构建自动化的脚本、环境、缓存与归档职责；引用不代表本轮已审校邻篇全部内容。不将 VM-09 提纲当作已有正文，不新增 VCS 或 CI 课程。A／B／C／D、交接表与建议均为未执行设计，没有新增作者经历或工程收益。

本次只写正文、交付工程计划新增的 V07.1 审校节及本记录；计划已有修订保留，不视为本次新增。不修改根路由、索引、缺口表、自动化正文或其他文章。

## 官方核对与适用条件

| 内容 | 官方依据 | 处理与边界 |
| --- | --- | --- |
| 可复现与环境记录 | [Definitions](https://reproducible-builds.org/docs/definition/)、[Recording the build environment](https://reproducible-builds.org/docs/recording/) | 指定源码、相关环境、构建指令和主产物集合；可重跑、功能接受和字节一致分别报告。记录形式随生态变化，不承诺一份清单自动覆盖全部输入或保证可取得 |
| 时间与签名差异 | [Timestamps](https://reproducible-builds.org/docs/timestamps/)、[Embedded signatures](https://reproducible-builds.org/docs/embedded-signatures/) | 时间与归档元数据可能影响结果，统一时区不是统一时间戳；签名前、签名后与比较工具的范围分开，不以排除差异后的子集冒充原包复现。未推荐改写实际包、替换签名或修改系统时钟 |
| 包锁职责 | [Unity 2022.3 Lock files](https://docs.unity3d.com/2022.3/Documentation/Manual/upm-conflicts-auto.html) | 保存包解析结果，本地／嵌入包可变，不能将 lock 文件当作整套环境和实际内容归档；不建议手动篡改锁文件 |
| 缓存与回调 | [Unity 2022.3 Incremental build pipeline](https://docs.unity3d.com/2022.3/Documentation/Manual/incremental-build-pipeline.html) | Clean Build 与增量缓存有各自范围，自定义回调及其输入影响需核对；不等同全部缓存清空，不外推其他 Unity／资源框架／Unreal 的命令或能力 |
| 平台步骤和产物 | [Unity 2022.3 iOS](https://docs.unity3d.com/2022.3/Documentation/Manual/iphone-BuildProcess.html)、[WebGL](https://docs.unity3d.com/2022.3/Documentation/Manual/webgl-building.html)、[Android App Bundle](https://developer.android.com/guide/app-bundle) | Xcode 工程与应用构建分开，本地 Xcode 构建需 macOS；AAB 为发布格式，Play 生成并签署设备 APK。WebGL 输出不证明小游戏宿主适配完成，未新增微信工具、接口或上传规则 |

资料核对日期为本记录日期。规范与工具契约支持解释，不能代替目标工程的完整性、来源、可取得性与运行证据；四阶段图是职责示意，不保证固定串行顺序或所有 C# 后端均生成原生代码。

## 验证与未核验范围

- 静态核对：除 description 与新增 lastmod 外，frontmatter 保持；原 H2／H3 及相对顺序保持，正文和计划保留原 LF，差异空白检查通过。四个站内复用目标存在。
- 本地回归：`python -B -m unittest discover -s tests -p test_reading_layouts.py -v`，19 项通过。
- 构建：`hugo --gc --minify --destination .tmp/build-fundamentals-20261031/publish-check`，退出码 0，生成 1443 页。
- 生成页：`python -B scripts/check-reading-pages.py --public .tmp/build-fundamentals-20261031/publish-check`，检查 1377 个阅读页，0 错误。
- 目标 HTML：六张表格、各层结论与 A／B／C／D 设计正常生成；原 10 个 H2／H3 锚点全部保留，四个复用链接解析且目标存在，九个官方链接完整，无残留 shortcode，DATA-TODO 不显示为正文。没有改样式／模板，本次未做浏览器视觉验收。
- 范围核对：暂存区与任务前一致；本次只写正文、计划新增 V07.1 节和本记录，不将整份计划差异算作本次交付。最终快照比对另观察到 AI 课程 trace／eval 正文及其状态／系列计划的同期更新，均未触碰，不纳入本次。
- `.tmp/build-fundamentals-20261031/` 保存任务前快照、计划既有差异、本次单文件增量和检查结果，供后续限定提交范围，不作为发布产物。
- 未执行游戏工程构建、输入漂移注入、缓存／签名对照、字节比较、商店或设备功能验收；DATA-TODO 保留其材料缺项。不会把站点构建成功写成游戏工程可复现。
- 本次不提交、不推送，其他改动保留。
