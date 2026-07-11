namespace Plugin.Esewa;

/// <summary>
/// Locator for the platform implementation of <see cref="IEsewaPayment"/>.
/// </summary>
public static class CrossEsewaPayment
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
