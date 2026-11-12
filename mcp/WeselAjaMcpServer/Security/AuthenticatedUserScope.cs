using System.Threading;

namespace WeselAjaMcp.Security;

public static class AuthenticatedUserScope
{
    private static readonly AsyncLocal<AuthenticatedMcpUser?> CurrentUser = new();

    public static Guid? GetUserId()
    {
        return CurrentUser.Value?.UserId;
    }

    public static Guid? GetTokenId()
    {
        return CurrentUser.Value?.TokenId;
    }

    public static IDisposable Begin(AuthenticatedMcpUser user)
    {
        var previousUser = CurrentUser.Value;

        CurrentUser.Value = user;

        return new Scope(previousUser);
    }

    private sealed class Scope(
        AuthenticatedMcpUser? previousUser) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            CurrentUser.Value = previousUser;
            _disposed = true;
        }
    }
}