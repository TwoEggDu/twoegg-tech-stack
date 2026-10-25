# Piccolo 10：刚体与碰撞查询怎样参与角色运动

> 单篇提纲与证据记录；篇级状态由 `piccolo-engine-series-plan.md` 维护。

## 定位与边界

- 接 09 的控制器终点查询，回答障碍来自哪里、胶囊如何摆放、查询与物理解算是否同一条链。
- 类型：Piccolo 源码分析；先建立刚体/形状/查询/模拟/Transform 同步模型，再落到固定源码。
- 源码 `f5053707fed4d3f94d270a436fb0d3a8ae54e3e5` 只读；不修物理适配，不开展完整引擎构建或运行。
- 09 负责命令和期望位移，10 负责物理参与点，11 负责视图；不扩写 Jolt 全教程。
- 新正文 order=17，接 09；GFX-02 与后续五篇整体顺移，24 篇连续 0～23。12/13/14 仍待写。

## 正文提纲

1. 玩家不是必然由动态刚体推动：默认 Player 无刚体组件，墙有刚体；控制器胶囊是查询形状。
2. Level 创建 PhysicsScene；RigidBody postLoad、资源几何与局部/全局变换，BodyID 与 GObjectID 分开。
3. toShape 的 Box/Sphere/Capsule 映射与尺寸/缩放；创建复合形状、Static/NON_MOVING，actor_type/inverse_mass 未消费。
4. 胶囊沿 Jolt Y 轴，控制器旋转 X 90°并抬高 h+r；查询终点、AnyHit 与接受/整步拒绝。
5. raycast/sweep/overlap 的输入输出和调用事实；无效形状 false、miss 保留 out_hits、默认查询过滤与模拟层过滤分开。
6. 变换写入、缩放重建、延迟删除及 Update 后 drain；固定 1/60 每 tick 一步，非 accumulator，非角色变步长同一时钟。
7. 组件/对象顺序与读回边界；层和形状生命周期/非均匀缩放限制；断点和最小独立检查。

## 执行清单

- [x] 核对资源、刚体、Jolt 适配、控制器、查询与模拟/删除路径。
- [x] 用原函数文本、原数学与最小记录替身检查尺寸、步进/清理顺序；Static 创建以源码审查核对。
- [x] 写正文、图表、固定来源与运行验证表，并自审。
- [x] 更新正文导航、阅读入口、canonical plan 和根路由，保持三层职责。
- [x] Hugo 与引用/分页/链接检查，填写证据记录。

## 验证记录

2026-10-25 完成。参数记录、CPU 数学和函数适配不等同 Jolt 碰撞、完整引擎或 GPU 运行。

### 固定源码与边界审查

- 本地 Piccolo HEAD 为 `f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，源码 `git status --porcelain` 为空。正文 17 个 blob 来源（含许可证）均有对应文件，五段 C++ 节选忽略空白后与原文匹配。
- 核对 Level→PhysicsScene、刚体 postLoad、复合形状创建、BodyID、姿态同步、scale 重建和析构请求路径。创建函数固定 Static/NON_MOVING，未消费 actor_type/inverse_mass；这项是源码审查，没有编译真实 CreateBody。
- 读取实际 wall.object.json：Box 半尺寸 `(0.15,5,3.5)`；Player 有 Motor 的物理控制器配置，没有 RigidBodyComponent。查询胶囊不通过 AddBody 常驻世界。
- 核对随附 Jolt 的 CapsuleShape Y 轴约定、NarrowPhaseQuery 默认过滤与 ObjectLayerFilter::ShouldCollide=true；官网现行参数不同，因此具体调用以 vendored 固定源码为准。
- 在 engine/source 范围核对 raycast/sweep 只有声明/定义，isOverlap 为当前控制器调用；核对 miss 不清 out_hits、toShape 失败 false、裸指针查询形状未见对应释放、RigidBodyShape 复制只处理 Box。没有把函数存在或源码风险写成运行现象。
- 核对 Level 组件/Character/PhysicsScene 顺序、unordered_map 对象表、每 tick 一步 1/60 而非 delta_time accumulator、Update 后 drain 待删列表；与 Motor 的变步长分开说明。

### 独立程序

临时目录 `%TEMP%/TechStackShow-piccolo-physics-10-20261025/`，原函数、资源检查、程序、日志和站点输出均不加入仓库。

- 提取 toShape、PhysicsScene::tick、RigidBodyComponent::updateGlobalTransform 三个原函数，以及控制器 orientation/local Transform 片段；统一换行后逐字匹配原文件。
- 数学直接使用原向量/四元数/矩阵头与 vector3.cpp、quaternion.cpp、matrix3.cpp、matrix4.cpp、math.cpp。GCC 8.3.0，C++17/O0/Wall/Wextra、function/data sections 与 linker gc-sections，编译及运行返回 0。存在原矩阵头 unsigned>=0 恒真警告，不宣称零警告。
- Jolt Shape 构造器是参数记录替身，Update/BodyInterface 是事件记录替身；PhysicsScene 的获取和 RigidBodyComponent 的 remove/create 辅助函数也使用最小记录上下文，没有链接 Jolt 库或执行真实 Shape/Body 创建。
- 原 toShape：盒 `(0.15,5,3.5)` 与比例 `(2,3,4)` 得半尺寸 `(0.3,15,14)`；球 r=.5 与比例 `(1,2,3)` 得 r=1；胶囊 h=.7/r=.3 与比例 `(2,4,3)` 得 h=2.1/r=.9；未知类型返回 nullptr。
- 原局部姿态片段与数学：X 轴正 90°将 Y 转到 Z，单位胶囊中心 z=1，底部 z=0、顶部 z=2。不是实际 Jolt 包围盒或接触测量。
- 原刚体更新函数：非缩放调用 pose；scale 分支记录 queue→create。原 tick 接着记录 Update→RemoveBody→DestroyBody，并清空 pending；传 .5/.001 都记录实际步长 1/60。
- 未测试真实 overlap、raycast、sweep、碰撞层实际命中、动态模拟、物理形状释放量、完整组件/引擎和 GPU；正文保留后续验证表。

| 临时产物 | SHA-256 |
| --- | --- |
| physics_checks.cpp | `2e03038724ccb83465547e5ca9340683cf8558a2f62d60bda8fdf721a690f9fd` |
| to_shape.inc | `78f385535c1878ccb447a2efc344f5bd39d4cb6cd859b516d843b05c48c926b2` |
| physics_tick.inc | `bf6c8dc1e1d225d3d88cda82c252625e7d3ca321ddb1b2609dbe8f21e13d7aff` |
| rigidbody_update.inc | `c150f920a3db28b1d57e30002d5350eb00bae556cfc2eb19afd0ec915f74e6d1` |
| capsule_pose.inc | `46c82560e1d9c90cb97749fe0ff8345a5209018c5ec2e8977b0e87fc0fafcd53` |
| resource-checks.json | `e783abb690021ba11c1a916a12ff27da42141e64af53aa914d23497b4538df77` |

### 站点与导航

- Hugo 0.158.0 构建返回 0，无 ERROR；共享工作树生成 1381 页、54 个静态文件，包含其他任务内容，不全归因于本篇。
- 10 使用 series_order=17，接 09；GFX-02 与后续五篇共六份已有正文顺移一位，24 篇连续 0～23，原 URL 不变。
- 24 篇与三个入口共 27 个生成页检查，1623 个本地链接/资源与 HTML 锚点存在；全部前后篇分页成立，09→10→GFX-02→11，18 末尾返回目录。
- 两级入口、canonical 目录、根系列路由、03/06/09/11/18 回链或待写说明同步；12/13/14 三篇保持待写，下一篇建议 12 动画。
- 共享 series-reading.css 与一份 Mermaid 图源码通过 HTML 静态检查；未做浏览器目视或实际图形渲染验收。
- 未修改引擎、资源、共享布局/CSS；未暂存、提交或推送本轮内容。
