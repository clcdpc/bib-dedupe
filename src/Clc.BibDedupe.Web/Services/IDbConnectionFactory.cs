using System.Data;

namespace Clc.BibDedupe.Web.Services;

public interface IDbConnectionFactory
{
    IDbConnection Create();
}
