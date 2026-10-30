# P4 工作模型实验

对应 [VM-04 正文](../../../content/delivery-engineering/version-management-04-p4.md)。区分 Workspace 磁盘内容、打开状态、pending Changelist、Shelf、Depot 已提交内容与 have list。

## 前置与运行

需要 Windows PowerShell 5.1、PATH 中的 `p4.exe` 和匹配版本的独立 `p4d.exe`。本次客户端、服务端均为 `2026.1/3089746`（NTX64，2026/10/07）。服务端取自[官方目录](https://ftp.perforce.com/perforce/r26.1/bin.ntx64/)，与同目录 `SHA256SUMS` 核对：

```text
p4d.exe SHA256
EEE7AC13927872282BCB3E4B037AF960F2EC0905A1E6747CB343E77AC1E7091D
```

二进制不纳入仓库。请自行准备匹配客户端的服务端并核对来源；本次存放在忽略目录 `.tmp/tools/p4-2026.1/`。从仓库根目录执行，输出目录须为新的 ASCII 路径：

```powershell
powershell.exe -NoProfile -File examples/version-management/p4-work-model/run-lab.ps1 `
  -LabRoot E:/workspace/TechStackShow/.tmp/p4-work-model-new `
  -EvidenceDirectory E:/workspace/TechStackShow/.tmp/p4-work-model-new-evidence `
  -P4dPath E:/workspace/TechStackShow/.tmp/tools/p4-2026.1/p4d.exe
```

将路径替换为自己的位置。脚本拒绝复用目录；不注册 Windows 服务，不修改全局配置。每条业务命令明确指定回环端口、`vm-lab` 用户和 `vm-a` / `vm-b` 客户端。`finally` 优先 `admin stop`，失败时仅停止自己启动的进程；receipt 记录停止结果，停止失败不能报告成功。

## 初始化的实际边界

2026.1 的裸数据根不能照搬旧版“直接启动后自动建用户”的脚本。本实验先用官方 [p4 init](https://help.perforce.com/helix-core/server-apps/cmdref/current/Content/CmdRef/p4_init.html) 建立 **personal server**，再在服务未运行时以 `p4d --reset-password vm-lab -s` 初始化合成用户口令和权限，随后显式启动回环 TCP 服务。离线操作只针对此次新建 `.p4root`。[p4d 离线命令说明](https://help.perforce.com/helix-core/server-apps/p4sag/current/Content/P4SAG/appendix.p4d.html)

这不是团队服务器安装方案。记录显示 `serverServices local`、`security=4 (default)`、`dm.user.noautocreate:1 (configure)`，后者来自 personal server 初始化。使用一个合成超级用户操作两个工作区，没有验证用户之间的权限隔离。随机口令仅在内存、实验数据库和本地 ticket 流程中使用，公开 stdin 标为 `[REDACTED]`；数据库、ticket、日志都保留在忽略的实验根目录中。

## 已记录结果

2026-10-30，**19 项检查通过，自己启动的 p4d 已停止**。

- [receipt.json](evidence/2026-10-30/receipt.json)：工具版本、二进制摘要、实际配置、内容快照、检查与停止状态。
- [commands.json](evidence/2026-10-30/commands.json)：逐条命令、退出码与写文件动作；`[LAB]`、`[INVOCATION]` 替代本地路径。

| 阶段 | A 磁盘 damage / charge | Shelf | Depot | B 磁盘 |
| --- | --- | --- | --- | --- |
| 基线 | 100 / off | 无 | 100 / off，#1 | 100 / off |
| 搁置后继续编辑 | 80 / on | 80 / off | 100 / off，#1 | 100 / off |
| B unshelve | 80 / on | 80 / off | 100 / off，#1 | 80 / off，打开 edit |
| B revert、A 删除 Shelf 后普通 submit | 80 / on | 已删除 | 80 / on，#2 | 100 / off |
| B sync | 80 / on | 无 | 80 / on，#2 | 80 / on |
| B 磁盘改为 70 并 reconcile | 80 / on | 无 | 80 / on，#2 | 70 / on，打开 edit |

本次编号 Changelist 为 2，文件新修订也为 #2；两者恰好同号，含义不同。脚本解析实际创建的 Changelist，不硬编码编号。

## 调试发现与复跑处理

早期尝试分别遇到 PowerShell 函数名 `P4` 与可执行文件同名、未初始化用户无法认证的问题，已改为 `Lab-P4` 并采用上述官方初始化路径。随后一次复跑在同步后同一秒内进行等长改写，`diff -se` 能识别差异而 `reconcile -n -e` 返回无待协调文件；仅改变该教学文件的修改时间后，预览能识别 edit。

因此脚本对这个教学改写显式将文件修改时间增加 2 秒，并在 commands 中记录 `separate_fixture_mtime_from_sync`，再检查预览与实际 reconcile。这里保存的是这一稳定条件下通过的运行；失败及对照现场保留在本地 `.tmp/`，没有把失败尝试算作通过。未证明所有 P4 版本、文件系统或时间精度下均会出现同样行为。

## 验证范围

同机回环 TCP、personal server、一个用户、两个普通 client view、一个非 Unicode ASCII 文本文件；客户端 `noallwrite`、`noclobber`、`LineEnd: unix`。只研究普通 submit，不使用 `submit -e` 从 Shelf 直接提交。没有 TLS、生产安全配置、外网、多用户审批、P4V、Streams 协作、锁、二进制资源或引擎验证。
