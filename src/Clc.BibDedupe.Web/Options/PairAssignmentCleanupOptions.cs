using System;

namespace Clc.BibDedupe.Web.Options;

public class PairAssignmentCleanupOptions
{
    public TimeSpan MinimumAssignmentAge { get; set; } = TimeSpan.FromHours(6);
}
