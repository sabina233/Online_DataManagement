import axios from 'axios';

// Create Axios Instance
const api = axios.create({
    baseURL: 'http://localhost:5000/api', // Asp.Net Core Default Port
    timeout: 10000
});

// Request Interceptor (Add Token)
api.interceptors.request.use(config => {
    const userJson = localStorage.getItem('auth_user');
    if (userJson) {
        // In a real app, you'd store a token separately. 
        // For now, we assume basic auth or we'll add headers if needed.
        // config.headers.Authorization = `Bearer ${token}`; 
    }
    return config;
}, error => {
    return Promise.reject(error);
});

// Response Interceptor
api.interceptors.response.use(response => {
    return response.data;
}, error => {
    console.error('API Error:', error);
    return Promise.reject(error);
});

export default api;
