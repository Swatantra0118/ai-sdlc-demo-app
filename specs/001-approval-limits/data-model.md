# Data Model: Approval Limits by Role

## Invoice

Existing invoice entity extended to represent an incomplete high-value approval.

| Field | Type | Nullable | Meaning |
|---|---|---:|---|
| `Id` | string | No | Invoice identity |
| `Amount` | decimal | No | Exact invoice amount used for approval thresholds |
| `Status` | InvoiceStatus | No | Existing lifecycle state; remains `PendingApproval` after the first high-value approval |
| `FirstApprovedBy` | string | Yes | Username recorded for the first approval of an invoice above 500,000.00 |
| `FirstApprovedAt` | DateTime (UTC) | Yes | Time of the first approval |
| `DecidedBy` | string | Yes | Username completing approval or rejecting the invoice |
| `DecidedAt` | DateTime (UTC) | Yes | Time of final approval or rejection |
| `DecisionNote` | string | Yes | Existing rejection reason |

`FirstApprovedBy` and `FirstApprovedAt` are null before the first approval. They are
set together when a high-value first approval succeeds. They are not modified by a
rejected attempt. Final approval sets the existing decision fields and transitions
the invoice to `Approved`.

## ApproverRole

Domain role identifier passed to approval policy from the authenticated session.

| Role | Single-approval limit |
|---|---:|
| `Manager` | 50,000.00 |
| `Approver` | 50,000.00 |
| `Admin` | 50,000.00 |
| `Director` | 500,000.00 |

All roles authorized by the approval endpoint may provide the first approval above
500,000.00. A distinct `Director` must provide the second.

## ApprovalLimits

Single authoritative Domain definition of role thresholds, represented as `decimal`.
The 50,000.00 and 500,000.00 limits are inclusive. Amounts above 500,000.00 require
two approvals; an amount of exactly 500,000.00 may be approved alone by a Director.

## ApprovalRejectedException

Domain rejection type derived from `InvalidOperationException`. Its message identifies
the exceeded limit or violated approval rule. The existing API error handler maps it
to HTTP 409 with a human-readable error response.

## State Transitions

| Current state | Condition/action | Result |
|---|---|---|
| PendingApproval, amount <= applicable single limit | Authorized role approves | Approved; set `DecidedBy` and `DecidedAt` |
| PendingApproval, amount > 500,000.00, no first approver | Any currently authorized role approves | Remains PendingApproval; set `FirstApprovedBy` and `FirstApprovedAt` |
| PendingApproval with first approval, amount > 500,000.00 | Distinct Director approves | Approved; set final decision fields |
| Any approval rejection condition | Attempt is rejected | No invoice field changes; no repository update |

User identity comparison for the two approvals trims surrounding whitespace and
ignores case. Validation completes before state mutation.
