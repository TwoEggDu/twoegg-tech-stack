---
title: "Agent Engineering｜系统课程"
slug: "agent-engineering"
url: "/ai-empowerment/agent-engineering/"
aliases:
  - "/ai-empowerment/agent-engineering-series-index/"
date: "2026-08-20"
description: "从 Model、Prompt、Tool 与 Agent Runtime 出发，逐层进入 Context、Reliable Agent、Harness、DeepSeek Harness 与 BuildPilot 的系统课程。"
draft: false
layout: "agent-course"
weight: 3000
featured: false
tags:
  - "Agent Engineering"
  - "AI Engineering"
  - "Harness Engineering"
  - "Course Index"
series: "Agent Engineering"
series_id: "agent-engineering"
series_role: "index"
series_order: 0
series_nav_order: 55
series_title: "Agent Engineering"
series_entry: true
series_audience:
  - "C# / Unity 工程师"
  - "使用过 Coding Agent 的开发者"
  - "希望设计专用 Agent 的工程师"
series_level: "入门到进阶"
series_best_for: "当你已经会使用 AI 工具，但想系统理解 Agent 怎样运行、为什么需要 Harness，以及怎样设计专用 Agent"
series_summary: "从 Model API 逐层建立 Agent Runtime、Context、Memory、Evidence、Permission、Budget、Trace、Eval 与 Harness，再用 DeepSeek Harness 验证抽象，最终完成 BuildPilot Design v1。"
---

<section class="course-section course-section-intro" aria-labelledby="learning-path-title">
  <div class="course-section-heading">
    <p class="eyebrow">Learning Path</p>
    <h2 id="learning-path-title">一条从模型调用走到可靠 Agent 工程的主线</h2>
    <p>课程先建立可编程合同与行动边界，再进入循环、状态和知识，最后把可靠性能力收束进 Harness，并用真实源码阅读与 BuildPilot 设计回收整条路径。</p>
  </div>
  <ol class="course-path" aria-label="Agent Engineering 知识主线">
    <li><strong>Model</strong><span>模型、API、Messages、Token</span></li>
    <li><strong>Prompt</strong><span>任务合同、约束、示例、失败语义</span></li>
    <li><strong>Structured Output</strong><span>机器可消费的输出合同</span></li>
    <li><strong>Function Calling</strong><span>模型表达行动意图</span></li>
    <li><strong>Tool Runtime</strong><span>Validate、Policy、Execute、Result</span></li>
    <li><strong>Agent Loop</strong><span>Decide、Act、Observe、Stop</span></li>
    <li><strong>Planning / Workflow</strong><span>计划、状态机、Checkpoint</span></li>
    <li><strong>Context</strong><span>装配、Snapshot、Receipt；Article 13 再讲 Debugging 与可重建性</span></li>
    <li><strong>Memory / RAG</strong><span>Working、Session、Long-term、Knowledge</span></li>
    <li><strong>Skill</strong><span>按需加载领域方法</span></li>
    <li><strong>Reliable Agent</strong><span>Evidence、Permission、Budget、Trace、Eval</span></li>
    <li><strong>Harness</strong><span>执行内核之外的工程控制面</span></li>
    <li><strong>DeepSeek Harness</strong><span>Evidence-first 源码阅读</span></li>
    <li><strong>BuildPilot</strong><span>把抽象回收到专用 Agent 设计</span></li>
  </ol>
</section>

<section id="course-map" class="course-section" aria-labelledby="course-map-title">
  <div class="course-section-heading">
    <p class="eyebrow">Course Map</p>
    <h2 id="course-map-title">Part I—VII 课程地图</h2>
    <p>已发布课程可直接进入；其余条目只展示 canonical 规划与真实状态，不生成链接。Article 23 是 Advanced / Optional，主线可从 22 直接进入 24。</p>
  </div>

  <article class="course-start-card">
    <div>
      <p class="course-part-label">Course Introduction</p>
      <h3>00｜Agent Engineering 世界地图</h3>
      <p>先区分 Model、Agent、Runtime、Harness 与 Host，建立后续课程共用的知识地图。</p>
    </div>
    <a class="card-link" href="{{< relref "ai-empowerment/agent-engineering-00-agent-engineering-world-map.md" >}}">开始 Article 00</a>
  </article>

  <div class="course-parts">
    <section class="course-part">
      <header><p class="course-part-label">Part I · 01—04</p><h3>从 LLM 到可编程模型</h3></header>
      <ol class="course-lessons">
        <li class="is-published"><span class="lesson-id">01</span><a href="{{< relref "ai-empowerment/agent-engineering-01-model-api-messages-token.md" >}}">模型调用到底发生了什么：LLM、Model API、Messages 与 Token</a><span class="lesson-status">已发布</span></li>
        <li class="is-published"><span class="lesson-id">02</span><a href="{{< relref "ai-empowerment/agent-engineering-02-prompt-engineering-contract-boundaries.md" >}}">Prompt Engineering：任务合同、角色、示例与边界</a><span class="lesson-status">已发布</span></li>
        <li class="is-published"><span class="lesson-id">03</span><a href="{{< relref "ai-empowerment/agent-engineering-03-structured-output-machine-contract.md" >}}">Structured Output：让模型输出成为机器可消费的合同</a><span class="lesson-status">已发布</span></li>
        <li class="is-published"><span class="lesson-id">04</span><a href="{{< relref "ai-empowerment/agent-engineering-04-model-adapter-llm-gateway.md" >}}">Model Adapter 与 LLM Gateway：Streaming、Error、Retry 和 Provider 差异</a><span class="lesson-status">已发布</span></li>
      </ol>
    </section>
    <section class="course-part">
      <header><p class="course-part-label">Part II · 05—11</p><h3>从模型到 Agent</h3></header>
      <ol class="course-lessons">
        <li class="is-published"><span class="lesson-id">05</span><a href="{{< relref "ai-empowerment/agent-engineering-05-function-calling-tool-use.md" >}}">Function Calling 与 Tool Use：模型如何表达行动意图</a><span class="lesson-status">已发布</span></li>
        <li class="is-published"><span class="lesson-id">06</span><a href="{{< relref "ai-empowerment/agent-engineering-06-tool-runtime.md" >}}">Tool Runtime：Validate、Policy、Execute、Result 与 Trace</a><span class="lesson-status">已发布</span></li>
        <li class="is-published"><span class="lesson-id">07</span><a href="{{< relref "ai-empowerment/agent-engineering-07-mcp-external-capability-boundary.md" >}}">MCP 与外部能力边界：协议解决什么，宿主仍需解决什么</a><span class="lesson-status">已发布</span></li>
        <li class="is-published"><span class="lesson-id">08</span><a href="{{< relref "ai-empowerment/agent-engineering-08-agent-loop.md" >}}">Agent Loop：Turn、Step、Decide、Act、Observe 与 Stop</a><span class="lesson-status">已发布</span></li>
        <li class="is-published"><span class="lesson-id">09</span><a href="{{< relref "ai-empowerment/agent-engineering-09-planning.md" >}}">Planning：Agent 为什么需要计划，又为什么不能迷信计划</a><span class="lesson-status">已发布</span></li>
        <li class="is-published"><span class="lesson-id">10</span><a href="{{< relref "ai-empowerment/agent-engineering-10-state-machine-workflow.md" >}}">State Machine 与 Workflow：确定性骨架和 Agent Decision Point</a><span class="lesson-status">已发布</span></li>
        <li class="is-published"><span class="lesson-id">11</span><a href="{{< relref "ai-empowerment/agent-engineering-11-long-running-agent.md" >}}">Long-running Agent：Checkpoint、Retry、Cancellation 与 Recovery</a><span class="lesson-status">已发布</span></li>
      </ol>
    </section>
    <section class="course-part">
      <header><p class="course-part-label">Part III · 12—17</p><h3>Agent 的信息、状态与知识</h3></header>
      <ol class="course-lessons">
        <li class="is-published"><span class="lesson-id">12</span><a href="{{< relref "ai-empowerment/agent-engineering-12-context-engineering.md" >}}">Context Engineering：每一个 Step 到底应该看到什么</a><span class="lesson-status">已发布</span></li>
        <li class="is-published"><span class="lesson-id">13</span><a href="{{< relref "ai-empowerment/agent-engineering-13-context-debugging.md" >}}">Context Debugging：Packing、Compression、Pollution 与可重建性</a><span class="lesson-status">已发布</span></li>
        <li class="is-published"><span class="lesson-id">14</span><a href="{{< relref "ai-empowerment/agent-engineering-14-working-memory-investigation-state.md" >}}">Working Memory 与 Investigation State：当前任务正在想什么</a><span class="lesson-status">已发布</span></li>
        <li class="is-published"><span class="lesson-id">15</span><a href="{{< relref "ai-empowerment/agent-engineering-15-session-long-term-project-memory.md" >}}">Session、Long-term Memory 与 Project Memory：事实、经验和作用域</a><span class="lesson-status">已发布</span></li>
        <li class="is-published"><span class="lesson-id">16</span><a href="{{< relref "ai-empowerment/agent-engineering-16-knowledge-base-rag.md" >}}">Knowledge Base 与 RAG：Retrieve、Filter、Rerank、Inject、Cite</a><span class="lesson-status">已发布</span></li>
        <li class="is-published"><span class="lesson-id">17</span><a href="{{< relref "ai-empowerment/agent-engineering-17-skill-engineering.md" >}}">Skill Engineering：按需加载领域方法，而不是再堆一层 Prompt</a><span class="lesson-status">已发布</span></li>
      </ol>
    </section>
    <section class="course-part">
      <header><p class="course-part-label">Part IV · 18—23</p><h3>Reliable Agent Engineering</h3></header>
      <ol class="course-lessons">
        <li class="is-published"><span class="lesson-id">18</span><a href="{{< relref "ai-empowerment/agent-engineering-18-evidence-contract.md" >}}">Evidence Contract：把自然语言推断变成可审计工程数据</a><span class="lesson-status">已发布</span></li>
        <li class="is-published"><span class="lesson-id">19</span><a href="{{< relref "ai-empowerment/agent-engineering-19-permission-approval-hitl-sandbox.md" >}}">Permission、Approval、Human-in-the-loop 与 Sandbox</a><span class="lesson-status">已发布</span></li>
        <li class="is-published"><span class="lesson-id">20</span><a href="{{< relref "ai-empowerment/agent-engineering-20-budget-engineering-token-step-cost-latency.md" >}}">Budget Engineering：Token、Step、Cost 与 Latency</a><span class="lesson-status">已发布</span></li>
        <li class="is-published"><span class="lesson-id">21</span><a href="{{< relref "ai-empowerment/agent-engineering-21-trace-replay-failure-taxonomy.md" >}}">Trace、Replay 与 Failure Taxonomy：错误究竟发生在哪一层</a><span class="lesson-status">已发布</span></li>
        <li class="is-published"><span class="lesson-id">22</span><a href="{{< relref "ai-empowerment/agent-engineering-22-eval-golden-dataset-regression.md" >}}">Eval、Golden Dataset 与 Regression：修复以后还会不会再坏</a><span class="lesson-status">已发布</span></li>
        <li class="is-optional"><span class="lesson-id">23</span><span>Single Agent、Subagent、Agent as Tool、Handoff 与 Multi-Agent</span><span class="lesson-status">高级 · 可选</span></li>
      </ol>
    </section>
    <section class="course-part">
      <header><p class="course-part-label">Part V · 24—27</p><h3>Harness Engineering</h3></header>
      <ol class="course-lessons">
        <li class="is-published"><span class="lesson-id">24</span><a href="{{< relref "ai-empowerment/agent-engineering-24-why-harness-cross-cutting-capabilities.md" >}}">为什么最终需要 Harness：横切能力由谁承载</a><span class="lesson-status">已发布</span></li>
        <li class="is-published"><span class="lesson-id">25</span><a href="{{< relref "ai-empowerment/agent-engineering-25-agent-runtime-vs-harness.md" >}}">Agent Runtime vs Harness：执行内核与工程控制面</a><span class="lesson-status">已发布</span></li>
        <li class="is-published"><span class="lesson-id">26</span><a href="{{< relref "ai-empowerment/agent-engineering-26-harness-minimum-capability-model.md" >}}">Harness 的最小能力模型：Capability、Policy、Session、Trace 与 Recovery</a><span class="lesson-status">已发布</span></li>
        <li class="is-published"><span class="lesson-id">27</span><a href="{{< relref "ai-empowerment/agent-engineering-27-harness-design-tradeoffs.md" >}}">Harness 的设计取舍：可替换性、复杂度、Bloat 与演化</a><span class="lesson-status">已发布</span></li>
      </ol>
    </section>
    <section class="course-part">
      <header><p class="course-part-label">Part VI · 28—37</p><h3>DeepSeek Harness</h3></header>
      <ol class="course-lessons">
        <li class="is-published"><span class="lesson-id">28</span><a href="{{< relref "ai-empowerment/agent-engineering-28-dsh-evidence-first-source-method.md" >}}">怎样把 DeepSeek Harness 当作 Evidence-first 源码教材</a><span class="lesson-status">已发布</span></li>
        <li class="is-published"><span class="lesson-id">29</span><a href="{{< relref "ai-empowerment/agent-engineering-29-dsh-host-to-agent-run.md" >}}">DeepSeek Harness 总图：从 Host 启动到一次 Agent Run</a><span class="lesson-status">已发布</span></li>
        <li class="is-published"><span class="lesson-id">30</span><a href="{{< relref "ai-empowerment/agent-engineering-30-dsh-plugin-core.md" >}}">Everything is a Plugin：插件内核如何承载 Capability 与生命周期</a><span class="lesson-status">已发布</span></li>
        <li class="is-published"><span class="lesson-id">31</span><a href="{{< relref "ai-empowerment/agent-engineering-31-dsh-profile-bundle-capability-seam.md" >}}">Profile、Bundle、Provider 与 Capability Seam</a><span class="lesson-status">已发布</span></li>
        <li class="is-published"><span class="lesson-id">32</span><a href="{{< relref "ai-empowerment/agent-engineering-32-dsh-system-prompt-assembly-prompt-context.md" >}}">System Prompt Assembly 与 PromptContext：多来源 Context 怎样组成</a><span class="lesson-status">已发布</span></li>
        <li class="is-planned"><span class="lesson-id">33</span><span>Inbox、Turn、Step 与 Agent Loop</span><span class="lesson-status">计划中</span></li>
        <li class="is-planned"><span class="lesson-id">34</span><span>Append-only Session Event：Replay、Resume、Fork 与 Projection</span><span class="lesson-status">计划中</span></li>
        <li class="is-planned"><span class="lesson-id">35</span><span>Tool Registry 与 Tool Execution Pipeline</span><span class="lesson-status">计划中</span></li>
        <li class="is-planned"><span class="lesson-id">36</span><span>Cost、Compaction、Trace、Cancellation 与 Recovery</span><span class="lesson-status">计划中</span></li>
        <li class="is-planned"><span class="lesson-id">37</span><span>RAG、Skill、Workflow、Subagent 与 Web / Headless：核心事实和扩展映射</span><span class="lesson-status">计划中</span></li>
      </ol>
    </section>
    <section class="course-part">
      <header><p class="course-part-label">Part VII · 38—44</p><h3>BuildPilot Design</h3></header>
      <ol class="course-lessons">
        <li class="is-proposal"><span class="lesson-id">38</span><span>游戏生产问题空间：什么时候该写 Script、Rule、Workflow，什么时候才需要 Agent</span><span class="lesson-status">设计规划</span></li>
        <li class="is-proposal"><span class="lesson-id">39</span><span>案例 A：Unity Compile Golden Fixture——设计一个可判定的诊断 Agent</span><span class="lesson-status">设计规划</span></li>
        <li class="is-proposal"><span class="lesson-id">40</span><span>案例 B：启动性能调查——设计一个长链路、多假设 Agent</span><span class="lesson-status">设计规划</span></li>
        <li class="is-proposal"><span class="lesson-id">41</span><span>从两个案例反推 BuildPilot Architecture：先找变化轴，再定模块</span><span class="lesson-status">设计规划</span></li>
        <li class="is-proposal"><span class="lesson-id">42</span><span>BuildPilot 的 Context 与 Capability 设计：让知识、技能和工具各就各位</span><span class="lesson-status">设计规划</span></li>
        <li class="is-proposal"><span class="lesson-id">43</span><span>BuildPilot 的治理闭环：Evidence、Policy、Session、Trace、Budget、Recovery 与 Eval</span><span class="lesson-status">设计规划</span></li>
        <li class="is-proposal"><span class="lesson-id">44</span><span>BuildPilot Design v1：设计评审、里程碑与退出条件</span><span class="lesson-status">设计规划</span></li>
      </ol>
    </section>
  </div>
</section>

<section class="course-section" aria-labelledby="learning-routes-title">
  <div class="course-section-heading">
    <p class="eyebrow">How to Learn</p>
    <h2 id="learning-routes-title">按目标选择学习路线</h2>
    <p>三条路线共享同一条知识依赖，不会把 Runtime、Context 与可靠性工程拆成彼此矛盾的捷径。</p>
  </div>
  <div class="course-route-grid">
    <article>
      <p class="course-route-label">路线 A · 完整系统学习</p>
      <h3>从 00 顺序走到 44</h3>
      <p><strong>00 → 01 → 02 → … → 44</strong></p>
      <p>适合第一次系统学习 Agent Engineering 的开发者。Article 23 是高级可选内容；如果暂时不做多 Agent，可在 22 后先进入 24。</p>
    </article>
    <article>
      <p class="course-route-label">路线 B · Agent Application / Runtime</p>
      <h3>先把可运行、可治理的 Agent 做对</h3>
      <p><strong>00—04 → 05—11 → 12—17 → 18—22 → 24—27</strong></p>
      <p>覆盖 Model、Prompt、Tool、Agent Loop、Workflow、Context、Memory、Skill、Evidence、Trace、Eval 与 Harness；先停在通用工程模型，不急着进入具体 Harness 源码。</p>
    </article>
    <article>
      <p class="course-route-label">路线 C · Coding Agent / Harness Engineering</p>
      <h3>从执行循环一路进入 Harness 与 BuildPilot</h3>
      <p><strong>08—22 → 24—27 → 28—37 → 38—44</strong></p>
      <p>适合已经能解释 00—07 基础合同的读者；若基础概念仍有空缺，应先回补 00—07，再进入 Agent Loop、Context、Skill、Trace、Eval、Harness 与专用 Agent 设计。</p>
    </article>
  </div>
</section>

<section class="course-section" aria-labelledby="labs-title">
  <div class="course-section-heading">
    <p class="eyebrow">Engineering Labs</p>
    <h2 id="labs-title">6 个实验，把“理解了”推进到“观察过”</h2>
    <p>Lab 是课程证据的一部分，不是独立公开文章。已完成实验链接到吸收其证据的课程正文；尚未开始的实验只展示插入位置和计划状态。</p>
  </div>
  <div class="course-lab-grid">
    <article class="is-verified"><span>Lab 01 · Article 03 后</span><h3>Structured Output</h3><p>Parse、Schema、DTO 与 Domain Validation。</p><a href="{{< relref "ai-empowerment/agent-engineering-03-structured-output-machine-contract.md" >}}">查看对应课程 · 已验证</a></article>
    <article class="is-verified"><span>Lab 02 · Article 06 后</span><h3>Tool Runtime</h3><p>Validate、Policy、Timeout、Result 与 Trace。</p><a href="{{< relref "ai-empowerment/agent-engineering-06-tool-runtime.md" >}}">查看对应课程 · 已验证</a></article>
    <article class="is-verified"><span>Lab 03 · Article 08 后</span><h3>Minimal Agent Loop</h3><p>Turn、Step、Observation、Stop 与预算终止。</p><a href="{{< relref "ai-empowerment/agent-engineering-08-agent-loop.md" >}}">查看对应课程 · 已验证</a></article>
    <article class="is-verified"><span>Lab 04 · Article 11 后</span><h3>State Machine + Checkpoint</h3><p>State、Checkpoint、Resume 与 Cancellation。</p><a href="{{< relref "ai-empowerment/agent-engineering-11-long-running-agent.md" >}}">查看对应课程 · 已验证</a></article>
    <article class="is-verified"><span>Lab 05 · Article 13 后</span><h3>Context Debugging</h3><p>Context Snapshot、Pollution、Truncation 与重建。</p><a href="{{< relref "ai-empowerment/agent-engineering-13-context-debugging.md" >}}">查看对应课程 · 已验证</a></article>
    <article class="is-verified"><span>Lab 06 · Article 22 后</span><h3>Trace + Eval</h3><p>Trace、Failure Layer、Golden Dataset 与 Regression。</p><a href="{{< relref "ai-empowerment/agent-engineering-22-eval-golden-dataset-regression.md" >}}">查看对应课程 · 已验证</a></article>
  </div>
</section>

<section class="course-boundary" aria-labelledby="course-boundary-title">
  <div>
    <p class="eyebrow">Course Navigation Layer</p>
    <h2 id="course-boundary-title">Index 负责导航，Article 00 负责建立知识地图</h2>
    <p>这页回答课程是什么、适合谁、有哪些部分、如何学习以及当前发布到哪里；Article 00 才开始解释 Model、Agent、Runtime、Harness 与 Host 之间的概念关系。</p>
  </div>
  <a class="button" href="{{< relref "ai-empowerment/agent-engineering-00-agent-engineering-world-map.md" >}}">开始学习 Article 00</a>
</section>
