using System.Collections.Concurrent;
using Npgsql;

namespace WeselAjaMcpServer.Data;

public interface IGeneratorRequestRepository
{
    Task RegisterAsync(
        Guid requestId,
        Guid userId,
        string target,
        string projectName,
        CancellationToken cancellationToken = default);

    Task<bool> IsOwnedAsync(
        Guid requestId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task StoreDownloadTicketAsync(
        string tokenHash,
        Guid requestId,
        Guid userId,
        Guid tokenId,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default);

    Task<bool> IsDownloadTicketValidAsync(
        string tokenHash,
        Guid requestId,
        string target,
        string projectName,
        CancellationToken cancellationToken = default);

    Task<bool> ConsumeDownloadTicketAsync(
        string tokenHash,
        Guid requestId,
        string target,
        string projectName,
        CancellationToken cancellationToken = default);
}

public sealed class PostgresGeneratorRequestRepository(
    NpgsqlDataSource dataSource) : IGeneratorRequestRepository
{
    public async Task RegisterAsync(
        Guid requestId,
        Guid userId,
        string target,
        string projectName,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            INSERT INTO mcp_generator_requests (
                request_id,
                user_id,
                target,
                project_name
            )
            VALUES (
                @request_id,
                @user_id,
                @target,
                @project_name
            );
            """;

        command.Parameters.AddWithValue("request_id", requestId);
        command.Parameters.AddWithValue("user_id", userId);
        command.Parameters.AddWithValue("target", target);
        command.Parameters.AddWithValue("project_name", projectName);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> IsOwnedAsync(
        Guid requestId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT EXISTS (
                SELECT 1
                FROM mcp_generator_requests
                WHERE request_id = @request_id
                  AND user_id = @user_id
            );
            """;

        command.Parameters.AddWithValue("request_id", requestId);
        command.Parameters.AddWithValue("user_id", userId);

        return (bool)(
            await command.ExecuteScalarAsync(cancellationToken)
            ?? false);
    }

    public async Task StoreDownloadTicketAsync(
        string tokenHash,
        Guid requestId,
        Guid userId,
        Guid tokenId,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            INSERT INTO mcp_download_tickets (
                token_hash,
                request_id,
                user_id,
                token_id,
                expires_at
            )
            SELECT
                @token_hash,
                request.request_id,
                request.user_id,
                access_token.id,
                @expires_at
            FROM mcp_generator_requests AS request
            INNER JOIN mcp_users AS account
                ON account.id = request.user_id
            INNER JOIN mcp_access_tokens AS access_token
                ON access_token.id = @token_id
               AND access_token.user_id = request.user_id
            WHERE request.request_id = @request_id
              AND request.user_id = @user_id
              AND account.is_active = TRUE
              AND access_token.revoked_at IS NULL
              AND (
                  access_token.expires_at IS NULL
                  OR access_token.expires_at > NOW()
              );
            """;

        command.Parameters.AddWithValue("token_hash", tokenHash);
        command.Parameters.AddWithValue("request_id", requestId);
        command.Parameters.AddWithValue("user_id", userId);
        command.Parameters.AddWithValue("token_id", tokenId);
        command.Parameters.AddWithValue("expires_at", expiresAtUtc);

        var affected = await command.ExecuteNonQueryAsync(
            cancellationToken);

        if (affected != 1)
        {
            throw new InvalidOperationException(
                "Unable to create download ticket for this project.");
        }
    }

    public async Task<bool> IsDownloadTicketValidAsync(
        string tokenHash,
        Guid requestId,
        string target,
        string projectName,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT EXISTS (
                SELECT 1
                FROM mcp_download_tickets AS ticket
                INNER JOIN mcp_generator_requests AS request
                    ON request.request_id = ticket.request_id
                   AND request.user_id = ticket.user_id
                INNER JOIN mcp_users AS account
                    ON account.id = ticket.user_id
                INNER JOIN mcp_access_tokens AS access_token
                    ON access_token.id = ticket.token_id
                   AND access_token.user_id = ticket.user_id
                WHERE ticket.token_hash = @token_hash
                  AND ticket.request_id = @request_id
                  AND ticket.revoked_at IS NULL
                  AND ticket.used_at IS NULL
                  AND ticket.expires_at > NOW()
                  AND request.target = @target
                  AND request.project_name = @project_name
                  AND account.is_active = TRUE
                  AND access_token.revoked_at IS NULL
                  AND (
                      access_token.expires_at IS NULL
                      OR access_token.expires_at > NOW()
                  )
            );
            """;

        command.Parameters.AddWithValue("token_hash", tokenHash);
        command.Parameters.AddWithValue("request_id", requestId);
        command.Parameters.AddWithValue("target", target);
        command.Parameters.AddWithValue("project_name", projectName);

        return (bool)(
            await command.ExecuteScalarAsync(cancellationToken)
            ?? false);
    }

    public async Task<bool> ConsumeDownloadTicketAsync(
        string tokenHash,
        Guid requestId,
        string target,
        string projectName,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            UPDATE mcp_download_tickets AS ticket
            SET used_at = NOW()
            WHERE ticket.token_hash = @token_hash
              AND ticket.request_id = @request_id
              AND ticket.revoked_at IS NULL
              AND ticket.used_at IS NULL
              AND ticket.expires_at > NOW()
              AND EXISTS (
                  SELECT 1
                  FROM mcp_generator_requests AS request
                  INNER JOIN mcp_users AS account
                      ON account.id = request.user_id
                  INNER JOIN mcp_access_tokens AS access_token
                      ON access_token.id = ticket.token_id
                     AND access_token.user_id = ticket.user_id
                  WHERE request.request_id = ticket.request_id
                    AND request.user_id = ticket.user_id
                    AND request.target = @target
                    AND request.project_name = @project_name
                    AND account.is_active = TRUE
                    AND access_token.revoked_at IS NULL
                    AND (
                        access_token.expires_at IS NULL
                        OR access_token.expires_at > NOW()
                    )
              )
            RETURNING ticket.token_hash;
            """;

        command.Parameters.AddWithValue("token_hash", tokenHash);
        command.Parameters.AddWithValue("request_id", requestId);
        command.Parameters.AddWithValue("target", target);
        command.Parameters.AddWithValue("project_name", projectName);

        var consumed = await command.ExecuteScalarAsync(
            cancellationToken);

        return consumed is not null;
    }
}

public sealed class InMemoryGeneratorRequestRepository
    : IGeneratorRequestRepository
{
    private readonly ConcurrentDictionary<Guid, GeneratorRequestOwner>
        _requests = new();

    private readonly ConcurrentDictionary<string, MemoryDownloadTicket>
        _tickets = new();

    public Task RegisterAsync(
        Guid requestId,
        Guid userId,
        string target,
        string projectName,
        CancellationToken cancellationToken = default)
    {
        if (!_requests.TryAdd(
            requestId,
            new GeneratorRequestOwner(
                userId,
                target,
                projectName)))
        {
            throw new InvalidOperationException(
                "Generator request already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<bool> IsOwnedAsync(
        Guid requestId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _requests.TryGetValue(requestId, out var request)
            && request.UserId == userId);
    }

    public Task StoreDownloadTicketAsync(
        string tokenHash,
        Guid requestId,
        Guid userId,
        Guid tokenId,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (!_requests.TryGetValue(requestId, out var request) ||
            request.UserId != userId)
        {
            throw new InvalidOperationException(
                "Generator request does not belong to this user.");
        }

        if (!_tickets.TryAdd(
            tokenHash,
            new MemoryDownloadTicket(
                requestId,
                userId,
                tokenId,
                request.Target,
                request.ProjectName,
                expiresAtUtc)))
        {
            throw new InvalidOperationException(
                "Download ticket already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<bool> IsDownloadTicketValidAsync(
        string tokenHash,
        Guid requestId,
        string target,
        string projectName,
        CancellationToken cancellationToken = default)
    {
        var valid =
            _tickets.TryGetValue(tokenHash, out var ticket)
            && ticket.RequestId == requestId
            && ticket.Target == target
            && ticket.ProjectName == projectName
            && ticket.ExpiresAtUtc > DateTime.UtcNow
            && !ticket.IsUsed;

        return Task.FromResult(valid);
    }

    public Task<bool> ConsumeDownloadTicketAsync(
        string tokenHash,
        Guid requestId,
        string target,
        string projectName,
        CancellationToken cancellationToken = default)
    {
        while (_tickets.TryGetValue(tokenHash, out var ticket))
        {
            if (ticket.RequestId != requestId ||
                ticket.Target != target ||
                ticket.ProjectName != projectName ||
                ticket.ExpiresAtUtc <= DateTime.UtcNow ||
                ticket.IsUsed)
            {
                return Task.FromResult(false);
            }

            var consumed = ticket with { IsUsed = true };

            if (_tickets.TryUpdate(tokenHash, consumed, ticket))
            {
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    private sealed record GeneratorRequestOwner(
        Guid UserId,
        string Target,
        string ProjectName);

    private sealed record MemoryDownloadTicket(
        Guid RequestId,
        Guid UserId,
        Guid TokenId,
        string Target,
        string ProjectName,
        DateTime ExpiresAtUtc,
        bool IsUsed = false);
}