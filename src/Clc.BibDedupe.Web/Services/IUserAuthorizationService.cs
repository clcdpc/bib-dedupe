namespace Clc.BibDedupe.Web.Services;

public interface IUserAuthorizationService
{
    Task<bool> IsAuthorizedAsync(string email);
}
