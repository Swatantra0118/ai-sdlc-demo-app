import { expect, test } from '@playwright/test';
import { ApprovalsPage } from '../pages/ApprovalsPage';
import { InvoicesPage } from '../pages/InvoicesPage';
import { LoginPage } from '../pages/LoginPage';

test('manager cannot approve an invoice above the 50,000 limit', async ({ page }) => {
  const loginPage = new LoginPage(page);
  const invoicesPage = new InvoicesPage(page);
  const approvalsPage = new ApprovalsPage(page);
  const customerName = `Approval limit customer ${Date.now()}`;

  await loginPage.signIn('demo.manager', 'ChangeMe123!');
  await invoicesPage.createInvoice(customerName, '75000');
  const invoiceId = await invoicesPage.submitInvoice(customerName);

  await approvalsPage.open();
  await approvalsPage.approveInvoice(invoiceId);

  const decisionAlert = approvalsPage.decisionAlert();
  await expect(decisionAlert).toBeVisible();
  await expect(decisionAlert).toContainText('50,000.00');

  await invoicesPage.open();
  await expect(invoicesPage.invoiceRow(customerName)).toContainText('Pending Approval');
});
