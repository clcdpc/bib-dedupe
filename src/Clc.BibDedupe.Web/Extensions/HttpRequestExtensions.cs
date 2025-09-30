using System;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Clc.BibDedupe.Web.Extensions;

public static class HttpRequestExtensions
{
    private const string XmlHttpRequest = "XMLHttpRequest";

    public static bool IsAjaxRequest(this HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var headerValue = request.Headers.XRequestedWith;

        return headerValue.Any(value => string.Equals(value, XmlHttpRequest, StringComparison.OrdinalIgnoreCase));
    }
}
