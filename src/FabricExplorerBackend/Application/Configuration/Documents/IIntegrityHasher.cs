namespace FabricExplorerBackend.Application.Configuration.Documents
{
    public interface IIntegrityHasher
    {
        ConfigurationIntegrity Compute(ConfigurationDocument document);

        /// <summary>true when the file has no integrity block or the block matches the content.</summary>
        bool Verify(ConfigurationDocument document);
    }
}
