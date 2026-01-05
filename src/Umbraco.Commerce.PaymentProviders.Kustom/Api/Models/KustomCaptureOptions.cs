using System.Text.Json.Serialization;

namespace Umbraco.Commerce.PaymentProviders.Kustom.Api.Models
{
    public class KustomCaptureOptions
    {
        [JsonPropertyName("captured_amount")]
        public int CapturedAmount { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("reference")]
        public string Reference { get; set; }
    }
}
