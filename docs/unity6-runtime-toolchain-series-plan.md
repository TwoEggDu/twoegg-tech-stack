# Unity 6 运行时与工具链变化系列规划

## 专栏定位

这组文章不是 Unity 6 Release Notes 的中文翻译，也不是每个新 API 的用法速查。

它真正要解决的问题是：

`Unity 6 在运行时模型、异步编程、资产管线、平台适配四个方向上做了哪些结构性变化——这些变化改变了什么设计假设，对现有代码和工作流有什么具体影响——让读者知道哪些变化值得立刻跟进，哪些可以暂时观望。`

一句话说，这个专栏的重点不是"Unity 6 新增了什么 API"，而是：

`哪些变化改变了底层假设，以及这些假设变化如何影响你的现有代码和决策。`

---

## 目标读者

- 用 Unity 2022 开发中大型项目，需要评估 Unity 6 运行时变化对现有代码影响的程序员
- 正在从 Coroutine 向 async/await 迁移，需要理解 Awaitable / UniTask 定位差异的开发者
- 关注 Unity 版本策略变化（6.x 滚动更新 vs 传统大版本跳跃），需要制定版本锁定策略的技术负责人
- 配合阅读"Unity 异步运行时"系列、"Asset System & Serialization"系列的读者

---

## 专栏在整体内容地图里的位置

```
[Unity 异步运行时]（16 篇）
  讲 Task / UniTask / PlayerLoop 的完整模型（基于 2022）
        ↓
[Unity 6 运行时与工具链变化]          ← 本专栏
  讲 Unity 6 新增的 Awaitable、Content Pipeline 变化、
  平台适配变化，以及版本策略的工程影响
        ↓
[Unity 6 升级决策指南]
  讲升级 vs 不升级的成本收益分析
```

本专栏是"Unity 异步运行时"的 Unity 6 增量层 + 工具链/平台变化的独立模块。

---

## 系列边界

### 属于这个系列的内容

- Awaitable API 的设计定位、与 UniTask / Coroutine 的架构对比
- Awaitable 如何挂入 PlayerLoop，与 UniTask PlayerLoopTiming 的差异
- Content Pipeline 后台化对 CI/CD 和团队协作的影响
- Android GameActivity 替代方案及其对热更新和插件化的影响
- Unity 6.x 滚动更新策略下的 Breaking Changes 应对框架
- Unity Sentis 端侧推理引擎的能力边界

### 不属于这个系列的内容

- 完整的 async/await 语言机制 → 已在"异步运行时"系列
- UniTask 源码拆解 → 已在"异步运行时"系列
- 渲染管线相关变化 → 放在"渲染管线升级实战"系列
- 升级决策和版本选择 → 放在"升级决策指南"系列
- DOTS / ECS 变化 → 已有 DOTS 系列，后续按需扩展

---

## 文章规划

### 第一组：异步编程模型变化（2 篇）

| 编号 | slug 方向 | 标题方向 | 核心问题 |
|------|-----------|----------|----------|
| U6T-01 | `unity6-runtime-01-awaitable-vs-unitask-vs-coroutine` | Awaitable vs UniTask vs Coroutine：三代异步方案的架构对比 | Unity 为什么要做 Awaitable（动机和定位）；Awaitable 的数据模型（pooled、struct-like behavior）；与 UniTask 在 PlayerLoop 集成、CancellationToken、WhenAll 等能力上的 feature matrix；库开发用 Awaitable、项目开发用 UniTask 的决策框架 |
| U6T-02 | `unity6-runtime-02-awaitable-playerloop` | Unity 6 Awaitable 的 PlayerLoop 集成：帧时序与调度语义 | Awaitable.NextFrameAsync() / WaitForSecondsAsync() / EndOfFrameAsync() 各自挂在 PlayerLoop 哪个阶段；与 UniTask.Yield / UniTask.DelayFrame 的时序对比；跨帧等待的取消和异常传播行为 |

### 第二组：工具链与管线变化（2 篇）

| 编号 | slug 方向 | 标题方向 | 核心问题 |
|------|-----------|----------|----------|
| U6T-03 | `unity6-runtime-03-content-pipeline` | Content Pipeline 后台化：对 CI/CD 和团队协作的影响 | 哪些导入任务可以后台执行了；对 Unity Accelerator 的影响；CI 构建时间的预期变化；团队成员拉取资产时的体验变化 |
| U6T-04 | `unity6-runtime-04-android-gameactivity` | Android GameActivity 替代方案：对热更新和插件化的影响 | GameActivity vs 旧 Activity 模型的架构差异；对 JNI 调用量的影响；HybridCLR / 热更新方案在 GameActivity 下的兼容性；插件（如 Firebase、广告 SDK）的适配状态 |

### 第三组：版本策略与前沿能力（2 篇）

| 编号 | slug 方向 | 标题方向 | 核心问题 |
|------|-----------|----------|----------|
| U6T-05 | `unity6-runtime-05-rolling-update-strategy` | Unity 6.x 滚动更新策略：Breaking Changes 的应对框架 | 从 2019→2020→2021→2022 的大版本跳跃到 6.0→6.1→6.5 的滚动更新，版本策略变了什么；每个小版本可能带 breaking changes 的影响；版本锁定、升级窗口、regression testing 的工程实践 |
| U6T-06 | `unity6-runtime-06-sentis` | Unity Sentis 推理引擎：端侧 AI 的实际能力边界 | Sentis 是什么（ONNX 推理后端）；支持的模型类型和算子覆盖；移动端推理性能的真实水平；与服务端推理的定位差异；当前适合的应用场景（NPC 行为、内容生成辅助）vs 不适合的场景 |

---

## 与已有系列的关系

| 已有系列 | 本系列的关系 |
|----------|-------------|
| Unity 异步运行时（16 篇） | U6T-01/02 是该系列的 Unity 6 增量层；默认读者已读过 Task/UniTask/PlayerLoop 基础 |
| Asset System & Serialization（42 篇） | U6T-03 涉及 Content Pipeline 变化，但不重复资产序列化的基础讲解 |
| HybridCLR（24 篇） | U6T-04 涉及 GameActivity 对热更新的影响，会交叉引用 HybridCLR 系列 |
| DOTS 系列（35+ 篇） | 本系列不覆盖 ECS/Entities 变化，由 DOTS 系列后续按需扩展 |

---

## 推荐写作顺序

1. U6T-01（Awaitable vs UniTask vs Coroutine）→ 与异步运行时系列衔接最紧密
2. U6T-02（Awaitable PlayerLoop）→ 紧接 01，完成技术细节
3. U6T-05（滚动更新策略）→ 影响面广，所有使用 Unity 6 的团队都需要
4. U6T-04（Android GameActivity）→ 对移动端项目有直接影响
5. U6T-03（Content Pipeline）→ 对团队协作有影响
6. U6T-06（Sentis）→ 前沿能力，优先级最低

---

## 当前状态

- 系列规划：✅ 完成
- 已完成文章：0/6
- 下一步：从 U6T-01 开始编辑定位

---

*创建日期：2026-04-13*
