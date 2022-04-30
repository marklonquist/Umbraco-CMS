using System.Data.Common;
using Microsoft.Extensions.Options;
using NPoco;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Infrastructure.Persistence.FaultHandling;
using Umbraco.Cms.Persistence.Postgresql.Services;
using Umbraco.Extensions;

namespace Umbraco.Cms.Persistence.Postgresql.Interceptors;

public class AddRetryPolicyInterceptor : ConnectionInterceptor
{
    private readonly IOptionsMonitor<ConnectionStrings> _connectionStrings;

    public AddRetryPolicyInterceptor(IOptionsMonitor<ConnectionStrings> connectionStrings)
        => _connectionStrings = connectionStrings;

    public override DbConnection OnConnectionOpened(IDatabase database, DbConnection conn)
    {
        if (!_connectionStrings.CurrentValue.IsConnectionStringConfigured())
        {
            return conn;
        }

        RetryStrategy retryStrategy = RetryStrategy.DefaultExponential;
        var commandRetryPolicy = new RetryPolicy(new TransientErrorDetectionStrategy(), retryStrategy);

        return new RetryDbConnection(conn, null, commandRetryPolicy);
    }
}
