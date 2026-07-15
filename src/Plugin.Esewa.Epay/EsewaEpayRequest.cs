namespace Plugin.Esewa.Epay;

/// <summary>
/// Describes an eSewa payment via the ePay v2 hosted checkout.
/// The library computes <c>total_amount</c> = <see cref="Amount"/> +
/// <see cref="TaxAmount"/> + <see cref="ServiceCharge"/> +
/// <see cref="DeliveryCharge"/> and signs the request with your secret key.
/// </summary>
public sealed class EsewaEpayRequest
{
    /// <summary>Base amount (before tax / charges).</summary>
    public required decimal Amount { get; init; }

    /// <summary>Merchant product/service code issued by eSewa (e.g. "EPAYTEST" in sandbox).</summary>
    public required string ProductCode { get; init; }

    /// <summary>Merchant secret key used to sign the request (HMAC-SHA256).</summary>
    public required string SecretKey { get; init; }

    /// <summary>Tax amount. Default 0.</summary>
    public decimal TaxAmount { get; init; }

    /// <summary>Product service charge. Default 0.</summary>
    public decimal ServiceCharge { get; init; }

    /// <summary>Product delivery charge. Default 0.</summary>
    public decimal DeliveryCharge { get; init; }

    /// <summary>
    /// Unique transaction id. If null, a new one is generated. Keep it if you
    /// need to reconcile / re-check the status later.
    /// </summary>
    public string? TransactionUuid { get; init; }

    /// <summary>Environment to run against.</summary>
    public EsewaEpayEnvironment Environment { get; init; } = EsewaEpayEnvironment.Sandbox;

    /// <summary>
    /// URL eSewa redirects to on success. The library intercepts this
    /// navigation in the WebView (it does not need to be a real page), so the
    /// default is fine unless your flow needs a specific value.
    /// </summary>
    public string SuccessUrl { get; init; } = "https://esewa.com.np/epay/success";

    /// <summary>URL eSewa redirects to on failure (intercepted, like SuccessUrl).</summary>
    public string FailureUrl { get; init; } = "https://esewa.com.np/epay/failure";

    /// <summary>
    /// After the checkout redirect, confirm the outcome authoritatively via the
    /// eSewa transaction status API (recommended). When true, the returned
    /// status reflects the gateway's status check, not just the redirect.
    /// </summary>
    public bool VerifyStatus { get; init; } = true;
}
