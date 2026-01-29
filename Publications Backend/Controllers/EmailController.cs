using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Publications_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public EmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendEmail([FromBody] SendEmailRequest request)
        {
            try
            {
                await _emailService.SendEmailAsync(request.To, request.Subject, request.Body, request.IsHtml);
                return Ok("Email sent successfully");
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to send email: {ex.Message}");
            }
        }

        [HttpPost("test")]
        public async Task<IActionResult> SendTestEmail([FromBody] SendTestEmailRequest request)
        {
            try
            {
                // Create a simple test email using existing method
                await _emailService.SendEmailAsync(request.Email, "Test Email", "This is a test email");
                return Ok("Test email sent successfully");
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to send test email: {ex.Message}");
            }
        }

        [HttpPost("order-confirmation/{orderId}")]
        public async Task<IActionResult> SendOrderConfirmation(Guid orderId)
        {
            try
            {
                await _emailService.SendOrderConfirmationAsync(orderId);
                return Ok("Order confirmation email sent");
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to send order confirmation: {ex.Message}");
            }
        }

        [HttpPost("invoice/{invoiceId}")]
        public async Task<IActionResult> SendInvoiceEmail(Guid invoiceId)
        {
            try
            {
                await _emailService.SendInvoiceAsync(invoiceId);
                return Ok("Invoice email sent");
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to send invoice email: {ex.Message}");
            }
        }
    }

    public class SendEmailRequest
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = true;
    }

    public class SendTestEmailRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}
