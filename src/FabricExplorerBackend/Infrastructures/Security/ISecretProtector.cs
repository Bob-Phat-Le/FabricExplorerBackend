namespace FabricExplorerBackend.Infrastructures.Security
{
    public interface ISecretProtector
    {
        string Protect(string secret);
        string Unprotect(string protectedSecret);
    }
}
