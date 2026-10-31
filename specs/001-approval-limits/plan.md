# Implementation Plan: Approval Limits by Role

**Branch**: `001-approval-limits` | **Date**: 2026-11-01 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-approval-limits/spec.md`

## Summary

Add role-aware invoice approval limits in the Domain service using exact decimal
thresholds. Manager, Approver, and Admin may approve alone through 50,000.00;
Director may approve alone through 500,000.00. Above 500,000.00, any role currently
authorized to approve may record the first approval and a distinct Director must
complete it. Rejected attempts return clear reasons and do not mutate or persist
invoice state. The API will pass the signed-in session role, the UI will show pending
second-approval details and rejection reasons, and local demo accounts
`demo.manager`, `demo.director`, and `demo.director2` will be added to configuration.
The change will be verified with unit, end-to-end, and mutation tests.

## Technical Context

**Language/Version**: C# / .NET 8

**Primary Dependencies**: ASP.NET Core 8 Web API; xUnit 2.9.2; Playwright Test
1.49.x

**Storage**: Existing singleton in-memory invoice repository; no database

**Testing**: `dotnet test backend/InvoiceApp.sln`; Playwright via `cd e2e; npm test`;
Stryker.NET targeting approval-domain code

**Target Platform**: ASP.NET Core web application and Chromium browser; Windows
development environment

**Project Type**: ASP.NET Core web service with static browser UI

**Performance Goals**: No feature-specific latency or throughput target; retain
existing demo-app response behavior

**Constraints**: Monetary values and thresholds use `decimal`; approval rules remain
in `InvoiceApp.Domain`; validate before mutation; preserve existing Approver/Admin
behavior through 50,000.00; no database; follow `AGENTS.md`

**Scale/Scope**: Single-process in-memory demo; one invoice is approved at a time;
one browser end-to-end scenario covers the two-person approval journey

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Domain owns business rules**: PASS — role limits, rejection decisions, and
  approval transitions will be implemented in `InvoiceApp.Domain`.
- **Exact money**: PASS — thresholds and comparisons use `decimal`.
- **Every rule has effective unit tests**: PASS — boundary and negative cases are
  planned, with unchanged-state assertions; Stryker validates test sensitivity.
- **Rejection is atomic**: PASS — validate role, limit, prior approver, and invoice
  state before changing invoice fields or updating the repository.
- **Small, separately committed changes**: PASS — implementation can be divided into
  domain, API/configuration, UI, and validation steps; each is independently
  reviewable.
- **Project constraints**: PASS — retain .NET 8, the existing in-memory store,
  authentication conventions, and accessible Playwright locators.

No constitution violations or unjustified complexity are identified.

**Post-Design Gate**: PASS — the design adds only Domain approval types and invoice
fields, extends the existing API/session/UI, and reuses the established xUnit and
Playwright projects. The contract preserves authorization and error conventions; no
database, new project, or business-rule duplication is introduced.

## Project Structure

### Documentation (this feature)

```text
specs/001-approval-limits/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── approvals.md
├── checklists/
│   └── requirements.md
└── tasks.md              # Created by /speckit-tasks
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── InvoiceApp.Domain/
│   │   ├── ApprovalService.cs
│   │   ├── Invoice.cs
│   │   ├── ApproverRole.cs                 # New
│   │   ├── ApprovalLimits.cs               # New
│   │   └── ApprovalRejectedException.cs    # New
│   └── InvoiceApp.Api/
│       ├── Controllers/ApprovalsController.cs
│       ├── Auth/RequireRoleAttribute.cs
│       ├── appsettings.json
│       └── wwwroot/
│           ├── app.js
│           └── index.html
└── tests/InvoiceApp.Tests/
    └── ApprovalServiceTests.cs
e2e/
├── pages/
└── tests/
    └── approval-limits.spec.ts             # One end-to-end test
```

**Structure Decision**: Extend the existing Domain, API, test, and Playwright projects
in place. Do not create a new application project, persistence layer, or database.
Keep authorization context extraction in the API and approval policy in the Domain.
