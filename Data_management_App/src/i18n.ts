import { createI18n } from 'vue-i18n';

const messages = {
    en: {
        login: {
            welcome: 'Welcome Back',
            subtitle: 'Data Management System',
            username: 'Username',
            username_ph: 'Enter your username',
            password: 'Password',
            password_ph: '••••••••',
            signin: 'Sign In',
            signing_in: 'Signing In...',
            auth_only: 'Protected System. Authorized Personnel Only.',
            error_username: 'Please enter a username'
        },
        layout: {
            dashboard: 'Dashboard',
            brands: 'Brands',
            entry: 'Data Entry',
            logout: 'Logout',
            profile: 'User Profile',
            user_management: 'User Management',
            big_screen: 'Big Screen'
        },
        home: {
            hello: 'Hello, {name}! 👋',
            welcome_text: 'Welcome to the Data Management System. Select a brand from the sidebar to view detailed analytics or start entering new data.',
            total_brands: 'Total Brands',
            active_tracking: 'Active items tracking',
            submit_data: 'Submit Data',
            add_new_records: 'Add new records for the year'
        },
        entry: {
            title: 'Data Entry',
            subtitle: 'Enter relevant data. Totals and variances will be calculated automatically.',
            add_row: 'Add Row',
            save: 'Save to Database',
            saving: 'Saving...',
            success: 'Data saved successfully!',
            error: 'Error saving data.',
            location_ph: 'e.g. Shanghai',
            item_ph: 'Brand Name'
        },
        brand: {
            all_time: 'All Time',
            export: 'Export Excel',
            performance_overview: 'Performance Overview',
            no_data: 'No data found for this brand. Please add data in the Entry page.',
            year_filter: 'Year',
            date_preview: 'Date Preview',
            date_filter: 'Date',
            filters: 'Filters',
            location_filter: 'Location'
        },
        profile: {
            title: 'User Center',
            account_info: 'Account Information',
            upload_avatar: 'Change Avatar',
            save: 'Save Changes',
            phone: 'Phone',
            department: 'Department',
            security: 'Security Settings',
            edit_profile: 'Edit Profile'
        },
        admin: {
            title: 'User Management',
            add_user: 'Add User',
            role: 'Role',
            status: 'Status',
            actions: 'Actions',
            edit: 'Edit',
            delete: 'Delete'
        },
        common: {
            switch_lang: 'English' // Label to switch TO
        },
        big_screen: {
            title: 'GLOBAL DATA CENTER',
            subtitle: 'PERFORMANCE REAL-TIME MONITORING',
            total_actual: 'TOTAL ACTUAL',
            total_forecast: 'TOTAL FORECAST',
            achievement: 'ACHIEVEMENT RATE',
            active_brands: 'ACTIVE BRANDS',
            regions_rank: 'TOP REGIONS PERFORMANCE',
            distribution: 'REGIONAL DISTRIBUTION',
            trend: 'ANNUAL TREND ANALYSIS',
            system_online: 'SYSTEM ONLINE',
            mom_growth: 'MoM Growth',
            predictive: 'Predictive Analysis',
            best_region: 'Best Region'
        }
    },
    zh: {
        login: {
            welcome: '欢迎回来',
            subtitle: '数据管理系统',
            username: '用户名',
            username_ph: '请输入您的用户名',
            password: '密码',
            password_ph: '••••••••',
            signin: '登录',
            signing_in: '登录中...',
            auth_only: '受保护系统。仅限授权人员访问。',
            error_username: '请输入用户名'
        },
        layout: {
            dashboard: '首页',
            brands: '品牌数据',
            entry: '数据录入',
            logout: '退出登录',
            profile: '用户资料',
            user_management: '用户管理',
            big_screen: '大屏报表'
        },
        home: {
            hello: '你好, {name}! 👋',
            welcome_text: '欢迎使用数据管理系统。从侧边栏选择一个品牌以查看详细分析，或开始录入新数据。',
            total_brands: '品牌总数',
            active_tracking: '当前追踪项目',
            submit_data: '提交数据',
            add_new_records: '添加本季度的新记录'
        },
        entry: {
            title: '数据录入',
            subtitle: '输入相关数据。总计和差异将自动计算。',
            add_row: '添加行',
            save: '保存到数据库',
            saving: '保存中...',
            success: '数据保存成功！',
            error: '保存数据时出错。',
            location_ph: '例如：上海',
            item_ph: '品牌名称'
        },
        brand: {
            all_time: '全部时间',
            export: '导出 Excel',
            performance_overview: '数据预览',
            no_data: '未找到该品牌的数据。请在录入页面添加数据。',
            year_filter: '年份',
            date_preview: '日期预览',
            date_filter: '日期',
            filters: '筛选',
            location_filter: '地区'
        },
        profile: {
            title: '用户中心',
            account_info: '账户信息',
            upload_avatar: '更换头像',
            save: '保存更改',
            phone: '电话',
            department: '部门',
            security: '安全设置',
            edit_profile: '编辑资料'
        },
        admin: {
            title: '用户管理',
            add_user: '添加用户',
            role: '角色',
            status: '状态',
            actions: '操作',
            edit: '编辑',
            delete: '删除'
        },
        common: {
            switch_lang: '中文' // Label to switch TO
        },
        big_screen: {
            title: '全球数据中心',
            subtitle: '业绩实时监控系统',
            total_actual: '累计实际额',
            total_forecast: '累计预测额',
            achievement: '年度达成率',
            active_brands: '活跃品牌数',
            regions_rank: '区域业绩排行榜',
            distribution: '全球区域分布',
            trend: '年度趋势深度分析',
            system_online: '系统在线',
            mom_growth: '环比增长',
            predictive: '智能预测分析',
            best_region: '表现最佳区域'
        }
    }
};

const i18n = createI18n({
    legacy: false, // Use Composition API
    locale: 'zh', // Default to Chinese as per request implication or keep 'en'? User asked to switch, implies current is default. Let's make 'zh' default if they're Chinese speakers, or 'en'. Let's pick 'zh' as starting or stick to 'en'. User is typing Chinese, let's default 'zh'.
    fallbackLocale: 'en',
    messages
});

export default i18n;
