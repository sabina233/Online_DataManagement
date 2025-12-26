import { defineStore } from 'pinia';
import { ref } from 'vue';
import api from '../services/api';

/**
 * 用户接口定义
 */
export interface User {
    id: string;
    username: string;
    role: 'admin' | 'user';
    phone?: string;
    email?: string;
    department?: string;
    created_at: string;
    avatar?: string;
}

/**
 * 身份认证 Store
 */
export const useAuthStore = defineStore('auth', () => {
    // 当前登录用户信息
    const user = ref<User | null>(null);

    // 管理员使用的用户列表
    const users = ref<User[]>([]);

    // 从本地存储还原登录状态
    const storedUser = localStorage.getItem('auth_user');
    if (storedUser) {
        user.value = JSON.parse(storedUser);
    }

    /**
     * 登录方法
     * @returns 返回 true 表示成功，string 表示错误信息
     */
    async function login(username: string, password: string): Promise<string | boolean> {
        try {
            // 请求登录接口
            const response = await api.post('/Auth/login', {
                username,
                password
            });

            // 后端返回格式为 { user, token }
            user.value = response.data.user;
            const token = response.data.token;

            // 持久化存储
            localStorage.setItem('auth_user', JSON.stringify(user.value));
            localStorage.setItem('token', token);

            return true;
        } catch (error: any) {
            console.error('登录失败:', error);
            if (error.response?.data?.message) {
                return error.response.data.message;
            }
            return false;
        }
    }

    /**
     * 登出方法
     */
    function logout() {
        user.value = null;
        users.value = [];
        localStorage.removeItem('auth_user');
        localStorage.removeItem('token');
    }

    /**
     * 获取所有用户（仅管理员）
     */
    async function fetchUsers() {
        if (user.value?.role !== 'admin') return;
        try {
            const res = await api.get('/User');
            users.value = res.data;
        } catch (e) {
            console.error('获取用户列表失败:', e);
        }
    }

    /**
     * 更新用户信息
     */
    async function updateUser(updatedUser: User) {
        try {
            // 保存到后端
            const res = await api.post('/User', updatedUser);

            // 如果更新的是当前用户，同步更新本地状态
            if (user.value && String(user.value.id) === String(res.data.id)) {
                user.value = res.data;
                localStorage.setItem('auth_user', JSON.stringify(user.value));
            }

            // 如果是管理员，刷新列表
            if (user.value?.role === 'admin') {
                await fetchUsers();
            }
        } catch (e) {
            console.error('更新用户信息失败:', e);
        }
    }

    /**
     * 删除用户（仅管理员）
     */
    async function deleteUser(id: string) {
        try {
            await api.delete(`/User/${id}`);
            if (user.value?.role === 'admin') {
                await fetchUsers();
            }
        } catch (e) {
            console.error('删除用户失败:', e);
        }
    }

    // 初始化时，如果是管理员则自动请求用户列表
    if (user.value?.role === 'admin') {
        fetchUsers();
    }

    return { user, users, login, logout, updateUser, fetchUsers, deleteUser };
});
