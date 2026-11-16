using System.Globalization;
using Nacionalidad.App.ViewModels;

namespace Nacionalidad.App.Converters;

/// <summary>Colores de una opción según su estado (normal, elegida, correcta, incorrecta) y el tema.</summary>
public sealed class OptionStateToColorConverter : IValueConverter
{
    /// <summary>"Background", "Stroke" o "Badge".</summary>
    public string Part { get; set; } = "Background";

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var state = value is OptionState s ? s : OptionState.Normal;
        var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
        string Res(string light) => dark ? light + "Dark" : light;

        string key = (Part, state) switch
        {
            ("Background", OptionState.Correct) => Res("SuccessBg"),
            ("Background", OptionState.Wrong) => Res("ErrorBg"),
            ("Background", _) => Res("CardBg"),
            ("Stroke", OptionState.Correct) => Res("Success"),
            ("Stroke", OptionState.Wrong) => Res("Error"),
            ("Stroke", OptionState.Selected) => Res("Primary"),
            ("Stroke", _) => Res("CardStroke"),
            ("Badge", OptionState.Correct) => Res("Success"),
            ("Badge", OptionState.Wrong) => Res("Error"),
            ("Badge", OptionState.Selected) => Res("Primary"),
            _ => "Locked",
        };
        return Application.Current?.Resources.TryGetValue(key, out var color) == true ? color : Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>true → color de éxito, false → color de error.</summary>
public sealed class BoolToResultColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var key = value is true ? "Success" : "Error";
        if (dark) key += "Dark";
        return Application.Current?.Resources.TryGetValue(key, out var color) == true ? color : Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>true → fondo de éxito, false → fondo de error.</summary>
public sealed class BoolToResultBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var key = value is true ? "SuccessBg" : "ErrorBg";
        if (dark) key += "Dark";
        return Application.Current?.Resources.TryGetValue(key, out var color) == true ? color : Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
