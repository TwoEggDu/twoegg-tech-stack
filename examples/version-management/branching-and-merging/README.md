# 分支与合并：历史汇合与行为验收

对应 [VM-06 正文](../../../content/delivery-engineering/version-management-06-branching-and-merging.md)及[实验计划](../../../docs/version-management-06-experiment-plan.md)。Git 执行三组验证，SVN/P4 仅在正文补官方机制对照，没有新增对应运行验证。

## 运行

需要 Python 3.11+ 与 PATH 中的 Git，无第三方 Python 包。从仓库根目录执行：

```powershell
python examples/version-management/branching-and-merging/run-lab.py `
  --lab-root E:/workspace/TechStackShow/.tmp/branch-merge-new `
  --evidence-directory E:/workspace/TechStackShow/.tmp/branch-merge-new-evidence
```

替换自己的绝对路径；两个输出目录都必须不存在且互不包含。脚本创建 `fast-forward`、`divergent`、`text-conflict`、`semantic` 四个独立仓库，再从 semantic 本地 clone 一个 receiver，不连接 GitHub/GitLab，不操作本项目 index、分支或远端。系统/全局 Git 配置在子进程中隔离，身份、LF、签名、空 hooks、`merge.ff=true`、`rerere.enabled=false` 仅设置在新仓库。

失败保留输出与 receipt，不自动清理或复用。`fixtures/` 中程序运行时复制为新仓库自己的 `damage.py`；原始值程序与最终封顶程序共用同一份固定检查器，JSON 配置由 runner 创建。

## 实测结果

2026-10-31，Git `2.53.0.windows.2`、Python `3.12.4`，**41 项检查通过**，176 条命令或写文件动作、七个快照。

- [receipt.json](evidence/2026-10-31/receipt.json)：实际提交、父提交、图、冲突输入、行为、文件 SHA256 与通过检查。
- [commands.json](evidence/2026-10-31/commands.json)：参数、stdout、stderr、实际及预期退出码；`[LAB]` 替代实验根路径。

| 场景 | 工具状态 | 内容或行为 |
| --- | --- | --- |
| 快进 | `merge --ff-only` 退出 0；main 指向 feature 原提交，无新增提交 | damage=80 |
| 两侧分叉后普通合并 | merge 退出 0；新提交的两个父提交分别为合并前 main 与 feature | damage=80、charge=on，来源分支不变 |
| 同行文本冲突 | merge 预期退出 1；HEAD 未前进，MERGE_HEAD 为来源；index stage 1/2/3 为 100/80/90 | 按该场景事先约定保留 80，解决后两父提交、无未合并条目且干净 |
| 主案例：基线 | 检查器退出 0 | 基础 100、倍率 100%，原始与最终值 100 |
| 主案例：main 单独变化 | 同一检查器退出 0 | 基础 120、倍率 100%，结果 120 |
| 主案例：feature 单独变化 | 同一检查器退出 0 | 基础 100、倍率 140%，结果 140 |
| 主案例：自动合并 | merge 退出 0，无文本冲突、工作区干净；检查器预期退出 1 | 基础 120、倍率 140%，结果 168，违反最终伤害上限 150 |
| 主案例：单独提交适配 | 检查器字节不变、退出 0；只改 damage.py，保留两个配置 | 原始值 168、最终值 150、上限仍为 150 |
| 新 receiver 取得精确适配 SHA | 检查器通过，工作区干净，文件 SHA256 与目标相同 | 168 / 150，来源分支仍停在原 SHA |

两个预期非零退出分别是文本冲突的 merge，以及自动合并后的业务检查。它们是教学对照，不是运行中的意外失败。

语义案例的教学身份（不是本项目提交）：

```text
基线       efae89e49888b36dd6794f9f607c6bee39dbe929
main 原端  e5a95b623bf4447d69b83051413a4a3250970fdf
feature    f0a5a66f8e7a98cd21bbb21b81f95a10036f23e7
错误合并   11343ed7c587b5e9e760c35be3b77d51c0c5593d
适配提交   1b0dbf322426a4e1c20bccab1521abce8bed81cd
```

## 业务约定与取证边界

最终伤害按 150 封顶是实验计划预先写明的教学规则；两侧单独未触发边界，组合后才暴露问题。适配保留 base=120 与 percent=140，只对最终结果封顶，仍展示 raw=168。没有改变上限、检查器或配置来掩盖失败，也没有声称这种公式适合实际游戏。

Git 的祖先可达性只证明该历史关系。在本次错误合并中，两侧提交均已可达，程序仍违反约定。显式冲突解决时也按约定选用了 80，不能从“feature 已是祖先”推出 feature 中的 90 被原样保留。

主案例刻意保留已经记录的错误合并，再用单独提交适配，以便追查。实际项目可在发布前采用候选合并、验证和团队接受流程；本实验没有部署流水线、推送坏版本或修改保护规则。

仅验证普通双分支 merge、一个单行文本冲突与整数教学行为。没有 rebase/cherry-pick、复杂多祖先、移动/删除冲突、二进制合并、脏工作区中止与恢复、SVN mergeinfo 或 P4 集成运行验证；也没有引擎、资源依赖、网络、生产审批与线上验收。
