using System;

namespace Umbraco.Commerce.PaymentProviders.Kustom.Api;

public class KustomApiException : Exception
{
    public int StatusCode { get; }
    public string ResponseBody { get; }

    public KustomApiException(int statusCode, string responseBody, Exception innerException)
        : base($"Kustom API returned {statusCode}: {responseBody}", innerException)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
