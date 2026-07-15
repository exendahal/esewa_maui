using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Plugin.Esewa.Epay;

/// <summary>Builds the signed ePay v2 form fields.</summary>
static class EsewaEpaySignature
{
    // eSewa signs exactly these fields, in this order.
    const string SignedFieldNames = "total_amount,transaction_uuid,product_code";

    public static string FormatAmount(decimal value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>
    /// Produces the ordered set of form fields (including the HMAC signature)
    /// to POST to the ePay v2 form endpoint.
    /// </summary>
    public static IReadOnlyList<KeyValuePair<string, string>> BuildFields(EsewaEpayRequest request, string transactionUuid)
    {
        var amount = FormatAmount(request.Amount);
        var taxAmount = FormatAmount(request.TaxAmount);
        var serviceCharge = FormatAmount(request.ServiceCharge);
        var deliveryCharge = FormatAmount(request.DeliveryCharge);
        var totalAmount = FormatAmount(
            request.Amount + request.TaxAmount + request.ServiceCharge + request.DeliveryCharge);

        var signature = Sign(totalAmount, transactionUuid, request.ProductCode, request.SecretKey);

        return new List<KeyValuePair<string, string>>
        {
            new("amount", amount),
            new("tax_amount", taxAmount),
            new("total_amount", totalAmount),
            new("transaction_uuid", transactionUuid),
            new("product_code", request.ProductCode),
            new("product_service_charge", serviceCharge),
            new("product_delivery_charge", deliveryCharge),
            new("success_url", request.SuccessUrl),
            new("failure_url", request.FailureUrl),
            new("signed_field_names", SignedFieldNames),
            new("signature", signature),
        };
    }

    /// <summary>
    /// HMAC-SHA256 over "total_amount=..,transaction_uuid=..,product_code=..",
    /// base64-encoded — the ePay v2 signature scheme.
    /// </summary>
    public static string Sign(string totalAmount, string transactionUuid, string productCode, string secretKey)
    {
        var message = $"total_amount={totalAmount},transaction_uuid={transactionUuid},product_code={productCode}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToBase64String(hash);
    }
}
