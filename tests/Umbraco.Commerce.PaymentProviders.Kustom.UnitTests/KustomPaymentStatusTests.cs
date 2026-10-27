using FluentAssertions;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.PaymentProviders.Kustom.Api.Models;

namespace Umbraco.Commerce.PaymentProviders.Kustom.UnitTests
{
    public class KustomPaymentStatusTests
    {
        private readonly KustomHppPaymentProvider _provider = new(null!, null!);

        [Theory]
        [InlineData(KustomOrder.Statuses.CAPTURED, 10000, 0, PaymentStatus.Captured)]
        [InlineData(KustomOrder.Statuses.PART_CAPTURED, 10000, 0, PaymentStatus.Captured)]
        [InlineData(KustomOrder.Statuses.CAPTURED, 10000, 2500, PaymentStatus.PartiallyRefunded)]
        [InlineData(KustomOrder.Statuses.PART_CAPTURED, 10000, 9999, PaymentStatus.PartiallyRefunded)]
        [InlineData(KustomOrder.Statuses.CAPTURED, 10000, 10000, PaymentStatus.Refunded)]
        [InlineData(KustomOrder.Statuses.PART_CAPTURED, 10000, 10000, PaymentStatus.Refunded)]
        [InlineData(KustomOrder.Statuses.REFUNDED, 10000, 10000, PaymentStatus.Refunded)]
        [InlineData(KustomOrder.Statuses.AUTHORIZED, 0, 0, PaymentStatus.Authorized)]
        public void GetPaymentStatus_Should_Map_Captured_And_Refunded_Amounts(string kustomStatus, int capturedAmount, int refundedAmount, PaymentStatus expected)
        {
            // preparation
            KustomOrder order = new()
            {
                Status = kustomStatus,
                CapturedAmount = capturedAmount,
                RefundedAmount = refundedAmount,
            };

            // execution
            PaymentStatus status = _provider.GetPaymentStatus(order);

            // asserts
            status.Should().Be(expected);
        }
    }
}
