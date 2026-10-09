using WeselAja.Integration.Configuration;
using WeselAja.Integration.Models;

namespace WeselAja.Integration;

internal static class Program
{
    private static async Task Main(
        string[] args)
    {
        if (
                    args.Length > 0
                    &&
                    string.Equals(
                        args[0],
                        "test",
                        StringComparison.OrdinalIgnoreCase))
        {
            SelfTest.Run();
            return;
        }
        if (
            args.Length > 0
            &&
            string.Equals(
                args[0],
                "callback",
                StringComparison.OrdinalIgnoreCase))
        {
            await CallbackServer.RunAsync();
            return;
        }

        if (
            args.Length > 0
            &&
            string.Equals(
                args[0],
                "redirect",
                StringComparison.OrdinalIgnoreCase))
        {
            await RedirectServer.RunAsync();
            return;
        }

        var config =
            WeselAjaConfig.FromEnvironment();

        using var client =
            new WeselAjaClient(
                config);

        if (
            args.Length > 0
            &&
            string.Equals(
                args[0],
                "payins",
                StringComparison.OrdinalIgnoreCase))
        {
            string? status =
                args.Length > 1
                    ? args[1]
                    : null;

            int? limit = null;

            if (
                args.Length > 2
                &&
                int.TryParse(
                    args[2],
                    out var parsedLimit))
            {
                limit = parsedLimit;
            }

            var result =
                await client.GetPayinsAsync(
                    status,
                    limit);

            Console.WriteLine(
                result.GetRawText());

            return;
        }

        var response =
            await client.CreatePaymentAsync(
                new CreatePaymentRequest
                {
                    InitiatedAmount =
                        10_000,

                    CustomerName =
                        "Andi Sansotia",

                    Bank =
                        "MDR",

                    Description =
                        "WeselAja sandbox example"
                });

        Console.WriteLine(
            "Payment created successfully.");

        Console.WriteLine(
            $"Status   : {response.Status}");

        Console.WriteLine(
            $"Reference: {response.ReferenceCode}");

        Console.WriteLine(
            $"Channel  : {response.PaymentChannel}");

        Console.WriteLine(
            $"Payment  : {response.PaymentCode}");
    }
}