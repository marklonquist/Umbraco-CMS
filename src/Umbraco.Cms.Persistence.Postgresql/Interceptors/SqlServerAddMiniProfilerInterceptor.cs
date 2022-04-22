using System.Data.Common;
using NPoco;
using StackExchange.Profiling;

namespace Umbraco.Cms.Persistence.Postgresql.Interceptors;

public class PostgreSQLAddMiniProfilerInterceptor : PostgreSQLConnectionInterceptor
{
    public override DbConnection OnConnectionOpened(IDatabase database, DbConnection conn)
        => new StackExchange.Profiling.Data.ProfiledDbConnection(conn, MiniProfiler.Current);
}
