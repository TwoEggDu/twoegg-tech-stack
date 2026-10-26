# 并发首次调用与类型初始化实验回填

- 定位：既有正文的案例补证，主线篇数与URL不变。
- 环境：lab-lock，本机自定义2022.3.62f3_96770f904ca7，package6cadd24/runtime8392978/il2cpp_plusba3a76c，Windows x64 Development IL2CPP Debug/Minimal/OptimizeSpeed；919原始输入，无旧异常补丁。
- build_id：d2edc1a5-8237-4f19-b2c6-bc260c0167b2。
- 问题：冷方法准备、成功/失败初始化、封闭泛型静态状态与跨解释器入口共享AOT类型。
- 方法：2/4托管Thread，外部门闩，AOT反射结果边界，逐线程有界Join；.cctor只用150ms Sleep延长窗口，不等待其他worker。
- 普通结果：18组合各3全新Player，共54；162次成功返回/60次预期初始化失败；一次初始化、失败后两次访问不重试、int/string静态状态隔离。
- 原生：8个独立PDB进程，MethodHandle映射目标MethodInfo，释放前interpData为空；成功hot发布缓存、失败Read不执行。方法入口硬件配置与ClassInit/Sleep软件配置分开；命中数不作执行或转换普查。软件/混合断点探索和硬件ClassInit单步事件另归档，不计最终样本。
- 分工：ECMA类型初始化集中矩阵；线程篇门闩/Join；源码起步冷缓存与两种准备路径；IL2CPP runtime准备/静态构造边界；三个索引共用ECMA锚点。
- 记录：本轮固定提交 [8ac20ea](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/8ac20eae2fe9734dc7f691e1028594be9d0dbd3b)，见[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/8ac20eae2fe9734dc7f691e1028594be9d0dbd3b/evidence/2026-10-26-concurrent-initialization/README.md)与[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/8ac20eae2fe9734dc7f691e1028594be9d0dbd3b/evidence/2026-10-26-concurrent-initialization/results.json)；实验已推送并核验，5638131是前轮。
- 未验：循环/递归初始化、原生回调竞争、长期压力、性能和其他平台；原始hot内部catch缺陷仍由异常实验维护。
- 验收：实验仓库测试、输入/产物/日志哈希重解析，干净Hugo零ERROR、7页链接锚点/预览、独立最终审阅。文稿随本次提交推送，Pages部署后续单独核验。
