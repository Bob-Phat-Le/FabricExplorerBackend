namespace FabricExplorerBackend.Commons.Results
{
    public sealed class Result<T>
    {
        public bool IsSuccess => !Errors.Any();
        public T? Data { get; set; }
        public IReadOnlyList<Error> Errors { get; private set; } = [];

        //[Obsolete]
        //public static Result<T> Failure(ErrorKind status, string error) => new Result<T>(status, errors: [error]);

        public static Result<T> Success(T? data = default) => new() { Data = data };
        public static Result<T> Failure(params Error[] errors) => new() { Errors = errors };
        public static implicit operator Result<T>(Error error) => Failure(error);
    }
}
