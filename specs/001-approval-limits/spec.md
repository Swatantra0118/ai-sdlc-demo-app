# Feature Specification: Approval Limits by Role

**Feature Branch**: `001-approval-limits`

**Created**: 2026-10-31

**Status**: Draft

**Input**: User description: "Add approval limits by role to the invoice approval service. A Manager may approve invoices up to 50,000. A Director may approve invoices up to 500,000. Anything above 500,000 needs two distinct approvers, and the second approver must be a director. The same user may never count as both approvers on one invoice. A rejected attempt returns a clear reason, and nothing about the invoice changes. Existing Approver and Admin roles keep working for small invoices."

## Clarifications

### Session 2026-10-31

- Q: Who may provide the first approval for an invoice above 500,000.00? → A: Any currently approval-authorized role may provide the first approval; a distinct Director must provide the second.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Approve Within Role Limit (Priority: P1)

An authorized Manager or Director reviews an invoice and approves it when its amount is within that role's single-approval limit. Existing Approver and Admin users can continue approving small invoices.

**Why this priority**: This preserves and extends the primary invoice approval flow while applying role-appropriate authority.

**Independent Test**: Submit invoices at and below each role's stated limit for approval; confirm authorized users can complete approval and legacy roles still approve small invoices.

**Acceptance Scenarios**:

1. **Given** a pending invoice for 50,000.00, **When** a Manager approves it, **Then** the invoice is approved.
2. **Given** a pending invoice for 50,000.01, **When** a Manager or a legacy Approver or Admin attempts to approve it alone, **Then** the attempt is rejected with a clear reason and the invoice remains unchanged.
3. **Given** a pending invoice for 500,000.00, **When** a Director approves it, **Then** the invoice is approved.
4. **Given** a pending invoice for 500,000.01, **When** a Director attempts to approve it alone, **Then** the attempt is rejected with a clear reason and the invoice remains unchanged.
5. **Given** a pending invoice for a small amount within the Manager limit, **When** an existing Approver or Admin approves it, **Then** the invoice is approved as it was before this feature.

---

### User Story 2 - Complete High-Value Approval (Priority: P1)

An authorized user begins approval of an invoice above 500,000.00, and a different Director completes the required second approval.

**Why this priority**: High-value invoices require separation of approvers and a Director's final authorization.

**Independent Test**: Start approval of a pending invoice over 500,000.00; verify it remains pending after the first distinct approval and is approved only after a different Director approves it.

**Acceptance Scenarios**:

1. **Given** a pending invoice above 500,000.00, **When** its first authorized approver approves it, **Then** the first approver and approval time are recorded and the invoice remains pending.
2. **Given** an invoice with a recorded first approval, **When** the same user attempts the second approval using a differently cased or whitespace-padded version of their username, **Then** the attempt is rejected and the invoice remains unchanged.
3. **Given** an invoice with a recorded first approval, **When** a second distinct user who is not a Director attempts approval, **Then** the attempt is rejected and the invoice remains unchanged.
4. **Given** an invoice with a recorded first approval, **When** a distinct Director approves it, **Then** the invoice becomes approved and records the completing approver.

---

### User Story 3 - Understand Rejected Approval (Priority: P2)

An approver who cannot approve an invoice receives a clear explanation of the rule that prevented approval, without changing invoice data.

**Why this priority**: Clear feedback makes approval decisions understandable and protects invoice data from partial changes.

**Independent Test**: Trigger each applicable rejection and verify that the reason identifies the violated limit or approval rule while all invoice fields remain as before the attempt.

**Acceptance Scenarios**:

1. **Given** an approval attempt that exceeds the user's single-approval limit, **When** the attempt is rejected, **Then** its reason names the applicable limit or required approval.
2. **Given** a rejected approval attempt, **When** the invoice is inspected afterward, **Then** its status and approval/decision fields are unchanged.

---

### Edge Cases

- An invoice exactly at 50,000.00 is within the Manager, Approver, and Admin single-approval limit; 50,000.01 is above it.
- An invoice exactly at 500,000.00 is within the Director single-approval limit; 500,000.01 requires two distinct approvers.
- A user attempting both approvals is treated as the same user even if username casing or surrounding whitespace differs.
- A non-Director cannot complete the second approval for an invoice above 500,000.00.
- A failed second approval must preserve the previously recorded first approval as well as all other invoice fields.
- Rejection at any stage must not change invoice state or persist an update.
- Existing Approver and Admin roles retain their ability to approve invoices within the Manager-level limit.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow a Manager to approve a pending invoice alone when its amount is at most 50,000.00.
- **FR-002**: The system MUST allow a Director to approve a pending invoice alone when its amount is at most 500,000.00.
- **FR-003**: Existing Approver and Admin roles MUST retain the ability to approve pending invoices alone when the amount is at most 50,000.00.
- **FR-004**: The system MUST require two distinct approvers for an invoice whose amount exceeds 500,000.00.
- **FR-005**: After the first approval of an invoice above 500,000.00, the system MUST record the first approver and approval time while leaving the invoice pending.
- **FR-006**: The second approver for an invoice above 500,000.00 MUST be a Director.
- **FR-007**: The system MUST reject an attempt for one user to provide both approvals for the same invoice. Usernames MUST be compared after trimming surrounding whitespace and without regard to case.
- **FR-008**: Every rejected approval attempt MUST provide a human-readable reason identifying the applicable limit or approval rule.
- **FR-009**: A rejected approval attempt MUST leave all invoice state unchanged, including status and approval/decision fields.
- **FR-010**: The system MUST treat the stated monetary thresholds as inclusive for single approvals: 50,000.00 for Manager, Approver, and Admin, and 500,000.00 for Director.
- **FR-011**: All approval decisions MUST be based on exact monetary amounts without rounding the invoice amount across a threshold.
- **FR-012**: For an invoice above 500,000.00, any role currently authorized to approve invoices MUST be permitted to provide the first approval, regardless of its single-approval limit; a distinct Director MUST provide the second approval.

### Key Entities

- **Invoice**: An invoice awaiting approval, with an amount, lifecycle status, and approval/decision details.
- **Approver**: An authenticated user with a role that may authorize invoice approval. The user's identity distinguishes the first and second approvers.
- **Approval record**: The identity and time associated with an approval step; high-value invoices require two distinct records before approval is complete.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of single-approval decisions at 50,000.00 and 50,000.01 match the Manager-level threshold, including legacy Approver and Admin roles.
- **SC-002**: 100% of Director decisions at 500,000.00 and 500,000.01 respectively allow solo approval at the limit and require the second-approval path above it.
- **SC-003**: 100% of tested high-value invoices remain pending after the first approval and become approved only after a distinct Director completes the second approval.
- **SC-004**: 100% of rejected approval attempts return an understandable reason and preserve all invoice data.

## Assumptions

- The existing sign-in and role authorization determine which users may attempt an approval; this feature changes approval limits and does not add or replace authentication.
- Amounts are compared at their supplied precision, with two-decimal currency examples used to express the boundaries.
- The role limits describe when a user may approve an invoice alone. Any role currently authorized to approve may record the first step for an invoice above 500,000.00, even when that amount exceeds its single-approval limit.
- “Small invoices” for legacy Approver and Admin roles means invoices at or below 50,000.00, preserving the existing Manager-level behavior.
