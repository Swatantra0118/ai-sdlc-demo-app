# Quickstart: Approval Limits by Role

## Prerequisites

- .NET 8 SDK/runtime
- Node.js and npm for the existing Playwright project
- Chromium installed for Playwright
- Stryker.NET and a compatible runtime for the installed Stryker version (current
  Stryker.NET guidance requires .NET 10 to run the tool; the application remains
  targeted at .NET 8)

## Run unit tests

From the repository root:

```powershell
dotnet test backend/InvoiceApp.sln
```

Expected: the approval service test suite passes, including exact boundary values
50,000.00 / 50,000.01 and 500,000.00 / 500,000.01. Every rejected-attempt test
verifies unchanged invoice state and no repository update.

## Run the end-to-end test

Install the project's Node dependencies and browser once:

```powershell
Set-Location e2e
npm install
npx playwright install chromium
npm test
```

Expected: the single browser scenario signs in as `demo.manager`, records the first
approval for a high-value invoice, shows that the invoice awaits a Director, then
signs in as `demo.director2` and completes approval.

## Run mutation testing

From `backend/tests/InvoiceApp.Tests`:

```powershell
dotnet stryker
```

Expected: inspect mutation results for the Domain approval rules. Mutants changing
either threshold boundary, first/second approval role rules, same-user comparison, or
rejection mutation ordering must be killed. Confirm the installed Stryker.NET runtime
prerequisite before running; do not retarget the application from .NET 8.

## Manual API verification

Use the contract in [contracts/approvals.md](./contracts/approvals.md). With a valid
session, verify that a high-value first approval returns a pending invoice with its
first approver/time, and a distinct Director's approval returns an approved invoice.
