using Microsoft.Data.SqlClient;

namespace FabricExplorerBackend.Features.Sql
{
    public interface ISqlConnectionFactory
    {
        /// <summary>Lấy access token (scope SQL) của connection. Token được cache bởi ITokenService.</summary>
        Task<string> GetAccessTokenAsync(Domain.Entities.Connection connection, CancellationToken cancellationToken = default);

        /// <summary>Lấy token từ connection rồi mở kết nối tới endpoint. Người gọi chịu trách nhiệm dispose kết nối.</summary>
        Task<SqlConnection> OpenConnectionAsync(
            Domain.Entities.Connection connection,
            SqlEndpoint endpoint,
            CancellationToken cancellationToken = default);

        /// <summary>Mở kết nối bằng token đã có sẵn (hữu ích khi chạy nhiều truy vấn liên tiếp với cùng một token).</summary>
        Task<SqlConnection> OpenConnectionAsync(
            string accessToken,
            SqlEndpoint endpoint,
            CancellationToken cancellationToken = default);
    }
}
