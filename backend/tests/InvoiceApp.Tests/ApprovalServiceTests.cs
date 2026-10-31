using System.Globalization;
using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalServiceTests
{
    private static Invoice InvoiceWith(
        string id,
        InvoiceStatus status,
        decimal amount = 100m) =>
        new() { Id = id, CustomerName = "Test School", Amount = amount, Status = status };

    private static ApprovalService ServiceFor(params Invoice[] invoices) =>
        new(new FakeInvoiceRepository(invoices));

    [Fact]
    // Catches moving a draft to any state other than pending approval.
    public void Submit_Draft_MovesToPendingApproval()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.Draft);

        var result = ServiceFor(invoice).Submit("INV-1");

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
    }

    [Fact]
    // Catches allowing an invoice in a non-draft state to be submitted again.
    public void Submit_NonDraft_Throws()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Approved));

        Assert.Throws<InvalidOperationException>(() => service.Submit("INV-1"));
    }

    [Fact]
    // Catches breaking approval of a small invoice by an existing Approver.
    public void Approve_SmallInvoiceByApprover_ApprovesAndRecordsDecision()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        var result = service.Approve("INV-1", "demo.approver", ApproverRole.Approver);

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.approver", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
    }

    [Fact]
    // Catches approving an invoice that is not pending.
    public void Approve_Draft_Throws()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Draft));

        Assert.Throws<InvalidOperationException>(
            () => service.Approve("INV-1", "demo.approver", ApproverRole.Approver));
    }

    [Fact]
    // Catches treating a missing invoice as an approvable invoice.
    public void Approve_MissingInvoice_Throws()
    {
        var service = ServiceFor();

        Assert.Throws<KeyNotFoundException>(
            () => service.Approve("INV-404", "demo.approver", ApproverRole.Approver));
    }

    [Theory]
    [InlineData("Manager", "50000.00")]
    [InlineData("Approver", "50000.00")]
    [InlineData("Admin", "50000.00")]
    [InlineData("Director", "500000.00")]
    // Catches treating any inclusive role limit as an exclusive threshold.
    public void Approve_AmountAtRoleLimit_Approves(string roleName, string amountText)
    {
        var role = Enum.Parse<ApproverRole>(roleName);
        var amount = decimal.Parse(amountText, CultureInfo.InvariantCulture);
        var service = ServiceFor(InvoiceWith("INV-LIMIT", InvoiceStatus.PendingApproval, amount));

        var result = service.Approve("INV-LIMIT", "demo.approver", role);

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.approver", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
    }

    [Theory]
    [InlineData("Manager", "50000.00", "50000.01")]
    [InlineData("Approver", "50000.00", "50000.01")]
    [InlineData("Admin", "50000.00", "50000.01")]
    [InlineData("Director", "500000.00", "500000.01")]
    // Catches permitting an amount one cent above a role's limit or mutating/persisting its rejection.
    public void Approve_AmountAboveRoleLimit_RejectsWithoutChangingInvoice(
        string roleName,
        string limitText,
        string amountText)
    {
        var role = Enum.Parse<ApproverRole>(roleName);
        var limit = decimal.Parse(limitText, CultureInfo.InvariantCulture);
        var amount = decimal.Parse(amountText, CultureInfo.InvariantCulture);
        var invoice = InvoiceWith("INV-OVER-LIMIT", InvoiceStatus.PendingApproval, amount);
        invoice.DecidedBy = "previous.decider";
        invoice.DecidedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        invoice.DecisionNote = "previous decision note";
        invoice.FirstApprovedBy = "first.approver";
        invoice.FirstApprovedAt = new DateTime(2026, 1, 1, 3, 4, 5, DateTimeKind.Utc);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        AssertLimitRejectedWithoutChanges(
            invoice,
            repository,
            () => service.Approve("INV-OVER-LIMIT", "demo.approver", role),
            role,
            limit,
            amount);
    }

    [Fact]
    // Catches losing the reason or approver when recording a rejection.
    public void Reject_PendingInvoice_RecordsReasonAndApprover()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        var result = service.Reject("INV-1", "demo.approver", "Duplicate invoice");

        Assert.Equal(InvoiceStatus.Rejected, result.Status);
        Assert.Equal("Duplicate invoice", result.DecisionNote);
    }

    [Fact]
    // Catches accepting a rejection without a meaningful reason.
    public void Reject_EmptyReason_Throws()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        Assert.Throws<ArgumentException>(() => service.Reject("INV-1", "demo.approver", "  "));
    }

    private static void AssertLimitRejectedWithoutChanges(
        Invoice invoice,
        FakeInvoiceRepository repository,
        Action attempt,
        ApproverRole role,
        decimal limit,
        decimal amount)
    {
        AssertRejectedWithoutChanges(
            invoice,
            repository,
            attempt,
            message =>
            {
                Assert.Contains(role.ToString(), message);
                Assert.Contains(limit.ToString("N2", CultureInfo.InvariantCulture), message);
                Assert.Contains(amount.ToString("N2", CultureInfo.InvariantCulture), message);
            });
    }

    private static void AssertRejectedWithoutChanges(
        Invoice invoice,
        FakeInvoiceRepository repository,
        Action attempt,
        Action<string> assertMessage)
    {
        var status = invoice.Status;
        var decidedBy = invoice.DecidedBy;
        var decidedAt = invoice.DecidedAt;
        var decisionNote = invoice.DecisionNote;
        var firstApprovedBy = invoice.FirstApprovedBy;
        var firstApprovedAt = invoice.FirstApprovedAt;

        var exception = Assert.Throws<ApprovalRejectedException>(attempt);

        assertMessage(exception.Message);
        Assert.Equal(status, invoice.Status);
        Assert.Equal(decidedBy, invoice.DecidedBy);
        Assert.Equal(decidedAt, invoice.DecidedAt);
        Assert.Equal(decisionNote, invoice.DecisionNote);
        Assert.Equal(firstApprovedBy, invoice.FirstApprovedBy);
        Assert.Equal(firstApprovedAt, invoice.FirstApprovedAt);
        Assert.Equal(0, repository.UpdateCount);
    }
}
