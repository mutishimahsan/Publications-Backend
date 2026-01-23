using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Infrastructure.Services
{
    public class PdfService : IPdfService
    {
        private readonly IConfiguration _configuration;

        public PdfService(IConfiguration configuration)
        {
            _configuration = configuration;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GenerateInvoicePdfAsync(Invoice invoice)
        {
            return await Task.Run(() =>
            {
                var order = invoice.Order;
                var customer = invoice.Customer;

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(50);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        // Header with MENTISERA branding
                        page.Header()
                        .AlignCenter()
                        .Column(column =>
                        {
                            column.Item().Text("MENTISERA Publications")
                                .SemiBold().FontSize(24).FontColor(Colors.Blue.Darken3);

                            column.Item().Text("Academic Publishing Store")
                                .FontSize(12).FontColor(Colors.Grey.Medium);
                        });

                        page.Content()
                            .PaddingVertical(40)
                            .Column(column =>
                            {
                                column.Spacing(20);

                                // Invoice Header
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text("BILL TO").SemiBold();
                                        col.Item().Text(customer?.FullName ?? "Customer");
                                        col.Item().Text(customer?.Email ?? "N/A");
                                        if (customer?.PhoneNumber != null)
                                            col.Item().Text($"Phone: {customer.PhoneNumber}");

                                        if (order?.BillingAddress != null)
                                        {
                                            col.Item().PaddingTop(5);
                                            col.Item().Text(order.BillingAddress.FullName);
                                            col.Item().Text(order.BillingAddress.AddressLine1);
                                            if (!string.IsNullOrEmpty(order.BillingAddress.AddressLine2))
                                                col.Item().Text(order.BillingAddress.AddressLine2);
                                            col.Item().Text($"{order.BillingAddress.City}, {order.BillingAddress.Country}");
                                        }
                                    });

                                    row.RelativeItem().AlignRight().Column(col =>
                                    {
                                        col.Item().Text($"INVOICE #: {invoice.InvoiceNumber}").SemiBold();
                                        col.Item().Text($"Date: {invoice.InvoiceDate:dd/MM/yyyy}");
                                        if (invoice.DueDate.HasValue)
                                            col.Item().Text($"Due Date: {invoice.DueDate:dd/MM/yyyy}");
                                        if (order != null)
                                            col.Item().Text($"Order #: {order.OrderNumber}");
                                        col.Item().Text($"Status: {invoice.Status}");
                                    });
                                });

                                column.Item().LineHorizontal(1);

                                // Items Table
                                if (order?.OrderItems != null && order.OrderItems.Any())
                                {
                                    column.Item().Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(3); // Product
                                            columns.RelativeColumn(2); // Format
                                            columns.RelativeColumn();  // Qty
                                            columns.RelativeColumn();  // Price
                                            columns.RelativeColumn();  // Total
                                        });

                                        // Header
                                        table.Header(header =>
                                        {
                                            header.Cell().Element(CellStyle).Text("Product");
                                            header.Cell().Element(CellStyle).Text("Format");
                                            header.Cell().Element(CellStyle).Text("Qty");
                                            header.Cell().Element(CellStyle).AlignRight().Text("Unit Price");
                                            header.Cell().Element(CellStyle).AlignRight().Text("Total");

                                            static IContainer CellStyle(IContainer container)
                                            {
                                                return container
                                                    .DefaultTextStyle(x => x.SemiBold())
                                                    .PaddingVertical(5)
                                                    .BorderBottom(1)
                                                    .BorderColor(Colors.Black);
                                            }
                                        });

                                        // Items
                                        foreach (var item in order.OrderItems)
                                        {
                                            var productFormat = item.Product?.Format.ToString() ?? "Print";
                                            var productTitle = item.Product?.Title ?? "Unknown Product";

                                            table.Cell().Element(CellStyle).Text(productTitle);
                                            table.Cell().Element(CellStyle).Text(productFormat);
                                            table.Cell().Element(CellStyle).AlignCenter().Text(item.Quantity.ToString());
                                            table.Cell().Element(CellStyle).AlignRight().Text($"Rs. {item.UnitPrice:0.00}");
                                            table.Cell().Element(CellStyle).AlignRight().Text($"Rs. {(item.Quantity * item.UnitPrice):0.00}");
                                        }

                                        static IContainer CellStyle(IContainer container)
                                        {
                                            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                                        }
                                    });
                                }

                                // Totals
                                column.Item().AlignRight().Column(col =>
                                {
                                    // Subtotal
                                    col.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text("Subtotal:");
                                        row.ConstantItem(100).Text($"Rs. {invoice.Subtotal:0.00}");
                                    });

                                    // Tax
                                    if (invoice.TaxAmount > 0)
                                    {
                                        col.Item().Row(row =>
                                        {
                                            row.RelativeItem().Text("Tax:");
                                            row.ConstantItem(100).Text($"Rs. {invoice.TaxAmount:0.00}");
                                        });
                                    }

                                    // Separator
                                    col.Item().LineHorizontal(1);

                                    // Total
                                    col.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text("Total Amount:");
                                        row.ConstantItem(100).Text($"Rs. {invoice.TotalAmount:0.00}");
                                    });
                                });

                                // Payment Information - FIXED
                                if (order?.Payments != null && order.Payments.Any())
                                {
                                    var payment = order.Payments.OrderByDescending(p => p.CreatedAt).FirstOrDefault();
                                    if (payment != null)
                                    {
                                        column.Item().PaddingTop(20).Column(paymentInfo =>
                                        {
                                            paymentInfo.Item().Text("Payment Information:").SemiBold();
                                            paymentInfo.Item().Text($"Method: {payment.Method}");
                                            paymentInfo.Item().Text($"Status: {payment.Status}");
                                            paymentInfo.Item().Text($"Reference: {payment.PaymentReference}");
                                            if (payment.ProcessedAt.HasValue)
                                                paymentInfo.Item().Text($"Paid On: {payment.ProcessedAt.Value:dd/MM/yyyy}");
                                        });
                                    }
                                }

                                // Invoice notes - FIXED
                                column.Item().PaddingTop(30).Column(notes =>
                                {
                                    notes.Item().Text("Notes:").SemiBold();
                                    notes.Item().Text("• Payment is due within 30 days of invoice date");
                                    notes.Item().Text("• Late payments may be subject to interest charges");
                                    notes.Item().Text("• Digital products are non-refundable");
                                    notes.Item().Text("• Print books can be returned within 7 days in original condition");
                                });

                                // Terms and conditions - FIXED
                                column.Item().PaddingTop(20).Column(terms =>
                                {
                                    terms.Item().Text("Terms & Conditions:").SemiBold();
                                    terms.Item().Text("1. All prices are in Pakistani Rupees (PKR)");
                                    terms.Item().Text("2. Digital downloads are available immediately after payment confirmation");
                                    terms.Item().Text("3. Print books are shipped within 3-5 business days");
                                    terms.Item().Text("4. For support, contact publications@mentisera.pk");
                                });

                                // Footer with company information - FIXED
                                column.Item().PaddingTop(40).Column(footer =>
                                {
                                    footer.Item().AlignCenter().Row(row =>
                                    {
                                        row.RelativeItem().Column(col =>
                                        {
                                            col.Item().Text("MENTISERA (SMC-Private) Limited").SemiBold();
                                            col.Item().Text("Academic Publications Division");
                                            col.Item().Text("Reg. No: 123456789");
                                            col.Item().Text("Email: publications@mentisera.pk");
                                            col.Item().Text("Website: publications.mentisera.pk");
                                            col.Item().Text("Phone: +92 300 1234567");
                                        });
                                    });

                                    footer.Item().PaddingTop(20).AlignCenter()
                                        .Text("This is a computer-generated invoice. No signature required.")
                                        .Italic().FontColor(Colors.Grey.Medium);
                                });
                            });
                    });
                });

                return document.GeneratePdf();
            });
        }
    }
}
