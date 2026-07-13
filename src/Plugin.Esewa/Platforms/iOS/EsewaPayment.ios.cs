using CoreText;
using Foundation;
using Microsoft.Maui.ApplicationModel;
using Plugin.Esewa.Binding;
using UIKit;

namespace Plugin.Esewa;

/// <summary>
/// iOS implementation backed by <c>EsewaSDK.xcframework</c> via the
/// <c>EsewaBridge</c> Swift shim (see <c>Native/Shim/EsewaBridge.swift</c>).
/// </summary>
sealed class EsewaPaymentImplementation : IEsewaPayment
{
    public Task<EsewaPaymentResult> PayAsync(EsewaPaymentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var viewController = Platform.GetCurrentUIViewController()
            ?? throw new InvalidOperationException(
                "No current UIViewController available to present the eSewa payment UI.");

        var tcs = new TaskCompletionSource<EsewaPaymentResult>();

        var environment = request.Environment == EsewaEnvironment.Production
            ? "production"
            : "development";

        NSDictionary? properties = null;
        if (request.Properties is { Count: > 0 })
        {
            properties = NSDictionary.FromObjectsAndKeys(
                request.Properties.Values.Cast<object>().ToArray(),
                request.Properties.Keys.Cast<object>().ToArray());
        }

        // The bridge and the delegate are passed to native code. The native
        // SDK may hold its delegate weakly, so we must keep both managed peers
        // rooted ourselves until a callback fires — otherwise the GC could
        // collect them mid-flow and the native callback would crash.
        var bridge = new EsewaBridge();
        var handler = new PaymentDelegate(tcs, bridge);
        PaymentDelegate.Root(handler);

        if (cancellationToken.CanBeCanceled)
        {
            // Complete the task early on cancellation, but keep the delegate
            // rooted; the SDK still owns the on-screen flow and will deliver a
            // final callback that unroots it.
            cancellationToken.Register(() => tcs.TrySetResult(EsewaPaymentResult.Cancelled()));
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                // The SDK renders its UI with bundled Asap / SourceSansPro fonts.
                // iOS only auto-registers UIAppFonts from the main app bundle, not
                // from an embedded framework, so we register the framework's fonts
                // ourselves — otherwise the SDK fatal-errors when a font is missing.
                EnsureFontsRegistered();

                bridge.InitiatePayment(
                    viewController,
                    environment,
                    request.ClientId,
                    request.SecretKey,
                    request.ProductName,
                    request.Amount,
                    request.ProductId,
                    request.CallbackUrl,
                    properties,
                    handler);
            }
            catch (Exception ex)
            {
                PaymentDelegate.Unroot(handler);
                tcs.TrySetResult(EsewaPaymentResult.Failed($"Failed to launch eSewa: {ex.Message}"));
            }
        });

        return tcs.Task;
    }

    static int _fontsRegistered;

    /// <summary>
    /// Registers the fonts bundled inside EsewaSDK.framework with the process,
    /// once. The framework declares them in its own Info.plist UIAppFonts, but
    /// iOS only honors that for the main app bundle — hence explicit registration.
    /// </summary>
    static void EnsureFontsRegistered()
    {
        if (Interlocked.Exchange(ref _fontsRegistered, 1) == 1)
            return;

        try
        {
            // The framework's CFBundleIdentifier (from its Info.plist).
            var bundle = NSBundle.FromIdentifier("com.esewa.EsewaSDK");
            var urls = bundle?.GetUrlsForResourcesWithExtension("ttf", null);
            if (urls is null)
                return;

            foreach (var url in urls)
                CTFontManager.RegisterFontsForUrl(url, CTFontManagerScope.Process);
        }
        catch
        {
            // Best effort — if registration fails the SDK will surface its own error.
        }
    }

    /// <summary>Bridges the native <c>EsewaSDKPaymentDelegate</c> callbacks to a Task.</summary>
    sealed class PaymentDelegate : EsewaSDKPaymentDelegate
    {
        // Roots live delegates so they (and their bridge) survive until the
        // native SDK delivers its final callback.
        static readonly HashSet<PaymentDelegate> Live = new();
        static readonly object Gate = new();

        readonly TaskCompletionSource<EsewaPaymentResult> _tcs;
#pragma warning disable IDE0052 // held only to keep the native bridge alive
        readonly EsewaBridge _bridge;
#pragma warning restore IDE0052

        public PaymentDelegate(TaskCompletionSource<EsewaPaymentResult> tcs, EsewaBridge bridge)
        {
            _tcs = tcs;
            _bridge = bridge;
        }

        public static void Root(PaymentDelegate handler)
        {
            lock (Gate)
                Live.Add(handler);
        }

        public static void Unroot(PaymentDelegate handler)
        {
            lock (Gate)
                Live.Remove(handler);
        }

        public override void OnPaymentSuccess(NSDictionary info)
        {
            Unroot(this);
            _tcs.TrySetResult(EsewaPaymentResult.Succeeded("Payment successful.", ToDictionary(info)));
        }

        public override void OnPaymentError(string errorDescription)
        {
            Unroot(this);
            var cancelled = errorDescription?.Contains("cancel", StringComparison.OrdinalIgnoreCase) == true;
            _tcs.TrySetResult(cancelled
                ? EsewaPaymentResult.Cancelled(errorDescription)
                : EsewaPaymentResult.Failed(errorDescription ?? "eSewa payment failed."));
        }

        static IReadOnlyDictionary<string, string>? ToDictionary(NSDictionary? info)
        {
            if (info is null || info.Count == 0)
                return null;

            var result = new Dictionary<string, string>();
            foreach (var key in info.Keys)
            {
                var value = info[key];
                result[key.ToString()] = value?.ToString() ?? string.Empty;
            }

            return result;
        }
    }
}
