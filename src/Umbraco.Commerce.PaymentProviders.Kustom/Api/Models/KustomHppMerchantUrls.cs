using System.Text.Json.Serialization;

namespace Umbraco.Commerce.PaymentProviders.Kustom.Api.Models
{
    public class KustomHppMerchantUrls
    {
        [JsonPropertyName("success")]
        public string Success { get; set; }

        [JsonPropertyName("cancel")]
        public string Cancel { get; set; }

        [JsonPropertyName("back")]
        public string Back { get; set; }

        [JsonPropertyName("failure")]
        public string Failure { get; set; }

        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("status_update")]
        public string StatusUpdate { get; set; }
    }
}
