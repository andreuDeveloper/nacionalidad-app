namespace Nacionalidad.App.Services;

public enum PurchaseOutcome
{
    Purchased,
    AlreadyOwned,
    Cancelled,
    Pending,
    NotAvailable,
    Failed,
}

/// <summary>Compra única del "Pase DELE A2" con Google Play Billing.</summary>
public interface IBillingService
{
    /// <summary>Identificador del producto en Google Play Console (producto integrado, no consumible).</summary>
    const string DelePassProductId = "pase_dele_a2";

    /// <summary>Estado guardado localmente; se confirma con <see cref="RestoreAsync"/>.</summary>
    bool HasDelePass { get; }

    event EventHandler? EntitlementChanged;

    /// <summary>Precio localizado que devuelve Google Play (p. ej. "14,99 €"), o null si no se puede consultar.</summary>
    Task<string?> GetDelePassPriceAsync();

    Task<PurchaseOutcome> PurchaseDelePassAsync();

    /// <summary>Consulta a Google Play las compras del usuario. Devuelve true si tiene el pase.</summary>
    Task<bool> RestoreAsync();
}
