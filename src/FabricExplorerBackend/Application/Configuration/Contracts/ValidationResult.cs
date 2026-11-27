using FabricExplorerBackend.Application.Configuration.Validation;

namespace FabricExplorerBackend.Application.Configuration.Contracts
{
    public sealed record ValidationResult(bool Valid, IReadOnlyList<ValidationIssue> Errors);
}
