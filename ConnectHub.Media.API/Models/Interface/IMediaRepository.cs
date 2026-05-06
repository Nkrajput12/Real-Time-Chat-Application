using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConnectHub.Media.API.Models.Interface
{
    public interface IMediaRepository
    {
        Task<MediaFile?> GetMediaByIdAsync(Guid fileId);
        Task<IEnumerable<MediaFile>> GetMediaByUploaderAsync(int userId);
        Task<IEnumerable<MediaFile>> GetMediaByRoomAsync(int roomId);
        Task<MediaFile> AddMediaAsync(MediaFile mediaFile);
        Task<bool> DeleteMediaAsync(Guid fileId);
        Task<IEnumerable<MediaFile>> GetExpiredFilesAsync(DateTime dateTime);
        Task SaveChangesAsync();
    }
}
