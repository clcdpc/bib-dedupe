using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Clc.BibDedupe.Web.Services;

public class InMemoryPairAssignmentStore : IPairAssignmentStore
{
    private readonly ConcurrentDictionary<(int LeftBibId, int RightBibId), Assignment> _assignments = new();

    private record Assignment(string UserEmail, DateTimeOffset AssignedAt);

    public Task AssignAsync(string userId, int leftBibId, int rightBibId)
    {
        var key = (leftBibId, rightBibId);
        _assignments.AddOrUpdate(
            key,
            _ => new Assignment(userId, DateTimeOffset.UtcNow),
            (_, existing) => string.Equals(existing.UserEmail, userId, StringComparison.OrdinalIgnoreCase)
                ? existing with { AssignedAt = DateTimeOffset.UtcNow }
                : existing);
        return Task.CompletedTask;
    }

    public Task ReleaseAsync(string userId, int leftBibId, int rightBibId)
    {
        var key = (leftBibId, rightBibId);
        if (_assignments.TryGetValue(key, out var existing) &&
            string.Equals(existing.UserEmail, userId, StringComparison.OrdinalIgnoreCase))
        {
            _assignments.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    public Task<int> ReleaseExpiredAsync(DateTimeOffset olderThan)
    {
        var removed = 0;

        foreach (var kvp in _assignments)
        {
            if (kvp.Value.AssignedAt < olderThan && _assignments.TryRemove(kvp.Key, out _))
            {
                removed++;
            }
        }

        return Task.FromResult(removed);
    }
}
