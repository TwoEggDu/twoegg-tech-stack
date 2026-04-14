---
title: "HybridCLR AOT 泛型回归防线｜怎么把这些坑前移到 Generate、CI 和构建检查里"
date: "2026-03-31"
description: '把 AOT 泛型问题从"线上日志出来再修"前移到工程流程里：哪些改动要重新 Generate，哪些清单要纳入 CI，metadata 止血和 native 根治该如何分工。'
weight: 53
tags:
  - "HybridCLR"
  - "IL2CPP"
  - "AOT"
  - "CI"
  - "GenerateAll"
  - "DisStripCode"
series: "HybridCLR"
hybridclr_version: "v6.x (main branch, 2024-2025)"
---
> AOT 泛型问题最贵的部分，从来不是补那几行代码，而是它往往只有到了真机、到了 IL2CPP、到了热更新路径真正跑起来时，才第一次把自己暴露出来。

这是 HybridCLR 系列第 24 篇。

前面几篇已经把问题空间拆完了：

- [HCLR-20]({{< relref "engine-toolchain/hybridclr-sharing-type-judgment-valuetuple-int-string-valuetuple-int-object.md" >}})：共享类型怎么判断
- [HCLR-21]({{< relref "engine-toolchain/hybridclr-disstripcode-writing-patterns-valuetype-reftype-nestedgeneric-delegate.md" >}})：DisStripCode 怎么写
- [HCLR-22]({{< relref "engine-toolchain/hybridclr-aotgenericreferences-disstripcode-metadata-how-to-work-together.md" >}})：`AOTGenericReferences`、DisStripCode、补 metadata 怎么配合
- [HCLR-23]({{< relref "engine-toolchain/hybridclr-aot-generic-pitfall-patterns-unitask-linq-dictionary-delegate-custom-container.md" >}})：高频坑型怎么归类

这一篇最后只做一件事：

`把这些判断前移到工程流程里，让问题尽量在 Generate、构建和 CI 阶段就暴露，而不是线上报错之后才被动排。`

## 这篇要回答什么

这篇主要回答 4 个问题：

1. 为什么 AOT 泛型问题必须前移。
2. 哪些改动会引入新的 AOT 泛型缺口。
3. `Generate`、构建、CI 各自该检查什么。
4. 线上止血和下版本根治，该怎么分工。

## 收束

AOT 泛型问题本质上是"构建产物和运行时路径之间的缺口"。
最有效的防线不是多写几条排障经验，而是把共享类型判断、AOTGenericReferences 清单、DisStripCode 维护和 metadata 同源性检查前移到构建流程里。

也就是说，这一类问题最怕的不是复杂，而是：

`它太容易在"开发期没触发、Editor 下没触发、Mono 下没触发"的情况下悄悄漏过去。`

![CI 检查 4 层结构](../../images/hybridclr/ci-check-layers.svg)

*图：4 层由粗到细。第 1 层不过，其他层都没意义。*

## 为什么必须前移

相比普通业务 bug，AOT 泛型问题有三个天然不利条件：

### 1. 只有 IL2CPP 真正跑起来时才会暴露

这意味着：

- Editor 里没报，不代表真机没事
- Mono 下没报，不代表 AOT 没缺口
- 单元测试过了，不代表最终包没问题

### 2. 它常常不是源码一眼能看见的

尤其是：

- async / UniTask
- LINQ
- 泛型委托
- 第三方库封装

这些路径经常在源码里看起来很普通，但编译后会生成新的 AOT 泛型需求。

### 3. 它既可能是"功能问题"，也可能只是"路径退化问题"

更危险的是第二种。

有些缺口补了 metadata 之后，功能不再报错，但执行路径已经掉到了 interpreter fallback。

如果流程里没有显式检查，你很可能会在"线上没炸"的表面平静里把性能回退带进正式版本。

## 哪些改动必须触发重新检查

如果你想把 AOT 泛型问题前移，第一步不是写脚本，而是先把"哪些改动意味着风险重新打开"列清楚。

我建议至少把下面这些当成强触发条件：

1. 新增了热更代码里使用的泛型类型
2. 修改了泛型方法的参数类型
3. 新增或升级了第三方库
4. 改了 async / UniTask / LINQ 使用方式
5. 修改了 `Managed Stripping Level`
6. 修改了 `Development` / `Release` 构建模式
7. 改了 AOT 程序集拆分或热更程序集拆分

这些变化的共同点是：

`它们都会让"热更侧需求"和"AOT 侧已有实例"之间的关系重新发生变化。`

## Generate 阶段应该做什么

我建议把 Generate 阶段的职责压成两件事：

### 一：更新需求清单

也就是：

- `AOTGenericReferences`
- 相关生成物

这一步回答的是：

`热更侧现在到底多了哪些新的 AOT 泛型需求。`

### 二：生成构建输入

也就是：

- `link.xml`
- stripped AOT dll
- 其他 HybridCLR 生成物

这一步回答的是：

`后面构建真正要消费的输入，是否和当前代码状态一致。`

如果只做第二步，不做第一步，你不知道需求变了什么。  
如果只做第一步，不做第二步，你知道缺口在哪，却没有把构建输入同步到当前状态。

## CI 阶段应该检查什么

到了 CI，我建议把检查项拆成 4 层。

### 第一层：生成物有没有跑

最基本的问题是：

- `GenerateAll` 跑没跑
- 或者至少相关 Generate 步骤跑没跑

如果这一步都不稳定，后面所有"排查技巧"都只是给随机结果擦屁股。

### 第二层：生成物和最终包是不是同源

这是 AOT 泛型问题最容易被忽略的一层。

比如：

- metadata dll 不是同一次构建产物
- `Development` / `Release` 不一致
- `AssembliesPostIl2CppStrip` 和最终 player 构建不一致

这类问题最讨厌的地方，不是它难懂，而是它看起来"文件都在"，却在运行时悄悄错层。

### 第三层：需求清单有没有新增

我建议把 `AOTGenericReferences` diff 变成 CI 的显式输出。

不是为了自动修复，而是为了让维护者一眼看到：

- 这次热更代码新增了哪些泛型需求
- 这些需求里哪些已经被现有 DisStripCode 覆盖
- 哪些需要人工判断是否要恢复 native 路径

CI 不一定要自动决定该怎么补，但至少要把新增需求抬到台面上。

### 第四层：热路径有没有只靠 metadata 在兜底

这是最容易漏的一层。

如果某条关键路径：

- 功能能跑
- 但只能靠 metadata + interpreter fallback 跑

那它不是"已经没问题了"，而是：

`已经从功能风险，转成了性能风险。`

这一层如果不在 CI 或发包检查里单独看，后面就会以"线上没炸"为理由被默认放过。

### 参考：CI 检查脚本示例

下面是一份可以直接嵌入 CI pipeline 的伪脚本，覆盖上面四层的核心检查点：

```bash
#!/bin/bash
set -euo pipefail

BUILD_TARGET="Android"  # 或 iOS
STRIP_DIR="HybridCLRData/AssembliesPostIl2CppStrip/${BUILD_TARGET}"
AOT_REF="Assets/HotUpdate/AOTGenericReferences.cs"
AOT_REF_PREV=".ci-cache/AOTGenericReferences.cs.prev"

# 1. 检查 MethodBridge.cpp 存在且 DEVELOPMENT 标志与构建模式一致
BRIDGE_FILE=$(find . -name "MethodBridge.cpp" | head -1)
if [ -z "$BRIDGE_FILE" ]; then
  echo "FAIL: MethodBridge.cpp not found. Did GenerateAll run?"
  exit 1
fi
if grep -q "DEVELOPMENT" "$BRIDGE_FILE"; then
  echo "WARN: MethodBridge.cpp contains DEVELOPMENT flag — confirm build mode matches"
fi

# 2. 检查 AssembliesPostIl2CppStrip 目录非空
if [ ! -d "$STRIP_DIR" ] || [ -z "$(ls -A $STRIP_DIR)" ]; then
  echo "FAIL: ${STRIP_DIR} is empty or missing"
  exit 1
fi
echo "OK: stripped assemblies present in ${STRIP_DIR}"

# 3. 检查 AOTGenericReferences.cs 已重新生成
if [ ! -f "$AOT_REF" ]; then
  echo "FAIL: ${AOT_REF} not found"
  exit 1
fi

# 4. diff AOTGenericReferences.cs，检测新增未注释条目（新缺口）
if [ -f "$AOT_REF_PREV" ]; then
  NEW_ENTRIES=$(diff "$AOT_REF_PREV" "$AOT_REF" \
    | grep "^>" | grep -v "^>.*\/\/" | grep -c "typeof\|new " || true)
  if [ "$NEW_ENTRIES" -gt 0 ]; then
    echo "WARN: ${NEW_ENTRIES} new uncommented AOT generic entries detected"
    diff "$AOT_REF_PREV" "$AOT_REF" | grep "^>" | grep -v "^>.*\/\/"
  fi
fi
cp "$AOT_REF" "$AOT_REF_PREV"

echo "CI AOT checks passed"
```

这份脚本不负责自动修复，它只负责把缺口在 CI 阶段抬到台面上。

## DisStripCode 的长期维护规则

如果你想让 DisStripCode 不从"修法层"退化成"遗留垃圾层"，我建议守住下面 5 条：

1. 只收"确认要恢复 native 路径"的实例
2. 每条引用标注来源：报错、案例、清单、业务场景
3. 按功能域拆文件，不把所有实例堆成一个巨型 helper
4. 热更 DLL 变更后，必须重新对照 `AOTGenericReferences`
5. 热路径和非热路径分开维护

这 5 条本质上是在回答同一个问题：

`DisStripCode 到底是临时补丁，还是项目级配置层。`

如果你不显式把它当配置层维护，它最后一定会变成"没人敢删、也没人知道为什么在这里"的历史包袱。

## 线上止血和下版本根治怎么分工

我建议把这件事写进团队约定，而不是留给每次事故现场临时判断。

### 线上止血

优先用来做：

- 补 metadata 恢复运行
- 降低事故范围
- 争取发包时间

### 下版本根治

优先用来做：

- 补 DisStripCode
- 调整 `link.xml` / `[Preserve]`
- 修正 Generate / 构建流程
- 把新需求纳入长期检查项

这样分工的好处，是把"应急"和"工程化"拆开。  
否则项目里最容易出现的情况就是：

`上一个事故靠 metadata 暂时压住了，于是所有人默认这就算修完了。`

## 最后给一份最小检查清单

每次发包前，我建议至少过下面这份清单：

```text
□ 热更 DLL 变化后，相关 Generate 步骤已重新执行
□ AOTGenericReferences 已更新，并人工看过新增需求
□ metadata dll 与最终 player 构建产物同源
□ DisStripCode 已同步关键热路径的 AOT 泛型实例
□ link.xml / [Preserve] 没有因为临时排障被无限放宽
□ 对关键热路径，确认不是"只靠 metadata 跑通"
```

这份清单不负责替你修问题。  
它负责的是另一件更重要的事：

`不让问题继续悄悄溜过去。`

## 收束

> AOT 泛型问题最有效的防线，不是积累更多"线上报了怎么办"的经验。而是把共享类型判断、清单 diff、DisStripCode 维护和 metadata 同源性检查前移到 Generate、构建和 CI 里，让它尽可能在发包前暴露。

---

## 系列位置

- 上一篇：<a href="{{< relref "engine-toolchain/hybridclr-aot-generic-pitfall-patterns-unitask-linq-dictionary-delegate-custom-container.md" >}}">HybridCLR AOT 泛型高频坑型录｜UniTask、LINQ、Dictionary、委托、自定义泛型容器怎么排</a>
- 回到入口：<a href="{{< relref "engine-toolchain/hybridclr-series-index.md" >}}">HybridCLR 系列索引｜先读哪篇，遇到什么问题该回看哪篇</a>
- 相关前文：<a href="{{< relref "engine-toolchain/hybridclr-ci-pipeline-generate-all-and-development-flag.md" >}}">HybridCLR 打包工程化｜GenerateAll 必须进 CI 流程，Development 一致性与 Launcher-only 场景</a>
- 相关前文：<a href="{{< relref "engine-toolchain/hybridclr-fix-decision-disstrip-linkxml-metadata.md" >}}">HybridCLR 修法决策｜DisStripCode、link.xml、补充元数据分别在什么时候用</a>
