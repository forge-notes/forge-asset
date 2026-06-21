<script setup lang="ts">
import type { Asset } from '../types/asset'

defineProps<{ asset: Asset }>()
</script>

<template>
  <article class="asset-label">
    <header>
      <span class="label-brand">ForgeAsset</span>
      <strong>{{ asset.assetCode }}</strong>
    </header>
    <h3>{{ asset.name }}</h3>
    <dl>
      <div><dt>分类</dt><dd>{{ asset.category || '-' }}</dd></div>
      <div><dt>品牌型号</dt><dd>{{ [asset.brand, asset.model].filter(Boolean).join(' ') || '-' }}</dd></div>
      <div><dt>存放位置</dt><dd>{{ asset.location || '-' }}</dd></div>
      <div><dt>使用人</dt><dd>{{ asset.owner || '-' }}</dd></div>
    </dl>
  </article>
</template>
