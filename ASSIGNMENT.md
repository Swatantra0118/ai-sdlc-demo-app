# AI in SDLC assignment — approval limits by role

**Author:** Swatantra Tiwari · **PR:** [Pull Request #1](https://github.com/Swatantra0118/ai-sdlc-demo-app/pull/1) · **Video:** [Link to Demo MP4](https://drive.google.com/file/d/1hcvdsjKhP4_zwJyO_Mkv9EJT5sPAWM-4/view?usp=sharing)

## 1. Summary

I added role-based approval limits to the invoice approval app: a Manager can approve up to 50,000, a Director up to 500,000, and anything above 500,000 needs two different approvers, with a Director giving the second approval. I used GitHub Copilot Free in VS Code (a Feature Lead agent delegating to Implementer, Test Writer and Reviewer sub-agents), Spec Kit for the specification, Stryker.NET 4.13.0 for mutation testing and Playwright for the end-to-end test.

My main lesson: Copilot was fast at writing the domain logic and the test scaffolding, but the bugs that mattered were subtle ones it didn't notice itself, such as changing the invoice before throwing. They only showed up because I read every diff and checked the tests with mutation testing.

## 2. Where everything is

| Deliverable | Location |
| --- | --- |
| Agent context | `AGENTS.md` (+ before/after evidence in `docs/context-evidence.md`) |
| Skills | `.github/skills/` (approval-rules, dotnet-unit-tests, playwright-e2e, pr-review, ai-interaction-log) |
| Agents and sub-agents | `.github/agents/` (Feature Lead → Implementer, Test Writer, Reviewer) |
| Hooks | `.github/hooks/ai-hooks.json` + `.github/hooks/scripts/` |
| Specification | `.specify/`, `specs/001-approval-limits/` |
| Unit tests | `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs` |
| E2E test | `e2e/tests/approval-limits.spec.ts`, `e2e/pages/` |
| Test review | `docs/test-review.md` |
| Mutation scores | `docs/mutation-scores.md` (and section 7 below) |
| AI interaction log | `docs/ai-log/ai-interactions.jsonl`, `docs/ai-log/AI_INTERACTIONS.md`, `docs/ai-log/chat-exports/` |

## 3. Does the context file earn its place?

Yes. To test this, I asked Copilot the same five questions (limits, what happens above 500,000, where the rule lives, the test commands, what must stay unchanged on rejection) twice in fresh chats: once with the original AGENTS.md and once after adding my sections. Both answers are in `docs/context-evidence.md`.

- **Before:** the original AGENTS.md has no approval rules, so Copilot <had to guess the limits / searched the code and said it couldn't find them> and didn't mention that a rejected attempt must leave the invoice unchanged.
- **After:** it gave the exact limits (50,000.00 and 500,000.00, both inclusive), the two-approver rule with a Director second, `ApprovalLimits` in the Domain project, and the "validate first, change nothing on rejection" rule.

The new sections also kept the work consistent across sub-agents. The layering and test-naming rules (`<ClassUnderTest>Tests`, `Method_Condition_ExpectedResult`) were already in the original file. My additions gave every sub-agent the same rules, boundaries and testing expectations without me repeating them in each prompt.

## 4. How the work was split

| Commit | What and why |
| --- | --- |
| [`e9d0775`](https://github.com/Swatantra0118/ai-sdlc-demo-app/commit/e9d077528f780ce17c6da9275e2dbb284407a642) | Agent context: AGENTS.md rules, five skills, Feature Lead with three sub-agents, logging hooks |
| [`53f34c3`](https://github.com/Swatantra0118/ai-sdlc-demo-app/commit/53f34c35cf2b61124d6d02d09411d6ee917583a7) | Spec Kit specification, clarifications, plan and tasks split into Phase A / B / C |
| [`71e0ee6`](https://github.com/Swatantra0118/ai-sdlc-demo-app/commit/71e0ee6ea8dfddc7bd6ad29923fe7fa6bfaca9f4) | Phase A: single-approver limits (Manager 50,000, Director 500,000), boundary and no-change tests, demo users, Stryker baseline |
| [`48a25c1`](https://github.com/Swatantra0118/ai-sdlc-demo-app/commit/48a25c1b50f5bda539425d89262adf6bce4fa3ce) | Phase B, first pass: Copilot's dual-approval implementation |
| [`be56a93`](https://github.com/Swatantra0118/ai-sdlc-demo-app/commit/be56a93018a615c994ce9fb6000ea48d92a46f50) | Phase B, corrected after my review: two distinct approvers above 500,000, second must be a Director, rejected attempts change nothing |
| [`5f53f35`](https://github.com/Swatantra0118/ai-sdlc-demo-app/commit/5f53f351374e342d27abed277cc944f7c3455b01) | Tests strengthened after manual review: every negative test checks all fields and the update count; different-case same-user test |
| [`4376677`](https://github.com/Swatantra0118/ai-sdlc-demo-app/commit/437667789136998b5a5adb9ee4ea67caa04c8216) | Playwright test: a Manager's over-limit approval is rejected in the UI and the invoice stays Pending Approval |
| [`e517861`](https://github.com/Swatantra0118/ai-sdlc-demo-app/commit/e5178619838906e353f85b48b3c50cd8c4a65884) | Tests that kill the 7 mutants that survived in the approval code after the feature |


## 5. Where the agent went wrong

**Changing the invoice before rejecting it.** In Copilot's first Phase B version (`48a25c1`), the same-user check (`string.Equals(...)`) ran *after* some invoice fields had already been set in memory. The in-memory repository hands back the stored object itself, so a "rejected" attempt had already changed the invoice. Copilot's tests still passed, because they only checked that an exception was thrown, not that the invoice was untouched. I caught this while reading the diff. I had the checks moved before any assignment (`be56a93`), then made every negative test snapshot all six invoice fields and the repository update count (`5f53f35`), so this mistake now fails a test.

**Tests that checked too little.** After the feature, Stryker still found 7 surviving mutants in the approval code. The tests confirmed an exception was thrown or a status changed, but not the exact reason text or that `_repository.Update(invoice)` was actually called. I added targeted tests for each one (`e517861`).

## 6. Tests

- **Unit tests:** 22 tests, all passing (`dotnet test backend/InvoiceApp.sln`). They cover the inclusive limits at 50,000.00 and 500,000.00, one cent above each limit, the first and second approval, the second approver having to be a Director, the same user being blocked (including different letter case and extra spaces), the rejection reason text, and that nothing changes on rejection.
- **E2E test:** 1 Playwright test (`cd e2e; npm test`). A Manager tries to approve a 75,000 invoice; the UI shows the limit error and the invoice stays Pending Approval.
- **Review of generated tests:** I read every test and recorded a verdict for each (kept / edited) in `docs/test-review.md`, along with a rule-to-test table showing every rule has a test that fails if the rule is broken.

## 7. Mutation scores

Same command both times: `cd backend/tests/InvoiceApp.Tests; dotnet stryker`

```text
Before (baseline, before any feature code):
Time Elapsed 00:00:41.0055445
The final mutation score is 30.77 %
Killed:   11
Survived: 10
Timeout:   1

After (feature complete, after killing survivors):
Time Elapsed 00:00:42.0983661
The final mutation score is 70.00 %
Killed:   42
Survived:  0
Timeout:   0
CompileError: 1
Total mutants tested: 42
```

The score went from 30.77 % to 70.00 %, and no mutants survive. The overall score isn't 100 % because some mutants have no test running that code at all: Stryker counts these "no coverage" mutants against the score. Most are in `DiscountCalculator.cs`, which had no tests before this assignment and isn't part of this feature.

Right after the feature, 7 mutants survived in the approval code. They survived because the tests checked that an exception was thrown or a status changed, but not the exact error message or that `_repository.Update(invoice)` was called. The property initializers in `Invoice.cs` (`= string.Empty;`) also survived, because every test set those values explicitly. I added tests for the exact message contents, the repository update count and the default values, which brought the survivors to zero.

## 8. AI review findings

I ran the review with the Reviewer agent against `git diff main...HEAD`. The first pass reported 0 findings, but the agent hadn't actually run the diff, so that result was empty rather than clean. On the second pass, after I made it list the files it reviewed first, it reported 2 findings.

| ID | Severity | Finding | Verdict | Reason / commit |
| --- | --- | --- | --- | --- |
| R1 | Medium | Two approvals arriving at the same moment could race on `FirstApprovedBy` | Deferred | The service reads, checks and then writes the invoice, and nothing makes those steps one atomic operation. The in-memory demo store can't fix this properly; it needs a real database with a transaction or row lock. Out of scope for this feature. |
| R2 | Low | The invalid-second-approver test only uses a Manager, not every non-Director role | Dismissed | All non-Director roles go through the same `role != Director` check. The Manager case already fails if that check is removed or flipped (no mutants survive there), so separate Approver and Admin cases would repeat the same test. |

## 9. Logging approach

Three layers together give a complete record:

1. **Hooks** (`.github/hooks/ai-hooks.json` with `log-event.cjs`) write every Copilot event (`sessionStart`, `userPromptSubmitted`, `preToolUse`, `postToolUse`, `subagentStart`, `subagentStop`, `agentStop`) as one JSON line with the same fields each time into `docs/ai-log/ai-interactions.jsonl`. A second hook (`format-csharp.cjs`) runs `dotnet format` after any C# edit so AI-written code follows one style.
2. **The `ai-interaction-log` skill** appends a short summary at the end of each task to `docs/ai-log/AI_INTERACTIONS.md`, including my own verdict on what the agent produced.
3. **Chat exports** in `docs/ai-log/chat-exports/` as a backup.

The hooks reliably captured prompts and tool calls, but not the agent's full replies, which is why the chat exports are needed.

## 10. Limitations and what I'd do next

- **Concurrency (R1):** the approval check and write aren't atomic. With a real database I'd add a transaction or optimistic concurrency check.
- **Commit history:** Phase B is split across `48a25c1` and `be56a93`, and the first has a vague message. Next time I'd squash them before opening the PR.
- **Tooling changes:** the current VS Code no longer has a separate Ask mode or the `useAgentsMdFile` setting, so I ran the before/after AGENTS.md comparison against the original file instead of turning it off. Installing Spec Kit also failed at first because uv picked Python 3.15, which has no prebuilt PyYAML; pinning `--python 3.12` fixed it.
- **Preview features:** hooks are still a preview feature, so I kept chat exports as a backup in case they missed anything.
- **Copilot Free limits:** I hit rate limiting during multi-agent runs, and worked around it by giving the agents smaller, single-step prompts.