using Nacionalidad.App.ViewModels;

namespace Nacionalidad.App.Views;

public partial class CcsePage : ContentPage
{
    private readonly CcseViewModel _vm;

    public CcsePage(CcseViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.RefreshAsync();
    }
}
