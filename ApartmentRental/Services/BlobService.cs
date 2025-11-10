using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ApartmentRental.Services
{
    public class BlobService : IBlobService
    {
        private readonly BlobContainerClient _containerClient;

        public BlobService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureBlob:ConnectionString"];
            var containerName = configuration["AzureBlob:ContainerName"];

            var serviceClient = new BlobServiceClient(connectionString);
            _containerClient = serviceClient.GetBlobContainerClient(containerName);

            _containerClient.CreateIfNotExists(PublicAccessType.Blob);
        }

        public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)
        {
            var blobName = $"{Guid.NewGuid()}-{fileName}";
            var blobClient = _containerClient.GetBlobClient(blobName);

            await blobClient.UploadAsync(fileStream, new BlobHttpHeaders
            {
                ContentType = contentType
            });

            return blobClient.Uri.ToString();
        }

        public async Task DeleteAsync(string blobUrl)
        {
            if (string.IsNullOrWhiteSpace(blobUrl)) return;

            var uri = new Uri(blobUrl);
            var blobName = Path.GetFileName(uri.LocalPath);

            var blobClient = _containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }
    }
}
