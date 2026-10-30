using FabricExplorerBackend.Application.QueryService;
using FabricExplorerBackend.Infrastructures.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data.Common;

namespace FabricExplorerBackend.Infrastructures.Sql
{
    /// <summary>Executes SQL on a Fabric SQL endpoint and hands out the streaming reader (no buffering).</summary>
    public sealed class FabricQueryExecutor(
        ISqlConnectionFactory connectionFactory,
        IOptions<QueryOptions> options) : IQueryExecutor
    {
        public async Task<IQueryExecution> ExecuteAsync(
            Domain.Entities.Connection connection,
            SqlEndpoint endpoint,
            string sql,
            CancellationToken cancellationToken = default)
        {
            var sqlConnection = await connectionFactory.OpenConnectionAsync(connection, endpoint, cancellationToken);
            try
            {
                var command = new SqlCommand(sql, sqlConnection)
                {
                    CommandTimeout = (int)Math.Ceiling(options.Value.CommandTimeout.TotalSeconds)
                };

                try
                {
                    var reader = await command.ExecuteReaderAsync(cancellationToken);
                    return new Execution(sqlConnection, command, reader);
                }
                catch
                {
                    await command.DisposeAsync();
                    throw;
                }
            }
            catch
            {
                await sqlConnection.DisposeAsync();
                throw;
            }
        }

        private sealed class Execution(SqlConnection connection, SqlCommand command, SqlDataReader reader) : IQueryExecution
        {
            public DbDataReader Reader => reader;

            public async ValueTask DisposeAsync()
            {
                // Closing a reader that was not read to the end would drain the remaining rows. When the
                // result was truncated (row limit) or the query cancelled that can be minutes of useless work,
                // so cancel the command first. It is a no-op when everything was already read.
                try { command.Cancel(); } catch { /* best effort */ }

                await reader.DisposeAsync();
                await command.DisposeAsync();
                await connection.DisposeAsync();
            }
        }
    }
}
