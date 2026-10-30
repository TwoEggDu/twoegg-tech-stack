# SVN 工作模型实验

对应 [VM-03 正文](../../../content/delivery-engineering/version-management-03-svn.md)。以两个文本文件展示仓库全局修订、路径工作修订、最后修改修订和本地修改的区别。

## 运行

需要 Windows PowerShell 5.1，以及 PATH 中的 `svn`、`svnadmin`、`svnlook`、`svnversion`。从本仓库根目录执行，两个输出目录都必须不存在：

```powershell
powershell.exe -NoProfile -File examples/version-management/svn-work-model/run-lab.ps1 `
  -LabRoot E:/workspace/TechStackShow/.tmp/svn-work-model-new `
  -EvidenceDirectory E:/workspace/TechStackShow/.tmp/svn-work-model-new-evidence
```

请将绝对路径改为自己的仓库路径。脚本新建本地 `file:///` 仓库、A/B 工作副本和一个明确取得 r3 的对照副本；使用独立客户端配置目录，不缓存认证，不删除或复用已有目录。脚本失败会退出并保留现场及失败 receipt。

## 已记录结果

2026-10-30，SVN / svnadmin `1.14.1`、PowerShell `5.1.19041.6456`，**19 项检查通过**。脱敏修正后的脚本又在新目录复跑通过。

- [receipt.json](evidence/2026-10-30/receipt.json)：版本、七个快照、检查结果。
- [commands.json](evidence/2026-10-30/commands.json)：逐条命令、退出码、写文件动作与输出；`[LAB]` 代替实验根路径。

| 阶段 | 仓库最新 | A 根目录 / damage / charge 工作修订 | svnversion | A 内容 damage / charge |
| --- | --- | --- | --- | --- |
| 基线 | 1 | 1 / 1 / 1 | `1` | 100 / off |
| A 提交 damage | 2 | 1 / 2 / 1 | `1:2` | 80 / off |
| B 提交 charge | 3 | 1 / 2 / 1 | `1:2` | 80 / off |
| A 只更新 charge | 3 | 1 / 2 / 3 | `1:3` | 80 / on |
| A 完整更新 | 3 | 3 / 3 / 3 | `3` | 80 / on |
| A 本地再改 damage | 3 | 3 / 3 / 3 | `3M` | 70 / on |

完整更新后，`damage.rules` 的工作修订为 3，最后修改修订仍为 2。干净 r3 对照副本的两份内容与 A 完整更新后的内容相同；A 最后留下的本地修改没有进入仓库。

## 验证范围

这是同机文件协议与普通文本实验。没有 SVN 网络服务、权限、冲突、分支、锁、externals 或引擎验证。干净、同一工作修订与工程可构建需要分别判断。本实验的数据和修订号只属于新建教学仓库。
