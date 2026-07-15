using System.Text.Json;

namespace Plugin.Esewa.Epay;

/// <summary>Calls the eSewa ePay v2 transaction status API (public GET).</summary>
static class EsewaEpayClient
{
    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

    /// <summary>
    /// Looks up the authoritative transaction status. Returns the raw fields
    /// (status, ref_id, total_amount, …) or null if the call fails (best effort).
    /// </summary>
    public static async Task<IReadOnlyDictionary<string, string>?> LookupAsync(
        EsewaEpayEnvironment environment, string productCode, string totalAmount, string transactionUuid,
        CancellationToken ct)
    {
        var url =
            $"{environment.StatusUrl()}?product_code={Uri.EscapeDataString(productCode)}" +
            $"&total_amount={Uri.EscapeDataString(totalAmount)}" +
            $"&transaction_uuid={Uri.EscapeDataString(transactionUuid)}";

        try
        {
            var json = await Http.GetStringAsync(url, ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return null;

            var result = new Dictionary<string, string>();
            foreach (var prop in doc.RootElement.EnumerateObject())
                result[prop.Name] = prop.Value.ToString();
            return result;
        }
        catch
        {
            return null; // network/parse failure — caller keeps the redirect result
        }
    }
}
