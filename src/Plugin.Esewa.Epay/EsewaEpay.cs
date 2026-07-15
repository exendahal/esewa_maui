namespace Plugin.Esewa.Epay;

/// <summary>
/// Entry point for launching an eSewa ePay v2 payment. Resolve the shared
/// instance from <see cref="Current"/>.
/// </summary>
public static class EsewaEpay
{
    static readonly Lazy<IEsewaEpayPayment> Implementation =
        new(() => new EsewaEpayImplementation(), LazyThreadSafetyMode.PublicationOnly);

    /// <summary>True on supported platforms (Android and iOS).</summary>
    public static bool IsSupported => Implementation.Value is not null;

    /// <summary>The current payment service.</summary>
    public static IEsewaEpayPayment Current => Implementation.Value;
}
