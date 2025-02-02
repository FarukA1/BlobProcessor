using System.Net;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace BlobProcessor.API
{
    public class Processor
    {
        private readonly ILogger _logger;
        private static readonly string _containerName = "filecontainer";
        private readonly string _connectionString = string.Empty;

        private BlobServiceClient _blobServiceClient;
        private BlobContainerClient _blobContainer;

        public Processor(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Processor>();

            _connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            if (string.IsNullOrEmpty(_connectionString))
            {
                _logger.LogError("Unable to get connection string");
            }

            _blobServiceClient = new BlobServiceClient(_connectionString);
            _blobContainer = _blobServiceClient.GetBlobContainerClient(_containerName);
            _blobContainer.CreateIfNotExistsAsync();
        }

        [Function("ListBlobs")]
        public async Task<HttpResponseData> ListBlobs(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blobs")] HttpRequestData req)
        {
            var blobs = new List<string>();
            await foreach (var blobItem in _blobContainer.GetBlobsAsync())
            {
                blobs.Add(blobItem.Name);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(blobs);
            return response;
        }

        [Function("CheckBlobExists")]
        public async Task<HttpResponseData> CheckBlobExists(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blobs/{blobName}/exists")] HttpRequestData req,
        string blobName)
        {
            var blobClient = _blobContainer.GetBlobClient(blobName);
            bool exists = await blobClient.ExistsAsync();

            var response = req.CreateResponse(exists ? HttpStatusCode.OK : HttpStatusCode.NotFound);
            await response.WriteStringAsync(exists ? $"Blob {blobName} exists." : $"Blob {blobName} does not exist.");
            return response;
        }

        [Function("UploadBlob")]
        public async Task<HttpResponseData> UploadBlob(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "blobs/{blobName}")] HttpRequestData req,
        string blobName)
        {
            _logger.LogInformation($"Uploading blob: {blobName}");

            var blobClient = _blobContainer.GetBlobClient(blobName);
            using var stream = req.Body;
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = req.Headers.GetValues("content-type").FirstOrDefault() });

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"Blob {blobName} uploaded successfully.");
            return response;
        }

        [Function("DownloadBlob")]
        public async Task<HttpResponseData> DownloadBlob(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blobs/{blobName}")] HttpRequestData req,
        string blobName)
        {
            _logger.LogInformation($"Downloading blob: {blobName}");

            var blobClient = _blobContainer.GetBlobClient(blobName);

            if (!(await blobClient.ExistsAsync()))
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"Blob {blobName} not found.");
                return notFoundResponse;
            }

            var downloadResponse = await blobClient.DownloadAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", downloadResponse.Value.ContentType);
            await downloadResponse.Value.Content.CopyToAsync(response.Body);
            return response;
        }

        [Function("UpdateBlob")]
        public async Task<HttpResponseData> UpdateBlob(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "blobs/{blobName}")] HttpRequestData req,
        string blobName)
        {
            var blobClient = _blobContainer.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"Blob {blobName} not found.");
                return notFoundResponse;
            }

            using var stream = req.Body;
            await blobClient.UploadAsync(stream, overwrite: true);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"Blob {blobName} updated successfully.");
            return response;
        }

        [Function("DeleteBlob")]
        public async Task<HttpResponseData> DeleteBlob(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "blobs/{blobName}")] HttpRequestData req,
        string blobName)
        {
            var blobClient = _blobContainer.GetBlobClient(blobName);

            if (!(await blobClient.ExistsAsync()))
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"Blob {blobName} not found.");
                return notFoundResponse;
            }

            await blobClient.DeleteAsync();
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"Blob {blobName} deleted successfully.");
            return response;
        }

        [Function("GenerateSasToken")]
        public async Task<HttpResponseData> GenerateSasToken(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blobs/{blobName}/sas")] HttpRequestData req, string blobName)
        {
            var blobClient = _blobContainer.GetBlobClient(blobName);
            if (!blobClient.Exists()) return req.CreateResponse(System.Net.HttpStatusCode.NotFound);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(blobClient.GenerateSasUri(sasBuilder).ToString());
            return response;
        }
    }
}

