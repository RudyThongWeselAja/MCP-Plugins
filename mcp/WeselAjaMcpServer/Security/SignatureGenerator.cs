using System.Security.Cryptography;
using System.Text;

namespace WeselAjaMcp.Security;

public interface ISignatureGenerator
{
    string Generate(
        string method,
        string uri,
        string timestamp,
        string body,
        string secretKey);
}

public class SignatureGenerator : ISignatureGenerator
{
    public string Generate(
        string method,
        string uri,
        string timestamp,
        string body,
        string secretKey)
    {
        var payload =
            $"{method}\n" +
            $"{uri}\n" +
            $"{timestamp}\n" +
            body;

        using var hmac = new HMACSHA256(
            Encoding.UTF8.GetBytes(secretKey)
        );

        var hash = hmac.ComputeHash(
            Encoding.UTF8.GetBytes(payload)
        );

        return Convert.ToBase64String(hash);
    }
}