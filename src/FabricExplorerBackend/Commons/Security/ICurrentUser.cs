namespace FabricExplorerBackend.Commons.Security
{
    /// <summary>The authenticated user of the current request.</summary>
    public interface ICurrentUser
    {
        /// <summary>The "oid" claim.</summary>
        /// <exception cref="Errors.ApplicationException">The claim is missing or is not a valid Guid.</exception>
        Guid Id { get; }
    }
}
