using System.Data.Common;
using Microsoft.Extensions.Options;
using NPoco;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Infrastructure.Persistence.FaultHandling;
using Umbraco.Extensions;

namespace Umbraco.Cms.Persistence.Postgresql.Interceptors;

public class PostgreSQLAddRetryPolicyInterceptor : PostgreSQLConnectionInterceptor
{
    private readonly IOptionsMonitor<ConnectionStrings> _connectionStrings;

    public PostgreSQLAddRetryPolicyInterceptor(IOptionsMonitor<ConnectionStrings> connectionStrings)
        => _connectionStrings = connectionStrings;

    public override DbConnection OnConnectionOpened(IDatabase database, DbConnection conn)
    {
        if (!_connectionStrings.CurrentValue.IsConnectionStringConfigured())
        {
            return conn;
        }

        RetryPolicy? connectionRetryPolicy = RetryPolicyFactory.GetDefaultSqlConnectionRetryPolicyByConnectionString(_connectionStrings.CurrentValue.ConnectionString);
        RetryPolicy? commandRetryPolicy = RetryPolicyFactory.GetDefaultSqlCommandRetryPolicyByConnectionString(_connectionStrings.CurrentValue.ConnectionString);

        if (connectionRetryPolicy == null && commandRetryPolicy == null)
        {
            return conn;
        }

        return new RetryDbConnection(conn, connectionRetryPolicy, commandRetryPolicy);
    }
}
