namespace Clc.BibDedupe.Web.Services;

public interface IPostmarkClientFactory
{
    IPostmarkClient Create(string serverToken);
}
