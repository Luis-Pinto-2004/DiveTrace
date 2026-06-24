import { test, expect } from '@playwright/test'

/**
 * Fluxos por perfil. Verifica que cada perfil aterra na sua área e
 * que o cliente está isolado das vistas internas de produção.
 */

const profiles = ['operador', 'qualidade', 'logistica', 'cliente'] as const

for (const profile of profiles) {
  test(`início de sessão demo como ${profile}`, async ({ page }) => {
    await page.goto('/')
    await page.getByRole('button', { name: profile, exact: true }).click()
    await expect(page.getByRole('button', { name: profile, exact: true })).toBeHidden()
    await expect(page.locator('nav').first()).toBeVisible()
  })
}

test('o cliente não vê vistas internas de produção', async ({ page }) => {
  await page.goto('/')
  await page.getByRole('button', { name: 'cliente', exact: true }).click()
  // O portal do cliente não deve expor o simulador nem a bancada do operador.
  await expect(page.getByText('Simulador de produção', { exact: false })).toHaveCount(0)
  await expect(page.getByText('Bancada do operador', { exact: false })).toHaveCount(0)
})
