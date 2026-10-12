namespace FabricExplorerBackend.Api.Contracts
{
    public sealed record ApiError(string Code, string Message, string TraceId, object? Details = null);

    public class ApiResponse<T>
    {
        public int StatusCode { get; set; }
        public T? Data { get; set; }
        public IList<string>? Errors { get; set; }   // giữ nguyên (null khi thành công) để FE hiện tại không vỡ
        public ApiError? Error { get; set; }          // mới: FE chuyển dần sang đọc error.code và error.traceId

        public static ApiResponse<T> Ok(T? data) => new() { StatusCode = 200, Data = data };
        public static ApiResponse<T> Fail(int status, ApiError error)
            => new() { StatusCode = status, Errors = [error.Message], Error = error };
    }
}
