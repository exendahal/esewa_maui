using Plugin.Esewa;

namespace EsewaSample;

public partial class MainPage : ContentPage
{
    // Demo test-environment merchant credentials from eSewa's ePay sandbox.
    // Replace with your own merchant client id / secret for production.
    const string TestClientId = "JB0BBQ4aD0UqIThFJwAKBgAXEUkEGQUBBAwdOgABHD4DChwUAB0R";
    const string TestSecretKey = "BhwIWQQADhIYSxILExMcAgFXFhcOBwAKBgAXEQ==";
    const string CallbackUrl = "https://developer.esewa.com.np";

    public MainPage()
    {
        InitializeComponent();
    }

    async void OnPayClicked(object? sender, EventArgs e)
    {
        if (!CrossEsewaPayment.IsSupported)
        {
            await DisplayAlertAsync("Unsupported", "eSewa payments are only available on Android and iOS.", "OK");
            return;
        }

        SetBusy(true);
        ResultFrame.IsVisible = false;

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

            // The one call the whole plugin exists for:
            EsewaPaymentResult result = await CrossEsewaPayment.Current.PayAsync(request);

            ShowResult(result);
        }
        catch (Exception ex)
        {
            ResultStatus.Text = "Error";
            ResultStatus.TextColor = Colors.Red;
            ResultMessage.Text = ex.Message;
            ResultDetails.Text = string.Empty;
            ResultFrame.IsVisible = true;
        }
        finally
        {
            SetBusy(false);
        }
    }

    void ShowResult(EsewaPaymentResult result)
    {
        ResultStatus.Text = result.Status.ToString();
        ResultStatus.TextColor = result.Status switch
        {
            EsewaPaymentStatus.Success => Colors.Green,
            EsewaPaymentStatus.Cancelled => Colors.Orange,
            _ => Colors.Red,
        };
        ResultMessage.Text = result.Message ?? string.Empty;
        ResultDetails.Text = result.Details is { Count: > 0 }
            ? string.Join("\n", result.Details.Select(kvp => $"{kvp.Key}: {kvp.Value}"))
            : string.Empty;
        ResultFrame.IsVisible = true;
    }

    void SetBusy(bool busy)
    {
        Busy.IsRunning = busy;
        Busy.IsVisible = busy;
        PayButton.IsEnabled = !busy;
    }
}
