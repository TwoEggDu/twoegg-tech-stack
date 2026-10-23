# 阅读组织本地预览

2026-10-23 制作并重组首页：从游戏交付出发，以客户端工程师的视角深入技术，同时评审“主题 → 专栏／系列 → 文章”的阅读体验。没有改动正式首页、文章正文、原 URL 或规划文档，也没有提交、推送或部署。

## 直接查看

新增三个首页对比版本：[A · 杂志](http://localhost:13149/preview/editorial/)、[B · 交付地图](http://localhost:13149/preview/atlas/)、[C · 阅读手册](http://localhost:13149/preview/reading/)。各首页顶部均可切换，原预览保留。最新设计取舍、六页检查结果与截图见 [HOME-ALTERNATIVES.md](HOME-ALTERNATIVES.md)。下文三页验证记录属于前几轮预览。

本次已启动的服务只监听本机，端口 `13149`：

- [首页](http://localhost:13149/preview/)
- [性能优化主题](http://localhost:13149/preview/performance/)
- [启动与加载系列](http://localhost:13149/preview/performance/startup-loading/)

三页页首可以相互跳转；主题和系列内保留文章捷径。文章打开后使用原站点模板，可通过浏览器后退回到预览。

## 运行与隔离

在项目根目录执行以下命令。本机已存在的 `.codex-bin/hugo.exe` 为 Hugo 0.158.0；如果本机 `hugo` 命令可用，也可以替换可执行文件部分。无需安装新依赖。

```powershell
.\.codex-bin\hugo.exe server --config hugo.toml,previews/reading-experience/preview.toml --baseURL http://localhost:13149/ --appendPort=false --bind 127.0.0.1 --port 13149 --destination .tmp/reading-preview-review/served --disableFastRender --noHTTPCache
```

已有服务占用端口时直接访问，不要再启动一份。此次服务 PID 记录在 `.tmp/reading-preview-review/server.pid`，标准输出与错误输出也在该目录；终止前应核对进程身份。

预览通过额外配置挂载自己的 content、layouts、data 和 static。普通 `hugo` 构建不挂载本目录，因此不会产生预览路由或加载预览 CSS。预览页带 `noindex, nofollow`，不进入文章列表、站内搜索或最近更新。搜索复用原有 `site-search.html`、`search.js` 与首页 JSON 索引。

## 文件与内容复用

| 文件／目录 | 用途 |
| --- | --- |
| `preview.toml` | 本地 Hugo 挂载配置 |
| `content/preview/` | 三个页面的独立路由与元信息 |
| `data/reading_preview.yaml` | 精选文章引用、导读、主题与症状映射 |
| `layouts/reading-preview/` | 独立页框架与模板入口 |
| `layouts/partials/reading-preview/` | 页面编排、文章与链接组件 |
| `static/css/reading-preview.css` | 独立作用域样式与响应式布局 |
| `check_links.py` | 标准库实现的构建产物链接／锚点检查，可附加本机 HTTP 检查 |

文章标题和地址由 `site.GetPage` 读取当前内容；缺失引用与 draft 引用会阻止预览构建。这里只重写导读，不保存正文副本。

- 首页先呈现四个交付目标：内容完整可用、功能协同运行、设备体验达标、版本持续迭代。首读复用生产管线总览；“内容完整可用”进入已有资源管线系列，由该系列展开打包、加载、版本与生命周期，再进入 YooAsset、Addressables 等具体方案。首页文案不出现这两个框架名。多工种协作、六主题与实践保留；回退与 A/B 实验分组作为次要入口。
- 六个技术主题保留少量代表系列，作为客户端深入研究与直接查阅的入口；服务端由端服版本协作引出。性能优化通往预览主题页，其余通往现有系列或技术目录。
- 个人实践选取资源完整性、AOT 元数据漏包和线上反馈三篇，分别说明作者参与范围，保留定性观察与证据边界。
- 性能入门为性能判断、瓶颈诊断、验证闭环三篇。CPU 与 GPU 两个问题入口复用同一个已有移动端优化系列的不同分支，并保留直达文章的捷径，没有创建空系列。
- 启动与加载主线为七篇：可用边界 → 依赖与分包 → 运行时加载链 → 时机管理 → 内存行为 → 加载与释放 → CI 基线。Content Directories、OS I/O、序列化和资源构建放在深入阅读。
- 首载与缓存失效文章标为诊断场景和方法说明。持续性能文章标为“提纲／待补”。实验折叠说明是待执行的验证设计，未包装成已有实验记录或优化成绩。

## 设计取舍

延续现有首页的暖白底、深色文字、宋体大标题与砖红链接。首页先说明交付目标，再连接问题、原理与方案；四个目标使用无序行式入口，避免让读者误认为必须按某个工具流程依次学习。六组协作交接与六个主题提供深入入口；主题页按问题导向分组；系列页以有序列表给出阅读顺序。文章采用行式列表，只有首读和重点系列轻度强调，避免所有内容成为相同大小的卡片。

手机端保留完整内容；六主题在常用手机宽度为两列，320px 为单列。页首搜索在手机上单独占一行。交互仅使用实际链接、搜索、锚点和原生 details；不添加筛选器、动画或装饰按钮。

## 验证记录

本地构建：

```powershell
.\.codex-bin\hugo.exe --config hugo.toml,previews/reading-experience/preview.toml --destination .tmp/reading-preview-review/build --logLevel warn
python previews/reading-experience/check_links.py .tmp/reading-preview-review/build
```

检查正在运行的本机服务：

```powershell
python previews/reading-experience/check_links.py .tmp/reading-preview-review/served '' http://127.0.0.1:13149
```

已完成的检查：

- 预览构建和普通构建均成功，无 ERROR 或 WARN。
- 三页 144 个静态链接／资源引用、56 个不同目标、20 个锚点均有效；开发服务器注入的 livereload 脚本另作 HTTP 验证。57 个本机 HTTP 目标全部返回 200。
- 首轮 Chrome 已走通主题 → 系列 → 原文、症状跳转和实验 details。上一轮验证了首页首读、启动与加载、技术主题锚点和搜索；Escape 可关闭搜索。本轮实际验证首页目标锚点 → 资源管线系列，确认系列中存在具体方案阅读入口。首页可见文案不含 YooAsset / Addressables。
- 三页在 320、390、768、1024、1280px 下未发现横向溢出；检查了首屏、六主题和系列主线的实际排版。属于浏览器窄屏验证，未在实体手机或其他浏览器实测。
- 普通构建全部原有输出文件与改动前哈希一致，不含 `/preview/` 或预览 CSS；本轮所有已跟踪文件哈希未变。既有未跟踪内容未动。

首轮截图工具曾超时，本轮连接已恢复，重新保存了首页、交付路线与手机主题截图，并确认检查后浏览器恢复到原有 1920px 视口。实体手机与跨浏览器验证未执行。性能主题和加载系列的原截图仍适用；本轮确认这两页生成 HTML 在统一换行符后与首页改动前一致，正文与结构未改；共享模板编辑带来了部分 CRLF/LF 差异。

最新首页截图在本机 Codex 可视化目录；主题与系列沿用首轮截图：

- [首页桌面截图](C:/Users/IGG/.codex/visualizations/2026/10/23/01a166c3-ae12-7940-b8fb-9f273c74da92/home-v3-desktop.jpg)
- [性能主题桌面截图](C:/Users/IGG/.codex/visualizations/2026/10/23/01a166c3-ae12-7940-b8fb-9f273c74da92/preview-performance-desktop.jpg)
- [加载系列桌面截图](C:/Users/IGG/.codex/visualizations/2026/10/23/01a166c3-ae12-7940-b8fb-9f273c74da92/preview-loading-desktop.jpg)
- [首页手机截图](C:/Users/IGG/.codex/visualizations/2026/10/23/01a166c3-ae12-7940-b8fb-9f273c74da92/home-v3-mobile.jpg)
- [交付目标桌面截图](C:/Users/IGG/.codex/visualizations/2026/10/23/01a166c3-ae12-7940-b8fb-9f273c74da92/home-v3-goals-desktop.jpg)
- [交付目标手机截图](C:/Users/IGG/.codex/visualizations/2026/10/23/01a166c3-ae12-7940-b8fb-9f273c74da92/home-v3-goals-mobile.jpg)
- [性能主题手机截图](C:/Users/IGG/.codex/visualizations/2026/10/23/01a166c3-ae12-7940-b8fb-9f273c74da92/preview-performance-mobile.jpg)
- [加载系列手机截图](C:/Users/IGG/.codex/visualizations/2026/10/23/01a166c3-ae12-7940-b8fb-9f273c74da92/preview-loading-mobile.jpg)
- [加载主线手机截图](C:/Users/IGG/.codex/visualizations/2026/10/23/01a166c3-ae12-7940-b8fb-9f273c74da92/preview-loading-path-mobile.jpg)

## 建议重点评审

1. 首页能否明确传达交付主线与客户端重心；四个交付目标是否让读者知道先读什么。
2. 六个主题的名称与代表系列，尤其客户端框架与引擎研发的边界是否符合预期。
3. 性能页按问题组织、系列页安排顺序的区别是否直观。
4. 七篇加载路径的先后是否顺畅；原文标题较长时，手机上的阅读负担是否可以接受。
5. 从交付目标到问题、原理，再到具体实现方案，是否比直接列出工具与流程更容易理解；回退、A/B 等入口是否保持适当篇幅。

### 协作入口补充

首页协作区依次呈现策划 × 客户端、策划 × 美术、策划 × QA、美术 × 客户端、客户端 × 服务端、研发 × QA × 发布。前三项分别复用技能系统 27（配置管道）、28（表现交付标准）、13（测试与回归）；导读说明共同约定与反馈，不把机制文章标成个人实践。构建、三页链接和本机 HTTP 检查通过，桌面及 390/320px 协作区无横向溢出。

[最新协作区截图](C:/Users/IGG/.codex/visualizations/2026/10/23/01a166c3-ae12-7940-b8fb-9f273c74da92/home-v4-collaboration-desktop.jpg)
