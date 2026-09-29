namespace NaijaPrimeSchool.Infrastructure.Payments;

// Bound from the "OnlinePayments" section of appsettings.json.
public class OnlinePaymentOptions
{
    public const string SectionName = "OnlinePayments";

    // Off by default so a fresh deployment never shows "Pay online" until
    // real gateway keys are configured.
    public bool Enabled { get; set; }

    // "Paystack" in production. "Simulator" is a local fake checkout for
    // development only; the web host refuses to start with it outside the
    // Development environment.
    public string Provider { get; set; } = "Paystack";

    // Smallest single online payment accepted, in naira.
    public decimal MinimumAmount { get; set; } = 100m;

    public PaystackOptions Paystack { get; set; } = new();
}

public class PaystackOptions
{
    public string BaseUrl { get; set; } = "https://api.paystack.co";

    // sk_live_... / sk_test_... — keep in user-secrets or an environment
    // variable, never in appsettings.json.
    public string? SecretKey { get; set; }
}
