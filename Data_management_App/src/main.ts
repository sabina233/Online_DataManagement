
import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'
import i18n from './i18n'
import './style.css'

/**
 * 应用入口文件：初始化 Vue 实例并加载核心插件
 */
const app = createApp(App)

// 注册状态管理 Pinia
app.use(createPinia())
// 注册路由管理
app.use(router)
// 注册国际化 i18n
app.use(i18n)

// 挂载应用至 HTML 节点
app.mount('#app')
