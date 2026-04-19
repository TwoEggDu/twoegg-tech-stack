# TechStackShow 完成度与质量审计报告

- 审计日期：2026-04-17
- 审计范围：根目录 `doc-plan.md`、`CLAUDE.md`、`content/**/*.md`
- 质量扫描口径：排除 `_index.md`
- 构建验证：`hugo --minify --printPathWarnings`
- 说明：完成度采用保守的“严格匹配”口径，按 `slug` 优先、标题关键词为辅；由于 `doc-plan.md` 与当前仓库内容已明显漂移，真实完成率更可能落在 `31.8% ~ 42.0%` 区间。另发现额外目录 `content/ai-empowerment/`，不计入本次要求的 12 个 section。

## 一、完成度审计（Completion）

### 1.1 总体结论

| 指标 | 数值 |
|---|---:|
| `doc-plan.md` 计划条目数 | 672 |
| `doc-plan.md` 中 `✅` 标记数 | 514 |
| 严格匹配确认已写数 | 214 |
| 严格口径完成率 | 31.8% |
| 宽松匹配上界 | 282 |
| 宽松口径完成率上界 | 42.0% |
| 无法稳定映射到 12 个 section 的计划条目 | 77 |

> 结论：`doc-plan.md` 中的 `✅` 标记，明显高于仓库里当前可核实的已写文章数；计划状态与内容实态已经脱节。

### 1.2 Section 总览表

| Section | 计划篇数 | 已写篇数（严格） | 完成率 | 实际文章总数 | 未对账存量 |
|---|---:|---:|---:|---:|---:|
| code-quality | 23 | 17 | 73.9% | 41 | 24 |
| delivery-engineering | 0 | 0 | 0.0% | 130 | 130 |
| engine-toolchain | 151 | 77 | 51.0% | 221 | 144 |
| essays | 0 | 0 | 0.0% | 17 | 17 |
| et-framework | 0 | 0 | 0.0% | 4 | 4 |
| et-framework-prerequisites | 0 | 0 | 0.0% | 10 | 10 |
| live-ops-engineering | 0 | 0 | 0.0% | 7 | 7 |
| performance | 68 | 30 | 44.1% | 109 | 79 |
| problem-solving | 0 | 0 | 0.0% | 10 | 10 |
| projects | 0 | 0 | 0.0% | 15 | 15 |
| rendering | 177 | 29 | 16.4% | 229 | 200 |
| system-design | 176 | 61 | 34.7% | 256 | 195 |

### 1.3 系列明细表

> 说明：下表列出当前 root `doc-plan.md` 中能稳定识别并对账的主要 series。由于计划与内容命名分叉明显，`delivery-engineering / essays / projects / et-framework* / live-ops-engineering / problem-solving` 等目录下的多数文章，当前在 root `doc-plan.md` 中无法稳定找到对应计划条目，因此表现为“未对账存量”而非“已写计划项”。

| Section | Series | 计划篇数 | 已写篇数 | `✅` 标记数 | 骨架篇数 | TODO 篇数 | 孤儿文章 | 缺失索引 |
|---|---|---:|---:|---:|---:|---:|---:|---|
| code-quality | 游戏质量保障体系 | 15 | 9 | 15 | 0 | 0 | 6 | 否 |
| code-quality | CI/CD 与工程质量 | 8 | 8 | 8 | 0 | 0 | 0 | 否 |
| engine-toolchain | 数据导向运行时 | 59 | 7 | 52 | 0 | 0 | 52 | 否 |
| engine-toolchain | Unity 资产系统与序列化 | 27 | 26 | 27 | 0 | 0 | 13 | 是 |
| engine-toolchain | HybridCLR | 25 | 24 | 25 | 0 | 0 | 12 | 是 |
| engine-toolchain | Unity 裁剪 | 11 | 7 | 7 | 0 | 0 | 0 | 否 |
| engine-toolchain | 存储设备与 IO 基础 | 6 | 0 | 6 | 0 | 0 | 0 | 否 |
| performance | 移动端硬件与优化 | 62 | 30 | 61 | 0 | 0 | 5 | 是 |
| performance | 底层硬件 · CPU 与内存体系 | 6 | 0 | 6 | 0 | 0 | 0 | 否 |
| rendering | Shader 手写技法 | 73 | 17 | 69 | 0 | 0 | 59 | 否 |
| rendering | Unity 渲染系统 | 35 | 4 | 35 | 0 | 0 | 34 | 否 |
| rendering | URP 深度 | 20 | 0 | 20 | 1 | 0 | 33 | 否 |
| rendering | 游戏图形系统 | 13 | 3 | 13 | 0 | 0 | 5 | 否 |
| rendering | 图形 API 基础 | 7 | 0 | 7 | 0 | 0 | 0 | 否 |
| system-design | 游戏后端基础 | 72 | 48 | 72 | 0 | 0 | 0 | 否 |
| system-design | Unreal Engine 架构与系统 | 25 | 5 | 25 | 0 | 0 | 21 | 否 |
| system-design | 数据结构与算法 | 23 | 0 | 23 | 0 | 0 | 23 | 否 |
| system-design | 游戏引擎架构地图 | 8 | 8 | 8 | 0 | 0 | 1 | 否 |

### 1.4 孤儿文章最重的系列

| Section | Series | 实际篇数 | 未对账篇数 |
|---|---|---:|---:|
| rendering | Shader 手写技法 | 76 | 68 |
| engine-toolchain | dotnet-runtime-ecosystem | 59 | 58 |
| system-design | 技能系统深度 | 38 | 38 |
| rendering | URP 深度 | 33 | 32 |
| rendering | Unity 渲染系统 | 38 | 31 |
| performance | 游戏预算管理 | 22 | 22 |
| performance | 移动端硬件与优化 | 35 | 21 |
| system-design | Unreal Engine 架构与系统 | 26 | 20 |
| essays | 工程判断 | 17 | 17 |
| projects | 项目案例 | 15 | 15 |

### 1.5 缺口清单（Top 20）

按“所在系列完成率 > 50% 且该篇仍未写，并且被其他文章 `relref` 引用”的标准筛选后：

- 未发现满足条件的稳定候选项。
- `hugo --minify --printPathWarnings` 构建结果为 `0 ERROR`，因此当前没有 Hugo 级别的失效 `relref` 可以支撑这类缺口。

## 二、质量审计（Quality）

### 2.1 规范性（硬性问题）

| 问题类型 | 数量 | 备注 |
|---|---:|---|
| YAML 引号错误 | 18 | 违反 `CLAUDE.md` 的外层引号规则 |
| 缺少 frontmatter | 67 | 文件开头无标准 `---` frontmatter |
| 缺少 `slug` | 56 | 统计口径为缺少必填字段 |
| 缺少 `date` | 17 | 统计口径为缺少必填字段 |
| 缺少 `description` | 10 | 统计口径为缺少必填字段 |
| `weight` 冲突 | 85 | 命中文件数；对应 39 个冲突组 |
| shortcode 引号错误 | 0 | 未发现中文引号版 `relref` |
| 失效 `relref` | 0 | 以 Hugo 实构结果为准 |
| 日期异常 | 0 | 未发现晚于 2026-04-17 或早于 2025-01-01 |
| Hugo 构建告警 | 1 | `Duplicate target paths: \\rendering\\index.html (2)` |

#### 2.1.1 规范性问题清单（按严重度排序）

##### S1：缺少 frontmatter / 缺少必填字段

| 文件 | 行号 | 问题类型 |
|---|---:|---|
| `content/engine-toolchain/hybridclr-series-index.md` | 1 | 缺少 frontmatter |
| `content/rendering/DATA-TODO.md` | 1 | 缺少 frontmatter |
| `content/et-framework-prerequisites/et-pre-01-threads-tasks-coroutines-and-fibers.md` | 1 | 缺少 frontmatter |
| `content/code-quality/code-quality-is-delivery-capability.md` | 1 | 缺少必填字段：`slug`、`date` |
| `content/engine-toolchain/coreclr-series-index.md` | 1 | 缺少必填字段：`description` |
| `content/engine-toolchain/il2cpp-series-index.md` | 1 | 缺少必填字段：`description` |
| `content/engine-toolchain/runtime-cross-series-index.md` | 1 | 缺少必填字段：`description` |
| `content/rendering/urp-pipeline-asset-cheat-sheet.md` | 1 | 缺少必填字段：`slug` |

##### S1：`weight` 冲突

| Series | Weight | 冲突数 | 代表文件 |
|---|---:|---:|---|
| 构建与调试前置 | 59 | 3 | `build-debug-02*` 三篇共享同一 weight |
| Shader 手写技法 | 4110–4470 | 14 组 | `shader-basic-*` 与 `shader-lighting-*` 多处重号 |
| Unity Shader Variant 治理 | 20 / 30 / 140 | 3 组 | `unity-shader-*` |
| Unity 资产系统与序列化 | 40 / 51 / 61 / 63 | 4 组 | `unity-*asset*` |
| dotnet-runtime-ecosystem | 42 / 49 / 60 / 62 / 64 / 79 | 6 组 | `coreclr / mono / il2cpp / runtime-cross*` |
| 移动端硬件与优化 | 2020 / 2070 / 2130 / 2170 / 2180 / 2210 | 6 组 | `mobile-* / gpu-opt-* / cpu-opt-*` |

##### S2：YAML 引号错误

| 文件 | 行号 | 问题类型 |
|---|---:|---|
| `content/engine-toolchain/data-oriented-runtime-00-why-engines-build-data-oriented-islands.md` | 3 | YAML 引号错误：`title` 含引号却仍用双引号 |
| `content/engine-toolchain/unity-importer-what-does-it-do-and-why-source-file-is-not-just-a-file.md` | 3 | YAML 引号错误：`title` |
| `content/performance/game-budget-07-scene-budget.md` | 4 | YAML 引号错误：`description` |
| `content/rendering/cachedshadows-01-overview.md` | 5 | YAML 引号错误：`description` |
| `content/system-design/game-engine-architecture-07-dots-and-mass.md` | 3 | YAML 引号错误：`title` |
| `content/engine-toolchain/unity-assetbundle-performance-memory-lzma-lz4-first-load-io.md` | 4 | YAML 引号错误：`description` |
| `content/engine-toolchain/unity-assetbundle-runtime-loading-chain-download-cache-dependencies-unload.md` | 4 | YAML 引号错误：`description` |
| `content/engine-toolchain/unity-prefab-file-what-is-it-template-object-graph-nested-variant-override.md` | 4 | YAML 引号错误：`description` |
| `content/engine-toolchain/unity-scene-file-what-is-it-object-graph-not-a-big-asset.md` | 3 | YAML 引号错误：`title` |
| `content/engine-toolchain/unity-serialized-assets-how-they-restore-to-runtime-objects.md` | 4 | YAML 引号错误：`description` |
| `content/engine-toolchain/unity-why-needs-assetbundle-delivery-not-loading.md` | 3 | YAML 引号错误：`title` |
| `content/engine-toolchain/unity-why-resource-mounted-scripts-fail-monoscript-assembly-boundaries.md` | 4 | YAML 引号错误：`description` |
| `content/performance/game-budget-12-apple-memory-budgets-legacy-and-current.md` | 4 | YAML 引号错误：`description` |
| `content/performance/game-budget-15-miniapp-platform-budget-wechat-alipay-douyin.md` | 4 | YAML 引号错误：`description` |
| `content/performance/game-budget-18-scene-budget-template.md` | 4 | YAML 引号错误：`description` |
| `content/performance/game-budget-20-vfx-budget-template.md` | 4 | YAML 引号错误：`description` |
| `content/system-design/skill-system-05-validation-and-constraints.md` | 4 | YAML 引号错误：`description` |
| `content/system-design/skill-system-06-targeting-and-hit-resolution.md` | 4 | YAML 引号错误：`description` |

##### S3：构建告警

| 文件 / 目标 | 行号 | 问题类型 |
|---|---:|---|
| `rendering/index.html` | 1 | Hugo 警告：`Duplicate target paths: \\rendering\\index.html (2)` |

### 2.2 结构性（内容骨架健康度）

#### 2.2.1 各 Section 结构统计表

| Section | 平均字数 | 骨架占比 | 标准起手段覆盖率 | 代码块/篇 | 表格/篇 | 列表/篇 | TODO 标记数 | 字数分布 `<500 / 500–2000 / 2000–5000 / >5000` |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| code-quality | 2704.8 | 0.0% | 0.0% | 0.49 | 1.98 | 50.76 | 0 | 0 / 12 / 27 / 2 |
| delivery-engineering | 1455.8 | 13.8% | 77.7% | 1.80 | 12.81 | 16.45 | 0 | 18 / 90 / 22 / 0 |
| engine-toolchain | 3593.7 | 3.6% | 0.0% | 4.58 | 5.52 | 51.43 | 0 | 8 / 27 / 154 / 32 |
| essays | 794.6 | 64.7% | 0.0% | 0.35 | 1.29 | 13.41 | 48 | 11 / 5 / 0 / 1 |
| et-framework | 2018.8 | 0.0% | 0.0% | 0.00 | 0.00 | 25.50 | 0 | 0 / 2 / 2 / 0 |
| et-framework-prerequisites | 1378.1 | 0.0% | 0.0% | 0.00 | 0.00 | 5.00 | 0 | 0 / 10 / 0 / 0 |
| live-ops-engineering | 2946.3 | 0.0% | 0.0% | 2.00 | 11.14 | 34.57 | 14 | 0 / 1 / 6 / 0 |
| performance | 2261.0 | 5.5% | 0.0% | 4.79 | 4.59 | 45.92 | 0 | 6 / 42 / 58 / 3 |
| problem-solving | 4313.8 | 0.0% | 0.0% | 8.20 | 7.70 | 55.10 | 0 | 0 / 1 / 6 / 3 |
| projects | 2511.3 | 40.0% | 0.0% | 0.80 | 3.47 | 16.93 | 34 | 6 / 3 / 2 / 4 |
| rendering | 1931.7 | 3.1% | 12.2% | 4.98 | 4.03 | 26.69 | 16 | 7 / 121 / 99 / 2 |
| system-design | 2340.4 | 3.5% | 0.0% | 6.03 | 2.91 | 25.38 | 0 | 9 / 98 / 140 / 9 |

#### 2.2.2 结构性判断

| 风险点 | 发现 |
|---|---|
| 骨架文占比偏高 | `essays` 64.7%、`projects` 40.0%、`delivery-engineering` 13.8% |
| 标准起手段缺失 | 除 `delivery-engineering` 与少量 `rendering` 外，绝大多数 section 未形成统一起手模板 |
| 结构元素不足 | `et-framework`、`et-framework-prerequisites` 代码块和表格密度接近 0 |
| TODO 聚集 | `essays` 48 处、`projects` 34 处、`rendering` 16 处、`live-ops-engineering` 14 处 |

### 2.3 连贯性（系列健康度）

| 问题类型 | 数量 |
|---|---:|
| `series_order` 跳号 | 27 |
| `series_order` 重号 | 2 |
| 系列缺少 `series_role: "index"` | 11 |
| 系列索引页未覆盖全部文章 | 24 |
| 同一 `series` 跨 section 分布 | 12 |

#### 2.3.1 连贯性问题清单

| 类型 | Series | 问题说明 |
|---|---|---|
| 缺失索引 | dotnet-runtime-ecosystem | 59 篇文章，但没有 `series_role: "index"` |
| 缺失索引 | HybridCLR | 36 篇文章，但没有 `series_role: "index"` |
| 缺失索引 | Unity 资产系统与序列化 | 39 篇文章，但没有 `series_role: "index"` |
| 缺失索引 | Addressables 与 YooAsset 源码解读 | 18 篇文章，但没有 `series_role: "index"` |
| 缺失索引 | CachedShadows 阴影缓存 | 8 篇文章，但没有 `series_role: "index"` |
| 缺失索引 | 移动端硬件与优化 | 35 篇文章，但没有 `series_role: "index"` |
| 索引漏链 | Shader 手写技法 | 索引页漏链 74 篇 |
| 索引漏链 | Unity 渲染系统 | 索引页漏链 36 篇 |
| 索引漏链 | Unity Shader Variant 治理 | 索引页漏链 27 篇 |
| 索引漏链 | Unreal Engine 架构与系统 | 索引页漏链 25 篇 |
| 索引漏链 | 数据结构与算法 | 索引页漏链 23 篇 |
| 跨 section | Addressables 与 YooAsset 源码解读 | 主体在 `engine-toolchain`，但有文章落在 `problem-solving` |
| 跨 section | 工程判断 | 横跨 `essays / rendering / system-design` |
| 跨 section | 移动端硬件与优化 | 主体在 `performance`，但有文章落在 `rendering` |
| 跨 section | 游戏引擎架构地图 | 横跨 `system-design / rendering` |
| 重号 | 游戏性能判断 | `series_order = 4` 重号 |
| 重号 | 技能系统深度 | `series_order = 0` 重号 |

### 2.4 Top 10 最需要修订的文章

> 综合打分依据：规范错误 + 骨架程度 + TODO 密度 + 结构元素缺失。

| 文件 | 综合分 | 建议动作 |
|---|---:|---|
| `content/rendering/urp-pipeline-asset-cheat-sheet.md` | 16 | 先补齐 frontmatter 与 slug，再把速查页补成可发布正文，并加入标准起手段 |
| `content/engine-toolchain/il2cpp-series-index.md` | 11 | 修 frontmatter，补系列导读、目录、阅读建议，避免“索引页像占位符” |
| `content/engine-toolchain/runtime-cross-series-index.md` | 11 | 同上，补成真正的系列入口页 |
| `content/essays/cross-discipline-boundaries.md` | 10 | 从骨架扩成正文，清掉 TODO，并加入最少 1 个案例表 |
| `content/essays/cross-project-tech-standardization.md` | 10 | 同上，补读者问题、决策框架、案例收束 |
| `content/essays/engineering-health-metrics.md` | 10 | 同上，补指标定义与误用边界 |
| `content/essays/ic-to-tech-lead.md` | 10 | 同上，补角色转换判断与反例 |
| `content/essays/multiplatform-publishing-pitfalls.md` | 10 | 同上，补踩坑模式与平台差异表 |
| `content/essays/onboarding-engineering.md` | 10 | 同上，补 onboarding 失败模式与检查清单 |
| `content/essays/performance-optimization-roi.md` | 10 | 同上，补 ROI 判断框架与案例拆解 |

## 三、构建结果

| 命令 | 结果 |
|---|---|
| `hugo --minify --printPathWarnings` | 构建成功 |
| `ERROR` | 0 |
| `WARN` | 1 |
| 关键告警 | `Duplicate target paths: \\rendering\\index.html (2)` |

## 四、结论与建议

### 4.1 当前最主要的完成度缺口

| 排名 | 缺口 | 判断 |
|---|---:|---|
| 1 | 渲染（`rendering`） | 计划 177，严格已写仅 29；且大量现有文章未能对齐 root `doc-plan` |
| 2 | 系统设计（`system-design`） | 计划 176，严格已写 61；系列规模大，但索引与连贯性问题密集 |
| 3 | 工具链（`engine-toolchain`） | 计划 151，严格已写 77；表面完成率过半，但 `✅` 与实文差距仍然很大 |

### 4.2 当前最紧急的质量问题

| 排名 | 问题 | 判断 |
|---|---:|---|
| 1 | Frontmatter 缺失 / 必填字段缺失 | 直接影响发布稳定性与后续治理自动化 |
| 2 | `weight` 冲突 | 已经影响系列排序一致性，且会放大索引页维护成本 |
| 3 | YAML 引号违规 | 与 `CLAUDE.md` 的“最重要规则”直接冲突，属于必须清零项 |

## 执行摘要

严格对账口径下，672 个计划条目仅能确认 214 篇已写，整体完成率 31.8%；最大缺口集中在 `rendering`、`system-design`、`engine-toolchain`。最紧急的质量问题是 67 篇无 frontmatter、85 条 `weight` 冲突、18 处 YAML 引号违规，另有 1 条 Hugo 重复目标路径告警。
