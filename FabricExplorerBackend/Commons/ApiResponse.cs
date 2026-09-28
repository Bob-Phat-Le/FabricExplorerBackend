namespace FabricExplorerBackend.Commons
{
    public class ApiResponse<T>
    {
        public bool IsSuccess { get; set; }
        public int StatusCode { get; set; }
        public T? Data { get; set; }
        public IList<string> Errors { get; set; } = null!;

        public ApiResponse() { }
        public ApiResponse(int statusCode, T? data = default)
        {
            StatusCode = statusCode;
            IsSuccess = true;
            Data = data;
            Errors = null!;
        }
        public ApiResponse(int statusCode, IList<string> errors)
        {
            IsSuccess = false;
            StatusCode = statusCode;
            Data = default;
            Errors = errors;
        }
        public ApiResponse(int statusCode, string error)
        {
            IsSuccess = false;
            StatusCode = statusCode;
            Data = default;
            Errors = new List<string> { error };
        }
    }
}
