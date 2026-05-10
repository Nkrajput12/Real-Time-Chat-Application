using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ConnectHub.Media.API.Models;
using ConnectHub.Media.API.Models.Interface;
using ConnectHub.Media.API.Repository.Data;

namespace ConnectHub.Media.API.Repository
{
    public class MediaRepository : IMediaRepository
    {
        private readonly MediaDbContext _context;

        public MediaRepository(MediaDbContext context)
        {
            _context = context;
        }

        public async Task<MediaFile?> GetMediaByIdAsync(Guid fileId)
        {
            return await _context.MediaFiles.FindAsync(fileId);
        }

        public async Task<IEnumerable<MediaFile>> GetMediaByUploaderAsync(int userId)
        {
            return await _context.MediaFiles.Where(m => m.UploadedBy == userId).ToListAsync();
        }

        public async Task<IEnumerable<MediaFile>> GetMediaByRoomAsync(int roomId)
        {
            return await _context.MediaFiles.Where(m => m.RoomId == roomId).ToListAsync();
        }

        public async Task<MediaFile> AddMediaAsync(MediaFile mediaFile)
        {
            _context.MediaFiles.Add(mediaFile);
            await _context.SaveChangesAsync();
            return mediaFile;
        }

        public async Task<bool> DeleteMediaAsync(Guid fileId)
        {
            var file = await _context.MediaFiles.FindAsync(fileId);
            if (file == null) return false;
            _context.MediaFiles.Remove(file);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<MediaFile>> GetExpiredFilesAsync(DateTime dateTime)
        {
            return await _context.MediaFiles.Where(m => m.ExpiresAt < dateTime).ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
