---
name: playwright-e2e
description: Conventions for Playwright end-to-end tests in e2e/ (page objects, role-based locators, unique test data). Use when writing or fixing e2e tests.
---
# Playwright tests

- Tests in e2e/tests/.spec.ts, page objects in e2e/pages/.ts.
- Locators: getByRole, getByLabel, getByText only. No CSS or XPath.
- The app keeps data in memory for the whole run, so create your own invoice with a unique customer name (include Date.now()).
- Useful accessible names in this UI: textbox "Username", "Password", "Customer name", "Amount"; buttons "Sign in", "Create invoice", "Submit invoice <ID> for approval", "Approve invoice <ID>"; tab "Approvals"; the error is the alert inside the approvals panel.
- Assert both the visible error text AND that the invoice is still Pending Approval in the Invoices table.
- Run: cd e2e; npm test