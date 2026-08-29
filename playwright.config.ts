import { defineConfig } from '@playwright/test';

const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? 'http://127.0.0.1:5055';
const usesExternalServer = Boolean(process.env.PLAYWRIGHT_BASE_URL);

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 2 : 0,
  reporter: process.env.CI ? [['html'], ['github']] : 'list',
  use: {
    baseURL,
    trace: 'retain-on-failure',
  },
  projects: [{ name: 'e2e' }],
  webServer: usesExternalServer
    ? undefined
    : {
        command: 'dotnet run --project FlowCore -c Release --no-launch-profile --urls http://127.0.0.1:5055',
        url: baseURL,
        reuseExistingServer: !process.env.CI,
        timeout: 120_000,
        env: {
          ASPNETCORE_ENVIRONMENT: 'Development',
          FLOWCORE_E2E: 'true',
        },
      },
});
