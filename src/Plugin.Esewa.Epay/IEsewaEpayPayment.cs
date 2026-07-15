namespace Plugin.Esewa.Epay;

/// <summary>
/// Cross-platform entry point for launching an eSewa ePay v2 payment.
/// Resolve the shared instance from <see cref="EsewaEpay.Current"/>.
/// </summary>
public interface IEsewaEpayPayment
{
    /// <summary>
    /// Presents the hosted eSewa checkout in a WebView and completes when the
    /// payment finishes, the user cancels, or an error occurs.
    /// </summary>
    Task<EsewaEpayResult> PayAsync(EsewaEpayRequest request, CancellationToken cancellationToken = default);
}
