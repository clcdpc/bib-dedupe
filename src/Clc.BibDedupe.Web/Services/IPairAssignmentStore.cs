using System;
using System.Threading.Tasks;

namespace Clc.BibDedupe.Web.Services;

public interface IPairAssignmentStore
{
    Task AssignAsync(string userId, int leftBibId, int rightBibId);
    Task ReleaseAsync(string userId, int leftBibId, int rightBibId);
    Task<int> ReleaseExpiredAsync(DateTimeOffset olderThan);
}
