using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace WeselAjaMcpServer.Clients;

public class OdooWeselAjaClient
{
    private const string Endpoint =
        "/weselaja/mcp";

    private readonly HttpClient _httpClient;
    private readonly string _odooUrl;
    private readonly string _apiToken;

    public OdooWeselAjaClient(
        HttpClient httpClient)
    {
        _httpClient = httpClient;

        _odooUrl =
            (
                Environment.GetEnvironmentVariable(
                    "XENITH_ODOO_URL"
                )
                ?? "http://127.0.0.1:8069"
            ).TrimEnd('/');

        _apiToken =
            (
                Environment.GetEnvironmentVariable(
                    "XENITH_ODOO_API_TOKEN"
                )
                ?? ""
            ).Trim();
    }

    public async Task<JsonElement> CallAsync(
        string tool,
        object? arguments = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(
                _apiToken))
        {
            throw new InvalidOperationException(
                "XENITH_ODOO_API_TOKEN is not configured."
            );
        }

        var payload = new
        {
            tool,
            arguments
        };

        var json =
            JsonSerializer.Serialize(
                payload
            );

        using var httpRequest =
            new HttpRequestMessage(
                HttpMethod.Post,
                $"{_odooUrl}{Endpoint}"
            );

        httpRequest.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _apiToken
            );

        httpRequest.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/json"
            )
        );

        httpRequest.Content =
            new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

        using var response =
            await _httpClient.SendAsync(
                httpRequest,
                cancellationToken
            );

        var responseBody =
            await response.Content.ReadAsStringAsync(
                cancellationToken
            );

        if (string.IsNullOrWhiteSpace(
                responseBody))
        {
            throw new InvalidOperationException(
                "Odoo returned an empty response. " +
                $"HTTP {(int)response.StatusCode}"
            );
        }

        try
        {
            using var document =
                JsonDocument.Parse(
                    responseBody
                );

            return document.RootElement.Clone();
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "Odoo returned invalid JSON. " +
                $"Output: {responseBody}",
                exception
            );
        }
    }
}