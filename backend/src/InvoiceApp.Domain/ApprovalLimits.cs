namespace InvoiceApp.Domain;

public static class ApprovalLimits
{
    public const decimal ManagerLimit = 50000.00m;
    public const decimal DirectorLimit = 500000.00m;

    public static decimal GetLimit(ApproverRole role) =>
        role switch
        {
            ApproverRole.Manager or ApproverRole.Approver or ApproverRole.Admin => ManagerLimit,
            ApproverRole.Director => DirectorLimit,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown approver role.")
        };
}
