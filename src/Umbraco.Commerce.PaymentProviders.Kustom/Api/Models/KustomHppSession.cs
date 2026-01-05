using System.Text.Json.Serialization;
using System;

namespace Umbraco.Commerce.PaymentProviders.Kustom.Api.Models
{
    public class KustomHppSession
    {
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; }

        [JsonPropertyName("session_url")]
        public string SessionUrl { get; set; }

        [JsonPropertyName("distribution_url")]
        public string DistributionUrl { get; set; }

        [JsonPropertyName("redirect_url")]
        public string RedirectUrl { get; set; }

        [JsonPropertyName("qr_code_url")]
        public string QrCodeUrl { get; set; }

        [JsonPropertyName("expires_at")]
        public DateTime ExpiresAt { get; set; }
    }
}
