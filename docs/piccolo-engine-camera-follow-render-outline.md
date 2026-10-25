# Piccolo 11：相机怎样跟随角色，并把视角交给渲染系统

> 执行层提纲与证据记录；篇级状态统一由 `piccolo-engine-series-plan.md` 维护。

## 定位与边界

- 采用用户已确认的三篇规划，先写 11，再写 15 GPU 资源、16 材质光照与阴影；这是阅读顺序，不是运行时串行流水线。
- 文章以 Piccolo 为案例，先建立引擎无关的视点、观察与投影模型，再解释固定实现的数据流。主问题：角色状态怎样成为渲染使用的视角。
- 固定公开源码 `BoomingTech/Piccolo@f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，保持只读。不引入 Unity/Unreal 对照阶段。
- 第三人称为主线，第一人称、自由相机与编辑器只说明职责差异；完整输入/运动归 09/10，纹理上传和光照归 15/16。
- 沿现有阅读样式新增正文，series_order=17，原 17 篇顺序保持；主线编号 11 不表示 09/10 已完成。

## 执行清单

- [x] 核对 Player 参数、活动角色接线、组件更新、第三人称公式和顺序。
- [x] 核对 CameraSwapData 覆盖/交换/消费、RenderCamera 模式与 FOV 更新条件。
- [x] 核对 CPU 可见性、每帧数据、Pass 缓冲与 mesh.vert 的消费。
- [x] 完成正文、数据流与坐标变换图、条件数值练习和断点观察表。
- [x] 回写 canonical 规划、阅读入口与相关正文回链，保留其他未写篇目的状态。
- [x] 核对代码摘录、固定版本链接、数值推导；运行 Hugo 与生成页导航检查。

## 正文提纲

1. 角色、视点、V、P 与可见性职责；列向量下 `p_clip=P*V*M*p_local`，裁剪空间不等于屏幕像素。
2. 通过关卡 character_name 找活动 Character，CameraComponent 只更新活动角色所属组件；Character 缓存不等于最新 Transform。
3. 第三人称 offset、center、姿态旋转、LookAt；按实际顺序先读 R，再写 yaw*R，无自动平滑或避障。
4. 相机更新整条 optional 记录；初始化 FOV 可被之后的姿态记录覆盖，逻辑描述 reset 不等于 GPU 完成。
5. Motor/Editor 两种 view 来源；setFOVx 不重算 fovy，setAspect 才重算，说明实际视口调用入口。
6. 同一 P*V 分别用于主相机包围盒筛选和每帧渲染数据，追到 Pass ringbuffer 和 mesh.vert。
7. 固定 P=(10,20,0)、R/pitch 单位、h=3、v=1.5，推导 eye/target、平移、偏航读写顺序、FOV 条件。
8. 第一人称与自由相机对照，编辑器共享 RenderCamera 的证据，后续断点表。

## 证据入口

- engine/asset/objects/character/player/player.object.json；engine/asset/level/1-1.level.json。
- engine/source/runtime/function/framework/component/camera/camera_component.cpp、.h。
- engine/source/runtime/function/character/character.cpp、.h；framework/level/level.cpp；framework/object/object.cpp。
- engine/source/runtime/function/render/render_swap_context.h、.cpp；render_system.cpp；render_camera.h、.cpp。
- engine/source/runtime/core/math/math.cpp；render/render_resource.cpp、render_scene.cpp；render/passes/main_camera_pass.cpp；engine/shader/glsl/mesh.vert。
- engine/source/editor/source/editor.cpp、editor_input_manager.cpp、editor_ui.cpp。

## 验证记录

- 2026-10-25，回读源码 SHA 为上述固定提交；克隆工作区干净，未修改引擎。正文六段 C++/GLSL 摘录去空白后匹配原文件，23 个固定版本链接均对应本地源文件、目录或许可证。
- 核对 Level 创建活动 Character、GObject 组件筛选、Level 对象更新早于 Character::tick；明确 Character 状态缓存的读取边界，未把组件顺序写成全局系统顺序。
- 核对第三人称先计算视点再写偏航、没有平滑/避障查询；区分初始化 FOV 与逐帧姿态对整条 optional 的替换，覆盖结论保留相同发布区间的前提。
- 数值检查目录：`%TEMP%/TechStackShow-piccolo-camera-11-20261025/`。`camera_checks.cpp` 的 SHA-256 为 `84927259c54443def515b86060fdc5940766e14d6cacd01c5014cfbce4b8e519`。检查程序复现跟随公式与偏航先后，用固定源码原有数学库和 RenderCamera 验证 LookAt、共同平移、Motor 视图、视点恢复与 FOV 重算；另以 std::optional 最小结构验证整条赋值，不依赖完整引擎。
- Windows x64，GCC 8.3.0（Strawberry Perl 附带）。编译参数 `-std=c++17 -O0 -Wall -Wextra`；include 目录为 `engine/source` 与 `engine/3rdparty/json11`；编译源为检查程序、原 `render_camera.cpp` 和 `runtime/core/math/*.cpp`。第一次缺少 json11 include 路径，补齐后编译退出 0；原数学头出现六处 unsigned >= 0 警告，未改第三方/引擎源码处理。可执行文件运行退出 0，四行 PASS；重算 FOVy 为 42.9957°。
- Hugo v0.158.0 构建退出 0、ERROR 为零，生成 1370 页与 54 个静态文件；输出和缓存位于上述临时目录的 public/cache。此为整站构建结果，不代表引擎构建成功。
- 18 篇系列文章加两级阅读入口共 20 个生成页，1194 处站内链接、锚点与资源引用检查通过。GFX-02 下一篇与 11 上一篇双向对应，11 的末页按钮返回目录 chapters；新页沿用 series-reading.css，包含两段 Mermaid 图源。静态 HTML 中图源由已有 site.js 转换，不以 HTML 中是否预先存在 SVG 判断渲染成功。
- 系列 metadata 连续唯一为 0～17，原文章顺序保留；阅读入口、06/07、GFX-01/02 回链与根系列汇总已更新。canonical 表登记 11 已写，09—18 的其余九篇仍待写；15/16 接续顺序只代表阅读与写作安排。
- 未运行 Piccolo 世界、输入、CameraComponent 或 GPU；实际跟随、模式切换、断点、显示延迟、GPU 捕获和浏览器目视仍待验证。未修改共享模板/CSS，未暂存、提交或推送。
