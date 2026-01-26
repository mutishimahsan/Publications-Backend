using Application.DTOs;
using Application.Interfaces;
using Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Publications_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DigitalProductsController : ControllerBase
    {
        private readonly IDigitalProductService _digitalProductService;
        private readonly ILogger<DigitalProductsController> _logger;

        public DigitalProductsController(
            IDigitalProductService digitalProductService,
            ILogger<DigitalProductsController> logger)
        {
            _digitalProductService = digitalProductService;
            _logger = logger;
        }

        [HttpPost("upload")]
        [Authorize(Roles = "SuperAdmin,ContentAdmin")]
        public async Task<IActionResult> UploadDigitalProduct([FromForm] UploadDigitalProductDto dto)
        {
            try
            {
                var fileUrl = await _digitalProductService.UploadDigitalFileAsync(dto);
                return Ok(new { fileUrl, message = "Digital file uploaded successfully" });
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
                _logger.LogError(ex, "Error uploading digital file for product {ProductId}", dto.ProductId);
                return StatusCode(500, new { message = "An error occurred while uploading the digital file" });
            }
        }

        [HttpDelete("{productId}/file")]
        [Authorize(Roles = "SuperAdmin,ContentAdmin")]
        public async Task<IActionResult> DeleteDigitalFile(Guid productId)
        {
            try
            {
                var success = await _digitalProductService.DeleteDigitalFileAsync(productId);
                if (success)
                    return Ok(new { message = "Digital file deleted successfully" });

                return NotFound(new { message = "Digital file not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting digital file for product {ProductId}", productId);
                return StatusCode(500, new { message = "An error occurred while deleting the digital file" });
            }
        }

        [HttpGet("access/my")]
        public async Task<IActionResult> GetMyDigitalAccess()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var accessList = await _digitalProductService.GetCustomerDigitalAccessAsync(currentUserId);
                return Ok(accessList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting digital access for user {UserId}", GetCurrentUserId());
                return StatusCode(500, new { message = "An error occurred while retrieving digital access" });
            }
        }

        [HttpGet("access/{id}")]
        public async Task<IActionResult> GetDigitalAccess(Guid id)
        {
            try
            {
                var access = await _digitalProductService.GetDigitalAccessAsync(id);

                // Check authorization
                var currentUserId = GetCurrentUserId();
                if (access.CustomerId != currentUserId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                return Ok(access);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting digital access {AccessId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving digital access" });
            }
        }

        [HttpPost("access/{id}/revoke")]
        [Authorize(Roles = "SuperAdmin,ContentAdmin")]
        public async Task<IActionResult> RevokeDigitalAccess(Guid id)
        {
            try
            {
                var success = await _digitalProductService.RevokeDigitalAccessAsync(id);
                if (success)
                    return Ok(new { message = "Digital access revoked successfully" });

                return NotFound(new { message = "Digital access not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking digital access {AccessId}", id);
                return StatusCode(500, new { message = "An error occurred while revoking digital access" });
            }
        }

        [HttpPut("access/{id}")]
        [Authorize(Roles = "SuperAdmin,ContentAdmin")]
        public async Task<IActionResult> UpdateDigitalAccess(Guid id, [FromBody] UpdateDigitalAccessDto dto)
        {
            try
            {
                var access = await _digitalProductService.UpdateDigitalAccessAsync(id, dto);
                return Ok(access);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating digital access {AccessId}", id);
                return StatusCode(500, new { message = "An error occurred while updating digital access" });
            }
        }

        [HttpGet("stats")]
        [Authorize(Roles = "SuperAdmin,ContentAdmin")]
        public async Task<IActionResult> GetDigitalAccessStats()
        {
            try
            {
                var stats = await _digitalProductService.GetDigitalAccessStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting digital access stats");
                return StatusCode(500, new { message = "An error occurred while retrieving statistics" });
            }
        }

        [HttpGet("expired")]
        [Authorize(Roles = "SuperAdmin,ContentAdmin")]
        public async Task<IActionResult> GetExpiredAccess()
        {
            try
            {
                var expiredAccess = await _digitalProductService.GetExpiredAccessAsync();
                return Ok(expiredAccess);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expired digital access");
                return StatusCode(500, new { message = "An error occurred while retrieving expired access" });
            }
        }

        [HttpPost("cleanup")]
        [Authorize(Roles = "SuperAdmin,ContentAdmin")]
        public async Task<IActionResult> CleanupExpiredAccess()
        {
            try
            {
                var cleaned = await _digitalProductService.CleanupExpiredAccessAsync();
                if (cleaned)
                    return Ok(new { message = "Expired digital access cleaned up successfully" });

                return Ok(new { message = "No expired digital access to clean up" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired digital access");
                return StatusCode(500, new { message = "An error occurred while cleaning up expired access" });
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("uid") ??
                             User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier") ??
                             User.FindFirst("sub");

            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            return Guid.Empty;
        }
    }
}
