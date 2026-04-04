---
title: "CDN 与资源分发：热更新包、Asset Bundle 的分发策略"
slug: "game-backend-infra-04-cdn-and-delivery"
date: "2026-04-04"
description: "游戏资源包通过 CDN 分发，为什么还是经常出现版本混乱和更新失败——问题不在 CDN 本身，在于 URL 设计和缓存策略的错误组合。"
tags:
  - "游戏后端"
  - "CDN"
  - "热更新"
  - "Asset Bundle"
  - "资源分发"
series: "游戏后端基础"
primary_series: "game-backend"
series_role: "article"
series_order: 13
weight: 3013
---

## 问题：CDN 用上了，为什么更新还是乱的

一个手游项目每次热更新后，总有玩家反映"更新完了但还是旧版本"。客服排查了几十个案例，发现问题不一样：

- 一部分玩家下载了新版本的 manifest 文件，但资源包还是旧的
- 一部分玩家下载失败，但客户端判定"已是最新版本"没有重试
- 少数玩家成功下载了新资源，但游戏崩溃了，因为新 Bundle 依赖的 Shader 还在旧版本

这三类问题指向的是三个不同层面的设计缺陷，但它们有一个共同的根源：**开发团队把 CDN 当成了一个"透明的文件服务器"，而没有理解它的缓存行为对资源版本控制的影响。**

CDN 的本质工作是**缓存并就近提供内容**。这个机制在分发静态图片和 JS 文件时近乎完美，但游戏资源分发有三个特殊性：文件大（单个 Bundle 几十到几百 MB）、版本强依赖（Bundle 之间有依赖图）、需要增量更新（不能每次全量下载）。这三点叠加，要求我们对 CDN 的缓存策略有精确的控制，而不是依赖默认行为。

---

## 抽象模型：CDN 是怎么工作的

### 基本架构：边缘节点与回源

CDN（Content Delivery Network）的核心结构：

```
客户端
  │
  ▼
边缘节点（Edge Node / PoP）← 全球分布，地理上靠近用户
  │ 命中缓存
  ├──────────────────────── 直接返回缓存内容 → 客户端
  │ 未命中（Cache Miss）
  ▼
源站（Origin Server）← 你的服务器 or 对象存储（OSS/S3）
  │
  ▼ 拉取内容，缓存到边缘节点
  └──────────────────────── 返回内容 → 客户端
```

**边缘节点**：分布在全球各地的 CDN 服务器，负责缓存内容并就近响应请求。国内典型的 CDN 服务商（阿里云 CDN、腾讯云 CDN、网宿）在各省有数十个 PoP（Point of Presence）节点。

**回源（Origin Fetch）**：当边缘节点没有缓存某个文件（Cache Miss），它会向源站发起请求，获取内容后缓存本地，再返回给客户端。

**缓存策略**：边缘节点缓存内容多久，由 HTTP 响应头控制（`Cache-Control`、`Expires`）。这是版本控制问题的核心所在。

### 两种失效机制

**1. TTL 过期（Time-To-Live）**

源站在响应头里设置 `Cache-Control: max-age=86400`，边缘节点缓存 24 小时后自动失效，下次请求重新回源。

问题：在 TTL 期间内，即使源站的文件已经更新，边缘节点仍然提供旧内容。

**2. 主动刷新（Cache Purge / Invalidation）**

CDN 控制台或 API 主动通知边缘节点"这个 URL 的缓存已失效，下次请求需要重新回源"。

问题：刷新是按 URL 进行的。如果你的资源 URL 没有版本信息（比如固定的 `main.bundle`），刷新后下一次请求虽然会回源拿到新内容，但如果玩家在刷新前就缓存了旧版本到本地（浏览器缓存或游戏客户端本地存储），CDN 刷新对他们无效。

---

## 核心问题：URL 设计决定了版本控制的上限

这是整个 CDN 资源分发最重要的设计决策，也是最容易犯错的地方。

### 错误的做法：URL 不包含版本信息

```
https://cdn.example.com/assets/hero_skin_001.bundle
https://cdn.example.com/assets/main.manifest
```

这种 URL 设计的后果：
- `main.manifest` 更新了，但 CDN 边缘节点还在缓存旧版本（TTL 没过）
- 即使你主动刷新 CDN，玩家客户端本地磁盘上已经下载的旧文件不会更新
- `hero_skin_001.bundle` 内容变了，但 URL 没变，客户端缓存逻辑无法知道该不该重新下载

### 正确的做法：内容哈希进 URL（Immutable URL）

```
https://cdn.example.com/assets/hero_skin_001_a3f8c2d1.bundle
https://cdn.example.com/assets/main_v20260404_1842.manifest
```

核心原则：**文件内容一旦变化，URL 就必须改变。** 常见实现方式：

**内容哈希（Content Hash）**

在构建流程中，对每个 Bundle 文件计算 MD5 或 SHA256，将哈希值的前 8 位嵌入文件名：

```
hero_skin_001.bundle → hero_skin_001_a3f8c2d1.bundle
```

文件内容不变，哈希不变，URL 不变，CDN 缓存永久有效（`Cache-Control: max-age=31536000, immutable`）。
文件内容变化，哈希变化，URL 变化，客户端必须重新下载，没有任何歧义。

**版本号或构建时间戳**

```
main.manifest → main_v20260404_1842.manifest
```

Manifest 文件（记录所有 Bundle 的版本对应关系）用构建版本号命名，每次热更新生成新文件名。客户端从一个固定的"版本索引"接口获取当前版本的 manifest URL，再去 CDN 拉取。

### 版本索引接口：唯一需要不走缓存的请求

在 Immutable URL 策略下，所有资源文件都可以无限期缓存。唯一需要动态获取的是"当前版本的 manifest 在哪里"这个信息，通常通过一个轻量的版本检查接口提供：

```
GET https://api.example.com/version/check?channel=android&build=1024
Response: {
  "manifest_url": "https://cdn.example.com/assets/main_v20260404_1842.manifest",
  "patch_size_mb": 12.3,
  "force_update": false
}
```

这个接口走业务服务器，不走 CDN 缓存（`Cache-Control: no-store`），保证客户端永远拿到最新的版本信息。

---

## 具体实现：Unity Asset Bundle 的 CDN 分发策略

### Manifest 文件的特殊性

Unity AssetBundle 构建后会生成一个主 Manifest 文件（`AssetBundles.manifest` 或 `AssetBundles`），记录所有 Bundle 的名称、哈希、依赖关系。这个文件是热更新的核心索引。

**错误做法**：把 Manifest 文件也放在 CDN 上，使用固定 URL，依赖 Cache-Control 过期或 CDN 刷新来更新。

**正确做法**：

1. 构建系统生成 Bundle 后，计算每个 Bundle 的内容哈希
2. 以哈希为文件名上传到 CDN（或 OSS/S3，再通过 CDN 分发）
3. 生成自定义的版本清单（version manifest），记录：
   ```json
   {
     "version": "1.2.3",
     "build": 1024,
     "bundles": [
       { "name": "hero_skin_001", "hash": "a3f8c2d1", "size": 2048576, "deps": ["shared_textures"] },
       { "name": "shared_textures", "hash": "b9e4f7a2", "size": 8192000, "deps": [] }
     ]
   }
   ```
4. 版本清单本身也用构建版本号命名（`manifest_1024.json`），上传到 CDN
5. 版本检查接口只返回当前 manifest 的 URL，不直接返回所有 bundle 信息

### 增量更新的实现

全量更新（每次下载所有 Bundle）对玩家极不友好。增量更新的逻辑：

1. 客户端持有当前版本的 bundle 哈希记录（本地存一份 version manifest）
2. 拉取新版 manifest 后，对比每个 bundle 的哈希
3. 哈希不变 → 文件未变，无需下载（即使 URL 中的哈希变了，意味着这是个新版本引用了老资源，直接使用本地缓存）
4. 哈希变化 → 文件已更新，需要重新下载

这里有个细节：客户端本地存储 bundle 文件时，应该以哈希为键（文件名包含哈希），而不是以 bundle 名为键。这样新旧版本的文件可以共存，下载失败时旧版本仍然可用，不需要回滚操作。

### Bundle 依赖图与下载顺序

Unity AssetBundle 有依赖关系（如 `hero_skin_001` 依赖 `shared_textures`）。热更新时必须先验证依赖是否满足，否则会出现"新 Bundle 依赖的旧 Bundle 接口已变化"的崩溃。

安全的更新顺序：
1. 先下载所有被依赖的基础 Bundle（叶节点）
2. 再下载依赖它们的 Bundle
3. 最后更新 manifest

这样即使更新中途断开，仍处于一致状态（新文件还没被 manifest 引用，旧 manifest 指向的老文件还在本地）。

---

## 具体实现：Unreal PAK 的分发差异

Unreal 的热更新机制使用 PAK 文件（`.pak`），原理与 AssetBundle 类似，但有几个关键差异：

**PAK 文件是完整包，不是 Bundle 图**

一个 PAK 文件可能包含大量资源，不像 AssetBundle 可以细粒度地按资源类型打包。这意味着增量更新的粒度更粗——一个 PAK 内只要有一个资源改变，整个 PAK 需要重新下载。

优化方向：设计合理的 PAK 分包策略，把高频更新的内容（活动 UI、节日皮肤）单独放在小 PAK 里，稳定内容（基础 Gameplay 资源）放在大 PAK 里，降低热更新的下载量。

**签名验证**

Unreal PAK 支持加密和签名。CDN 分发时，PAK 文件在服务器端加密，客户端下载后用内置的 key 解密验证。这个机制要求密钥管理流程安全，不能在客户端代码中硬编码。

---

## 回滚与灰度发布

### 回滚：不删文件，只更新版本指针

基于 Immutable URL 策略的一个重要优势：**回滚只需要更新版本检查接口返回的 manifest URL**。

旧版本的所有 Bundle 文件依然在 CDN 上，只要把版本检查接口指回旧版本的 manifest，玩家客户端就会重新下载到旧版本（已有的旧文件缓存命中，实际下载量为零）。

回滚步骤：
1. 版本检查接口配置回滚（把 `manifest_url` 改为上一个版本）
2. 客户端下次检查版本时获得旧 manifest URL
3. 对比本地缓存，发现需要"降级"到旧版本文件
4. 下载旧版本文件（大概率命中本地缓存，如果本地没清理的话）

### 灰度发布（Canary Release）

灰度发布在 CDN 层面无法直接实现（CDN 不感知用户身份），需要在版本检查接口层处理：

```
GET /version/check?channel=android&build=1024&uid=123456789

后端逻辑：
- 计算 uid 的哈希值 % 100
- 如果结果 < 10（10% 灰度）→ 返回新版 manifest URL
- 否则 → 返回稳定版 manifest URL
```

这样 CDN 上同时存在新旧两个版本的资源（都是 Immutable URL，不会冲突），灰度比例的调整只需要改后端配置，不需要操作 CDN。

### CDN 主动刷新的正确使用场景

有了 Immutable URL 策略，**几乎不需要主动刷新 CDN 缓存**。但以下场景仍需要：

1. **紧急安全漏洞**：发现某个资源包包含违规内容或安全漏洞，必须立即失效，不能等 TTL。此时通过 CDN API 批量刷新相关 URL。
2. **Manifest 文件**（如果你选择了固定 URL 的设计）：但正确做法是用版本号避免这种情况。
3. **版本检查接口**：如果版本检查接口前面加了 CDN 缓存（不推荐，但某些场景下为了减少源站压力会这样做），需要在发布后立刻刷新。

---

## 工程边界：CDN 不能解决什么

**CDN 无法保证下载原子性**

玩家下载到一半断网，本地是半个 Bundle。客户端需要自己实现断点续传和文件完整性校验（下载完成后对比服务器提供的 MD5/SHA256）。

**CDN 无法处理版本之间的逻辑兼容性**

CDN 只负责分发文件，不知道"新 Shader 和旧 Bundle 组合会崩溃"。这是热更新系统在客户端逻辑层需要解决的问题：版本兼容矩阵、最低基础包版本要求、强制更新机制。

**CDN 无法替代包体优化**

首包大小是另一个维度的问题。CDN 加速的是已经在分发的内容，但如果初始包体过大导致转化率低，CDN 帮不上忙。首包大小的优化需要在资源打包策略和按需加载（On-Demand Loading）层面解决。

**全球分发的监管合规**

在某些地区（尤其是中国内地），CDN 服务商需要有 ICP 备案，HTTPS 证书需要在境内 CDN 节点上配置。游戏资源包中如果包含用户数据（如存档），还涉及数据本地化要求。这些是合规问题，不是技术问题，但 CDN 选型时必须考虑。

---

## 最短结论

游戏资源分发的版本混乱，根源几乎都是 URL 不包含内容哈希——只要把文件内容哈希嵌入 URL，CDN 缓存就从"可能过期的障碍"变成"永久有效的加速器"，热更新、回滚、灰度发布的复杂度都会大幅下降。
