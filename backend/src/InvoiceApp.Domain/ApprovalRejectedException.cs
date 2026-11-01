namespace InvoiceApp.Domain;

public class ApprovalRejectedException : InvalidOperationException
{
    public ApprovalRejectedException(string message)
        : base(message)
    {
    }
}
