namespace Clc.BibDedupe.Web.Services;

public class NoOpEmailSender : IEmailSender
{
    public Task SendAsync(string recipientEmail, string subject, string body) => Task.CompletedTask;
}
