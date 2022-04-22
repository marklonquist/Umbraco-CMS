using System.Runtime.Serialization;
using Umbraco.Cms.Core.Install.Models;
using Umbraco.Cms.Infrastructure.Persistence;

namespace Umbraco.Cms.Persistence.Postgresql.Services;

/// <summary>
/// Provider metadata for PostgreSQL
/// </summary>
[DataContract]
public class PostgreSQLDatabaseProviderMetadata : IDatabaseProviderMetadata
{
    /// <inheritdoc />
    public Guid Id => new ("771ebf93-c469-4e71-82a9-170485bcc007");

    /// <inheritdoc />
    public int SortOrder => 2;

    /// <inheritdoc />
    public string DisplayName => "PostgreSQL";

    /// <inheritdoc />
    public string DefaultDatabaseName => string.Empty;

    /// <inheritdoc />
    public string ProviderName => Constants.ProviderName;

    /// <inheritdoc />
    public bool SupportsQuickInstall => false;

    /// <inheritdoc />
    public bool IsAvailable => true;

    /// <inheritdoc />
    public bool RequiresServer => true;

    /// <inheritdoc />
    public string ServerPlaceholder => string.Empty;

    /// <inheritdoc />
    public bool RequiresCredentials => true;

    /// <inheritdoc />
    public bool SupportsIntegratedAuthentication => true;

    /// <inheritdoc />
    public bool RequiresConnectionTest => true;

    /// <inheritdoc />
    public bool ForceCreateDatabase => false;

    /// <inheritdoc />
    public string GenerateConnectionString(DatabaseModel databaseModel) =>
        $"Server={databaseModel.Server};Port={databaseModel.Server};Database={databaseModel.DatabaseName};User Id={databaseModel.Login};Password={databaseModel.Password};Command Timeout=5;";
}
