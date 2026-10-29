# 文章呈现与创建约定

普通文章默认使用 Piccolo 的阅读外观：字体、页宽、顶部面包屑、桌面侧边目录与移动端折叠目录共用现有阅读样式。页面样式不依赖作者是否记得填写 `type`。

## 页面与系列的关系

- 默认正文入口和 `type: reading-series` 都调用 `layouts/partials/reading/article-document.html`；不要再创建另一套普通文章外壳。
- 独立文章显示“首页 / 主题 / 文章”，没有可确定的主题时回到完整技术目录，不虚构系列或篇数。
- 系列章节显示“首页 / 主题 / 系列 / 第 NN 篇”。主题优先取文章或系列索引的 `topic_key`，其余通过共用的主题映射处理。
- 系列索引使用 `series_role: index` 与 `series_id`；章节使用 `primary_series`。`series` 是展示名称，不是样式开关。
- `reading_index` 可明确指定索引文件；指向不存在的页或不匹配的系列会报错。新系列统一使用严格的 `type: reading-series`，缺少真实 ID、索引、章节顺序或出现重复顺序时应修正配置。
- 新系列保持唯一索引和完整、不重复的 `series_order`。直接用作篇号时必须是非负整数，已有 00 起始编号保留；`reading_number_by_position: true` 表示按阅读位置编号，此时允许非负小数作为插入排序值（兼容 YooAsset 的既有约定），显示篇号仍是整数。
- 历史文章只有系列名称时，唯一匹配的索引可用于导航；历史顺序不完整时沿已有 weight 排列。无法确定系列时只显示可靠的主题返回链接。
- 旧运行时文章中的 `series_id` 曾表示模块标签，不当作新的 `primary_series` 使用；不要批量填假 ID 来绕过检查。
- 索引包含真实 `chapters` 锚点时返回该位置，否则返回索引页本身。正文不再手写重复的上一篇、下一篇和旧栏目返回链接。
- 首页、主题页、完整技术目录、Agent 课程等专用入口保留专用布局；它们不是普通文章模板。

## 创建文章

普通文章：

```powershell
hugo new content delivery-engineering/my-article.md
```

系列索引与章节：

```powershell
hugo new content --kind series-index delivery-engineering/my-series-index.md
hugo new content --kind series-article delivery-engineering/my-series-01.md
```

三个模板均生成 `draft: true`。先填标题、描述和内容；系列模板还需要替换 `replace-with-series-id`、展示名称、主题与篇目顺序，再用 `hugo server -D` 预览。确认可发布后才改成 `draft: false`。

索引的 `reading_groups` 按 `series_order` 划分章节；0 开始的系列应同步调整 first。章节可以省略 `reading_title`、`reading_summary`，默认沿用标题和描述。`topic_key` 可选 `delivery`、`gameplay`、`frameworks`、`engine`、`performance`。

## 发布检查

需要 Hugo 与 Python 3.11 以上，不依赖第三方 Python 包。在仓库根目录执行：

```powershell
python -m unittest discover -s tests -p test_reading_layouts.py -v
hugo --gc --minify --destination .tmp/publish-check
python scripts/check-reading-pages.py --public .tmp/publish-check
```

使用独立输出目录，避免正在运行的 Hugo 预览服务同时修改待检查文件。若构建覆盖了 `--baseURL`，检查命令也传入相同的 `--base-url`，包括站点子路径。

回归测试会实际创建和构建新文章，覆盖缺省类型、独立文章、系列编号、历史元数据以及失效索引。生成页检查覆盖阅读样式、标题与面包屑、导航目标和锚点；旧文章外壳或错误链接会使检查失败。GitHub Actions 在上传 Pages 产物前执行这两类检查。

样式修改还应在浏览器查看一篇普通文章、一个独立页面、一篇 Piccolo 章节和系列索引，并检查窄屏目录。自动检查不替代视觉验收，也不表示生产站点已经发布。
