<template>
  <div class="user-center">
    <div class="profile-layout">
        <!-- Left: Profile Card -->
        <div class="card profile-card">
            <div class="avatar-large" :class="{ 'clickable': isEditing }" @click="isEditing && triggerAvatarUpload()">
                <img v-if="form.avatar" :src="form.avatar" />
                <div v-else class="avatar-placeholder">
                    {{ form.username?.charAt(0).toUpperCase() }}
                </div>
                <div v-if="isEditing" class="avatar-overlay">
                    <span>{{ t('profile.upload_avatar') }}</span>
                </div>
            </div>
            <input type="file" ref="fileInput" class="hidden-input" accept="image/*" @change="handleAvatarChange" />
            
            <h2>{{ form.username }}</h2>

            <p class="role-badge">{{ form.role === 'admin' ? 'System Administrator' : 'User' }}</p>
            
            <button class="btn" :class="isEditing ? 'btn-primary' : 'btn-secondary'" @click="toggleEdit">
                {{ isEditing ? t('profile.save') : t('profile.edit_profile') }}
            </button>
        </div>

        <div class="right-column">
            <!-- Account Info -->
            <div class="card info-card">
                <div class="card-header">
                    <h3>{{ t('profile.account_info') }}</h3>
                </div>
                <div class="info-list">
                    <div class="info-row">
                        <label>{{ t('login.username') }}</label>
                        <div class="info-val">
                             {{ form.username }}
                        </div>
                    </div>
                    <div class="info-row">
                        <label>{{ t('profile.phone') }}</label>
                        <div class="info-val">
                            <span v-if="!isEditing">{{ form.phone || '-' }}</span>
                            <input v-else v-model="form.phone" class="input-edit" />
                        </div>
                    </div>
                    <div class="info-row">
                        <label>{{ t('login.password') }}</label>
                         <div class="info-val">
                            <span>********</span>
                             <a v-if="isEditing" @click="showPwdModal = true" class="link-action">{{ t('admin.edit') }}</a>
                        </div>
                    </div>
                    <div class="info-row">
                        <label>Email</label>
                        <div class="info-val">
                            <span v-if="!isEditing">{{ form.email || '-' }}</span>
                             <input v-else v-model="form.email" class="input-edit" />
                        </div>
                    </div>
                    <div class="info-row">
                        <label>{{ t('profile.department') }}</label>
                        <div class="info-val">
                             <span v-if="!isEditing">{{ form.department || '-' }}</span>
                             <input v-else v-model="form.department" class="input-edit" />
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <!-- Password Change Modal -->
    <div v-if="showPwdModal" class="modal-overlay">
        <div class="modal card pwd-modal">
            <h3>{{ t('profile.change_password') || 'Change Password' }}</h3>
            <div class="pwd-form">
                <div class="form-item">
                    <label>Current Password</label>
                    <input type="password" v-model="pwdForm.old" class="input-std" />
                </div>
                <div class="form-item">
                    <label>New Password</label>
                    <input type="password" v-model="pwdForm.new" class="input-std" />
                </div>
                <div class="form-item">
                    <label>Confirm New Password</label>
                    <input type="password" v-model="pwdForm.confirm" class="input-std" />
                </div>
            </div>
            <div v-if="pwdError" class="error-msg">{{ pwdError }}</div>
            <div class="modal-actions">
                <button class="btn btn-secondary" @click="closePwdModal">Cancel</button>
                <button class="btn btn-primary" @click="handlePwdChange" :disabled="isChangingPwd">
                    {{ isChangingPwd ? 'Updating...' : t('profile.save') }}
                </button>
            </div>
        </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useAuthStore } from '../stores/auth';
import { useI18n } from 'vue-i18n';
import { compressAvatar } from '../utils/image';
import api from '../services/api';

/**
 * 核心 Store 与 国际化
 */
const authStore = useAuthStore();
const { t } = useI18n();
const fileInput = ref<HTMLInputElement | null>(null);

// 编辑状态
const isEditing = ref(false);
const form = ref<any>({});

// 密码模态框状态
const showPwdModal = ref(false);
const isChangingPwd = ref(false);
const pwdError = ref('');
const pwdForm = ref({ old: '', new: '', confirm: '' });

/**
 * 挂载时初始化表单
 */
onMounted(() => {
    if (authStore.user) {
        form.value = { ...authStore.user };
    }
});

/**
 * 触发隐藏的头像上传输入框
 */
const triggerAvatarUpload = () => fileInput.value?.click();

/**
 * 处理头像选择并进行客户端压缩
 */
const handleAvatarChange = async (event: Event) => {
    const target = event.target as HTMLInputElement;
    if (target.files && target.files[0]) {
        try {
            // 使用 Canvas API 压缩图像，限制最大尺寸为 300x300
            const compressed = await compressAvatar(target.files[0], {
                maxWidth: 300,
                maxHeight: 300,
                quality: 0.8
            });
            form.value.avatar = compressed;
        } catch (error) {
            console.error('头像压缩失败:', error);
            // 这里可以添加用户提示，例如“图片处理失败”
        }
    }
};

/**
 * 切换编辑/保存模式
 */
const toggleEdit = async () => {
    if (isEditing.value) {
        // 保存时调用认证 Store 更新用户信息
        await authStore.updateUser(form.value);
    }
    isEditing.value = !isEditing.value;
};

/**
 * 关闭密码修改模态框并重置表单
 */
const closePwdModal = () => {
    showPwdModal.value = false;
    pwdError.value = '';
    pwdForm.value = { old: '', new: '', confirm: '' };
};

/**
 * 执行密码修改逻辑
 */
const handlePwdChange = async () => {
    // 逻辑验证：两次新密码必须一致
    if (pwdForm.value.new !== pwdForm.value.confirm) {
        pwdError.value = "两次输入的新密码不匹配";
        return;
    }
    if (!pwdForm.value.old || !pwdForm.value.new) {
        pwdError.value = "表单字段不能为空";
        return;
    }

    isChangingPwd.value = true;
    try {
        // 请求后端修改密码接口
        await api.post('/Auth/change-password', {
            username: form.value.username,
            oldPassword: pwdForm.value.old,
            newPassword: pwdForm.value.new
        });
        closePwdModal();
        alert(t('profile.pwd_success') || "密码修改成功");
    } catch (e: any) {
        // 捕获后端返回的错误消息
        pwdError.value = e.response?.data?.message || "身份验证失败，请重试";
    } finally {
        isChangingPwd.value = false;
    }
};
</script>

<style scoped>
.user-center { padding: 24px; max-width: 1100px; margin: 0 auto; }
.profile-layout { display: flex; gap: 32px; align-items: flex-start; }

.card { background: white; border-radius: 12px; box-shadow: var(--shadow-sm); border: 1px solid var(--border-light); overflow: hidden; }

/* Left Profile Card */
.profile-card { flex: 0 0 280px; padding: 40px 24px; text-align: center; display: flex; flex-direction: column; align-items: center; gap: 20px; }
.avatar-large { 
    width: 120px; height: 120px; border-radius: 50%; overflow: hidden; 
    background: #f1f5f9; color: var(--text-light); display: flex; align-items: center; justify-content: center;
    font-size: 3rem; font-weight: 600; box-shadow: 0 4px 20px rgba(0,0,0,0.08);
    position: relative; border: 4px solid #fff;
}
.avatar-large.clickable { cursor: pointer; }
.avatar-large img { 
    width: 100%; 
    height: 100%; 
    object-fit: cover; 
    display: block;
}
.avatar-overlay {
    position: absolute; top:0; left:0; right:0; bottom:0; background: rgba(0,0,0,0.5); color: white;
    display: flex; align-items: center; justify-content: center; font-size: 0.8rem; opacity: 1;
}

.hidden-input { display: none; }
.input-edit { border: 1px solid #cbd5e1; border-radius: 4px; padding: 6px 12px; width: 100%; font-size: 0.95rem; }
.name-edit { text-align: center; font-size: 1.4rem; font-weight: 600; margin-top: -10px; }

.role-badge { color: #64748b; font-size: 0.85rem; background: #f1f5f9; padding: 4px 12px; border-radius: 20px; }

/* Right Info Cards */
.right-column { flex: 1; display: flex; flex-direction: column; gap: 24px; }
.card-header { padding: 20px 24px; border-bottom: 1px solid #f1f5f9; background: #fafafa; }
.card-header h3 { font-size: 1.05rem; font-weight: 600; color: var(--text-main); margin: 0; }

.info-list { padding: 8px 0; }
.info-row { display: flex; padding: 18px 24px; border-bottom: 1px solid #f8fafc; align-items: center; }
.info-row:last-child { border-bottom: none; }
.info-row label { width: 140px; color: #64748b; font-size: 0.95rem; font-weight: 500; }
.info-val { flex: 1; color: var(--text-main); font-size: 1rem; }

.link-action { color: var(--primary-600); text-decoration: underline; font-size: 0.9rem; margin-left: auto; cursor: pointer; }

/* Modal */
.modal-overlay { 
    position: fixed; top: 0; left: 0; right: 0; bottom: 0; 
    background: rgba(15, 23, 42, 0.6); display: flex; align-items: center; justify-content: center; z-index: 1000; 
    backdrop-filter: blur(2px);
}
.pwd-modal { width: 400px; padding: 24px; }
.pwd-form { display: flex; flex-direction: column; gap: 16px; margin: 20px 0; }
.form-item { display: flex; flex-direction: column; gap: 6px; }
.form-item label { font-size: 0.85rem; color: #64748b; }
.input-std { padding: 10px 12px; border: 1px solid #ddd; border-radius: 6px; }
.error-msg { color: #ef4444; font-size: 0.85rem; margin-bottom: 12px; text-align: center; }
.modal-actions { display: flex; justify-content: flex-end; gap: 12px; }

@media (max-width: 850px) {
    .profile-layout { flex-direction: column; align-items: stretch; }
    .profile-card { width: 100%; flex: none; }
}
</style>
