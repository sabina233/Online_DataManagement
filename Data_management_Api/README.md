# 数据管理系统 - 后端 (.NET Core API)

这是数据管理系统的后端服务，基于 **ASP.NET Core Web API** 构建。它提供安全的数据接口，支持基于 JWT 的身份验证，并连接 SQL Server 数据库进行持久化存储。

## 核心技术栈

- **框架**: ASP.NET Core 8.0 (或更早版本 compatible)
- **ORM**: Entity Framework Core (First-Code / Manual Migration)
- **数据库**: Microsoft SQL Server
- **文档**: Swagger UI
- **鉴权**: JWT (JSON Web Token)

## 目录结构

```text
DataManagementApi/
├── Controllers/     # API 控制器 (AuthController, BrandController, OrderController)
├── Data/            # 数据库上下文 (ApplicationDbContext)
├── Models/          # 实体模型 (User, Brand, KmartRecord 等)
├── DTOs/            # 数据传输对象 (LoginDto, RegisterDto)
└── Program.cs       # 程序入口与服务配置
```

## 核心功能模块

1.  **身份认证 (System Auth)**
    *   `/api/auth/login`: 用户登录，颁发 ISO 格式 JWT Token。
    *   `/api/auth/register`: 新用户注册（默认角色为 User）。
    *   **密码安全**: 使用 BCrypt 或强哈希算法（视具体实现）存储密码。

2.  **品牌数据管理 (Brand Management)**
    *   提供各品牌（如 Kmart, Target 等）的库存与订单数据增删改查。
    *   **动态 Schema**: 支持不同品牌拥有不同的数据字段结构。

3.  **Kmart 专项业务**
    *   `/api/orders/kmart/*`: 处理 Kmart 特有的 RFID 与订单数据。
    *   支持按月、按类目聚合查询。
    *   自动记录 `ModifiedBy` 字段，实现审计追踪。

## 快速开始

### 1. 数据库配置
确保本地安装了 SQL Server (Express 或 Developer 版)。
在 `appsettings.json` 中配置连接字符串：
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=DataManagementDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### 2. 数据库迁移
项目当前使用手动迁移脚本（在 `Program.cs` 中实现）。
只需运行项目，系统会自动检查并创建所需的表结构（`Users`, `Brands`, `KmartDailyRecords` 等）。

### 3. 运行项目
在项目根目录下运行：
```bash
dotnet run
```
或者使用 Visual Studio / VS Code 打开并启动调试。

### 4. 访问 API 文档
启动成功后，访问: `https://localhost:7123/swagger` (端口可能不同，请查看控制台输出) 可查看完整的 API 文档。

## 安全特性

- **CORS 策略**: 配置了允许前端开发端口跨域访问。
- **Authorize 属性**: 敏感接口均受全权保护，必须在 Header 中携带 `Bearer <token>`。
