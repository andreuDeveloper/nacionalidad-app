using Nacionalidad.App.Views;

namespace Nacionalidad.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(Routes.Ccse, typeof(CcsePage));
        Routing.RegisterRoute(Routes.Quiz, typeof(QuizPage));
        Routing.RegisterRoute(Routes.Result, typeof(ResultPage));
        Routing.RegisterRoute(Routes.Dele, typeof(DelePage));
    }
}
