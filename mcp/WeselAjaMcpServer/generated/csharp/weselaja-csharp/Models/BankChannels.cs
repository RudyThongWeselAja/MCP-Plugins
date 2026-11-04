namespace WeselAja.Integration.Models;

public static class BankChannels
{
    private static readonly Dictionary<
        string,
        string>
        Channels =
            new(
                StringComparer.OrdinalIgnoreCase)
            {
                ["BRI"] =
                    "BRI.VA",

                ["BANKBRI"] =
                    "BRI.VA",

                ["MANDIRI"] =
                    "MDR.VA",

                ["BANKMANDIRI"] =
                    "MDR.VA",

                ["MDR"] =
                    "MDR.VA",

                ["BNI"] =
                    "BNI.VA",

                ["BANKBNI"] =
                    "BNI.VA",

                ["PERMATA"] =
                    "PTB.VA",

                ["BANKPERMATA"] =
                    "PTB.VA",

                ["PTB"] =
                    "PTB.VA",

                ["CIMB"] =
                    "CIMB.VA",

                ["CIMBNIAGA"] =
                    "CIMB.VA",

                ["BANKCIMB"] =
                    "CIMB.VA",

                ["BCA"] =
                    "BCA.VA",

                ["BANKBCA"] =
                    "BCA.VA"
            };

    public static string Resolve(
        string bank)
    {
        if (
            string.IsNullOrWhiteSpace(
                bank))
        {
            throw new ArgumentException(
                "Bank is required.");
        }

        var key =
            Normalize(bank);

        if (
            Channels.TryGetValue(
                key,
                out var channel))
        {
            return channel;
        }

        throw new ArgumentException(
            $"Unsupported bank '{bank}'. " +
            "Supported banks: " +
            "BRI, Mandiri, BNI, Permata, CIMB, BCA.");
    }

    public static string Normalize(
        string bank)
    {
        return bank
            .Trim()
            .ToUpperInvariant()
            .Replace(
                " ",
                string.Empty)
            .Replace(
                "-",
                string.Empty)
            .Replace(
                "_",
                string.Empty);
    }

    public static IReadOnlyDictionary<
        string,
        string>
        GetSupportedBanks()
    {
        return new Dictionary<
            string,
            string>
        {
            ["BRI"] =
                "BRI.VA",

            ["Mandiri"] =
                "MDR.VA",

            ["BNI"] =
                "BNI.VA",

            ["Permata"] =
                "PTB.VA",

            ["CIMB"] =
                "CIMB.VA",

            ["BCA"] =
                "BCA.VA"
        };
    }
}