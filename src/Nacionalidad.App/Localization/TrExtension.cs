namespace Nacionalidad.App.Localization;

/// <summary>Markup extension: <c>{loc:Tr Clave}</c>. Se actualiza al cambiar de idioma.</summary>
[ContentProperty(nameof(Key))]
[AcceptEmptyServiceProvider]
public sealed class TrExtension : IMarkupExtension<BindingBase>
{
    public string Key { get; set; } = string.Empty;

    /// <summary>Formato opcional, p. ej. "{0}:" .</summary>
    public string? StringFormat { get; set; }

    public BindingBase ProvideValue(IServiceProvider serviceProvider) =>
        new Binding($"[{Key}]", BindingMode.OneWay, source: Translator.Instance, stringFormat: StringFormat);

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
