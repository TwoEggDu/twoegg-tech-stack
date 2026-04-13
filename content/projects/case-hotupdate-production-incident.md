---
title: "案例：一次热更新上线事故的复盘 — 从 CDN 缓存到 AB 依赖断裂"
description: "一次热更新发布后，20% 的玩家卡在加载界面。表面原因是 CDN 缓存未刷新，真正根因是 AssetBundle 依赖链在增量构建时断裂。这篇从发现、定位、修复到防回归完整复盘。"
slug: "case-hotupdate-production-incident"
weight: 21
featured: false
tags:
  - "Projects"
  - "Incident"
  - "AssetBundle"
  - "HotUpdate"
  - "Postmortem"
---

## 一句话总结

热更新发布后 20% 用户卡加载，表面是 CDN 缓存未刷新，真正根因是增量构建时 CI 缓存策略清掉了上次构建产物，导致 shared bundle 的 manifest hash 断裂，老用户本地缓存的旧 bundle 和新 manifest 对不上。

---

## 事故时间线

这是一次周四下午的常规热更新发布。版本内容不多，主要是几个 UI 界面的调整和一批配表更新。按以往的经验，这种小版本基本不会出问题。

| 时间 | 事件 |
|------|------|
| T+0 (14:30) | 热更新包通过 Jenkins 构建完成，推送至 CDN，后台下发更新通知 |
| T+15min | 客服群开始收到玩家反馈："更新完进不去游戏" |
| T+25min | QA 在测试设备上复现失败——因为测试设备是当天新装的包 |
| T+30min | 运维拉出实时日志，确认约 20% 活跃用户卡在资源加载界面 |
| T+45min | 我拿了一台有上个版本缓存的设备，复现成功 |
| T+1h | 初步判断 CDN 边缘节点返回了旧文件，提交 CDN 刷新工单 |
| T+1h30min | CDN 全量刷新完成，大部分用户恢复 |
| T+2h | 仍有约 5% 用户持续异常，日志报 `AssetBundle.LoadAsset` 返回 null |
| T+3h | 开始怀疑不是单纯的 CDN 问题，转向对比构建产物 |
| T+4h | 定位到 shared_ui bundle 的依赖链断裂 |
| T+4h30min | 决定回滚到上一个稳定版本 |
| T+5h | 回滚完成，同时触发全量（非增量）重新构建 |
| T+6h | 新构建完成，内部验证通过 |
| T+7h | 重新发布热更新 |
| T+8h | 全量恢复，异常率降到 0.1% 以下 |

整个过程从发布到全量恢复用了大约八小时。其中真正浪费时间的是中间那两个小时——我一度以为 CDN 刷新就能解决全部问题。

---

## 表面现象：卡在加载界面

玩家看到的是更新完成后，进入游戏时卡在加载界面，进度条走到大约 60% 就不动了。

客户端日志里能看到的关键信息：

```
[AssetBundleLoader] Loading bundle: shared_ui.bundle
[AssetBundleLoader] Bundle loaded, hash: a3f7c2e1
[AssetBundleLoader] LoadAsset<GameObject>("UIPanel_Shop") returned null
[AssetBundleLoader] ERROR: Asset missing in bundle. Expected path: assets/ui/panels/uipanel_shop.prefab
[ResourceManager] TIMEOUT: Resource loading exceeded 30s threshold, stuck assets: [shared_ui/UIPanel_Shop, shared_ui/UIPanel_Mail]
```

`LoadAsset` 返回 null，但 bundle 本身加载成功了——说明 bundle 文件拿到了，但里面找不到预期的 asset。

几个关键观察：

- 只影响**有旧版本缓存的老用户**。当天新安装的用户、清过缓存的用户都不受影响。
- 不是所有 bundle 都出问题，只有依赖 `shared_ui` 的 bundle 报错。
- 出问题的用户比例（约 20%）和上一个版本热更新的到达率基本吻合——也就是说，恰好是那些"上次热更成功、这次又热更"的用户。

当时我心里第一反应是：CDN 缓存。这个判断不算错，但只对了一半。

---

## 第一层定位：CDN 缓存未刷新

我们的热更新流程是这样的：

1. Jenkins 构建新的 AB 包，生成 `version.json`（包含每个 bundle 的 hash）
2. 客户端启动时拉 `version.json`，和本地缓存的 hash 做对比
3. hash 变了的 bundle 从 CDN 下载新版本
4. 下载完成后替换本地缓存，进入加载流程

问题出在第 3 步。`version.json` 已经更新了（因为它不走 CDN 缓存，直接从源站拉），但 CDN 边缘节点上的 bundle 文件还是旧的。

客户端拿到新的 `version.json`，发现 `shared_ui.bundle` 的 hash 变了，去 CDN 下载，但 CDN 返回的还是旧文件。客户端收到文件后做 hash 校验——这里有个坑：**我们当时的校验逻辑是校验文件大小而不是内容 hash**。旧文件和新文件大小恰好差不多（差了几十字节，在允许的误差范围内），校验就过了。

提交 CDN 全量刷新后，大部分用户能下载到正确的新文件，问题恢复。

但我当时犯了一个错误：看到大部分用户恢复后，以为剩下的 5% 只是 CDN 刷新还没完全生效的长尾。就和运维说"再等等看"。

等了半小时，5% 没有任何变化。

---

## 第二层定位：AB 依赖链断裂

重新看那 5% 用户的日志，发现和之前的 CDN 问题不一样。这些用户已经下载到了正确的新 bundle（hash 校验通过），但加载还是失败。

关键日志：

```
[AssetBundleLoader] Loading bundle: shared_ui.bundle (hash: b8e4d1f9) -- OK
[AssetBundleLoader] Loading dependency: shared_textures.bundle (from cache, hash: 7c2a9e3d)
[AssetBundleLoader] LoadAsset<Sprite>("icon_shop_gem") from shared_textures -- returned null
[AssetBundleLoader] WARNING: Dependency bundle hash mismatch. Manifest expects: 7c2a9e3d, actual loaded: 5f1b8c4a
```

这条日志说的是：新的 `shared_ui.bundle` 声明依赖 `shared_textures.bundle`（hash `7c2a9e3d`），客户端去加载这个依赖时，发现本地缓存里有一个 `shared_textures.bundle`，但它的实际 hash 是 `5f1b8c4a`——这是上个版本的 hash。

问题链条还原：

1. 这次增量构建时，`shared_textures.bundle` 的内容实际上没有变化
2. 但构建系统重新生成了它的 manifest 条目，给了它一个新的 hash（`7c2a9e3d`）
3. 新的 `shared_ui.bundle` 的 manifest 里记录的依赖 hash 是 `7c2a9e3d`
4. `version.json` 里 `shared_textures.bundle` 的 hash 也是 `7c2a9e3d`
5. CDN 上确实有 hash 为 `7c2a9e3d` 的 `shared_textures.bundle`（内容和旧的一样，但文件头信息不同）
6. 但客户端本地缓存里存的是旧版本（hash `5f1b8c4a`），客户端对比 `version.json` 发现 hash 变了，理应去下载新版本
7. 问题是——CDN 刷新前，这些用户下载到的是旧文件；CDN 刷新后，客户端认为"我已经下载过了"（因为下载记录还在），就没有重新下载

**根因：客户端的下载状态记录没有在 CDN 刷新后被重置。** 它记住了"我已经尝试下载过 `shared_textures.bundle` 的新版本"，但实际拿到的是旧文件。

这就是为什么"CDN 刷新后新装的用户没问题，但之前尝试过下载的用户还是不行"。

---

## 为什么增量构建会断裂

找到直接原因后，我花了一天时间去排查为什么增量构建会产出有问题的 manifest。

Unity 的增量 AB 构建依赖一个前提：**上一次构建的完整产物必须存在于 OutputPath 中。** 构建系统会读取上次的 `.manifest` 文件来判断哪些 bundle 需要重新打包、哪些可以跳过。

```
OutputPath/
├── shared_ui.bundle
├── shared_ui.bundle.manifest
├── shared_textures.bundle
├── shared_textures.bundle.manifest
├── ... (其他 bundle)
├── OutputPath                    ← 主 manifest 文件
└── OutputPath.manifest           ← 主 manifest 的 manifest
```

如果这个目录是完整的，增量构建能正确判断：哪些 bundle 的源资源变了需要重建，哪些没变可以跳过。

但我们的 CI（Jenkins）出了一个问题。

排查 Jenkins 的构建日志，我发现出事的那次构建之前，有一个"清理磁盘空间"的定时任务跑过。这个定时任务的逻辑是：当构建机磁盘使用率超过 80%，删除超过 7 天未修改的构建产物。

```
# Jenkins 清理脚本（简化）
find /data/jenkins/workspace/*/AssetBundles/Android -mtime +7 -name "*.bundle" -delete
```

注意这个脚本只删了 `.bundle` 文件，没删 `.manifest` 文件。

结果就是：OutputPath 里 `.manifest` 文件都在，但部分 `.bundle` 文件被删了。

Unity 的 `BuildPipeline.BuildAssetBundles` 在增量模式下读到了完整的 `.manifest` 文件，认为那些 bundle 还存在，于是跳过了它们的重建。但实际上 bundle 文件已经不在了。

对于 `shared_textures.bundle` 这种"内容没变但文件被删了"的情况：

1. 构建系统看到 `.manifest` 还在，判断"不需要重建"
2. 但生成新的主 manifest 时，因为 bundle 文件不存在，重新计算了一个 hash
3. 这个新 hash 和原来的不一样（因为计算依据不同了）
4. 其他依赖 `shared_textures` 的 bundle 被重建时，记录了这个新 hash 作为依赖
5. 最终发布的 `shared_textures.bundle` 是从源站 fallback 拿的旧文件（我们有一个"如果 OutputPath 没有就从上次发布的备份拷贝"的逻辑），但它的 hash 和新 manifest 里记录的 hash 对不上

这就是增量构建 manifest hash 断裂的完整链路。

用一句话概括：**CI 的磁盘清理只删了 bundle 文件没删 manifest 文件，导致增量构建基于不完整的上下文生成了不一致的 manifest。**

---

## 修法一（治标）：回滚 + 全量重建

定位到根因后，立即执行的操作：

**第一步：回滚。**

```bash
# 在发布后台执行版本回滚
python3 hotupdate_publish.py rollback --version 2.4.1.1056 --target cdn_prod
# 输出：
# [Rollback] Restoring version.json to 2.4.1.1056
# [Rollback] CDN purge submitted for 347 files
# [Rollback] Rollback complete. Active version: 2.4.1.1056
```

回滚后，客户端拿到的 `version.json` 回到上一个稳定版本，所有 hash 和本地缓存一致，不会触发下载，用户可以正常进入游戏。

**第二步：全量重建。**

在 Jenkins 上手动触发构建，关键改动是加了 `FORCE_REBUILD` 参数：

```bash
# Jenkins 构建参数
FORCE_REBUILD=true
BUILD_TARGET=Android
BUNDLE_VERSION=2.4.1.1057

# 构建脚本里的处理
if [ "$FORCE_REBUILD" = "true" ]; then
    echo "[Build] Force rebuild: cleaning OutputPath"
    rm -rf "${OUTPUT_PATH}/*"
fi
```

全量重建意味着从零开始打所有 bundle，不依赖任何上次构建的产物。输出的 manifest 是完全一致的，不会有 hash 断裂的问题。

**第三步：内部验证。**

这次验证特意准备了两类设备：

- 全新安装的设备（验证基本流程）
- 保留了 2.4.1.1056 版本缓存的设备（验证增量更新路径）

两类设备都通过后，才重新发布。

---

## 修法二（治本）：构建链路防护

回滚止血后，接下来一周我做了四件事。

### 1. CI 构建前校验 OutputPath 完整性

在构建脚本的最前面加了一个校验步骤：

```csharp
// BuildPipeline 调用前的校验
public static bool ValidateOutputPath(string outputPath)
{
    var manifestFiles = Directory.GetFiles(outputPath, "*.manifest");
    var bundleFiles = Directory.GetFiles(outputPath, "*.bundle");

    var manifestNames = manifestFiles
        .Select(f => Path.GetFileNameWithoutExtension(f))
        .ToHashSet();
    var bundleNames = bundleFiles
        .Select(f => Path.GetFileNameWithoutExtension(f))
        .ToHashSet();

    // 每个 .manifest 必须有对应的 .bundle
    var orphanedManifests = manifestNames.Except(bundleNames).ToList();
    if (orphanedManifests.Count > 0)
    {
        Debug.LogError($"[BuildValidator] Orphaned manifests found (no matching bundle): " +
                       $"{string.Join(", ", orphanedManifests)}");
        Debug.LogError("[BuildValidator] OutputPath is inconsistent. " +
                       "Falling back to full rebuild.");
        return false;
    }

    return true;
}
```

校验失败时不是报错退出，而是自动降级为全量构建。因为在生产环境下，"构建成功但产物有问题"远比"构建失败"危险。

### 2. 增量构建前比对 manifest hash 连续性

逻辑是加载当前构建的主 manifest 和上次发布的主 manifest，通过 `GetAllAssetBundles()` 取出两边的 bundle 列表做差集，找出"上次有、这次突然消失"的 bundle。

这个检查的目的不是阻止变更，而是让构建日志里留下"和上次发布相比，哪些 bundle 消失了、哪些 hash 变了"的记录。出问题时可以直接翻日志。

### 3. 发布前自动化冒烟测试

这是我觉得最该早做的一件事。

在发布流程里增加一个"模拟老用户"的测试步骤。做法是：

1. 从上一个稳定版本的 CDN 备份拉取一套完整的 bundle 缓存
2. 把这套缓存放到测试设备上
3. 然后执行这次热更新
4. 验证更新后能否正常加载所有资源

```yaml
# Jenkins pipeline 片段
stage('Smoke Test - Cached User') {
    steps {
        sh '''
            # 下载上一版本的完整 bundle 作为模拟缓存
            python3 tools/download_version_snapshot.py \
                --version ${LAST_STABLE_VERSION} \
                --output /tmp/smoke_test_cache/

            # 启动模拟器，注入缓存
            python3 tools/device_test.py \
                --cache-dir /tmp/smoke_test_cache/ \
                --update-url ${CDN_STAGING_URL}/version.json \
                --check asset_load_all \
                --timeout 120
        '''
    }
}
```

这个测试如果当时就有，那次事故在发布前就会被拦住。

### 4. AB 依赖图 CI 可视化

每次构建完成后，遍历 `AssetBundleManifest` 的所有 bundle 和依赖关系，生成 DOT 格式的依赖图，输出到 Jenkins 的构建报告里。

可视化本身不能阻止问题，但它让"依赖关系发生了异常变化"这件事变得一眼就能看出来。在出事的那次构建里，如果有这张图，我会看到 `shared_textures` 的入边数量从 12 变成了 0——因为它的 hash 和其他 bundle 记录的依赖 hash 对不上了。

---

## 防回归措施

事后整理出的防回归清单：

### 构建环节

| 措施 | 触发时机 | 失败处理 |
|------|----------|----------|
| OutputPath 完整性校验 | 增量构建前 | 降级为全量构建 |
| Manifest hash 连续性比对 | 构建完成后 | 输出 warning 到构建日志 |
| CI 磁盘清理规则修正 | 定时清理 | 只清理完整的构建目录，不单独删文件 |

磁盘清理脚本从"按文件类型删"改成了"按构建目录整体删"：

```bash
# 修改前（导致不一致的根源）
find /data/jenkins/workspace/*/AssetBundles -mtime +7 -name "*.bundle" -delete

# 修改后（要删就删整个构建输出目录）
find /data/jenkins/workspace/*/AssetBundles -maxdepth 1 -mindepth 1 -type d -mtime +7 -exec rm -rf {} +
```

### 发布环节

| 措施 | 说明 |
|------|------|
| CDN 刷新纳入自动化流程 | 发布脚本自动触发 CDN purge，不再依赖手动提工单 |
| 缓存用户模拟测试 | 用上一版本的 bundle 缓存做增量更新冒烟测试 |
| 下载状态重置机制 | 客户端增加"强制重新校验"逻辑，CDN 刷新后可通过服务端指令触发 |

### 客户端补丁

客户端资源管理器增加了一个强制校验逻辑：

```csharp
// 服务端可通过配置下发强制校验指令
if (serverConfig.forceRevalidateCache)
{
    Debug.Log("[ResourceManager] Server requested cache revalidation");
    // 清除下载状态记录，强制重新比对所有 bundle 的 hash
    PlayerPrefs.DeleteKey("bundle_download_state");
    // 下次加载时会重新走 hash 比对 + 按需下载流程
}
```

这个机制保证了即使将来再出现类似问题，我们也可以从服务端推一个指令让客户端重新校验缓存，而不是让玩家手动清缓存。

---

## 反思

### 做对了的事

**快速回滚止血。** 定位到问题后没有试图在线上修，而是先回滚到上一个稳定版本。虽然回滚意味着这次热更新的内容要延迟上线，但比让 20% 的玩家持续卡在加载界面强太多。

**回滚后离线复现。** 回滚之后，我在本地搭了一套和线上一样的环境来复现问题。这比在生产环境上反复试要安全得多，而且可以随意调试。

### 做错了的事

**增量构建的 CI 缓存策略从没人审查过。** 那个磁盘清理脚本是运维同事很早之前写的，针对的是一般的构建产物。但 AB 增量构建对文件完整性有特殊要求，这个要求从来没有传达给运维，也没有人审查过清理策略是否兼容。

**发布前没有模拟老用户场景的测试。** QA 每次测试热更新，用的都是当天新装的包或者从干净状态开始的设备。这意味着"有旧版本缓存的用户"的更新路径从来没被测试过。这次事故发生后我回头想，这简直是一个必然会暴雷的盲区。

**CDN 刷新是手动的。** 发布流程到推 CDN 这一步就结束了，CDN 缓存刷新要手动提工单。这在一年前可能问题不大（那时候用户量小，CDN 缓存命中率低），但现在用户量上来之后，CDN 缓存问题的影响面完全不同了。

**hash 校验用了文件大小而不是内容 hash。** 客户端下载完 bundle 后的校验逻辑是比对文件大小。这是很早期写的逻辑，当时图省事。如果用的是内容 hash（比如 MD5 或者 CRC32），CDN 返回旧文件时客户端就能立即发现，而不是等到 `LoadAsset` 返回 null 时才知道。

---

## 这篇真正想留下来的结论

这次事故让我意识到一件事：热更新链路的风险不在你最关注的地方，而在那些"一直没出过问题所以没人看"的地方。

CDN 缓存刷新这件事，我知道重要，但因为之前每次都手动处理都没出过问题，就一直没把它自动化。CI 的磁盘清理策略，我压根没想过它会影响 AB 构建。老用户的缓存更新路径，QA 从来没测过，我也没提过要测。

这些不是什么高深的技术问题。每一个单独拿出来，解决方案都很简单。但它们组合在一起，就构成了一条完整的事故链——任何一环被拦住，这次事故都不会发生。

如果只能留下一个教训，我会说：**构建和发布链路里，每一个"不归我管"的环节，都是一个潜在的事故源。不是说要你去接管所有事情，而是至少要知道那些环节的假设是什么，以及这些假设在你的场景下是否成立。**

那个磁盘清理脚本的假设是"构建产物可以按文件类型独立删除"。这个假设对编译产物成立，但对 AB 增量构建不成立。如果我早一点了解过这个脚本的逻辑，就不会有这次事故。

另一个收获是关于测试覆盖的：**如果你的用户有多条更新路径（新装、增量更新、跨版本更新、有缓存/无缓存），那每条路径都需要测试覆盖。** 只测"最干净"的那条路径，等于在赌其他路径不会出问题。而生产环境里，走"非最干净路径"的用户往往是大多数。
