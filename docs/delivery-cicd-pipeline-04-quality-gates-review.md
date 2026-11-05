# 《CI/CD 管线 04｜质量门自动化》审校与验证记录

日期：2026-10-31。状态：原位修订与本地验证；不提交、不推送、不发布。

## 范围与复用

本轮仅修改正文 `content/delivery-engineering/delivery-cicd-pipeline-04-quality-gates.md`、canonical 系列计划 `docs/delivery-engineering-column-plan.md` 的 V16.4 新增审校节，以及本记录。系列计划已有其他未提交修改，本轮不认领或覆盖。根路由、基础与案例执行快照、索引和其他正文不修改。

以现有 G09“解决进入持续检查”的基础缺口为主，沿用专栏先讲必要基础、再补实际案例的原则。本篇解释结果怎样成为可接受的证据，支持读者判断能否推进、何时复验、谁维护规则，不把脚本数量和绿灯次数当作生产管线已落地的证明。

| 已有正文 | 复用职责 | 本次增量 |
| --- | --- | --- |
| [Quality Gate](../content/code-quality/quality-gate-how-to-connect-automation-and-ai-review.md) | 阻断阶段、人工审阅与门禁分工 | 结果事实、自动判定、最终接受分开 |
| [烟测与回归最小链](../content/code-quality/smoke-tests-and-regression-minimal-chain.md) | 入口与历史风险的检查选择 | 未执行与覆盖不足不形成通过结论 |
| [构建系统基础](../content/delivery-engineering/delivery-multiplatform-build-01-fundamentals.md) | 输入、变换、产物与候选身份 | 将报告绑定本次运行、候选清单与规则 |
| [Shader 变体 CI](../content/rendering/unity-shader-variant-ci-monitoring.md) | 变体机制与监控 | 同平台、API 与剔除口径比较，不把日志缺失当零 |
| [分档验证与基线](../content/code-quality/device-tier-validation-matrix-baseline-and-visual-regression.md)、[性能诊断](../content/performance/game-performance-diagnosis-method.md) | 设备覆盖、采样、基线与对照 | 基线变更责任、有效条件与复验范围 |

保留 slug、title、date、系列、序号、weight 和五类检查的原顺序；新增 lastmod。脚本与性能基线两个小节换为更准确的标题，并显式保留旧锚点 `实际的-ci-质量门脚本示例`、`性能基线管理的实操经验`。

## 修改与证据边界

- 补清通过、不通过、执行异常、跳过和过期；定义候选／规则／基线／环境／覆盖范围与复用条件。
- 以 Python 标准库判定器替换误放行的 Bash 示例，处理进程退出、超时、报告完整性、运行与候选／规则身份及诊断预算。
- 明确存量预算与新增诊断不同，抑制和例外需审阅；移除缺少来源的项目经历、改善数字及统一性能容差。
- 补清规则／基线维护、检查／CI 维护、输入修复与最终接受责任；例外不改写失败事实，基线不随成功运行自动更新。
- 所有演示预算、资源／包体／聚合报告均为教学示意。脚本仅验证编译报告总量预算，不实现完整告警差异、真实输入追踪、报告鉴伪或全部门禁。

## 官方资料与适用条件

资料核对日期为 2026-10-31；官方契约不等于本地工程已实现。

| 资料 | 已核对内容与采用边界 |
| --- | --- |
| [Unity 2022.3 Editor 命令行](https://docs.unity3d.com/2022.3/Documentation/Manual/EditorCommandLineArguments.html) | `-executeMethod` 执行 Editor 文件夹中静态方法；异常或显式非零退出可表达失败。`-batchmode -buildTarget` 本身不证明具体检查完成。项目检查入口与报告生产端仍须实现和验证 |
| [BuildSummary.result](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Build.Reporting.BuildSummary-result.html) | 表达构建结果；调用构建时检查其结果，不能只看调用返回或进程存活 |
| [Unity 2022.3 Shader variants](https://docs.unity3d.com/2022.3/Documentation/Manual/shader-variants.html) | 目标平台和图形 API 影响变体集合；本文要求固定统计范围，不给跨版本通用日志正则 |
| [Python 3.12 subprocess](https://docs.python.org/3.12/library/subprocess.html) | `run` 等待子进程；`check=False` 需自行检查返回码；timeout 终止并等待直接子进程，初始进程创建耗时不能保证被中断。后代进程、CI 取消与跨 OS 行为未验证 |

新脚本本轮运行环境为 Windows、Python 3.12.4；旧 Bash 复现用 Git Bash 5.2.37。新脚本不依赖 Bash／grep。没有运行真实 Unity Editor、实际诊断生产端、设备性能或生产 CI，也没有验证 macOS／Linux。没有把本轮协议测试写成作者历史项目实测。

## 先复现旧误放行

对修改前文章 Bash 代码只替换 Unity 可执行文件和日志路径，配备隔离的两提交 Git 仓库及告警预算文件。教学 shell 子进程写一条告警并分别返回 0 和 7，均不执行 Unity。

- 两次旧包装脚本均返回 0 并打印 PASS；其中子进程返回 7 的场景证明非零退出未参与判定。
- 无错误匹配时，`grep -c ... || echo 0` 产生 `0\n0`，数值比较报 `integer expression expected`，脚本仍继续。
- 旧代码摘录 SHA-256：`a1362a3ab0cd958e3986480fd60c26525b8bb37a7b7be1a218d9cbaa234b699e`。
- 复现驱动为证明错误而返回非零，属于预期的反例；不把此结果描述成真实 Unity 构建失败。

## 修订脚本最小正反验证

正文 Python fence 与实际执行的 `compile_gate.py` 逐字核对，代码 SHA-256（不含 fence、无尾换行）：`c839bfae4ff31d09d5398fb8b73fd1365c6cfa769b15f13340533852a7a51fa1`。

测试先准备反例，再实现判定器，独立子进程执行实际正文代码。共 3 个 unittest 方法、22 个场景均符合预期：

| 分组 | 场景数 | 预期及结果 |
| --- | --- | --- |
| 零告警／预算边界 | 2 | 返回 0 且打印 PASS |
| 有错误／告警超限 | 2 | 返回 1，不打印 PASS |
| 正常报告但子进程非零 | 1 | 返回 2，不打印 PASS |
| 缺失／损坏／缺字段／不完整报告 | 4 | 返回 2，不打印 PASS |
| 跳过／生产端异常／错误类型或 schema | 5 | 返回 2，不打印 PASS |
| 旧 run／candidate／policy | 3 | 返回 2，不打印 PASS |
| 负数／布尔计数／布尔预算 | 3 | 返回 2，不打印 PASS |
| 超时／不存在的检查程序 | 2 | 返回 2，不打印 PASS |

这些用例验证受测协议判定。子进程回显指纹只是测试协议，不证明真实工具消费正确输入；旧候选反例也不证明完整自动失效系统已建立。目标工程的真实诊断、报告生成、超时取消和设备性能保留为正文 DATA-TODO。

本地驱动、JSON 结果、日志和快照保存在忽略目录 `.tmp/quality-gates-20261031/`，不增加跟踪脚本或改动其他实验。下方保留可重跑驱动，避免测试依据只存在临时目录：将正文代码保存为 `compile_gate.py`，再将下方代码保存为同目录 `test_gate.py`，用 Python 3.12 执行 `python -B test_gate.py`。在独立目录操作，会新建 `protocol-tests/`、保留每轮证据和 `protocol-results.json`；其中 fake_check 仅为教学报告生产端。

## 站点与范围验证

以下均为本轮实际执行；网站检查基于当前共享工作区，不将其他未提交文章视为本次成果。

- `python -B -m unittest discover -s tests -p test_reading_layouts.py -v`：19 项通过。
- `hugo --gc --minify --destination .tmp/quality-gates-20261031/publish-check`：Hugo 0.157.0 构建成功，1443 页。
- `python -B scripts/check-reading-pages.py --public .tmp/quality-gates-20261031/publish-check`：1377 个阅读页，0 错误。
- 生成目标页包含 10 张表、完整脚本和报告协议；23 个站内目标存在，无残留 shortcode、重复 ID，两个旧锚点保留。本轮未修改样式，未宣称完成浏览器视觉验收。
- 正文代码与执行版本一致，元数据和原章节相对顺序保留；relref 全部指向已有文件。对照本轮起始快照，系列计划只新增 V16.4 审校节 6 行，其他既有改动保留。
- 从正文和本记录重新提取代码，在独立目录复跑 3 个测试方法、22 个场景，全部符合预期；留存驱动可重跑。
- 对 3329 份起始文件做哈希对照：范围外有 22 份文件在共享工作期间发生变化，未回退或纳入本轮；起始文件无缺失，暂存差异与起始记录一致。本轮没有暂存、提交、推送操作。
- `git diff --check` 对本轮已跟踪文件无错误；本记录另核对无行尾空白、UTF-8 可读。

静态检查、生成页、范围对照和本轮精确增量保存于 `.tmp/quality-gates-20261031/`。后续若提交，须使用本轮正文／新记录及计划的独立增量，不能把共享计划整个工作区差异混入。

## 可重跑协议验证驱动

```python
from pathlib import Path
import json, subprocess, sys, unittest

ROOT=Path(__file__).resolve().parent
WORK=ROOT/'protocol-tests'
WORK.mkdir(exist_ok=True)
FAKE=WORK/'fake_check.py'
FAKE.write_text('''import json, os, pathlib, sys, time
mode=sys.argv[1]
if mode=="timeout": time.sleep(4)
if mode=="missing": sys.exit(0)
p=pathlib.Path(os.environ["GATE_REPORT_PATH"])
if mode=="malformed": p.write_text("{",encoding="utf-8"); sys.exit(0)
r={"schema":1,"check":"compile","run_id":os.environ["GATE_RUN_ID"],"candidate_sha256":os.environ["GATE_CANDIDATE_SHA256"],"policy_sha256":os.environ["GATE_POLICY_SHA256"],"status":"completed","errors":0,"warnings":0}
if mode=="errors": r["errors"]=1
if mode=="warnings": r["warnings"]=21
if mode=="boundary": r["warnings"]=20
if mode=="skipped": r["status"]="skipped"
if mode=="producer_error": r["status"]="error"
if mode=="stale_run": r["run_id"]="previous-run"
if mode=="stale_candidate": r["candidate_sha256"]="old-candidate"
if mode=="stale_policy": r["policy_sha256"]="old-policy"
if mode=="wrong_check": r["check"]="different-check"
if mode=="bad_schema": r["schema"]=2
if mode=="schema_bool": r["schema"]=True
if mode=="negative_count": r["warnings"]=-1
if mode=="boolean_count": r["errors"]=False
if mode=="missing_field": del r["warnings"]
if mode=="incomplete": del r["status"]
p.write_text(json.dumps(r),encoding="utf-8")
sys.exit(7 if mode=="nonzero" else 0)
''',encoding='utf-8',newline='')
CANDIDATE=WORK/'candidate.json';CANDIDATE.write_text('{"schema":1,"source":"teaching-revision","target":"teaching-only"}',encoding='utf-8')
POLICY=WORK/'policy.json';POLICY.write_text('{"schema":1,"check":"compile","warning_budget":20}',encoding='utf-8')
CASES=[('pass',0),('boundary',0),('nonzero',2),('missing',2),('malformed',2),('errors',1),('warnings',1),('skipped',2),('producer_error',2),('stale_run',2),('stale_candidate',2),('stale_policy',2),('wrong_check',2),('bad_schema',2),('schema_bool',2),('negative_count',2),('boolean_count',2),('missing_field',2),('incomplete',2),('timeout',2)]
results=[]
class GateTests(unittest.TestCase):
    def test_protocol_cases(self):
        for mode,expected in CASES:
            with self.subTest(mode=mode):
                command=[sys.executable,str(ROOT/'compile_gate.py'),'--candidate',str(CANDIDATE),'--policy',str(POLICY),'--evidence-dir',str(WORK/'runs'),'--timeout','0.25' if mode=='timeout' else '5','--',sys.executable,str(FAKE),mode]
                r=subprocess.run(command,capture_output=True,text=True,timeout=15)
                results.append({'mode':mode,'expected':expected,'actual':r.returncode,'stdout':r.stdout,'stderr':r.stderr})
                self.assertEqual(r.returncode,expected,r.stderr)
                self.assertEqual('PASS:' in r.stdout,expected==0)
                folders=list((WORK/'runs').iterdir())
                self.assertTrue(folders)
    def test_missing_executable(self):
        command=[sys.executable,str(ROOT/'compile_gate.py'),'--candidate',str(CANDIDATE),'--policy',str(POLICY),'--evidence-dir',str(WORK/'runs'),'--timeout','5','--',str(WORK/'does-not-exist.exe')]
        r=subprocess.run(command,capture_output=True,text=True,timeout=15)
        results.append({'mode':'missing_executable','expected':2,'actual':r.returncode,'stdout':r.stdout,'stderr':r.stderr})
        self.assertEqual(r.returncode,2);self.assertNotIn('PASS:',r.stdout)
    def test_bad_policy(self):
        bad=WORK/'bad-policy.json';bad.write_text('{"schema":1,"check":"compile","warning_budget":true}')
        r=subprocess.run([sys.executable,str(ROOT/'compile_gate.py'),'--candidate',str(CANDIDATE),'--policy',str(bad),'--evidence-dir',str(WORK/'runs'),'--timeout','5','--',sys.executable,str(FAKE),'pass'],capture_output=True,text=True,timeout=15)
        results.append({'mode':'bad_policy','expected':2,'actual':r.returncode,'stdout':r.stdout,'stderr':r.stderr})
        self.assertEqual(r.returncode,2);self.assertNotIn('PASS:',r.stdout)

if __name__=='__main__':
    suite=unittest.defaultTestLoader.loadTestsFromTestCase(GateTests)
    result=unittest.TextTestRunner(verbosity=2).run(suite)
    (ROOT/'protocol-results.json').write_text(json.dumps({'python':sys.version,'cases':results,'passed':result.wasSuccessful(),'unity_executed':False},indent=2),encoding='utf-8')
    raise SystemExit(0 if result.wasSuccessful() else 1)
```
