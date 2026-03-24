using Microsoft.AspNetCore.DataProtection;

namespace DriftDNS.Infrastructure.Security;

public interface ICredentialProtector
{
    string Protect(string plaintext);
    string Unprotect(string value);
}

public class CredentialProtector : ICredentialProtector
{
    private readonly IDataProtector _protector;

    public CredentialProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("DriftDNS.Credentials.v1");
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string value)
    {
        try
        {
            return _protector.Unprotect(value);
        }
        catch
        {
            // Fallback: unencrypted value from previous installations
            return value;
        }
    }
}
