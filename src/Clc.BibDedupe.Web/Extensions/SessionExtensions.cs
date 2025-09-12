using Microsoft.AspNetCore.Http;

namespace Clc.BibDedupe.Web;

public static class SessionExtensions
{
    private const string AuthMessageKey = "AuthMessage";

    public static void SetAuthMessage(this ISession session, string message)
        => session.SetString(AuthMessageKey, message);

    public static string? TakeAuthMessage(this ISession session)
    {
        var value = session.GetString(AuthMessageKey);
        if (value is not null)
        {
            session.Remove(AuthMessageKey);
        }
        return value;
    }
}
