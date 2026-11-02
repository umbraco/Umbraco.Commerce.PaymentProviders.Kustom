using System.Text;
using System.Text.Json;
using FluentAssertions;
using Flurl.Http.Testing;
using Umbraco.Commerce.PaymentProviders.Kustom.Api;
using Umbraco.Commerce.PaymentProviders.Kustom.Api.Models;

namespace Umbraco.Commerce.PaymentProviders.Kustom.UnitTests
{
    // Every KustomClient call is run against Flurl's HttpTest, so no request leaves the process.
    public sealed class KustomClientTests : IDisposable
    {
        private readonly HttpTest _httpTest = new();
        private readonly KustomClient _client = new(new PlaygroundKustomClientConfig("user", "pass", KustomApiRegion.Europe));

        private static readonly string BasicAuth = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:pass"));

        public void Dispose()
        {
            _httpTest.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task CreateCheckoutOrder_Posts_Snake_Case_Body_Without_Nulls()
        {
            _httpTest.RespondWith("""{"order_id":"c1","status":"checkout_incomplete"}""");

            KustomCheckoutOrder checkoutOrder = await _client.CreateCheckoutOrderAsync(new KustomCreateCheckoutOrderOptions
            {
                MerchantReference1 = "ORDER-1",
                PurchaseCountry = "SE",
                PurchaseCurrency = "SEK",
                Locale = "sv-SE",
                OrderAmount = 12500,
                OrderTaxAmount = 2500,
                OrderLines =
                [
                    new KustomOrderLine { Reference = "SKU1", Name = "Thing", TaxRate = 2500, UnitPrice = 12500, Quantity = 1, TotalAmount = 12500, TotalTaxAmount = 2500 },
                ],
                BillingAddress = new KustomAddress { GivenName = "Jane", Country = "SE" },
                MerchantUrls = new KustomCheckoutOrderMerchantUrls
                {
                    Terms = "https://shop.example.com/terms",
                    Checkout = "https://shop.example.com/checkout",
                    Confirmation = "https://shop.example.com/confirmation",
                    Push = "https://shop.example.com/callback",
                },
            });

            checkoutOrder.OrderId.Should().Be("c1");
            checkoutOrder.Status.Should().Be(KustomCheckoutOrder.Statuses.CHECKOUT_INCOMPLETE);
            _httpTest.ShouldHaveCalled($"{KustomClient.EuPlaygroundApiUrl}/checkout/v3/orders")
                .WithVerb(HttpMethod.Post)
                .WithHeader("Authorization", BasicAuth)
                .WithHeader("Cache-Control", "no-cache")
                .Times(1);

            using JsonDocument body = JsonDocument.Parse(_httpTest.CallLog[0].RequestBody!);
            JsonElement root = body.RootElement;
            root.GetProperty("merchant_reference1").GetString().Should().Be("ORDER-1");
            root.GetProperty("purchase_country").GetString().Should().Be("SE");
            root.GetProperty("purchase_currency").GetString().Should().Be("SEK");
            root.GetProperty("order_amount").GetInt32().Should().Be(12500);
            root.GetProperty("order_tax_amount").GetInt32().Should().Be(2500);
            root.TryGetProperty("merchant_reference2", out _).Should().BeFalse("null values are not sent");
            root.TryGetProperty("shipping_address", out _).Should().BeFalse("null values are not sent");

            JsonElement line = root.GetProperty("order_lines")[0];
            line.GetProperty("reference").GetString().Should().Be("SKU1");
            line.GetProperty("tax_rate").GetInt32().Should().Be(2500);
            line.GetProperty("total_amount").GetInt32().Should().Be(12500);
            line.TryGetProperty("total_discount_amount", out _).Should().BeFalse("null values are not sent");
            line.TryGetProperty("type", out _).Should().BeFalse("null values are not sent");

            JsonElement address = root.GetProperty("billing_address");
            address.GetProperty("given_name").GetString().Should().Be("Jane");
            address.GetProperty("country").GetString().Should().Be("SE");

            JsonElement urls = root.GetProperty("merchant_urls");
            urls.GetProperty("terms").GetString().Should().Be("https://shop.example.com/terms");
            urls.GetProperty("checkout").GetString().Should().Be("https://shop.example.com/checkout");
            urls.GetProperty("confirmation").GetString().Should().Be("https://shop.example.com/confirmation");
            urls.GetProperty("push").GetString().Should().Be("https://shop.example.com/callback");
            urls.TryGetProperty("validation", out _).Should().BeFalse("null values are not sent");
        }

        [Fact]
        public async Task GetCheckoutOrder_Reads_Status()
        {
            _httpTest.RespondWith("""{"order_id":"c1","status":"checkout_complete"}""");

            KustomCheckoutOrder checkoutOrder = await _client.GetCheckoutOrderAsync("c1");

            _httpTest.ShouldHaveCalled($"{KustomClient.EuPlaygroundApiUrl}/checkout/v3/orders/c1").WithVerb(HttpMethod.Get).Times(1);
            checkoutOrder.Status.Should().Be(KustomCheckoutOrder.Statuses.CHECKOUT_COMPLETE);
        }

        [Fact]
        public async Task Api_Error_Becomes_KustomApiException_With_Status_And_Body()
        {
            _httpTest.RespondWith("""{"error_code":"BAD_VALUE","error_messages":["Bad value: order_lines"]}""", 400);

            Func<Task> act = () => _client.CaptureOrderAsync("o1", new KustomCaptureOptions { CapturedAmount = 100 });

            KustomApiException ex = (await act.Should().ThrowAsync<KustomApiException>()).Which;
            ex.StatusCode.Should().Be(400);
            ex.ResponseBody.Should().Contain("BAD_VALUE");
            ex.Message.Should().StartWith("Kustom API returned 400:");
        }

        [Fact]
        public async Task CreateHppSession_Posts_Options_And_Merchant_Urls()
        {
            _httpTest.RespondWith("""{"session_id":"h1","redirect_url":"https://pay.kustom.com/eu/hpp/payments/abc"}""");

            KustomHppSession session = await _client.CreateHppSessionAsync(new KustomCreateHppSessionOptions
            {
                PaymentSessionUrl = $"{KustomClient.EuPlaygroundApiUrl}/checkout/v3/orders/c1",
                Options = new KustomHppOptions
                {
                    PlaceOrderMode = KustomHppOptions.PlaceOrderModes.CAPTURE_ORDER,
                    PaymentMethodCategories = ["pay_later", "pay_over_time"],
                    PaymentFallback = true,
                },
                MerchantUrls = new KustomHppMerchantUrls
                {
                    Success = "https://shop.example.com/continue",
                    Cancel = "https://shop.example.com/cancel?reason=cancel",
                    StatusUpdate = "https://shop.example.com/callback?sid={{session_id}}&token=abc",
                },
            });

            session.SessionId.Should().Be("h1");
            session.RedirectUrl.Should().Be("https://pay.kustom.com/eu/hpp/payments/abc");
            _httpTest.ShouldHaveCalled($"{KustomClient.EuPlaygroundApiUrl}/hpp/v1/sessions").WithVerb(HttpMethod.Post).Times(1);

            using JsonDocument body = JsonDocument.Parse(_httpTest.CallLog[0].RequestBody!);
            JsonElement root = body.RootElement;
            root.GetProperty("payment_session_url").GetString().Should().EndWith("/checkout/v3/orders/c1");
            root.GetProperty("options").GetProperty("place_order_mode").GetString().Should().Be("CAPTURE_ORDER");
            root.GetProperty("options").GetProperty("payment_method_categories").GetArrayLength().Should().Be(2);
            root.GetProperty("options").GetProperty("payment_fallback").GetBoolean().Should().BeTrue();
            root.GetProperty("options").TryGetProperty("logo_url", out _).Should().BeFalse("null values are not sent");
            root.GetProperty("merchant_urls").GetProperty("status_update").GetString()
                .Should().Be("https://shop.example.com/callback?sid={{session_id}}&token=abc", "Kustom fills in the {{session_id}} placeholder");
        }

        [Fact]
        public async Task GetOrder_Reads_Amounts_And_Status()
        {
            _httpTest.RespondWith("""{"order_id":"o1","status":"CAPTURED","original_order_amount":12500,"captured_amount":12500,"refunded_amount":500,"kustom_reference":"K-REF"}""");

            KustomOrder order = await _client.GetOrderAsync("o1");

            _httpTest.ShouldHaveCalled($"{KustomClient.EuPlaygroundApiUrl}/ordermanagement/v1/orders/o1").WithVerb(HttpMethod.Get).Times(1);
            order.OrderId.Should().Be("o1");
            order.Status.Should().Be(KustomOrder.Statuses.CAPTURED);
            order.OriginalOrderAmount.Should().Be(12500);
            order.CapturedAmount.Should().Be(12500);
            order.RefundedAmount.Should().Be(500);
            order.KustomReference.Should().Be("K-REF");
        }

        [Fact]
        public async Task CaptureOrder_Posts_Captured_Amount()
        {
            await _client.CaptureOrderAsync("o1", new KustomCaptureOptions { CapturedAmount = 9900, Description = "Capture Order ORDER-1" });

            _httpTest.ShouldHaveCalled($"{KustomClient.EuPlaygroundApiUrl}/ordermanagement/v1/orders/o1/captures")
                .WithVerb(HttpMethod.Post)
                .WithRequestBody("*\"captured_amount\":9900*")
                .WithRequestBody("*\"description\":\"Capture Order ORDER-1\"*")
                .Times(1);
            _httpTest.CallLog[0].RequestBody.Should().NotContain("\"reference\"", "null values are not sent");
        }

        [Fact]
        public async Task RefundOrder_Posts_Refunded_Amount()
        {
            await _client.RefundOrderAsync("o1", new KustomRefundOptions { RefundAmount = 2500, Description = "Refund Order ORDER-1" });

            _httpTest.ShouldHaveCalled($"{KustomClient.EuPlaygroundApiUrl}/ordermanagement/v1/orders/o1/refunds")
                .WithVerb(HttpMethod.Post)
                .WithRequestBody("*\"refunded_amount\":2500*")
                .Times(1);
        }

        [Fact]
        public async Task CancelOrder_Posts_To_Cancel()
        {
            await _client.CancelOrderAsync("o1");

            _httpTest.ShouldHaveCalled($"{KustomClient.EuPlaygroundApiUrl}/ordermanagement/v1/orders/o1/cancel")
                .WithVerb(HttpMethod.Post)
                .WithHeader("Authorization", BasicAuth)
                .Times(1);
        }

        [Fact]
        public void ParseSessionEvent_Rewinds_And_Reads_Session()
        {
            using MemoryStream stream = new(Encoding.UTF8.GetBytes("""{"event_id":"e1","session":{"session_id":"s1","status":"COMPLETED","order_id":"o1","kustom_reference":"K-REF"}}"""));
            stream.Seek(0, SeekOrigin.End);

            KustomSessionEvent evt = _client.ParseSessionEvent(stream);

            evt.EventId.Should().Be("e1");
            evt.Session.SessionId.Should().Be("s1");
            evt.Session.Status.Should().Be(KustomSession.Statuses.COMPLETED);
            evt.Session.OrderId.Should().Be("o1");
            evt.Session.KustomReference.Should().Be("K-REF");
        }
    }
}
