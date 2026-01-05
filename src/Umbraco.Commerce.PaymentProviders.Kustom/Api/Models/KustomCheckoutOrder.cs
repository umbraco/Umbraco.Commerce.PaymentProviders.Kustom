using System.Text.Json.Serialization;

namespace Umbraco.Commerce.PaymentProviders.Kustom.Api.Models;

public class KustomCheckoutOrder
{
    [JsonPropertyName("order_id")]
    public string OrderId { get; set; }
}
