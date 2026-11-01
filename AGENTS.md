# Project conventions for AI agents

Invoice approval demo app: ASP.NET Core 8 Web API with a small static web UI.

## Layout

- `backend/src/InvoiceApp.Domain`: business rules, no web or storage code. `ApprovalService` owns the invoice lifecycle (Draft, PendingApproval, Approved, Rejected).
- `backend/src/InvoiceApp.Api`: controllers, sign-in (`Auth/`), in-memory repository, and the web UI under `wwwroot/`.
- `backend/tests/InvoiceApp.Tests`: xUnit tests, one test class per class under test, named `<ClassUnderTest>Tests`.
- `e2e`: Playwright end-to-end tests (`tests/`) and page objects (`pages/`).
- `docs/use-cases`: short briefs for features we may build.

## Conventions

- Controllers live in `Controllers/`, one per resource, route `api/[controller]`. Request bodies are `record` types declared inside the controller.
- Controllers inherit `ApiControllerBase` and use `Handle(...)` so service errors map to 404, 409 and 400.
- Every endpoint is behind sign-in. Restrict actions with `[RequireRole("...")]`: destructive actions are Admin only, approvals are Approver or Admin.
- Business rules go in the Domain project, never in controllers.
- Register services in `Program.cs`. Repositories are interfaces in Domain with an in-memory implementation in Api.
- Every new rule or endpoint needs tests. Use `FakeInvoiceRepository` in tests.
- Use clear names; no one-letter variables or `flag` parameters.
- End-to-end tests use page objects and role-based locators (`getByRole`, `getByLabel`), never CSS or XPath selectors.

## Commands

- Run the app: `dotnet run --project backend/src/InvoiceApp.Api` then open http://localhost:5000
- Run the tests: `dotnet test backend/InvoiceApp.sln`
- Run end-to-end tests: `cd e2e && npm install && npx playwright install chromium && npm test`

## Feature: approval limits by role

The source of truth is specs/ (Spec Kit). Summary of the rules:

| Role | May approve alone | Notes |
| --- | --- | --- |
| Manager | amount <= 50,000.00 | 50,000.00 is allowed; 50,000.01 is not |
| Director | amount <= 500,000.00 | 500,000.00 is allowed alone |
| Approver, Admin (legacy) | same limit as Manager | keeps old behaviour for small invoices |

- Above 500,000.00 an invoice needs TWO distinct approvers.
  - The first approval is recorded (FirstApprovedBy, FirstApprovedAt) and the status stays PendingApproval.
  - The second approver MUST be a Director.
  - The same user may NEVER count as both approvers. Compare usernames trimmed and case-insensitively.
- A rejected approval attempt throws ApprovalRejectedException (derives from InvalidOperationException, so the API returns 409) with a human-readable reason that names the limit or rule.
- On a rejected attempt NOTHING about the invoice changes: no field is assigned and IInvoiceRepository.Update is not called. Validate everything first, mutate last. The repository returns the stored object itself, so assigning a field before throwing IS a change.
- Amounts are decimal. Never use double or float for money. Limits live in one place: ApprovalLimits in the Domain project.

## Layering for this feature

- Domain (InvoiceApp.Domain): ApproverRole enum, ApprovalLimits, ApprovalRejectedException, and all decisions inside ApprovalService.Approve.
- Api: ApprovalsController only reads the signed-in user's name and role from the session and passes them in. [RequireRole] on approvals lists Manager, Director, Approver, Admin.
- UI (wwwroot/app.js): shows the error message in the existing decision-error alert. No rules in JavaScript.

## Testing expectations

- Unit tests: dotnet test backend/InvoiceApp.sln. Test boundaries exactly: 50,000.00 / 50,000.01 and 500,000.00 / 500,000.01.
- Every negative test also asserts the invoice is unchanged (status, DecidedBy, DecidedAt, FirstApprovedBy) AND that Update was not called.
- Name tests Method_Condition_ExpectedResult, e.g. Approve_ManagerAboveLimit_IsRejectedAndInvoiceUnchanged.
- E2E: cd e2e; npm test. Page objects in e2e/pages, role-based locators only.
- Mutation testing: cd backend/tests/InvoiceApp.Tests; dotnet stryker. A surviving mutant in approval code means a missing test.

## Review expectations

When reviewing a diff, report findings as a table: ID, Severity (High/Medium/Low), File:line, Finding, Suggested fix. Check in this order: rule correctness at boundaries, same-user check, "nothing changes on rejection", layering, test quality (would the test fail if the rule were broken?), naming. Do not invent findings to fill the table.

## Logging

All AI interactions are logged by hooks in .github/hooks/ to docs/ai-log/ai-interactions.jsonl. At the end of each task, use the ai-interaction-log skill to append a summary to docs/ai-log/AI_INTERACTIONS.md.

## Demo accounts (not real credentials, local demo only)

- demo.approver / ChangeMe123! (role Approver, Manager-level limit)
- demo.admin / ChangeMe123! (role Admin, Manager-level limit)
- demo.manager / ChangeMe123! (role Manager)
- demo.director / ChangeMe123! (role Director)
- demo.director2 / ChangeMe123! (role Director, used for second approvals)

## Never

- Commit real credentials, tokens or customer data.
- Add business logic to `Program.cs` or controllers.
