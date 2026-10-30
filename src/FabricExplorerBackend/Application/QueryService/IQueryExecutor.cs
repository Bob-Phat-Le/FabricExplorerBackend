using FabricExplorerBackend.Infrastructures.Sql;
using System.Data.Common;

namespace FabricExplorerBackend.Application.QueryService
{
    /// <summary>Runs SQL against an endpoint and exposes the streaming reader. It never materializes the result.</summary>
    public interface IQueryExecutor
    {
        Task<IQueryExecution> ExecuteAsync(
            Domain.Entities.Connection connection,
            SqlEndpoint endpoint,
            string sql,
            CancellationToken cancellationToken = default);
    }

    /// <summary>Owns the SQL connection/command/reader. Disposing it releases all of them.</summary>
    public interface IQueryExecution : IAsyncDisposable
    {
        DbDataReader Reader { get; }
    }
}
