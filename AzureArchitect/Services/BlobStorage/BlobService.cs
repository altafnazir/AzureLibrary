using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using AzureServices.Entity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AzureArchitect.Services.BlobStorage
{
    public class BlobService : IBlobService
    {
        private readonly BlobServiceClient _client;
        private readonly ILogger<BlobService> _logger;
        private readonly string _connectionString;        

        public BlobService(StorageConfiguration storageConfiguration, BlobServiceClient blobServiceClient, ILogger<BlobService> logger)
        {
            _connectionString = storageConfiguration.ConnectionString!;
            _client = blobServiceClient;
            _logger = logger;
        }

        public async Task CreateContainerIfNotExistsAsync(string containerName, CancellationToken cancellationToken = default)
        {
            var container = _client.GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task UploadAsync(string containerName, string blobName, Stream content, bool uniqueBlobName = true, CancellationToken cancellationToken = default)
        {
            var container = _client.GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (uniqueBlobName)
            {
                // Split into name and extension
                var nameWithoutExt = Path.GetFileNameWithoutExtension(blobName) ?? string.Empty;
                var extension = Path.GetExtension(blobName) ?? string.Empty;

                // Create a UTC timestamp for uniqueness
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");

                // Compose unique blob name: {name}_{timestamp}{ext}
                blobName = $"{nameWithoutExt}_{timestamp}{extension}";
            }

            var blob = container.GetBlobClient(blobName);
            content.Position = 0;

            await blob.UploadAsync(content, overwrite: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task BulkUploadAsync(string containerName, List<FileToUpload> files, CancellationToken cancellationToken = default)
        {
            var tasks = files.Select(async file =>
            {
                try
                {
                    await UploadAsync(containerName, file.FileName, file.Content, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            });

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Upload file from specified local path
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="filePath"></param>
        /// <param name="blobName">If not set, uploaded filename will be appended with unique identifier </param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        public async Task UploadFromFileAsync(string containerName, string filePath, string? blobName = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("filePath is required.", nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("File not found.", filePath);

            var container = _client.GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(blobName))
            {
                var baseBlobName = Path.GetFileName(filePath);

                // Split into name and extension
                var nameWithoutExt = Path.GetFileNameWithoutExtension(baseBlobName) ?? string.Empty;
                var extension = Path.GetExtension(baseBlobName) ?? string.Empty;

                // Create a UTC timestamp for uniqueness
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");

                // Compose unique blob name: {name}_{timestamp}{ext}
                blobName = $"{nameWithoutExt}_{timestamp}{extension}";
            }
            
            var blob = container.GetBlobClient(blobName);
            await blob.UploadAsync(filePath, overwrite: true);
        }
        public async Task BulkUploadFromFileAsync(string containerName, List<string> filePaths, CancellationToken cancellationToken = default)
        {
            var tasks = filePaths.Select(async filePath =>
            {
                try
                {
                    await UploadFromFileAsync(containerName, filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            });

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Generate a short-lived SAS URI for a blob using the account key from the configured connection string.
        /// Requires the connection string to contain AccountName and AccountKey (shared key).
        /// </summary>
        /// <param name="containerName">Container name.</param>
        /// <param name="blobName">Blob name.</param>
        /// <param name="expiresIn">Time span until the SAS expires (from now).</param>
        /// <param name="permissions">Permissions for the SAS (default: Read).</param>
        /// <returns>Uri containing the SAS token.</returns>
        public async Task<Uri> GenerateBlobSasUri(string containerName, string blobName, TimeSpan expiresIn, BlobSasPermissions permissions = BlobSasPermissions.Read)
        {
            // Validate inputs (synchronous, cheap)
            if (string.IsNullOrWhiteSpace(containerName)) throw new ArgumentException("containerName is required.", nameof(containerName));
            if (string.IsNullOrWhiteSpace(blobName)) throw new ArgumentException("blobName is required.", nameof(blobName));
            if (expiresIn <= TimeSpan.Zero) throw new ArgumentException("expiresIn must be a positive TimeSpan.", nameof(expiresIn));

            // Parse connection string for shared key
            if (!TryParseConnectionStringForSharedKey(out var accountName, out var accountKey))
            {
                throw new InvalidOperationException("Cannot generate SAS: connection string does not contain AccountName and AccountKey (shared key required).");
            }

            var credential = new StorageSharedKeyCredential(accountName, accountKey);
            var blobClient = _client.GetBlobContainerClient(containerName).GetBlobClient(blobName);

            // The SAS generation is CPU-bound (building objects and formatting strings).
            // Use Task.Run to avoid compiler warning about 'async' without 'await'
            // and to keep method asynchronous for callers.
            var uri = await Task.Run(() =>
            {
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = containerName,
                    BlobName = blobName,
                    Resource = "b",
                    ExpiresOn = DateTimeOffset.UtcNow.Add(expiresIn)
                };

                sasBuilder.SetPermissions(permissions);

                var sasQueryParameters = sasBuilder.ToSasQueryParameters(credential);

                var uriBuilder = new UriBuilder(blobClient.Uri)
                {
                    Query = sasQueryParameters.ToString()
                };

                return uriBuilder.Uri;
            }).ConfigureAwait(false);

            return uri;
        }

        private bool TryParseConnectionStringForSharedKey(out string? accountName, out string? accountKey)
        {
            accountName = null;
            accountKey = null;

            if (string.IsNullOrWhiteSpace(_connectionString)) return false;

            // connection string format: key1=value1;key2=value2;...
            var parts = _connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var idx = part.IndexOf('=');
                if (idx <= 0) continue;
                var key = part.Substring(0, idx).Trim();
                var value = part.Substring(idx + 1).Trim();
                if (key.Equals("AccountName", StringComparison.OrdinalIgnoreCase))
                    accountName = value;
                else if (key.Equals("AccountKey", StringComparison.OrdinalIgnoreCase))
                    accountKey = value;
            }

            return !string.IsNullOrWhiteSpace(accountName) && !string.IsNullOrWhiteSpace(accountKey);
        }

        public async Task<Stream> DownloadAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(containerName)) throw new ArgumentException(nameof(containerName));
            if (string.IsNullOrWhiteSpace(blobName)) throw new ArgumentException(nameof(blobName));
            var container = _client.GetBlobContainerClient(containerName);
            var blob = container.GetBlobClient(blobName);

            var existsResp = await blob.ExistsAsync(cancellationToken).ConfigureAwait(false);
            if (!existsResp.Value)
                throw new FileNotFoundException($"Blob '{blobName}' was not found in container '{containerName}'.");

            var resp = await blob.DownloadAsync(cancellationToken).ConfigureAwait(false);
            var ms = new MemoryStream();
            await resp.Value.Content.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
            ms.Position = 0;
            return ms;
        }

        public async Task DownloadToFileAsync(string containerName, string blobName, string destinationPath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(containerName)) throw new ArgumentException(nameof(containerName));
            if (string.IsNullOrWhiteSpace(blobName)) throw new ArgumentException(nameof(blobName));
            if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("destinationPath is required.", nameof(destinationPath));

            var directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var container = _client.GetBlobContainerClient(containerName);
            var blob = container.GetBlobClient(blobName);

            var existsResp = await blob.ExistsAsync(cancellationToken).ConfigureAwait(false);
            if (!existsResp.Value)
                throw new FileNotFoundException($"Blob '{blobName}' was not found in container '{containerName}'.");

            await blob.DownloadToAsync(destinationPath, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteAsync(string containerName, string blobName, DeleteSnapshotsOption deleteSnapshotsOption = DeleteSnapshotsOption.IncludeSnapshots, CancellationToken cancellationToken = default)
        {
            var container = _client.GetBlobContainerClient(containerName);
            var blob = container.GetBlobClient(blobName);
            await blob.DeleteIfExistsAsync(deleteSnapshotsOption,
                                                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> ExistsAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
        {
            var container = _client.GetBlobContainerClient(containerName);
            var blob = container.GetBlobClient(blobName);
            var resp = await blob.ExistsAsync(cancellationToken).ConfigureAwait(false);
            return resp.Value;
        }

        public async Task<BlobProperties> GetPropertiesAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(containerName)) throw new ArgumentException(nameof(containerName));
            if (string.IsNullOrWhiteSpace(blobName)) throw new ArgumentException(nameof(blobName));
            var container = _client.GetBlobContainerClient(containerName);
            var blob = container.GetBlobClient(blobName);
            var resp = await blob.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return resp.Value;
        }

        public async Task<IDictionary<string, string>> GetMetadataAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(containerName)) throw new ArgumentException(nameof(containerName));
            if (string.IsNullOrWhiteSpace(blobName)) throw new ArgumentException(nameof(blobName));
            var properties = await GetPropertiesAsync(containerName, blobName, cancellationToken).ConfigureAwait(false);
            // BlobProperties.Metadata is IReadOnlyDictionary<string,string>, copy to Dictionary for mutability
            return new Dictionary<string, string>(properties.Metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
        }

        public async Task SetMetadataAsync(string containerName, string blobName, IDictionary<string, string> metadata, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(containerName)) throw new ArgumentException(nameof(containerName));
            if (string.IsNullOrWhiteSpace(blobName)) throw new ArgumentException(nameof(blobName));
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));

            var container = _client.GetBlobContainerClient(containerName);
            var blob = container.GetBlobClient(blobName);

            // Ensure blob exists before attempting operations
            var existsResp = await blob.ExistsAsync(cancellationToken).ConfigureAwait(false);
            if (!existsResp.Value)
                throw new FileNotFoundException($"Blob '{blobName}' was not found in container '{containerName}'.");

            // Read current properties to get existing metadata
            var propsResp = await blob.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            var currentMetadata = propsResp.Value.Metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Merge: apply incoming keys (null or empty => remove key)
            var merged = new Dictionary<string, string>(currentMetadata, StringComparer.OrdinalIgnoreCase);
            foreach (var kv in metadata)
            {
                if (kv.Value == null || kv.Value == string.Empty)
                {
                    merged.Remove(kv.Key);
                }
                else
                {
                    merged[kv.Key] = kv.Value;
                }
            }

            // If nothing changed, return early
            if (DictionaryEqual(merged, currentMetadata))
                return;

            var conditions = new BlobRequestConditions { IfMatch = propsResp.Value.ETag };

            await blob.SetMetadataAsync(merged, conditions: conditions, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        static bool DictionaryEqual(IDictionary<string, string> a, IDictionary<string, string> b)
        {
            if (a.Count != b.Count) return false;
            foreach (var kv in a)
            {
                if (!b.TryGetValue(kv.Key, out var v)) return false;
                if (!string.Equals(kv.Value, v, StringComparison.Ordinal)) return false;
            }
            return true;
        }

        // Set HTTP headers (content-type, cache-control, etc.)
        public async Task SetHttpHeadersAsync(string containerName, string blobName, BlobHttpHeaders headers, CancellationToken cancellationToken = default)
        {
            if (headers == null) throw new ArgumentNullException(nameof(headers));
            if (string.IsNullOrWhiteSpace(containerName)) throw new ArgumentException(nameof(containerName));
            if (string.IsNullOrWhiteSpace(blobName)) throw new ArgumentException(nameof(blobName));

            var container = _client.GetBlobContainerClient(containerName);
            var blob = container.GetBlobClient(blobName);
            await blob.SetHttpHeadersAsync(headers, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<BlobItem>> ListBlobsAsync(string containerName, string? prefix = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(containerName)) throw new ArgumentException(nameof(containerName));

            var container = _client.GetBlobContainerClient(containerName);
            var results = new List<BlobItem>();
            await foreach (var item in container.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                results.Add(item);
            }
            return results;
        }

        // Acquire a lease on a blob. leaseDuration should be between 15 and 60 seconds; default to 60s when null.
        public async Task<string> AcquireLeaseAsync(string containerName, string blobName, TimeSpan? leaseDuration = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(containerName)) throw new ArgumentException(nameof(containerName));
            if (string.IsNullOrWhiteSpace(blobName)) throw new ArgumentException(nameof(blobName));

            var container = _client.GetBlobContainerClient(containerName);
            var blob = container.GetBlobClient(blobName);

            var exists = await blob.ExistsAsync(cancellationToken).ConfigureAwait(false);
            if (!exists.Value) throw new InvalidOperationException($"Blob '{blobName}' does not exist in container '{containerName}'.");

            var leaseClient = blob.GetBlobLeaseClient();

            var duration = leaseDuration ?? TimeSpan.FromSeconds(60);
            // Ensure duration is within allowed range (15-60s)
            if (duration < TimeSpan.FromSeconds(15) || duration > TimeSpan.FromSeconds(60))
                throw new ArgumentOutOfRangeException(nameof(leaseDuration), "Lease duration must be between 15 and 60 seconds for blobs.");

            var response = await leaseClient.AcquireAsync(duration, cancellationToken: cancellationToken).ConfigureAwait(false);
            return response.Value.LeaseId;
        }

        public async Task ReleaseLeaseAsync(string containerName, string blobName, string leaseId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(containerName)) throw new ArgumentException(nameof(containerName));
            if (string.IsNullOrWhiteSpace(blobName)) throw new ArgumentException(nameof(blobName));

            if (string.IsNullOrWhiteSpace(leaseId)) throw new ArgumentException("leaseId is required.", nameof(leaseId));

            var container = _client.GetBlobContainerClient(containerName);
            var blob = container.GetBlobClient(blobName);
            var leaseClient = blob.GetBlobLeaseClient(leaseId);

            await leaseClient.ReleaseAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task RenewLeaseAsync(string containerName, string blobName, string leaseId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(containerName)) throw new ArgumentException(nameof(containerName));
            if (string.IsNullOrWhiteSpace(blobName)) throw new ArgumentException(nameof(blobName));

            if (string.IsNullOrWhiteSpace(leaseId)) throw new ArgumentException("leaseId is required.", nameof(leaseId));

            var container = _client.GetBlobContainerClient(containerName);
            var blob = container.GetBlobClient(blobName);
            var leaseClient = blob.GetBlobLeaseClient(leaseId);

            await leaseClient.RenewAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        // Break an active lease on the blob (makes it available after break period)
        public async Task BreakLeaseAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(containerName)) throw new ArgumentException(nameof(containerName));
            if (string.IsNullOrWhiteSpace(blobName)) throw new ArgumentException(nameof(blobName));

            var container = _client.GetBlobContainerClient(containerName);
            var blob = container.GetBlobClient(blobName);
            var leaseClient = blob.GetBlobLeaseClient();

            await leaseClient.BreakAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        // Start server-side copy and return copyId
        public async Task<string> StartCopyFromUriAsync(string containerName, string blobName, Uri sourceUri, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(containerName)) throw new ArgumentException(nameof(containerName));
            if (string.IsNullOrWhiteSpace(blobName)) throw new ArgumentException(nameof(blobName));

            if (sourceUri == null) throw new ArgumentNullException(nameof(sourceUri));

            var container = _client.GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken).ConfigureAwait(false);
            var blob = container.GetBlobClient(blobName);

            var resp = await blob.StartCopyFromUriAsync(sourceUri, cancellationToken: cancellationToken).ConfigureAwait(false);
            return resp.Id ?? string.Empty;
        }

        // Abort an ongoing copy operation
        public async Task AbortCopyAsync(string containerName, string blobName, string copyId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(containerName)) throw new ArgumentException(nameof(containerName));
            if (string.IsNullOrWhiteSpace(blobName)) throw new ArgumentException(nameof(blobName));
            if (string.IsNullOrWhiteSpace(copyId)) throw new ArgumentException(nameof(copyId));

            var container = _client.GetBlobContainerClient(containerName);
            var blob = container.GetBlobClient(blobName);

            await blob.AbortCopyFromUriAsync(copyId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        // Create snapshot and return snapshot id string
        public async Task<string> CreateSnapshotAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(containerName)) throw new ArgumentException(nameof(containerName));
            if (string.IsNullOrWhiteSpace(blobName)) throw new ArgumentException(nameof(blobName));

            var container = _client.GetBlobContainerClient(containerName);
            var blob = container.GetBlobClient(blobName);

            var resp = await blob.CreateSnapshotAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return resp.Value.Snapshot ?? string.Empty;
        }

        public async Task SetAccessTierAsync(string containerName, string blobName, AccessTier tier, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(containerName)) throw new ArgumentException(nameof(containerName));
            if (string.IsNullOrWhiteSpace(blobName)) throw new ArgumentException(nameof(blobName));

            var container = _client.GetBlobContainerClient(containerName);
            var blob = container.GetBlobClient(blobName);

            await blob.SetAccessTierAsync(tier, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        // Generate user-delegation SAS (requires the BlobServiceClient to be authenticated with TokenCredential)
        public async Task<Uri> GenerateUserDelegationSasUriAsync(string containerName, string blobName, TimeSpan expiresIn, BlobSasPermissions permissions = BlobSasPermissions.Read, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(containerName)) throw new ArgumentException(nameof(containerName));
            if (string.IsNullOrWhiteSpace(blobName)) throw new ArgumentException(nameof(blobName));
            if (expiresIn <= TimeSpan.Zero) throw new ArgumentException("expiresIn must be positive", nameof(expiresIn));

            var startsOn = DateTimeOffset.UtcNow.AddMinutes(-5);
            var expiresOn = DateTimeOffset.UtcNow.Add(expiresIn);

            // Acquire user delegation key
            var userDelegationKeyResp = await _client.GetUserDelegationKeyAsync(startsOn, expiresOn, cancellationToken: cancellationToken).ConfigureAwait(false);
            var userDelegationKey = userDelegationKeyResp.Value;

            var accountName = GetAccountName();

            var container = _client.GetBlobContainerClient(containerName);
            var blobClient = container.GetBlobClient(blobName);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = blobName,
                Resource = "b",
                StartsOn = startsOn,
                ExpiresOn = expiresOn
            };

            sasBuilder.SetPermissions(permissions);

            var sas = sasBuilder.ToSasQueryParameters(userDelegationKey, accountName);

            var uriBuilder = new UriBuilder(blobClient.Uri) { Query = sas.ToString() };
            return uriBuilder.Uri;
        }

        // Try to determine account name: prefer parsing connection string, fall back to blob service host
        private string GetAccountName()
        {
            if (TryParseConnectionStringForSharedKey(out var accountName, out _))
            {
                if (!string.IsNullOrWhiteSpace(accountName)) return accountName;
            }

            // Fallback: parse from _client.Uri host (format: {account}.blob.core.windows.net)
            var host = _client.Uri.Host;
            var segments = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 0) return segments[0];

            throw new InvalidOperationException("Unable to determine storage account name for user-delegation SAS.");
        }
    }
}