using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.DistributedLocking;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Persistence.SqlSyntax;
using Umbraco.Cms.Persistence.Postgresql.Interceptors;
using Umbraco.Cms.Persistence.Postgresql.Services;
using Umbraco.Extensions;

namespace Umbraco.Cms.Persistence.Postgresql;

/// <summary>
/// Postgresql support extensions for IUmbracoBuilder.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Add required services for Postgresql support.
    /// </summary>
    public static IUmbracoBuilder AddUmbracoPostgresqlSupport(this IUmbracoBuilder builder)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ISqlSyntaxProvider, SyntaxProvider>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IBulkSqlInsertProvider, BulkSqlInsertProvider>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IDatabaseCreator, DatabaseCreator>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IDatabaseProviderMetadata, DatabaseProviderMetadata>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IDistributedLockingMechanism, DistributedLockingMechanism>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IProviderSpecificInterceptor, AddMiniProfilerInterceptor>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IProviderSpecificInterceptor, AddRetryPolicyInterceptor>());

        DbProviderFactories.UnregisterFactory(Constants.ProviderName);
        DbProviderFactories.RegisterFactory(Constants.ProviderName, NpgsqlFactory.Instance);

        return builder;
    }
}
