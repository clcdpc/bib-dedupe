namespace Clc.BibDedupe.Web.Services;

public interface IEmailSender
{
    Task SendAsync(string recipientEmail, string subject, string body);
}
