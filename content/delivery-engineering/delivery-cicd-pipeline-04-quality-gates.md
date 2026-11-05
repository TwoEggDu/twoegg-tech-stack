---
title: "CI/CD 管线 04｜质量门自动化——编译、资源、Shader 变体、性能基线"
slug: "delivery-cicd-pipeline-04-quality-gates"
date: "2026-04-14"
lastmod: "2026-10-31"
description: "把质量门结论绑定候选、规则、环境与覆盖范围，区分检查失败、执行异常、跳过与过期，修正编译判定示例，并说明基线、豁免和维护责任。"
tags:
  - "Delivery Engineering"
  - "CI/CD"
  - "Quality Gates"
series: "CI/CD 管线"
primary_series: "delivery-cicd-pipeline"
series_role: "article"
series_order: 40
weight: 1540
delivery_layer: "principle"
delivery_volume: "V16"
delivery_reading_lines:
  - "L1"
  - "L2"
---

## 这篇解决什么问题

V16-03 讲了构建自动化——代码能编译通过、产物能打出来。但"能打出来"不等于"质量达标"。质量门的作用是在构建产物到达 QA 之前，自动拦住确定性问题——不需要人判断就能确定"不达标"的东西，交给机器去挡。

> 范围与证据：Unity 契约按 2022.3 LTS、脚本运行语义按 Python 3.12 官方文档核对。本文已用教学子进程复现旧示例的误放行，并验证修订脚本的正反分支；没有运行真实 Unity 检查入口、设备性能测试或生产 CI，不将协议验证写成编译、性能或发布验收。

门禁阶段、阻断与人工判断复用 [Quality Gate 的职责说明]({{< relref "code-quality/quality-gate-how-to-connect-automation-and-ai-review.md" >}})；检查哪些入口与历史风险，复用[烟测与回归最小链]({{< relref "code-quality/smoke-tests-and-regression-minimal-chain.md" >}})；输入与产物身份复用[构建系统基础]({{< relref "delivery-engineering/delivery-multiplatform-build-01-fundamentals.md" >}})。本篇负责结果如何进入自动判定，不重建一套质量体系。

## 先限定结果的有效范围

“这次检查通过”至少要能回答：**检查了哪份候选／产物，用哪版规则和基线，在什么工具与环境中，实际覆盖了哪些对象？** 报告还要带运行身份、起止与完成状态、证据位置、有效条件和后续责任人。

| 状态 | 本文采用的含义 | 门禁怎样处理 |
| --- | --- | --- |
| 通过 | 检查完成、对象和规则匹配，已覆盖范围满足接受条件 | 只接受该范围，不推成全部质量已达标 |
| 检查不通过 | 检查完成，发现违反规则或预算的结果 | 按该项阻断／告警级别处理并复验 |
| 执行异常 | 工具启动失败、超时、报告缺失／损坏或采集不完整 | 保留异常，必需项不能按通过处理 |
| 未执行／跳过 | 没跑，或按指定通道未安排该项 | 不生成通过结论；发布所需项仍须补齐或明确例外 |
| 失效／过期 | 候选、规则、基线或适用条件已变化，旧结果不再覆盖 | 按影响重新执行或形成有依据的复用决定 |

不是所有检查都要重跑，也不是所有结果都能复用。先定义依赖与覆盖关系：输入变化是否影响该项、环境是否仍可比、规则和基线是否相同、必要证据是否完整。只有这些条件成立时才接受复用，不能按“上一次流水线是绿的”放行当前候选。

规则版本、设备／平台范围、结果有效期和例外条件属于受控策略。聚合器分别保存**检查事实、自动判定和最终接受决定**；临时放行不应改写原本失败或未执行的事实。

## 五类质量检查

### 检查一：编译检查

**目标：零 Error、Warning 在预算内。**

| 检查项 | 工具 | 阈值 | 失败处理 |
|--------|------|------|---------|
| 编译 Error | 已实现的项目检查入口、执行状态与完整诊断报告 | 指定范围错误数为 0 | 不通过或执行异常均不能接受为通过 |
| 编译 Warning | Unity 构建日志解析 | ≤N 条（团队约定） | 超预算标记失败 |
| Lint / 静态分析 | Roslyn Analyzer / 自定义规则 | 严重级别零容忍 | 管线终止 |
| 代码规范 | .editorconfig + 格式化检查 | 按已批准规则 | 自动修复形成新候选，相关结果重新核对 |

**Warning 预算的管理方法**：

1. 固定诊断来源、平台、编译范围和规则版本，保留稳定诊断身份，而不只留总数。
2. 区分存量预算与新增诊断：总数不变，也可能删除了一个旧问题又引入一个新问题。
3. 预算与基线由责任人按成本、风险和实际样本调整，变更应被审阅并可追溯，不要求每个迭代机械递减。
4. 抑制、忽略和豁免纳入审阅，不能靠隐藏诊断满足预算；零 Warning 也不证明功能正确。

### 教学脚本：先确认执行，再接受报告 {#实际的-ci-质量门脚本示例}

旧示例保存了 `BUILD_EXIT` 却未检查，并把 `grep -c` 的无匹配返回与备用 `echo 0` 拼接为非整数。隔离复现中，教学子进程返回 7，旧脚本仍输出 PASS 并返回 0。仅改一条日志正则不足以覆盖启动、超时、日志缺失和候选错配。

修订采用 Python 3.12 标准库编排，避免跨 Windows／macOS／Linux 照搬 Bash 路径与工具语义。下面是**教学判定器**，不是项目中已部署的 CI：它只接受本次运行的完整编译检查报告，不负责生成真实 Unity 诊断，也不实现性能、资源或发布总门禁。

先准备受控的候选清单和规则文件。`candidate.json` 应指向实际输入及目标，详细身份模型沿用构建基础；示意规则为 `{"schema":1,"check":"compile","warning_budget":20}`。预算 20 仅用于演示。保存以下代码为 `compile_gate.py`：

```python
import argparse
import hashlib
import json
import math
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import uuid


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--candidate', type=Path, required=True)
    parser.add_argument('--policy', type=Path, required=True)
    parser.add_argument('--evidence-dir', type=Path, required=True)
    parser.add_argument('--timeout', type=float, required=True)
    parser.add_argument('command', nargs=argparse.REMAINDER)
    args = parser.parse_args()
    command = args.command[1:] if args.command[:1] == ['--'] else args.command
    try:
        if not command or not math.isfinite(args.timeout) or args.timeout <= 0:
            raise ValueError('command and positive finite timeout required')
        candidate = args.candidate.read_bytes()
        policy = args.policy.read_bytes()
        if not isinstance(json.loads(candidate), dict):
            raise ValueError('candidate must be a JSON object')
        rule = json.loads(policy)
        if not isinstance(rule, dict) or type(rule.get('schema')) is not int:
            raise ValueError('invalid policy')
        budget = rule.get('warning_budget')
        if rule['schema'] != 1 or rule.get('check') != 'compile' or type(budget) is not int or budget < 0:
            raise ValueError('invalid compile policy or warning budget')
        run_id = uuid.uuid4().hex
        args.evidence_dir.mkdir(parents=True, exist_ok=True)
        run_dir = Path(tempfile.mkdtemp(prefix=run_id + '-', dir=args.evidence_dir)).resolve()
        report_path = run_dir / 'report.json'
        (run_dir / 'candidate.json').write_bytes(candidate)
        (run_dir / 'policy.json').write_bytes(policy)
        expected = {
            'run_id': run_id,
            'candidate_sha256': hashlib.sha256(candidate).hexdigest(),
            'policy_sha256': hashlib.sha256(policy).hexdigest(),
        }
        env = os.environ.copy()
        env.update({
            'GATE_RUN_ID': run_id,
            'GATE_CANDIDATE_SHA256': expected['candidate_sha256'],
            'GATE_POLICY_SHA256': expected['policy_sha256'],
            'GATE_REPORT_PATH': str(report_path),
            'GATE_CANDIDATE_PATH': str(run_dir / 'candidate.json'),
            'GATE_POLICY_PATH': str(run_dir / 'policy.json'),
        })
        print('Evidence:', run_dir, flush=True)
        with (run_dir / 'console.log').open('wb') as log:
            result = subprocess.run(command, env=env, stdin=subprocess.DEVNULL,
                                    stdout=log, stderr=subprocess.STDOUT,
                                    timeout=args.timeout, check=False)
        if result.returncode != 0:
            raise ValueError(f'check process exited {result.returncode}')
        report = json.loads(report_path.read_text(encoding='utf-8'))
        if not isinstance(report, dict) or type(report.get('schema')) is not int:
            raise ValueError('invalid report')
        if report['schema'] != 1 or report.get('check') != 'compile':
            raise ValueError('wrong report schema or check')
        if any(report.get(key) != value for key, value in expected.items()):
            raise ValueError('report does not match this run/candidate/policy')
        if report.get('status') != 'completed':
            raise ValueError('check did not complete; skipped is not passed')
        counts = [report.get('errors'), report.get('warnings')]
        if any(type(value) is not int or value < 0 for value in counts):
            raise ValueError('missing or invalid diagnostic counts')
        errors, warnings = counts
        if errors or warnings > budget:
            print(f'FAIL: {errors} errors, {warnings}/{budget} warnings')
            return 1
        print(f'PASS: compile scope only; {warnings}/{budget} warnings')
        return 0
    except subprocess.TimeoutExpired:
        print('ERROR: check timed out', file=sys.stderr)
        return 2
    except (OSError, ValueError, TypeError) as exc:
        print(f'ERROR: {exc}', file=sys.stderr)
        return 2


if __name__ == '__main__':
    raise SystemExit(main())
```

调用形式如下，检查程序与参数需替换为实际入口；600 秒只是演示配置，不是通用超时标准：

```text
python compile_gate.py --candidate candidate.json --policy compile-policy.json --evidence-dir gate-runs --timeout 600 -- <检查程序> <参数>
```

检查入口从 `GATE_CANDIDATE_PATH`／`GATE_POLICY_PATH` 取得本次快照，核对真正消费的输入和规则；完成后向 `GATE_REPORT_PATH` 写入一个完整 JSON 对象。该协议为本文自定，不是 Unity 自动输出格式：

```json
{
  "schema": 1,
  "check": "compile",
  "run_id": "本次 GATE_RUN_ID",
  "candidate_sha256": "本次候选清单字节的 SHA-256",
  "policy_sha256": "本次规则文件字节的 SHA-256",
  "status": "completed",
  "errors": 0,
  "warnings": 0
}
```

`errors`／`warnings` 必须来自指定范围的实际诊断，异常或不完整采集不能填写为零。运行身份来自本次环境，清单指纹的核对不能替代实际输入核对；只原样回显几个字段不会自动证明工具使用了正确资源。这个示例只验证总量预算，不实现新增诊断差异算法或恶意报告鉴别。

Unity 接入按 [2022.3 命令行文档](https://docs.unity3d.com/2022.3/Documentation/Manual/EditorCommandLineArguments.html)核对：`-executeMethod` 对应项目实际存在的 Editor 静态入口，失败可抛异常或以非零码退出。单有 `-batchmode -buildTarget` 不证明某项检查执行完成。入口若调用构建，还应检查 [BuildSummary.result](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Build.Reporting.BuildSummary-result.html)，而非把方法返回或进程存活当作构建成功。本文未提供或运行该 Unity 报告生产端。

判定器每次创建独立证据目录，保留清单快照、控制台日志及取得的报告；非零退出、缺报告、错身份、跳过和坏数据都不能输出 PASS。返回码 0 只代表该编译协议范围通过，1 表示诊断不满足规则，2 表示无法接受有效结果；具体异常细节保留在输出与证据中，不靠一个数字区分所有原因。

按 [Python 3.12 subprocess 文档](https://docs.python.org/3.12/library/subprocess.html)，`run(timeout=...)` 超时会终止并等待直接子进程，但进程创建耗时不保证被该时限中断。真实 CI 还需按执行器处理后代进程、取消、清理及证据保留；本文没有验证 Unity 子进程树或全部 OS 的超时行为。

### 最小正反验证：验证判定器，不冒充引擎验收

本轮在 Windows、Python 3.12.4 下实际运行正文同一份代码，使用 Python 教学子进程写入协议报告；旧 Bash 片段另在 Git Bash 的隔离目录复现。修订后的 22 个场景均符合预期：

| 场景 | 预期与本轮结果 |
| --- | --- |
| 零错误，告警为零或恰好等于预算 | 返回 0，输出 PASS，2 项通过 |
| 有错误或告警超预算 | 返回 1，无 PASS，2 项通过 |
| 子进程返回非零，即使留有正常报告 | 返回 2，无 PASS，1 项通过 |
| 缺失、损坏、不完整报告或缺计数字段 | 返回 2，无 PASS，4 项通过 |
| 跳过、生产端异常、检查类型／schema 不匹配 | 返回 2，无 PASS，5 项通过 |
| 旧运行、旧候选或旧规则的报告 | 返回 2，无 PASS，3 项通过 |
| 非法计数、非法规则预算 | 返回 2，无 PASS，3 项通过 |
| 超时、检查程序不存在 | 返回 2，无 PASS，2 项通过 |

这些结果证明本次受测判定分支的行为；不证明 Unity 已编译、计数来源完整或团队 CI 已采用。后续还需目标工程验证静态入口、实际诊断、生成报告与取消／恢复，并在 macOS／Linux 等所用执行环境复测。候选变化的负对照只验证报告指纹不匹配被拒绝，不证明已经建立全部输入的自动失效系统。

<!-- DATA-TODO: 在目标 Unity 工程接入报告生产端，记录 Editor 与目标平台版本、输入与规则身份、真实编译成功/错误、诊断完整性和超时取消证据；设备性能另按同口径基线验证，不以本轮教学协议测试替代。 -->

原 `#pragma warning disable` 检查也不能仅对 `HEAD~1` 做一次搜索就称覆盖完整候选。按实际基准与候选范围检查新增抑制、诊断规则变动及例外，保留人工审阅；规则文件可在版本库或受控策略系统管理，关键是版本与变更责任可追溯。

### 检查二：资源合规检查

**目标：指定资源集合符合已批准的制作与导入标准。** 以下阈值和报告数字均是教学示意，实际规则需按资源用途、设备与例外条件定义。

| 检查项 | 检查内容 | 阈值 |
|--------|---------|------|
| 命名规范 | 文件名是否符合命名约定 | 不合规文件数 = 0 |
| 纹理格式 | 是否使用了约定的压缩格式（ASTC/ETC2） | 不合规文件数 = 0 |
| 纹理尺寸 | 是否超过最大尺寸限制（如 2048x2048） | 不合规文件数 = 0 |
| 模型面数 | 单个模型是否超过面数上限 | 按资源类型区分 |
| 音频格式 | 采样率和压缩格式是否合规 | 不合规文件数 = 0 |
| 疑似重复资源 | 内容、用途、导入设置和依赖是否允许合并 | 列出证据后按规则判断，不由同字节直接推定可删除 |

**实现方式**：编写 Unity Editor 脚本，在 -batchmode 下扫描项目资源目录，输出检查报告。

```
检查报告格式：
┌─────────────────────────────────────────────────┐
│ 资源合规检查报告                                    │
├──────────┬──────┬───────┬────────────────────────┤
│ 检查项    │ 通过  │ 失败   │ 详情                    │
├──────────┼──────┼───────┼────────────────────────┤
│ 命名规范  │ 1234 │ 3     │ tex_hero_01.PNG (应为小写)│
│ 纹理格式  │ 890  │ 0     │                         │
│ 纹理尺寸  │ 888  │ 2     │ bg_main.png (4096x4096) │
│ 重复检测  │ -    │ 1     │ icon_a.png = icon_b.png │
└──────────┴──────┴───────┴────────────────────────┘
```

### 检查三：Shader 变体数监控

**目标：变体总数不超过预算，无意外变体暴增。**

Shader 变体数量是包体膨胀和构建时间增长的常见原因。V05 渲染管线系列已覆盖变体管理的技术细节，这里讲 CI 集成：

| 检查项 | 数据来源 | 阈值 |
|--------|---------|------|
| 变体总数 | 构建日志中的 shader compilation 统计 | ≤N（团队约定） |
| 变体数增量 | 与同口径、已批准的基线对比 | 单次增量不超过 M |
| 单个 Shader 变体数 | 逐 Shader 统计 | 超限的 Shader 列出告警 |

**CI 集成方式**：

1. 按所用 Editor／渲染管线版本取得 Shader 变体统计
2. 记录平台、图形 API、构建配置、Shader／Pass 范围及剔除前后口径，验证解析器能识别该版本日志
3. 与范围和口径一致、已批准的基线对比，缺日志或格式变化按执行异常处理
4. 增量超阈值 → 标记失败，列出新增变体来源

变体机制复用[Shader 变体 CI 监控]({{< relref "rendering/unity-shader-variant-ci-monitoring.md" >}})；平台与图形 API 的统计范围参见 [Unity 2022.3 变体说明](https://docs.unity3d.com/2022.3/Documentation/Manual/shader-variants.html)。本文不规定一条跨版本通用日志正则。变体数变化也不等于已测得相应的运行时性能变化。

### 检查四：包体大小预算

**目标：包体大小不超过 V06 / V14 定义的预算。**

| 检查项 | 数据来源 | 阈值 |
|--------|---------|------|
| 总包体大小 | Build Report / 构建产物文件大小 | ≤预算（按渠道区分） |
| 包体增量 | 与上一个版本对比 | 单版本增量不超过 N MB |
| Top N 大文件 | Build Report 解析 | 列出便于排查 |
| 资源类型分布 | 纹理/音频/Mesh/代码各占多少 | 趋势监控 |

**教学报告示例**：下列预算为项目自定值，不是平台上限。

| 平台 | 当前大小 | 预算 | 状态 | 与上版本差异 |
|------|---------|------|------|------------|
| iOS | 186 MB | 200 MB | 通过 | +4.2 MB |
| Android APK | 142 MB | 150 MB | 通过 | +3.8 MB |
| 微信首包 | 18.5 MB | 20 MB | 通过 | +0.3 MB |

### 检查五：性能基线回归

**目标：在可比条件下，候选满足目标预算与已批准的回归判据。** 不能任取“上次成功构建”的数字作为基线；它可能不是相同设备、场景、配置或采样口径。

| 检查项 | 采集方式 | 判定标准 |
|--------|---------|---------|
| 帧时间 | 约定场景、阶段、窗口与分布 | 项目预算与同口径回归判据 |
| 内存峰值 | 明确进程／引擎指标与采样窗口 | 绝对预算、变化及测量覆盖分别记录 |
| 启动时间 | 定义冷／热条件与起止点 | 同条件对照，不只比较一个均值 |
| 加载时间 | 同一任务到约定可用条件 | 区分读取、对象就绪与首次可操作 |

按成本和风险安排快速、夜间与候选通道；不规定性能检查一律只能在完整通道运行。某项在快速通道跳过，只说明本轮未测；候选放行仍按其必需项集合判断。细节复用[分档验证与基线]({{< relref "code-quality/device-tier-validation-matrix-baseline-and-visual-regression.md" >}})和[性能诊断方法]({{< relref "performance/game-performance-diagnosis-method.md" >}})。

### 性能基线管理的适用条件 {#性能基线管理的实操经验}

**条件一：可比不只取决于设备名称。** 同机对照也需记录热／供电、后台负载、构建配置、内容、缓存和采样开销。跨设备比较应另有分组与可比性依据，不能把一台设备结果直接替代整个档位。

**条件二：更新基线需要理由和接受责任。** 基线记录设备／工具、场景、程序与内容身份、样本、算法和有效范围。内容、工具或产品预算变化时先判断是否仍可比，再由指定接受人审阅是否更新；保留新旧版本和理由，不在每次成功跑测后自动抬高基线。

**条件三：波动需要样本，不预填统一容差。** 保存多轮分布、异常与失败，区分真实退化、测试不稳和采集问题；重复次数、统计方法与容差根据目标和数据确定。三次中位数或某个固定百分比都不是通用保证，更不能靠无限重跑挑最好结果。

## 检查结果聚合

聚合报告便于决策，原始日志与各项证据仍应可追溯。下面为教学示意，假定候选 C17、规则 P3，均不是已运行系统的记录：

```
质量门聚合报告 — 候选 C17 / 规则 P3 / 本次运行 R456
══════════════════════════════════════
编译检查        ✓ 通过   (0 Error, 12 Warning, 预算 20)
资源合规检查     ✗ 失败   (3 个文件不合规)
Shader 变体     ✓ 通过   (12,340 个变体, 预算 15,000)
包体大小        ✓ 通过   (iOS 186MB / 预算 200MB)
性能基线        ○ 跳过   (快速通道不执行)
══════════════════════════════════════
总体判定：本通道不通过（资源合规检查未通过）
发布接受：未作出（性能项仍需按发布必需集合处理）
```

聚合前先检查必需项是否齐全、运行和候选是否匹配、报告是否有效，再应用阻断／告警／例外策略。缺报告、超时或旧候选结果不能从列表中消失，也不能被计成零问题；一个可选项不阻断，不代表它获得通过结论。

## 误报管理

质量门不能"经常失败但大家都忽略"——那样等于没有门。

| 误报原因 | 处理方法 |
|---------|---------|
| 阈值或采样不适合目标 | 先核对数据、口径和复现，再审阅规则调整 |
| 规则不适用于某些资源 | 明确适用范围；例外有原因、责任人与到期条件 |
| 基线过期或条件不可比 | 保留失效状态，按新旧条件对照后决定重建基线 |
| 外部依赖、工具或报告异常 | 先判断是实际问题还是执行异常，不统称误报 |

**豁免机制**：

- 例外策略受控且可审阅，可存版本库或受控策略服务；记录确切策略身份。
- 每条例外限定候选／规则／范围，包含理由、风险、批准者、补验责任与到期条件。
- 到期或依赖条件变化后重新判定；是否自动失效要由实现和验证证明，不能只写在规范里。
- 豁免不改写原检查事实，不能因临时允许推进就把失败改成通过。

| 责任 | 需要完成的动作 |
| --- | --- |
| 规则与基线维护者 | 定义覆盖、判据、有效条件及变更记录，处理误报和过时规则 |
| 检查／CI 维护者 | 维护执行、超时、报告协议和聚合，保留证据并处置基础设施异常 |
| 内容或代码负责人 | 修正所负责输入，形成新候选并申请相关复检 |
| 接受人 | 在权限范围内决定是否接受及例外，不替未执行项补造结果 |

修复后回到原受阻步骤复验，检查新候选与相关结果的对应，再将有效修法进入规则或回归。职责说明可复用既有门禁文章，不把脚本数量或连续绿灯次数当作采用与收益证据。

## 质量门与 V13 验证体系的关系

V13 讲了四层验证体系（功能/性能/稳定性/兼容性）。V16 的质量门是 V13 中"自动化验证"的实现层——把 V13 定义的验证标准变成 CI 中可执行的检查脚本。

```
V13 定义标准 → V14 定义预算 → V16 实现自动检查
```

## 小结与检查清单

- [ ] 编译检查是否实现了零 Error + Warning 预算
- [ ] 资源合规检查是否覆盖命名、格式、尺寸、重复
- [ ] Shader 变体数是否有 CI 监控和增量告警
- [ ] 包体大小是否按渠道设定预算并在 CI 中检查
- [ ] 性能基线是否在约定通道中执行，并满足候选的必需覆盖范围
- [ ] 必需结果是否齐全并绑定本次候选、运行、规则与有效条件
- [ ] 误报率是否在可接受范围（不导致团队忽略质量门）
- [ ] 执行异常、跳过、过期与例外是否独立呈现并有处理责任

---

**下一步应读**：[部署自动化]({{< relref "delivery-engineering/delivery-cicd-pipeline-05-deployment-automation.md" >}}) — fastlane、Gradle、微信 CLI 与 CDN 发布

**扩展阅读**：[验证与测试系列]({{< relref "delivery-engineering/delivery-verification-testing-series-index.md" >}}) — V13 定义了质量门背后的验证体系
