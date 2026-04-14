# URP 系列待补清单（只有你能做的部分）

> 这个文件不会发布。包含两类待办：实测数据/截图 + 项目经验叙事。
> 完成一项后在前面打 `[x]`，并记录数据来源。
> 
> **分工说明**：文章重写、结构调整、深度补充由 AI 负责。
> 下面列的全部是**只有你能做的事**——需要真机、真实项目经验、或编辑器操作。

---

## 零、项目经验叙事（4 处 EXPERIENCE-TODO）

这 4 处是整个系列从"AI 方法论"变成"做过项目的人写的方法论"的关键。
每处只需要 3-5 句话的脱敏项目经验。搜索 `EXPERIENCE-TODO` 可定位。

- [ ] **urp-platform-03-online-governance.md（第 37 行）**
  - 需要：一次线上分档出错的真实案例
  - 框架：某款设备 / GPU 家族在系统升级后表现异常 → 怎么发现 → 怎么修 → 修完怎么验证
  - 例：「我们在 X 项目上线 3 个月后遇到了…骁龙 778G 在 Android 13 升级后…」

- [ ] **urp-platform-04-thermal-and-dynamic-tiering.md（第 38 行）**
  - 需要：第一次发现热机掉帧的真实排查过程
  - 框架：QA 报了什么 bug → 最初怀疑什么 → 用什么工具发现是热机 → 最终做了什么
  - **额外**：如果你实际调过状态机，把 1.15x / 8 秒 / 30 秒这些阈值替换成真实调出来的数字

- [ ] **urp-lighting-02-shadow.md（第 134 行）**
  - 需要：一次 Shadow Bias 调参的真实经历
  - 框架：美术反馈什么问题 → 最初以为是什么 → 实际是 Bias 问题 → 最终用什么值

- [ ] **urp-ext-01-renderer-feature.md（第 251 行）**
  - 需要：项目里用 Renderer Feature 做了什么真实效果
  - 框架：列 2-3 个真实例子（受击闪白、径向模糊、低血量红屏等）→ 为什么不用 Volume → Feature 的优势

---

## 一、高优先级：核心性能数值（需要 Profiler 截图）

### 1. Shadow 渲染开销（urp-lighting-02-shadow.md）

- [ ] **第 20 行**："约占总渲染时间的 20-40%"
  - 需要：用 Snapdragon Profiler 或 Xcode GPU Capture，4 Cascade / 1024 Atlas，测一帧的 Shadow Pass 耗时 vs 总帧时间
  - 建议设备：中端 Android（骁龙 778G / 870）+ iPhone 12/13
  - 产出：一张 Profiler 截图 + 一行标注百分比

- [ ] **第 220-228 行**：Shadow 开销表（1.5ms / 3ms / 8-12ms）
  - 需要：同一场景，分别测 1 Cascade 512、2 Cascade 1024、4 Cascade 2048 的 Shadow Pass 耗时
  - 建议设备：低端（骁龙 680）、中端（骁龙 870）、高端（骁龙 8 Gen 2）各测一组
  - 产出：表格填入实测数据，替换当前的估算值

- [ ] **第 198 行**："Medium 在中端手机上会消耗约 2-5ms"
  - 需要：Soft Shadow Quality 分别设 Low/Medium/High，测各档耗时
  - 产出：三行数据

### 2. SSAO 移动端开销（urp-lighting-03-ambient-occlusion.md）

- [ ] **第 171 行**："SSAO 完整开启在中低端手机上通常消耗 3-8ms（1080P）"
  - 需要：同一场景，SSAO 开/关对比，1080P 分辨率
  - 建议设备：中端 Android + iPhone
  - 产出：开关对比的帧时间截图

- [ ] **第 175-180 行**：SSAO 降质项表格（~75%、~30%）
  - 需要：半分辨率 SSAO vs 全分辨率，测实际节省
  - 产出：填入实测百分比

### 3. 移动端配置优化收益（urp-platform-01-mobile.md）

- [ ] **第 158 行**："帧时间可以节省 15-30%"（关闭 Additional Lights Shadow）
  - 需要：有 4+ 附加光源的场景，开关附加光阴影对比
  - 产出：帧时间对比数据

- [ ] **第 101 行**："带宽节省可以很显著"（Native RenderPass）
  - 需要：Xcode GPU Frame Capture 或 Mali Graphics Debugger，开关 Native RenderPass 对比
  - 产出：Load/Store 次数对比截图，或带宽数值对比

---

## 二、中优先级：效果对比截图（需要 Game View 截图）

### 4. MSAA 对比（urp-config-01-pipeline-asset.md）

- [ ] **第 68-77 行**：MSAA Disabled vs 2x vs 4x
  - 需要：同一 Cube/角色边缘的 Game View 截图，三档各一张
  - 建议：靠近几何边缘截图，分辨率 1080P，关闭 FXAA 后对比
  - 产出：3 张截图，标注 Disabled / 2x / 4x

### 5. Render Scale 对比（urp-config-01-pipeline-asset.md）

- [ ] **第 96-102 行**：Render Scale 0.5 vs 0.75 vs 1.0
  - 需要：同一场景的 Game View 截图，三档各一张
  - 建议：注意观察纹理细节和几何边缘的模糊程度
  - 产出：3 张截图

### 6. Shadow Cascade 可视化（urp-lighting-02-shadow.md）

- [ ] **第 56-88 行**：Cascade 分割的 Scene View 可视化
  - 需要：打开 Scene View 的 Shadow Cascade 可视化模式（Scene View 左上角 → Overdraw / Cascades）
  - 分别截 2 Cascade 和 4 Cascade 的分割区域颜色图
  - 产出：2 张截图

### 7. Shadow Bias 对比（urp-lighting-02-shadow.md）

- [ ] **Bias 参数段落**：Shadow Acne vs Peter Panning
  - 需要：Depth Bias = 0（acne 明显）、Depth Bias 过大（peter panning 明显）、Depth Bias 合理值，各截一张
  - 产出：3 张截图

### 8. SSAO 效果对比（urp-lighting-03-ambient-occlusion.md）

- [ ] **第 117 行**：全分辨率 vs 半分辨率 SSAO 边缘差异
  - 需要：同一角落/缝隙区域，全分辨率和半分辨率各一张
  - 产出：2 张截图

### 9. HDR 开关对比（urp-config-01-pipeline-asset.md）

- [ ] **第 58-66 行**：HDR 关闭 vs 开启，Bloom 效果差异
  - 需要：有高光物体的场景，开/关 HDR 各截一张（注意 Bloom 的过曝区域）
  - 产出：2 张截图

---

## 三、低优先级：架构验证截图

### 10. Native RenderPass 合并验证（urp-config-02-renderer-settings.md）

- [ ] **第 147 行**：Frame Debugger 截图，显示 Pass 合并前后的差异
  - 需要：Native RenderPass 开/关，Frame Debugger 里的 Pass 列表对比
  - 产出：2 张 Frame Debugger 截图

### 11. Camera Stack Frame Debugger（urp-config-03-camera-stack.md）

- [ ] **动手验证段落**：两个 Camera 的 Draw Call 分组
  - 需要：Base + Overlay Camera 场景的 Frame Debugger 截图，标注哪些 Draw Call 属于哪个 Camera
  - 产出：1 张 Frame Debugger 截图

### 12. CommandBuffer Read-Write Hazard（urp-pre-01-commandbuffer.md）

- [ ] **第 81 行**："差一帧"或"条带"的画面表现
  - 需要：故意制造 Read-Write Hazard（src=dst）在移动端的截图
  - 产出：1 张异常画面截图 + 1 张修复后正常画面

### 13. Thermal 降档帧时间曲线（urp-platform-04-thermal-and-dynamic-tiering.md）

- [ ] **第 33-34 行**：冷机 → 热机的帧时间变化曲线
  - 需要：用 Unity Profiler 或 Perfdog，录制一台中端手机从冷机开始连续运行 15-20 分钟的帧时间曲线
  - 标注从哪个时间点开始帧时间明显抬升
  - 产出：1 张帧时间曲线图

---

## 完成标准

每项数据补充后，需要：
1. 截图保存到 `static/images/urp/` 目录，命名格式 `{article-slug}-{description}.png`
2. 在对应文章的数值位置插入截图引用或更新数值
3. 标注测试设备型号、Unity 版本、URP 版本

---

## 你的工作计划（按时间块安排）

### 第一批：效果对比截图（约 1 小时，只需要编辑器 + Game View）

这批不需要真机，编辑器里就能做，门槛最低：

1. MSAA 对比（3 张）— 改 Pipeline Asset 截 Game View
2. Render Scale 对比（3 张）— 同上
3. HDR 开关 + Bloom 对比（2 张）— 需要场景里有高光物体
4. Shadow Bias 对比（3 张）— 调 Bias 参数截图
5. Cascade 可视化（2 张）— Scene View 切 Cascade 视图
6. SSAO 效果对比（2 张）— 开关 SSAO 截图

共 15 张截图，存到 `static/images/urp/`。

### 第二批：真机 Profiler 数据（约 2-3 小时，需要真机 + Development Build）

需要至少一台中端 Android 或 iPhone：

1. Shadow Pass 耗时百分比（1 张 Profiler 截图 + 1 行数据）
2. Shadow 各档位耗时表（填 3x3 表格）
3. Soft Shadow 各档位耗时（填 3 行数据）
4. SSAO 开/关帧时间对比（1 张截图 + 2 个数值）
5. Additional Lights Shadow 收益（2 个数值）

### 第三批：高级验证（约 1-2 小时，需要 Xcode 或 Snapdragon Profiler）

如果有 iOS 开发环境或 Qualcomm 设备：

1. Native RenderPass 带宽对比（1 张 Xcode 截图）
2. 热机帧时间曲线（1 张 Perfdog 截图，需要跑 15 分钟）
3. Read-Write Hazard 异常画面（1 张移动端截图）

### 建议节奏

- 有空的时候先做第一批（不需要真机，1 小时搞定）
- 周末抽时间做第二批（需要打包到真机）
- 第三批看你手边有没有工具，有就做，没有先跳过
