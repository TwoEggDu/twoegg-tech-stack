# 资源可用链审校记录

日期：2026-10-31。正文：[读盘完成，为什么还是不等于资源可用](../content/performance/game-performance-read-does-not-mean-ready.md)。篇级归属：[游戏性能判断系列](game-performance-series-plan.md)。

## 范围

按作者要求原位审校六阶段主线，补完成条件、依赖与调度、预算取舍、失败退出、阶段责任表与教学场景。同步性能索引、性能 canonical plan、FDE 方法 plan 与执行计划；不新增同义加载总论，不扩写完整资源框架。FDE 公开入口已有到本文的链接，无需重复添加。IO-06 的其他工作不计入本轮。

正文末尾原有“资源交付路径 · 7/7”及性能 plan 中客户端公共基础七阶段规划属于既有工作区改动，保持原样，不计入本轮提交范围。本轮无提交、推送或部署。

## 问题与修改

| 问题 | 修订 |
| --- | --- |
| 六阶段容易被读成固定串行流程 | 保留解释顺序，说明跳过、交叠、提前发现依赖与无需 GPU 的情况 |
| 完成条件缺少用途与接管边界 | 区分字节、资产、实例和用途；加入阶段、完成条件及责任表 |
| 读取被等同为物理磁盘访问 | 补缓存、有效范围、错误与内容身份检查 |
| 首次显示卡顿直接归因于延迟上传 | 区分上传、管线状态、激活与提交；现象只能提出待验证假设 |
| 同帧收口被直接认定为卡顿原因 | 改为检查关键路径、排队、同步和共享资源竞争，补依赖调度图与线程约束 |
| 提前加载、并发与对象池缺少代价 | 加入就绪时间、帧时间、重叠内存、后处理积压与背压取舍 |
| 缺少失败或退出后的责任 | 补部分成功清理、共享持有、取消语义、迟到结果代次检查与释放边界 |
| 技能例子容易被当成项目实证 | 明确为教学推演，贯穿正常使用、失败替代、准备中退出；未填写收益数字 |

## 资料与证据边界

以下官方资料用于核对指定版本的契约；不宣称完成引擎运行或设备测量。

- Unity 6.0：[纹理与网格加载](https://docs.unity3d.com/6000.0/Documentation/Manual/LoadingTextureandMeshData.html)，区分符合条件的同步与异步上传路径。
- Unity 6.0：[AssetBundle 压缩](https://docs.unity3d.com/6000.0/Documentation/Manual/AssetBundles-Cache.html)，区分 LZMA 内容流与 LZ4 分块。
- Unity 6.0：[场景激活](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AsyncOperation-allowSceneActivation.html)，核对 0.9、isDone 与队列边界。
- Unity 6.0：[异步与线程约束](https://docs.unity.com/en-us/engine/6000.0/manual/scripting/programming-distribute-work-threads/async-await-support/async-awaitable-continuations)，多数 Unity API 只允许主线程调用。
- Addressables 1.21.21：[操作句柄](https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/AddressableAssetsAsyncOperationHandle.html)，区分完成与成功、加载与实例化，以及 LoadAssetsAsync 的提前释放契约。
- Addressables 1.21.21：[内存管理](https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/MemoryManagement.html)，引用计数与实际卸载不同。
- Epic 页面标注 UE 5.8：[异步加载](https://dev.epicgames.com/documentation/en-us/unreal-engine/asynchronous-asset-loading-in-unreal-engine)、[PSO 预缓存](https://dev.epicgames.com/documentation/en-us/unreal-engine/pso-precaching-for-unreal-engine)，核对失败完成回调与表现／等待取舍，不外推所有版本及 RHI。

阶段表、预算表、依赖图和技能场景是教学与工程建议，不是引擎固定实现，也不是作者项目成果。未进行 Unity／UE 运行、设备 trace、故障注入或优化复测；正文保留 DATA-TODO。

## 验证

- `python -m unittest discover -s tests -p test_reading_layouts.py -v`：19 项通过。首次普通沙箱启动 Hugo 子进程遇到 WinError 5，授权执行环境下以相同命令重跑通过。
- `hugo --gc --minify --destination .tmp/resource-ready-audit-20261031/public`：最终正文构建通过，1443 页。
- `python scripts/check-reading-pages.py --public .tmp/resource-ready-audit-20261031/public`：1377 个阅读页面，0 errors。
- 独立本地预览已检查正文、阶段责任表、技能教学场景及目录定位；正文返回性能系列、性能系列 OS 补读链接进入正文均正常。保留现有视觉样式，未改模板或 CSS。
- 以本轮开始前快照比较差异，正文既有资源交付路径与性能 plan 七阶段规划保持原样。存储系列 plan 和 IO-06 审校记录在任务期间另有变化，本轮未编辑或回退它们。
- 本轮范围与前后快照保存在 `.tmp/resource-ready-audit-20261031/`，用于后续按增量提交；当前无提交、推送或部署。检查针对当前工作区生成站点，不代表引擎运行或设备性能验证。
