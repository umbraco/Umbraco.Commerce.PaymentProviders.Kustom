using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using Umbraco.Commerce.PaymentProviders.Kustom.Api.Models;

namespace Umbraco.Commerce.PaymentProviders.Kustom.Api
{
    public class KustomClient
    {
        public const string EuLiveApiUrl = "https://api.kustom.co";
        public const string NaLiveApiUrl = "https://api-na.kustom.co";
        public const string OcLiveApiUrl = "https://api-oc.kustom.co";

        public const string EuPlaygroundApiUrl = "https://api.playground.kustom.co";
        public const string NaPlaygroundApiUrl = "https://api-na.playground.kustom.co";
        public const string OcPlaygroundApiUrl = "https://api-oc.playground.kustom.co";

        private readonly KustomClientConfig _config;

        public KustomClient(KustomClientConfig config)
        {
            _config = config;
        }

        public async Task<KustomCheckoutOrder> CreateCheckoutOrderAsync(KustomCreateCheckoutOrderOptions opts, CancellationToken cancellationToken = default)
        {
            return await RequestAsync("/checkout/v3/orders", async (req, ct) => await req
                    .PostJsonAsync(opts, cancellationToken: ct)
                    .ReceiveJson<KustomCheckoutOrder>().ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<KustomHppSession> CreateHppSessionAsync(KustomCreateHppSessionOptions opts, CancellationToken cancellationToken = default)
        {
            return await RequestAsync("/hpp/v1/sessions", async (req, ct) => await req
                .PostJsonAsync(opts, cancellationToken: ct)
                .ReceiveJson<KustomHppSession>().ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<KustomCheckoutOrder> GetCheckoutOrderAsync(string orderId, CancellationToken cancellationToken = default)
        {
            return await RequestAsync($"/checkout/v3/orders/{orderId}", async (req, ct) => await req
                .GetAsync(cancellationToken: ct)
                .ReceiveJson<KustomCheckoutOrder>().ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<KustomOrder> GetOrderAsync(string orderId, CancellationToken cancellationToken = default)
        {
            return await RequestAsync($"/ordermanagement/v1/orders/{orderId}", async (req, ct) => await req
                .GetAsync(cancellationToken: ct)
                .ReceiveJson<KustomOrder>().ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        public async Task CancelOrderAsync(string orderId, CancellationToken cancellationToken = default)
        {
            await RequestAsync($"/ordermanagement/v1/orders/{orderId}/cancel", async (req, ct) => await req
                .PostAsync(null, cancellationToken: ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        public async Task CaptureOrderAsync(string orderId, KustomCaptureOptions opts, CancellationToken cancellationToken = default)
        {
            await RequestAsync($"/ordermanagement/v1/orders/{orderId}/captures", async (req, ct) => await req
                .PostJsonAsync(opts, cancellationToken: ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        public async Task RefundOrderAsync(string orderId, KustomRefundOptions opts, CancellationToken cancellationToken = default)
        {
            await RequestAsync($"/ordermanagement/v1/orders/{orderId}/refunds", async (req, ct) => await req
                .PostJsonAsync(opts, cancellationToken: ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        public KustomSessionEvent ParseSessionEvent(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            if (stream.CanSeek)
            {
                stream.Seek(0, 0);
            }

            return JsonSerializer.Deserialize<KustomSessionEvent>(stream);
        }

        /// <summary>
        /// Asynchronously parses a session event from the given stream.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="ParseSessionEvent(Stream)"/>, this overload only performs asynchronous
        /// reads, which makes it safe to use directly with an ASP.NET Core request body stream. On
        /// Kestrel/IIS with the default <c>AllowSynchronousIO = false</c>, the synchronous overload
        /// throws <see cref="InvalidOperationException"/> ("Synchronous operations are disallowed"),
        /// even when buffering is enabled, because the first read still comes from the server.
        /// Like the synchronous overload, a seekable stream is rewound before it is read.
        /// </remarks>
        /// <param name="stream">The stream containing the serialized session event JSON.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task<KustomSessionEvent?> ParseSessionEventAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(stream);

            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            return await JsonSerializer.DeserializeAsync<KustomSessionEvent>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private async Task<TResult> RequestAsync<TResult>(string url, Func<IFlurlRequest, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken = default)
        {
            FlurlRequest req = new FlurlRequest(_config.BaseUrl + url)
                .WithSettings(x => x.JsonSerializer = new CustomFlurlJsonSerializer(new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    PreferredObjectCreationHandling = JsonObjectCreationHandling.Replace,
                }))
                .WithHeader("Cache-Control", "no-cache")
                .WithBasicAuth(_config.Username, _config.Password);

            try
            {
                return await func.Invoke(req, cancellationToken).ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                var errorBody = await ex.GetResponseStringAsync().ConfigureAwait(false);
                throw new KustomApiException(ex.StatusCode ?? 0, errorBody, ex);
            }
        }
    }
}
