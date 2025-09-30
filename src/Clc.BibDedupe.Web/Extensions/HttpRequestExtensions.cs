using System;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Clc.BibDedupe.Web.Extensions;

public static class HttpRequestExtensions
{
    private const string XmlHttpRequest = "XMLHttpRequest";
    private const string RequestedWithHeader = "X-Requested-With";

    public static bool IsAjaxRequest(this HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Headers.TryGetValue(RequestedWithHeader, out var headerValue)
            && headerValue.Any(value => string.Equals(value, XmlHttpRequest, StringComparison.OrdinalIgnoreCase));
    }
}
