# Approval API Contract

## Approve an invoice

`POST /api/approvals/{invoiceId}/approve`

The request requires a valid signed-in session bearer token and one of the
`Manager`, `Director`, `Approver`, or `Admin` roles. The request has no body. The API
uses the authenticated session username and role; clients cannot choose the acting
identity or role.

### Successful response

Returns `200 OK` with the invoice JSON. JSON field names follow the API's existing
camel-case serialization. `firstApprovedBy` and `firstApprovedAt` are included as
nullable fields.

- A first approval for an invoice above 500,000.00 returns `PendingApproval` and
  populates the first-approval fields.
- A completed approval returns `Approved` and populates `decidedBy` and `decidedAt`.

### Error responses

| Status | Meaning | Response |
|---|---|---|
| 401 | No valid session | Existing authentication response |
| 403 | Session role is not permitted to approve | Existing role authorization response |
| 404 | Invoice does not exist | JSON object with `error` |
| 409 | Invoice state, role limit, or two-approver rule rejects the attempt | JSON object with a human-readable `error` |

A 409 response must correspond to no invoice mutation and no repository update.
Existing invoice rejection endpoint behavior is unchanged by this contract.
