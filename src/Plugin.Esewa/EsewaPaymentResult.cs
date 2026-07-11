namespace Plugin.Esewa;

/// <summary>
/// Outcome of a payment attempt.
/// </summary>
public enum EsewaPaymentStatus
{
    /// <summary>Payment completed successfully.</summary>
    Success,

    /// <summary>Payment failed or was rejected by the SDK / backend.</summary>
    Failure,

    /// <summary>The user cancelled the payment flow.</summary>
    Cancelled,
}

/// <summary>
/// Normalized result returned from <see cref="IEsewaPayment.PayAsync"/>.
/// </summary>
public sealed class EsewaPaymentResult
{
    /// <summary>Overall status of the attempt.</summary>
    public required EsewaPaymentStatus Status { get; init; }

    /// <summary>Human readable message from the SDK, when available.</summary>
    public string? Message { get; init; }

    /// <summary>
    /// Raw key/value details supplied by the native SDK on success
    /// (transaction details, reference codes, etc.).
    /// </summary>
    public IReadOnlyDictionary<string, string>? Details { get; init; }

    public bool IsSuccess => Status == EsewaPaymentStatus.Success;

    internal static EsewaPaymentResult Cancelled(string? message = null) =>
        new() { Status = EsewaPaymentStatus.Cancelled, Message = message ?? "Payment cancelled." };

    internal static EsewaPaymentResult Failed(string? message) =>
        new() { Status = EsewaPaymentStatus.Failure, Message = message };

    internal static EsewaPaymentResult Succeeded(string? message, IReadOnlyDictionary<string, string>? details) =>
        new() { Status = EsewaPaymentStatus.Success, Message = message, Details = details };
}
