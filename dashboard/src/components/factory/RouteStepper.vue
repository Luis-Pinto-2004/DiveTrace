<script setup lang="ts">
import { computed } from 'vue'
import { routeView, unitStatusLabel, unitStatusTone, type Unit } from '@/stores/operations'
import SupportTag from '@/components/factory/SupportTag.vue'

const props = defineProps<{ unit: Unit }>()
const rv = computed(() => routeView(props.unit))
</script>

<template>
  <div class="route">
    <div class="route__top">
      <span
        class="route__status"
        :class="`route__status--${unitStatusTone(unit)}`"
      >{{ unitStatusLabel(unit) }}</span>
      <SupportTag :unit="unit" />
      <span class="route__next">
        <template v-if="rv.nextLabel">Próxima etapa: <strong>{{ rv.nextLabel }}</strong></template>
        <template v-else>Fim da rota</template>
      </span>
    </div>

    <ol class="route__steps">
      <li
        v-for="(s, i) in rv.stages"
        :key="s.key"
        class="route__step"
        :class="[`route__step--${s.status}`, { 'route__step--milestone': s.key === 'suporte' }]"
      >
        <span class="route__dot">
          <span
            v-if="s.status === 'done'"
            aria-hidden="true"
          >✓</span>
          <span
            v-else
            aria-hidden="true"
          >{{ i + 1 }}</span>
        </span>
        <span class="route__label">{{ s.label }}</span>
        <span
          v-if="s.key === 'suporte'"
          class="route__milestone-tag"
        >marco</span>
      </li>
    </ol>

    <p
      v-if="rv.offRoute"
      class="route__off"
    >
      <span aria-hidden="true">⚠</span> {{ rv.offRouteReason }}
    </p>
  </div>
</template>

<style scoped>
.route {
  display: flex;
  flex-direction: column;
  gap: 0.7rem;
}
.route__top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.6rem;
  flex-wrap: wrap;
}
.route__status {
  font-size: 0.7rem;
  font-weight: 800;
  padding: 0.12rem 0.5rem;
  border-radius: 9999px;
  border: 1px solid transparent;
}
.route__status--ok {
  color: var(--dt-ok-text);
  background: var(--dt-ok-surface);
  border-color: var(--dt-ok-border);
}
.route__status--info {
  color: var(--dt-info-text);
  background: var(--dt-info-surface);
  border-color: var(--dt-info-border);
}
.route__status--warn {
  color: var(--dt-warn-text);
  background: var(--dt-warn-surface);
  border-color: var(--dt-warn-border);
}
.route__status--action {
  color: var(--dt-action-text);
  background: var(--dt-action-surface);
  border-color: var(--dt-action-border);
}
.route__status--critical {
  color: var(--dt-critical-text);
  background: var(--dt-critical-surface);
  border-color: var(--dt-critical-border);
}
.route__status--neutral {
  color: var(--dt-neutral-text);
  background: var(--dt-neutral-surface);
  border-color: var(--dt-neutral-border);
}
.route__next {
  font-size: 0.7rem;
  color: var(--dt-neutral-text);
}
.route__next strong {
  color: var(--dt-text-strong);
}
.route__steps {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  overflow-x: auto;
  gap: 0;
}
.route__step {
  flex: 1 1 0;
  min-width: 78px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.3rem;
  text-align: center;
  position: relative;
  padding-top: 0.2rem;
}
/* linha de ligação entre etapas */
.route__step::before {
  content: '';
  position: absolute;
  top: calc(0.2rem + 11px);
  left: -50%;
  width: 100%;
  height: 2px;
  background: var(--dt-border);
  z-index: 0;
}
.route__step:first-child::before {
  display: none;
}
.route__step--done::before {
  background: var(--dt-ok-solid);
}
.route__dot {
  position: relative;
  z-index: 1;
  width: 22px;
  height: 22px;
  border-radius: 9999px;
  display: grid;
  place-items: center;
  font-size: 0.62rem;
  font-weight: 900;
  border: 2px solid var(--dt-border);
  background: var(--dt-surface);
  color: var(--dt-neutral-text);
}
.route__step--done .route__dot {
  background: var(--dt-ok-solid);
  border-color: var(--dt-ok-solid);
  color: #fff;
}
.route__step--current .route__dot {
  border-color: var(--dt-info-solid);
  color: var(--dt-info-text);
  background: var(--dt-info-surface);
  box-shadow: 0 0 0 3px var(--dt-info-surface);
}
.route__label {
  font-size: 0.6rem;
  font-weight: 700;
  line-height: 1.15;
  color: var(--dt-neutral-text);
}
.route__step--current .route__label {
  color: var(--dt-text-strong);
}
.route__step--milestone .route__dot {
  border-color: var(--dt-action-solid);
}
.route__step--milestone.route__step--pending .route__dot {
  color: var(--dt-action-text);
  background: var(--dt-action-surface);
}
.route__milestone-tag {
  font-size: 0.5rem;
  font-weight: 900;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: var(--dt-action-text);
  background: var(--dt-action-surface);
  border: 1px solid var(--dt-action-border);
  border-radius: 9999px;
  padding: 0 0.3rem;
}
.route__off {
  margin: 0;
  font-size: 0.72rem;
  font-weight: 700;
  color: var(--dt-action-text);
  background: var(--dt-action-surface);
  border: 1px solid var(--dt-action-border);
  border-radius: var(--dt-radius);
  padding: 0.45rem 0.6rem;
}
</style>
