using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class DigitalProductService : IDigitalProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;
        private readonly IProductRepository _productRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<DigitalProductService> _logger;
        private readonly IConfiguration _configuration;

        public DigitalProductService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService,
            IProductRepository productRepository,
            IOrderRepository orderRepository,
            IMapper mapper,
            ILogger<DigitalProductService> logger,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _mapper = mapper;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<string> UploadDigitalFileAsync(UploadDigitalProductDto dto)
        {
            var product = await _productRepository.GetByIdAsync(dto.ProductId);
            if (product == null)
            {
                throw new NotFoundException("Product", dto.ProductId);
            }

            // Validate product format supports digital
            if (product.Format != ProductFormat.Digital && product.Format != ProductFormat.Both)
            {
                throw new ValidationException("Product is not available in digital format.");
            }

            // Save the file
            var filePath = await _fileStorageService.SaveFileAsync(dto.File, "digital-products");
            var fileUrl = await _fileStorageService.GetFileUrlAsync(filePath);

            // Update product with digital file info
            product.DigitalFilePath = filePath;
            product.DigitalFileUrl = fileUrl;
            product.DigitalFileSize = dto.File.Length;
            product.DigitalFileMimeType = dto.File.ContentType;

            if (dto.MaxDownloads.HasValue)
                product.MaxDownloads = dto.MaxDownloads.Value;

            if (dto.DownloadExpiryDays.HasValue)
                product.DownloadExpiryDays = dto.DownloadExpiryDays.Value;

            await _productRepository.UpdateAsync(product);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Digital file uploaded for product {ProductId}: {FilePath}",
                product.Id, filePath);

            return fileUrl;
        }

        public async Task<bool> DeleteDigitalFileAsync(Guid productId)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null || string.IsNullOrEmpty(product.DigitalFilePath))
            {
                return false;
            }

            // Delete file from storage
            await _fileStorageService.DeleteFileAsync(product.DigitalFilePath);

            // Clear product digital file info
            product.DigitalFilePath = null;
            product.DigitalFileUrl = null;
            product.DigitalFileSize = null;
            product.DigitalFileMimeType = null;

            await _productRepository.UpdateAsync(product);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Digital file deleted for product {ProductId}", productId);
            return true;
        }

        public async Task<DigitalDownloadDto> GenerateDownloadLinkAsync(Guid orderItemId, Guid customerId)
        {
            // Get order item and validate
            var orderItem = await _orderRepository.GetOrderItemByIdAsync(orderItemId);
            if (orderItem == null)
            {
                throw new NotFoundException("Order item", orderItemId);
            }

            // Check if product is digital
            if (orderItem.Product.Format == ProductFormat.Print)
            {
                throw new ValidationException("Product is not available in digital format.");
            }

            // Check if order is paid
            var order = orderItem.Order;
            if (order.PaymentStatus != PaymentStatus.Paid)
            {
                throw new ValidationException("Order is not paid yet.");
            }

            // Get or create digital access
            var digitalAccess = await _unitOfWork.DigitalAccesses.GetValidAccessAsync(orderItemId, customerId);
            if (digitalAccess == null)
            {
                digitalAccess = await GrantDigitalAccessAsync(orderItemId);
            }

            // Generate download token if needed
            if (string.IsNullOrEmpty(digitalAccess.CurrentToken) ||
                !digitalAccess.TokenExpiresAt.HasValue ||
                digitalAccess.TokenExpiresAt.Value < DateTime.UtcNow.AddMinutes(5))
            {
                digitalAccess.GenerateNewToken(TimeSpan.FromHours(24)); // 24-hour token
                await _unitOfWork.DigitalAccesses.UpdateAsync(digitalAccess);
                await _unitOfWork.SaveChangesAsync();
            }

            // Generate secure download URL
            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://publications.mentisera.pk";
            var downloadUrl = $"{baseUrl}/api/download/digital/{digitalAccess.CurrentToken}";

            var downloadsRemaining = digitalAccess.MaxDownloads - digitalAccess.DownloadCount;
            var product = orderItem.Product;

            return new DigitalDownloadDto
            {
                DownloadUrl = downloadUrl,
                SecureToken = digitalAccess.CurrentToken!,
                ExpiresAt = digitalAccess.TokenExpiresAt!.Value,
                DownloadsRemaining = downloadsRemaining,
                FileName = GetFileNameFromPath(product.DigitalFilePath),
                FileSize = product.DigitalFileSize ?? 0,
                MimeType = product.DigitalFileMimeType ?? "application/octet-stream"
            };
        }

        public async Task<DigitalAccessDto> GetDigitalAccessAsync(Guid digitalAccessId)
        {
            var digitalAccess = await _unitOfWork.DigitalAccesses.GetByIdAsync(digitalAccessId);
            if (digitalAccess == null)
            {
                throw new NotFoundException("Digital access", digitalAccessId);
            }

            return await MapDigitalAccessToDto(digitalAccess);
        }

        public async Task<DigitalAccessDto> GetDigitalAccessByOrderItemAsync(Guid orderItemId)
        {
            var digitalAccess = await _unitOfWork.DigitalAccesses.GetByOrderItemIdAsync(orderItemId);
            if (digitalAccess == null)
            {
                throw new NotFoundException("Digital access for order item", orderItemId);
            }

            return await MapDigitalAccessToDto(digitalAccess);
        }

        public async Task<IEnumerable<DigitalAccessDto>> GetCustomerDigitalAccessAsync(Guid customerId)
        {
            var accessList = await _unitOfWork.DigitalAccesses.GetActiveAccessByCustomerAsync(customerId);
            var dtos = new List<DigitalAccessDto>();

            foreach (var access in accessList)
            {
                dtos.Add(await MapDigitalAccessToDto(access));
            }

            return dtos;
        }

        public async Task<DigitalAccessDto> GrantDigitalAccessAsync(Guid orderItemId)
        {
            var orderItem = await _orderRepository.GetOrderItemByIdAsync(orderItemId);
            if (orderItem == null)
            {
                throw new NotFoundException("Order item", orderItemId);
            }

            // Check if access already exists
            var existingAccess = await _unitOfWork.DigitalAccesses.GetByOrderItemIdAsync(orderItemId);
            if (existingAccess != null)
            {
                return await MapDigitalAccessToDto(existingAccess);
            }

            // Validate product has digital file
            if (string.IsNullOrEmpty(orderItem.Product.DigitalFilePath))
            {
                throw new ValidationException("Digital file not available for this product.");
            }

            // Calculate expiry date
            DateTime? expiryDate = null;
            if (orderItem.Product.DownloadExpiryDays.HasValue)
            {
                expiryDate = DateTime.UtcNow.AddDays(orderItem.Product.DownloadExpiryDays.Value);
            }

            // Create digital access
            var digitalAccess = new DigitalAccess
            {
                OrderItemId = orderItemId,
                ProductId = orderItem.ProductId,
                CustomerId = orderItem.Order.CustomerId,
                AccessGrantedAt = DateTime.UtcNow,
                AccessExpiresAt = expiryDate,
                MaxDownloads = orderItem.Product.MaxDownloads ?? 3,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // Generate initial token
            digitalAccess.GenerateNewToken(TimeSpan.FromHours(24));

            await _unitOfWork.DigitalAccesses.AddAsync(digitalAccess);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Digital access granted for order item {OrderItemId} to customer {CustomerId}",
                orderItemId, orderItem.Order.CustomerId);

            return await MapDigitalAccessToDto(digitalAccess);
        }

        public async Task<DigitalAccessDto> UpdateDigitalAccessAsync(Guid digitalAccessId, UpdateDigitalAccessDto dto)
        {
            var digitalAccess = await _unitOfWork.DigitalAccesses.GetByIdAsync(digitalAccessId);
            if (digitalAccess == null)
            {
                throw new NotFoundException("Digital access", digitalAccessId);
            }

            if (dto.MaxDownloads.HasValue)
            {
                digitalAccess.MaxDownloads = dto.MaxDownloads.Value;
            }

            if (dto.DownloadExpiryDays.HasValue)
            {
                digitalAccess.AccessExpiresAt = DateTime.UtcNow.AddDays(dto.DownloadExpiryDays.Value);
            }

            if (dto.ResetDownloadCount == true)
            {
                digitalAccess.DownloadCount = 0;
                digitalAccess.LastDownloadedAt = null;
            }

            await _unitOfWork.DigitalAccesses.UpdateAsync(digitalAccess);
            await _unitOfWork.SaveChangesAsync();

            return await MapDigitalAccessToDto(digitalAccess);
        }

        public async Task<bool> RevokeDigitalAccessAsync(Guid digitalAccessId)
        {
            var digitalAccess = await _unitOfWork.DigitalAccesses.GetByIdAsync(digitalAccessId);
            if (digitalAccess == null)
            {
                return false;
            }

            digitalAccess.IsActive = false;
            digitalAccess.CurrentToken = null;
            digitalAccess.TokenExpiresAt = null;

            await _unitOfWork.DigitalAccesses.UpdateAsync(digitalAccess);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Digital access revoked: {DigitalAccessId}", digitalAccessId);
            return true;
        }

        public async Task<FileDownloadResult> ProcessDownloadAsync(string token)
        {
            var digitalAccess = await _unitOfWork.DigitalAccesses.GetByTokenAsync(token);
            if (digitalAccess == null)
            {
                throw new NotFoundException("Download token", token);
            }

            // Validate access
            if (!digitalAccess.IsActive)
            {
                throw new ValidationException("Digital access is no longer active.");
            }

            if (digitalAccess.IsExpired)
            {
                throw new ValidationException("Digital access has expired.");
            }

            if (!digitalAccess.HasDownloadsRemaining)
            {
                throw new ValidationException("Maximum download limit reached.");
            }

            // Get product
            var product = digitalAccess.Product;
            if (string.IsNullOrEmpty(product.DigitalFilePath))
            {
                throw new ValidationException("Digital file not found.");
            }

            // Get file stream
            var fileStream = await _fileStorageService.GetFileAsync(product.DigitalFilePath);

            // Update download count
            digitalAccess.IncrementDownloadCount();
            await _unitOfWork.DigitalAccesses.UpdateAsync(digitalAccess);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Digital download processed: Product {ProductId}, Customer {CustomerId}, Download #{DownloadCount}",
                product.Id, digitalAccess.CustomerId, digitalAccess.DownloadCount);

            return new FileDownloadResult
            {
                FileStream = fileStream,
                FileName = GetFileNameFromPath(product.DigitalFilePath),
                MimeType = product.DigitalFileMimeType ?? "application/octet-stream",
                FileSize = product.DigitalFileSize ?? 0,
                DigitalAccess = await MapDigitalAccessToDto(digitalAccess)
            };
        }

        public async Task<bool> ValidateDownloadTokenAsync(string token)
        {
            var digitalAccess = await _unitOfWork.DigitalAccesses.GetByTokenAsync(token);
            if (digitalAccess == null)
            {
                return false;
            }

            return digitalAccess.IsActive &&
                   !digitalAccess.IsExpired &&
                   digitalAccess.HasDownloadsRemaining;
        }

        public async Task<IEnumerable<DigitalAccessDto>> GetExpiredAccessAsync()
        {
            var expiredAccess = await _unitOfWork.DigitalAccesses.GetExpiredAccessAsync();
            var dtos = new List<DigitalAccessDto>();

            foreach (var access in expiredAccess)
            {
                dtos.Add(await MapDigitalAccessToDto(access));
            }

            return dtos;
        }

        public async Task<bool> CleanupExpiredAccessAsync()
        {
            var expiredAccess = await _unitOfWork.DigitalAccesses.GetExpiredAccessAsync();
            var count = 0;

            foreach (var access in expiredAccess)
            {
                access.IsActive = false;
                access.CurrentToken = null;
                access.TokenExpiresAt = null;
                count++;
            }

            if (count > 0)
            {
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Cleaned up {Count} expired digital access records", count);
            }

            return count > 0;
        }

        public async Task<DigitalAccessStatsDto> GetDigitalAccessStatsAsync()
        {
            var allAccess = await _unitOfWork.DigitalAccesses.GetAllAsync();
            var now = DateTime.UtcNow;
            var thirtyDaysAgo = now.AddDays(-30);

            var stats = new DigitalAccessStatsDto
            {
                TotalAccessGrants = allAccess.Count(),
                ActiveAccessGrants = allAccess.Count(a => a.IsActive && !a.IsExpired && a.HasDownloadsRemaining),
                ExpiredAccessGrants = allAccess.Count(a => !a.IsActive || a.IsExpired || !a.HasDownloadsRemaining),
                TotalDownloads = allAccess.Sum(a => a.DownloadCount),
                DownloadsLast30Days = allAccess
                    .Where(a => a.LastDownloadedAt.HasValue && a.LastDownloadedAt.Value >= thirtyDaysAgo)
                    .Sum(a => a.DownloadCount)
            };

            // Group by product (get product names)
            var productGroups = new Dictionary<string, int>();
            foreach (var access in allAccess)
            {
                var productName = access.Product?.Title ?? "Unknown";
                if (!productGroups.ContainsKey(productName))
                    productGroups[productName] = 0;
                productGroups[productName] += access.DownloadCount;
            }

            stats.DownloadsByProduct = productGroups;

            // Group by customer (top 10)
            var customerGroups = new Dictionary<string, int>();
            foreach (var access in allAccess)
            {
                var customerEmail = access.Customer?.Email ?? "Unknown";
                if (!customerGroups.ContainsKey(customerEmail))
                    customerGroups[customerEmail] = 0;
                customerGroups[customerEmail] += access.DownloadCount;
            }

            stats.DownloadsByCustomer = customerGroups
                .OrderByDescending(kv => kv.Value)
                .Take(10)
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            return stats;
        }

        private async Task<DigitalAccessDto> MapDigitalAccessToDto(DigitalAccess digitalAccess)
        {
            var dto = new DigitalAccessDto
            {
                Id = digitalAccess.Id,
                OrderItemId = digitalAccess.OrderItemId,
                ProductId = digitalAccess.ProductId,
                ProductTitle = digitalAccess.Product?.Title ?? "Unknown Product",
                ProductFormat = digitalAccess.Product?.Format.ToString() ?? "Digital",
                AccessGrantedAt = digitalAccess.AccessGrantedAt,
                AccessExpiresAt = digitalAccess.AccessExpiresAt,
                DownloadCount = digitalAccess.DownloadCount,
                MaxDownloads = digitalAccess.MaxDownloads,
                LastDownloadedAt = digitalAccess.LastDownloadedAt,
                HasDownloadsRemaining = digitalAccess.HasDownloadsRemaining,
                IsExpired = digitalAccess.IsExpired,
                CustomerId = digitalAccess.CustomerId,
                CustomerEmail = digitalAccess.Customer?.Email ?? "Unknown"
            };

            // Generate download URL if access is valid
            if (digitalAccess.IsActive && !digitalAccess.IsExpired && digitalAccess.HasDownloadsRemaining)
            {
                var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://publications.mentisera.pk";
                dto.DownloadUrl = $"{baseUrl}/api/download/digital/{digitalAccess.CurrentToken}";
            }

            return dto;
        }

        private string GetFileNameFromPath(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "download.pdf";

            return Path.GetFileName(filePath);
        }
    }
}
