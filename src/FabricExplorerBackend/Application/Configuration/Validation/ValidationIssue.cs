namespace FabricExplorerBackend.Application.Configuration.Validation
{
    /// <summary>One problem found in an entry. <see cref="Field"/> is the JSON property it is about.</summary>
    public sealed record ValidationIssue(string Code, string Field, string Message);
}
