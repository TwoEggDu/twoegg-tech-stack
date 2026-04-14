---
title: "服务端架构与构建 05｜服务端编译与容器化——.NET 构建、Docker 镜像与 CI"
slug: "delivery-server-architecture-05-build-containerize"
date: "2026-04-14"
description: "服务端构建比客户端简单——单一平台、单一架构。但容器化引入了新的工程维度：镜像分层、多阶段构建、镜像大小控制和注册中心管理。"
tags:
  - "Delivery Engineering"
  - "Server"
  - "Docker"
  - ".NET"
  - "CI/CD"
series: "服务端架构与构建"
primary_series: "delivery-server-architecture"
series_role: "article"
series_order: 50
weight: 950
delivery_layer: "practice"
delivery_volume: "V10"
delivery_reading_lines:
  - "L3"
---

## 这篇解决什么问题

服务端构建的目标是产出可部署的产物。对 C# / .NET 服务端来说，这通常是一个 Docker 镜像。本篇覆盖从 .NET 编译到 Docker 镜像再到 CI 集成的完整链路。

## .NET 服务端构建

### 构建命令

```bash
# 发布为自包含的可执行文件
dotnet publish -c Release -r linux-x64 --self-contained true \
  -o ./publish/ -p:PublishSingleFile=true
```

关键参数：

| 参数 | 作用 | 推荐值 |
|------|------|--------|
| `-c Release` | 构建配置 | Release（生产必须） |
| `-r linux-x64` | 目标运行时 | 服务器通常 Linux x64 |
| `--self-contained` | 是否包含 .NET Runtime | true（不依赖服务器安装 Runtime） |
| `PublishSingleFile` | 是否合并为单文件 | true（简化部署） |
| `PublishTrimmed` | 是否裁剪未使用代码 | 视情况（裁剪可能影响反射） |

### NuGet 依赖管理

.NET 的包管理通过 NuGet。确保构建可重复：

- `Directory.Packages.props` 统一管理所有项目的依赖版本
- `nuget.lock` 文件锁定传递依赖（`dotnet restore --locked-mode`）
- CI 中使用 `--locked-mode` 还原——确保和开发环境一致

## Docker 容器化

### 多阶段构建

推荐使用多阶段 Dockerfile——构建阶段和运行阶段分离：

```dockerfile
# 构建阶段
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -r linux-x64 -o /app/publish

# 运行阶段
FROM mcr.microsoft.com/dotnet/runtime-deps:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["./GameServer"]
```

**多阶段构建的好处**：
- 运行镜像不包含 SDK 和构建中间产物——镜像体积显著减小
- 构建环境和运行环境隔离——运行镜像更安全

### 镜像大小控制

| 基础镜像 | 大小 | 适用 |
|---------|------|------|
| `dotnet/sdk` | ~700MB | 仅用于构建阶段 |
| `dotnet/aspnet` | ~200MB | 需要 ASP.NET 功能的服务 |
| `dotnet/runtime` | ~180MB | 纯 .NET 运行时 |
| `dotnet/runtime-deps` | ~10MB | 自包含应用（最小） |
| Alpine 变体 | 更小 | 适合对镜像大小敏感的环境 |

**推荐**：使用 `runtime-deps` + `PublishSingleFile` + `--self-contained`。最终镜像通常 50-100MB。

### 镜像标签策略

```
registry.example.com/game-server:v1.2.3-build456
                                  ↑版本号  ↑构建号
```

- 每次构建的镜像标签包含版本号和构建号
- `latest` 标签指向最新的稳定版本
- 不要覆盖已发布的标签——已部署的环境引用的标签必须不变

### 注册中心

Docker 镜像发布到容器注册中心：

| 方案 | 适用 |
|------|------|
| Docker Hub | 公开项目 |
| AWS ECR / GCR / ACR | 云服务商生态 |
| Harbor / GitLab Registry | 私有部署 |

CI 构建完成后自动推送到注册中心，部署系统从注册中心拉取镜像。

## CI 集成

### 服务端 CI 标准流程

```
1. Checkout 代码
2. dotnet restore --locked-mode（还原依赖）
3. dotnet build（编译）
4. dotnet test（运行测试）
5. dotnet publish（发布产物）
6. docker build（构建镜像）
7. docker push（推送到注册中心）
8. 触发部署管线（可选——自动部署到 staging 环境）
```

### 与客户端 CI 的协调

客户端和服务端的 CI 通常是独立的管线，但在以下情况需要联动：

- **Proto 变更**：协议文件变更时同时触发客户端和服务端 CI
- **Share 代码变更**：共享代码变更时同时触发
- **版本发布**：发版前确认客户端和服务端的 CI 都通过

## 常见错误做法

**Docker 镜像中包含 SDK**。使用 `dotnet/sdk` 作为运行镜像——镜像 700MB+，安全攻击面大。运行镜像必须只包含运行时。

**不锁定 NuGet 依赖**。两次构建可能还原不同版本的传递依赖。必须使用 `nuget.lock` + `--locked-mode`。

**镜像标签覆盖**。新构建的镜像覆盖了旧标签。回滚时发现旧标签已经指向新镜像。标签必须唯一且不可变。

## 小结

- [ ] .NET 构建是否使用 Release 配置 + self-contained
- [ ] Dockerfile 是否使用多阶段构建
- [ ] 运行镜像是否基于 runtime-deps（而非 sdk）
- [ ] 镜像标签是否包含版本号和构建号且不可变
- [ ] NuGet 依赖是否通过 lock 文件锁定
- [ ] CI 是否在测试通过后才构建和推送镜像

---

V10 服务端架构与构建到这里结束。

五篇文章覆盖了：客户端/服务端差异（01）、架构选型（02）、ET Framework（03）、Server ECS（04）和编译容器化（05）。

**推荐下一步**：V11 服务端部署与运维 — 服务端构建完成后怎样部署、监控和扩缩容

**扩展阅读**：[游戏后端基础设施系列]({{< relref "system-design/game-backend-series-index.md" >}}) — 服务器架构、部署和运维的完整功能设计
