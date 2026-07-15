namespace Plugin.Esewa.Epay;

/// <summary>Outcome of an eSewa ePay v2 payment (mirrors the gateway's status values).</summary>
public enum EsewaEpayStatus
{
    /// <summary>Payment completed successfully.</summary>
    Complete,

    /// <summary>Payment is pending / not yet confirmed.</summary>
    Pending,

    /// <summary>The user cancelled / closed the checkout.</summary>
    Cancelled,

    /// <summary>The payment failed.</summary>
    Failed,

    /// <summary>The payment was (fully or partially) refunded.</summary>
    Refunded,

    /// <summary>Status could not be determined (ambiguous / not found).</summary>
    Unknown,
}

/// <summary>Normalized result returned from <see cref="IEsewaEpayPayment.PayAsync"/>.</summary>
public sealed class EsewaEpayResult
{
    /// <summary>Overall status of the attempt.</summary>
    public required EsewaEpayStatus Status { get; init; }

    /// <summary>The transaction id that was sent (transaction_uuid).</summary>
    public string? TransactionUuid { get; init; }

    /// <summary>eSewa transaction reference (transaction_code / ref_id) once completed.</summary>
    public string? TransactionCode { get; init; }

    /// <summary>Total amount charged.</summary>
    public string? TotalAmount { get; init; }

    /// <summary>Merchant product code.</summary>
    public string? ProductCode { get; init; }

    /// <summary>Human readable message.</summary>
    public string? Message { get; init; }

    /// <summary>All raw key/value fields returned by the gateway.</summary>
    public IReadOnlyDictionary<string, string>? Details { get; init; }

    public bool IsSuccess => Status == EsewaEpayStatus.Complete;

    internal static EsewaEpayResult Cancelled(string? message = null) =>
        new() { Status = EsewaEpayStatus.Cancelled, Message = message ?? "Payment cancelled." };

    internal static EsewaEpayResult Failed(string? message) =>
        new() { Status = EsewaEpayStatus.Failed, Message = message ?? "Payment failed." };
}
