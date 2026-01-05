namespace Umbraco.Commerce.PaymentProviders.Kustom.Api.Models
{
    public class PlaygroundKustomClientConfig : KustomClientConfig
    {
        public override string BaseUrl
        {
            get
            {
                if (ApiRegion == KustomApiRegion.Europe)
                    return KustomClient.EuPlaygroundApiUrl;

                if (ApiRegion == KustomApiRegion.NorthAmerica)
                    return KustomClient.NaPlaygroundApiUrl;

                if (ApiRegion == KustomApiRegion.Oceania)
                    return KustomClient.OcPlaygroundApiUrl;

                return null;
            }
        }

        public PlaygroundKustomClientConfig(string username, string password, KustomApiRegion apiRegion)
            : base(username, password, apiRegion)
        { }
    }
}
