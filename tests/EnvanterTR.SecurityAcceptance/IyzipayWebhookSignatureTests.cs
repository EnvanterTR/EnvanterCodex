using EnvanterTR.Web.Data;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed class IyzipayWebhookSignatureTests
{
    private const string Secret = "synthetic-webhook-secret";
    private const string Signature = "dd1432e8946260e4422b5eafb9e6c934e650cadf5fc3c097caf6f73b3c23ca12";

    [Fact]
    public void Checkout_form_v3_signature_matches_the_provider_canonical_payload()
    {
        Assert.True(IyzipayWebhookSignature.Verify(Secret, "CHECKOUT_FORM_AUTH", "payment-123",
            "checkout-token", "checkout-456", "SUCCESS", Signature));
    }

    [Fact]
    public void Checkout_form_v3_signature_rejects_tampering_and_malformed_headers()
    {
        Assert.False(IyzipayWebhookSignature.Verify(Secret, "CHECKOUT_FORM_AUTH", "payment-other",
            "checkout-token", "checkout-456", "SUCCESS", Signature));
        Assert.False(IyzipayWebhookSignature.Verify(Secret, "CHECKOUT_FORM_AUTH", "payment-123",
            "checkout-token", "checkout-456", "FAILURE", Signature));
        Assert.False(IyzipayWebhookSignature.Verify(Secret, "CHECKOUT_FORM_AUTH", "payment-123",
            "checkout-token", "checkout-456", "SUCCESS", "not-a-signature"));
        Assert.False(IyzipayWebhookSignature.Verify("", "CHECKOUT_FORM_AUTH", "payment-123",
            "checkout-token", "checkout-456", "SUCCESS", Signature));
    }
}
