---
name: approval-rules
description: Business rules for invoice approval limits by role (Manager 50,000, Director 500,000, two distinct approvers above 500,000 with a Director second). Use when implementing, testing or reviewing anything in ApprovalService.Approve.
---
# Approval rules

## Decision table (amount in decimal)
| Situation | Result |
| --- | --- |
| Manager/Approver/Admin, amount <= 50,000.00 | Approved |
| Manager/Approver/Admin, amount > 50,000.00 | Rejected: exceeds 50,000.00 limit |
| Director, amount <= 500,000.00 | Approved |
| Any role, amount > 500,000.00, no first approval yet | First approval recorded, status stays PendingApproval |
| amount > 500,000.00, second approver not a Director | Rejected: second approver must be a Director |
| amount > 500,000.00, second approver is the first approver | Rejected: same user cannot be both approvers |
| amount > 500,000.00, Director different from first | Approved; DecidedBy = second approver |

## Hard rules
1. Validate every rule BEFORE assigning any field. Then assign, then call Update once.
2. On rejection throw ApprovalRejectedException(reason). Never call Update.
3. Compare usernames with Trim() and StringComparison.OrdinalIgnoreCase.
4. Limits live only in ApprovalLimits. No magic numbers elsewhere.

## Test matrix every change must keep green
50,000.00 Manager ok; 50,000.01 Manager rejected; 500,000.00 Director ok; 500,000.01 Director alone -> first approval only; same user twice rejected (also with different letter case); Manager as second approver rejected; each rejection leaves the invoice unchanged.