import api from './config';
import type { User } from '../stores/auth';

export const getUsers = () => {
    return api.get<any, User[]>('/users');
};

export const deleteUser = (id: string) => {
    return api.delete(`/users/${id}`);
};

export const createUser = (user: User) => {
    return api.post('/users', user);
};
