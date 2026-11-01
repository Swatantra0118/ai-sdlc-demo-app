import { Page } from '@playwright/test';

export class LoginPage {
  constructor(private readonly page: Page) {}

  async signIn(username: string, password: string): Promise<void> {
    await this.page.goto('/');
    await this.page.getByLabel('Username').fill(username);
    await this.page.getByLabel('Password').fill(password);
    await this.page.getByRole('button', { name: 'Sign in' }).click();
  }
}
