using XenithPayMcp.Configuration;
using XenithPayMcp.Models;

namespace XenithPayMcp.Services;

public class XenithPayBlocksService
{
    private readonly XenithPayOptions _options;

    public XenithPayBlocksService(XenithPayOptions options)
    {
        _options = options;
    }

    public bool IsActive()
    {
        return _options.Enabled;
    }

    public PaymentMethodInfo GetPaymentMethodInfo()
    {
        return new PaymentMethodInfo
        {
            Title = _options.Title,
            Description = _options.Description,
            Icon = _options.Icon,
            Supports = ["products"]
        };
    }
}