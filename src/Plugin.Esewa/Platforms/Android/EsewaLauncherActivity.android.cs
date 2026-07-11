using Android.App;
using Android.Content;
using Android.OS;
using Com.F1soft.Esewapaymentsdk;

namespace Plugin.Esewa;

/// <summary>
/// A transparent, single-shot relay activity that launches the native
/// <c>EsewaPaymentActivity</c> for a result and forwards the outcome back to
/// the awaiting <see cref="Task{TResult}"/>. Registered automatically via the
/// <see cref="ActivityAttribute"/> when the plugin is referenced.
/// </summary>
[Activity(
    Theme = "@android:style/Theme.Translucent.NoTitleBar",
    ExcludeFromRecents = true,
    Exported = false)]
sealed class EsewaLauncherActivity : Activity
{
    const int RequestCode = 0xE5E7;

    // Native SDK intent extra / result keys (verified against the AAR).
    const string EsewaConfigExtra = "com.esewa.android.sdk.config";
    const string EsewaPaymentExtra = "com.esewa.android.sdk.payment";
    const string EsewaResultMessage = "com.esewa.android.sdk.paymentConfirmation";
    const int EsewaResultExtrasInvalid = 2;
    const string PaymentActivityClass = "com.f1soft.esewapaymentsdk.ui.screens.EsewaPaymentActivity";

    // Extras this relay activity accepts from PayAsync.
    const string ExtraClientId = "plugin.esewa.clientId";
    const string ExtraSecretKey = "plugin.esewa.secretKey";
    const string ExtraEnvironment = "plugin.esewa.environment";
    const string ExtraAmount = "plugin.esewa.amount";
    const string ExtraProductName = "plugin.esewa.productName";
    const string ExtraProductId = "plugin.esewa.productId";
    const string ExtraCallbackUrl = "plugin.esewa.callbackUrl";
    const string ExtraProperties = "plugin.esewa.properties";
    const string StateLaunched = "plugin.esewa.launched";

    static TaskCompletionSource<EsewaPaymentResult>? _pending;
    bool _launched;

    public static Task<EsewaPaymentResult> StartAsync(
        Activity host, EsewaPaymentRequest request, CancellationToken cancellationToken)
    {
        // Only one payment flow can be in flight at a time.
        _pending?.TrySetResult(EsewaPaymentResult.Cancelled("Superseded by a new payment request."));

        var tcs = new TaskCompletionSource<EsewaPaymentResult>();
        _pending = tcs;

        if (cancellationToken.CanBeCanceled)
            cancellationToken.Register(() => Complete(EsewaPaymentResult.Cancelled()));

        var intent = new Intent(host, typeof(EsewaLauncherActivity));
        intent.PutExtra(ExtraClientId, request.ClientId);
        intent.PutExtra(ExtraSecretKey, request.SecretKey);
        intent.PutExtra(ExtraEnvironment, (int)request.Environment);
        intent.PutExtra(ExtraAmount, request.Amount);
        intent.PutExtra(ExtraProductName, request.ProductName);
        intent.PutExtra(ExtraProductId, request.ProductId);
        intent.PutExtra(ExtraCallbackUrl, request.CallbackUrl);

        if (request.Properties is { Count: > 0 })
        {
            var bundle = new Bundle();
            foreach (var kvp in request.Properties)
                bundle.PutString(kvp.Key, kvp.Value);
            intent.PutExtra(ExtraProperties, bundle);
        }

        host.StartActivity(intent);
        return tcs.Task;
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        _launched = savedInstanceState?.GetBoolean(StateLaunched) ?? false;
        if (_launched)
            return;

        _launched = true;
        LaunchNativePayment();
    }

    protected override void OnSaveInstanceState(Bundle outState)
    {
        outState.PutBoolean(StateLaunched, _launched);
        base.OnSaveInstanceState(outState);
    }

    void LaunchNativePayment()
    {
        try
        {
            var environment = (EsewaEnvironment)Intent!.GetIntExtra(ExtraEnvironment, 0);

            var configuration = new EsewaConfiguration(
                Intent.GetStringExtra(ExtraClientId)!,
                Intent.GetStringExtra(ExtraSecretKey)!,
                environment == EsewaEnvironment.Production
                    ? EsewaConfiguration.EnvironmentProduction
                    : EsewaConfiguration.EnvironmentTest);

            var properties = new Dictionary<string, string>();
            if (Intent.GetBundleExtra(ExtraProperties) is { } bundle)
            {
                foreach (var key in bundle.KeySet() ?? Enumerable.Empty<string>())
                    properties[key] = bundle.GetString(key) ?? string.Empty;
            }

            var payment = new EsewaPayment(
                Intent.GetStringExtra(ExtraAmount)!,
                Intent.GetStringExtra(ExtraProductName)!,
                Intent.GetStringExtra(ExtraProductId)!,
                Intent.GetStringExtra(ExtraCallbackUrl)!,
                properties);

            var esewaIntent = new Intent();
            esewaIntent.SetClassName(this, PaymentActivityClass);
            esewaIntent.PutExtra(EsewaConfigExtra, configuration);
            esewaIntent.PutExtra(EsewaPaymentExtra, payment);

            StartActivityForResult(esewaIntent, RequestCode);
        }
        catch (Exception ex)
        {
            Complete(EsewaPaymentResult.Failed($"Failed to launch eSewa: {ex.Message}"));
            Finish();
        }
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);

        if (requestCode != RequestCode)
            return;

        var message = data?.GetStringExtra(EsewaResultMessage);

        var result = resultCode switch
        {
            Result.Ok => EsewaPaymentResult.Succeeded(message, ReadDetails(data)),
            Result.Canceled => EsewaPaymentResult.Cancelled(message),
            _ => EsewaPaymentResult.Failed(
                (int)resultCode == EsewaResultExtrasInvalid
                    ? message ?? "Invalid payment configuration supplied to eSewa."
                    : message ?? "eSewa payment failed."),
        };

        Complete(result);
        Finish();
    }

    static void Complete(EsewaPaymentResult result)
    {
        var pending = _pending;
        _pending = null;
        pending?.TrySetResult(result);
    }

    static IReadOnlyDictionary<string, string>? ReadDetails(Intent? data)
    {
        if (data?.Extras is not { } extras)
            return null;

        var details = new Dictionary<string, string>();
        foreach (var key in extras.KeySet() ?? Enumerable.Empty<string>())
        {
            var value = extras.Get(key);
            if (value is not null)
                details[key] = value.ToString() ?? string.Empty;
        }

        return details.Count > 0 ? details : null;
    }
}
