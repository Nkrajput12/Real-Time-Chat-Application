using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using ConnectHub.Media.API.Models;
using ConnectHub.Media.API.Models.Interface;
using ConnectHub.Media.API.Service.Interface;

namespace ConnectHub.Media.API.Service
{
    public class MediaService : IMediaService
    {
        private readonly IMediaRepository _repo;
        private readonly string _connectionString;
        private readonly string _containerName = "chat-media";

        public MediaService(IMediaRepository repo, IConfiguration configuration)
        {
            _repo = repo;
            _connectionString = configuration.GetConnectionString("AzureBlobStorage");
            if (string.IsNullOrEmpty(_connectionString) || _connectionString == "UseDevelopmentStorage=true")
            {
                _connectionString = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;";
            }
        }

        public async Task<MediaFile?> UploadFileAsync(IFormFile file, int uploadedBy, int? roomId = null, int? messageId = null)
        {
            if (file == null || file.Length == 0) return null;

            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(Azure.Storage.Blobs.Models.PublicAccessType.None);

            var fileId = Guid.NewGuid();
            var extension = Path.GetExtension(file.FileName);
            var blobClient = containerClient.GetBlobClient($"{fileId}{extension}");

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, true);
            }

            // Generate a SAS URL valid for 7 days — needed because public blob access
            // is disabled at the account level (PublicAccessNotPermitted)
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = blobClient.Name,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddDays(7)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);
            var sasUrl = blobClient.GenerateSasUri(sasBuilder).ToString();

            var mediaFile = new MediaFile
            {
                FileId = fileId,
                UploadedBy = uploadedBy,
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSizeKb = file.Length / 1024,
                BlobUrl = sasUrl,
                RoomId = roomId,
                MessageId = messageId,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            return await _repo.AddMediaAsync(mediaFile);
        }

        public async Task<string?> GenerateSasUrlAsync(Guid fileId)
        {
            var mediaFile = await _repo.GetMediaByIdAsync(fileId);
            if (mediaFile == null) return null;

            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            var extension = Path.GetExtension(mediaFile.FileName);
            var blobClient = containerClient.GetBlobClient($"{fileId}{extension}");

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = blobClient.Name,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            return sasUri.ToString();
        }

        public async Task<bool> DeleteFileAsync(Guid fileId)
        {
            var mediaFile = await _repo.GetMediaByIdAsync(fileId);
            if (mediaFile == null) return false;

            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            var extension = Path.GetExtension(mediaFile.FileName);
            var blobClient = containerClient.GetBlobClient($"{fileId}{extension}");

            await blobClient.DeleteIfExistsAsync();
            await _repo.DeleteMediaAsync(fileId);
            return true;
        }

        public async Task CleanupExpiredFilesAsync()
        {
            var expiredFiles = await _repo.GetExpiredFilesAsync(DateTime.UtcNow);
            foreach (var file in expiredFiles)
            {
                await DeleteFileAsync(file.FileId);
            }
        }

        public async Task<MediaFile?> GetFileByIdAsync(Guid fileId)
        {
            return await _repo.GetMediaByIdAsync(fileId);
        }
    }
}
