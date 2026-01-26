using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class DigitalAccess : AuditableEntity
    {
        public Guid OrderItemId { get; set; }
        public virtual OrderItem OrderItem { get; set; } = null!;

        public Guid ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;

        public Guid CustomerId { get; set; }
        public virtual User Customer { get; set; } = null!;

        public DateTime AccessGrantedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AccessExpiresAt { get; set; }
        public int DownloadCount { get; set; } = 0;
        public int MaxDownloads { get; set; } = 3;
        public DateTime? LastDownloadedAt { get; set; }

        // For secure download tokens
        public string? CurrentToken { get; set; }
        public DateTime? TokenExpiresAt { get; set; }

        // Status
        public bool IsActive { get; set; } = true;

        public bool IsExpired => AccessExpiresAt.HasValue && AccessExpiresAt.Value < DateTime.UtcNow;
        public bool HasDownloadsRemaining => DownloadCount < MaxDownloads;

        public void IncrementDownloadCount()
        {
            DownloadCount++;
            LastDownloadedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void GenerateNewToken(TimeSpan validity)
        {
            CurrentToken = Guid.NewGuid().ToString("N");
            TokenExpiresAt = DateTime.UtcNow.Add(validity);
            UpdatedAt = DateTime.UtcNow;
        }

        public bool IsTokenValid(string token)
        {
            if (string.IsNullOrEmpty(CurrentToken) || CurrentToken != token)
                return false;

            if (TokenExpiresAt.HasValue && TokenExpiresAt.Value < DateTime.UtcNow)
                return false;

            return true;
        }
    }
}
