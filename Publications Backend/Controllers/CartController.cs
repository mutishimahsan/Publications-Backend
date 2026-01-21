using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Publications_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        private Guid? GetUserId()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return null;
        }

        private string GetSessionId()
        {
            var sessionId = Request.Cookies["cart_session_id"];
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
                Response.Cookies.Append("cart_session_id", sessionId, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(30)
                });
            }
            return sessionId;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            try
            {
                var userId = GetUserId();
                var sessionId = userId == null ? GetSessionId() : null;

                var cart = await _cartService.GetCartAsync(userId, sessionId);
                return Ok(cart);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            try
            {
                var userId = GetUserId();
                var sessionId = userId == null ? GetSessionId() : null;

                var cart = await _cartService.AddToCartAsync(dto, userId, sessionId);
                return Ok(cart);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("items/{productId}")]
        public async Task<IActionResult> UpdateCartItem(Guid productId, [FromBody] UpdateCartItemDto dto)
        {
            try
            {
                var userId = GetUserId();
                var sessionId = userId == null ? GetSessionId() : null;

                var cart = await _cartService.UpdateCartItemAsync(productId, dto, userId, sessionId);
                return Ok(cart);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("items/{productId}")]
        public async Task<IActionResult> RemoveFromCart(Guid productId)
        {
            try
            {
                var userId = GetUserId();
                var sessionId = userId == null ? GetSessionId() : null;

                var cart = await _cartService.RemoveFromCartAsync(productId, userId, sessionId);
                return Ok(cart);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var userId = GetUserId();
                var sessionId = userId == null ? GetSessionId() : null;

                await _cartService.ClearCartAsync(userId, sessionId);
                return Ok(new { message = "Cart cleared successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetCartItemCount()
        {
            try
            {
                var userId = GetUserId();
                var sessionId = userId == null ? GetSessionId() : null;

                var count = await _cartService.GetCartItemCountAsync(userId, sessionId);
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("merge")]
        [Authorize]
        public async Task<IActionResult> MergeCart([FromQuery] string sessionId)
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                var cart = await _cartService.MergeCartsAsync(userId.Value, sessionId);
                return Ok(cart);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
