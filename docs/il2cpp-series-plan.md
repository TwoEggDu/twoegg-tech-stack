# IL2CPP 系列计划

更新：2026-10-25。本文是该系列唯一的 canonical 篇级目录。目录含 10 篇正文，索引另计。

## 定位与边界

连接 ECMA-335 的程序描述与 HybridCLR 的动态扩展：先理解产物分工，再沿转换器、runtime、metadata、泛型、GC、能力边界和裁剪阅读。公开仓只描述行为与实验，不转载 Unity 受限 C++ 实现。

## 阅读顺序与状态

文件均在 `content/engine-toolchain/`。已写仅表示正文存在。

| 顺序 | 文件 | 职责与状态 |
|---|---|---|
| 导读 | `il2cpp-runtime-map-global-metadata-gameassembly-libil2cpp.md` | 已写；唯一运行时地图入口 |
| D1 | `il2cpp-architecture-csharp-to-cpp-to-native-pipeline.md` | 已写；核心机制 |
| D2 | `il2cpp-converter-il-to-cpp-code-generation.md` | 已写；核心机制 |
| D3 | `il2cpp-libil2cpp-runtime-metadatacache-class-runtime.md` | 已写；核心机制 |
| D4 | `il2cpp-global-metadata-dat-format-loading-binding.md` | 已写；核心机制 |
| D5 | `il2cpp-generic-code-generation-sharing-instantiation.md` | 已写；核心机制 |
| D6 | `il2cpp-gc-integration-boehm-wbarrier-finalization.md` | 已写；核心机制 |
| D7 | `il2cpp-ecma335-coverage-supported-unsupported.md` | 已写；核心机制 |
| D8 | `il2cpp-managed-code-stripping-linker-linkxml.md` | 已写；核心机制 |
| 按需排查 | `il2cpp-global-metadata-deep-dive.md` | 已重整；构建同源、格式兼容、符号匹配，保留旧 URL |

发布索引：`il2cpp-series-index.md`。推荐从导读进入 D1～D8，排障专题按需阅读；之后接 HybridCLR 源码阅读起步。

## 去重决策

原两篇“运行时地图”同名且重复。保留 runtime-map 为概念入口；deep-dive 改名并收拢成产物一致性排查，重复的 runtime 分工和文件格式解释通过链接复用。与 D4 的边界：D4 解释结构和绑定，排查专题解释证据与检查顺序。

## 待补强

- 转换器与 runtime 篇：补固定构建样本的 C#、IL、生成物和运行时对象对应关系。
- 与 ECMA-335、HybridCLR 共用一个泛型调用综合实验，待写。
- 当前不声称全系列经过统一 Unity patch 的 Player 验证。

旧 `il2cpp-runtime-map-article-plan.md` 是导读单篇的历史执行材料，不承担本系列进度管理。
