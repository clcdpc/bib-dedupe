using System;

namespace Clc.BibDedupe.Web.Services;

public class DecisionConflictException : Exception
{
    public DecisionConflictException(int conflictingBibId)
        : base($"Bib {conflictingBibId} is already part of another merge decision. Remove the existing decision before merging this bib again.")
    {
        ConflictingBibId = conflictingBibId;
    }

    public DecisionConflictException(int conflictingBibId, string message)
        : base(message)
    {
        ConflictingBibId = conflictingBibId;
    }

    public int ConflictingBibId { get; }
}
