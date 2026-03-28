# Engine Notes Front Matter 规范（v1）

> 目标：把 `Engine Notes` 从“自动展开的归档页”改成“显式入口驱动的知识入口页”。

## 1. 设计原则

- 不再让模板靠 `weight` 猜“哪个页面代表整个系列”。
- 系列入口页负责输出系列元数据；正文页负责声明自己属于哪条主线。
- 交叉归属不再靠 `series` 数组平铺，而是改成 `primary_series + related_series`。
- 在完全迁移前，模板保持兼容旧字段：`series: "系列标题"`。

## 2. 字段约定

### 2.1 系列入口页

用于“系列索引页 / 第 0 篇 / 入口页”。

```yaml
series: "HybridCLR"
series_id: "hybridclr"
series_role: "index"
series_order: 0
series_nav_order: 30
series_title: "HybridCLR"
series_audience:
  - "Unity 客户端"
  - "热更新 / 工具链"
series_level: "进阶"
series_best_for: "当你想把 HybridCLR 从 build-time、runtime 到排障链路一起看清"
```

字段说明：

- `series`：当前仓库里仍保留的人类可读系列名，兼容旧模板与旧文章。
- `series_id`：稳定 ID。后续模板和交叉引用优先依赖它，不依赖中文标题。
- `series_role`：系列内角色。入口页固定写 `index`。
- `series_order`：系列内部顺序。入口页固定写 `0`。
- `series_nav_order`：`Engine Notes` 列表页上系列卡片的排序。
- `series_title`：系列卡片显示标题。默认可与 `series` 相同。
- `series_audience`：系列卡片上的“适合谁”标签。
- `series_level`：系列难度，如 `入门 / 进阶 / 深水区`。
- `series_best_for`：一句话说明这个系列最适合解决什么问题。

### 2.2 系列正文页

用于主线正文、案例、FAQ、附录等系列成员页。

```yaml
series: "HybridCLR"
primary_series: "hybridclr"
series_role: "article"
series_order: 3
```

可选值：

- `series_role: article`：主线正文
- `series_role: case`：案例 / 事故 / 诊断文
- `series_role: faq`：FAQ / 高频误解
- `series_role: appendix`：附录 / 补充 / 索引之外的延伸文

说明：

- `primary_series` 是新主字段，值写 `series_id`。
- 在完全迁移前，建议继续保留 `series: "系列标题"`，方便旧模板、旧搜索结果和人工阅读。
- `series_order` 只负责系列内部顺序，不再拿它承担全站卡片排序。

### 2.3 交叉挂载页

```yaml
series: "Unity Shader Variant 治理"
primary_series: "unity-shader-variants"
related_series:
  - "unity-assets"
series_role: "article"
series_order: 4
```

规则：

- Landing page 只按 `primary_series` 收录。
- `related_series` 只用于文章页底部、索引页回链或后续的“相关主题”。
- 不再新增 `series:` 数组写法。

## 3. 二级索引（子系列）约定

当一个大系列继续拆出清晰子线时，再加下面这组字段：

```yaml
subseries: "Shader Variant 治理"
subseries_id: "shader-variants"
subseries_role: "index"
subseries_order: 0
```

正文页：

```yaml
subseries: "Shader Variant 治理"
primary_subseries: "shader-variants"
subseries_role: "article"
subseries_order: 2
```

使用条件：

- 系列内部已经出现稳定分支。
- 读者进入方式明显不止一种。
- 同一组文章里开始混入主线、案例、FAQ、实战、诊断等不同文体。

## 4. 迁移顺序

按下面顺序做，避免模板和内容互相打架：

1. 先给已有索引页补 `series_id / series_role / series_order / series_nav_order`。
2. 再把 `Engine Notes` landing page 改成“入口页优先、旧规则兜底”。
3. 再把大系列的正文逐步补 `primary_series`。
4. 最后再把旧的 `series:` 数组改成 `primary_series + related_series`。

## 5. 当前执行策略

- 现有模板已经优先识别 `series_role: index` 的入口页。
- 现有模板已经兼容旧的 `series: "系列标题"`。
- 还没补入口页的系列，会先落到“待补入口页的系列”区块。

## 6. 不建议继续做的写法

- 不要继续让模板用“排序后的第一篇”代表整个系列。
- 不要继续新增 `series:` 数组来表达双挂载。
- 不要把 `weight` 同时当成“全站排序”和“系列内部顺序”。
- 不要把系列 badge 元数据重复写到每篇正文里，优先写在入口页。
