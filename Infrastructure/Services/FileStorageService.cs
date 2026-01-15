using Application.Interfaces;
using Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IHostEnvironment _environment;
        private readonly StorageSettings _storageSettings;

        public FileStorageService(
            IHostEnvironment environment,
            IOptions<StorageSettings> storageSettings)
        {
            _environment = environment;
            _storageSettings = storageSettings.Value;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file provided.");

            // Create folder if it doesn't exist
            var uploadsFolder = Path.Combine(_environment.ContentRootPath, "wwwroot", folder);
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Path.Combine(folder, fileName).Replace("\\", "/");
        }

        public Task<Stream> GetFileAsync(string filePath)
        {
            var fullPath = Path.Combine(_environment.ContentRootPath, "wwwroot", filePath);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"File not found: {filePath}");

            // This is synchronous but returns Task for interface compatibility
            Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            return Task.FromResult(stream);
        }

        public Task<bool> DeleteFileAsync(string filePath)
        {
            var fullPath = Path.Combine(_environment.ContentRootPath, "wwwroot", filePath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<string> GetFileUrlAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return Task.FromResult(string.Empty);

            return Task.FromResult($"/{filePath}");
        }

        public Task<string> GenerateSecureDownloadUrlAsync(string filePath, Guid userId, int expiryHours = 72)
        {
            var expiry = DateTime.UtcNow.AddHours(expiryHours);
            var data = $"{filePath}|{userId}|{expiry:O}";

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_storageSettings.DigitalFilesPath)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                var token = Convert.ToBase64String(hash);
                token = Uri.EscapeDataString(token);

                var url = $"/api/download/secure?file={Uri.EscapeDataString(filePath)}&user={userId}&expiry={expiry:O}&token={token}";
                return Task.FromResult(url);
            }
        }
    }
}