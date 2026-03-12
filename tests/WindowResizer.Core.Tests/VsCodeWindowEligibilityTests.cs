using WindowResizer.Core.Windows;

namespace WindowResizer.Core.Tests;

public class VsCodeWindowEligibilityTests
{
    [Fact]
    public void IsEligibleAcceptsVisibleTopLevelVsCodeWindow()
    {
        var window = CreateWindow(processName: "Code");

        Assert.True(VsCodeWindowEligibility.IsEligible(window));
    }

    [Fact]
    public void IsEligibleRejectsHelperWindowWithOwner()
    {
        var window = CreateWindow(processName: "Code", hasOwner: true);

        Assert.False(VsCodeWindowEligibility.IsEligible(window));
    }

    [Fact]
    public void IsEligibleRejectsMinimizedOrCloakedWindows()
    {
        var minimizedWindow = CreateWindow(processName: "Code", isMinimized: true);
        var cloakedWindow = CreateWindow(processName: "Code", isCloaked: true);

        Assert.False(VsCodeWindowEligibility.IsEligible(minimizedWindow));
        Assert.False(VsCodeWindowEligibility.IsEligible(cloakedWindow));
    }

    [Fact]
    public void IsEligibleRejectsNonVsCodeProcess()
    {
        var window = CreateWindow(processName: "notepad");

        Assert.False(VsCodeWindowEligibility.IsEligible(window));
    }

    private static TopLevelWindowInfo CreateWindow(
        string processName,
        bool isVisible = true,
        bool isMinimized = false,
        bool isCloaked = false,
        bool hasOwner = false,
        bool isToolWindow = false)
    {
        return new TopLevelWindowInfo(
            Handle: 1,
            Title: "Test Window",
            ClassName: "Chrome_WidgetWin_1",
            ProcessName: processName,
            IsVisible: isVisible,
            IsMinimized: isMinimized,
            IsCloaked: isCloaked,
            HasOwner: hasOwner,
            IsToolWindow: isToolWindow);
    }
}
