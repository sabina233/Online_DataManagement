# 数据管理系统 - 后端 API (ASP.NET Core 10)

这是一个基于 .NET 10 的高性能聚合数据管理系统后端，为企业提供多维度的实际（AC）与预测（FC）数据管理、分析及大屏展示能力。

## 主要功能

- **JWT 安全认证**：引入基于 JSON Web Token 的身份验证机制，确保数据接口安全。
- **数据冲突检测**：在数据入库前自动检查是否存在冲突，保证数据一致性。
- **自动计算逻辑**：后台自动计算月度达成率、季度累计及汇总数据。
- **批量处理**：支持数据的大批量保存与更新。
- **Swagger 集成**：内置可视化 API 文档，支持直接在界面进行 Token 认证调试。

## 技术栈

- **框架**：ASP.NET Core 10.0 (Web API)
- **数据库**：Entity Framework Core 10 (支持 SQL Server 和 内存数据库)
- **认证**：Microsoft.AspNetCore.Authentication.JwtBearer
- **文档**：Swashbuckle (Swagger)

## 项目结构

```text
DataManagementApi/
├── Controllers/       # API 控制器 (Auth认证, Data业务数据, User用户管理)
├── Data/              # 数据库上下文 (AppDbContext)
├── Models/            # 数据模型类 (User, DataRecord)
├── appsettings.json   # 配置文件 (包含数据库连接串及 JWT 密钥)
└── Program.cs         # 应用启动、中间件及依赖注入配置
```

## 运行指南

1. **配置数据库**：在 `appsettings.json` 中修改 `DefaultConnection` 连接串。
2. **还原包**：在根目录执行 `dotnet restore`。
3. **运行**：执行 `dotnet run`。
4. **访问调试**：浏览器打开 `http://localhost:5071/swagger`。

## 安全性说明

> [!NOTE]
> 当前核心业务接口已启用 `[Authorize]` 保护。登录后需获取 Token 并通过 `Authorization: Bearer <token>` 请求头进行访问。

## 源码仓库

[https://github.com/sabina233/Online_DataManagement](https://github.com/sabina233/Online_DataManagement)
