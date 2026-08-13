using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServices.Facade
{
    public interface IKeyVaultService
    {
        Task<(bool IsValid, string? ErrorMessage)> ValidateAsync(string? secretName = null, CancellationToken cancellationToken = default);
        Task<KeyVaultSecret> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);
        Task<KeyVaultSecret> SetSecretAsync(string secretName, string value, CancellationToken cancellationToken = default);
        Task<DeleteSecretOperation> DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default);
        Task<KeyVaultKey> GetKeyAsync(string keyName, CancellationToken cancellationToken = default);
        Task<KeyVaultKey> CreateRsaKeyAsync(string keyName, int rsaKeySize = 2048, CancellationToken cancellationToken = default);
    }
}
