namespace Umbraco.Commerce.PaymentProviders.Kustom.Api.Models
{
    public class LiveKustomClientConfig : KustomClientConfig
    {
        public override string BaseUrl
        {
            get
            {
                if (ApiRegion == KustomApiRegion.Europe)
                    return KustomClient.EuLiveApiUrl;

                if (ApiRegion == KustomApiRegion.NorthAmerica)
                    return KustomClient.NaLiveApiUrl;

                if (ApiRegion == KustomApiRegion.Oceania)
                    return KustomClient.OcLiveApiUrl;

                return null;
            }
        }

        public LiveKustomClientConfig(string username, string password, KustomApiRegion apiRegion)
            : base(username, password, apiRegion)
        { }
    }
}
