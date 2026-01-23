using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Common;
using Domain.Enums;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;
        private readonly IPdfService _pdfService;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly ILogger<InvoiceService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public InvoiceService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService,
            IPdfService pdfService,
            IEmailService emailService,
            IMapper mapper,
            ILogger<InvoiceService> logger,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
            _pdfService = pdfService;
            _emailService = emailService;
            _mapper = mapper;
            _logger = logger;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<InvoiceDto> GetInvoiceByIdAsync(Guid id)
        {
            var invoice = await _unitOfWork.Invoices.GetByIdAsync(id);
            if (invoice == null)
            {
                throw new NotFoundException("Invoice", id);
            }

            return await MapInvoiceToDto(invoice);
        }

        public async Task<InvoiceDto> GetInvoiceByNumberAsync(string invoiceNumber)
        {
            var invoice = await _unitOfWork.Invoices.GetByInvoiceNumberAsync(invoiceNumber);
            if (invoice == null)
            {
                throw new NotFoundException("Invoice", invoiceNumber);
            }

            return await MapInvoiceToDto(invoice);
        }

        public async Task<IEnumerable<InvoiceDto>> GetInvoicesByOrderIdAsync(Guid orderId)
        {
            var invoices = await _unitOfWork.Invoices.GetByOrderIdAsync(orderId);
            var dtos = new List<InvoiceDto>();

            foreach (var invoice in invoices)
            {
                dtos.Add(await MapInvoiceToDto(invoice));
            }

            return dtos;
        }

        public async Task<IEnumerable<InvoiceDto>> GetInvoicesByCustomerIdAsync(Guid customerId)
        {
            var invoices = await _unitOfWork.Invoices.GetByCustomerIdAsync(customerId);
            var dtos = new List<InvoiceDto>();

            foreach (var invoice in invoices)
            {
                dtos.Add(await MapInvoiceToDto(invoice));
            }

            return dtos;
        }

        public async Task<IEnumerable<InvoiceDto>> GetInvoicesByStatusAsync(string status)
        {
            if (!Enum.TryParse<InvoiceStatus>(status, true, out var statusEnum))
            {
                throw new ValidationException("Invalid invoice status.");
            }

            var invoices = await _unitOfWork.Invoices.GetInvoicesByStatusAsync(statusEnum);
            var dtos = new List<InvoiceDto>();

            foreach (var invoice in invoices)
            {
                dtos.Add(await MapInvoiceToDto(invoice));
            }

            return dtos;
        }

        public async Task<InvoiceDto> GenerateInvoiceAsync(GenerateInvoiceDto dto)
        {
            // Get order details
            var order = await _unitOfWork.Orders.GetByIdAsync(dto.OrderId);
            if (order == null)
            {
                throw new NotFoundException("Order", dto.OrderId);
            }

            // Check if invoice already exists for this order
            var existingInvoices = await _unitOfWork.Invoices.GetByOrderIdAsync(order.Id);
            if (existingInvoices.Any(i => i.Status != InvoiceStatus.Cancelled))
            {
                throw new ValidationException("An active invoice already exists for this order.");
            }

            // Validate order has items
            if (!order.OrderItems.Any())
            {
                throw new ValidationException("Cannot generate invoice for empty order.");
            }

            // Generate invoice number
            var invoiceNumber = await GenerateInvoiceNumberAsync();

            // Create invoice
            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumber,
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                InvoiceDate = dto.InvoiceDate ?? DateTime.UtcNow,
                DueDate = dto.DueDate ?? DateTime.UtcNow.AddDays(30),
                Status = InvoiceStatus.Issued,
                Subtotal = order.Subtotal,
                TaxAmount = order.TaxAmount,
                TotalAmount = order.TotalAmount,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _unitOfWork.Invoices.AddAsync(invoice);
            await _unitOfWork.SaveChangesAsync();

            // Generate PDF
            var pdfUrl = await GenerateInvoicePdfAsync(invoice.Id);

            return await MapInvoiceToDto(invoice);
        }

        public async Task<InvoiceDto> UpdateInvoiceStatusAsync(Guid invoiceId, string status)
        {
            if (!Enum.TryParse<InvoiceStatus>(status, true, out var statusEnum))
            {
                throw new ValidationException("Invalid invoice status.");
            }

            await _unitOfWork.Invoices.UpdateInvoiceStatusAsync(invoiceId, statusEnum);
            await _unitOfWork.SaveChangesAsync();

            var invoice = await _unitOfWork.Invoices.GetByIdAsync(invoiceId);
            return await MapInvoiceToDto(invoice);
        }

        public async Task<string> GenerateInvoicePdfAsync(Guid invoiceId)
        {
            var invoice = await _unitOfWork.Invoices.GetByIdAsync(invoiceId);
            if (invoice == null)
            {
                throw new NotFoundException("Invoice", invoiceId);
            }

            // Generate PDF using QuestPDF
            var pdfBytes = await _pdfService.GenerateInvoicePdfAsync(invoice);

            // Save PDF to storage
            var fileName = $"{invoice.InvoiceNumber.Replace("/", "-")}.pdf";

            // Create IFormFile from byte array
            var stream = new MemoryStream(pdfBytes);
            var formFile = new FormFile(stream, 0, pdfBytes.Length, "invoice", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var filePath = await _fileStorageService.SaveFileAsync(formFile, "invoices");

            // Update invoice with file path
            invoice.FilePath = filePath;
            invoice.FileUrl = await _fileStorageService.GetFileUrlAsync(filePath);

            await _unitOfWork.Invoices.UpdateAsync(invoice);
            await _unitOfWork.SaveChangesAsync();

            return invoice.FileUrl;
        }

        public async Task<byte[]> GetInvoicePdfBytesAsync(Guid invoiceId)
        {
            var invoice = await _unitOfWork.Invoices.GetByIdAsync(invoiceId);
            if (invoice == null || string.IsNullOrEmpty(invoice.FilePath))
            {
                throw new NotFoundException("Invoice PDF", invoiceId);
            }

            using var stream = await _fileStorageService.GetFileAsync(invoice.FilePath);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            return memoryStream.ToArray();
        }

        public async Task<string> GetInvoiceDownloadUrlAsync(Guid invoiceId)
        {
            var invoice = await _unitOfWork.Invoices.GetByIdAsync(invoiceId);
            if (invoice == null)
            {
                throw new NotFoundException("Invoice", invoiceId);
            }

            if (string.IsNullOrEmpty(invoice.FilePath))
            {
                throw new ValidationException("Invoice PDF not generated yet.");
            }

            // Generate secure download URL
            var userId = invoice.CustomerId ?? Guid.Empty;
            return await _fileStorageService.GenerateSecureDownloadUrlAsync(invoice.FilePath, userId, 72);
        }

        public async Task<bool> SendInvoiceEmailAsync(Guid invoiceId)
        {
            try
            {
                var invoice = await _unitOfWork.Invoices.GetByIdAsync(invoiceId);
                if (invoice == null || invoice.Customer == null)
                {
                    return false;
                }

                // Generate download URL
                var downloadUrl = await GetInvoiceDownloadUrlAsync(invoiceId);
                var invoiceDto = await MapInvoiceToDto(invoice);

                // Send email using the existing method
                await _emailService.SendInvoiceAsync(invoiceId); // Use existing method

                // Or if you want to use the email and download URL, you need to update the EmailService implementation

                // Update invoice record
                invoice.EmailedAt = DateTime.UtcNow;
                invoice.Status = InvoiceStatus.Sent;

                await _unitOfWork.Invoices.UpdateAsync(invoice);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending invoice email for invoice {InvoiceId}", invoiceId);
                return false;
            }
        }

        public async Task<bool> RecordInvoiceDownloadAsync(Guid invoiceId)
        {
            try
            {
                await _unitOfWork.Invoices.IncrementDownloadCountAsync(invoiceId);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording invoice download for invoice {InvoiceId}", invoiceId);
                return false;
            }
        }

        public async Task<IEnumerable<InvoiceDto>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var invoices = await _unitOfWork.Invoices.GetInvoicesByDateRangeAsync(startDate, endDate);
            var dtos = new List<InvoiceDto>();

            foreach (var invoice in invoices)
            {
                dtos.Add(await MapInvoiceToDto(invoice));
            }

            return dtos;
        }

        public async Task<IEnumerable<InvoiceDto>> GetOverdueInvoicesAsync()
        {
            var overdueDate = DateTime.UtcNow;
            var invoices = await _unitOfWork.Invoices.GetInvoicesByStatusAsync(InvoiceStatus.Issued);

            var overdueInvoices = invoices
                .Where(i => i.DueDate.HasValue && i.DueDate.Value < overdueDate)
                .ToList();

            var dtos = new List<InvoiceDto>();
            foreach (var invoice in overdueInvoices)
            {
                dtos.Add(await MapInvoiceToDto(invoice));
            }

            return dtos;
        }

        private async Task<string> GenerateInvoiceNumberAsync()
        {
            // Format: INV-YYYYMM-XXXXX
            var prefix = "INV";
            var yearMonth = DateTime.UtcNow.ToString("yyyyMM");

            // Get invoices for current month
            var startDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var invoices = await _unitOfWork.Invoices.GetInvoicesByDateRangeAsync(startDate, endDate);

            int sequentialNumber = 1;
            if (invoices.Any())
            {
                var lastInvoice = invoices
                    .Where(i => i.InvoiceNumber.StartsWith($"{prefix}-{yearMonth}"))
                    .OrderByDescending(i => i.InvoiceNumber)
                    .FirstOrDefault();

                if (lastInvoice != null)
                {
                    var lastNumberPart = lastInvoice.InvoiceNumber.Split('-').Last();
                    if (int.TryParse(lastNumberPart, out int lastSeq))
                    {
                        sequentialNumber = lastSeq + 1;
                    }
                }
            }

            return $"{prefix}-{yearMonth}-{sequentialNumber.ToString("D5")}";
        }

        private async Task<InvoiceDto> MapInvoiceToDto(Invoice invoice)
        {
            var dto = _mapper.Map<InvoiceDto>(invoice);

            // Map invoice items from order items
            if (invoice.Order?.OrderItems != null)
            {
                dto.Items = invoice.Order.OrderItems.Select(oi => new InvoiceItemDto
                {
                    ProductTitle = oi.Product?.Title ?? "Unknown Product",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.Quantity * oi.UnitPrice,
                    Format = oi.Product?.Format.ToString() ?? "Print"
                }).ToList();
            }

            // Get file URL if exists
            if (!string.IsNullOrEmpty(invoice.FilePath))
            {
                dto.FileUrl = await _fileStorageService.GetFileUrlAsync(invoice.FilePath);
            }

            // Map customer details
            if (invoice.Customer != null)
            {
                dto.Customer = new CustomerDto
                {
                    Id = invoice.Customer.Id,
                    Email = invoice.Customer.Email,
                    FullName = invoice.Customer.FullName,
                    PhoneNumber = invoice.Customer.PhoneNumber
                };
            }

            return dto;
        }
    }
}
