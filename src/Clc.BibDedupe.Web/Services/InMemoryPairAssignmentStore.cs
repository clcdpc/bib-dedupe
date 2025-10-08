using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Clc.BibDedupe.Web.Services;

public class InMemoryPairAssignmentStore : IPairAssignmentStore
{
    private readonly ConcurrentDictionary<(int LeftBibId, int RightBibId), string> _assignments = new();

    public Task AssignAsync(string userId, int leftBibId, int rightBibId)
    {
        var key = (leftBibId, rightBibId);
        _assignments.AddOrUpdate(
            key,
            _ => userId,
            (_, existing) => string.Equals(existing, userId, StringComparison.OrdinalIgnoreCase) ? userId : existing);
        return Task.CompletedTask;
    }

    public Task ReleaseAsync(string userId, int leftBibId, int rightBibId)
    {
        var key = (leftBibId, rightBibId);
        if (_assignments.TryGetValue(key, out var existing) && string.Equals(existing, userId, StringComparison.OrdinalIgnoreCase))
        {
            _assignments.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }
}
