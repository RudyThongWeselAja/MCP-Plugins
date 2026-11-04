namespace WeselAja.Integration.Exceptions;

public class WeselAjaException :
    Exception
{
    public WeselAjaException(
        string message)
        : base(message)
    {
    }

    public WeselAjaException(
        string message,
        Exception innerException)
        : base(
            message,
            innerException)
    {
    }
}

public sealed class
    WeselAjaConfigurationException :
    WeselAjaException
{
    public WeselAjaConfigurationException(
        string message)
        : base(message)
    {
    }
}

public sealed class
    WeselAjaApiException :
    WeselAjaException
{
    public int? StatusCode { get; }

    public string? ResponseBody { get; }

    public WeselAjaApiException(
        string message,
        int? statusCode = null,
        string? responseBody = null)
        : base(message)
    {
        StatusCode =
            statusCode;

        ResponseBody =
            responseBody;
    }

    public WeselAjaApiException(
        string message,
        Exception innerException)
        : base(
            message,
            innerException)
    {
    }
}