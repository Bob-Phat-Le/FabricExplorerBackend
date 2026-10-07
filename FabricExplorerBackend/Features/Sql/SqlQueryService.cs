using Microsoft.Data.SqlClient;
using System.Globalization;

namespace FabricExplorerBackend.Features.Sql
{
    public class SqlQueryService(
        ISqlConnectionFactory connectionFactory,
        IConfiguration configuration) : ISqlQueryService
    {
        public async Task<T?> ExecuteScalarAsync<T>(
            Domain.Entities.Connection connection,
            SqlEndpoint endpoint,
            string sql,
            IReadOnlyDictionary<string, object?>? parameters = null,
            CancellationToken cancellationToken = default)
        {
            await using var sqlConnection = await connectionFactory.OpenConnectionAsync(connection, endpoint, cancellationToken);
            await using var command = CreateCommand(sqlConnection, sql, parameters);

            var scalar = await command.ExecuteScalarAsync(cancellationToken);
            return ConvertScalar<T>(scalar);
        }

        public async Task<IReadOnlyList<T>> QueryAsync<T>(
            Domain.Entities.Connection connection,
            SqlEndpoint endpoint,
            string sql,
            Func<SqlDataReader, T> map,
            IReadOnlyDictionary<string, object?>? parameters = null,
            CancellationToken cancellationToken = default)
        {
            await using var sqlConnection = await connectionFactory.OpenConnectionAsync(connection, endpoint, cancellationToken);
            await using var command = CreateCommand(sqlConnection, sql, parameters);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var rows = new List<T>();
            while (await reader.ReadAsync(cancellationToken))
                rows.Add(map(reader));

            return rows;
        }

        private SqlCommand CreateCommand(
            SqlConnection sqlConnection, string sql, IReadOnlyDictionary<string, object?>? parameters)
        {
            var command = new SqlCommand(sql, sqlConnection)
            {
                CommandTimeout = configuration.GetValue("Sql:CommandTimeoutSeconds", 60)
            };

            if (parameters != null)
            {
                foreach (var (name, value) in parameters)
                {
                    var parameterName = name.StartsWith('@') ? name : "@" + name;
                    command.Parameters.AddWithValue(parameterName, value ?? DBNull.Value);
                }
            }

            return command;
        }

        private static T? ConvertScalar<T>(object? scalar)
        {
            if (scalar is null or DBNull)
                return default;

            // Hỗ trợ cả kiểu nullable, ví dụ ExecuteScalarAsync<long?>
            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return (T)Convert.ChangeType(scalar, targetType, CultureInfo.InvariantCulture);
        }
    }
}
