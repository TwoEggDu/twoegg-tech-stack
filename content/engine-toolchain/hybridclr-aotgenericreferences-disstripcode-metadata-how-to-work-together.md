---
title: "HybridCLR AOTGenericReferences、DisStripCode、补 metadata 到底怎么配合"
date: "2026-03-31"
description: "把 AOT 泛型修法里最容易混掉的三样东西拆开：AOTGenericReferences 是什么，DisStripCode 做什么，补充 metadata 又在解决哪一层问题，以及它们该按什么顺序协作。"
weight: 51
tags:
  - "HybridCLR"
  - "IL2CPP"
  - "AOT"
  - "AOTGenericReferences"
  - "DisStripCode"
  - "LoadMetadataForAOTAssembly"
series: "HybridCLR"
---
> 真正把人拖进重复排障的，往往不是不会修，而是把 `AOTGenericReferences.cs`、DisStripCode 和补 metadata 三件不同的事，误当成同一套“HybridCLR 泛型修法”。

这是 HybridCLR 系列第 22 篇。

前两篇 [HCLR-20]({{< relref "engine-toolchain/hybridclr-sharing-type-judgment-valuetuple-int-string-valuetuple-int-object.md" >}}) 和 [HCLR-21]({{< relref "engine-toolchain/hybridclr-disstripcode-writing-patterns-valuetype-reftype-nestedgeneric-delegate.md" >}}) 已经把“该写什么共享类型”和“DisStripCode 具体怎么写”拆开了。

但项目里真正高频的误判，往往还要再往前一步：

`AOTGenericReferences、DisStripCode、LoadMetadataForAOTAssembly 这三样东西，到底谁负责什么，什么时候叠加，什么时候只需要其中一个？`

这一篇只做这件事。

## 这篇要回答什么

这篇主要回答 4 个问题：

1. `AOTGenericReferences.cs` 到底是需求清单，还是修复器。
2. DisStripCode 解决的到底是哪一层缺口。
3. 补充 metadata 为什么能让报错消失，但不一定回到 native 路径。
4. 这三样东西在真实项目里应该按什么顺序协作。

## 收束

AOTGenericReferences 告诉你热更侧需要什么。DisStripCode 负责把需要的 AOT 泛型实例真正做进 native 世界。补 metadata 负责让解释器在必要时还能把 AOT 世界看懂。三者是上下游关系，不是同义词。

所以你要先问的，不是“我该改哪个文件”，而是：

`我现在缺的是线索、native 实现，还是解释执行所需的 metadata。`

## 第一层：AOTGenericReferences 是“需求清单”，不是“自动修复器”

很多人第一次看到 `AOTGenericReferences.cs`，会天然把它理解成：

`HybridCLR 已经帮我把该补的 AOT 泛型都自动补好了。`

这条直觉不稳。

更准确的理解是：

`它描述的是：热更侧目前依赖了哪些 AOT 泛型类型 / 方法实例。`

也就是说，它回答的是：

`现在有哪些地方可能需要你去关心 AOT 泛型缺口。`

它不直接回答：

- 这些实例是不是已经在 AOT 里有了 native 实现
- 这些实例是不是在当前项目里已经被某段 DisStripCode 覆盖
- 这些实例是不是可以先靠 metadata 恢复运行

所以它最适合扮演的角色，是：

`排查时的线索入口，和维护时的需求清单。`

如果把它误当成“修好证明”，你后面会一直在错误的完成感里排查。

## 第二层：DisStripCode 负责把实例真正做进 AOT native 世界

这一层的职责最干脆。

DisStripCode 做的是：

- 显式写出共享后的泛型实例
- 让 IL2CPP 在构建时看见它
- 从而把对应实例真正编进 AOT native 产物

它对应解决的是：

`AOT native 实例缺失`

所以只要你真正想恢复的是 native 路径，而不是“先让功能跑起来”，最终都绕不开这一步。

但要注意：

`DisStripCode 也不是万能修法。`

它不解决：

- 被裁剪掉的类型 / 成员可见性
- 解释器看不见 AOT method body
- metadata 和最终包不匹配

这也是为什么前一篇 [HCLR-21]({{< relref "engine-toolchain/hybridclr-disstripcode-writing-patterns-valuetype-reftype-nestedgeneric-delegate.md" >}}) 只负责回答“怎么写”，不负责回答“什么时候该只补 metadata”。

## 第三层：补 metadata 负责“让解释器还能走下去”

补充 metadata 最容易被写偏的地方，在于它太容易给人一种“问题已经彻底解决”的错觉。

更稳的口径是：

`补 metadata 解决的是：当解释器需要读取 AOT 世界的 method body、泛型定义和布局时，它还能不能继续往下执行。`

所以它最适合解决的是：

- 当前版本先恢复运行
- 需要解释器继续工作
- 不想因为一次 AOT 泛型缺口立刻重打包

它不等于：

`凭空补出本来就没被 AOT native 编出来的那份实现。`

这也是为什么项目里经常出现一种典型现象：

- 补了 metadata 之后，原始报错消失了
- 但热路径开始掉帧

原因不是“修错了”，而是：

`执行路径从 native 切到了 interpreter fallback。`

## 把三者摆到同一张图上

如果只按协作顺序看，我建议把它们理解成下面这条链：

```text
热更代码变化
  ↓
1. AOTGenericReferences 识别缺口
   Generate 流程扫描热更 DLL，输出”哪些 AOT 泛型实例是需求点”
  ↓
2. DisStripCode 保留 AOT 实例
   在 AOT 程序集里显式写出共享实例引用，
   让 IL2CPP 构建时为它们生成 native 实现
  ↓
3. 补充 metadata 补齐运行时可见性
   调用 LoadMetadataForAOTAssembly，
   让解释器能读取 AOT method body 和泛型定义
```

注意这三步是因果链，不是并列选项。AOTGenericReferences 先告诉你缺什么，DisStripCode 把能做进 native 的实例补进去，补充 metadata 再为解释器兜底。裁剪问题（类型 / 成员被 strip）不在这条链上，应走 `link.xml` / `[Preserve]`。

这条链里，三者分别站在：

- `AOTGenericReferences`：需求发现（识别缺口）
- `DisStripCode`：native 实现实装（保留实例）
- 补 metadata：解释路径兜底（运行时可见性）

只要把这三个位置站稳，很多“到底改哪”的争论其实会自动消失。

## 三种最常见的协作场景

### 场景一：新功能开发期，先看清单，再补 AOT

这种场景下最稳的顺序是：

1. 先让生成流程把 `AOTGenericReferences` 更新出来
2. 看差异里新增了哪些共享实例
3. 热路径和关键路径，优先补 DisStripCode
4. 再重打包验证

这时 metadata 更像兜底，不是主修法。

### 场景二：线上已经出错，先恢复运行

这种场景下顺序会反过来：

1. 先判断是不是 AOT 泛型缺口
2. 如果不能马上发包，优先补 metadata 让功能恢复
3. 下一个包版本再把热路径相关实例补回 DisStripCode

这时 metadata 是应急，DisStripCode 是后续恢复 native。

### 场景三：看起来像泛型，其实是裁剪

这也是最容易混进去的一类。

如果问题本质是：

- 类型被裁
- 成员被裁
- 反射入口没保

那你无论是看 `AOTGenericReferences` 还是补 DisStripCode，都不在真正的问题层。

这时应该直接回到：

- `link.xml`
- `[Preserve]`
- stripping level

去判断可见性是不是先坏了。

## 一个项目里最稳的分工方式

如果你希望这三样东西在工程里长期不打架，我建议用下面的分工：

- `AOTGenericReferences.cs`：只当清单和 diff 入口
- DisStripCode：只放“确认要恢复或保留 native 路径”的实例
- metadata 加载逻辑：只当解释执行兜底和线上恢复手段

这三者不要混写成一种“统一泛型修法层”。

一旦混写，后面就会出现两种典型问题：

- 以为看到了清单，就等于已经修了
- 以为报错没了，就等于已经回到 native 了

## 收束

> `AOTGenericReferences` 告诉你热更侧缺口可能在哪。
> DisStripCode 决定这些缺口哪些真正要被做进 AOT native 世界。
> 补 metadata 负责在必要时让解释器还能继续工作。
> 三者是上下游，不是同义词。

---

## 系列位置

- 上一篇：<a href="{{< relref "engine-toolchain/hybridclr-disstripcode-writing-patterns-valuetype-reftype-nestedgeneric-delegate.md" >}}">HybridCLR DisStripCode 写法手册｜值类型、引用类型、嵌套泛型、委托分别该怎么写</a>
- 下一篇：<a href="{{< relref "engine-toolchain/hybridclr-aot-generic-pitfall-patterns-unitask-linq-dictionary-delegate-custom-container.md" >}}">HybridCLR AOT 泛型高频坑型录｜UniTask、LINQ、Dictionary、委托、自定义泛型容器怎么排</a>
- 相关前文：<a href="{{< relref "engine-toolchain/hybridclr-fix-decision-disstrip-linkxml-metadata.md" >}}">HybridCLR 修法决策｜DisStripCode、link.xml、补充元数据分别在什么时候用</a>
- 基础回链：<a href="{{< relref "engine-toolchain/hybridclr-toolchain-what-generate-buttons-do.md" >}}">HybridCLR 工具链拆解｜LinkXml、AOTDlls、MethodBridge、AOTGenericReference 到底在生成什么</a>
