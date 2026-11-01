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
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        var result = service.Submit("INV-1");

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    // Catches allowing an invoice in a non-draft state to be submitted again.
    public void Submit_NonDraft_Throws()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Approved));

        var exception = Assert.Throws<InvalidOperationException>(() => service.Submit("INV-1"));

        Assert.Equal("Invoice INV-1 is Approved; only drafts can be submitted.", exception.Message);
    }

    [Fact]
    // Catches breaking approval of a small invoice by an existing Approver.
    public void Approve_SmallInvoiceByApprover_ApprovesAndRecordsDecision()
    {
        var repository = new FakeInvoiceRepository(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));
        var service = new ApprovalService(repository);

        var result = service.Approve("INV-1", "demo.approver", ApproverRole.Approver);

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.approver", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    // Catches approving an invoice that is not pending.
    public void Approve_Draft_Throws()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Draft));

        var exception = Assert.Throws<InvalidOperationException>(
            () => service.Approve("INV-1", "demo.approver", ApproverRole.Approver));

        Assert.Equal("Invoice INV-1 is Draft; only pending invoices can be decided.", exception.Message);
    }

    [Fact]
    // Catches treating a missing invoice as an approvable invoice.
    public void Approve_MissingInvoice_Throws()
    {
        var service = ServiceFor();

        var exception = Assert.Throws<KeyNotFoundException>(
            () => service.Approve("INV-404", "demo.approver", ApproverRole.Approver));

        Assert.Equal("Invoice INV-404 was not found.", exception.Message);
    }

    [Theory]
    [InlineData("Manager", "50000.00")]
    [InlineData("Approver", "50000.00")]
    [InlineData("Admin", "50000.00")]
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

    [Fact]
    // Catches denying a Director's one-step approval at the inclusive 500,000.00 boundary.
    public void Approve_DirectorAt500000_ApprovesInOneStep()
    {
        var invoice = InvoiceWith("INV-DIRECTOR-LIMIT", InvoiceStatus.PendingApproval, 500000.00m);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        var result = service.Approve("INV-DIRECTOR-LIMIT", "director.one", ApproverRole.Director);

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("director.one", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
        Assert.Null(result.FirstApprovedBy);
        Assert.Null(result.FirstApprovedAt);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Theory]
    [InlineData("Manager", "50000.00", "50000.01")]
    [InlineData("Approver", "50000.00", "50000.01")]
    [InlineData("Admin", "50000.00", "50000.01")]
    // Catches permitting an amount one cent above a Manager-level limit or mutating/persisting its rejection.
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
    // This replaces the Phase A rejection: Phase B requires a first approval above 500,000.00.
    public void Approve_DirectorJustAbove500000_RecordsFirstApprovalAndRemainsPending()
    {
        var invoice = InvoiceWith("INV-DIRECTOR-FIRST", InvoiceStatus.PendingApproval, 500000.01m);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        var result = service.Approve("INV-DIRECTOR-FIRST", "first.director", ApproverRole.Director);

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
        Assert.Equal("first.director", result.FirstApprovedBy);
        Assert.NotNull(result.FirstApprovedAt);
        Assert.Null(result.DecidedBy);
        Assert.Null(result.DecidedAt);
        Assert.Null(result.DecisionNote);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    // Catches incorrectly applying the Manager limit to the first approver of a high-value invoice.
    public void Approve_ManagerAsFirstApproverAbove500000_RecordsFirstApprovalAndRemainsPending()
    {
        var invoice = InvoiceWith("INV-MANAGER-FIRST", InvoiceStatus.PendingApproval, 600000m);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        var result = service.Approve("INV-MANAGER-FIRST", "first.manager", ApproverRole.Manager);

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
        Assert.Equal("first.manager", result.FirstApprovedBy);
        Assert.NotNull(result.FirstApprovedAt);
        Assert.Null(result.DecidedBy);
        Assert.Null(result.DecidedAt);
        Assert.Null(result.DecisionNote);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    // Catches failing to complete a high-value invoice with a distinct Director as the second approver.
    public void Approve_DifferentDirectorSecond_ApprovesInvoice()
    {
        var invoice = InvoiceWith("INV-DIRECTOR-SECOND", InvoiceStatus.PendingApproval, 600000m);
        invoice.FirstApprovedBy = "first.manager";
        invoice.FirstApprovedAt = new DateTime(2026, 1, 1, 3, 4, 5, DateTimeKind.Utc);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        var firstApprovedAt = invoice.FirstApprovedAt;

        var result = service.Approve("INV-DIRECTOR-SECOND", "second.director", ApproverRole.Director);

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("second.director", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
        Assert.Equal("first.manager", result.FirstApprovedBy);
        Assert.Equal(firstApprovedAt, result.FirstApprovedAt);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    // Catches allowing the first approver to approve the same high-value invoice again.
    public void Approve_SameUserTwice_IsRejectedWithoutChangingInvoice()
    {
        var invoice = InvoiceWith("INV-SAME-USER", InvoiceStatus.PendingApproval, 600000m);
        invoice.FirstApprovedBy = "same.user";
        invoice.FirstApprovedAt = new DateTime(2026, 1, 1, 3, 4, 5, DateTimeKind.Utc);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        AssertRejectedWithoutChanges(
            invoice,
            repository,
            () => service.Approve("INV-SAME-USER", "same.user", ApproverRole.Director),
            message => Assert.Contains("same", message, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    // Catches comparing usernames without both trimming whitespace and ignoring letter case.
    public void Approve_SameUserWithDifferentCaseAndWhitespace_IsRejectedWithoutChangingInvoice()
    {
        var invoice = InvoiceWith("INV-SAME-USER-NORMALIZED", InvoiceStatus.PendingApproval, 600000m);
        invoice.FirstApprovedBy = "  Same.User  ";
        invoice.FirstApprovedAt = new DateTime(2026, 1, 1, 3, 4, 5, DateTimeKind.Utc);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        AssertRejectedWithoutChanges(
            invoice,
            repository,
            () => service.Approve("INV-SAME-USER-NORMALIZED", "same.user ", ApproverRole.Director),
            message => Assert.Contains("same", message, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    // Catches allowing a non-Director to provide the required second approval.
    public void Approve_ManagerAsSecondApprover_IsRejectedWithoutChangingInvoice()
    {
        var invoice = InvoiceWith("INV-MANAGER-SECOND", InvoiceStatus.PendingApproval, 600000m);
        invoice.FirstApprovedBy = "first.approver";
        invoice.FirstApprovedAt = new DateTime(2026, 1, 1, 3, 4, 5, DateTimeKind.Utc);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        AssertRejectedWithoutChanges(
            invoice,
            repository,
            () => service.Approve("INV-MANAGER-SECOND", "second.manager", ApproverRole.Manager),
            message => Assert.Contains("Director", message, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    // Catches losing the reason or approver when recording a rejection.
    public void Reject_PendingInvoice_RecordsReasonAndApprover()
    {
        var repository = new FakeInvoiceRepository(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));
        var service = new ApprovalService(repository);

        var result = service.Reject("INV-1", "demo.approver", "Duplicate invoice");

        Assert.Equal(InvoiceStatus.Rejected, result.Status);
        Assert.Equal("Duplicate invoice", result.DecisionNote);
        Assert.Equal("demo.approver", result.DecidedBy);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    // Catches accepting a rejection without a meaningful reason.
    public void Reject_EmptyReason_Throws()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        var exception = Assert.Throws<ArgumentException>(
            () => service.Reject("INV-1", "demo.approver", "  "));

        Assert.Equal("A reason is required to reject an invoice. (Parameter 'reason')", exception.Message);
        Assert.Equal("reason", exception.ParamName);
    }

    [Fact]
    // Catches removing the pending-state guard from rejection and persisting a non-pending decision.
    public void Reject_NonPendingInvoice_ThrowsWithoutChangingInvoice()
    {
        var invoice = InvoiceWith("INV-NON-PENDING", InvoiceStatus.Draft);
        invoice.DecidedBy = "previous.decider";
        invoice.DecidedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        invoice.DecisionNote = "previous decision note";
        invoice.FirstApprovedBy = "first.approver";
        invoice.FirstApprovedAt = new DateTime(2026, 1, 1, 3, 4, 5, DateTimeKind.Utc);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        var status = invoice.Status;
        var decidedBy = invoice.DecidedBy;
        var decidedAt = invoice.DecidedAt;
        var decisionNote = invoice.DecisionNote;
        var firstApprovedBy = invoice.FirstApprovedBy;
        var firstApprovedAt = invoice.FirstApprovedAt;

        var exception = Assert.Throws<InvalidOperationException>(
            () => service.Reject("INV-NON-PENDING", "demo.approver", "Duplicate invoice"));

        Assert.Equal(
            "Invoice INV-NON-PENDING is Draft; only pending invoices can be decided.",
            exception.Message);
        Assert.Equal(status, invoice.Status);
        Assert.Equal(decidedBy, invoice.DecidedBy);
        Assert.Equal(decidedAt, invoice.DecidedAt);
        Assert.Equal(decisionNote, invoice.DecisionNote);
        Assert.Equal(firstApprovedBy, invoice.FirstApprovedBy);
        Assert.Equal(firstApprovedAt, invoice.FirstApprovedAt);
        Assert.Equal(0, repository.UpdateCount);
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
        var updateCount = repository.UpdateCount;

        var exception = Assert.Throws<ApprovalRejectedException>(attempt);

        assertMessage(exception.Message);
        Assert.Equal(status, invoice.Status);
        Assert.Equal(decidedBy, invoice.DecidedBy);
        Assert.Equal(decidedAt, invoice.DecidedAt);
        Assert.Equal(decisionNote, invoice.DecisionNote);
        Assert.Equal(firstApprovedBy, invoice.FirstApprovedBy);
        Assert.Equal(firstApprovedAt, invoice.FirstApprovedAt);
        Assert.Equal(updateCount, repository.UpdateCount);
    }
}
