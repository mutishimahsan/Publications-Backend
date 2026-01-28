using Application.Interfaces;
using Application.Services;
using Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace Publications_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DownloadController : ControllerBase
    {
        private readonly IDigitalProductService _digitalProductService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IInvoiceService _invoiceService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DownloadController> _logger;

        public DownloadController(
            IFileStorageService fileStorageService,
            IInvoiceService invoiceService,
            IConfiguration configuration,
            ILogger<DownloadController> logger)
        {
            _fileStorageService = fileStorageService;
            _invoiceService = invoiceService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("secure")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadSecure(
            [FromQuery] string file,
            [FromQuery] Guid? user,
            [FromQuery] string expiry,
            [FromQuery] string token)
        {
            try
            {
                // Validate token
                if (!IsValidToken(file, user, expiry, token))
                {
                    return Unauthorized(new { message = "Invalid or expired download token" });
                }

                // Parse expiry
                if (!DateTime.TryParse(expiry, out var expiryDate))
                {
                    return BadRequest(new { message = "Invalid expiry date" });
                }

                // Check if token is expired
                if (expiryDate < DateTime.UtcNow)
                {
                    return Unauthorized(new { message = "Download link has expired" });
                }

                // Get file stream
                using var stream = await _fileStorageService.GetFileAsync(file);
                var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var fileName = Path.GetFileName(file);

                // Check if this is an invoice file and record download
                if (file.Contains("invoices", StringComparison.OrdinalIgnoreCase) && user.HasValue)
                {
                    try
                    {
                        // Try to find invoice by filename pattern
                        var invoiceNumber = Path.GetFileNameWithoutExtension(file)
                            .Replace("-", "/")  // Convert filename back to invoice number
                            .Replace("_", "-");

                        // You might want to add a method to find invoice by filename
                        // For now, we'll just log the download attempt
                        _logger.LogInformation("Invoice download attempted: {InvoiceNumber} by user {UserId}",
                            invoiceNumber, user.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to record invoice download for file {File}", file);
                    }
                }

                return File(memoryStream, GetContentType(fileName), fileName);
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { message = "File not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in secure download for file {File}", file);
                return StatusCode(500, new { message = "Error downloading file" });
            }
        }

        [HttpGet("invoice/{id}")]
        [Authorize]
        public async Task<IActionResult> DownloadInvoice(Guid id)
        {
            try
            {
                var invoice = await _invoiceService.GetInvoiceByIdAsync(id);

                // Check if user has permission to download this invoice
                var currentUserId = GetCurrentUserId();
                if (currentUserId != invoice.Customer?.Id && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                var pdfBytes = await _invoiceService.GetInvoicePdfBytesAsync(id);

                // Record the download
                await _invoiceService.RecordInvoiceDownloadAsync(id);

                var fileName = $"{invoice.InvoiceNumber}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Domain.Common.NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading invoice {InvoiceId}", id);
                return StatusCode(500, new { message = "Error downloading invoice" });
            }
        }

        [HttpGet("digital/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadDigitalFile(string token)
        {
            try
            {
                var result = await _digitalProductService.ProcessDownloadAsync(token);

                // Set download headers
                Response.Headers.Add("X-Downloads-Remaining",
                    (result.DigitalAccess.MaxDownloads - result.DigitalAccess.DownloadCount).ToString());
                Response.Headers.Add("X-Product-Name", result.DigitalAccess.ProductTitle);

                return File(result.FileStream, result.MimeType, result.FileName);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing digital download with token {Token}", token);
                return StatusCode(500, new { message = "An error occurred while downloading the file" });
            }
        }


        [HttpGet("digital-product/{orderItemId}")]
        [Authorize]
        public async Task<IActionResult> DownloadDigitalProduct(Guid orderItemId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var downloadLink = await _digitalProductService.GenerateDownloadLinkAsync(orderItemId, currentUserId);
                return Ok(downloadLink);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating download link for order item {OrderItemId}", orderItemId);
                return StatusCode(500, new { message = "An error occurred while generating download link" });
            }
        }

        private bool IsValidToken(string filePath, Guid? userId, string expiry, string token)
        {
            var data = $"{filePath}|{userId}|{expiry}";
            var secret = _configuration["StorageSettings:DigitalFilesPath"] ?? "default-secret-for-file-downloads";

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var expectedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            var expectedToken = Convert.ToBase64String(expectedHash);

            // Compare tokens (constant-time comparison to prevent timing attacks)
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedToken),
                Encoding.UTF8.GetBytes(Uri.UnescapeDataString(token)));
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".txt" => "text/plain",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream",
            };
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("uid") ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            return Guid.Empty;
        }


    }
}
