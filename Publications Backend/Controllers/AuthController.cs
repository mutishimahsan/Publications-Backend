using Application.DTOs;
using Application.Services;
using Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Publications_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IUserService userService, ILogger<AuthController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            try
            {
                var result = await _userService.RegisterAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            try
            {
                var result = await _userService.LoginAsync(dto);
                return Content(result.Token, "text/plain");
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpGet("get-profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                _logger.LogInformation("GetProfile endpoint called");

                // Try multiple claim types to be safe
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                                ?? User.FindFirst("userId")
                                ?? User.FindFirst(JwtRegisteredClaimNames.Sub);

                _logger.LogInformation($"Found user ID claim: {userIdClaim?.Type} = {userIdClaim?.Value}");

                if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
                {
                    _logger.LogWarning("No valid user ID claim found in token");
                    return Unauthorized(new { message = "Invalid token: No user identifier found" });
                }

                if (!Guid.TryParse(userIdClaim.Value, out var userGuid))
                {
                    _logger.LogWarning($"Invalid user ID format in claim: {userIdClaim.Value}");
                    return Unauthorized(new { message = "Invalid user identifier format" });
                }

                _logger.LogInformation($"Getting profile for user ID: {userGuid}");

                try
                {
                    var profile = await _userService.GetProfileAsync(userGuid);
                    _logger.LogInformation($"Successfully retrieved profile for user: {userGuid}");
                    return Ok(profile);
                }
                catch (NotFoundException ex)
                {
                    _logger.LogWarning($"Profile not found for user ID: {userGuid}");
                    return NotFound(new { message = ex.Message });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error getting profile for user ID: {userGuid}");
                    return StatusCode(500, new { message = "An error occurred while retrieving profile" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetProfile endpoint");
                return StatusCode(500, new { message = "An unexpected error occurred" });
            }
        }

        [HttpPut("update-profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? User.FindFirstValue("userId")
                             ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var profile = await _userService.UpdateProfileAsync(userGuid, dto);
                return Ok(profile);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            try
            {
                var userId = User.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                {
                    return Unauthorized();
                }

                await _userService.ChangePasswordAsync(userGuid, dto.CurrentPassword, dto.NewPassword);
                return Ok(new { message = "Password changed successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
