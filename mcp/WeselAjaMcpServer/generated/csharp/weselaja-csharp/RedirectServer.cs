
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace WeselAja.Integration;

internal static class RedirectServer
{
    public static async Task RunAsync()
    {
        var portValue = Environment
            .GetEnvironmentVariable("WESELAJA_REDIRECT_PORT");

        var port =
            int.TryParse(portValue, out var configuredPort)
            && configuredPort > 0
                ? configuredPort
                : 8080;

        var builder = WebApplication.CreateBuilder(
            Array.Empty<string>());

        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

        var app = builder.Build();

        app.MapGet(
            "/payment/weselaja/return",
            (HttpRequest request) =>
            {
                var parameters = request.Query.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.ToString());

                return Results.Json(
                    new
                    {
                        success = true,
                        path = request.Path.Value,
                        parameters
                    },
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
            });

        Console.WriteLine(
            "WeselAja redirect handler listening on " +
            $"http://0.0.0.0:{port}" +
            "/payment/weselaja/return");

        await app.RunAsync();
    }
}
