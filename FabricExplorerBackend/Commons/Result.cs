using FabricExplorerBackend.Enums;

namespace FabricExplorerBackend.Commons
{
    public class Result<T>
    {
        public bool IsSuccess => ResultStatus == ResultStatus.Success;
        public ResultStatus ResultStatus { get; set; }
        public T? Data { get; set; }
        public IList<string> Errors { get; set; } = null!;

        public Result() { }
        public Result(T? data = default)
        {
            ResultStatus = ResultStatus.Success;
            Data = data;
            Errors = null!;
        }
        public Result(ResultStatus resultStatus, IList<string> errors)
        {
            ResultStatus = resultStatus;
            Data = default;
            Errors = errors;
        }
        public Result(ResultStatus resultStatus, string error)
        {
            ResultStatus = resultStatus;
            Data = default;
            Errors = new List<string> { error };
        }

        public static Result<T> Success(T? data = default) => new Result<T>(data);
        public static Result<T> Failure(ResultStatus status, string error) => new Result<T>(status, errors: [error]);
        public static Result<T> Failure(ResultStatus status, IList<string> errors) => new Result<T>(status, errors: errors);
    }
}
