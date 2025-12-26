<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/auth';
import { useI18n } from 'vue-i18n';
import { LogIn } from 'lucide-vue-next';

/**
 * 登录视图：处理用户身份验证
 */
const { t } = useI18n();
const username = ref('Admin');
const password = ref('');
const error = ref('');
const isLoading = ref(false);

const authStore = useAuthStore();
const router = useRouter();

/**
 * 处理登录逻辑
 */
const handleLogin = async () => {
    isLoading.value = true;
    error.value = '';
    
    if (username.value && password.value) {
        // 调用 AuthStore 执行登录请求
        const result = await authStore.login(username.value, password.value);
        if (result === true) {
            router.push('/'); // 登录成功后跳转至首页
        } else {
             // 登录失败，显示后端返回的错误信息或本地默认提示
            error.value = (typeof result === 'string') ? result : t('login.error_username');
        }
    } else {
        error.value = '请输入用户名和密码';
    }
    isLoading.value = false;
};
</script>

<template>
  <div class="login-container">
    <div class="login-card card">
        <div class="brand-logo">
            <div class="logo-icon">
                <LogIn :size="28" />
            </div>
            <h1>{{ t('login.welcome') }}</h1>
            <p>{{ t('login.subtitle') }}</p>
        </div>

        <form @submit.prevent="handleLogin" class="login-form">
            <div class="form-group">
                <label>{{ t('login.username') }}</label>
                <input type="text" v-model="username" :placeholder="t('login.username_ph')" />
            </div>
            
            <div class="form-group">
                <label>{{ t('login.password') }}</label>
                <input type="password" v-model="password" :placeholder="t('login.password_ph')" />
            </div>

            <button type="submit" class="btn btn-primary full-width" :disabled="isLoading">
                {{ isLoading ? t('login.signing_in') : t('login.signin') }}
            </button>
        </form>

        <p class="footer-text">{{ t('login.auth_only') }}</p>
    </div>
  </div>
</template>

<style scoped>
.login-container {
    height: 100vh;
    width: 100vw;
    display: flex;
    align-items: center;
    justify-content: center;
    background: linear-gradient(135deg, var(--bg-body) 0%, #e0e7ff 100%);
}

.login-card {
    width: 100%;
    max-width: 420px;
    padding: 48px;
    background: rgba(255, 255, 255, 0.9);
    backdrop-filter: blur(10px);
    border: 1px solid white;
}

.brand-logo {
    text-align: center;
    margin-bottom: 32px;
}

.logo-icon {
    width: 56px;
    height: 56px;
    background: var(--primary-100);
    color: var(--primary-600);
    border-radius: 16px;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    margin-bottom: 16px;
}

.brand-logo h1 {
    font-size: 1.75rem;
    font-weight: 700;
    margin-bottom: 8px;
    color: var(--text-main);
}

.brand-logo p {
    color: var(--text-secondary);
}

.form-group {
    margin-bottom: 20px;
}

.form-group label {
    display: block;
    margin-bottom: 8px;
    font-weight: 500;
    font-size: 0.9rem;
    color: var(--text-main);
}

.full-width {
    width: 100%;
    margin-top: 12px;
    height: 44px;
    font-size: 1rem;
}

.footer-text {
    margin-top: 24px;
    text-align: center;
    font-size: 0.8rem;
    color: var(--text-light);
}
</style>
