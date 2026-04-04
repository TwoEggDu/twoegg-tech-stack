---
title: "数据安全：SQL 注入防护、敏感数据加密存储、GDPR 基本合规"
slug: "game-backend-security-03-data-security"
date: "2026-04-04"
description: "玩家数据泄露对游戏公司意味着什么代价？从 SQL 注入防护到 GDPR 合规，梳理防御成本最低的基础数据安全措施。"
tags:
  - "游戏后端"
  - "安全"
  - "数据安全"
  - "GDPR"
series: "游戏后端基础"
primary_series: "game-backend"
series_role: "article"
series_order: 32
weight: 3032
---

## 数据泄露的代价

2024 年，一家中型游戏公司因数据库配置错误，导致 300 万玩家的账号密码（MD5 哈希）和手机号码泄露。事故后，该公司面临：监管罚款、大量玩家投诉注销账号、品牌公关危机，以及随之而来的媒体负面报道。

这不是极端案例。数据安全事件对游戏公司的影响链条已经很清晰：

**技术层面：** 玩家账号被撞库攻击利用，导致账号盗号事件激增，客服工单暴涨。

**法律层面：** GDPR（欧盟）、PIPL（中国个人信息保护法）等数据保护法规设有明确的罚款机制，严重违规可达年营收的 4%（GDPR）。

**商业层面：** 玩家对游戏公司的数据安全信任一旦崩塌，流失很难挽回。

游戏公司在数据安全上的困境在于：预防投入是隐性的，泄露后的代价是显性的。本文的目标是梳理**防御成本最低、覆盖面最广的基础措施**。

---

## SQL 注入：最古老但仍有效的攻击

### 攻击原理

SQL 注入的本质是**数据和指令的混淆**：攻击者将 SQL 指令伪装成用户输入数据，让数据库把它当作指令执行。

游戏场景的一个典型例子：

```python
# 危险写法：字符串拼接构建 SQL
def get_player_by_name(name: str):
    sql = f"SELECT * FROM players WHERE username = '{name}'"
    return db.execute(sql)
```

如果攻击者输入用户名为 `'; DROP TABLE players; --`，拼接后的 SQL 变成：

```sql
SELECT * FROM players WHERE username = ''; DROP TABLE players; --'
```

数据库会执行两条语句，第二条删除整张玩家表。

更隐蔽的攻击是**数据提取**，通过注入 `UNION SELECT` 将其他表（如支付记录、账号密码）的数据拼入查询结果返回给攻击者，且不产生明显的错误。

### Prepared Statement 防护

防御 SQL 注入的标准方案是**参数化查询（Prepared Statement）**：

```python
# 安全写法：参数化查询
def get_player_by_name(name: str):
    sql = "SELECT * FROM players WHERE username = %s"
    return db.execute(sql, (name,))  # name 作为数据，不嵌入 SQL 文本
```

参数化查询中，用户输入的数据永远不会被解释为 SQL 指令。数据库驱动会在发送给数据库之前将参数和查询模板分开处理，数据中的特殊字符（单引号、分号等）不会影响 SQL 结构。

**在 ORM 中同样需要注意：** 大多数 ORM（如 SQLAlchemy、Django ORM）默认使用参数化查询，但如果使用了 `raw()` 或 `text()` 拼接原始 SQL，同样存在注入风险。

```python
# 危险的 ORM 用法
db.execute(text(f"SELECT * FROM players WHERE username = '{name}'"))

# 安全的 ORM 用法
db.execute(text("SELECT * FROM players WHERE username = :name"), {"name": name})
```

### 其他防护层

- **最小数据库权限：** 游戏业务服务连接数据库的账号，只授予其业务需要的权限（SELECT/INSERT/UPDATE），不授予 DROP/ALTER 等 DDL 权限。
- **输入验证：** 对用户名、道具名称等字段做格式校验（长度限制、字符集限制），减少注入机会。
- **WAF（Web Application Firewall）：** 作为额外防御层，检测常见注入 Pattern，但不能替代参数化查询。

---

## 密码存储：绝不明文，慎用 MD5

### 为什么 MD5 不够

很多早期游戏后端将密码用 MD5 哈希后存储。MD5 有两个根本问题：

1. **速度太快：** MD5 的设计目标是高速哈希，现代 GPU 每秒可以计算数十亿次 MD5，彩虹表攻击和暴力破解都非常高效。
2. **无盐（Salt）：** 如果不加盐，相同密码总是产生相同的哈希值，攻击者可以用预计算的彩虹表直接查表还原。

### bcrypt / Argon2

专为密码存储设计的哈希算法的核心特点是**计算代价可调节**（cost factor），使哈希过程慢到让暴力破解不可行。

```python
import bcrypt

# 存储密码
def hash_password(plain_password: str) -> str:
    salt = bcrypt.gensalt(rounds=12)  # 12 轮，约 250ms/次
    return bcrypt.hashpw(plain_password.encode(), salt).decode()

# 验证密码
def verify_password(plain_password: str, hashed: str) -> bool:
    return bcrypt.checkpw(plain_password.encode(), hashed.encode())
```

`rounds=12` 意味着每次哈希约耗时 250ms，对于用户登录可以接受，但对于攻击者的暴力破解，每秒只能尝试约 4 次，而不是数十亿次。

**Argon2** 是更新的密码哈希算法，赢得 2015 年密码哈希竞赛，能同时抵抗 GPU 并行攻击（通过内存硬度）和 FPGA/ASIC 攻击。新项目推荐优先使用 Argon2id。

**选型参考：**

| 算法 | 推荐度 | 特点 |
|------|--------|------|
| Argon2id | 首选 | 内存硬度 + 时间硬度，抵抗 GPU 攻击 |
| bcrypt | 可用 | 成熟稳定，广泛支持 |
| scrypt | 可用 | 内存硬度，但参数调整较复杂 |
| MD5 / SHA-1 | 禁止 | 速度过快，不适合密码存储 |
| SHA-256 + salt | 不推荐 | 比 MD5 好，但速度仍然过快 |

---

## 敏感数据加密：单向 vs 可逆的选择

不同类型的敏感数据，加密策略不同。

### 单向哈希（不可逆）

适用于**只需要验证，不需要还原**的场景：

- **密码：** 登录时只需要验证用户输入的密码与存储的哈希是否匹配，不需要知道原始密码。
- **手机号用于唯一性校验：** 只需要判断"这个手机号是否已注册"，不需要展示原始手机号（某些场景）。

```python
import hashlib
# 手机号哈希（加固定盐防彩虹表）
def hash_phone(phone: str, app_salt: str) -> str:
    return hashlib.sha256(f"{app_salt}{phone}".encode()).hexdigest()
```

**缺点：** 一旦需要"找回这个玩家的手机号"（比如客服查询），单向哈希无法满足。

### 可逆加密（对称加密）

适用于**需要还原原始数据**的场景：

- 手机号（客服需要查看、短信验证需要发送）
- 邮箱地址（需要发送通知邮件）
- 真实姓名（实名认证需要核对）
- 支付相关信息（退款需要查账）

常用方案：AES-256-GCM（既加密又做完整性验证）。

```python
from cryptography.hazmat.primitives.ciphers.aead import AESGCM
import os

def encrypt_sensitive(data: str, key: bytes) -> bytes:
    aesgcm = AESGCM(key)
    nonce = os.urandom(12)  # 96-bit nonce
    ciphertext = aesgcm.encrypt(nonce, data.encode(), None)
    return nonce + ciphertext  # 将 nonce 和密文一起存储

def decrypt_sensitive(encrypted: bytes, key: bytes) -> str:
    aesgcm = AESGCM(key)
    nonce = encrypted[:12]
    ciphertext = encrypted[12:]
    return aesgcm.decrypt(nonce, ciphertext, None).decode()
```

**密钥管理是核心挑战：** 加密密钥本身要安全存储。使用云服务的游戏公司通常通过 KMS（Key Management Service）管理加密密钥，密钥不直接出现在代码或配置文件中。

---

## GDPR 对游戏的主要影响

GDPR（General Data Protection Regulation）是欧盟数据保护法规，面向欧盟玩家的游戏必须遵守。2021 年生效的中国 PIPL 在核心原则上与 GDPR 高度相似。

### 主要影响点

**1. 数据最小化原则（Data Minimization）**

只收集业务真正需要的数据。如果游戏的核心功能不需要玩家生日，就不应该强制要求填写（除非用于年龄验证等合规目的）。

实际检查点：
- 数据库中的哪些字段是必要的？
- 哪些埋点数据收集了不必要的个人信息？

**2. 用户删除权（Right to Erasure，俗称"被遗忘权"）**

玩家有权要求删除其个人数据。游戏公司需要有能力执行账号注销，并删除（或匿名化）相关个人数据。

**注意：** 部分数据出于法律义务（如财务记录、反欺诈记录）可以保留，但需要与个人数据分开处理。

**3. 数据泄露通知义务**

发生数据泄露时，GDPR 要求在 72 小时内向监管机构通报（如果可能影响用户权利），严重事件还需要通知受影响的用户。这意味着游戏公司需要有**数据泄露检测和应急响应流程**，而不是等到媒体曝光才反应。

**4. 隐私政策的实质性要求**

隐私政策不能是一段用户看不懂的法律文本。GDPR 要求说明：收集了哪些数据、为什么收集、存储多久、是否分享给第三方、用户如何行使权利。

### 游戏公司的合规最低要求

| 要求 | 实现方式 |
|------|----------|
| 隐私政策 | 有清晰的隐私政策，首次启动时告知并获取同意 |
| 数据可导出 | 提供账号数据导出功能（游戏数据、充值记录等） |
| 账号注销 | 账号注销后真正删除/匿名化个人信息 |
| 三方 SDK 管理 | 了解接入的广告/分析 SDK 收集了哪些数据 |
| 未成年人保护 | 对 13 岁（或 16 岁，视地区而定）以下用户的数据获取父母同意 |

---

## 数据安全审计记录

数据安全的基本记录规范，往往在游戏团队中被忽视，但在安全事件发生后至关重要：

**操作日志：**
- 谁在什么时间对什么数据执行了什么操作（特别是写操作）
- 后台系统的每一次关键操作（GM 工具的道具发放、封号操作等）

**访问日志：**
- 记录敏感数据（手机号、邮箱）的查询来源（哪个服务、哪个接口）
- 异常大批量查询的告警

**变更记录：**
- 数据库结构变更记录
- 加密密钥的更换记录

这些日志在安全审计、事故追查、合规检查时都是必要依据。

---

## 工程边界

数据安全措施有其局限性，需要明确：

- **SQL 注入防护** 保护数据库层，但如果 Redis 或 NoSQL 存在类似的查询注入，同样需要关注（MongoDB 的 NoSQL 注入、Redis Lua 脚本注入）。
- **加密存储** 保护静态数据，但不保护传输中的数据（需要 HTTPS/TLS）和运行时内存中的数据。
- **GDPR 合规** 是持续过程，不是一次性检查。随着产品功能变化，数据收集范围会变，需要定期重新评估。

---

## 最短结论

数据安全的基础防线不复杂：Prepared Statement 消灭注入风险，bcrypt/Argon2 让密码存储足够慢，AES 加密保护需要还原的敏感字段，GDPR 合规的最低门槛是能让用户导出和删除自己的数据。

这些措施的共同特点是：**一次性配置，长期生效**。把它们在项目初期就做对，比泄露后亡羊补牢要廉价得多。
