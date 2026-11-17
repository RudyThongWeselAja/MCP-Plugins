using Npgsql;
using NpgsqlTypes;
using WeselAjaMcp.Security;

namespace WeselAjaMcpServer.Data;

public sealed class AccessTokenRepository(
    NpgsqlDataSource dataSource)
{
    public async Task<IssuedTokenData> CreateAsync(
        string displayName,
        string tokenHash,
        string tokenHint,
        DateTime? expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        var userId = Guid.NewGuid();
        var tokenId = Guid.NewGuid();

        await using var connection =
            await dataSource.OpenConnectionAsync(cancellationToken);

        await using var transaction =
            await connection.BeginTransactionAsync(cancellationToken);

        await using (var userCommand = connection.CreateCommand())
        {
            userCommand.Transaction = transaction;

            userCommand.CommandText =
                """
                INSERT INTO mcp_users (id, display_name)
                VALUES (@id, @display_name);
                """;

            userCommand.Parameters.AddWithValue("id", userId);
            userCommand.Parameters.AddWithValue(
                "display_name",
                displayName);

            await userCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        DateTime createdAt;

        await using (var tokenCommand = connection.CreateCommand())
        {
            tokenCommand.Transaction = transaction;

            tokenCommand.CommandText =
                """
                INSERT INTO mcp_access_tokens (
                    id,
                    user_id,
                    token_hash,
                    token_hint,
                    expires_at
                )
                VALUES (
                    @id,
                    @user_id,
                    @token_hash,
                    @token_hint,
                    @expires_at
                )
                RETURNING created_at;
                """;

            tokenCommand.Parameters.AddWithValue("id", tokenId);
            tokenCommand.Parameters.AddWithValue("user_id", userId);
            tokenCommand.Parameters.AddWithValue("token_hash", tokenHash);
            tokenCommand.Parameters.AddWithValue("token_hint", tokenHint);

            tokenCommand.Parameters.Add(
                new NpgsqlParameter(
                    "expires_at",
                    NpgsqlDbType.TimestampTz)
                {
                    Value = expiresAtUtc is null
                        ? DBNull.Value
                        : expiresAtUtc.Value
                });

            createdAt = (DateTime)(
                await tokenCommand.ExecuteScalarAsync(cancellationToken)
                ?? throw new InvalidOperationException(
                    "Failed to create access token."));
        }

        await transaction.CommitAsync(cancellationToken);

        return new IssuedTokenData(
            userId,
            tokenId,
            createdAt,
            expiresAtUtc);
    }

    public async Task<AuthenticatedMcpUser?> FindValidTokenAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            UPDATE mcp_access_tokens AS token
            SET last_used_at = NOW()
            FROM mcp_users AS account
            WHERE token.user_id = account.id
              AND token.token_hash = @token_hash
              AND token.revoked_at IS NULL
              AND (
                    token.expires_at IS NULL
                    OR token.expires_at > NOW()
              )
              AND account.is_active = TRUE
            RETURNING
                token.id,
                token.user_id,
                account.display_name;
            """;

        command.Parameters.AddWithValue("token_hash", tokenHash);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new AuthenticatedMcpUser(
            reader.GetGuid(1),
            reader.GetGuid(0),
            reader.GetString(2));
    }

    public async Task<IReadOnlyList<AccessTokenListItem>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var results = new List<AccessTokenListItem>();

        await using var connection =
            await dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT
                account.id,
                token.id,
                account.display_name,
                token.token_hint,
                token.created_at,
                token.expires_at,
                token.revoked_at,
                token.last_used_at,
                (
                    account.is_active
                    AND token.revoked_at IS NULL
                    AND (
                        token.expires_at IS NULL
                        OR token.expires_at > NOW()
                    )
                ) AS is_active
            FROM mcp_access_tokens AS token
            INNER JOIN mcp_users AS account
                ON account.id = token.user_id
            ORDER BY token.created_at DESC;
            """;

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new AccessTokenListItem(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetDateTime(4),
                reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                reader.GetBoolean(8)));
        }

        return results;
    }

    public async Task<bool> RevokeAsync(
        Guid tokenId,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await dataSource.OpenConnectionAsync(cancellationToken);

        await using var transaction =
            await connection.BeginTransactionAsync(cancellationToken);

        await using var tokenCommand = connection.CreateCommand();

        tokenCommand.Transaction = transaction;

        tokenCommand.CommandText =
            """
            UPDATE mcp_access_tokens
            SET revoked_at = NOW()
            WHERE id = @token_id
              AND revoked_at IS NULL
            RETURNING id;
            """;

        tokenCommand.Parameters.AddWithValue("token_id", tokenId);

        var revokedToken = await tokenCommand.ExecuteScalarAsync(
            cancellationToken);

        if (revokedToken is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await using var ticketCommand = connection.CreateCommand();

        ticketCommand.Transaction = transaction;

        ticketCommand.CommandText =
            """
            UPDATE mcp_download_tickets
            SET revoked_at = NOW()
            WHERE token_id = @token_id
              AND revoked_at IS NULL;
            """;

        ticketCommand.Parameters.AddWithValue("token_id", tokenId);

        await ticketCommand.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return true;
    }
}

public sealed record IssuedTokenData(
    Guid UserId,
    Guid TokenId,
    DateTime CreatedAtUtc,
    DateTime? ExpiresAtUtc
);