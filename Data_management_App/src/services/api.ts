import axios from 'axios';

const api = axios.create({
    baseURL: 'http://localhost:5071/api', // 后端 API 地址
    headers: {
        'Content-Type': 'application/json'
    }
});

// 请求拦截器：在每个请求头中自动添加认证 Token
api.interceptors.request.use(
    config => {
        const token = localStorage.getItem('token');
        if (token) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    },
    error => {
        return Promise.reject(error);
    }
);

// 响应拦截器：统一处理 API 错误
api.interceptors.response.use(
    response => response,
    error => {
        console.error('API 错误:', error);
        // 如果是 401 错误，可能需要跳转到登录页或清理用户信息
        if (error.response?.status === 401) {
            localStorage.removeItem('token');
        }
        return Promise.reject(error);
    }
);

export default api;
