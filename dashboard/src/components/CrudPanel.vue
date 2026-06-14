<script setup lang="ts">
import { computed } from 'vue'
import { t } from '../prefs'

type CrudOption = {
  value: string | number
  label: string
}

type CrudField = {
  key: string
  label: string
  type?: 'text' | 'number' | 'select' | 'textarea' | 'datetime'
  required?: boolean
  min?: number
  disabledOnEdit?: boolean
  options?: () => CrudOption[]
}

type CrudColumn = {
  key: string
  label: string
  badge?: boolean
  format?: (value: unknown, item: Record<string, unknown>) => string
}

type CrudMessage = {
  type: 'success' | 'error'
  text: string
}

type CrudConfig = {
  key: string
  title: string
  description?: string
  fields: CrudField[]
  columns: CrudColumn[]
  items: () => Record<string, unknown>[]
  maxVisibleRows?: number
  maxTableHeight?: string
  allowDelete?: boolean
  canDelete?: (item: Record<string, unknown>) => true | string
}

const props = defineProps<{
  config: CrudConfig
  form: Record<string, unknown>
  formOpen: boolean
  editing: boolean
  message?: CrudMessage
  statusClass: (status: string | undefined) => string
}>()

const emit = defineEmits<{
  add: []
  edit: [item: Record<string, unknown>]
  cancel: []
  save: []
  delete: [item: Record<string, unknown>]
  updateField: [key: string, value: string]
}>()

function cellValue(column: CrudColumn, item: Record<string, unknown>) {
  const value = item[column.key]
  if (column.format) return column.format(value, item)
  if (value === null || value === undefined || value === '') return '-'
  return String(value)
}

function inputValue(key: string) {
  const value = props.form[key]
  if (value === null || value === undefined) return ''
  return String(value)
}

const tableShellStyle = computed(() => {
  if (props.config.maxTableHeight) return { '--table-shell-max-height': props.config.maxTableHeight }
  if (props.config.maxVisibleRows) {
    return { '--table-shell-max-height': `calc(${props.config.maxVisibleRows} * 2.55rem + 2.75rem)` }
  }
  return undefined
})
</script>

<template>
  <section class="industrial-panel">
    <div class="section-heading">
      <div class="min-w-0">
        <p>{{ t('CRUD operations') }}</p>
        <h3>{{ t(config.title) }}</h3>
        <p v-if="config.description" class="mt-2 max-w-3xl normal-case tracking-normal text-slate-500 dark:text-slate-400">{{ t(config.description) }}</p>
      </div>
      <button class="btn-primary w-full shrink-0 sm:w-auto" type="button" @click="emit('add')">{{ t('Add') }}</button>
    </div>

    <p
      v-if="message"
      class="mt-4 rounded-lg border px-4 py-3 text-sm font-semibold"
      :class="message.type === 'success'
        ? 'border-emerald-200 bg-emerald-50 text-emerald-800 dark:border-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-100'
        : 'border-red-200 bg-red-50 text-red-800 dark:border-red-700 dark:bg-red-900/30 dark:text-red-100'"
    >
      {{ t(message.text) }}
    </p>

    <div v-if="formOpen" class="crud-form-shell">
      <form class="grid max-h-[90vh] gap-4 overflow-y-auto p-4 lg:grid-cols-2 sm:p-5" @submit.prevent="emit('save')">
        <label v-for="field in config.fields" :key="field.key" class="form-label" :class="{ 'lg:col-span-2': field.type === 'textarea' }">
          <span>{{ t(field.label) }}<span v-if="field.required" class="text-red-600"> *</span></span>
          <select
            v-if="field.type === 'select'"
            class="form-input"
            :disabled="editing && field.disabledOnEdit"
            :value="inputValue(field.key)"
            @change="emit('updateField', field.key, ($event.target as HTMLSelectElement).value)"
          >
            <option v-for="option in field.options?.() || []" :key="String(option.value)" :value="option.value">{{ option.label }}</option>
          </select>
          <textarea
            v-else-if="field.type === 'textarea'"
            class="form-input min-h-24"
            :value="inputValue(field.key)"
            @input="emit('updateField', field.key, ($event.target as HTMLTextAreaElement).value)"
          />
          <input
            v-else
            class="form-input"
            :type="field.type === 'number' ? 'number' : field.type === 'datetime' ? 'datetime-local' : 'text'"
            :min="field.min"
            :disabled="editing && field.disabledOnEdit"
            :value="inputValue(field.key)"
            @input="emit('updateField', field.key, ($event.target as HTMLInputElement).value)"
          />
        </label>

        <div class="sticky bottom-0 lg:col-span-2 -mx-4 -mb-4 mt-2 flex flex-wrap items-center gap-3 border-t border-slate-200 bg-white/95 px-4 py-4 backdrop-blur dark:border-slate-700 dark:bg-slate-800/95 sm:-mx-5 sm:-mb-5 sm:px-5">
          <button class="btn-primary w-full sm:w-auto" type="submit">{{ editing ? t('Save') : t('Add') }}</button>
          <button class="btn-secondary w-full sm:w-auto" type="button" @click="emit('cancel')">{{ t('Cancel') }}</button>
        </div>
      </form>
    </div>

    <div class="table-shell" :style="tableShellStyle">
      <table class="data-table">
        <thead>
          <tr>
            <th v-for="column in config.columns" :key="column.key">{{ t(column.label) }}</th>
            <th>{{ t('Actions') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in config.items()" :key="String(item.id)">
            <td v-for="column in config.columns" :key="column.key">
              <span v-if="column.badge" :class="statusClass(String(item[column.key] || ''))" :title="cellValue(column, item)">{{ cellValue(column, item) }}</span>
              <span v-else class="cell-clamp" :title="cellValue(column, item)">{{ cellValue(column, item) }}</span>
            </td>
            <td>
              <div class="flex flex-wrap gap-2">
                <button class="btn-secondary btn-compact" type="button" @click="emit('edit', item)">{{ t('Edit') }}</button>
                <button
                  v-if="config.allowDelete !== false"
                  class="btn-danger btn-compact disabled:cursor-not-allowed disabled:opacity-50"
                  type="button"
                  :disabled="config.canDelete?.(item) !== true && config.canDelete?.(item) !== undefined"
                  :title="typeof config.canDelete?.(item) === 'string' ? t(String(config.canDelete?.(item))) : t('Delete')"
                  @click="emit('delete', item)"
                >
                  {{ t('Delete') }}
                </button>
              </div>
            </td>
          </tr>
          <tr v-if="!config.items().length">
            <td :colspan="config.columns.length + 1">
              <div class="empty-state">
                <strong>{{ t('No records found') }}</strong>
                <p>{{ t('No operational records are currently available for this table.') }}</p>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>
