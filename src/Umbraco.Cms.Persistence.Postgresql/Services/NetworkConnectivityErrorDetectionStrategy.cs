using Npgsql;
using Umbraco.Cms.Infrastructure.Persistence.FaultHandling;

namespace Umbraco.Cms.Persistence.Postgresql.Services;

public class NetworkConnectivityErrorDetectionStrategy : ITransientErrorDetectionStrategy
{
    public bool IsTransient(Exception ex)
    {
        if (ex is not NpgsqlException npgsqlException)
        {
            return false;
        }


    }
}
