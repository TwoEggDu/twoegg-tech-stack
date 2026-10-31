# 任务提交：交付 A，保留 B

对应 [VM-05 正文](../../../content/delivery-engineering/version-management-05-task-submission.md)与[实验计划](../../../docs/version-management-05-experiment-plan.md)。Git 跑完整闭环，SVN/P4 在正文中只补机制差异，没有新增对应端到端实测。

## 运行

需要 Python 3.11+、Git（Windows 上使用 Git for Windows，需能运行标准 shell hook），无第三方 Python 依赖。从仓库根目录执行：

```powershell
python examples/version-management/task-submission/run-lab.py `
  --lab-root E:/workspace/TechStackShow/.tmp/task-submission-new `
  --evidence-directory E:/workspace/TechStackShow/.tmp/task-submission-new-evidence
```

将绝对路径替换为自己的位置。两个输出目录必须不存在且互不包含。脚本创建本地裸远端、开发仓库、两个 index 导出目录和接收仓库；不删除或复用已有目录，不操作本项目 index，不连接 GitHub/GitLab。Git 系统/全局配置通过子进程环境隔离，身份、换行、签名和 hooksPath 仅配置在新开发仓库。

`fixtures/` 中三份脚本表示基线、A-only、A+B，执行时复制成各目录自己的 `skill.py`。A 新增 JSON 依赖并读取伤害 80，B 将蓄力从 false 改为 true。备注、合成二进制、删除和未跟踪草稿是 B 的额外工作。

## 实测记录

2026-10-30，Git `2.53.0.windows.2`、Python `3.12.4`，**33 项检查通过**。保存 65 条命令或文件动作，包含两个预期非零退出；不是所有命令都退出 0。

- [receipt.json](evidence/2026-10-30/receipt.json)：提交与树身份、33 项断言、九个快照、各目录文件 SHA256。
- [commands.json](evidence/2026-10-30/commands.json)：参数、stdin、stdout、stderr、实际与预期退出码；`[LAB]` 代替实验根路径。
- [task-a.patch](evidence/2026-10-30/task-a.patch)：从基线到 A-only 的精确补丁，仅修改伤害函数。

补丁中的空上下文行带有 unified diff 必需的单空格标记，因此本目录 `.gitattributes` 仅为 evidence 下的 `.patch` 关闭行尾空白诊断；保留补丁原始字节，其余代码、JSON 与正文仍检查行尾空白。

| 对照 | 结果 |
| --- | --- |
| 开发目录 A+B，加本地 JSON | 退出 0，`{"charge": true, "damage": 80}` |
| 只把 A 脚本纳入 index 后导出 | 退出 1，`FileNotFoundError`，缺 `damage.json` |
| 再把 JSON 纳入 index，重新导出 | 退出 0，`{"charge": false, "damage": 80}` |
| 本实验 pre-commit hook 明确拒绝 | commit 退出 1，HEAD、候选 tree 与开发目录内容保留，远端仍为基线 |
| 禁用本实验拒绝器后正常 commit | 提交 tree 与通过验证的 index tree 相同，远端仍为基线 |
| 成功 push 后模拟丢弃回执 | 控制端记录真实成功；调用方只得到 unknown；重新查询远端得到精确候选 SHA，无需重试 |
| 接收仓库明确取得候选 SHA | 退出 0，80 / false；仓库干净，文件散列与完整 index 导出相同 |

实际教学身份（不是本项目的提交）：

```text
baseline  df5b6753921f7163c5f5943cb05ea5e33d21d9bb
candidate 4bfd9a509e56aab439d2a6e602723c9df6ba432a
tree      6e64749d3ab63e778f07e843d7f403cc0d0660e5
```

开发目录在选择、补齐依赖、明确拒绝、成功 commit、push 与查询后，文件路径及字节散列都与选择前一致；B 的删除也始终保留。候选只变更 `skill.py` 与 `damage.json`，合成二进制保持基线，B 的草稿不进入候选。散列记录证明文件内容保留，不证明编辑器未保存缓冲区、ACL 或所有时间戳等状态。

## 可复跑方式与失败边界

自动脚本采用 `git apply --cached` 应用已知 A 补丁，而非驱动 `git add -p` 的交互菜单。正文将交互选择作为手工入口，不能把自动补丁应用写成实际测过的交互流程。这里只处理独立函数中的可分离改动；语义耦合或二进制文件不能据此承诺可拆分。

明确拒绝由新建实验 hook 提供。解除它是为继续教学而禁用自己的固定拒绝器，不是生产失败处理建议，不修改用户 hook、不使用 `--no-verify`。没有承诺任意第三方 hook 失败都保持文件不变。

回执丢失由包装步骤模拟：真实 `git push` 已结束，控制端保留退出码及输出供审计，然后给模拟调用方 unknown；调用方重新执行 `git ls-remote` 判断是否交付。未制造真实网络断连、超时、服务崩溃或并发远端更新。查询失败或 SHA 不符时应保留未知状态，不沿用本次“无需重试”的结论。

首次尝试中，Python 文本 stdin 在 Windows 上把 LF 补丁转换为 CRLF，导致预检失败。只读对照确认 LF 字节预检退出 0、CRLF 字节退出 1；runner 改用二进制 stdin 后在新目录跑通。失败现场保留在 `.tmp/vm05-task-submission-failed-evidence-20261030/`，公开 dated evidence 只包含最终通过运行。

验证范围仅为普通教学脚本的输入与行为，以及 Git 内容交接。没有引擎、Prefab/.meta、真实资源导入、构建、生产门禁或 SVN/P4 同任务实测。
