# Article 31 Card｜Profile、Bundle、Provider 与 Capability Seam

## Problem Space

配置文件出现一个 Provider，不等于 Capability 已经可用；Profile 名称也不能说明最终生效配置。本篇要重建 Effective Configuration，并沿一个真实 Provider/Consumer seam 验证能力如何出现。

## Required Questions

1. Profile、Bundle、Patch/Overlay 的真实 Schema 与加载顺序是什么？
2. Base Bundle + Profile + Patch/Overlay 怎样形成 Effective Configuration？
3. Service Definition、Provider、Consumer 如何组成 Capability Seam？
4. Headless 与 Web 共享什么，Host 差异在哪一层？
5. 两个 Profile dump 有哪些结构性差异，哪些差异来自本地 overlay？
6. BuildPilot 为什么只吸收 capability set 与 read-only profile？

## Boundaries

- 不把 config row 写成 activation。
- 不把两个 dump 的差异写成所有运行时差异。
- 不启动 Article 32—44。
