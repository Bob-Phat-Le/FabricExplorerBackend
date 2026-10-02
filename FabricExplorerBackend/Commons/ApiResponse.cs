namespace FabricExplorerBackend.Commons
{
    public class ApiResponse<T>
    {
        public int StatusCode { get; set; }
        public T? Data { get; set; }
        public IList<string> Errors { get; set; } = null!;

        public ApiResponse() { }
        public ApiResponse(int statusCode, T? data = default)
        {
            StatusCode = statusCode;
            Data = data;
            Errors = null!;
        }
        public ApiResponse(int statusCode, IList<string> errors)
        {
            StatusCode = statusCode;
            Data = default;
            Errors = errors;
        }
        public ApiResponse(int statusCode, string error)
        {
            StatusCode = statusCode;
            Data = default;
            Errors = new List<string> { error };
        }
    }
}
