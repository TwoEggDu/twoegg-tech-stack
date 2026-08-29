# Article 30 Card｜Everything is a Plugin：插件内核如何承载 Capability 与生命周期

## Identity

- Canonical ID: `30`
- Part: `VI｜DeepSeek Harness`
- Weight: `M`
- Optional: `NO`
- Required Evidence Work: `PINNED SOURCE MAP + ONE REAL PLUGIN LIFECYCLE TRACE`
- Article Type: `SOURCE_TRACE / LIFECYCLE`

## Problem Space

“Everything is a Plugin”若只按目录或注册表解释，会掩盖 Capability 何时出现、Effect 属于哪个 Scope，以及 Dispose 是否真正撤销贡献。本篇必须用一个真实插件闭合生命周期。

## Core Questions

1. Plugin、Cordis Context、Service、Event、Effect、Scope、Dependency、Dispose 的固定源码对象是什么？
2. 一个代表性插件怎样从 install 进入 register、operate 与 dispose？
3. Scope 与 reversible effect 的真实语义是什么？
4. 隐式依赖、初始化顺序与调试代价如何出现？
5. 普通 DI / 模块化单体何时已经足够？
6. BuildPilot 为什么默认 `SIMPLIFY`？

## Frozen Boundaries

- 不遍历全部插件。
- 不混淆 Plugin Context / Model Context、Plugin Event / Session Event、Plugin / Tool。
- 不把 source path 自动写成 runtime confirmation。
- Article 31—37 只保留边界；Article 38—44 不启动。
