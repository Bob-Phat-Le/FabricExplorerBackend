namespace FabricExplorerBackend.Application.Configuration.Contracts
{
    public sealed record ImportResultItem(
        string Type,
        string? Key,
        string? Name,
        string Action,
        Guid? Id,
        bool Success,
        ImportItemError? Error,
        IReadOnlyList<string> Flags)
    {
        public const string Created = "created";
        public const string Updated = "updated";
        public const string Skipped = "skipped";
        public const string Failed = "failed";

        public static ImportResultItem Succeeded(string type, string? key, string? name, string action, Guid id, IReadOnlyList<string> flags)
            => new(type, key, name, action, id, true, null, flags);

        public static ImportResultItem Failure(string type, string? key, string? name, string code, string message)
            => new(type, key, name, Failed, null, false, new ImportItemError(code, message), []);

        /// <summary>Not imported because a reference could not be resolved: counted as skipped but not as a success.</summary>
        public static ImportResultItem Unresolved(string type, string? key, string? name, string message)
            => new(type, key, name, Skipped, null, false, new ImportItemError("reference_unresolved", message), []);
    }
}
