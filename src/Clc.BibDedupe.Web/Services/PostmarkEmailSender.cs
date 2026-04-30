using Clc.BibDedupe.Web.Options;
using Clc.Postmark.Api;
using Clc.Postmark.Api.Models;
using Microsoft.Extensions.Options;

namespace Clc.BibDedupe.Web.Services;

public class PostmarkEmailSender(
    IOptions<PostmarkOptions> postmarkOptions,
    IOptions<DecisionBatchNotificationOptions> notificationOptions) : IEmailSender
{
    public async Task SendAsync(string recipientEmail, string subject, string body)
    {
        var serverToken = postmarkOptions.Value.ServerToken;
        var senderEmail = notificationOptions.Value.SenderEmail;

        if (string.IsNullOrWhiteSpace(serverToken))
        {
            throw new InvalidOperationException("Postmark ServerToken is required to send decision batch notifications.");
        }

        if (string.IsNullOrWhiteSpace(senderEmail))
        {
            throw new InvalidOperationException("DecisionBatchNotifications SenderEmail is required to send decision batch notifications.");
        }

        var client = new PostmarkClient(serverToken);
        var request = new PostmarkMessageRequest
        {
            From = senderEmail,
            To = recipientEmail,
            Subject = subject,
            TextBody = body
        };

        await client.SendMessageAsync(request);
    }
}
