namespace FabricExplorerBackend.Application.LandingZone.Contracts
{
    public sealed class UploadLandingZoneFilesRequest
    {
        // Destination folder, relative to the Landing Zone root ("" = root).
        public string? Path { get; set; }

        // Replace files that already exist. Default: existing files are reported as failed.
        public bool Overwrite { get; set; }

        public IFormFileCollection Files { get; set; } = null!;
    }
}
