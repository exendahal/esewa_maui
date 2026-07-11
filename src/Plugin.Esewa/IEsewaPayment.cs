namespace Plugin.Esewa;

/// <summary>
/// Unified, cross-platform entry point for launching an eSewa payment.
/// Resolve the shared instance from <see cref="EsewaPayment.Current"/>.
/// </summary>
public interface IEsewaPayment
{
    /// <summary>
    /// Presents the native eSewa payment UI and completes when the user
    /// finishes, cancels, or the flow errors out.
    /// </summary>
    /// <param name="request">Payment details.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A normalized <see cref="EsewaPaymentResult"/>.</returns>
    Task<EsewaPaymentResult> PayAsync(EsewaPaymentRequest request, CancellationToken cancellationToken = default);
}
