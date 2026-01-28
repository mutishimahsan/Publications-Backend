using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class DigitalAccessDto
    {
        public Guid Id { get; set; }
        public Guid OrderItemId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductTitle { get; set; } = string.Empty;
        public string ProductFormat { get; set; } = string.Empty;
        public DateTime AccessGrantedAt { get; set; }
        public DateTime? AccessExpiresAt { get; set; }
        public int DownloadCount { get; set; }
        public int MaxDownloads { get; set; }
        public DateTime? LastDownloadedAt { get; set; }
        public bool HasDownloadsRemaining { get; set; }
        public bool IsExpired { get; set; }
        public string? DownloadUrl { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
    }

    public class DigitalDownloadDto
    {
        public string DownloadUrl { get; set; } = string.Empty;
        public string SecureToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public int DownloadsRemaining { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string MimeType { get; set; } = string.Empty;
    }

    public class UploadDigitalProductDto
    {
        public Guid ProductId { get; set; }
        public IFormFile File { get; set; } = null!;
        public int? MaxDownloads { get; set; }
        public int? DownloadExpiryDays { get; set; }
    }

    public class UpdateDigitalAccessDto
    {
        public int? MaxDownloads { get; set; }
        public int? DownloadExpiryDays { get; set; }
        public bool? ResetDownloadCount { get; set; }
    }

    public class DigitalAccessStatsDto
    {
        public int TotalAccessGrants { get; set; }
        public int ActiveAccessGrants { get; set; }
        public int ExpiredAccessGrants { get; set; }
        public int TotalDownloads { get; set; }
        public int DownloadsLast30Days { get; set; }
        public Dictionary<string, int> DownloadsByProduct { get; set; } = new();
        public Dictionary<string, int> DownloadsByCustomer { get; set; } = new();
    }

    public class FileDownloadResult
    {
        public Stream FileStream { get; set; } = null!;
        public string FileName { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DigitalAccessDto DigitalAccess { get; set; } = null!;
    }
}
