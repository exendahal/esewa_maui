using Plugin.Esewa;
using Plugin.Esewa.Epay;

namespace EsewaSample;

public partial class MainPage : ContentPage
{
    // --- Native SDK (ePay) test credentials ---
    const string TestClientId = "JB0BBQ4aD0UqIThFJwAKBgAXEUkEGQUBBAwdOgABHD4DChwUAB0R";
    const string TestSecretKey = "BhwIWQQADhIYSxILExMcAgFXFhcOBwAKBgAXEQ==";
    const string CallbackUrl = "https://developer.esewa.com.np";

    // --- ePay v2 (web checkout) sandbox test credentials ---
    const string EpayProductCode = "EPAYTEST";
    const string EpaySecretKey = "8gBm/:&EnhH.1/q";

    public MainPage()
    {
        InitializeComponent();
    }

    // Native eSewa SDK (Android AAR / iOS xcframework).
    async void OnPayClicked(object? sender, EventArgs e)
    {
        if (!EsewaPayment.IsSupported)
        {
            await DisplayAlertAsync("Unsupported", "eSewa payments are only available on Android and iOS.", "OK");
            return;
        }

        SetBusy(true);
        try
        {
            var request = new EsewaPaymentRequest
            {
                ClientId = TestClientId,
                SecretKey = TestSecretKey,
                Amount = string.IsNullOrWhiteSpace(AmountEntry.Text) ? "100" : AmountEntry.Text.Trim(),
                ProductName = string.IsNullOrWhiteSpace(ProductNameEntry.Text) ? "Test Product" : ProductNameEntry.Text.Trim(),
                ProductId = string.IsNullOrWhiteSpace(ProductIdEntry.Text) ? "TXN-001" : ProductIdEntry.Text.Trim(),
                CallbackUrl = CallbackUrl,
                Environment = EsewaEnvironment.Test,
            };

            EsewaPaymentResult result = await EsewaPayment.Current.PayAsync(request);

            Render(
                result.Status.ToString(),
                result.Status switch
                {
                    EsewaPaymentStatus.Success => Colors.Green,
                    EsewaPaymentStatus.Cancelled => Colors.Orange,
                    _ => Colors.Red,
                },
                result.Message,
                result.Details);
        }
        catch (Exception ex)
        {
            RenderError(ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    // eSewa ePay v2 web checkout (pure C#, hosted page in a WebView).
    async void OnPayEpayClicked(object? sender, EventArgs e)
    {
        if (!EsewaEpay.IsSupported)
        {
            await DisplayAlertAsync("Unsupported", "eSewa ePay is only available on Android and iOS.", "OK");
            return;
        }

        SetBusy(true);
        try
        {
            var request = new EsewaEpayRequest
            {
                Amount = decimal.TryParse(AmountEntry.Text, out var a) ? a : 100m,
                ProductCode = EpayProductCode,
                SecretKey = EpaySecretKey,
                Environment = EsewaEpayEnvironment.Sandbox,
            };

            EsewaEpayResult result = await EsewaEpay.Current.PayAsync(request);

            Render(
                result.Status.ToString(),
                result.Status switch
                {
                    EsewaEpayStatus.Complete => Colors.Green,
                    EsewaEpayStatus.Cancelled => Colors.Orange,
                    EsewaEpayStatus.Pending => Colors.Goldenrod,
                    _ => Colors.Red,
                },
                result.TransactionCode is { Length: > 0 } ? $"{result.Message}  (ref: {result.TransactionCode})" : result.Message,
                result.Details);
        }
        catch (Exception ex)
        {
            RenderError(ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    void Render(string status, Color color, string? message, IReadOnlyDictionary<string, string>? details)
    {
        ResultStatus.Text = status;
        ResultStatus.TextColor = color;
        ResultMessage.Text = message ?? string.Empty;
        ResultDetails.Text = details is { Count: > 0 }
            ? string.Join("\n", details.Select(kvp => $"{kvp.Key}: {kvp.Value}"))
            : string.Empty;
        ResultFrame.IsVisible = true;
    }

    void RenderError(Exception ex)
    {
        ResultStatus.Text = "Error";
        ResultStatus.TextColor = Colors.Red;
        ResultMessage.Text = ex.Message;
        ResultDetails.Text = string.Empty;
        ResultFrame.IsVisible = true;
    }

    void SetBusy(bool busy)
    {
        if (busy)
            ResultFrame.IsVisible = false;
        Busy.IsRunning = busy;
        Busy.IsVisible = busy;
        PayButton.IsEnabled = !busy;
        EpayButton.IsEnabled = !busy;
    }
}
