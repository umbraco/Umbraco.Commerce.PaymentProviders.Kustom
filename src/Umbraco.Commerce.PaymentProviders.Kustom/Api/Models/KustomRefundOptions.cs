using System.Text.Json.Serialization;

namespace Umbraco.Commerce.PaymentProviders.Kustom.Api.Models
{
    public class KustomRefundOptions
    {
        [JsonPropertyName("refunded_amount")]
        public int RefundAmount { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("reference")]
        public string Reference { get; set; }
    }
}
