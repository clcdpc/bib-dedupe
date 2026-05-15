using Clc.Postmark.Models;

namespace Clc.BibDedupe.Web.Services;

public interface IPostmarkClient
{
    void Send(EmailMessage message);
}
