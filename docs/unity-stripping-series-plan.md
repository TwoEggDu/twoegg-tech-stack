# Unity 裁剪系列计划

## 已完成

1. 已发布：《Unity 裁剪 01｜Unity 的裁剪到底分几层》
2. 已发布：《Unity 裁剪 02｜Managed Stripping Level 到底做了什么》
3. 已发布：《Unity 裁剪 03｜Unity 为什么有时看不懂你的反射》
4. 已发布：《Unity 裁剪 04｜哪些 Unity 代码最怕 Strip，以及怎样写得更适合裁剪》
5. 已发布：《Unity 裁剪 05｜Strip Engine Code 到底在裁什么》

## 系列主线

- 先把 `managed stripping`、`Strip Engine Code`、`native symbol strip` 三层边界拆开。
- 再解释 `Managed Stripping Level` 的真实行为，以及 `Mono` / `IL2CPP` 下的差异。
- 再解释 Unity 当前源码会自动保留哪些入口，为什么它不可能理解任意运行时反射。
- 再落到更适合裁剪的代码模式、保留策略和工程化改法。
- 最后把 `Strip Engine Code` 的实现链闭环到 `UnityLinker -> UnityClassRegistration.cpp -> native build`。

## 当前状态

- 系列文章已完成 `5/5`，内容范围完整。
- 下一步只剩站点构建验证；若 `hugo` 验证通过，这条自动化即可置为 `PAUSED`。
