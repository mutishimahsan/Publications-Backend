using Application.DTOs;
using Application.Services;
using Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace Publications_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IConfiguration _configuration;

        public PaymentsController(IPaymentService paymentService, IConfiguration configuration)
        {
            _paymentService = paymentService;
            _configuration = configuration;
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPayment(Guid id)
        {
            try
            {
                var payment = await _paymentService.GetPaymentAsync(id);
                return Ok(payment);
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

        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetOrderPayments(Guid orderId)
        {
            try
            {
                var payments = await _paymentService.GetPaymentsByOrderAsync(orderId);
                return Ok(payments);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("online")]
        public async Task<IActionResult> ProcessOnlinePayment([FromBody] ProcessOnlinePaymentDto dto)
        {
            try
            {
                var payment = await _paymentService.ProcessOnlinePaymentAsync(dto);
                return Ok(payment);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("offline")]
        public async Task<IActionResult> ProcessOfflinePayment([FromForm] ProcessOfflinePaymentDto dto)
        {
            try
            {
                var payment = await _paymentService.ProcessOfflinePaymentAsync(dto);
                return Ok(payment);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("stripe/checkout/{orderId}")]
        public async Task<IActionResult> CreateStripeCheckoutSession(Guid orderId)
        {
            try
            {
                var userId = GetUserId();
                var session = await _paymentService.CreateStripeCheckoutSessionAsync(orderId, userId);
                return Ok(session);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("stripe/webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleStripeWebhook()
        {
            try
            {
                var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                var signature = Request.Headers["Stripe-Signature"];

                var result = await _paymentService.HandleStripeWebhookAsync(json, signature);

                if (result)
                {
                    return Ok();
                }
                else
                {
                    return BadRequest(new { message = "Webhook processing failed" });
                }
            }
            catch (StripeException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("verify/{paymentReference}")]
        public async Task<IActionResult> VerifyPayment(string paymentReference)
        {
            try
            {
                var payment = await _paymentService.VerifyAndCompletePaymentAsync(paymentReference);
                return Ok(payment);
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

        [HttpGet("pending/offline")]
        [Authorize(Roles = "SuperAdmin,FinanceAdmin")]
        public async Task<IActionResult> GetPendingOfflinePayments()
        {
            try
            {
                var payments = await _paymentService.GetPendingOfflinePaymentsAsync();
                return Ok(payments);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("offline/approve")]
        [Authorize(Roles = "SuperAdmin,FinanceAdmin")]
        public async Task<IActionResult> ApproveOfflinePayment([FromBody] ApproveOfflinePaymentDto dto)
        {
            try
            {
                var approvedBy = User.Identity?.Name ?? "System";
                var payment = await _paymentService.ApproveOfflinePaymentAsync(dto, approvedBy);
                return Ok(payment);
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
}
