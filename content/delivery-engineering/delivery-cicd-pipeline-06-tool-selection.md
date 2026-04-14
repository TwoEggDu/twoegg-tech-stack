---
title: "CI/CD 管线 06｜CI 工具选型——Jenkins、GitHub Actions、GitLab CI 对比"
slug: "delivery-cicd-pipeline-06-tool-selection"
date: "2026-04-14"
description: "三大 CI 工具各有适用场景：Jenkins 最灵活但需要维护，GitHub Actions 云原生但 macOS Runner 贵，GitLab CI 自托管体验好。游戏项目还要额外考虑 Unity 许可证和大型仓库支持。"
tags:
  - "Delivery Engineering"
  - "CI/CD"
  - "Jenkins"
  - "GitHub Actions"
  - "GitLab CI"
series: "CI/CD 管线"
primary_series: "delivery-cicd-pipeline"
series_role: "article"
series_order: 60
weight: 1560
delivery_layer: "practice"
delivery_volume: "V16"
delivery_reading_lines:
  - "L1"
  - "L2"
---

## 这篇解决什么问题

选 CI 工具是很多团队在搭建管线时的第一个决策点。选错了不至于致命，但迁移成本高。这一篇从游戏项目的实际需求出发，对比三个主流 CI 工具，给出选型建议。

## 三大工具对比

### 基础对比

| 维度 | Jenkins | GitHub Actions | GitLab CI |
|------|---------|---------------|-----------|
| 部署模式 | 自托管 | 云托管 + 自托管 Runner | 云托管 + 自托管 Runner |
| 管线定义 | Jenkinsfile (Groovy) | YAML (.github/workflows/) | YAML (.gitlab-ci.yml) |
| 插件生态 | 最丰富（1800+ 插件） | Marketplace（Actions） | 内置功能为主 |
| 学习曲线 | 陡峭（Groovy + 插件配置） | 平缓（YAML + 模板丰富） | 中等 |
| 社区支持 | 最成熟 | 增长最快 | 稳定 |

### 游戏项目关键维度

| 维度 | Jenkins | GitHub Actions | GitLab CI |
|------|---------|---------------|-----------|
| macOS Runner | 自建 Mac 构建机 | 官方提供（$0.08/min） | 自建 Mac 构建机 |
| Windows Runner | 自建 | 官方提供 + 自建 | 自建 |
| Unity 支持 | GameCI 插件 / 自定义 | GameCI Action | 自定义脚本 |
| 大型仓库 | 无限制（自托管） | LFS 支持但有存储限制 | LFS 支持，自托管无限制 |
| 构建缓存 | 本地磁盘（自托管） | actions/cache（有大小限制） | CI/CD 缓存（自托管无限制） |
| 并行构建 | 取决于 Agent 数量 | 取决于 Runner 数量和并发限制 | 取决于 Runner 数量 |
| 私密性 | 完全自控 | 代码在 GitHub | 自托管可完全自控 |

### 成本对比

| 场景 | Jenkins | GitHub Actions | GitLab CI |
|------|---------|---------------|-----------|
| 小团队（5 人） | 一台自建服务器成本 | 免费额度通常够用 | 免费额度 + 自建 Runner |
| 中型团队（20 人） | 3-5 台构建机 + 维护人力 | 云 Runner 费用显著 | 自托管 + Ultimate 许可费 |
| 大团队（50+ 人） | 构建集群 + 专人维护 | 自建 Runner 为主 | 自建 Runner 为主 |
| macOS 构建 | 自购 Mac Mini/Pro | $0.08/min（贵） | 自购 Mac |
| 存储（大型游戏仓库） | 自控 | LFS 额外收费 | 自托管自控 |

## Jenkins：最灵活，维护成本最高

### 优势

- **完全自托管**：代码和构建产物不离开公司网络，安全性最高
- **插件生态最丰富**：几乎任何需求都有现成插件
- **Pipeline as Code**：Jenkinsfile 支持复杂的逻辑分支和条件判断
- **无并发限制**：取决于硬件资源，不受平台配额约束
- **构建缓存无限制**：本地磁盘，Library 缓存可以几十 GB

### 劣势

- **维护成本高**：需要专人维护 Jenkins 服务器、插件更新、安全补丁
- **Groovy 学习曲线**：Jenkinsfile 的 Groovy 语法比 YAML 复杂
- **UI 老旧**：默认界面不如竞品现代（Blue Ocean 插件有改善）
- **升级风险**：插件版本兼容性问题是常见的维护负担

### 适合的场景

- 对代码安全性要求高（金融、军工、大型游戏公司）
- 构建需求复杂，需要高度定制
- 有专职 DevOps 人员维护基础设施
- 已有 Jenkins 资产，迁移成本高

## GitHub Actions：云原生，生态好

### 优势

- **零维护**：云托管 Runner 不需要维护构建机
- **生态好**：Marketplace 有大量现成 Action，GameCI 提供完整的 Unity 构建支持
- **YAML 配置**：简洁直观，学习成本低
- **与 GitHub 深度集成**：PR 检查、Issue 联动、Release 发布一体化

### 劣势

- **macOS Runner 贵**：$0.08/min，一次 iOS 构建（60 分钟）约 $4.8
- **缓存限制**：每个仓库 10 GB 缓存上限，游戏项目的 Library 缓存可能不够
- **大型仓库支持有限**：LFS 存储和带宽有额外费用
- **构建时间限制**：单个 Job 最长 6 小时（通常够用，但全量构建可能超时）
- **代码在 GitHub**：部分公司不接受代码托管在外部

### 适合的场景

- 开源项目或已使用 GitHub 托管代码的团队
- 中小型项目，构建频率不高
- 不需要频繁 iOS 构建（或愿意自建 macOS Runner）
- 重视开发体验和生态便利性

## GitLab CI：自托管体验最好

### 优势

- **自托管体验好**：GitLab 自托管版 + Runner，完整自控
- **内置功能丰富**：CI/CD、制品库、Container Registry 内置
- **YAML 配置**：语法清晰，include/extends 复用机制好
- **Runner 管理**：Runner 注册和管理比 Jenkins Agent 简单

### 劣势

- **Ultimate 许可费**：高级功能（安全扫描、合规等）需要 Ultimate 版
- **社区不如 GitHub 活跃**：第三方集成和模板相对少
- **自托管维护**：GitLab 服务器本身需要维护（升级、备份、性能）
- **Unity 支持需自建**：没有像 GameCI 那样成熟的官方集成

### 适合的场景

- 已使用 GitLab 托管代码的团队
- 希望自托管但不想维护 Jenkins 的团队
- 需要 Git 仓库 + CI/CD + 制品库一体化的团队

## Unity 特有考量

### Unity 许可证管理

CI 构建机上运行 Unity 需要许可证。许可证管理是游戏项目 CI 的特有难题：

| 许可证类型 | CI 使用方式 | 注意事项 |
|-----------|-----------|---------|
| Personal | 可用于 CI（收入限制内） | 需要手动激活，不适合多 Agent |
| Plus/Pro | 浮动许可证或指定机器 | 许可证数量需覆盖所有 CI Agent |
| Unity Build Server | 专为 CI 设计 | 费用额外，但许可证管理最简单 |

**Unity Build Server License** 是专为 CI 场景设计的许可证类型——按构建机数量付费，不需要为每台 Agent 单独激活。对于有多台构建机的团队，这是最省心的选择。

### batchmode 构建

CI 上的 Unity 构建必须使用 batchmode（无 GUI）：

```
Unity -batchmode -nographics -quit
      -projectPath /path/to/project
      -executeMethod BuildScript.PerformBuild
      -buildTarget iOS
      -logFile build.log
```

batchmode 的注意事项：

| 注意事项 | 说明 |
|---------|------|
| 无 GUI | 不能依赖 EditorWindow 交互 |
| 错误处理 | 需要在 BuildScript 中捕获异常并设置退出码 |
| 日志输出 | 指定 -logFile，否则日志输出到 stdout |
| 超时保护 | 设置超时，防止 Unity 卡死导致 CI Agent 被占用 |

### Library 缓存策略（CI 工具对比）

| CI 工具 | Library 缓存方案 | 限制 |
|---------|-----------------|------|
| Jenkins | Agent 本地磁盘，天然持久 | 磁盘空间需规划 |
| GitHub Actions | actions/cache，跨 Job 共享 | 10 GB 上限，可能不够 |
| GitLab CI | CI cache 或 Runner 本地 | 自托管无限制 |

**实践建议**：如果用 GitHub Actions，Library 缓存考虑用自建 Runner（缓存存本地磁盘）而非云 Runner（受 10 GB 限制）。

## 选型决策树

```
代码是否可以放在外部托管？
  ├─ 否 → Jenkins 或 GitLab 自托管
  │        ├─ 有专职 DevOps → Jenkins
  │        └─ 没有专职 DevOps → GitLab 自托管
  └─ 是 → 当前用什么代码托管？
           ├─ GitHub → GitHub Actions（+ 自建 macOS Runner）
           ├─ GitLab.com → GitLab CI
           └─ 其他 → 按团队规模和预算选择
```

没有"最好"的 CI 工具，只有最适合当前团队规模、安全要求和预算的选择。初期选错了也不致命——管线逻辑在脚本层，CI 工具只是调度层，迁移时脚本可以复用。

## V16 系列总结

V16 六篇文章覆盖了 CI/CD 管线的完整知识体系：

| 篇目 | 解决的问题 |
|------|-----------|
| 01 本质 | CI/CD 是什么、五阶段模型 |
| 02 架构 | 多端并行、扇入扇出、失败隔离 |
| 03 构建 | 脚本设计、环境管理、缓存、归档 |
| 04 质量门 | 五类检查的 CI 集成 |
| 05 部署 | 三端部署自动化 + CDN 发布 |
| 06 选型 | Jenkins / GitHub Actions / GitLab CI 对比 |

管线把 V01-V15 定义的标准和流程变成了自动执行的代码。V17 将讲管线产出的构建如何通过灰度策略安全地推送给用户——从"能自动打包"到"能安全上线"。

## 小结与检查清单

- [ ] 是否评估了三大 CI 工具在游戏项目关键维度上的表现
- [ ] 是否考虑了 macOS Runner 的成本和可用性（iOS 构建必需）
- [ ] Unity 许可证在 CI 上的管理方式是否明确
- [ ] batchmode 构建脚本是否有错误处理和超时保护
- [ ] Library 缓存策略是否适配所选 CI 工具的缓存机制
- [ ] 管线脚本是否与 CI 工具解耦（脚本层可复用，调度层可替换）

---

**下一步应读**：V17 灰度上线与线上运营系列 — 构建产出后怎么安全地推送给用户

**扩展阅读**：[多端构建系列]({{< relref "delivery-engineering/delivery-multiplatform-build-series-index.md" >}}) — V07 覆盖了各平台构建的具体配置和注意事项
