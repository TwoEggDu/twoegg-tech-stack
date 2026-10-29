# Git 工作模型：暂存后继续修改

配套文章：[版本管理 02](../../../content/delivery-engineering/version-management-02-git.md)。

用一个 `skill.rules` 文本文件，核对工作区、暂存区、提交与远端之间的关系。实验只在新目录中创建一个本地裸仓库和两个独立仓库工作区，不使用生产仓库，也不连接网络。

## 执行

在 TechStackShow 根目录运行，输出目录必须尚不存在：

```powershell
powershell.exe -NoProfile -File examples/version-management/git-work-model/run-lab.ps1 `
  -LabRoot E:/workspace/TechStackShow/.tmp/version-management-git-20261029 `
  -EvidenceDirectory E:/workspace/TechStackShow/examples/version-management/git-work-model/evidence/2026-10-29
```

其他机器替换绝对路径；复跑时同时换两个输出目录。脚本拒绝覆盖已有目录，不删除既有文件。命令行需要支持 `git init --initial-branch` 和 `git switch` 的 Git；已验证环境见下表，不承诺其他环境已经通过。

脚本临时使用空的全局配置文件、禁用系统配置读取并采用空初始化模板；这些设置只对当前进程和子进程有效，退出时恢复。身份、换行和签名配置只写入实验仓库。不读取或修改用户的全局 Git 配置。

## 场景与检查

1. 基线：`damage=100`、`charge=off`，推送并创建接收方。
2. 修改 `damage=80` 并暂存，再将工作区中的 `charge` 改成 `on`。
3. 对照 `status --short`、两种 `diff`、暂存内容和 `HEAD` 内容。
4. 提交后核对未暂存修改仍保留，并查询远端仍为旧提交。
5. 推送后核对接收方的文件和本地远端跟踪引用仍未更新。
6. 在接收方 fetch，再核对其 `origin/main` 更新、`HEAD` 与文件仍未变化。
7. 接收方以 detached HEAD 方式取得指定提交，核对只含已提交修改；开发方仍保留后续编辑。

所有关键预期都用断言检查；任何 Git 非零退出码或断言失败会停止执行。失败时仍尽量保留已经取得的证据，不把部分结果写成成功。

## 本次实测

| 项目 | 结果 |
| --- | --- |
| 日期 | 2026-10-29 |
| Git | `2.53.0.windows.2` |
| PowerShell | Windows PowerShell `5.1.19041.6456` |
| 操作系统版本 | `Microsoft Windows NT 10.0.19045.0` |
| 关键检查 | 23 项通过 |
| V0 | `516755fb6f3e47f72f8be0680011b193a6be0971` |
| V1 | `4948ff5d011831377760eda670e4b8b8a794b377` |

证据文件：

- [receipt.json](evidence/2026-10-29/receipt.json)：环境、提交身份、七个阶段快照及通过的检查。
- [commands.json](evidence/2026-10-29/commands.json)：64 条命令和写文件动作记录，包含 Git 参数、实际输出与退出码。实验绝对路径替换为 `[LAB]`，内容和提交身份保留原值。

提交身份也受提交元数据影响，复跑不要求得到相同 SHA；应核对每次运行内部的内容、引用关系与断言结果。

## 验证边界

这份证据支持第 01 篇中“候选内容与开发目录不同、明确取得指定版本”的 Git 文本文件场景，以及第 02 篇的状态与引用变化。它没有验证同文件任务拆分、故障恢复、真实网络、GitHub/GitLab 审批、SVN、P4、Unity/Unreal 导入、依赖完整性、构建或游戏行为。
