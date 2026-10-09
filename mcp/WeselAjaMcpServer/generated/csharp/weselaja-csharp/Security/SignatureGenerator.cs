using System.Security.Cryptography;
using System.Text;

namespace WeselAja.Integration.Security;

public static class SignatureGenerator
{
    public static string Generate(
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

        using var hmac =
            new HMACSHA256(
                Encoding.UTF8.GetBytes(
                    secretKey));

        var hash =
            hmac.ComputeHash(
                Encoding.UTF8.GetBytes(
                    payload));

        return Convert.ToBase64String(
            hash);
    }

    public static string GenerateCallbackSignature(
        string method,
        string uri,
        string rawBody,
        string timestamp,
        string secretKey)
    {
        var payload =
            $"{method}\n" +
            $"{uri}\n" +
            $"{rawBody}\n" +
            $"{timestamp}";

        using var hmac =
            new HMACSHA256(
                Encoding.UTF8.GetBytes(
                    secretKey));

        var hash =
            hmac.ComputeHash(
                Encoding.UTF8.GetBytes(
                    payload));

        return Convert.ToBase64String(
            hash);
    }

    public static bool VerifyCallbackSignature(
        string method,
        string uri,
        string rawBody,
        string timestamp,
        string signature,
        string secretKey)
    {
        if (
            string.IsNullOrWhiteSpace(
                signature))
        {
            return false;
        }

        var expected =
            GenerateCallbackSignature(
                method,
                uri,
                rawBody,
                timestamp,
                secretKey);

        var expectedBytes =
            Encoding.UTF8.GetBytes(
                expected);

        var providedBytes =
            Encoding.UTF8.GetBytes(
                signature.Trim());

        return
            CryptographicOperations
                .FixedTimeEquals(
                    expectedBytes,
                    providedBytes);
    }
}