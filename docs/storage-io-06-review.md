# 存储与 IO 06 修订与验证记录

日期：2026-10-31。对应[既有正文](../content/engine-toolchain/storage-io-06-profiling-with-fio-iostat-perfetto-instruments.md)与[系列 plan](storage-io-foundations-series-plan.md)。这是执行记录，不新增系列或篇级目录。

## 范围与复用

按用户确认原位修订 IO-06，保留标题、slug、系列、篇号和 weight。增量是观测口径、同一任务的证据关联、按问题选择工具，以及相关命令和解读条件。仅修改本篇、系列 plan 的 IO-06 状态与相关范围说明，新增本记录；不修改既有机制文章、根路由、基础与案例对照表及其他工作区改动。

复用 IO-03 的文件系统／缓存、IO-04 的请求路径、线程调度、内存记账、资源可用链。相应正文用于解释机制，本篇负责如何记录、关联与验证，不另开同义文章。

## 修正与官方依据

| 位置／原问题 | 本次处理 | 官方依据与适用边界 |
| --- | --- | --- |
| 应用等待、设备统计、资源可用混用 | 分对象、起止、单位、窗口、聚合；不相加不同层耗时，不从设备均值推 P99 | 具体工具定义见下；任务阶段与 Ready 为本文建议的观测约定 |
| marker 只有阶段名字 | 增加 run／task／request／依赖身份、输入版本、实际字节、结果状态、时钟域；区分系统完成与应用观测结果 | [Perfetto 时钟同步](https://perfetto.dev/docs/concepts/clock-sync)；身份与阶段模型为工程建议，非引擎既有接口 |
| fio 当硬件天花板、三项配置未隔离 | 限定 Linux 普通文件、psync／QD1；stonewall 串行、预备实际文件、readonly 与禁止自动建文件；记录 engine 与实际深度 | [fio 官方手册](https://fio.readthedocs.io/en/latest/fio_doc.html)：readonly、allow_file_create、stonewall、direct、iodepth、输出定义；检索页面标为 rev. 3.42，实际执行以安装版本为准 |
| fio 吞吐／延迟与游戏耗时混用 | 解释 clat／lat，分开吞吐与 Ready；顺序大块／随机小块比较不能称随机性单因素结果 | fio 官方手册的 I/O depth、Interpreting the output；未运行 fio |
| iostat 被当作应用请求证据 | 限 Linux sysstat；显式采样间隔／次数、时间与单位，核对设备映射；修正合并后完成请求、请求粒度、await、aqu-sz 和 util 的解释 | [sysstat 官方 iostat 手册源码](https://github.com/sysstat/sysstat/blob/master/man/iostat.in)，页面手册日期 AUGUST 2026；本机版本、内核统计与设备覆盖仍需目标机核对 |
| Perfetto 被当作自动因果图 | UI 入口限定 Android 11+ 及调试连接；标明旧系统服务／路径条件、数据源缺口、线程状态和依赖关联 | [系统采集](https://perfetto.dev/docs/getting-started/system-tracing)、[高级 Android 采集](https://perfetto.dev/docs/learning-more/android) |
| 跨线程沿用同步 marker、空白等于未工作 | 同线程同步 section；跨线程用异步配对及不冲突 cookie；Java 同步／异步接口分别注明 API 18／29，其他 API 单独核对 | [Android Trace](https://developer.android.com/reference/android/os/Trace)、[Perfetto ATrace 插桩](https://perfetto.dev/docs/getting-started/atrace)、[缓冲与丢失](https://perfetto.dev/docs/concepts/buffers) |
| Apple 旧模板清单外推所有目标，Directory I/O 当查找计数 | 明示归档 Mac 指南的 2016 范围；按当前 Xcode／OS／目标检查模板与权限；区分目录修改与路径查找，模拟器不代真机存储 | [Apple 归档 Mac I/O 指南](https://developer.apple.com/library/archive/documentation/Performance/Conceptual/power_efficiency_guidelines_osx/MinimizingIO.html) |
| 文件活动与 CPU 重叠直接判后处理，缺少请求身份 | 加 signpost／任务映射与区间配对，注明 Time Profiler 采样不能精确计每次调用 | [Recording Performance Data](https://developer.apple.com/documentation/os/recording-performance-data)、[beginInterval](https://developer.apple.com/documentation/os/ossignposter/begininterval(_:id:))、[Determining execution frequency](https://developer.apple.com/tutorials/instruments/determining-execution-frequency)；前后两页另读取 Apple 官方文档 JSON 正文核对 |
| 固定 fio → iostat → 时间线，套模式直接下结论 | 改为按问题选入口，观察／假设／反证／对照分开；以同输入、同 Ready 和代价指标复测，再讨论管线沉淀 | 工程建议与未执行的验证设计，无项目根因或收益实测 |

## 验证与限制

- 官方资料已核对，正文就近引用。公开文档核对不代表目标设备运行验证；fio 官方页面经网页工具读取，直连下载返回 403，未据此宣称完成外站所有锚点的自动检查。
- 未运行 fio／iostat，未采集 Android／Apple trace，未确认任意目标 Xcode 的 instrument 可用性；正文命令为有条件的示例，保留 DATA-TODO 等待真实样本。
- 模板回归：`python -B -m unittest discover -s tests -p test_reading_layouts.py -v`，19 项通过。
- 本地构建：`hugo --gc --minify --destination .tmp/io06-revision-20261031/publish-check`，退出码 0，生成 1443 页。
- 生成页检查：`python -B scripts/check-reading-pages.py --public .tmp/io06-revision-20261031/publish-check`，检查 1377 个阅读页，0 错误。
- 目标 HTML 已检查：标题与原路径保留，系列导航为第 06 篇／共 06 篇；5 张表格、命令块与 5 个既有机制文章链接均已渲染，无残留 shortcode，DATA-TODO 未显示为正文。没有修改样式或模板，本次未做浏览器视觉验收。
- 差异检查：正文 `git diff --check` 通过；本次写入仅正文、系列 plan 的 IO-06 状态／复用说明和本记录。原有未提交内容及暂存区保留。系列 plan 中同期出现的 IO-01 状态说明更新来自其他工作，原样保留，不计入本次修改。
- 2026-10-31 用户授权仅提交并推送本次正文、系列 plan 增量和本记录；提交／推送状态以 Git 历史与远端回读为准，线上部署未验证。
