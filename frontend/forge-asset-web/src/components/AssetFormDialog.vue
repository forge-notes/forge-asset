<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import axios from 'axios'
import http from '../api/http'
import type { Asset, AssetFormInput, GenerateAssetCodeResponse } from '../types/asset'

const props = withDefaults(defineProps<{
  modelValue: boolean
  asset?: Asset | null
}>(), {
  asset: null,
})
const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  saved: []
}>()
const formRef = ref<FormInstance>()
const saving = ref(false)
const form = reactive(emptyForm())

const title = computed(() => (props.asset ? '编辑资产' : '新增资产'))
const rules: FormRules<AssetFormInput> = {
  assetCode: [{ required: true, message: '请输入资产编号', trigger: 'blur' }],
  name: [{ required: true, message: '请输入资产名称', trigger: 'blur' }],
}

watch(
  () => props.modelValue,
  async (visible) => {
    if (!visible) return
    Object.assign(form, props.asset ? assetToForm(props.asset) : emptyForm())
    if (!props.asset) {
      try {
        const { data } = await http.post<GenerateAssetCodeResponse>('/assets/generate-code')
        form.assetCode = data.assetCode
      } catch {
        ElMessage.error('资产编号生成失败')
      }
    }
  },
)

function emptyForm(): AssetFormInput {
  return {
    assetCode: '', name: '', category: '', brand: '', model: '', serialNumber: '',
    location: '', owner: '', purchaseDate: null, remark: '', status: 'normal',
  }
}

function assetToForm(asset: Asset): AssetFormInput {
  return {
    assetCode: asset.assetCode,
    name: asset.name,
    category: asset.category ?? '',
    brand: asset.brand ?? '',
    model: asset.model ?? '',
    serialNumber: asset.serialNumber ?? '',
    location: asset.location ?? '',
    owner: asset.owner ?? '',
    purchaseDate: asset.purchaseDate?.slice(0, 10) ?? null,
    remark: asset.remark ?? '',
    status: asset.status,
  }
}

async function save() {
  if (!formRef.value) return
  await formRef.value.validate()
  saving.value = true
  try {
    const payload = { ...form, purchaseDate: form.purchaseDate || null }
    if (props.asset) await http.put(`/assets/${props.asset.id}`, payload)
    else await http.post('/assets', payload)
    ElMessage.success(props.asset ? '资产已更新' : '资产已新增')
    emit('update:modelValue', false)
    emit('saved')
  } catch (error: unknown) {
    if (axios.isAxiosError(error) && error.response?.status === 409) ElMessage.error('资产编号已存在')
    else if (axios.isAxiosError(error) && error.response) ElMessage.error('保存失败，请检查填写内容')
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <el-dialog :model-value="modelValue" :title="title" width="760px" class="asset-dialog" destroy-on-close @close="emit('update:modelValue', false)">
    <el-form ref="formRef" :model="form" :rules="rules" label-position="right" label-width="82px">
      <div class="form-grid">
        <el-form-item label="资产编号" prop="assetCode">
          <el-input v-model="form.assetCode" />
        </el-form-item>
        <el-form-item label="资产名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入资产名称" />
        </el-form-item>
        <el-form-item label="分类"><el-input v-model="form.category" placeholder="如：电脑设备" /></el-form-item>
        <el-form-item label="品牌"><el-input v-model="form.brand" placeholder="请输入品牌" /></el-form-item>
        <el-form-item label="型号"><el-input v-model="form.model" placeholder="请输入型号" /></el-form-item>
        <el-form-item label="序列号"><el-input v-model="form.serialNumber" placeholder="请输入序列号" /></el-form-item>
        <el-form-item label="存放位置"><el-input v-model="form.location" placeholder="请输入存放位置" /></el-form-item>
        <el-form-item label="使用人"><el-input v-model="form.owner" placeholder="请输入使用人" /></el-form-item>
        <el-form-item label="购买日期">
          <el-date-picker v-model="form.purchaseDate" type="date" value-format="YYYY-MM-DD" placeholder="选择日期" style="width: 100%" />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="form.status" style="width: 100%">
            <el-option label="正常" value="normal" />
            <el-option label="维修中" value="repair" />
            <el-option label="停用" value="inactive" />
          </el-select>
        </el-form-item>
        <el-form-item label="备注" class="form-full">
          <el-input v-model="form.remark" type="textarea" :rows="3" maxlength="500" show-word-limit placeholder="补充说明（选填）" />
        </el-form-item>
      </div>
    </el-form>
    <template #footer>
      <el-button @click="emit('update:modelValue', false)">取消</el-button>
      <el-button type="primary" :loading="saving" @click="save">保存</el-button>
    </template>
  </el-dialog>
</template>
