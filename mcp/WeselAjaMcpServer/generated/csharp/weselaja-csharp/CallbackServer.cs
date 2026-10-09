
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using WeselAja.Integration.Configuration;
using WeselAja.Integration.Security;

namespace WeselAja.Integration;

internal static class CallbackServer
{
    public static async Task RunAsync()
    {
        EnvLoader.Load();

        var secret = Environment
            .GetEnvironmentVariable("WEB_SIGNATURE_SECRET")
            ?.Trim();

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException(
                "WEB_SIGNATURE_SECRET is required.");
        }

        var portValue = Environment
            .GetEnvironmentVariable("WESELAJA_CALLBACK_PORT");

        var port =
            int.TryParse(portValue, out var configuredPort)
            && configuredPort > 0
                ? configuredPort
                : 8081;

        var builder = WebApplication.CreateBuilder(
            Array.Empty<string>());

        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

        var app = builder.Build();

        app.MapPost(
            "/payment/weselaja/callback",
            async (HttpRequest request) =>
            {
                var timestamp = request.Headers[
                    "x-middleware-timestamp"].ToString();

                var signature = request.Headers[
                    "x-middleware-signature"].ToString();

                using var reader = new StreamReader(
                    request.Body,
                    Encoding.UTF8);

                var rawBody = await reader.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(timestamp))
                {
                    return Results.Json(
                        new
                        {
                            success = false,
                            error = "Missing x-middleware-timestamp"
                        },
                        statusCode: StatusCodes.Status400BadRequest);
                }

                if (string.IsNullOrWhiteSpace(signature))
                {
                    return Results.Json(
                        new
                        {
                            success = false,
                            error = "Missing x-middleware-signature"
                        },
                        statusCode: StatusCodes.Status400BadRequest);
                }

                var uri = request.Path.Value
                    ?? "/payment/weselaja/callback";

                if (request.QueryString.HasValue)
                {
                    uri += request.QueryString.Value;
                }

                var valid = SignatureGenerator
                    .VerifyCallbackSignature(
                        "POST",
                        uri,
                        rawBody,
                        timestamp,
                        signature,
                        secret);

                if (!valid)
                {
                    return Results.Json(
                        new
                        {
                            success = false,
                            verified = false,
                            error = "Invalid callback signature"
                        },
                        statusCode: StatusCodes.Status401Unauthorized);
                }

                object body;

                try
                {
                    using var document = JsonDocument.Parse(rawBody);
                    body = document.RootElement.Clone();
                }
                catch (JsonException)
                {
                    body = rawBody;
                }

                return Results.Json(new
                {
                    success = true,
                    verified = true,
                    body
                });
            });

        Console.WriteLine(
            "WeselAja callback handler listening on " +
            $"http://0.0.0.0:{port}" +
            "/payment/weselaja/callback");

        await app.RunAsync();
    }
}
