# Piccolo 09：从一次按键到角色位移

> 执行层提纲与证据记录；篇级目录以 `piccolo-engine-series-plan.md` 为准。

## 定位与边界

- Piccolo 源码分析：按键事件 → 命令位 → 速度/方向/期望位移 → 控制器目标 → Transform/Character/渲染交接。
- 固定只读源码 `f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`。用户已确认写作范围，沿既有系列直接落地。
- 复用 03 逐帧、06 变换与 11 相机；只说明控制器的终点重叠查询，不展开 10 的物理实现，也不替代 12 动画和 14 脚本。
- 09 插入 08 之后，GFX-02 与 11/15/16/17/18 顺移一个 series_order；总 23 篇，连续 0～22。10/12/13/14 保持待写。

## 提纲

1. 区分输入事件、持续命令、期望运动、受约束目标与已消费位置。
2. 游戏/编辑模式与 focus 标记；GLFW 回调、PRESS/RELEASE、REPEAT；jump 不是严格单帧脉冲。
3. 实际 logicalTick 先 World 后 Input，pollEvents 在帧尾；只对指定事件到达点推导时序。
4. 活动 Character 的对象 ID 筛选，Motor 对应对象才执行；定义/实例与 Lua 会影响运行参数。
5. 默认 move_speed/ratio/acceleration；斜向归一、相反键、无命令保留方向减速、free camera 与跳跃冻结。
6. 期望位移公式；none/physics/invalid 三路；physics 在终点 overlap 时整步退回，无 sweep/滑墙保证；Z 平面落地特殊条件。
7. setPosition 写 next 与持久字段；Character 在组件之后取 target；默认 Transform/Animation/Mesh/Motor/Camera/Lua 次序与本帧/下帧表。
8. 原函数 CPU 适配、资源核对及后续断点表；与实际键盘、Jolt、GPU 运行分开。

## 执行清单

- [x] 读取输入、主循环、Player/Level、Motor、控制器、Transform、Character 和相机消费源码。
- [x] 独立编译原运动/输入函数及数学实现的最小适配，验证参数、边界与数值。
- [x] 写正文、图表、固定提交引用，完成自审。
- [x] 更新 canonical 目录、根路由、入口、相关正文与章节顺序。
- [x] Hugo 构建、源码节选与生成页链接/分页检查，补证据记录。

## 验证记录

2026-10-25 完成正文与导航；独立输入/运动函数不等于真实 GLFW 事件、Jolt 场景或完整引擎验证。

### 固定源码与资源

- 本地 Piccolo HEAD 为 `f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，源码树 `git status --porcelain` 为空；未修改引擎或资源。
- 正文 20 个固定提交 blob 链接均有本地文件（含许可证），九段 C++ 节选忽略空白后与原文件匹配。另引用 GLFW 官方输入文档，仅说明事件/持续状态与回调时机的边界。
- 读取真实 1-1.level.json 与 player.object.json：活动角色 Player，重力 `(0,0,-15)`；合并实例覆盖后的顺序为 Transform/Animation/Mesh/Motor/Camera/Lua。Player 实例的绕 Z 旋转覆盖与定义的单位旋转分开说明。
- 核对速度参数 2/1/2/2/2、定义跳高 1；Lua 示例还会依据 is_moving 写跳高 10/5，因此数值练习固定参数，不冒充运行配置实测。
- 源码审查保留 jump 按回调清除、focusMode 与世界/Input 顺序、活动角色 ID 筛选、自由相机残留减速、空中方向冻结、终点重叠退回、Z 判定重复加位移等边界。
- 帧表限定默认组件顺序、P0/P1、dirty 与交换条件，分别追 Transform current/next、Character 和 Mesh/Camera 消费；未将源码时序表称为实际帧捕获。

### 独立 C++ 适配

工作目录 `%TEMP%/TechStackShow-piccolo-input-09-20261025/`。所有提取、程序、编译日志、结果和站点输出均为临时产物。

- 提取九个原函数：InputSystem 的 onKey/onKeyInGameMode/tick，Motor 的五个运动计算函数，CharacterController::move。统一换行后逐字检查；GameCommand、ControllerType、JumpState 也由原定义提取。
- 向量、四元数、矩阵和 Math 使用原头文件与 vector3.cpp、quaternion.cpp、matrix3.cpp、matrix4.cpp、math.cpp。GCC 8.3.0、C++17、O0、Wall/Wextra、function/data sections 与 linker gc-sections 编译，编译和运行均返回 0；原矩阵头存在 unsigned>=0 恒真的警告，不宣称零警告。
- 最小替身提供 WindowSystem 的 focus 状态、全局 World/Physics 返回、Motor 所需字段与类声明；输入 tick 的鼠标角计算/clear 在替身中为空。PhysicsScene::isOverlap 由测试给定 true/false，不执行 Jolt、真实胶囊或场景碰撞。
- 检查 PRESS/RELEASE 组合、编辑器模式拒绝游戏输入、focus invalid 位、jump 跨 tick 保留及后续 REPEAT 清除。
- 检查 W 初始比率 0→0.2、单位朝向位移 -0.04Y，松键后 0.2→0.1 与 -0.01Y，斜向单位长度、相反键零方向、Shift 分支比率 1.2、free 命令残留 -0.16Y；实际 Player 旋转下同一步变为约 +0.04X。
- 检查固定 h=1/g=15 的起跳速度 √30 与后续减 1.5，none 目标直接相加、physics 假定重叠时拒绝/不重叠时接受，以及当前 z=1/dz=-0.6 的 Z 平面特例钳到 0。
- 未运行完整 tickPlayerMotor、Character/组件调度、Lua、窗口或真实物理；表内各值不充当完整运动或画面验收。

| 临时产物 | SHA-256 |
| --- | --- |
| movement_checks.cpp | `b817d33160be17137ced0c4fea1b98780914a96cc036f6dfea04ec9a572c3041` |
| motor_functions.inc | `5bd66f30ade9e3c813670d7db55b46bf5c8a92d183a3c39b370011fe310937a8` |
| input_functions.inc | `4f3d7e858fcdfcffd9ba9dc04d5a8902510c033715cd01a730bba2ea8d64a035` |
| controller_move.inc | `649dab7d9a4c1765d769ac6c8e0bd6d36e724e4f68ff9980d670b4a0269c7a16` |
| resource-checks.json | `a2dfcedc93d9800e685efe8940b2e6d665d5859d71f71eb3ec089e0e92dbf510` |

### 站点与导航

- Hugo 0.158.0 构建返回 0，无 ERROR；共享工作树生成 1378 页、54 个静态文件，包含其他任务内容，不全部归因于本文。
- 09 使用 series_order=16，位于 08 后；GFX-02 与后续 11/15/16/17/18 六份已有正文的顺序字段各加 1，现 23 篇连续为 0～22，URL 保持不变。
- 检查 23 篇与三个入口共 26 个生成页，1555 个本地链接/资源引用及 HTML 锚点存在；逐一核对全部系列分页，08→09→GFX-02→11 成立，末篇 18 回目录。
- 两级阅读入口、canonical 目录与根系列路由已更新；03、06、08、11 回链到 09，18 的剩余选题说明同步；10/12/13/14 四篇保持待写。
- 一份 Mermaid 图源码与共享 series-reading.css 引用通过生成 HTML 检查；未做浏览器目视或实际 Mermaid 渲染验收。
- 完整引擎构建、实际输入/角色运动、Jolt 查询与 GPU 画面待验证；未修改共享布局/CSS，未暂存、提交或推送本轮内容。
