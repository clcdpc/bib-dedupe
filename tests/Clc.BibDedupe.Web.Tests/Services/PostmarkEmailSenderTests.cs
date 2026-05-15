using Clc.BibDedupe.Web.Options;
using Clc.BibDedupe.Web.Services;
using Microsoft.Extensions.Options;
using Moq;

namespace Clc.BibDedupe.Web.Tests.Services;

[TestClass]
public class PostmarkEmailSenderTests
{
    [TestMethod]
    public async Task SendAsync_Throws_InvalidOperationException_When_ServerToken_Is_Missing()
    {
        var postmarkOptions = Options.Create(new PostmarkOptions { ServerToken = "" });
        var notificationOptions = Options.Create(new DecisionBatchNotificationOptions { SenderEmail = "sender@example.com" });
        var clientFactoryMock = new Mock<IPostmarkClientFactory>();

        var sender = new PostmarkEmailSender(postmarkOptions, notificationOptions, clientFactoryMock.Object);

        var action = () => sender.SendAsync("recipient@example.com", "Subject", "Body");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Postmark ServerToken is required to send decision batch notifications.");
    }

    [TestMethod]
    public async Task SendAsync_Throws_InvalidOperationException_When_SenderEmail_Is_Missing()
    {
        var postmarkOptions = Options.Create(new PostmarkOptions { ServerToken = "token" });
        var notificationOptions = Options.Create(new DecisionBatchNotificationOptions { SenderEmail = "" });
        var clientFactoryMock = new Mock<IPostmarkClientFactory>();

        var sender = new PostmarkEmailSender(postmarkOptions, notificationOptions, clientFactoryMock.Object);

        var action = () => sender.SendAsync("recipient@example.com", "Subject", "Body");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DecisionBatchNotifications SenderEmail is required to send decision batch notifications.");
    }

    [TestMethod]
    public async Task SendAsync_Calls_PostmarkClient_With_Correct_Parameters()
    {
        var postmarkOptions = Options.Create(new PostmarkOptions { ServerToken = "test-token" });
        var notificationOptions = Options.Create(new DecisionBatchNotificationOptions { SenderEmail = "sender@example.com" });
        var clientMock = new Mock<IPostmarkClient>();
        var clientFactoryMock = new Mock<IPostmarkClientFactory>();

        clientFactoryMock.Setup(f => f.Create("test-token")).Returns(clientMock.Object);

        var sender = new PostmarkEmailSender(postmarkOptions, notificationOptions, clientFactoryMock.Object);

        await sender.SendAsync("recipient@example.com", "The Subject", "The Body");

        clientMock.Verify(c => c.Send(It.Is<Clc.Postmark.Models.EmailMessage>(m =>
            m.From == "sender@example.com" &&
            m.To.Contains("recipient@example.com") &&
            m.Subject == "The Subject" &&
            m.TextBody == "The Body"
        )), Times.Once);
    }
}
