namespace Plugin.Esewa;

/// <summary>
/// Describes a single payment to initiate through the eSewa SDK.
/// The same request shape is used on both Android and iOS.
/// </summary>
public sealed class EsewaPaymentRequest
{
    /// <summary>Merchant client id issued by eSewa.</summary>
    public required string ClientId { get; init; }

    /// <summary>Merchant secret key issued by eSewa.</summary>
    public required string SecretKey { get; init; }

    /// <summary>Amount to charge, as a string (e.g. "100").</summary>
    public required string Amount { get; init; }

    /// <summary>Human readable product / service name shown to the payer.</summary>
    public required string ProductName { get; init; }

    /// <summary>Merchant-side unique id for this product / transaction.</summary>
    public required string ProductId { get; init; }

    /// <summary>Callback url registered with eSewa for this merchant.</summary>
    public required string CallbackUrl { get; init; }

    /// <summary>Environment to run the payment against.</summary>
    public EsewaEnvironment Environment { get; init; } = EsewaEnvironment.Test;

    /// <summary>Optional extra properties forwarded to the SDK.</summary>
    public IDictionary<string, string>? Properties { get; init; }
}
