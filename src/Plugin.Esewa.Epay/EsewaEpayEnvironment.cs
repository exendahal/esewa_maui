namespace Plugin.Esewa.Epay;

/// <summary>Target eSewa ePay v2 environment.</summary>
public enum EsewaEpayEnvironment
{
    /// <summary>Sandbox / RC environment (rc-epay.esewa.com.np).</summary>
    Sandbox,

    /// <summary>Live production environment (epay.esewa.com.np).</summary>
    Production,
}

static class EsewaEpayEnvironmentExtensions
{
    /// <summary>The hosted-checkout form endpoint (form POST target).</summary>
    public static string FormUrl(this EsewaEpayEnvironment env) => env switch
    {
        EsewaEpayEnvironment.Production => "https://epay.esewa.com.np/api/epay/main/v2/form",
        _ => "https://rc-epay.esewa.com.np/api/epay/main/v2/form",
    };

    /// <summary>The transaction status-check endpoint.</summary>
    public static string StatusUrl(this EsewaEpayEnvironment env) => env switch
    {
        EsewaEpayEnvironment.Production => "https://epay.esewa.com.np/api/epay/transaction/status/",
        _ => "https://rc.esewa.com.np/api/epay/transaction/status/",
    };
}
