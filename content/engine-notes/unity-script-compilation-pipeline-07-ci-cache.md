---
date: "2026-03-28"
title: "Unity 脚本编译管线 07｜CI 编译缓存：Library 哪些能缓存、哪些不能"
description: "CI 上每次都全量重编是因为 Library/ 没有缓存。解释 Unity 编译产物的结构，说明哪些目录值得缓存、哪些必须强制失效，以及 Jenkins 和 GitHub Actions 的典型缓存配置。"
slug: "unity-script-compilation-pipeline-07-ci-cache"
weight: 68
featured: false
tags:
  - "Unity"
  - "CI"
  - "Build"
  - "Cache"
  - "Jenkins"
  - "GitHub Actions"
series: "Unity 脚本编译管线"
series_order: 7
---

> CI 打包慢，大概率不是你的代码有多复杂，而是每次都在做本地从不重复的工作：全量解压 Package、全量编译脚本、全量跑 ILPP。把 `Library/` 里对的目录缓存起来，CI 耗时可以从 20 分钟降到接近本地的 5 分钟。

## 这篇要回答什么

- Unity 的编译产物具体在 `Library/` 的哪些子目录
- 哪些目录值得缓存，哪些缓存了反而出问题
- 什么情况下必须让缓存失效
- Jenkins 和 GitHub Actions 的缓存配置怎么写

---

## CI 和本地的时间差从哪里来

本地第二次打包快，是因为 `Library/` 已经存在：

- `Bee/` 里有上次的依赖图和中间产物，bee_backend 可以做增量判断
- `PackageCache/` 里 Package 已经解压好，不用重新下载解压
- `ScriptAssemblies/` 里有编译好的 .dll，没改动的程序集直接跳过

CI 每次从干净环境启动，这三件事全部重来一遍。差距就在这里。

---

## Unity 编译产物在哪里

```
Library/
  ScriptAssemblies/       ← 编译好的 .dll（编辑器模式用）
  Bee/                    ← bee_backend 工作目录
    artifacts/            ← 中间产物（.dll、.pdb、.o 等）
    1300b0aEDbg.dag        ← 依赖图（增量编译的核心状态）
    *.traceevents          ← 构建追踪日志（不需要缓存）
  PackageCache/           ← Package 解压缓存（几乎不变，收益高）
  il2cpp/                 ← IL2CPP 中间产物（打包时生成，Unity 2021+ 支持增量）
  ShaderCache/            ← Shader 编译缓存（大项目明显）
Temp/                     ← 运行时临时文件（每次重建，不缓存）
```

`Bee/` 是整个增量编译的大脑。依赖图 `.dag` 文件记录了每个输入文件的 hash 和上次的输出，bee_backend 靠它决定哪些程序集需要重编。没有这个文件，就是全量重编。

---

## 哪些目录值得缓存

| 目录 | 是否缓存 | 原因 |
|------|---------|------|
| `Library/Bee/` | ✓ 值得 | bee_backend 增量编译核心，缓存命中时大量程序集直接跳过 |
| `Library/ScriptAssemblies/` | ✓ 值得 | 编译好的 .dll，配合 `Bee/` 效果最好 |
| `Library/PackageCache/` | ✓ 强烈推荐 | Package 解压很慢，而且几乎不随代码改动变化 |
| `Library/il2cpp/` | ✓ 有条件 | Unity 2021+ 支持增量 IL2CPP，值得缓存；旧版本意义不大 |
| `Library/ShaderCache/` | ✓ 有价值 | Shader 编译缓存，项目 Shader 多时省时明显 |
| `Library/Bee/*.traceevents` | ✗ 不需要 | 只是构建追踪日志，缓存没有意义 |
| `Temp/` | ✗ 不缓存 | 每次运行时重建，缓存旧数据反而可能出错 |
| `Library/` 整个目录 | ✗ 不推荐 | 太大，且部分子目录不能跨版本复用，Key 设计也很难准确 |

---

## 缓存失效的触发条件

缓存最怕的不是"没命中"，而是"命中了错误的缓存"，导致编译出来的产物不对。

### 必须强制清空缓存的情况

**Unity 版本升级**
`Library/` 的格式与 Unity 版本强绑定。升级 Unity 后必须丢弃整个缓存，否则轻则报错，重则编出的包运行异常。

**Package 版本变化**
`PackageCache/` 中每个 Package 以版本号为目录名存储，理论上旧版本目录自动失效。但如果整体缓存 Key 不更新，rsync/restore 可能带入旧版本文件。最安全的做法是把 `Packages/packages-lock.json` 的 hash 纳入缓存 Key。

### 不需要清空、缓存仍然有效的情况

**Define Symbols 改变**
Symbols 改变会触发全量重编，但 `Bee/` 的依赖图机制会正确处理这种变化——它会重编受影响的程序集，结果写回同一个缓存位置。下次如果 Symbols 改回去，又能命中旧缓存。不需要手动失效。

**.asmdef 结构改变（新增/删除程序集）**
bee_backend 会重新计算依赖图，受影响程序集重编。已有的无关程序集缓存仍然有效。

### 缓存 Key 的推荐设计

```
unity-{UNITY_VERSION}-{hash(Packages/packages-lock.json)}
```

- `UNITY_VERSION` 保证 Unity 升级后缓存自动失效
- `packages-lock.json` hash 保证 Package 变化后失效
- 代码改动**不需要**纳入 Key，bee_backend 自己会处理增量

---

## Jenkins 配置示例

通过 rsync 把缓存目录存到共享磁盘，用 Unity 版本和 packages-lock.json hash 作为路径。

```groovy
// Jenkinsfile 片段
pipeline {
    environment {
        UNITY_VERSION = '2022.3.20f1'
        // 提前计算 packages-lock.json 的 hash
        PACKAGES_HASH = sh(
            script: "sha1sum Packages/packages-lock.json | cut -c1-8",
            returnStdout: true
        ).trim()
        CACHE_BASE = "/cache/unity/${UNITY_VERSION}/${PACKAGES_HASH}"
    }
    stages {
        stage('Restore Cache') {
            steps {
                sh 'rsync -a ${CACHE_BASE}/Library/Bee/ ./Library/Bee/ || true'
                sh 'rsync -a ${CACHE_BASE}/Library/PackageCache/ ./Library/PackageCache/ || true'
                sh 'rsync -a ${CACHE_BASE}/Library/ScriptAssemblies/ ./Library/ScriptAssemblies/ || true'
            }
        }
        stage('Build') {
            steps {
                sh '${UNITY_PATH} -batchmode -projectPath . -buildTarget Android -executeMethod Builder.Build -quit'
            }
        }
        stage('Save Cache') {
            steps {
                sh 'rsync -a ./Library/Bee/ ${CACHE_BASE}/Library/Bee/'
                sh 'rsync -a ./Library/PackageCache/ ${CACHE_BASE}/Library/PackageCache/'
                sh 'rsync -a ./Library/ScriptAssemblies/ ${CACHE_BASE}/Library/ScriptAssemblies/'
            }
        }
    }
}
```

注意 `|| true` 的作用：第一次运行时缓存目录不存在，rsync 会报错，加 `|| true` 让 stage 不因此失败。

---

## GitHub Actions 配置示例

`actions/cache` 原生支持多路径和 `restore-keys` 降级匹配，适合 Unity 缓存场景。

```yaml
- name: Compute packages hash
  run: echo "PACKAGES_HASH=$(sha1sum Packages/packages-lock.json | cut -c1-8)" >> $GITHUB_ENV

- name: Cache Unity Library
  uses: actions/cache@v3
  with:
    path: |
      Library/Bee
      Library/ScriptAssemblies
      Library/PackageCache
    # 精确 Key：Unity 版本 + Package hash
    key: unity-${{ env.UNITY_VERSION }}-${{ env.PACKAGES_HASH }}
    # 降级 Key：Package 有小变化时，先用旧缓存再增量
    restore-keys: |
      unity-${{ env.UNITY_VERSION }}-

- name: Build
  run: |
    $UNITY_PATH -batchmode -projectPath . -buildTarget Android \
      -executeMethod Builder.Build -quit
```

`restore-keys` 的作用：当精确 Key 未命中时（比如刚更新了某个 Package），用 `unity-{版本}-` 前缀匹配最近的一次缓存。即使 PackageCache 有一个包过期，其他包的缓存仍然有效，bee_backend 只会重新处理变化的部分。

---

## 常见误区

**把整个 `Library/` 打包缓存**
`Library/` 在中等项目里可能有几个 GB，上传下载本身就要好几分钟，抵消了缓存收益。更麻烦的是，`Library/` 里有些文件（比如 asset 导入产物）与平台、贴图压缩格式绑定，换个构建节点就可能出问题。按目录精确缓存是更稳的做法。

**缓存不设过期或清理策略**
`Library/Bee/artifacts/` 是增量产物堆叠的地方，随着时间积累会持续膨胀。建议在 CI 系统里设置缓存目录的 TTL（Jenkins 可以用 cron 定期清理，GitHub Actions cache 有 7 天自动过期）。

**只缓存 `ScriptAssemblies/`，不缓存 `Bee/`**
`ScriptAssemblies/` 里只有最终 .dll，没有依赖图。Unity 启动时如果找不到 `Bee/` 的依赖图，还是会走全量重编流程，结果只是把 .dll 原地覆盖了一遍。两者要配合缓存才有效。

**忽略 `PackageCache/`**
这是收益最稳定、风险最低的缓存。Package 内容几乎不随业务代码变化，但解压过程可能占用 2–5 分钟（取决于依赖 Package 的数量）。即使其他缓存都不设，`PackageCache/` 也值得单独配置。

---

## 实际收益参考

| 缓存项 | 典型收益 | 备注 |
|--------|---------|------|
| `PackageCache/` | 首次命中省 2–5 分钟 | 几乎与代码改动无关，命中率极高 |
| `Bee/` + `ScriptAssemblies/` | 无代码改动时近乎 0 重编时间 | 有代码改动时省 50%–80% |
| `il2cpp/`（Unity 2021+） | IL2CPP 打包省 3–10 分钟 | 取决于代码量，首包收益最大 |
| `ShaderCache/` | 省 1–3 分钟 | Shader 多的项目更明显 |

三项合计，CI 耗时从 20 分钟降到 5–8 分钟是合理预期，与本地差距基本消除。

---

## 小结

- `Library/Bee/`、`Library/ScriptAssemblies/`、`Library/PackageCache/` 是最值得缓存的三个目录
- 缓存 Key 用 **Unity 版本 + `packages-lock.json` hash** 组合，覆盖两个最重要的失效场景
- Unity 版本升级后必须清空缓存；代码改动、Symbols 变化不需要手动失效
- 不要整个 `Library/` 打包，按目录精确缓存收益更高、风险更低

---

- 上一篇：[Unity 脚本编译管线 06｜.asmdef 设计：如何分包让增量编译更快]({{< relref "engine-notes/unity-script-compilation-pipeline-06-asmdef-design.md" >}})
- 下一篇：[Unity 脚本编译管线 08｜编译报错排查：从错误信息定位根因]({{< relref "engine-notes/unity-script-compilation-pipeline-08-compilation-errors.md" >}})
