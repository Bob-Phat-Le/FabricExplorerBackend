using Microsoft.Data.SqlClient;

namespace FabricExplorerBackend.Features.Sql
{
    /// <summary>
    /// Thực thi truy vấn chỉ-đọc lên SQL endpoint của Fabric. Mỗi lời gọi mở một kết nối riêng
    /// (SqlClient tự dùng connection pool) và đóng ngay khi xong, nên an toàn khi gọi song song.
    /// Lỗi của SQL Server được ném nguyên dạng (SqlException) để người gọi tự quyết định cách xử lý.
    /// </summary>
    public interface ISqlQueryService
    {
        /// <summary>
        /// Chạy câu lệnh và trả về giá trị ở cột đầu tiên của dòng đầu tiên, ép sang kiểu T.
        /// Trả về default nếu không có dòng nào hoặc giá trị là NULL.
        /// </summary>
        Task<T?> ExecuteScalarAsync<T>(
            Domain.Entities.Connection connection,
            SqlEndpoint endpoint,
            string sql,
            IReadOnlyDictionary<string, object?>? parameters = null,
            CancellationToken cancellationToken = default);

        /// <summary>Chạy câu lệnh và dùng <paramref name="map"/> để đổi từng dòng kết quả thành T.</summary>
        Task<IReadOnlyList<T>> QueryAsync<T>(
            Domain.Entities.Connection connection,
            SqlEndpoint endpoint,
            string sql,
            Func<SqlDataReader, T> map,
            IReadOnlyDictionary<string, object?>? parameters = null,
            CancellationToken cancellationToken = default);
    }
}
