namespace Clc.BibDedupe.Web.Services;

public class PostmarkClientFactory : IPostmarkClientFactory
{
    public IPostmarkClient Create(string serverToken)
    {
        return new PostmarkClientWrapper(serverToken);
    }
}
