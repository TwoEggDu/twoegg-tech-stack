---
title: "多端构建 06｜微信小游戏构建专项——从 Unity WebGL 到微信环境的转换链路"
slug: "delivery-multiplatform-build-06-wechat"
date: "2026-04-14"
description: "Unity 构建微信小游戏的产物不是原生应用，而是 WebGL 输出经过适配层转换后的小游戏包。WASM 编译、JS 桥接、内存限制和平台 API 适配——每一步都有特有的工程问题。"
tags:
  - "Delivery Engineering"
  - "Build System"
  - "WeChat Mini Game"
  - "WebGL"
  - "WASM"
series: "多端构建"
primary_series: "delivery-multiplatform-build"
series_role: "article"
series_order: 60
weight: 660
delivery_layer: "platform"
delivery_volume: "V07"
delivery_reading_lines:
  - "L2"
  - "L5"
---

## 这篇解决什么问题

微信小游戏不是原生应用——它运行在微信的 JS 环境中，通过 WebGL 渲染。Unity 构建微信小游戏的链路比原生平台长且特殊，需要专门的工程处理。

## 构建链路

```
Unity 项目
    ↓ BuildTarget.WebGL
Unity WebGL 产物（.wasm + .js + .data）
    ↓ 微信小游戏适配工具（Unity 官方插件或社区工具）
微信小游戏项目（game.js + game.json + WASM + 资源）
    ↓ 微信开发者工具
上传提审
```

### 阶段一：Unity WebGL 构建

Unity 的 WebGL 构建将 IL2CPP 产出的 C++ 代码编译为 WebAssembly（WASM），引擎运行时编译为 JS + WASM 混合：

| 产物 | 内容 | 典型大小 |
|------|------|---------|
| .wasm | IL2CPP 编译的游戏代码 + 引擎运行时 | 10-30MB（压缩前） |
| .framework.js | 引擎 JS 胶水代码 | 1-3MB |
| .data | StreamingAssets 和首包资源 | 取决于内容量 |
| .loader.js | 加载器脚本 | 几 KB |

**WASM 体积优化**是微信小游戏构建的关键——因为 WASM 文件通常是主包中最大的部分。

优化手段：
- Managed Stripping Level = High
- Engine Code Stripping 开启
- 去除不需要的 Unity 模块（Physics、Animation 等不用的模块）
- 压缩：Brotli 可以把 WASM 压缩到原始大小的 30-40%

### 阶段二：微信适配

Unity WebGL 产物不能直接作为微信小游戏运行。需要通过适配层转换：

**适配层的职责**：
- 将 Unity 的 WebGL 渲染调用适配到微信的 Canvas/WebGL 环境
- 将 Unity 的文件系统调用适配到微信的虚拟文件系统
- 将 Unity 的音频系统适配到微信的音频 API
- 提供 Unity C# 到微信 JS API 的桥接

**官方工具**：Unity 提供了微信小游戏转换插件（`com.unity.wechat-minigame`），自动完成大部分适配工作。

**社区工具**：也有社区维护的转换方案，可能支持更灵活的定制。

### 阶段三：微信开发者工具

适配后的项目通过微信开发者工具上传和调试：

```bash
# 命令行上传（CI 集成）
miniprogram-ci upload \
  --project /path/to/minigame \
  --version 1.2.3 \
  --desc "Build #456"
```

`miniprogram-ci` 是微信提供的命令行工具，可以在 CI 中自动化上传流程。

## 微信环境的构建约束

### 内存限制

微信小游戏的可用内存远小于原生应用。构建时需要在 Unity 的 WebGL 设置中配置内存上限：

| 设置 | 建议值 | 说明 |
|------|--------|------|
| Initial Memory Size | 64-128MB | 启动时分配的内存 |
| Maximum Memory Size | 256-512MB | 最大可用内存 |
| Memory Growth Mode | Linear / None | 是否允许动态增长 |

**内存增长的坑**：如果设置为允许增长（Linear），WASM 内存会在需要时扩展。但每次扩展需要重新分配内存并复制数据——扩展大块内存时可能导致瞬时卡顿。建议在启动时就分配足够的内存（Initial 设大一些），减少运行时扩展。

### WebGL 渲染限制

微信小游戏的 WebGL 环境和原生 OpenGL ES / Metal 有差异：

| 限制 | 影响 | 应对 |
|------|------|------|
| 不支持 Compute Shader | GPU 计算类功能不可用 | 改用 CPU 实现或裁掉 |
| WebGL 2.0 纹理格式 | 支持 ASTC 但部分旧设备不支持 | 准备 fallback 格式 |
| Shader 精度 | mediump / lowp 在不同设备上表现差异大 | 统一使用 highp 或明确标注精度 |
| 最大纹理尺寸 | 部分设备限制为 4096 | 控制纹理最大尺寸 |
| Draw Call 开销 | WebGL 的 Draw Call 开销高于原生 | 更激进的批处理策略 |

### JS 桥接

Unity C# 代码和微信 JS API 之间的通信通过桥接实现：

```csharp
// C# 侧调用微信 API
[DllImport("__Internal")]
private static extern void WXShowShareMenu();

// JS 侧实现
mergeInto(LibraryManager.library, {
    WXShowShareMenu: function() {
        wx.showShareMenu({ withShareTicket: true });
    }
});
```

**桥接的性能注意**：C# 到 JS 的调用有序列化/反序列化开销。高频调用（如每帧调用微信 API）会显著影响性能。应该在 C# 侧缓存数据，减少跨语言调用次数。

## CI 中的微信小游戏构建

```
1. Unity WebGL 构建（产出 WASM + JS + 资源）
2. 微信适配工具转换（产出小游戏项目）
3. miniprogram-ci 检查（代码质量、包体大小）
4. miniprogram-ci 上传（自动化提审）
5. 归档构建产物
```

**CI Agent 要求**：
- 安装了 Node.js（miniprogram-ci 依赖）
- 安装了微信开发者工具或 miniprogram-ci
- 配置了小游戏的 AppID 和上传密钥

## 常见事故与排障

**事故：WASM 文件超出主包限制**。Unity WebGL 构建的 WASM 文件压缩后仍然超过 4MB。解法：开启所有 Stripping 选项、去除不需要的 Unity 模块、考虑 WASM 分包加载（需要引擎版本支持）。

**事故：某些设备上 WebGL 渲染异常**。Shader 在 WebGL 环境下的精度行为和原生不同。排查路径：用微信开发者工具的远程调试连接真机，查看 WebGL 错误日志。

**事故：JS 桥接调用返回 undefined**。C# 的 `[DllImport]` 函数名和 JS 的 `mergeInto` 中的函数名不匹配（大小写敏感）。排查路径：检查两侧的函数名是否完全一致。

## 小结与检查清单

- [ ] WASM 体积是否经过 Stripping 和压缩优化
- [ ] 内存上限是否在 WebGL 设置中明确配置
- [ ] Shader 是否有 WebGL 兼容版本（无 Compute Shader 依赖）
- [ ] JS 桥接的函数名是否两侧一致
- [ ] CI 是否集成了 miniprogram-ci 自动上传
- [ ] 构建产物是否在微信开发者工具中验证过

---

**下一步应读**：[构建时间优化]({{< relref "delivery-engineering/delivery-multiplatform-build-07-build-optimization.md" >}}) — 三端构建从 2 小时压到 30 分钟

**扩展阅读**：V06-05 微信小游戏分发 — 构建后的分包策略和 CDN 资源管理
