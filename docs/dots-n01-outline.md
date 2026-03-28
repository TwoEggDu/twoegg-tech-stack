# Unity DOTS N01｜详细提纲：Client World / Server World / Ghost 各自在解决什么

## 本提纲用途

- 对应文章：`DOTS-N01`
- 本次增量类型：`详细提纲`
- 上游资料：
  - `docs/unity-dots-follow-up-batch-1-editorial-workorders.md`
  - `docs/unity-dots-follow-up-shared-glossary.md`
  - `docs/unity-dots-follow-up-article-template.md`
- 本篇定位：`DOTS NetCode` 子系列入口篇，先把 World、Ghost、Authority、Prediction 的地图立住

## 文章主问题与边界

- 这篇只回答：`NetCode 里的 Client World / Server World / Ghost / Authority 到底分别在解决什么。`
- 这篇不展开：`Ghost 字段同步和 Snapshot 粒度`
- 这篇不展开：`CommandData 输入链和 Tick 对齐`
- 这篇不展开：`Prediction / Rollback 的执行细节`
- 这篇不展开：`角色 / 投射物 / 技能系统的具体拆法`
- 本篇允许落下的判断强度：`先画世界地图、角色划分和阅读路径，再把具体同步机制留给后文。`

## 一句话中心判断

- `DOTS NetCode 不是“网络版 ECS API”，而是一套把 Client / Server World、Ghost、Snapshot、Prediction 和权威边界重新组合起来的运行时划分；先把这张地图画清，后面的同步、预测和排障才不会写成混战。`

## 行文顺序与字数预算

| 正文部分 | 目标字数 | 本段任务 |
| --- | --- | --- |
| 1. 为什么 NetCode 很容易被看成 API 套皮 | 300 - 450 | 先拆掉最常见的误读 |
| 2. Client World 与 Server World 分别负责什么 | 700 - 900 | 立住最基础的世界划分 |
| 3. Ghost 在链路里站什么位置 | 450 - 650 | 解释同步对象模型，而不是直接讲字段复制 |
| 4. Authority、Prediction、Interpolation 三种角色 | 600 - 800 | 区分权威裁决、本地预测和远端显示 |
| 5. 常见误读为什么会不断复发 | 300 - 450 | 收掉几个最典型的概念混淆 |
| 6. 这张地图决定后面几篇怎么读 | 250 - 400 | 把 N02 / N03 / N04 / N05 / N06 / N07 接出来 |

## 详细结构

### 1. 为什么 NetCode 很容易被看成 API 套皮

- 开头先摆读者最常见的错误预期：
  - `有 Ghost、有网络包，所以 NetCode 应该只是又一套同步组件`
- 然后指出问题本体：
  - 真正的难点不是“怎么发包”
  - 而是“世界如何分开、状态怎样复制、谁持有权威、哪里允许预测”
- 这一节的动作：
  - 先把全文从 API 教程里拉出来
  - 给后文埋下 `World`、`Ghost`、`Authority` 这三个词

### 2. Client World 与 Server World 分别负责什么

- 先讲为什么要拆成多个 World，而不是“一个 World + 网络模块”
- 这节必须清楚压出的判断：
  - Server World 负责权威状态推进
  - Client World 不只是“显示端”，它还承担输入采样、本地预测、远端显示
- 建议放一张最小链路图：
  - 输入进入哪一侧
  - 权威状态在哪一侧产生
  - 远端显示在哪一侧重建
- 不要在这里讲具体 `CommandData` 或 `Snapshot` 细节，只立地图

### 3. Ghost 在链路里站什么位置

- 这节不要写成字段清单
- 重点是解释：
  - Ghost 为什么存在
  - 它在 Client / Server World 之间扮演的是什么角色
  - 它和“普通 ECS Entity”在同步问题上有什么额外职责
- 这节要明确指出：
  - Ghost 不是“任何实体都自动应该变成的东西”
  - 同步对象的选择本身就是预算问题

### 4. Authority、Prediction、Interpolation 三种角色

- 这是全文第二个主干段落
- 必须讲清：
  - `Authority`：谁能最终裁决状态
  - `Prediction`：本地为什么要先算
  - `Interpolation`：远端对象为什么不能也走预测
- 这里可以借一个非常小的时序示意：
  - 本地输入
  - 服务端确认
  - 远端回放
- 目标不是展开 `Rollback`
  - 只是先把三种角色划开，给 `N04` 留空间

### 5. 常见误读为什么会不断复发

- 至少要收 3 个误解：
  - 误解 1：同步状态 = 同步所有 ECS 数据
  - 误解 2：预测 = 客户端复制一份服务器逻辑就结束
  - 误解 3：远端显示也应该和本地对象一样跑预测
- 每个误解只点名，不展开具体修法
- 作用是把 `N02 / N03 / N04` 的分工合理化

### 6. 这张地图决定后面几篇怎么读

- 最后用一小段把后续路径挂出来：
  - `N03`：状态复制链
  - `N02`：输入链
  - `N04`：Prediction / Rollback
  - `N05`：同步预算与相关性
  - `N06`：角色 / 投射物 / 技能三类对象
  - `N07`：排障顺序
- 最后一句建议压成：
  - `只有先分清世界、权威和角色，NetCode 才不是一堆名字相似却互相打架的同步功能。`
