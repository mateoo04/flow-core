import { expect, type Page } from '@playwright/test';

/** Signs in through the visible development-only demo-login UI. */
export async function signInAsDemoUser(page: Page): Promise<void> {
  await page.goto('/account/login');
  await expect(page.getByRole('heading', { name: 'Welcome back' })).toBeVisible();
  await page.getByRole('button', { name: 'Explore demo workspace' }).click();
  await expect(page.getByRole('heading', { name: 'My Tasks' })).toBeVisible();
}
