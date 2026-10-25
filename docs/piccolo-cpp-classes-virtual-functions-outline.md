# Piccolo C++ 前置 02：类、继承与虚函数

> 执行层提纲与证据记录；篇级状态统一在 `piccolo-engine-series-plan.md`。

## 定位与范围

- 用户已确认：先补文章，再做 Piccolo 调试；本篇结合实际代码讲类、继承与虚函数，更新相关导航。
- 类型：原理篇。承接智能指针篇，回答通过基类访问时怎样找到执行函数，以及怎样正确析构。
- 最小模型：类与对象、基类子对象、表达式的静态类型、对象的动态类型、虚调用与所有权分别判断。
- 源码基线：`BoomingTech/Piccolo@f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，保持只读。
- 范围：组件与 RHI 两条调用链，补 override、final、纯虚析构、切片、构造析构中的虚调用；不展开 ABI、多重继承、完整反射生成或 Vulkan 资源回收。
- 产物：新增正文与本提纲，更新 CPP-01 的继续阅读、系列 plan、两个阅读入口与根路由的 Piccolo 描述。复用 reading-series 样式。
- 验证：固定源码逐项核对；编译运行从正文提取的 C++17 完整程序，另核对错误 override 的编译诊断；Hugo 构建、导航及浏览器检查。Piccolo 本身不构建、不启动、不调试；不提交或推送。

## 正文提纲

1. 从同一行 tick 为什么进入不同实现提出问题。
2. 最小模型：类型描述、对象实例、基类部分、访问接口与实际对象。
3. 先读声明：public 继承、成员访问、声明与定义；拆开 CLASS 宏得到普通 C++ 类头。
4. 组件调用链：ReflectionPtr 的 operator-> 返回 Component*，GObject 筛选更新，虚调用到具体组件；基类空实现不是纯虚函数。
5. override 保证覆盖关系，区别重载与隐藏；final 封闭派生或再次覆盖，不能据此推断机器码和性能。
6. RHI：抽象接口、VulkanRHI 创建赋值、prepareContext 分派、纯虚析构仍有定义。
7. Component* 删除链与虚析构；语言析构路径正确不等于项目资源回收完整。
8. 基类引用与基类值复制的区别，按值切片不会保留派生行为；智能指针向上转换不切片。
9. 构造析构期间的虚调用限制，不能假设基类构造函数会调用派生初始化。
10. 一个完整独立程序观察非虚调用、覆盖、限定调用、切片与虚析构；静态阅读练习和继续阅读。

## 固定源码证据

路径相对于 Piccolo 仓库；正文链接固定到上述提交。

| 文件 / 符号 | 核对结果 |
| --- | --- |
| `engine/source/runtime/function/framework/component/component.h` | protected 状态，公开虚析构；postLoadResource 和 tick 均有默认实现，Component 非抽象 |
| `engine/source/runtime/core/meta/reflection/reflection.h` | 普通编译分支 CLASS 展开成 class；ReflectionPtr<T>::operator-> 返回 T*；删除宏执行 delete 后将当前包装置空 |
| `engine/source/runtime/function/framework/component/mesh/mesh_component.h`、`.cpp` | public Component；tick/postLoadResource 使用 override；加载函数自行设置 m_parent_object，tick 处理脏网格数据 |
| `engine/source/runtime/function/framework/component/transform/transform_component.h`、`.cpp` | public Component；tick/postLoadResource 覆盖，tick 交换缓冲索引并处理状态 |
| `engine/source/runtime/function/framework/object/object.h`、`.cpp` | ReflectionPtr<Component> 容器；shouldComponentTick 筛选后 tick；加载后 postLoadResource；析构执行组件删除宏 |
| `engine/source/runtime/function/render/render_system.h`、`.cpp` | m_rhi 为 shared_ptr<RHI>；initialize 创建 VulkanRHI；tick 调用 prepareContext |
| `engine/source/runtime/function/render/interface/rhi.h` | RHI 纯虚接口；virtual ~RHI() = 0，文件末尾 inline RHI::~RHI() = default |
| `engine/source/runtime/function/render/interface/vulkan/vulkan_rhi.h`、`.cpp` | VulkanRHI final : public RHI；initialize/prepareContext 为 override final；prepareContext 选择当前帧命令缓冲；析构函数体目前只有 TODO |

## 语言规则资料

核对 C++ 工作草案的 class.derived、class.access.base、class.virtual、class.abstract、expr.call、class.copy.ctor、class.cdtor、class.dtor 和 expr.delete。正文使用 C++17 范围内的规则；不把当前草案新增机制带入示例。

## 验证记录

- 固定源码 SHA 已回读为 `f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，源码工作区无改动；逐项核对上表的继承、创建、筛选、调用和删除路径。
- 教学程序从正文完整代码块提取，使用已有 Windows x64 GCC 8.3.0，参数 `-std=c++17 -O0 -g -Wall -Wextra -pedantic`。编译与运行退出码均为 0，无编译诊断，全部断言通过，实际输出与正文九行逐行一致。
- 错误 override 片段同样从正文提取，用 `-std=c++17 -Wall -Wextra -pedantic -fsyntax-only` 检查；退出码 1，报告 `marked 'override', but does not override`，符合该片段的教学预期。
- 15 个固定源码引用对应本地存在的文件或目录，4 个正文 relref 均存在。新篇、CPP-01、架构地图、Piccolo 首页与引擎首页共 164 处生成后本地链接、锚点及资源引用通过检查。
- Hugo v0.158.0 构建退出码 0，生成 1345 页、52 个静态文件，ERROR 为零。
- 浏览器确认复用 reading-series 样式、引擎与 Piccolo 面包屑、两张表和一张继承图 SVG；无图语法错误和页面整体横向溢出。新篇的上一篇指向 CPP-01，返回目录为 Piccolo 首页的 chapters 锚点。
- 自查区分了默认虚实现与纯虚接口、覆盖与隐藏、虚调用与显式限定调用、切片与指针转换、所有权与虚析构；VulkanRHI 析构 TODO 未被表述成已完成的资源回收。
- Piccolo 未构建、未启动、未调试；站点只做本地写作和验证，未提交或推送。工作区其他任务改动保留。
