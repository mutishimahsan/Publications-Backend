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
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoicesController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetInvoice(Guid id)
        {
            try
            {
                var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
                return Ok(invoice);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("number/{invoiceNumber}")]
        public async Task<IActionResult> GetInvoiceByNumber(string invoiceNumber)
        {
            try
            {
                var invoice = await _invoiceService.GetInvoiceByNumberAsync(invoiceNumber);
                return Ok(invoice);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetInvoicesByOrder(Guid orderId)
        {
            try
            {
                var invoices = await _invoiceService.GetInvoicesByOrderIdAsync(orderId);
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetCustomerInvoices(Guid customerId)
        {
            try
            {
                var invoices = await _invoiceService.GetInvoicesByCustomerIdAsync(customerId);
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("status/{status}")]
        [Authorize(Roles = "SuperAdmin,FinanceAdmin")]
        public async Task<IActionResult> GetInvoicesByStatus(string status)
        {
            try
            {
                var invoices = await _invoiceService.GetInvoicesByStatusAsync(status);
                return Ok(invoices);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("generate")]
        [Authorize(Roles = "SuperAdmin,FinanceAdmin")]
        public async Task<IActionResult> GenerateInvoice([FromBody] GenerateInvoiceDto dto)
        {
            try
            {
                var invoice = await _invoiceService.GenerateInvoiceAsync(dto);
                return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, invoice);
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
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPut("{id}/status/{status}")]
        [Authorize(Roles = "SuperAdmin,FinanceAdmin")]
        public async Task<IActionResult> UpdateInvoiceStatus(Guid id, string status)
        {
            try
            {
                var invoice = await _invoiceService.UpdateInvoiceStatusAsync(id, status);
                return Ok(invoice);
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
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("{id}/pdf")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadInvoicePdf(Guid id, [FromQuery] string token)
        {
            try
            {
                // In production, validate the token here
                var pdfBytes = await _invoiceService.GetInvoicePdfBytesAsync(id);

                // Record the download
                await _invoiceService.RecordInvoiceDownloadAsync(id);

                var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
                var fileName = $"{invoice.InvoiceNumber}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("{id}/download-url")]
        public async Task<IActionResult> GetInvoiceDownloadUrl(Guid id)
        {
            try
            {
                var downloadUrl = await _invoiceService.GetInvoiceDownloadUrlAsync(id);
                return Ok(new { downloadUrl });
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
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("{id}/send-email")]
        [Authorize(Roles = "SuperAdmin,FinanceAdmin")]
        public async Task<IActionResult> SendInvoiceEmail(Guid id)
        {
            try
            {
                var success = await _invoiceService.SendInvoiceEmailAsync(id);
                if (success)
                    return Ok(new { message = "Invoice email sent successfully" });

                return BadRequest(new { message = "Failed to send invoice email" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("period")]
        [Authorize(Roles = "SuperAdmin,FinanceAdmin")]
        public async Task<IActionResult> GetInvoicesByPeriod([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var invoices = await _invoiceService.GetInvoicesByDateRangeAsync(startDate, endDate);
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
