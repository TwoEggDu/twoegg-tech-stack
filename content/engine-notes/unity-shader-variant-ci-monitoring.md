+++
title = "Shader Variant 数量监控与 CI 集成：怎么把变体治理接入构建流程"
description = "把 shader variant 数量监控拆成构建期采集、基线对比、阈值告警和 CI 集成，讲清怎样把变体治理从人工排查变成可持续运转的自动化机制。"
slug = "unity-shader-variant-ci-monitoring"
weight = 62
featured = false
tags = ["Unity", "Shader", "Build", "CI", "Variant"]
series = ["Unity 资产系统与序列化", "Unity Shader Variant 治理"]
+++

前面这条线，把 shader variant 的原理、工具选择、运行时机制和排查流程都覆盖了。

但还差最后一块：

`怎么让这些东西持续工作，而不是只在出事故时才用一次。`

没有自动化监控，变体治理的常态就是：

- 版本发了才发现变体数量又涨了
- 某次迭代悄悄引入了一个 `multi_compile`，两周后构建开始变慢
- 线上出事故后，回头查才发现早几个版本就开始偏了
- 每次手动跑一遍审计，但没人保证每次都跑

所以这篇只讲一件事：

`怎么把 shader variant 的数量监控接入构建流程，让问题在进入主干之前就被发现。`

## 先给一句总判断

`Shader variant 监控的核心不是追求"永远不变"，而是让每一次变化都有迹可查、超出预期时立刻发出信号；接入 CI 的目的是把人工判断变成自动门禁。`

## 一、先建采集：让每次构建都输出一份变体报告

监控的前提是数据。没有每次构建的变体记录，就没有对比基础。

### 1. 采集入口：IPreprocessShaders

前面实操篇已经讲过，采集变体数据的入口是 `IPreprocessShaders`。这里的重点是**输出格式**：

要让数据可以被程序对比，格式必须稳定、可聚合。建议最终输出 JSON 或 CSV，每条记录包含：

```json
{
  "shaderName": "Universal Render Pipeline/Lit",
  "passName": "ForwardLit",
  "passType": "Normal",
  "stage": "Vertex",
  "keywords": ["_MAIN_LIGHT_SHADOWS", "_ADDITIONAL_LIGHTS"],
  "platform": "Android",
  "graphicsAPI": "Vulkan"
}
```

### 2. 聚合统计：从明细到汇总

明细数据用于排查，汇总数据用于监控。每次构建后，在报告基础上生成一份汇总：

```json
{
  "buildDate": "2026-03-24",
  "gitCommit": "ddf1c1f",
  "platform": "Android",
  "totalVariants": 4823,
  "perShader": {
    "Universal Render Pipeline/Lit": 1204,
    "Universal Render Pipeline/Particles/Unlit": 312
  }
}
```

这份汇总是后续对比和告警的基础。

### 3. 输出路径

建议把汇总文件输出到一个固定路径（比如 `Build/ShaderVariantReport.json`），便于 CI 脚本读取和存档。

## 二、建立基线：什么是"正常的"变体数量

有了每次的数据之后，需要一个基线来判断"这次是否正常"。

### 1. 基线的来源

不要手动设一个固定数字作为基线，那样很难维护。更稳的做法是：

**以上一个稳定版本的报告作为动态基线。**

每次构建通过后，把这次的报告存档为新基线。这样基线会随着项目正常演进而更新，不会因为有意的变体增加而反复误报。

### 2. 基线存储

最简单的方案是把基线文件存在 git 仓库里（比如 `Build/ShaderVariantBaseline.json`），每次通过验证后提交更新。

这样有两个好处：

- 基线变化有 git 记录，能追溯是谁在什么时候改的
- PR 里如果包含基线更新，review 时自然会看到变体数量的变化

## 三、对比逻辑：发现哪些变化

每次构建后，拿新报告和基线做对比，核心要检查三类变化：

### 1. 总量变化

```python
delta = current["totalVariants"] - baseline["totalVariants"]
delta_percent = delta / baseline["totalVariants"] * 100
```

如果总量增长超过某个阈值（比如 5%），触发告警。

### 2. 单个 shader 变化

只看总量容易遮住问题。某个 shader 变体数量翻倍，但总量只涨了 2%，仅看总量不会报警。

对比每个 shader 的变体数，找出增幅最大的几个：

```python
for shader, count in current["perShader"].items():
    baseline_count = baseline["perShader"].get(shader, 0)
    if baseline_count > 0:
        growth = (count - baseline_count) / baseline_count
        if growth > 0.2:  # 单个 shader 增长超过 20%
            flag_as_warning(shader, baseline_count, count)
```

### 3. 新增 shader

如果某个 shader 在基线里不存在，但在本次构建里出现了，通常意味着有新 shader 被引入了构建。这本身不一定是问题，但应该被显式报出来，让人知道。

## 四、CI 集成：把对比变成门禁

有了采集和对比逻辑，就可以接入 CI。

### 1. 基本流程

```
CI 触发构建
  → Unity 构建 (IPreprocessShaders 生成报告)
  → 读取当前报告 + 基线报告
  → 运行对比脚本
  → 超出阈值 → CI 失败 / 发出通知
  → 未超阈值 → CI 通过 → 更新基线存档
```

### 2. 对比脚本示例

下面是一个够用的 Python 脚本骨架：

```python
import json
import sys

def load_report(path):
    with open(path) as f:
        return json.load(f)

def compare(current_path, baseline_path, threshold_total=0.05, threshold_shader=0.20):
    current = load_report(current_path)
    baseline = load_report(baseline_path)

    warnings = []
    failures = []

    # 总量检查
    total_delta = current["totalVariants"] - baseline["totalVariants"]
    total_pct = total_delta / baseline["totalVariants"]
    if total_pct > threshold_total:
        failures.append(
            f"总变体数增长 {total_pct:.1%}（基线 {baseline['totalVariants']} → 当前 {current['totalVariants']}）"
        )

    # 单 shader 检查
    for shader, count in current["perShader"].items():
        base = baseline["perShader"].get(shader)
        if base is None:
            warnings.append(f"新增 shader：{shader}（{count} 个变体）")
        elif base > 0:
            growth = (count - base) / base
            if growth > threshold_shader:
                failures.append(
                    f"{shader} 变体增长 {growth:.1%}（{base} → {count}）"
                )

    return failures, warnings

failures, warnings = compare(
    sys.argv[1],  # 当前报告路径
    sys.argv[2],  # 基线报告路径
)

for w in warnings:
    print(f"[WARN] {w}")
for f in failures:
    print(f"[FAIL] {f}")

if failures:
    sys.exit(1)  # CI 失败
```

### 3. 通知方式

CI 失败本身就是一种通知，但如果想更主动，可以在脚本里加：

- 钉钉 / 飞书 / Slack Webhook，把变体增长的具体 shader 和数量发到群里
- 在 PR 的 comment 里自动贴出 diff 摘要

变体数量增长的通知需要有人去看，所以格式要直接：

```
⚠️ Shader Variant 变化报告
总变体数：4823 → 5234（+411，+8.5%）

增长最多的 shader：
  Universal Render Pipeline/Lit: 1204 → 1508（+304，+25.2%）
  Custom/FX_Particle: 88 → 176（+88，+100%）

新增 shader：
  Custom/UI_Blur（312 个变体）
```

## 五、阈值怎么定

没有通用答案，但有几个参考原则：

| 场景 | 建议阈值 |
|------|---------|
| 总变体数单次增长 | > 5% 告警，> 15% 阻断 |
| 单个 shader 增长 | > 20% 告警，> 50% 阻断 |
| 新增 shader | 告警（不阻断，但需要人工确认） |
| 变体数减少 | 只告警不阻断（可能是正常 stripping，也可能是漏保护） |

变体数**减少**也要关注，尤其是幅度大的情况——可能是 stripping 规则误伤了关键路径。

## 六、基线更新策略

基线不是一成不变的，需要一套明确的更新策略，否则会变成两种极端：

- 基线永不更新 → 每次有意增加变体都要手动绕过检查
- 基线每次都自动更新 → 监控形同虚设，问题会悄悄累积

推荐的策略：

1. **主干构建通过后，自动更新基线**——正常迭代不需要人工干预
2. **大版本里程碑前，做一次人工审查**——确认当前基线是否合理
3. **基线更新提交进 git**——历史可查，变化可追溯
4. **对于有意的变体增加**（比如新增了一个 shader），在 PR 里写清楚原因，同时更新基线

## 七、最小落地方案

如果现在想快速接入但不想一步到位，最小可行版本是：

**第一步**（一天内）：接 `IPreprocessShaders`，每次构建输出一份汇总 JSON，包含总量和每个 shader 的数量。

**第二步**（一天内）：写一个简单对比脚本，手动跑，把结果发到群里。

**第三步**（按需）：接入 CI，加阈值检查，超限时发通知。

**第四步**（按需）：把基线存入 git，让每次变化有历史记录。

不需要一步做完。第一步和第二步加起来，已经能让团队从"完全不知道变体数量在涨"变成"每次迭代都能感知到变化"。

## 最后收成一句话

`Shader variant 监控的目的不是阻止变化，而是让每一次变化都被感知到；接入 CI 的最小路径是：IPreprocessShaders 输出报告，对比脚本检测增长，超阈值时阻断或通知。`
