using Microsoft.AspNetCore.Http;

namespace Clc.BibDedupe.Web;

public static class SessionExtensions
{
    private const string AuthMessageKey = "AuthMessage";
    private const string AuthUserNameKey = "AuthUserName";

    public static void SetAuthMessage(this ISession session, string message, string? userName = null)
    {
        session.SetString(AuthMessageKey, message);

        if (string.IsNullOrWhiteSpace(userName))
        {
            session.Remove(AuthUserNameKey);
        }
        else
        {
            session.SetString(AuthUserNameKey, userName);
        }
    }

    public static AuthorizationMessage? TakeAuthMessage(this ISession session)
    {
        var value = session.GetString(AuthMessageKey);
        if (value is null)
        {
            session.Remove(AuthUserNameKey);
            return null;
        }

        var userName = session.GetString(AuthUserNameKey);
        session.Remove(AuthMessageKey);
        session.Remove(AuthUserNameKey);
        return new AuthorizationMessage(value, userName);
    }
}

public readonly record struct AuthorizationMessage(string Message, string? UserName);
