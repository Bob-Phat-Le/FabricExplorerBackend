namespace FabricExplorerBackend.Infrastructures.Securities
{
    public interface ISecretProtector
    {
        string Protect(string secret);
        string Unprotect(string protectedSecret);
    }
}
