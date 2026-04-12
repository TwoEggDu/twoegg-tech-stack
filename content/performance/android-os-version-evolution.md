---
title: "Android 版本演进｜每个关键版本改变了游戏的哪条运行规则"
slug: "android-os-version-evolution"
date: "2026-03-28"
description: "Android 每次大版本更新都会收紧某些系统权限或改变运行模型，但大多数文档只讲新功能，不讲「升级后游戏哪里会出问题」。本篇按版本节点梳理对游戏影响最大的运行规则变化，以及 targetSdk 升级时必须处理的行为差异。"
tags:
  - "Android"
  - "版本兼容"
  - "targetSdk"
  - "移动端"
series: "移动端硬件与优化"
weight: 2240
---

Android 版本的变化对游戏开发者的影响，不是"新功能能不能用"，而是"原来能用的东西什么时候不能用了"。每次 targetSdk 升级，系统都会打开一批新的限制开关，直接影响后台行为、权限模型和文件访问。

---

## targetSdk 是什么，为什么它决定游戏的运行行为

Android 的兼容性机制依赖两个版本号：

```
minSdkVersion：安装门槛，低于这个版本的设备无法安装
targetSdkVersion：告诉系统「这个 App 已经适配到哪个版本」

关键机制：
  targetSdk < 系统版本 → 系统启用「兼容模式」，模拟旧版行为
  targetSdk = 系统版本 → 系统按最新规则运行，不做向下兼容

实际影响：
  targetSdk 28 的游戏运行在 Android 14 设备上
  → 部分 Android 14 的新限制不生效（系统帮你兼容）
  → 但 Google Play 要求 targetSdk 必须跟进到最新
```

**Google Play 的强制要求（截至 2025 年）**：
- 新应用上架：targetSdk ≥ 34（Android 14）
- 已有应用更新：targetSdk ≥ 34
- 不更新的老版本：可以继续留在商店，但新设备用户会看到警告

每次被迫升级 targetSdk，就是被迫接受那个版本之前积累的所有行为变化。

---

## Android 8.0（API 26）：后台服务被限制

**对游戏的影响**：后台音乐、实时数据同步、长连接保活。

```
Android 8.0 之前：
  startService() 可以在后台无限运行
  → 常见做法：游戏切后台后，用 Service 保持网络连接、同步进度

Android 8.0 之后（targetSdk ≥ 26）：
  App 进入后台 → 几分钟内系统杀掉后台 Service
  → startService() 在后台直接抛 IllegalStateException

正确做法：
  需要后台保持运行 → 改用 startForegroundService()（带通知栏图标）
  需要延迟任务 → 改用 JobScheduler 或 WorkManager
  需要网络长连接 → 改用 FCM 推送唤醒，而不是保持 Socket 常连
```

**实际踩坑场景**：游戏切后台后，用来维持多人对战 WebSocket 连接的 Service 被系统杀掉，玩家重新进来发现已经掉线。

---

## Android 9.0（API 28）：明文网络流量被封锁

**对游戏的影响**：HTTP 接口、资产下载。

```
Android 9.0 默认行为（targetSdk ≥ 28）：
  所有明文 HTTP 流量被系统拦截，只允许 HTTPS

  影响场景：
  → CDN 资产包下载用的 http:// → 直接失败
  → 热更新服务器用 HTTP → 下载报错
  → 日志上报用 HTTP → 静默失败

解决方案：
  全部迁移到 HTTPS（推荐）

  或者在 AndroidManifest.xml 中配置网络安全策略：
  res/xml/network_security_config.xml：
    <domain-config cleartextTrafficPermitted="true">
      <domain includeSubdomains="true">your-cdn.com</domain>
    </domain-config>
```

同时，Android 9 也是 **Vulkan 驱动质量的分水岭**——API 28 以下的 Vulkan 驱动普遍不稳定，Unity 和 Unreal 的官方建议是 minSdk 28 以上才考虑默认开启 Vulkan。

---

## Android 10（API 29）：Thermal API 可用 + 后台位置收紧

**对游戏的影响**：主动热管控、地图类游戏权限。

```
新增：PowerManager.getThermalHeadroom()
  → 可以查询设备在未来 N 秒的热余量（0=即将降频，1=完全安全）
  → 游戏可以根据这个值主动降低画质，避免被动降频

  // 查询 30 秒内的热余量
  float headroom = powerManager.getThermalHeadroom(30);
  if (headroom < 0.5f) {
      // 主动降低 GPU 负载
  }
```

**后台位置权限收紧**：
```
Android 9 及以前：
  申请 ACCESS_FINE_LOCATION → 前台后台都能用

Android 10（targetSdk ≥ 29）：
  前台位置和后台位置分开申请
  后台位置需要额外申请 ACCESS_BACKGROUND_LOCATION
  且系统会弹出额外确认弹窗
```

---

## Android 11（API 30）：Scoped Storage 强制执行

**对游戏的影响**：截图保存、录像、外部存储读写、OBB 资产包。

```
Android 10：Scoped Storage 引入，但 requestLegacyExternalStorage=true 可以绕过
Android 11（targetSdk ≥ 30）：requestLegacyExternalStorage 完全失效，必须用新 API

新规则：
  App 只能无权限访问自己的私有目录（getExternalFilesDir()）
  访问公共媒体（图片/视频）→ 必须用 MediaStore API
  访问任意文件 → 必须申请 MANAGE_EXTERNAL_STORAGE（Google Play 审核严格）

游戏截图保存的正确做法（Android 11+）：
  ContentValues values = new ContentValues();
  values.put(MediaStore.Images.Media.DISPLAY_NAME, "screenshot.png");
  values.put(MediaStore.Images.Media.MIME_TYPE, "image/png");
  values.put(MediaStore.Images.Media.RELATIVE_PATH, "Pictures/MyGame");
  Uri uri = resolver.insert(MediaStore.Images.Media.EXTERNAL_CONTENT_URI, values);
```

**OBB 文件**（Android 大资产包）：

```
OBB 路径（/sdcard/Android/obb/包名/）在 Android 11 上不再需要存储权限
→ App 可以直接读写自己包名下的 OBB
→ 但注意：如果游戏之前依赖 READ_EXTERNAL_STORAGE 读 OBB，升级 targetSdk 后这个权限失效
```

**包可见性限制（Package Visibility）**：

```
Android 11（targetSdk ≥ 30）：
  不能直接查询其他 App 是否安装（用于跳转到其他 App 或检测反作弊工具）

  解决：在 AndroidManifest.xml 声明需要查询的包名
  <queries>
    <package android:name="com.tencent.mm" />  // 微信
  </queries>
```

---

## Android 12（API 31）：Game Mode API + 精确闹钟权限

**对游戏的影响**：性能模式接入、定时任务。

```
Game Mode API（Android 12+）：
  系统提供三种游戏模式，用户可在系统设置中切换：
  GAME_MODE_STANDARD   → 普通模式
  GAME_MODE_PERFORMANCE → 性能优先（允许更高功耗）
  GAME_MODE_BATTERY    → 省电优先（限制帧率）

  游戏可以查询当前模式并调整策略：
  GameManager gameManager = getSystemService(GameManager.class);
  int mode = gameManager.getGameMode();
```

**精确闹钟权限**：

```
Android 12（targetSdk ≥ 31）：
  AlarmManager 的精确闹钟需要申请 SCHEDULE_EXACT_ALARM 权限
  → 大多数游戏不需要精确闹钟，可以忽略
  → 如果游戏有「离线奖励提醒」功能，需要适配
```

**启动画面（Splash Screen）API**：

```
Android 12 强制为所有 App 添加启动画面（即使没有主动实现）
→ 如果游戏自己实现了启动画面，会和系统的叠在一起，出现双重 Splash
→ 解决：用官方 SplashScreen API 替换自定义实现
```

---

## Android 13（API 33）：通知权限需要主动申请

**对游戏的影响**：推送通知、离线召回。

```
Android 12 及以前：
  App 安装后默认有发通知的权限

Android 13（targetSdk ≥ 33）：
  发通知需要用户授权 POST_NOTIFICATIONS 权限
  → 游戏需要在合适时机弹出权限请求
  → 用户拒绝后，所有本地通知和推送通知都无法显示

推荐时机：
  不要一打开游戏就弹权限请求（用户会直接拒绝）
  在用户完成首次游戏体验后，结合功能说明再请求
```

**媒体权限细化**：

```
Android 12 及以前：READ_EXTERNAL_STORAGE 可以读所有外部文件
Android 13（targetSdk ≥ 33）：
  READ_MEDIA_IMAGES  → 只能读图片
  READ_MEDIA_VIDEO   → 只能读视频
  READ_MEDIA_AUDIO   → 只能读音频
  旧的 READ_EXTERNAL_STORAGE 权限在 API 33+ 失效
```

---

## Android 14（API 34）：64-bit 强制 + Photo Picker

**对游戏的影响**：32-bit 代码、用户头像/相册功能。

```
64-bit 强制要求：
  Android 14 设备上，纯 32-bit APK 无法安装
  → 包含 ARMv7 only 的 so 库 → 直接安装失败
  → 解决：确保所有 Native 库都有 ARM64-v8a 版本
  → Unity：Build Settings 中勾选 ARM64，取消 ARMv7

  检查方法：
  unzip -l your-app.apk | grep lib/
  应该看到 lib/arm64-v8a/xxx.so
  而不只是 lib/armeabi-v7a/xxx.so
```

**Photo Picker 强制使用**：

```
targetSdk ≥ 34 的 App，访问用户相册必须通过系统 Photo Picker
→ 不能再用 READ_MEDIA_IMAGES 直接读取（系统会弹出 Photo Picker 界面）
→ 影响场景：游戏内更换头像、上传自定义图片
```

---

## Android 15（API 35）：边到边显示强制执行

**对游戏的影响**：全屏游戏的 UI 布局、系统手势冲突。

```
Android 15（targetSdk ≥ 35）：
  强制边到边（Edge-to-Edge）显示
  → App 内容延伸到状态栏和导航栏区域
  → 原来用 WindowInsets 躲避状态栏的布局需要重新适配

  游戏的常见问题：
  → HUD 元素被系统状态栏遮挡
  → 底部返回手势区域和游戏触控区域重叠

  解决：
  ViewCompat.setOnApplyWindowInsetsListener(view) { v, insets ->
      val bars = insets.getInsets(WindowInsetsCompat.Type.systemBars())
      // 用 bars.top / bars.bottom 偏移 HUD 元素
  }
```

---

## targetSdk 升级清单

每次被迫升级 targetSdk，按这个清单检查：

| 升级到 | 必查项 |
|--------|-------|
| API 26 | 后台 Service 是否还存在？改用 JobScheduler |
| API 28 | 所有网络请求是否已迁移到 HTTPS |
| API 29 | 是否使用后台位置？需申请额外权限 |
| API 30 | 外部存储读写是否已迁移到 MediaStore / 私有目录 |
| API 31 | 是否查询其他 App 安装状态？在 Manifest 声明 queries |
| API 33 | 推送通知是否主动申请 POST_NOTIFICATIONS |
| API 34 | 确认所有 so 库包含 ARM64 版本；相册访问改用 Photo Picker |
| API 35 | 全屏 UI 适配 Edge-to-Edge，检查 HUD 位置 |
