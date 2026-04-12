---
date: "2026-03-28"
title: "Unity 脚本编译管线 09｜编译机器人实践：从触发到通知的全链路"
description: "游戏团队里的编译机器人不只是 CI 脚本，而是一套从触发、调度、执行到通知的闭环。讲清楚机器人的核心组件、Unity 专属的注意事项，以及失败处理和产物管理的工程实践。"
slug: "unity-script-compilation-pipeline-09-build-robot"
weight: 70
featured: false
tags:
  - "Unity"
  - "CI"
  - "Build"
  - "Automation"
  - "DevOps"
series: "Unity 脚本编译管线"
series_order: 9
---

> 编译机器人的本质不是"自动化打包"，而是把打包这件事从"工程师的手工活"变成"团队的基础设施"。好的机器人让策划也能一键触发构建，让工程师在睡觉时收到精准的失败通知，让每一个产物都可溯源。

---

## 这篇要回答什么

- 编译机器人由哪几层组成，各层职责是什么？
- 触发层怎么设计才能让非技术同学也能用？
- 调度层如何避免构建风暴和重复构建？
- Unity 在无头模式下有哪些必须注意的坑？
- 通知层的失败摘要怎么做才有用？
- 产物怎么命名、保留多久、放在哪？

---

## 场景：现有流程的问题

很多团队的打包流程是：

```
打开 Jenkins → 找到对应 Job → 手动填参数 → 点构建 → 等邮件
```

这个流程的问题不是慢，而是三个结构性缺陷：

| 缺陷 | 表现 |
|------|------|
| 对非技术同学不友好 | 策划/美术不知道该填哪个 Job，参数含义不明确 |
| 对重复触发没有保护 | 同一分支被多人同时触发，机器被打满 |
| 失败时没有明确责任人 | 邮件发给所有人，没人认领，失败悬在那里 |

编译机器人要解决的，是把这条链变成：**可被任何人通过 IM 触发、有明确状态反馈、失败有归因的系统**。

---

## 四个核心组件

```
┌─────────────────────────────────────────────────┐
│                   触发层                         │
│   IM Bot / Web UI / Webhook / 定时任务           │
└──────────────────────┬──────────────────────────┘
                       │ 构建请求（参数化）
┌──────────────────────▼──────────────────────────┐
│                   调度层                         │
│   Job 队列 / 优先级 / 去重 / 超时保护             │
└──────────────────────┬──────────────────────────┘
                       │ 分配到可用 Agent
┌──────────────────────▼──────────────────────────┐
│                   执行层                         │
│   Jenkins / GitHub Actions + Unity 命令行        │
└──────────────────────┬──────────────────────────┘
                       │ 构建结果 + 产物路径
┌──────────────────────▼──────────────────────────┐
│                   通知层                         │
│   构建状态 / 产物链接 / 失败摘要 / 责任人推断      │
└─────────────────────────────────────────────────┘
```

每一层都是独立可替换的模块。触发层换成 Webhook 不影响执行层；执行层从 Jenkins 迁移到 GitHub Actions 不影响通知层。这个分层是后续所有设计决策的基础。

---

## 触发层

### 四种触发方式

| 触发方式 | 适用场景 | 技术实现 |
|----------|----------|----------|
| IM Bot（钉钉/企业微信） | 研发、测试日常触发 | Bot 解析命令 → 调用 CI API |
| Web UI | 美术、策划触发 | 简化表单，隐藏复杂参数 |
| Webhook | PR 合并后自动触发 | Git 服务器推送事件 → CI API |
| 定时任务 | 夜间全量包、每日冒烟 | Cron 表达式，固定参数 |

### IM Bot 命令设计

Bot 接收自然语言风格的命令，解析后转成 CI 参数：

```
/build android release feature/new-skill
/build ios debug main
/build android release --hotfix   # 热修复包，插队优先
```

解析逻辑保持简单：`平台 + 类型 + 分支` 三段式，可选标志用 `--` 前缀。解析失败时 Bot 回复帮助文本，不静默失败。

### Web UI 参数设计

给非技术同学的页面只暴露四个参数，其余用默认值：

| 参数 | 控件类型 | 默认值 |
|------|----------|--------|
| 平台 | 单选（Android / iOS） | Android |
| 分支 | 下拉（只列活跃分支） | main |
| 构建类型 | 单选（Debug / Release） | Release |
| 是否包含热更包 | 复选框 | 不勾选 |

### Webhook 触发的参数注入

PR 合并时，Webhook 携带的上下文足够推断大部分参数：

```
PR 目标分支 → 决定构建类型（main → Release，其余 → Debug）
PR 标题/标签 → 决定是否触发特定流水线（如含 [hotfix] 标签）
提交者信息 → 注入到通知层，用于失败归因
```

---

## 调度层

调度层是最容易被忽视、出问题最多的一层。

### 构建队列

同时运行的 Job 数量取决于可用 Agent 机器数量。超出的请求进入队列等待，不要无限制并发。

```
[等待队列]  →  [运行中: Agent 1]
            →  [运行中: Agent 2]
            →  [运行中: Agent 3]  (最大并发 = Agent 数量)
```

### 优先级规则

| 优先级 | 条件 |
|--------|------|
| P0（最高） | 热修复包（`--hotfix` 标志） |
| P1 | 主干分支（`main` / `master`） |
| P2 | 功能分支 |
| P3 | 定时任务 |

优先级高的请求入队时插到同优先级末尾，不会抢占正在运行的 Job。

### 去重策略

同一分支、同一参数组合，在队列中只保留最新一条：

```
队列中已有: [feature/new-skill | android | release]
新请求到来: [feature/new-skill | android | release]
→ 丢弃旧请求，替换为新请求（用新的 commit hash）
```

这避免了"快速连续 push 导致队列堆积"的问题。已在运行中的 Job 不受影响，等它结束后再处理最新请求。

### 超时保护

Unity 打包偶尔会无限卡死（资源导入死循环、网络请求挂起）。必须设硬超时：

| 构建类型 | 超时建议 |
|----------|----------|
| 增量构建（有缓存） | 45 分钟 |
| 全量构建（无缓存） | 90 分钟 |
| 仅脚本编译检查 | 15 分钟 |

超时触发后：强杀 Unity 进程 → 清理 `Temp/` → 发送超时告警（区别于构建失败告警）。

---

## 执行层：Unity 专属注意事项

这一层的 Jenkins / GitHub Actions 配置不是本篇重点，Unity 命令行的坑才是。

### 基础启动命令

```bash
Unity.exe \
  -batchmode \
  -nographics \
  -quit \
  -projectPath "E:/Projects/MyGame" \
  -executeMethod BuildScript.BuildAndroid \
  -logFile "E:/build_logs/unity_build.log" \
  -buildTarget Android
```

关键参数说明：

| 参数 | 作用 | 注意事项 |
|------|------|----------|
| `-batchmode` | 无 UI 模式 | 必须，否则会弹出许可证对话框 |
| `-nographics` | 禁用图形设备 | 在无 GPU 的 CI 机器上必须加 |
| `-quit` | 执行完后退出 | 配合 `-executeMethod` 使用 |
| `-logFile` | 指定日志文件路径 | **不要依赖 stdout**，Unity 的 stdout 输出不完整 |

### 构建脚本的退出方式

构建脚本必须用 `EditorApplication.Exit(exitCode)` 明确退出，不要让 Unity "自然退出"：

```csharp
public static class BuildScript
{
    public static void BuildAndroid()
    {
        try
        {
            var report = BuildPipeline.BuildPlayer(GetBuildOptions());
            if (report.summary.result == BuildResult.Succeeded)
            {
                EditorApplication.Exit(0);   // 成功
            }
            else
            {
                EditorApplication.Exit(1);   // 失败
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Build exception: {e}");
            EditorApplication.Exit(2);       // 异常
        }
    }
}
```

退出码要有语义，CI 脚本根据退出码做分支处理（0 = 成功，1 = 构建失败，2 = 脚本异常）。

### Unity 进程残留检测

构建开始前检查是否有残留的 Unity 进程占用同一项目：

```bash
# 检查是否有 Unity 进程正在使用该项目目录
if pgrep -f "projectPath.*MyGame" > /dev/null; then
    echo "ERROR: Unity process already running for this project"
    exit 1
fi
```

残留进程最常见的来源：上一次构建超时后强杀不彻底，或 Unity crash 后进程变成僵尸。

### Library/ 缓存

按本系列 07 篇的策略配置，本篇不重复。核心结论：主干分支的 `Library/` 值得缓存，功能分支视情况而定。

### 时区统一

Jenkins Agent 的系统时区必须与产物时间戳使用的时区一致，否则产物命名会出现时间跳跃：

```bash
# 在 Jenkins Agent 启动脚本中强制指定时区
export TZ=Asia/Shanghai
```

---

## 通知层

### 通知内容的最小集合

| 字段 | 成功时 | 失败时 |
|------|--------|--------|
| 构建状态 | 成功 + 耗时 | 失败 + 失败阶段 |
| 触发信息 | 触发者 + 分支 + 平台 | 触发者 + 分支 + 平台 |
| 关键步骤耗时 | 编译 Xmin，打包 Xmin | 到失败为止的耗时 |
| 产物链接 | 下载链接 | 无 |
| 错误摘要 | 无 | 关键错误行（不是全量日志） |
| 责任人推断 | 无 | 最近提交者 |

### 失败通知示例

```
❌ Android Release 构建失败
触发：张三 | 分支：feature/new-skill | 耗时：12min
失败阶段：Script Compilation
错误摘要：CS0246: 找不到类型 'NewSkillData'（SkillSystem.asmdef）
最近提交：李四（2 commits ago，3 files changed）
日志：[查看完整日志](http://ci.internal/job/123/log)
```

### 错误摘要提取

从 Unity 日志中提取有效错误行，不要把全量日志贴进通知：

```bash
# 从 Unity 日志提取编译错误
grep -E "^.*\.cs\([0-9]+,[0-9]+\): (error|Error)" unity_build.log \
  | head -5 \
  > error_summary.txt
```

失败摘要取前 5 条错误，超出部分附日志链接。错误行要带文件名和行号，方便开发直接定位。

### 责任人推断

失败时自动查最近几条提交，不是为了"甩锅"，是为了让对的人第一时间知道：

```bash
# 获取最近 5 条提交的作者
git log --oneline -5 --format="%an (%ar)" HEAD
```

通知里只显示最近 2-3 个提交者，告警同时 @ 他们。

---

## 失败处理的工程实践

### 失败分类

不同类型的失败处理策略不同，不要统一重试：

| 失败类型 | 判断依据 | 处理策略 |
|----------|----------|----------|
| 编译失败 | Unity 退出码 1，日志含 `error CS` | 通知开发，不自动重试 |
| 打包失败 | Unity 退出码 1，日志含 `BuildFailedException` | 通知开发，不自动重试 |
| Unity 崩溃 | Unity 进程异常退出（退出码非 0/1） | 自动重试一次，同时告警运维 |
| 环境失败 | 磁盘满、网络超时、Agent 失联 | 自动重试一次，同时告警运维 |
| 超时 | 达到超时阈值强杀 | 告警运维，人工介入 |

编译失败自动重试没有意义——代码错误不会因为重试而消失，只会浪费机器时间和队列资源。

### 失败后的清理

构建失败或超时后，执行清理脚本：

```bash
# 失败后清理脚本
cleanup_after_failure() {
    local project_path=$1

    # 清理 Temp/ 避免影响下次构建
    rm -rf "${project_path}/Temp/"

    # 释放可能残留的文件锁
    # （在 Windows 上可能需要额外处理）

    echo "Cleanup completed for ${project_path}"
}
```

`Temp/` 是 Unity 的临时编译产物目录，异常退出后如果不清理，下次构建可能读到损坏的缓存文件导致奇怪错误。

---

## 产物管理

### 命名规范

```
{project}_{platform}_{version}_{branchHash}_{timestamp}.apk

示例：
MyGame_Android_1.2.3_main-a1b2c3d_20260328-143052.apk
MyGame_Android_1.2.3_feature-newskill-e4f5g6h_20260328-160030.apk
```

| 字段 | 说明 |
|------|------|
| `project` | 项目代号，多项目共用存储时区分 |
| `platform` | `Android` / `iOS` |
| `version` | 来自 `ProjectSettings` 的版本号 |
| `branchHash` | `{分支短名}-{commit hash 前 7 位}` |
| `timestamp` | `YYYYMMDD-HHmmss`，CI Agent 时区（需统一） |

### 版本号注入

构建时自动把 git commit hash 写入 `PlayerSettings`，产物可溯源：

```csharp
// 在构建前执行
var commitHash = RunCommand("git", "rev-parse --short HEAD");
PlayerSettings.bundleVersion = $"{PlayerSettings.bundleVersion}+{commitHash}";
```

### 存储策略

产物不放在 CI 机器本地，构建完成后立即上传到专用文件服务器：

```
构建完成
   ↓
上传到文件服务器（Nginx / OSS / S3）
   ↓
通知层发送下载链接
   ↓
CI 机器本地产物文件可删除
```

保留时长：

| 来源分支 | 保留时长 |
|----------|----------|
| 主干（`main` / `master`） | 30 天 |
| 功能分支 | 7 天 |
| 热修复分支 | 60 天（手动标记可延长） |
| 定时构建 | 3 天 |

定期清理脚本按时间戳扫描文件服务器，超期自动删除。

---

## 系统架构全图

读完这篇，你应该能画出这张图：

```
外部触发
  │
  ├── IM Bot（/build 命令解析）
  ├── Web UI（简化参数表单）
  ├── Webhook（PR 合并事件）
  └── Cron（定时任务）
         │
         ▼
   [调度层 - 队列服务]
     ├── 去重（同分支同参数）
     ├── 优先级排序
     ├── 并发控制（≤ Agent 数量）
     └── 超时守护进程
         │
         ▼
   [执行层 - CI Agent]
     ├── 进程残留检测
     ├── Library/ 缓存恢复
     ├── Unity -batchmode 启动
     ├── BuildScript.Build()
     │     ├── 成功 → EditorApplication.Exit(0)
     │     └── 失败 → EditorApplication.Exit(1)
     ├── 产物上传（文件服务器）
     └── Temp/ 清理（失败时）
         │
         ▼
   [通知层 - 消息服务]
     ├── 成功通知（状态 + 耗时 + 下载链接）
     └── 失败通知（阶段 + 错误摘要 + 责任人）
```

每一个方框都是一个明确的决策点：可以替换实现，可以独立监控，可以单独扩容。

---

## 小结

| 层 | 核心决策点 |
|----|-----------|
| 触发层 | 命令解析要有错误反馈；Web UI 只暴露必要参数 |
| 调度层 | 去重 + 优先级 + 超时三件套缺一不可 |
| 执行层 | `-logFile` 而非 stdout；`EditorApplication.Exit` 明确退出码；构建前清残留进程 |
| 通知层 | 失败摘要提取关键错误行；责任人推断用最近提交者 |
| 产物管理 | 命名含 commit hash；主干 30 天，功能分支 7 天；不存本地 |

编译机器人建设是渐进的。从"能跑通"到"稳定可靠"的关键跨越，是补齐调度层的去重和超时保护，以及通知层的失败归因。这两件事做好之后，绝大多数构建问题都会在第一时间被正确的人处理。

---

- 上一篇：[Unity 脚本编译管线 08｜编译报错排查：从错误信息定位根因]({{< relref "engine-toolchain/unity-script-compilation-pipeline-08-compilation-errors.md" >}})
- 延伸阅读：[游戏项目自动化构建：Jenkins / GitHub Actions 打包流水线]({{< relref "code-quality/game-project-build-pipeline-jenkins-github-actions.md" >}})
