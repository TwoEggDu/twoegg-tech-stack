# 交付预算 14.2：包体预算小节审校记录

2026-11-05 · B1/B2 局部补桥。仅修[交付预算体系](../content/delivery-engineering/delivery-performance-stability-02-budget-system.md)的包体小节，不新增文章、篇号或实测。

## 1. 范围与复用

接续[执行计划](client-performance-budget-tiering-monitoring-execution-plan.md)中“渠道限制另查当前来源”的已登记问题。复读[包体预算 02](../content/performance/game-budget-02-package-budget.md)的首包/全量/增量/常驻分层，以及交付分发 03/04/05 的相关段落；后者仍有需要另行核对的旧数字，未作为当前平台额度的依据。本批保留预算框架职责，直接回链已有分层正文。

## 2. 关键主张与来源

| 主张位置 | 依据与修改 | 范围 |
| --- | --- | --- |
| [正文](../content/delivery-engineering/delivery-performance-stability-02-budget-system.md) L103–105，包体小节首两段 | Apple 构建限制与 Google 压缩下载口径分别核对；移除原三行“推荐首包大小” | 不给项目新填通用额度，不将上传、下载、安装和运行占用混为一项 |
| 正文 L107–112，包体规则表 | 平台硬限制、首次任务下载、更新和磁盘缓存分别给对象、条件与不能推出的结论 | 规则模板，不是平台接受或体验通过结果 |
| 正文 L114–116，教学账本段 | A=80+20、B=60+40，必要传输均 100 MiB；冷缓存、互斥集合、无重试和忽略协议开销均为明示教学前提 | 只解释字节转移；耗时、转化及真实下载收益无数据 |

本轮取得并实际阅读：

- [Apple Maximum build file sizes](https://developer.apple.com/help/app-store-connect/reference/app-uploads/maximum-build-file-sizes)：构建大小表及 uncompressed app size 的口径。原件/文本保存在本地 `.tmp/budget-package-revision-20261105/apple-build-sizes.html`、`.txt`。
- [Google Play maximum size limits](https://support.google.com/googleplay/android-developer/answer/9859372?hl=en)：明确限制按 Play Console 计算的 compressed download size，区分 module/asset pack，bundletool 只能近似估计。原件/文本同目录 `google-size-limits.html`、`.txt`。
- 微信旧候选链接 `https://developers.weixin.qq.com/minigame/dev/guide/base-ability/subpackages.html` 返回 HTTP 404，未取得现行额度正文。因此本批不保留原 20 MB 推荐线，也没有把其他旧文的 4/8/12/20/30 MB 值当当前官方规则；现行数值和适用能力仍待后续专项复核。下载状态在同目录 `sources.json`，不把保存入口或成功下载其他平台资料说成微信核查完成。

## 3. 专家追问与回答

| 追问 | 回答与判据 | 结论 |
| --- | --- | --- |
| 平台允许的大小能直接作为项目首包目标吗？ | 先区分产物/模块、压缩与下载口径；平台接收不证明用户任务体验达到要求 | 方法满足；无项目额度推荐 |
| AAB 或 IPA 变小，就能报告首进下载减少吗？ | 核对同一个任务终点前的完整必要下载集合；教学 A/B 均 100 MiB | 反例满足；没有真实产物或下载测试 |
| 字节相同，耗时也相同吗？ | 缺连接、并发、安装、准备与反馈证据；不能从字节单独推出耗时或转化率 | 推断边界满足 |
| 可以把任意资源移到任务后再算小包吗？ | 先证明它确实不是该任务的必要内容，核对功能及延后首用代价，再重新定义集合 | 条件变化推演满足；作者理解待反馈 |

## 4. 分层结论与检查

本小节按基础/预算方法职责通过 AI 编辑审校：对象、规则来源、条件和教学推断边界清楚。S1/S2 的任务边界与分层模型、S3/S4 的口径与证据身份、S5 的延后取舍满足本批职责；S6 仅完成候选规则字段与教学判断，完整预算推导及实际校准未完成；S7/S8 的工具运行与生产采用不属本次局部修订承诺。结论不扩大为整篇通过、当前平台额度全量核查或实际发布验收。

源文算例复算通过，A/B 必要下载均 100 MiB；旧推荐表已移除。19 项阅读模板回归通过；Hugo 0.157.0 默认构建 1478 页，生成页检查覆盖 1409 个阅读页面、错误为 0。生成正文 7 张表、教学身份、5 处 relref 入口及目标文件已核对，CPE-02 草稿未进入默认站点；审校与本批计划链接、显式锚点和表格列数检查通过，`git diff --check` 无空白错误。独立输出 `.tmp/budget-package-revision-20261105/site/` 使用配置中的 `https://twoeggdu.github.io/twoegg-tech-stack/` baseURL；日志、算例及生成检查脚本与 `validation.json` 同目录保存。

上述检查覆盖构建时完整工作区，未验证精确提交候选或线上部署。本轮无样式/图示修改，未做浏览器视觉验收。作者理解待反馈；实际构建候选上传、下载/网络测试、工具实现及团队使用未执行。本轮未暂存、提交、推送或发布，旧六文件候选快照保持独立。

## 5. 待补与下一步

微信现行数值保持待核查；原分发专项文章的数字与蜂窝下载叙述后续按其原 plan 局部修订，不把本次局部检查冒充整套分发文章通过。客户端专栏下一批处理加载完成点和 CI 的规则/验收边界，随后继续完整预算教学推导，仍不提前开展实际测量。
