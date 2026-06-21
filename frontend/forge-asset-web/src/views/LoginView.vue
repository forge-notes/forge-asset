<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { Lock, User } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import http, { TOKEN_KEY, USER_KEY } from '../api/http'
import type { LoginRequest, LoginResponse } from '../types/auth'

const router = useRouter()
const loading = ref(false)
const form = reactive<LoginRequest>({ username: 'admin', password: 'admin123' })

async function login() {
  if (!form.username || !form.password) {
    ElMessage.warning('请输入用户名和密码')
    return
  }
  loading.value = true
  try {
    const { data } = await http.post<LoginResponse>('/auth/login', form)
    localStorage.setItem(TOKEN_KEY, data.token)
    localStorage.setItem(USER_KEY, data.username)
    ElMessage.success('登录成功')
    await router.replace('/assets')
  } catch {
    ElMessage.error('用户名或密码错误')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <main class="login-page">
    <section class="login-brand-panel" aria-label="ForgeAsset 介绍">
      <div class="login-brand">
        <span class="brand-mark">F</span>
        <span>ForgeAsset</span>
      </div>
      <div class="brand-message">
        <h1>让每一件资产<br />清晰可循。</h1>
        <p>轻量、可靠的资产登记与标签打印工具。</p>
      </div>
      <span class="brand-version">V0.1 · ASSET MANAGEMENT</span>
    </section>

    <section class="login-form-panel">
      <div class="login-box">
        <div class="mobile-brand">
          <span class="brand-mark">F</span>
          <span>ForgeAsset</span>
        </div>
        <h2>欢迎回来</h2>
        <p class="login-subtitle">登录后管理资产与打印标签</p>
        <el-form :model="form" size="large" @submit.prevent="login">
          <el-form-item>
            <el-input v-model="form.username" :prefix-icon="User" placeholder="用户名" autocomplete="username" />
          </el-form-item>
          <el-form-item>
            <el-input v-model="form.password" :prefix-icon="Lock" type="password" placeholder="密码" autocomplete="current-password" show-password @keyup.enter="login" />
          </el-form-item>
          <el-button type="primary" class="login-button" :loading="loading" @click="login">登录</el-button>
        </el-form>
        <p class="login-hint">默认账号：admin / admin123</p>
      </div>
    </section>
  </main>
</template>
