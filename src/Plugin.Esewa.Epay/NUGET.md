# esewa_maui.epay

Accept **eSewa** payments in your .NET MAUI app via eSewa's current **ePay v2**
hosted checkout — a lightweight, **pure C#** library (no native SDK binding, no
`AAR`/`xcframework`). It signs the request, opens the hosted eSewa page in a
WebView, and verifies the signed result behind one cross-platform call.

```csharp
using Plugin.Esewa.Epay;

var result = await EsewaEpay.Current.PayAsync(new EsewaEpayRequest
{
    Amount      = 100,
    ProductCode = "<your-merchant-code>",     // "EPAYTEST" in sandbox
    SecretKey   = "<your-merchant-secret>",
    Environment = EsewaEpayEnvironment.Production,
});

if (result.IsSuccess)
    await DisplayAlert("Success", $"ref {result.TransactionCode}", "OK");
```

## Requirements

| | |
| --- | --- |
| Framework | .NET MAUI (.NET 10+) |
| Android | API 24+ |
| iOS | 13.0+ |

## Installation

```bash
dotnet add package esewa_maui.epay
```

No manifest edits, no URL-scheme registration — nothing extra to set up.

## How it works

1. Builds the ePay v2 form (`amount`, `total_amount`, `transaction_uuid`,
   `product_code`, HMAC-SHA256 `signature`, …).
2. Auto-submits it to the hosted eSewa checkout inside a WebView.
3. Intercepts the success / failure redirect and decodes the signed response.
4. When `VerifyStatus` is enabled (default), confirms the outcome via the eSewa
   transaction **status API** — the authoritative source of truth.

## API

| Member | Description |
| --- | --- |
| `EsewaEpay.Current.PayAsync(request)` | Runs the hosted checkout, returns a normalized result. |
| `EsewaEpayRequest` | `Amount` (+ `TaxAmount`/`ServiceCharge`/`DeliveryCharge`), `ProductCode`, `SecretKey`, `TransactionUuid?`, `Environment`, `SuccessUrl`/`FailureUrl`, `VerifyStatus`. |
| `EsewaEpayResult` | `Status`, `TransactionUuid`, `TransactionCode`, `TotalAmount`, `ProductCode`, `Message`, `Details`. |
| `EsewaEpayStatus` | `Complete`, `Pending`, `Cancelled`, `Failed`, `Refunded`, `Unknown`. |

## Sandbox

`ProductCode = "EPAYTEST"`, `SecretKey = "8gBm/:&EnhH.1/q"`, and
`Environment = EsewaEpayEnvironment.Sandbox`. See
<https://developer.esewa.com.np/> for merchant onboarding and the latest ePay v2
reference.

## License

MIT. The library talks to eSewa's public ePay v2 API; eSewa and its gateway are
operated by eSewa — see <https://developer.esewa.com.np/>.
