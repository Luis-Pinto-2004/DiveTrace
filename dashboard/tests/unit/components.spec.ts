import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import StatusBadge from '@/components/common/StatusBadge.vue'
import KpiCard from '@/components/common/KpiCard.vue'
import EmptyState from '@/components/common/EmptyState.vue'
import StatStrip from '@/components/common/StatStrip.vue'

describe('StatusBadge', () => {
  it('apresenta a etiqueta e deriva o tom do estado', () => {
    const wrapper = mount(StatusBadge, { props: { label: 'Bloqueado', status: 'Blocked' } })
    expect(wrapper.text()).toContain('Bloqueado')
    expect(wrapper.classes()).toContain('dt-badge--critical')
  })

  it('respeita um tom explícito', () => {
    const wrapper = mount(StatusBadge, { props: { label: 'OK', tone: 'ok' } })
    expect(wrapper.classes()).toContain('dt-badge--ok')
  })

  it('expõe role=status para acessibilidade', () => {
    const wrapper = mount(StatusBadge, { props: { label: 'Ativo', tone: 'info' } })
    expect(wrapper.attributes('role')).toBe('status')
  })
})

describe('KpiCard', () => {
  it('apresenta valor, unidade e tendência', () => {
    const wrapper = mount(KpiCard, {
      props: {
        label: 'Unidades em curso',
        value: 128,
        unit: 'un',
        trend: 'up',
        trendLabel: '+12%',
        tone: 'info',
      },
    })
    expect(wrapper.text()).toContain('128')
    expect(wrapper.text()).toContain('un')
    expect(wrapper.text()).toContain('+12%')
    expect(wrapper.classes()).toContain('dt-kpi--info')
  })
})

describe('EmptyState', () => {
  it('apresenta título e descrição', () => {
    const wrapper = mount(EmptyState, {
      props: { title: 'Sem decisões pendentes', description: 'Tudo em dia.' },
    })
    expect(wrapper.text()).toContain('Sem decisões pendentes')
    expect(wrapper.text()).toContain('Tudo em dia.')
  })

  it('renderiza o slot de ação', () => {
    const wrapper = mount(EmptyState, {
      props: { title: 'Vazio' },
      slots: { action: '<button>Criar</button>' },
    })
    expect(wrapper.find('button').exists()).toBe(true)
  })
})

describe('StatStrip', () => {
  it('renderiza um KpiCard por item', () => {
    const wrapper = mount(StatStrip, {
      props: {
        items: [
          { label: 'A', value: 1 },
          { label: 'B', value: 2 },
          { label: 'C', value: 3 },
        ],
      },
    })
    expect(wrapper.findAllComponents(KpiCard)).toHaveLength(3)
  })
})
