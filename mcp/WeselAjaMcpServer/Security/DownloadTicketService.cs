using System.Security.Cryptography;
using System.Text;
using WeselAjaMcpServer.Data;

namespace WeselAjaMcp.Security;

public sealed class DownloadTicketService(
    IGeneratorRequestRepository repository)
{
    private static readonly TimeSpan TicketLifetime =
        TimeSpan.FromHours(24);

    public async Task<IssuedDownloadTicket> CreateAsync(
        Guid requestId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var tokenId = AuthenticatedUserScope.GetTokenId();

        if (!tokenId.HasValue ||
            AuthenticatedUserScope.GetUserId() != userId)
        {
            throw new InvalidOperationException(
                "Authenticated token context is missing.");
        }

        var randomBytes = RandomNumberGenerator.GetBytes(32);

        var ticket =
            "dl_" +
            Convert.ToBase64String(randomBytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

        var expiresAtUtc = DateTime.UtcNow.Add(TicketLifetime);
        var ticketHash = HashTicket(ticket);

        await repository.StoreDownloadTicketAsync(
            ticketHash,
            requestId,
            userId,
            tokenId.Value,
            expiresAtUtc,
            cancellationToken);

        return new IssuedDownloadTicket(
            ticket,
            expiresAtUtc);
    }

    public Task<bool> ValidateAsync(
        string ticket,
        Guid requestId,
        string target,
        string projectName,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidFormat(ticket))
        {
            return Task.FromResult(false);
        }

        return repository.IsDownloadTicketValidAsync(
            HashTicket(ticket),
            requestId,
            target,
            projectName,
            cancellationToken);
    }

    public Task<bool> ConsumeAsync(
        string ticket,
        Guid requestId,
        string target,
        string projectName,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidFormat(ticket))
        {
            return Task.FromResult(false);
        }

        return repository.ConsumeDownloadTicketAsync(
            HashTicket(ticket),
            requestId,
            target,
            projectName,
            cancellationToken);
    }

    private static bool IsValidFormat(string ticket)
    {
        return !string.IsNullOrWhiteSpace(ticket)
            && ticket.Length <= 256
            && ticket.StartsWith(
                "dl_",
                StringComparison.Ordinal);
    }

    private static string HashTicket(string ticket)
    {
        var bytes = Encoding.UTF8.GetBytes(ticket);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public sealed record IssuedDownloadTicket(
    string Token,
    DateTime ExpiresAtUtc
);