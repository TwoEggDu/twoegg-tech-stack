---
title: "Unity DOTS M06｜构建、CI 与发布：Burst、AOT、Headless、Profiler 数据怎么进工程链"
slug: "dots-m06-build-ci-and-release"
date: "2026-03-28"
draft: true
description: "把 DOTS 的构建、CI、发布和回归验证串成一条工程链：Burst 编译、AOT 构建、Headless 批测、Profiler 证据和发布门禁如何收束成可执行规则。"
tags:
  - "Unity"
  - "DOTS"
  - "ECS"
  - "BuildPipeline"
  - "CI"
  - "Release"
  - "Engineering"
series: "Unity DOTS 项目落地与迁移"
primary_series: "unity-dots-project-migration"
series_role: "article"
series_order: 6
weight: 2206
---

如果这篇只记一句话，我建议记这个：**DOTS 真正进项目后，最先崩的通常不是仿真代码，而是构建链。**

Burst 编译、AOT 构建、Headless 批测、Profiler 证据、发布门禁，这几件事如果没有串成一条工程链，DOTS 就很容易停留在“本地能跑”的状态，真正到 CI 和发布阶段才暴露问题。本文不讲通用 CI 教程，只讲 DOTS 在工程链里的门禁该怎么排、该在哪一层拦、该产出什么证据。

---

## 为什么 DOTS 不能只当成普通 Unity 构建任务

普通 Unity 项目里，构建链主要验证的是“能不能打出包”。DOTS 项目里，构建链还要额外验证三件事：

- Burst 相关代码能不能稳定编译。
- AOT / IL2CPP 路径能不能覆盖到真实运行时。
- 试点系统在 Headless 或批处理环境里是否还能复现。

这意味着，DOTS 的构建链不是一个单点任务，而是一条带门禁的证据链。你不能只看最后的 Player 包是否生成成功，还要看中间每一层是否已经把最常见的工程风险拦住。

如果只做“构建成功”这一层，很多 DOTS 问题会被推迟到运行时才暴露，比如：

- Burst 在本地 Editor 可编，到了 CI 或不同平台却失败。
- 编辑器里看起来正确，IL2CPP / AOT 目标上行为变了。
- 试点系统本地跑通，但 Headless smoke 一跑就炸。
- 性能回归没有证据链，最后只能靠主观感觉判断。

---

## 工程链应该按什么顺序收束

DOTS 的工程链最好按“便宜的先拦、昂贵的后拦”来分层，而不是把所有检查都丢进一个统一流水线。

| 层级 | 主要验证什么 | 典型产物 | 失败后该怎么处理 |
|------|--------------|----------|------------------|
| 本地预检 | Burst 编译、asmdef 边界、基础静态错误 | 编译日志、Burst 报告 | 直接阻断合并 |
| PR 门禁 | Editor smoke、最小回归测试、关键系统能否进场景 | 测试结果、日志 | 阻断进入 nightly |
| Nightly | AOT / IL2CPP、Headless 批测、平台差异 | 构建包、测试报告 | 阻断发布候选 |
| Release candidate | Profiler 证据、性能阈值、包版本冻结 | profiler capture、基线对比 | 阻断打 tag |
| Release | 可回滚、可追溯、可复现 | release artifact bundle | 才能对外发版 |

这张表的核心不是“流程更长”，而是**每一层只拦自己该拦的问题**。Burst 问题不该等到发布候选才发现；性能回归也不该在本地编辑器里靠感觉判断。

---

## Burst 应该拦在哪一层

Burst 不是“跑得快的优化器”，而是构建链的一部分。它的价值不是帮你猜测性能，而是把一部分代码质量问题提前变成编译门禁。

最稳的做法是把 Burst 相关检查放进更早的层级：

- 本地预检阶段就跑 Burst 编译，尽早暴露不支持的写法。
- PR 门禁阶段把 Burst 报告当成硬门禁，不把警告当成“以后再说”。
- 如果某个系统依赖 Burst 才成立，就不要把它放到“可选优化”里对待。

这里最容易犯的错，是把 Burst 失败当成“构建细节”。实际上，Burst 失败往往意味着：

- 你用了不适合 Jobs / Burst 的写法。
- 你把 managed 依赖带进了热路径。
- 你的调度边界、容器选择或代码结构还没有冻结。

也就是说，Burst 编译不是最后一步的检查，而是第一批结构约束。

---

## AOT、Headless 和平台验证要放在哪

如果项目已经决定上线，AOT / IL2CPP 验证就不能只留在“偶尔手工点一次 Build”。

比较稳的分法是：

- Editor smoke 负责验证试点系统在编辑器里能否跑通。
- Headless / batchmode 负责验证无界面环境里的启动、加载和最小玩法链路。
- AOT / IL2CPP 构建负责验证真实发布目标的编译与运行差异。

这三层不是重复，而是覆盖不同风险：

- Editor smoke 主要拦“开发期已知问题”。
- Headless smoke 主要拦“无 UI、无交互环境下的启动与逻辑问题”。
- AOT 构建主要拦“目标平台与运行时编译差异”。

如果三层都没过，就不要讨论“性能发布”。先让目标链路能稳定起跑，再谈优化和上线。

---

## Profiler 证据不能只存在本地

DOTS 项目真正的回归判断，不能只靠“这次好像快了”。你需要的是可追溯的证据包。

最少应该把这些东西绑定到同一个 build id 上：

- 构建日志。
- Burst 编译结果。
- Headless smoke 结果。
- 关键场景的 profiler capture。
- 对照基线的差异摘要。

这里的关键点是，Profiler 数据不是“调试时顺手看一下”的临时产物，而是 release chain 的一部分。它要能回答三个问题：

- 这次构建对应哪一份代码。
- 这次性能结果对应哪一个场景。
- 这次回归是否能和上一版直接对照。

如果 profiler 证据没有进工程链，性能优化就会退化成经验判断，DOTS 的收益也很难在团队内部被长期证明。

---

## 最小可落地的工程链

下面这个伪流程不是某个工具的模板，而是 DOTS 项目更合理的门禁顺序：

```text
stage "pre-merge":
  run burst_compile_check
  run editor_smoke_test
  if burst_failed or smoke_failed:
    block_merge

stage "nightly":
  run il2cpp_aot_build
  run headless_batchmode_smoke
  archive build_logs and test_reports
  if aot_failed or smoke_failed:
    block_release_candidate

stage "release_candidate":
  run benchmark_scene
  capture profiler_artifacts
  compare with baseline
  if perf_regressed or artifact_missing:
    block_tag

stage "release":
  freeze package_versions
  publish only if all gates green
  store rollback_bundle
```

这条链路里，`block_merge`、`block_release_candidate`、`block_tag` 是三个不同层级的门。不要把它们混成一个“构建失败”。

---

## 代价、限制与边界

DOTS 工程链的代价，首先是反馈时间会变长。Burst、AOT、Headless、Profiler 这几层都不应该被每一次微小改动无差别地全量触发，否则 CI 很快就会变成噪音源。

其次，版本敏感性会提高。Entities、Burst、Collections、包依赖和构建参数一旦变化，验证链就必须跟着更新。你不能只在代码层补丁，却不更新门禁规则。

再次，不是每个项目都需要同样重的链路。小项目或试点期项目，可能只需要 Burst 编译 + 最小 smoke + 一份基线 profiler；但一旦进入正式发布，就必须把 AOT 和证据归档补上。

所以，工程链的原则不是“越重越好”，而是**门禁与风险层级对齐**。

---

## 什么时候该用，什么时候不该用

如果你的项目已经有明确的 DOTS 试点系统，而且这个系统会进入正式构建，那么这条链就应该尽早建立。它能帮你把“能跑”变成“可回归、可证明、可发布”。

如果你只是做概念验证，或者 DOTS 还停留在局部实验，先别把整套 release chain 一次性铺满。那样很容易把试点做成流程负担，最后团队只记得 CI 很慢，而不是 DOTS 解决了什么。

更具体地说：

- 只有 Editor 原型时，先做 Burst 与最小 smoke。
- 已经要进 nightly 时，补 Headless 和 AOT。
- 需要对外发布时，补 profiler 基线、artifact 归档和 rollback bundle。

---

## 常见踩坑 / 误用 / 排障入口

最常见的坑，是 Burst 只在本地手工跑，CI 里却没有同等检查。结果就是本地没问题，远端一合并就炸。

第二个坑，是把 headless smoke 当成“随便跑一下”。如果这个 smoke 没有覆盖启动、加载、试点系统和最小交互链路，它就拦不住真正的发布问题。

第三个坑，是 profiler 证据没有和 build id 绑定。这样一来，性能回归出现时，你根本不知道哪份数据对应哪次提交。

第四个坑，是把 release gate 写成“只要 build 成功就发版”。对 DOTS 来说，这通常太弱了。构建成功不等于 AOT 正确，不等于性能达标，也不等于能回滚。

---

## 小结

DOTS 进项目后，构建链就不再只是“打包流程”，而是 `Burst -> AOT -> Headless -> Profiler -> Release gate` 的证据链。

门禁要分层：便宜的检查前置，昂贵的验证放到 nightly 或 release candidate，性能证据必须随 build 归档，release 之前再冻结版本和回滚包。

下一步应读：`DOTS-M04｜版本升级与包依赖：Entities / Burst / Collections 升级最容易炸哪`

理由：构建和发布链一旦立住，接下来最容易把它打穿的就是包升级与依赖变化。

扩展阅读：`DOTS-M05｜测试与验证：怎样证明这次引入 DOTS 真的值`

理由：如果还没有稳定的 A/B 基线和阈值，构建链里的 profiler 证据也很难真正变成决策依据。
