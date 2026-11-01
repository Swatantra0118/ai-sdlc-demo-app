---
name: pr-review
description: Review a pull request diff for the approval-limits feature and output findings in a fixed table. Use when asked to review a diff, branch or PR.
---
# PR review

Input: the diff of the current branch against main (run: git diff main...HEAD).

Check, in order:
1. Boundaries: <= vs < at 50,000 and 500,000; decimal not double.
2. Two-approver rule: second must be Director; same user (case-insensitive) blocked.
3. Nothing changes on rejection: no assignment before validation; Update not called.
4. Layering: no rules in controllers, Program.cs or app.js.
5. Tests: would each test fail if its rule were removed? Missing negative tests?
6. Security: no real secrets; role taken from the server session, never from the request body.

Output exactly this table and nothing before it:
| ID | Severity | File:line | Finding | Suggested fix |
Then one line: "Total findings: N". Do not pad with style nitpicks.