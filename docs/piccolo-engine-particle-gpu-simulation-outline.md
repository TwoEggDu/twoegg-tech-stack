# 13 粒子发射器怎样进入 GPU 模拟与绘制：执行提纲

## 定位与边界

- 类型：Piccolo 源码分析；问题是对象上的发射器怎样生成粒子、复用槽位，并将模拟结果交给绘制。
- 先解释发射器、粒子池、存活/空闲索引和绘制紧凑数组，再进入固定源码。读者目标是能定位状态和设计观察，不要求先学完整特效编辑器。
- 基线：`BoomingTech/Piccolo@f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，公开源码，仅只读核对。
- 复用 04/06 的组件与交换、07 的提交、11 的相机、15 的资源和 17 的场景颜色；不扩展完整特效资产管线或替换引擎实现。
- 13 插在 12 后，order=21；15/16/17/18 顺移至 22～25，共 26 篇。14 Lua 保持待写。

## 正文提纲

1. 从“有发射器却没有粒子”出发，解释描述、槽位、索引和绘制数据的差别。
2. ParticleComponent 的创建/tick/dirty，ParticleManager 到 RenderSwapData，渲染端按批次建池；ID 与下标约定、替换而非追加的边界。
3. 容量、Particle 布局、alive 双表/dead 表、counter 和间接参数，区分模拟池与 renderParticles。
4. 按实际 CPU 调用顺序说明先绘制/提交，再复制深度法线和模拟，结果供后续绘制。
5. kickoff → emit → simulate 的状态变化、flap 翻转、256 线程组及间接 Dispatch；用小池推导复用与死亡边界。
6. 点发射与图像平面发射；固定 dt、生命来源，CPU/Shader 类型不匹配和共享计数竞争不泛化成精确发射频率。
7. 屏幕空间深度/法线碰撞的可见面限制，与 Jolt 区分。
8. compact render buffer、计数回读、四顶点实例 billboard、纹理着色和混合；接到 tone mapping。
9. 独立布局/字节与状态实验、五份 Shader 编译、未来引擎观察表，证据分层。

## 执行清单

- [x] 核对组件、交换、GPU 资源、三个计算 Shader 与两份 billboard Shader。
- [x] 独立布局/状态实验与 Shader 编译。
- [x] 写正文、图表和源码节选并核对引用。
- [x] 更新阅读入口、正文关联导航、canonical plan、根路由和排序。
- [x] Hugo、源码/生成页/分页检查，记录真实验证范围。

## 验证记录

2026-10-25 完成。源码与独立实验不充当完整引擎或 GPU 运行证明。

### 固定源码与资源

- 本地源码 HEAD 为 `f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，只读核对后源码工作树 `git status --porcelain` 为空。
- 21 个固定 blob 引用均有本地文件，六段 C++/GLSL 节选忽略空白后匹配原文。类型1为 logo 图像平面发射，不是任意三角网格采样。
- 核对组件创建/tick/dirty、编辑模式筛选、整批创建和 ID/下标、固定池与双存活索引、三个计算阶段、间接参数、counter 回读、billboard 与后处理顺序。记录先绘制/提交、再复制图像和模拟、供后续 Draw 消费的 CPU 路径，不充当 GPU 时间线。
- 实际全局 JSON：emit_gap=10、emit_count=100000、time_step=.004、max_life=.04、gravity.z=-30；实际组件：type1、life=(1.5,0)、acc.z=-2.5、size=(.02,.02,0)。核对 UBO 硬编码 xemit_count、全局 max_life 未用于寿命计算、int gap/float emit_delta 契约与 life.z 非原子共享写。
- spark.HDR 源文件在 engine/asset 下；logo 源文件在 engine/source/editor/resource 下，editor CMake 将 resource 目录复制至输出。首次独立检查误把 logo 当作 engine 根目录源文件，失败后按实际 CMake 路径修正；未验证构建输出或运行目录资源。
- 核对屏幕深度/法线碰撞、寿命严格负值回收、紧凑输出、普通实例 Draw、RGB/alpha 消费与混合。批次替换/计数重置、队列同步、坐标一致性、排序和实际增删/重载保留运行边界；未修改引擎。

### 独立 CPU 与资源检查

产物位于 `%TEMP%/TechStackShow-piccolo-particle-13-20261025/`，不加入仓库。

- `run_cpu_checks.py` 从原 particle_pass.h 提取 uvec4、UBO、Particle、IndirectArgumemt 和 ParticleCounter 声明，包含原 particle_desc.h 与其数学头文件；没有编译或运行 ParticlePass、VulkanRHI 或 GPU kernel。
- GCC 8.3.0，参数为 C++17/O0/Wall/Wextra/Wno-unused-parameter，include 为 engine/source 与 engine/3rdparty/json11。编译和独立可执行程序返回0。原 matrix3.h 有 unsigned 比较警告，不记为完整引擎或零警告构建。
- 静态断言：Particle=64、counter=16、UBO=80、EmitterDesc=160；Particle 的 life/velocity/color 偏移12/16/48，EmitterDesc 的 life/color 偏移128/144，第二份间接参数偏移16。Argument 的主机 sizeof=36；没有据此声称所有 GPU block 大小/对齐已运行验证。
- int10 的四字节经 memcpy 按 float 解释为约 `1.40129846e-44`。独立无碰撞标量例：vz=2.5、acc=-32.5、dt=.004，更新后 vz=2.37、z=.00948；不是默认随机发射轨迹实测。
- 自写串行四槽教学模型检查两次存活保留、第三次严格负值回收、槽位重新使用以及存活/空闲守恒；不执行原并行 Shader，不验证原子顺序或共享发射门槛。
- 组数边界：0→0、1→1、256→1、257→2、100000→391、300000→1172。核对真实 JSON 与两种纹理的源路径存在，不执行引擎反序列化/纹理上传。

| 临时产物 | SHA-256 |
| --- | --- |
| particle_layout.cpp | `b8093ab91a74fa6f62d22665e852016803d611351ae3fccd83a7a964f951f523` |
| run_cpu_checks.py | `cdcd1671d2a44fe72f2e1d0b180c1a5d202ff2b1a8cb1b83b78c1af059ba48c3` |
| cpu-resource-checks.json | `77e0c65fb17b49366b0e52a47eda53051ddd7af05db36d7a30825be4a70d5337` |

### Shader 与站点

- 使用 `engine/3rdparty/VulkanSDK/bin/Win32/glslangValidator.exe`，参数 `-I<engine/shader/include> -V100 -o <TEMP输出> <原Shader>`；下列五次编译均返回0。核对 SPIR-V magic `0x07230203` 与版本1.0；未运行独立 SPIR-V 验证器或 GPU pipeline。

| Shader | SPIR-V 字节 | SHA-256 |
| --- | --- | --- |
| particle_kickoff.comp | 2920 | `a07a87ddcacb61b14142c090b053d15a68ba11dfbb38e7737b28592eae088236` |
| particle_emit.comp | 13096 | `b5d396bcee93d490b6600d00f618c6753b3a745809c9cd4e5f2baa88d1d3c46e` |
| particle_simulate.comp | 12684 | `6f4d30d8e961a85479d77b047cf81304d067117950e8457d7df37355a77c09d3` |
| particlebillboard.vert | 5428 | `0b7810aca66c8bc0476ebf458ef59474f4e7707a73839cac7708163bfb19c6ec` |
| particlebillboard.frag | 1292 | `77af83f5806ad90a40281528ba2b26bb180a3ff9bbad2813c3034463d4c4cf4f` |

- Hugo 0.158.0 使用临时 destination/cache 构建返回0，无 ERROR；共享工作树生成1397页、54静态文件，其中含其他任务内容，不能全部归因于本篇。
- `final_checks.py` 检查26篇的 series_order 连续0～25；13位于12后、15前；15/16/17/18顺移至22～25，URL不变；全部上一页/下一页与末篇回目录检查通过。
- 检查26篇与三个入口共29个生成页，1765个本地链接/资源/HTML锚点存在；阅读首页、引擎首页、游戏图形入口及07/09/10/12/15/17/18正文均能到达13。入口13条目及状态说明各一份，14为唯一待写主线，根路由只更新系列概况。
- 一份 Mermaid 图源码、共享 series-reading.css 引用通过静态检查；未做浏览器目视或 Mermaid 实际渲染验收。
- 未运行完整引擎、粒子发射/GPU模拟/碰撞/绘制、Validation Layer 或图形捕获；未改引擎/资源/共享布局与CSS，未暂存、提交或推送。
