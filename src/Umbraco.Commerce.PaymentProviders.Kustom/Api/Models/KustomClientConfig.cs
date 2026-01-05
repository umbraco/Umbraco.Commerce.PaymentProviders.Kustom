namespace Umbraco.Commerce.PaymentProviders.Kustom.Api.Models
{
    public abstract class KustomClientConfig
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public KustomApiRegion ApiRegion { get; set; }
        public abstract string BaseUrl { get; }

        public KustomClientConfig(string username, string password, KustomApiRegion apiRegion)
        {
            Username = username;
            Password = password;
            ApiRegion = apiRegion;
        }
    }
}
