using System;

namespace Clc.BibDedupe.Web.Services;

public class ConflictingMergeDecisionException : Exception
{
    public ConflictingMergeDecisionException(string message) : base(message)
    {
    }
}
