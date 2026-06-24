import { test, expect } from '@playwright/test'

/**
 * Smoke E2E — fluxo mínimo crítico.
 * Requer o stack a correr (frontend + API). Em CI é levantado via
 * docker-compose antes desta suite.
 */

test.beforeEach(async ({ page }) => {
  await page.goto('/')
})

test('apresenta o ecrã de início de sessão em PT-PT', async ({ page }) => {
  await expect(page.getByRole('button', { name: 'supervisor', exact: true })).toBeVisible()
  // Conteúdo em português europeu (sem termos pt-BR).
  await expect(page.locator('body')).not.toContainText('você')
})

test('início de sessão demo como supervisor abre o centro de comando', async ({ page }) => {
  await page.getByRole('button', { name: 'supervisor', exact: true }).click()
  await expect(page.getByRole('button', { name: 'supervisor', exact: true })).toBeHidden()
  // A navegação principal fica disponível após autenticação.
  await expect(page.locator('nav').first()).toBeVisible()
})

test('alterna para o modo escuro', async ({ page }) => {
  await page.getByRole('button', { name: 'supervisor', exact: true }).click()
  const html = page.locator('html')
  const before = await html.getAttribute('class')
  // Procura um controlo de tema acessível por etiqueta.
  const themeToggle = page
    .getByRole('button', { name: /tema|modo escuro|dark|claro/i })
    .first()
  if (await themeToggle.count()) {
    await themeToggle.click()
    const after = await html.getAttribute('class')
    expect(after).not.toBe(before)
  }
})
