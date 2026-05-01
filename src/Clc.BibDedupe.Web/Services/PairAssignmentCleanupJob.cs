using System;
using System.Threading.Tasks;
using Clc.BibDedupe.Web.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Clc.BibDedupe.Web.Services;

public class PairAssignmentCleanupJob(
    IPairAssignmentStore pairAssignmentStore,
    IOptions<PairAssignmentCleanupOptions> options,
    ILogger<PairAssignmentCleanupJob> logger)
{
    public async Task CleanupAsync()
    {
        var minimumAge = options.Value.MinimumAssignmentAge;

        if (minimumAge <= TimeSpan.Zero)
        {
            logger.LogInformation("Skipping pair assignment cleanup because the minimum age {MinimumAge} is not positive.", minimumAge);
            return;
        }

        var cutoff = DateTimeOffset.Now - minimumAge;
        var released = await pairAssignmentStore.ReleaseExpiredAsync(cutoff);

        if (released > 0)
        {
            logger.LogInformation(
                "Released {ReleasedCount} pair assignments older than {Cutoff:O}.",
                released,
                cutoff);
        }
        else
        {
            logger.LogDebug("No pair assignments older than {Cutoff:O} were found for cleanup.", cutoff);
        }
    }
}
