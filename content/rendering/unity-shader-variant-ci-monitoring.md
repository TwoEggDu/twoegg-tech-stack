---
date: "2026-03-24"
lastmod: "2026-10-26"
title: "Shader Variant 数量监控与 CI 集成：怎么把变体治理接入构建流程"
description: "把 shader variant 数量监控拆成构建期采集、基线对比、阈值告警和 CI 集成，讲清怎样把变体治理从人工排查变成可持续运转的自动化机制。"
slug: "unity-shader-variant-ci-monitoring"
weight: 150
featured: false
tags:
  - "Unity"
  - "Shader"
  - "Build"
  - "CI"
  - "Variant"
series: "Unity Shader Variant 治理"
---
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

> 版本与证据范围：回调与字段按 Unity 6000.0 文档核对。JSON、数字、阈值和对比脚本是教学示例，没有实际 Unity 采集器、构建回执、CI 门禁或目标 Player 命中结果。本文监控的是标明观察点的数量变化，数量通过不代表动态覆盖通过。

## 先给一句总判断

`Shader variant 监控的核心不是追求"永远不变"，而是让每一次变化都有迹可查、超出预期时立刻发出信号；接入 CI 的目的是把人工判断变成自动门禁。`

## 一、先建采集：让每次构建都输出一份变体报告

监控的前提是数据。没有每次构建的变体记录，就没有对比基础。

### 1. 采集入口：IPreprocessShaders

`IPreprocessShaders.OnProcessShader` 可以观察当前 Shader snippet 的 `ShaderCompilerData` 列表。它在编译处理链上被调用，处理器按 `callbackOrder` 排序；在本处理器入口记下的 `data.Count`，只是此处看到的候选快照，不是全部原始候选，也不是最终进包数量，见 [OnProcessShader](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Build.IPreprocessShaders.OnProcessShader.html)。该版本文档还说明：已在缓存中的 Shader 不调用此回调，因此一次成功的增量构建不能仅靠本轮回调得到完整总量。

要分清四层：声明推导的理论组合、本回调观察到的编译条目、后续构建/产物能确认的保留内容，以及 Player 真正需要并命中的组合。本篇以第二层做可比监控，其他层另留证据，不用一个“总变体数”包办，详见[裁剪层次]({{< relref "rendering/unity-shader-variant-stripping-stages-prefiltering-builtin-custom.md" >}})。

要让数据可以被程序对比，格式必须稳定、可聚合。建议最终输出 JSON 或 CSV，每条记录包含：

```json
{
  "shaderGuid": "00000000000000000000000000000001",
  "shaderName": "Universal Render Pipeline/Lit",
  "passName": "ForwardLit",
  "passType": "Normal",
  "shaderStage": "Vertex",
  "keywords": ["_MAIN_LIGHT_SHADOWS", "_ADDITIONAL_LIGHTS"],
  "platformKeywords": [],
  "shaderCompilerPlatform": "Vulkan",
  "graphicsTier": "Tier1",
  "countPoint": "OnProcessShader.entry"
}
```

这是字段形状，不是某次真实回调记录。`shaderStage` 表示 Vertex/Fragment 等程序阶段，`countPoint` 表示采集发生在哪里，两者不能混称 stage。前者及 pass 信息来自 [ShaderSnippetData](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.ShaderSnippetData.html)，编译平台、tier、平台/Shader 关键字来自 [ShaderCompilerData](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.ShaderCompilerData.html)；构建目标和配置还需由本次构建上下文记录，不能只由编译平台名称猜测。

同一关键字组合可能出现在不同 pass、程序阶段、tier 或编译平台，不能合并后当作同一条目。pass 名称也未必唯一；需保留回调/snippet 关联和可用 pass 标识，跨构建的稳定映射另行确认。关键词排序只用于规范化展示，不能抹掉其他维度。本例不覆盖 Compute Shader，其回调需另列。

### 2. 聚合统计：从明细到汇总

明细数据用于排查，汇总数据用于监控。每次构建结束后生成带口径的汇总；下面的数字均为虚构，`perShader` 使用示例资产身份，名称只作展示：

```json
{
  "schemaVersion": 1,
  "buildId": "teaching-current",
  "sourceRevision": "example-revision",
  "complete": true,
  "comparisonKey": {
    "unityVersion": "<完整补丁版本>",
    "pipelineVersion": "<管线包版本>",
    "buildTarget": "Android",
    "graphicsAPIs": ["Vulkan"],
    "artifactKind": "Player",
    "contentScope": "<构建入口与资源范围标识>",
    "buildMode": "clean-shader-build",
    "settingsDigest": "<构建及裁剪配置摘要>",
    "collectorVersion": "example-v1",
    "callbackOrder": 0,
    "countPoint": "OnProcessShader.entry",
    "countUnit": "compiler-data-entry"
  },
  "totalObservedEntries": 1516,
  "perShader": {
    "guid:00000000000000000000000000000001": 1204,
    "guid:00000000000000000000000000000002": 312
  }
}
```

这里的总量是选定范围内、同一观察点的条目计数，每条只计一次；多观察点、重复采集和不同产物不能直接相加。示例两个 Shader 的小计等于 1516，实际报告必须校验明细与汇总。缺失、失败或不完整的采集不能伪装成零。`complete` 由采集器依据构建完成、缓存策略和覆盖记录产生，不能只写成 true；本例选择按文档要求触发全部相关 Shader 回调的 clean 构建，不把增量缺失当成零。

同一统计范围下才比较趋势：Unity/管线版本、构建目标/API、Player 或 Bundle、构建范围、相关设置、采集器版本和观察点均需一致；源码提交标识用于追溯变化，不要求相同。口径变化时另建经审查的基线。资产可用 GUID 等身份区分，同名 Shader 不能混合；无资产 GUID 的内置对象需另定身份规则。

### 3. 输出路径

建议按构建身份和产物保存，例如 `Build/<buildId>/<artifactKind>/ShaderVariantReport.json`，同时保留明细、日志与完成状态。固定文件名可作入口，但要防止失败或并行构建读到上一轮文件；增量/缓存路径是否覆盖完整也需验证，不能把没观察到自动当成不存在。

## 二、建立基线：什么是"正常的"变体数量

有了每次的数据之后，需要一个基线来判断"这次是否正常"。

### 1. 基线的来源

不要手动设一个固定数字作为基线，那样很难维护。更稳的做法是：

**以同口径、经过审查的稳定构建报告作为基线。**

每次构建可以自动归档当前报告，但归档不等于提升为基线。构建成功和数量未越线，也不能证明保留或动态覆盖正确；更新依据需包含改动原因、报告有效性和相应验收记录，避免连续的小幅增长被自动吸收。

### 2. 基线存储

最简单的方案是把基线文件存在 git 仓库里（比如 `Build/ShaderVariantBaseline.json`），每次通过验证后提交更新。

这样有两个好处：

- 基线变化有 git 记录，能追溯是谁在什么时候改的
- PR 里如果包含基线更新，review 时自然会看到变体数量的变化

## 三、对比逻辑：发现哪些变化

每次构建后，拿新报告和基线做对比，核心要检查三类变化：

### 1. 总量变化

```python
delta = current["totalObservedEntries"] - baseline["totalObservedEntries"]
base = baseline["totalObservedEntries"]
delta_percent = None if base == 0 else delta / base * 100
```

通过口径与完整性检查后，再按项目阈值告警。零基线下比例没有定义，应报告绝对变化；减少也要复查。本例不提供通用的百分比标准。

### 2. 单个 shader 变化

只看总量容易遮住问题。某个 shader 变体数量翻倍，但总量只涨了 2%，仅看总量不会报警。

按稳定身份对比两份报告的 Shader 并集，同时列出绝对增减与可定义的增长比例。新增、缺失和零基线单独报告，不用 `get(..., 0)` 把“未记录”直接改成“确认为零”；具体骨架见下节。

### 3. 新增 shader

基线里没有、本次有的身份，只能先标为“本观察点新增”；也可能是采集或输入变化，不能直接认定是新增源码。基线有、本次没有的身份也要列出，不自动当作零成本优化。

## 四、CI 集成：把对比变成门禁

有了采集和对比逻辑，就可以接入 CI。

### 1. 基本流程

```
CI 触发带身份的构建
  → Unity 构建并采集指定观察点，结束后校验报告完整性
  → 读取本轮报告 + 同口径基线报告
  → 校验版本、范围、阶段与数据一致性
  → 运行对比脚本
  → 数据无效或超出项目规则 → 阻断 / 提示审查
  → 数量检查通过 → 归档当前报告，继续保留/交付/动态验收
  → 按审查规则决定是否提升为基线
```

### 2. 对比脚本示例

下面是教学对比骨架，假定采集器已经写出前述报告。它只验证同口径汇总并报告变化，不采集 Unity 数据、不检查最终包内二进制，也不代替动态覆盖；阈值由外部项目配置传入：

```python
import json
import math
import sys

def load_report(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)

def validate(report):
    if report.get("schemaVersion") != 1 or report.get("complete") is not True:
        raise ValueError("报告版本不支持或采集未完成")
    required = {
        "unityVersion", "pipelineVersion", "buildTarget", "graphicsAPIs",
        "artifactKind", "contentScope", "buildMode", "settingsDigest", "collectorVersion",
        "callbackOrder", "countPoint", "countUnit",
    }
    key = report.get("comparisonKey", {})
    if not required.issubset(key) or any(key[k] in (None, "", []) for k in required):
        raise ValueError("缺少统计口径")
    counts = report["perShader"]
    total = report["totalObservedEntries"]
    if (type(total) is not int or total < 0
            or any(type(v) is not int or v < 0 for v in counts.values())
            or sum(counts.values()) != total):
        raise ValueError("汇总数据不一致")

def compare(current_path, baseline_path, threshold_total, threshold_shader):
    if any(not math.isfinite(v) or v < 0 for v in (threshold_total, threshold_shader)):
        raise ValueError("增长阈值必须是有限的非负比例")
    current = load_report(current_path)
    baseline = load_report(baseline_path)
    validate(current)
    validate(baseline)
    if current["comparisonKey"] != baseline["comparisonKey"]:
        raise ValueError("统计口径不同，需另行审查基线")

    warnings = []
    failures = []

    # 总量是本观察点的条目数，不是最终交付变体数。
    total_delta = current["totalObservedEntries"] - baseline["totalObservedEntries"]
    base_total = baseline["totalObservedEntries"]
    if base_total == 0 and total_delta > 0:
        warnings.append(f"零基线新增 {total_delta} 条，需审查绝对预算")
    elif base_total > 0 and total_delta / base_total > threshold_total:
        failures.append(
            f"观察条目增长 {total_delta / base_total:.1%}"
        )
    if total_delta < 0:
        warnings.append(f"观察条目减少 {-total_delta} 条，需复查保留与覆盖")

    # 单 shader 检查
    for shader in sorted(set(current["perShader"]) | set(baseline["perShader"])):
        count = current["perShader"].get(shader)
        base = baseline["perShader"].get(shader)
        if base is None:
            warnings.append(f"本观察点新增 Shader：{shader}（{count} 条）")
        elif count is None:
            warnings.append(f"本观察点缺少 Shader：{shader}（原 {base} 条）")
        elif count < base:
            warnings.append(f"{shader} 条目减少：{base} → {count}")
        elif base == 0 and count > 0:
            warnings.append(f"{shader} 从零增至 {count}，需审查绝对预算")
        elif base > 0:
            growth = (count - base) / base
            if growth > threshold_shader:
                failures.append(
                    f"{shader} 变体增长 {growth:.1%}（{base} → {count}）"
                )

    return failures, warnings

if __name__ == "__main__":
    # 用法：script.py 当前报告 基线报告 总量增长阈值 单 Shader 增长阈值
    failures, warnings = compare(sys.argv[1], sys.argv[2], float(sys.argv[3]), float(sys.argv[4]))
    for w in warnings:
        print(f"[WARN] {w}")
    for f in failures:
        print(f"[FAIL] {f}")
    sys.exit(1 if failures else 0)
```

本轮仅用合成 JSON 检查了 13 组情况，包括零基线、增长/减少、身份新增/缺失、观察点或构建模式不一致、报告不完整和汇总错误；这只验证上面 Python 对比逻辑的这些分支，没有运行 Unity 采集器或 CI。

### 3. 通知方式

CI 失败本身就是一种通知，但如果想更主动，可以在脚本里加：

- 钉钉 / 飞书 / Slack Webhook，把变体增长的具体 shader 和数量发到群里
- 在 PR 的 comment 里自动贴出 diff 摘要

通知应带构建身份、口径和基线链接；以下只是虚构的展示样式，同名 Shader 实际应附身份，不能从通知文字反推真实数据：

```
⚠️ Shader Variant 变化报告
观察点：OnProcessShader.entry；单位：compiler-data-entry
观察条目：1516 → 1940（+424，+28.0%）

增长最多的 shader：
  Universal Render Pipeline/Lit: 1204 → 1508（+304，+25.2%）
  Universal Render Pipeline/Particles/Unlit: 312 → 344（+32，+10.3%）

新增 shader：
  Custom/UI_Blur（88 条）
```

## 五、阈值怎么定

没有通用答案，但有几个参考原则：

| 场景 | 项目需要定义的规则 |
|------|---------|
| 观察总量或单 Shader 增长 | 结合绝对量、比例、构建预算与变更原因设告警/阻断，本文不定通用百分比 |
| 新增、缺失或零基线 | 显式报告，检查输入、范围与绝对预算，不跳过比较 |
| 条目减少 | 复查过滤与动态需求；若违反保留契约可阻断，不能统一只告警 |
| 报告缺失、不完整或口径改变 | 不计为数量通过，先修采集或审查新基线 |

变体数**减少**也要关注，尤其是幅度大的情况——可能是 stripping 规则误伤了关键路径。

## 六、基线更新策略

基线不是一成不变的，需要一套明确的更新策略，否则会变成两种极端：

- 基线永不更新 → 每次有意增加变体都要手动绕过检查
- 基线每次都自动更新 → 监控形同虚设，问题会悄悄累积

推荐的策略：

1. **主干构建完成后自动归档，按审查规则提升基线**——数量检查通过不能单独触发提升
2. **大版本里程碑前，做一次人工审查**——确认当前基线是否合理
3. **基线更新提交进 git**——历史可查，变化可追溯
4. **对于有意的变体变化**，在 PR 里写清原因和验收范围，审查后更新对应口径的基线

## 七、最小落地方案

如果现在想快速接入但不想一步到位，最小可行版本是：

**第一步**：选定版本、构建与采集观察点，接 `IPreprocessShaders`，输出带完整性和范围记录的明细/汇总。

**第二步**：用零基线、增长、减少、身份新增/缺失和口径变化的样例检查对比逻辑，再与真实构建报告对照。

**第三步**（按需）：接入 CI，加阈值检查，超限时发通知。

**第四步**（按需）：把基线存入 git，让每次变化有历史记录。

不需要一步做完。第一步和第二步加起来，已经能让团队从"完全不知道变体数量在涨"变成"每次迭代都能感知到变化"。

数量趋势只帮助定位下一步该查什么。还需将构建回执与 Player/Bundle、目标 API 和动态路线对应，确认需求确实保留、交付并可用，见[动态覆盖契约]({{< relref "rendering/unity-shader-variant-collection-coverage-and-keyword-contract.md" >}})。

<!-- DATA-TODO: 实现并验证采集器，固定完整 Unity/管线版本、回调顺序、构建范围/API 和 Player/Bundle 身份；检查全量/增量/缓存构建、重复采集、失败和并行报告的完整性。对照本观察点、构建保留/产物与目标动态路线，保留日志和回执；尚无 Unity 采集、CI 或运行命中结果。 -->

## 官方文档参考

- [Unity 6000.0 OnProcessShader](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Build.IPreprocessShaders.OnProcessShader.html)

## 最后收成一句话

`先确认每个数字在哪一层、按什么范围得到，再比较同口径变化；数量门禁与动态覆盖各自保留证据。`
