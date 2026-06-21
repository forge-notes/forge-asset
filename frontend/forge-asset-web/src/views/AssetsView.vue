<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { Delete, Edit, Plus, Printer, Search, SwitchButton } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import http, { TOKEN_KEY, USER_KEY } from '../api/http'
import AssetFormDialog from '../components/AssetFormDialog.vue'
import PrintPreviewDialog from '../components/PrintPreviewDialog.vue'
import type { Asset, AssetStatus } from '../types/asset'

const router = useRouter()
const assets = ref<Asset[]>([])
const loading = ref(false)
const keyword = ref('')
const selected = ref<Asset[]>([])
const formVisible = ref(false)
const printVisible = ref(false)
const editingAsset = ref<Asset | null>(null)
const username = localStorage.getItem(USER_KEY) || 'admin'

const filteredAssets = computed(() => {
  const term = keyword.value.trim().toLowerCase()
  if (!term) return assets.value
  return assets.value.filter((item) =>
    item.assetCode.toLowerCase().includes(term) || item.name.toLowerCase().includes(term),
  )
})

const statusLabels: Record<AssetStatus, string> = { normal: '正常', repair: '维修中', inactive: '停用' }

function getStatusLabel(status: AssetStatus): string {
  return statusLabels[status]
}

async function loadAssets() {
  loading.value = true
  try {
    const { data } = await http.get<Asset[]>('/assets')
    assets.value = data
  } catch {
    ElMessage.error('资产列表加载失败')
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editingAsset.value = null
  formVisible.value = true
}

function openEdit(asset: Asset) {
  editingAsset.value = asset
  formVisible.value = true
}

async function removeAsset(asset: Asset) {
  try {
    await ElMessageBox.confirm(`确定删除资产“${asset.name}”吗？`, '删除确认', {
      type: 'warning', confirmButtonText: '删除', cancelButtonText: '取消',
    })
    await http.delete(`/assets/${asset.id}`)
    ElMessage.success('资产已删除')
    await loadAssets()
  } catch (error) {
    if (error !== 'cancel' && error !== 'close') ElMessage.error('删除失败')
  }
}

function openPrint() {
  if (!selected.value.length) {
    ElMessage.warning('请先勾选需要打印的资产')
    return
  }
  printVisible.value = true
}

function logout() {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(USER_KEY)
  router.replace('/login')
}

function formatDate(value: string | null | undefined) {
  if (!value) return '-'
  return new Intl.DateTimeFormat('zh-CN', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
}

onMounted(loadAssets)
</script>

<template>
  <div class="app-shell">
    <aside class="sidebar">
      <div class="sidebar-brand"><span class="brand-mark">F</span><span>ForgeAsset</span></div>
      <nav><a class="nav-item active" href="/assets"><span class="nav-icon">□</span>资产管理</a></nav>
      <span class="sidebar-version">V0.1</span>
    </aside>

    <main class="main-content">
      <header class="topbar">
        <div><h1>资产管理</h1><p>登记、维护并打印资产标签</p></div>
        <div class="user-area"><span class="user-avatar">{{ username.slice(0, 1).toUpperCase() }}</span><span>{{ username }}</span><el-button text :icon="SwitchButton" @click="logout">退出登录</el-button></div>
      </header>

      <section class="content-area">
        <div class="toolbar">
          <el-input v-model="keyword" class="search-input" :prefix-icon="Search" clearable placeholder="搜索资产编号或名称" />
          <div class="toolbar-actions">
            <span v-if="selected.length" class="selection-count">已选 {{ selected.length }} 项</span>
            <el-button :icon="Printer" @click="openPrint">打印选中资产</el-button>
            <el-button type="primary" :icon="Plus" @click="openCreate">新增资产</el-button>
          </div>
        </div>

        <div class="table-frame">
          <el-table v-loading="loading" :data="filteredAssets" row-key="id" empty-text="暂无资产，点击右上角新增" @selection-change="selected = $event">
            <el-table-column type="selection" width="46" />
            <el-table-column prop="assetCode" label="资产编号" width="160" />
            <el-table-column prop="name" label="资产名称" min-width="150" show-overflow-tooltip />
            <el-table-column prop="category" label="分类" min-width="100"><template #default="{ row }">{{ row.category || '-' }}</template></el-table-column>
            <el-table-column label="品牌型号" min-width="140" show-overflow-tooltip><template #default="{ row }">{{ [row.brand, row.model].filter(Boolean).join(' ') || '-' }}</template></el-table-column>
            <el-table-column prop="location" label="存放位置" min-width="120"><template #default="{ row }">{{ row.location || '-' }}</template></el-table-column>
            <el-table-column prop="owner" label="使用人" min-width="80"><template #default="{ row }">{{ row.owner || '-' }}</template></el-table-column>
            <el-table-column label="状态" width="78"><template #default="{ row }"><el-tag :type="row.status === 'normal' ? 'success' : row.status === 'repair' ? 'warning' : 'info'" effect="light">{{ getStatusLabel(row.status) }}</el-tag></template></el-table-column>
            <el-table-column label="更新时间" width="150"><template #default="{ row }">{{ formatDate(row.updatedAt) }}</template></el-table-column>
            <el-table-column label="操作" width="132"><template #default="{ row }"><el-button link type="primary" :icon="Edit" @click="openEdit(row)">编辑</el-button><el-button link type="danger" :icon="Delete" @click="removeAsset(row)">删除</el-button></template></el-table-column>
          </el-table>
          <footer class="table-footer">共 {{ filteredAssets.length }} 项资产</footer>
        </div>
      </section>
    </main>

    <AssetFormDialog v-model="formVisible" :asset="editingAsset" @saved="loadAssets" />
    <PrintPreviewDialog v-model="printVisible" :assets="selected" />
  </div>
</template>
