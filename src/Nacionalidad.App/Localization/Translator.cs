using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace Nacionalidad.App.Localization;

/// <summary>
/// Acceso a los textos de la interfaz (.resx) con cambio de idioma en caliente.
/// En XAML: <c>Text="{loc:Tr Home_Greeting}"</c>. En C#: <c>Translator.Instance["Home_Greeting"]</c>.
/// </summary>
public sealed class Translator : INotifyPropertyChanged
{
    public static Translator Instance { get; } = new();

    private readonly ResourceManager _resources =
        new("Nacionalidad.App.Resources.Strings.AppResources", typeof(Translator).Assembly);

    private CultureInfo _culture = CultureInfo.CurrentUICulture;

    private Translator() { }

    public event PropertyChangedEventHandler? PropertyChanged;

    public CultureInfo Culture => _culture;

    public string this[string key] => _resources.GetString(key, _culture) ?? $"[{key}]";

    public string Format(string key, params object?[] args) => string.Format(_culture, this[key], args);

    public void SetCulture(CultureInfo culture)
    {
        _culture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        // "Item[]" refresca todos los bindings al indexador.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Culture)));
    }
}
