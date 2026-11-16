using Nacionalidad.App.ViewModels;

namespace Nacionalidad.App.Views;

public partial class ResultPage : ContentPage
{
    private readonly ResultViewModel _vm;

    public ResultPage(ResultViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _vm.Load();
    }

    protected override bool OnBackButtonPressed()
    {
        _vm.CloseCommand.Execute(null);
        return true;
    }
}
