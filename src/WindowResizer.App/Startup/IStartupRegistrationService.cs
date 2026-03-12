namespace WindowResizer.App.Startup;

public interface IStartupRegistrationService
{
    void Enable(string executablePath);

    void Disable();
}
