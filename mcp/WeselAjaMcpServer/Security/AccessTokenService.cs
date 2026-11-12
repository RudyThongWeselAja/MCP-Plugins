using System.Security.Cryptography;
using System.Text;
using WeselAjaMcpServer.Data;

namespace WeselAjaMcp.Security;

public sealed class AccessTokenService(
    AccessTokenRepository repository)
{
    public async Task<IssuedAccessToken> CreateAsync(
        string displayName,
        DateTimeOffset? expiresAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException(
                "Display name is required.");
        }

        displayName = displayName.Trim();

        if (displayName.Length > 100)
        {
            throw new ArgumentException(
                "Display name cannot exceed 100 characters.");
        }

        if (expiresAtUtc.HasValue &&
            expiresAtUtc.Value <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentException(
                "Expiration must be in the future.");
        }

        var randomBytes = RandomNumberGenerator.GetBytes(32);

        var token =
            "wja_" +
            Convert.ToBase64String(randomBytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

        var tokenHash = HashToken(token);
        var tokenHint = token[..12];

        var created = await repository.CreateAsync(
            displayName,
            tokenHash,
            tokenHint + "...",
            expiresAtUtc?.UtcDateTime,
            cancellationToken);

        return new IssuedAccessToken(
            created.UserId,
            created.TokenId,
            displayName,
            token,
            tokenHint + "...",
            created.CreatedAtUtc,
            created.ExpiresAtUtc);
    }

    public async Task<AuthenticatedMcpUser?> ValidateAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token) ||
            token.Length > 256 ||
            !token.StartsWith(
                "wja_",
                StringComparison.Ordinal))
        {
            return null;
        }

        return await repository.FindValidTokenAsync(
            HashToken(token),
            cancellationToken);
    }

    public Task<IReadOnlyList<AccessTokenListItem>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return repository.GetAllAsync(cancellationToken);
    }

    public Task<bool> RevokeAsync(
        Guid tokenId,
        CancellationToken cancellationToken = default)
    {
        return repository.RevokeAsync(
            tokenId,
            cancellationToken);
    }

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}