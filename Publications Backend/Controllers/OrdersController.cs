using Application.DTOs;
using Application.Services;
using Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Publications_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ICartService _cartService;

        public OrdersController(IOrderService orderService, ICartService cartService)
        {
            _orderService = orderService;
            _cartService = cartService;
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            throw new UnauthorizedAccessException("User ID not found in token");
        }

        [HttpGet]
        public async Task<IActionResult> GetMyOrders()
        {
            try
            {
                var userId = GetUserId();
                var orders = await _orderService.GetOrdersByCustomerAsync(userId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(Guid id)
        {
            try
            {
                var order = await _orderService.GetOrderAsync(id);

                // Check if user owns this order
                var userId = GetUserId();
                if (order.CustomerId != userId && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
                {
                    return Forbid();
                }

                return Ok(order);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            try
            {
                var userId = GetUserId();
                var order = await _orderService.CreateOrderAsync(dto, userId);
                return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("from-cart")]
        public async Task<IActionResult> CreateOrderFromCart([FromBody] CreateOrderDto? dto = null)
        {
            try
            {
                var userId = GetUserId();
                var sessionId = Request.Cookies["cart_session_id"];

                var order = await _orderService.CreateOrderFromCartAsync(userId, sessionId, dto);
                return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelOrderDto? dto = null)
        {
            try
            {
                var order = await _orderService.GetOrderAsync(id);
                var userId = GetUserId();

                // Check if user owns this order or is admin
                if (order.CustomerId != userId && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
                {
                    return Forbid();
                }

                var cancelledOrder = await _orderService.CancelOrderAsync(id, dto?.Reason);
                return Ok(cancelledOrder);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("number/{orderNumber}")]
        public async Task<IActionResult> GetOrderByNumber(string orderNumber)
        {
            try
            {
                var order = await _orderService.GetOrderByNumberAsync(orderNumber);

                // Check if user owns this order
                var userId = GetUserId();
                if (order.CustomerId != userId && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
                {
                    return Forbid();
                }

                return Ok(order);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin,SuperAdmin,FinanceAdmin")]
        public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
        {
            try
            {
                var order = await _orderService.UpdateOrderStatusAsync(id, dto.Status);
                return Ok(order);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public class CancelOrderDto
    {
        public string? Reason { get; set; }
    }

    public class UpdateOrderStatusDto
    {
        public Domain.Enums.OrderStatus Status { get; set; }
    }
}
