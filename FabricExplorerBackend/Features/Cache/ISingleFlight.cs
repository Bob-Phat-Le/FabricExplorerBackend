namespace FabricExplorerBackend.Features.Cache
{
    /// <summary>
    /// Gộp các lời gọi đồng thời có cùng key thành MỘT lần thực thi: người đến sau chờ kết quả của người đến trước
    /// thay vì tự chạy lại (vd. 5 request cùng thấy cache token trống -> chỉ 1 request đi xin token).
    ///
    /// QUY TẮC cho <c>work</c> (vì nó chạy tách khỏi request của người gọi đầu tiên):
    ///  - chỉ được dùng dữ liệu thuần (id, entity đã load) và các service không giữ tài nguyên theo request;
    ///  - KHÔNG dùng IUnitOfWork/DbContext/HttpContext: scope của request đầu có thể đã bị dispose khi work còn chạy;
    ///  - không dùng CancellationToken của request: hủy một request không được làm hỏng kết quả của những người đang chờ.
    ///
    /// Mỗi key chỉ được dùng với MỘT kiểu T.
    /// </summary>
    public interface ISingleFlight
    {
        /// <param name="cancellationToken">Chỉ hủy việc CHỜ của người gọi này; work vẫn chạy tiếp cho những người còn lại.</param>
        Task<T> RunAsync<T>(string key, Func<Task<T>> work, CancellationToken cancellationToken = default);
    }
}
