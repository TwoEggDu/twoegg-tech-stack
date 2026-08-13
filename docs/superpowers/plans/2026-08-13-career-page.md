# Career V0.1 实施计划

## Task 1：建立页面骨架与数据边界

- 新增 `content/career.md`
- 新增 `data/career.yaml`，集中维护 Hero、能力、成果、时间线、证据与联系方式
- 新增 `layouts/_default/career.html`，按六个 Section 渲染

## Task 2：接入现有站点

- 在 `hugo.toml` 中新增“求职”导航，放在“关于”之前
- 在 `static/css/site.css` 末尾追加 Career 命名空间样式
- 复用现有 CSS 变量、按钮和布局语言，不改现有页面模板

## Task 3：验证内容与呈现

- 运行 Hugo 完整构建
- 校验 `/career/` 和所有内部证据链接
- 扫描 Career 构建产物中的隐私信息、内部代号和未验证数字
- 使用本地站点检查桌面端与移动端布局以及控制台错误

## Task 4：精确发布

- 只暂存 Career 页面白名单文件
- 检查暂存差异和 Git 状态，确认不包含用户现有草稿
- 提交并推送到 `main`，提供线上地址和 V0.1 复盘
