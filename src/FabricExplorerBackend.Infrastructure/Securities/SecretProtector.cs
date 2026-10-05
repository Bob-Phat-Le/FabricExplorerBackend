using Microsoft.AspNetCore.DataProtection;

namespace FabricExplorerBackend.Infrastructures.Securities
{
    public class SecretProtector(IDataProtector protector) : ISecretProtector
    {
        public string Protect(string secret)
        {
            return protector.Protect(secret);
        }

        public string Unprotect(string protectedSecret)
        {
            return protector.Unprotect(protectedSecret);
        }
    }
}
