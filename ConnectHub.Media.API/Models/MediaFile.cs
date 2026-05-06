using System;

namespace ConnectHub.Media.API.Models
{
    public class MediaFile
    {
        public Guid FileId { get; set; }
        public int UploadedBy { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeKb { get; set; }
        public string BlobUrl { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public int? MessageId { get; set; }
        public int? RoomId { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
    }
}
