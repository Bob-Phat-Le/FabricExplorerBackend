namespace FabricExplorerBackend.Commons.Results
{
    public class Result<T>
    {
        public bool IsSuccess => ResultStatus == ErrorKind.Success;
        public ErrorKind ResultStatus { get; set; }
        public T? Data { get; set; }
        public IList<string> Errors { get; set; } = null!;

        public Result() { }
        public Result(T? data = default)
        {
            ResultStatus = ErrorKind.Success;
            Data = data;
            Errors = null!;
        }
        public Result(ErrorKind resultStatus, IList<string> errors)
        {
            ResultStatus = resultStatus;
            Data = default;
            Errors = errors;
        }
        public Result(ErrorKind resultStatus, string error)
        {
            ResultStatus = resultStatus;
            Data = default;
            Errors = new List<string> { error };
        }

        public static Result<T> Success(T? data = default) => new Result<T>(data);
        public static Result<T> Failure(ErrorKind status, string error) => new Result<T>(status, errors: [error]);
        public static Result<T> Failure(ErrorKind status, IList<string> errors) => new Result<T>(status, errors: errors);
    }
}
