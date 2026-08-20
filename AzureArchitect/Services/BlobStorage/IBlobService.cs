using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using AzureServices.Entity;

namespace AzureArchitect.Services.BlobStorage
{
    public interface IBlobService
    {
        Task CreateContainerIfNotExistsAsync(string containerName, CancellationToken cancellationToken = default);
        Task UploadAsync(string containerName, string blobName, Stream content, bool uniqueBlobName = true, CancellationToken cancellationToken = default);
        Task BulkUploadAsync(string containerName, List<FileToUpload> files, CancellationToken cancellationToken = default);
        Task UploadFromFileAsync(string containerName, string filePath, string? blobName = null, CancellationToken cancellationToken = default);
        Task BulkUploadFromFileAsync(string containerName, List<string> filePaths, CancellationToken cancellationToken = default);
        Task<Stream> DownloadAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
        Task DownloadToFileAsync(string containerName, string blobName, string destinationPath, CancellationToken cancellationToken = default);
        Task DeleteAsync(string containerName, string blobName, DeleteSnapshotsOption deleteSnapshotsOption = DeleteSnapshotsOption.IncludeSnapshots, CancellationToken cancellationToken = default);
        Task<Uri> GenerateBlobSasUri(string containerName, string blobName, TimeSpan expiresIn, BlobSasPermissions permissions = BlobSasPermissions.Read);
        Task<bool> ExistsAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
        Task<BlobProperties> GetPropertiesAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
        Task<IDictionary<string, string>> GetMetadataAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
        Task SetMetadataAsync(string containerName, string blobName, IDictionary<string, string> metadata, CancellationToken cancellationToken = default);
        Task SetHttpHeadersAsync(string containerName, string blobName, BlobHttpHeaders headers, CancellationToken cancellationToken = default);
        Task<IEnumerable<BlobItem>> ListBlobsAsync(string containerName, string? prefix = null, CancellationToken cancellationToken = default);
        // leasing for concurrency / long-running exclusive access
        Task<string> AcquireLeaseAsync(string containerName, string blobName, TimeSpan? leaseDuration = null, CancellationToken cancellationToken = default);
        Task ReleaseLeaseAsync(string containerName, string blobName, string leaseId, CancellationToken cancellationToken = default);
        Task RenewLeaseAsync(string containerName, string blobName, string leaseId, CancellationToken cancellationToken = default);
        Task BreakLeaseAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
        // copy and snapshot
        Task<string> StartCopyFromUriAsync(string containerName, string blobName, Uri sourceUri, CancellationToken cancellationToken = default);
        Task AbortCopyAsync(string containerName, string blobName, string copyId, CancellationToken cancellationToken = default);
        Task<string> CreateSnapshotAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
        // access tier and immutability
        Task SetAccessTierAsync(string containerName, string blobName, AccessTier tier, CancellationToken cancellationToken = default);
        // user-delegation SAS (preferred over account key)
        Task<Uri> GenerateUserDelegationSasUriAsync(string containerName, string blobName, TimeSpan expiresIn, BlobSasPermissions permissions = BlobSasPermissions.Read, CancellationToken cancellationToken = default);
    }
}