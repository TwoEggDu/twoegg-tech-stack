---
title: "包体管理与分发 06｜CDN 部署与版本管理——发布、回滚与缓存一致性"
slug: "delivery-package-distribution-06-cdn"
date: "2026-04-14"
description: "CDN 不是'把文件传上去就完了'。部署顺序、缓存策略、版本回滚、多区域同步——这些工程细节决定了热更新是否可靠。"
tags:
  - "Delivery Engineering"
  - "Package Management"
  - "CDN"
  - "Deployment"
series: "包体管理与分发"
primary_series: "delivery-package-distribution"
series_role: "article"
series_order: 60
weight: 560
delivery_layer: "principle"
delivery_volume: "V06"
delivery_reading_lines:
  - "L1"
  - "L2"
---

## 这篇解决什么问题

资源文件和热更新包通常托管在 CDN 上。CDN 的部署和版本管理看似简单（"上传文件到服务器"），但实际有大量工程细节——处理不好就是热更新事故的高发区。

## 本质是什么

CDN 部署的核心挑战是**缓存一致性**：你上传了新文件，但用户的请求可能仍然被 CDN 边缘节点返回旧文件。

### 部署顺序：先内容后索引

```
正确顺序：
1. 上传所有 Bundle 文件到 CDN
2. 等待所有 Bundle 在 CDN 上可用
3. 上传 Manifest / Catalog（索引文件）

错误顺序：
1. 同时上传 Bundle 和 Manifest
→ 客户端可能拿到新 Manifest 但 Bundle 还没部署完
→ 下载 404
```

**索引文件是最后上传的**——因为客户端通过索引文件判断"需要下载什么"。索引文件在所有 Bundle 就位后才发布，保证客户端请求到的 Bundle 一定存在。

### 文件命名：内容寻址

Bundle 文件名应该包含内容哈希：

```
✗ bundle_characters.bundle          (每次更新覆盖同名文件)
✓ bundle_characters_a1b2c3d4.bundle (新版本是新文件名)
```

内容寻址的优势：
- **CDN 缓存友好**：新版本和旧版本是不同的 URL，不存在缓存覆盖问题
- **自然支持回滚**：回滚时只需要把索引文件指回旧版本的文件名，旧文件还在 CDN 上
- **增量部署**：未变化的 Bundle 保持原 URL，CDN 缓存继续生效

### 缓存控制

| 文件类型 | 缓存策略 | 理由 |
|---------|---------|------|
| Manifest / Catalog | `Cache-Control: no-cache` 或短 TTL（1 分钟） | 必须每次拿最新版本 |
| Bundle（含 hash 文件名） | `Cache-Control: max-age=31536000` (1 年) | 文件名变了就是新 URL |
| 配置文件 | `Cache-Control: no-cache` 或短 TTL | 热推配置必须实时 |

**Manifest 绝不能被长时间缓存**。如果 Manifest 被 CDN 缓存了 24 小时，热更新推送后用户要等 24 小时才能拿到新版本。

### 多区域同步

全球发行的项目使用多区域 CDN。部署时必须确保所有区域都同步完成后再发布索引文件：

```
1. 上传 Bundle 到所有 CDN 区域
2. 验证所有区域的 Bundle 可访问（对每个区域发 HTTP HEAD 请求确认）
3. 所有区域确认后，上传 Manifest
```

如果某个区域同步延迟，该区域的用户会拿到新 Manifest 但下载 Bundle 时 404。

### 版本回滚

CDN 上的版本回滚不是"删除新文件"——因为内容寻址下新旧文件并存。回滚是**把索引文件指回旧版本**：

```
回滚前：
  Manifest v2 → 指向 bundle_xxx_newHash.bundle

回滚后：
  Manifest v1 → 指向 bundle_xxx_oldHash.bundle
  （新 Bundle 文件仍然保留在 CDN 上，不删除）
```

回滚的前置条件：
- 旧版本的 Manifest 和 Bundle 文件仍然在 CDN 上（不能在部署新版本时删除旧文件）
- 回滚操作是预设的脚本或 CI 步骤（不是手动到 CDN 控制台操作）
- 回滚后必须验证（拉取回滚后的 Manifest，确认指向正确的 Bundle）

### 清理策略

随着版本迭代，CDN 上会积累大量旧版本的 Bundle 文件。清理策略：

- 保留最近 N 个版本的所有文件（N 通常为 3-5）
- 超过 N 个版本的文件可以安全删除
- 清理前确认没有存量客户端仍在使用旧版本（通过版本分布数据判断）

## 部署自动化

CDN 部署应该完全自动化，作为 CI/CD 管线的一部分：

```
CI 构建 → 产出 Bundle + Manifest
  → 上传 Bundle 到 CDN（并行上传，有重试）
  → 等待所有区域同步
  → 验证 Bundle 可访问
  → 上传 Manifest
  → 验证 Manifest 返回最新版本
  → 通知相关人员"热更新已部署"
```

每一步都有超时和重试机制。任何一步失败都终止部署，不会出现"上传了一半"的中间状态。

## 常见事故与排障

**事故：热更新后部分用户加载失败**。CDN 某个区域的同步延迟了 10 分钟。在这 10 分钟内，该区域的用户拿到了新 Manifest 但下载 Bundle 时 404。

**预防**：部署流程中必须有"所有区域同步确认"步骤，确认后才发布 Manifest。

**事故：回滚失败——旧版本文件已被清理**。部署新版本时 CI 自动清理了上上个版本的文件。回滚到上上个版本时发现 Bundle 文件已不存在。

**预防**：清理策略保留最近 5 个版本。回滚操作前先验证目标版本的文件是否完整。

## 小结与检查清单

- [ ] 部署顺序是否先 Bundle 后 Manifest
- [ ] Bundle 文件名是否包含内容哈希（内容寻址）
- [ ] Manifest 的缓存策略是否为 no-cache 或短 TTL
- [ ] 多区域部署是否有同步确认步骤
- [ ] 回滚操作是否预设为脚本（不是手动操作）
- [ ] 旧版本文件是否保留（至少最近 3-5 个版本）
- [ ] CDN 部署是否完全自动化（CI/CD 管线的一部分）

---

**下一步应读**：[热更新资源管线]({{< relref "delivery-engineering/delivery-package-distribution-07-hotupdate-resources.md" >}}) — 从 CDN 部署到客户端的完整热更新流程

**扩展阅读**：[案例：一次热更新上线事故]({{< relref "projects/case-hotupdate-production-incident.md" >}}) — CDN 缓存一致性问题导致的真实事故
