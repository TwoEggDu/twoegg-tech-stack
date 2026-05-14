---
title: "AI 赋能 10｜Skill 自动沉淀：不靠 RL 的\"自进化\"在 Claude Code 里怎么实现"
slug: "ai-empowerment-10-skill-auto-distillation"
date: "2026-05-13"
description: "Hermes Agent 的\"自进化\"靠两条腿：Skill 自动沉淀 + RL 训练。后者门槛极高、跟普通工程师没关系。这篇只讲前者——用 Claude Code 已有的 hook + memory + skill 三件套，怎样模拟出一条轻量级的\"任务结束自动复盘\"路径。"
tags:
  - "AI Engineering"
  - "Claude Code"
  - "Skill System"
  - "Memory"
series: "AI 赋能游戏开发"
primary_series: "ai-empowerment"
series_role: "article"
series_order: 100
weight: 200
---

> **读这篇之前**：本篇假设你已经读过 [06｜Skill 系统：给 AI 注入领域规则]({{< relref "ai-empowerment/ai-empowerment-06-skill-domain-knowledge.md" >}}) 和 [09｜把 SDD 套进内容生产]({{< relref "ai-empowerment/ai-empowerment-09-sdd-in-content-production.md" >}})。这篇不重讲 Skill 的基础概念，讲的是怎样让 Skill **自己长出来**。

> **本篇当前状态**：骨架。需要至少 2 周的 hook + memory 真实运行记录才能完成。当前可读但留有大量 `DATA-TODO`。

## 这篇解决什么问题

2026 年 4 月底有一篇阿里云的文章在工程圈传得很广——《深度解析 Hermes Agent 如何实现"自进化"》。它讲了 Hermes Agent（Nous Research 出的开源 Agent 项目）怎样做到"运行时间越长，能力越强"。

Hermes 的"自进化"靠两条腿：

1. **动态 Skill 沉淀**：每次任务完成后启动后台复盘 Agent，把执行轨迹中的"踩坑、纠错、最佳实践"抽象成新 Skill 或更新已有 Skill
2. **RL 训练闭环**：用 GRPO 算法 + 多维度奖励函数把通用大模型蒸馏成领域模型

第二条对普通工程师**完全不适用**：

- 你大概率不在做 Agent 框架
- 你大概率没有 GPU 训练资源
- 你大概率不需要蒸馏小模型替代 API
- 即使做了，门槛是阿里那篇里的"算法同学"级别

这篇文章**只讲第一条**——而且把它从"Agent 框架级实现"降到"个人工程师在 Claude Code 里怎么搭一套轻量版"。

## 为什么不直接用 Hermes

Hermes 那套"后台审查 Agent"很优雅：主 Agent 完成对用户回复后，后台异步 fork 一个轻量 Agent 实例做三类复盘（记忆审查 / 技能审查 / 综合审查），各自对应一份独立 prompt。

但它是 Agent 框架级别的设计。我用 Claude Code，不写 Agent 框架。我要解决的等价问题是：

**怎样让 Claude Code 在我跑完一次任务后，自动跑一遍"今天学到了什么、要不要写进 memory 或 skill"？**

Claude Code 本身已经提供了三个我需要的原语：

- **Hook**：在某个事件（PreToolUse / PostToolUse / Stop / SubagentStop 等）发生时跑命令
- **Memory**：跨 session 持久化的事实层（`~/.claude/CLAUDE.md` 用户级 / 项目 `CLAUDE.md` 项目级 / `MEMORY.md` 自动记忆）
- **Skill**：项目或全局的 SOP，按 description 触发

这三个原语组合起来，理论上能实现一条"轻量版自进化"路径：

```
任务结束（Stop hook 触发）
    │
    ▼
启动一个独立 session 跑复盘
    │
    ├─ 这次任务有什么值得记的事实？→ 写 MEMORY.md
    ├─ 这次任务有什么可复用的流程？→ 提议新建 Skill
    └─ 这次任务踩了什么规则缺失的坑？→ 提议改 CLAUDE.md
    ▼
人审一遍，决定是否合并
```

注意"人审一遍"——不是全自动。后面会展开为什么不全自动。

## 三件套的具体配置

### 件 1：Stop hook

Claude Code 的 Stop hook 在每次主 Agent 回应结束时触发。可以在 `~/.claude/settings.json` 或项目级 `.claude/settings.json` 配：

```json
{
  "hooks": {
    "Stop": [
      {
        "matcher": "",
        "hooks": [
          {
            "type": "command",
            "command": "node ~/scripts/claude-retro.js"
          }
        ]
      }
    ]
  }
}
```

`claude-retro.js` 做的事：

1. 读这次 session 的对话历史（Claude Code 把它存在本地）
2. 跑一个简单判断：这次任务**值不值得**复盘
3. 如果值得 → 把对话历史 + 复盘 prompt 写进一个临时文件
4. 提示作者"建议跑一次复盘"

**关键设计：hook 不直接调 Claude API 跑复盘**。Hermes 的后台 Agent 是 Agent 框架内置的——异步、不耗主流程 token。我没有那种环境，所以 hook 只做"提议"，复盘本身用一个独立 Claude Code session 跑（人手动启动）。

<!-- DATA-TODO: 真实跑过这个 hook 之后，补一段 `claude-retro.js` 的最终代码。当前阶段只写思路，避免编造。 -->

### 件 2：复盘 session 的 prompt

复盘 session 不需要复杂的 Agent 编排，一条 prompt 就够：

```
读 /tmp/last-session.json 里的对话历史。回答四个问题：

1. 这次任务里 AI 第一次犯什么错？人怎么纠正的？
   → 这条值得写进 CLAUDE.md 或某个 Skill 吗？

2. 这次任务里有哪些步骤是"模式化、可重复"的？
   → 这是不是一个新 Skill 的候选？

3. 这次任务里有哪些事实（文件路径、约定、版本）是会复用的？
   → 这是不是该写进 MEMORY.md？

4. 这次任务里有哪些是"一次性偏好"——只在这次有效、不该规则化？
   → 这部分不沉淀。明确说"不沉淀"。

每一条都给出建议位置（文件名 / 章节名）和具体文本（不要写"建议添加 XX 规则"，直接给可粘贴的最终内容）。
最后给我一个**拒绝清单**——这次任务里哪些是 AI 想沉淀但你觉得不该沉淀的。
```

这条 prompt 的关键设计有两点：

1. **强制对称地输出"该沉淀 / 不该沉淀"两类**——避免单方向只加规则导致 Bloat（参见 [Harness Engineering 03｜Bloat 反模式与瘦身]({{< relref "harness-engineering/harness-engineering-03-bloat-and-slimming.md" >}})）
2. **要求给出"可粘贴的最终内容"而不是"建议添加 XX"**——避免人还要二次翻译

<!-- EXPERIENCE-TODO: 真跑过几次之后，调一遍这条 prompt。当前是设计稿，预计要改。 -->

### 件 3：Memory 与 Skill 的分工

复盘出来的产出有 3 个去向，对应不同的持久层：

| 沉淀类型 | 存哪里 | 形态 |
|---------|------|-----|
| 跨项目通用规则 | `~/.claude/CLAUDE.md` | 短规则，1-2 行 |
| 本项目专属规则 | 项目 `CLAUDE.md` 或 `AGENTS.md` | 短规则，1-2 行 |
| 跨 session 事实 | 项目 `MEMORY.md` 或 `CLAUDE.md` 的 Memory 段 | 事实条目 |
| 可复用流程 | `~/.claude/skills/*.md` 或项目级 skill | SOP 文件 |
| 一次性偏好 | 不沉淀 | 当前对话内说明即可 |

最后那一行最容易被忽略——**很多人会把"我希望这次输出中文"沉淀为规则**，下次跑英文项目就被规则绊一脚。

## 跟 Hermes 设计的对照

把上面整套对回 Hermes：

| Hermes 设计 | 我的轻量版对应 | 妥协 |
|------------|--------------|------|
| 后台异步 Agent | Stop hook + 人手动启动复盘 session | 不是异步、要人触发 |
| _skill_nudge_interval 每 10 轮提醒 | Stop hook 每次都跑（粒度更细） | 噪音可能大、需要 retro.js 过滤 |
| 三类审查 prompt（记忆 / 技能 / 综合） | 一条合并 prompt 含四问题 | 简化、但加了"拒绝清单" |
| 自动写入 Skill | 人审后手动写入 | 慢、但避免误沉淀 |
| RL 训练闭环 | **不做** | 完全跳过 |

**没追求自动化是有意的**。Hermes 的全自动适合一个长期运行、跨多用户的 Agent 服务——它不能停下来等人审。我作为单个工程师做单个项目，停下来人审一秒钟的成本，比沉淀错规则未来反复出错的成本低得多。

## 评判这套有没有用的指标

跑 2 周之后看 5 个数：

| 指标 | 怎么算 | 健康阈值 |
|------|--------|---------|
| 复盘触发率 | 复盘 session 启动次数 / Stop hook 触发次数 | 10-30%（大部分任务不值得复盘） |
| 沉淀采纳率 | 实际写进 memory/skill 的条数 / 复盘提议的条数 | 30-60%（拒绝清单不能为空） |
| 重复犯错率 | 同类错在沉淀后 2 周内再犯 | < 10% |
| 规则总量增长 | CLAUDE.md / Skill 总行数 | 每周 < 50 行（防 Bloat） |
| 主动删规则次数 | 因为沉淀冗余而回头删 | 至少每月 1 次 |

最后一项最反直觉——**主动删规则次数应该 > 0**。如果跑了几个月一条都没删，说明沉淀机制有，瘦身机制没有，Bloat 是必然的。这个判断在 [Harness Engineering 03｜Bloat 反模式与瘦身]({{< relref "harness-engineering/harness-engineering-03-bloat-and-slimming.md" >}}) 里展开了。

<!-- DATA-TODO: 跑 2 周之后，把这 5 个指标的真实数据填进来。当前阈值是设计阶段的预估，可能要根据实际数据调。 -->

## 常见错觉与陷阱

### 错觉 1：沉淀越多越好

每次任务都产出几条新规则，CLAUDE.md 一周长 200 行——这不是 Skill 自动沉淀的胜利，是 Bloat 的开端。

正确节奏：**每次复盘里"拒绝沉淀"的条数应该至少跟"采纳沉淀"持平**。这逼自己每次都主动判断"这条值不值"。

### 错觉 2：沉淀位置无所谓

把"本项目的 frontmatter 引号规则"写进 `~/.claude/CLAUDE.md`（用户级），下次跑别的项目时这条规则也会触发，跟新项目的约定打架。

正确做法：**严格分层**。跨项目通用 → 用户级。本项目专属 → 项目级。一次性偏好 → 不沉淀。

### 错觉 3：Skill 自动生成 = 不用维护

新生成的 Skill 跟手写 Skill 一样，会过期、会跟其他 Skill 冲突、会因为项目演进而失效。**自动生成只解决了"生”，没解决"养"**。Drift（参见 [Harness Engineering 04｜Drift 与文档腐烂]({{< relref "harness-engineering/harness-engineering-04-drift-and-rot.md" >}})）在自动生成的 Skill 上一样会发生。

<!-- EXPERIENCE-TODO: 跑 2 周后回来补一段"我亲眼看到的 1-2 个错觉示例"。当前是基于 Hermes 描述和 Bloat 反模式的推断，不是亲历。 -->

## 跟 RL 训练那条线的明确切割

阿里那篇文章用了**一半篇幅讲 RL**——GRPO 算法、奖励函数设计、轨迹压缩、批量数据合成、teacher model 蒸馏。

这些**全部不要尝试**，除非：

- 你是算法工程师，本职就是训模型
- 你有 GPU 资源（不是 4090 那种，是 A100/H100 级别）
- 你有明确的领域基准（GSM8K / HumanEval 这类）
- 你的目标是把 API 成本降下来或合规要求本地模型

普通工程师碰 RL 这条线 90% 的概率是浪费时间。Skill 自动沉淀这条轻量线，你能拿到 Hermes "自进化"价值的大头——而且不用碰任何模型训练。

## 收束

Skill 自动沉淀的核心不是"自动"，是"沉淀"。**手动沉淀也行**——只要每次任务结束你都问自己那四个问题（值得记的错、可复用的流程、跨 session 的事实、一次性偏好），并且**明确拒绝一部分沉淀**，效果就有 80%。

Stop hook + 复盘 session 这套配置只是把"每次问自己"这个动作机械化。它的价值不在自动化程度，在**降低你忘记问的概率**。

最短结论是：

**Hermes 的两条腿里，Skill 沉淀是给普通人的，RL 训练是给算法同学的。别搞混。**

下一篇 [11｜kb/ 与 LLM Wiki 范式]({{< relref "ai-empowerment/ai-empowerment-11-kb-and-llm-wiki-pattern.md" >}}) 把视角再放大——多次沉淀的产物长期怎么组织、怎么避免散乱。

<!-- DATA-TODO: 全篇大量 TODO。要写到能发布的状态，必须：(1) 真跑 2 周 Stop hook (2) 至少 5 次复盘记录 (3) 5 项指标有数据 (4) 至少 1 个"主动删规则"的真实案例。当前发布会让读者看到一篇"听起来对但没实测"的文章。 -->

<!-- EXPERIENCE-TODO: claude-retro.js 的最终实现代码、复盘 prompt 的迭代历史、跟 Hermes 框架的对照表是否真的成立——都需要真实运行后回填。 -->
