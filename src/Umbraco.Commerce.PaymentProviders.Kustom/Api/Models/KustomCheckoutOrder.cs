using System.Text.Json.Serialization;

namespace Umbraco.Commerce.PaymentProviders.Kustom.Api.Models;

public class KustomCheckoutOrder
{
    [JsonPropertyName("order_id")]
    public string OrderId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    public static class Statuses
    {
        public const string CHECKOUT_INCOMPLETE = "checkout_incomplete";
        public const string CHECKOUT_COMPLETE = "checkout_complete";
    }
}
