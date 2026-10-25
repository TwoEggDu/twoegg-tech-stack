# Piccolo 12：一段动画怎样成为骨骼姿态与网格蒙皮

> 单篇执行提纲与证据记录；篇级目录由 `piccolo-engine-series-plan.md` 维护。

## 定位与边界

- 类型：Piccolo 源码分析；先区分片段采样、局部姿态、层级派生、逆绑定修正、顶点权重与对象变换。
- 固定只读源码 `f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`；以默认 Player 的一段动画走到主相机 mesh.vert。
- 复用 09/10 的角色运动、15 的 GPU 资源、07 的提交和 18 的拾取限制。不扩写动画状态机、IK 或完整通用混合系统。
- 12 插入 11 后，order=20；15/16/17/18 四篇顺移，总 25 篇连续 0～24。13/14 保持待写。

## 正文提纲

1. 对象整体运动与骨骼局部变形分别负责什么；S=当前骨骼派生变换×逆绑定，M×ΣwS×p 的最小模型。
2. Player 的 clip、骨架、通道映射、mask 和 mesh_bind 资源，固定资源数量与编号约定。
3. AnimationComponent phase 0 更新/循环，frame_position 未用于这里；AnimationManager 缓存/准备数据与 Skeleton 实际只应用 clip0/weight1 分开。
4. reset bind pose、线性采样/nLerp、相对叠加、父先子后的层级传播、inverse T-pose 输出与合法输入条件。
5. Mesh dirty 门槛、identity 槽0、index+1 与顺序契约，渲染实体/可见节点/每实例 palette 交接。
6. mesh_bind 四权重与索引、上传归一和 index_index/vertex_index 的区别；默认资源顺序索引恰好匹配，不泛化。
7. row_major buffer、set0/binding2 palette、set1/binding0 bindings、gl_InstanceIndex/gl_VertexIndex 和 shader 最终使用；阴影与拾取分别核对。
8. 原 Skeleton/Node/数学的独立数值与实际资源结构检查、Shader 编译、后续运行观察表。

## 执行清单

- [x] 核对动画采样、骨骼、Mesh/渲染交接、顶点 Shader 和实际资源。
- [x] 建立最小 CPU/资源与 Shader 独立验证，严格说明加载桥接和 GPU 边界。
- [x] 写正文、图表、源代码节选并自审。
- [x] 更新导航、元数据顺序、canonical plan 与根路由。
- [x] Hugo、源码引用和生成页/分页检查，补证据记录。

## 验证记录

2026-10-25 完成。CPU 骨骼与 Shader 编译不能代替引擎动画播放或 GPU 蒙皮。

### 源码与实际资源

- 本地源码 HEAD 为 `f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，源码工作树只读且 `git status --porcelain` 为空。21 个固定 blob 链接均有本地对应文件（含许可证），七段 C++/GLSL 节选忽略空白后匹配原文。
- 核对 phase 更新、frame_position 未消费、管理器缓存与数据复制、clip0/weight1 应用、相对绑定姿态变化、层级传播、inverse T-pose 及 palette/索引契约。
- 实际骨架 58 根、映射 58 项且值域 0～57，名称逐项对应；flat 与父先子后结构检查通过。片段 total_frame=23、node_count=58，各通道三种键长度分别为 22 或 23；mask 的 enabled 有 213 项，不据此推导骨架数。
- 实际 mesh_bind 的顶点、索引和绑定条目各 55500；索引正好 0～55499 顺序展开。正权重关节索引值域 3～58，权重总和约 0.999896～1.000001。
- 核对 Mesh 外层 dirty、单位槽0与按数组追加、RenderEntity/可见节点、主相机 palette 动态偏移、每实例1024槽、四权重缓冲与行主序。上传绑定以 index_index 写入而 Shader 按 gl_VertexIndex 读取，默认顺序索引满足对应，不宣称一般共享顶点布局均正确。
- 主相机和两种阴影使用 model_position，拾取最终位置仍用 in_position。保留混合批次条件、零有效权重、非均匀缩放法线与短通道共享帧索引的限制；不修改源码。

### 原 Skeleton/Node 与数学的 CPU 检查

产物在 `%TEMP%/TechStackShow-piccolo-animation-12-20261025/`，不加入仓库。

- GCC 8.3.0、C++17/O0/Wall/Wextra、function/data sections 与 linker gc-sections；直接编译 skeleton.cpp、node.cpp、utilities.cpp 与 vector3.cpp/quaternion.cpp/matrix3.cpp/matrix4.cpp/math.cpp 八份原引擎源文件，另用仓库 json11 解析 JSON。编译与运行返回 0；原文件有类型比较、未用局部变量等警告，不宣称零警告。
- 独立读取桥接逐项构造原 SkeletonData、AnimationClip、AnimSkelMap。RawBone 先值初始化，缺失整数字段取 0；Transform 缺省位置0、旋转单位、scale1；tpose_matrix 缺省单位，仅覆写存在字段。没有调用引擎生成的 Serializer 或 AnimationManager，也没有替换 Skeleton/Node 算法。
- 默认真实资源 phase=.5，exact_frame=11，输出58矩阵，补槽0为59。原数学按四权重公式计算第0顶点：绑定 `(0.525734,-0.044840,1.169006)`，关节34/33、权重 .511982/.488018，结果约 `(0.215444,-0.343468,1.187255)`。未乘对象/相机矩阵，未验证 GPU 精度或画面。
- phase0 与 phase.5 输出不同；相同 phase 把 blend_weight 改全0，或在输入增加第二片段，原 applyAnimation 输出不变。只验证 Skeleton 消费范围，没有执行管理器的多片段权重准备。
- 两骨骼合成例验证逆绑定抵消、父平移传播、两帧位置插值与重复 apply 不累加上一帧；phase wrap 另以相同表达式检查 .9+.2→.1，不称为运行完整 AnimationComponent。

| 临时产物 | SHA-256 |
| --- | --- |
| animation_checks.cpp | `a4bacff6c53e86fce5587c3b217b5adb34bed5593f61fb8382b0d580d55a304f` |
| resource-paths.json | `fcabd60b787141a97454e4d2fc2256e45c41baadec5c39a2f47a054fe86f7dd8` |
| resource-checks.json | `df5c9833467eeffafc21e45e2023c34b7a8dfabefed295b95df2262a4cb3d6e8` |

### Shader 与站点

- Piccolo 随附 glslangValidator 以原 include 路径和 `-V100` 编译四份原顶点 Shader，均返回0；SPIR-V magic `0x07230203`、版本1.0。未执行独立 SPIR-V 验证器或 GPU pipeline。

| Shader | SPIR-V 字节 | SHA-256 |
| --- | --- | --- |
| mesh.vert | 10348 | `61f91daebce87f389cc6dc3fe7acaed69ccb83db76b7053bcfdb019132a74641` |
| mesh_directional_light_shadow.vert | 6936 | `1eb0e11d61c30c56e81379b584e1f27000def4d7a678a6ac0f444ba2a246c18f` |
| mesh_point_light_shadow.vert | 6316 | `3d11965a04d68e8c980d8e435ab3e54b83942d6753d13e580fe2c15704a54525` |
| mesh_inefficient_pick.vert | 6844 | `e398fc8754396a707e1869ec58bb8d8ca5bf1e36da776f7fd0526e580f9b6aa6` |

- Hugo 0.158.0 构建返回0，无 ERROR；共享工作树生成1382页、54静态文件，含其他任务内容，不全归因于本篇。
- 25篇 series_order 连续0～24，12位于11后、15前；15/16/17/18四篇顺移，URL不变。全系列分页及末尾回目录检查通过。
- 检查25篇与三个入口共28个生成页，1691个本地链接/资源与HTML锚点存在；三个入口及07/09/10/11/15/18正文链接12。根路由只更新系列概况，13/14仍待写，下一篇建议13粒子。
- 一份Mermaid图源码与共享series-reading.css引用通过静态检查；未做浏览器目视或实际Mermaid渲染验收。
- 未运行完整引擎、动画播放、资源生成式反序列化、GPU上传/蒙皮、阴影或拾取画面；未改引擎、资源、共享布局/CSS，未暂存、提交或推送。
