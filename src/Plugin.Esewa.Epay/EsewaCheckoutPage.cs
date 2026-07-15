using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace Plugin.Esewa.Epay;

/// <summary>
/// A modal page hosting the eSewa ePay v2 checkout. It loads an auto-submitting
/// HTML form that POSTs the signed fields to eSewa, then intercepts the
/// success / failure redirect to resolve the result.
/// </summary>
sealed class EsewaCheckoutPage : ContentPage
{
    readonly TaskCompletionSource<EsewaEpayResult> _tcs;
    readonly string _successUrl;
    readonly string _failureUrl;
    readonly string _transactionUuid;
    readonly string _productCode;
    bool _done;

    public EsewaCheckoutPage(
        string formUrl,
        IReadOnlyList<KeyValuePair<string, string>> fields,
        string successUrl,
        string failureUrl,
        string transactionUuid,
        string productCode,
        TaskCompletionSource<EsewaEpayResult> tcs)
    {
        _tcs = tcs;
        _successUrl = successUrl;
        _failureUrl = failureUrl;
        _transactionUuid = transactionUuid;
        _productCode = productCode;

        Title = "eSewa";

        var cancel = new ToolbarItem { Text = "Cancel" };
        cancel.Clicked += (_, _) => Complete(EsewaEpayResult.Cancelled());
        ToolbarItems.Add(cancel);

        var web = new WebView { Source = new HtmlWebViewSource { Html = BuildHtml(formUrl, fields) } };
        web.Navigating += OnNavigating;
        Content = web;
    }

    void OnNavigating(object? sender, WebNavigatingEventArgs e)
    {
        var url = e.Url ?? string.Empty;

        if (url.StartsWith(_successUrl, StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
            Complete(ParseCallback(url));
        }
        else if (url.StartsWith(_failureUrl, StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
            Complete(EsewaEpayResult.Failed("Payment failed or was declined."));
        }
    }

    // Covers the user swiping/hardware-back dismissing the sheet without paying.
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Complete(EsewaEpayResult.Cancelled());
    }

    void Complete(EsewaEpayResult result)
    {
        if (_done)
            return;
        _done = true;

        _tcs.TrySetResult(result);

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                if (Navigation.ModalStack.Count > 0)
                    await Navigation.PopModalAsync();
            }
            catch { /* already dismissed */ }
        });
    }

    EsewaEpayResult ParseCallback(string url)
    {
        var details = new Dictionary<string, string>();
        string? status = null, transactionCode = null, totalAmount = null;

        var data = GetQueryValue(url, "data");
        if (!string.IsNullOrEmpty(data))
        {
            try
            {
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(data!));
                using var doc = JsonDocument.Parse(json);
                foreach (var prop in doc.RootElement.EnumerateObject())
                    details[prop.Name] = prop.Value.ToString();

                status = details.GetValueOrDefault("status");
                transactionCode = details.GetValueOrDefault("transaction_code");
                totalAmount = details.GetValueOrDefault("total_amount");
            }
            catch (Exception ex)
            {
                return EsewaEpayResult.Failed($"Could not read eSewa response: {ex.Message}");
            }
        }

        return new EsewaEpayResult
        {
            Status = MapStatus(status),
            TransactionUuid = details.GetValueOrDefault("transaction_uuid") ?? _transactionUuid,
            TransactionCode = transactionCode,
            TotalAmount = totalAmount,
            ProductCode = details.GetValueOrDefault("product_code") ?? _productCode,
            Message = status ?? "Payment completed.",
            Details = details.Count > 0 ? details : null,
        };
    }

    internal static EsewaEpayStatus MapStatus(string? status) => status?.Trim().ToUpperInvariant() switch
    {
        "COMPLETE" => EsewaEpayStatus.Complete,
        "PENDING" => EsewaEpayStatus.Pending,
        "CANCELED" or "CANCELLED" => EsewaEpayStatus.Cancelled,
        "FULL_REFUND" or "PARTIAL_REFUND" => EsewaEpayStatus.Refunded,
        "NOT_FOUND" or "AMBIGUOUS" => EsewaEpayStatus.Unknown,
        null or "" => EsewaEpayStatus.Unknown,
        _ => EsewaEpayStatus.Failed,
    };

    static string? GetQueryValue(string url, string key)
    {
        var q = url.IndexOf('?');
        if (q < 0)
            return null;

        foreach (var pair in url[(q + 1)..].Split('&'))
        {
            var eq = pair.IndexOf('=');
            if (eq <= 0)
                continue;
            if (string.Equals(pair[..eq], key, StringComparison.OrdinalIgnoreCase))
                return Uri.UnescapeDataString(pair[(eq + 1)..]);
        }
        return null;
    }

    static string BuildHtml(string formUrl, IReadOnlyList<KeyValuePair<string, string>> fields)
    {
        var inputs = new StringBuilder();
        foreach (var f in fields)
            inputs.Append($"<input type=\"hidden\" name=\"{WebUtility.HtmlEncode(f.Key)}\" value=\"{WebUtility.HtmlEncode(f.Value)}\"/>");

        return
            "<!DOCTYPE html><html><head><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"/></head>" +
            "<body onload=\"document.forms[0].submit()\" style=\"font-family:sans-serif;text-align:center;padding-top:40px\">" +
            $"<form action=\"{WebUtility.HtmlEncode(formUrl)}\" method=\"POST\">{inputs}</form>" +
            "<p>Redirecting to eSewa…</p></body></html>";
    }
}
