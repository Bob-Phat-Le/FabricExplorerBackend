using FabricExplorerBackend.Features.Token;
using Microsoft.Data.SqlClient;

namespace FabricExplorerBackend.Features.Sql
{
    public class SqlConnectionFactory(
        IConfiguration configuration,
        ITokenService tokenService) : ISqlConnectionFactory
    {
        public Task<string> GetAccessTokenAsync(Domain.Entities.Connection connection)
        {
            // Mỗi dịch vụ có audience riêng nên token SQL khác token của Fabric API;
            // TokenService phải cache theo cả scope thì hai loại token này mới không lẫn vào nhau
            var scope = configuration.GetValue("Scopes:sql", "https://database.windows.net/.default")!;
            return tokenService.GetOrCreateTokenAsync(connection, scope);
        }

        public async Task<SqlConnection> OpenConnectionAsync(
            Domain.Entities.Connection connection, SqlEndpoint endpoint, CancellationToken cancellationToken = default)
        {
            var accessToken = await GetAccessTokenAsync(connection);
            return await OpenConnectionAsync(accessToken, endpoint, cancellationToken);
        }

        public async Task<SqlConnection> OpenConnectionAsync(
            string accessToken, SqlEndpoint endpoint, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(endpoint.Server))
                throw new ArgumentException("SQL endpoint server is required.", nameof(endpoint));
            if (string.IsNullOrWhiteSpace(endpoint.Database))
                throw new ArgumentException("SQL endpoint database is required.", nameof(endpoint));

            // Dùng builder thay vì ghép chuỗi để tên server/database không làm hỏng connection string
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = endpoint.Server,
                InitialCatalog = endpoint.Database,
                Encrypt = true,
                ConnectTimeout = configuration.GetValue("Sql:ConnectTimeoutSeconds", 30),
                ApplicationName = "FabricExplorerBackend"
            };

            var sqlConnection = new SqlConnection(builder.ConnectionString) { AccessToken = accessToken };
            try
            {
                await sqlConnection.OpenAsync(cancellationToken);
                return sqlConnection;
            }
            catch
            {
                await sqlConnection.DisposeAsync();
                throw;
            }
        }
    }
}
