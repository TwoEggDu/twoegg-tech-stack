---
title: "事务与并发：ACID、锁机制，以及为什么扣道具必须用事务"
slug: "game-backend-db-transaction-and-concurrency"
date: "2026-04-04"
description: "不用事务会出什么问题？游戏里的超卖、重复扣道具从哪里来？从 ACID 四个属性到乐观锁 vs 悲观锁，讲清楚游戏后端并发写的正确处理方式和常见踩坑。"
tags:
  - "游戏后端"
  - "数据库"
  - "事务"
  - "并发控制"
  - "MySQL"
series: "游戏后端基础"
primary_series: "game-backend"
series_role: "article"
series_order: 4
weight: 3004
---

## 这篇文章在解决什么问题

游戏后端事故里，有一类问题的发生机制特别相似：

- 玩家的道具被扣了两次
- 限时活动的奖励被同一个玩家领了两遍
- 商店里的限量物品超卖了

这些问题在测试环境很难复现——单线程测试完全没问题，上线后玩家并发一高就出现。原因几乎都是同一个：**缺少正确的并发控制**。

而并发控制问题的解法，核心是**事务和锁**。但很多开发者对事务的理解停留在"BEGIN / COMMIT"的语法层面，对为什么要用事务、用错了会出什么问题、事务粒度应该怎么设计，缺乏清晰的认识。

这篇文章从这些问题出发。

---

## ACID 四个属性在游戏场景的含义

ACID 是事务的四个保证属性：Atomicity（原子性）、Consistency（一致性）、Isolation（隔离性）、Durability（持久性）。教科书里的定义是抽象的，但在游戏场景里，每个属性都有很具体的意义。

### 原子性（Atomicity）

"要么全成功，要么全失败"。

玩家购买商品：**扣除金币** + **增加道具**，这两个操作必须是原子的。如果扣除金币成功后服务器宕机，道具没有增加，玩家会流失并投诉。如果没有事务，这个问题只能靠业务层的补偿逻辑（定期核对、补发）来修复，成本很高。

有了原子性保证，这两个操作要么同时成功，要么同时回滚——中间状态不会暴露给业务层。

### 一致性（Consistency）

"事务前后，数据满足业务约束"。

一致性在技术上依赖原子性和隔离性来实现，但它的含义是业务级别的：玩家金币不能变成负数，背包物品数量不能是负数，这些约束在事务执行前后都应该成立。

数据库的 `CHECK` 约束、`NOT NULL`、`FOREIGN KEY` 是数据库层面的一致性保障，但业务一致性（"玩家不能购买超过库存的物品"）需要应用层来维护。

### 隔离性（Isolation）

"并发执行的事务互不干扰"。

这是四个属性里最容易出问题的一个，也是游戏并发 Bug 的主要来源。隔离级别有四个（从低到高）：

- **READ UNCOMMITTED**：可以读到其他事务未提交的数据（脏读），几乎没有实际使用场景
- **READ COMMITTED**：只读已提交的数据，可能出现不可重复读（两次读同一行结果不同）
- **REPEATABLE READ**：同一事务内多次读同一行结果相同，MySQL InnoDB 的默认级别，用 MVCC 实现
- **SERIALIZABLE**：完全串行化执行，性能最差，游戏后端几乎不用

**MySQL InnoDB 默认的 REPEATABLE READ 对大多数游戏场景是足够的**，但要理解它不能防止幻读（Phantom Read）——当你用 `SELECT ... WHERE score > 100` 查询时，同一事务内两次执行可能看到不同的行数（因为另一个事务在中间插入了新行）。InnoDB 通过 Next-Key Lock（间隙锁）来部分解决幻读问题。

### 持久性（Durability）

"已提交的事务，数据不会丢失"。

InnoDB 通过 WAL（Write-Ahead Logging，即 redo log）实现持久性。事务提交时，数据先写 redo log（顺序 I/O，快），再异步刷到数据页（随机 I/O）。即便服务器宕机，重启后 InnoDB 会用 redo log 恢复未刷盘的数据。

`innodb_flush_log_at_trx_commit = 1`（默认值）表示每次事务提交都把 redo log 刷到磁盘，这是最严格的持久性保证，但也最慢。对于游戏业务，这个配置通常保持默认。

---

## 游戏里的并发写问题

### 典型场景：同时购买同一件限量物品

```
玩家 A 请求购买：读取库存 = 1，库存 > 0，准备扣减
玩家 B 请求购买：读取库存 = 1，库存 > 0，准备扣减
玩家 A 写入：库存 = 1 - 1 = 0
玩家 B 写入：库存 = 1 - 1 = 0  （此时已经是 0 了，但 B 还是成功扣减）
```

两个玩家都"成功"购买了库存只有 1 件的物品，超卖发生。这个问题的根源是**读-改-写**（Read-Modify-Write）操作不是原子的。

### 典型场景：同时使用同一件道具

玩家开了两个客户端（或网络抖动导致请求重发），同一个"使用道具"请求被发出了两次。如果服务器没有并发控制，两个请求同时读到道具数量 = 1，都认为可以使用，都进行扣减，结果道具数量变成 -1。

---

## 乐观锁 vs 悲观锁

解决并发写问题有两种主流方案：

### 悲观锁（Pessimistic Lock）

假设冲突一定会发生，操作前先加锁，其他事务等待。

```sql
BEGIN;

-- SELECT FOR UPDATE 会对读取的行加排他锁
SELECT gold, version FROM player_currency
WHERE user_id = ? FOR UPDATE;

-- 检查金币是否足够
-- 如果足够，更新
UPDATE player_currency
SET gold = gold - ?, version = version + 1
WHERE user_id = ?;

COMMIT;
```

`SELECT ... FOR UPDATE` 让后续到来的相同查询阻塞，直到当前事务提交。这个方案的优点是正确性有保证，缺点是**在高并发下锁等待会成为性能瓶颈**，甚至导致死锁。

适用场景：**冲突概率高**的操作，比如热门商品秒杀、公会金库操作。

### 乐观锁（Optimistic Lock）

假设冲突很少发生，操作时不加锁，提交时检查是否有冲突。

```sql
-- 读取时记录版本号
SELECT gold, version FROM player_currency WHERE user_id = ?;
-- 假设 version = 5

-- 更新时检查版本号是否变化
UPDATE player_currency
SET gold = gold - ?, version = version + 1
WHERE user_id = ? AND version = 5;  -- version 匹配才更新

-- 检查 affected_rows：0 表示版本已变，有并发冲突，需要重试
```

`affected_rows = 0` 说明在这个事务的读取和写入之间，有另一个事务修改了数据（version 变了），此时应用层需要重试整个操作。

适用场景：**冲突概率低**的操作，比如玩家更新昵称、修改设置。优点是没有锁等待，性能更好；缺点是冲突时需要重试，如果冲突频率高，大量重试会制造更多压力。

### 数据库原子操作（另一种思路）

很多场景可以绕过乐观锁/悲观锁，直接用数据库的原子更新：

```sql
-- 原子扣减金币，只有金币 >= 100 时才更新
UPDATE player_currency
SET gold = gold - 100
WHERE user_id = ? AND gold >= 100;

-- 检查 affected_rows：0 表示金币不足
```

这个语句在 MySQL InnoDB 里是原子的（单语句隐式事务），不需要显式的乐观锁版本号，适合简单的扣减操作。

---

## InnoDB 的锁机制

理解 InnoDB 的锁，对于排查死锁和锁等待问题很重要。

### 行锁（Row Lock）

InnoDB 是行级锁，不是表锁（MyISAM 才是表锁）。行锁分两种：
- **共享锁（S Lock）**：`SELECT ... LOCK IN SHARE MODE`，允许并发读，阻止写
- **排他锁（X Lock）**：`SELECT ... FOR UPDATE` 或 `UPDATE / DELETE`，阻止其他事务读和写

### 间隙锁（Gap Lock）与 Next-Key Lock

间隙锁是 InnoDB 在 REPEATABLE READ 隔离级别下为防止幻读引入的机制。

```sql
-- 如果没有 id = 50 的行，但查询范围包含 50
SELECT * FROM item_instance WHERE instance_id BETWEEN 40 AND 60 FOR UPDATE;
```

这个查询不仅锁定已存在的行（40-60 范围内），还锁定了 40-60 的"间隙"，阻止其他事务在这个范围内插入新行。

间隙锁的副作用是**会导致意外的锁等待**。如果你的业务逻辑里有基于范围的 `SELECT FOR UPDATE`，需要特别注意。

### 死锁

死锁发生在两个事务互相等待对方持有的锁：

```
事务 A：锁了 item_instance.instance_id = 1001，等待锁 player_currency.user_id = 10001
事务 B：锁了 player_currency.user_id = 10001，等待锁 item_instance.instance_id = 1001
```

InnoDB 有内置的死锁检测，检测到死锁会选择"代价较小"的事务（通常是已经修改行数较少的那个）回滚，另一个事务继续执行。

**预防死锁的原则**：
1. 多个事务对同一组资源加锁时，**保持一致的加锁顺序**
2. 控制事务执行时间，尽量短事务
3. 避免在事务内部做耗时的外部调用（HTTP 请求、RPC 调用）

排查死锁：`SHOW ENGINE INNODB STATUS` 可以看到最近一次死锁的详细信息。

---

## 事务粒度设计

**不要把整个请求包在一个大事务里**，这是游戏后端最常见的事务设计错误。

一个"战斗结算"请求可能包含：
1. 检查玩家状态（可以不在事务里）
2. 读取战斗数据（可以不在事务里）
3. 计算奖励（纯计算，不需要事务）
4. 写入玩家经验值（需要事务）
5. 写入获得的道具（需要事务）
6. 更新排行榜分数（需要事务，但可以单独一个事务）
7. 发送系统通知（不需要事务，甚至可以异步）

如果把这 7 个步骤全放在一个事务里，事务持有的锁时间会很长，在高并发下造成大量锁等待。

**正确做法**：
- 只把真正需要原子性保证的操作放在同一个事务里（步骤 4+5）
- 其他操作拆开或异步处理
- 通知、日志等非核心操作，放到事务提交后异步执行

### 游戏里最常见的事务坑

**坑 1：事务内做 HTTP/RPC 调用**

```python
# 危险：事务内部有网络调用
with db.transaction():
    player = db.query("SELECT ... FOR UPDATE")
    result = http.call("https://payment-service/verify")  # 如果这里超时，事务会挂很久
    db.execute("UPDATE player_currency ...")
```

RPC 调用的延迟无法预测，事务持有锁的时间随之变得不可控。应该先完成外部调用，确认结果后再开启事务。

**坑 2：事务内部静默忽略错误**

```python
with db.transaction():
    try:
        db.execute("UPDATE player_currency SET gold = gold - 100 WHERE user_id = ?")
        db.execute("INSERT INTO item_instance ...")
    except Exception:
        pass  # 忽略错误，事务没有回滚
```

捕获异常但不回滚事务，会导致事务处于不一致状态（扣了金币但没加道具）。异常必须导致事务回滚。

**坑 3：用 SELECT 结果做业务判断，没有加锁**

```python
# 错误：读到的数据在 UPDATE 之前可能已经被其他事务修改
count = db.query_one("SELECT COUNT(*) FROM item_instance WHERE user_id = ?")
if count < bag_capacity:
    db.execute("INSERT INTO item_instance ...")
```

读取背包数量和插入新物品之间，可能有其他并发请求插入了物品，导致实际物品数量超过背包上限。应该用 `SELECT ... FOR UPDATE` 锁定相关行，或者使用数据库约束（`CHECK` 约束 + 唯一索引）来保证。

---

## 工程边界

事务不该承担的职责：

**事务不是业务补偿的替代品**。对于跨服务的分布式操作（玩家从服务 A 扣款，在服务 B 创建订单），数据库事务无法跨越服务边界。这类场景需要分布式事务（2PC、Saga 模式）或最终一致性方案，不是单机事务能解决的。

**事务不能保证外部系统的一致性**。事务提交后发推送通知、记录日志，这些外部操作不在事务保护范围内，即便事务成功，通知也可能失败。业务层需要对这类"尽力而为"的操作有明确的容错设计。

---

## 最短结论

事务的本质是边界声明——告诉数据库哪些操作必须原子，但事务粒度越大、持有锁越久，并发性能损失越大，事务设计的艺术在于找到最小的一致性边界。
