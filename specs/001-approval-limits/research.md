# Research: Approval Limits by Role

## Decisions

### Domain rule validation

- **Decision**: Keep approval policy in the existing Domain service and test it with
  isolated xUnit tests, using explicit boundary cases and Arrange-Act-Assert structure.
- **Rationale**: The repository already has a domain service, fake repository, and
  xUnit project. Fast tests can cover each role/amount and rejected state transition
  without starting the web host.
- **Alternatives considered**: Testing approval limits only through API or browser
  calls was rejected because it makes exact rule coverage slower and obscures
  unchanged-state guarantees.
- **Reference**: [Microsoft unit-testing best practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)

### End-to-end coverage

- **Decision**: Use one accessible-locator Playwright scenario to exercise first
  approval, visible pending second-approval state, and completion by a different
  Director.
- **Rationale**: The scenario demonstrates the complete high-value user journey,
  while the unit suite covers the more extensive role and amount boundary matrix.
  Use Playwright assertions and unique invoice data instead of fixed waits.
- **Alternatives considered**: Repeating every threshold in the browser suite was
  rejected because unit tests are the more direct and reliable place for rule
  boundary coverage.
- **References**: [Playwright locators](https://playwright.dev/docs/locators),
  [Playwright assertions](https://playwright.dev/docs/test-assertions)

### Mutation testing

- **Decision**: Run Stryker.NET against approval-domain production code and review
  survivors affecting thresholds, role checks, identity normalization, and mutation
  order.
- **Rationale**: Mutation testing checks that tests detect meaningful changes in
  approval decisions; it supplements rather than replaces explicit boundary tests.
- **Alternatives considered**: Setting an arbitrary non-zero mutation-score gate
  before observing the baseline was rejected; first confirm the critical approval
  rule mutants are killed.
- **Toolchain note**: Stryker.NET 4.13.0 is installed globally in the inspected
  environment. The current Stryker.NET getting-started guidance indicates a .NET 10
  runtime is needed to run the tool; this environment has .NET 8 and 9 runtimes but
  no .NET 10 runtime. Keep the application target at .NET 8 and verify/install the
  tool runtime prerequisite before mutation validation.
- **References**: [Stryker.NET mutations](https://stryker-mutator.io/docs/stryker-net/mutations/),
  [Stryker.NET configuration](https://stryker-mutator.io/docs/stryker-net/configuration/),
  [Stryker.NET getting started](https://stryker-mutator.io/docs/stryker-net/getting-started/)

## Assumptions

- ASP.NET Core's existing session middleware supplies a role string matching the
  configured demo-user role names; the approval endpoint remains restricted to
  Manager, Director, Approver, or Admin.
- The new demo users use the local-demo password documented in `AGENTS.md`; these
  values are sample credentials, not production secrets.
- The Playwright acceptance scenario uses in-memory state and unique invoice data;
  no database reset or migration is needed.
- The .NET 8 target remains unchanged even if the installed Stryker tool requires a
  newer runtime to execute.
