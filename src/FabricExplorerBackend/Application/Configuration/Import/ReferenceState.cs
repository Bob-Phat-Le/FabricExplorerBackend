namespace FabricExplorerBackend.Application.Configuration.Import
{
    public enum ReferenceState
    {
        Unresolved,
        /// <summary>Points at a connection that exists now.</summary>
        Existing,
        /// <summary>Points at a connection that is in the file and does not exist yet.</summary>
        Pending
    }
}
