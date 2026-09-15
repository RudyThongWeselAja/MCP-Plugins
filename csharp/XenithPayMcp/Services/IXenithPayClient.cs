using XenithPayMcp.Models;

namespace XenithPayMcp.Services;

public interface IXenithPayClient
{
    Task<CreatePaymentResponse> CreatePaymentAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default);
}