using WeselAjaMcp.Models;

namespace WeselAjaMcp.Services;

public interface IWeselAjaClient
{
    Task<CreatePaymentResponse> CreatePaymentAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default);
}