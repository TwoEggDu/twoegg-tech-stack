---
title: "脚本热更新 06｜热更新事故模式——依赖断裂、元数据缺失与版本不匹配"
slug: "delivery-hot-update-06-incident-patterns"
date: "2026-04-14"
description: "热更新出事故时，症状通常是'崩溃'或'功能异常'，但根因分散在构建链路、CDN 部署和运行时加载的不同环节。这篇按事故模式分类，给出排查路径。"
tags:
  - "Delivery Engineering"
  - "Hot Update"
  - "Incident"
  - "Troubleshooting"
series: "脚本热更新"
primary_series: "delivery-hot-update"
series_role: "article"
series_order: 60
weight: 760
delivery_layer: "principle"
delivery_volume: "V08"
delivery_reading_lines:
  - "L1"
  - "L2"
  - "L4"
---

## 这篇解决什么问题

热更新出了事故，通常表现为三种症状：崩溃、功能异常、性能退化。但这三种症状可能对应十几种不同的根因。这一篇按事故模式分类，给出每种模式的症状、根因和排查路径。

## 事故模式分类

热更新事故的根因分布在四个环节：

```
构建环节 → CDN 部署环节 → 客户端下载环节 → 运行时加载环节
```

### 模式一：元数据缺失

**症状**：热更后某些泛型方法调用时崩溃，错误信息 `ExecutionEngineException` 或 `TypeLoadException`。

**根因**：AOT 元数据没有正确补充。热更代码调用了首包 AOT 编译时不存在的泛型实例化。

**排查路径**：
1. 查看崩溃堆栈中的方法签名——确认是否是泛型方法
2. 检查元数据 DLL 是否包含该方法所在的 Assembly
3. 检查元数据 DLL 的来源——是否从首包构建时提取（而非手动管理）
4. 检查首包和热更包的构建是否使用同一版本的 Unity 和 HybridCLR

**预防**：AOT 元数据提取和归档完全自动化，CI 中校验元数据版本和首包版本的一致性。

### 模式二：依赖断裂

**症状**：热更后某些类无法加载，错误信息 `TypeLoadException` 或 `FileNotFoundException`。

**根因**：热更 Assembly 引用了不在热更包中也不在首包中的类型。常见原因：
- 热更代码引用了一个新版本首包才有的类型，旧首包用户崩溃
- 热更代码引用了一个被 Stripping 裁掉的类型
- 热更 Assembly 的依赖列表变了，但运行时没有加载所有依赖

**排查路径**：
1. 查看 TypeLoadException 中的类型名称
2. 确认该类型属于哪个 Assembly
3. 检查该 Assembly 是否在首包中存在且未被 Stripping 裁掉
4. 如果是新类型，检查是否超出了热更的兼容性范围

**预防**：V08-05 的首包兼容性验证（用最近 3 个版本首包测试热更包）。

### 模式三：版本不匹配

**症状**：热更后整体行为异常——不是某个方法崩溃，而是大面积的逻辑错误或渲染异常。

**根因**：热更包的构建环境和首包不一致：
- Unity 版本不同（甚至小版本不同也可能导致 IL 差异）
- IL2CPP 版本不同（元数据格式不兼容）
- HybridCLR 版本不同（解释器行为差异）
- 编译选项不同（Debug vs Release）

**排查路径**：
1. 对比首包和热更包构建时的环境信息（CI 日志中应该有记录）
2. 确认 Unity 版本号精确匹配（包括 patch 版本）
3. 确认 HybridCLR Package 版本一致
4. 确认 Stripping 级别一致

**预防**：CI 中锁定构建环境，首包和热更包从同一个 CI 配置中产出。构建日志中记录完整的环境版本信息。

### 模式四：CDN 缓存导致的不完整更新

**症状**：部分用户热更正常，部分用户热更后异常。异常用户的分布和地理区域相关。

**根因**：CDN 某些边缘节点返回了旧版本的 Manifest 或 Bundle 文件。用户拿到的是新旧混合的文件——新 Manifest + 旧 Bundle 或旧 Manifest + 新 Bundle。

**排查路径**：
1. 收集异常用户的地理分布——如果集中在某些区域，高度怀疑 CDN 缓存问题
2. 用异常用户的 IP 段请求 CDN，检查返回的文件版本
3. 检查 Manifest 的 Cache-Control header 是否正确（应该是 no-cache）
4. 检查 Bundle 文件名是否包含内容哈希（[V06-06 CDN 分发]({{< relref "delivery-engineering/delivery-package-distribution-06-cdn.md" >}}) 已覆盖）

**预防**：Manifest 使用 no-cache + 短 TTL，Bundle 文件名包含内容哈希，部署后在所有 CDN 区域验证文件可用性。

### 模式五：下载中断导致的部分更新

**症状**：热更下载完成后功能异常——某些资源缺失或内容不匹配。

**根因**：下载过程中断网，恢复后断点续传逻辑有 Bug——某些文件只下载了一半但被标记为"已完成"，或者部分文件是新版本、部分是旧版本。

**排查路径**：
1. 检查本地所有热更文件的哈希——和 Manifest 中的哈希逐一比对
2. 找到哈希不匹配的文件——这些就是下载不完整的文件
3. 检查断点续传逻辑——是否在文件级别记录了下载完成状态
4. 检查原子替换逻辑——是否在所有文件下载完成后才从临时目录移到正式目录

**预防**：[V06-07 热更新资源管线]({{< relref "delivery-engineering/delivery-package-distribution-07-hotupdate-resources.md" >}}) 的原子性替换机制（先下载到临时目录，全部校验通过后再替换）。

### 模式六：热更后首次加载卡顿

**症状**：热更包下载后首次进入游戏明显卡顿（2-5 秒），后续正常。

**根因**：热更的 Shader 没有预热。新的 Shader 变体在首次使用时需要编译，编译阻塞了渲染线程。

**排查路径**：
1. 用 Profiler 查看首帧的耗时分布——如果 Shader.Parse 或 Shader.CreateGPUProgram 耗时长，确认是 Shader 预热问题
2. 检查热更包是否包含了新的 Shader 或新的 Shader 变体
3. 检查 Shader 预热逻辑是否覆盖了热更新增的变体

**预防**：热更包包含新 Shader 时，更新 ShaderVariantCollection 并在下次启动时预热。

## 事故响应流程

热更新事故的响应应该有标准流程：

```
1. 发现（监控告警 / 用户反馈）
   ↓
2. 评估影响范围（影响多少用户？是全量还是部分？）
   ↓
3. 决策：回滚 or 修复
   - Crash 率 > 阈值 → 立即回滚（把 Manifest 指回旧版本）
   - Crash 率在阈值内 → 定位根因 → 修复 → 发新热更包
   ↓
4. 执行回滚或修复
   ↓
5. 验证（回滚后 Crash 率是否恢复正常 / 修复后问题是否消失）
   ↓
6. 复盘（根因是什么、为什么没在发布前验证中拦住、怎么预防）
```

**回滚的执行时间应该在 5 分钟以内**——把 CDN 上的 Manifest 指回上一个版本。这不需要重新构建，只需要更新一个文件。

## 小结与检查清单

- [ ] 是否建立了热更新事故的分类体系（知道可能出哪些类型的事故）
- [ ] 每种事故模式是否有排查路径文档
- [ ] 回滚操作是否预设为一键脚本（5 分钟内执行）
- [ ] CDN Manifest 的缓存策略是否正确
- [ ] 下载的原子性替换机制是否经过验证
- [ ] 热更后是否有 Shader 预热覆盖新增变体
- [ ] 事故响应后是否有复盘和防复发措施

---

V08 脚本热更新到这里结束。

六篇文章覆盖了：本质（01）、架构选型（02）、HybridCLR 工程化（03）、DHE 进阶（04）、验证策略（05）和事故模式（06）。

**推荐下一步**：V09 平台发布 — 从构建和热更新进入平台发布：iOS / Android / 微信各自的审核和发布流程

**扩展阅读**：

- [案例：一次热更新上线事故的复盘]({{< relref "projects/case-hotupdate-production-incident.md" >}}) — 完整的事故时间线、根因分析和防复发措施
- [HybridCLR 故障诊断手册｜遇到报错时先判断是哪一层坏了]({{< relref "engine-toolchain/hybridclr-troubleshooting-diagnose-by-layer.md" >}}) — 模式一~三（元数据/依赖/版本）的分层排障路径
- [HybridCLR 崩溃定位专题｜从 native crash 调用栈读出 HybridCLR 的层次]({{< relref "engine-toolchain/hybridclr-crash-analysis.md" >}}) — hybridclr::、AOT 泛型缺失、MethodBridge 缺失、metadata 不匹配各自的特征
- [HybridCLR 真实案例诊断｜TypeLoadException 到 async 栈溢出]({{< relref "engine-toolchain/hybridclr-case-typeload-and-async-native-crash.md" >}}) — 一次完整的 native crash 符号化分析
