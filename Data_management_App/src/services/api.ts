import axios from 'axios';

const api = axios.create({
    baseURL: 'http://localhost:5071/api', // 后端 API 地址
    //baseURL: 'http://192.168.11.9:5113/api',
    headers: {
        'Content-Type': 'application/json'
    }
});

// 请求拦截器：在每个请求头中自动添加认证 Token
api.interceptors.request.use(
    config => {
        const token = localStorage.getItem('token');
        if (token) {
            // Optional: Check if token is expired locally to save a request
            try {
                const parts = token.split('.');
                if (parts.length !== 3) throw new Error('Invalid token');
                const payload = JSON.parse(atob(parts[1]!));
                const now = Math.floor(Date.now() / 1000);
                if (payload.exp && payload.exp < now) {
                    // Token expired
                    throw new Error('Token expired');
                }
                config.headers.Authorization = `Bearer ${token}`;
            } catch (e) {
                // Invalid or expired token
                localStorage.removeItem('token');
                localStorage.removeItem('user');
                window.location.href = '/login';
                return Promise.reject(new Error('Token expired'));
            }
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
            localStorage.removeItem('user'); // Also clear user info if any
            // Redirect to login
            window.location.href = '/login';
        }
        return Promise.reject(error);
    }
);

export default api;
