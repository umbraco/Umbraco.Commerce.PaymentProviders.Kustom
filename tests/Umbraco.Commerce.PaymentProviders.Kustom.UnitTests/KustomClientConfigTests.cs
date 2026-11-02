using FluentAssertions;
using Umbraco.Commerce.PaymentProviders.Kustom.Api;
using Umbraco.Commerce.PaymentProviders.Kustom.Api.Models;

namespace Umbraco.Commerce.PaymentProviders.Kustom.UnitTests
{
    public class KustomClientConfigTests
    {
        [Theory]
        [InlineData(KustomApiRegion.Europe, KustomClient.EuLiveApiUrl)]
        [InlineData(KustomApiRegion.NorthAmerica, KustomClient.NaLiveApiUrl)]
        [InlineData(KustomApiRegion.Oceania, KustomClient.OcLiveApiUrl)]
        public void Live_Config_Uses_Region_Url(KustomApiRegion region, string expected)
        {
            new LiveKustomClientConfig("u", "p", region).BaseUrl.Should().Be(expected);
        }

        [Theory]
        [InlineData(KustomApiRegion.Europe, KustomClient.EuPlaygroundApiUrl)]
        [InlineData(KustomApiRegion.NorthAmerica, KustomClient.NaPlaygroundApiUrl)]
        [InlineData(KustomApiRegion.Oceania, KustomClient.OcPlaygroundApiUrl)]
        public void Playground_Config_Uses_Region_Url(KustomApiRegion region, string expected)
        {
            new PlaygroundKustomClientConfig("u", "p", region).BaseUrl.Should().Be(expected);
        }

        [Theory]
        [InlineData(true, typeof(PlaygroundKustomClientConfig), "test-user", "test-pass")]
        [InlineData(false, typeof(LiveKustomClientConfig), "live-user", "live-pass")]
        public void Provider_Picks_Credentials_By_Test_Mode(bool testMode, Type expectedType, string expectedUser, string expectedPassword)
        {
            KustomHppSettings settings = new()
            {
                TestMode = testMode,
                TestApiUsername = "test-user",
                TestApiPassword = "test-pass",
                LiveApiUsername = "live-user",
                LiveApiPassword = "live-pass",
                ApiRegion = KustomApiRegion.NorthAmerica,
            };

            KustomClientConfig config = new TestableProvider().ClientConfig(settings);

            config.Should().BeOfType(expectedType);
            config.Username.Should().Be(expectedUser);
            config.Password.Should().Be(expectedPassword);
            config.ApiRegion.Should().Be(KustomApiRegion.NorthAmerica);
        }

        [Theory]
        [InlineData("https://shop.example.com/cancel", "reason=back", "https://shop.example.com/cancel?reason=back")]
        [InlineData("https://shop.example.com/cancel?x=1", "reason=back", "https://shop.example.com/cancel?x=1&reason=back")]
        public void AppendQueryString_Picks_Separator(string url, string qs, string expected)
        {
            new TestableProvider().Append(url, qs).Should().Be(expected);
        }

        private sealed class TestableProvider : KustomHppPaymentProvider
        {
            public TestableProvider()
                : base(null!, null!)
            {
            }

            public KustomClientConfig ClientConfig(KustomSettingsBase settings) => GetKustomClientConfig(settings);

            public string Append(string url, string qs) => AppendQueryString(url, qs);
        }
    }
}
