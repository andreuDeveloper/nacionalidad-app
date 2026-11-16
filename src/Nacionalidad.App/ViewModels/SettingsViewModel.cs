using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nacionalidad.App.Services;

namespace Nacionalidad.App.ViewModels;

public sealed partial class LanguageItem : ObservableObject
{
    public required AppLanguage Language { get; init; }
    public string Name => Language.NativeName;
    public FlowDirection Direction => Language.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

public sealed partial class SettingsViewModel : BaseViewModel
{
    private readonly LanguageService _language;
    private readonly IBillingService _billing;
    private readonly ContentService _content;

    public SettingsViewModel(LanguageService language, IBillingService billing, ContentService content, IAppInfo appInfo)
    {
        _language = language;
        _billing = billing;
        _content = content;
        Languages = LanguageService.Supported
            .Select(l => new LanguageItem { Language = l, IsSelected = l.Code == language.Code })
            .ToList();
        VersionText = T.Format("Settings_Version", appInfo.VersionString);
    }

    public IReadOnlyList<LanguageItem> Languages { get; }

    [ObservableProperty] public partial string VersionText { get; set; }
    [ObservableProperty] public partial string ContentVersionText { get; set; } = string.Empty;
    [ObservableProperty] public partial bool HasDelePass { get; set; }

    [RelayCommand]
    private async Task SelectLanguage(LanguageItem item)
    {
        if (item.Language.Code == _language.Code) return;
        _language.SetLanguage(item.Language.Code);
        foreach (var l in Languages) l.IsSelected = l.Language.Code == item.Language.Code;
        // Los textos fijos se actualizan solos; los calculados (versión, etc.) se recalculan aquí.
        VersionText = T.Format("Settings_Version", AppInfo.Current.VersionString);
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        HasDelePass = _billing.HasDelePass;
        try
        {
            var bank = await _content.GetCcseAsync();
            ContentVersionText = T.Format("Settings_ContentVersion", bank.ContentVersion);
        }
        catch (Exception)
        {
            ContentVersionText = string.Empty;
        }
    }

    [RelayCommand]
    private async Task Restore()
    {
        IsBusy = true;
        try
        {
            HasDelePass = await _billing.RestoreAsync();
            await AlertAsync(T["Settings_Purchases"], HasDelePass ? T["Settings_RestoreOk"] : T["Settings_RestoreNone"]);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
