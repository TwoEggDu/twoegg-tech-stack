# Piccolo 17：场景颜色怎样经过后处理成为最终画面

> 执行层提纲与证据记录；篇级状态由 `piccolo-engine-series-plan.md` 统一维护。

## 定位与边界

- 接续用户确认的提纲：色调映射、LUT、FXAA、UI 合成与输出；先通用职责，再固定版本的真实实现，不能把未实现功能补写成已有行为。
- 固定源码 `BoomingTech/Piccolo@f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，保持只读，不修 Shader/颜色管理或同步设计。
- 16 停在浮点场景颜色；本文沿默认延迟路径继续到交换链图像与呈现请求，不重复 BRDF、资源加载或整帧 Vulkan 教程。
- 概念后处理顺序与附件实际复用分别表达；odd/even 是附件名字，不是 CPU 双缓冲或帧索引。
- 新正文 series_order=20，接 16；系列共 21 篇，09—18 中 11/15/16/17 已写，其余六篇仍待写，18 保留原选题。

## 执行清单

- [x] 核对默认 FXAA 配置、主相机调用顺序、subpass 输入输出、附件格式/复用与重建后的描述符更新。
- [x] 核对 Uncharted2 曲线、4.5 倍输入、11.2 白点归一、幂函数编码与交换链 format/colorSpace 的关系。
- [x] 核对 LUT 资源已加载/绑定但颜色分级 Shader 实际透传，不写成已生效的查色。
- [x] 核对 FXAA 的 sampler 邻域、亮度权重、提前返回、边缘与亚像素步骤、开关两路附件和初始化约束。
- [x] 核对 UI 清零、绘制、条件选色而非 alpha-over、UI 自身混合与最后合成的区别、Present 完成边界。
- [x] 独立编译相关 GLSL，复用原曲线文本做 CPU 条件检查，补流程/附件表、数值与观察任务。
- [x] 更新正文回链、阅读入口与 canonical 规划，运行 Hugo、源码/链接和生成页检查。

## 正文提纲

1. 场景线性照明、tone mapping、颜色编码、调色、空间抗锯齿与 UI 组合各自负责什么。
2. backup odd/even 与 post-process odd 的资源身份、两路 FXAA 配置和 subpass/draw 区别；不把复用名字当帧间双缓冲。
3. Uncharted2 逐通道曲线与固定曝光比例，白点归一后再 pow，强输入仍可大于 1；非自动曝光、非精确 sRGB EOTF、非完整 HDR 显示方案。
4. 通用 LUT 的颜色映射模型，固定 color_grading.frag 只 textureSize + 透传，声明和资源绑定不证明查色生效。
5. FXAA 需要邻域采样而非 subpassLoad 当前像素；按实际 .578 权重、阈值与偏移解释，保留默认关闭和非时间抗锯齿边界。
6. even 从 tone-map 输入复用为清零 UI 目标，ImGui 内部 alpha 混合与最后 CombineUI 条件选色分开；特殊阈值未测 B、alpha 也做幂变换。
7. 交换链 preferred UNORM + SRGB_NONLINEAR，fallback 不能假定格式；提交、present 请求与显示完成分别取证；重建后更新输入 View。
8. 给定灰度输入、平坦/对比邻域、透明/半透明/特殊 UI 像素的条件练习与后续 GPU 捕获表。

## 证据入口

- shader/glsl/tone_mapping.frag、color_grading.frag、fxaa.frag/.vert、combine_ui.frag。
- render/passes/main_camera_pass.cpp、tone_mapping_pass.cpp、color_grading_pass.cpp、fxaa_pass.cpp、ui_pass.cpp、combine_ui_pass.cpp。
- render/render_pipeline.cpp、render_system.cpp、render_resource.cpp；interface/vulkan/vulkan_rhi.cpp；asset/global/rendering.global.json；imgui/backends/imgui_impl_vulkan.cpp。
- Vulkan 官方 VkFormat、VkColorSpaceKHR、vkQueuePresentKHR 与 framebuffer 文档；只取固定路径相关基础语义。

## 验证记录

2026-10-25 完成正文与导航更新，验证分层记录如下。

### 源码与文章范围

- 只读源码 HEAD 为 `f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，`git status --porcelain` 为空。正文 15 个固定提交的 blob 链接均有本地对应文件（含许可证），七段 GLSL/C++ 节选忽略空白后与对应原文件匹配。
- 核对主相机 A/B/P 两路交接、RGBA16 浮点格式、P 的采样用途、UI 保留 A 与清零 B，以及交换链重建后的各输入 View 更新。
- 保留真实实现边界：LUT 当前透传；FXAA 默认关闭，实际亮度系数含 0.578；Combine UI 仅按 R/G/A 条件选色，其 pipeline 禁用混合。未将这些路径写成完整调色、标准 alpha-over 或已验证的运行时热切换。
- 输出核对 UNORM/SRGB_NONLINEAR 首选组合与 fallback，区分手工幂编码、格式转换和 Present 请求，不据此宣称显示完成。

### 独立编译与 CPU 条件检查

工作产物保存在本机临时目录 `%TEMP%/TechStackShow-piccolo-post-processing-17-20261025/`，不作为仓库发布内容。

- 使用 Piccolo 随附 glslangValidator，以 `-V100` 和原 shader/include 路径编译下列五份原始 GLSL，均返回 0；产物头均为 SPIR-V magic `0x07230203`、版本 1.0。这里只检查编译与文件头，未执行独立 SPIR-V 验证器或 GPU pipeline。

| Shader | SPIR-V 字节 | SHA-256 |
| --- | --- | --- |
| tone_mapping.frag | 2576 | `bcebe3c159152c2b0da6cefee619e285d4de431811a7ef81d585ad58979c3e2e` |
| color_grading.frag | 1156 | `d6ae13c837de266e447b99d75dae08ac2aa16943654ba576f265d807e999c3be` |
| fxaa.frag | 13220 | `f990c7f35b184311703dadb912dc566c6e2793bb3af736a052f60b943c91441a` |
| fxaa.vert | 1360 | `03ceddafaa9a1fe26a46998b3a450aa66d420854c2f83cf5559d5a5951712d28` |
| combine_ui.frag | 2016 | `9220fda094195d4ef5a08125d83aa05f10d386dfbe2b2d9bdb1405b1ee2c5fa3` |

- GCC 8.3.0 使用 `-std=c++17 -O0 -Wall -Wextra` 编译最小向量适配程序，编译与运行均返回 0。原 Uncharted2Tonemap 定义文本逐字保留（统一换行后匹配），只在外部适配向量和复现固定倍数、归一与幂运算。
- 灰度输入 0.18、1、4、16 分别约为 0.540080、0.877668、1.040841、1.099526；断言 `11.2/4.5` 的输出约为 1、强输入仍大于 1。未将 CPU 浮点取消误差或数值表当作 GPU 精度/显示测量。
- FXAA 仅检查给定亮度范围的提前返回条件；UI 仅检查透明零、黑色不透明、给定半透明值与纯蓝零 alpha 的最后分支。未模拟完整 FXAA 搜索、真实 ImGui 混合、附件转换或显示器。
- CPU 适配程序 SHA-256：`3a4833a13a211e8b01832bba55ff8feda68fa4258af7d62c16f147d4578230e5`；原曲线提取文件 SHA-256：`6fe2f8b708a6c493212db7087bcccb6eb7970418429aad084aa8cf7ec65e31c3`。

### 站点、导航与未验证项

- Hugo 0.158.0 构建返回 0，无 ERROR；本次工作树生成 1375 页、54 个静态文件，输出与缓存均放临时目录。此数量包含共享工作树的其他内容，不归因于本篇新增。
- 检查系列 21 篇正文与三个入口的 24 个生成页，1414 个本地链接/资源引用及 HTML 锚点均存在；series_order 连续为 0～20。
- 确认 16 下一篇指向 17、17 上一篇返回 16，17 末尾回到系列章节目录；三个入口均可进入 17。正文包含一份 Mermaid 图源码并引用共享 series-reading.css。仅做生成 HTML 静态检查，未宣称浏览器目视或 Mermaid 实际渲染已验证。
- canonical 目录、阅读入口、图形索引、16/07/GFX-01/GFX-02 回链及根系列路由已同步；09、10、12、13、14、18 六篇保持待写。
- 未运行完整引擎、GPU 后处理、UI 图形捕获或显示时序验证。未修改 Piccolo 源码、共享布局或 CSS；未暂存、提交或推送本轮内容。
