---
name: dotnet-unit-tests
description: How to write xUnit tests in backend/tests/InvoiceApp.Tests for this repo, including the invoice-unchanged assertion. Use when adding or reviewing C# unit tests.
---
# Writing unit tests here

- One class per class under test: ApprovalServiceTests.
- Name: Method_Condition_ExpectedResult.
- Arrange with FakeInvoiceRepository; it has an UpdateCount property. Assert it.
- Use [Theory] with [InlineData] for boundary values; pass amounts as strings and decimal.Parse them, because attributes cannot hold decimals.
- Every negative test:
  1. snapshot Status, DecidedBy, DecidedAt, FirstApprovedBy, FirstApprovedAt, DecisionNote before the call;
  2. Assert.Throws<ApprovalRejectedException>(...) and assert the message names the rule;
  3. assert every snapshotted field is unchanged and UpdateCount == 0.
- Before finishing, ask: "If I deleted the line of production code this test is about, would this test fail?" If not, the test is worthless; fix it.
- Run: dotnet test backend/InvoiceApp.sln