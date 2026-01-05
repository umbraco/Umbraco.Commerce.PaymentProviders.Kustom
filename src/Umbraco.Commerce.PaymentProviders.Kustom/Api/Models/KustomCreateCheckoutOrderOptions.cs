using System.Text.Json.Serialization;

namespace Umbraco.Commerce.PaymentProviders.Kustom.Api.Models;

public class KustomCreateCheckoutOrderOptions : KustomOrderBase
{
    [JsonPropertyName("merchant_urls")]
    public KustomCheckoutOrderMerchantUrls MerchantUrls { get; set; } = new();
}
