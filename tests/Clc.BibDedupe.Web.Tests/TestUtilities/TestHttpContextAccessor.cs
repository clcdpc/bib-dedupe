using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Clc.BibDedupe.Web.Tests.TestUtilities;

public sealed class TestHttpContextAccessor : IHttpContextAccessor
{
    public HttpContext? HttpContext { get; set; }

    public static TestHttpContextAccessor WithSession(ISession session)
    {
        var context = new DefaultHttpContext();
        context.Features.Set<ISessionFeature>(new SessionFeature(session));
        return new TestHttpContextAccessor
        {
            HttpContext = context
        };
    }

    private sealed class SessionFeature(ISession session) : ISessionFeature
    {
        public ISession Session { get; set; } = session;
    }
}
