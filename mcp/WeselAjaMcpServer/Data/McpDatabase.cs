using Npgsql;

namespace WeselAjaMcpServer.Data;

public sealed class McpDatabase(NpgsqlDataSource dataSource)
{
    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS mcp_users (
                id UUID PRIMARY KEY,
                display_name TEXT NOT NULL,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS mcp_access_tokens (
                id UUID PRIMARY KEY,
                user_id UUID NOT NULL REFERENCES mcp_users(id)
                    ON DELETE CASCADE,
                token_hash TEXT NOT NULL UNIQUE,
                token_hint VARCHAR(24) NOT NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                expires_at TIMESTAMPTZ NULL,
                revoked_at TIMESTAMPTZ NULL,
                last_used_at TIMESTAMPTZ NULL
            );

            CREATE INDEX IF NOT EXISTS idx_mcp_tokens_user_id
                ON mcp_access_tokens(user_id);

            CREATE TABLE IF NOT EXISTS mcp_generator_requests (
                request_id UUID PRIMARY KEY,
                user_id UUID NOT NULL REFERENCES mcp_users(id)
                    ON DELETE CASCADE,
                target TEXT NOT NULL,
                project_name TEXT NOT NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE INDEX IF NOT EXISTS idx_mcp_generator_requests_user
                ON mcp_generator_requests(user_id);

            CREATE TABLE IF NOT EXISTS mcp_download_tickets (
                token_hash TEXT PRIMARY KEY,
                request_id UUID NOT NULL
                    REFERENCES mcp_generator_requests(request_id)
                    ON DELETE CASCADE,
                user_id UUID NOT NULL
                    REFERENCES mcp_users(id)
                    ON DELETE CASCADE,
                token_id UUID NOT NULL
                    REFERENCES mcp_access_tokens(id)
                    ON DELETE CASCADE,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                expires_at TIMESTAMPTZ NOT NULL,
                revoked_at TIMESTAMPTZ NULL,
                used_at TIMESTAMPTZ NULL
            );

            ALTER TABLE mcp_download_tickets
                ADD COLUMN IF NOT EXISTS token_id UUID
                REFERENCES mcp_access_tokens(id)
                ON DELETE CASCADE;

            ALTER TABLE mcp_download_tickets
                ADD COLUMN IF NOT EXISTS used_at TIMESTAMPTZ NULL;

            DELETE FROM mcp_download_tickets
            WHERE token_id IS NULL;

            ALTER TABLE mcp_download_tickets
                ALTER COLUMN token_id SET NOT NULL;

            CREATE INDEX IF NOT EXISTS idx_mcp_download_tickets_request
                ON mcp_download_tickets(request_id);

            CREATE INDEX IF NOT EXISTS idx_mcp_download_tickets_token
                ON mcp_download_tickets(token_id);
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}