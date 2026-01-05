using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.PaymentProviders;
using Umbraco.Commerce.PaymentProviders.Kustom.Api.Models;
using Umbraco.Commerce.Extensions;

namespace Umbraco.Commerce.PaymentProviders.Kustom
{
    public abstract class KustomPaymentProviderBase<TSettings> : PaymentProviderBase<TSettings>
        where TSettings : KustomSettingsBase, new()
    {
        protected KustomPaymentProviderBase(UmbracoCommerceContext ctx)
            : base(ctx)
        { }

        public override string GetContinueUrl(PaymentProviderContext<TSettings> ctx)
        {
            ctx.Settings.MustNotBeNull("ctx.Settings");
            ctx.Settings.ContinueUrl.MustNotBeNull("ctx.Settings.ContinueUrl");

            return ctx.Settings.ContinueUrl;
        }

        public override string GetCancelUrl(PaymentProviderContext<TSettings> ctx)
        {
            ctx.Settings.MustNotBeNull("ctx.Settings");
            ctx.Settings.CancelUrl.MustNotBeNull("ctx.Settings.CancelUrl");

            return ctx.Settings.CancelUrl;
        }

        public override string GetErrorUrl(PaymentProviderContext<TSettings> ctx)
        {
            ctx.Settings.MustNotBeNull("ctx.Settings");
            ctx.Settings.ErrorUrl.MustNotBeNull("ctx.Settings.ErrorUrl");

            return ctx.Settings.ErrorUrl;
        }

        protected KustomClientConfig GetKustomClientConfig(KustomSettingsBase settings)
        {
            if (!settings.TestMode)
            {
                return new LiveKustomClientConfig(
                    settings.LiveApiUsername,
                    settings.LiveApiPassword,
                    settings.ApiRegion);
            }
            else
            {
                return new PlaygroundKustomClientConfig(
                    settings.TestApiUsername,
                    settings.TestApiPassword,
                    settings.ApiRegion);
            }
        }

        public PaymentStatus GetPaymentStatus(KustomOrder order)
        {
            var status = PaymentStatus.Authorized;

            switch (order.Status)
            {
                case KustomOrder.Statuses.CANCELLED:
                case KustomOrder.Statuses.EXPIRED:
                    status = PaymentStatus.Cancelled;
                    break;

                case KustomOrder.Statuses.CAPTURED:
                case KustomOrder.Statuses.PART_CAPTURED:
                    if (order.RefundedAmount > 0)
                    {
                        status = PaymentStatus.Refunded;
                    }
                    else
                    {
                        status = PaymentStatus.Captured;
                    }

                    break;

                case KustomOrder.Statuses.REFUNDED:
                    status = PaymentStatus.Refunded;
                    break;

                case KustomOrder.Statuses.CLOSED:
                    status = PaymentStatus.Error;
                    break;
            }

            return status;
        }

        protected string AppendQueryString(string url, string qs)
        {
            return url + (url.Contains("?") ? "&" : "?") + qs;
        }

        protected string AppendQueryStringParam(string url, string key, string value)
        {
            return url + (url.Contains("?") ? "&" : "?") + key + "=" + value;
        }
    }
}
