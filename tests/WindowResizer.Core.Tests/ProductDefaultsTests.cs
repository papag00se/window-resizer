using WindowResizer.Core;

namespace WindowResizer.Core.Tests;

public class ProductDefaultsTests
{
    [Fact]
    public void DefaultWindowWidthMatchesDocumentedSeedWidth()
    {
        Assert.Equal(1823, ProductDefaults.DefaultWindowWidthPx);
    }

    [Fact]
    public void ApplicationNameMatchesProjectIdentity()
    {
        Assert.Equal("WindowResizer", ProductDefaults.ApplicationName);
    }
}
