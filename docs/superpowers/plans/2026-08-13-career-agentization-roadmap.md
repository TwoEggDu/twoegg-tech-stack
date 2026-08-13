# Career Agentization Roadmap Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a truthful four-stage Agentization Roadmap to the Career page between Career Snapshot and Evidence.

**Architecture:** Keep all Roadmap copy in `data/career.yaml`, render it from one isolated section in `layouts/_default/career.html`, and append Career-scoped responsive styles to `static/css/site.css`. Reuse the current Hugo site, Career visual variables, and evidence routes; do not add JavaScript or a new content system.

**Tech Stack:** Hugo templates, YAML data, semantic HTML, responsive CSS, PowerShell validation, local browser QA.

## Global Constraints

- Keep the current Hero positioning unchanged; Agentization remains a future direction.
- Distinguish exactly four statuses: `已具备`, `下一步`, `计划推进`, `长期方向`.
- Do not claim an Agent platform, automatic repair, autonomous publishing, or completed Agentization.
- Keep Roadmap copy in `data/career.yaml`; the template only renders data.
- Place the section after Career Snapshot and before Evidence.
- Link only to existing `/ai-empowerment/` and `/harness-engineering/` pages.
- Desktop uses a four-stage horizontal track; mobile uses a vertical track.
- Do not modify existing article content or the four pre-existing user drafts.

---

### Task 1: Add the Roadmap data contract and semantic section

**Files:**
- Modify: `data/career.yaml`
- Modify: `layouts/_default/career.html`

**Interfaces:**
- Consumes: `site.Data.career.roadmap`
- Produces: one `<section class="career-section career-roadmap">` with four `.career-roadmap-stage` articles and two `.career-roadmap-link` evidence links.

- [ ] **Step 1: Run the structural assertion before implementation**

```powershell
& $careerHugo --destination .tmp\career-roadmap-red --cleanDestinationDir
$html = Get-Content -Raw -Encoding UTF8 .tmp\career-roadmap-red\career\index.html
if ($html.Contains('career-roadmap')) { throw 'Expected Roadmap to be absent before implementation' }
```

Expected: PASS because the current Career page has no Roadmap section.

- [ ] **Step 2: Add the exact Roadmap data block**

Append a top-level `roadmap` block to `data/career.yaml` before `evidence`:

```yaml
roadmap:
  eyebrow: "Next Step / Agentization Roadmap"
  title: "把已经工程化的工作，继续变成可验证、可监督的 Agent 能力"
  summary: "目标不是给现有工具套一层对话界面，而是把多年积累的规则、文档、工具和流水线，逐步整理成 Agent 能理解上下文、调用工具、提供证据并接受验证的研发基础设施。"
  stages:
    - number: "01"
      status: "已具备"
      state: "current"
      title: "现有工程基础"
      description: "已经长期运行的规则、工具和流水线，是后续 Agent 化的真实起点。"
      items:
        - "导表与配置依赖校验"
        - "美术资源提交前检查"
        - "错误原因与修复文档绑定"
        - "Jenkins 构建与交付流水线"
        - "跨项目资产与多平台 SDK 工具链"
    - number: "02"
      status: "下一步"
      state: "next"
      title: "上下文与工具接口化"
      description: "先让既有系统变得可理解、可调用、可验证，再讨论模型怎样参与。"
      items:
        - "规则、依赖与修复知识可检索"
        - "检查和构建状态可只读查询"
        - "工具输出形成结构化证据"
        - "权限、范围与验证条件明确"
    - number: "03"
      status: "计划推进"
      state: "planned"
      title: "只读诊断 Agent"
      description: "从低风险、高复用的诊断场景开始，先建立可信的证据与判断链。"
      items:
        - "配置与依赖错误诊断"
        - "资源问题解释与修复指引"
        - "Unity / Jenkins 构建失败分析"
        - "跨项目接入规则与差异检查"
    - number: "04"
      status: "长期方向"
      state: "future"
      title: "受控执行与跨项目复用"
      description: "在只读诊断和回归验证稳定以后，再逐步开放人工审批下的工具调用。"
      items:
        - "白名单内执行可逆修改"
        - "触发构建并收集验证结果"
        - "保留审批、证据、停止与回滚"
        - "把成立的能力迁移到其他 Unity 项目"
  note: "当前只承诺逐步验证这条路径；自动发布、无审批修改和不可追踪执行不属于 Roadmap。"
  links:
    - label: "查看 AI 赋能"
      link: "ai-empowerment/"
    - label: "查看 Harness Engineering"
      link: "harness-engineering/"
```

- [ ] **Step 3: Render the Roadmap between Snapshot and Evidence**

Add this isolated section immediately before `<section id="evidence" ...>`:

```html
<section class="career-section career-roadmap" aria-labelledby="roadmap-title">
  <header class="career-section-head career-section-head-split">
    <div>
      <p class="eyebrow">{{ $career.roadmap.eyebrow }}</p>
      <h2 id="roadmap-title">{{ $career.roadmap.title }}</h2>
    </div>
    <p>{{ $career.roadmap.summary }}</p>
  </header>
  <div class="career-roadmap-track">
    {{ range $career.roadmap.stages }}
      <article class="career-roadmap-stage" data-roadmap-state="{{ .state }}">
        <div class="career-roadmap-stage-head">
          <span class="career-roadmap-number">{{ .number }}</span>
          <span class="career-roadmap-status">{{ .status }}</span>
        </div>
        <h3>{{ .title }}</h3>
        <p>{{ .description }}</p>
        <ul>{{ range .items }}<li>{{ . }}</li>{{ end }}</ul>
      </article>
    {{ end }}
  </div>
  <footer class="career-roadmap-footer">
    <p>{{ $career.roadmap.note }}</p>
    <div class="career-roadmap-links">
      {{ range $career.roadmap.links }}
        <a class="career-roadmap-link" href="{{ .link | relURL }}">{{ .label }} <span aria-hidden="true">→</span></a>
      {{ end }}
    </div>
  </footer>
</section>
```

- [ ] **Step 4: Build and verify the semantic contract**

```powershell
& $careerHugo --destination .tmp\career-roadmap-structure --cleanDestinationDir
$html = Get-Content -Raw -Encoding UTF8 .tmp\career-roadmap-structure\career\index.html
if (-not $html.Contains('class="career-section career-roadmap"')) { throw 'Roadmap section missing' }
if (([regex]::Matches($html, 'class="career-roadmap-stage"')).Count -ne 4) { throw 'Expected four Roadmap stages' }
foreach ($status in @('已具备','下一步','计划推进','长期方向')) { if (-not $html.Contains($status)) { throw "Missing status: $status" } }
```

Expected: Hugo exits `0`; all assertions pass.

### Task 2: Add the responsive maturity-track visual

**Files:**
- Modify: `static/css/site.css`

**Interfaces:**
- Consumes: `.career-roadmap`, `.career-roadmap-track`, `.career-roadmap-stage`, `data-roadmap-state`, `.career-roadmap-footer`.
- Produces: four equal desktop columns above `980px`, two columns at tablet width, one connected vertical track at `720px` and below.

- [ ] **Step 1: Verify the Roadmap CSS is absent**

```powershell
if (Select-String -Quiet -Path static\css\site.css -Pattern '^\.career-roadmap-track') { throw 'Expected Roadmap CSS to be absent' }
```

Expected: PASS before styling is added.

- [ ] **Step 2: Append the exact Career-scoped Roadmap styles**

```css
.career-roadmap-track {
  position: relative;
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 12px;
}

.career-roadmap-track::before {
  content: "";
  position: absolute;
  top: 34px;
  right: 6%;
  left: 6%;
  height: 1px;
  background: rgba(18, 24, 22, 0.14);
}

.career-roadmap-stage {
  --roadmap-color: var(--muted);
  position: relative;
  z-index: 1;
  min-width: 0;
  padding: 22px;
  border: 1px solid var(--border);
  border-top: 4px solid var(--roadmap-color);
  border-radius: 8px;
  background: var(--surface);
  box-shadow: 0 10px 24px rgba(18, 24, 22, 0.05);
  transition: transform 160ms ease, box-shadow 160ms ease;
}

.career-roadmap-stage[data-roadmap-state="current"] {
  --roadmap-color: var(--teal);
}

.career-roadmap-stage[data-roadmap-state="next"] {
  --roadmap-color: var(--accent);
}

.career-roadmap-stage[data-roadmap-state="planned"] {
  --roadmap-color: var(--gold);
}

.career-roadmap-stage[data-roadmap-state="future"] {
  --roadmap-color: #78817d;
}

.career-roadmap-stage-head {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: center;
  margin-bottom: 24px;
}

.career-roadmap-number {
  color: var(--roadmap-color);
  font-family: var(--mono);
  font-size: 0.75rem;
  font-weight: 700;
}

.career-roadmap-status {
  padding: 4px 8px;
  border: 1px solid color-mix(in srgb, var(--roadmap-color) 24%, transparent);
  border-radius: 999px;
  color: var(--roadmap-color);
  background: color-mix(in srgb, var(--roadmap-color) 7%, transparent);
  font-size: 0.7rem;
  font-weight: 700;
  white-space: nowrap;
}

.career-roadmap-stage h3 {
  margin: 0;
  font-family: var(--headline);
  font-size: 1.35rem;
  line-height: 1.16;
}

.career-roadmap-stage > p {
  margin: 12px 0 0;
  color: var(--muted);
  font-size: 0.9rem;
}

.career-roadmap-stage ul {
  display: grid;
  gap: 8px;
  margin: 20px 0 0;
  padding-left: 1.1rem;
  color: var(--ink);
  font-size: 0.84rem;
}

.career-roadmap-stage li::marker {
  color: var(--roadmap-color);
}

.career-roadmap-stage:hover {
  box-shadow: 0 15px 30px rgba(18, 24, 22, 0.08);
  transform: translateY(-2px);
}

.career-roadmap-footer {
  display: flex;
  justify-content: space-between;
  gap: 24px;
  align-items: flex-start;
  margin-top: 18px;
  padding: 18px 20px;
  border: 1px solid rgba(18, 24, 22, 0.09);
  border-radius: 8px;
  background: rgba(18, 24, 22, 0.035);
}

.career-roadmap-footer > p {
  max-width: 48rem;
  margin: 0;
  color: var(--muted);
  font-size: 0.88rem;
}

.career-roadmap-links {
  display: flex;
  flex: 0 0 auto;
  flex-wrap: wrap;
  gap: 8px 16px;
}

.career-roadmap-link {
  color: var(--accent-strong);
  font-size: 0.88rem;
  font-weight: 700;
}
```

- [ ] **Step 3: Add responsive breakpoints**

```css
@media (max-width: 980px) {
  .career-roadmap-track {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .career-roadmap-track::before {
    display: none;
  }

  .career-roadmap-footer {
    display: grid;
  }
}

@media (max-width: 720px) {
  .career-roadmap-track {
    grid-template-columns: 1fr;
    gap: 14px;
    padding-left: 18px;
  }

  .career-roadmap-track::before {
    display: block;
    top: 0;
    right: auto;
    bottom: 0;
    left: 5px;
    width: 1px;
    height: auto;
  }

  .career-roadmap-stage::before {
    content: "";
    position: absolute;
    top: 26px;
    left: -18px;
    width: 9px;
    height: 9px;
    border: 2px solid var(--surface);
    border-radius: 50%;
    background: var(--roadmap-color);
    box-shadow: 0 0 0 1px var(--roadmap-color);
  }

  .career-roadmap-footer {
    padding: 16px;
  }

  .career-roadmap-links {
    display: grid;
  }
}
```

- [ ] **Step 4: Run source-level CSS assertions**

```powershell
$css = Get-Content -Raw -Encoding UTF8 static\css\site.css
foreach ($needle in @('.career-roadmap-track','data-roadmap-state="current"','grid-template-columns: repeat(4','grid-template-columns: repeat(2','grid-template-columns: 1fr')) {
  if (-not $css.Contains($needle)) { throw "Missing CSS contract: $needle" }
}
```

Expected: all assertions pass.

### Task 3: Verify content truthfulness, links, responsive layout, and regression safety

**Files:**
- Test: `.tmp/career-roadmap-build/career/index.html`
- Review: `data/career.yaml`
- Review: `layouts/_default/career.html`
- Review: `static/css/site.css`

**Interfaces:**
- Consumes: completed Tasks 1 and 2.
- Produces: a verified Career V0.2 commit without unrelated user files.

- [ ] **Step 1: Run the complete Hugo build**

```powershell
& $careerHugo --destination .tmp\career-roadmap-build --cleanDestinationDir
```

Expected: exit code `0`, page count remains valid, no Hugo `ERROR`.

- [ ] **Step 2: Verify Roadmap ordering and both evidence targets**

```powershell
$html = Get-Content -Raw -Encoding UTF8 .tmp\career-roadmap-build\career\index.html
$snapshot = $html.IndexOf('career-snapshot')
$roadmap = $html.IndexOf('career-roadmap')
$evidence = $html.IndexOf('career-evidence')
if (-not ($snapshot -lt $roadmap -and $roadmap -lt $evidence)) { throw 'Roadmap section order is wrong' }
foreach ($target in @('ai-empowerment\index.html','harness-engineering\index.html')) {
  if (-not (Test-Path (Join-Path '.tmp\career-roadmap-build' $target))) { throw "Missing evidence target: $target" }
}
```

- [ ] **Step 3: Run the truthfulness and privacy scan**

```powershell
$blocked = @('17278814166','18300766991','已完成 Agent 化','已落地智能体平台','自动修复生产问题','全自动交付','自主发布')
foreach ($value in $blocked) { if ($html.Contains($value)) { throw "Blocked public claim: $value" } }
```

Expected: no blocked content found.

- [ ] **Step 4: Run desktop and mobile browser QA**

Start a hidden Hugo server, then inspect `/twoegg-tech-stack/career/` at `1440x900` and `390x844`.

Desktop assertions:

- four computed Roadmap columns
- section order is Snapshot -> Roadmap -> Evidence
- no horizontal overflow
- no console errors or warnings

Mobile assertions:

- one computed Roadmap column
- all four statuses remain visible
- footer links stack without clipping
- no horizontal overflow
- no console errors or warnings

- [ ] **Step 5: Run final diff and whitelist checks**

```powershell
git diff --check
git status --short
git diff -- data/career.yaml layouts/_default/career.html static/css/site.css docs/superpowers/plans/2026-08-13-career-agentization-roadmap.md
```

Expected: only the implementation plan and three Career files are new or modified for this task; the four user drafts remain unstaged.

- [ ] **Step 6: Commit the verified implementation**

```powershell
git add -- data/career.yaml layouts/_default/career.html static/css/site.css docs/superpowers/plans/2026-08-13-career-agentization-roadmap.md
git diff --cached --check
git commit -m "Add Career agentization roadmap"
```

Do not push unless the user explicitly asks after reviewing the result.
