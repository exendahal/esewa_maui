using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace Plugin.Esewa.Epay;

/// <summary>
/// Cross-platform eSewa ePay v2 flow: sign the request, present the hosted
/// checkout in a modal WebView, and resolve the signed success/failure redirect.
/// </summary>
sealed class EsewaEpayImplementation : IEsewaEpayPayment
{
    public async Task<EsewaEpayResult> PayAsync(EsewaEpayRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.ProductCode))
            throw new ArgumentException("ProductCode is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.SecretKey))
            throw new ArgumentException("SecretKey is required.", nameof(request));

        var transactionUuid = string.IsNullOrWhiteSpace(request.TransactionUuid)
            ? Guid.NewGuid().ToString("N")
            : request.TransactionUuid!;

        var totalAmount = EsewaEpaySignature.FormatAmount(
            request.Amount + request.TaxAmount + request.ServiceCharge + request.DeliveryCharge);

        var fields = EsewaEpaySignature.BuildFields(request, transactionUuid);

        var tcs = new TaskCompletionSource<EsewaEpayResult>();

        if (cancellationToken.CanBeCanceled)
            cancellationToken.Register(() => tcs.TrySetResult(EsewaEpayResult.Cancelled()));

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var navigation = GetNavigation()
                ?? throw new InvalidOperationException("No active page available to present the eSewa checkout.");

            var page = new EsewaCheckoutPage(
                request.Environment.FormUrl(),
                fields,
                request.SuccessUrl,
                request.FailureUrl,
                transactionUuid,
                request.ProductCode,
                tcs);

            await navigation.PushModalAsync(new NavigationPage(page));
        }).ConfigureAwait(false);

        var result = await tcs.Task.ConfigureAwait(false);

        // Confirm authoritatively via the status API (the redirect is only a hint).
        // Skip when the user cancelled — there's nothing to confirm.
        if (request.VerifyStatus && result.Status != EsewaEpayStatus.Cancelled)
        {
            var lookup = await EsewaEpayClient
                .LookupAsync(request.Environment, request.ProductCode, totalAmount, transactionUuid, cancellationToken)
                .ConfigureAwait(false);

            if (lookup is not null)
                result = Reconcile(result, lookup, transactionUuid, request.ProductCode);
        }

        return result;
    }

    static EsewaEpayResult Reconcile(
        EsewaEpayResult redirect, IReadOnlyDictionary<string, string> lookup, string transactionUuid, string productCode)
    {
        var details = new Dictionary<string, string>();
        if (redirect.Details is not null)
            foreach (var kv in redirect.Details)
                details[kv.Key] = kv.Value;
        foreach (var kv in lookup) // status-API fields win
            details[kv.Key] = kv.Value;

        var statusText = lookup.GetValueOrDefault("status");
        return new EsewaEpayResult
        {
            Status = EsewaCheckoutPage.MapStatus(statusText),
            TransactionUuid = transactionUuid,
            TransactionCode = lookup.GetValueOrDefault("ref_id") ?? redirect.TransactionCode,
            TotalAmount = lookup.GetValueOrDefault("total_amount") ?? redirect.TotalAmount,
            ProductCode = productCode,
            Message = statusText ?? redirect.Message,
            Details = details,
        };
    }

    static INavigation? GetNavigation()
    {
        var window = Application.Current?.Windows.FirstOrDefault();
        return window?.Page?.Navigation;
    }
}
