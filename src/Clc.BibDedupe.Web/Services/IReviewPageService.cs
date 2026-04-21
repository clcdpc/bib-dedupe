using Clc.BibDedupe.Web.Models;

namespace Clc.BibDedupe.Web.Services;

public interface IReviewPageService
{
    Task<ReviewPageResult?> BuildAsync(string userEmail, int? leftBibId, int? rightBibId);
}
