<script setup lang="ts">
import StatusBadge from './StatusBadge.vue'
import EmptyState from './EmptyState.vue'
import type { StatusTone } from '@/utils/status'

export interface ActivityItem {
  id: string | number
  title: string
  detail?: string
  timestamp?: string
  tag?: string
  tone?: StatusTone
}

withDefaults(
  defineProps<{
    items: ActivityItem[]
    emptyTitle?: string
  }>(),
  { emptyTitle: 'Sem atividade recente' },
)
</script>

<template>
  <div class="dt-feed">
    <EmptyState
      v-if="!items.length"
      :title="emptyTitle"
      icon="◷"
    />
    <ul
      v-else
      class="dt-feed__list"
    >
      <li
        v-for="item in items"
        :key="item.id"
        class="dt-feed__item"
      >
        <div class="dt-feed__main">
          <p class="dt-feed__title">
            {{ item.title }}
          </p>
          <p
            v-if="item.detail"
            class="dt-feed__detail"
          >
            {{ item.detail }}
          </p>
        </div>
        <div class="dt-feed__meta">
          <StatusBadge
            v-if="item.tag"
            :label="item.tag"
            :tone="item.tone"
            size="sm"
          />
          <span
            v-if="item.timestamp"
            class="dt-feed__time"
          >{{ item.timestamp }}</span>
        </div>
      </li>
    </ul>
  </div>
</template>

<style scoped>
.dt-feed__list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
}
.dt-feed__item {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 0.75rem;
  padding: 0.6rem 0;
  border-bottom: 1px solid var(--dt-neutral-border);
}
.dt-feed__item:last-child {
  border-bottom: none;
}
.dt-feed__main {
  min-width: 0;
}
.dt-feed__title {
  margin: 0;
  font-size: 0.8125rem;
  font-weight: 700;
  color: var(--dt-text-strong);
  overflow-wrap: break-word;
}
:global(.dark) .dt-feed__title {
  color: #f1f5f9;
}
.dt-feed__detail {
  margin: 0.1rem 0 0;
  font-size: 0.75rem;
  color: var(--dt-neutral-text);
  overflow-wrap: break-word;
}
.dt-feed__meta {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 0.3rem;
  flex-shrink: 0;
}
.dt-feed__time {
  font-size: 0.6875rem;
  color: var(--dt-neutral-text);
  white-space: nowrap;
}
</style>
