# 数据管理系统 - 前端 (Vue 3 + Vite)

这是一个现代化的、高响应式的数据管理与可视化平台。它为用户提供直观的数据编辑界面，并配有极具视觉冲击力的指挥中心大屏，用于监控全球业务表现。

## 核心功能

- **可视化指挥中心 (Big Screen)**：
    - **实时监控**：基于 ECharts 的动态地图、趋势图和排行榜。
    - **双语支持**：一键切换中英文。
    - **高级预测**：内置预测算法，辅助业务决策。
    - **高科技 UI**：赛博朋克风格设计，极佳的视觉呈现效果。
- **数据管理 (Data Center)**：
    - **智能录入**：支持数据的分月份、分大区快速录入。
    - **自动冲突检测**：避免数据重复提交。
- **用户中心 (User Center)**：
    - **头像压缩**：客户端自动压缩高清头像，解决 Base64 过大导致的加载问题。
    - **权限控制**：基于管理员/普通用户的多层级权限设计。

## 技术栈

- **框架**：Vue 3 (Composition API)
- **状态管理**：Pinia
- **路由**：Vue Router
- **国际化**：Vue I18n
- **图表库**：ECharts
- **HTTP 客户端**：Axios (集成 JWT 拦截器)
- **UI 组件库**：Element Plus (部分模块)
- **图标**：Lucide Vue Next

## 项目结构

```text
src/
├── assets/         # 静态资源 (图片, 全局样式)
├── components/     # 通用组件
├── services/       # 接口服务 (api.ts 处理认证拦截)
├── stores/         # 状态管理 (auth-认证, data-业务数据)
├── utils/          # 工具类 (image-图像压缩)
└── views/          # 页面视图 (BigScreenView-指挥中心, UserCenter-个人中心)
```

## 运行指南

1. **环境准备**：确保已安装 Node.js (推荐 v18+)。
2. **安装依赖**：在根目录执行 `npm install`。
3. **配置后端**：在 `src/services/api.ts` 中确保 `baseURL` 指向本地运行的后端 API。
4. **启动开发服务器**：执行 `npm run dev`。
5. **访问项目**：打开提示的 `localhost` 地址（通常是 5173）。

## 安全与性能优化

- **JWT 认证**：通过 Axios 拦截器在每个请求中自动携带安全令牌。
- **图像优化**：头像上传使用 Canvas API 预处理，大幅减少带宽消耗和后端存储压力。

## 源码仓库

[https://github.com/sabina233/Online_DataManagement](https://github.com/sabina233/Online_DataManagement)
