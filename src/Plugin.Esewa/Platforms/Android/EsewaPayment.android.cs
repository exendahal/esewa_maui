using Microsoft.Maui.ApplicationModel;

namespace Plugin.Esewa;

/// <summary>
/// Android implementation backed by <c>com.f1soft.esewapaymentsdk</c>.
/// The actual <c>startActivityForResult</c> / <c>onActivityResult</c> dance is
/// handled by <see cref="EsewaLauncherActivity"/>, a transparent relay
/// activity, so we do not depend on any host-app plumbing.
/// </summary>
sealed class EsewaPaymentImplementation : IEsewaPayment
{
    public Task<EsewaPaymentResult> PayAsync(EsewaPaymentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var activity = Platform.CurrentActivity
            ?? throw new InvalidOperationException(
                "No current Activity. Ensure Plugin.Esewa is called from a running Android Activity.");

        return EsewaLauncherActivity.StartAsync(activity, request, cancellationToken);
    }
}
