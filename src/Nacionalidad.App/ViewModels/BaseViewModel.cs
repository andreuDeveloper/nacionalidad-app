using CommunityToolkit.Mvvm.ComponentModel;
using Nacionalidad.App.Localization;

namespace Nacionalidad.App.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    protected static Translator T => Translator.Instance;

    protected static Task AlertAsync(string title, string message) =>
        Shell.Current.DisplayAlertAsync(title, message, T["Common_Ok"]);

    protected static Task<bool> ConfirmAsync(string title, string message) =>
        Shell.Current.DisplayAlertAsync(title, message, T["Common_Yes"], T["Common_No"]);
}
