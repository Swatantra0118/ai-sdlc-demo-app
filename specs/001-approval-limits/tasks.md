# Tasks: Approval Limits by Role

**Input**: Design documents from `/specs/001-approval-limits/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/approvals.md

**Organization**: Tasks are ordered into the requested test-first delivery phases. Phase A and Phase B each end with a separate commit. Every task has a checklist, sequential ID, applicable story label, and concrete file path.

## Phase 1: Setup

**Purpose**: Reuse the existing .NET 8 solution, xUnit project, static UI, and Playwright project; no project initialization is needed.

No setup tasks.

---

## Phase 2: Foundational

**Purpose**: The existing in-memory repository, session authentication, API error mapping, and test projects already provide the required foundation.

No foundational tasks. Phase A can begin immediately.

---

## Phase 3: Phase A — Single-Approver Limits (User Story 1, Priority: P1)

**Goal**: Enforce role-specific solo limits, preserve legacy Approver/Admin behavior, and ensure rejected attempts return a clear reason without changing invoice state.

**Independent Test**: Unit tests prove Manager/Approver/Admin decisions at 50,000.00 and 50,000.01 and Director decisions at 500,000.00 and 500,000.01. Every rejected attempt preserves invoice fields and does not call `Update`.

### Tests for Phase A

> Write these tests first and confirm they fail against the current implementation.

- [ ] T001 [US1] Add xUnit boundary and rejection tests in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs` for Manager, Director, Approver, and Admin; assert inclusive limits, clear rejection reasons, unchanged status/DecidedBy/DecidedAt/FirstApprovedBy/FirstApprovedAt, and no repository update for every rejection; add an Update call counter in `backend/tests/InvoiceApp.Tests/FakeInvoiceRepository.cs`.

### Implementation for Phase A

- [ ] T002 [US1] Add nullable `string? FirstApprovedBy` and `DateTime? FirstApprovedAt` properties to `backend/src/InvoiceApp.Domain/Invoice.cs` as unused initial state; add `ApproverRole` enum and single-source decimal thresholds for Manager/Approver/Admin (50,000.00) and Director (500,000.00) in `backend/src/InvoiceApp.Domain/ApproverRole.cs` and `backend/src/InvoiceApp.Domain/ApprovalLimits.cs`; add `ApprovalRejectedException` derived from `InvalidOperationException` in `backend/src/InvoiceApp.Domain/ApprovalRejectedException.cs`.
- [ ] T003 [US1] Change `ApprovalService.Approve` to receive the approver role, validate pending state and solo limit before any mutation, and throw a human-readable `ApprovalRejectedException` without updating the repository when a solo limit is exceeded in `backend/src/InvoiceApp.Domain/ApprovalService.cs`; update the call and assertions in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs` to pass the role.
- [ ] T004 [US1] Pass the authenticated session role with the username to the domain approval service and permit Manager, Director, Approver, and Admin on approval actions in `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs`; add `demo.manager`, `demo.director`, and `demo.director2` with the local-demo password documented in `AGENTS.md` to `backend/src/InvoiceApp.Api/appsettings.json`.
- [ ] T005 [US1] Run `dotnet test backend/InvoiceApp.sln` and resolve Phase A failures while preserving the rejection no-mutation assertions in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`.
- [ ] T006 [US1] Commit the completed Phase A single-approver limits and tests separately, including `backend/src/InvoiceApp.Domain/ApprovalService.cs`, `backend/src/InvoiceApp.Domain/Invoice.cs`, `backend/src/InvoiceApp.Domain/ApproverRole.cs`, `backend/src/InvoiceApp.Domain/ApprovalLimits.cs`, `backend/src/InvoiceApp.Domain/ApprovalRejectedException.cs`, `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs`, `backend/src/InvoiceApp.Api/appsettings.json`, `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`, and `backend/tests/InvoiceApp.Tests/FakeInvoiceRepository.cs`.

**Checkpoint**: Phase A is independently testable and committed; ordinary single-approver decisions work, and the nullable first-approval fields remain unused.

---

## Phase 4: Phase B — Two Approvers Above 500,000 (User Story 2, Priority: P1)

**Goal**: Record a first approval while remaining pending for amounts above 500,000.00, then require a distinct Director to complete approval.

**Independent Test**: Unit tests prove first approval state, successful distinct Director completion, rejection of a repeated username despite case/whitespace differences, rejection of a non-Director second approver, and unchanged invoice state/no repository update for each rejected attempt.

### Tests for Phase B

> Write these tests first and confirm they fail against the Phase A implementation.

- [ ] T007 [US2] Add xUnit high-value approval tests in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs` for any currently authorized role recording the first approval above 500,000.00, a distinct Director completing approval, a non-Director being rejected as second approver, and same-user rejection after trimming and case-insensitive comparison; every rejected case must assert unchanged invoice state including FirstApprovedBy and no repository update.

### Implementation for Phase B

- [ ] T008 [US2] Extend `ApprovalService.Approve` in `backend/src/InvoiceApp.Domain/ApprovalService.cs` to validate all high-value second-approval rules before mutation, record first approval while status remains PendingApproval, and approve only after a distinct Director's second approval; keep `FirstApprovedBy` and `FirstApprovedAt` unchanged unless a valid first approval succeeds.
- [ ] T009 [US2] Show the recorded first approver and a clear pending-Director second-approval state for high-value invoices in `backend/src/InvoiceApp.Api/wwwroot/app.js`, using the existing invoice data fields and accessible markup.
- [ ] T010 [US2] Run `dotnet test backend/InvoiceApp.sln` and resolve Phase B failures while preserving unchanged-invoice and no-update assertions for rejected second approvals in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`.
- [ ] T011 [US2] Commit the completed Phase B two-approver rule, UI state, and tests separately, including `backend/src/InvoiceApp.Domain/ApprovalService.cs`, `backend/src/InvoiceApp.Api/wwwroot/app.js`, and `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`.

**Checkpoint**: Phase B is independently testable on top of the Phase A commit; invoices above 500,000.00 cannot be finalized by one user or without a Director second approver.

---

## Phase 5: Phase C — Playwright Over-Limit Rejection (User Story 3, Priority: P2)

**Goal**: Verify that a Manager's rejected approval attempt just above 50,000.00 shows the clear reason and leaves the invoice pending.

**Independent Test**: One browser test signs in as `demo.manager`, creates and submits a uniquely identified 50,000.01 invoice, attempts approval, verifies the visible rejection reason, and reloads or refreshes the invoice list to confirm it remains PendingApproval.

### Tests for Phase C

> Add exactly one Playwright end-to-end test using accessible role/name locators and unique invoice data.

- [ ] T012 [US3] Add exactly one Playwright test in `e2e/tests/approval-limits.spec.ts` and any required page-object support in `e2e/pages/approval-limits-page.ts`; sign in as `demo.manager`, submit a unique 50,000.01 invoice, attempt approval, check the decision-error alert for the limit reason, and verify after refresh that the invoice remains PendingApproval.

### Validation for Phase C

- [ ] T013 [US3] Run `npm test` from `e2e/` and fix failures in `e2e/tests/approval-limits.spec.ts` or its page objects without adding a second Playwright scenario.

**Checkpoint**: Exactly one end-to-end test proves the rejected over-limit user journey through the UI.

---

## Phase 6: Polish & Cross-Cutting Validation

**Purpose**: Verify test sensitivity and the documented validation workflow across the completed phases.

- [ ] T014 Verify the Stryker runtime prerequisite documented in `specs/001-approval-limits/research.md`, then run `dotnet stryker` from `backend/tests/InvoiceApp.Tests/` against approval rules without retargeting the application from .NET 8; investigate surviving threshold, role, username-normalization, and mutation-order mutants and record results in `specs/001-approval-limits/quickstart.md`.
- [ ] T015 Update `specs/001-approval-limits/quickstart.md` to document the implemented single Playwright rejection scenario and final validation outcomes while retaining the manual high-value approval verification.

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No tasks; the .NET solution and test projects already exist.
- **Foundational (Phase 2)**: No tasks; existing auth, repository, and error handling are reused.
- **Phase A (Phase 3 / US1)**: Starts immediately; write T001 before T002–T004, then validate and commit with T005–T006.
- **Phase B (Phase 4 / US2)**: Depends on the Phase A commit; write T007 before T008–T009, then validate and commit with T010–T011.
- **Phase C (Phase 5 / US3)**: Depends on the Phase A and Phase B commits and demo account setup; write and run exactly one E2E case with T012–T013.
- **Polish (Phase 6)**: Depends on all three phases; run Stryker and the documented validation with T014–T015.

### User Story Dependencies

- **US1 — Single-Approver Limits (P1)**: Independent MVP; includes the Phase A commit.
- **US2 — Complete High-Value Approval (P1)**: Extends US1 and depends on its separately committed behavior.
- **US3 — Understand Rejected Approval (P2)**: E2E verification depends on the Manager account and limit behavior from US1, plus the completed feature state from US2.

### Within Each Phase

- Unit or end-to-end tests are added before the feature implementation tasks in that phase.
- Phase A and Phase B must be committed separately before beginning the next phase.
- Domain policy remains in `InvoiceApp.Domain`; API code passes authenticated identity/role and the UI presents the service response.
- Rejection tests assert unchanged invoice state and no repository update.

### Parallel Opportunities

- No Phase A or Phase B test/implementation tasks are marked parallel because they share `ApprovalServiceTests.cs`, `FakeInvoiceRepository.cs`, and sequential compile-time contracts.
- The Phase C page-object support and its single test are intentionally one task to avoid creating an unreferenced helper or splitting a single test's setup.
- Phase A, Phase B, and Phase C are sequential by the requested separately committed delivery order.

## Parallel Example: Phase A

```text
T001 is completed first and must fail against the existing implementation.
Then complete T002, T003, and T004 in order because the API depends on the new Domain role type.
Complete T005 and the separate Phase A commit T006 before starting Phase B.
```

## Parallel Example: Phase B

```text
Complete and fail T007 against the Phase A implementation first.
Then complete T008 before T009 so the UI can consume the first-approval fields.
Complete T010 and commit Phase B with T011 before beginning Phase C.
```

## Implementation Strategy

1. **MVP — Phase A**: Test and deliver inclusive single-approver limits, clear rejection reasons, unchanged-state behavior, legacy role compatibility, and demo role access; commit independently.
2. **Increment — Phase B**: Test and deliver the two-approver high-value path, same-user prevention, Director second approval, and visible pending state; commit independently.
3. **Acceptance — Phase C**: Add exactly one Playwright test for a rejected Manager over-limit attempt and verify state remains pending.
4. **Quality gate**: Run Stryker against approval policy and the complete quickstart validation; do not treat a surviving approval-rule mutant as acceptable.
