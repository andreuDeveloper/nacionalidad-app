using System.ComponentModel;
using Nacionalidad.App.ViewModels;

namespace Nacionalidad.App.Views;

public partial class QuizPage : ContentPage
{
    private readonly QuizViewModel _vm;
    private bool _started;

    public QuizPage(QuizViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _vm.PropertyChanged += OnViewModelChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_started) return;
        _started = true;
        _vm.Start();
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        if (!Navigation.NavigationStack.Contains(this)) _vm.Stop();
    }

    /// <summary>Botón atrás de Android: pide confirmación en lugar de salir directamente.</summary>
    protected override bool OnBackButtonPressed()
    {
        _vm.ExitCommand.Execute(null);
        return true;
    }

    private async void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Al cambiar de pregunta, volver arriba; al mostrar la corrección, bajar hasta ella.
        if (e.PropertyName == nameof(QuizViewModel.Index))
            await Scroller.ScrollToAsync(0, 0, false);
        else if (e.PropertyName == nameof(QuizViewModel.ShowFeedback) && _vm.ShowFeedback)
            await Scroller.ScrollToAsync(0, Scroller.ContentSize.Height, true);
    }
}
