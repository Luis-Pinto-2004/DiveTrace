<script setup lang="ts">
import TraceGraph from '@/components/factory/TraceGraph.vue'
</script>

<template>
  <div
    class="trace"
    data-demo="grafo"
  >
    <TraceGraph />
  </div>
</template>

<style scoped>
.trace {
  display: flex;
  flex-direction: column;
}
</style>
