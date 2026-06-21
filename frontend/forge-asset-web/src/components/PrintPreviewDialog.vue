<script setup lang="ts">
import { computed, nextTick, reactive, watch } from 'vue'
import type { CSSProperties } from 'vue'
import { ElMessage } from 'element-plus'
import AssetLabel from './AssetLabel.vue'
import type { Asset } from '../types/asset'
import type { PrintSettings } from '../types/print'

const PRINT_SETTINGS_KEY = 'forge_asset_print_settings'
const DEFAULT_PRINT_SETTINGS: PrintSettings = {
  protectorWidth: 55,
  protectorHeight: 40,
  labelWidth: 50,
  labelHeight: 35,
  pageMargin: 10,
  labelGap: 5,
}

type PrintCssVariables = CSSProperties & {
  '--label-width': string
  '--label-height': string
  '--page-margin': string
  '--label-gap': string
  '--label-columns': number
  '--print-safe-margin': string
}

withDefaults(defineProps<{ modelValue: boolean; assets?: Asset[] }>(), {
  assets: () => [],
})
const emit = defineEmits<{ 'update:modelValue': [value: boolean] }>()
const settings = reactive<PrintSettings>(loadPrintSettings())

const labelColumns = computed(() => {
  const printableWidth = 210 - settings.pageMargin * 2
  const columns = Math.floor((printableWidth + settings.labelGap) / (settings.labelWidth + settings.labelGap))
  return Math.max(1, columns)
})

const printCssVariables = computed<PrintCssVariables>(() => ({
  '--label-width': `${settings.labelWidth}mm`,
  '--label-height': `${settings.labelHeight}mm`,
  '--page-margin': `${settings.pageMargin}mm`,
  '--label-gap': `${settings.labelGap}mm`,
  '--label-columns': labelColumns.value,
  '--print-safe-margin': '5mm',
}))

watch(settings, (value) => {
  localStorage.setItem(PRINT_SETTINGS_KEY, JSON.stringify(value))
}, { deep: true })

function loadPrintSettings(): PrintSettings {
  try {
    const saved = JSON.parse(localStorage.getItem(PRINT_SETTINGS_KEY) || '{}') as Partial<PrintSettings>
    return {
      protectorWidth: normalize(saved.protectorWidth, DEFAULT_PRINT_SETTINGS.protectorWidth, 20, 200),
      protectorHeight: normalize(saved.protectorHeight, DEFAULT_PRINT_SETTINGS.protectorHeight, 20, 200),
      labelWidth: normalize(saved.labelWidth, DEFAULT_PRINT_SETTINGS.labelWidth, 20, 190),
      labelHeight: normalize(saved.labelHeight, DEFAULT_PRINT_SETTINGS.labelHeight, 20, 277),
      pageMargin: normalize(saved.pageMargin, DEFAULT_PRINT_SETTINGS.pageMargin, 0, 30),
      labelGap: normalize(saved.labelGap, DEFAULT_PRINT_SETTINGS.labelGap, 0, 30),
    }
  } catch {
    return { ...DEFAULT_PRINT_SETTINGS }
  }
}

function normalize(value: unknown, fallback: number, min: number, max: number): number {
  return typeof value === 'number' && Number.isFinite(value)
    ? Math.min(max, Math.max(min, value))
    : fallback
}

function recommendLabelSize() {
  settings.labelWidth = Math.max(20, settings.protectorWidth - 5)
  settings.labelHeight = Math.max(20, settings.protectorHeight - 5)
  ElMessage.success(`已推荐 ${settings.labelWidth} × ${settings.labelHeight} mm 标签`)
}

async function printLabels() {
  await nextTick()
  window.print()
}
</script>

<template>
  <el-dialog :model-value="modelValue" title="A4 标签打印预览" width="1060px" class="print-dialog" @close="emit('update:modelValue', false)">
    <div class="print-dialog-content" :style="printCssVariables">
      <section class="print-settings" aria-labelledby="print-settings-title">
        <div class="print-settings-header">
          <div>
            <h3 id="print-settings-title">打印设置</h3>
            <p>尺寸单位均为毫米（mm），设置会自动保存在当前浏览器。</p>
          </div>
          <el-button type="primary" plain @click="recommendLabelSize">根据保护贴尺寸推荐标签尺寸</el-button>
        </div>
        <div class="print-settings-grid">
          <label><span>保护贴宽度 mm</span><el-input-number v-model="settings.protectorWidth" :min="20" :max="200" :precision="1" :step="1" controls-position="right" /></label>
          <label><span>保护贴高度 mm</span><el-input-number v-model="settings.protectorHeight" :min="20" :max="200" :precision="1" :step="1" controls-position="right" /></label>
          <label><span>标签宽度 mm</span><el-input-number v-model="settings.labelWidth" :min="20" :max="190" :precision="1" :step="1" controls-position="right" /></label>
          <label><span>标签高度 mm</span><el-input-number v-model="settings.labelHeight" :min="20" :max="277" :precision="1" :step="1" controls-position="right" /></label>
          <label><span>页面边距 mm</span><el-input-number v-model="settings.pageMargin" :min="0" :max="30" :precision="1" :step="1" controls-position="right" /></label>
          <label><span>标签间距 mm</span><el-input-number v-model="settings.labelGap" :min="0" :max="30" :precision="1" :step="1" controls-position="right" /></label>
        </div>
      </section>

      <div class="preview-meta">已选择 {{ assets.length }} 项资产 · 保护贴 {{ settings.protectorWidth }} × {{ settings.protectorHeight }} mm · 标签 {{ settings.labelWidth }} × {{ settings.labelHeight }} mm · 每行 {{ labelColumns }} 个</div>
      <div class="paper-stage">
        <div class="print-sheet">
          <AssetLabel v-for="asset in assets" :key="asset.id" :asset="asset" />
        </div>
      </div>
    </div>
    <template #footer>
      <el-button @click="emit('update:modelValue', false)">关闭</el-button>
      <el-button type="primary" @click="printLabels">浏览器打印</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.paper-stage {
  background: #e5eaf2;
  padding: 28px;
  overflow: auto;
  border-radius: 8px;
}

.print-sheet {
  width: 210mm;
  min-height: 297mm;
  box-sizing: border-box;
  padding: var(--page-margin);
  margin: 0 auto;
  background: #fff;
  display: grid;
  grid-template-columns: repeat(var(--label-columns), var(--label-width));
  grid-auto-rows: var(--label-height);
  gap: var(--label-gap);
  align-content: start;
  justify-content: start;
}

@media print {
  @page {
    size: A4 portrait;
    margin: 5mm;
  }

  html,
  body {
    width: 200mm;
    min-height: 287mm;
    margin: 0;
    padding: 0;
    background: #fff;
  }

  body * {
    visibility: hidden;
  }

  .print-sheet,
  .print-sheet * {
    visibility: visible;
  }

  .print-sheet {
    position: absolute;
    left: 0;
    top: 0;
    width: 200mm;
    min-height: 287mm;
    margin: 0;
    padding: var(--page-margin);
    box-shadow: none;
    overflow: visible;
    grid-template-columns: repeat(var(--label-columns), var(--label-width));
    grid-auto-rows: var(--label-height);
    gap: var(--label-gap);
  }
}
</style>
