using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nacionalidad.App.Services;

namespace Nacionalidad.App.ViewModels;

/// <summary>
/// Pantalla del DELE A2. Mientras no haya contenido (fases 3-5) solo informa; la compra está preparada
/// en <see cref="IBillingService"/> pero no se ofrece hasta que <see cref="FeatureFlags.DeleAvailable"/> sea true.
/// </summary>
public sealed partial class DeleViewModel(IBillingService billing) : BaseViewModel
{
    public IReadOnlyList<string> Features { get; } =
    [
        T["Dele_FeatureReading"],
        T["Dele_FeatureListening"],
        T["Dele_FeatureWriting"],
        T["Dele_FeatureSpeaking"],
        T["Dele_FeatureExams"],
        T["Dele_FeaturePlan"],
    ];

    public bool IsAvailable => FeatureFlags.DeleAvailable;

    [ObservableProperty] public partial string Price { get; set; } = BillingService.FallbackPrice;

    public async Task RefreshAsync()
    {
        if (!IsAvailable) return;
        Price = await billing.GetDelePassPriceAsync() ?? BillingService.FallbackPrice;
    }

    [RelayCommand]
    private Task GoCcse() => Shell.Current.GoToAsync($"../{Routes.Ccse}");
}
