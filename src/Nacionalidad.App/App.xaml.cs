using Nacionalidad.App.Services;

namespace Nacionalidad.App;

public partial class App : Application
{
    private readonly AppShell _shell;
    private readonly LanguageService _language;

    public App(AppShell shell, LanguageService language)
    {
        InitializeComponent();
        _shell = shell;
        _language = language;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        _language.Initialize();
        _shell.FlowDirection = _language.FlowDirection;
        return new Window(_shell);
    }
}
