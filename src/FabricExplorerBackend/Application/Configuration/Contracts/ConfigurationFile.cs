namespace FabricExplorerBackend.Application.Configuration.Contracts
{
    public sealed record ConfigurationFile(byte[] Content, string FileName, string ContentType, int Exported, int Skipped);
}
