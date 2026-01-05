using System.Text.Json.Serialization;

namespace Umbraco.Commerce.PaymentProviders.Kustom.Api.Models
{
    public class KustomCreateHppSessionOptions
    {
        [JsonPropertyName("payment_session_url")]
        public string PaymentSessionUrl { get; set; }

        [JsonPropertyName("merchant_urls")]
        public KustomHppMerchantUrls MerchantUrls { get; set; }

        [JsonPropertyName("options")]
        public KustomHppOptions Options { get; set; }

        public KustomCreateHppSessionOptions()
        {
            MerchantUrls = new KustomHppMerchantUrls();
            Options = new KustomHppOptions();
        }
    }
}
