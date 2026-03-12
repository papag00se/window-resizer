using WindowResizer.Core;

namespace WindowResizer.App.IntegrationTests;

public class Form1IntegrationTests
{
    [Fact]
    public void MainFormUsesSharedApplicationName()
    {
        string? formTitle = null;

        var thread = new Thread(() =>
        {
            using var form = new WindowResizer.App.Form1();
            formTitle = form.Text;
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        Assert.Equal(ProductDefaults.ApplicationName, formTitle);
    }
}
