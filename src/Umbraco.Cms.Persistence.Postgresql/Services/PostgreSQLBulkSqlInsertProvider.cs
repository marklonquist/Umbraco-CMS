using System.Data;
using Microsoft.Data.SqlClient;
using NPoco;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Extensions;

namespace Umbraco.Cms.Persistence.Postgresql.Services
{
    /// <summary>
    /// A bulk sql insert provider for Sql Server
    /// </summary>
    public class PostgreSQLBulkSqlInsertProvider : IBulkSqlInsertProvider
    {
        public string ProviderName => Constants.ProviderName;

        public int BulkInsertRecords<T>(IUmbracoDatabase database, IEnumerable<T> records)
        {
            var recordsA = records.ToArray();
            if (recordsA.Length == 0) return 0;

            var pocoData = database.PocoDataFactory.ForType(typeof(T));
            if (pocoData == null) throw new InvalidOperationException("Could not find PocoData for " + typeof(T));

            return BulkInsertRecordsPostgreSQL(database, pocoData, recordsA);
        }

        /// <summary>
        /// Bulk-insert records using PostgreSQL BulkCopy method.
        /// </summary>
        /// <typeparam name="T">The type of the records.</typeparam>
        /// <param name="database">The database.</param>
        /// <param name="pocoData">The PocoData object corresponding to the record's type.</param>
        /// <param name="records">The records.</param>
        /// <returns>The number of records that were inserted.</returns>
        private int BulkInsertRecordsPostgreSQL<T>(IUmbracoDatabase database, PocoData pocoData, IEnumerable<T> records)
        {
            // TODO: The main reason this exists is because the NPoco InsertBulk method doesn't return the number of items.
            // It is worth investigating the performance of this vs NPoco's because we use a custom BulkDataReader
            // which in theory should be more efficient than NPocos way of building up an in-memory DataTable.

            // create command against the original database.Connection
            using (var command = database.CreateCommand(database.Connection, CommandType.Text, string.Empty))
            {
                // use typed connection and transaction or SqlBulkCopy
                var tConnection = NPocoDatabaseExtensions.GetTypedConnection<SqlConnection>(database.Connection);
                var tTransaction = NPocoDatabaseExtensions.GetTypedTransaction<SqlTransaction>(command.Transaction);
                var tableName = pocoData.TableInfo.TableName;

                var syntax = database.SqlContext.SqlSyntax as PostgreSQLSyntaxProvider;
                if (syntax == null) throw new NotSupportedException("SqlSyntax must be PostgreSQLSyntaxProvider.");

                using (var copy = new SqlBulkCopy(tConnection, SqlBulkCopyOptions.Default, tTransaction)
                {
                    BulkCopyTimeout = 0, // 0 = no bulk copy timeout. If a timeout occurs it will be an connection/command timeout.
                    DestinationTableName = tableName,
                    // be consistent with NPoco: https://github.com/schotime/NPoco/blob/5117a55fde57547e928246c044fd40bd00b2d7d1/src/NPoco.PostgreSQL/SqlBulkCopyHelper.cs#L50
                    BatchSize = 4096
                })
                using (var bulkReader = new PocoDataDataReader<T, PostgreSQLSyntaxProvider>(records, pocoData, syntax))
                {
                    //we need to add column mappings here because otherwise columns will be matched by their order and if the order of them are different in the DB compared
                    //to the order in which they are declared in the model then this will not work, so instead we will add column mappings by name so that this explicitly uses
                    //the names instead of their ordering.
                    foreach (var col in bulkReader.ColumnMappings)
                    {
                        copy.ColumnMappings.Add(col.DestinationColumn, col.DestinationColumn);
                    }

                    copy.WriteToServer(bulkReader);
                    return bulkReader.RecordsAffected;
                }
            }
        }
    }
}
