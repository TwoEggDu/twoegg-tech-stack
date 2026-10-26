# V-T / V-C 原创源素材

这是[纹理与通道最小验证工作单](../../docs/mobile-texture-channel-validation-workorder-2026-10-26.md)的源输入，不是 Unity 工程或实验结果。

运行 `python examples/mobile-texture-validation/generate_fixtures.py`，用 Python 标准库生成固定的三个 1024×1024 RGBA8 PNG、一个 UV 测试板 OBJ 和来源清单。所有图案与几何由本目录代码直接构造，没有使用外部图片、第三方模型、私有工程素材或 AI 生成贴图。

| 文件 | 用途与限制 |
| --- | --- |
| `fixtures/base-alpha.png` | 条纹、色块、斜线与梯度；Alpha 从左向右变化。RGB 细节用于尺寸/格式对照，Alpha 的使用必须由材质状态确认 |
| `fixtures/base-opaque.png` | 与上一张解码后的 RGB 逐字节相同，Alpha 全为 255；仍是 RGBA PNG，不能当作移除 Alpha 的格式结果 |
| `fixtures/metal-ao-smooth.png` | R/G/A 分别作为 metallic/occlusion/smoothness 候选，B 为诊断信号；只有正确接入对应 Shader 输入后才有这些语义 |
| `fixtures/uv-board.obj` | 原创平面，源数据 4 个位置、2 个三角形、完整 0—1 UV；导入后的顶点布局、法线/切线和显示朝向另查 |
| `fixtures/source-manifest.json` | 记录尺寸、源编码、文件字节、SHA256 和解码通道摘要；不填写 Unity、ASTC 或运行结果 |

源图 PNG 字节反映容器压缩，不等于 GPU 块数据或运行内存。两张 RGB 相同的 PNG，经不同块压缩后不保证 RGB 误差也相同；Alpha 消费对照先用相同未压缩格式隔离，再另做格式实验。

这些高对比诊断图案用于暴露采样与压缩差异，不能代表角色、UI、法线贴图或全部美术内容的接受条件。首轮不测绘制合批、整帧性能或“最佳块大小”。

当前只准备源素材；Unity 场景、材质、导入结果、APK、设备截图和运行记录均待执行。结果状态以工作单和实际回执为准。
