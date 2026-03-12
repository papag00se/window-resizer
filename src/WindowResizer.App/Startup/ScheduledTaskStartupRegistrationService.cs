using System.Diagnostics;
using WindowResizer.Core;

namespace WindowResizer.App.Startup;

public sealed class ScheduledTaskStartupRegistrationService : IStartupRegistrationService
{
    private readonly string _taskName;

    public ScheduledTaskStartupRegistrationService(string? taskName = null)
    {
        _taskName = string.IsNullOrWhiteSpace(taskName) ? ProductDefaults.ApplicationName : taskName;
    }

    public void Enable(string executablePath)
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"{_taskName}.startup.xml");

        try
        {
            File.WriteAllText(tempFilePath, StartupTaskXmlBuilder.Build(executablePath));
            RunSchtasks($"/Create /TN \"{_taskName}\" /XML \"{tempFilePath}\" /F");
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    public void Disable()
    {
        RunSchtasks($"/Delete /TN \"{_taskName}\" /F");
    }

    private static void RunSchtasks(string arguments)
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            Arguments = arguments,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        });

        process!.WaitForExit();
        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"schtasks.exe failed with exit code {process.ExitCode}: {error}");
        }
    }
}
