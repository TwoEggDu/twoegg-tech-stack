---
title: "CI/CD 管线 04｜质量门自动化——编译、资源、Shader 变体、性能基线"
slug: "delivery-cicd-pipeline-04-quality-gates"
date: "2026-04-14"
description: "质量门不是人工 Review 的替代品，而是在人工介入之前先用机器拦住确定性问题。五类检查覆盖编译、资源、Shader、包体和性能。"
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

## 五类质量检查

### 检查一：编译检查

**目标：零 Error、Warning 在预算内。**

| 检查项 | 工具 | 阈值 | 失败处理 |
|--------|------|------|---------|
| 编译 Error | Unity -batchmode -buildTarget | 0（零容忍） | 管线终止，通知提交者 |
| 编译 Warning | Unity 构建日志解析 | ≤N 条（团队约定） | 超预算标记失败 |
| Lint / 静态分析 | Roslyn Analyzer / 自定义规则 | 严重级别零容忍 | 管线终止 |
| 代码规范 | .editorconfig + 格式化检查 | 不合规文件数 = 0 | 标记失败或自动修复 |

**Warning 预算的管理方法**：

1. 记录当前 Warning 总数作为基线（如 200 条）
2. 新提交不允许增加 Warning（≤200）
3. 每个迭代降低预算（200→180→160...）
4. 最终目标是零 Warning 或接近零

### 实际的 CI 质量门脚本示例

以下是编译检查阶段实际使用的脚本片段，展示了怎么在 CI 中自动化这些检查：

```bash
#!/bin/bash
# === 编译检查质量门 ===

UNITY_PATH="/Applications/Unity/Hub/Editor/2022.3.20f1/Unity.app/Contents/MacOS/Unity"
PROJECT_PATH="$(pwd)"
BUILD_LOG="/tmp/unity_build.log"

# 1. Unity 批处理编译
$UNITY_PATH \
  -batchmode -nographics -quit \
  -projectPath "$PROJECT_PATH" \
  -executeMethod BuildScript.BuildAll \
  -logFile "$BUILD_LOG" 2>&1

BUILD_EXIT=$?

# 2. 解析编译错误
ERROR_COUNT=$(grep -c "^Compilation error:" "$BUILD_LOG" || echo "0")
if [ "$ERROR_COUNT" -gt 0 ]; then
  echo "FATAL: $ERROR_COUNT compilation errors found"
  grep "^Compilation error:" "$BUILD_LOG"
  exit 1
fi

# 3. 统计 Warning 数量并对比预算
WARNING_COUNT=$(grep -c "^warning CS" "$BUILD_LOG" || echo "0")
WARNING_BUDGET=$(cat .ci/warning_budget.txt)  # 预算值存在版本库里
if [ "$WARNING_COUNT" -gt "$WARNING_BUDGET" ]; then
  echo "FAIL: $WARNING_COUNT warnings exceeds budget of $WARNING_BUDGET"
  echo "New warnings:"
  diff <(cat .ci/warning_baseline.txt) \
       <(grep "^warning CS" "$BUILD_LOG" | sort) \
       || true
  exit 1
fi

# 4. 检查是否有新增的 #pragma warning disable
SUPPRESS_COUNT=$(git diff HEAD~1 --unified=0 -- '*.cs' \
  | grep "^+" | grep -c "#pragma warning disable" || echo "0")
if [ "$SUPPRESS_COUNT" -gt 0 ]; then
  echo "WARN: $SUPPRESS_COUNT new warning suppressions detected"
  git diff HEAD~1 --unified=0 -- '*.cs' \
    | grep -A2 "#pragma warning disable"
  # 不阻断但标记为 Warning
fi

echo "PASS: Compilation check passed ($ERROR_COUNT errors, $WARNING_COUNT/$WARNING_BUDGET warnings)"
```

**几个实操细节**：

- Warning 预算值 `warning_budget.txt` 必须存在版本库里，不能存在 CI 配置中——这样修改预算也需要走 Code Review
- Warning 基线文件 `warning_baseline.txt` 每次预算下调时更新，用于对比"新增了哪些 Warning"
- `#pragma warning disable` 的检测是一个容易被忽略但很重要的检查——有些开发者会用这个来绕过 Warning 预算

### 检查二：资源合规检查

**目标：资源符合 V02-03 定义的标准。**

| 检查项 | 检查内容 | 阈值 |
|--------|---------|------|
| 命名规范 | 文件名是否符合命名约定 | 不合规文件数 = 0 |
| 纹理格式 | 是否使用了约定的压缩格式（ASTC/ETC2） | 不合规文件数 = 0 |
| 纹理尺寸 | 是否超过最大尺寸限制（如 2048x2048） | 不合规文件数 = 0 |
| 模型面数 | 单个模型是否超过面数上限 | 按资源类型区分 |
| 音频格式 | 采样率和压缩格式是否合规 | 不合规文件数 = 0 |
| 重复资源 | 是否有内容相同但路径不同的资源 | 重复数 = 0 |

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
| 变体数增量 | 与上一次基线对比 | 单次增量不超过 M |
| 单个 Shader 变体数 | 逐 Shader 统计 | 超限的 Shader 列出告警 |

**CI 集成方式**：

1. 构建时开启 Shader 编译日志
2. 构建后脚本解析日志，提取变体统计
3. 与上一次成功构建的基线对比
4. 增量超阈值 → 标记失败，列出新增变体来源

### 检查四：包体大小预算

**目标：包体大小不超过 V06 / V14 定义的预算。**

| 检查项 | 数据来源 | 阈值 |
|--------|---------|------|
| 总包体大小 | Build Report / 构建产物文件大小 | ≤预算（按渠道区分） |
| 包体增量 | 与上一个版本对比 | 单版本增量不超过 N MB |
| Top N 大文件 | Build Report 解析 | 列出便于排查 |
| 资源类型分布 | 纹理/音频/Mesh/代码各占多少 | 趋势监控 |

**报告示例**：

| 平台 | 当前大小 | 预算 | 状态 | 与上版本差异 |
|------|---------|------|------|------------|
| iOS | 186 MB | 200 MB | 通过 | +4.2 MB |
| Android APK | 142 MB | 150 MB | 通过 | +3.8 MB |
| 微信首包 | 18.5 MB | 20 MB | 通过 | +0.3 MB |

### 检查五：性能基线回归

**目标：关键性能指标不劣于上一个版本的基线。**

| 检查项 | 采集方式 | 判定标准 |
|--------|---------|---------|
| 帧时间 | 自动化性能跑测 | 不超过基线的 110% |
| 内存峰值 | 自动化内存跑测 | 不超过基线 + N MB |
| 启动时间 | 冷启动计时 | 不超过基线的 120% |
| 加载时间 | 场景切换计时 | 不超过基线的 120% |

**注意**：性能基线检查通常只在完整通道（每日构建/发布构建）中执行，快速通道不包含——因为需要真机跑测，耗时太长。

### 性能基线管理的实操经验

性能基线检查看起来简单，但实际操作中有几个坑：

**坑一：基线设备的一致性**。性能数据在不同设备上差异巨大，基线必须在**同一台物理设备**上采集才有对比意义。某次我们的基线采集设备因为温度过高（放在机柜里散热不好）导致 CPU 降频，基线数据比正常偏低 15%——之后每次比对都显示"性能劣化"，误报了两周才有人排查出是设备问题。

**坑二：基线的更新策略**。基线不能永远不变——随着内容增长（更多关卡、更多角色、更多特效），内存和加载时间自然会增长。我们的做法是：
- 每个大版本发布后更新基线
- 基线更新需要 Technical Lead 审批（防止有人为了通过检查故意放宽基线）
- 基线历史保留，可以看到从第一个版本到现在的性能趋势

**坑三：性能波动导致的误报**。同一台设备、同一个包，两次跑性能测试的结果可能有 3-5% 的波动（后台进程、温度、存储碎片等因素）。因此"劣化 ≤ 5%"这个阈值实际上需要设到 8-10% 才能避免频繁误报。或者更好的做法是跑三次取中位数，再和基线比对。

## 检查结果聚合

五类检查的结果应该聚合成一份统一报告，而不是分散在五个不同的日志文件中：

```
质量门聚合报告 — Build #456
══════════════════════════════════════
编译检查        ✓ 通过   (0 Error, 12 Warning, 预算 20)
资源合规检查     ✗ 失败   (3 个文件不合规)
Shader 变体     ✓ 通过   (12,340 个变体, 预算 15,000)
包体大小        ✓ 通过   (iOS 186MB / 预算 200MB)
性能基线        ○ 跳过   (快速通道不执行)
══════════════════════════════════════
总体判定：失败（资源合规检查未通过）
```

## 误报管理

质量门不能"经常失败但大家都忽略"——那样等于没有门。

| 误报原因 | 处理方法 |
|---------|---------|
| 阈值设置过严 | 根据历史数据调整到合理范围 |
| 规则不适用于某些资源 | 在检查脚本中维护豁免列表 |
| 基线数据不准确 | 定期重新采集基线 |
| 外部依赖变化导致误报 | 检查脚本区分项目资源和第三方资源 |

**豁免机制**：

- 豁免列表存储在版本库中（如 `quality_exemptions.json`）
- 每条豁免有原因说明和有效期
- 过期豁免自动失效，强制重新审查
- 豁免变更需要通过 Code Review

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
- [ ] 性能基线是否在完整通道中检测回归
- [ ] 五类检查结果是否聚合成统一报告
- [ ] 误报率是否在可接受范围（不导致团队忽略质量门）
- [ ] 豁免机制是否有有效期和审查流程

---

**下一步应读**：[部署自动化]({{< relref "delivery-engineering/delivery-cicd-pipeline-05-deployment-automation.md" >}}) — fastlane、Gradle、微信 CLI 与 CDN 发布

**扩展阅读**：[验证与测试系列]({{< relref "delivery-engineering/delivery-verification-testing-series-index.md" >}}) — V13 定义了质量门背后的验证体系
