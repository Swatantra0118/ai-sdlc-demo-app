import { Locator, Page } from '@playwright/test';

export class InvoicesPage {
  private readonly panel: Locator;

  constructor(private readonly page: Page) {
    this.panel = page.getByRole('tabpanel', { name: 'Invoices' });
  }

  async createInvoice(customerName: string, amount: string): Promise<void> {
    await this.panel.getByLabel('Customer name').fill(customerName);
    await this.panel.getByLabel('Amount').fill(amount);
    await this.panel.getByRole('button', { name: 'Create invoice' }).click();
    await this.invoiceRow(customerName).waitFor();
  }

  async submitInvoice(customerName: string): Promise<string> {
    const invoiceRow = this.invoiceRow(customerName);
    const invoiceId = await invoiceRow.getByRole('cell').first().innerText();
    await invoiceRow.getByRole('button', { name: `Submit invoice ${invoiceId} for approval` }).click();
    return invoiceId;
  }

  invoiceRow(customerName: string): Locator {
    return this.panel.getByRole('row').filter({ hasText: customerName });
  }

  async open(): Promise<void> {
    await this.page.getByRole('tab', { name: 'Invoices' }).click();
  }
}
