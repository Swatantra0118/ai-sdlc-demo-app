import { Locator, Page } from '@playwright/test';

export class ApprovalsPage {
  private readonly panel: Locator;

  constructor(private readonly page: Page) {
    this.panel = page.getByRole('tabpanel', { name: 'Approvals' });
  }

  async open(): Promise<void> {
    await this.page.getByRole('tab', { name: 'Approvals' }).click();
  }

  async approveInvoice(invoiceId: string): Promise<void> {
    await this.panel.getByRole('button', { name: `Approve invoice ${invoiceId}` }).click();
  }

  decisionAlert(): Locator {
    return this.panel.getByRole('alert');
  }
}
