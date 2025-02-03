using System;
using System.IO;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace BlobProcessor
{
	public class BlobProcessorService
	{
        private readonly BlobServiceClient _blobServiceClient;
        private readonly BlobContainerClient _blobContainer;
        private readonly string _containerName;

        public BlobProcessorService(string connectionString, string containerName)
		{
            _containerName = containerName;
            _blobServiceClient = new BlobServiceClient(connectionString);
            _blobContainer = _blobServiceClient.GetBlobContainerClient(_containerName);
            _blobContainer.CreateIfNotExistsAsync();
        }

        public async Task<List<string>> GetBlobsAsync()
        {
            try
            {
                var blobs = new List<string>();
                await foreach (var blobItem in _blobContainer.GetBlobsAsync())
                {
                    blobs.Add(blobItem.Name);
                }

                return blobs;
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to get list of blobs, {ex.Message}");
            }
        }


        public async Task UploadBlobAsync(string blobName, Stream fileStream)
        {
            try
            {
                var blobClient = _blobContainer.GetBlobClient(blobName);
                await blobClient.UploadAsync(fileStream, overwrite: true);
            }
            catch(Exception ex)
            {
                throw new Exception($"Unable to upload {blobName}, {ex.Message}");
            }
        }

        public async Task<Stream> DownloadBlobAsync(string blobName)
        {
            try
            {
                var blobClient = _blobContainer.GetBlobClient(blobName);
                var response = await blobClient.DownloadAsync();
                return response.Value.Content;
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to download {blobName}, {ex.Message}");
            }
        }

        public async Task<bool> DeleteBlobAsync(string blobName)
        {
            try
            {
                var blobClient = _blobContainer.GetBlobClient(blobName);
                return await blobClient.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to delete {blobName}, {ex.Message}");
            }
        }

        public async Task<bool> BlobExistsAsync(string blobName)
        {
            try
            {
                var blobClient = _blobContainer.GetBlobClient(blobName);
                return await blobClient.ExistsAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to check {blobName}, {ex.Message}");
            }
        }

        public async Task<string> GenerateSasToken(string blobName)
        {
            try
            {
                var blobClient = _blobContainer.GetBlobClient(blobName);

                if (!await BlobExistsAsync(blobName))
                {
                    return null;
                }

                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = _containerName,
                    BlobName = blobName,
                    Resource = "b",
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
                };
                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                return blobClient.GenerateSasUri(sasBuilder).ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to generate sas {blobName}, {ex.Message}");
            }
        }
    }
}

