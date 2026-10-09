
using System;

namespace WeselAja.Integration.Configuration;

public sealed class WeselAjaConfigurationException : Exception
{
    public WeselAjaConfigurationException(string message)
        : base(message)
    {
    }

    public WeselAjaConfigurationException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}
