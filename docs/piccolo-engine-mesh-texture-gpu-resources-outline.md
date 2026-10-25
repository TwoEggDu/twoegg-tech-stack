# Piccolo 15：网格与纹理怎样从文件成为 GPU 资源

> 执行层提纲与证据记录；篇级状态由 `piccolo-engine-series-plan.md` 统一维护。

## 定位与边界

- 用户确认先写 11 相机，再写 15 GPU 资源、16 材质光照与阴影；本轮只落地 15。
- 先建立文件、CPU 载荷、资源身份、设备资源和绑定的通用模型，再沿固定版本 Piccolo 追一份 Wall 网格与 gold 材质纹理。
- 固定源码 `BoomingTech/Piccolo@f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，不修改引擎、共享模板或 CSS，不提交推送。
- 不重复 05 的反射恢复、06 的矩阵更新、07 的整帧提交、11 的相机；本篇停止在资源能被 Draw/Shader 访问的交接，表面颜色计算留给 16。
- 新正文 series_order=18，接在 11 后；既有 18 篇顺序保留，09—18 中仅 11/15 成稿，其余保持待写。

## 执行清单

- [x] 核对 MeshComponent 描述、资源路径与 processSwapData 的身份和缓存分支。
- [x] 核对 OBJ/JSON 的 CPU 顶点索引与纹理解码、色彩空间、失败返回。
- [x] 核对顶点拆流、staging/VMA、Image/mip/layout、ImageView/Sampler/描述符与 Draw 绑定。
- [x] 区分局部载荷、临时上传资源和长期设备缓存的生命周期，说明 queue wait 与释放条件。
- [x] 完成正文、提纲与资源数值/缓存练习，登记源码、独立 CPU 检查与 GPU 运行的不同范围。
- [x] 更新导航、canonical 状态和系列汇总，运行 Hugo、固定源码/链接与生成页检查。

## 正文提纲

1. 两个实例共享一份内容：文件、CPU 字节、资源 ID、Buffer/Image 与绑定各负责什么。
2. 从 Wall_1 到 Wall 定义及 gold.material.json，MeshComponent 发布路径；网格键是路径，材质键是五个纹理路径，标识分配不是成功加载证明。
3. OBJ 经 tinyobj 展开三角形角点，JSON 提供顶点/索引/绑定；CPU 结构与 GPU 三路顶点流的对应，uint16 索引边界。
4. host-visible coherent staging，写入后 copy 到 VMA GPU_ONLY 缓冲；copy 辅助函数提交并等待 graphics queue idle，解释成本和释放前提。
5. stb 解码 RGBA8、base color 的 sRGB 选择与数据纹理；TextureData 只是 CPU 图像，缺失路径会走回退，回退字节需与格式匹配。
6. createGlobalImage 根据格式算字节，创建 Image、上传第 0 层、生成 mip、最终 shader read layout、建立 ImageView。
7. Image、View、Sampler、DescriptorSet 分工，材质 set=2，网格 vertex/index 绑定；更新描述符不是上传或布局转换。
8. 首次/重复/移动/新材质/删除/修改同路径文件的条件表；区分资源创建复用与完整流式加载、失败恢复、回收系统。
9. 用固定资源和原 tinyobj/stb、GuidAllocator/资源键做独立 CPU 检查；保留后续断点与 GPU 捕获表。

## 证据入口

- engine/asset/level/1-1.level.json；objects/environment/wall/wall.object.json；wall/components/mesh/wall.obj；objects/environment/_material/gold.material.json；objects/_textures/gold.tga、mr.tga、n.tga。
- engine/source/runtime/function/framework/component/mesh/mesh_component.cpp。
- engine/source/runtime/function/render/render_system.cpp、render_resource_base.cpp/.h、render_resource.cpp/.h、render_type.h、render_mesh.h、render_guid_allocator.h、render_scene.cpp、render_common.h。
- engine/source/runtime/function/render/interface/rhi.h；interface/vulkan/vulkan_rhi.cpp、vulkan_util.cpp；passes/main_camera_pass.cpp；engine/shader/glsl/mesh_gbuffer.frag。
- Vulkan 官方资料：vkQueueWaitIdle、VkDescriptorImageInfo、Memory Allocation；只使用与固定实现相关的基础语义，不引入新扩展能力。

## 验证记录

- 2026-10-25 回读固定 SHA，克隆工作区干净，未改引擎源码。六段 C++/GLSL 节选去空白后匹配原文件，17 个固定版本链接均可对应本地文件、目录或许可证；三个 Vulkan 官方页面核对基础语义，不把新扩展套到固定源码。
- 独立检查目录 `%TEMP%/TechStackShow-piccolo-gpu-resources-15-20261025/`，源码 `resource_checks.cpp` 的 SHA-256 为 `bf57c6a3f817ec7da0a6c22c5718a090a9f4283a65761cb97d00d39acc492bbe`。使用 Windows x64 GCC 8.3.0，`-std=c++17 -O0`，include 路径为固定 engine/source、3rdparty/json11、stb、tinyobjloader。程序直接包含原数学/资源类型与 GuidAllocator 头，并编译原 tinyobj/stb 实现；编译退出 0，运行退出 0。
- OBJ 实际读取：8 个位置、12 个三角形，按原加载器逐角点展开规则为 36 个顶点；CPU 顶点结构 44 字节，对应 1584 字节顶点载荷和 72 字节 uint16 索引载荷。未调用完整 loadStaticMesh，加载器展开过程由源码核对，解析和统计由独立程序完成。
- 三张 TGA 实际解码为 RGBA8：gold 1024×1024、第 0 层 4194304 字节、计算 11 层；mr 64×64、16384 字节、7 层；n 512×512、1048576 字节、10 层。计算完整 mip 像素载荷分别为 5592404、21844、1398100 字节，不是 VMA 实测分配量。
- 原资源键/GuidAllocator 检查重复网格与材质取得相同身份，改 normal 路径取得不同材质身份；两份材质仍可引用相同 base color 路径，不能从材质缓存推断全局纹理 Image 去重。检查分配身份不等于执行或验证设备资源创建。
- 缺失文件由 stb 返回 nullptr；在本次 IEEE 754 小端环境中，float[4] 回退数组前四字节为 0,0,0,63，验证的是字节与 RGBA8 解释不匹配。未运行 GPU 采样，未修改回退实现。
- 源码核对 copyBuffer/图像辅助路径到 endSingleTimeCommands 的 graphics queue submit/wait idle，正常等待完成与释放暂存资源分别说明；材质身份先分配、设备缓存先插入再构造的失败边界保留，未宣称有自动失败恢复协议。核对 RenderResource::clear 为空、对象删除未逐项回收共享资源，不登记为完整回收闭环。
- 首次整站构建被同期新增 CXX-02 中三个旧名称 relref 阻塞。确认目标存在后，仅修正 reading/unreal.md → reading/unreal-engine.md、reading/unity.md → reading/unity-engine.md、piccolo-cpp-02-inheritance-lifecycle.md → piccolo-cpp-02-classes-inheritance-virtual-functions.md；该并行文章的其他内容保留。此为构建所需的小范围链接修复，不把该文章登记为本轮新写产物。
- Hugo v0.158.0 随后构建退出 0、ERROR 为零，生成 1372 页、54 个静态文件，输出/缓存为上述目录的 public/cache。19 篇 Piccolo、两级阅读入口、游戏图形系统入口和 CXX-02 共 23 个生成页，1341 处站内链接、锚点及资源引用检查通过；原 18 篇顺序保留，series_order 连续唯一为 0～18。
- 11 下一篇与 15 上一篇双向对应，15 末页按钮返回目录 chapters；新页沿用 series-reading.css，包含一段 Mermaid 图源。图源与 HTML 静态结构检查不等于浏览器目视或 Mermaid 实际渲染验收。
- canonical 表登记 15 已写，09—18 中其余八篇仍待写，下一篇优先 16；根 doc-plan 仅改系列级汇总。11、06/07、GFX-01/02 与相关入口已回链。游戏图形系统入口同步用户最新“通用原理或 Piccolo”范围，移除必须扩展其他引擎的旧推进承诺。
- 未执行完整 Piccolo、RenderResource、VMA、Vulkan、GPU mip/采样或回收；运行时断点、画面、GPU 捕获与浏览器目视仍待验证。未改共享模板/CSS，未暂存、提交或推送本轮文件。
