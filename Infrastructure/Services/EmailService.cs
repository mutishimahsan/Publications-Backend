using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly AppDbContext _context;

        public EmailService(
            IConfiguration configuration,
            ILogger<EmailService> logger,
            IWebHostEnvironment environment,
            AppDbContext context)
        {
            _configuration = configuration;
            _logger = logger;
            _environment = environment;
            _context = context;
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("SmtpSettings");

                if (!smtpSettings.Exists() || string.IsNullOrEmpty(smtpSettings["Host"]))
                {
                    _logger.LogWarning("SMTP settings not configured. Email sending is disabled.");
                    return; // Silently fail in development
                }

                using var client = new SmtpClient(smtpSettings["Host"])
                {
                    Port = int.Parse(smtpSettings["Port"] ?? "587"),
                    Credentials = new NetworkCredential(
                        smtpSettings["Username"],
                        smtpSettings["Password"]),
                    EnableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true")
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(
                        smtpSettings["FromEmail"] ?? "noreply@mentisera.com",
                        smtpSettings["FromName"] ?? "MENTISERA Publications"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };

                mailMessage.To.Add(to);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {To}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {To}", to);
                throw;
            }
        }

        public async Task SendOrderConfirmationAsync(Guid orderId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .Include(o => o.Payments)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null || order.Customer == null)
                {
                    _logger.LogWarning("Order {OrderId} or customer not found for email confirmation", orderId);
                    return;
                }

                var latestPayment = order.Payments?.OrderByDescending(p => p.CreatedAt).FirstOrDefault();
                var paymentMethod = latestPayment?.Method.ToString() ?? "Not Specified";

                var subject = $"Order Confirmation - #{order.OrderNumber}";
                var body = await LoadEmailTemplateAsync("OrderConfirmation", new Dictionary<string, string>
                {
                    ["OrderNumber"] = order.OrderNumber,
                    ["CustomerName"] = order.Customer.FullName,
                    ["OrderDate"] = order.CreatedAt.ToString("dd MMM yyyy"),
                    ["TotalAmount"] = FormatCurrency(order.TotalAmount),
                    ["PaymentMethod"] = GetPaymentMethodDisplay(paymentMethod),
                    ["ShippingAddress"] = FormatAddress(order.ShippingAddress),
                    ["BillingAddress"] = FormatAddress(order.BillingAddress),
                    ["OrderItems"] = FormatOrderItems(order.OrderItems.ToList()),
                    ["SupportEmail"] = _configuration["Contact:SupportEmail"] ?? "support@mentisera.com",
                    ["WebsiteUrl"] = _configuration["AppSettings:BaseUrl"] ?? "https://mentisera.com"
                });

                await SendEmailAsync(order.Customer.Email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order confirmation for order {OrderId}", orderId);
                throw;
            }
        }

        public async Task SendPaymentConfirmationAsync(Guid paymentId)
        {
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Order)
                        .ThenInclude(o => o.Customer)
                    .FirstOrDefaultAsync(p => p.Id == paymentId);

                if (payment == null || payment.Order?.Customer == null)
                {
                    _logger.LogWarning("Payment {PaymentId} or customer not found", paymentId);
                    return;
                }

                var subject = $"Payment Confirmation - #{payment.Order.OrderNumber}";
                var body = await LoadEmailTemplateAsync("PaymentConfirmation", new Dictionary<string, string>
                {
                    ["OrderNumber"] = payment.Order.OrderNumber,
                    ["CustomerName"] = payment.Order.Customer.FullName,
                    ["PaymentDate"] = payment.CreatedAt.ToString("dd MMM yyyy HH:mm"),
                    ["Amount"] = FormatCurrency(payment.Amount),
                    ["PaymentMethod"] = payment.Method.ToString(),
                    ["TransactionId"] = payment.TransactionId ?? "N/A",
                    ["PaymentStatus"] = payment.Status.ToString(),
                    ["ReceiptUrl"] = GetReceiptUrl(payment.Id),
                    ["SupportEmail"] = _configuration["Contact:SupportEmail"] ?? "support@mentisera.com"
                });

                await SendEmailAsync(payment.Order.Customer.Email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending payment confirmation for payment {PaymentId}", paymentId);
                throw;
            }
        }

        public async Task SendInvoiceAsync(Guid invoiceId)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Order)
                        .ThenInclude(o => o.Customer)
                    .FirstOrDefaultAsync(i => i.Id == invoiceId);

                if (invoice == null || invoice.Order?.Customer == null)
                {
                    _logger.LogWarning("Invoice {InvoiceId} or customer not found", invoiceId);
                    return;
                }

                // Get amount paid from related payments
                var payments = await _context.Payments
                    .Where(p => p.OrderId == invoice.OrderId && p.Status == Domain.Enums.PaymentStatus.Completed)
                    .ToListAsync();

                var amountPaid = payments.Sum(p => p.Amount);

                // Calculate discount and grand total
                var discountAmount = 0m; // Default to 0 if you don't have discount logic
                var grandTotal = invoice.TotalAmount - discountAmount;

                var subject = $"Invoice #{invoice.InvoiceNumber}";
                var body = await LoadEmailTemplateAsync("Invoice", new Dictionary<string, string>
                {
                    ["InvoiceNumber"] = invoice.InvoiceNumber,
                    ["CustomerName"] = invoice.Order.Customer.FullName,
                    ["InvoiceDate"] = invoice.CreatedAt.ToString("dd MMM yyyy"),
                    ["DueDate"] = invoice.DueDate?.ToString("dd MMM yyyy"),
                    ["Amount"] = FormatCurrency(invoice.TotalAmount),
                    ["TaxAmount"] = FormatNullableCurrency(invoice.TaxAmount),
                    ["DiscountAmount"] = FormatCurrency(discountAmount),
                    ["GrandTotal"] = FormatCurrency(grandTotal),
                    ["AmountPaid"] = FormatCurrency(amountPaid),
                    ["BalanceDue"] = FormatCurrency(grandTotal - amountPaid),
                    ["Status"] = invoice.Status.ToString(),
                    ["DownloadUrl"] = GetInvoiceDownloadUrl(invoice.Id),
                    ["PaymentInstructions"] = GetPaymentInstructions(),
                    ["SupportEmail"] = _configuration["Contact:SupportEmail"] ?? "support@mentisera.com"
                });

                await SendEmailAsync(invoice.Order.Customer.Email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending invoice {InvoiceId}", invoiceId);
                throw;
            }
        }

        public async Task SendDownloadLinkAsync(Guid orderItemId)
        {
            try
            {
                var orderItem = await _context.OrderItems
                    .Include(oi => oi.Product)
                    .Include(oi => oi.Order)
                        .ThenInclude(o => o.Customer)
                    .FirstOrDefaultAsync(oi => oi.Id == orderItemId);

                if (orderItem == null || orderItem.Order?.Customer == null)
                {
                    _logger.LogWarning("Order item {OrderItemId} or customer not found", orderItemId);
                    return;
                }

                var digitalAccess = await _context.DigitalAccesses
                    .FirstOrDefaultAsync(da => da.OrderItemId == orderItemId && da.IsActive);

                if (digitalAccess == null)
                {
                    _logger.LogWarning("No active digital access found for order item {OrderItemId}", orderItemId);
                    return;
                }

                var subject = $"Download Link for {orderItem.Product.Title}";
                var body = await LoadEmailTemplateAsync("DownloadLink", new Dictionary<string, string>
                {
                    ["ProductTitle"] = orderItem.Product.Title,
                    ["CustomerName"] = orderItem.Order.Customer.FullName,
                    ["AccessExpires"] = digitalAccess.AccessExpiresAt?.ToString("dd MMM yyyy") ?? "Never",
                    ["MaxDownloads"] = digitalAccess.MaxDownloads.ToString(),
                    ["DownloadLink"] = GetDownloadLink(digitalAccess.CurrentToken),
                    ["Instructions"] = GetDownloadInstructions(),
                    ["SupportEmail"] = _configuration["Contact:SupportEmail"] ?? "support@mentisera.com"
                });

                await SendEmailAsync(orderItem.Order.Customer.Email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending download link for order item {OrderItemId}", orderItemId);
                throw;
            }
        }

        public async Task SendPasswordResetAsync(string email, string resetToken)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    _logger.LogWarning("User not found for password reset: {Email}", email);
                    return;
                }

                var resetLink = $"{_configuration["AppSettings:BaseUrl"]}/reset-password?token={resetToken}&email={WebUtility.UrlEncode(email)}";

                var subject = "Password Reset Request - MENTISERA Publications";
                var body = await LoadEmailTemplateAsync("PasswordReset", new Dictionary<string, string>
                {
                    ["CustomerName"] = user.FullName ?? "Customer",
                    ["ResetLink"] = resetLink,
                    ["ExpiryHours"] = "24",
                    ["SupportEmail"] = _configuration["Contact:SupportEmail"] ?? "support@mentisera.com",
                    ["ContactSupport"] = "If you didn't request this password reset, please contact our support team immediately."
                });

                await SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email to {Email}", email);
                throw;
            }
        }

        #region Helper Methods

        private async Task<string> LoadEmailTemplateAsync(string templateName, Dictionary<string, string> variables)
        {
            try
            {
                var templatePath = Path.Combine(_environment.ContentRootPath, "EmailTemplates", $"{templateName}.html");

                if (File.Exists(templatePath))
                {
                    var template = await File.ReadAllTextAsync(templatePath);
                    return ReplaceTemplateVariables(template, variables);
                }

                return CreateDefaultTemplate(templateName, variables);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading email template: {TemplateName}", templateName);
                return CreateDefaultTemplate(templateName, variables);
            }
        }

        private string ReplaceTemplateVariables(string template, Dictionary<string, string> variables)
        {
            var result = template;
            foreach (var variable in variables)
            {
                result = result.Replace($"{{{{{variable.Key}}}}}", variable.Value);
            }
            return result;
        }

        private string CreateDefaultTemplate(string templateName, Dictionary<string, string> variables)
        {
            var content = variables.TryGetValue("Body", out var body) ? body : "Please view this email in an HTML-enabled email client.";

            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>{variables.GetValueOrDefault("Subject", "MENTISERA Publications")}</title>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #1a237e; color: white; padding: 20px; text-align: center; }}
                    .content {{ padding: 30px; background-color: #f9f9f9; }}
                    .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; border-top: 1px solid #ddd; margin-top: 30px; }}
                    .button {{ display: inline-block; background-color: #1a237e; color: white; padding: 12px 30px; text-decoration: none; border-radius: 4px; margin: 15px 0; }}
                    .important {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 4px; margin: 20px 0; }}
                </style>
            </head>
            <body>
                <div class='header'>
                    <h1>MENTISERA Publications</h1>
                </div>
                <div class='content'>
                    {content}
                </div>
                <div class='footer'>
                    <p>&copy; {DateTime.Now.Year} MENTISERA Publications. All rights reserved.</p>
                    <p>This email was sent automatically. Please do not reply to this email.</p>
                    <p>Contact: {variables.GetValueOrDefault("SupportEmail", "support@mentisera.com")}</p>
                </div>
            </body>
            </html>";
        }

        private string GetPaymentMethodDisplay(string? paymentMethod)
        {
            return paymentMethod switch
            {
                "CreditCard" => "Credit Card",
                "DebitCard" => "Debit Card",
                "BankTransfer" => "Bank Transfer",
                "JazzCash" => "JazzCash",
                "EasyPaisa" => "EasyPaisa",
                "CashOnDelivery" => "Cash on Delivery",
                _ => paymentMethod ?? "N/A"
            };
        }

        private string FormatAddress(Address? address)
        {
            if (address == null) return "N/A";

            return $@"
            {address.FullName}<br/>
            {address.AddressLine1}<br/>
            {(!string.IsNullOrEmpty(address.AddressLine2) ? address.AddressLine2 + "<br/>" : "")}
            {address.City}, {address.State} {address.PostalCode}<br/>
            {address.Country}<br/>
            Phone: {address.PhoneNumber}";
        }

        private string FormatOrderItems(List<OrderItem> items)
        {
            var result = "<ul>";
            foreach (var item in items)
            {
                var itemTotal = item.Quantity * item.UnitPrice;
                result += $@"
                <li>
                    <strong>{item.Product.Title}</strong><br>
                    Quantity: {item.Quantity} × {FormatCurrency(item.UnitPrice)} = {FormatCurrency(itemTotal)}
                </li>";
            }
            result += "</ul>";
            return result;
        }

        private string FormatCurrency(decimal amount)
        {
            return amount.ToString("C");
        }

        private string FormatNullableCurrency(decimal? amount)
        {
            return amount?.ToString("C") ?? "$0.00";
        }

        private string GetReceiptUrl(Guid paymentId)
        {
            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://mentisera.com";
            return $"{baseUrl}/payments/{paymentId}/receipt";
        }

        private string GetInvoiceDownloadUrl(Guid invoiceId)
        {
            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://mentisera.com";
            return $"{baseUrl}/invoices/{invoiceId}/download";
        }

        private string GetDownloadLink(string? token)
        {
            if (string.IsNullOrEmpty(token)) return "#";

            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://mentisera.com";
            return $"{baseUrl}/download/{token}";
        }

        private string GetPaymentInstructions()
        {
            return @"<p>Please make payment within 7 days to avoid any late fees. You can pay via:</p>
            <ul>
                <li>Bank Transfer</li>
                <li>Credit/Debit Card</li>
                <li>JazzCash/EasyPaisa</li>
                <li>Cash on Delivery (for eligible orders)</li>
            </ul>";
        }

        private string GetDownloadInstructions()
        {
            return @"<p><strong>Download Instructions:</strong></p>
            <ol>
                <li>Click the download link above</li>
                <li>Save the file to your preferred location</li>
                <li>Open the file using the appropriate software</li>
                <li>If you encounter any issues, contact our support team</li>
            </ol>
            <p><strong>Note:</strong> This download link has limited uses and will expire. Please download and save the file immediately.</p>";
        }

        #endregion
    }
}
