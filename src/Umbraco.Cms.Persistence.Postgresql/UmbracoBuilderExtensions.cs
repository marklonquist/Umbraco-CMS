using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.DistributedLocking;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Persistence.SqlSyntax;
using Umbraco.Cms.Persistence.Postgresql.Interceptors;
using Umbraco.Cms.Persistence.Postgresql.Services;

namespace Umbraco.Cms.Persistence.Postgresql;

/// <summary>
/// PostgreSQL support extensions for IUmbracoBuilder.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Add required services for PostgreSQL support.
    /// </summary>
    public static IUmbracoBuilder AddUmbracoPostgresqlSupport(this IUmbracoBuilder builder)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ISqlSyntaxProvider, PostgreSQLSyntaxProvider>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IBulkSqlInsertProvider, PostgreSQLBulkSqlInsertProvider>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IDatabaseCreator, PostgreSQLDatabaseCreator>());

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IDatabaseProviderMetadata, PostgreSQLDatabaseProviderMetadata>());

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IDistributedLockingMechanism, PostgreSQLDistributedLockingMechanism>());

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IProviderSpecificInterceptor, PostgreSQLAddMiniProfilerInterceptor>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IProviderSpecificInterceptor, PostgreSQLAddRetryPolicyInterceptor>());

        DbProviderFactories.UnregisterFactory(Constants.ProviderName);
        DbProviderFactories.RegisterFactory(Constants.ProviderName, NpgsqlFactory.Instance);

        return builder;
    }
}
