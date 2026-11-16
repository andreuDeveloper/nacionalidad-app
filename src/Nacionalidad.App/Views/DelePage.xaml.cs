using Nacionalidad.App.ViewModels;

namespace Nacionalidad.App.Views;

public partial class DelePage : ContentPage
{
    private readonly DeleViewModel _vm;

    public DelePage(DeleViewModel vm)
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
