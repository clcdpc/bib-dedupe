namespace Clc.BibDedupe.Web.Options;

public class DecisionBatchNotificationOptions
{
    public const string SectionName = "DecisionBatchNotifications";

    public bool Enabled { get; set; }
    public string? SenderEmail { get; set; }
}
