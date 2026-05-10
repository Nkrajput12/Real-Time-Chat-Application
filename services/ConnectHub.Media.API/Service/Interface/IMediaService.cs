using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ConnectHub.Media.API.Models;

namespace ConnectHub.Media.API.Service.Interface
{
    public interface IMediaService
    {
        Task<MediaFile?> UploadFileAsync(IFormFile file, int uploadedBy, int? roomId = null, int? messageId = null);
        Task<string?> GenerateSasUrlAsync(Guid fileId);
        Task<bool> DeleteFileAsync(Guid fileId);
        Task CleanupExpiredFilesAsync();
        Task<MediaFile?> GetFileByIdAsync(Guid fileId);
    }
}
