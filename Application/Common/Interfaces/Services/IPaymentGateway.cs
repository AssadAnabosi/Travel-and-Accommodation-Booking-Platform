using Application.Common.Models;

namespace Application.Common.Interfaces.Services;

public interface IPaymentGateway
{
    Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken cancellationToken = default);
}