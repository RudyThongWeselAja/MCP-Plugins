using WeselAjaMcp.Configuration;
using WeselAjaMcp.Models;

namespace WeselAjaMcp.Services;

public class WeselAjaBlocksService
{
    private readonly WeselAjaOptions _options;

    public WeselAjaBlocksService(WeselAjaOptions options)
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