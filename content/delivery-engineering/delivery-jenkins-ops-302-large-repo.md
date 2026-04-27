---
date: "2026-04-27"
title: "大仓库在 Jenkins 下的 Workspace 策略"
description: 'Unity 仓库 50-200 GB 是常态，Git LFS / 浅克隆 / 缓存仓 / 镜像加速各有适用边界。本篇拆解四种工具的取舍，给出一个真实大仓的组合方案——把 checkout 时间从 40 分钟压到 3 分钟。'
slug: "delivery-jenkins-ops-302-large-repo"
weight: 1583
featured: false
tags:
  - "Delivery Engineering"
  - "CI/CD"
  - "Jenkins"
  - "Git"
  - "Large Repo"
series: "游戏团队 Jenkins 实战"
series_id: "delivery-jenkins-ops"
series_role: "article"
series_order: 130
delivery_layer: "practice"
delivery_volume: "V16"
delivery_parent_series: "delivery-cicd-pipeline"
delivery_reading_lines:
  - "L1"
  - "L2"
---

## 在本篇你会读到

- **几十 GB 仓库的 git 操作物理代价** —— checkout / clone / fetch 时间分布
- **Git LFS** —— 适合什么、不适合什么
- **浅克隆与部分克隆** —— 历史压缩的取舍
- **缓存仓（reference repo）** —— Agent 上的"本地 git mirror"
- **CDN 镜像与边缘加速** —— 跨地域团队必备
- **真实组合方案** —— 40 分钟 → 3 分钟的演进路径

---

## 几十 GB 仓库的 git 操作物理代价

游戏团队 Unity 仓库的典型构成：

```
.git/                  10-30 GB    # git 历史 + LFS pointer
Assets/                30-150 GB    # 美术资源（贴图、模型、动画）
ProjectSettings/       几 MB
Packages/              几百 MB
Library/               不进 git
```

仓库总大小常态 **50-200 GB**。这个量级下，git 操作的成本：

| 操作 | 仓库 1 GB | 仓库 50 GB | 仓库 200 GB |
|------|---------|---------|-----------|
| 全量 clone（首次） | 30 秒 | 15-30 分钟 | 1-2 小时 |
| fetch（增量） | 几秒 | 1-3 分钟 | 5-15 分钟 |
| checkout（切分支） | 几秒 | 30 秒-3 分钟 | 5-30 分钟 |
| status | 几毫秒 | 几秒 | 30 秒以上 |

CI 上每条 build 流水线开头的 "checkout SCM" 阶段，**没有优化的话能占整个 build 时长的 30%**。

### 物理成本拆解

慢在哪？

- **网络传输**：50 GB 在 100Mbps 网络下纯传输 67 分钟
- **磁盘 I/O**：解压 + 写入几十 GB 文件，磁盘是瓶颈
- **Git 索引计算**：几十万文件的 hash 校验需要 CPU
- **LFS pull**：如果用了 LFS，每个 pointer 都要单独下载

减时间不能只优化一个维度——四个维度都要看。

---

## Git LFS：适合什么、不适合什么

Git LFS（Large File Storage）把大文件从 git 历史中分离，本体存储在 LFS 服务器，git 仓库里只放 pointer。

### LFS 适合

- **大二进制文件**（贴图、模型、视频）—— 不需要 diff
- **历史中很少修改的文件**（avatar、UI 素材）
- **明确分类的目录**（`Assets/Textures/`、`Assets/Models/`）

### LFS 不适合

- **频繁修改的小二进制**（小 prefab）—— LFS pull 开销 > 节省
- **文本文件**（.cs、.shader、.json）—— git 自己处理就好
- **YAML / Unity Scene**（virtually-text 文件）—— 哪怕大也不该用 LFS

### LFS 在 Jenkins 上的陷阱

#### 陷阱 1：Agent 没装 git-lfs

git 默认不知道 LFS，需要单独装：

```bash
sudo apt install git-lfs
git lfs install --system   # 系统级初始化
```

新 Agent 上线没装 → checkout 看起来成功，但 LFS 文件全是 pointer 文本，build 失败。

#### 陷阱 2：LFS 和浅克隆冲突

LFS pull 默认拉所有 LFS 历史。和"只要最新"冲突——你以为浅克隆省了空间，结果 LFS 把空间又拉回来了。

修复：

```bash
# 浅克隆 + LFS 也只拉最新
GIT_LFS_SKIP_SMUDGE=1 git clone --depth 1 ...
git lfs pull --include="Assets/Textures/" --exclude=""
```

#### 陷阱 3：LFS server 带宽

LFS 服务器通常是 GitHub Enterprise / Bitbucket / GitLab 自带，带宽不一定大。100 个 Agent 同时拉 LFS → server 压垮。需要在公司内网搭 LFS 镜像，详见后文。

---

## 浅克隆与部分克隆

### 浅克隆（shallow clone）

```bash
git clone --depth 1 <url>
```

只拉最新一次提交的 snapshot，不要历史。

**优点**：clone 速度从 30 分钟降到 1-3 分钟（仓库历史 10 GB 时）。

**代价**：

- 不能 `git log` 看历史
- 不能 `git blame`
- 不能切到旧分支（如果旧 commit 不在浅克隆范围内）

**Jenkins 配置**：

```groovy
checkout([
    $class: 'GitSCM',
    branches: [[name: '*/main']],
    extensions: [[
        $class: 'CloneOption',
        shallow: true,
        depth: 1
    ]],
    userRemoteConfigs: [[url: 'git@...']]
])
```

### 部分克隆（partial clone，git 2.19+）

```bash
git clone --filter=blob:none <url>     # 不下载 blob
git clone --filter=tree:0 <url>        # 不下载 tree
```

只拉 commit metadata + tree，blob 按需懒加载。

**优点**：能保留全历史，但 clone 速度接近浅克隆。

**代价**：

- 需要 server 端支持 partial clone（GitHub / GitLab 较新版本支持）
- 第一次访问某个文件时还是要拉

游戏团队**推荐 partial clone 而不是 shallow clone**——能保留 `git log` 等基本能力，只对 blob 优化。

### Sparse Checkout（稀疏检出）

只检出仓库的子目录：

```bash
git clone --filter=blob:none --no-checkout <url>
git sparse-checkout init --cone
git sparse-checkout set Assets/Code Packages
git checkout main
```

适合**不需要全部资源**的 build：比如服务端代码 build 不需要美术资源。Jenkins 多产品仓库（mono-repo）场景特别有效。

---

## 缓存仓（Reference Repository）

Jenkins 文档里推荐的"git advanced clone"功能，本质是**Agent 上维护一个本地 git mirror，每次 clone 都从 mirror 加速**。

### 工作原理

```
Agent 上：
/data/git-cache/myproject.git/   # mirror（裸仓库，定时 fetch）

每次 build：
git clone --reference /data/git-cache/myproject.git \
          git@server:myproject.git workspace/
```

`--reference` 让 git 优先从本地 mirror 取 object，只有 mirror 里没有的才从远端拉。

### 实际效果

| 场景 | 无 reference | 用 reference |
|------|-------------|-------------|
| 首次 clone | 30 分钟 | 5 分钟（只拉新增 object） |
| 增量 fetch | 3 分钟 | 30 秒 |

### 维护

mirror 需要定时更新：

```bash
# /etc/cron.d/git-cache-update
*/30 * * * * jenkins cd /data/git-cache/myproject.git && git fetch --all
```

每 30 分钟拉一次，让 mirror 保持新鲜。

### Jenkins 配置

```groovy
checkout([
    $class: 'GitSCM',
    branches: [[name: '*/main']],
    extensions: [[
        $class: 'CloneOption',
        reference: '/data/git-cache/myproject.git',
        shallow: false
    ]],
    userRemoteConfigs: [[url: 'git@server:myproject.git']]
])
```

---

## CDN 镜像与边缘加速

跨地域团队（国内开发 + 海外发版）的特殊问题：git server 在一个地域，跨地域 clone 慢。

### 方案 A：自建 git mirror（区域镜像）

在每个地域部署一个 git mirror：

```
[Primary Git Server, US]
    ↓ 实时同步
[China Mirror, CN]
[EU Mirror, EU]
```

中国 Agent 走 China Mirror，欧洲 Agent 走 EU Mirror。Push 仍然走 Primary。

实现：用 [Gitaly](https://gitlab.com/gitlab-org/gitaly) 或 [Gerrit](https://www.gerritcodereview.com/) 的多 master 复制功能。

### 方案 B：Git LFS Cache Proxy

LFS 大文件单独走 CDN：

```
[LFS Origin, US]
    ↓
[CloudFront / Aliyun CDN]
    ↓
[Agents in CN]
```

实现：[lfs-folderstore](https://github.com/sinbad/lfs-folderstore) 之类的代理。

### 方案 C：包到云盘 / 对象存储

某些极特殊场景（巨型仓库 + 超大资源），git 不再适合。

- Source code → git
- Large assets → 对象存储（OSS / S3），CI 拉的时候 sync

这个方案改动大，是"最后的手段"。

---

## 真实组合方案：40 分钟 → 3 分钟

某游戏团队 80 GB 仓库的优化时间线：

### 起点：纯 git clone（40 分钟）

最早的 Pipeline：

```groovy
checkout scm   // Jenkins 默认全量 clone
```

每条 build 开头 40 分钟 git clone。一天 50 条 build = 33 小时纯 git 开销。

### 阶段 1：浅克隆（15 分钟）

```groovy
checkout([
    $class: 'GitSCM',
    extensions: [[$class: 'CloneOption', shallow: true, depth: 1]]
])
```

降到 15 分钟。但失去了 `git log` 和 `git blame`，开发者抱怨。

### 阶段 2：浅克隆 + LFS 优化（10 分钟）

加 LFS sparse pull：

```groovy
sh 'GIT_LFS_SKIP_SMUDGE=1 git clone --depth 1 ...'
sh 'git lfs pull --include="Assets/Textures/Build/" --exclude=""'  // 只拉 build 用的
```

降到 10 分钟。但浅克隆的限制还在。

### 阶段 3：部分克隆 + 缓存仓（4 分钟）

放弃浅克隆，改用 partial clone + reference repo：

```groovy
checkout([
    $class: 'GitSCM',
    extensions: [
        [$class: 'CloneOption',
         reference: '/data/git-cache/myproject.git',
         shallow: false,
         honorRefspec: true],
    ]
])
```

mirror 在每个 Agent 上每 30 分钟更新。

降到 4 分钟。能保留全部 git 能力。

### 阶段 4：分平台 sparse checkout（3 分钟）

iOS build 不需要 Android-only 资源，用 sparse checkout 排除：

```groovy
sh 'git sparse-checkout set Assets/Code Assets/Common Assets/iOS Packages'
```

降到 3 分钟。**总体时长从 40 分钟 → 3 分钟，节省 92%**。

### 总结

四种工具不是互斥的，是**组合使用**的：

```
缓存仓        ← 治理 git history + blob
+ 部分克隆    ← 减少 blob 拉取
+ Sparse      ← 减少需要的目录
+ LFS sparse  ← 减少 LFS 资源
```

每个团队按自己仓库结构调整组合。

---

## 文末导读

下一步进 303 符号表与崩溃栈：IL2CPP 产物的符号链路——大仓库 checkout 优化后，下一个游戏团队特化是符号表归档。

L3 面试官线读者：本篇核心是"40 分钟 → 3 分钟"的演进——大仓库优化不是单一技术，是分层组合。每一层各解决 30% 的问题，叠加起来才有数量级降本。
