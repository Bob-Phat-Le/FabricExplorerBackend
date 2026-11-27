namespace FabricExplorerBackend.Application.Configuration.Contracts
{
    public sealed class ImportOptions
    {
        /// <summary>File connection key -> id of an existing connection. Lets saved queries point at another connection.</summary>
        public Dictionary<string, Guid>? ReferenceMappings { get; set; }
        /// <summary>All or nothing instead of entry by entry.</summary>
        public bool Atomic { get; set; }
        /// <summary>Continue although the integrity hash of the file does not match.</summary>
        public bool IgnoreIntegrityMismatch { get; set; }
    }
}
