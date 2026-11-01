# Test review — approval limits by role

I went through every test in ApprovalServiceTests.cs myself after Copilot's Test Writer sub-agent generated or rewrote them. For each one I asked three things: which rule does it check, would it fail if that rule were broken, and (for negative tests) does it prove the invoice wasn't touched.

*Summary:* 16 test methods, 20 test cases once the [Theory] rows are counted. I kept 14 as they were and edited 2. I didn't reject any outright, but the Phase A test that expected a Director at 500,000.01 to be rejected was replaced in Phase B (details below).

Overall the generated tests are better than I expected. The best decision was the shared AssertRejectedWithoutChanges helper: every negative test snapshots all six invoice fields plus UpdateCount before the call and checks them afterwards. That means a bug where the code sets a field and then throws can't slip through. I checked this by moving invoice.Status = InvoiceStatus.Approved; above the limit check, and three tests failed straight away.

## 11.2 Every test, with my verdict

| Test | Rule covered | Mistake it catches | Verdict |
| --- | --- | --- | --- |
| Submit_Draft_MovesToPendingApproval | Lifecycle: draft → pending | Submit putting the invoice in the wrong state | Kept. Existing test, only renamed. |
| Submit_NonDraft_Throws | Lifecycle: only drafts can be submitted | Re-submitting an already decided invoice | Kept. Existing test, only renamed. |
| Approve_SmallInvoiceByApprover_ApprovesAndRecordsDecision | Legacy Approver role still works for small amounts | The new limits breaking the old happy path | Kept. Existing test adapted to the new role parameter. |
| Approve_Draft_Throws | Only pending invoices can be approved | Approving a draft | Kept. Existing test, new signature. |
| Approve_MissingInvoice_Throws | Unknown invoice → not found | Treating a missing invoice as approvable | Kept. Existing test, new signature. |
| Approve_AmountAtRoleLimit_Approves (Manager, Approver, Admin at 50,000.00) | Manager-level limit is inclusive | >= used instead of > | Kept. It doesn't check UpdateCount, but the Director 500,000 test already covers "Update is called on success". |
| Approve_DirectorAt500000_ApprovesInOneStep | Director limit is inclusive; no second approver needed at exactly 500,000.00 | Two-approver rule kicking in one cent too early | Kept. Good: it also checks no first-approval fields were set and Update ran exactly once. |
| Approve_AmountAboveRoleLimit_RejectsWithoutChangingInvoice (Manager, Approver, Admin at 50,000.01) | Manager-level limit + nothing changes on rejection | Limit set too high; fields changed before the throw; Update called on rejection | Kept. The setup is a bit odd (it gives a 50,000.01 invoice a FirstApprovedBy, which can't happen in real use), but it makes the "unchanged" check stricter, so I left it. It also checks the message names the role, the limit and the amount. |
| Approve_DirectorAt50000001_RecordsFirstApprovalAndRemainsPending | Above 500,000 the first approval is recorded and the invoice stays pending | Approving a high-value invoice in one step | *Edited: renamed* to Approve_DirectorJustAbove500000_RecordsFirstApprovalAndRemainsPending. "50000001" reads like fifty million. This test replaces the Phase A test that expected a rejection here; that change was on purpose, because Phase B changes the rule. |
| Approve_ManagerAsFirstApproverAbove500000_RecordsFirstApprovalAndRemainsPending | Any approval role can give the first of two approvals | Wrongly applying the Manager limit to a first approval | Kept. This matches the decision I made at the Spec Kit clarify step. |
| Approve_DifferentDirectorSecond_ApprovesInvoice | A different Director completes the approval | Two-approver flow never finishing | Kept. I like that it also checks the first approver's name and timestamp weren't overwritten. |
| Approve_SameUserTwice_IsRejectedWithoutChangingInvoice | The same user can never be both approvers | Missing same-user check | Kept. The message check only looks for the word "same", which is loose, but the field and UpdateCount checks do the real work. |
| Approve_SameUserWithDifferentCaseAndWhitespace_IsRejectedWithoutChangingInvoice | Same-user check is trimmed and case-insensitive | Comparing usernames with plain == | Kept. This is the trap I was most worried about. Swapping OrdinalIgnoreCase for Ordinal makes it fail, as it should. |
| Approve_ManagerAsSecondApprover_IsRejectedWithoutChangingInvoice | Second approver must be a Director | Letting a non-Director finish a high-value approval | Kept. |
| Reject_PendingInvoice_RecordsReasonAndApprover | Reject stores the reason and who rejected | Losing the reason or the approver | *Edited.* The name promises the approver is recorded, but the test never checked DecidedBy. I added Assert.Equal("demo.approver", result.DecidedBy);. Before this, deleting invoice.DecidedBy = approver; from Reject broke no test. |
| Reject_EmptyReason_Throws | A rejection needs a reason | Accepting a blank reason | Kept. Existing test, not part of this feature. |

## 11.3 Does every rule have a test?

| Rule | Positive test | Negative test |
| --- | --- | --- |
| Manager may approve up to 50,000 | Approve_AmountAtRoleLimit_Approves (Manager at 50,000.00) | Approve_AmountAboveRoleLimit_RejectsWithoutChangingInvoice (Manager at 50,000.01) |
| Approver and Admin keep the Manager limit | Approve_AmountAtRoleLimit_Approves (Approver, Admin at 50,000.00); Approve_SmallInvoiceByApprover_ApprovesAndRecordsDecision | Approve_AmountAboveRoleLimit_RejectsWithoutChangingInvoice (Approver, Admin at 50,000.01) |
| Director may approve up to 500,000 | Approve_DirectorAt500000_ApprovesInOneStep | Above 500,000 it becomes a first approval, not a rejection: Approve_DirectorJustAbove500000_RecordsFirstApprovalAndRemainsPending |
| Above 500,000 needs two approvers | Approve_ManagerAsFirstApproverAbove500000_…, Approve_DirectorJustAbove500000_…, Approve_DifferentDirectorSecond_ApprovesInvoice | Approve_DirectorJustAbove500000_… (one approval alone doesn't approve) |
| Second approver must be a Director | Approve_DifferentDirectorSecond_ApprovesInvoice | Approve_ManagerAsSecondApprover_IsRejectedWithoutChangingInvoice |
| The same user can never be both approvers | — | Approve_SameUserTwice_IsRejectedWithoutChangingInvoice, Approve_SameUserWithDifferentCaseAndWhitespace_IsRejectedWithoutChangingInvoice |
| A rejected attempt gives a clear reason | — | All four rejection tests check the exception message (role, limit and amount for limit rejections; "same" or "Director" for the two-approver rules) |
| A rejected attempt changes nothing | — | Every rejection test goes through AssertRejectedWithoutChanges: Status, DecidedBy, DecidedAt, DecisionNote, FirstApprovedBy, FirstApprovedAt and UpdateCount are all unchanged |

Every rule has at least one test that fails when the rule is broken. Empty "positive" cells are expected, because those rules only say what must not happen.

## Gaps I noticed but left alone

- Only a Manager is tested as an invalid second approver. Approver and Admin go through the same role != Director check, so one case is enough to kill that mutant. I chose not to add two near-identical tests.
- Reject_EmptyReason_Throws doesn't check the invoice is unchanged. That's original code outside this feature, so I've noted it rather than expanding the scope.