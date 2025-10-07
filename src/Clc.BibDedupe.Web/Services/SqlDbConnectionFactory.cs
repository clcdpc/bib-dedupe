using System.Data;
using Microsoft.Data.SqlClient;

namespace Clc.BibDedupe.Web.Services;

public class SqlDbConnectionFactory(string connectionString)
    : IDbConnectionFactory, IDecisionProcessingDbConnectionFactory
{
    public IDbConnection Create()
    {
        var connection = new SqlConnection(connectionString);
        return connection;
    }
}
