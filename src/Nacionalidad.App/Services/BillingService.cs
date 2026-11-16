using Plugin.InAppBilling;

namespace Nacionalidad.App.Services;

/// <summary>
/// Implementación con Plugin.InAppBilling. El derecho de uso se cachea en Preferences para funcionar sin conexión,
/// y se revalida con Google Play al restaurar compras.
/// </summary>
public sealed class BillingService(IPreferences preferences) : IBillingService
{
    private const string EntitlementKey = "billing.dele_pass";
    public const string FallbackPrice = "14,99 €";

    public bool HasDelePass => preferences.Get(EntitlementKey, false);

    public event EventHandler? EntitlementChanged;

    public async Task<string?> GetDelePassPriceAsync()
    {
        var billing = CrossInAppBilling.Current;
        try
        {
            if (!await billing.ConnectAsync()) return null;
            var items = await billing.GetProductInfoAsync(ItemType.InAppPurchase, [IBillingService.DelePassProductId]);
            return items?.FirstOrDefault()?.LocalizedPrice;
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            await billing.DisconnectAsync();
        }
    }

    public async Task<PurchaseOutcome> PurchaseDelePassAsync()
    {
        var billing = CrossInAppBilling.Current;
        try
        {
            if (!await billing.ConnectAsync()) return PurchaseOutcome.NotAvailable;

            var purchase = await billing.PurchaseAsync(IBillingService.DelePassProductId, ItemType.InAppPurchase);
            if (purchase is null) return PurchaseOutcome.Cancelled;

            if (purchase.State is PurchaseState.PaymentPending or PurchaseState.Deferred) return PurchaseOutcome.Pending;
            if (purchase.State != PurchaseState.Purchased) return PurchaseOutcome.Failed;

            // Google cancela la compra a los 3 días si no se reconoce (acknowledge).
            if (purchase.IsAcknowledged != true)
                await billing.FinalizePurchaseAsync([purchase.TransactionIdentifier]);

            SetEntitlement(true);
            return PurchaseOutcome.Purchased;
        }
        catch (InAppBillingPurchaseException ex) when (ex.PurchaseError == PurchaseError.AlreadyOwned)
        {
            SetEntitlement(true);
            return PurchaseOutcome.AlreadyOwned;
        }
        catch (InAppBillingPurchaseException ex) when (ex.PurchaseError == PurchaseError.UserCancelled)
        {
            return PurchaseOutcome.Cancelled;
        }
        catch (Exception)
        {
            return PurchaseOutcome.Failed;
        }
        finally
        {
            await billing.DisconnectAsync();
        }
    }

    public async Task<bool> RestoreAsync()
    {
        var billing = CrossInAppBilling.Current;
        try
        {
            if (!await billing.ConnectAsync()) return HasDelePass;
            var purchases = await billing.GetPurchasesAsync(ItemType.InAppPurchase);
            var owned = purchases?.Any(p => p.ProductId == IBillingService.DelePassProductId && p.State == PurchaseState.Purchased) == true;
            SetEntitlement(owned);
            return owned;
        }
        catch (Exception)
        {
            return HasDelePass;
        }
        finally
        {
            await billing.DisconnectAsync();
        }
    }

    private void SetEntitlement(bool value)
    {
        if (HasDelePass == value) return;
        preferences.Set(EntitlementKey, value);
        EntitlementChanged?.Invoke(this, EventArgs.Empty);
    }
}
