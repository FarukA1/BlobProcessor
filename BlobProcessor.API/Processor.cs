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
        private readonly BlobProcessorService _blobProcessorService;
        private static readonly string _containerName = "filecontainer";
        private readonly string _connectionString = string.Empty;

        public Processor(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Processor>();

            _connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            if (string.IsNullOrEmpty(_connectionString))
            {
                _logger.LogError("Unable to get connection string");
            }

            _blobProcessorService = new BlobProcessorService(_connectionString, _containerName);
        }

        [Function("ListBlobs")]
        public async Task<HttpResponseData> ListBlobs(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blobs")] HttpRequestData req)
        {
            var blobs = await _blobProcessorService.GetBlobsAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(blobs);
            return response;
        }

        [Function("CheckBlobExists")]
        public async Task<HttpResponseData> CheckBlobExists(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blobs/{blobName}/exists")] HttpRequestData req,
        string blobName)
        {
            bool exists = await _blobProcessorService.BlobExistsAsync(blobName);

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

            using var stream = req.Body;
            await _blobProcessorService.UploadBlobAsync(blobName, stream);

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

            if (!await _blobProcessorService.BlobExistsAsync(blobName))
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            var downloadResponse = await _blobProcessorService.DownloadBlobAsync(blobName);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain");
            await downloadResponse.CopyToAsync(response.Body);
            return response;
        }

        [Function("UpdateBlob")]
        public async Task<HttpResponseData> UpdateBlob(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "blobs/{blobName}")] HttpRequestData req,
        string blobName)
        {
            if (!await _blobProcessorService.BlobExistsAsync(blobName))
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            using var stream = req.Body;
            await _blobProcessorService.UploadBlobAsync(blobName, stream);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"Blob {blobName} updated successfully.");
            return response;
        }

        [Function("DeleteBlob")]
        public async Task<HttpResponseData> DeleteBlob(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "blobs/{blobName}")] HttpRequestData req,
        string blobName)
        {
            if (!await _blobProcessorService.BlobExistsAsync(blobName))
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            await _blobProcessorService.DeleteBlobAsync(blobName);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"Blob {blobName} deleted successfully.");
            return response;
        }

        [Function("GenerateSasToken")]
        public async Task<HttpResponseData> GenerateSasToken(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blobs/{blobName}/sas")] HttpRequestData req, string blobName)
        {
            if (!await _blobProcessorService.BlobExistsAsync(blobName))
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            string sasUri = await _blobProcessorService.GenerateSasToken(blobName);

            if (string.IsNullOrEmpty(sasUri))
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"Blob {blobName} not found.");
                return notFoundResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(sasUri);
            return response;
        }
    }
}

