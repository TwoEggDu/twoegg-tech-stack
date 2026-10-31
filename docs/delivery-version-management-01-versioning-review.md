# 版本号设计：身份与兼容关系审校记录

日期：2026-10-31。对应[原文](../content/delivery-engineering/delivery-version-management-01-versioning.md)与[交付工程计划 V04](delivery-engineering-column-plan.md#v04--版本与分支管理)。本文件为执行记录，不新增文章、URL 或篇号。

## 范围与复用

按用户确认，原位补清版本标识、构建身份与兼容关系，修正版本语义与更新方式的绑定，核对平台字段与编号公式。保留 title／slug／date／tags／series／weight、系列与交付元数据及原 H2 相对位置；将误导性的“四段结构”节名改为“版本语义与构建身份”，新增 lastmod 并更新 description。所有编号、清单与情景为教学设计，没有新增作者亲历或项目收益。

复用版本控制模型的候选／提交关系，以及跨端版本协调的兼容矩阵、候选与实际消费验收。不重讲三种 VCS，不将 VM-09 与更新发布系列中的提纲当作已成稿正文，不实现发布器或构建系统。

本次只写原文、交付工程计划新增的 V04.1 审校小节及本记录。计划已有其他修订全部保留，不纳入本次；不改根路由、索引、缺口表或其他文章。

## 官方资料与条件

| 内容 | 官方依据 | 本次结论与限制 |
| --- | --- | --- |
| SemVer 语义与排序 | [SemVer 2.0.0](https://semver.org/spec/v2.0.0.html) | 先声明 API；Major／Minor／Patch 表达对应变化，不强制选择商店／热更或支持固定历史范围；预发布标识与平台维度分开，构建元数据不参与优先级比较 |
| Apple 发行与构建版本 | [CFBundleShortVersionString](https://developer.apple.com/documentation/bundleresources/information-property-list/cfbundleshortversionstring)、[CFBundleVersion](https://developer.apple.com/documentation/bundleresources/information-property-list/cfbundleversion) | 分别为三段与一至三段点分整数，不能直接承载括号、平台后缀或完整 SemVer。只核对字段契约，不宣称 App Store 提交、上架或安装验收通过 |
| Android 字段与分配 | [Version your app](https://developer.android.com/studio/publish/versioning) | versionName 是展示字符串，versionCode 为内部正整数；核对降级防护、发布顺序、Play 2100000000 上限与已用编号约束，多 APK 有专门条件，不概括成跨应用／轨道的简单全局顺序 |
| 构建与兼容证据 | 上述规范及已有版本控制／跨端协调正文 | 身份命名空间、来源记录、产物核对、兼容矩阵与实际消费分开；映射与分配建议是工程设计，不声称平台自动提供这些能力 |

Apple 网页工具只能取得 JavaScript 入口，Markdown 链接在该工具中因 content-type 无法解析；改用标准 HTTPS 读取相同官方 `.md` 正文，缓存在 `.tmp/versioning-01-20261031/official/`。微信官方上传／CI 文档未能取得可核验正文，未补造字段与格式规则；删除原文笼统的“version 字符串无强制格式”，正文保留按所用宿主另核对的边界。Apple 上传帮助页亦未取得可用正文，未据其推导发布接受规则。

公式采用项目自定的教学范围：M、m 为 0～99，p 为 0～999，b 为 0～99，排除全零；不把该范围当作平台要求。碰撞反例、最大值、反解、拒绝条件和发布顺序分别检查，不能用算术通过替代构建或商店验收。独立单调分配是另一种项目方案，未实现并发、重试或分配服务。

## 验证与未核验范围

- Python 公式核对通过：80 组非全零边界元组往返且无碰撞、10 组全零／负数／越界输入拒绝、固定种子 20261031 的 10000 组随机元组往返；最大值为 999999999。另确认原无限范围公式的碰撞值 10200401，以及编码合法但低于既有发布编号的反例。检查对象为数学公式和教学范围，不是生产分配器或商店接口。
- 静态核对：除 description 与新增 lastmod 外，frontmatter 保持；原 H2 相对位置保持，重命名章节保留原锚点；正文与计划维持原 LF。差异空白检查通过，四个站内复用目标均存在。
- 本地回归：`python -B -m unittest discover -s tests -p test_reading_layouts.py -v`，19 项通过。
- 最终构建：`hugo --gc --minify --destination .tmp/versioning-01-20261031/publish-check`，退出码 0，生成 1443 页；补原书签兼容锚点后已重建。
- 最终生成页：`python -B scripts/check-reading-pages.py --public .tmp/versioning-01-20261031/publish-check`，检查 1377 个阅读页，0 错误。
- 目标 HTML 检查：三张表格、公式数值与边界已生成，原 12 个 H2／H3 锚点全部保留；四个复用链接可达、四个官方链接完整，无残留 shortcode，DATA-TODO 不显示为正文。本次未改样式／模板，未做浏览器视觉验收。
- 范围核对：暂存区与任务前一致；本次只写正文、计划新增 V04.1 节和本记录。工作区另有性能正文、入口与规划的同期更新，保留其内容，不纳入本次。
- `.tmp/versioning-01-20261031/` 保存原快照、计划既有差异、本次单文件增量、官方缓存及检查结果，供后续精确区分提交范围，不作为发布产物。
- 没有真实构建、包内字段提取、商店上传、安装升级、双端兼容或符号匹配实验；DATA-TODO 保留这些材料缺项。原文设计不写成团队已运行能力。
- 本次不提交、不推送，其他改动保留。
