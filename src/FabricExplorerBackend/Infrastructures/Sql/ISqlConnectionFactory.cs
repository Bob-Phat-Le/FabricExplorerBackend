using Microsoft.Data.SqlClient;

namespace FabricExplorerBackend.Infrastructures.Sql
{
    public interface ISqlConnectionFactory
    {
        Task<string> GetAccessTokenAsync(Domain.Entities.Connection connection, CancellationToken cancellationToken = default);

        Task<SqlConnection> OpenConnectionAsync(
            Domain.Entities.Connection connection,
            SqlEndpoint endpoint,
            CancellationToken cancellationToken = default);

        Task<SqlConnection> OpenConnectionAsync(
            string accessToken,
            SqlEndpoint endpoint,
            CancellationToken cancellationToken = default);
    }
}
