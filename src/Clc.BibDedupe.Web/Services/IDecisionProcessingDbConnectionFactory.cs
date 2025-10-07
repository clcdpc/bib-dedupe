using System.Data;

namespace Clc.BibDedupe.Web.Services;

public interface IDecisionProcessingDbConnectionFactory
{
    IDbConnection Create();
}
