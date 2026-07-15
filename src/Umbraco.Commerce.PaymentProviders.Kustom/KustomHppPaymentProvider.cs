using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Umbraco.Commerce.Common.Logging;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Core.PaymentProviders;
using Umbraco.Commerce.Extensions;
using Umbraco.Commerce.PaymentProviders.Kustom.Api;
using Umbraco.Commerce.PaymentProviders.Kustom.Api.Models;

namespace Umbraco.Commerce.PaymentProviders.Kustom
{
    [PaymentProvider("kustom-hpp")]
    public class KustomHppPaymentProvider(
        UmbracoCommerceContext ctx,
        ILogger<KustomHppPaymentProvider> logger)
        : KustomPaymentProviderBase<KustomHppSettings>(ctx)
    {
        public override bool CanFetchPaymentStatus => true;
        public override bool CanCancelPayments => true;
        public override bool CanCapturePayments => true;
        public override bool CanRefundPayments => true;
        public override bool CanPartiallyRefundPayments => true;

        public override IEnumerable<TransactionMetaDataDefinition> TransactionMetaDataDefinitions =>
        [
            new("kustomSessionId"),
            new("kustomOrderId"),
            new("kustomReference"),
        ];

        public override string GetCancelUrl(PaymentProviderContext<KustomHppSettings> ctx)
        {
            ctx.Settings.MustNotBeNull("ctx.Settings");
            ctx.Settings.CancelUrl.MustNotBeNull("ctx.Settings.CancelUrl");

            var cancelUrl = ctx.Settings.CancelUrl;

            if (ctx.HttpContext.Request != null)
            {
                IQueryCollection qs = ctx.HttpContext.Request.Query;

                StringValues reason = qs["reason"];

                cancelUrl = AppendQueryStringParam(cancelUrl, "reason", reason);

                if (!string.IsNullOrWhiteSpace(ctx.Settings.ErrorUrl) && (reason == "failure" || reason == "error"))
                {
                    return AppendQueryStringParam(ctx.Settings.ErrorUrl, "reason", reason);
                }
            }

            return cancelUrl;
        }

        public override async Task<PaymentFormResult> GenerateFormAsync(PaymentProviderContext<KustomHppSettings> ctx, CancellationToken cancellationToken = default)
        {
            // Ensure payment method is specified
            if (!ctx.Order.PaymentInfo.PaymentMethodId.HasValue)
            {
                throw new InvalidOperationException("Payment method is required to process the payment.");
            }

            // Ensure billing country is specified
            if (!ctx.Order.PaymentInfo.CountryId.HasValue)
            {
                throw new InvalidOperationException("Billing country is required to process the payment.");
            }

            // Get country information
            var billingCountry = await Context.Services.CountryService.GetCountryAsync(ctx.Order.PaymentInfo.CountryId.Value);
            var billingCountryCode = billingCountry.Code.ToUpperInvariant();

            // Ensure billing country has valid ISO 3166 code
            var iso3166Countries = await Context.Services.CountryService.GetIso3166CountryRegionsAsync();
            if (iso3166Countries.All(x => x.Code != billingCountryCode))
            {
                throw new InvalidOperationException("Country must be a valid ISO 3166 billing country code: " + billingCountry.Name);
            }

            // Get currency information
            var currency = await Context.Services.CurrencyService.GetCurrencyAsync(ctx.Order.CurrencyId);
            var currencyCode = currency.Code.ToUpperInvariant();

            // Ensure currency has valid ISO 4217 code
            if (!Iso4217.CurrencyCodes.ContainsKey(currencyCode))
            {
                throw new InvalidOperationException("Currency must be a valid ISO 4217 currency code: " + currency.Name);
            }

            var kustomSecretToken = Guid.NewGuid().ToString("N");
            var clientConfig = GetKustomClientConfig(ctx.Settings);
            var client = new KustomClient(clientConfig);

            // Prepare ctx.Order lines
            // NB: We add ctx.Order lines without any discounts applied as we'll then add
            // one global discount amount at the end. This is just the easiest way to
            // allow everything to add up and successfully validate at the Kustom end.
            var orderLines = ctx.Order.OrderLines.Select(orderLine => new KustomOrderLine
            {
                Reference = orderLine.Sku,
                Name = orderLine.Name,
                Type = !string.IsNullOrWhiteSpace(ctx.Settings.ProductTypePropertyAlias) && orderLine.Properties.TryGetValue(ctx.Settings.ProductTypePropertyAlias, out PropertyValue? property)
                    ? property?.Value
                    : null,
                TaxRate = (int)(orderLine.TaxRate.Value * 10000),
                UnitPrice = (int)AmountToMinorUnits(orderLine.UnitPrice.WithoutAdjustments.WithTax),
                Quantity = (int)orderLine.Quantity,
                TotalAmount = (int)AmountToMinorUnits(orderLine.TotalPrice.WithoutAdjustments.WithTax),
                TotalTaxAmount = (int)AmountToMinorUnits(orderLine.TotalPrice.WithoutAdjustments.Tax)
            }).ToList();

            // Add shipping method fee ctx.OrderLine
            if (ctx.Order.ShippingInfo.ShippingMethodId.HasValue && ctx.Order.ShippingInfo.TotalPrice.WithoutAdjustments.WithTax > 0)
            {
                var shippingMethod = await Context.Services.ShippingMethodService.GetShippingMethodAsync(ctx.Order.ShippingInfo.ShippingMethodId.Value);

                orderLines.Add(new KustomOrderLine
                {
                    Reference = shippingMethod.Sku,
                    Name = shippingMethod.Name + " Fee",
                    Type = KustomOrderLine.Types.SHIPPING_FEE,
                    TaxRate = (int)(ctx.Order.ShippingInfo.TaxRate * 10000),
                    UnitPrice = (int)AmountToMinorUnits(ctx.Order.ShippingInfo.TotalPrice.WithoutAdjustments.WithTax),
                    Quantity = 1,
                    TotalAmount = (int)AmountToMinorUnits(ctx.Order.ShippingInfo.TotalPrice.WithoutAdjustments.WithTax),
                    TotalTaxAmount = (int)AmountToMinorUnits(ctx.Order.ShippingInfo.TotalPrice.WithoutAdjustments.Tax),
                });
            }

            // Add payment method fee (as surcharge) ctx.Orderline
            if (ctx.Order.PaymentInfo.TotalPrice.Value.WithTax > 0)
            {
                var paymentMethod = await Context.Services.PaymentMethodService.GetPaymentMethodAsync(ctx.Order.PaymentInfo.PaymentMethodId.Value);

                orderLines.Add(new KustomOrderLine
                {
                    Reference = paymentMethod.Sku,
                    Name = paymentMethod.Name + " Fee",
                    Type = KustomOrderLine.Types.SURCHARGE,
                    TaxRate = (int)(ctx.Order.PaymentInfo.TaxRate * 10000),
                    UnitPrice = (int)AmountToMinorUnits(ctx.Order.PaymentInfo.TotalPrice.WithoutAdjustments.WithTax),
                    Quantity = 1,
                    TotalAmount = (int)AmountToMinorUnits(ctx.Order.PaymentInfo.TotalPrice.WithoutAdjustments.WithTax),
                    TotalTaxAmount = (int)AmountToMinorUnits(ctx.Order.PaymentInfo.TotalPrice.WithoutAdjustments.Tax),
                });
            }

            // Add any discounts
            if (ctx.Order.TotalPrice.TotalAdjustment < 0)
            {
                // Derive the tax rate from the discount's own tax vs net amounts rather than
                // the order's blended rate, so tax_rate and total_tax_amount stay consistent
                // (e.g. a discount applied to a 0% VAT line must report tax_rate 0). Fixes #826.
                var discountWithTax = AmountToMinorUnits(ctx.Order.TotalPrice.TotalAdjustment.WithTax);
                var discountTax = AmountToMinorUnits(ctx.Order.TotalPrice.TotalAdjustment.Tax);
                var discountNet = discountWithTax - discountTax;

                orderLines.Add(new KustomOrderLine
                {
                    Reference = "DISCOUNT",
                    Name = "Discounts",
                    Type = KustomOrderLine.Types.DISCOUNT,
                    TaxRate = discountNet != 0 ? (int)Math.Round((decimal)discountTax / discountNet * 10000) : 0,
                    UnitPrice = 0,
                    Quantity = 1,
                    TotalDiscountAmount = (int)discountWithTax * -1,
                    TotalAmount = (int)discountWithTax,
                    TotalTaxAmount = (int)discountTax,
                });
            }
            else if (ctx.Order.TotalPrice.TotalAdjustment > 0)
            {
                // Derive the tax rate from the fee's own tax vs net amounts rather than
                // the order's blended rate, so tax_rate and total_tax_amount stay consistent. Fixes #826.
                var feeWithTax = AmountToMinorUnits(ctx.Order.TotalPrice.TotalAdjustment.WithTax);
                var feeTax = AmountToMinorUnits(ctx.Order.TotalPrice.TotalAdjustment.Tax);
                var feeNet = feeWithTax - feeTax;

                orderLines.Add(new KustomOrderLine
                {
                    Reference = "FEE",
                    Name = "Additional Fees",
                    Type = KustomOrderLine.Types.SURCHARGE,
                    TaxRate = feeNet != 0 ? (int)Math.Round((decimal)feeTax / feeNet * 10000) : 0,
                    UnitPrice = 0,
                    Quantity = 1,
                    TotalAmount = (int)feeWithTax,
                    TotalTaxAmount = (int)feeTax,
                });
            }

            // Add gift cards
            if (ctx.Order.TransactionAmount.Adjustment.Value < 0)
            {
                foreach (GiftCardAdjustment giftCard in ctx.Order.TransactionAmount.Adjustments.OfType<GiftCardAdjustment>())
                {
                    orderLines.Add(new KustomOrderLine
                    {
                        Reference = "Gift Card " + giftCard.GiftCardCode,
                        Name = "Discounts",
                        Type = KustomOrderLine.Types.GIFT_CARD,
                        TaxRate = (int)(ctx.Order.TaxRate * 10000),
                        UnitPrice = 0,
                        Quantity = 1,
                        TotalDiscountAmount = (int)AmountToMinorUnits(giftCard.Amount) * -1,
                        TotalAmount = (int)AmountToMinorUnits(giftCard.Amount),
                        TotalTaxAmount = 0,
                    });
                }
            }

            // Create a checkout order using the Kustom Checkout API
            var resp1 = await client.CreateCheckoutOrderAsync(
                new KustomCreateCheckoutOrderOptions
                {
                    MerchantReference1 = ctx.Order.OrderNumber,
                    PurchaseCountry = billingCountryCode,
                    PurchaseCurrency = currencyCode,
                    Locale = ctx.Order.LanguageIsoCode, // TODO: Validate?

                    OrderLines = orderLines,
                    OrderAmount = (int)AmountToMinorUnits(ctx.Order.TransactionAmount.Value),
                    OrderTaxAmount = (int)AmountToMinorUnits(ctx.Order.TotalPrice.Value.Tax),

                    BillingAddress = new KustomAddress
                    {
                        GivenName = ctx.Order.CustomerInfo.FirstName,
                        FamilyName = ctx.Order.CustomerInfo.LastName,
                        Email = ctx.Order.CustomerInfo.Email,
                        StreetAddress = !string.IsNullOrWhiteSpace(ctx.Settings.BillingAddressLine1PropertyAlias)
                            ? ctx.Order.Properties[ctx.Settings.BillingAddressLine1PropertyAlias]?.Value : null,
                        StreetAddress2 = !string.IsNullOrWhiteSpace(ctx.Settings.BillingAddressLine2PropertyAlias)
                            ? ctx.Order.Properties[ctx.Settings.BillingAddressLine2PropertyAlias]?.Value : null,
                        City = !string.IsNullOrWhiteSpace(ctx.Settings.BillingAddressCityPropertyAlias)
                            ? ctx.Order.Properties[ctx.Settings.BillingAddressCityPropertyAlias]?.Value : null,
                        Region = !string.IsNullOrWhiteSpace(ctx.Settings.BillingAddressStatePropertyAlias)
                            ? ctx.Order.Properties[ctx.Settings.BillingAddressStatePropertyAlias]?.Value : null,
                        PostalCode = !string.IsNullOrWhiteSpace(ctx.Settings.BillingAddressZipCodePropertyAlias)
                            ? ctx.Order.Properties[ctx.Settings.BillingAddressZipCodePropertyAlias]?.Value : null,
                        Country = billingCountryCode
                    },

                    MerchantUrls = new KustomCheckoutOrderMerchantUrls
                    {
                        Terms = new Uri(new Uri(ctx.Urls.CancelUrl), !string.IsNullOrWhiteSpace(ctx.Settings.TermsUrl) ? ctx.Settings.TermsUrl : "/terms").ToString(),
                        Checkout = ctx.Settings.CancelUrl,
                        Confirmation = ctx.Settings.ContinueUrl,
                        Push = ctx.Urls.CallbackUrl
                    }
                },
                cancellationToken).ConfigureAwait(false);

            // Create a HPP session
            var resp2 = await client.CreateHppSessionAsync(
                new KustomCreateHppSessionOptions
                {
                    PaymentSessionUrl = $"{clientConfig.BaseUrl}/checkout/v3/orders/{resp1.OrderId}",
                    Options = new KustomHppOptions
                    {
                        PlaceOrderMode = ctx.Settings.Capture
                            ? KustomHppOptions.PlaceOrderModes.CAPTURE_ORDER
                            : KustomHppOptions.PlaceOrderModes.PLACE_ORDER,
                        LogoUrl = !string.IsNullOrWhiteSpace(ctx.Settings.PaymentPageLogoUrl)
                            ? ctx.Settings.PaymentPageLogoUrl.Trim()
                            : null,
                        PageTitle = !string.IsNullOrWhiteSpace(ctx.Settings.PaymentPagePageTitle)
                            ? ctx.Settings.PaymentPagePageTitle.Trim()
                            : null,
                        PaymentMethodCategories = !string.IsNullOrWhiteSpace(ctx.Settings.PaymentMethodCategories)
                            ? ctx.Settings.PaymentMethodCategories.Split([','], StringSplitOptions.RemoveEmptyEntries)
                                .Select(x => x.Trim())
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .ToArray()
                            : null,
                        PaymentMethodCategory = !string.IsNullOrWhiteSpace(ctx.Settings.PaymentMethodCategory)
                            ? ctx.Settings.PaymentMethodCategory.Trim()
                            : null,
                        PaymentFallback = ctx.Settings.EnableFallbacks
                    },
                    MerchantUrls = new KustomHppMerchantUrls
                    {
                        Success = ctx.Urls.ContinueUrl,
                        Cancel = AppendQueryString(ctx.Urls.CancelUrl, "reason=cancel"),
                        Back = AppendQueryString(ctx.Urls.CancelUrl, "reason=back"),
                        Failure = AppendQueryString(ctx.Urls.CancelUrl, "reason=failure"),
                        Error = AppendQueryString(ctx.Urls.CancelUrl, "reason=error"),
                        StatusUpdate = AppendQueryString(ctx.Urls.CallbackUrl, "sid={{session_id}}&token=" + kustomSecretToken),
                    }
                },
                cancellationToken).ConfigureAwait(false);

            return new PaymentFormResult()
            {
                Form = new PaymentForm(resp2.RedirectUrl, PaymentFormMethod.Get),
                MetaData = new Dictionary<string, string>
                {
                    { "kustomSessionId", resp2.SessionId },
                    { "kustomSecretToken", kustomSecretToken }
                }
            };
        }

        public override async Task<CallbackResult> ProcessCallbackAsync(PaymentProviderContext<KustomHppSettings> ctx, CancellationToken cancellationToken = default)
        {
            IQueryCollection qs = ctx.HttpContext.Request.Query;
            StringValues sessionId = qs["sid"];
            StringValues token = qs["token"];

            if (!string.IsNullOrWhiteSpace(sessionId) && ctx.Order.Properties["kustomSessionId"] == sessionId
                && !string.IsNullOrWhiteSpace(token) && ctx.Order.Properties["kustomSecretToken"] == token)
            {
                var clientConfig = GetKustomClientConfig(ctx.Settings);
                var client = new KustomClient(clientConfig);

                using (Stream stream = ctx.HttpContext.Request.Body)
                {
                    KustomSessionEvent evt = client.ParseSessionEvent(stream);
                    if (evt != null && evt.Session.Status == KustomSession.Statuses.COMPLETED)
                    {
                        KustomOrder kustomOrder = await client.GetOrderAsync(evt.Session.OrderId, cancellationToken).ConfigureAwait(false);

                        return new CallbackResult
                        {
                            TransactionInfo = new TransactionInfo
                            {
                                AmountAuthorized = AmountFromMinorUnits(kustomOrder.OriginalOrderAmount),
                                TransactionFee = 0m,
                                TransactionId = kustomOrder.OrderId,
                                PaymentStatus = GetPaymentStatus(kustomOrder)
                            },
                            MetaData = new Dictionary<string, string>
                            {
                                { "kustomOrderId", evt.Session.OrderId },
                                { "kustomReference", evt.Session.KustomReference },
                            },
                        };
                    }
                }
            }

            return CallbackResult.Ok();
        }

        public override async Task<ApiResult> FetchPaymentStatusAsync(PaymentProviderContext<KustomHppSettings> ctx, CancellationToken cancellationToken = default)
        {
            try
            {
                var orderId = ctx.Order.TransactionInfo.TransactionId;

                var clientConfig = GetKustomClientConfig(ctx.Settings);
                var client = new KustomClient(clientConfig);

                var kustomOrder = await client.GetOrderAsync(orderId, cancellationToken).ConfigureAwait(false);
                if (kustomOrder != null)
                {
                    return new ApiResult
                    {
                        TransactionInfo = new TransactionInfoUpdate
                        {
                            TransactionId = kustomOrder.OrderId,
                            PaymentStatus = GetPaymentStatus(kustomOrder)
                        }
                    };
                }

            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching Kustom payment status for ctx.Order {OrderNumber}", ctx.Order.OrderNumber);
            }

            return ApiResult.Empty;
        }

        public override async Task<ApiResult> CapturePaymentAsync(PaymentProviderContext<KustomHppSettings> ctx, CancellationToken cancellationToken = default)
        {
            try
            {
                var orderId = ctx.Order.TransactionInfo.TransactionId;

                var clientConfig = GetKustomClientConfig(ctx.Settings);
                var client = new KustomClient(clientConfig);

                await client.CaptureOrderAsync(orderId, new KustomCaptureOptions
                {
                    Description = $"Capture Order {ctx.Order.OrderNumber}",
                    CapturedAmount = (int)AmountToMinorUnits(ctx.Order.TransactionInfo.AmountAuthorized.Value)
                }, cancellationToken).ConfigureAwait(false);

                return new ApiResult
                {
                    TransactionInfo = new TransactionInfoUpdate
                    {
                        TransactionId = orderId,
                        PaymentStatus = PaymentStatus.Captured
                    }
                };

            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error capturing Kustom payment for ctx.Order {OrderNumber}", ctx.Order.OrderNumber);
            }

            return ApiResult.Empty;
        }

        public override async Task<ApiResult?> RefundPaymentAsync(
            PaymentProviderContext<KustomHppSettings> context,
            PaymentProviderOrderRefundRequest refundRequest,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(refundRequest);

            try
            {
                string orderId = context.Order.TransactionInfo.TransactionId;

                KustomClientConfig clientConfig = GetKustomClientConfig(context.Settings);
                KustomClient client = new(clientConfig);

                await client.RefundOrderAsync(
                    orderId,
                    new KustomRefundOptions
                    {
                        Description = $"Refund Order {context.Order.OrderNumber}",
                        RefundAmount = (int)AmountToMinorUnits(refundRequest.RefundAmount),
                    },
                    cancellationToken).ConfigureAwait(false);

                return new ApiResult
                {
                    TransactionInfo = new TransactionInfoUpdate
                    {
                        TransactionId = orderId,
                        PaymentStatus = PaymentStatus.Refunded
                    },
                };

            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error refunding Kustom payment for ctx.Order {OrderNumber}", context.Order.OrderNumber);
            }

            return ApiResult.Empty;
        }


        public override async Task<ApiResult> CancelPaymentAsync(PaymentProviderContext<KustomHppSettings> ctx, CancellationToken cancellationToken = default)
        {
            try
            {
                var orderId = ctx.Order.TransactionInfo.TransactionId;

                var clientConfig = GetKustomClientConfig(ctx.Settings);
                var client = new KustomClient(clientConfig);

                await client.CancelOrderAsync(orderId, cancellationToken).ConfigureAwait(false);

                return new ApiResult
                {
                    TransactionInfo = new TransactionInfoUpdate
                    {
                        TransactionId = orderId,
                        PaymentStatus = PaymentStatus.Cancelled
                    }
                };

            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error canceling Kustom payment for ctx.Order {OrderNumber}", ctx.Order.OrderNumber);
            }

            return ApiResult.Empty;
        }
    }
}
