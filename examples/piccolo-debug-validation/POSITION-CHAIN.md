# Wall_1：Transform 到渲染表示的实际调试

2026-10-26，实验身份 `piccolo-position-chain-20261026`。复用 [Debug 基线](README.md) 的固定提交加两处兼容补丁产物，EXE/PDB/ini SHA-256 均回读一致；没有再改或编译引擎。

## 这次验证到哪里

在第一次呈现后找到 Wall_1，在下一次 `logicalTick` 之前通过 CDB 调用一次真实 `setPosition`。从 Transform 两份缓冲走到 Mesh 描述、交换队列、列表内 RenderEntity 与主相机可见节点；该迭代的提交/呈现返回成功。入口为受控调试调用，没有操作鼠标拖轴或字段面板，没有保存场景、捕获本次 GPU 帧或比较像素。

实测身份为 GObjectID=2、part=0、instance_id=5、mesh_asset_id=5、material_asset_id=2。所有地址、ID和容器下标须在自己的进程重新确认；资源 ID 与对象/实例 ID 的相同数字只是巧合。

## 如何复现

在独立实验副本中使用已匹配的 Debug exe/PDB，工作目录为 `source/bin`。在交互终端运行 CDB，将 `-cf` 指向本目录 `position-chain.cdb`，`-logo` 指向自己的日志路径；完整启动方式见基线 README。原始实验位于 `%TEMP%/PiccoloDebug20261026`。

1. 脚本在第一次 `vulkan_rhi.cpp:494` 暂停，先查询活动关卡的对象表。原始日志中 `m_gobjects[2].second._Ptr->m_name` 为 Wall_1；这里的下标来自已观察的容器条目，不代表所有版本都按对象 ID 排序。
2. 展开该对象的 `m_components`，按类型名确认 Transform、Mesh、RigidBody。将 Transform 和 Mesh 的地址分别放入 `$t0`、`$t1`。通过渲染场景的 `m_mesh_object_id_map.Where(e => e.second == 2)` 找到实例5，再核对对应列表元素与原矩阵。
3. 设置一次性 `engine.cpp:70` 断点，继续到下一次 `logicalTick` 调用前。此时已存在旧渲染实体，且尚未进行本轮世界/组件 tick。本次起点 current/next=1/0，两份位置均为 P0，dirty=false。
4. `.dvalloc 0x1000` 分配参数暂存页，把返回地址放入 `$t9`，再用 `ef @$t9 2.94597 -0.38554 0.0` 写入 Vector3。只把它用作入参，不直接改交换数据或渲染实体。
5. 在 `transform_component.cpp:21` 设一次性断点，记录 setter 已完成的写入，随后调用并执行：

```text
.call /v PiccoloEditor!Piccolo::TransformComponent::setPosition(@$t0, @$t9)
g
```

方法的第一个参数是 `this`，第二个参数是 Vector3 引用的地址，详见 [Microsoft .call](https://learn.microsoft.com/en-us/windows-hardware/drivers/debuggercmds/-call--call-function-)。在这里选择主线程安全点调用仅写字段的 setter；不要在任意线程、锁持有现场或崩溃上下文照搬。调用完成后 CDB 恢复原上下文并暂停，先核对 current 仍为 P0、next 和 m_transform 已为 P1、dirty=true。

6. 根据本进程的真实 ID 调整 `position-chain-watch.cdb`，然后通过 CDB 的 `$><` 读取此文件。该文件整理自实际执行并最终有效的断点定义，已替换掉日志中失败的可见性条件；它依赖本例 ID=2/5、槽1与 `$t0/$t1`，不是自动查找对象的通用脚本。
7. 断点顺序为 Transform 交换与复制 → Mesh 描述/恢复局部矩阵 → addDirtyGameObject → 交换前后 → processSwapData 的已有实体分支/赋值后 → 清队列 → 可见性测试与节点 → 本轮 present 返回。交换前会暂停，检查逻辑槽1的一条 G=2 描述、一个 part 和矩阵，再继续。

本机 CDB 的 `dx` 能展开容器与引用，但 `@@c++` 在可见性循环中将 `entity`、`temp_node` 视为指针，因此条件用 `entity->m_instance_id` 和 `temp_node->node_id`。原始日志保留第一次用点运算符失败、再修正的过程，不能将该调试器条件错误当成引擎故障。`.call` 的人工栈也不能作为原生编辑器入口的调用链证据。

## 实际记录

| 阶段 | 结果 |
| --- | --- |
| 输入 | P0≈(1.94597,-0.38554,0)，P1≈(2.94597,-0.38554,0) |
| setter 返回 | current/next=1/0；缓冲0=P1、缓冲1=P0，原始字段=P1，dirty=true，scale dirty=false |
| Transform tick | 索引0/1；编辑器复制后两份都是P1 |
| Mesh | 描述矩阵为M1，原始part局部矩阵恢复单位矩阵，入队后dirty=false |
| 交接 | 逻辑/渲染槽1/0→0/1；旧渲染槽七optional全nullopt，逻辑槽1仅一条G=2、part0描述 |
| 消费 | 已有实体、网格、材质标志均true；实例/网格/材质ID保持5/5/2，列表内实体赋值后为M1 |
| 清理 | 消费后的槽1对象optional变为nullopt |
| 可见性 | 主相机列表有一条node_id=5；节点模型矩阵指针与实体矩阵地址相等，读出新位置 |
| 后续到达点 | 同一轮queue submit与present均VK_SUCCESS，m_frame_count=2，主线程0x1afc4 |

`position-chain-receipt.json` 保存矩阵和状态。脚本从本次日志提取矩阵输出，检查 Mesh、队列、临时实体、列表实体四份M1相同；与原矩阵按1e-5比较，仅x平移元素改变。该精度来自CDB的十进制显示，不冒充16个元素的原始二进制转储。

`debug-position-chain.log` 保留实际命令、断点与状态，仅归一化本机实验根/用户路径。`position-chain.cdb` 为本次启动命令文件；`position-chain-watch.cdb` 是从日志提取的最终断点定义。三个文件的字节指纹见独立回执，不改写之前首帧实验的回执。

实验到本轮present后由调试器 `q` 结束，不作为正常窗口退出记录。部署关卡文件与固定源码资源字节一致；原阅读克隆、EXE/PDB/ini保持原身份。

## 仍未验证

较晚setter、多个写入者/同对象多记录、其他置脏来源、刚体姿态、编辑器拖轴/面板、保存重载以及后续所有帧仍未覆盖；没有保存变换后包围盒数值，也没有捕获对应Draw、GPU读取值或像素。进入主相机可见列表不代表通过遮挡测试或一定产生特定屏幕像素。原时序反例继续标为源码推导。
