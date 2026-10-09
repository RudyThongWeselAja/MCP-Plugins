using WeselAja.Integration.Models;
using WeselAja.Integration.Security;

namespace WeselAja.Integration;

internal static class SelfTest
{
    public static void Run()
    {
        Assert(
            BankChannels.Resolve("BRI") ==
                "BRI.VA",
            "BRI mapping failed.");

        Assert(
            BankChannels.Resolve("Bank BRI") ==
                "BRI.VA",
            "Bank BRI mapping failed.");

        Assert(
            BankChannels.Resolve("Mandiri") ==
                "MDR.VA",
            "Mandiri mapping failed.");

        Assert(
            BankChannels.Resolve("Bank Mandiri") ==
                "MDR.VA",
            "Bank Mandiri mapping failed.");

        Assert(
            BankChannels.Resolve("BNI") ==
                "BNI.VA",
            "BNI mapping failed.");

        Assert(
            BankChannels.Resolve("Permata") ==
                "PTB.VA",
            "Permata mapping failed.");

        Assert(
            BankChannels.Resolve("CIMB") ==
                "CIMB.VA",
            "CIMB mapping failed.");

        Assert(
            BankChannels.Resolve("CIMB Niaga") ==
                "CIMB.VA",
            "CIMB Niaga mapping failed.");

        Assert(
            BankChannels.Resolve("BCA") ==
                "BCA.VA",
            "BCA mapping failed.");

        var unsupportedRejected =
            false;

        try
        {
            BankChannels.Resolve(
                "Bank Tidak Ada");
        }
        catch (ArgumentException)
        {
            unsupportedRejected =
                true;
        }

        Assert(
            unsupportedRejected,
            "Unsupported bank should be rejected.");

        const string method =
            "POST";

        const string uri =
            "/payment/weselaja/callback";

        const string body =
            "{\"hello\":\"world\"}";

        const string timestamp =
            "2026-01-01T00:00:00.000Z";

        const string secret =
            "test-secret";

        var signature =
            SignatureGenerator
                .GenerateCallbackSignature(
                    method,
                    uri,
                    body,
                    timestamp,
                    secret);

        Assert(
            SignatureGenerator
                .VerifyCallbackSignature(
                    method,
                    uri,
                    body,
                    timestamp,
                    signature,
                    secret),
            "Callback signature verification failed.");

        Assert(
            !SignatureGenerator
                .VerifyCallbackSignature(
                    method,
                    uri,
                    body,
                    timestamp,
                    signature + "x",
                    secret),
            "Invalid callback signature should be rejected.");

        Console.WriteLine(
            "All C# WeselAja self-tests passed.");
    }

    private static void Assert(
        bool condition,
        string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(
                message);
        }
    }
}