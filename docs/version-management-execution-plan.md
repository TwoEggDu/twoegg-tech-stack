# 版本管理专栏执行计划

> 创建：2026-10-30。执行层文件；篇级目录与职责以 [series-plan](version-management-series-plan.md) 为准。
> 用户已确认：先批量补齐 SVN、P4 工具模型，落下计划后在本会话执行实验与正文。采用当前会话顺序执行；本轮不包含提交推送。

**Goal:** 完成 VM-03、VM-04 的隔离实验、证据、正文和系列状态同步。

**Architecture:** 每种工具使用独立脚本和新建实验根目录，记录命令、退出码、内容与工具状态；先执行断言，再将实际结果写入正文。复用 Git 实验的证据格式，不强行统一工具语义。

**Tech Stack:** Windows PowerShell、SVN CLI / svnadmin / svnlook / svnversion、P4 CLI / 独立 p4d、Hugo、Python 标准库。

**Spec:** [版本管理系列规划](version-management-series-plan.md)，VM-03、VM-04 现有提纲及本会话确认的命令行写法。

## 全局约束

- 实验输出根目录必须不存在；不删除或复用既有仓库，不修改用户的全局客户端配置。
- SVN 采用 `file:///` 的自建本地仓库；P4 服务端使用新建数据根、显式回环地址与端口，不注册 Windows 服务。若需下载，仅使用官方来源并核对校验和，二进制保留在忽略目录。
- P4 每条连接明确指定服务端、实验用户与工作区；结束时停止本次启动的进程。
- 脚本与证据用 UTF-8、LF；公开记录替换实验绝对路径和环境身份，不保存认证材料。
- 正文分别标注官方机制、教学数据、实际观察和未验证范围；不把同机实验写成生产、网络、引擎或审查平台验证。
- 只修改本轮相关内容与入口中的相关行，保留其他工作区改动。用户此前的提交授权不自动扩展到本批。

## 审阅重点

1. SVN 的工作修订与最后修改修订不是同一字段；不能以根目录一个编号代表全部路径。
2. 选择性更新可能保留混合修订；完整更新后的工作修订变化不等于每个文件内容都变化。
3. P4 Changelist 组织打开的文件，不把 pending 误写成独立保存了一份固定文件内容。
4. Shelf、工作区文件和 Depot 已提交内容要分别取证，尤其检查搁置后继续编辑的情况。
5. P4 服务只由本轮启动与停止，客户端不得误连已有环境；二进制缺失或权限失败不应阻断 SVN 工作。

## 第一批：SVN 与 P4 工具模型

### 任务 1：SVN 实验与正文

文件：

- `examples/version-management/svn-work-model/run-lab.ps1`
- `examples/version-management/svn-work-model/README.md`
- `examples/version-management/svn-work-model/evidence/2026-10-30/{commands,receipt}.json`
- `content/delivery-engineering/version-management-03-svn.md`

步骤：

- [x] 用 `svnadmin create` 建立新仓库，导入两个普通文本文件为 r1；创建 A、B 两个工作副本。
- [x] A 提交第一个文件为 r2，B 提交另一文件为 r3；分别记录仓库最新修订、路径工作修订、最后修改修订、内容与 `svnversion`。
- [x] A 先只更新一个文件，再完整更新；检查混合修订与最后修改修订的差异。
- [x] 新工作副本明确取得 r3，比较内容；保留一份本地未提交修改，验证仓库版本与本地修改分开表达。
- [x] 保存真实输出与断言结果，写 README 和正文。分支、冲突与引擎依赖只说明后续边界。

### 任务 2：P4 实验与正文

文件：

- `examples/version-management/p4-work-model/run-lab.ps1`
- `examples/version-management/p4-work-model/README.md`
- `examples/version-management/p4-work-model/evidence/2026-10-30/{commands,receipt}.json`
- `content/delivery-engineering/version-management-04-p4.md`

步骤：

- [x] 核对客户端、服务端版本与来源；按需准备官方独立服务端并保存二进制校验和。
- [x] 新建只监听回环地址的测试服务、实验用户和两个独立映射工作区；提交初始文件。
- [x] 创建编号 Changelist，打开并修改文件，搁置；继续编辑该文件，分别检查工作区、Shelf 与 Depot。
- [x] B 从 Shelf 取得内容，核对正式 Depot 未改变；A 选择性提交并核对 B 不会自动同步。
- [x] B 释放本实验打开状态后明确同步已提交版本，核对修订与内容；补一个磁盘改动与 reconcile 的小对照。
- [x] 停止本次 p4d，保存实际输出和断言结果，写 README 和正文。锁、Streams、审查平台与生产安全配置留后续。

### 任务 3：阅读入口、验证与回写

- [x] 先更新 canonical plan 为四篇正文、六篇提纲，逐工具标明验证边界，再同步首页与必要导航。
- [x] 检查证据字段、断言、正文输出和版本身份一致，代码及 JSON 不带尾部空白。
- [x] 执行阅读模板回归、Hugo 构建、生成页检查；核对生成页面中的标题、正文标识、章节关系和命令块。
- [x] 需要浏览器时使用现有预览接口检查；不为文章验证接管其他桌面应用。
- [x] 回写实际结果和未完成项，向用户交付正文与实验入口；不自动提交发布。

## 后续批次（VM-05、VM-06 已完成首轮实验与正文）

第二批：VM-05 的具体设计与结果见 [任务提交实验计划](version-management-05-experiment-plan.md)，Git 主线完成 33 项检查并写成正文，SVN/P4 补机制差异；VM-06 的 [分支合并实验](version-management-06-experiment-plan.md) 完成 41 项检查与正文，VM-07 资源协作尚未启动。第三批：VM-08～10，连接审查、构建基线与选型迁移，尚未启动。本文不复制篇级目录。

## 执行记录

- 初查：SVN CLI / svnadmin / svnlook / svnversion 为 1.14.1；已有 P4 客户端，独立服务端尚待准备。尚未执行本批实验。
- SVN：19 项检查通过，52 条命令或写文件记录、七个快照；脱敏修正后在另一个新根目录复跑同样通过。正式证据位于 `examples/version-management/svn-work-model/evidence/2026-10-30/`。
- P4：客户端与服务端均为 `2026.1/3089746`，官方 `p4d.exe` 的 SHA256 已与官方清单核对。首次裸根初始化遇到默认认证限制，改用 `p4 init` 创建 personal server，再离线为合成用户初始化口令，显式启动回环服务；实际 `security=4`、`dm.user.noautocreate=1`、`serverServices local` 已记录。这是独立教学环境，未部署团队服务。
- P4 调试：修正 PowerShell 同名函数递归；另一次复跑发现同秒等长磁盘改写可被 reconcile 预览漏检，以只改变修改时间的对照定位条件。正式脚本为该教学改写显式增加 2 秒修改时间并记录，随后 19 项检查通过，46 条命令或写文件记录，`server_stopped: true`。早期尝试现场归档在本地 `.tmp/version-management-p4-attempt-records/`，最终通过记录归入 `examples/version-management/p4-work-model/evidence/2026-10-30/`。
- 正文：VM-03、VM-04 从提纲转为正文，附命令、实际状态表、版本与未验证范围；两套 README 说明复跑方法与已知条件。先回写 canonical plan，再同步首页、工程与交付入口、阅读路径及根路由的系列状态，现为四篇正文、六篇提纲。其他工作区内容改动保留。
- 本地验收：18 项阅读模板回归通过；Hugo `0.157.0` 构建成功；1,373 个生成阅读页检查为 0 错误。JSON 通过解析、成功标识、19 项断言与认证字段脱敏核对，相关文件 LF / 尾部空白检查通过。默认沙箱曾无法启动已安装的 Hugo，改在允许的执行上下文后通过，不作为文章失败。
- 页面验收：原预览服务已停止，重启只监听 `127.0.0.1:13139` 的静态预览后，在后台新标签核对 SVN、P4 正文外观、目录、命令块和前后章节关系；首页显示四篇正文与六篇提纲。保留首页预览供阅读，未改站点模板；本批尚未提交、推送或线上发布。
- 结论边界：通过的是本地文本版本控制实验和页面检查；网络服务运维、权限隔离、审查平台、锁、完整引擎依赖与构建行为均未完成对应验收。下一批先进入 VM-05 任务提交，再设计分支、资源与审查实验。
- 交付授权补充：首轮验收后，用户明确要求提交并推送 SVN、P4 正文、实验证据与相关规划更新。交付采用精确文件范围，共享入口只纳入版本管理的状态行，其他专栏改动保留；VM-05 本轮只讨论实验设计，不执行实验或起草正文。
