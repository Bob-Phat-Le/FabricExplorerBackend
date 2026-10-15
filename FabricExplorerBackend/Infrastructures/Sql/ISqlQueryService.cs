using Microsoft.Data.SqlClient;

namespace FabricExplorerBackend.Infrastructures.Sql
{
    public interface ISqlQueryService
    {
        Task<T?> ExecuteScalarAsync<T>(
            Domain.Entities.Connection connection,
            SqlEndpoint endpoint,
            string sql,
            IReadOnlyDictionary<string, object?>? parameters = null,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<T>> QueryAsync<T>(
            Domain.Entities.Connection connection,
            SqlEndpoint endpoint,
            string sql,
            Func<SqlDataReader, T> map,
            IReadOnlyDictionary<string, object?>? parameters = null,
            CancellationToken cancellationToken = default);
    }
}
