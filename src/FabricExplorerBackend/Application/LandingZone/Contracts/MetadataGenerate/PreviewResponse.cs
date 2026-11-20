namespace FabricExplorerBackend.Application.LandingZone.Contracts.MetadataGenerate
{
    /// <summary>
    /// Result of generating _metadata.json. A request that fails validation is still answered with 200:
    /// <see cref="IsValid"/> is false, <see cref="Errors"/> lists every problem and <see cref="Json"/> is empty.
    /// </summary>
    public record PreviewResponse(
        string FileName,
        string Json,
        bool IsValid,
        IReadOnlyList<string> Errors
    );
}
