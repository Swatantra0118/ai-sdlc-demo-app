## Before AGENTS.md changes

1. **Manager/Director limits and inclusivity:** Not specified in the repository’s docs or code, so I can’t confirm the amounts or whether the thresholds are inclusive.
2. **Above 500,000:** The behavior is not specified; the app’s current materials don’t define escalation or rejection for that amount.
3. **Rule location:** Put it in `InvoiceApp.Domain`, in `ApprovalService.cs`. Controllers must **not** contain business logic, per `AGENTS.md`.
4. **Test commands:** Unit: `dotnet test backend/InvoiceApp.sln`. E2E: `cd e2e && npm install && npx playwright install chromium && npm test` (from `AGENTS.md`).
5. **On rejection:** The invoice’s amount must stay unchanged; rejection updates the status and decision metadata in `ApprovalService.cs`.

# After AGENTS.md changes

1. **Manager:** up to and including **50,000.00**. **Director:** up to and including **500,000.00**. `AGENTS.md`
2. Above **500,000.00**, two distinct approvers are required. The first approval is recorded and the invoice stays pending; a Director must provide the second approval.
3. Put the rule in **InvoiceApp.Domain**, with limits centralized in `ApprovalLimits` and approval decisions in `ApprovalService.Approve`. Controllers must not contain business rules; they pass the signed-in user’s name and role to the service.
4. Unit tests: `dotnet test backend/InvoiceApp.sln`  
   E2E tests: `cd e2e && npm install && npx playwright install chromium && npm test`
5. On a rejected attempt, **nothing on the invoice may change**—including its status or decision fields—and the repository’s `Update` must not be called.