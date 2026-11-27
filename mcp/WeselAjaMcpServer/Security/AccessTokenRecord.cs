namespace WeselAjaMcp.Security;

public sealed record AuthenticatedMcpUser(
    Guid UserId,
    Guid TokenId,
    string DisplayName
);

public sealed record IssuedAccessToken(
    Guid UserId,
    Guid TokenId,
    string DisplayName,
    string Token,
    string TokenHint,
    DateTime CreatedAtUtc,
    DateTime? ExpiresAtUtc
);

public sealed record AccessTokenListItem(
    Guid UserId,
    Guid TokenId,
    string DisplayName,
    string TokenHint,
    DateTime CreatedAtUtc,
    DateTime? ExpiresAtUtc,
    DateTime? RevokedAtUtc,
    DateTime? LastUsedAtUtc,
    bool IsActive
);