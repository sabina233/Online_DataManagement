import api from '../services/api';
import type { User } from '../stores/auth';

export const login = (username: string) => {
    return api.post<any, User>('/auth/login', { username });
};

export const logout = () => {
    // If backend has logout logic
    return api.post('/auth/logout');
};

export const updateUser = (user: User) => {
    return api.put('/users', user);
};
