# CachedShadows 桥接预读 Batch 1 编辑工作单

这批不是给 `CachedShadows 01~08` 续编号，而是给“绝对新手”补桥。

建议定位为：`CachedShadows 阴影缓存专题 / 桥接预读`

建议阅读顺序：

1. 桥接预读 01
2. 桥接预读 02
3. `CachedShadows 阴影缓存专题索引`
4. `CachedShadows 01`
5. `CachedShadows 02`
6. `CachedShadows 03`
7. 桥接预读 03
8. `CachedShadows 04`
9. `CachedShadows 05`
10. `CachedShadows 06`
11. `CachedShadows 07`
12. `CachedShadows 08`

## 编辑工作单

**选题：** CachedShadows 预读 01｜先把阴影链路拆成四层：生成、接收、生效、交付
**系列 / 子系列：** CachedShadows 阴影缓存专题 / 桥接预读 01

---

### 1. 系列职责
先给新人一张“四层地图”，让读者在读 CachedShadows 正文前，先知道自己看到的术语和症状分别属于哪一层。

---

### 2. 与相邻文章的重复风险

| 相邻文章 | 重叠风险点 | 本篇处理方式 |
|---------|-----------|------------|
| 前一篇：`Shadow Map 机制：生成、级联与阴影质量问题` | 都会提到阴影图生成、阴影质量和“阴影为什么不对” | 前文负责 Shadow Map 基础原理，本篇只负责把问题拆成“生成 / 接收 / 生效 / 交付”四层，不重讲级联、Bias、分辨率数学 |
| 后一篇：`CachedShadows 阴影缓存 01｜它替代了 URP 主光阴影链路里的哪一段` | 都会讲“它替代了什么”与“URP 主光阴影链路” | 本篇不讲 TopHeroUnity 的具体替代位置，只讲一张抽象但可落地的分层图，帮助读者知道后文每篇站在哪一层 |

---

### 3. 本文必须回答的核心问题
> 我现在卡的是哪一层？

---

### 4. 本文明确不展开的内容

- `Shadow Map` 的级联、深度比较、Bias 调参（由 `Shadow Map 机制：生成、级联与阴影质量问题` 覆盖）
- TopHeroUnity 里 `CachedShadows` 替代 URP 主光阴影的具体位置（由 `CachedShadows 01` 覆盖）
- `Shader / Variant / Hidden Shader / preload` 的运行时交付边界（由 `CachedShadows 04` 覆盖）

---

### 5. 推荐二级标题结构

```text
1. 为什么先别急着背 CachedShadows 名词  — 职责：先把“新人卡住的不是名词数量，而是不知道问题站在哪层”说清楚
2. 阴影问题为什么至少要拆成四层  — 职责：定义生成、接收、生效、交付四层各自回答什么问题
3. 同一个现象为什么可能来自不同层  — 职责：讲“没影子”“不刷新”“Editor 有真机没”为什么不能只盯一个点
4. 把 TopHeroUnity 的常见术语挂回四层图  — 职责：把 main light、receiver、Renderer Feature、variant、preload 这些词先安到正确层级
5. 进入 CachedShadows 正文前先记住的五个判断句  — 职责：把全文收成一张最小判断卡片
```

> 4~6 个，每节职责不重叠，顺序体现“先分层，再识别症状，再进入项目正文”的逻辑。

---

### 6. 读者前置知识

| 必须掌握 | 了解即可 |
|---------|---------|
| 知道主光、投影体、接收阴影不是同一件事 | `Pipeline Asset`、`Renderer`、`Feature` 这些词的大致含义 |
| 知道 Editor 看到的效果不一定等于真机运行时效果 | `keyword`、`variant`、`preload` 是运行时边界相关术语 |

---

### 7. 文末导读建议

- **下一步应读：** `CachedShadows 阴影缓存 01｜它替代了 URP 主光阴影链路里的哪一段` — 理由：把这张四层地图正式接到 TopHeroUnity 当前这套真实阴影系统上
- **扩展阅读：** `Shadow Map 机制：生成、级联与阴影质量问题` — 理由：如果你在“生成层”卡住，就该回去补 Shadow Map 基础

---

## 编辑工作单

**选题：** CachedShadows 预读 02｜先认 TopHeroUnity 里的六个关键对象：Quality、Pipeline Asset、Renderer、Feature、Camera、触发器
**系列 / 子系列：** CachedShadows 阴影缓存专题 / 桥接预读 02

---

### 1. 系列职责
在进入 `CachedShadows 03` 之前，先把“项目里谁在决定什么”讲清楚，避免新人把“资产存在”误判成“运行时命中”。

---

### 2. 与相邻文章的重复风险

| 相邻文章 | 重叠风险点 | 本篇处理方式 |
|---------|-----------|------------|
| 前一篇：`URP 架构详解：从 Asset 到 RenderPass 的层级结构` | 都会讲 URP 里的 Asset、Renderer、Pass 分层关系 | 前文负责通用 URP 架构，本篇只挑 TopHeroUnity 当前 CachedShadows 主线里最关键的六个对象，不讲通用 API 和完整层级细节 |
| 后一篇：`CachedShadows 阴影缓存 03｜从 Quality 到 Camera：TopHeroUnity 里一个阴影是怎么真正被启用的` | 都会讲 Quality、Pipeline Asset、Renderer、Camera 的链路 | 本篇只做“对象认路”和“谁管什么”的预读，不进入字段值、平台默认档位和实际证据链 |

---

### 3. 本文必须回答的核心问题
> 我该先看哪个对象？

---

### 4. 本文明确不展开的内容

- `m_CurrentQuality`、`m_DefaultRendererIndex`、`featureReferences` 等具体字段证据（由 `CachedShadows 03` 覆盖）
- 一帧里的静态缓存、动态叠加与 receiver 交接（由 `CachedShadows 02` 覆盖）
- `Shader / Variant / Hidden Shader / preload` 的构建与运行时边界（由 `CachedShadows 04` 覆盖）

---

### 5. 推荐二级标题结构

```text
1. 为什么新人最容易把“看到了资源”当成“命中了链路”  — 职责：先把最常见误判说透
2. 六个关键对象各自管什么  — 职责：逐个定位 Quality、Pipeline Asset、Renderer、Feature、Camera、触发器的职责
3. 这六个对象在 TopHeroUnity 里怎么串起来  — 职责：先给对象关系图，但不进入字段值
4. 哪些关系是“拥有”，哪些关系只是“触发”  — 职责：讲清资产归属链和运行时刷新链不是一回事
5. 读 CachedShadows 03 前要先带着哪张图  — 职责：把全文收成一张“对象 -> 作用 -> 常见误判”的对照表
```

> 4~6 个，每节职责不重叠，顺序体现“先识别对象，再识别关系，再进入真实生效链”的逻辑。

---

### 6. 读者前置知识

| 必须掌握 | 了解即可 |
|---------|---------|
| 知道 Unity 项目里存在 `Quality`、`Camera`、资源文件和运行时脚本这几类对象 | `Renderer Feature` 能往渲染链路里插入额外 pass |
| 知道“资产被挂上去”和“运行时真的执行到”可能是两件事 | 平台默认质量档和 Editor 当前质量档可能不同 |

---

### 7. 文末导读建议

- **下一步应读：** `CachedShadows 阴影缓存 03｜从 Quality 到 Camera：TopHeroUnity 里一个阴影是怎么真正被启用的` — 理由：把这张对象关系图正式落到字段和真实平台链路上
- **扩展阅读：** `URP 架构详解：从 Asset 到 RenderPass 的层级结构` — 理由：如果你分不清 Asset 和 Pass 的层级，先回去补 URP 总图

---

## 编辑工作单

**选题：** CachedShadows 预读 03｜编辑器有、真机没时，别先看代码：先分平台层、资源层、触发层
**系列 / 子系列：** CachedShadows 阴影缓存专题 / 桥接预读 03

---

### 1. 系列职责
给“Editor 有、真机没”这个最高频现场症状一条最短分流路径，让读者先分层，再决定该去读 `04`、`05` 还是 `06`。

---

### 2. 与相邻文章的重复风险

| 相邻文章 | 重叠风险点 | 本篇处理方式 |
|---------|-----------|------------|
| 前一篇：`CachedShadows 阴影缓存 04｜为什么编辑器有阴影、打包后可能没了：SVC、Hidden Shader、StaticShaders 各管什么` | 都会讲“Editor 有、打包后没了”以及资源链问题 | `04` 负责把 `Shader / Variant / Hidden Shader / preload` 拆开，本篇只负责先把症状分到平台层、资源层、触发层，不进入具体机制 |
| 后一篇：`CachedShadows 阴影缓存 05｜症状总表：没影子、不刷新、只有 Editor 有、Android 没有时先查什么` | 都会讲排查顺序和常见症状 | `05` 负责完整症状总表，本篇只处理“Editor 有、真机没”这一类最常见入口，并把人导向正确后文 |

---

### 3. 本文必须回答的核心问题
> Editor 有、真机没先分哪层？

---

### 4. 本文明确不展开的内容

- `SVC`、`Hidden Shader`、`StaticShaders.prefab`、`ProcedurePreload` 的完整职责边界（由 `CachedShadows 04` 覆盖）
- “完全没影子”“不刷新”“画质差”这些其他症状的完整总表（由 `CachedShadows 05`、`CachedShadows 07` 覆盖）
- `Frame Debugger`、`RenderDoc`、日志证据链的完整验证流程（由 `CachedShadows 06` 覆盖）

---

### 5. 推荐二级标题结构

```text
1. 为什么“Editor 有、真机没”最容易把人带去看错代码  — 职责：先说明这个现象为什么不能直接下结论
2. 先分平台层：你是不是根本没跑到以为的那套配置  — 职责：先切掉“平台档位没命中”的误判
3. 再分资源层：更像是 shader、variant 还是 preload 边界  — 职责：先做资源层的第一轮分流，但不展开机制
4. 最后分触发层：它是真没阴影，还是没按时刷新  — 职责：把“没刷新”从“没阴影”里拆出来
5. 三条最短检查路径  — 职责：按最常见三种现场表现给出最小排查顺序
6. 分完层后该跳哪篇  — 职责：把本篇导向 `CachedShadows 04/05/06`
```

> 4~6 个，每节职责不重叠，顺序体现“先分流，再进入专项原理或排查文”的逻辑。

---

### 6. 读者前置知识

| 必须掌握 | 了解即可 |
|---------|---------|
| 知道 Editor 和真机是两套不同运行环境 | `SVC`、`Hidden Shader`、`preload` 这些词和资源交付有关 |
| 知道“有影子但不刷新”和“完全没影子”不是同一种症状 | `Frame Debugger`、`RenderDoc` 能用来做运行时验证 |

---

### 7. 文末导读建议

- **下一步应读：** `CachedShadows 阴影缓存 05｜症状总表：没影子、不刷新、只有 Editor 有、Android 没有时先查什么` — 理由：把这篇的单症状分流，接到完整的排查总表
- **扩展阅读：** `CachedShadows 阴影缓存 04｜为什么编辑器有阴影、打包后可能没了：SVC、Hidden Shader、StaticShaders 各管什么` — 理由：如果你已经判断更像资源交付问题，就该直接进入资源层细拆
