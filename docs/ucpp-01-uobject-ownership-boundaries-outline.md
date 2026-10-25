# UCPP-1｜普通对象与 UObject：创建、指针、持有和结束的边界

更新：2026-10-25。类型：映射篇。Canonical 归属为 [UE 全栈路线的 C++ 专项表](ue-fullstack-expert-series-plan.md#cpp-source-prerequisites)，不另建系列计划。

## 选题与边界

- 面向读完 CXX-01～03、已有 C# / Unity 经验的读者，解释普通 C++、UObject、Actor 三种对象协议怎样共存。
- 共同模型：创建入口 → 保存的访问路径 → 真正的保活关系 → 业务结束 → 最终销毁/存储释放。Outer、Actor Owner、场景附着和 GC 引用图分别判断。
- 复用 CXX-02 的资源责任、UFS-03B 的默认组件与 Actor、UE-01 的对象/类型/CDO、UE-03 与 DS-23 的 GC 问题；不重复 RAII 语言课，不扩成完整 GC 内核或任务线程教程。
- 正文：`content/engine-toolchain/ucpp-01-uobject-ownership-boundaries.md`；系列归 UE 全栈，专项编号 UCPP-1，不增加公共 CXX 篇数或 UFS 编号。

## 文章骨架

1. 地址仍非空，为什么业务对象已经不能继续使用。
2. 普通对象、UObject 与 Actor 的创建/持有/结束模型。
3. UPROPERTY/TObjectPtr、弱/软引用、native owner 的 TStrongObjectPtr；保活不等于业务授权或线程安全。
4. Outer 与 Owner 不替代 GC 可达路径，解释为何已有组件示例不能证明无条件 Outer 保活。
5. 两文件自有 UE 样例：普通资源清理、仅 Outer、属性强持有和弱观察；先解除持有，再请求 GC 并另行观察。
6. Actor 的 Destroy/EndPlay 与最终 GC 分开；请求 GC 不等于 API 返回时已清理完成。
7. 验证步骤、预测表、隐藏引用排查、阅读入口与未验证范围。

## 证据与版本

- 延续 UFS 目标 UE 5.8.2；2026-10-25 可访问的 Epic 页面显示 5.8 文档，不视为 patch 实测。
- 实际核对 Object Pointers、Objects、Unreal Object Handling、Actor Lifecycle，以及 TStrongObjectPtr/TWeakObjectPtr 和 UEngine API。正文保留支持具体判断的链接。
- `docs/engine-source-roots.md` 的 Unreal 状态仍是 TODO、路径为空；本机命令入口未找到 UnrealEditor/UnrealBuildTool。未开展源码核查、UHT/UBT、UE 编译、PIE、独立包或 GC 实验。
- 代码是自有教学样例；API、头文件、声明与使用做静态核对。验证预期与实际结果分栏，不用 mock 引擎或普通 GCC 结果替代 UE 通过。

## 配套修改

- UE-01 修正 Outer 保活及创建/加载生命周期表述，并让长期持有示例显式进入反射成员。
- UE-03、DS-23 收紧 GC 可见持有、弱观察、普通智能指针、请求收集与最终回收的口径；原 URL、原归属保持。
- UFS-03B、旧对象/GC 文与架构索引回链专项；UE canonical 表、公共目录与引擎入口同步。
- 清理当前公共 C++ 入口的待写状态，包括 Piccolo/UE/IL2CPP 入口及 Piccolo plan；历史 outline 的当时记录保留。
- 只更新根 doc-plan 的 UE 系列汇总；共享工作区其他修改保留。写作前快照位于本机临时目录 `ucpp01-before-uzz98ahy`。

## 验证记录

- Hugo v0.157.0 extended 在跟踪基线 `85a7d98487408300ab06c3ec0faeb510ef63b6ac` 加本轮精确增量的隔离目录构建通过：1410 页、零 ERROR；未把共享工作区其他修改混入候选。
- 11 个相关内容页的 269 个 relref 目标、围栏与 shortcode 引号检查通过；生成页 516 处本站链接/资源/锚点全部有效，同域主页与 AllWeather 另站共 12 处链接不按本站本地文件处理。八个既有入口/旧文生成页均有 UCPP-1 回链。
- 两个自有 UE 代码块完成声明/定义、generated.h 最后 include、四个蓝图调用入口、弱观察无旧裸指针解引用等静态检查；并非 UHT 或 UE 编译通过。
- 清理当前入口的公共课过期状态；历史 CXX/PCPP outline 的当时验证记录不回写成今日状态。
- UE 验证继续为待执行，不因文章成稿更新 M0 或个人能力状态。提交和推送以 Git 记录为准。
