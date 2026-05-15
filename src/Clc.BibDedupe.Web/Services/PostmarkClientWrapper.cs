using Clc.Postmark;
using Clc.Postmark.Models;

namespace Clc.BibDedupe.Web.Services;

public class PostmarkClientWrapper(string serverToken) : IPostmarkClient
{
    private readonly PostmarkClient _client = new(serverToken);

    public void Send(EmailMessage message)
    {
        _client.Send(message);
    }
}
