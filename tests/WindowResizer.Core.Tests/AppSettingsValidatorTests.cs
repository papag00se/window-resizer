using WindowResizer.Core.Settings;

namespace WindowResizer.Core.Tests;

public class AppSettingsValidatorTests
{
    [Fact]
    public void ValidateAllowsPositiveWidth()
    {
        var settings = new AppSettings(1823, RunAtSignIn: true);

        AppSettingsValidator.Validate(settings);
    }

    [Fact]
    public void ValidateRejectsZeroWidth()
    {
        var settings = new AppSettings(0, RunAtSignIn: true);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => AppSettingsValidator.Validate(settings));

        Assert.Equal("WindowWidthPx", exception.ParamName);
    }
}
