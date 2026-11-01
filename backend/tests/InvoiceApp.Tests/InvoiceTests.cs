using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class InvoiceTests
{
    [Fact]
    // Catches removing either default empty-string initializer.
    public void Constructor_DefaultStringProperties_AreEmpty()
    {
        var invoice = new Invoice();

        Assert.Equal(string.Empty, invoice.Id);
        Assert.Equal(string.Empty, invoice.CustomerName);
    }
}
