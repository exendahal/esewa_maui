namespace Plugin.Esewa;

/// <summary>
/// Entry point for launching an eSewa payment. Resolve the shared instance from
/// <see cref="Current"/>.
/// </summary>
public static class EsewaPayment
{
    static readonly Lazy<IEsewaPayment> Implementation =
        new(CreatePlatformImplementation, LazyThreadSafetyMode.PublicationOnly);

    /// <summary>True when a supported platform implementation is available.</summary>
    public static bool IsSupported => Implementation.Value is not null;

    /// <summary>The current platform implementation.</summary>
    public static IEsewaPayment Current =>
        Implementation.Value
        ?? throw new NotImplementedException(
            "eSewa payment is not supported on this platform. Only Android and iOS are supported.");

    static IEsewaPayment CreatePlatformImplementation()
    {
#if ANDROID || IOS
        return new EsewaPaymentImplementation();
#else
        return null!;
#endif
    }
}
