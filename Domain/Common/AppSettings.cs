using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Common
{
    public class JwtSettings
    {
        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int TokenExpirationInMinutes { get; set; } = 60;
    }

    public class DatabaseSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
    }

    public class StorageSettings
    {
        public string DigitalFilesPath { get; set; } = "wwwroot/digital-files";
        public string InvoiceFilesPath { get; set; } = "wwwroot/invoices";
        public string PaymentProofsPath { get; set; } = "wwwroot/payment-proofs";
        public int DownloadLinkExpiryHours { get; set; } = 72;
        public int MaxDownloadsPerFile { get; set; } = 5;
    }

    public class EmailSettings
    {
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string SmtpUsername { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
    }
}
