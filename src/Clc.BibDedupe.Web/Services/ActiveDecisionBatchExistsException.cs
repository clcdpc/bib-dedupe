namespace Clc.BibDedupe.Web.Services;

public class ActiveDecisionBatchExistsException : Exception
{
    public ActiveDecisionBatchExistsException(string userEmail)
        : base($"An active batch already exists for {userEmail}.")
    {
    }

    public ActiveDecisionBatchExistsException(string userEmail, Exception innerException)
        : base($"An active batch already exists for {userEmail}.", innerException)
    {
    }
}
