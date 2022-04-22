using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Cms.Persistence.Postgresql;

/// <summary>
/// Automatically adds PostgreSQL support to Umbraco when this project is referenced.
/// </summary>
public class PostgresqlComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
        => builder.AddUmbracoPostgresqlSupport();
}
