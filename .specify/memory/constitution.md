<!--
Sync Impact Report
Version change: unratified scaffold -> 1.0.0
Modified principles: None; all five core principles are established for the first time.
Added sections: Additional Constraints; Development Workflow.
Removed sections: None.
-->
# Invoice Approval Demo Constitution

## Core Principles

### I. Domain Owns Business Rules
All invoice business rules MUST reside in `InvoiceApp.Domain`. Controllers, the API
startup configuration, and the web UI MUST NOT implement or duplicate business
decisions. API layers may translate requests, pass authenticated identity and role to
domain services, and present service results. This separation keeps rules consistent
across entry points.

### II. Exact Money
All monetary values and calculations MUST use `decimal`; `double` and `float` MUST NOT
be used for money. A business limit MUST have one authoritative definition in the
Domain project. Exact representation and a single source of truth prevent rounding
errors and inconsistent decisions.

### III. Every Rule Has Effective Unit Tests
Every business rule MUST have unit tests, including applicable boundary and negative
cases. Negative tests MUST verify that rejected operations leave the invoice unchanged
and do not call the repository update operation. Tests MUST be named
`Method_Condition_ExpectedResult`. The test suite MUST use mutation testing with Stryker
to verify that tests fail when a rule is removed or materially changed.

### IV. Rejection Is Atomic
An operation that is rejected MUST leave all persisted and in-memory domain state
unchanged. Implementations MUST validate every rejection condition before mutating an
entity or calling a repository update. This prevents partial state changes even when
the repository exposes stored objects directly.

### V. Changes Are Small and Separately Committed
Implementation work MUST be divided into small, cohesive steps, with each completed
step committed separately. Each step MUST preserve existing behavior outside its
stated scope and include the tests and directly related documentation needed for its
change. Small changes make review and regression diagnosis more reliable.

## Additional Constraints

- The application targets .NET 8 and follows the project structure documented in
  `AGENTS.md`.
- Domain code MUST NOT depend on web or storage implementation details.
- API endpoints MUST follow the existing authentication, role-authorization, and
  service-error handling conventions.
- End-to-end tests MUST use page objects and role-based locators; CSS and XPath
  selectors MUST NOT be used.
- Real credentials, tokens, and customer data MUST NOT be committed. Demo credentials
  are for the local demo only.

## Development Workflow

- Before implementation, identify the governing requirements and the smallest
  independently reviewable change.
- Add or update unit tests for every changed rule, including boundary and rejected
  cases; run the targeted tests before committing.
- Run the repository's relevant build, test, and end-to-end checks for the changed
  surface. Run Stryker for approval-rule changes and investigate surviving mutants.
- Review every change for domain layering, unchanged state on rejection, effective
  tests, and consistency with `AGENTS.md`.
- Keep documentation that directly governs changed behavior synchronized with the
  implementation.

## Governance

This constitution governs project implementation and review. Amendments MUST be
proposed as documented changes and reviewed for consistency with the existing
principles. Every pull request or equivalent change review MUST assess compliance
with this constitution; deviations require an explicit, reviewed amendment rather
than an undocumented exception.

The version uses semantic versioning: MAJOR for backward-incompatible removal or
redefinition of a principle, MINOR for a new principle or materially expanded
governance, and PATCH for clarifications and non-semantic refinements. The
`AGENTS.md` file provides operational guidance and MUST remain consistent with this
constitution.

**Version**: 1.0.0 | **Ratified**: 2026-10-31 | **Last Amended**: 2026-10-31
