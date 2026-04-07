---
title: "MMO 大世界架构：AOI 算法（九宫格 / 十字链表）、空间分区、跨服通信设计"
slug: "game-backend-depth-02-mmo-aoi"
date: "2026-04-05"
description: "深度拆解 MMO 大世界服务端的核心问题：AOI 算法选型（九宫格与十字链表的真实实现差异）、地图空间分区策略、以及跨进程玩家迁移协议设计。"
tags:
  - "游戏后端"
  - "MMO"
  - "AOI"
  - "网络同步"
  - "空间分区"
series: "游戏后端基础"
primary_series: "game-backend"
series_role: "article"
series_order: 56
weight: 3056
---

## 问题空间

一张 MMO 大世界地图上同时有 5000 名玩家在线。如果服务端要把每个玩家的位置变化广播给所有其他玩家，每秒需要发送 5000 × 4999 ≈ 2500 万条消息。这在任何架构下都是灾难。

现实中，玩家不需要知道 3 公里外发生了什么。他们只需要知道**视野范围内**发生的事情。这就是 AOI（Area of Interest，兴趣区域）要解决的核心问题：**对每个玩家，实时维护一个"需要感知的对象集合"，只向该玩家推送集合内发生的事件。**

AOI 系统需要高效回答两类查询：
1. 实体 E 进入/离开哪些玩家的感知范围？（触发订阅变更）
2. 实体 E 移动时，哪些玩家需要收到其新位置？（触发位置同步）

这两类查询的频率极高——在一个 60Hz 服务端上，5000 名玩家每秒触发 300,000 次位置更新，每次都可能触发 AOI 范围重算。算法的常数系数决定了能否扛住负载。

---

## 抽象模型

### AOI 的数学本质

AOI 问题可以抽象为：给定一组点（玩家/实体），对于每个查询点 Q，找出距离 Q 小于半径 R 的所有点。

这是经典的**范围查询**问题。朴素解法 O(N²) 无法接受，游戏工程中演化出两种主流实现路径：

- **空间哈希 / 格子划分**：九宫格 AOI 的基础思路
- **排序列表扫描**：十字链表 AOI 的基础思路

两者各有权衡，对不同游戏特征的适配性不同。

### 感知模型的变体

AOI 不一定是圆形或方形感知区域。常见变体：
- **固定半径圆形**：最常见，实现简单，适合大多数 MMO
- **扇形视野**：FPS/TPS 服务端，有视角方向遮挡
- **优先级分层**：近处的实体发送高频率位置，远处发送低频率（Unreal 的 NetRelevancy 机制）

本文重点讨论固定半径 AOI，这是 MMO 大世界的典型场景。

---

## 具体实现

### 九宫格 AOI

#### 核心思想

将地图划分为等尺寸的格子（Cell）。每个格子维护一个实体列表。当实体位置变化时，检查其所在格子是否变更。

"九宫格"得名于感知范围计算方式：以实体所在格子为中心，取周围 3×3 的格子集合，集合内所有实体互相可见。

```
+---+---+---+
| 1 | 2 | 3 |
+---+---+---+
| 4 | E | 5 |    E 所在格子 + 8 个相邻格子 = 感知范围
+---+---+---+
| 6 | 7 | 8 |
+---+---+---+
```

#### 数据结构

```python
# 伪代码
class Cell:
    entities: Set[Entity]

class AOIManager:
    grid: Dict[Tuple[int,int], Cell]  # (col, row) -> Cell
    cell_size: float  # 格子边长

    def get_cell(self, x: float, z: float) -> Tuple[int,int]:
        return (int(x / self.cell_size), int(z / self.cell_size))

    def get_neighbors(self, cx: int, cz: int) -> List[Cell]:
        # 返回 (cx, cz) 周围 3x3 的格子
        result = []
        for dx in [-1, 0, 1]:
            for dz in [-1, 0, 1]:
                cell = self.grid.get((cx+dx, cz+dz))
                if cell:
                    result.append(cell)
        return result
```

#### 实体移动处理

```python
def on_entity_move(self, entity: Entity, new_x: float, new_z: float):
    old_cell = entity.cell
    new_cell_coord = self.get_cell(new_x, new_z)
    new_cell = self.grid[new_cell_coord]

    if old_cell == new_cell:
        # 同格子内移动，只广播位置更新，无需重算 AOI
        broadcast_to_watchers(entity, new_x, new_z)
        return

    # 格子变化：需要计算进入/离开的感知集合
    old_neighbors = set(self.get_neighbors(*old_cell.coord))
    new_neighbors = set(self.get_neighbors(*new_cell_coord))

    entered = new_neighbors - old_neighbors  # 新进入视野的格子
    left = old_neighbors - new_neighbors     # 离开视野的格子

    for cell in entered:
        for other in cell.entities:
            notify_enter(entity, other)  # entity 进入 other 的视野

    for cell in left:
        for other in cell.entities:
            notify_leave(entity, other)  # entity 离开 other 的视野

    # 更新格子归属
    old_cell.entities.remove(entity)
    new_cell.entities.add(entity)
    entity.cell = new_cell
```

这里有个对称性：当 entity 进入 other 的视野时，other 也进入 entity 的视野（如果双方感知半径相同）。实现时通常用**订阅者/被观察者**模型统一管理，避免重复计算。

#### 格子大小的调优困境

九宫格 AOI 的最大问题是格子大小难以确定：

- **格子太大**：每个格子实体数多，进入/离开时的通知量大（最坏情况退化为 O(N)）。3×3 格子覆盖的面积远大于实际感知半径，产生大量"超范围"的 AOI 事件。
- **格子太小**：格子数量多，高速移动实体频繁跨格，增加格子变更处理频率。同时内存中需要维护大量稀疏格子。

经验法则：格子边长 ≈ 感知半径。这样 3×3 格子的覆盖直径约等于 3 倍感知半径，在精度和性能间取得平衡。但在玩家密度不均的场景（城镇入口挤满人，荒野空无一人），固定格子大小无法针对热点区域优化。

**改进方向**：四叉树（Quadtree）动态分割，热点区域细分格子，冷区合并格子。但实现复杂度显著增加，通常只在玩家分布极不均匀的开放世界游戏中使用。

---

### 十字链表 AOI

#### 核心思想

维护两条有序链表：所有实体按 X 坐标排序（X 轴链表），按 Z 坐标排序（Z 轴链表）。

每个实体在 X 链表和 Z 链表上各有一个节点。每个节点还维护两个"哨兵节点"，分别标记感知范围的左边界/右边界（X 轴上）和前边界/后边界（Z 轴上）。

```
X 轴链表（按 x 坐标升序）：
... [A.left_guard] -> [B] -> [A] -> [C] -> [A.right_guard] -> [D] ...
    x=-50           x=10   x=20   x=35   x=70                x=90
    (A 感知半径 50)
```

当 A 的 right_guard 越过某个实体 D 时，D 进入 A 的 X 轴感知范围；当 left_guard 越过 D 时，D 离开 X 轴范围。同理 Z 轴。**只有同时在 X 轴和 Z 轴范围内的实体，才真正进入感知范围。**

#### 更新流程

```python
def on_entity_move(self, entity: Entity, new_x: float, new_z: float):
    # 更新 X 轴链表位置
    x_changes = self.update_sorted_list(
        entity, new_x, self.x_list, axis='x'
    )
    # 更新 Z 轴链表位置
    z_changes = self.update_sorted_list(
        entity, new_z, self.z_list, axis='z'
    )

    # 合并两轴变化，找出真正进入/离开的实体
    for other, event in x_changes:
        if event == 'enter' and other in entity.z_watchers:
            notify_enter(entity, other)
        elif event == 'leave' and other not in entity.z_watchers:
            notify_leave(entity, other)
    # Z 轴同理（对称处理）
```

`update_sorted_list` 的关键操作是插入排序：将实体的新位置插入有序链表，过程中统计哪些节点被哨兵节点越过。由于游戏中实体通常是小步移动，每帧位移量小，链表节点只需移动少量位置，接近 **O(k)** 其中 k 为感知范围内的实体数。

#### 十字链表的优势与劣势

**优势：**
- 感知范围精度高，不依赖格子大小调优，可以设置任意半径
- 对均匀分布的实体场景性能稳定
- 不需要预分配格子内存，地图边界不影响算法

**劣势：**
- 实现复杂，哨兵节点的维护、双向通知逻辑容易出 bug
- 玩家聚集时性能下降明显：感知范围内 1000 个实体，每次移动都要扫描 1000 个节点
- 高速移动实体（飞行、传送）可能一帧跨越大量节点，触发 O(N) 扫描

**适用场景：**
- 实体密度较低、分布均匀的大世界
- 需要精确感知半径而非格子粒度的系统

---

### 大世界空间分区

#### 为什么单进程撑不住

即使 AOI 算法再高效，单进程的物理瓶颈（CPU 核数、内存、网络带宽）决定了无法在一个进程内运行包含上千玩家的超大地图。大世界服务端的标准方案是**按区域（Zone）分配进程**。

```
地图划分示意（逻辑区域）：
+----------+----------+
|  Zone A  |  Zone B  |
|  (Proc1) |  (Proc2) |
+----------+----------+
|  Zone C  |  Zone D  |
|  (Proc3) |  (Proc4) |
+----------+----------+
```

每个 Zone 进程独立运行游戏逻辑，维护本区域内所有实体的状态。

#### 边界处理的工程难点

玩家在 Zone 边界附近移动时，情况变得复杂：

1. **双边可见问题**：玩家站在 Zone A/B 边界上，其感知范围横跨两个 Zone。需要向两个进程订阅 AOI 事件。
2. **状态迁移**：玩家从 Zone A 移入 Zone B，其状态（位置、血量、技能 CD、背包数据）必须从 Proc1 迁移到 Proc2，期间不能中断玩家连接。
3. **迁移原子性**：迁移过程中玩家不能"消失"也不能"重复出现"，两个进程需要协调。

#### 边界 AOI 协议

常见实现是**镜像实体（Ghost Entity）**方案：

```
玩家 P 在 Zone A，感知半径跨越 Zone B：

Zone A (Proc1):
  - P 的真实实体（可接受输入）
  - B 区实体 Q 的镜像副本（只读，由 Proc2 定期同步）

Zone B (Proc2):
  - P 的镜像副本（只读，由 Proc1 定期同步）
  - Q 的真实实体
```

镜像副本的更新频率通常低于真实实体，因为跨进程同步有额外开销。这意味着跨 Zone 的 AOI 感知会有轻微的额外延迟（通常在 1-2 帧以内）。

#### 玩家 Zone 迁移协议

```
1. Proc1 检测到玩家 P 越过边界，向 Proc2 发送迁移请求
   Request { player_id, full_state, session_token }

2. Proc2 接收请求，在本地创建 P 的实体，进入"预备态"
   - 不接受外部输入，等待 Proc1 确认

3. Proc2 向 Proc1 回复 ACK
   Response { success, new_entity_id }

4. Proc1 收到 ACK，通知客户端：
   "你的主控进程已切换到 Proc2，后续消息请发往新地址"

5. Proc2 将 P 从"预备态"切换为"活跃态"，开始接受输入

6. Proc1 删除 P 的实体，释放资源
```

迁移窗口（步骤 2-5）期间，客户端可能同时向 Proc1 发送旧消息。Proc1 需要缓存并转发到 Proc2，或客户端等待确认后再发送（取决于协议设计）。

---

### 跨服通信

"跨服"在 MMO 语境中有两层含义：同一物理服务器内的跨进程，以及不同玩家实例（服务器组）之间的通信（如跨服副本、全服拍卖行）。

#### 跨进程（同地图分区）

使用内部消息总线（如 ZeroMQ、自研 Actor 框架）进行低延迟通信。Zone 进程通过服务发现找到目标 Zone 的地址，直连发送消息。

关键点：**跨 Zone 的玩家交互（如跨 Zone 喊话、攻击）必须走同步协议，确保两端状态一致**。攻击的处理权通常归属于攻击者所在的 Zone（或目标所在的 Zone，取决于系统设计），另一端只接收结果通知。

#### 跨服实例（全服玩法）

全服拍卖行、全服排行榜等玩法需要聚合来自所有服务器组的数据。典型方案：

```
各服务器组 -> 定期推送变更 -> 全服聚合服务 -> 只读副本分发给各服务器组

写操作路径：
  玩家操作 -> 本服 -> 路由到全服聚合服务 -> 广播变更 -> 各服副本更新
  （延迟约 100-500ms，对拍卖行等非实时场景可接受）
```

---

## 工程边界

### 热点聚集导致的 AOI 风暴

九宫格和十字链表在实体高度聚集时都会退化。城镇中心广场 1000 名玩家，每人每帧移动都要向其他 999 人广播位置——这已经不是 AOI 算法的问题，而是**实体密度控制**问题。

应对手段：
- **分层广播**：核心感知区（5m 以内）高频更新，外围感知区低频更新
- **合并广播**：将 100ms 内同一区域的多个位置更新合并为一条消息
- **服务端 AOI 截流**：每个实体每秒最多向单个观察者推送 N 次位置更新

### Zone 切分粒度

Zone 切得太细，跨 Zone 交互频繁，迁移协议开销大；Zone 切得太粗，单 Zone 进程压力大，无法水平扩展。

实践中，Zone 大小通常按照"单 Zone 最大承载玩家数"倒推，例如：单 Zone 上限 500 玩家，地图预期最高同时 10000 玩家，则至少需要 20 个 Zone 进程。Zone 边界通常设在地形上的自然分隔（山脉、海峡），减少玩家频繁跨 Zone。

### 动态 Zone 扩容

固定 Zone 划分无法应对玩家分布动态变化（活动时间城镇聚集、平时荒野分散）。动态 Zone 方案允许在运行时将一个 Zone 的部分区域拆分到新进程，但需要在线迁移所有相关玩家实体，工程复杂度极高，通常只有头部 MMO 的后期架构才会实现。

---

## 最短结论

AOI 算法选九宫格还是十字链表，本质是在"实现复杂度"与"密度适应性"之间选一个，真正决定大世界扩展性上限的是空间分区和跨进程迁移协议设计得是否干净。
