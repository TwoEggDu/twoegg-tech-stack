# 游戏引擎架构地图 06｜证据卡：跨平台引擎到底在抽象什么

## 本轮覆盖与证据边界

2026-10-25 修订：区分运行时适配和离线目标描述，用渲染目标不可用的设计例子解释能力、错误与回退责任。本文为原理篇附实现对照，保留原编号、标题与 URL；本卡对应修订后的正文，替换旧首稿证据清单。

- 正文：[06 平台抽象](../content/system-design/game-engine-architecture-06-platform-abstraction.md)。
- 结构：接口与设备差异 → 运行端/离线端模型 → 契约与能力 → 回退示例 → 职责及实现对照。
- 基线：Unity 6.0（6000.0）与 Unreal Engine 5.6 官方公开资料，本轮直接读取下列页面。
- 工程例子：高精度颜色目标与低档效果路线，使用伪代码；不保证某种实际格式或设备的支持情况。
- 验证范围：未做设备兼容性、性能测试或引擎运行；整站 Hugo 构建由主任务统一执行，本卡不提前记为通过。

## 当前正文直接来源

| 官方资料 | 直接支持的事实 | 不支持的推断 |
| --- | --- | --- |
| [Unity 6.0 Build Profiles](https://docs.unity3d.com/6000.0/Documentation/Manual/build-profiles.html) | 面向平台的构建配置，可保存多份配置 | 构建配置证明当前运行设备能力 |
| [Unity 6.0 Configure graphics APIs](https://docs.unity3d.com/6000.0/Documentation/Manual/configure-graphicsAPIs.html) | 文档中桌面平台的自动后端与手动候选顺序 | 所有平台都有相同回退行为 |
| [Unity 6.0 IsFormatSupported](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SystemInfo.IsFormatSupported.html) | 按格式与指定用途查询支持，所需用途不支持时操作失败 | 查询支持保证创建成功或替项目决定视觉降级 |
| [UE 5.6 RHI](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/RHI?application_version=5.6) | RHI 是 Runtime 模块，列出动态接口、资源与命令类型 | RHI 等于整个 OS 层或可由类型列表推导内部调度 |
| [UE 5.6 Packaging](https://dev.epicgames.com/documentation/en-us/unreal-engine/packaging-your-project?application_version=5.6) | 目标平台、SDK/组件要求及代码/资产转换流程 | 完成 Cook 证明任何目标设备支持所有效果 |

## 设计判断与最小模型

- 平台职责是总体主题，运行时依赖图应把目标描述和离线工具分开，不合成同一帧调用链。
- 平台契约定义请求语义、所有权、完成和失败；具体功能与体验回退交给理解需求的使用方。
- 示例中后端报告能力与创建结果，渲染系统实施候选路线，画质策略决定降级，离线工具交付这些路线。
- 能力检查通过不免除创建结果检查；开发机运行能力不能直接当成另一目标设备的能力。
- 运行与离线职责划分为作者解释模型，不声称覆盖 UE/Unity 的全部平台模块。

## 对旧稿的修正

- 拆开 Runtime RHI 与目标配置/工具职责，不再称所有平台相关接口为一层运行时抽象。
- 补文件、窗口和线程契约的通用例子，避免平台只剩图形设置与打包参数。
- 不沿用旧稿默认版本的 ITargetPlatform 能力枚举。其 5.6 API 页面本轮读取未成功，已用可直接核验的 5.6 Packaging 资料说明离线目标；没有将可读取的当前版本 API 冒充 5.6 证据。
- 删除旧证据卡对平台宏在 Editor 中“一律省略”的泛化；当前正文只讨论条件编译与设备查询职责，不给出未经本轮核验的具体宏行为。
- 删除重复论证、内部 READY 状态及旧“下一步建提纲”提示。

## 校验记录

- 已检查标题、日期、slug、series、weight 与修订前一致；正文 3 个 relref 目标存在。
- 正文 5 个官方来源与本卡对应，版本限定为 Unity 6000.0 / UE 5.6；Markdown 代码围栏成对。
- 本轮六个文件的 git diff --check 通过；尚未执行整站构建与浏览器图形验证，由主任务统一完成。
- 网页资料核对与站点构建不代表引擎运行验证。
