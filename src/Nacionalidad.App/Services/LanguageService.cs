using System.Globalization;
using Nacionalidad.App.Localization;

namespace Nacionalidad.App.Services;

public sealed record AppLanguage(string Code, string NativeName, bool IsRightToLeft);

/// <summary>Idioma de la interfaz y de las explicaciones. Se guarda en Preferences.</summary>
public sealed class LanguageService
{
    private const string PreferenceKey = "app.language";

    public static readonly IReadOnlyList<AppLanguage> Supported =
    [
        new("es", "Español", false),
        new("ar", "العربية", true),
        new("en", "English", false),
        new("fr", "Français", false),
        new("ur", "اردو", true),
    ];

    private readonly IPreferences _preferences;

    public LanguageService(IPreferences preferences)
    {
        _preferences = preferences;
        Current = Resolve(_preferences.Get<string?>(PreferenceKey, null) ?? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
    }

    public event EventHandler? LanguageChanged;

    public AppLanguage Current { get; private set; }

    public string Code => Current.Code;

    public FlowDirection FlowDirection => Current.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    /// <summary>Aplica el idioma guardado (llamar al arrancar).</summary>
    public void Initialize() => Apply();

    public void SetLanguage(string code)
    {
        Current = Resolve(code);
        _preferences.Set(PreferenceKey, Current.Code);
        Apply();
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Dirección de un texto concreto según el idioma en el que está escrito.</summary>
    public static FlowDirection DirectionFor(string languageCode) =>
        Supported.FirstOrDefault(l => l.Code == languageCode)?.IsRightToLeft == true
            ? FlowDirection.RightToLeft
            : FlowDirection.LeftToRight;

    private void Apply()
    {
        Translator.Instance.SetCulture(new CultureInfo(Current.Code));
        if (Application.Current?.Windows is { Count: > 0 } windows && windows[0].Page is { } page)
            page.FlowDirection = FlowDirection;
    }

    private static AppLanguage Resolve(string? code) =>
        Supported.FirstOrDefault(l => string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase)) ?? Supported[0];
}
