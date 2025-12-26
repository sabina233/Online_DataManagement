<template>
  <div class="user-management">
    <div class="header-section">
        <h3>{{ t('admin.title') }}</h3>
        <p class="text-secondary">Manage system users and permissions.</p>
    </div>

    <div class="card table-container">
        <table>
            <thead>
                <tr>
                    <th style="width: 60px"></th>
                    <th>{{ t('login.username') }}</th>
                    <th>{{ t('admin.role') }}</th>
                    <th>{{ t('profile.department') }}</th>
                    <th>{{ t('profile.phone') }}</th>
                    <th>{{ t('admin.status') }}</th>
                    <th>{{ t('admin.actions') }}</th>
                </tr>
            </thead>
            <tbody>
                <tr v-for="user in authStore.users" :key="user.id">
                    <td>
                        <div class="avatar-small">
                            <img v-if="user.avatar" :src="user.avatar" class="avatar-img" />
                            <span v-else>{{ user.username?.charAt(0).toUpperCase() }}</span>
                        </div>
                    </td>
                    <td>{{ user.username }}</td>
                    <td>
                        <span class="badge" :class="user.role === 'admin' ? 'admin' : 'user'">
                            {{ user.role }}
                        </span>
                    </td>
                    <td>{{ user.department || '-' }}</td>
                    <td>{{ user.phone || '-' }}</td>
                    <td><span class="status-dot active"></span> Active</td>
                    <td>
                        <div class="action-buttons">
                            <button class="btn-text edit" @click="startEdit(user)">{{ t('admin.edit') }}</button>
                            <button class="btn-text delete" 
                                    v-if="user.id !== authStore.user?.id"
                                    @click="handleDelete(user.id)">
                                {{ t('admin.delete') }}
                            </button>
                        </div>
                    </td>
                </tr>
            </tbody>
        </table>
    </div>

    <!-- Edit Modal -->
    <div v-if="editingUser" class="modal-overlay">
        <div class="modal card">
            <h3>{{ t('admin.edit') }} User</h3>
            <div class="form-group">
                <label>Role</label>
                <select v-model="editingUser.role" class="input-std">
                    <option value="user">User</option>
                    <option value="admin">Admin</option>
                </select>
            </div>
             <div class="form-group">
                <label>Department</label>
                <input v-model="editingUser.department" class="input-std" />
            </div>
             <div class="form-group">
                <label>Phone</label>
                <input v-model="editingUser.phone" class="input-std" />
            </div>
            <div class="modal-actions">
                <button class="btn btn-secondary" @click="editingUser = null">Cancel</button>
                <button class="btn btn-primary" @click="saveEdit">Save</button>
            </div>
        </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useAuthStore, type User } from '../stores/auth';
import { useI18n } from 'vue-i18n';

/**
 * 身份认证 Store
 */
const authStore = useAuthStore();
const { t } = useI18n();

// 当前正在编辑的用户对象
const editingUser = ref<User | null>(null);

/**
 * 开始编辑某个用户，克隆其数据到本地状态
 */
const startEdit = (user: User) => {
    editingUser.value = { ...user };
};

/**
 * 保存用户编辑结果
 */
const saveEdit = async () => {
    if (editingUser.value) {
        await authStore.updateUser(editingUser.value);
        editingUser.value = null; // 关闭模态框
    }
};

/**
 * 删除用户逻辑
 */
const handleDelete = async (id: string) => {
    // 这里使用 confirm 进行简单的浏览器二次确认
    if (confirm('确定要删除该用户吗？此操作不可撤销。')) {
        await authStore.deleteUser(id);
    }
};
</script>

<style scoped>
.user-management { max-width: 100%; }
.header-section { margin-bottom: 24px; }
.table-container { padding: 0; overflow: hidden; }

table { width: 100%; border-collapse: collapse; }
th, td { padding: 12px 24px; text-align: left; border-bottom: 1px solid var(--border-light); }
th { background: #f8fafc; color: var(--text-secondary); font-weight: 600; font-size: 0.9rem; }

.avatar-small { 
    width: 32px; height: 32px; border-radius: 50%; background: var(--primary-100); color: var(--primary-600);
    display: flex; align-items: center; justify-content: center; font-size: 0.8rem; font-weight: 600;
    overflow: hidden;
}
.avatar-img { width: 100%; height: 100%; object-fit: cover; }

.badge { padding: 4px 8px; border-radius: 4px; font-size: 0.8rem; font-weight: 500; text-transform: capitalize; }
.badge.admin { background: #fee2e2; color: #ef4444; }
.badge.user { background: #dbeafe; color: #3b82f6; }

.status-dot { display: inline-block; width: 8px; height: 8px; border-radius: 50%; margin-right: 6px; }
.status-dot.active { background: #10b981; }

.action-buttons { display: flex; gap: 12px; }
.btn-text { background: none; border: none; font-size: 0.9rem; cursor: pointer; padding: 0; }
.btn-text.edit { color: var(--primary-600); }
.btn-text.delete { color: var(--danger); }
.btn-text:hover { text-decoration: underline; }

/* Modal */
.modal-overlay { position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 1000; }
.modal { background: white; padding: 24px; width: 400px; border-radius: 8px; }
.form-group { margin-bottom: 16px; }
.form-group label { display: block; margin-bottom: 4px; color: var(--text-secondary); font-size: 0.9rem; }
.input-std { width: 100%; padding: 8px; border: 1px solid var(--border-light); border-radius: 4px; }
.modal-actions { display: flex; justify-content: flex-end; gap: 12px; margin-top: 24px; }
</style>
