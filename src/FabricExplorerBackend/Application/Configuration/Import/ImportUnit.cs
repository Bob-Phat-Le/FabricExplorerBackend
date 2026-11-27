using FabricExplorerBackend.Application.Configuration.Contracts;
using FabricExplorerBackend.Application.Configuration.Documents;
using FabricExplorerBackend.Application.Configuration.Validation;

namespace FabricExplorerBackend.Application.Configuration.Import
{
    /// <summary>One entry waiting to be imported. Planning is separated from applying so the execution strategy decides how it runs.</summary>
    public sealed record ImportUnit(
        string Type,
        string? Key,
        string? Name,
        Func<ImportSession, CancellationToken, Task<ImportResultItem>> ApplyAsync)
    {
        /// <summary>An entry that failed validation: reported as failed without touching the database.</summary>
        public static ImportUnit Rejected(string type, string? key, string? name, ValidationIssue issue)
            => new(type, key, name, (_, _) => Task.FromResult(
                ImportResultItem.Failure(type, key, name, issue.Code, $"{issue.Field}: {issue.Message}")));
    }
}
