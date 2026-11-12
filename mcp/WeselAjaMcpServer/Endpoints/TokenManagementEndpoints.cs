using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WeselAjaMcp.Security;

namespace WeselAjaMcpServer.Endpoints;

public static class TokenManagementEndpoints
{
    public static IEndpointRouteBuilder MapTokenManagementEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/admin/tokens");

        group.MapPost(
            "",
            async (
                CreateAccessTokenRequest request,
                AccessTokenService service,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.DisplayName))
                {
                    return Results.BadRequest(new
                    {
                        success = false,
                        message = "Display name is required."
                    });
                }

                if (request.ExpiresInDays is <= 0 or > 3650)
                {
                    return Results.BadRequest(new
                    {
                        success = false,
                        message = "Expiration must be between 1 and 3650 days."
                    });
                }

                var expiration = request.ExpiresInDays.HasValue
                    ? DateTimeOffset.UtcNow.AddDays(
                        request.ExpiresInDays.Value)
                    : (DateTimeOffset?)null;

                try
                {
                    var issued = await service.CreateAsync(
                        request.DisplayName,
                        expiration,
                        cancellationToken);

                    return Results.Ok(new
                    {
                        success = true,
                        userId = issued.UserId,
                        tokenId = issued.TokenId,
                        displayName = issued.DisplayName,
                        token = issued.Token,
                        tokenHint = issued.TokenHint,
                        createdAtUtc = issued.CreatedAtUtc,
                        expiresAtUtc = issued.ExpiresAtUtc,
                        message = "Save this token securely. It will only be shown once."
                    });
                }
                catch (ArgumentException exception)
                {
                    return Results.BadRequest(new
                    {
                        success = false,
                        message = exception.Message
                    });
                }
            });

        group.MapGet(
            "",
            async (
                AccessTokenService service,
                CancellationToken cancellationToken) =>
            {
                var tokens = await service.GetAllAsync(
                    cancellationToken);

                return Results.Ok(new
                {
                    success = true,
                    tokens
                });
            });

        group.MapDelete(
            "/{tokenId:guid}",
            async (
                Guid tokenId,
                AccessTokenService service,
                CancellationToken cancellationToken) =>
            {
                var revoked = await service.RevokeAsync(
                    tokenId,
                    cancellationToken);

                if (!revoked)
                {
                    return Results.NotFound(new
                    {
                        success = false,
                        message = "Token not found or already revoked."
                    });
                }

                return Results.Ok(new
                {
                    success = true,
                    message = "Token revoked successfully."
                });
            });

        return endpoints;
    }
}

public sealed record CreateAccessTokenRequest(
    string? DisplayName,
    int? ExpiresInDays
);