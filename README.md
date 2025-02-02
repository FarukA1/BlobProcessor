# BlobProcessor

BlobProcessor is an Azure Function-based API for performing CRUD operations on Azure Blob Storage. It includes support for file uploads, downloads and security features (SAS tokens).

## Features
- **Upload Blobs**: Upload single or multiple files to Azure Blob Storage.
- **Download Blobs**: Retrieve stored files.
- **Delete Blobs**: Remove files individually or in bulk.
- **List Blobs**: Get a list of all stored blobs.
- **Check Blob Existence**: Verify if a blob exists.
- **Security**: Generate SAS tokens for secure access.

## Endpoints
### 1. List Blobs
**GET** `/api/blobs`
Retrieves a list of all blobs in the storage container.

### 2. Upload Blob
**POST** `/api/blobs/{blobName}`
Uploads a single file to the blob container.

### 3. Download Blob
**GET** `/api/blobs/{blobName}`
Downloads a file from blob storage.

### 4. Delete Blob
**DELETE** `/api/blobs/{blobName}`
Deletes a specified blob.

### 5. Update Blob
**PUT** `/api/blobs/{blobName}`
Updates a specified blob.

### 6. Check Blob Existence
**GET** `/api/blobs/{blobName}/exists`
Checks if a blob exists.

### 7. Generate SAS Token
**GET** `/api/blobs/{blobName}/sas`
Generates a secure access token for a blob.

## Setup and Deployment
### Prerequisites
- Azure Storage Account
- Azure Function Core Tools
- .NET SDK (latest version)

### Local Development
1. Clone the repository:
   ```sh
   git clone https://github.com/FarukA1/blobprocessor.git
   cd blobprocessor
   
2. Set up your storage connection string:
   ```sh
    export AzureWebJobsStorage="your_connection_string"

3. Start Azurite (for local storage emulation):
   ```sh
   azurite --silent --location ./azurite --debug ./azurite_debug.log

4. Run the function app:
   ```sh
   func start



