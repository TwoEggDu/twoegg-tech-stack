---
date: "2026-04-27"
title: "符号表与崩溃栈：IL2CPP 产物的符号链路"
description: '通用 CI 的产物链路是单向的，游戏团队是双向的——线上 crash 栈要反向回到 CI 归档的符号表才能定位代码。本篇拆解从用户设备到代码行的完整链路，以及"符号表丢失/版本错位/清理过度"三类故障。'
slug: "delivery-jenkins-ops-303-symbols-crashstack"
weight: 1584
featured: false
tags:
  - "Delivery Engineering"
  - "CI/CD"
  - "Jenkins"
  - "Unity"
  - "IL2CPP"
  - "Symbols"
series: "游戏团队 Jenkins 实战"
series_id: "delivery-jenkins-ops"
series_role: "article"
series_order: 140
delivery_layer: "practice"
delivery_volume: "V16"
delivery_parent_series: "delivery-cicd-pipeline"
delivery_reading_lines:
  - "L1"
  - "L2"
  - "L4"
leader_pick: true
---

## 在本篇你会读到

- **链路全景** —— 从用户设备 crash 到代码行的完整反向链路
- **符号产物** —— dSYM / Android symbols / il2cpp metadata 各自的角色
- **CI 侧归档** —— 什么、什么时候、归档到哪
- **监控系统侧消费** —— 自动符号化的接入
- **历史保留策略** —— 6-12 个月的版本生命周期
- **真实事故** —— 缺符号表的 crash 还原失败案例

---

## 链路全景：从用户设备到代码行

通用 Web/服务端的"调试"是单向链路：

```
用户报错 → 服务端日志（含文件名 + 行号） → 修复
```

游戏团队的"调试"是反向链路：

```
用户设备 crash
    ↓ 上报到 Crashlytics / Bugly
stripped 栈帧（只有地址，无文件名）
    ↓ 符号化（symbolicate）
含文件名 + 行号的栈
    ↓
程序员能看到出错位置
    ↓ 修复
```

关键节点：**符号化**这一步需要 CI 当时归档的符号表。**符号表丢了 = 永远不知道是哪行代码**。

---

## 符号产物：每个平台的角色

不同平台符号化机制不同，CI 必须分别处理。

### iOS：dSYM

`.dSYM` 是 Apple 的 debug symbols 格式，包含：

- 编译产物的符号表（函数地址 → 符号名）
- 源码 location 信息（地址 → 文件 + 行号）

**大小**：单平台单 build 200MB-5GB（取决于代码量）。

**生成位置**：Unity iOS build 完成后，Xcode 项目里的 `Build/iOS/Build/Intermediates.noindex/ArchiveIntermediates/.../dSYMs/` 下。

**关键点**：

- **dSYM 必须和 binary 一起归档**（同一次 build）
- **重新 build 同一个 commit 生成的 dSYM 不一样**——内部地址不同，老 dSYM 不能用于新 binary 的 crash

### Android：so + mapping.txt

Android 符号化需要两份：

#### `.so` symbols（native 部分，IL2CPP 编译产物）

- 在 `Build/Android/Build/.../symbols.zip` 下
- 大小：100-500 MB / 单 build
- 用于：还原 IL2CPP 转译后 C++ 的 native crash 栈

#### `mapping.txt`（ProGuard/R8 混淆映射）

- 在 `Build/Android/Build/.../mapping.txt` 下
- 大小：几 MB
- 用于：还原 Java/Kotlin 部分的混淆栈（不含 IL2CPP 部分）

**关键点**：两份缺一不可。Crashlytics 后台需要分别上传这两份。

### WebGL：与 iOS / Android 不同

WebGL 是 emscripten 编译的，符号化用 `.symbols` 文件 + source map：

- `.wasm.symbols`：函数地址 → 符号名
- `.wasm.map`：地址 → 源码位置（可选，体积大）

WebGL 平台 crash 处理在游戏团队不是常态，符号表归档可以更宽松。

### IL2CPP 中间产物：il2cpp_data

IL2CPP 把 C# 转译为 C++，转译过程中生成 `il2cpp_data` 文件夹（含 metadata、generated cpp）。**这部分通常不需要归档**——因为 dSYM / so symbols 已经包含了最终的符号信息。

但**特殊情况**：如果做"反向追溯到 C# 源码行"，需要保留 il2cpp 转译时的 mapping 文件（line mapping），这会让归档体积再翻倍。

---

## CI 侧归档：什么、什么时候、归档到哪

### 归档 What

每次发版（release / hotfix）build **必须**归档：

- iOS: `.ipa` + `.dSYM.zip`
- Android: `.apk` / `.aab` + `symbols.zip` + `mapping.txt`
- WebGL: `.wasm` + `.wasm.symbols`（按需）

每次 dev / qa build **可选**归档：

- 同上，但保留期短

每次 feature build **不必**归档（feature 不会上线，crash 不会回到这个 build）。

### 归档 When

最佳时机：**build 完成的同一个 stage 内**——保证产物和符号表 100% 对应。

```groovy
stage('Build iOS') {
    steps {
        sh 'unity -batchmode -executeMethod Build.iOS'
        sh 'cd Build/iOS && xcodebuild -archivePath out.xcarchive ...'
    }
}

stage('Archive') {
    steps {
        // 同一 stage 内归档所有产物 + 符号表
        archiveArtifacts artifacts: '''
            Build/iOS/out.ipa,
            Build/iOS/out.app.dSYM.zip
        '''
    }
}
```

**反模式**：把 dSYM 上传放在另一个独立 stage，甚至独立 Pipeline。如果中间发生 rebuild（哪怕是同 commit），dSYM 和 binary 会版本错位。

### 归档 Where

按"短期访问 vs 长期保留"分两层：

#### 短期：Jenkins archive

近 30 天的 build 产物 + 符号表归档到 Master 的 `archiveArtifacts`，业务方下载 build 时一并下载符号表。

#### 长期：对象存储（OSS / S3）

超过 30 天的版本，转移到对象存储：

```groovy
post {
    success {
        // 长期归档
        sh '''
            aws s3 cp Build/iOS/out.ipa s3://game-builds/${env.BRANCH_NAME}/${env.BUILD_NUMBER}/
            aws s3 cp Build/iOS/out.app.dSYM.zip s3://game-builds/${env.BRANCH_NAME}/${env.BUILD_NUMBER}/
        '''
    }
}
```

理由：

- Master 磁盘有限，不能堆积 GB 级符号表（详见 203）
- 对象存储有版本控制 + 跨地域复制 + 长期低成本

---

## 监控系统侧消费：自动符号化

CI 归档了符号表只是第一步。要让线上 crash 自动符号化，必须把符号表推到监控平台。

### Crashlytics 集成

Crashlytics（Firebase）的工作流：

1. App 上传 stripped crash → Crashlytics 服务器
2. Crashlytics 在符号表库里找对应 build 的符号表
3. 如果找到 → 自动符号化 → 后台显示文件名 + 行号
4. 如果没找到 → 显示"unsymbolicated"，永远是地址

CI 上传符号表的命令：

```bash
# iOS
firebase crashlytics:symbols:upload \
    --app=$IOS_APP_ID \
    Build/iOS/out.app.dSYM.zip

# Android
firebase crashlytics:symbols:upload \
    --app=$ANDROID_APP_ID \
    Build/Android/symbols.zip
```

集成到 Pipeline：

```groovy
stage('Upload Symbols') {
    steps {
        withCredentials([string(credentialsId: 'firebase-token', variable: 'FB_TOKEN')]) {
            sh '''
                firebase crashlytics:symbols:upload \
                    --token=$FB_TOKEN \
                    --app=$IOS_APP_ID \
                    Build/iOS/out.app.dSYM.zip
            '''
        }
    }
}
```

### Bugly / 友盟集成

国内监控平台同样需要上传：

```bash
# Bugly Android
java -jar bugly-android-symtabfileuploader.jar \
    -appid $APP_ID -appkey $APP_KEY \
    -bid $BUNDLE_ID -version $VERSION \
    -inputSymbol Build/Android/symbols.zip
```

每个平台有自己的 CLI 工具，集成方式类似。

### 上传失败的处理

上传符号表是**次要任务**——上传失败不应该让整条流水线 fail（业务方仍然能拿到 build）。但要告警：

```groovy
catchError(buildResult: 'UNSTABLE', stageResult: 'FAILURE') {
    sh 'firebase crashlytics:symbols:upload ...'
}
```

build 标记 UNSTABLE，监控系统看到这个状态发告警。**不能把符号化失败藏起来**——藏起来意味着未来这个版本的 crash 都不能符号化，业务方不知道。

---

## 历史保留策略：6-12 个月的版本生命周期

### 为什么要保留这么久

- **玩家更新慢**：很多玩家可能 2-3 个月不更新。某用户在 2 个月后才崩，CI 早就 release 5 个新版本了，但符号表必须还在。
- **安全 / 合规问题滞后**：发版 6 个月后被发现的安全漏洞，复盘要符号表
- **法律合规**：某些地区要求保留 N 年的发版记录

### 保留分级

| 版本类型 | 保留期 |
|---------|------|
| feature build | 7 天 |
| dev build | 30 天 |
| qa build | 90 天 |
| release / hotfix（已下线） | **永久或至少 12 个月** |
| release / hotfix（线上） | 永久（玩家可能在用） |

### 实现：对象存储 + 生命周期策略

OSS / S3 都支持 lifecycle policy：

```yaml
# S3 lifecycle
Rules:
  - Id: feature-builds-cleanup
    Filter:
      Prefix: builds/feature/
    Expiration:
      Days: 7
  
  - Id: release-builds-archival
    Filter:
      Prefix: builds/release/
    Transitions:
      - Days: 90
        StorageClass: GLACIER  # 冷存储，便宜
```

release 版本超过 90 天后转冷存储——便宜，但需要符号化时拉回来需要几小时。可以接受。

### "误删"防护

最危险的事故：**清理脚本误删了线上版本的符号表**。

防护：

- 永久版本的对象存储桶**开启版本控制**（versioning）—— 误删能恢复
- 关键版本的符号表**异地复制**（OSS 跨区域复制）
- 清理脚本**永远不直接删 release/* 路径**——只在 dev/* feature/* 路径上跑

---

## 真实事故：缺符号表的 crash 还原失败

### 时间线

- **T-3 个月**：发版 v2.4.0
- **T 时刻**：用户 X 升级到 v2.4.0 后开始崩溃
- **T+1 周**：crash 上报到 Crashlytics，但状态是 "Unsymbolicated"
- **T+2 周**：业务方反馈"这个 crash 占 5% 用户，必须修"
- **研发查看**：Crashlytics 后台只有地址栈：`0x00007fff8c2b1234`

### 排查链路

1. 看 CI 当时的 build：build #1247，3 个月前
2. 检查 archive：build #1247 的 archive 已被自动清理（默认 30 天）
3. 检查对象存储 OSS：v2.4.0 路径下有 `.ipa`，但**没有 `.dSYM.zip`**
4. 找历史归档脚本：3 个月前 CI 没有上传 dSYM 到 OSS（当时归档脚本只 cp 了 `.ipa`）
5. 检查 Crashlytics 历史上传记录：v2.4.0 没传过符号表

### 救火尝试

- **尝试 1**：rebuild 同 commit → 新 dSYM 和原 binary 地址不一致，无法用于符号化（详见 001 总论的"版本错位"陷阱）
- **尝试 2**：联系 Apple 索要 App Store 提交时的 dSYM —— Apple 后台只保留过去一段时间的 build，时间到了就没了
- **尝试 3**：人工分析地址栈猜测函数 —— 极度耗时且不准

### 最终方案

放弃符号化，只能根据"crash 在某个特定操作触发"反推大致位置，5 个研发人天才定位到。

### 损失

- 用户对 v2.4.0 失去信任，差评率上升
- 修复延迟 2 周（如果有符号表 30 分钟能定位）
- 建立了"符号表归档不完整"的事故案例

### 事后改进

- ✅ 所有 release / hotfix build 强制归档 dSYM 到 OSS
- ✅ Crashlytics 上传必须成功才能标记 build 为 SUCCESS
- ✅ 监控告警："Crashlytics Symbol upload failed" 立刻通知
- ✅ 季度演练：随机抽一个老版本，验证符号化链路完好

---

## 文末导读

下一步进 304 多平台并行打包与隔离——符号表归档稳定后，下一个特化是同时出 iOS / Android / WebGL 时的资源隔离。

L3 面试官线读者：本篇核心是"反向链路"那一节——CI 不再是"build 完就完了"，是"持续支撑线上调试"的反向支撑系统。这一个认知改变了归档策略、保留期、监控接入的所有决策。
