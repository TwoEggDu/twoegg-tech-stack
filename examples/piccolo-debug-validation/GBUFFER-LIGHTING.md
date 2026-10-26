# Wall_1：GBuffer 像素与延迟光照的离线验证

2026-10-26，实验身份 `piccolo-gbuffer-lighting-20261026`。复用 [GBuffer Draw 实测](GBUFFER-DRAW.md) 的同进程捕获：`gbuffer_frame6348.rdc`、`gbuffer_frame28131.rdc`。本轮不重新运行或修改引擎，不改变先前回执；新增 `gbuffer-lighting-receipt.json` 保存捕获与材料指纹。

## 问题与验证链

目标仍是 GObjectID2 / part0 / 渲染实例5 / Draw实例槽0 的 Wall_1。验证以下边界：

```text
目标实例顶点与实际 viewport
    → 像素中心、三角形内部、预测深度
    → GBuffer 片元输入的世界位置与同批七实例归属
    → GBuffer A/B/C 与 D32 的实际字节
    → 延迟光照的 input attachments 与片元输入
    → Lo / La / Libl 与 RGBA16F 场景颜色读回
```

前后分别取点，不默认同一屏幕位置或同一对象就意味着同一表面位置。输出止于延迟光照；不覆盖粒子后续混合、tone mapping、UI、交换链图像或显示器扫描输出。

附件原始字节来自捕获回放后的GPU图像读回，并非原始捕获时另存的显存快照。它与shader调试值是两种观察途径，报告分别保留，不将其中任一者称为历史硬件逐位轨迹。

## 实际事件、附件与坐标

- 原始捕获 SHA-256 与 `gbuffer-receipt.json` 匹配，源基线为 Piccolo `f5053707fed4d3f94d270a436fb0d3a8ae54e3e5` 加先前两处兼容补丁。本轮没有新的引擎运行、补丁或构建。
- 捕获帧6348/28131都使用 GBuffer事件177；延迟光照事件由 `Deferred Lighting` marker 内唯一 Draw 定位，结果为186、subpass1、pipeline289、fragment shader288。
- 完整图像1280×720，场景viewport为x289/y60/width668/height448。像素坐标左上角为原点；重心插值在像素中心x+0.5/y+0.5计算。不是用整个窗口宽高投影。
- GBuffer A/B/C 资源247/251/255，格式分别为RGBA8 UNORM、RGBA8 UNORM、RGBA8 SRGB；相机深度145为D32。光照set1/binding0—3引用相同四资源及视图，输出259为RGBA16F。
- 原始字节在事件177和186逐点读回，三颜色和深度均一致；shader调试的GBuffer输入和scene_depth也与读回值一致。

| 项目 | 修改前主样本 | 修改后主样本 |
| --- | --- | --- |
| 像素 | (663,242) | (650,266) |
| 实例槽 / 几何三角形序号 | 0 / 4 | 0 / 4 |
| 深度读回 | 0.9629561305 | 0.9749283195 |
| 投影深度误差 | 4.09e-7 | 6.80e-7 |
| 插值世界位置对片元输入最大差 | 2.95e-5 | 1.01e-4 |
| RGBA16F实际RGB | (0.2073974609,0.2414550781,0.2556152344) | (0.1789550781,0.2093505859,0.2238769531) |

脚本将同一网格的源顶点逐一变换到七个实际实例，检查覆盖该像素的各个三角形，使用片元输入世界位置反查；1e-3世界坐标最大误差门限内均唯一匹配槽0。三角形编号由几何与索引推导，不从PixelHistory的primitive字段获得。

修改后另复查旧坐标(663,242)：它仍唯一匹配槽0，但从原三角形4变为三角形3，UV也变化。它没有在本次实验变成另一个对象；这个实测结果比“同坐标一定换对象”的假设更具体。前后主样本也不是严格同一UV的控制实验，不能用其颜色差独立推导“平移使墙变暗”。

## 表面记录与光照分支

修改后主样本的原始A RGB为[128,255,126]，B RGBA为[0,127,190,1]，C的sRGB字节为[96,84,60]。A解码法线约(0.00392163,1,-0.01176471)；B解码metallic0、specular127/255、roughness190/255、shadingModelID1。C在光照中实际取得的线性值为(0.1171875,0.0888671875,0.045166015625)。

GBuffer shader调试中写出前specular为0.5，实际UNORM字节为127；按实际输入核对，不把条件数学练习的理想值当成附件值。C存储字节与shader实际线性输入分别记录，不手工重复sRGB解码。A/C的alpha没有约定的表面语义，原数据仅保留供复核，不作为着色解释。

光照每帧buffer Resource149，effective byte offset4319248，range688。反射确认camera/ambient/point_light_num/scene_directional_light的成员偏移分别为64/80/96/592，方向光颜色位于该结构内偏移16。前后buffer指纹相同：camera(-5,0,3)、ambient(0.03,0.03,0.03)、point_light_count0、directional direction约(0.57735,-0.57735,0.57735)、color(1,1,1)。

修改后主样本的N·L约-0.581878，修改前约-0.577350；方向光分支未通过NoL>0判断。片元trace没有执行方向光closest_depth/current_depth/shadow对应变量，不能称为完成阴影比较验证。

| 修改后分量 | shader调试RGB |
| --- | --- |
| Lo | (0,0,0) |
| La | (0.0035156249,0.0026660156,0.0013549804) |
| Libl | (0.1755238175,0.2067140043,0.2225455940) |
| result_color | (0.1790394485,0.2093800157,0.2239005715) |

两点三项求和误差均小于7e-9，说明这个分支的颜色由环境补光与IBL保留；不能把Lo为零解释成阴影遮挡。IBL与阴影图的绑定保存在报告中，但不是对所有资源生成、过滤、BRDF与光源组合的完整验收。

## 回放调试值与实际读回怎样比较

shader调试值不是原始GPU逐位执行轨迹。脚本保存其SPIR-V SHA-256及本次`OpName`与调试变量的关联，不硬编码`_419`等中间ID；字段不存在或分支未执行时不补造值。

RGBA16F实际存储值同时通过`GetTextureData`原始half字节与`PickPixel`读回核对，两者一致。shader调试result与实际附件RGB均有微小差异，最大约0.692个对应FP16存储间隔。这里定义间隔为该已存储half指数段相邻值之差；脚本采用小于2个间隔的检查门限。没有证明差异仅来自最后一次量化，也没有要求回放调试与原GPU执行逐位相同。

PixelHistory返回事件128、139和177，但preMod/postMod/shaderOut的`IsValid()`均为False，不能读取其中占位字节当作颜色。其reported primitive全为0，不能据此宣称目标几何三角形为0。材料保留事件记录与有效性标志；颜色来自实际附件读回，对象/三角形归属来自几何、深度和片元输入交叉检查。

## 复跑

依赖已安装RenderDoc1.43和原始两份RDC，不需要另装Python模块。Windows GUI进程可能先向终端返回；以下等待方式避免把“启动成功”当成“回放完成”：

```powershell
Set-Location 'E:/sources/twoegg-tech-stack/examples/piccolo-debug-validation'
$env:PICCOLO_GBUFFER_LAB = Join-Path $env:TEMP 'PiccoloDebug20261026'
Start-Process -FilePath 'C:/Program Files/RenderDoc/qrenderdoc.exe' `
  -ArgumentList @('--python', (Join-Path $PWD 'gbuffer-lighting-replay.py')) `
  -WindowStyle Hidden -Wait
Get-Content (Join-Path $env:PICCOLO_GBUFFER_LAB 'gbuffer-lighting-report.json') -Raw
```

当前目录需有上一轮的`gbuffer-replay-report.json`，也可用`PICCOLO_GBUFFER_REPORT`指定路径。脚本验证原始RDC指纹，按此前目标顶点报告选择候选像素，重新查询实际viewport、反射与绑定。报告写入实验目录；必须确认更新时间、无`error`、`checks`均满足门限，不能只看GUI程序退出码。代码使用RenderDoc嵌入式Python3.6可用语法，不依赖`__file__`或第三方科学计算包。

本次脚本与导出：

- `gbuffer-lighting-replay.py`：最终已执行脚本。
- `gbuffer-lighting-report.json`：前后两个主样本与修改后旧坐标的完整导出，含布局、绑定、原始像素、归属、shader调试语义值及检查。
- `gbuffer-lighting-pixels.csv`：三个样本的像素、归属、深度、表面数据与光照输出摘要。
- `gbuffer-lighting-receipt.json`：源与捕获身份、脚本/报告/CSV/本文指纹和本地站点验证结果。

原始RDC继续留在本机实验目录，不加入公开站点仓库。只有导出数据时可以检查表内计算，不能独立重新执行GPU捕获回放。本轮沿用先前启动API-Version-Violation的已知限制，没有零验证错误或新增正常退出结论；离线回放通过不等于新一轮原生运行或线上部署通过。
