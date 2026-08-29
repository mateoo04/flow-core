import { expect, test } from '@playwright/test';
import { signInAsDemoUser } from './helpers/auth';

test.describe.serial('FlowCore UI – task workflow', () => {
  test('creates, assigns, comments on, edits and deletes a task through the browser', async ({ page }) => {
    const stamp = `Playwright task ${Date.now()}`;
    const updatedTitle = `${stamp} updated`;
    const comment = `Playwright comment ${Date.now()}`;

    await test.step('1. Sign in through the visible demo-login screen', async () => {
      await signInAsDemoUser(page);
    });

    await test.step('2. Open the Acme workspace from the navigation', async () => {
      await page.getByRole('button', { name: 'Workspaces' }).click();
      await page.getByRole('link', { name: 'Acme Corporation', exact: true }).click();
      await expect(page.getByRole('heading', { name: 'Acme Corporation', exact: true })).toBeVisible();
    });

    await test.step('3. Open a project from the workspace screen', async () => {
      await page.getByRole('link', { name: /Acme\.com: marketing & sign-up/ }).click();
      await expect(page.getByRole('heading', { name: 'Acme.com: marketing & sign-up', exact: true })).toBeVisible();
    });

    await test.step('4. Click New task on the project board', async () => {
      await page.getByRole('link', { name: 'New task', exact: true }).first().click();
      await expect(page.getByRole('heading', { name: 'New task', exact: true })).toBeVisible();
    });

    await test.step('5. Fill in the task title, description, status, priority and estimate', async () => {
      await page.getByLabel('Title').fill(stamp);
      await page.getByLabel('Description').fill('Created entirely through the Playwright browser scenario.');
      await page.getByLabel('Status').selectOption({ label: 'In progress' });
      await page.getByLabel('Priority').selectOption({ label: 'High' });
      await page.getByLabel('Story points').fill('5');
    });

    await test.step('6. Assign Sam Member using the assignee autocomplete control', async () => {
      const assigneeInput = page.locator('[data-autocomplete-multi] [data-ac-input]');
      await assigneeInput.fill('Sam');
      await expect(page.getByRole('option', { name: /Sam Member/ })).toBeVisible();
      await assigneeInput.press('Enter');
      await expect(page.locator('[data-ac-chip]')).toContainText('Sam Member');
    });

    await test.step('7. Submit the task form and verify the visible task details', async () => {
      await page.getByRole('button', { name: 'Create task' }).click();
      await expect(page.getByRole('heading', { name: stamp, exact: true })).toBeVisible();
      await expect(page.getByText('Created entirely through the Playwright browser scenario.')).toBeVisible();
      await expect(page.getByText('Sam Member', { exact: true })).toBeVisible();
    });

    await test.step('8. Add a comment through the task screen', async () => {
      await page.getByLabel('Add comment').fill(comment);
      await page.getByRole('button', { name: 'Add comment' }).click();
      await expect(page.getByText(comment, { exact: true })).toBeVisible();
    });

    await test.step('9. Edit the task title through the UI', async () => {
      await page.getByRole('link', { name: 'Edit task' }).click();
      await expect(page.getByRole('heading', { name: 'Edit task' })).toBeVisible();
      await page.getByLabel('Title').fill(updatedTitle);
      await page.getByRole('button', { name: 'Save changes' }).click();
      await expect(page.getByRole('heading', { name: updatedTitle, exact: true })).toBeVisible();
    });

    await test.step('10. Delete the task and confirm it no longer appears on the project board', async () => {
      page.once('dialog', dialog => dialog.accept());
      await page.getByRole('button', { name: 'Delete task' }).click();
      await expect(page.getByRole('heading', { name: 'Acme.com: marketing & sign-up', exact: true })).toBeVisible();
      await expect(page.getByText(updatedTitle, { exact: true })).toHaveCount(0);
    });
  });
});
